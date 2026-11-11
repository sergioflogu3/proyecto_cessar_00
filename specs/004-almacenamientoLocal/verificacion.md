# Verificación 004 — Adjuntos de tickets guardados en una carpeta del servidor

Fecha: 2026-11-11. Entorno: Docker local (`docker-compose`, `ASPNETCORE_ENVIRONMENT=Development`),
cuenta de pruebas `qa_admin`, datos ficticios. El destino carpeta se activó con un override de
compose fuera del repo (`Adjuntos__Destino=Carpeta`, `Adjuntos__Carpeta=/tmp/adjuntos`,
`AzureBlobStorage__ConnectionString=""`). Herramientas: `curl`, `sqlcmd` y Chrome DevTools.

## Destino nube (RF-2)

| Comprobación | Resultado |
|---|---|
| Arranque sin `Adjuntos__*` | Arranca con nube; login 200 (T3) |
| Ticket #3 con `nube.pdf` | Guardado en Azurite como `tickets/3/nube.pdf` (nombre original, igual que antes) |
| Descarga del adjunto 3 | 200, contenido idéntico, `application/pdf`, `attachment`, `nosniff` |

## Destino carpeta

| Comprobación | RF | Resultado |
|---|---|---|
| Configuraciones inválidas (`Otro`, carpeta vacía o ausente, sin escritura, dentro de `wwwroot`) | RF-4, RF-5, RF-7, RF-8 | No arranca, con su mensaje (detalle en T3 de `tasks.md`) |
| Arranque con la cadena de Azure vacía | RF-3 | Arranca; `/Tickets/Index`, `/Nuevos`, `/Create` y `/Details/3` → 200 |
| Ticket #4 con dos `doc.pdf` de contenido distinto | RF-9, RF-10 | 2 filas con `NombreArchivo=doc.pdf` y rutas `tickets/4/<guid>.pdf` distintas; 2 archivos en disco |
| Descarga de los adjuntos 4 y 5 (`curl` y navegador) | RF-10, RF-11 | 200; cada uno con su contenido; `application/pdf`, `attachment; filename=doc.pdf`, `nosniff` |
| Adjunto 3 (subido con nube) descargado con carpeta | RF-12 | 404 |
| Archivo borrado del disco (T4) | RF-12 | 404 con cuerpo vacío; el log registra solo el id del adjunto |
| Ticket sin adjuntos | — | Se crea (302) |
| URL directa: `/tickets/...`, `/tmp/adjuntos/...`, `/adjuntos/...`, `/uploads/...`, `/../tmp/...`, `/%2e%2e/tmp/...`, con y sin sesión | RNF seguridad | 404 en todos los casos (control: `/css/site.css` → 200). Descarga sin sesión → 401 |
| `RutaArchivo` manipulada en la BD: `../appsettings.json`, `../../app/appsettings.json`, `/app/appsettings.json`, `tickets/../../secreto2.pdf`, `../secreto2.pdf`, `..\secreto2.pdf`, `tickets\..\..\secreto2.pdf`, `../adjuntos2.pdf` (carpeta hermana), `C:\Windows\win.ini`, `tickets/4/../../../secreto2.pdf` | RF-13 | 404 con 0 bytes, aunque había señuelos `.pdf` fuera de la carpeta; 9 avisos con solo el id y ninguna excepción. Ruta original restaurada → 200 |
| Subidas no válidas: `.pdf` con contenido de ejecutable, `.exe`, `.html`, PDF de 11 MB | RF-15 | Rechazadas con su mensaje de validación; no se crean tickets, filas ni archivos, y no quedan `.tmp` |
| Fallo a mitad de la escritura | RF-14 | Comprobado en T2 con un programa de prueba: se conserva el error original y no queda `.tmp` ni archivo final |

## Navegador (Chrome DevTools)

- Consola en `/Tickets/Index`, `/Tickets/Nuevos` y `/Tickets/Create`: sin errores ni avisos.
- `/Tickets/Details/4`: solo el aviso `verbose` preexistente del formulario de cambio de contraseña del layout.
- 375 px (`emulate 375x800x2,mobile,touch`): Index, Nuevos, Create y Details/4 sin
  desbordamiento horizontal (`scrollWidth` = 375). El detalle muestra "Adjuntos (2)" con sus
  enlaces de descarga. No hay cambios de interfaz en esta spec.

## Producción (MonsterASP.NET, T7)

Usuario, 2026-11-11, en `sistema-tickets.runasp.net` con `Adjuntos__Destino=Carpeta` y
`Adjuntos__Carpeta=../private/uploads`: `/Tickets/Index` y `/Tickets/Nuevos` cargan, se crea
un ticket con un PDF, se descarga con su nombre original y el archivo aparece en
`private/uploads/tickets/<id>/`.

## Salvedades

- Las pruebas detalladas se hicieron en Linux (Docker). En Windows (MonsterASP) se comprobó el
  flujo normal (T7), pero no las rutas manipuladas ni la URL directa.
- Disco lleno real (RF-14) no se reprodujo; se simuló con un stream que falla (T2).
- La app corre como `root` en el contenedor, así que la falta de permiso de escritura se probó con `/proc/adjuntos`.
- Datos de prueba que quedan en la BD local de Docker: tickets #2, #3 y #4 y sus adjuntos
  (1, 2, 3, 4 y 5); los archivos del ticket #2 ya no existen.
