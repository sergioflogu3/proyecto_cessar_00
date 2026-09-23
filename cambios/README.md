# Registro de cambios — Remediación de hallazgos de seguridad

Esta carpeta documenta, hallazgo por hallazgo, lo que se implementó en el código para
resolver los problemas de seguridad catalogados en `planes/01-problemas-de-seguridad.md`.
Cada archivo cubre un hallazgo: qué se encontró, qué se cambió, en qué archivos, y qué
limitaciones quedaron conscientemente pendientes.

Todos los cambios se hicieron con el mismo criterio: **resolver el hallazgo sin romper la
funcionalidad existente** del sistema.

| Hallazgo | Título | Severidad | Detalle |
|---|---|---|---|
| [C1](./C1-clave-jwt-placeholder.md) | Clave JWT con valor placeholder / predecible | Crítica | Validación al arrancar + secretos fuera del repo |
| [C2](./C2-credenciales-bd-committeadas.md) | Cadena de conexión con credenciales reales committeada | Crítica | Placeholder + validación al arrancar + rotación pendiente del lado del usuario |
| [C3](./C3-jwt-no-revocable.md) | Token JWT no revocable + desactivación sin efecto inmediato | Crítica | Revocación server-side (`jti`) + verificación de usuario activo por request |
| [A1](./A1-fuerza-bruta-login.md) | Sin límite de intentos de login (fuerza bruta) | Alta | Rate limiting por IP + bloqueo temporal por usuario + logging |
| [A2](./A2-adjuntos-sin-whitelist.md) | Adjuntos sin lista blanca de tipos/extensión | Alta | Whitelist + magic bytes + descarga forzada con `nosniff` |

## Notas generales

- Ningún cambio requirió tocar la lógica de negocio de tickets/inventario/campo/reportes;
  todo el trabajo quedó contenido en autenticación, configuración y el flujo de adjuntos.
- Cada hallazgo se verificó con `dotnet build SistemaTickets.sln` (compilación limpia, sin
  errores) — no existe suite de tests automatizados en el repo (ver `AGENTS.md`), así que la
  verificación funcional real (correr la app, iniciar sesión, subir un adjunto, etc.) queda
  pendiente de que alguien la ejecute contra una base de datos real.
- El detalle "vivo" de cómo funciona cada mecanismo (para que futuras sesiones de Claude Code
  lo entiendan sin releer todo esto) quedó también documentado en `AGENTS.md` y `CLAUDE.md`,
  secciones Auth / Database.
