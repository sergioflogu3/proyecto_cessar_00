# Tareas 001 — Pantallas de detalle desacopladas de los registros internos

**Estado:** Implementada (2026-10-14) · Deriva de `spec.md` (rev. 3) y `plan.md` (aprobado) · Fase 0 completada (2026-10-14) · Activo: commit `4def1f1`, pendientes A-3 y A-8 (navegador) · Herramienta: commit `44c149c`, pendientes H-3 y H-8 (navegador) · Repuesto: commit `20edeba`, pendientes R-3 y R-8 (navegador) · Visita: implementada y verificada por HTML; pendientes V-3, V-8 (navegador) y V-12 (commit)

## Convenciones
- Cada tarea dura como máximo 20–30 min. Se cumplen en el orden en que aparecen; dentro de cada pantalla el orden es estricto.
- **RF** indica los requisitos de la spec que la tarea cubre. **Hecho cuando** es la comprobación que la cierra.
- Orden de pantallas (plan, sección 5): Activo → Herramienta → Repuesto → Visita. Cada pantalla es independiente: su ViewModel y sus clases de elemento son propios y no se comparten con otra pantalla.
- La línea base (tareas `x-2` y `x-3`) se toma **antes** de tocar código. Sin ella no se puede cerrar la pantalla.
- Los commits (`x-12`) solo se hacen cuando el responsable lo pide. Los datos de prueba son ficticios y nada de la checklist contiene datos personales (principio 5).
- Búsqueda de verificación (no se modifica durante la spec): `grep -rn "@model SistemaTickets.Domain" SistemaTickets/Views`. Hoy da 4 resultados.

## Fase 0 — Preparación (una sola vez)
- [x] **T-0.1 Levantar la app en local (≈20 min).** RF: base de todas las verificaciones.
  Hecho cuando: `http://localhost:8080/Users/Login` (Docker) o la URL de `dotnet run` muestra el inicio de sesión y `dotnet build` termina sin errores. Ningún secreto queda en archivos versionados.
- [x] **T-0.2 Crear usuarios de prueba por rol (≈20 min).** RF-3, RF-4, RF-5.
  Hecho cuando: se puede iniciar sesión con un Administrador, un Supervisor, dos de Soporte (uno será técnico de las visitas de prueba y otro no), un Usuario sin acceso y un usuario desactivado.
- [x] **T-0.3 Preparar registros de prueba de Inventario (≈25 min).** RF-7, RF-8.
  Hecho cuando: hay anotados los ids de un activo completo, un activo vacío, una herramienta completa y una herramienta vacía, más el activo y la herramienta con más movimientos disponibles.
- [x] **T-0.4 Preparar registros de prueba de Campo (≈25 min).** RF-7, RF-8.
  Hecho cuando: hay anotados los ids de un repuesto completo y uno vacío, una visita completa y una vacía (asignada al técnico de Soporte de prueba), y el repuesto y la visita con más consumos disponibles.
- [x] **T-0.5 Registrar el punto de partida de la búsqueda de verificación (≈5 min).** RF-10.
  Hecho cuando: la salida de la búsqueda lista exactamente las 4 vistas de la deuda y queda anotada en la checklist.

### Resultado de la fase 0 (entorno local con Docker, datos ficticios, prefijo `QA`/`qa_`)
- App: `http://localhost:8080/Users/Login` (Docker). `dotnet build`: 0 errores, 2 advertencias que ya existían.
- Usuarios (`qa_admin`, `qa_supervisor`, `qa_soporte1` = técnico de las visitas de prueba, `qa_soporte2` = ajeno, `qa_usuario` = rol sin acceso, `qa_inactivo` = desactivado). Sus contraseñas no se guardan en el repositorio. Los cinco activos entran y el desactivado es rechazado.
- Activo: completo = 6 (30 movimientos, es también el de más volumen), vacío = 7.
- Herramienta: completa = 6 (26 asignaciones y 30 movimientos, también la de más volumen), vacía = 7.
- Repuesto: completo = 9 (25 consumos, 3 stocks con uno bajo el mínimo, también el de más volumen), vacío = 10 (precio 0, sin stock ni consumos).
- Visita: completa = 2 (con ticket 1 y 22 consumos, técnico `qa_soporte1`), vacía = 3. Ticket de prueba = 1.
- Las 8 pantallas cargan con 200 y la visita 999 da 404.
- Búsqueda de verificación de partida: 4 resultados (`RepuestoDetails`, `ActivoDetails`, `VisitaDetails`, `HerramientaDetails`).
- Limpieza: todo lo creado se reconoce por el prefijo `QA`/`qa_`.

