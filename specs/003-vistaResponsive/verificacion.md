# Verificación 003 — Vista y responsive

Evidencias por tarea. Capturas en `capturas/` (1280 x 800, rol Administrador).

## T1. CSS base I (tokens, foco, contraste, aspecto uniforme)

### Línea base (antes de tocar nada)

Método: con sesión de Administrador, se descargó el HTML de cada pantalla y se contaron `input/select/textarea` (salvo hidden/submit/button) sin `aria-label`, `aria-labelledby` ni `<label>` asociada. Incluye el contenido estático de los modales (la carga del HTML los incluye) y excluye los campos generados por script (adjuntos, selects de modales cargados por script). La consola de Chrome cuenta lo mismo en Tickets/Index (6 avisos «No label associated with a form field»).

Solo se midió el rol **Administrador** (único con sesión disponible); Supervisor, Soporte y Usuario quedan para las tareas de módulo y T13.

| Pantalla (Administrador) | Campos | Sin etiqueta |
|---|---|---|
| Login (anónimo: usuario y contraseña con solo placeholder) | 2 | 2 |
| Home/Administrador | 3 | 3 |
| Tickets/Index | 6 | 6 |
| Tickets/Nuevos | 3 | 3 |
| Tickets/Create | 11 | 5 |
| Supervisor/Seguimiento | 3 | 3 |
| Inventario/Activos | 6 | 6 |
| Inventario/ActivoCreate | 14 | 5 |
| Inventario/Herramientas | 9 | 9 |
| Inventario/HerramientaCreate | 7 | 3 |
| Campo/Visitas | 5 | 5 |
| Campo/VisitaCreate | 8 | 3 |
| Campo/Repuestos | 5 | 5 |
| Campo/RepuestoCreate | 9 | 3 |
| Users/Index | 12 | 12 |
| Users/Create | 10 | 3 |
| Configuracion/Index | 7 | 7 |
| Dashboard/Index | 3 | 3 |
| Reportes/Index | 11 | 11 |

Los 3 de base en toda pantalla autenticada son las contraseñas del modal de cambio de contraseña del `_Layout`. No están medidas aún (por rol ni con modales dinámicos): Details/Edit de Tickets, detalles e ids de Inventario/Campo, ActivoEdit, Home de los otros 3 roles. T1 no corrige etiquetas (es de T3 en adelante).

Capturas de línea base a 1280 px: `capturas/antes-{1-tickets,2-herramientas,3-repuestos,4-usuarios,5-configuracion,6-dashboard}.png`. No hay captura del Login (había sesión activa y volver a entrar limitaba el uso del login).

Métricas de la pantalla testigo Tickets/Index (1280 px, antes): `html` 15 px; h4 243,77 197x27 (22,5 px); tarjeta 243,149 1015x85; botón 124x29; badge 57x19 (11,25 px, relleno 3,94/7,31); th 60x38; td 60x45; tabla 1015x84; menú 220x800; barra superior 220,0 1060x54.

### Hallazgo que no estaba en el plan

El proyecto incluye **Bootstrap 5.1.0** (`wwwroot/lib/bootstrap`), no 5.3. Consecuencias:
- No existen `bg-*-subtle` (31 usos en 6 vistas) ni `text-*-emphasis` ni las variables `--bs-btn-*`, `--bs-card-*`. Los «badges suaves» de Dashboard y Configuración hoy se pintan **sin fondo** (solo texto de color). Por eso la fila 8 del plan se midió sobre blanco (lo que se ve de verdad) y se corrigió con colores fijos de `.text-*`, sin definir esas clases (decidiría el usuario si quiere restaurar su fondo; sería un cambio visual).
- `btn-outline-warning`, `a` y `.card-header` se redefinieron con reglas explícitas, no con variables.

### Contrastes de las 14 combinaciones (antes -> después)

Fórmula WCAG del plan, medida en Chrome con los estilos computados (alfa compuesto sobre el fondo). Umbral 4,5 texto, 3 iconos/foco.

