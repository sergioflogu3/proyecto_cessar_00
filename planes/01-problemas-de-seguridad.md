# Análisis de Riesgos de Seguridad — SistemaTickets

> Documento de catálogo de vulnerabilidades detectadas por revisión de código.
> Severidad: **Crítica · Alta · Mediana · Baja**
> Basado en el código fuente tal como está en el repositorio.

---

## Resumen ejecutivo

| Severidad | Cantidad | Riesgo principal |
|---|---|---|
| 🔴 Críticas | 3 | Falsificación de identidad (JWT), secretos en el repo |
| 🟠 Altas | 5 | Fuerza bruta, adjuntos sin filtrar, sin HTTPS garantizado |
| 🟡 Medianas | 6 | Enumeración de usuarios, fugas de información, debilidad de contraseñas |
| 🟢 Bajas | 4 | Cabeceras de seguridad, dependencias, configuración menor |

---

## 🔴 CRÍTICAS

### C1. Clave JWT con valor placeholder / predecible
- **Evidencia:** `SistemaTickets/appsettings.json:6` — `"Key": "REEMPLAZAR-CON-CLAVE-JWT-SEGURA-MINIMO-32-CARACTERES-AQUI"`. `Infrastructure/Security/JwtHelper.cs:19` lee la clave con `jwtSection["Key"]!` **sin validar longitud ni que fue reemplazada**. `Program.cs` tampoco la valida al arrancar.
- **Impacto:** si se despliega sin reemplazar la clave (error humano típico), cualquier atacante que haya visto el repo puede **firmar tokens JWT válidos con rol `Administrador`** y tomar control total del sistema (ver historial, todos los tickets, crear admins).
- **Remediación:**
  1.Validar al arranque (`Program.cs`): fallar/morir si la clave no existe, tiene < 32-64 caracteres o es igual al placeholder.
  2. Guardar la clave real en variables de entorno / Azure Key Vault / User Secrets, **nunca en el repo**.

### C2. Cadena de conexión con credenciales reales committeada
- **Evidencia:** `SistemaTickets/appsettings.json:3` — `User Id=ariel;Password=ariel123` en texto plano dentro del repositorio.
- **Impacto:** acceso directo a la base de datos por cualquier persona con acceso al código (compañeros de aula, GitHub público, etc.). Con `NHibernate:UpdateSchema=true` además podría alterar el esquema.
- **Remediación:** mover la conexión a variables de entorno / Key Vault, dejar en el repo solo un `appsettings.json.example` con placeholders, y **rotar la contraseña de `ariel`** (ya está expuesta).

### C3. Token JWT no revocable + desactivación de usuario sin efecto inmediato
- **Evidencia:** `Controllers/UsersController.cs:74-79` (Logout solo borra la cookie; el JWT sigue vigente), `Program.cs:79` (`ClockSkew = TimeSpan.Zero`, validación solo por fecha), `UsersController.Deactivate` (no invalida sesiones).
- **Impacto:** si un usuario es **desactivado por mala práctica**, o se roba un token, este sigue siendo aceptado durante hasta 2 horas; el "logout" del usuario cierra la sesión no cierra nada del lado del servidor.
- **Remediación:** corto plazo: reducir `ExpireMinutes` a 15-30; mediano: lista de revocación (tabla `RevokedTokens` o cache en memoria consultada en `OnTokenValidated`) y validar en cada request que el usuario sigue activo (claim `jti` + estado en BD).

---

## 🟠 ALTAS

### A1. Sin límite de intentos de login (fuerza bruta)
- **Evidencia:** `Controllers/UsersController.cs:39-49` — `ValidateCredentialsAsync` se invoca sin ningun mecanismo de rate limiting, bloqueo temporal o CAPTCHA; no hay registro de intentos fallidos.
- **Impacto:** ataque de diccionario/fuerza bruta en línea sin fricción contra las credenciales de `Administrador`.
- **Remediación:** rate limiting por IP+usuario (ASP.NET Core `RateLimiter` middleware), bloqueo temporal después de N intentos, log de intentos fallidos.

### A2. Adjuntos sin lista blanca de tipos/extensión
- **Evidencia:** `Controllers/TicketsController.cs:122-131` solo valida **tamaño** (10 MB); el `ContentType` y nombre vienen del cliente (`file.ContentType ?? "application/octet-stream"`), no hay whitelist ni escaneo.
- **Impacto:** un usuario puede subir `payload.html`/`payload.svg` (XSS almacenado si el blob se sirve inline), o archivos ejecutables/ofimáticos con macros.
- **Remediación:** lista blanca de extensiones/MIME (pdf, imágenes, docx, xlsx...), validar magic bytes, y servir adjuntos con `Content-Disposition: attachment` + `X-Content-Type-Options: nosniff`.

### A3. `RequireHttpsMetadata = false` y cookie `Secure` condicional
- **Evidencia:** `Program.cs:63`, `UsersController.cs:65` (`Secure = HttpContext.Request.IsHttps`).
- **Impacto:** si el sitio se accede por HTTP en algún entorno (puerto local, proxy mal configurado), el JWT viaja sin cifrar y la cookie se envía igual → robo de sesión por MITM/red local.
- **Remediación:** forzar `Secure = true` siempre; exigir HTTPS en todo el despliegue; considerar HSTS con `preload`.

### A4. Excepciones con mensaje interno al cliente
- **Evidencia:** `UsersController.cs:183` (`errors = new[] { ex.Message }`), `TicketsController.cs:105` (patrón idéntico en varios controladores AJAX).
- **Impacto:** mensajes de excepción de NHibernate/SqlClient revelan nombres de tablas, columnas y a veces fragmentos de SQL → facilita reconocimiento del backend.
- **Remediación:** loggear la excepción completa en el server (ILogger) y devolver un mensaje genérico al cliente.

