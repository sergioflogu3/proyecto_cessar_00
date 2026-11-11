# Tareas 004 — Adjuntos de tickets guardados en una carpeta del servidor

Estado: completadas (2026-11-11). Una tarea cada vez; al terminar cada una: `dotnet build` limpio,
la comprobación indicada y parar.

- [x] **T1. Funciones puras de rutas y tipo por extensión.** RF-5, RF-6, RF-8, RF-11, RF-13
  Crear `RutaAdjuntos` (`ResolverCarpeta`, `ResolverDentroDeRaiz`, `ExtensionSegura`) y
  `AttachmentValidator.ContentTypePorExtension`.
- Hecho cuando: build limpio y un programa de prueba desechable en el scratchpad (no versionado)
  confirma con ejemplos: carpeta vacía → error; carpeta relativa → ruta absoluta bajo la
  aplicación; `wwwroot` o una subcarpeta → error; `tickets/1/a.pdf` → dentro; `../x`,
  `..\x`, `/etc/passwd`, `C:\x` y `tickets/../../x` → `null`; `informe.PDF` → `.pdf`;
  `a.p df` y sin extensión → `""`.
- Resultado (2026-11-11): build limpio (solo las 2 advertencias CS8602 preexistentes) y 52/52
  casos correctos en macOS. Las ramas propias de Windows (rutas sin distinguir mayúsculas) no se
  ejecutaron; se comprueban en T7 sobre MonsterASP.

- [x] **T2. `LocalFileStorageService`.** RF-9, RF-10, RF-12, RF-13, RF-14
  Subida con nombre GUID, escritura en `.tmp` y movimiento al final; descarga con
  `FileNotFoundException` si la ruta no es válida o el archivo no existe.
- Hecho cuando: build limpio y el programa de prueba de T1 sube dos archivos con el mismo nombre
  (dos rutas distintas, contenido íntegro), descarga cada uno con el tipo correcto, recibe
  `FileNotFoundException` con `../appsettings.json` y con un archivo inexistente, y no deja
  ningún `.tmp` cuando el stream de origen falla a mitad de la copia.
- Resultado (2026-11-11): build limpio (solo las 2 advertencias preexistentes) y 18/18 casos
  correctos en macOS. Además de lo pedido: archivo de 3 MB idéntico byte a byte, nombre con
  `../` guardado dentro de su ticket, sin extensión → `application/octet-stream`, y el fallo a
  mitad conserva el error original sin dejar ni `.tmp` ni archivo final.

- [x] **T3. Selección del destino y validación al arrancar en `Program.cs`.** RF-1 a RF-8
- Hecho cuando: en Docker, la configuración actual arranca con nube sin cambios (RF-2), y con
  la sobrescritura temporal no arranca ni registra un mensaje claro en cada caso: destino
  `Otro` (RF-4), carpeta vacía (RF-5), `/private/adjuntos` sin permiso (RF-7) y
  `wwwroot/adjuntos` (RF-8). Con `Carpeta` + `/tmp/adjuntos`, arranca sin cadena de Azure
  (RF-3).
- Resultado (2026-11-11), en Docker con la imagen reconstruida:
  - Sin `Adjuntos__*`: arranca con nube y el login responde 200 (RF-2).
  - `Otro` (RF-4), carpeta vacía y carpeta ausente con `carpeta` en minúsculas (RF-5),
    `wwwroot/adjuntos` y `/app/wwwroot` (RF-8): el contenedor termina sin reinicios y con
    su mensaje en el log.
  - `Carpeta` + `/tmp/adjuntos` + cadena de Azure vacía: arranca, el login responde 200, la
    carpeta se crea y el archivo de prueba de escritura no queda (RF-3, RF-6).
  - La app corre como `root` en el contenedor, así que RF-7 se probó con `/proc/adjuntos`.
    El mensaje incluye la ruta, pero la causa del sistema operativo ("Could not find file")
    no es muy descriptiva.

- [x] **T4. 404 en la descarga cuando falta el contenido.** RF-12
  `catch (FileNotFoundException)` en `TicketsController.DescargarAdjunto`, con `LogWarning` del
  id del adjunto.
- Hecho cuando: build limpio y, en Docker con destino carpeta, un adjunto cuyo archivo se borró
  devuelve 404, sin rutas en la respuesta, y el log muestra solo el id del adjunto.
