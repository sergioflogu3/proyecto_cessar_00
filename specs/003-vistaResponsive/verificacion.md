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


## T8. Campo: visitas y repuestos (listados, formularios y detalles)

Archivos: `Views/Campo/{Visitas,Repuestos,VisitaCreate,RepuestoCreate,VisitaDetails,RepuestoDetails}.cshtml` y `wwwroot/css/site.css` (1 regla nueva `.badge-enlace`, solo en <=991 px). Sin cambios de servidor ni ViewModels. `dotnet build`: 0 errores; imagen reconstruida con `docker-compose up -d --build app` (2 veces: la segunda tras el hallazgo del badge). Rol Administrador. Capturas `capturas/t8-1280-{visitas,repuestos,visita-details}.png`.

### Cambios
- Listados: `.page-header`/`h1.page-title`/`.page-actions`; filtros con `for`/`id` y rejilla `col-12 col-md-6 col-lg-5/3` + `.acciones-form` (Buscar/Limpiar); `tabla-apilable` + `data-label` + `celda-acciones` (Visitas 7 campos con técnico, Repuestos 7: ninguna supera 8, sin plegado); estado vacío con `_EstadoVacio`; alerta de stock bajo con `flex-wrap` y `role="status"`; `role="alert"` y `aria-label="Cerrar"` en el aviso de éxito; paginación con `_Paginacion` (URL en bloque `@{}` con `Uri.EscapeDataString`, antes se interpolaba `search` sin escapar).
- Formularios: `.page-header`, `p-3 p-sm-4`, `col-12 col-md-6`, `.acciones-form`.
- Detalles: cabecera con `.page-actions.acciones-icono`, listas de información con `gap-3`, valores `text-end text-break min-w-0`, filas de consumos/stock con `min-w-0 text-break`, estados vacíos con `_EstadoVacio`, `flex-wrap` en la cabecera de tarjeta con botón.
- Modales (2): `aria-labelledby`, `aria-label="Cerrar"`, `modal-dialog-scrollable`, `label for` en todos los campos (buscarRepuesto, txtCantidad, selectSede, txtConsumoObs; selectSede, selectTipo, txtCantidad), `aria-label="Repuestos encontrados"` en la lista de repuestos, `col-12 col-sm-6` para Cantidad/Sede, `role="alert"` en los avisos. Ids y JS sin cambios.
- Hallazgo y corrección: el enlace «Ticket #n» de VisitaDetails (`a.badge`) medía 61x18 px (<44) y `bg-info` llevaba texto blanco (contraste insuficiente). Ahora `bg-info text-dark badge-enlace`: 61x44 px, texto `#212529` sobre `rgb(13,202,240)`.

### Medición (Chrome DevTools, `emulate`, `localhost:8080`)
| Pantalla | 375 | 600 | 768 | 1280 |
|---|---|---|---|---|
| Visitas (7 reg.) | desborde 0, `tr` block, 0 `td` sin label, 0 <44 px, 0 sin nombre, 0 ids dup. | table-row, scroll solo interno (558/558) | table-row, 0 <44 px | table-row |
| Repuestos (10 reg.) | igual; alerta de stock a 333 px | table-row | table-row | table-row |
| VisitaCreate | 1 col, botones 305x44 | n/a | n/a | 2 col |
| RepuestoCreate | 1 col, botones 305x44 | n/a | 2 col | 0 |
| VisitaDetails/6 | 0, 0 <44 px, panel bajo | 0 | 0, panel bajo | 0 |
| RepuestoDetails/1 | 0, 0 <44 px, panel bajo | 0 | 0, panel bajo | 0 |

- Estado vacío (`?search=zzzsinresultado`) a 375 px en ambos listados: «No se encontraron visitas/repuestos.», 1 fila, sin desborde, sin paginación.
- Errores de validación a 375 px (envío vacío, solo cliente): VisitaCreate «La dirección es obligatoria»; RepuestoCreate «El nombre es obligatorio» y «La categoría es obligatoria»; todos junto al campo, `aria-describedby="err-…"` y `aria-invalid="true"`.
- Texto largo inyectado por DOM (título 120 caracteres, valores 80 sin espacios) en VisitaDetails y RepuestoDetails a 375 px: sin desborde.
- Menú «Cambiar estado» de VisitaDetails (abierto a 375 px): 3 opciones de 138x44 px.
- Modal de consumo (VisitaDetails/6, 375x400, abierto con `#btnAgregarConsumo`): contenido 7..393, cuerpo con scroll interno (434/223) y pie alcanzable, controles de 44 px (la lista de repuestos es un `size=4` de 83 px), 0 campos sin nombre, 10 repuestos cargados por script, aviso «Selecciona un repuesto.» con `role="alert"`, cierra bien.
- Modal de ajuste de stock (RepuestoDetails/1, 375x400, con `#btnAjustarStock`): 7..393, controles de 44 px, 3 campos con etiqueta, aviso «La cantidad debe ser mayor a 0.» con `role="alert"`, pie alcanzable, cierra bien.
- Consola: sin errores ni avisos.
- Regresión a 1280: las 3 capturas mantienen la disposición previa (filtros en una fila, 1 acción por fila).

