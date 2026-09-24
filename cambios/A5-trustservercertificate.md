# A5 — `TrustServerCertificate=True` en la cadena de conexión

**Severidad:** Alta · **Estado:** ✅ Resuelto

## Hallazgo original

- **Evidencia:** `appsettings.json:3`.
- **Impacto:** el cliente no valida el certificado TLS del SQL Server → un MITM dentro de la red
  puede interceptar consultas/credenciales de BD.
- **Remediación pedida:** usar certificado válido para el SQL Server y quitar el flag (o
  `Encrypt=True;TrustServerCertificate=False`).

## Por qué esto no es un fail-fast como C1/C2

`Jwt:Key`/`ConnectionStrings:DefaultConnection` vacíos o con el placeholder del repo **siempre**
están mal — no hay ningún despliegue real donde eso sea correcto, así que ahí sí tiene sentido
que la app se niegue a arrancar (C1/C2). `TrustServerCertificate=True` es distinto: es una
elección de configuración que puede ser legítima según cómo esté armado el SQL Server real de
cada quien (por ejemplo, hay entornos on-prem donde today por today se usa un certificado
autofirmado a propósito). No hay forma de verificar desde el código si el certificado del SQL
Server de destino es válido o no — solo se puede detectar el flag en la cadena de conexión. Por
eso la remediación se implementó como **advertencia en logs**, no como bloqueo de arranque.

## Cambios realizados

### 1. Advertencia al arrancar (`Program.cs`)

Justo después de `var app = builder.Build();`:

```csharp
if (!app.Environment.IsDevelopment() &&
    connectionString.Contains("TrustServerCertificate=true", StringComparison.OrdinalIgnoreCase))
{
    app.Logger.LogWarning(
        "ConnectionStrings:DefaultConnection tiene TrustServerCertificate=True fuera de " +
        "Development. Esto deshabilita la validación del certificado TLS del SQL Server y " +
        "permite un MITM en la red. Usa un certificado válido para el SQL Server y configura " +
        "Encrypt=True;TrustServerCertificate=False.");
}
```

Se decidió no incluir esta advertencia en `Development` por el mismo motivo que la cookie
`Secure` de A3: la SQL Server local/Docker suele tener certificado autofirmado y forzar la
advertencia (o peor, un fail-fast) ahí rompería el flujo normal de desarrollo sin agregar
seguridad real (un atacante que ya está dentro de tu propia máquina/red de contenedores locales
tiene problemas mayores que este flag).

### 2. Plantillas y documentación con el valor seguro por defecto

Se cambió `TrustServerCertificate=True` → `Encrypt=True;TrustServerCertificate=False` en:
- `SistemaTickets/appsettings.json` y `appsettings.json.example`.
- `README.md` (bloque de configuración de ejemplo y el comando `dotnet user-secrets set` de
  ejemplo).
- `cambios/C2-credenciales-bd-committeadas.md` (el comando de ejemplo, por consistencia).

`Encrypt=True` ya era el comportamiento por defecto de `Microsoft.Data.SqlClient` 7.x (el
paquete que usa este proyecto) desde hace varias versiones — se dejó explícito en el connection
string para que la intención sea legible a simple vista, no porque cambie el comportamiento real.

### 3. `docker-compose.yml`: excepción documentada, no removida

El servicio `sqlserver` de docker-compose es un contenedor efímero con el certificado
autofirmado que SQL Server genera solo (no hay ninguna PKI propia configurada en este repo, ver
`docker/sql/entrypoint.sh`). Poner `TrustServerCertificate=False` ahí sin más rompería la
conexión `app` → `sqlserver` (el driver rechazaría el certificado autofirmado). Se dejó
`TrustServerCertificate=True` **a propósito**, documentado con un comentario, y es seguro hoy
solo porque ese mismo servicio ya corre con `ASPNETCORE_ENVIRONMENT=Development` (la excepción
que se agregó para A3) — la advertencia de `Program.cs` no dispara ahí. Se agregó `Encrypt=True`
explícito igual, por consistencia con el resto de las cadenas de conexión.

Si en algún momento se quiere simular un despliegue real con el stack de Docker (como ya se
documentó para A3), hace falta además de volver `ASPNETCORE_ENVIRONMENT` a `Production`: darle
al contenedor `sqlserver` un certificado real (o al menos uno cuya CA esté instalada como
confiable en el contenedor `app`) y recién ahí cambiar a
`Encrypt=True;TrustServerCertificate=False`. Eso implica configurar `MSSQL_TLS_CERT` /
`MSSQL_TLS_KEY` / `MSSQL_TLS_CA_CERT` en el servicio `sqlserver` y confiar esa CA en la imagen de
`app` (similar en espíritu a lo que se hizo con Caddy para el front HTTPS en A3, pero para el
protocolo TDS de SQL Server) — no se implementó porque es una pieza de infraestructura nueva y
no fue lo que pidió este hallazgo; se puede armar como seguimiento si hace falta.

## Archivos modificados

- `SistemaTickets/Program.cs` — advertencia al arrancar.
- `SistemaTickets/appsettings.json`, `appsettings.json.example` — `Encrypt=True;
  TrustServerCertificate=False`.
- `docker-compose.yml` — `Encrypt=True` explícito + comentario documentando la excepción.
- `README.md`, `AGENTS.md`, `CLAUDE.md` — documentación del criterio y de la excepción de
  Docker.
- `cambios/C2-credenciales-bd-committeadas.md` — ejemplo actualizado por consistencia.

## Cómo se verificó

Se probó contra el stack de Docker real, no solo se revisó el código:

1. `dotnet build SistemaTickets.sln` — compila limpio, 0 errores.
2. Se reconstruyó y reinició el contenedor `app` (`ASPNETCORE_ENVIRONMENT=Development`, tal como
   está en `docker-compose.yml`) — sin advertencia en los logs, `http://localhost:8080/`
   responde `200` (la app sigue funcionando igual que antes).
3. Se corrió un contenedor de un solo uso con la misma imagen, en la misma red de Docker, con
   `ASPNETCORE_ENVIRONMENT=Production` y la misma cadena de conexión
   (`TrustServerCertificate=True`) — el log mostró la advertencia exacta esperada:
   *"ConnectionStrings:DefaultConnection tiene TrustServerCertificate=True fuera de
   Development..."*. Confirma que la advertencia dispara cuando debe y se queda callada cuando
   no debe (Development).
4. Se limpió el contenedor de prueba; el stack quedó igual que antes de la verificación.

## Limitaciones conocidas

- La advertencia solo detecta el string `TrustServerCertificate=true` en la cadena de conexión
  — no verifica si el certificado del SQL Server real es efectivamente válido (eso no se puede
  comprobar desde el código de arranque sin intentar la conexión real). Es una señal, no una
  garantía.
- El contenedor `sqlserver` de docker-compose sigue sin certificado propiamente validable — la
  mitigación real (una PKI local completa) queda como trabajo futuro opcional, descrito arriba.
