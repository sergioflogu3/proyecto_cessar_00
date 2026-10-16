# Plan 003 — Vista y responsive de todo el proyecto

Estado: aprobado (decisiones D1..D12 aceptadas; D5, D8 y D10 recogidas en la spec ajustada el 15/10/2026; D12 aplicada)

Parte de la spec 003 (aprobada). Sin paquetes nuevos, sin `package.json`, sin paso de build de JS (principio 1, RNF-1). Sin cambios en datos, permisos, rutas ni lógica de servidor (RNF-2). Las vistas siguen enlazadas a ViewModels (RNF-4).

## Hallazgos del código actual (base del plan)

- `wwwroot/css/site.css` (393 líneas): un único punto de corte para el menú (`991.98px`) y otro para el login (`640px`); `html{font-size:14px}` fijo en px (no respeta el tamaño de texto del navegador, RF-27); regla `.btn:focus,.form-control:focus{outline:none; box-shadow:...rgba(.25)}` (línea 289) y `.login-input:focus{outline:none;box-shadow:none}` (línea 368): son las dos únicas reglas propias que quitan el contorno (RF-22, CF-4); además Bootstrap trae `outline:0` en `.form-control:focus`, `.btn-close`, etc.
- Colores sospechosos de no llegar a AA: `--color-muted #718096`, placeholder del login `#a0aec0`, `.sidebar-section rgba(255,255,255,.4)`, `.sidebar-role .5`, `.sidebar-user-info .7`, `.login-subtitle .75` sobre el degradado, `badge bg-warning` sin `text-dark`, `bg-*-subtle text-*` (success/primary/secondary), `text-warning`, `text-info`, `btn-outline-warning`, filas `opacity-50` en Configuración.
- Marcado repetido: la cabecera de página es `<div class="d-flex justify-content-between align-items-center mb-4">` con `<h4 class="mb-0 fw-bold">` + `<small class="text-muted">` en todas las pantallas (cada una con variantes: Configuración usa `gap-3`, Detalles usa `flex-wrap ... align-items-start`, Users/Create usa `mb-4 gap-3` sin descripción). Los estados vacíos son `<td colspan class="text-center py-5 text-muted">Texto</td>` sin icono. Las 6 paginaciones (Tickets, Visitas, Repuestos, Activos, Herramientas, Usuarios) pintan **todas** las páginas con `@for (i=1..TotalPaginas)`: con cientos de registros desborda (RF-28).
- Filtros: `row g-2` con `col-md-4/3/2/2/1` (5 columnas desde 768 px): incumple RF-6 en 768-991 px. El botón limpiar de Tickets/Herramientas es `✕` sin nombre accesible.
- Etiquetas: la causa de los ~34 avisos son `<label class="form-label">` **sin `for`** y controles **sin `id`** (filtros, `Configuracion/Index` con 6 formularios + 6 modales, modales de Inventario/Campo, `_Layout` modal de contraseña, `Login` solo con placeholder, interruptores/casillas). Los campos con `asp-for` ya generan `for`/`id`.
- Modales: 19 modales de Bootstrap con `modal-dialog-centered`, sin `modal-dialog-scrollable`; `btn-close` sin `aria-label`.
- Menú lateral (`_Layout`): el botón tiene `aria-label="Toggle sidebar"` (inglés), sin `aria-expanded`; sin Escape; no cierra al elegir opción; el menú cerrado sigue siendo tabulable fuera de pantalla; `.sidebar-username` usa `text-truncate`; `.sidebar-link` usa `white-space:nowrap`.
- Dashboard: Chart.js 4.4.4 desde CDN; `chartMes` sin `maintainAspectRatio:false` (se aplasta en 375 px); etiquetas largas sin rotación/truncado; si falla el CDN, `new Chart` lanza `ReferenceError`.
- Discrepancia menor con la spec: la paginación de Visitas es Razor del servidor (no generada por script). RF-28 se aplica igual; propongo corregir el texto de «Casos límite» en un cambio menor de spec (ver decisión D12).

## Archivos y responsabilidades

### Base común (fase 1)

| Archivo | Responsabilidad | RF |
|---|---|---|
| `SistemaTickets/wwwroot/css/site.css` | Tokens (espaciado, tamaños de texto, foco, grises AA, `--tap-min:44px`), `html{font-size}` en %, reglas globales de foco `:focus-visible`, cabecera de página (`.page-header`, `.page-title`, `.page-subtitle`, `.page-actions`), tarjetas/tablas/filtros/formularios uniformes, badges, estado vacío, jerarquía de botones, tabla apilada (`.tabla-apilable`), plegado (`.td-secundario`, `.btn-plegar`), paginación móvil, modales en móvil, objetivos de 44 px en ≤991 px, ajuste de colores AA, `.chart-box`, menú lateral (visibilidad, ancho máximo, envoltura de texto) | RF-2, 3, 4, 5, 6, 7, 8, 9, 13..19, 21..24, 27, 28 |
| `SistemaTickets/wwwroot/js/site.js` (hoy vacío) | (a) plegado de filas apiladas (delegado, `aria-expanded`); (b) accesibilidad de validación: `id` en los `span[data-valmsg-for]`, `aria-describedby`/`aria-invalid` en el campo; (c) validación mínima de formularios sin ViewModel (`form[data-validar]`, mensajes en español desde `data-msg-*`). Sin librerías | RF-5, 25, 26 |
| `Views/Shared/_Layout.cshtml` | Botón de menú (`aria-label` en español, `aria-expanded`, `aria-controls`); script del menú: Escape, cierre al elegir opción, cierre al pasar a ≥992 px (rotación), retorno de foco al botón; `for`/`id` en las 3 contraseñas del modal; `aria-label="Cerrar"` en `btn-close`; `modal-dialog-scrollable` | RF-9, 11, 20, 24 |
| `Views/Shared/_LoginLayout.cshtml` | Cargar `site.js` (validación accesible); sin cambios de marca | RF-12, 26 |
| `Views/Shared/_EstadoVacio.cshtml` (nuevo parcial) | Icono + mensaje + acción opcional; se usa dentro de `<td colspan>` y fuera de tablas | RF-16 |
| `Views/Shared/_Paginacion.cshtml` (nuevo parcial) | Barra con ventana de páginas con elipsis; recibe `(int actual, int total, Func<int,string> url)`; sustituye las 6 copias | RF-28, 24 |
| `Views/Shared/_BadgeEstado.cshtml` (nuevo parcial) | Badge uniforme para estados con color de BD (`Estado.Color`/`Prioridad.Color`): elige texto blanco o negro para llegar a 4,5:1 (función pura, ver siguiente sección) | RF-15, 23 |

