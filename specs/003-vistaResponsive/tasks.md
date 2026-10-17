# Tareas 003 — Vista y responsive de todo el proyecto

Estado: implementada (T1-T13 hechas el 17/10/2026, con las salvedades de `verificacion.md`).

Excepción explícita: la skill sdd pide proponer dividir la spec si salen más de 10 tareas. El usuario decidió mantener UNA sola spec con 13 tareas, porque cubre 32 pantallas, 14 tablas y 19 modales con un sistema visual común.

## Cierre común (CC) de cada tarea
Toda tarea termina, en este orden, con:
1. `dotnet build SistemaTickets.sln` sin errores.
2. `docker-compose up -d --build app` (las vistas y `wwwroot` van en la imagen) y abrir `http://localhost:8080`.
3. Script de medición de Chrome DevTools del plan (desborde, tablas, objetivos, etiquetas, foco, contraste) a 375, 600, 768 y 1280 px sobre las pantallas de la tarea, con los roles que las ven, abriendo cada modal de la tarea con su disparador real y comprobando que la consola no tiene errores nuevos.
4. Anotar resultados en `specs/003-vistaResponsive/verificacion.md` y PARAR (una tarea cada vez; no empezar la siguiente sin orden).

Cuentas de control (se comprueban en la T13): 32 pantallas, 14 tablas, 19 modales.
- Pantallas: T4 2 + T5 4 + T6 2 + T7 5 + T8 6 + T9 2 + T10 1 + T12 10 = 32.
- Tablas: T4 2 + T5 1 + T6 2 + T8 2 + T9 1 + T10 6 = 14.
- Modales: T3 1 + T4 1 + T5 1 + T6 3 + T7 3 + T8 2 + T9 2 + T11 6 = 19.
- Paginaciones (6): T4 tickets; T6 activos y herramientas; T8 visitas y repuestos; T9 usuarios.

## Base común

- [x] **T1. CSS base I: tokens, foco, contraste y aspecto uniforme.** Antes de tocar nada, registrar la línea base (avisos de campo sin etiqueta por pantalla y rol, y capturas a 1280 px de 6 módulos). Después, en `site.css`: tokens (texto, espaciado, foco, `--tap-min`), `html` en porcentaje, regla global `:focus-visible` y retirada de las reglas que quitan el contorno, grises/menú/login/badges/avisos con contraste AA, cabecera de página, tarjetas, jerarquía de botones, badges, estado vacío. RF-13, RF-14, RF-15, RF-16, RF-17, RF-18, RF-19, RF-21, RF-22, RF-23, RF-27
- Hecho cuando: existe `verificacion.md` con la línea base; la búsqueda `outline:\s*(none|0)|box-shadow:\s*none` en `site.css` da 0 sin sustituto; los contrastes de las 14 combinaciones del plan se miden y quedan en 4,5:1 (texto) o 3:1 (iconos, foco); el matiz del degradado y los colores de marca no cambian; la pantalla testigo Tickets/Index (rol Administrador) no tiene regresión a 1280 px; CC en Login y Tickets/Index.

- [x] **T2. CSS base II: responsive común.** En `site.css`: tabla apilada (`.tabla-apilable`, cabecera visualmente oculta, `.celda-acciones`, `.fila-vacia`), plegado `.td-secundario`/`.btn-plegar`, `.acciones-form`, orden de botones por CSS, cabecera de página en móvil, paginación móvil, modales en móvil (alto máximo, cuerpo con scroll, pie visible), objetivos de 44 px y 8 px de separación en ≤991 px (con el `<label>` como objetivo de casillas e interruptores), texto largo, `.chart-box`. RF-2, RF-3, RF-4, RF-5, RF-6, RF-7, RF-8, RF-9, RF-24, RF-25, RF-28
- Hecho cuando: todas las reglas nuevas de tamaño están dentro de `max-width` (≥992 px sin cambios de tamaño, comprobado a 1280 px en la pantalla testigo); sin tablas aún marcadas no hay regresión; una tabla de prueba temporal (marcada a mano y retirada al terminar) se apila a 375 px, conserva formato de tabla a 600/768 px y su cabecera sigue en el árbol de accesibilidad; CC en Login y Tickets/Index.