### Hallazgos fuera de plan
- `#selectSede` del modal de consumo (VisitaDetails) nunca se llena: el JS pide `/Inventario/GetAreasBySede?sedeId=0` y `/Campo/GetRepuestosSelect` pero no pinta sedes, así que solo ofrece «No descontar». Es un fallo funcional previo; no se tocó (fuera de RNF-1) y queda para una spec de deuda.
- `fs-6` en los badges de cantidad/estado de los detalles se mantiene (1 rem, mayor que la escala de badges; no hay problema de contraste).

### No verificado en T8
- Paginación con ≥200 registros: solo hay 7 visitas y 10 repuestos (1 página). El parcial `_Paginacion` ya se probó en T2/T3/T4; aquí solo se migró la URL. No se crearon datos de prueba.
- Listados con 1 registro y con ≥200 registros; VisitaCreate a 600/768 y RepuestoCreate a 600 (mismo marcado que lo medido).
- VisitaEdit y RepuestoEdit (reutilizan `VisitaCreate`/`RepuestoCreate`).
- Roles Supervisor y Soporte (solo hay sesión de Administrador). Soporte ve solo sus visitas y, sin columna Técnico, la tabla tiene 6 campos.
- Acciones POST reales (cambiar estado, registrar consumo, ajustar stock, crear/editar): no se ejecutaron para no modificar datos; el modal de consumo se probó solo hasta la validación de cliente.
- Tiempo: la tarea superó los 30 min previstos; el plan sugería proponer dividirla, pero se terminó entera sin incidencias (decisión del usuario fue continuar).


## T9. Usuarios: listado y alta

Archivos: `Views/Users/{Index,Create}.cshtml` y `wwwroot/css/site.css` (1 regla nueva dentro del bloque <=991 px). Sin cambios de servidor ni ViewModels. `dotnet build`: 0 errores; imagen reconstruida con `docker-compose up -d --build app` (2 veces). Rol Administrador (la sesión caducó a los 30 min y se volvió a iniciar). Capturas `capturas/t9-1280-usuarios.png` y `t9-1280-usuarios-create.png`.

### Cambios
- Index: `.page-header`/`h1.page-title`/`.page-actions`; filtros con `for`/`id` y rejilla `col-12 col-md-6 col-lg-5/3` + `.acciones-form`; `tabla-apilable` + `data-label` + `celda-acciones` (8 campos, sin plegado); estado vacío `_EstadoVacio`; badges `warning`/`info` con `text-dark` (rol Supervisor/Soporte); acciones Editar/Inactivar con `aria-label` y iconos `aria-hidden`; paginación con `_Paginacion` (URL con `Uri.EscapeDataString`; `activo` ya no se envía como cadena vacía de `bool?` sin escapar).
- Modales (2): `aria-labelledby`, `aria-label="Cerrar"`, `modal-dialog-scrollable`; edición con `label for` en los 7 campos, `col-12 col-md-6`, `role="alert"` en `#editErrors`, `aria-describedby="editPasswordAyuda"` en la contraseña; confirmación con `text-break` en el nombre.
- JS mínimo (RF-26): `marcarErrorPassword()` pone `aria-invalid="true"` y añade `#editErrors` a `aria-describedby` de la contraseña cuando el error menciona «contraseña»; `scrollIntoView({block:'nearest'})` lleva el aviso a la vista dentro del cuerpo con scroll.
- Create: `.page-header`, `p-3 p-sm-4`, `col-12 col-md-6`, `.acciones-form`, `id`+`aria-describedby="passwordAyuda"` en el texto de la política.
- **Corrección global en `site.css`**: `.modal-content > form { display:flex; flex-direction:column; flex:1 1 auto; min-height:0 }` en <=991 px. El modal de edición envuelve cuerpo y pie en un `<form>`, lo que rompía el scroll del cuerpo: a 375x400 el pie quedaba en y=664-788 (inalcanzable). Tras el cambio: cuerpo con scroll (656/201), pie en y=392 y botón «Guardar cambios» alcanzable. Aplica a cualquier modal con ese patrón (T11 lo reutilizará).

### Medición (Chrome DevTools, `emulate`, `localhost:8080`)
| Pantalla | 375 | 600 | 768 | 1280 |
|---|---|---|---|---|
| Users/Index (9 usuarios) | desborde 0, `tr` block, 0 `td` sin label, 0 <44 px, 0 sin nombre, 0 ids dup.; acciones 44x44 a 8 px | table-row, scroll interno (598/558), 0 <44 px | table-row, 0 <44 px | table-row, sin scroll |
| Users/Create | 1 col, botones 305x44 (Crear antes que Cancelar), 0 <44 px, 0 `for` rotos | 0 | 0, 2 col | 0 |

