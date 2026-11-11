# Plan 004 — Adjuntos de tickets guardados en una carpeta del servidor

Estado: aprobado e implementado (2026-11-11)

## Archivos y responsabilidades

| Archivo | Cambio | RF |
|---|---|---|
| `Infrastructure/Security/RutaAdjuntos.cs` (nuevo) | Funciones puras de rutas: resolver la carpeta de adjuntos, comprobar que no está en la carpeta pública, resolver una ruta registrada sin salir de la carpeta y obtener una extensión segura | RF-5, RF-6, RF-8, RF-13 |
| `Infrastructure/Security/AttachmentValidator.cs` | Nuevo método público `ContentTypePorExtension(ext)` que reutiliza la lista blanca existente | RF-11 |
| `Infrastructure/Services/LocalFileStorageService.cs` (nuevo) | Implementa `IStorageService` sobre la carpeta de adjuntos | RF-9, RF-10, RF-12, RF-13, RF-14 |
| `Program.cs` | Leer `Adjuntos:Destino`; con `Nube`, lo de hoy (validación M6 + `AzureBlobStorageService`); con `Carpeta`, validar la carpeta y registrar `LocalFileStorageService` | RF-1 a RF-8 |
| `Controllers/TicketsController.cs` | `DescargarAdjunto`: si el almacenamiento lanza `FileNotFoundException`, registrar el id del adjunto y devolver 404 | RF-12 |
| `appsettings.json.example`, `README.md`, `CLAUDE.md` | Documentar `Adjuntos:*` y los pasos de MonsterASP | Criterio de finalización |

Sin cambios en `IStorageService`, `AzureBlobStorageService`, vistas, ViewModels, esquema de BD
ni `SistemaTickets.csproj`. La subida (`TicketsController.Create`) no cambia: las
validaciones de RF-15 siguen antes de llamar al almacenamiento, sea cual sea el destino.

## Configuración

```
Adjuntos:Destino   = "Nube" | "Carpeta"      (vacío o ausente → Nube; sin distinguir mayúsculas)
Adjuntos:Carpeta   = ruta absoluta o relativa a la carpeta de la aplicación (ContentRootPath)
```
Variables de entorno: `Adjuntos__Destino`, `Adjuntos__Carpeta`. En MonsterASP:
`Adjuntos__Destino=Carpeta` y `Adjuntos__Carpeta=../private/uploads`.

## Funciones puras (`RutaAdjuntos`)
No dependen de la fecha; no aplica el parámetro "hoy".

- `ResolverCarpeta(string? carpeta, string contentRoot, string? webRoot) → string`
  - Vacía o en blanco → `InvalidOperationException` (RF-5).
  - Absoluta: se usa tal cual; relativa: `Path.Combine(contentRoot, carpeta)`. Después, `Path.GetFullPath`.
  - Si `webRoot` no es nulo y la ruta es igual a `GetFullPath(webRoot)` o empieza por ella más el separador (sin distinguir mayúsculas) → `InvalidOperationException` (RF-8).