### A5. `TrustServerCertificate=True` en la cadena de conexión
- **Evidencia:** `appsettings.json:3`.
- **Impacto:** el cliente no valida el certificado TLS del SQL Server → un MITM dentro de la red puede interceptar consultas/credenciales de BD.
- **Remediación:** usar certificado válido para el SQL Server y quitar el flag (o `Encrypt=True;TrustServerCertificate=False`).

---

## 🟡 MEDIANAS

### M1. Enumeración de usuarios por mensajes de error diferenciados
- **Evidencia:** `UsersController.cs:47` ("Credenciales inválidas...") vs `UsersController.cs:54` ("No se encontró el usuario.") — mensajes distintos permiten saber si un login existe.
- **Remediación:** un único mensaje genérico para ambos casos; mismo tiempo de respuesta.

### M2. Política de contraseñas débil/no visible
- **Evidencia:** `UsersController.Create` solo valida que la contraseña no esté vacía (`:113`); no se observan requisitos de longitud/complejidad ni lista de contraseñas filtradas.
- **Remediación:** mínimo 10-12 caracteres, validación de complejidad o contraseñas pass-phrase, y comparar contra top-10k filtradas.

### M3. Sin validación de reproducción/cambio de rol en `Edit`
- **Evidencia:** `UsersController.Edit` permite cambiar `Rol` a cualquier valor que envíe el Administrador; no hay confirmación ni auditoría de cambios de rol.
- **Impacto:** un administrador comprometido (o un error de UI) puede promover masivamente; no queda registro de quién cambó qué rol ni cuándo.
- **Remediación:** registro de auditoría (quién/cuándo/valor anterior→nuevo) para cambios de rol y de-activaciones.

### M4. `CurrentUserId()` con fallback `"0"`
- **Evidencia:** `UsersController.cs:194,207`, `TicketsController.cs:34-35` — `int.Parse(... ?? "0")`.
- **Impacto:** si el claim falta o está corrupto, se crean registros con `Usuario Id=0` en lugar de rechazar la petición — datos huérfanos y confusión de trazabilidad.
- **Remediación:** si el claim es inválido, devolver 401/403 y no continuar.

### M5. `Logout` por GET sin protección anti-CSRF
- **Evidencia:** `UsersController.cs:74-78`.
- **Impacto:** una imagen/enlace externo `</...s>/Users/Logout>` (link) puede cerrar la sesión de un usuario sin su consentimiento (CSRF de bajo impacto, pero denegación de servicio molesta).
- **Remediación:** convertir el logout a POST con token anti-forgery, o validar `SameSite`.

### M6. SMIME/SMTP y Blob con credenciales futuras en texto plano
- **Evidencia:** `appsettings.json` secciones `Email` y `AzureBlobStorage` (hoy vacías / `UseDevelopmentStorage`, pero el diseño contempla password de Gmail en config).
- **Impacto:** en cuanto se llenen con credenciales reales quedan committeadas (igual que C2).
- **Remediación:** mismas que C2: secretos fuera del repo desde el día 1 (Key Vault, variables de entorno).

---

## 🟢 BAJAS

### B1. Sin cabeceras de seguridad HTTP
- **Evidencia:** no hay middleware que agregue `Content-Security-Policy`, `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy` ni `Permissions-Policy`.
- **Remediación:** agregar un middleware de cabeceras en `Program.cs` (5 líneas, impacto grande en XSS/iframes).

### B2. Dependencias sin revisión de CVEs / sin escaneo automático
- **Evidencia:** `SistemaTickets.csproj` (NHibernate 5.5.2, SqlClient 7.0.0, QuestPDF 2024.12.0...); no hay CI ni `dotnet list package --vulnerable` automatizado.
- **Remediación:** actualizar minor versions y añadir análisis de dependencias (GitHub Dependabot / `dotnet list package --vulnerable` en CI).

### B3. `AllowedHosts: "*"`
- **Evidencia:** `appsettings.json:33`.
- **Impacto:** permite host-header injection si el despliegue está mal aislado.
- **Remediación:** restringir al dominio real publicado.

### B4. Logs sin política de saneamiento
- **Evidencia:** nivel `Information` global (`appsettings.json:28`); no hay revisión de qué queda registrado en logs de excepciones (podrían caer datos de tickets o tokens `SaveToken = true`, `Program.cs:64`).
- **Remediación:** revisar qué se loguea; evitar loguear claims/tokens; nivel `Warning` en producción para `Microsoft` namespace.

---

## Plan de remediación sugerido (orden de ejecución)

1. **Hoy mismo:** rotar contraseña BD (`C2`), validators de clave JWT al arranque (`C1`).
2. **Esta semana:** rate limiting en login (`A1`), whitelist de adjuntos (`A2`), mensajes de error genéricos (`A4`), `Secure=true` siempre (`A3`).
3. **Este mes:** revocación de tokens (`C3`), política de contraseñas (`M2`), cabeceras de seguridad (`B1`), secretos en Key Vault (`C2/M6`).
4. **Trabajo futuro:** auditoría de cambios de rol (`M3`), CI con escaneo de dependencias (`B2`), 2FA.

---

*Método: revisión manual de código (no hay tests ni escáner automatizado en el proyecto). Cada hallazgo incluye archivo y línea para verificación directa; re-ejecutar tras cada remediación.*