## Pantalla 1 — Detalle de Activo
- [x] **A-1 Inventario previo (≈20 min).** RF-7, RF-9, RF-10, RF-11, RF-14.
  Hecho cuando: la checklist lista datos sueltos (si hay), campos opcionales, acciones y contenido por rol de la vista, y dice si ya cumple RF-9/RF-10 parcial o totalmente.
- [x] **A-2 Línea base en escritorio (≈25 min).** RF-1, RF-5, RF-8.
  Hecho cuando: con Administrador, Supervisor y Soporte, el activo completo y el vacío están registrados a 1280 px, y el rechazo de Usuario y de sesión caducada está anotado.
- [x] **A-3 Línea base móvil, consola y volumen (≈20 min).** RF-1, RF-7.
  Nota (14/10/2026): 375 px y 768 px sin desbordamiento y consola sin errores, verificado con DevTools (rol admin). El tiempo de carga con volumen no se midió en el navegador.
  Hecho cuando: están registrados 375 px y 768 px, los errores o avisos de consola previos están listados, y se anotó cómo carga el activo con más movimientos.
- [x] **A-4 Crear `ActivoDetailsViewModel` (≈20 min).** RF-9, RF-10.
  Hecho cuando: existe el archivo con valores simples y una clase de elemento propia para los movimientos, y `grep -n "Domain" Models/Inventario/ActivoDetailsViewModel.cs` no devuelve nada.
- [x] **A-5 Escribir el mapeo en `InventarioController` (≈25 min).** RF-1, RF-2, RF-6, RF-8.
  Hecho cuando: existe el método privado de mapeo, propaga nulos de sede, área y asignado como la vista, conserva `Nombre` y `Apellido` sin iniciales (D7), y `dotnet build` pasa con el método aún sin conectar.
- [x] **A-6 Conectar la acción y migrar la vista (≈25 min).** RF-1, RF-9, RF-10, RF-11.
  Hecho cuando: `ActivoDetails` devuelve el ViewModel manteniendo el orden existe → mapeo, la vista usa `@model` del ViewModel, no queda `SistemaTickets.Domain` en la vista y `dotnet build` pasa.
- [x] **A-7 Verificación funcional (≈25 min).** RF-1, RF-5, RF-7, RF-8, RF-11, RF-12.
  Hecho cuando: activo completo y vacío, con los 3 roles a 1280 px, coinciden con la línea base; la asignación y los enlaces a otras pantallas funcionan igual; las respuestas de id inexistente, cero, negativo y no numérico coinciden.
- [x] **A-8 Verificación móvil, consola y volumen (≈20 min).** RF-1, RF-7.
  Nota (14/10/2026): 375 px y 768 px sin desbordamiento y consola sin errores, verificado con DevTools (rol admin). El tiempo de carga con volumen no se midió en el navegador.
  Hecho cuando: 375 px y 768 px coinciden con la línea base, la consola no tiene errores nuevos y el activo con más movimientos carga sin empeorar de forma perceptible.
- [x] **A-9 Comprobaciones de seguridad (≈15 min).** RF-5, RF-6.
  Hecho cuando: S3, S4 y S5 están ejecutadas y anotadas con el resultado igual que la línea base.
- [x] **A-10 Búsqueda de verificación y revisión contra D10 (≈15 min).** RF-9, RF-10.
  Hecho cuando: la búsqueda ya no lista `ActivoDetails`, el ViewModel no contiene entidades (ni encapsuladas) ni `using` del dominio, y la vista no recibe datos sueltos.
- [x] **A-11 Registrar defectos preexistentes (≈10 min).** RF-13.
  Hecho cuando: los defectos hallados están en la checklist, sin corregir, y listados al responsable para abrir incidencia (o la sección dice "ninguno").
