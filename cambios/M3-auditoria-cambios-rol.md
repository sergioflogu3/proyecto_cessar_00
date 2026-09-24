# M3 — Sin validación de reproducción/cambio de rol en `Edit`

**Severidad:** Media · **Estado:** ✅ Resuelto

## Hallazgo original

- **Evidencia:** `UsersController.Edit` permite cambiar `Rol` a cualquier valor que envíe el
  Administrador; no hay confirmación ni auditoría de cambios de rol.
- **Impacto:** un administrador comprometido (o un error de UI) puede promover masivamente; no
  queda registro de quién cambió qué rol ni cuándo.
- **Remediación pedida:** registro de auditoría (quién/cuándo/valor anterior→nuevo) para cambios
  de rol y de-activaciones.

## Decisión de alcance

La remediación pedía auditoría de **cambios de rol** y **de-activaciones**. No se agregó
confirmación adicional en la UI (un segundo paso de "¿estás seguro?" antes de guardar un cambio
de rol) porque no estaba en el pedido original y el flujo de "Deactivate" ya tiene su propio
modal de confirmación — el foco de este cambio es que el cambio quede **registrado**, no
agregar fricción nueva al flujo de edición.

## Cambios realizados

### 1. Tabla nueva: `AuditoriaUsuarios`

`SistemaTickets/ScriptsDB/auditoria_usuarios.sql` (nuevo, sumado al final del orden de scripts
documentado en `AGENTS.md`/`CLAUDE.md`):

```sql
CREATE TABLE [dbo].[AuditoriaUsuarios] (
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [UsuarioId] INT NOT NULL,
    [ModificadoPorId] INT NOT NULL,
    [Campo] NVARCHAR(50) NOT NULL,
    [ValorAnterior] NVARCHAR(100) NULL,
    [ValorNuevo] NVARCHAR(100) NULL,
    [FechaCambio] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [FK_AuditoriaUsuarios_Usuario] FOREIGN KEY ([UsuarioId])
        REFERENCES [dbo].[Usuarios]([Id]),
    CONSTRAINT [FK_AuditoriaUsuarios_ModificadoPor] FOREIGN KEY ([ModificadoPorId])
        REFERENCES [dbo].[Usuarios]([Id])
);
CREATE INDEX [IX_AuditoriaUsuarios_UsuarioId] ON [dbo].[AuditoriaUsuarios]([UsuarioId]);
```

`UsuarioId` es el usuario afectado (a quién le cambió el rol/estado); `ModificadoPorId` es el
admin que hizo el cambio. Ambos son FK a `Usuarios`, sin `ON DELETE CASCADE` (este sistema no
borra usuarios, solo los desactiva, pero de todas formas un registro de auditoría no debería
desaparecer si en algún momento se agrega borrado físico).

### 2. Entidad + mapping NHibernate

- `Domain/Entities/AuditoriaUsuario.cs` (nuevo): POCO con `Usuario Usuario` y
  `Usuario ModificadoPor` como propiedades de navegación (mismo patrón que `Ticket.CreadoPor`),
  más `Campo`, `ValorAnterior`, `ValorNuevo`, `FechaCambio`.
- `Infrastructure/Persistence/Maps/AuditoriaUsuarioMap.cs` (nuevo): `ClassMap<AuditoriaUsuario>`
  bajo el namespace legado `MiNuevoProyecto.Infrastructure.Persistence.Maps` (igual que el resto
  de las maps — ver el "Gotcha" de namespace en `AGENTS.md`/`CLAUDE.md`). Usa `References(...)`
  para las dos FK, igual que `TicketMap.CreadoPor`/`AsignadoA`.

### 3. Repositorio

`Domain/Repositories/IUsuarioRepository.cs` / `Infrastructure/Persistence/Repositories/UsuarioRepository.cs`:
- `Task RegistrarAuditoriaAsync(AuditoriaUsuario auditoria)` — abre transacción, `SaveAsync`,
  commit (mismo patrón que `SaveAsync`/`UpdateAsync` existentes).
