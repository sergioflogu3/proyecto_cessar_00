# SistemaTickets

Sistema de gestión de tickets de soporte técnico desarrollado en ASP.NET Core 8 MVC. Permite administrar incidentes, asignar agentes, controlar inventario de equipos y repuestos, generar reportes en PDF/Excel, y gestionar visitas técnicas a campo.

## Características

- **Gestión de Tickets**: Creación, asignación, seguimiento de estados y prioridades, comentarios internos/externos, adjuntos.
- **Roles y Permisos**: Administrador, Supervisor, Soporte y Usuario.
- **Inventario**: Control de equipos, herramientas, repuestos y movimientos de stock.
- **Campo / Visitas Técnicas**: Registro de visitas a sedes con consumo de repuestos.
- **Reportes**: Exportación a PDF (QuestPDF) y Excel (ClosedXML).
- **Notificaciones**: Envío de correos electrónicos vía SMTP.
- **Almacenamiento de Archivos**: Adjuntos de tickets en Azure Blob Storage.
- **Autenticación**: JWT Bearer + Cookie dual para API y navegador.

## Tecnologías y Dependencias

| Tecnología | Versión |
|------------|---------|
| .NET | 8.0 |
| ASP.NET Core MVC | 8.0 |
| NHibernate | 5.5.2 |
| FluentNHibernate | 3.4.1 |
| SQL Server | 2012+ |
| Azure.Storage.Blobs | 12.28.0 |
| QuestPDF | 2024.12.0 |
| ClosedXML | 0.102.3 |
| BCrypt.Net-Next | 4.0.3 |

## Requisitos Previos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [SQL Server](https://www.microsoft.com/sql-server) (local o Docker)
- (Opcional) [Docker](https://docs.docker.com/get-docker/) y Docker Compose
- (Opcional) [Azure Storage Emulator](https://learn.microsoft.com/azure/storage/common/storage-use-emulator) o cuenta de Azure Blob Storage para adjuntos

## Configuración

Edita `SistemaTickets/appsettings.json` antes de ejecutar:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=SISTickets;User Id=sa;Password=TuPassword123;TrustServerCertificate=True;"
  },
  "Jwt": {
    "Key": "TU-CLAVE-JWT-SUPER-SEGURA-DE-AL-MENOS-32-CARACTERES",
    "Issuer": "SistemaTickets",
    "Audience": "SistemaTicketsUsers",
    "ExpireMinutes": "30"
  },
  "Encryption": {
    "Key": "TU-CLAVE-AES-256-DE-AL-MENOS-32-CARACTERES"
  },
  "Email": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": "587",
    "EnableSsl": "true",
    "Username": "tu-email@gmail.com",
    "Password": "tu-app-password",
    "FromAddress": "tu-email@gmail.com",
    "FromName": "SistemaTickets - Soporte"
  },
  "AzureBlobStorage": {
    "ConnectionString": "UseDevelopmentStorage=true",
    "ContainerName": "sistema"
  }
}
```

> **Importante**: La primera vez que ejecutes, configura `NHibernate:UpdateSchema` en `true` en `appsettings.json` para crear las tablas automáticamente. **Cámbialo a `false` después del primer arranque exitoso** para evitar migraciones accidentales.

> **Jwt:Key y ConnectionStrings:DefaultConnection son obligatorias y se validan al arrancar**: la app **no inicia** si faltan, o si siguen siendo el valor de ejemplo del repo (`Jwt:Key` además exige mínimo 32 caracteres). `appsettings.json` solo trae placeholders — usa `appsettings.json.example` como referencia y define los valores reales con User Secrets (dev) o variables de entorno `Jwt__Key` / `ConnectionStrings__DefaultConnection` (producción/Docker), nunca en el archivo versionado:
> ```bash
> cd SistemaTickets
> dotnet user-secrets set "Jwt:Key" "$(openssl rand -base64 48)"
> dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=SISTickets;User Id=tu_usuario;Password=tu_password;TrustServerCertificate=True;"
> ```
> Si tu SQL Server local todavía usa el usuario/contraseña que estaban committeados en el repo (`ariel` / `ariel123`), **rótalos ahora**: esas credenciales quedaron expuestas en el historial de git y deben considerarse comprometidas aunque ya no aparezcan en el archivo actual.

## Levantar en Local

### 1. Base de datos SQL Server

Opción A — SQL Server local:
```bash
# Crea la base de datos "SISTickets" manualmente o vía SQL Server Management Studio
```

Opción B — SQL Server en Docker:
```bash
docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=TuPassword123" \
  -p 1433:1433 --name sqlserver \
  -d mcr.microsoft.com/mssql/server:2022-latest
