# Spec 003 — Vista y responsive de todo el proyecto

Estado: implementada

Historial:
- Aprobada inicialmente.
- Implementada 17/10/2026 con salvedades (T1-T13; ver `verificacion.md`): 27 de las 32 pantallas medidas con el rol Administrador; sin medir Inicio, Privacidad (vista huérfana, sin acción) y los inicios de Supervisor, Soporte y Usuario; paginaciones sin probar con ≥200 registros.
- Ajustada 15/10/2026 tras revisar el plan: (a) excepción de objetivos de 44 px para casillas, interruptores y enlaces dentro de un párrafo (RF-24, CF-5); (b) validación mínima en cliente, con mensajes en español en la vista, para los formularios de Configuración sin ViewModel (RF-26, RNF-2); (c) guarda de carga de la librería de gráficos en el Dashboard (RF-10, RNF-1); (d) corrección de «Casos límite»: la paginación del listado de visitas la genera el servidor, no un script; (e) esta spec se mantiene como una sola, con 12-13 tareas (excepción explícita a la regla de más de 10 tareas, ver tasks.md).

## Contexto y objetivo
Hoy la aplicación se usa sobre todo en escritorio y en móvil se nota: las tablas se desplazan en horizontal dentro de su contenedor, los formularios, filtros y cabeceras no están pensados para anchos de 375 px, cada pantalla resuelve a su manera el espaciado, los títulos, los estados vacíos y los badges, y hay campos de formulario sin etiqueta asociada (la consola del navegador avisa de unos 34). Además, el estilo global quita el contorno de foco en botones y campos, lo que perjudica a quien navega con teclado.

Evidencia del estado actual (a fecha de la spec): 14 tablas repartidas en 9 pantallas (listados de tickets, tickets nuevos, seguimiento, visitas, repuestos, activos, herramientas, usuarios y configuración, que por sí sola tiene 6), todas dentro de un contenedor de desplazamiento horizontal; el estilo solo define puntos de corte para el menú lateral (hasta 991 px) y para el login (hasta 640 px); ninguna regla específica adapta tablas, formularios o filtros a pantallas pequeñas.

El objetivo es que **todas** las pantallas sean cómodas y legibles en móvil (375 px) y tableta (768 px), con aspecto uniforme y accesibilidad básica, **conservando la identidad visual actual** (paleta azul, logo, librería de componentes en uso) y sin cambiar funcionalidad, permisos ni datos.

## Usuarios
- Administrador, Supervisor, Soporte y Usuario: usan las pantallas que ya tienen según su rol; esta spec no cambia permisos.
- Personas que usan teclado o lector de pantalla: se benefician de las mejoras de accesibilidad.

## Historias de usuario
- HU-1. Como usuario que trabaja desde el móvil, quiero que ninguna pantalla se desborde ni tenga elementos cortados para poder consultar y operar sin hacer zoom ni desplazarme de lado.
- HU-2. Como usuario móvil, quiero leer cada registro de un listado de forma apilada (sin desplazamiento horizontal) para ver sus datos y sus acciones de un vistazo.
- HU-3. Como usuario de tableta, quiero que filtros, formularios y cabeceras aprovechen el ancho sin romperse a 768 px.
- HU-4. Como usuario habitual, quiero que cabeceras de página, badges, estados vacíos y botones se vean igual en todas las pantallas para reconocerlos sin esfuerzo.
- HU-5. Como persona que usa teclado o lector de pantalla, quiero que cada campo tenga su etiqueta y que el foco sea visible para saber dónde estoy y qué estoy rellenando.
- HU-6. Como usuario con pantalla pequeña, quiero objetivos de pulsación suficientemente grandes para no pulsar el elemento equivocado.