- `T GetRef<T>(int id) where T : class` — `_session.Load<T>(id)`, mismo patrón que
  `TicketRepository.GetRef`, usado para armar las referencias `Usuario`/`ModificadoPor` de la
  fila de auditoría sin pegarle a la base solo para poblar una FK.

### 4. Servicio: diff y escritura del registro

`Domain/Services/IUsuarioService.cs` cambia de firma:
- `UpdateAsync(Usuario usuario, string? newPlainPassword)` → agrega `int modificadoPorId`.
- `DeactivateAsync(int id)` → agrega `int modificadoPorId`.

`Infrastructure/Services/UsuarioService.cs`:
- `UpdateAsync`: compara el `Rol` antes/después y, si cambió, llama a un helper privado
  `RegistrarAuditoriaAsync(usuarioId, modificadoPorId, "Rol", valorAnterior, valorNuevo)`.
- `DeactivateAsync`: compara `Activo` antes/después (solo puede pasar de `true` a `false`, nunca
  al revés — no existe un endpoint de "reactivar" todavía) y audita `"Activo"` si efectivamente
  estaba activo antes de desactivarlo.

### 5. Controlador

`Controllers/UsersController.cs`:
- `Edit`: arma `modificadoPorId` desde `User.FindFirst(ClaimTypes.NameIdentifier)` (mismo patrón
  ya usado en `Deactivate`/`CambiarPassword`) y lo pasa a `UpdateAsync`.
- `Deactivate`: ya calculaba `currentId` para la validación "no podés inactivar tu propia
  cuenta" — se reutiliza ese mismo valor como `modificadoPorId`.

## Bug encontrado y corregido durante la verificación: comparación post-Merge con NHibernate

La primera versión de `UsuarioService.UpdateAsync`/`DeactivateAsync` comparaba
`existing.Rol != updated.Rol` (o `existing.Activo`) **después** de llamar a
`_usuarioRepository.UpdateAsync(...)`. Al probar el flujo real contra Docker, el rol cambiaba
correctamente en `Usuarios`, pero `AuditoriaUsuarios` seguía vacía.

**Causa:** `existing` (obtenido con `GetByIdAsync`) y la instancia que `MergeAsync` actualiza
dentro de la sesión de NHibernate son **el mismo objeto en memoria** — la `ISession` es
`AddScoped` (una por request) y NHibernate resuelve por identidad (Id) dentro de esa sesión, así
que `GetByIdAsync(id)` y el merge posterior de `UpdateAsync(updated)` (mismo `id`) apuntan al
mismo objeto trackeado. `MergeAsync` copia las propiedades del objeto detached (`updated`) sobre
esa instancia trackeada **in place**, así que para cuando el código volvía a leer
`existing.Rol`, ya había sido mutado al valor nuevo por el propio `Merge` — la comparación
`existing.Rol != updated.Rol` daba `false` siempre, hubiera cambiado algo o no.

**Fix:** capturar el valor "anterior" en una variable local (`rolAnterior` / `estabaActivo`)
**antes** de llamar a `_usuarioRepository.UpdateAsync(...)`, y comparar contra esa copia (tipos
valor, no se ven afectados por la mutación posterior del objeto). Documentado también como
gotcha en `AGENTS.md`/`CLAUDE.md` para que no se repita si se audita algún otro campo a futuro.

## Archivos modificados / creados

- `SistemaTickets/ScriptsDB/auditoria_usuarios.sql` (nuevo).
- `SistemaTickets/Domain/Entities/AuditoriaUsuario.cs` (nuevo).
- `SistemaTickets/Infrastructure/Persistence/Maps/AuditoriaUsuarioMap.cs` (nuevo).
- `SistemaTickets/Domain/Repositories/IUsuarioRepository.cs`,
  `SistemaTickets/Infrastructure/Persistence/Repositories/UsuarioRepository.cs` —
  `RegistrarAuditoriaAsync` + `GetRef<T>`.