```

### 2. Scripts de base de datos

Ejecuta los scripts en `SistemaTickets/ScriptsDB/` en orden:
1. `users.sql`
2. `tickets.sql`
3. `inventario.sql`
4. `campo_repuestos.sql`
5. `alter_tickets_prioridad_nullable.sql`

### 3. Ejecutar la aplicación

```bash
# Restaurar paquetes
dotnet restore SistemaTickets.sln

# Ejecutar (usa el perfil "https" por defecto — ver nota abajo)
dotnet run --project SistemaTickets/SistemaTickets.csproj

# La aplicación estará disponible en:
# https://localhost:7114 (o el puerto que indique la consola)
```

> **A3**: la cookie de sesión (`jwt`) es `Secure=true` en todo entorno real (Docker,
> producción) — ahí **necesita HTTPS** para funcionar. Localmente (`ASPNETCORE_ENVIRONMENT=
> Development`, que es lo que ponen los tres perfiles de `launchSettings.json`) esa excepción
> no aplica, así que **podés probar con el perfil `http`** sin certificados:
> ```bash
> dotnet run --project SistemaTickets/SistemaTickets.csproj --launch-profile http
> # http://localhost:5213
> ```
> Para probar con HTTPS local (recomendado si querés replicar el comportamiento real), confiá
> el certificado de desarrollo una sola vez — `dotnet dev-certs https --trust` — y corré con
> `--launch-profile https` (o sin flag, ya que es el perfil por defecto).

Usuario por defecto: debes crearlo desde la interfaz o insertarlo directamente en la tabla `Usuarios` (la contraseña debe hashearse con BCrypt).

## Docker

Los archivos Docker ya están incluidos en el repositorio:
- **`Dockerfile`** — Multi-etapa (build + runtime)
- **`docker-compose.yml`** — App + SQL Server + Azurite (emulador Azure Blob Storage) + Caddy (proxy HTTPS)
- **`docker/sql/init.sql`** — Crea la base de datos `SISTickets`
- **`docker/sql/entrypoint.sh`** — Arranca SQL Server y ejecuta el script de inicialización
- **`docker/caddy/Caddyfile`** — Termina TLS con un certificado autofirmado local y reenvía a la app

> **A3**: hay dos formas de entrar, las dos publicadas en `localhost`:
> - **HTTP directo** — `http://localhost:8080` (host y puerto parametrizables en `.env` con
>   `APP_HOST`/`APP_PORT`; por defecto se publica solo en `127.0.0.1`, no en toda la red). Esto
>   funciona porque el servicio `app` corre con `ASPNETCORE_ENVIRONMENT=Development`, que
>   deshabilita el `Secure` de la cookie de sesión (`jwt`) — ver detalle en `AGENTS.md`/
>   `CLAUDE.md`, sección Auth.
> - **HTTPS vía proxy** — `https://localhost:8443`, a través del servicio `caddy` (certificado
>   autofirmado local; el navegador va a advertir que no es de una CA pública, es esperado).
>
> Si cambiás `ASPNETCORE_ENVIRONMENT` a `Production` en `docker-compose.yml` (para simular un
> despliegue real), la cookie vuelve a exigir `Secure=true` siempre y el acceso por HTTP directo
> deja de sostener el login — en ese caso usá el proxy Caddy.

### Instrucciones Docker

```bash
# 1. Asegurar permisos de ejecución al script (Linux/Mac)
chmod +x docker/sql/entrypoint.sh

# 1.1. Crear tu .env local con una clave JWT real (no versionado, ver .env.example)
cp .env.example .env
# Edita .env y define JWT_KEY, por ejemplo:
#   JWT_KEY=$(openssl rand -base64 48)

# 2. Limpiar contenedores anteriores (recomendado si falló antes)
docker-compose down -v

# 3. Construir y levantar (la primera vez creará la BD automáticamente)
docker-compose up -d --build

# 4. Monitorear logs de SQL Server (espera ~30-60 segundos)
docker-compose logs -f sqlserver

# 5. Verificar que la app arrancó
docker-compose logs -f app

# Detener
docker-compose down

# Detener y eliminar volúmenes (borra la BD)
docker-compose down -v
```