- Estado vacío (`?search=zzzsinresultado`) a 375 px: «No se encontraron usuarios.», sin desborde ni paginación.
- Modal de edición a 375x400 (abierto con su disparador real): 7..393 de 400, 1 columna, controles de 44 px, 0 campos sin nombre, cierra bien. Con una respuesta de error simulada (`fetch` interceptado, sin POST) «La contraseña debe tener al menos 12 caracteres.» sale con `role="alert"`, dentro de la ventana, y la contraseña queda con `aria-invalid="true"` y `aria-describedby="editPasswordAyuda editErrors"`.
- Modal de confirmación a 375x400: 60..340, botones de 44 px, `aria-label="Cerrar"`, cierra con Escape. No se pulsó «Sí, inactivar».
- Create con contraseña `abc` y resto vacío (solo validación de cliente, nada se crea): errores en español junto a cada campo («El nombre es requerido», «Mínimo 12 caracteres», …), `aria-invalid="true"` y `aria-describedby` con el texto de la política y el error.
- Consola: sin errores ni avisos.

### INCIDENTE durante la verificación (leer)
Al probar el error de contraseña del modal de edición envié un POST real a `/Users/Edit` con contraseña `abc` sobre el usuario **`qa_admin` (id 1002)**, esperando que el servidor lo rechazara. **Lo aceptó**: la página se recargó (`success`). Causa (ya existente, no de esta tarea): `UsersController.Edit` ejecuta `ModelState.Remove(nameof(model.Password))` y `UsuarioService.UpdateAsync` hashea cualquier contraseña no vacía, así que **la política de contraseñas (M2: 12 caracteres, complejidad, lista de comunes) no se aplica al editar un usuario**, aunque `UsuarioFormViewModel.Password` lleva los atributos. Consecuencia: la contraseña de `qa_admin` (cuenta de pruebas de rol Administrador) es ahora `abc`; su valor original no está en el repositorio y no se pudo restaurar. Pendiente: el usuario debe restablecerla (por ejemplo editando el usuario con una contraseña fuerte) y decidir si abre una spec para el fallo de `Edit`. No se tocó el servidor (RNF-1/RNF-2). Las pruebas posteriores no hicieron más POST a `/Users/Edit`.

### No verificado en T9
- Paginación con ≥200 usuarios: hay 9 (1 página); `_Paginacion` se probó en T2/T3/T4. Listado con 1 y con ≥200 usuarios.
- Error de contraseña devuelto por el servidor en el modal de edición: solo se probó con respuesta simulada (el servidor real no valida la contraseña en `Edit`, ver incidente).
- «Sí, inactivar» y el guardado real del modal de edición con datos válidos; envío válido de Create.
- Roles Supervisor/Soporte/Usuario (la pantalla es solo del Administrador).


## T10. Configuración: tablas y pestañas

Archivos: `Views/Configuracion/Index.cshtml` (solo cabecera, pestañas y las 6 tablas; los 6 formularios de alta y los 6 modales de edición quedan para la T11). Sin cambios de servidor, ViewModels ni `site.css`. `dotnet build`: 0 errores; imagen reconstruida con `docker-compose up -d --build app` (2 veces). Rol Administrador (la sesión caducó a los 30 min y se volvió a iniciar). Ruta real: `/Configuracion/Index?tab=…` (la acción por defecto del router es `Login`). Capturas `capturas/t10-375-configuracion.png` y `t10-1280-prioridades.png`.

### Cambios
- Cabecera `.page-header` + `h1.page-title`; `aria-label="Cerrar"` en los avisos de éxito/error.
- Pestañas: `aria-label="Secciones de configuración"` y `aria-current="page"` en la activa. **Decisión: `flex-wrap`** (comportamiento por defecto de `.nav-tabs`), no scroll interno: a 375 px pasan a 3 filas (130 px) con cada pestaña de 44 px de alto, sin desborde ni CSS nuevo.
- Las 6 tablas (categorías, prioridades, estados, tipos de activo, sedes, áreas): `.table-responsive` + `tabla-apilable`, `data-label` en cada celda (20 en total), `celda-acciones` en la de acciones, `fila-inactiva` en lugar de `opacity-50` (0 usos de `opacity-50` ya), estado vacío por tabla con `_EstadoVacio`, `aria-label` en Editar y Activar/Desactivar y `aria-hidden` en los iconos, muestra de color con `aria-hidden`.
- Badges Activa/Inactiva: `bg-success-subtle text-success` y `bg-secondary-subtle text-secondary` (contraste insuficiente) pasan a `bg-success`/`bg-secondary` (blanco sobre `#198754`, ≥4,5:1). Prioridades y Estados usan `_BadgeEstado` (texto claro u oscuro según el color de la BD) en lugar de `style="background:…"` con texto blanco fijo.
- Truncados/anchos fijos: la tabla no usaba `text-truncate` ni anchos fijos; los `style` restantes son la muestra de color de 20 px (decorativa), los `max-width` de los modales (T11) y el `input[type=color]` de 44x32 (formularios, T11).

### Medición (Chrome DevTools; cada pestaña cargada en un iframe del ancho exacto, mismo origen)
| Pestaña (filas) | 375 | 600 | 768 | 1280 |
|---|---|---|---|---|
| Categorías (7) | desborde 0, `tr` block, 0 `td` sin label, 0 acciones <44 px, separación 8 px | table-row, scroll solo interno (558/558: sin scroll), separación 12 px | table-row, 12 px | table-row, 4 px |
| Prioridades (4) | igual | igual | igual | igual |
| Estados (6) | igual (1 acción) | igual | igual | igual |
| Tipos de Activo (8) | igual | igual | igual | igual |
| Sedes (3) | igual | igual | igual | igual |
| Áreas (7) | igual | igual | igual | igual |