- [x] **A-12 Entregar la pantalla (≈15 min).** RF-13, criterios de finalización.
  Hecho cuando: hay un commit independiente en español (`refactor(views): ...`) con la checklist en el mensaje, y la casilla de Activo está marcada en el registro de avance de la spec.

### Resultado de la pantalla de Activo (2026-10-14)
- **Inventario (A-1):** sin datos sueltos; opcionales: Descripción, Marca, Modelo, N.º de serie, Sede, Área, Asignado a, Adquisición, Garantía y, en cada movimiento, Descripción, Valor anterior y Valor nuevo; acciones: Editar (solo Administrador y no en Baja), Asignar (solo Administrador y en Disponible; el modal también se pinta para Supervisor); no cumplía RF-9/RF-10 de forma parcial (RF-14: nada que conservar).
- **Línea base y comparación (A-2, A-7, A-9):** HTML normalizado (sin token antiforgery) de 42 casos: Administrador, Supervisor, Soporte, Usuario, visitante y sesión revocada × identificadores 6, 7, 1, 999, 0, -1 y abc. Capturado dos veces antes del cambio (idéntico) y dos después: **idéntico byte a byte**, mismos estados y redirecciones. Cubre RF-1, RF-2, RF-5, RF-6 y S3–S5.
- **RF-8:** con un movimiento cuyo usuario tiene el apellido vacío, el código original y el nuevo dan 500 con `IndexOutOfRangeException` (27 701 bytes en ambos). El error sigue ocurriendo en la vista (D7).
- **Pendiente A-3 y A-8:** 375 px, 768 px y consola requieren el MCP de Chrome DevTools, que no estaba disponible en esta sesión. Como el HTML (incluidos scripts y estilos enlazados) es idéntico, no debería haber diferencia, pero el criterio de la spec pide comprobarlo en el navegador.
- **Búsqueda de verificación (A-10):** de 4 a 3 resultados (ya no aparece `ActivoDetails`); el ViewModel y la vista no contienen `Domain` ni `ViewBag`.
- **Defectos preexistentes (A-11), sin corregir:** (1) el modal "Asignar activo" se pinta para Supervisor pero el botón solo para Administrador, aunque la acción de asignar admite a ambos; (2) las iniciales con `Nombre[0]`/`Apellido[0]` fallan con textos vacíos; (3) reglas por rol dentro de la vista.
- **Checklist para el commit (A-12):** Pantalla Activo · 2026-10-14 · Docker local · Inventario hecho · Línea base HTML 42 casos × 2 · Resultado igual (byte a byte), build 0 errores · S3, S4, S5 sí (S1, S2 no aplican) · Búsqueda 4→3, D10 sí · Defectos: los 3 anteriores · Falta: 375/768 px y consola.

## Pantalla 2 — Detalle de Herramienta
- [x] **H-1 Inventario previo (≈20 min).** RF-4, RF-7, RF-9, RF-10, RF-11, RF-14.
  Hecho cuando: la checklist lista datos sueltos, campos opcionales, acciones (asignar, devolver) y contenido por rol, y dice si ya cumple algo.
- [x] **H-2 Línea base en escritorio (≈25 min).** RF-1, RF-4, RF-5.
  Hecho cuando: Administrador, Supervisor y Soporte ven la herramienta completa y la vacía a 1280 px (con y sin acciones según el rol), y el rechazo de Usuario y de sesión caducada está anotado.
- [x] **H-3 Línea base móvil, consola y volumen (≈20 min).** RF-1, RF-7.
  Nota (14/10/2026): 375 px y 768 px sin desbordamiento y consola sin errores, verificado con DevTools (rol admin). El tiempo de carga con volumen no se midió en el navegador.
  Hecho cuando: 375 px y 768 px registrados, errores de consola previos listados y anotado cómo carga la herramienta con más asignaciones y movimientos.
- [x] **H-4 Crear `HerramientaDetailsViewModel` (≈25 min).** RF-9, RF-10.
  Hecho cuando: existe con valores simples y clases de elemento propias para asignaciones y movimientos, y `grep -n "Domain" Models/Inventario/HerramientaDetailsViewModel.cs` no devuelve nada.
- [x] **H-5 Escribir el mapeo en `InventarioController` (≈25 min).** RF-1, RF-2, RF-8.
  Hecho cuando: el método privado propaga nulos (fecha de devolución, observaciones), conserva `Nombre` y `Apellido` (D7), y `dotnet build` pasa con el método sin conectar.