- `ResolverDentroDeRaiz(string raiz, string? rutaRegistrada) → string?`
  - Nula o en blanco, con `\0`, con `:` o absoluta (`Path.IsPathRooted`, o empieza por `/` o `\`) → `null`.
  - Cambia `/` y `\` por `Path.DirectorySeparatorChar`, combina con `raiz` y aplica `GetFullPath`.
  - Si el resultado no empieza por `raiz` + separador (sin distinguir mayúsculas) → `null` (RF-13).
- `ExtensionSegura(string? nombreArchivo) → string`
  - Toma el último segmento del nombre tras cambiar `\` por `/`, y su extensión.
  - Si cumple `^\.[A-Za-z0-9]{1,10}$`, la devuelve en minúsculas; si no, `""`.

Las comparaciones de rutas no distinguen mayúsculas en Windows y sí en Linux. Ignorarlas
en Linux dejaría pasar `../ADJUNTOS/x` como si estuviera dentro de `/tmp/adjuntos`.
(Ajuste tras T1; donde arriba dice "sin distinguir mayúsculas", aplica esta regla.)

## Algoritmo

**Arranque (`Program.cs`)**
```
destino = config["Adjuntos:Destino"]
si vacío o "Nube"   → ValidateAzureBlobStorageConnectionString (como hoy) + AddSingleton<IStorageService, AzureBlobStorageService>
si "Carpeta"        → raiz = RutaAdjuntos.ResolverCarpeta(config["Adjuntos:Carpeta"], env.ContentRootPath, env.WebRootPath)
                      ComprobarEscritura(raiz)
                      AddSingleton<IStorageService>(new LocalFileStorageService(raiz))
otro valor          → InvalidOperationException("Adjuntos:Destino tiene el valor '<x>'. Valores admitidos: Nube, Carpeta.")   (RF-4)

ComprobarEscritura(raiz):
  Directory.CreateDirectory(raiz)
  escribir y borrar ".prueba-escritura-<guid>"
  si falla (IOException o UnauthorizedAccessException) → InvalidOperationException con la ruta y el motivo   (RF-7)
```
Se mantiene el patrón actual de `Validate*`: excepción al arrancar, que el host registra.

**Subida (`LocalFileStorageService.UploadAsync`)**
```
dir    = raiz/tickets/<ticketId>; Directory.CreateDirectory(dir)
nombre = Guid.NewGuid():N + RutaAdjuntos.ExtensionSegura(fileName)     // nunca el nombre original (RF-10, RF-13)
tmp    = dir/nombre + ".tmp"
escribir el stream en tmp (FileMode.CreateNew)
mover tmp a dir/nombre
si algo falla → borrar tmp si existe y relanzar la excepción          (RF-14: misma respuesta que hoy ante un fallo)
devolver "tickets/<ticketId>/<nombre>"                                 // separador "/", cabe en NVARCHAR(500)
```

**Descarga (`LocalFileStorageService.DownloadAsync`)**
```
ruta = RutaAdjuntos.ResolverDentroDeRaiz(raiz, rutaRegistrada)
si ruta es null o el archivo no existe → FileNotFoundException (mensaje sin rutas)   (RF-12, RF-13)
stream = FileStream(ruta, lectura, compartido para lectura, asíncrono)
tipo   = AttachmentValidator.ContentTypePorExtension(extensión) ?? "application/octet-stream"
devolver (stream, tipo)
```

**`TicketsController.DescargarAdjunto`**: rodea la llamada a `DownloadAsync` con
`catch (FileNotFoundException)`, registra `"Contenido del adjunto {AdjuntoId} no encontrado"` con
`LogWarning` y devuelve `NotFound()`. El resto de la acción (permisos, `nosniff`, nombre
original) no cambia (RF-11).

## Interfaz
No cambia ninguna pantalla. El usuario adjunta y descarga igual que hoy.

## Decisiones

| # | Decisión | Alternativa descartada y motivo |
|---|---|---|
| D1 | Nombre interno con GUID + extensión segura | Nombre original con sufijo: arrastra acentos, nombres reservados de Windows y riesgo de salirse de la carpeta; el GUID resuelve RF-10 y RF-13 a la vez. El nombre original ya se guarda en `NombreArchivo`. |
| D2 | Tipo de contenido derivado de la extensión con la lista blanca existente | Guardar un archivo de metadatos al lado, o leer `TipoContenido` de la BD: más piezas o cambiar la interfaz. La extensión ya se validó al subir, así que el tipo resultante es el mismo. |
| D3 | `FileNotFoundException` capturada en el controlador | Cambiar `IStorageService` para devolver un valor nulo: obligaría a tocar `AzureBlobStorageService`, cuyo comportamiento debe seguir igual (RF-2). Azure nunca lanza esa excepción. |
| D4 | Escribir en `.tmp` y mover al final | Escribir directamente el archivo final: un fallo a medias dejaría un archivo incompleto (RF-14). |
| D5 | Validar la carpeta al arrancar, en `Program.cs` | Validar en el primer uso: es lo que hoy falla con Azure (HU-3). Se sigue el patrón `Validate*` existente. |
| D6 | Configuración `Adjuntos:Destino` / `Adjuntos:Carpeta` | `Storage:Provider`: el principio 6 pide nombres en español. Se mantiene `AzureBlobStorage:*` sin cambios para no romper Docker. |

## Estrategia de verificación
La constitución (principio 4) no permite frameworks de tests, así que no hay `dotnet test`. Se verifica así:

1. `dotnet build` limpio tras cada tarea.
2. **Docker con destino nube** (configuración actual sin cambios): subir y descargar un adjunto (RF-2).
3. **Docker con destino carpeta**, mediante un `docker-compose` de sobrescritura temporal en el scratchpad (no versionado) con `Adjuntos__Destino=Carpeta` y `Adjuntos__Carpeta=/tmp/adjuntos`. La imagen de .NET 8 ejecuta la app con un usuario sin privilegios, por lo que `/tmp` es escribible y `/private` no, lo que sirve para probar RF-7.
4. Checklist de los criterios de finalización de la spec, con Chrome DevTools para descargas, cabeceras, consola y vista móvil. La ruta manipulada de RF-13 se prueba cambiando `RutaArchivo` de un adjunto de prueba en la BD local de Docker, nunca en producción.
5. Despliegue en MonsterASP y repetición de la checklist con destino carpeta.

Riesgo a vigilar: si `private` no es escribible en MonsterASP Free, la app no arrancará y el log
lo dirá (RF-7). En ese caso se consulta al soporte antes de buscar otra carpeta.