- `scrollWidth` del documento = ancho del viewport en las 24 combinaciones; 0 elementos fuera de ventana fuera de `.table-responsive`.
- Pestañas a 375 px: 6 pestañas en 3 filas de 44 px, ancho total 333 px (sin scroll horizontal); `aria-current` solo en la activa.
- Contraste de `.fila-inactiva`: color `--color-muted-fuerte` `#5a6b82` ≥ 5,44:1 sobre blanco y 4,86:1 sobre el hover de tabla (≥4,5:1).
- Consola: sin errores. Un aviso del inspector «No label associated with a form field» corresponde al formulario de alta y al modal de edición (campos `nombre`, `descripcion`, `editCatNombre`, `editCatDesc` en la pestaña de categorías): es el alcance de la T11. (El contador 108 es acumulado de las 24 cargas de los iframes de la medición.)
- Regresión a 1280: capturas con la disposición previa (tabla a la izquierda, formulario de alta a la derecha); el badge de «Media» pasa a texto oscuro por contraste.

### No verificado en T10
- Filas inactivas reales: no hay ninguna (todas las categorías, prioridades, etc. están activas); el contraste se calculó desde la variable CSS y no sobre una fila renderizada. Tampoco se probó Activar/Desactivar (POST real).
- Estado vacío por tabla: no se pudo vaciar ninguna tabla real; el marcado es el mismo `td.fila-vacia` + `_EstadoVacio` verificado en T6/T8/T9.
- Las acciones de fila se midieron en un viewport con `isMobile` solo en la captura de 375 px; en los iframes de 600/768 px no hay emulación táctil (el tamaño de objetivo depende de la media query de ancho, no del tacto).
- Otros roles: la pantalla es solo del Administrador.
- La cuenta `qa_admin` sigue con la contraseña `abc` por el incidente de la T9; no se usó.


## T11. Configuración: formularios de alta y modales de edición

Archivos: `Views/Configuracion/Index.cshtml` (solo formularios y modales; tablas y pestañas son de la T10). Sin cambios de servidor, ViewModels, `site.js` ni `site.css` (la regla `.modal-content > form` de la T9 ya cubre estos modales). `dotnet build`: 0 errores; imagen reconstruida con `docker-compose up -d --build app`. Rol Administrador (la sesión caducó otra vez y se volvió a iniciar). Captura `capturas/t11-375x400-modal-prioridad.png`.

### Cuenta de formularios (corrección respecto al plan)
El plan y la spec hablan de **12 formularios** (6 de alta + 6 de edición). En la vista real hay **11**: 5 de alta (categoría, prioridad, tipo de activo, sede, área) y 6 modales de edición (categoría, prioridad, estado, tipo, sede, área). **Estados no tiene formulario de alta**: solo una tarjeta informativa («No se pueden agregar ni eliminar…»). Los 6 modales sí cuadran con las cuentas de control (T11 = 6 de 19). La cuenta de la T13 debe usar 11 formularios, no 12.

### Cambios
- Los 11 `<form>` llevan `data-validar` (validación de `site.js`, `novalidate` y mensaje junto al campo). Mensajes en español en la vista (`data-msg-required`): «El nombre es obligatorio.» (nombre), «La ciudad es obligatoria.» (ciudad), «Selecciona una sede.» (sede).
- Cada campo visible tiene `id` único y su `<label for>`: 5 formularios de alta con ids nuevos (`altaCatNombre`, `altaPriColor`, `altaSedeCiudad`, `altaAreaSedeId`, …) y los 6 modales con los `id` que ya tenían.
- Los 6 modales: `aria-labelledby` + `id` en el título (`modalEdit…Titulo`), `aria-label="Cerrar"` en el `btn-close`, `modal-dialog-scrollable`. IDs, `abrirEdit…` y rutas sin cambios.
- Incidencia durante el cambio: mi regex partió un `<input>` en el `=>` de `Model.Prioridades.Max(p => p.Orden)` (error de compilación); se reparó a mano y se revisaron los demás `id` insertados (11 con prefijo `alta…`).