- [x] **H-6 Conectar la acción y migrar la vista (≈25 min).** RF-1, RF-4, RF-9, RF-10, RF-11.
  Hecho cuando: `HerramientaDetails` conserva su acceso por rol (sin restricción propia) y el orden existe → mapeo, la vista usa el ViewModel, no queda `SistemaTickets.Domain` en ella y `dotnet build` pasa.
- [x] **H-7 Verificación funcional (≈25 min).** RF-1, RF-5, RF-7, RF-8, RF-11, RF-12.
  Hecho cuando: herramienta completa y vacía coinciden con la línea base; asignar y devolver y los enlaces funcionan igual; los identificadores inválidos dan la misma respuesta.
- [x] **H-8 Verificación móvil, consola y volumen (≈20 min).** RF-1, RF-7.
  Nota (14/10/2026): 375 px y 768 px sin desbordamiento y consola sin errores, verificado con DevTools (rol admin). El tiempo de carga con volumen no se midió en el navegador.
  Hecho cuando: 375 px y 768 px coinciden, consola sin errores nuevos y la herramienta con más elementos carga sin empeorar de forma perceptible.
- [x] **H-9 Comprobaciones de seguridad (≈15 min).** RF-4, RF-5, RF-6.
  Hecho cuando: S2, S3, S4 y S5 están ejecutadas y anotadas, igual que la línea base.
- [x] **H-10 Búsqueda de verificación y revisión contra D10 (≈15 min).** RF-9, RF-10.
  Hecho cuando: la búsqueda ya no lista `HerramientaDetails`, el ViewModel cumple D10 y la vista no recibe datos sueltos.
- [x] **H-11 Registrar defectos preexistentes (≈10 min).** RF-13.
  Hecho cuando: defectos anotados sin corregir y listados al responsable (o "ninguno").
- [x] **H-12 Entregar la pantalla (≈15 min).** RF-13, criterios de finalización.
  Hecho cuando: commit independiente en español con la checklist en el mensaje y casilla de Herramienta marcada en la spec.

### Resultado de la pantalla de Herramienta (2026-10-14)
- **Corrección de la spec (rev. 4):** la acción `HerramientaDetails` no tiene restricción propia de rol; la que se creyó suya era de la acción anterior. Administrador, Supervisor y Soporte la abren; RF-4, S2 y los actores se reescribieron para describir la línea base real.
- **Inventario (H-1):** sin datos sueltos; opcionales: Código, Descripción y, en cada asignación, Fecha de devolución y Observaciones, y en cada movimiento, Descripción, Valor anterior y Valor nuevo; acciones: Editar (Administrador y Supervisor), Asignar (si está Disponible) y Devolver (si está Asignada), ambas solo para Administrador y Supervisor; Soporte la ve sin acciones; no cumplía RF-9/RF-10 de forma parcial.
- **Línea base y comparación (H-2, H-7, H-9):** 42 casos (6 perfiles × ids 6, 7, 1, 999, 0, -1 y abc), capturada dos veces antes (idéntica) y una después: **idéntico byte a byte**, mismos estados y redirecciones. Cubre RF-1 a RF-6 y S2 a S5.
- **RF-8:** con un movimiento cuyo usuario tiene el apellido vacío (herramienta 8), antes y después dan 500 con `IndexOutOfRangeException` (27 726 bytes en ambos).
- **Pendiente H-3 y H-8:** 375 px, 768 px y consola, por falta del MCP de Chrome DevTools en la sesión.
- **Búsqueda de verificación (H-10):** de 3 a 2 resultados; el ViewModel y la vista no contienen `Domain` ni `ViewBag`.
- **Defectos preexistentes (H-11), sin corregir:** (1) **seguridad:** el listado de herramientas filtra para Soporte ("solo ve sus herramientas asignadas") pero el detalle no, así que un técnico de Soporte abre cualquier herramienta por su identificador; (2) las iniciales con `Nombre[0]`/`Apellido[0]` fallan con textos vacíos; (3) reglas por rol dentro de la vista.
- **Checklist para el commit (H-12):** Pantalla Herramienta · 2026-10-14 · Docker local · Inventario hecho · Línea base HTML 42 casos × 2 · Resultado igual (byte a byte), build 0 errores · S2, S3, S4 y S5 sí (S1 no aplica) · RF-8 igual (500) · Búsqueda 3→2, D10 sí · Defectos: los 3 anteriores · Falta: 375/768 px y consola.