- Resultado (2026-11-11), en Docker con destino carpeta (`/tmp/adjuntos`) y `qa_admin`, con `curl`:
  - `/Tickets/Index` se renderiza sin cadena de Azure.
  - El ticket #2 se creó con dos `informe.pdf` de contenido distinto: dos archivos con GUID y
    cada descarga devuelve su contenido (RF-10). Cabeceras: `application/pdf`,
    `attachment; filename=informe.pdf` y `nosniff` (RF-11).
  - Tras borrar el archivo del adjunto 1, su descarga da 404 con cuerpo vacío. El log
    registra solo `Contenido del adjunto 1 no encontrado en el almacenamiento.` y no hay
    excepciones. El adjunto 2 sigue descargándose.
  - Quedan en la BD local de Docker el ticket #2 y sus 2 adjuntos de prueba.

- [x] **T5. Verificación completa en Docker.** RF-2, RF-3, RF-9 a RF-15
- Hecho cuando: toda la checklist de los criterios de finalización de la spec pasa en Docker
  (destino carpeta y destino nube), comprobada con Chrome DevTools: cabeceras
  `Content-Disposition: attachment` y `X-Content-Type-Options: nosniff`, consola sin errores
  y pantallas de tickets en móvil (375 px) iguales que antes. Los resultados se anotan en
  `specs/004-almacenamientoLocal/verificacion.md`.
- Resultado final (2026-11-11, retomada después de T6): **completa**. Todos los criterios
  pasan con ambos destinos; el detalle está en `verificacion.md`.
- Estado intermedio (2026-11-11): interrumpida por el usuario para pasar a T6.
  - Hecho, destino nube (RF-2): el ticket #3 con `nube.pdf` se guarda en Azurite como
    `tickets/3/nube.pdf` (nombre original, igual que antes) y la descarga es idéntica, con
    `application/pdf`, `attachment` y `nosniff`.
  - Pendiente, destino carpeta: pantallas (Index, Nuevos, Create, Details), dos adjuntos
    con el mismo nombre en un contenedor nuevo, URL directa a la carpeta, adjunto de la nube
    descargado con carpeta (RF-12), rutas manipuladas en la BD (RF-13), validaciones de subida
    (RF-15), Chrome DevTools (consola y 375 px) y `verificacion.md`. Parte de esto ya se
    comprobó con `curl` en T4.

- [x] **T6. Documentación.** Criterio de finalización
  `appsettings.json.example`, `README.md` y `CLAUDE.md`: claves `Adjuntos:*`, valores por
  defecto, validaciones al arrancar y pasos para MonsterASP. Registrar como deuda el fallo
  preexistente con nombres repetidos en el destino nube.
- Hecho cuando: la documentación describe ambos destinos y una búsqueda de `Adjuntos__` en
  ella devuelve las dos variables con sus valores de MonsterASP.
- Resultado (2026-11-11):
  - `appsettings.json.example`: nueva sección `Adjuntos` (`Destino: "Nube"`, `Carpeta: ""`);
    el JSON es válido.
  - `README.md`: nueva sección «Adjuntos: nube o carpeta» (claves, valores por defecto,
    validaciones, efecto de cambiar de destino, backups y pasos de MonsterASP), y ajustes en
    Características, Requisitos y Seguridad.
  - `CLAUDE.md`: entrada «Adjuntos en carpeta (spec 004)», `LocalFileStorageService` en la
    lista de servicios y la spec 004 en la lista de specs.
  - La búsqueda devuelve `Adjuntos__Destino = Carpeta` y `Adjuntos__Carpeta =
    ../private/uploads` en el README. La deuda de nombres repetidos con la nube ya figuraba
    abajo.
  - No se tocó `SistemaTickets/README.md` (guía de defensa), que sigue describiendo solo Azure.

- [x] **T7. Despliegue y comprobación en MonsterASP (lo ejecuta el usuario, con guía).** RF-1 a RF-15
- Hecho cuando: con `Adjuntos__Destino=Carpeta` y `Adjuntos__Carpeta=../private/uploads`,
  `/Tickets/Index` y `/Tickets/Nuevos` cargan, se crea un ticket con adjunto, se descarga y el
  archivo aparece en `private/uploads/tickets/<id>/` por FTP.
- Resultado (2026-11-11): el usuario confirma que se cumplen todos los puntos en
  `sistema-tickets.runasp.net` (Windows/IIS, `win-x86`, `Production`). La carpeta `private`
  admite escritura sin cambios de permisos, y el resultado de las rutas en Windows queda
  cubierto por el uso real. Las comprobaciones de seguridad (rutas manipuladas y URL directa)
  solo se hicieron en Docker; no se repitieron en producción.

## Deuda detectada (fuera de alcance)
- Destino nube: subir dos archivos con el mismo nombre en un mismo ticket falla en el segundo
  (el blob ya existe) después de crear el ticket. Preexistente; no lo corrige esta spec.
- Data Protection: las claves no se guardan de forma persistente fuera de Development
  (necesario en MonsterASP). Requiere su propia spec o ampliar esta.
