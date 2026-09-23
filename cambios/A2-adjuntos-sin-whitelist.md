# A2 — Adjuntos sin lista blanca de tipos/extensión

**Severidad:** Alta · **Estado:** ✅ Resuelto

## Hallazgo original

- **Evidencia:** `Controllers/TicketsController.cs:122-131` solo validaba tamaño (10 MB); el
  `ContentType` y el nombre venían del cliente (`file.ContentType ?? "application/octet-stream"`),
  sin whitelist ni escaneo.
- **Impacto:** un usuario podía subir `payload.html`/`payload.svg` (XSS almacenado si el blob
  se sirve inline), o archivos ejecutables/ofimáticos con macros.
- **Remediación pedida:** lista blanca de extensiones/MIME (pdf, imágenes, docx, xlsx...),
  validar magic bytes, y servir adjuntos con `Content-Disposition: attachment` +
  `X-Content-Type-Options: nosniff`.

## Cambios realizados

### 1. Lista blanca de extensiones/MIME

Nuevo helper estático `Infrastructure/Security/AttachmentValidator`, con un mapa cerrado de
extensiones permitidas → tipo MIME canónico:

`.pdf`, `.jpg`/`.jpeg`, `.png`, `.gif`, `.webp`, `.doc`, `.docx`, `.xls`, `.xlsx`, `.txt`, `.csv`.

Cualquier otra extensión (incluidas `.html`, `.svg`, `.exe`, `.js`, `.docm`/`.xlsm` con macros)
se rechaza por defecto (deny-by-default, no hace falta una lista negra).

### 2. Validación de magic bytes

Cada archivo se valida contra la firma real de su tipo declarado, no solo contra el nombre:

| Extensión | Firma verificada |
|---|---|
| `.pdf` | `%PDF` (`25 50 44 46`) |
| `.jpg`/`.jpeg` | `FF D8 FF` |
| `.png` | firma PNG completa (8 bytes) |
| `.gif` | `GIF8` |
| `.webp` | `RIFF....WEBP` |
| `.doc`, `.xls` | firma OLE Compound File (`D0 CF 11 E0 A1 B1 1A E1`) |
| `.docx`, `.xlsx` | firma ZIP (`50 4B 03 04`, Office Open XML es un contenedor ZIP) |
| `.txt`, `.csv` | heurística anti-binario (sin bytes nulos, sin cabecera `MZ` de ejecutable) |

Un `payload.html` renombrado a `.pdf` se rechaza porque su contenido no empieza con `%PDF`.

### 3. El `Content-Type` del cliente deja de usarse

`TicketsController.Create` ya no confía en `file.ContentType` (el dato que el hallazgo
señalaba como controlado por el atacante). Lo que se guarda en `TicketAdjunto.TipoContenido` y
lo que se sube a Azure Blob Storage es siempre el MIME **canónico** que devuelve
`AttachmentValidator`, calculado a partir de la extensión ya verificada contra el contenido
real.

### 4. Servido seguro de adjuntos

`TicketsController.DescargarAdjunto`:
- Ya forzaba descarga (`Content-Disposition: attachment`) gracias al overload
  `File(stream, contentType, fileDownloadName)` de ASP.NET Core MVC, que agrega ese header
  automáticamente al recibir un nombre de archivo — se dejó explícito con un comentario.
- Se agregó explícitamente el header `X-Content-Type-Options: nosniff`, que el framework no
  agrega solo, para que el navegador nunca intente "adivinar" el tipo de contenido.

### Detalle de implementación (orden de validación)

Se reestructuró el loop de adjuntos en `TicketsController.Create` para validar **tamaño +
extensión + magic bytes antes de crear el ticket** (evita dejar un ticket huérfano si un
adjunto se rechaza), reutilizando los streams ya abiertos para la subida real en vez de
reabrirlos.

## Archivos modificados / creados

- `SistemaTickets/Infrastructure/Security/AttachmentValidator.cs` (nuevo).
- `SistemaTickets/Controllers/TicketsController.cs` — validación en `Create`, headers de
  seguridad en `DescargarAdjunto`.
- `README.md`, `AGENTS.md`, `CLAUDE.md` — documentación del mecanismo.

## Cómo verificar

1. Subir un `.pdf`/`.png`/`.docx` real como adjunto de un ticket → se acepta normalmente.
2. Renombrar un archivo `.html` o `.exe` a `.pdf` y subirlo → se rechaza con "no es un tipo de
   archivo permitido, o su contenido no coincide con la extensión".
3. Subir un `.svg`, `.js` o cualquier extensión fuera de la lista → se rechaza directamente por
   extensión.
4. Descargar un adjunto ya subido → la respuesta trae `Content-Disposition: attachment` y
   `X-Content-Type-Options: nosniff`; el navegador siempre ofrece guardar el archivo, nunca lo
   renderiza inline.

## Limitaciones conocidas

- Los adjuntos que ya estuvieran subidos con tipos no permitidos **antes** de este fix no se
  re-validan retroactivamente — esto solo protege subidas nuevas. Si hace falta, revisar/migrar
  los blobs existentes sería un paso aparte (implica tocar datos ya almacenados).
- `.docx` y `.xlsx` comparten la misma firma ZIP (ambos son contenedores Office Open XML); no
  se abre el ZIP para distinguir cuál es cuál internamente — se consideró suficiente para el
  objetivo del hallazgo (bloquear ejecutables/HTML disfrazados), no para detectar con precisión
  absoluta el subtipo de Office.