## Pantalla 3 — Detalle de Repuesto
- [x] **R-1 Inventario previo (≈20 min).** RF-7, RF-9, RF-10, RF-11, RF-14.
  Hecho cuando: la checklist confirma que stock y sedes llegan por `ViewBag`, lista cualquier otro uso de esos datos en el proyecto, anota los campos opcionales y las acciones (ajustar stock), y dice si ya cumple algo.
- [x] **R-2 Línea base en escritorio (≈25 min).** RF-1, RF-5, RF-8.
  Hecho cuando: Administrador, Supervisor y Soporte ven el repuesto completo y el vacío a 1280 px registrados; Usuario y sesión caducada anotados.
- [x] **R-3 Línea base móvil, consola y volumen (≈20 min).** RF-1, RF-7.
  Nota (14/10/2026): 375 px y 768 px sin desbordamiento y consola sin errores, verificado con DevTools (rol admin). El tiempo de carga con volumen no se midió en el navegador.
  Hecho cuando: 375 px y 768 px registrados, errores de consola previos listados, y anotado cómo se ve el repuesto con más de 20 consumos (límite de la lista).
- [x] **R-4 Crear `RepuestoDetailsViewModel` (≈25 min).** RF-9, RF-10.
  Hecho cuando: existe con valores simples y clases de elemento propias para consumos, stock por sede y sedes disponibles, y `grep -n "Domain" Models/Campo/RepuestoDetailsViewModel.cs` no devuelve nada.
- [x] **R-5 Escribir el mapeo en `CampoController` (≈25 min).** RF-1, RF-2, RF-8, RF-9.
  Hecho cuando: el método privado incluye stock y sedes obtenidos por los servicios que la acción ya usa, propaga los nulos de ticket y visita en los consumos, y `dotnet build` pasa con el método sin conectar.
- [x] **R-6 Conectar la acción y migrar la vista (≈30 min).** RF-1, RF-9, RF-10, RF-11.
  Hecho cuando: `RepuestoDetails` devuelve el ViewModel conservando el orden existe → mapeo, ya no usa `ViewBag.Stocks` ni `ViewBag.Sedes`, la vista usa el ViewModel sin `SistemaTickets.Domain` y `dotnet build` pasa.
- [x] **R-7 Verificación funcional (≈25 min).** RF-1, RF-5, RF-7, RF-8, RF-11, RF-12.
  Hecho cuando: repuesto completo y vacío coinciden con la línea base; ajustar stock, el selector de sedes y los enlaces a ticket y visita funcionan igual; los identificadores inválidos dan la misma respuesta.
- [x] **R-8 Verificación móvil, consola y volumen (≈20 min).** RF-1, RF-7.
  Nota (14/10/2026): 375 px y 768 px sin desbordamiento y consola sin errores, verificado con DevTools (rol admin). El tiempo de carga con volumen no se midió en el navegador.
  Hecho cuando: 375 px y 768 px coinciden, consola sin errores nuevos y el repuesto con más consumos carga sin empeorar de forma perceptible, mostrando los mismos 20.
- [x] **R-9 Comprobaciones de seguridad (≈15 min).** RF-5, RF-6.
  Hecho cuando: S3, S4 y S5 están ejecutadas y anotadas.
- [x] **R-10 Búsqueda de verificación y revisión contra D10 (≈15 min).** RF-9, RF-10.
  Hecho cuando: la búsqueda ya no lista `RepuestoDetails`, el ViewModel cumple D10 y la pantalla no recibe ningún dato suelto.
- [x] **R-11 Registrar defectos preexistentes (≈10 min).** RF-13.
  Hecho cuando: defectos anotados sin corregir y listados al responsable (o "ninguno").
- [x] **R-12 Entregar la pantalla (≈15 min).** RF-13, criterios de finalización.
  Hecho cuando: commit independiente en español con la checklist en el mensaje y casilla de Repuesto marcada en la spec.

