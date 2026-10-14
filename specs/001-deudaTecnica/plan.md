# Plan 001 — Pantallas de detalle desacopladas de los registros internos

**Estado:** Implementada (2026-10-14) · Implementa `spec.md` (rev. 3)

## 1. Enfoque
Para cada una de las cuatro pantallas se crea un modelo propio (ViewModel) en `Models/`, la acción
del controlador lo rellena a partir del registro que ya obtiene del servicio, y la vista pasa a
usarlo en lugar del registro interno. No cambian servicios, repositorios, entidades, mapeos de
NHibernate, rutas ni permisos. El archivo `SistemaTickets.csproj` no se toca (principio 1).

## 2. Decisiones técnicas (y por qué)
- **D1. El mapeo vive en el controlador**, en un método privado por pantalla, como ya hace
  `BuildVisitaForm`. Es traducción para la interfaz, no regla de negocio (principio 3), y evita
  crear interfaces de servicio nuevas.
- **D2. Modelos planos**: solo texto, números, fechas, booleanos e ids. Las listas relacionadas
  (consumos, movimientos, asignaciones, stock, sedes) son clases de elemento propias del
  ViewModel; nunca una entidad ni una colección de entidades (RF-9, RF-10).
- **D3. Las fechas viajan sin convertir** y la vista sigue llamando a `ToLocalTime()` como hoy,
  para que el resultado sea idéntico en cualquier zona horaria (equivalencia).
- **D4. Orden y `Take(20)` se quedan en la vista**, aplicados sobre la lista del ViewModel. Así el
  diff de las vistas es mínimo y no se mueve lógica de presentación a otro sitio.
- **D5. Las reglas por rol de la vista (`puedeEditar`, etc.) no se tocan**: leen los claims, no
  entidades, y moverlas es otro cambio (ver sección 7).
- **D6. `VisitaTecnica.Estados`**: la vista de visita lee esa lista estática de la entidad. El
  ViewModel llevará la lista de estados posibles como texto y la vista seguirá filtrando el
  estado actual igual que hoy.
- **D7. Iniciales del movimiento**: la vista hace `Nombre[0]` y `Apellido[0]`. El ViewModel
  conserva `Nombre` y `Apellido` sin precalcular iniciales, para mantener el comportamiento
  exacto, incluida la falla con nombres vacíos (RF-8, RF-13).
- **D8. Orden de comprobaciones intacto** en cada acción: existe → permiso → mapeo → vista
  (RF-2, RF-3, RF-6).
- **D9. Carga diferida**: el mapeo recorre las relaciones dentro de la acción, con la misma
  sesión de la petición que ya usa la vista, así que se hacen las mismas consultas. Se vigilará
  que no aparezcan consultas extra perceptibles (requisito de rendimiento).
- **D10. Prohibido incluir entidades dentro de un ViewModel**: ni como propiedad, ni en colecciones,
  ni como tipo genérico, ni encapsuladas en otro objeto, ni mediante `using` del dominio para ese
  fin. Solo valores simples y clases de elemento propias (definición de "modelo propio" de la spec).
  Se comprueba en el paso 7 de cada pantalla.

## 3. Archivos afectados
| Pantalla | ViewModel nuevo | Acción | Vista |
|---|---|---|---|
| Repuesto | `Models/Campo/RepuestoDetailsViewModel.cs` | `CampoController.RepuestoDetails` | `Views/Campo/RepuestoDetails.cshtml` |
| Visita técnica | `Models/Campo/VisitaDetailsViewModel.cs` | `CampoController.VisitaDetails` | `Views/Campo/VisitaDetails.cshtml` |
| Activo | `Models/Inventario/ActivoDetailsViewModel.cs` | `InventarioController.ActivoDetails` | `Views/Inventario/ActivoDetails.cshtml` |
| Herramienta | `Models/Inventario/HerramientaDetailsViewModel.cs` | `InventarioController.HerramientaDetails` | `Views/Inventario/HerramientaDetails.cshtml` |