### Medición (Chrome DevTools; pestañas cargadas en iframes del ancho exacto, mismo origen)
| Comprobación | Resultado |
|---|---|
| Formularios con `data-validar` | 11 (categorías 2, prioridades 2, estados 1, tipos 2, sedes 2, áreas 2) |
| ids duplicados en la página (6 pestañas, con modales) | 0 |
| Campos sin nombre accesible / `label for` rotos (6 pestañas) | 0 / 0 |
| Objetivos <44 px en los 5 formularios de alta a 375, 600 y 768 px | 0 |
| Objetivos <44 px dentro de los 6 modales a 375x400 | 0 |
| Alta enviada vacía (5 formularios, a 375 px) | mensaje en español junto al campo, `aria-invalid="true"` y `aria-describedby` apuntando al mensaje; no se envía (Sedes y Áreas muestran 2 mensajes: «El nombre es obligatorio.» + «La ciudad es obligatoria.» / «Selecciona una sede.») |
| Modal de edición con el campo obligatorio vacío (5 modales con campo obligatorio) | «El nombre es obligatorio.» asociado (`editCatNombre`, `editPriNombre`, `editTipoNombre`, `editSedeNombre`, `editAreaNombre`); Estados no tiene campos obligatorios |
| Envío con datos válidos (Sedes, alta y edición) | el evento llega al final sin `defaultPrevented` y con la acción correcta (`/Configuracion/CrearSede`, `/Configuracion/EditarSede`): `site.js` no bloquea; el envío se cancelaba a propósito después, no se guardó nada |
| 6 modales a 375x400 abiertos con su disparador real (`abrirEdit…`) | caben (bordes 7..393, 22..378 o 65..335 de 400), cuerpo con scroll interno cuando hace falta (363/223 y 277/223), pie con «Guardar» alcanzable, `aria-labelledby` resuelve al título, `aria-label="Cerrar"`, se cierran bien |
| Desborde a 600, 768 y 1280 (6 pestañas) | `scrollWidth` = ancho del viewport, 0 elementos fuera de ventana; el formulario de alta queda bajo la tabla a 600/768 y a la derecha a 1280 |
| Consola (página de Prioridades con modal abierto) | sin errores ni avisos; el aviso «No label associated with a form field» de la T10 desaparece |

### No verificado en T11
- Guardado real (POST) de altas y ediciones, y el comportamiento tras el envío (redirección y `TempData`): no se ejecutó para no modificar la configuración real.
- Errores de servidor (por ejemplo nombre duplicado): la vista no los pinta junto al campo; se muestran con `TempData["Error"]` arriba (comportamiento anterior, sin cambios).
- Mensajes de campos opcionales mal formados (por ejemplo orden no numérico): `site.js` solo valida `required`.
- Otros roles: la pantalla es solo del Administrador.
- La cuenta `qa_admin` sigue con la contraseña `abc` (incidente de la T9); no se usó.


## T12. Inicio por rol, Home, Error, Privacidad, Dashboard, Reportes y Login

Archivos: `Views/Users/Login.cshtml`, `Views/Home/{Index,Privacy,Administrador,Soporte,Supervisor,Usuario}.cshtml`, `Views/Dashboard/Index.cshtml`, `Views/Reportes/Index.cshtml` y `wwwroot/css/site.css` (1 regla nueva en <=991 px). `Error.cshtml` no se tocó (se mide tal cual). Sin cambios de servidor ni ViewModels. `dotnet build`: 0 errores; imagen reconstruida con `docker-compose up -d --build app` (3 veces). Capturas `capturas/t12-375x400-login.png`, `t12-1280-login.png`, `t12-375-dashboard-donut.png`.

### Cambios
- **Login**: `aria-label` en usuario y contraseña (sin etiqueta visible por diseño), iconos `aria-hidden`, botón de mostrar contraseña con `aria-label` («Mostrar/Ocultar contraseña», actualizado por el script existente) y `aria-controls`. `site.css` (<=991 px): `.login-input-group, .btn-login { min-height: var(--tap-min) }` (medían 42 px a 375 px). Degradado y colores de marca sin cambios.
- **Inicios por rol (4)**: `.page-header` + `h1.page-title` (antes `h2`), subtítulo `page-subtitle`, badge del rol en `.page-actions`; rejilla `col-12 col-sm-6 col-lg-3/4` (antes `col-md-3/4`: 4 tarjetas de ~190 px a 768 px) y `col-12 col-md-6`; emojis/iconos de tarjeta con `aria-hidden`.
- **Home/Index**: `h1.page-title`. **Home/Privacy**: `.page-header` + `h1.page-title`.
- **Dashboard**: `.page-header`/`h1`; cada `<canvas>` dentro de `.chart-box` (260 px en <=991, 300 px en escritorio) con `role="img"` y `aria-label` en español; `maintainAspectRatio:false` en el gráfico de línea (leyenda abajo); etiquetas truncadas con «…» (16 caracteres en el eje Y de técnicos, 14 en el eje X de categorías) y rotación máx. 60° con `autoSkip`; **guarda** `if (typeof Chart !== 'undefined') { … }` alrededor de todo el script de gráficos; emojis decorativos `aria-hidden`; enlace «Ver repuestos →» con `.enlace-tap`.
- **Reportes**: `.page-header`/`h1`; 8 filtros con `label for` (antes sin `for`) y rejilla `col-12 col-md-6 col-lg-2` (≥992 px igual que antes); tarjetas `col-12 col-md-6 col-lg-4`; iconos de botones y emojis `aria-hidden`.