- [x] **T3. Layout, parciales comunes y `site.js`.** `_Layout` (botón del menú en español con `aria-expanded`/`aria-controls`; Escape, cierre al elegir opción, cierre al rotar y retorno del foco; menú cerrado no tabulable; modal de cambio de contraseña con `for`/`id`, `aria-label="Cerrar"` y modal con scroll), `_LoginLayout` (carga de `site.js`), parciales `_EstadoVacio`, `_Paginacion` (ventana con elipsis) y `_BadgeEstado`, y `site.js` (plegado delegado con `aria-expanded`, `id`/`aria-describedby`/`aria-invalid` de errores, validación mínima `data-validar` con mensajes `data-msg-*`). Cubre 1 modal (cambio de contraseña). RF-5, RF-9, RF-11, RF-12, RF-15, RF-16, RF-20, RF-24, RF-26, RF-28
- Hecho cuando: el menú cumple RF-11 a 375 y 768 px (cierra por fuera, por opción y con Escape, y el foco vuelve al botón); `VentanaPaginas` da como máximo 7 elementos para total 1, 7 y 300 (comprobado a mano con esos casos); `TextoSobre` da ≥ 4,5:1 con colores claros y oscuros de prueba; el modal de contraseña se abre sin desbordar y sin avisos de etiqueta en un rol por cada uno de los 4 roles (modal 1 de 19); CC en Login y Tickets/Index con los 4 roles.

## Tickets y Seguimiento (6 pantallas, 3 tablas, 2 modales, 1 paginación)

- [x] **T4. Tickets: listado y nuevos.** Pantallas Tickets/Index (listado) y Tickets/Nuevos: cabecera `h1`, filtros (rejilla, botón limpiar con nombre accesible), tablas apiladas (2 de 14) con plegado si superan 8 campos (contar columnas reales), estado vacío, badges, paginación del listado (1 de 6), modal «tomar ticket» de Nuevos (1 de 19). RF-1 a RF-9, RF-13 a RF-16, RF-20, RF-24, RF-25, RF-28
- Hecho cuando: las 2 pantallas con 0, 1 y ≥200 tickets no desbordan a 375/600/768/1280; las 2 tablas se apilan a 375 px y son tabla a 600/768/1280; ninguna tarjeta muestra más de 8 campos sin plegar; la paginación a 375 px no desborda con ≥200 registros; 0 etiquetas sin nombre y 0 objetivos < 44 px a 375/768 px; el modal «tomar ticket» funciona igual con los roles que lo ven; CC en Tickets/Index y Tickets/Nuevos.

- [x] **T5. Seguimiento y formularios y detalle de Tickets.** Pantallas Supervisor/Seguimiento (tabla matriz, 1 de 14; contar columnas por estados configurados y plegar si hay más de 8), Tickets/Create (lista de adjuntos generada por script con nombre accesible), Tickets/Edit y Tickets/Details (panel lateral bajo el contenido, tarjetas, modal de confirmar cierre, 1 de 19). RF-1, RF-2, RF-3, RF-5, RF-6, RF-7, RF-8, RF-9, RF-10, RF-13, RF-20, RF-24, RF-25, RF-26
- Hecho cuando: las 4 pantallas no desbordan a los 4 anchos; la tabla de Seguimiento cumple RF-3/RF-4/RF-5; con un envío inválido en Create y Edit el error aparece junto al campo, en español y asociado (`aria-describedby`); los adjuntos añadidos por script tienen botón de quitar con nombre accesible; el panel lateral de Details queda bajo el contenido a 375 y 768 px; el modal de cierre funciona igual; 0 etiquetas sin nombre; CC en las 4 pantallas (Supervisor para Seguimiento; Usuario/Soporte/Administrador para el resto).

## Inventario (7 pantallas, 2 tablas, 6 modales, 2 paginaciones)