## 4. Datos que lleva cada modelo (según lo que la vista lee hoy)
- **Repuesto**: Id, Nombre, Codigo, Descripcion, Categoria, UnidadMedida, PrecioUnitario; consumos
  (Cantidad, FechaConsumo, Observaciones, nombre del técnico, id de ticket y de visita opcionales);
  stock por sede (Id, nombre de sede, CantidadActual, CantidadMinima, BajoMinimo); sedes
  disponibles (Id, Nombre). Hoy stock y sedes llegan por `ViewBag`, es el único dato suelto.
- **Visita**: Id, Estado, Descripcion, Direccion, Observaciones, FechaVisita, FechaCreacion; id y
  nombre completo del técnico; id de ticket opcional; estados posibles; consumos (Cantidad,
  FechaConsumo, Observaciones, nombre del técnico, código, nombre y unidad del repuesto).
- **Activo**: Id, Nombre, Descripcion, Tipo, Estado, NumeroSerie, Marca, Modelo, FechaAdquisicion,
  FechaGarantia, CreadoEn; nombres opcionales de sede, área y asignado; movimientos (TipoMovimiento,
  FechaMovimiento, Descripcion, ValorAnterior, ValorNuevo, Nombre, Apellido y nombre completo del usuario).
- **Herramienta**: Id, Nombre, Codigo, Descripcion, Tipo, Estado, CreadoEn; asignaciones (Activa,
  FechaAsignacion, FechaDevolucion, Observaciones, nombre del técnico y de quien asigna); movimientos
  como en Activo.
La lista exacta se confirma en el inventario previo de cada pantalla (spec, criterios de finalización).

## 5. Secuencia de trabajo por pantalla (un commit por pantalla, orden libre)
1. **Inventario y línea base**: anotar en la checklist datos sueltos, campos opcionales, acciones y
   contenido por rol; comprobar si ya cumple algo (RF-14); registrar la línea base con DevTools.
2. Crear el ViewModel y sus clases de elemento.
3. Rellenarlo en la acción, sin alterar el orden de comprobaciones (D8).
4. Cambiar el `@model` de la vista y sus referencias; quitar el uso de `ViewBag` y de la entidad.
5. `dotnet build` sin errores.
6. Verificar con Chrome DevTools: registro completo y vacío, cada rol con acceso, 375 / 768 / 1280 px,
   consola sin errores nuevos, y las comprobaciones S1–S5 que apliquen.
7. Búsqueda de verificación (la de la constitución) y revisión manual del ViewModel contra D10
   (sin entidades, ni encapsuladas, ni `using` del dominio).
8. Commit en español (`refactor(views): ...`) con la checklist en el mensaje; marcar la casilla en
   el registro de avance de la spec.

**Orden aprobado**: Activo → Herramienta → Repuesto → Visita (sigue siendo libre si hay un motivo).

## 6. Riesgos y mitigaciones
- **Nulos distintos**: la vista usa `?.` y `?? "—"` en sede, área, asignado y ticket; el mapeo
  debe propagar nulos igual y la vista conserva los mismos textos de reemplazo.
- **Errores preexistentes**: si la vista fallaba con relaciones incompletas, el error pasa del
  render al mapeo; la respuesta de error observable debe ser la misma (RF-8). Se comprueba con un
  caso real antes de cerrar la pantalla.
- **Regresión en los scripts de la pantalla**: la vista usa ids en enlaces y llamadas del navegador
  (`AjustarStock`, `AsignarActivo`, `VisitaCambiarEstado`...). El ViewModel debe incluir todos los
  ids que esos scripts necesitan.
- **Consultas extra**: recorrer relaciones en el controlador no debe añadir viajes a la base de datos.

## 7. Deuda adyacente detectada (fuera de alcance, para incidencia aparte)
- Otros modelos de listado también contienen entidades (p. ej. el de repuestos y su alerta de
  stock). La búsqueda de la constitución solo detecta vistas con `@model` del dominio, así que no
  los ve.
- Las vistas contienen reglas por rol (`puedeEditar`), que el principio 3 asigna a los servicios.
- Las iniciales con `Nombre[0]` fallan con textos vacíos (D7).

## 8. Al cerrar la spec
Retirar la deuda de la constitución, actualizar `MEMORY.md` y el apartado de specs de `CLAUDE.md`.

## 9. Decisiones del responsable
- D1 aprobada (mapeo en el controlador); D2–D9 sin cambios; D10 añadida.
- Orden: Activo → Herramienta → Repuesto → Visita.
