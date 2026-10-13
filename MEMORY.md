# MEMORY.md — SistemaTickets
Memoria del proyecto entre sesiones. Máximo ~50 líneas: resume o elimina lo que ya no
aporte. Detalle técnico completo en `CLAUDE.md`; aquí solo estado, decisiones y pendientes.

## Estado actual
- App help-desk ASP.NET Core 8 MVC (dominio en español): tickets, inventario, visitas
  técnicas, reportes (PDF/Excel) y gestión de usuarios/roles.
- Rama de trabajo: `developer` (principal: `main`).
- Hardening de seguridad aplicado: hallazgos A1–A5, B1–B4, C1–C3, M1–M6
  (writeups en `cambios/`, catálogo en `planes/01-problemas-de-seguridad.md`).
- Migración de íconos Bootstrap → FontAwesome completada (dashboard, Usuarios, Configuración).
- README actualizado para otros ambientes de desarrollo.
- Sin tests automatizados, sin CI instalado, sin 2FA.
- Constitución con 6 principios en `docs/constitution.md` (stack, specs, lógica/UI, tests,
  datos, idioma). `AGENTS.md` y `CLAUDE.md` ahora están en español.
- MCP de Chrome DevTools y Context7 en `.mcp.json`.

## Decisiones (y por qué)
- Specs en `/specs` (raíz, no en `docs/`); la spec manda sobre el código. 001-deudaTecnica aprobada, rev. 2 tras QA (sin implementar).
- Tests: sin instalar dependencias; se verifica con `dotnet build` + Chrome DevTools.
- Un solo proyecto web en capas (Controllers → Domain → Infrastructure), DI toda en
  `Program.cs`: simple de desplegar y de seguir.
- NHibernate + FluentNHibernate sobre SQL Server; `NHibernate:UpdateSchema=true` solo en el
  primer despliegue, luego `false` para evitar deriva de esquema.
- JWT dual (Bearer o cookie `jwt`) con revocación en memoria: logout/desactivación surten
  efecto en la siguiente petición.
- Secretos nunca en `appsettings.json` (user-secrets en dev, env vars en Docker/prod); el
  arranque falla si `Jwt:Key` o la cadena de conexión son placeholder.
- ViewModels en `Models/`: las vistas nunca se enlazan a entidades (evita over-posting).
- `planes/ci.yml` queda como borrador sin instalar: activarlo ejecutaría Actions en cada
  push/PR, decisión del usuario, no hacerlo sin pedirlo.
- En Docker, `app` corre en `Development` para que `http://localhost:8080` sostenga el login;
  Caddy da HTTPS local en `https://localhost:8443`.

## Aprendizajes y errores a evitar
- Mapas FluentNHibernate usan namespace `MiNuevoProyecto.*` (herencia): no "corregirlo".
- Auditoría de usuarios: capturar el valor "antes" en una variable antes de `UpdateAsync`
  (identity map de NHibernate devuelve la misma instancia).
- `UseStatusCodePages` debe registrarse antes de `UseAuthentication`/`UseAuthorization`.
- Tras desplegar con dominio real, actualizar `AllowedHosts` o todo da 400.
- Queries LINQ de NHibernate: `.Fetch(...)` en pasos separados (evita producto cartesiano).
- En `catch` de acciones AJAX usar `JsonError(ex, accion)`, nunca devolver `ex.Message`.
- Cambiar el `.txt` de contraseñas comunes exige actualizar también su `LogicalName`.

## Próximos pasos
- Implementar `specs/001-deudaTecnica` (pantalla a pantalla): 4 vistas ligadas a entidades (principio 3).
- Especificar el resto del comportamiento en `/specs` (principio 2).
- Decidir si instalar CI (`planes/ci.yml` → `.github/workflows/`) o Dependabot (B2).
- Añadir proyecto de tests (hoy `dotnet build` + prueba manual son la única validación).
- Persistir revocación de tokens e intentos de login si se escala a varias instancias.
- Evaluar 2FA y revisar `planes/03-plan-arquitectura.md` para mejoras de arquitectura.