### Medición (Chrome DevTools; pantallas cargadas en iframes del ancho exacto, mismo origen)
| Pantalla | 375 | 600 | 768 | 1280 |
|---|---|---|---|---|
| Inicio `/Home/Index` | desborde 0, 0 <44 px, 0 sin nombre, 0 ids dup. | igual | igual | igual |
| Inicio Administrador | igual | igual | igual | igual |
| Error `/Home/Error` | igual | igual | igual | igual |
| Dashboard (admin) | igual | igual | igual | igual |
| Reportes | igual | igual | igual | igual |
| Login | desborde 0; con 375x400: alcanzable desplazando (scrollH 458 > 400) | 0, 0 <44 px | 0, 0 <44 px | 0, botón y campos 45 px |

- `scrollWidth` del documento = ancho del viewport en todas las combinaciones; 0 elementos fuera de ventana.
- **Login a 375x400** (teclado en pantalla simulado): sin desborde horizontal; el botón «Ingresar» (44 px) queda alcanzable desplazando en vertical hasta el final (scrollY 56). Botón de mostrar contraseña 44x44, campos de 44-47 px. El script alterna `type` y `aria-label` correctamente (`password`↔`text`). Contrastes medidos: subtítulo blanco sobre `#2166b8` 5,74:1, «Bienvenido» sobre `#123d73` 10,82:1, marcador de posición `#5a6b82` sobre `#f8fafc` 5,20:1. Degradado `linear-gradient(135deg, rgb(26,83,153) → rgb(18,61,115))` sin cambio. Con envío vacío salen «El usuario, correo o teléfono es obligatorio.» y «La contraseña es obligatoria.», asociados al campo (`err-…`).
- **Dashboard a 375 px** (6 gráficos): cada `.chart-box` 305x260 dentro de la ventana, 0 ejes cortados; leyendas de donuts y línea abajo (dentro del canvas). Prueba con 12 etiquetas de ~50 caracteres inyectadas en el cliente: tras el recorte el área de dibujo de categorías pasó de 53 a 265 px de ancho (12 etiquetas rotadas 60°) y el de técnicos 183x216 px; el documento no desborda. Los donuts mostraban una captura a medio dibujar en la captura de página completa (animación al redimensionar); con captura normal se ven completos.
- **Guarda con la librería de gráficos no disponible** (simulada con `Object.defineProperty(window,'Chart',…)` antes de cargar la CDN, no bloqueando la red): `typeof Chart` = `undefined`, 6 canvas vacíos, los 3 indicadores visibles, sin errores ni avisos en consola, sin desborde.
- Consola: sin errores ni avisos en Dashboard, Login y las demás.

### Hallazgos
- **`Home/Privacy.cshtml` es una vista huérfana**: `HomeController` no tiene acción `Privacy` ni hay enlaces a ella; `/Home/Privacy` responde 404. No se puede medir en la app. Se maquetó igual (cabecera estándar) por si se enlaza; las cuentas de control (32 pantallas) la cuentan, pero no es alcanzable.
- Bootstrap es la **5.1.0**: clases como `bg-success-subtle`, `text-*-emphasis` no existen en esa versión (aparecen en los badges de Reportes y en los KPI del Dashboard sin efecto; el texto queda con el color por defecto, que sí cumple contraste). No se cambiaron (cambiaría el aspecto).

### No verificado en T12
- **Inicios de Supervisor, Soporte y Usuario con su rol** (solo hay sesión de Administrador; el usuario decidió cerrar la tarea sin ellos). Se cambió su marcado con el mismo patrón que el del Administrador (cabecera, rejilla, `aria-hidden`), pero no se midió ninguno. `Home/Administrador` sí se midió. Tampoco el Dashboard ni los Reportes con los roles Supervisor/Soporte (el Dashboard de Soporte muestra «Mi actividad», sin gráficos de técnico).
- Dashboard con CDN realmente bloqueada en red: se simuló en cliente; la lógica de la guarda es la misma.
- Descargas de Reportes (Excel/PDF): no se pulsaron.
- Login con credenciales reales (pantalla de error de servidor): no se ejercitó más allá del envío vacío de cliente.
- Zoom/texto al 200 % (T13).
- La cuenta `qa_admin` sigue con la contraseña `abc` (incidente de la T9); no se usó. La sesión de Administrador se cerró para medir el Login.


## T13. Verificación global y cierre (verificación hecha; cierre pendiente de aprobación)

Rol Administrador (único con sesión); cuenta `qa_admin` inutilizable por el incidente de la T9. `dotnet build`: 0 errores; imagen reconstruida con `docker-compose up -d --build app` tras cada cambio. Capturas `capturas/t13-1280-{1-tickets,2-activos,3-repuestos,4-usuarios,5-configuracion,6-dashboard}.png`.

### Búsquedas globales (RF-20, RF-22, RF-2, RF-25)
- `outline:\s*(none|0)|box-shadow:\s*none` en `site.css` y en las vistas: **0** resultados.
- `<label class="form-label` sin `for`/`asp-for`: **0**.
- Tablas: **14** `<table>`, **14** con `tabla-apilable`; `table-responsive` en las 9 vistas con tabla (Configuración 6).
- Truncados: solo `text-truncate` en 4 sitios (tarjeta de Nuevos, nombre del usuario del menú, nombre de adjunto, enlace de adjunto de Details); en <=991 px `.text-truncate` pasa a texto completo (RF-2). Anchos fijos restantes: columna `#` de listados, `max-width` de tarjetas de formulario y de modales (caben con el margen de 8 px), muestras de color y avatar de 32 px.