## Definiciones
- **Móvil**: ancho de ventana de 575 px o menos (se verifica a 375 px). **Tableta**: de 576 px a 991 px (se verifica a 600 px y a 768 px). **Escritorio**: 992 px o más (se verifica a 1280 px). Los anchos de verificación son 375, 600, 768 y 1280 px; no se exige el comportamiento en rangos no medidos más allá de lo que implican los límites de cada rango.
- **Desbordamiento horizontal**: el ancho total desplazable del documento supera el ancho de la ventana. El desplazamiento horizontal interno de una tabla dentro de su propio contenedor (RF-4) no cuenta como desbordamiento del documento ni como ocultar contenido, siempre que el documento no se desplace.
- **Tabla que "no cabe"**: tabla cuyo ancho natural (con el contenido sin recortar) supera el ancho disponible de su contenedor.
- **Vista apilada**: en móvil, cada fila de una tabla se presenta como una tarjeta o bloque vertical donde cada dato aparece con su nombre de columna, sin desplazamiento horizontal.
- **Objetivo táctil**: botón, enlace, casilla, selector o campo sobre el que se pulsa.
- **Cabecera de página**: bloque superior de cada pantalla con título, descripción opcional y acciones principales.
- **Estado vacío**: mensaje que se muestra cuando un listado o sección no tiene registros.
- **Tabla**: se cuenta cada elemento tabla por separado, no cada pantalla (14 tablas en 9 pantallas).
- **Inventario de pantallas**: lista cerrada de **32 pantallas** (las 32 vistas de contenido de la carpeta de vistas; no cuentan como pantalla los 5 archivos auxiliares: los dos layouts, los dos de arranque/imports y el parcial de scripts de validación, aunque su contenido se verifica dentro de las pantallas que los usan):
  - Acceso y comunes (4): Login; Inicio (bienvenida genérica); Error; Privacidad.
  - Inicio por rol (4): Administrador, Supervisor, Soporte, Usuario.
  - Tickets (5): listado, nuevos, crear, editar, detalle.
  - Seguimiento (1): seguimiento de tickets (Supervisor).
  - Campo (6): visitas (listado, crear, detalle) y repuestos (listado, crear, detalle).
  - Inventario (7): activos (listado, crear, editar, detalle) y herramientas (listado, crear, detalle).
  - Usuarios (2): listado, crear.
  - Configuración (1): listas de categorías, prioridades, estados, tipos, sedes y áreas (6 tablas).
  - Reportes (1): filtros y descargas.
  - Dashboard (1): indicadores y gráficos.
  - Total: 4 + 4 + 5 + 1 + 6 + 7 + 2 + 1 + 1 + 1 = 32.
- **Pantallas con tabla (9)**: tickets (listado), tickets nuevos, seguimiento, visitas, repuestos, activos, herramientas, usuarios y configuración (14 tablas).
- **Listados con paginación (6)**: tickets, visitas, repuestos, activos, herramientas y usuarios.
- **Modales relevantes (19, de 11 pantallas)**, que se abren uno a uno en la verificación: usuarios (edición, confirmación); configuración (edición de categorías, prioridades, estados, tipos, sedes y áreas); activos listado (baja); activo detalle (asignar); herramientas listado (asignar, devolver); herramienta detalle (asignar, devolver); repuesto detalle (ajuste de stock); visita detalle (consumo de repuestos); tickets nuevos (tomar ticket); ticket detalle (confirmar cierre); cambio de contraseña (en el layout común, disponible desde toda pantalla autenticada; se verifica al menos en una pantalla por rol).
- **Ya conformes** (sin rehacer): botones de acción solo icono de la spec 002.

## Requisitos funcionales

