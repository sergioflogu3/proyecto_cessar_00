# M6 — SMIME/SMTP y Blob con credenciales futuras en texto plano

**Severidad:** Media · **Estado:** ✅ Resuelto

## Hallazgo original

- **Evidencia:** `appsettings.json` secciones `Email` y `AzureBlobStorage` (hoy vacías /
  `UseDevelopmentStorage`, pero el diseño contempla password de Gmail en config).
- **Impacto:** en cuanto se llenen con credenciales reales quedan committeadas (igual que C2).
- **Remediación pedida:** mismas que C2: secretos fuera del repo desde el día 1 (Key Vault,
  variables de entorno).

## Diagnóstico: no había nada que "limpiar" hoy, pero sí dos huecos reales

A diferencia de C2 (que tenía una contraseña real committeada, `ariel/ariel123`), acá
`appsettings.json` ya estaba limpio: `Email:Username`/`Email:Password` vacíos,
`AzureBlobStorage:ConnectionString` en `UseDevelopmentStorage=true` (el valor correcto para
Azurite local, no una credencial real). El hallazgo es **preventivo**: que el día que alguien
necesite credenciales reales, el camino de menor esfuerzo sea el seguro (env vars/secret
manager), no editar `appsettings.json` directamente. Revisando el código se encontraron dos
huecos concretos que hacían eso más probable de lo que debería:

1. **`AzureBlobStorage:ConnectionString` no tenía ninguna validación al arrancar** (a diferencia
   de `Jwt:Key`/`ConnectionStrings:DefaultConnection`, que sí fallan rápido con un mensaje claro
   si faltan). Si en un despliegue real esta variable quedara vacía, el primer síntoma sería una
   excepción críptica de `Azure.Storage.Blobs` recién cuando alguien subiera un adjunto (el
   servicio es `AddSingleton`, se construye recién al resolverse por primera vez) — un mensaje
   de error mucho peor que el de C1/C2, y en el peor momento (con un usuario esperando).
2. **`docker-compose.yml` no tenía ningún wiring para `Email:*`.** Si alguien necesitara mandar
   correos reales desde el stack de Docker, la forma más rápida (y la más tentadora) hubiera
   sido escribir el usuario/password de Gmail directo en `docker-compose.yml` — que si está
   versionado, es exactamente el mismo problema que C2.

## Cambios realizados

### 1. Validación al arrancar para `AzureBlobStorage:ConnectionString`

`SistemaTickets/Program.cs` — nuevo `ValidateAzureBlobStorageConnectionString`, mismo patrón que
`ValidateJwtKey`/`ValidateConnectionString` (C1/C2), invocado al principio de `Main` (antes de
la validación de la cadena de conexión a la BD):

```csharp
private static void ValidateAzureBlobStorageConnectionString(string? connectionString)
{
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException(
            "AzureBlobStorage:ConnectionString no está configurada. Defínela mediante variable de " +
            "entorno (AzureBlobStorage__ConnectionString), User Secrets o Azure Key Vault antes de " +
            "iniciar la aplicación.");
    }
}
```

**Diferencia deliberada con C1/C2**: no hay chequeo de texto "placeholder" (`REEMPLAZAR`, etc.).
El valor committeado por defecto (`UseDevelopmentStorage=true`) es una cadena de conexión **real
y funcional** para el emulador local, no algo que haya que reemplazar en desarrollo — a
diferencia de `Jwt:Key`/la cadena de conexión a SQL Server, donde el valor committeado es
deliberadamente inválido. Solo se puede validar de forma genérica que no esté vacía; que sea la
cadena correcta para el entorno (Azurite en dev/Docker vs. una cuenta real de Azure Storage en
producción) es responsabilidad de quien despliega, no algo verificable estáticamente.

`Email:*` **no** recibió una validación equivalente — es genuinamente opcional
(`EmailService.SendTicketCopyAsync` ya se salta el envío, con un log en `Debug`, si
`Email:SmtpHost` está vacío) y no bloquea la creación de tickets. Agregar un fail-fast ahí
rompería el arranque para cualquiera que no quiera configurar SMTP, que es el caso por defecto.

### 2. `docker-compose.yml`: `Email:*` y el Blob real, vía `.env`

Se agregó el wiring de las 7 claves de `Email:*` como variables de entorno, todas con default
vacío/no-op (mismo comportamiento actual si no se completan):