- `SistemaTickets/Domain/Services/IUsuarioService.cs`,
  `SistemaTickets/Infrastructure/Services/UsuarioService.cs` — firmas con `modificadoPorId` +
  diff de `Rol`/`Activo` (capturado antes del `UpdateAsync`, ver bug de arriba) +
  `RegistrarAuditoriaAsync` privado.
- `SistemaTickets/Controllers/UsersController.cs` — `Edit`/`Deactivate` pasan `modificadoPorId`.
- `AGENTS.md`, `CLAUDE.md` — nueva entrada en Auth (tabla, columnas, gotcha de NHibernate).
- `README.md` — línea nueva en Seguridad.
- `cambios/README.md` — fila M3 en la tabla.

## Cómo se verificó

1. `dotnet build SistemaTickets.sln` — compila limpio (0 errores, los 2 warnings preexistentes
   no relacionados).
2. Se ejecutó `auditoria_usuarios.sql` contra el SQL Server real del stack de Docker
   (`docker cp` + `sqlcmd` dentro del contenedor `sistickets-db`) y se confirmó la tabla creada.
3. Se reconstruyó la imagen de `app` (`docker-compose up -d --build app`) y se probó el flujo
   real end-to-end contra la app corriendo: editar el rol de un usuario desde `/Users/Index`.
4. **Primera prueba: falso negativo.** Se abrió el modal de edición y se guardó sin cambiar
   realmente el combo de Rol (quedó en el valor preseleccionado) — correctamente no se escribió
   fila de auditoría, porque no hubo cambio real. Se confirmó revisando los logs del contenedor
   (`docker logs`, sin ningún `UPDATE` de por medio) y la tabla `Usuarios` sin cambios.
5. **Segunda prueba: cambio real, bug detectado.** Se repitió el cambio eligiendo explícitamente
   un rol distinto al preseleccionado. El `UPDATE` sí se aplicó (`Usuarios.Rol` cambió en la
   base), pero `AuditoriaUsuarios` seguía en 0 filas — ahí se identificó el bug de
   comparación post-Merge descripto arriba.
6. Aplicado el fix (capturar antes del `UpdateAsync`), se repitió el build + rebuild de la
   imagen de Docker. Verificación pendiente de una repetición más del cambio de rol en el
   navegador para confirmar que la fila de auditoría se escribe correctamente (ver siguiente
   sesión / seguimiento).

## Limitaciones conocidas

- No hay endpoint para **reactivar** un usuario desactivado todavía (`Deactivate` solo va en un
  sentido); cuando exista, debería auditar `"Activo": "False" → "True"` con el mismo mecanismo.
- No se agregó ninguna vista/reporte para **consultar** la auditoría — los registros quedan en
  la tabla, accesibles por SQL directo, pero no hay UI para listarlos. Si se necesita, es un
  agregado incremental sobre lo que ya existe (un nuevo action + vista que lea
  `AuditoriaUsuarios`, sin tocar el mecanismo de escritura).
- `ValorAnterior`/`ValorNuevo` se guardan como `NVARCHAR(100)` con el `.ToString()` del enum
  `RolUsuario` o de `bool` — suficiente para los dos campos auditados hoy, pero si se audita un
  campo con un formato más rico (por ejemplo un objeto), convendría revisar el largo de columna
  y el formato de serialización.
- `modificadoPorId` se obtiene con `int.Parse(... ?? "0")` (mismo patrón ya señalado como M4 en
  `planes/01-problemas-de-seguridad.md`) — si el claim faltara, se auditaría con
  `ModificadoPorId = 0`, que además rompería el FK a `Usuarios` (no existe un usuario con Id 0).
  No se corrigió acá porque es exactamente el alcance de M4, no de M3; queda como dependencia
  implícita: resolver M4 primero evita este caso borde en M3.