### Responsive
- RF-1. EL SISTEMA no presentará desbordamiento horizontal del documento en ninguna de las 32 pantallas del inventario ni en ninguno de sus estados (con datos, vacía, con errores de validación, con cada modal abierto) a 375, 600, 768 y 1280 px. Medición: ancho desplazable del documento menor o igual que el ancho de la ventana. Las pantallas Error y Privacidad quedan cubiertas por este requisito.
- RF-2. EL SISTEMA no recortará ni ocultará contenido ni controles en 375 px y 768 px: los textos largos se ajustan en varias líneas. En ancho de 991 px o menos no se usará truncado con puntos suspensivos como única forma de ver el texto completo; en escritorio se podrá truncar con puntos suspensivos y texto completo en ayuda emergente, pero esa ayuda no será nunca el único acceso al texto en ≤991 px (no funciona en táctil).
- RF-3. MIENTRAS el ancho de ventana sea de 575 px o menos, EL SISTEMA presentará cada tabla de datos en vista apilada: cada fila como bloque con el nombre de cada columna junto a su valor y las acciones de la fila agrupadas, sin desplazamiento horizontal. Esto incluye las 14 tablas y toda fila generada por script.
- RF-4. MIENTRAS el ancho de ventana esté entre 576 px y 991 px, EL SISTEMA mostrará las tablas con formato de tabla; SI una tabla no cabe, ENTONCES EL SISTEMA permitirá el desplazamiento horizontal solo dentro de su propio contenedor, nunca del documento. MIENTRAS el ancho sea de 992 px o más, EL SISTEMA mostrará las tablas con formato de tabla como hoy.
- RF-5. EL SISTEMA mantendrá en la vista apilada la misma información, el mismo orden de los datos principales, las mismas acciones y las mismas condiciones de visibilidad que en la tabla. Se permite agrupar los datos secundarios sin eliminarlos. Criterio de alto razonable: cada tarjeta mostrará como máximo 8 campos visibles a la vez; los restantes quedarán agrupados en un bloque plegable accionable con teclado, que indique su estado (abierto o cerrado) a los lectores de pantalla.
- RF-6. MIENTRAS el ancho de ventana sea de 767 px o menos, EL SISTEMA colocará los campos de formularios y de filtros en una sola columna a ancho completo; entre 768 px y 991 px, en un máximo de dos columnas; en escritorio conservará la disposición actual.
- RF-7. MIENTRAS el ancho de ventana sea de 575 px o menos, EL SISTEMA colocará los botones de acción principal de formularios (Guardar, Cancelar) y de filtros (Buscar, Limpiar) apilados o en una fila que no desborde, a ancho completo o equivalente, en orden coherente (acción principal primero).
- RF-8. MIENTRAS el ancho de ventana sea de 575 px o menos, EL SISTEMA reorganizará la cabecera de página (título, descripción y acciones) para que las acciones queden bajo el título, sin solapes ni desbordes.
- RF-9. CUANDO un modal se abra en móvil, EL SISTEMA lo mostrará completo dentro de la ventana: ancho máximo igual al de la ventana menos un margen, cuerpo con desplazamiento vertical interno si el modal es más alto que la ventana y pie con botones siempre alcanzable sin salir del modal. La comprobación con teclado en pantalla se hace emulando un viewport de altura reducida (p. ej. 375 x 400 px).
- RF-10. EL SISTEMA adaptará a 375 px y 768 px los bloques propios de cada módulo: tarjetas de inicio por rol, indicadores y gráficos del Dashboard, filtros de Reportes, paneles laterales del detalle de ticket (pasan a ir bajo el contenido principal) y tarjetas de datos de los detalles. En los gráficos existentes del Dashboard se permite ajustar su configuración de tamaño, leyenda y etiquetas y añadir una guarda que, SI la librería de gráficos no se ha cargado, ENTONCES omita dibujar los gráficos sin error y deje visibles los indicadores (sin cambiar de librería ni añadir paso de compilación); CUANDO las etiquetas sean largas o numerosas a 375 px, EL SISTEMA las rotará o truncará sin cortar ejes ni leyenda, y si la leyenda no cabe la colocará debajo.
- RF-11. MIENTRAS el ancho de ventana sea de 991 px o menos, CUANDO se abra el menú lateral, EL SISTEMA lo mostrará sin tapar de forma permanente el contenido, con su fondo de cierre, y lo cerrará al pulsar fuera, al elegir una opción y con la tecla Escape (comportamiento nuevo permitido). Al cerrarse, EL SISTEMA devolverá el foco al botón que lo abrió. No se exige trampa de foco dentro del menú más allá de lo anterior.
- RF-12. EL SISTEMA mostrará la pantalla de acceso (Login) completa y sin desplazamiento horizontal en 375 px, incluido el teclado en pantalla abierto (comprobado con viewport de altura reducida): el botón de entrar sigue alcanzable desplazando en vertical.
- RF-28. MIENTRAS el ancho de ventana sea de 575 px o menos, EL SISTEMA presentará la barra de paginación de los 6 listados paginados sin desbordar la ventana (p. ej. con elipsis, o solo anterior/siguiente y la página actual), y sus controles cumplirán RF-24. Con cientos de registros la barra mantiene el mismo comportamiento.