```yaml
Email__SmtpHost: "${EMAIL_SMTP_HOST:-smtp.gmail.com}"
Email__SmtpPort: "${EMAIL_SMTP_PORT:-587}"
Email__EnableSsl: "${EMAIL_ENABLE_SSL:-true}"
Email__Username: "${EMAIL_USERNAME:-}"
Email__Password: "${EMAIL_PASSWORD:-}"
Email__FromAddress: "${EMAIL_FROM_ADDRESS:-}"
Email__FromName: "${EMAIL_FROM_NAME:-SistemaTickets - Soporte}"
```

Y `AzureBlobStorage__ConnectionString` pasó de un valor hardcodeado a uno parametrizable con el
mismo default (la well-known key pública de Azurite, documentada por Microsoft — **no** es un
secreto, es la misma en cualquier instalación de Azurite del mundo, por eso puede seguir
hardcodeada como default):

```yaml
AzureBlobStorage__ConnectionString: "${AZURE_BLOB_CONNECTION_STRING:-DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://azurite:10000/devstoreaccount1;}"
```

Con esto, si en algún momento hace falta un Azure Storage real o SMTP real desde Docker, las
credenciales van en el `.env` local (gitignored) y nunca se tocan `docker-compose.yml` ni
`appsettings.json`.

### 3. `.env.example`: documentar las variables opcionales

Se agregaron `EMAIL_USERNAME`, `EMAIL_PASSWORD`, `EMAIL_FROM_ADDRESS` y
`AZURE_BLOB_CONNECTION_STRING`, todas vacías por defecto, con comentarios explicando que son
opcionales (Email) o solo necesarias para reemplazar el emulador local (Azure Blob) — mismo
estilo que la entrada ya existente de `JWT_KEY`.

## Archivos modificados

- `SistemaTickets/Program.cs` — `ValidateAzureBlobStorageConnectionString` (nuevo) + invocación
  al inicio de `Main`.
- `docker-compose.yml` — `Email__*` (7 claves nuevas) + `AzureBlobStorage__ConnectionString`
  parametrizado vía `.env`.
- `.env.example` — `EMAIL_USERNAME`, `EMAIL_PASSWORD`, `EMAIL_FROM_ADDRESS`,
  `AZURE_BLOB_CONNECTION_STRING` (nuevas, vacías, documentadas).
- `AGENTS.md`, `CLAUDE.md` — sección External dependencies actualizada.
- `README.md` — línea nueva en Seguridad.
- `cambios/README.md` — fila M6 en la tabla.

## Cómo se verificó

1. `dotnet build SistemaTickets.sln` — compila limpio (0 errores).
2. `docker-compose build app` — reconstruida la imagen con el cambio.
3. `docker run --rm -e AzureBlobStorage__ConnectionString="" sistematickets-master-app:latest`
   → falla rápido con el mensaje esperado:
   ```
   Unhandled exception. System.InvalidOperationException: AzureBlobStorage:ConnectionString
   no está configurada. Defínela mediante variable de entorno
   (AzureBlobStorage__ConnectionString), User Secrets o Azure Key Vault antes de iniciar la
   aplicación.
   ```
   (Se verificó primero, por error, contra la imagen vieja — sin este fix seguía de largo hasta
   fallar más adelante al conectar a NHibernate con una cadena de conexión inventada; confirma
   que la validación nueva corta antes, en el punto correcto.)
4. `docker-compose up -d app` con la configuración real (sin overrides) → arranca sano.
   `curl http://localhost:8080/Users/Login` → `200`. `docker logs sistickets-app` sin errores.
5. No se probó el envío de un correo real (no hay credenciales de Gmail disponibles en este
   entorno) — la validación es sobre la infraestructura (que las variables existan y se puedan
   completar sin tocar el repo), no sobre el envío en sí, que ya estaba probado y fuera de
   alcance de este hallazgo.

## Limitaciones conocidas

- Igual que en C1/C2, **no hay manera de detectar en runtime si un valor de configuración vino
  de `appsettings.json` (committeado) o de una variable de entorno** — `IConfiguration` los
  combina de forma transparente. La validación de arranque solo puede garantizar "hay un valor
  utilizable", no "el valor no está commiteado en texto plano en algún archivo". La prevención
  real depende de que nadie edite `appsettings.json`/`docker-compose.yml` a mano con un secreto
  real — de ahí que el foco de este cambio haya sido dejar el camino fácil (`.env`) ya armado,
  no un chequeo automático imposible de implementar de forma confiable.
- No se agregó escaneo de secretos en CI (gitleaks o similar) — el repo no tiene CI instalado
  todavía (`planes/02-ci-cd-plan.md` es un borrador, ver `CLAUDE.md`) y no estaba en el alcance
  de la remediación pedida para M6 ("mismas que C2"). Si se implementa CI, agregar un paso de
  escaneo de secretos sería una mejora natural sobre este trabajo.