- [x] **T6. Inventario: listados de activos y herramientas.** Pantallas Inventario/Activos y Inventario/Herramientas: cabecera, filtros, tablas apiladas (2 de 14; la fila de herramientas con 4 acciones en solo icono parte en dos líneas con 44 px y 8 px), estado vacío, badges (incluido `bg-warning` con `text-dark`), 2 paginaciones (2 de 6 → 3 de 6 acumuladas con T4), 3 modales: baja de activo, asignar herramienta y devolver herramienta (3 de 19). RF-1 a RF-9, RF-13 a RF-16, RF-20, RF-23, RF-24, RF-25, RF-28
- Hecho cuando: las 2 pantallas no desbordan con 0, 1 y ≥200 registros; las 2 tablas se apilan a 375 px y son tabla a 600/768/1280 con scroll solo interno si no caben; las 4 acciones de herramientas caben en 375 px sin solapar; el icono de «Devolver» llega a 3:1; los 3 modales caben en 375 x 400 con el pie alcanzable; 0 etiquetas sin nombre (incluidos los selects cargados por script); CC en ambas pantallas con Administrador, Supervisor y Soporte.

- [x] **T7. Inventario: formularios y detalles.** Pantallas Inventario/ActivoCreate, ActivoEdit, ActivoDetails (modal asignar, 1), HerramientaCreate y HerramientaDetails (modales asignar y devolver, 2): rejilla de formularios, botones de acción, tarjetas de datos, estados vacíos de las secciones, errores de validación. Cubre 3 modales (6 de 19 acumulados con T6). RF-1, RF-2, RF-6, RF-7, RF-9, RF-10, RF-13, RF-15, RF-16, RF-20, RF-24, RF-26
- Hecho cuando: las 5 pantallas no desbordan a los 4 anchos, con errores de validación visibles; cada formulario tiene 1 columna a 375 px y ≤2 columnas a 768 px; los botones Guardar/Cancelar quedan a ancho completo con la acción principal primero a 375 px; los 3 modales funcionan igual; las casillas e interruptores tienen etiqueta de 44 px; 0 etiquetas sin nombre; CC en las 5 pantallas.

## Campo (6 pantallas, 2 tablas, 2 modales, 2 paginaciones)

- [x] **T8. Campo: visitas y repuestos (listados, formularios y detalles).** Pantallas Campo/Visitas y Campo/Repuestos (2 tablas, 2 de las 14 acumuladas; paginación Razor de Visitas y de Repuestos, 2 de 6), Campo/VisitaCreate, Campo/RepuestoCreate, Campo/VisitaDetails (modal de consumo de repuestos con select por script) y Campo/RepuestoDetails (modal de ajuste de stock): 2 modales. RF-1 a RF-10, RF-13 a RF-16, RF-20, RF-24, RF-25, RF-26, RF-28
- Hecho cuando: las 6 pantallas no desbordan a los 4 anchos con 0, 1 y ≥200 registros; las 2 tablas cumplen RF-3/RF-4/RF-5; la paginación a 375 px no desborda; los 2 modales funcionan igual y caben en 375 x 400; los selects cargados por script tienen etiqueta; 0 etiquetas sin nombre; CC en las 6 pantallas con Administrador, Supervisor y Soporte. Si supera 30 min, parar y proponer dividirla en listados (Visitas, Repuestos) y formularios/detalles (sería una 14.ª tarea, a decidir por el usuario).

## Usuarios y Configuración (3 pantallas, 7 tablas, 8 modales, 1 paginación)

- [x] **T9. Usuarios: listado y alta.** Pantallas Users/Index (tabla de usuarios, 1 de 14; badge activo/inactivo; paginación, 1 de 6 → 6 de 6 acumuladas) y Users/Create; 2 modales: edición y confirmación (2 de 19). Política de contraseña visible y con error asociado. RF-1 a RF-9, RF-13 a RF-16, RF-20, RF-23, RF-24, RF-25, RF-26, RF-28
- Hecho cuando: las 2 pantallas no desbordan a los 4 anchos con 0, 1 y ≥200 usuarios; la tabla cumple RF-3/RF-4/RF-5; la paginación a 375 px no desborda; los 2 modales caben en 375 x 400 y funcionan igual; los errores de contraseña salen junto al campo y en español; 0 etiquetas sin nombre; CC con rol Administrador.