### Consistencia visual
- RF-13. EL SISTEMA presentará la cabecera de página con la misma estructura, tipografía y espaciado en todas las pantallas del inventario (excepto Login y Error).
- RF-14. EL SISTEMA usará una escala única de espaciado y de tamaños de texto para títulos, subtítulos, texto de tabla y texto de ayuda en todas las pantallas; no habrá dos tamaños distintos para el mismo rol tipográfico.
- RF-15. EL SISTEMA mostrará los estados (de ticket, de prioridad, de activo, de herramienta, de visita, activo/inactivo de usuario) con badges de la misma forma, tamaño y espaciado en todas las pantallas. Conservará el color que hoy tiene cada estado salvo el mínimo ajuste de tono necesario para alcanzar el contraste de RF-23.
- RF-16. CUANDO un listado, tabla o sección no tenga registros, EL SISTEMA mostrará un estado vacío uniforme (icono, mensaje en español y, si el usuario puede crear registros, la acción de crear), igual en todas las pantallas. Sus colores se ajustan solo lo necesario para cumplir RF-23.
- RF-17. EL SISTEMA usará la misma jerarquía de botones en todas las pantallas: acción principal (relleno de color), secundaria (contorno) y destructiva (color de peligro), con la misma altura, radio y espaciado entre botones con texto. Los botones solo icono de la spec 002 quedan fuera de la igualdad de altura en escritorio y conservan allí su tamaño actual.
- RF-18. EL SISTEMA aplicará el mismo estilo a tarjetas, tablas, filtros y formularios (bordes, sombras, relleno interior y separación entre bloques) en todas las pantallas.
- RF-19. EL SISTEMA conservará la paleta, el logo y la tipografía de marca actuales; el degradado de la pantalla de acceso no cambia de matiz.

### Accesibilidad básica
- RF-20. EL SISTEMA asociará a cada campo de formulario (texto, número, fecha, selector, área de texto, casilla, interruptor, archivo) una etiqueta visible o, si el diseño no admite etiqueta visible (p. ej. buscador con icono), un nombre accesible en español. Medición con las herramientas oficiales: consola de Chrome (0 avisos de «campo sin etiqueta») y auditoría de accesibilidad de Lighthouse (0 campos sin nombre accesible). La línea base (unos 34 avisos hoy) se registra por pantalla y por rol, abriendo cada modal del inventario y activando los campos generados por script.
- RF-21. CUANDO un control reciba el foco con el teclado, EL SISTEMA mostrará un indicador de foco con contraste de al menos 3:1 frente al fondo adyacente, medido; esto aplica a campos, botones, enlaces, opciones del menú lateral, pestañas, controles de modales y componentes de la librería de componentes (modales, selectores).
- RF-22. EL SISTEMA no eliminará el contorno de foco de ningún control salvo que lo sustituya por otro indicador que cumpla el contraste de RF-21; un sustituto que no llegue a 3:1 no cuenta como sustituto.
- RF-23. EL SISTEMA cumplirá una relación de contraste mínima de 4,5:1 para texto normal y de 3:1 para texto grande e iconos informativos, incluidos badges, estados vacíos, texto de ayuda, texto gris sobre fondo claro, marcadores de posición, el texto sobre el degradado del Login y el menú lateral (opción activa, inactiva y secciones). Se permite un ajuste ligero de tono solo donde no se cumpla; los colores de marca (azul, logo) y el matiz del degradado se conservan.
- RF-24. MIENTRAS el ancho de ventana sea de 991 px o menos, EL SISTEMA dará a todo objetivo táctil un área de pulsación de al menos 44 x 44 px, incluidos los botones solo icono de la spec 002, los iconos de cabecera, el botón de abrir el menú y el de mostrar contraseña; el espacio entre dos objetivos contiguos será de al menos 8 px. Excepciones (criterio WCAG 2.5.8): en casillas e interruptores el objetivo es su etiqueta asociada (el área de la etiqueta cuenta, no solo el cuadro del control), que debe medir al menos 44 x 44 px; y los enlaces dentro de un párrafo de texto corrido quedan exentos del tamaño mínimo y de la separación. MIENTRAS el ancho sea de 992 px o más, EL SISTEMA no cambiará el tamaño actual de los objetivos (los botones solo icono conservan su tamaño actual), y en todo ancho conservará su título y su nombre accesible.
- RF-25. EL SISTEMA mantendrá en las tablas de datos cabeceras de columna semánticas y, en la vista apilada, un orden de lectura por teclado y lector de pantalla igual al visual.
- RF-26. CUANDO un campo falle la validación, EL SISTEMA mostrará el mensaje de error junto al campo, en español, asociado al campo para lectores de pantalla y sin depender solo del color. En los formularios que no tienen ViewModel (los 12 de alta y edición de Configuración) se permite una validación mínima en el cliente (campo obligatorio) con mensajes en español definidos en la propia vista.
- RF-27. EL SISTEMA permitirá ampliar el texto hasta el 200 % y el zoom del navegador hasta el 200 % sin pérdida de contenido ni funcionalidad, comprobado en 375 px, 768 px y escritorio.