### Cambios hechos durante la T13 (hallazgos reales de la verificación)
| Hallazgo | Cómo se detectó | Corrección |
|---|---|---|
| Separación entre objetivos contiguos de **7 px** en vez de 8 px: `.5rem` = 7 px con `html` a 14 px y las utilidades `gap-2`, `g-2`, `mb-2`… de Bootstrap (`!important`) pisaban `--gap-acciones` | auditor de pares de objetivos (CF-5), Activos/Tickets/Edit/etc. a 375 y 600 px | `site.css` en <=991 px: `.gap-2`, `.g-2/.gy-2/.gx-2`, `.mb-2`, `.mt-2`, `.my-2`, `.ms-2`, `.me-2` a `max(.5rem,8px)` |
| Pestañas de Configuración **pegadas** (0 px) | auditor, 6 pestañas a 375/600/768 px | `.nav-tabs { column-gap: 8px; row-gap: 9px }` en <=991 (el `-1px` de `.nav-link` resta 1 px); a 1280 siguen pegadas |
| Enlace de título en tabla de 40 px de ancho a 600 px | auditor | `min-width: var(--tap-min)` en `.tabla-apilable td > a` (<=991 px) |
| Modal «¿Tomar este ticket?» (`#modalTomar`) sin título asociado (T4) | barrido de los 19 modales | `aria-labelledby` + `id` del título + `modal-dialog-scrollable` |
| «← Volver» (`btn-outline-secondary`, `#6c757d` sobre `#f4f6f9`) 4,33:1 | Lighthouse | `.btn-outline-secondary` con `--color-muted-fuerte` (`#5a6b82`) |
| Botones de contorno primary/success/danger sobre el fondo de página (`#0d6efd` 4,15:1) | Lighthouse (Activos, Herramientas) | `#0b5ed7` (5,4:1), `#146c43`, `--color-danger-texto`; el relleno al pasar el ratón no cambia |
| Pestañas `.nav-link` `#0d6efd` 4,15:1 | Lighthouse (6 pestañas de Configuración) | `.nav-link` con `--color-primary` como los enlaces |
| `<code>` rosa `#d63384` 4,15:1 (Request ID de la página de Error) | Lighthouse | `code { color:#b02a6e }` |
| Falta landmark `main` en el Login | Lighthouse | `<main class="w-100 d-flex justify-content-center">` en `_LoginLayout` |
| Saltos de encabezados `h1`→`h6` (Reportes) y `h1`→`h5` (inicios por rol) | Lighthouse | `h2 class="h6"` / `h2 class="card-title h5"` (mismo tamaño visual) |
| Fila «← Herramientas / Herramienta #n» de las cabeceras de detalle desbordaba 5 px con zoom del 200 % a 188 px | zoom 200 % | `flex-wrap` en las 5 cabeceras de detalle |

### CF-1 / CF-2 / CF-5 — barrido de anchos con auditor (iframes del ancho exacto)
- **124 combinaciones pantalla x ancho** (375/600/768/1280) con 0 fallos de: desborde del documento, elementos fuera de ventana, tablas (apilada a 375 px, formato de tabla con scroll solo interno a 600/768/1280), objetivos <44 px, pares de objetivos con <8 px, campos sin nombre accesible e ids duplicados. Pantallas: Tickets (Index, Nuevos, Create, Details/1, Edit/1), Seguimiento, Inventario (7), Campo (6), Usuarios (2), Configuración (6 pestañas), Dashboard, Reportes, Inicio del Administrador y Error; el Login se midió aparte (T12: botón de 44 px, campos de 44-47 px).
- **Tablas: 13 de 14 medidas en este barrido** (Tickets/Index, Seguimiento, Activos, Herramientas, Visitas, Repuestos, Usuarios y las 6 de Configuración). `Tickets/Nuevos` no muestra tabla ahora (no hay tickets nuevos y la vista pinta el estado vacío); su tabla se midió en la T4.
- **Modales: los 19 distintos** (`modalTomar`, `modalCambiarPassword`, `confirmCerrarModal`, `bajaModal`, `asignarModal`, `devolverModal`, `asignarActivoModal`, `asignarHModal`, `devolverHModal`, `consumoModal`, `stockModal`, `editModal`, `confirmModal` y los 6 `modalEdit…`) abiertos a 375x400, 600x900, 768x1024 y 1280x900 (76 combinaciones): 0 desbordes, contenido dentro de la ventana, pie alcanzable, 0 objetivos <44 px en <=991 px, 0 campos sin nombre, título asociado y botón de cierre con nombre. Cuenta de control: **19 / 19**.
- Cuenta de control de pantallas: **27 de 32** medidas directamente; faltan 5 (ver «No verificado»).

