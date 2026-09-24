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
| [A3](./A3-https-forzado.md) | `RequireHttpsMetadata=false` y cookie `Secure` condicional | Alta | Cookie `Secure=true` fijo + HSTS con preload + proxy HTTPS local en Docker |
| [A4](./A4-excepciones-mensaje-interno.md) | Excepciones con mensaje interno al cliente | Alta | Mensajes de negocio pasan, el resto se loggea y se generaliza (13 sitios en 4 controladores) |
| [A5](./A5-trustservercertificate.md) | `TrustServerCertificate=True` en la cadena de conexión | Alta | Plantillas con `Encrypt=True;TrustServerCertificate=False` + advertencia al arrancar fuera de Development |
| [M1](./M1-enumeracion-usuarios.md) | Enumeración de usuarios por mensajes de error diferenciados | Media | Mensaje único + tiempo de respuesta equiparado (BCrypt contra hash señuelo) |
| [M2](./M2-politica-contrasenas.md) | Política de contraseñas débil/no visible | Media | Mínimo 12 caracteres + complejidad (mayúscula/minúscula/número/especial) + lista de ~10k contraseñas comunes (SecLists) + requisito visible en la UI |
| [M3](./M3-auditoria-cambios-rol.md) | Sin validación de reproducción/cambio de rol en `Edit` | Media | Tabla `AuditoriaUsuarios` + registro de quién/cuándo/valor anterior→nuevo en cambios de rol y desactivación |
| [M4](./M4-fallback-usuarioid-cero.md) | `CurrentUserId()` con fallback `"0"` | Media | Filtro global `RequireValidUserIdFilter` — 401 antes de la acción si el claim falta o es inválido |

## Bugs (no son hallazgos del plan de seguridad, pero surgieron durante este trabajo)

| Bug | Título | Detalle |
|---|---|---|
| [B1](./B1-blanco-al-vencer-sesion.md) | Página en blanco al acceder sin sesión válida | Orden de middleware: `UseStatusCodePages` reordenado antes de `UseAuthentication`/`UseAuthorization` |

## Notas generales

- Ningún cambio requirió tocar la lógica de negocio de tickets/inventario/campo/reportes;
  todo el trabajo quedó contenido en autenticación, configuración y el flujo de adjuntos.
- Cada hallazgo se verificó con `dotnet build SistemaTickets.sln` (compilación limpia, sin
  errores) — no existe suite de tests automatizados en el repo (ver `AGENTS.md`), así que la
  verificación funcional real (correr la app, iniciar sesión, subir un adjunto, etc.) queda
  pendiente de que alguien la ejecute contra una base de datos real. La excepción es A3: ahí sí
  se levantó el stack de Docker completo para probar el proxy HTTPS de punta a punta (ver el
  detalle en `A3-https-forzado.md`).
- El detalle "vivo" de cómo funciona cada mecanismo (para que futuras sesiones de Claude Code
  lo entiendan sin releer todo esto) quedó también documentado en `AGENTS.md` y `CLAUDE.md`,
  secciones Auth / Database.