## Requisitos no funcionales
- RNF-1. No se añaden frameworks, librerías ni pasos de compilación de recursos de cliente (principio 1 de la constitución); se reutilizan los componentes y los iconos ya disponibles. Se permite código de cliente propio mínimo (plegado de RF-5, accesibilidad de errores y validación mínima de RF-26, menú de RF-11, guarda de RF-10).
- RNF-2. Ningún cambio en datos, permisos, rutas, textos de negocio ni comportamiento del servidor; los identificadores y manejadores de los que dependen los flujos actuales se conservan. Lista cerrada de textos nuevos permitidos: (a) los estados vacíos de RF-16; (b) los mensajes de validación en español de RF-26, definidos en las anotaciones de los ViewModels y, para los 12 formularios de Configuración sin ViewModel, en la propia vista (validación de cliente de campo obligatorio); (c) las etiquetas y nombres accesibles de RF-20 y el texto del control de plegado de RF-5. Ningún otro cambio de comportamiento de servidor.
- RNF-3. Todos los textos nuevos de la lista de RNF-2 estarán en español (principio 6).
- RNF-4. Las vistas siguen enlazadas a ViewModels, sin cambiar esa relación (principio 3).
- RNF-5. Sin pérdida de rendimiento perceptible: no se cargan recursos de gran tamaño nuevos.
- RNF-6. La verificación se limita a Chrome (escritorio y móvil/tableta emulados con DevTools); Safari, Firefox y Edge quedan fuera. Solo se usan funciones CSS de uso general.

## Casos límite
- Tablas con muchas columnas (herramientas, activos, seguimiento, tickets): la vista apilada muestra todos los datos con el límite de RF-5 (máximo 8 campos visibles, el resto plegado).
- Fila con 4 acciones en solo icono (herramientas, rol Administrador/Supervisor): caben en 375 px con objetivos de 44 px y 8 px de separación, partiendo a una segunda línea si es necesario.
- Celdas con texto sin espacios muy largo (títulos, descripciones, correos, nombres de archivo): no desbordan; en ≤991 px se ajustan a varias líneas.
- Tablas de Configuración: son 6 tablas en la misma pantalla con acciones por fila; conservan su funcionamiento en vista apilada y en tableta.
- Estados con distinto contenido: pantalla con 0 registros, con 1 registro y con cientos (la paginación o la lista larga no desbordan ni se salen de la ventana).
- Modales con formularios largos (edición de usuario, ajuste de stock, asignación) en 375 px y con altura reducida por teclado en pantalla (emulación).
- Rotación del dispositivo (vertical a horizontal) con un modal o el menú abierto: no se pierde contenido.
- Texto de usuario muy largo o con caracteres acentuados y mayúsculas (dominio en español).
- Pantalla de detalle con panel lateral: en 375 px y 768 px el panel va debajo del contenido, con el orden lógico de lectura conservado.
- Dashboard con gráficos: en 375 px el gráfico se redimensiona sin cortar leyendas ni etiquetas (ver RF-10).
- Navegador con zoom o texto ampliado al 200 %: sin solapes (en 375 px, 768 px y escritorio).
- Pantallas por rol: el mismo layout debe verificarse con cada rol, pues cambian el menú y las acciones visibles.
- Campos que hoy se generan por script o dentro de modales (no visibles al cargar): también deben tener etiqueta o nombre accesible.
- Elementos generados por script: hoy no hay tablas ni filas de tabla generadas por script en las vistas; sí hay opciones de selectores cargadas por script (asignaciones, consumos, área), la lista de adjuntos al crear un ticket, la lista de adjuntos y los mensajes de error de modales. La paginación del listado de visitas la genera el servidor, como las otras cinco, y se rige por RF-28. Todos deben cumplir RF-1, RF-20, RF-24 y RF-28; SI en el futuro cercano apareciera una tabla o fila generada por script, ENTONCES también se apila (RF-3).
- Fallo de carga de recursos externos (iconos o gráficos desde CDN): la página sigue usable y sin desborde; los botones solo icono siguen identificándose por su título y nombre accesible, y el Dashboard muestra sus indicadores aunque falle el gráfico.
- Campos de adjuntos (archivo) y de fecha nativos: no desbordan en móvil y sus objetivos cumplen RF-24.
- Impresión de reportes y descargas PDF/Excel: no se ven afectadas; la impresión de pantallas no cambia.