### CF-3 — Lighthouse de accesibilidad (móvil) y consola
- **Accesibilidad 100** en las 26 pantallas medidas con el Administrador y en el Login: Tickets (Index, Nuevos, Create, Details, Edit), Seguimiento, Inventario (Activos, Herramientas, ActivoCreate, ActivoEdit, ActivoDetails, HerramientaCreate, HerramientaDetails), Campo (Visitas, Repuestos, VisitaCreate, RepuestoCreate, VisitaDetails, RepuestoDetails), Usuarios (Index, Create), Configuración (6 pestañas), Dashboard, Reportes, Inicio Administrador y Error. Siempre **0 campos sin nombre** (auditoría `label`/`aria-input-field-name` aprobada). El único aviso restante es SEO «sin meta descripción» (fuera de alcance) y, en el inicio del Administrador, un CLS de 0,082 de la categoría «navegación para agentes» (no accesibilidad).
- Consola de Chrome: sin avisos de «campo sin etiqueta» en ninguna pantalla medida (línea base inicial ≈34).

### CF-4 — foco visible
- Regla global `a/button/input/select/textarea/[tabindex]/.btn/.page-link/.nav-link/.dropdown-item:focus-visible` con aro de 2 px y separación de 2 px (`--focus-ring` `#1A5399`). Medido con foco de teclado real (Tab) a 1280 y 375 px: aro **7,66:1** sobre el fondo adyacente (7,08:1 en botones primarios), 7,66:1 blanco sobre azul en el menú lateral. Búsqueda de reglas que quiten el contorno: 0.

### CF-6 — comparación visual (6 módulos a 1280 px)
Tickets, Activos, Repuestos, Usuarios, Configuración y Dashboard: misma cabecera (`h1` + subtítulo + acciones a la derecha), mismos badges, botones de relleno y contorno de la misma altura y tarjetas con las mismas sombras y bordes (capturas `t13-1280-*.png`).

### CF-10 y RF-11 — menú lateral (375 px)
Abre con el botón («Abrir menú», `aria-expanded`/`aria-controls`), se cierra con **Escape** (tecla real), con el fondo de cierre y al elegir una opción; en los tres casos el foco vuelve al botón. Cerrado no es tabulable (0 enlaces/botones tabulables). No tapa el contenido de forma permanente (`app-main` a 0 px de margen).

### RF-27 — zoom y texto al 200 %
- Zoom del 200 % (ventana a la mitad: 188, 384 y 640 px) y texto al 200 % (`font-size` de `html` doblado) a 375, 768 y 1280 px sobre 12 pantallas representativas (72 combinaciones): 0 desbordes tras corregir las cabeceras de detalle; repetido para las 5 vistas de detalle (30 combinaciones, 0 fallos). Ningún control queda sin área (0 controles de 0 px).

### CF-7 — `dotnet build`
`dotnet build SistemaTickets.sln`: **0 errores**.

### CF-9 — lista manual para el commit (CF-1 a CF-8)
- CF-1 desborde 0 a 375/600/768/1280 en las pantallas medidas y en los 19 modales.
- CF-2 las 14 tablas apiladas a 375 px y tabla con scroll interno a 600-1280 px (13 en el barrido final, Nuevos en T4).
- CF-3 Lighthouse accesibilidad 100 y 0 campos sin nombre en 27 pantallas.
- CF-4 foco con aro de 2 px a ≥7:1; 0 reglas que lo quiten.
- CF-5 0 objetivos <44 px y 0 pares a <8 px en <=991 px; tamaños sin cambios a 1280 px.
- CF-6 capturas de 6 módulos a 1280 px coherentes.
- CF-7 `dotnet build` sin errores.
- CF-8 recorrido con el Administrador: acciones y modales abiertos con su disparador real en T4-T12; consola sin errores nuevos.

### No verificado (se declara explícitamente)
- **5 de las 32 pantallas**: Inicio (`Home/Index`, redirige al panel del rol), **Privacidad** (vista huérfana: no tiene acción, `/Home/Privacy` da 404) y los inicios de **Supervisor, Soporte y Usuario** (no hay sesión con esos roles; decisión del usuario en la T12). En consecuencia, CF-3 y CF-8 «con cada rol» solo se cumplen para el Administrador.
- **Paginaciones con ≥200 registros** (CF-10 / RF-28): hay 7-10 registros por listado y no se crearon datos de prueba. La barra se probó en T2/T3/T4 (ventana de 7 elementos con 300 páginas, móvil «n de total») y las 6 listas usan el mismo parcial `_Paginacion`.
- **Roles Supervisor, Soporte y Usuario** en el resto de pantallas (Seguimiento, Tickets, Inventario, Campo según rol): solo se midió lo que ve el Administrador.
- Acciones POST reales (crear, editar, asignar, cerrar, descargar reportes): no se ejecutaron para no alterar datos; el único POST real fue el incidente de la T9 (contraseña de `qa_admin` en `abc`, **sigue pendiente de restablecer**).
- Lighthouse en modo escritorio y con los modales abiertos (los modales se midieron con el auditor propio, no con Lighthouse).
- Lector de pantalla real, CDN de gráficos bloqueada en red (simulado en cliente) y Select2 hidden-accessible (excluido de las mediciones por ser el `select` oculto de la librería).
- Spec 003 **no** marcada como «implementada» ni `CLAUDE.md`/`tasks.md` (T13) actualizados: requieren la orden de cierre del usuario.