- [x] **T10. Configuración: tablas y pestañas.** Pantalla Configuracion/Index (1 de 32): cabecera, pestañas (sin desborde: se decide al medir entre `flex-wrap` y scroll interno), las 6 tablas de categorías, prioridades, estados, tipos, sedes y áreas (6 de 14, con `.table-responsive`, apiladas a 375 px, `.fila-inactiva` en lugar de `opacity-50`), badges activa/inactiva, estado vacío por tabla y revisión de los 11 usos de truncado/anchos fijos. RF-1 a RF-5, RF-8, RF-13 a RF-16, RF-23, RF-24, RF-25
- Hecho cuando: la pantalla no desborda a los 4 anchos; las 6 tablas se apilan a 375 px y son tabla a 600/768/1280 con scroll solo interno; las acciones de fila miden ≥44 px con 8 px de separación a 375/768 px; las pestañas caben o se desplazan solo dentro de su contenedor; las filas inactivas llegan a 4,5:1; CC con rol Administrador en cada una de las 6 pestañas.

- [x] **T11. Configuración: formularios de alta y modales de edición.** En Configuracion/Index: 6 formularios de alta y 6 modales de edición (6 de 19, quedan 19/19 con el resto), `for`/`id` únicos, `aria-label="Cerrar"`, rejilla y botones, validación mínima `data-validar` con mensajes en español `data-msg-*` en la vista (12 formularios en total, texto nuevo permitido por RNF-2b). RF-6, RF-7, RF-9, RF-20, RF-24, RF-26
- Hecho cuando: enviar vacío un formulario de alta o de edición muestra el mensaje en español junto al campo, asociado con `aria-describedby`, y no envía; con datos válidos se envía y guarda igual que antes; los 6 modales de edición caben en 375 x 400, con pie alcanzable; no hay ids duplicados en la página; 0 etiquetas sin nombre y 0 objetivos < 44 px a 375/768 px; CC con rol Administrador abriendo los 6 modales.

## Inicio, Dashboard, Reportes y Login (10 pantallas)

- [x] **T12. Inicio por rol, Home, Error, Privacidad, Dashboard, Reportes y Login.** Las 10 pantallas restantes: Login (con teclado en pantalla simulado 375 x 400, mostrar contraseña de 44 px, aro de foco, placeholder y subtítulo con contraste), Inicio, Error, Privacidad (enlaces de párrafo exentos), 4 inicios por rol (tarjetas), Dashboard (KPI, `chart-box`, `maintainAspectRatio:false`, leyenda abajo, etiquetas rotadas o truncadas, guarda `typeof Chart === 'undefined'`; comprobar con la CDN bloqueada) y Reportes (filtros, descargas). RF-1, RF-2, RF-10, RF-12, RF-13 a RF-16, RF-19, RF-20, RF-23, RF-24, RF-26
- Hecho cuando: las 10 pantallas no desbordan a los 4 anchos; Login completo a 375 x 400 con el botón alcanzable por scroll vertical y el degradado sin cambio de matiz; el Dashboard a 375 px no corta ejes ni leyenda; con la CDN de gráficos bloqueada el Dashboard muestra los KPI y la consola no tiene errores nuevos; los 4 inicios por rol se verifican con su rol; 0 etiquetas sin nombre; CC en las 10 pantallas.

## Cierre

- [x] **T13. Verificación global y cierre de la spec.** Revisión de CF-1 a CF-10: recorrido de las 32 pantallas con sus 4 anchos (más 375 x 400 y rotación) y los 4 roles aplicables; los 19 modales abiertos; las 14 tablas; las 6 paginaciones con ≥200 registros; Lighthouse de accesibilidad por pantalla y rol; capturas de cabecera, badges, estado vacío y botones de al menos 6 módulos a 1280 px frente a la línea base; zoom y texto al 200 % en 375, 768 y 1280; búsquedas globales (`outline:none`, `<label class="form-label">` sin `for`, `table-responsive`/`tabla-apilable` en las 14 tablas, truncados). Marcar la spec como «implementada», actualizar `MEMORY.md` y `CLAUDE.md` (carencias conocidas) con la orden del usuario, y preparar la lista manual de CF-1..CF-8 para el commit. RF-1 a RF-28
- Hecho cuando: `verificacion.md` recoge CF-1 a CF-10 con las cuentas 32 pantallas, 14 tablas y 19 modales; Lighthouse y consola muestran 0 campos sin etiqueta en todas las pantallas y roles; 0 reglas de foco sin sustituto; `dotnet build` sin errores; el usuario aprueba el cierre y la spec pasa a «implementada».