| # | Elemento / fondo | Antes | Después |
|---|---|---|---|
| 1 | `--color-muted` sobre #fff / #f4f6f9 / #f8fafc | 4,02 / 3,71 / 3,84 | 5,44 / 5,02 / 5,20 (`#5a6b82`) |
| 1 | `.text-muted` sobre #fff / #f4f6f9 / #f8f9fa | 4,69 / 4,33 / 4,45 | 5,44 / 5,02 / 5,16 |
| 2 | Placeholder login sobre #f8fafc | 2,16 | 5,20 |
| 3 | `.sidebar-section` (alfa .4 -> .78, 0,75 rem) | 2,56 | 5,36 |
| 3 | `.sidebar-role` (.5 -> .8) | 3,16 | 5,55 |
| 3 | `.sidebar-user-info` (.7 -> .85) | 4,65 | 6,04 |
| 3 | `.sidebar-username` (.9, sin cambio) | 6,55 | 6,55 |
| 3 | `.sidebar-link` inactivo (.78 -> .9) | 5,36 | 6,55 |
| 3 | `.sidebar-link:hover` (fondo +.10) | 4,37 | 5,23 |
| 3 | `.sidebar-link.active` (blanco, fondo +.15) | 5,33 | 5,33 |
| 3 | `.sidebar-logout` (rgba(255,180,180,.85) -> #ffcdcd) | 3,69 | 5,42 |
| 3 | `.sidebar-logout:hover` (#ffaaaa, sin cambio) | 4,71 | 4,71 |
| 4 | `.login-subtitle` en #2166b8 / #123d73 (.75 -> .92) | 3,98 / 6,83 | 5,13 / 9,42 |
| 4 | `.login-welcome` blanco en #2166b8 / #123d73 | 5,74 / 10,82 | 5,74 / 10,82 |
| 5 | `.text-danger` sobre #fff / #f8fafc (`#dc3545` -> `#b02a37`) | 4,53 / 4,33 | 6,50 / 6,21 |
| 5 | `.field-validation-error` (heredaba el gris del cuerpo) | 11,99 / 11,46 | 6,50 / 6,21 (ahora en rojo AA, con «⚠») |
| 6 | Badge de BD «Abierto» (blanco sobre #0d6efd) / «Media» (blanco sobre #ffc107) | 4,50 / 1,63 | 4,50 / 1,63 **pendiente**: lo resuelve `_BadgeEstado` (T3) |
| 7 | `badge bg-warning` / `bg-info` (blanco -> #212529) | 1,63 / 1,96 | 9,46 / 7,88 |
| 8 | `text-primary/success/warning/secondary` de los badges suaves, sobre #fff (real: sin fondo) | 4,50 / 4,53 / 1,63 / 4,69 | 7,66 / 6,45 (`#146c43`) / 5,54 / 5,44 (`#5a6b82`) |
| 8 | Las mismas sobre #f4f6f9 | 4,16 / 4,18 / 1,51 / 4,33 | 7,08 / 5,96 / 5,12 / 5,02 |
| 9 | `.text-warning` sobre #fff / #f4f6f9 | 1,63 / 1,51 | 5,54 / 5,12 (`#8a6100`) |
| 9 | `.text-info` sobre #fff / #f4f6f9 | 1,96 / 1,81 | 5,06 / 4,68 (`#087990`) |
| 10 | `btn-outline-warning` icono/borde sobre #fff | 1,63 | 5,54 (hover: blanco sobre `#8a6100`, 5,54) |
| 11 | `btn-outline-primary/success/secondary/danger` sobre #fff | 4,50 / 4,53 / 4,69 / 4,53 | sin cambio (solo medir) |
| 12 | Fila inactiva: `opacity-50` / `.fila-inactiva` sobre #fff | 3,12 | 5,44 (clase nueva; las vistas la adoptan en T10) |
| 13 | Estado vacío: texto e icono sobre #fff / #f4f6f9 | clase nueva | 5,44 / 5,02 |
| 14 | `text-primary` y enlaces `<a>` sobre #fff / #f4f6f9 | 4,50 / 4,16 | 7,66 / 7,08 (azul de marca `#1A5399`) |
| 14 | `.topbar-title`, `.login-show-btn` (#f8fafc), su hover `#CC1E24` | 11,99 / 7,32 / 5,31 | sin cambio |
| foco | `--focus-ring` `#1A5399` sobre #fff / #f4f6f9; aro blanco sobre el menú | nuevo | 7,66 / 7,08; 7,66 |

Notas: la medición «:active» de `btn-outline-warning` en Chrome dio 3,79 por estar a mitad de una transición de color; el par final (blanco sobre `#8a6100`) calculado es 5,54.

### Foco visible (RF-21, RF-22, CF-4)

- `grep -E "outline:\s*(none|0)|box-shadow:\s*none"` en `site.css`: **0**.
- Aro medido en Chrome tras Tab y `focus()`: menú (`.sidebar-link`, `.sidebar-logout`) `2px solid #fff`, offset -2, 7,66:1; campos, selects y botones del contenido `2px solid #1A5399`, offset 2, 7,66:1 (7,08:1 sobre #f4f6f9). Login: aro en el grupo del campo (`:focus-within`, `#1A5399`) y el propio input sin aro propio (transparente) para no duplicarlo. `.btn:focus` y `.form-control:focus` ya no quitan el contorno.

### Desborde y regresión (Tickets/Index, Administrador)

| Ancho | Desborde del documento | Culpables |
|---|---|---|
| 375 | 0 (scrollWidth 375) | ninguno |
| 600 | 0 | ninguno |
| 768 | 0 | ninguno |
| 1280 | 0 | ninguno |

Login (iframe sin sesión, mismos 4 anchos): scrollWidth = ancho en todos; subtítulo alfa .92, placeholder `#5a6b82`.

Regresión a 1280 px en Tickets/Index (misma geometría antes/después): html 15 px, h4 22,5 px en 243,77 197x27, tarjeta 243,149 1015x85, botón 124x29, th/td/tabla idénticos, menú 220x800, barra 54 px. Única diferencia: badge 56x19 en vez de 57x19 (peso 600 en lugar de 700 de Bootstrap, RF-15). Capturas «después»: `capturas/despues-*.png` (mismas 6 pantallas).

Consola: sin errores nuevos; solo el aviso preexistente «No label associated with a form field» (6 en Tickets/Index, igual que la línea base) y el verbose de contraseñas.

### Decisiones y límites de T1

- `--fs-titulo` es 1,5 rem (no 1,375 rem del plan): 1,5 rem iguala el `h4` actual a 1280 px y evita regresión; no hay `--fs-tabla` (aplicarlo achicaría las tablas actuales; se decide al migrar vistas).
- `--card-pad` queda en 1 rem también en >=576 px (el plan pedía 1,25 rem): evita cambiar el relleno de las tarjetas en escritorio. `.card-header` pasa a fondo blanco y relleno vertical .75 rem.
- `a` global usa el azul de marca (`#1A5399`) en lugar del azul de Bootstrap; `.text-primary` igual (4,16:1 -> 7,08:1 sobre #f4f6f9). Es un ajuste de tono permitido por RF-23.
- `.text-danger/.text-success/.text-secondary/.text-muted/.text-warning/.text-info` se sobrescriben globalmente con `!important` (los de Bootstrap ya lo llevan).
- Botones: Bootstrap ya da la misma altura y radio a todos; T1 no cambia sus tamaños (RF-17 no pide más en escritorio), solo corrige `btn-outline-warning` y el foco.
- Cabecera de página (`.page-header/.page-title/.page-subtitle/.page-actions`), `.estado-vacio` y `.fila-inactiva` quedan definidos pero ninguna vista los usa todavía; adoptarlos es de T4 en adelante.
- `html` 87,5 % / 93,75 % (14/15 px con el texto por defecto); barra superior con `min-height`.

### No verificado en T1
- Roles Supervisor, Soporte y Usuario (solo hay sesión de Administrador).
- Campos generados por script y contenido dinámico de modales en la línea base.
- Lighthouse (no se ejecutó; se mide en T13).
- Zoom/texto al 200 % (T13).

## T2. CSS base II (responsive común)

Cambio: solo `site.css` (bloque «RESPONSIVE COMUN», al final). Ninguna vista tocada. `dotnet build`: 0 errores; imagen reconstruida con `docker-compose up -d --build app`.

Método: en Tickets/Index (Administrador, `http://localhost:8080`) se inyectó con DevTools un bloque de prueba (cabecera `.page-header`, tabla `.tabla-apilable` con `.td-secundario`/`.btn-plegar`/`.celda-acciones`/`.fila-vacia`, interruptor, `.acciones-form`, paginación con `data-total`, `.chart-box` y un modal con cuerpo largo). No se guardó nada en el repo; recargar la página lo elimina. Viewport con la herramienta `emulate` (`resize_page` no cambió `innerWidth`).

| Ancho | Desborde documento | Tabla de prueba | Objetivos <44 px | Separación acciones / paginación |
|---|---|---|---|---|
| 375 | 0 (375) | `tr` `block`, `thead` recortado (visually-hidden) con 5 `th` en el árbol, `::before` con `data-label`, `.td-secundario` oculto y visible tras pulsar «Ver más», `.btn-plegar` visible | 0 | 8 / 8 px |
| 600 | 0 | `tr` `table-row` (tabla), `.btn-plegar` oculto, scroll solo dentro de `.table-responsive` (la palabra de 80 caracteres de la prueba lo fuerza) | 0 | 8 / 8 px |
| 768 | 0 | igual que 600 | 0 | 8 / 8 px |
| 1280 | 0 | pantalla real sin cambios | n/a (reglas solo ≤991) | n/a |

- Paginación a 375 px: 3 elementos visibles (‹, «2 de 9», ›); a ≥576 px, los 7 de la prueba.
- Botones de formulario a 375 px: columna de 333 px, `Guardar` (`order:-1`) antes que `Cancelar`; el interruptor mide 28x14 pero su `<label>` 298x44.
- Modal a 375x400: contenido 7..393 px (dentro de 400), cuerpo con scroll interno, pie en 268..392 con «Confirmar» primero, `btn-close` 44x44, sin desborde.
- `.chart-box`: 260 px de alto a 375 px, 300 px desde 576 px.
- Regresión a 1280 px (Tickets/Index real, tras reconstruir): idéntica a T1 (h4 196,66x27, tarjeta 1015x85,13, botón 123,7x29,19, badge 55,99x19,13, th 60x38, td 60x45,19, tabla 1015, menú 220x800, barra 54). Consola sin errores ni avisos.
- `grep -E "outline:\s*(none|0)|box-shadow:\s*none" site.css`: 0 (las tablas apiladas anulan el sombreado de filas con `--bs-table-accent-bg`, no con `box-shadow:none`).

### Hallazgos y decisiones fuera de plan
- **Semántica de tabla con `display:block`**: el árbol de accesibilidad de DevTools a 375 px ya no muestra `table`/`row`/`cell` en la tabla apilada (a 768 px se comprobó el snapshot por separado). Los textos de `thead` siguen presentes pero sin rol de cabecera, y añadir `role="table|rowgroup|row|columnheader|cell"` por script no cambió el snapshot de texto. Queda **sin resolver** si Chrome conserva la semántica (el snapshot de texto puede filtrar roles); decidir en T4 con la primera tabla real: el plan B (roles ARIA en el marcado) está previsto en el plan. Capturas del snapshot en `capturas/t2-snapshot-*.txt`.
- `--gap-acciones` pasó de `.5rem` a `max(.5rem, 8px)`: con `html` a 14 px, `.5rem` son 7 px y RF-24 pide 8.
- `.btn-plegar` se define como `.btn-plegar, .btn.btn-plegar` porque `.btn{display:inline-flex}` en ≤991 px lo volvía visible a 768 px.
- Plegado en ≥576 px: el botón queda oculto y las celdas `.td-secundario` visibles (solo se pliegan en ≤575 px).
- Paginación móvil: el «n de total» sale de `data-total` en el `<a>` activo (`.page-link[data-total]::after`); el parcial de T3 debe poner ese atributo y las clases `page-anterior`/`page-siguiente`.
- Formularios: la rejilla (1 columna ≤767, ≤2 en 768-991) se hace en el marcado con `col-12 col-md-6 col-lg-*` (plan D4); T2 no añade CSS de rejilla.
- Separación de acciones en ≥576 px depende de `.celda-acciones` con hijos directos; las acciones envueltas en otros contenedores se ajustarán al migrar cada vista.
- `.text-truncate` pasa a texto envolvente en ≤991 px para todas las vistas actuales (plan: «se anulan en ≤991 px»).

### No verificado en T2
- Login (con sesión activa `/Users/Login` redirige a Home; el `login-show-btn` de 44 px no se midió). Tampoco Supervisor/Soporte/Usuario, ni tablas, filtros o modales reales (se migran en T3 en adelante), ni teclado en pantalla real.

## T3. Layout, parciales comunes y site.js

Cambio: `_Layout.cshtml`, `_LoginLayout.cshtml`, `site.js`, `site.css` (solo el bloque del menú en <=991 px) y 3 parciales nuevos (`_EstadoVacio`, `_Paginacion`, `_BadgeEstado`). Ninguna vista de módulo migrada (los parciales se adoptan desde T4). `dotnet build`: 0 errores; imagen reconstruida con `docker-compose up -d --build app`. Sesión: solo Administrador.

### Menú lateral (RF-11), Tickets/Index, medido con eventos reales sobre el DOM
| Ancho | Botón | Cerrado | Abrir | Escape | Clic fuera | Elegir opción | Foco vuelve al botón |
|---|---|---|---|---|---|---|---|
| 375 | 44x44, `aria-label="Abrir menú"`, `aria-controls`, `aria-expanded` false/true | `visibility:hidden` (enlaces no tabulables), antes y después de la transición | `visibility:visible`, ancho 220 | cierra | cierra | cierra | sí en los 3 |
| 768 | igual | igual | igual | cierra | cierra | cierra | sí en los 3 |
- Rotación: con el menú abierto a 768 y paso a 1280 se cierra, sin backdrop, menú visible fijo; a 600 y 1280 desborde 0.
- "Cambiar contraseña" (botón del menú) cierra el menú móvil; Escape cierra el modal y el foco vuelve a `sidebarToggle` (a 375).
- Desborde del documento 0 a 375, 600, 768, 1280.

### Modal de cambio de contraseña (modal 1 de 19), 375 x 400, abierto con su disparador real
Contenido 7..368 x 7..393 (dentro del viewport), pie 262..393 alcanzable, cuerpo con scroll, `modal-dialog-scrollable`, `btn-close` 44x44 con `aria-label="Cerrar"`, 3 campos con `<label for>` (Contraseña actual / Nueva / Confirmar), desborde 0. Fuera del modal quedan 3 avisos de etiqueta en la página (filtros `search`, `estadoId`, `prioridadId` de Tickets/Index): son de T4.

### Funciones puras (comprobadas a mano, sin proyecto de tests)
Método: vista temporal (`Home/Administrador`) que renderizaba los parciales con casos de prueba; **retirada** (`git checkout`) y imagen reconstruida.
- `VentanaPaginas` (contando elementos numericos/elipsis del DOM): total 0 y 1 no pintan barra; total 2 -> 2; 7 -> 7 (todas); 8 -> 7 (`1 2 3 4 5 … 8` / `1 … 4 5 6 7 8`); 20 y 300 -> siempre 7 (`1 … 149 150 151 … 300`, `1 2 3 4 5 … 300`, `1 … 296 297 298 299 300`). `data-total` solo en el enlace activo, clases `page-anterior`/`page-siguiente` presentes. El caso total 1 devuelve 1 elemento en la función pero el parcial no dibuja barra (igual que las 6 paginaciones actuales, `TotalPaginas > 1`).
- `TextoSobre`: blanco #fff 15,43; negro 21,00; #ffc107 9,46; #0d6efd 4,50; #1A5399 7,66; #ff0000 5,25; #777777 4,69; #ffff00 14,37; vacío/inválido (`#123`, "") -> `bg-secondary`. Todos >= 4,5.
- `_EstadoVacio` renderiza icono (`aria-hidden`), mensaje y botón opcional.

### site.js (comprobado inyectando marcado temporal en la página, no guardado)
- Plegado: el delegado alterna `.fila-expandida`, `aria-expanded` y el texto «Ver más/Ver menos»; la celda secundaria pasa de `none` a `flex` y vuelve.
- Validación del servidor: `span[data-valmsg-for]` recibe `id` `err-<campo>`, el campo `aria-describedby` y `aria-invalid="true"`.
- `data-validar`: enviar vacío se cancela, muestra el `data-msg-required` en `div.invalid-feedback` asociado por `aria-describedby`, enfoca el campo y se limpia al escribir.
- `_LoginLayout` carga `site.js` (200). Consola de Tickets/Index: solo el aviso de etiquetas de los 3 filtros (T4) y el verbose de contraseñas.

### Decisiones fuera de plan
- `TextoSobre`: el plan afirmaba que blanco u `#212529` siempre dan 4,5:1; es falso (p. ej. #ff0000: 4,00 con blanco y 3,83 con #212529). La función elige blanco, luego `#212529` y, si ninguno llega a 4,5, negro puro (>= 4,58:1 garantizado). Ratios arriba.
- Se valida el color con `^#[0-9a-fA-F]{6}$` antes de ponerlo en `style` (evita inyección de CSS).
- Los enlaces Anterior/Siguiente deshabilitados se pintan como `<span class="page-link">` (no enlaces).
- El menú móvil usa `translateX(-100%)` y `width:min(220px,85vw)` (el plan pedía ese ancho).
- Se añadió que "Cambiar contraseña" cierre el menú y que al cerrar el modal el foco vuelva al botón del menú en <992 px.
- `aria-label` de Anterior/Siguiente sustituye al texto visible en el nombre accesible («Página anterior/siguiente», textos de la lista del plan).

### No verificado en T3
- Roles Supervisor, Soporte y Usuario (solo hay sesión de Administrador): el modal de contraseña es común al `_Layout`, pero no se abrió con esos roles.
- Login: no se vio renderizado (sesión activa); solo se comprobó que carga `site.js`. Los avisos de etiqueta del Login (usuario/contraseña) siguen pendientes (T12).
- Parciales aún sin uso en vistas reales (T4 en adelante); `_Paginacion` en móvil se midió en T2 con marcado equivalente.
- Rotación real de dispositivo (solo cambio de viewport de DevTools).

## T4. Tickets: listado y nuevos

Cambio: `Tickets/Index.cshtml`, `Tickets/Nuevos.cshtml`, `site.css` (3 reglas nuevas, ver abajo) y `_BadgeEstado.cshtml` (1 ajuste). Controladores y ViewModels sin cambios (la paginación usa `PaginaActual/TotalPaginas/Search/FiltroEstado/FiltroPrioridad`, ya existentes). `dotnet build`: 0 errores; imagen reconstruida con `docker-compose up -d --build app`. Sesión: solo Administrador.

Qué se hizo: cabecera `.page-header` con `h1.page-title`; filtros con `for`/`id`, rejilla `col-12 col-md-6 col-lg-4/3/2/3` y botón limpiar con `title`/`aria-label` «Limpiar filtros» (grupo `.acciones-form`); `tabla-apilable` + `data-label` en todos los `td` + `celda-acciones`; badges de estado/prioridad con `_BadgeEstado`; estado vacío con `_EstadoVacio`; paginación con `_Paginacion`; `aria-label="Cerrar"` en el modal «tomar». Ids, handlers JS (`btn-tomar`, `btnConfirmarTomar`, `row-<id>`, contador `.badge.bg-primary.fs-6`) y botones solo icono de la spec 002 intactos.

Campos por tabla: Tickets/Index 8 (admin/supervisor; 6 para otros roles) y Tickets/Nuevos 6: ninguna supera 8, no se añade plegado (`td-secundario`/`btn-plegar` sin usar aquí).

### Semántica de tabla con `display:block` (pendiente de T2): RESUELTO, no hace falta plan B
`take_snapshot` verbose a 375 px sobre la tabla real (`capturas/t4-snapshot-375-index.txt`): el árbol conserva `table > rowgroup > row > columnheader` (9 cabeceras) y `row > cell`; el nombre de cada `cell` incluye la etiqueta del `::before` («Título QA Ticket…»). Chrome conserva los roles; no se añadieron `role` en el marcado.

### Medición (Chrome DevTools, emulación de viewport, `http://localhost:8080`)
Estados 0 / 1 / ≥200 simulados SIN tocar datos: Index tiene 1 ticket real (caso 1); el caso 0 y el de muchos se inyectaron por script (10 filas con título de 150 caracteres sin espacios y paginación de 300 páginas con el mismo marcado del parcial; el servidor solo devuelve 10 por página). Nuevos tiene 0 tickets reales (caso 0 real: «Todo al día…»); 1 y 200 filas inyectadas con el mismo marcado de la vista.

| Pantalla / ancho | Desborde doc. | Tabla | Objetivos <44 px | Etiquetas sin nombre | ids duplicados |
|---|---|---|---|---|---|
| Index 375 (0/1/muchos) | 0 / 0 / 0 | `tr` block, 0 `td` sin label, 8 campos | 0 | 0 | 0 |
| Index 600 (1/muchos) | 0 | table-row, scroll solo interno | 0 | 0 | 0 |
| Index 768 (0/1/muchos) | 0 | table-row, sin scroll (títulos se parten) | 0 | 0 | 0 |
| Index 1280 (0/1/muchos) | 0 | table-row | n/a | 0 | 0 |
| Nuevos 375 (0/1/200) | 0 | `tr` block, 0 `td` sin label, 6 campos | 0 | 0 | 0 |
| Nuevos 600 / 768 (0/200) | 0 / 0 | table-row | 0 | 0 | 0 |
| Nuevos 1280 (0/200) | 0 | table-row; scroll interno solo con título de 90 letras sin espacios (como antes) | n/a | 0 | 0 |

- Paginación a 375 px con 300 páginas simuladas: 3 elementos (‹ Anterior 71x44, «1 de 300» 66x44, Siguiente › 79x44), derecha en 303 px; a 600 px, 7 elementos en 522 px.
- Filtros a 768 px: 2 columnas (350 px) con Buscar + limpiar en la última fila; a 375 px 1 columna con Buscar primero y limpiar debajo, ancho completo; a 1280 px: misma disposición de antes (4 / 3 / 2 / 3 columnas).
- Regresión a 1280 (Tickets/Index real): idéntica a T2 (h1 196,66x27 en 242,5/76,5; tarjeta 1015x85,13; botón 123,7x29,19; badge 55,99x19,13; th 60x38; td 60x45,19; tabla 1015x83,69). Capturas: `capturas/t4-*.png`.
- Modal «tomar ticket» (Nuevos; abierto con su disparador `.btn-tomar` real, con una fila inyectada porque no hay tickets nuevos): 375x800 y 375x400, contenido 94..306 px dentro de 400, pie 186..306 con «Confirmar» (44 px) antes de «Cancelar», `btn-close` 44x44 con `aria-label="Cerrar"`, título «#1 — Ticket 1» rellenado por el script, Escape lo cierra, desborde 0. **No se pulsó «Confirmar»** (no se toman tickets reales); el `fetch` a `/Tickets/TomarTicket` y la actualización del contador no se probaron.
- Consola: sin errores ni avisos nuevos; ya no hay avisos de etiquetas en Index ni Nuevos (solo el verbose de contraseñas del modal del `_Layout`).

### Hallazgos y decisiones fuera de plan
- `site.css` (≤575 px): las celdas de `.tabla-apilable` anulan `ps-4`/`pe-4` (`!important` descentraban la etiqueta de la primera celda); las etiquetas `::before` fijan `.875rem` (si no, heredaban `.small` en unas celdas y no en otras).
- `site.css` (≤991 px): enlaces de texto dentro de `td` de `.tabla-apilable` (título del ticket) con `min-height:44px` (medidos 132x21 antes del ajuste); y `overflow-wrap:anywhere` en las celdas para partir palabras largas en lugar de ensanchar la tabla (RF-2).
- `_BadgeEstado.cshtml`: se quita el `border:1px solid` inline (sumaba 2 px al badge: 58x21 en vez de 56x19); el borde sutil ya lo da `.badge[style*="background"]` con `box-shadow` inset.
- `_Paginacion` recibe el generador de URL desde un bloque `@{ }` de la vista (Razor no admite una lambda con `$"…"` dentro del atributo `model`). La URL ahora codifica `search` con `Uri.EscapeDataString` (antes sin codificar).
- Estado vacío de Nuevos: el mensaje es «Todo al día. No hay tickets pendientes de asignación.» (antes un `h5` + párrafo con emoji ✅); mismo texto, un solo párrafo con icono FontAwesome. Index: icono `fa-inbox`, texto sin cambios, sin botón de crear (la cabecera ya lo tiene).
- Contador «N sin asignar» de Nuevos: `align-self-start` en móvil dentro de la cabecera apilada.
- `Nuevos` sigue enlazada a `IList<Ticket>` (entidad) y `Index` a un ViewModel con `IList<Ticket>`; no se tocó (fuera de T4).

### No verificado en T4
- Roles Supervisor, Soporte y Usuario (solo hay sesión de Administrador): la columna «Creado por/Asignado a» solo cambia para Administrador/Supervisor, y Nuevos lo ven Soporte/Supervisor; las filas por rol y el flujo real de «tomar» no se ejercitaron.
- Estados ≥200 con datos reales y paginación servida por el parcial con ≥200 registros (se usó marcado equivalente inyectado; el parcial se comprobó en T3 con total 1, 7 y 300).
- Contraste de las etiquetas y badges nuevos y foco por teclado (no se midió el script completo; T13/Lighthouse).
- Roles ARIA con lector de pantalla real (solo árbol de accesibilidad de Chrome).

## T5. Seguimiento, Tickets/Create, Edit y Details

Archivos: `Views/Supervisor/Seguimiento.cshtml`, `Views/Tickets/{Create,Edit,Details}.cshtml`, `wwwroot/css/site.css`. Sin cambios de servidor ni de ViewModels. Capturas `capturas/t5-*.png`. `dotnet build`: 0 errores; imagen reconstruida con `docker-compose up -d --build app`. Rol Administrador (único con sesión).

### Cambios
- Cabecera `.page-header` + `h1.page-title` en las 4 pantallas (Create/Edit con «Volver» + título; Details con acciones solo icono en `.page-actions.acciones-icono`, que en móvil no se estiran a ancho completo).
- Seguimiento: `tabla-apilable`, `data-label` en cada celda (Técnico, un estado por columna, Total), `tfoot` también apilado. Columnas reales = Técnico + N estados + Total, con N = estados distintos de los tickets activos (los calcula el controlador; hoy 1). Con N > 6 (más de 8 campos) los estados 7.º en adelante llevan `td-secundario` y cada tarjeta (fila y total) lleva `btn-plegar` en su celda Total. Estado vacío con `_EstadoVacio` (dos textos originales), badge de prioridad con `_BadgeEstado`, `aria-label` en la barra de progreso.
- Create/Edit: filas `col-12 col-md-6`, `acciones-form`, relleno `p-3 p-sm-4`. `label for` del solicitante (antes apuntaba a un id inexistente porque el select tiene `id="selectSolicitante"`), `span` de validación para el solicitante, `data-msg-required` en español, combobox de Select2 con `aria-labelledby` (etiqueta + valor). Adjuntos: la zona de arrastre es `role="button"` `tabindex="0"` con Enter/Espacio; la lista se genera con DOM (`textContent`, no `innerHTML` con el nombre del archivo), botón de quitar con `aria-label="Quitar <nombre>"` y 44 x 44 px; `label for="inputArchivos"`; `aria-live="polite"` en la lista.
- Details: badges con `_BadgeEstado` (antes `font-size:.85rem` en línea), `aria-label` en el textarea de comentario y los 3 selectores del panel, `role="alert"` en los 4 avisos de error, nombres accesibles «Descargar <archivo>», `aria-labelledby` + `aria-label="Cerrar"` en el modal de cierre, enlaces de adjunto a 44 px (`.enlace-tap`), `min-w-0` para que el texto largo no ensanche las filas flex, `font-size` en línea de .7rem a .75rem. Lógica JS, ids y modal sin cambios (los 4 botones, `postAction`, `confirmCerrarBtn`).
- `site.css`: `.min-w-0`, `.celda-valor`, `.adjunto-nombre`; en <=991 `.enlace-tap`, Select2 a 44 px (selección, opciones y buscador); en <=575 el `tfoot` se apila como el `tbody`, `.form-check` con `flex-wrap` (el texto de ayuda dentro de `.form-check` quedaba encima de la etiqueta), borde inferior de celdas con especificidad suficiente (Bootstrap 5.1 pinta `currentColor` la última fila de un `tbody` seguido de `tfoot`).

### Medición (Chrome DevTools, emulación de viewport, `http://localhost:8080`)
Script del plan: desborde del documento, elementos fuera de ventana, objetivos <44 px (solo <=991), campos sin nombre accesible, ids duplicados, estado de las tablas.

| Pantalla / ancho | Desborde doc. | Tabla | Objetivos <44 px | Campos sin nombre | ids dup. |
|---|---|---|---|---|---|
| Seguimiento 375 | 0 | `tr` block, 0 `td` sin label, 3 campos | 0 | 0 | 0 |
| Seguimiento 375 con 9 estados (DOM) | 0 | 8 campos visibles por tarjeta (fila y total) | 0 | 0 | 0 |
| Seguimiento 600 / 768 / 1280 | 0 / 0 / 0 | table-row, sin scroll | 0 / 0 / n/a | 0 | 0 |
| Create 375 / 600 / 768 / 1280 | 0 | n/a | 0 / 0 / 0 / n/a | 0 | 0 |
| Edit 375 / 600 / 768 / 1280 | 0 | n/a | 0 | 0 | 0 |
| Details 375 / 600 / 768 / 1280 | 0 | n/a | 0 | 0 | 0 |

- Plegado (9 estados inyectados por DOM, solo cliente): 8 `td` visibles; «Ver más» pone `aria-expanded="true"`, texto «Ver menos» y 11 `td` visibles; otra pulsación restaura. Botón de 44 px.
- Create: 3 filas de errores a 375 px con envío de cliente (solicitante, título y descripción vacíos, sin POST): mensaje junto al campo en español, `aria-invalid="true"` y `aria-describedby` apuntando al mensaje. Envío al servidor con el formulario forzado y vacío (sin crear nada, `ModelState` inválido): errores «El título es obligatorio», «La descripción es obligatoria», «Selecciona una prioridad.» con `aria-describedby` y `aria-invalid`. Edit: igual con título vacío (cliente y servidor); el ticket 1 no se modificó.
- Adjuntos por script (2 archivos simulados, uno de 11 MB, sin subir): nombre largo sin espacios se parte en líneas, «Muy grande» y botones «Quitar <nombre>» de 44 x 44. Select2 de solicitante: selección 44 px, opciones 44 px, buscador 44 px.
- Details: a 375 y 768 px el panel lateral va bajo el contenido (a 768 px: `col-lg-4` empieza en y=803, tras la columna principal que acaba en 781, 746 px de ancho). Con título de 120 caracteres sin espacios, comentario de 210 caracteres sin espacios y adjunto de nombre largo (inyectados por DOM): sin desborde.
- Modal «Cerrar ticket» (abierto con `#btnCerrar`): 375x800 contenido 7..368 px, pie en 535 px; 375x400 contenido 65..335 px dentro de los 400; «Sí, cerrar» (44 px) antes de «Cancelar» (44 px), `btn-close` 44 x 44 con `aria-label="Cerrar"`; Escape y Cancelar lo cierran. **No se pulsó «Sí, cerrar»** (no se cierran tickets reales); `fetch` a `/Tickets/Cerrar` sin probar.
- Consola: sin errores ni avisos (solo el verbose de contraseñas del modal del `_Layout`). Un aviso «Blocked aria-hidden» salió con mi `aria-hidden="true"` inicial en el modal; se quitó y desapareció.
- Regresión a 1280: capturas `t5-1280-seguimiento.png`, `t5-1280-create.png`, `t5-1280-details.png`; misma disposición de antes (badges de Details pasan de .85rem a .75rem por la escala única, RF-15).

### Hallazgos y decisiones fuera de plan
- `data-msg-required="Selecciona un solicitante."` en la vista (el `required` nativo daba «This field is required.» de jquery-validate): el plan ponía los mensajes en los ViewModels, pero cambiar `CreadoPorId` a `[Required]` cambiaría el comportamiento del servidor para el rol Usuario (RNF-2). Texto dentro de RF-26.
- Se añadió el `span asp-validation-for="CreadoPorId"` (necesario: sin él jquery.validate.unobtrusive oculta el mensaje).
- La zona de arrastre pasa a ser accesible con teclado (`role`, `tabindex`, Enter/Espacio): código de cliente propio mínimo (RNF-1), no estaba descrito.
- La lista de adjuntos se reescribió con `createElement` en vez de `innerHTML` (el nombre del archivo se interpolaba sin escapar); mismo aspecto y mismo `window.quitarArchivo`.
- `.form-check` con `flex-wrap` en <=991 es una corrección global (no limitada a Create): afecta a toda casilla con texto de ayuda dentro de `.form-check`.
- Seguimiento: el umbral de plegado es 6 estados (Técnico + 6 + Total = 8 campos); la fila «Total» del `tfoot` también lleva su botón.

### No verificado en T5
- Roles Supervisor, Soporte y Usuario (solo hay sesión de Administrador): Seguimiento lo ve Supervisor/Administrador; Create para Usuario (sin Solicitante, Prioridad ni «Asignar a») y Details según rol (comentario «Nota interna», paneles de estado/asignar/prioridad) no se ejercitaron. Edit es solo Administrador.
- Estados reales > 6 en Seguimiento (hoy hay 1 estado activo): el plegado se probó con columnas clonadas por DOM.
- Acciones POST de Details (comentario, estado, asignar, prioridad, cerrar) y envío válido de Create/Edit: no se ejecutaron para no modificar datos.
- Contraste de foco y de texto nuevo, texto/zoom al 200 %, lector de pantalla real: T13.


## T6. Inventario: listados de activos y herramientas (cerrada con salvedades, ver «No verificado»)

Archivos: `Views/Inventario/Activos.cshtml`, `Views/Inventario/Herramientas.cshtml`, `wwwroot/css/site.css` (regla `td.celda-acciones{white-space:nowrap}` global y `margin-bottom:0` de las acciones en <=991 px). `dotnet build`: 0 errores. Sin cambios de servidor ni de ViewModels.

Cambios en vistas: cabecera `.page-header`/`h1.page-title`/`.page-actions`; filtros con `for`/`id` y rejilla `col-12 col-md-6 col-lg-*` + `.acciones-form` (Herramientas: «✕» con `title`/`aria-label` «Limpiar filtros»); `tabla-apilable` + `data-label` + `celda-acciones` (Activos 8 campos, Herramientas 6: sin plegado); estado vacío con `_EstadoVacio` en `td.fila-vacia`; `bg-warning` con `text-dark` (En Mantenimiento / En Reparación); `_Paginacion` con URL en bloque `@{}` (`Uri.EscapeDataString`); 3 modales con `aria-labelledby`, `aria-label="Cerrar"`, `modal-dialog-scrollable`, `label for` en `selectTecnico`, `txtAsignarObs`, `txtDevolverObs`, `role="alert"` en el error de asignar. Ids y handlers JS (`confirmarBaja`, `abrirAsignar`, `confirmarDevolver`, `bajaBtn`, `confirmarAsignarBtn`, `confirmarDevolverBtn`) sin cambios.

### Medido (Administrador, Activos, 8 registros reales, `localhost:8080`)
- 375: desborde 0, `tr` block, 0 `td` sin label, 0 objetivos <44 px, 0 campos sin nombre, 0 ids duplicados; acciones 44x44 con 8 px de separación (33/85/137). Captura `capturas/t6-375-activos.png`.
- 600: desborde 0, `tr` table-row, scroll solo dentro de `.table-responsive`, 0 <44 px, 0 sin nombre.
- 768: desborde 0, table-row, 0 <44 px. Con la CSS nueva (inyectada, ver abajo) las 3 acciones van en una fila con 8 px y la tabla desplaza solo dentro de su contenedor. Captura `t6-768-activos.png`.
- 1280: con la CSS nueva (inyectada) las 3 acciones quedan en una línea (antes la 3.ª caía debajo). Captura `t6-1280-activos.png`.

### Reconstrucción y mediciones finales (tras liberar el disco)
`dotnet build`: 0 errores; `docker-compose up -d --build app` correcto (32 GB libres; el bloqueo anterior era el disco lleno). Ahora las mediciones son con la imagen real, no con CSS inyectada. Administrador, `localhost:8080`, viewport con `emulate`.

| Pantalla / ancho | Desborde doc. | Tabla | Objetivos <44 px | Sin nombre | ids dup. |
|---|---|---|---|---|---|
| Herramientas 375 | 0 | `tr` block, 0 `td` sin label | 0 | 0 | 0 |
| Herramientas 600 | 0 | table-row, scroll interno (605 > 558) | 0 | n/a | n/a |
| Herramientas 768 | 0 | table-row, sin scroll | 0 | n/a | n/a |
| Herramientas 1280 | 0 | table-row, sin scroll | n/a | n/a | n/a |

- Acciones de Herramientas: 44x44 con 8 px de separación a 375, 600 y 768 px, en una sola fila. Con los datos reales ninguna fila tiene 4 acciones a la vez (Asignar y Devolver son excluyentes: 3 por fila), así que el caso de «4 acciones en dos líneas» no se pudo ejercitar.
- Icono «Devolver»: color `rgb(138,97,0)` sobre blanco (≈5,9:1, supera 3:1 y 4,5:1).
- Estado vacío (`?search=zzzzsinresultado`) en Activos y Herramientas a 375 px: mensaje «No se encontraron activos/herramientas.», `fila-vacia` en bloque, sin desborde, sin barra de paginación.
- Modales a 375x400 abiertos con su disparador real (`abrirAsignar`, `confirmarDevolver`, baja de activo): caben (contenido 17..384, 46..355 y 60..340 px de 400), pie y botones de 44 px alcanzables, `btn-close` 44x44 con `aria-label="Cerrar"`, `aria-labelledby`, 0 campos sin nombre (incluido `selectTecnico` cargado por script, 8 opciones), cierran bien.
- Consola de Activos: sin errores ni avisos.
- Capturas: `t6-375-herramientas.png`, `t6-1280-herramientas.png` (misma disposición que antes, 3 acciones en una línea).

### No verificado en T6
- Paginación con ≥200 registros en Activos/Herramientas: solo hay 8 de cada (1 página, la barra no se dibuja). El parcial `_Paginacion` se probó en T2/T3/T4 (ventana de 7 elementos, móvil «n de total», 200+ tickets); aquí solo se comprobó que la URL se arma en el bloque `@{}`. No se crearon registros de prueba para no ensuciar la BD.
- Estado con 1 registro y con ≥200 registros.
- Roles Supervisor y Soporte (no hay credenciales; Soporte ve Herramientas con solo «Ver»).
- Acciones POST reales (baja, asignar, devolver): no se ejecutaron para no modificar datos.
- Consola de Herramientas con los modales abiertos: no se listó por separado.


## T7. Inventario: formularios y detalles

Archivos: `Views/Inventario/{ActivoCreate,HerramientaCreate,ActivoDetails,HerramientaDetails}.cshtml` (`ActivoEdit` reutiliza `ActivoCreate`). Sin cambios de servidor, ViewModels ni `site.css`. `dotnet build`: 0 errores; imagen reconstruida con `docker-compose up -d --build app`. Rol Administrador (único con sesión). Capturas `capturas/t7-1280-activo-details.png` y `t7-1280-herramienta-details.png`.

### Cambios
- Cabecera `.page-header` + `h1.page-title` en las 5 pantallas (Details con `.page-actions.acciones-icono`, `min-w-0`).
- Formularios: `card-body p-3 p-sm-4`; Activo pasa de `col-md-4/8` a `col-12 col-md-6 col-lg-4/8` (1 columna a 375, 2 a 768, disposición de siempre a 1280); Herramienta `col-12 col-md-6`; `.acciones-form` (principal primero a 375 px por CSS ya existente).
- **Corrección de etiquetas**: en `ActivoCreate` los `<select>` Sede y Área tienen `id="sedeSelect"`/`id="areaSelect"` pero su `<label asp-for>` apuntaba a `SedeId`/`AreaId` (sin elemento), es decir, sin etiqueta asociada. Ahora `label for="sedeSelect"`/`"areaSelect"`.
- Details: badges `warning` con `text-dark` (En Mantenimiento / En Reparación); listas de información con `gap-3`, `px-3 px-sm-4`, valores `text-end text-break min-w-0`; movimientos y asignaciones con `px-3 px-sm-4`, `min-w-0 text-break`; avatar `.7rem` → `.75rem` (escala única); estados vacíos de las secciones con `_EstadoVacio`.
- Modales (3): `aria-labelledby` + `id` en el título, `aria-label="Cerrar"`, `modal-dialog-scrollable`, `label for` en `selectUsuarioActivo`, `selectTecnicoH`, `txtAsignarObs`, `txtDevolverObs`, `role="alert"` en los avisos de error. No se puso `aria-hidden` inicial (daba aviso en T5). Ids y JS sin cambios.

### Medición (Chrome DevTools, `emulate`, `localhost:8080`)
| Pantalla | 375 | 600 | 768 | 1280 |
|---|---|---|---|---|
| ActivoCreate | desborde 0, 1 col, 0 <44 px, 0 sin nombre, 0 `for` rotos, 0 ids dup. | 0 | 0, 2 col | 0, 3 col |
| ActivoEdit/1 | igual, 1 col | 0 | 0, 2 col | 0 |
| HerramientaCreate | igual, 1 col | n/a | 0, 2 col | 0 |
| ActivoDetails/1 | 0, 0 <44 px, panel bajo el contenido | 0 | 0, 0 <44 px, panel bajo | 0 |
| HerramientaDetails/6 (asignada) | 0, 0 <44 px, panel bajo | n/a | n/a | n/a |
| HerramientaDetails/1 (disponible) | n/a | 0 | 0, 0 <44 px, panel bajo | 0 |

- Botones Guardar/Cancelar a 375 px: 305 px de ancho (completo), 44 px de alto, principal primero (ActivoCreate y HerramientaCreate).
- Errores de validación a 375 px (envío vacío, solo cliente, no se creó nada): «El nombre es obligatorio» y «El tipo es obligatorio» junto al campo, `aria-describedby="err-…"` y `aria-invalid="true"`; ActivoEdit con nombre vacío igual. Sin desborde.
- Texto largo inyectado por DOM (título de 120 caracteres y valores de 80 sin espacios) en ActivoDetails y HerramientaDetails a 375 px: sin desborde.
- Acciones de cabecera de Details: 44x44 con 8 px de separación a 375 px; icono «Devolver» `rgb(138,97,0)` (≈5,9:1).
- Modales a 375x400 abiertos con su disparador real (`#btnAsignarActivo`, `#btnAsignarH`, `#btnDevolver`): caben (61..339, 20..380, 35..365 de 400), botones y selects de 44 px, cierre de 44x44 con `aria-label="Cerrar"`, etiquetas asociadas, validación vacía muestra el aviso con `role="alert"` («Selecciona un usuario.» / «Selecciona técnico.»), cierran bien. Selects cargados por script con 8 opciones.
- Estados vacíos de HerramientaDetails/1: «Sin asignaciones registradas.» y «Sin movimientos.» con `_EstadoVacio`.
- Consola (ActivoDetails y HerramientaDetails/1): sin errores ni avisos.

### No verificado en T7
- Roles Supervisor, Soporte y Usuario (solo hay sesión de Administrador). Supervisor ve los modales de asignar/devolver de herramienta y de asignar activo; los botones de cabecera de ActivoDetails/HerramientaDetails dependen del rol.
- Casillas e interruptores con etiqueta de 44 px: estas 5 pantallas no tienen ninguna.
- Envíos válidos y POST reales (crear/editar activo o herramienta, asignar, devolver): no se ejecutaron para no modificar datos; tampoco el envío de servidor con `ModelState` inválido (solo validación de cliente).
- ActivoDetails en un activo con garantía vencida / en mantenimiento (badge `warning text-dark`): no hay datos; el cambio es solo de clase.
- Herramientas con movimientos o asignaciones reales largas: el texto largo se probó inyectado por DOM.
- HerramientaCreate a 600 px y HerramientaDetails/6 a 600/768/1280 (se midió la 1, mismo marcado).