## Fuera de alcance
- Cambio de paleta, logo o tipografía de marca; rediseño de identidad.
- Modo oscuro o selector de temas.
- Funcionalidades nuevas, nuevos campos, nuevos filtros o nuevos reportes; cambios de permisos o de flujo.
- Rehacer los botones de acción solo icono de la spec 002 (solo se ajusta su tamaño táctil y su espaciado en ≤991 px).
- Sustituir la librería de componentes actual o de gráficos por otra, incorporar un framework de frontend o un paso de compilación.
- Aplicación nativa, PWA o modo sin conexión.
- Cambios de texto de negocio y traducción a otros idiomas.
- Accesibilidad completa nivel AA/AAA más allá de los requisitos RF-20 a RF-27 (p. ej. auditoría completa de lector de pantalla, subtítulos, trampa de foco en el menú lateral).
- Contenido de los PDF y Excel generados.

## Criterios de finalización
- CF-1. Para cada una de las 32 pantallas del inventario, a 375, 600, 768 y 1280 px, el ancho desplazable del documento es menor o igual que el de la ventana (estados con datos, vacío y cada uno de los 19 modales abiertos).
- CF-2. A 375 px, las 9 pantallas con tabla (14 tablas) se presentan en vista apilada; entre 576 y 991 px (600 y 768 px) conservan formato de tabla, con desplazamiento solo interno si no cabe; a 1280 px conservan formato de tabla.
- CF-3. La consola de Chrome y Lighthouse muestran 0 campos sin etiqueta ni nombre accesible en todas las pantallas y roles, con cada modal abierto y los campos generados por script activados (línea base registrada antes: unos 34).
- CF-4. Todo control interactivo muestra foco visible con contraste medido de al menos 3:1 al navegar con Tab; búsqueda en el código de reglas que eliminan el contorno de foco sin sustituto que cumpla 3:1: 0.
- CF-5. Medición de objetivos táctiles a 375 px y a 768 px: 0 objetivos con menos de 44 x 44 px (y 0 pares contiguos con menos de 8 px de separación) en las pantallas del inventario, con las excepciones de RF-24 (etiqueta de casillas e interruptores como objetivo; enlaces dentro de un párrafo); a 1280 px los tamaños no cambian respecto a hoy.
- CF-6. Comparación visual de cabeceras, badges, estados vacíos y botones entre al menos 6 módulos: misma estructura, tamaños y espaciados (capturas incluidas en la verificación).
- CF-7. `dotnet build` sin errores.
- CF-8. Verificación en el navegador (Chrome DevTools) en 375 x 800, 600, 768 x 1024 y 1280, con cada rol aplicable: las 32 pantallas del inventario se recorren, sus acciones y los 19 modales funcionan igual que antes y la consola no muestra errores nuevos.
- CF-9. Sin tests automatizados (principio 4 de la constitución): la lista manual de CF-1 a CF-8 va en el commit de cada módulo.
- CF-10. Los 6 listados paginados (RF-28) no desbordan a 375 px con cientos de registros, y el menú lateral cumple RF-11 (cierre por fuera, por opción y por Escape, con retorno del foco).

## Dudas abiertas
- Ninguna.