Todo lo demás son vistas Razor existentes, agrupadas por módulo (fases 2 a 6).

### Reparto por módulos (orden por fases)

| Fase | Pantallas (del inventario de 32) | Archivos de vista | Qué cambia (resumen) | RF |
|---|---|---|---|---|
| 2. Tickets y Seguimiento (6) | listado, nuevos, crear, editar, detalle, seguimiento | `Tickets/{Index,Nuevos,Create,Edit,Details}`, `Supervisor/Seguimiento` | cabecera, filtros, tabla apilada (3 tablas), modal «tomar» y «confirmar cierre», panel lateral bajo el contenido, lista de adjuntos generada por script | RF-1..9, 10, 13..20, 24..28 |
| 3. Inventario (7) | activos (4), herramientas (3) | `Inventario/*` (7 vistas) | tablas Activos/Herramientas (la fila de herramientas con 4 acciones), 5 modales, formularios | ídem |
| 4. Campo (6) | visitas (3), repuestos (3) | `Campo/*` (6 vistas) | tablas Visitas/Repuestos, modal de ajuste de stock y de consumo de repuestos (select cargado por script) | ídem |
| 5. Usuarios y Configuración (3) | usuarios (listado, crear), configuración | `Users/{Index,Create}`, `Configuracion/Index` | 1 tabla + 2 modales; 6 tablas + 6 formularios de alta + 6 modales de edición; pestañas | ídem |
| 6. Inicio, Dashboard, Reportes y Login (10) | Login, Inicio, Error, Privacidad, 4 inicios por rol, Dashboard, Reportes | `Users/Login`, `Home/*` (6), `Shared/Error`, `Dashboard/Index`, `Reportes/Index` | tarjetas de inicio, KPIs, gráficos (configuración), filtros de Reportes, login | RF-1, 2, 10, 12, 13..23 |

Total de pantallas: 6 + 7 + 6 + 3 + 10 = 32 (la fase 6 reúne Login, Inicio, Error, Privacidad, los 4 inicios por rol, Dashboard y Reportes = 10).

## Patrón de tabla apilada (RF-3, RF-4, RF-5, RF-25)

Técnica elegida: **CSS puro con `data-label` en cada `<td>`** y una clase `tabla-apilable` en la tabla; sin duplicar el marcado.

```
<table class="table tabla-apilable align-middle mb-0">
  <thead class="table-light"> ... </thead>           <!-- sin cambios -->
  <tbody>
    <tr>
      <td data-label="#">...</td>
      <td data-label="Título">...</td>
      <td class="td-secundario" data-label="Creado por">...</td>
      <td class="celda-acciones"> <button class="btn-plegar"...> + botones de acción </td>
```

CSS (resumen, solo en `max-width: 575.98px`):
- `.tabla-apilable thead` se oculta **visualmente** (patrón visually-hidden: `position:absolute; clip`), no con `display:none`, para conservar las cabeceras para lectores de pantalla (RF-25).
- `tr` pasa a `display:block` (tarjeta: borde, radio, sombra y separación de la escala común, RF-18); `td` a `display:flex; justify-content:space-between; gap` con `td::before{content:attr(data-label); font-weight:600; color:var(--color-muted-fuerte)}`; valor con `overflow-wrap:anywhere` y alineado a la derecha/izquierda según anchura.
- `td.celda-acciones` ocupa toda la fila (`flex-wrap`, `gap:.5rem`), sin `::before`; las acciones conservan su marcado y condiciones `@if` (RF-5, spec 002 intacta).
- Fila vacía (`td colspan`): clase `fila-vacia` sin `::before`.
- El orden del DOM no cambia, así que el orden de lectura (teclado/lector) = orden visual (RF-25). Se verifica en el árbol de accesibilidad de DevTools que Chrome conserva la semántica de tabla con `display:block` (Chrome la conserva desde la v80; RNF-6 limita a Chrome). Si no lo hiciera, plan B: añadir `role="table|rowgroup|row|cell|columnheader"` en las tablas afectadas.
- En 576-991 px y ≥992 px no se aplica nada: la tabla sigue como hoy dentro de `.table-responsive` (desplazamiento solo interno, RF-4). Las seis tablas de `Configuracion` que hoy **no** tienen `.table-responsive` (la tabla va directa en `.card-body p-0`) pasan a llevarlo.

