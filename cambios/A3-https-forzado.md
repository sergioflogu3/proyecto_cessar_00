# A3 — `RequireHttpsMetadata = false` y cookie `Secure` condicional

**Severidad:** Alta · **Estado:** ✅ Resuelto

## Hallazgo original

- **Evidencia:** `Program.cs:63`, `UsersController.cs:65` (`Secure = HttpContext.Request.IsHttps`).
- **Impacto:** si el sitio se accedía por HTTP en algún entorno (puerto local, proxy mal
  configurado), el JWT viajaba sin cifrar y la cookie se enviaba igual → robo de sesión por
  MITM/red local.
- **Remediación pedida:** forzar `Secure = true` siempre; exigir HTTPS en todo el despliegue;
  considerar HSTS con `preload`.

## Cambios realizados

### 1. Cookie `Secure` fija por entorno, no por la petición

`UsersController.Login` ya no decide `Secure` según si la petición actual llegó por HTTPS
(`HttpContext.Request.IsHttps` — ese era justo el bug: un proxy mal configurado podía engañar
esa condición). Ahora es `Secure = !_environment.IsDevelopment()`:

- Fuera de `Development` (Docker, producción) siempre es `true` — el navegador nunca reenvía
  el JWT por una conexión sin cifrar, sin importar qué tan mal configurado esté algún entorno
  intermedio.
- En `Development` es `false` a propósito, para poder seguir probando localmente por HTTP sin
  lidiar con certificados (ver "Cómo probar localmente por HTTP" más abajo). Esto se decide por
  `ASPNETCORE_ENVIRONMENT`, una variable fija por despliegue — nunca por un header o el esquema
  de la petición entrante, que es lo que hacía insegura a la versión original.

### 2. `RequireHttpsMetadata = true`

En `Program.cs`, `AddJwtBearer` tenía `RequireHttpsMetadata = false`. Se cambió a `true`. Esta
app no usa `Authority`/`MetadataAddress` (no hay descubrimiento OIDC — la clave de firma se
pasa directo como `IssuerSigningKey`), así que la opción no tiene efecto funcional hoy, pero se
corrigió igual: no hay motivo para dejar una bandera permisiva sin usar, y protege contra una
futura regresión si alguien agrega `Authority` sin notar que esto estaba en `false`.

### 3. HSTS con preload

Se agregó `builder.Services.AddHsts(...)` con `Preload = true`, `IncludeSubDomains = true`,
`MaxAge = 365 días`. Se aplica a través del `app.UseHsts()` que ya existía (sigue activo solo
fuera de `Development`, como recomienda Microsoft).

### 4. Consecuencia: local y Docker necesitan HTTPS de verdad

Como la cookie ya no funciona por HTTP, había que asegurarse de que el flujo normal de
desarrollo/pruebas siguiera funcionando:

- **`dotnet run` local**: `Properties/launchSettings.json` tenía el perfil `"http"` primero en
  el JSON, que es el que usa `dotnet run` sin argumentos. Se reordenó para que `"https"`
  (puerto 7114, con fallback a 5213) sea el default.
- **Docker Compose**: el contenedor `app` solo servía HTTP plano en el puerto 8080, publicado
  directo al host — con `Secure=true` eso ya no sostiene el login. Se agregó un servicio
  `caddy` (imagen `caddy:2-alpine`) que termina TLS con un certificado autofirmado local
  (`tls internal` de Caddy, ver `docker/caddy/Caddyfile`) y reenvía en texto plano *solo*
  dentro de la red interna de docker-compose hacia `app:8080`. Es el único servicio publicado
  al host ahora (`8443:443`); `app` pasó de `ports` a `expose` (ya no es alcanzable directo
  desde el host).

## Cómo probar localmente por HTTP

### `dotnet run`

Como `Secure` solo es `true` fuera de `Development`, y los tres perfiles de
`launchSettings.json` ya seteaban `ASPNETCORE_ENVIRONMENT=Development`, alcanza con correr el
perfil `http` explícitamente:

```bash
dotnet run --project SistemaTickets/SistemaTickets.csproj --launch-profile http
# http://localhost:5213 — la cookie jwt se emite sin Secure, el login funciona por HTTP
```

El perfil `https` (ahora el default de `dotnet run` sin flags) sigue siendo la forma
recomendada de probar localmente, porque reproduce el comportamiento real de Docker/producción.

### Docker Compose (actualización posterior)

Después del fix inicial (que dejaba `app` solo alcanzable a través de `caddy`), se pidió poder
seguir entrando por HTTP directo en `localhost` también en Docker, parametrizable por `.env`.
Se hicieron dos cambios en `docker-compose.yml`:

1. `app` pasó de `ASPNETCORE_ENVIRONMENT=Production` a `Development` — la misma bandera que
   hace que `Secure=false` localmente con `dotnet run`.
2. `app` volvió a publicarse directo al host (de `expose` a `ports`), con host y puerto
   parametrizables: `"${APP_HOST:-127.0.0.1}:${APP_PORT:-8080}:8080"`. `APP_HOST`/`APP_PORT` se
   agregaron a `.env.example` (default `127.0.0.1:8080` — Docker exige una IP real como bind
   host, `"localhost"` como string no es válido ahí, así que `127.0.0.1` es el equivalente).

Con esto, `docker compose up` deja **dos** caminos de entrada, los dos en `localhost`:
`http://localhost:8080` (directo, parametrizable) y `https://localhost:8443` (vía Caddy, sigue
igual). Si en algún momento se quiere simular un despliegue real, basta con volver
`ASPNETCORE_ENVIRONMENT` a `Production` en el servicio `app` — ahí el puerto HTTP directo deja
de sostener el login (la cookie vuelve a exigir `Secure=true` siempre) y Caddy queda como única
vía.

## Archivos modificados / creados

- `SistemaTickets/Controllers/UsersController.cs` — cookie `jwt` con `Secure =
  !_environment.IsDevelopment()` (inyecta `IWebHostEnvironment`).
- `SistemaTickets/Program.cs` — `RequireHttpsMetadata = true`, `AddHsts(...)`.
- `SistemaTickets/Properties/launchSettings.json` — perfil `https` primero.
- `docker-compose.yml` — nuevo servicio `caddy`; `app` con `ASPNETCORE_ENVIRONMENT=Development`
  y publicado en `${APP_HOST:-127.0.0.1}:${APP_PORT:-8080}:8080`; nuevo volumen `caddy_data`.
- `docker/caddy/Caddyfile` (nuevo).
- `.env.example`, `.env` — nuevas variables `APP_HOST` / `APP_PORT`.
- `README.md`, `AGENTS.md`, `CLAUDE.md` — documentación del requisito de HTTPS fuera de
  Development, y de los dos caminos de acceso locales/Docker (`http://localhost:8080` directo,
  `https://localhost:8443` vía Caddy).

## Cómo se verificó

Se levantó el stack completo (`docker compose up -d --build`) para probar el proxy de verdad,
no solo revisar el YAML:

1. Primer intento con `Caddyfile` usando `:443 { tls internal ... }` (sin hostname) →
   handshake TLS fallaba (`tlsv1 alert internal error`): Caddy no sabía para qué hostname emitir
   el certificado autofirmado. Se corrigió a `localhost { tls internal ... }`.
2. Tras el fix: `curl -k https://localhost:8443/` devuelve `200` y el HTML de
   `/Users/Login` (redirigido automáticamente), con headers `via: 1.1 Caddy` y
   `server: Kestrel` — confirma que Caddy realmente está haciendo de proxy hacia la app.
3. `dotnet build SistemaTickets.sln` — compila limpio, 0 errores.

Para la actualización posterior (acceso HTTP directo parametrizable):

4. `docker compose config` — valida que `${APP_HOST:-127.0.0.1}:${APP_PORT:-8080}:8080` resuelve
   bien (probar con `"localhost"` como valor directo falla: *"invalid IP address: localhost"* —
   por eso el default es `127.0.0.1`, no el string `"localhost"`).
5. `docker compose up -d app` + `curl http://localhost:8080/` → `200`, `docker compose ps`
   confirma el bind `127.0.0.1:8080->8080/tcp` (no expuesto a toda la red).
6. Se sobreescribió `APP_PORT=8090` en `.env`, se recreó el contenedor, y se confirmó que
   `localhost:8090` responde y `localhost:8080` ya no — o sea que la variable realmente se lee
   desde `.env`. Se restauró `.env` a `8080` después.

No se pudo probar un login end-to-end (usuario+contraseña reales) porque la base de datos del
stack de prueba estaba vacía (sin usuarios sembrados) — eso es independiente de este cambio.

## Limitaciones conocidas

- El certificado que genera Caddy en Docker es autofirmado y local — el navegador va a mostrar
  una advertencia la primera vez (`https://localhost:8443`). Es el comportamiento esperado para
  un entorno de desarrollo/demo local; en un despliegue real (Azure App Service, o cualquier
  otro con un dominio propio) el certificado lo provee la plataforma o un `Let's Encrypt`/CA
  real, no este `Caddyfile`.
- El "preload" de HSTS (`Preload = true`) solo agrega el header `includeSubDomains; preload`;
  no envía el dominio a la lista de precarga de los navegadores (`hstspreload.org`) — eso es
  una acción manual del dueño del dominio real en producción, fuera del alcance del código.