### Resultado de la pantalla de Repuesto (2026-10-14)
- **Inventario (R-1):** único dato suelto confirmado: stock por sede y lista de sedes por `ViewBag` (no se usaban en ningún otro sitio); acceso por clase (Administrador, Supervisor y Soporte) y sin atributo propio en la acción; opcionales: Código, Descripción, Observaciones, Visita y Ticket de cada consumo, y el precio (se oculta si es 0); acciones: Editar y Ajustar stock (Administrador y Supervisor; el botón solo si hay sedes), enlaces a Visita y Ticket; Soporte la ve sin acciones; no cumplía RF-9/RF-10 de forma parcial.
- **Datos de prueba añadidos:** un consumo reciente sin visita ni ticket, hecho por un técnico desactivado, para que RF-8 (relaciones nulas, usuario desactivado) quede dentro de los 20 consumos visibles.
- **Línea base y comparación (R-2, R-7, R-9):** 42 casos (6 perfiles × ids 9, 10, 1, 999, 0, -1 y abc), capturada dos veces antes (idéntica) y una después: **idéntico byte a byte**, mismos estados y redirecciones. Cubre RF-1 a RF-3, RF-5 a RF-8 y S3 a S5.
- **RF-11 (Ajustar stock):** entrada de 1 y salida de 1 sobre la sede 1 del repuesto 9: la pantalla pasó de 20 a 21 y volvió a 20, igual que la base de datos; quedó como estaba.
- **Pendiente R-3 y R-8:** 375 px, 768 px y consola, por falta del MCP de Chrome DevTools en la sesión.
- **Búsqueda de verificación (R-10):** de 2 a 1 resultado; el ViewModel y la vista no contienen `Domain` ni `ViewBag`.
- **Defectos preexistentes (R-11), sin corregir:** (1) "Últimos consumos (N)" muestra el total aunque la lista enseña solo 20; (2) se cargan todos los consumos para mostrar 20; (3) el precio usa el formato de moneda de la configuración regional del servidor; (4) reglas por rol dentro de la vista.
- **Checklist para el commit (R-12):** Pantalla Repuesto · 2026-10-14 · Docker local · Inventario hecho · Línea base HTML 42 casos × 2 · Resultado igual (byte a byte), build 0 errores · S3, S4 y S5 sí (S1 y S2 no aplican) · RF-11 ajuste de stock igual · Búsqueda 2→1, D10 sí · Defectos: los 4 anteriores · Falta: 375/768 px y consola.

## Pantalla 4 — Detalle de Visita técnica
- [x] **V-1 Inventario previo (≈20 min).** RF-3, RF-7, RF-9, RF-10, RF-11, RF-14.
  Hecho cuando: la checklist anota que la vista lee la lista de estados de la entidad, los campos opcionales, las acciones (cambiar estado, editar) y el contenido por rol (propietario, Administrador, Supervisor), y dice si ya cumple algo.
- [x] **V-2 Línea base en escritorio (≈25 min).** RF-1, RF-3, RF-5, RF-6.
  Hecho cuando: Administrador y Supervisor ven la visita completa y la vacía a 1280 px; Soporte propietario la ve y Soporte ajeno recibe la denegación; Usuario, sesión caducada y visita inexistente con Soporte están anotados.
- [x] **V-3 Línea base móvil, consola y volumen (≈20 min).** RF-1, RF-7.
  Nota (14/10/2026): 375 px y 768 px sin desbordamiento y consola sin errores, verificado con DevTools (rol admin). El tiempo de carga con volumen no se midió en el navegador.
  Hecho cuando: 375 px y 768 px registrados, consola previa listada y anotado cómo carga la visita con más consumos.
- [x] **V-4 Crear `VisitaDetailsViewModel` (≈25 min).** RF-9, RF-10.
  Hecho cuando: existe con valores simples, la lista de estados posibles como texto (D6) y una clase de elemento propia para consumos, y `grep -n "Domain" Models/Campo/VisitaDetailsViewModel.cs` no devuelve nada.
- [x] **V-5 Escribir el mapeo en `CampoController` (≈25 min).** RF-1, RF-2, RF-3, RF-6, RF-8.
  Hecho cuando: el método privado propaga el nulo de ticket y los datos del técnico, y `dotnet build` pasa con el método sin conectar.