Por qué CSS puro y no otra opción:
- Alternativa descartada: **duplicar el marcado** (tarjetas con `d-sm-none` + tabla con `d-none d-sm-block`). Duplica ~14 tablas, repite `id`/`onclick`/`data-*` (rompe RNF-2: los manejadores como `abrirAsignar(@h.Id)` y ids de modales deben ser únicos), obliga a `aria-hidden` en uno de los dos para que el lector no lea todo dos veces y abre el riesgo de que las dos versiones diverjan (RF-5).
- Alternativa descartada: generar `data-label` con JS leyendo el `<thead>`. Depende de JS y se ve mal hasta que carga; el `data-label` explícito también permite etiquetas distintas del encabezado («Acciones», «#»).

### Plegado de campos > 8 (RF-5)

- Se cuentan campos de datos, no las acciones. Revisión de las 14 tablas: Tickets (admin/supervisor) tiene exactamente 8 datos; Activos, 8; Visitas, 6; Repuestos, Herramientas y Nuevos, ≤ 7; Usuarios, ≤ 8; Configuración, 4 por tabla. **Seguimiento** es una matriz (técnico + una columna por estado + total) y puede superar 8 según los estados configurados. Cada tarea de módulo cuenta las columnas reales en esa pantalla y aplica el plegado solo si hay más de 8.
- Técnica: las celdas excedentes llevan `td-secundario`; en ≤575 px quedan ocultas (`display:none`) salvo que la fila tenga `fila-expandida`. El control es un `<button type="button" class="btn-plegar" aria-expanded="false" aria-controls="...">` dentro de la celda de acciones (o de la última celda si no hay acciones), visible solo en ≤575 px, con texto «Ver más» / «Ver menos» (texto permitido por RNF-2c). Un único manejador delegado en `site.js` alterna `aria-expanded` y la clase; es accionable con Enter/Espacio al ser `<button>`.
- Alternativa descartada: `<details>` dentro de una celda. Una fila `<tr>` no puede envolverse en `<details>` (HTML inválido) y habría que duplicar los datos en escritorio.
- Alternativa descartada: casilla oculta con `:checked` (sin JS). No expone `aria-expanded` (incumple RF-5).

## Utilidades CSS nuevas y breakpoints

Breakpoints (coinciden con los de Bootstrap 5, que ya usa la app): `575.98px` (móvil), `767.98px` (formularios a una columna), `991.98px` (tableta/menú/objetivos táctiles). El login conserva su punto propio de 640 px (apila el tarjetón; no está en el inventario de rangos de la spec y a 600 y 375 px ya queda apilado).

Tokens (`:root`):
- Tamaños de texto, uno por rol (RF-14): `--fs-titulo:1.375rem`, `--fs-subtitulo:.875rem`, `--fs-tabla:.875rem`, `--fs-ayuda:.8125rem`, `--fs-badge:.75rem`. Los `style="font-size:.85rem|.7rem|.75rem"` sueltos y las clases `small` usadas como «texto de ayuda» se reconducen a estos roles. Suelo mínimo de .75rem (hoy hay .65rem en el menú).
- Espaciado: se mantiene la escala de Bootstrap (múltiplos de .25rem): bloque a bloque `mb-4`, relleno de tarjeta `--card-pad:1rem` (≥576 px: `1.25rem`), separación de acciones `.5rem`.
- Foco, grises y `--tap-min:44px`.
- `html{font-size:87.5%}` y `@media(min-width:768px){html{font-size:93.75%}}` (14 px/15 px por defecto, igual que hoy, pero escalable con el texto del navegador; RF-27).

Clases nuevas (todas en `site.css`):
- `.page-header` (flex, `flex-wrap`, `gap`, `margin-bottom` fijo), `.page-title` (se aplica a un `<h1>`; ver D2), `.page-subtitle`, `.page-actions`. En ≤575 px: columna, acciones a ancho completo bajo el título (RF-8), título con `overflow-wrap:anywhere`.
- `.tabla-apilable`, `.td-secundario`, `.btn-plegar`, `.celda-acciones`, `.fila-vacia`, `.fila-inactiva` (sustituye a `opacity-50` de Configuración: la opacidad rebaja el contraste del texto; la fila inactiva se marca con color gris AA + el badge «Inactiva» que ya existe).
- `.estado-vacio` (icono `fa-regular`/`fa-solid` ya disponible, mensaje, acción), `.acciones-form` (en ≤575 px: columna, a ancho completo, `Guardar`/`Buscar` primero con `order`; ver D6), `.chart-box` (altura controlada: `height:260px` en ≤575, 300 px en ≥576, `position:relative`).
- `.badge` unificado: `font-size:var(--fs-badge); font-weight:600; padding:.35em .65em; border-radius:.375rem`.
- `.card{border:0; box-shadow:var(--sombra-card)}` y `.card-header{background:#fff; padding:.75rem var(--card-pad)}` globales (las clases utilitarias `border-0 shadow-sm bg-white border-bottom py-3` que hoy se repiten pasan a ser inertes y se pueden retirar gradualmente).
- Modales (≤575 px): `.modal-dialog{margin:.5rem}`, `.modal-content{max-height:calc(100vh - 1rem); max-height:calc(100dvh - 1rem); display:flex; flex-direction:column}`, `.modal-body{overflow-y:auto; min-height:0}`, `.modal-footer` siempre visible (`flex-shrink:0; flex-wrap:wrap`). Es CSS global: no hay que editar los 19 modales para el desplazamiento. El `style="max-width:420px"` inline de algunos modales no desborda (el diálogo es de ancho automático menos márgenes).
- Texto largo (RF-2): `.app-content{overflow-wrap:break-word}` y, dentro de tarjetas apiladas y `.page-title`, `overflow-wrap:anywhere`. Los `text-truncate`/`white-space:nowrap`/anchos fijos en vistas (hoy 38 apariciones en 19 archivos, 11 en Configuración) se revisan módulo a módulo: se mantienen en ≥992 px y se anulan en ≤991 px (`text-truncate` pasa a `white-space:normal`). En `_Layout`, `.sidebar-username` y `.sidebar-link` pasan a `white-space:normal` en ≤991 px.
- Objetivos de 44 px (RF-24), solo en `max-width:991.98px`: `.btn, .btn-sm, .btn-close, .page-link, .nav-link, .sidebar-toggle-btn, .login-show-btn, .sidebar-link, .sidebar-logout, .dropdown-item, .form-control, .form-select` con `min-height:var(--tap-min)`; botones solo icono con `min-width:var(--tap-min)` y `padding` suficiente; `gap:.5rem` entre acciones de fila (en lugar de los `ms-1` actuales, que son 4 px); casillas e interruptores: el `<label>` asociado es el objetivo (`display:inline-flex; min-height:44px; align-items:center`) con el `<input>` dentro del `.form-check` (ver D5). En ≥992 px no se redefine ningún tamaño.

## Foco visible (RF-21, RF-22, CF-4)

- Se elimina la regla de la línea 289 y se sustituye por una regla global:
  `:where(a,button,input,select,textarea,summary,[tabindex]):focus-visible { outline:2px solid var(--focus-ring); outline-offset:2px; }`
  (más variantes para componentes de Bootstrap que lo redefinen con `box-shadow` o `outline:0`: `.btn:focus-visible`, `.form-control:focus`, `.form-select:focus`, `.form-check-input:focus`, `.btn-close:focus`, `.page-link:focus`, `.nav-link:focus-visible`, `.accordion-button`…; el box-shadow translúcido de Bootstrap se deja o se anula, pero **nunca** como único indicador).
- Fondos distintos, valores distintos de `--focus-ring`:
  - Superficies claras (`#fff`, `#f4f6f9`, `#f8fafc`): `--focus-ring:#1A5399` (color de marca). Contraste calculado contra `#fff`: L(#1A5399)=0,087 → (1,05)/(0,137)=7,7:1; contra `#f4f6f9` (L=0,920): 7,1:1. Con `outline-offset:2px` el borde adyacente es siempre el fondo de la página/tarjeta y no el relleno del botón (un botón relleno azul no se «come» el aro).
  - Menú lateral (fondo `#1A5399`): `.sidebar-link:focus-visible, .sidebar-logout:focus-visible{outline:2px solid #fff; outline-offset:-2px}` (7,7:1 contra el azul; el offset negativo evita que `overflow:hidden` del menú lo recorte).
  - Login: el contorno nulo de `.login-input:focus` se sustituye por `.login-input-group:focus-within{outline:2px solid var(--color-primary); outline-offset:2px}` (7,7:1 frente al fondo blanco del panel) y `.login-show-btn:focus-visible` con el mismo aro. El borde actual de 1,5 px de `#e2e8f0` (≈1,2:1) no cuenta como sustituto.
- Verificación: pulsar Tab en cada pantalla y leer `outline`/`box-shadow` computados (ver script) y calcular el contraste contra el primer fondo opaco ancestro.

## Contraste AA (RF-15, RF-16, RF-23)

Fórmula (WCAG 2.x), usada también en el script de medición:
```
canal(c) = c/255;  lin = c<=0.04045 ? c/12.92 : ((c+0.055)/1.055)^2.4
L = 0.2126*lin(R) + 0.7152*lin(G) + 0.0722*lin(B)
contraste = (max(L1,L2)+0.05) / (min(L1,L2)+0.05)
color con alfa a sobre fondo F:  mezcla = a*C + (1-a)*F  (por canal, antes de calcular L)
umbral: texto normal 4.5; texto grande (>=24px, o >=18.66px en negrita) y iconos informativos 3.0; indicador de foco 3.0
```

Lista concreta de combinaciones a medir (antes y después) y ajuste candidato. Los candidatos son una primera hipótesis calculada a mano; el valor final se fija con la medición real en Chrome y se conserva el matiz (RF-19):

| # | Elemento | Combinación actual | Estimación | Ajuste candidato |
|---|---|---|---|---|
| 1 | `--color-muted` (texto gris de ayuda) | `#718096` sobre `#fff`, `#f4f6f9`, `#f8fafc` | ≈ 4,0:1 (no cumple) | nuevo `--color-muted-fuerte:#5a6b82` (≈ 5,0:1 sobre `#f4f6f9`); `.text-muted` de Bootstrap se mide también sobre `#f4f6f9` y `.table-light` |
| 2 | Placeholder del login | `#a0aec0` sobre `#f8fafc` | ≈ 2,1:1 | `#5a6b82` |
| 3 | Menú: `.sidebar-section` (.4), `.sidebar-role` (.5), `.sidebar-user-info` (.7), `.sidebar-link` inactivo (.78), `.sidebar-logout` (`rgba(255,180,180,.85)`), y sus estados `:hover` y `.active` (el fondo cambia a `rgba(255,255,255,.1/.15)` sobre el azul) | blanco con alfa sobre `#1A5399` | .4 y .5 no cumplen (≈ 2,3 y 3,1:1); .7 ≈ 4,7:1 justo | subir alfas al mínimo que dé ≥ 4,5:1 en todos los estados (esperado ≈ .8 para sección/rol y ≈ .88 para link); subir la sección de .65rem a .75rem |
| 4 | Login: `.login-subtitle` (.75) y blanco sobre el degradado | peor punto = extremo claro `#2166b8` (L=0,133) | .75 ≈ 3,8:1 (no cumple); blanco puro ≈ 5,7:1 (cumple) | alfa ≥ .9 en el subtítulo; matiz del degradado intacto (RF-19). Se mide en ambos extremos del degradado de `.login-left` |
| 5 | Mensajes de error `.text-danger small` | `#dc3545` sobre `#fff` y sobre `#f8fafc` | ≈ 4,5:1 sobre blanco, justo por debajo sobre `#f8fafc` | `.field-validation-error, .text-danger` a `#b02a37` (Bootstrap `danger` oscuro) en mensajes de validación |
| 6 | Badges de BD (`style="background-color:@Color"`) con texto blanco | color arbitrario | depende del color | parcial `_BadgeEstado`: texto blanco o `#212529` según cuál dé más contraste (siempre se alcanza ≥ 4,5:1 con alguno de los dos). No se toca el tono de fondo |
| 7 | `badge bg-warning` (Herramientas «En Reparación», Visitas, KPI) con texto blanco | `#ffc107` / `#fff` | ≈ 1,6:1 | `text-dark` (≈ 10:1); `bg-info` igual |
| 8 | `bg-primary-subtle text-primary`, `bg-success-subtle text-success`, `bg-warning-subtle text-warning`, `bg-secondary-subtle text-secondary` (Dashboard KPI, Configuración «Activa/Inactiva») | texto de color pleno sobre fondo suave | ≈ 3,2-3,5:1 (no cumple 4,5) | usar `text-*-emphasis` de Bootstrap en lugar de `text-*` |
| 9 | `text-warning`, `text-info` sobre blanco (KPI «Sin asignar», tiempo promedio) | `#ffc107` / `#0dcaf0` | ≈ 1,6 y 1,9:1 | `#8a6100` (≈ 5,5:1) y `#087990` (≈ 5,1:1); definidos como `--color-aviso`, `--color-info` y aplicados a `.text-warning`/`.text-info` |
| 10 | `btn-outline-warning` (Devolver: icono y borde) | `#ffc107` sobre `#fff` | ≈ 1,6:1 (icono informativo, umbral 3:1) | variables de botón `--bs-btn-color/--bs-btn-border-color:#8a6100`, hover relleno `#8a6100` con texto blanco |
| 11 | `btn-outline-success/secondary/danger/primary` (iconos) | `#198754`, `#6c757d`, `#dc3545`, `#0d6efd` sobre blanco | ≥ 4,5:1 (esperado) | solo medir |
| 12 | Filas `opacity-50` (Configuración) | texto al 50 % de opacidad | no cumple | `.fila-inactiva` (color `--color-muted-fuerte`, sin opacidad) |
| 13 | Estado vacío (icono y texto) | nuevo | — | texto `--color-muted-fuerte`, icono `#5a6b82` (≥ 3:1) |
| 14 | `text-primary` en enlaces pequeños («Ver repuestos →»), `.sidebar-link.active`, `.topbar-title`, `.login-show-btn` y su `:hover` `#CC1E24` | `#0d6efd`/`#fff`, `#fff`/azul, rojo/`#f8fafc` | ≈ 4,5:1 / 7,7:1 / 5,3:1 | solo medir; si `#0d6efd` queda por debajo sobre `#f4f6f9`, usar `--color-primary` `#1A5399` en enlaces de texto |

Gráficos del Dashboard: los colores de las series no se tocan (no son texto ni iconos informativos; la leyenda y el tooltip llevan la misma información). Los emoji de las tarjetas KPI son decorativos (`aria-hidden`), no se miden.

## Patrón de formularios y etiquetas (RF-6, RF-7, RF-20, RF-26)

- Rejilla: se usa **la rejilla de Bootstrap en el marcado**, sin CSS que la sobrescriba. Filtros: campos `col-12 col-md-6 col-lg-<ancho actual>` y un único contenedor de acciones `col-12 col-lg-auto acciones-form d-flex gap-2` (Buscar + Limpiar). Resultado: ≤767 px una columna; 768-991 px dos columnas; ≥992 px exactamente la disposición actual (los anchos `col-md-4/3/2/2/1` pasan a `col-lg-*` con los mismos valores). Formularios con `col-md-6` ya cumplen (una columna bajo 768 px, dos por encima); los que usen `col-md-3/4` se pasan a `col-md-6 col-lg-3/4`.
- Alternativa descartada: reglas CSS que fuerzan `width:100%`/`50%` sobre `[class*="col-md-"]` en `.filtros`. Acopla el CSS al nombre de las clases de Bootstrap y obliga a sobrescribir sus anchos.
- Botón limpiar (`✕`): se mantiene el símbolo y se añade `aria-label="Limpiar filtros"` y `title`; en ≤575 px ocupa el ancho completo (RF-7).
- Etiquetas: cada control con `id` único en la página y su `<label for>`; en vistas con `asp-for` ya está. Controles sin etiqueta visible (buscador con icono, `✕`, input de adjuntos generado por script) reciben `aria-label` en español. `btn-close` de modales y alertas: `aria-label="Cerrar"`. Los campos cargados por script (técnico, repuesto, área) ya tienen su `<select id>` en el HTML estático: solo se les añade `label for`. La lista de adjuntos de `Tickets/Create` (generada por script): cada botón de quitar lleva `aria-label` con el nombre del archivo.
- Errores (RF-26): en vistas con `asp-validation-for` el `span` ya existe junto al campo. `site.js` le asigna `id` (`err-<name>`), añade ese `id` a `aria-describedby` del campo y fija `aria-invalid="true"` mientras la clase `input-validation-error` esté presente; `.field-validation-error::before{content:"⚠ "}` evita depender solo del color (el propio mensaje es texto). Los mensajes por defecto en inglés se evitan completando `ErrorMessage` en español en las anotaciones de los ViewModels que no lo tengan (RNF-2b; se listan las que falten en la tarea de cada módulo).
- Formularios sin ViewModel (los 6 de alta y 6 de edición de Configuración envían `nombre`/`descripcion` sueltos con `required`): marcados `data-validar` y con `data-msg-required="..."` en español en la vista; `site.js` muestra el mensaje en un `div.invalid-feedback` con `id` asociado por `aria-describedby`. Ver D8.
- Botones de acción de formularios (RF-7): en ≤575 px, `.acciones-form` en columna a ancho completo; la acción principal primero visualmente mediante `order` (D6).

## Paginación móvil (RF-28)

- Parcial `_Paginacion` con una **función pura** de ventana de páginas (`@functions`, sin «hoy» ni estado): `VentanaPaginas(actual, total)` devuelve la lista de ítems: `1`, `…`, `actual-1`, `actual`, `actual+1`, `…`, `total`, de modo que haya como máximo 7 elementos con cualquier número de páginas (cientos de registros). Casos: total ≤ 7 muestra todas; actual en los extremos mantiene dos vecinas.
- CSS: en ≤575 px se ocultan los números intermedios (`.page-item:not(.active):not(.page-anterior):not(.page-siguiente)` fuera de la actual) y se muestra «Anterior» · «n de total» · «Siguiente»; las etiquetas «‹»/«›» llevan `aria-label` («Página anterior», «Página siguiente») y la página actual `aria-current="page"`. Todos los `.page-link` miden 44 px en ≤991 px.
- Las seis vistas llaman al parcial pasando su generador de URL (conservan sus parámetros de filtro actuales: `search`, `estado`, `estadoId`, etc.).
- Alternativa descartada: `overflow-x:auto` en la barra. Es desplazamiento horizontal propio, no es lo que pide RF-28 («sin desbordar») y esconde páginas.

## Menú lateral (RF-11, RF-21, RF-24)

Solo `_Layout.cshtml` y `site.css`, sin librerías ni trampa de foco (fuera de alcance):
- Botón: `aria-label="Abrir menú"`, `aria-controls="appSidebar"`, `aria-expanded` que sigue al estado, 44×44 px en ≤991 px.
- `openSidebar()` guarda `ultimoDisparador`; `closeSidebar()` quita la clase, actualiza `aria-expanded` y, si el cierre fue por Escape, clic fuera o elegir una opción, hace `toggleBtn.focus()`.
- Listeners nuevos: `keydown` en `document` (Escape con menú abierto), `click` en cada `.sidebar-link` de `<a>` (cierra), `resize`/`matchMedia('(min-width:992px)')` (cierra y limpia el backdrop al rotar a ≥992 px sin perder el contenido).
- CSS: el menú cerrado en ≤991 px lleva `visibility:hidden` con transición retardada (`visibility 0s .25s`) para que sus enlaces no sean tabulables fuera de pantalla; abierto, `visibility:visible`. Ancho `min(var(--sidebar-width), 85vw)`; `.sidebar-link` y `.sidebar-username` con texto envolvente en ≤991 px.
- Se mantiene `z-index` (1040/1035) y el botón «Cambiar contraseña» y «Salir» como están.

## Gráficos del Dashboard (RF-10)

Solo configuración y envoltorios de tamaño; misma librería, mismo CDN:
- Cada `<canvas>` va dentro de `<div class="chart-box">` (altura por breakpoint) y se quitan los atributos `height="..."`; todos los gráficos pasan a `maintainAspectRatio:false` (incluido `chartMes`, hoy sin él).
- Leyenda: `position:'bottom'` en los donuts (ya) y en `chartMes` (hoy `top`), `labels:{boxWidth:12, usePointStyle:true, font:{size:12}}`.
- Etiquetas: `ticks:{autoSkip:true, maxRotation:45, minRotation:0}` en barras verticales/línea; en la barra horizontal por técnico, `callback` que trunca a ~18 caracteres **solo cuando** `matchMedia('(max-width:575.98px)').matches`, con el nombre completo en el tooltip (el texto completo sigue existiendo en la tarjeta, no es el único acceso a la información; ver RF-2, los nombres completos están en el tooltip y en la lista de técnicos de otras pantallas).
- Un guard de una línea `if (typeof Chart === 'undefined') return;` en cada IIFE para que, si el CDN falla, no haya `ReferenceError` y los KPI (renderizados en servidor) sigan visibles. Es un cambio mínimo fuera de «solo configuración»; ver D10.
- Tarjetas KPI: `col-12 col-sm-6 col-lg-4` (hoy `col-6` para Soporte; a 375 px dos tarjetas por fila ≈ 170 px cada una: se evalúa y, si el valor se corta, `col-12`); filas de «Total» (`col-md-4`) pasan a `col-12 col-md-4`.

## Estado vacío y badges (RF-15, RF-16)

- `_EstadoVacio`: modelo `(string icono, string mensaje, string? urlCrear, string? textoCrear)`; si el usuario puede crear (condición ya existente en cada vista) se muestra el botón primario de crear. Mensajes: se reutilizan los textos actuales («No se encontraron tickets.», etc.); los de listados que hoy no tienen mensaje (secciones y tarjetas) usan los textos de la lista cerrada de RNF-2a.
- Badges estáticos (`bg-success`, `bg-primary`, `bg-secondary`, `bg-warning`, `bg-danger`) se mantienen y reciben `text-dark` donde haga falta (fila 7 de la tabla de contraste). Los de color de BD usan `_BadgeEstado`. La función pura del parcial:

```
TextoSobre(colorHex):  // "#RRGGBB" -> "#fff" | "#212529"
    L = luminancia(colorHex)                       // fórmula de la sección Contraste
    cBlanco = 1.05 / (L + 0.05)
    cNegro  = (L + 0.05) / (luminancia("#212529") + 0.05)
    devolver cBlanco >= cNegro ? "#fff" : "#212529"
```
Es pura (entrada: un string; salida: un string; sin reloj ni estado). Si `Color` viene vacío o inválido, devuelve blanco y el fondo cae al `bg-secondary`.

## Funciones puras

No hay lógica de negocio nueva ni funciones con «hoy». Las únicas funciones puras (justificación: dan resultado determinista medible sin servidor) son las dos de vista: `TextoSobre(colorHex)` y `VentanaPaginas(actual, total)`, ambas dentro de sus parciales Razor. Sin `dotnet test` (principio 4).

## Decisiones

- D1. Tabla apilada con CSS puro + `data-label` en cada `td` (ver sección). Alternativas descartadas: duplicar marcado; `data-label` por JS desde `<thead>`.
- D2. Título de página `<h1 class="page-title">` en lugar de `<h4 class="mb-0 fw-bold">`. Alternativa descartada: conservar `h4`, que deja las pantallas sin `h1` (mala jerarquía para lector de pantalla) y obliga a que el tamaño dependa de la etiqueta. Aspecto visual idéntico por la clase. **Requiere confirmación.**
- D3. Plegado de campos > 8 con botón + `aria-expanded` + JS delegado mínimo en `site.js` (JS propio, no librería). Alternativas descartadas: `<details>` (HTML inválido en `<tr>`), casilla oculta con `:checked` (sin estado accesible).
- D4. Rejilla con clases Bootstrap en el marcado (`col-12 col-md-6 col-lg-*`) en lugar de CSS que sobrescriba anchos. Alternativa descartada: reglas `[class*="col-md-"]`.
- D5. Casillas e interruptores: el objetivo de 44 px es el `<label>` asociado (que envuelve visualmente al input), no el cuadro de 16 px. Los enlaces dentro de un párrafo de texto corrido (p. ej. Privacidad) quedan exentos del 44 px (criterio WCAG 2.5.8 de enlaces en línea); los enlaces de tabla/menú/tarjeta sí cumplen. Alternativa descartada: agrandar el `<input>` a 44 px (deforma el formulario). **Aprobada; ya recogida en RF-24 y CF-5 de la spec.**
- D6. «Acción principal primero» en móvil con `order` de CSS en `.acciones-form` y `.modal-footer` (en el DOM suele estar Cancelar antes que Guardar). Alternativa descartada: reordenar el DOM en ~25 sitios y compensar en escritorio con `flex-direction:row-reverse` (más cambios y riesgo de regresión). Coste: el orden de tabulación en móvil no coincide con el visual en esos pies. **Requiere confirmación.**
- D7. Ajustes de color con variables y utilidades propias (`--color-muted-fuerte`, `--color-aviso`, `--color-info`, alfas del menú y del login), medidos y fijados con valores reales, conservando matiz (RF-19). Alternativa descartada: sobrescribir las variables de Bootstrap (`--bs-warning-rgb`), que también oscurecería los fondos `bg-warning` de todos los componentes.
- D8. Formularios sin ViewModel (Configuración): validación mínima en `site.js` con mensajes en `data-msg-*` de la vista, en lugar de las burbujas nativas del navegador (idioma del navegador, no asociadas al campo). Alternativa descartada: crear ViewModels con anotaciones para esos 12 formularios (cambiaría controladores, fuera de RNF-2). **Aprobada; RF-26 y RNF-2 de la spec permiten ahora estos mensajes en la vista (la lista cerrada de textos nuevos incluye los 12 formularios de Configuración).**
- D9. `html{font-size}` en porcentaje (87,5 %/93,75 %) y alturas fijas (`height:54px` de la barra superior) a `min-height`, para que el texto y el zoom al 200 % no recorten (RF-27). Alternativa descartada: mantener px.
- D10. Guard `typeof Chart` en los scripts de gráficos, aunque excede «solo configuración»: es la única forma de que el Dashboard muestre sus indicadores sin errores nuevos si falla el CDN (caso límite). **Aprobada; RF-10 y RNF-1 de la spec la contemplan (configuración + guarda de carga).**
- D11. Un parcial por patrón repetido (`_EstadoVacio`, `_Paginacion`, `_BadgeEstado`) en vez de copiar marcado en las 32 pantallas. Alternativa descartada: marcado copiado (la spec 002 usó esa vía porque eran 21 usos sin lógica; aquí hay 6 paginaciones con lógica de ventana y ~14 estados vacíos, y RF-13..18 piden uniformidad que un parcial garantiza). Los parciales no son paquetes ni cambian la relación con los ViewModels.
- D12. Corregir en un cambio menor de la spec la frase «paginación del listado de visitas generada por script» (es Razor del servidor); no cambia ningún RF. **Aplicada en la spec.**
- D2 y D6 también aprobadas (h1 en títulos; orden de botones por CSS). Aspecto (D7/RF-13..18) aceptado.

## Riesgos

1. **Regresión en escritorio** (≥992 px): se mitiga escribiendo todas las reglas nuevas dentro de `@media (max-width:991.98px)` salvo los tokens/uniformidad (cabecera, tarjetas, badges, botones). Esos últimos cambian el aspecto en escritorio de forma intencional (RF-13..18); se compara con una captura previa de 6 módulos a 1280 px antes de empezar (CF-6) y los botones solo icono de la spec 002 conservan su tamaño (RF-24).
2. **Configuración con 6 tablas + 12 formularios/modales**: la pantalla más densa (32 `<th>`, 11 usos de truncado/anchos fijos). Se parte en dos pasos por separado dentro de la fase 5 (tablas+pestañas; formularios+modales). Las pestañas (`nav-tabs`) con 6 elementos desbordan a 375 px: pasan a `flex-nowrap overflow-x:auto` **dentro de su propio contenedor** o a `flex-wrap` (se decide al medir; el desplazamiento interno de las pestañas es aceptable por RF-1, pero se prefiere `flex-wrap` por RF-24).
3. **Semántica de tabla con `display:block`**: ver plan B de roles ARIA.
4. **Campos duplicados por `id`**: al añadir `id` a los controles sin él, vigilar duplicados en una misma página (Configuración y modales de Inventario); la medición cuenta ids repetidos.
5. **Vistas precompiladas**: sin reconstruir la imagen no se ven los cambios (ver verificación).
6. **Colores de BD arbitrarios**: el parcial garantiza 4,5:1 solo para texto; si una prioridad usa un color casi blanco sobre fondo blanco de tarjeta, el borde del badge no llega a 3:1. Se añade `border:1px solid rgba(0,0,0,.15)` a los badges de BD para evitarlo.
7. **Cobertura**: 32 pantallas × 4 anchos × 4 roles × 19 modales es mucho recorrido manual. Se automatiza con el script de medición y una lista de rutas; las pantallas por rol se priorizan (ver verificación).

## Estrategia de verificación

No hay `dotnet test` (principio 4). Para cada tarea/módulo:

1. `dotnet build SistemaTickets.sln` sin errores (CF-7).
2. Reconstruir y levantar: `docker-compose up -d --build app` (las vistas Razor van precompiladas y `wwwroot` se copia a la imagen, por lo que cada cambio de `.cshtml`, CSS o JS exige reconstruir; con `dotnet run` también hay que recompilar). Abrir `http://localhost:8080`. Las credenciales de cada rol las aporta el usuario y no se guardan en el repositorio, en `MEMORY.md` ni en el plan.
3. Búsquedas (grep) previas y posteriores, que van en el commit:
   - `outline:\s*(none|0)|box-shadow:\s*none` en `site.css` y vistas: 0 sin sustituto (CF-4).
   - `<label[^>]*form-label[^>]*>` sin `for=` ni `asp-for`: 0 (RF-20).
   - `table-responsive` y `tabla-apilable` presentes en las 14 tablas; `<th>` conservados (RF-25).
   - `style="[^"]*font-size` y `text-truncate`: revisados uno a uno (RF-2, RF-14).
4. Script de medición en Chrome DevTools (`evaluate_script`; se pega en la consola, **no se guarda en el repo**: no hay JS de prueba ni build, principio 1). Pseudocódigo:

```
medir():
  ancho = innerWidth
  // RF-1 / CF-1
  desborde = document.documentElement.scrollWidth - ancho          // debe ser <= 0
  culpables = elementos visibles con getBoundingClientRect().right > ancho+0.5
              que NO estén dentro de un .table-responsive ni de un .modal  -> listar selector, 5 primeros
  // RF-3 / RF-4 / CF-2
  para cada table.tabla-apilable:
      apilada = getComputedStyle(tr_primera).display == "block"     // esperado: ancho<=575 true, resto false
      scrollInterno = contenedor.scrollWidth > contenedor.clientWidth   // permitido solo si !apilada
      tdSinLabel = td sin data-label y sin clase celda-acciones/fila-vacia   // debe ser 0 si apilada
      camposVisibles = por tr: cuenta de td visibles con ::before                // <= 8
  // RF-24 / CF-5
  objetivos = [a[href], button, input visibles, select, textarea, summary, [role=button]]
      excluir: ocultos (rect 0), .visually-hidden, enlaces dentro de <p> con texto corrido
      rect = para check/radio/switch: unión del rect del <label> asociado; resto: su rect
      falla si rect.width < 44 || rect.height < 44   (solo cuando ancho <= 991)
      pares = vecinos del mismo contenedor ordenados; falla si hueco < 8 sin solapar
  // RF-20 / CF-3
  campos = input (no hidden/submit/button), select, textarea
      nombre = aria-label | aria-labelledby (texto) | el.labels con texto no vacío
      falla si vacío; reportar también ids duplicados
  // RF-21 / RF-22 / CF-4 (se ejecuta tras pulsar Tab con press_key, para activar :focus-visible)
  foco():
      el = document.activeElement
      cs = getComputedStyle(el)
      indicador = outline (cs.outlineStyle != "none" && outlineWidth>0) ? cs.outlineColor
                  : box-shadow opaco ? su color : ninguno
      fondo = primer ancestro con background-color opaco (componer alfas)
      contraste(indicador, fondo) >= 3
  // RF-23
  recorrer nodos de texto visibles:
      color = color computado compuesto con su alfa sobre fondo
      fondo = background-color opaco del ancestro (componer alfas); si hay degradado -> se mide a mano en ambos extremos
      grande = fontSize>=24 || (fontSize>=18.66 && fontWeight>=700)
      umbral = grande ? 3 : 4.5
      reportar (selector, color, fondo, contraste) únicos que no lleguen al umbral
  devolver {ancho, desborde, culpables, tablas, objetivos, campos, foco, contraste}
```
   El mismo script con los mismos criterios se pasa antes de tocar nada (línea base: número de avisos de «campo sin etiqueta» por pantalla y rol, abriendo cada modal y activando los campos de script) y después de cada módulo.
5. Lighthouse (auditoría de accesibilidad del MCP de Chrome DevTools): 0 campos sin nombre accesible por pantalla (CF-3).
6. Recorrido en Chrome DevTools: viewports 375×800, 600, 768×1024 y 1280 (más 375×400 para teclado en pantalla simulado con modales y login, y rotación 375↔800 con menú y un modal abiertos). Por pantalla: carga, estado vacío (filtro sin resultados), 1 registro y muchos (≥ 200 para la paginación), errores de validación, cada modal del módulo (abierto con su disparador real, para activar los selects cargados por script), zoom 200 % y texto ampliado en 375, 768 y 1280. Consola: sin errores nuevos.
7. Roles: Administrador (todas las pantallas), Supervisor (todas menos Usuarios y Configuración), Soporte (Tickets, Nuevos, Inventario/Herramientas asignadas, Campo, Dashboard), Usuario (Inicio, Tickets, Crear/Detalle). El menú y las acciones visibles cambian por rol, así que en cada módulo se repasa con los roles que lo ven.
8. Capturas de cabecera, badges, estado vacío y botones de al menos 6 módulos (CF-6), y lista manual de CF-1..CF-8 en el commit de cada módulo (CF-9). La línea base y los resultados se anotan en `specs/003-vistaResponsive/verificacion.md` (archivo de evidencias que se crearía al implementar).

## Reparto del trabajo (para que salgan tareas de 20-30 min; sin escribirlas)

Cada paquete es una tarea candidata con una verificación completa de su alcance:

1. CSS base: tokens, foco, `html` en %, grises/menú/login/badges AA, cabecera, tarjetas, botones, badges, estado vacío, tabla apilada, paginación, modales, 44 px, `chart-box`. RF-3..9, 13..19, 21..24, 27, 28.
2. Layout y comunes: `_Layout` (botón, script de menú, modal de contraseña), `_LoginLayout`, `site.js` (plegado, validación accesible), parciales `_EstadoVacio`, `_Paginacion`, `_BadgeEstado`. RF-5, 11, 15, 16, 20, 26, 28.
3. Tickets y Seguimiento, tablas: `Tickets/Index`, `Tickets/Nuevos`, `Supervisor/Seguimiento` (3 tablas, filtros, modal «tomar», paginación). RF-1..9, 24, 25, 28.
4. Tickets, formularios y detalle: `Create`, `Edit`, `Details` (adjuntos por script, modal de cierre, panel lateral). RF-1, 2, 6, 7, 9, 10, 20, 26.
5. Inventario, listados: `Activos`, `Herramientas` (2 tablas, filtros, 4 modales, 4 acciones por fila). RF-1..9, 24, 28.
6. Inventario, formularios y detalles: `ActivoCreate`, `ActivoEdit`, `ActivoDetails`, `HerramientaCreate`, `HerramientaDetails` (modales de asignar y devolver). RF-1, 2, 6, 7, 9, 10, 20, 26.
7. Campo (6): `Visitas`, `Repuestos`, `VisitaCreate`, `RepuestoCreate`, `VisitaDetails`, `RepuestoDetails` (modal de stock y de consumo). Es la más grande tras Configuración; puede partirse en listados (2 tablas) y formularios/detalles.
8. Usuarios y Configuración (3): `Users/Index`, `Users/Create` y `Configuracion/Index`. Candidata a partirse en dos: Usuarios; Configuración (tablas y pestañas) y, aparte, formularios/modales de Configuración con la validación `data-validar`.
9. Inicio por rol (4) + Inicio, Error, Privacidad, Reportes: tarjetas, filtros de Reportes, 44 px, contrastes.
10. Dashboard y Login + verificación final transversal (4 roles × 4 anchos × 32 pantallas, Lighthouse, capturas CF-6, grep globales, `dotnet build`).

Nota de tamaño: resuelto por el usuario. Se mantiene UNA sola spec con 13 tareas (excepción explícita a la regla de más de 10 tareas); el reparto definitivo está en `tasks.md`.

## Cobertura de requisitos

| RF / CF | Dónde |
|---|---|
| RF-1, CF-1 | todo el plan; script `desborde`/`culpables` |
| RF-2 | utilidades de texto largo; revisión de `text-truncate`; gráficos |
| RF-3, RF-4, CF-2 | patrón de tabla apilada; `.table-responsive` en Configuración |
| RF-5 | plegado de campos > 8 |
| RF-6, RF-7 | patrón de formularios, `acciones-form` |
| RF-8, RF-13 | `.page-header` |
| RF-9 | CSS de modales |
| RF-10 | gráficos, KPI, panel lateral (`col-lg-8`/`col-lg-4` ya apila bajo 992 px; se verifica orden de lectura), tarjetas de detalle |
| RF-11, CF-10 | menú lateral |
| RF-12 | Login (`100vh` con `100dvh`, tarjetón apilado, botón alcanzable en 375×400) |
| RF-14..19 | tokens, `.badge`, `_BadgeEstado`, `_EstadoVacio`, jerarquía de botones, `.card` |
| RF-20, CF-3 | patrón de etiquetas |
| RF-21, RF-22, CF-4 | foco visible |
| RF-23 | tabla de contraste AA |
| RF-24, CF-5 | 44 px en ≤991 px |
| RF-25 | `thead` visualmente oculto, mismo orden DOM |
| RF-26 | `site.js` + anotaciones en español |
| RF-27 | `html` en %, `min-height`, envoltura de texto |
| RF-28, CF-10 | `_Paginacion` |
| CF-6..CF-9 | verificación (capturas, build, commit con checklist) |
