# MEMORY.md — SistemaTickets
Memoria del proyecto entre sesiones. Máximo ~50 líneas: resume o elimina lo que ya no
aporte. Detalle técnico completo en `CLAUDE.md`; aquí solo estado, decisiones y pendientes.

## Estado actual
- App help-desk ASP.NET Core 8 MVC (dominio en español): tickets, inventario, visitas
  técnicas, reportes (PDF/Excel) y gestión de usuarios/roles.
- Rama de trabajo: `developer` (principal: `main`).
- Hardening de seguridad aplicado: hallazgos A1–A5, B1–B4, C1–C3, M1–M6
  (writeups en `cambios/`, catálogo en `planes/01-problemas-de-seguridad.md`).
- Íconos migrados a FontAwesome; README actualizado. Sin tests, sin CI instalado, sin 2FA.
- Constitución con 6 principios en `docs/constitution.md`; `AGENTS.md` y `CLAUDE.md` en español.
- MCP de Chrome DevTools y Context7 en `.mcp.json` (requieren reiniciar Claude Code y aprobarlos;
  sin ellos, el contenido se verifica comparando HTML capturado con curl).

## Decisiones (y por qué)
- Specs en `/specs` (raíz, no en `docs/`); la spec manda sobre el código. 001-deudaTecnica IMPLEMENTADA y cerrada (14/10/2026): las 4 vistas de detalle usan ViewModel propio, búsqueda de verificación en 0, deuda retirada de la constitución, deuda adyacente listada al final de `tasks.md`. Commits 4def1f1, 44c149c, 20edeba, 29dea95; cierre documental sin commit. Volumen no medido en navegador.
- DevTools: `resize_page` no baja de ~500 px; usar `emulate` con viewport `375x800x2,mobile,touch`.
- Un solo proyecto web en capas, DI toda en `Program.cs`: simple de desplegar y de seguir.
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
- Fallos de permisos conocidos (herramienta sin filtro por técnico, VisitaCambiarEstado sin propietario): leer el atributo de cada acción, no el vecino.
- Login limitado a 10 intentos/min por IP: al capturar HTML por rol, espaciar las ejecuciones
  (~60 s) o los logins fallan en silencio.

## Próximos pasos
- Abrir incidencias/specs para la deuda adyacente de `specs/001-deudaTecnica/tasks.md` (2 fallos de permisos en herramienta y visita primero).
- Especificar el resto del comportamiento en `/specs` (principio 2).
- Decidir si instalar CI (`planes/ci.yml` → `.github/workflows/`) o Dependabot (B2).
- Añadir proyecto de tests (hoy `dotnet build` + prueba manual son la única validación).
- Persistir revocación de tokens e intentos de login si se escala; evaluar 2FA.