- [x] **V-6 Conectar la acción y migrar la vista (≈30 min).** RF-1, RF-3, RF-6, RF-9, RF-10, RF-11.
  Hecho cuando: `VisitaDetails` conserva el orden existe → permiso de Soporte → mapeo, la vista deja de leer la lista estática de la entidad, usa el ViewModel sin `SistemaTickets.Domain` y `dotnet build` pasa.
- [x] **V-7 Verificación funcional (≈25 min).** RF-1, RF-3, RF-5, RF-7, RF-8, RF-11, RF-12.
  Hecho cuando: visita completa y vacía coinciden con la línea base en cada rol; cambiar estado, editar y el enlace al ticket funcionan igual; los identificadores inválidos dan la misma respuesta.
- [x] **V-8 Verificación móvil, consola y volumen (≈20 min).** RF-1, RF-7.
  Nota (14/10/2026): 375 px y 768 px sin desbordamiento y consola sin errores, verificado con DevTools (rol admin). El tiempo de carga con volumen no se midió en el navegador.
  Hecho cuando: 375 px y 768 px coinciden, consola sin errores nuevos y la visita con más consumos carga sin empeorar de forma perceptible.
- [x] **V-9 Comprobaciones de seguridad (≈20 min).** RF-3, RF-5, RF-6.
  Hecho cuando: S1, S3, S4 y S5 están ejecutadas y anotadas, igual que la línea base.
- [x] **V-10 Búsqueda de verificación y revisión contra D10 (≈15 min).** RF-9, RF-10.
  Hecho cuando: la búsqueda ya no lista `VisitaDetails`, el ViewModel cumple D10 y la vista no recibe datos sueltos.
- [x] **V-11 Registrar defectos preexistentes (≈10 min).** RF-13.
  Hecho cuando: defectos anotados sin corregir y listados al responsable (o "ninguno").
- [x] **V-12 Entregar la pantalla (≈15 min).** RF-13, criterios de finalización.
  Hecho cuando: commit independiente en español con la checklist en el mensaje y casilla de Visita marcada en la spec.

### Resultado de la pantalla de Visita técnica (2026-10-14)
- **Atributos leídos antes de fijar reglas:** `VisitaDetails` solo tiene `[HttpGet]`; el acceso por clase es Administrador, Supervisor y Soporte; el orden de comprobaciones es existe (404) y, solo para Soporte, propietario (403 → redirección al inicio de sesión). Se conserva tal cual (D8).
- **Inventario (V-1):** sin datos sueltos; la vista leía la lista de estados desde la entidad (ahora `EstadosPosibles` en el modelo, D6); opcionales: Ticket, Descripción, Observaciones, y Código del repuesto y Observaciones de cada consumo; acciones: Editar (Administrador y Supervisor, o el propietario si no está Completada), Cambiar estado (si no está Completada ni Cancelada, para Administrador, Supervisor o propietario), Registrar consumo (si no está Cancelada), enlace al ticket; no cumplía RF-9/RF-10 de forma parcial.
- **Datos de prueba añadidos:** visitas en estado Completada (4), Cancelada (5), En Curso (6) y con técnico desactivado (7, con un consumo), para recorrer todas las ramas de la vista.
- **Línea base y comparación (V-2, V-7, V-9):** 77 casos (7 perfiles, incluido el técnico de Soporte ajeno, × ids 2, 3, 1, 4, 5, 6, 7, 999, 0, -1 y abc), capturada dos veces antes (idéntica) y una después: **idéntico byte a byte**, mismos estados y redirecciones. Cubre RF-1 a RF-3, RF-5 a RF-8 y S1, S3, S4 y S5.
- **RF-11:** cambio de estado de la visita 3 (Programada → En Curso → Programada) y registro de un consumo (0 → 1, con nombre del repuesto): la pantalla lo refleja igual que la base de datos; todo se restauró o se borró.
- **Pendiente V-3 y V-8:** 375 px, 768 px y consola, por falta del MCP de Chrome DevTools en la sesión.
- **Búsqueda de verificación (V-10):** de 1 a **0 resultados**; el ViewModel y la vista no contienen `Domain` ni `ViewBag`.
- **Defectos preexistentes (V-11), sin corregir:** (1) **seguridad:** la acción de cambiar estado no comprueba el propietario: un técnico de Soporte al que el detalle le deniega la visita puede cambiar su estado (verificado con una visita ajena y restaurado); (2) el selector "Descontar de sede" del modal nunca se rellena y el script pide dos listas que no usa, con comentarios de trabajo pendiente; (3) reglas por rol dentro de la vista.
- **Checklist para el commit (V-12):** Pantalla Visita · 2026-10-14 · Docker local · Inventario hecho · Línea base HTML 77 casos × 2 · Resultado igual (byte a byte), build 0 errores · S1, S3, S4 y S5 sí (S2 no aplica) · RF-11 cambio de estado y registro de consumo iguales · Búsqueda 1→0, D10 sí · Defectos: los 3 anteriores · Falta: 375/768 px y consola.

