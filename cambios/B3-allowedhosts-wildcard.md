# B3 — `AllowedHosts: "*"`

**Severidad:** Baja · **Estado:** ✅ Resuelto

## Hallazgo original

- **Evidencia:** `appsettings.json:33`.
- **Impacto:** permite host-header injection si el despliegue está mal aislado.
- **Remediación pedida:** restringir al dominio real publicado.

## Decisión: no hay dominio real todavía

La remediación pedía restringir al "dominio real publicado", pero este proyecto no tiene un
dominio de producción — revisando el repo entero (`docker-compose.yml`, `docker/caddy/Caddyfile`,
`README.md`/`AGENTS.md`/`CLAUDE.md`) el único host documentado en cualquier entorno es
`localhost` (acceso directo por `dotnet run`, Docker en `localhost:8080`, o vía el proxy Caddy
en `https://localhost:8443` — el `Caddyfile` tiene literalmente `localhost { ... }` como su único
site block).

Se consultó esto explícitamente antes de elegir un valor (adivinar mal acá no es un problema de
estilo — un valor equivocado deja la app inaccesible con `400` para todo el mundo). Se confirmó
que no hay dominio real todavía, así que la decisión fue restringir a los hosts que **hoy
realmente sirven la app**, dejando documentado que hace falta actualizarlo el día que haya un
dominio real.

## Cambios realizados

`SistemaTickets/appsettings.json` y `appsettings.json.example`:

```diff
- "AllowedHosts": "*",
+ "AllowedHosts": "localhost;127.0.0.1",
```

Se incluyen ambos (`localhost` y `127.0.0.1`) porque la app se accede indistintamente por
cualquiera de los dos según el entorno (ver verificación abajo).

No se tocó `docker-compose.yml` ni el `Caddyfile` — Caddy reenvía el header `Host` original del
cliente al hacer `reverse_proxy` (no lo reescribe a otra cosa), así que las requests que llegan
a la app vía Caddy también traen `Host: localhost`, ya cubierto por el valor nuevo.

## Archivos modificados

- `SistemaTickets/appsettings.json`, `SistemaTickets/appsettings.json.example` —
  `AllowedHosts: "*"` → `"localhost;127.0.0.1"`.
- `AGENTS.md`, `CLAUDE.md` — nueva entrada documentando el valor y qué hacer con un dominio real.
- `README.md` — línea nueva en Seguridad.
- `cambios/README.md` — fila B3 en la tabla.

## Cómo se verificó

Se reconstruyó la imagen de `app` (`docker-compose up -d --build app`) y se probó contra el
contenedor real, con headers `Host` explícitos vía `curl`:

```bash
curl -H "Host: localhost"        http://127.0.0.1:8080/Users/Login  # → 200
curl -H "Host: 127.0.0.1"        http://127.0.0.1:8080/Users/Login  # → 200
curl -H "Host: evil.example.com" http://127.0.0.1:8080/Users/Login  # → 400
```

Los dos hosts válidos siguen sirviendo la app con normalidad; un `Host` header arbitrario/falso
ahora se rechaza con `400` (antes, con `"*"`, hubiera devuelto `200` igual — esa es exactamente
la superficie de host-header injection que describe el hallazgo).

También se probó el camino completo por Caddy: `docker-compose up -d caddy` +
`curl -k https://localhost:8443/Users/Login` → `200`, confirmando que el proxy con TLS local
sigue funcionando sin cambios (Caddy preserva el `Host: localhost` que ya está permitido).

## Limitaciones conocidas

- **Si en algún momento se despliega con un dominio propio, hay que agregarlo a
  `AllowedHosts`** (formato `"localhost;127.0.0.1;midominio.com"`) — de lo contrario, todas las
  requests a ese dominio real van a devolver `400`. Esto no está automatizado ni validado al
  arrancar (a diferencia de `Jwt:Key`/la cadena de conexión) porque, a diferencia de esos casos,
  no hay un "placeholder inválido" reconocible: `"localhost;127.0.0.1"` es un valor
  perfectamente válido y funcional hoy, no algo que haya que reemplazar necesariamente.
- Mismo caso con `.env`'s `APP_HOST=0.0.0.0` (exponer el puerto directo a la LAN, ya documentado
  en `.env.example` como algo a usar "con cuidado"): si alguien accede por la IP/hostname de la
  máquina en la red local en vez de `localhost`, esa request también va a caer en `400` hasta
  que ese host se agregue a `AllowedHosts`. No se agregó automáticamente porque depende de la
  red de cada quien (no hay un valor genérico correcto para todos los casos).
