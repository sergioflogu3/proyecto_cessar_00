# Spec 002 — Botones de acción solo con icono

Estado: implementada

## Contexto y objetivo
Hoy las acciones sobre un registro (ver, editar, dar de baja, asignar...) se muestran en
unas pantallas como botones con texto y en otras (gestión de usuarios y configuración)
como botones solo con icono. La mezcla ensancha las filas de las tablas, sobre todo en
móvil, y da una apariencia inconsistente. El objetivo es que **todas** las acciones sobre un
único registro se presenten como botón solo con icono, igual que en la pantalla de gestión
de usuarios, sin cambiar qué hace cada botón, quién lo ve ni cuándo aparece, y sin perder
accesibilidad.

## Usuarios
- Administrador, Supervisor, Soporte y Usuario: ven los botones según los permisos que ya tienen; esta spec no cambia permisos.

## Historias de usuario
- HU-1. Como usuario del sistema, quiero ver todas las acciones de un registro como iconos uniformes para reconocerlas rápido y que las tablas ocupen menos ancho.
- HU-2. Como usuario de lector de pantalla o de ratón, quiero que cada icono tenga un nombre y una ayuda emergente para saber qué hace antes de pulsarlo.

## Definiciones
- **Acción sobre un registro**: botón o enlace que actúa sobre un único registro concreto (ver, editar, dar de baja, asignar, devolver, tomar, cerrar, ajustar stock), situado en una fila de tabla o en la cabecera de la pantalla de detalle de ese registro.
- **Botón solo icono**: botón de la misma apariencia que el de editar en la gestión de usuarios (contorno de color, tamaño pequeño), cuyo contenido visible es únicamente un icono, sin texto.
- **Texto anterior**: el texto que el botón mostraba antes del cambio (p. ej. "Dar de baja", "Cerrar ticket", "Tomar ticket").
- **Pantallas afectadas** (inventario a fecha de la spec, 21 botones):
  - Listado de tickets: Ver. Tickets nuevos: Tomar ticket. Detalle de ticket: Editar, Cerrar ticket.
  - Listado de visitas: Ver. Listado de repuestos: Ver. Detalle de visita: Editar. Detalle de repuesto: Editar, Ajustar Stock.
  - Listado de activos: Ver, Editar, Dar de baja. Listado de herramientas: Ver, Editar, Asignar, Devolver.
  - Detalle de activo: Editar, Asignar. Detalle de herramienta: Editar, Asignar, Devolver.
- **Ya conformes** (sin cambios): gestión de usuarios, listas de configuración, descarga de adjunto.

## Requisitos funcionales
- RF-1. EL SISTEMA mostrará cada acción sobre un registro de las pantallas afectadas como botón solo icono, sin texto visible.
- RF-2. EL SISTEMA dará a cada botón solo icono un atributo de título y un nombre accesible, ambos iguales a su texto anterior.
- RF-3. EL SISTEMA usará un icono distinto y coherente por tipo de acción, el mismo en todas las pantallas (p. ej. Ver y Editar idénticos en listados y detalles).
- RF-4. EL SISTEMA conservará el color semántico actual de cada botón (peligro para cerrar/dar de baja, éxito para asignar, aviso para devolver) en su variante de contorno.
- RF-5. CUANDO el usuario pulse un botón solo icono, EL SISTEMA ejecutará exactamente la misma acción que antes (misma navegación, mismo modal, misma petición).
- RF-6. MIENTRAS se cumpla la condición que hoy hace visible un botón (rol, estado del registro, existencia de sedes...), EL SISTEMA lo mostrará, y en caso contrario no.
- RF-7. EL SISTEMA mantendrá sin cambios los identificadores de los botones y los manejadores que los usan, de forma que todos los flujos que dependen de ellos sigan funcionando.
- RF-8. EL SISTEMA mantendrá con texto: Guardar, Cancelar, Buscar, Limpiar, Crear/Nuevo, los botones de confirmación de modales, los enlaces de navegación entre listados (p. ej. Ver Activos, Ver Repuestos), los botones "Volver" y los botones de las tarjetas de inicio.
- RF-9. CUANDO varias acciones se muestren juntas en una fila o cabecera, EL SISTEMA las separará con el mismo espaciado y sin desbordar el ancho en 375 px.
- RF-10. SI un botón solo icono se activa con teclado (Tab + Enter/Espacio), ENTONCES EL SISTEMA responderá igual que al pulsar con ratón, mostrando el foco visible.

## Requisitos no funcionales
- RNF-1. Los textos de título y nombre accesible estarán en español.
- RNF-2. No se añaden librerías ni recursos nuevos: se usan los iconos ya disponibles en el sistema.
- RNF-3. Ningún cambio en datos, permisos ni comportamiento del servidor.

## Casos límite
- Botones que aparecen o desaparecen según estado (Asignar solo con herramienta/activo disponible, Devolver solo si asignada, Dar de baja solo si no está de baja): se mantiene la misma lógica.
- Fila con 4 botones (herramienta, administrador/supervisor): debe caber en 375 px sin romper la tabla (desplazamiento horizontal de la tabla existente aceptable, sin solapes).
- Textos de los modales que mencionen la acción (p. ej. "Confirmar baja") no cambian.
- Un botón que tras pulsarse cambia su propio texto por script: no aplica a ninguno de los 21 (verificado); el de "Procesando..." es el botón de confirmación del modal, que queda con texto.

## Fuera de alcance
- Botones de formulario o de panel que no son acción sobre el registro: "Aplicar estado", "Asignar" (agente) y "Aplicar prioridad" del panel lateral del detalle de ticket, "Enviar comentario", "Cambiar estado" y "+ Registrar consumo" del detalle de visita, descargas de reportes (Excel/PDF), "Crear Categoría/Prioridad...".
- Cambiar permisos, textos de modales, estilos globales o añadir confirmaciones nuevas.

## Criterios de finalización
- CF-1. Búsqueda de verificación: ningún enlace o botón de acción de las pantallas afectadas contiene texto visible (los 21 del inventario pasan a 0 con texto); documentada en el commit.
- CF-2. Los 21 botones tienen título y nombre accesible iguales al texto anterior (revisión con grep y con el árbol de accesibilidad del navegador).
- CF-3. `dotnet build` sin errores.
- CF-4. Verificación en el navegador (Chrome DevTools) en escritorio y móvil 375x800, con cada rol aplicable: cada botón se ve como icono, ejecuta su acción (abrir modal, navegar, confirmar), consola sin errores nuevos.
- CF-5. Sin tests automatizados (principio 4 de la constitución): la comprobación es la lista manual de CF-4 en el commit.

## Dudas abiertas
- [RESUELTA al aprobar la spec, 15/10/2026] "Cambiar estado" (desplegable) y "+ Registrar consumo" en el detalle de visita, y "Asignar" / "Aplicar estado" / "Aplicar prioridad" del panel lateral del ticket: se han tratado como fuera de alcance (son formularios o desplegables, no acciones simples); ¿confirmas?
- [RESUELTA al aprobar la spec, 15/10/2026] Botones de descarga Excel/PDF de Reportes (acción sobre un informe, no sobre un registro): ¿fuera de alcance?
- [RESUELTA al aprobar la spec, 15/10/2026] Iconos propuestos: Ver (ojo), Editar (lápiz, como usuarios), Cerrar ticket (candado), Tomar ticket (mano), Dar de baja (caja de archivo; usuarios usa "prohibido" para inactivar), Asignar (usuario con +), Devolver (flecha de retorno), Ajustar stock (controles deslizantes). ¿Los aceptas?