> **Nota sobre la primera ejecución**: 
> - El contenedor `sqlserver` tarda ~30-60 segundos en inicializar. El `healthcheck` verifica que la BD `SISTickets` exista antes de arrancar la app.
> - La app no levantará hasta que el healthcheck pase (`condition: service_healthy`).
> - El contenedor `azurite` proporciona el emulador de Azure Blob Storage en el puerto `10000`.
> - Una vez la app arranque con `NHibernate:UpdateSchema: true`, NHibernate creará las tablas automáticamente. 
> - Si prefieres crear las tablas manualmente, ejecuta los scripts de `ScriptsDB/` en orden dentro del contenedor:
> ```bash
> docker exec -it sistickets-db /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'TuPassword123!' -C -d SISTickets -i /ruta/al/script.sql
> ```

## Estructura del Proyecto

```
SistemaTickets/
├── Controllers/          # Controladores MVC
├── Domain/
│   ├── Entities/         # Entidades de dominio
│   ├── Enums/            # Enumeraciones
│   ├── Repositories/     # Interfaces de repositorios
│   └── Services/         # Interfaces de servicios
├── Infrastructure/
│   ├── Persistence/      # NHibernate, mappings, repositorios
│   ├── Security/         # JWT helper
│   └── Services/         # Implementaciones (email, blob, reportes)
├── Models/               # ViewModels por feature
├── Views/                # Vistas Razor
├── ScriptsDB/            # Scripts SQL DDL/DML
├── wwwroot/              # Assets estáticos
├── appsettings.json      # Configuración
└── Program.cs            # Punto de entrada
```

## Seguridad

- **No subas** `appsettings.json` con credenciales reales al repositorio; el archivo versionado solo debe contener placeholders (ver `appsettings.json.example`). `ConnectionStrings:DefaultConnection` se valida al arrancar igual que `Jwt:Key` (ver sección Configuración).
- Las claves `Jwt:Key` y `Encryption:Key` deben tener **al menos 32 caracteres**. `Jwt:Key` se valida al arrancar la app (ver sección Configuración) y el arranque falla si quedó en su valor de ejemplo.
- En producción, usa variables de entorno o Azure Key Vault para secretos.
- La cookie de sesión (`jwt`) se emite con `Secure=true` en todo entorno real (Docker,
  producción) — ahí solo viaja por HTTPS; `docker-compose` ya incluye un proxy Caddy con TLS
  local para esto (ver sección Docker). En `Development` (`ASPNETCORE_ENVIRONMENT`, no el
  esquema de la petición) esa restricción no aplica, para poder probar localmente por HTTP sin
  certificados (ver sección "Ejecutar la aplicación").
- El esquema de base de datos se actualiza automáticamente solo cuando `NHibernate:UpdateSchema` es `true`. Desactívalo en producción.
- Los tokens JWT expiran a los `Jwt:ExpireMinutes` minutos (30 por defecto) y quedan revocados de inmediato al hacer logout o al desactivar un usuario — no hace falta esperar a que expire el token para que deje de aceptarse (ver detalle en `AGENTS.md`/`CLAUDE.md`, sección Auth).
- El login (`/Users/Login` POST) tiene rate limiting por IP (10 solicitudes/minuto) y bloqueo temporal por usuario tras 5 intentos fallidos en 15 minutos, con log de cada intento fallido — mitiga ataques de fuerza bruta/diccionario contra credenciales (ver `AGENTS.md`/`CLAUDE.md`, sección Auth).
- Los adjuntos de tickets solo aceptan una lista blanca de extensiones (pdf, imágenes, doc/docx, xls/xlsx, txt/csv), verificada contra el contenido real del archivo (magic bytes) — el `Content-Type` que manda el navegador se ignora. Se descargan siempre como archivo adjunto (`Content-Disposition: attachment`) con `X-Content-Type-Options: nosniff`, nunca se renderizan inline (ver `AGENTS.md`/`CLAUDE.md`).

## Licencia

Proyecto privado. Todos los derechos reservados.