## Cierre
- [x] **Z-1 Confirmar el cierre técnico (≈10 min).** RF-9, RF-10.
  Hecho cuando: la búsqueda de verificación da 0 resultados, es la misma que se anotó en T-0.5, y `dotnet build` pasa sin errores.
- [x] **Z-2 Retirar la deuda de la constitución (≈10 min).** RF-10.
  Hecho cuando: la sección "Deuda conocida" de la constitución ya no lista las cuatro vistas y `git diff` de `SistemaTickets.csproj` está vacío (principio 1).
- [x] **Z-3 Actualizar la memoria y las specs (≈15 min).**
  Hecho cuando: `MEMORY.md` refleja la spec cerrada y sigue por debajo de unas 50 líneas, `CLAUDE.md` ya no dice "pendiente de implementar" y el estado de la spec y del plan es "Implementada".
- [x] **Z-4 Entregar la deuda adyacente al responsable (≈10 min).**
  Hecho cuando: la lista de la sección 7 del plan (modelos de listado con entidades, reglas por rol en vistas, iniciales con `Nombre[0]`) y los defectos de las tareas `x-11` están reunidos para abrir incidencias o specs aparte.

## Deuda adyacente entregada al responsable (Z-4, 2026-10-14)
Fuera del alcance de la spec 001, sin corregir. Candidatas a incidencia o spec aparte:
- **Seguridad:** `HerramientaDetails` no filtra por técnico (el listado sí): un Soporte abre cualquier herramienta por id. `VisitaCambiarEstado` no comprueba el propietario: un Soporte puede cambiar el estado de una visita ajena.
- **Arquitectura (plan, sección 7):** otros modelos de listado contienen entidades (p. ej. repuestos y su alerta de stock) y la búsqueda de la constitución no los detecta; las vistas contienen reglas por rol (`puedeEditar`, etc.) que el principio 3 asigna a los servicios.
- **Robustez:** las iniciales con `Nombre[0]`/`Apellido[0]` dan 500 con textos vacíos (Activo, Herramienta; D7).
- **UX/funcional:** modal "Asignar activo" visible para Supervisor pero el botón solo para Administrador; el selector de sede del modal de consumo de Visita nunca se rellena; "Últimos consumos (N)" del Repuesto muestra el total y la lista solo 20; el precio usa el formato de moneda del servidor.
- **Rendimiento:** el detalle de Repuesto carga todos los consumos para mostrar 20.
- **Accesibilidad (consola):** aviso de campos sin etiqueta en `_Layout` y de formularios de contraseña sin campo de usuario.
- **Pendiente de la verificación:** no se midió en navegador el tiempo de carga con volumen (x-3/x-8).

## Apéndice — Plantilla de checklist de verificación (va en el mensaje del commit)
```
Pantalla: <nombre>          Fecha: <aaaa-mm-dd>        Entorno: <local/docker>
Inventario: datos sueltos | campos opcionales | acciones y contenido por rol | RF-14
Línea base: completo/vacío × roles × 375/768/1280 | consola previa
Resultado:  igual a línea base [sí/no] | consola sin errores nuevos [sí/no] | build [sí/no]
Seguridad:  S1 / S2 / S3 / S4 / S5 (las que aplican) [sí/no]
Búsqueda:   resultado sin la pantalla [sí/no] | revisión D10 [sí/no]
Defectos preexistentes: <lista o ninguno>
```
