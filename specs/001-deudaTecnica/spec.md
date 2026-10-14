# Spec 001 — Deuda técnica: pantallas de detalle desacopladas de los registros internos

**Estado:** Aprobada (rev. 1) · Rev. 2 con las correcciones de la revisión QA (2026-10-13) · Rev. 3 con las aclaraciones y supuestos confirmados por el responsable (2026-10-13) · Rev. 4 con la corrección del acceso a la herramienta (RF-4, S2 y actores; 2026-10-14) · En implementación

## Contexto y objetivo
Cuatro pantallas de detalle (repuesto, activo, visita técnica y herramienta) se alimentan hoy
directamente con los registros internos del dominio, y alguna recibe además datos sueltos
fuera de su modelo (listas de stock y de sedes). Esto incumple el principio 3 de la
constitución, expone más información de la que cada pantalla necesita y acopla lo que ve el
usuario con cómo se guardan los datos. El objetivo es que cada pantalla reciba un modelo
propio, con exactamente lo que muestra o necesita para sus enlaces y acciones, **sin que el
usuario perciba ningún cambio**. Merece la pena porque es la única deuda declarada en la
constitución y sin saldarla el principio 3 no es verificable.

### Definiciones
- **Las cuatro pantallas**: las listadas como deuda del principio 3 de la constitución (detalle de repuesto, de activo, de visita técnica y de herramienta).
- **Línea base**: el comportamiento observable de una pantalla inmediatamente antes de modificarla: respuesta (contenido, estado o redirección), datos, formatos, orden, acciones y contenido disponibles según el rol, mensajes, y errores o avisos en la consola del navegador.
- **Diferencia visible**: cualquier diferencia de texto, valor, orden, estilo o disposición perceptible al comparar línea base y resultado lado a lado, en escritorio (≥ 1280 px) y móvil (375 px y 768 px). No cuenta lo no perceptible (espacios, estructura interna) ni las diferencias debidas exclusivamente al transcurso normal del tiempo o a datos dinámicos ya presentes en la línea base, siempre que su comportamiento siga siendo equivalente.
- **Registro interno del dominio**: objeto que representa un registro guardado del negocio o sus relaciones. Los valores simples (texto, número, fecha, estado) no lo son.
- **Modelo propio**: aquel cuya estructura expone únicamente los valores que requiere la pantalla y no contiene referencias a registros internos del dominio, ni colecciones de ellos, ni propiedades tipadas con dichos registros. Encapsular un registro interno dentro de otro objeto no lo convierte en modelo propio.
- **Búsqueda de verificación**: la definida en la constitución o, en su defecto, la misma búsqueda con la que se identificaron originalmente las cuatro pantallas. No se modifica durante la ejecución de esta spec.
- **Dato suelto**: cualquier dato que la pantalla recibe fuera de su modelo propio.
- **Registro completo**: todos sus campos opcionales con valor y todas sus listas relacionadas con al menos un elemento. **Registro vacío**: todos sus campos opcionales sin valor y sus listas sin elementos.

## Usuarios / actores
- **Administrador**: accede a las cuatro pantallas.
- **Supervisor**: accede a las cuatro pantallas.
- **Soporte**: accede a repuesto, activo y herramienta, y solo a las visitas que tiene asignadas.
- **Usuario (rol sin acceso) y visitante sin sesión válida**: no acceden a ninguna.
- **Equipo de desarrollo**: mantiene el sistema y necesita poder verificar el principio 3.

## Historias de usuario
- H1: Como administrador, supervisor o técnico de soporte quiero ver el detalle de un repuesto, activo, visita o herramienta exactamente como hoy, para no tener que reaprender nada.
- H2: Como equipo de desarrollo quiero que cada pantalla reciba todo lo que muestra en un único modelo propio, para poder cambiar el almacenamiento sin tocar lo que ve el usuario.
- H3: Como responsable del proyecto quiero poder comprobar, con la búsqueda de verificación (fija durante toda la spec) y una revisión manual, que ninguna pantalla depende de registros internos ni recibe datos sueltos, para dar por saldada la deuda.

## Requisitos funcionales (criterios de aceptación en EARS)
- RF-1: CUANDO un usuario con permiso abre el detalle de un registro existente, EL SISTEMA lo mostrará sin diferencia visible respecto de la línea base, incluido el contenido y las acciones disponibles para su rol.
- RF-2: CUANDO un usuario abre el detalle de un registro que no existe (identificador inexistente, cero, negativo o no numérico), EL SISTEMA dará la misma respuesta que la línea base.
- RF-3: SI un técnico de Soporte abre una visita que no tiene asignada, ENTONCES EL SISTEMA denegará el acceso con la misma respuesta que la línea base.
- RF-4: CUANDO un Administrador, un Supervisor o un técnico de Soporte abre el detalle de una herramienta, EL SISTEMA permitirá el acceso igual que la línea base, sin añadir ni quitar restricciones por rol: Administrador y Supervisor ven además las acciones de editar y de asignar o devolver; Soporte no.
- RF-5: SI el usuario no tiene sesión válida (inexistente, caducada o revocada), está desactivado o su rol no tiene permiso sobre la pantalla, ENTONCES EL SISTEMA dará la misma respuesta que la línea base (p. ej. redirección al inicio de sesión).
- RF-6: CUANDO coinciden varias condiciones de rechazo (p. ej. un técnico de Soporte pide una visita que no existe), EL SISTEMA aplicará el mismo orden de comprobación que la línea base.
- RF-7: CUANDO un dato opcional del registro está vacío, EL SISTEMA mostrará la pantalla sin errores nuevos y sin diferencia visible respecto de la línea base para ese caso. Los campos opcionales de cada pantalla son los que la línea base ya trata como opcionales y se anotan en el inventario previo.
- RF-8: SI una relación está incompleta, un registro relacionado no existe o un usuario asociado está desactivado o eliminado, ENTONCES EL SISTEMA conservará el comportamiento de la línea base para ese caso, incluso si hoy produce un error, sin añadir errores nuevos.
- RF-9: EL SISTEMA entregará a cada una de las cuatro pantallas todo lo que muestra o necesita para sus enlaces y acciones dentro de un único modelo propio (según la definición), sin datos sueltos (incluidas las listas de stock y de sedes y cualquier otro dato suelto que revele el inventario previo de esa pantalla).
- RF-10: EL SISTEMA no hará depender ninguna de las cuatro pantallas de registros internos del dominio, ni en su modelo ni en datos sueltos. Encapsular un registro interno dentro de otro objeto no cumple este requisito.
- RF-11: CUANDO el usuario usa las acciones que la línea base ofrece en la pantalla (editar, ajustar stock, asignar o devolver, cambiar estado, enlaces a otras pantallas) o recorre los flujos que terminan en ella (volver tras crear, editar o ejecutar una acción, recargar, volver atrás), EL SISTEMA se comportará como en la línea base, también ante acciones simultáneas de varios usuarios.
- RF-12: MIENTRAS convivan pantallas ya migradas y no migradas, EL SISTEMA mantendrá los enlaces entre ellas funcionando como en la línea base.
- RF-13: SI se detecta un defecto preexistente durante la migración de una pantalla (dato mal formateado, enlace roto, error de contenido o comportamiento incorrecto), ENTONCES EL SISTEMA lo mantendrá sin corregirlo y el defecto se registrará como deuda o incidencia independiente. Única excepción: si el defecto impide cumplir RF-9 o RF-10 en esa pantalla, se aplica el cambio mínimo imprescindible y se documenta en la incidencia y en la checklist.
- RF-14: SI el inventario previo de una pantalla muestra que ya cumple total o parcialmente RF-9 y RF-10, ENTONCES EL SISTEMA conservará esa implementación y solo se completarán los requisitos pendientes, sin dispensar a la pantalla de superar todas las verificaciones de esta spec.

## Requisitos no funcionales
- **Equivalencia**: ningún texto, dato, orden de listas ni formato (fechas, importes) cambia. Se verifica en el mismo entorno antes y después (misma zona horaria y misma configuración regional del servidor). No es incumplimiento lo que cambie solo por el transcurso normal del tiempo o por datos dinámicos ya presentes en la línea base, mientras su comportamiento siga siendo equivalente.
- **Seguridad y datos del usuario** (principio 5): cada pantalla recibe únicamente los datos que muestra o necesita; no se añaden datos personales nuevos a ninguna respuesta. Las pruebas usan datos ficticios, y ni los registros de actividad, ni la checklist, ni las capturas contienen datos personales. En cada entrega se revisan los registros de actividad nuevos o modificados.
- **Comprobaciones manuales de seguridad** (principio 4), para cada pantalla a la que aplique:
  - S1: con Soporte, una visita asignada se muestra, una ajena se deniega y una inexistente da "no encontrado".
  - S2: con Administrador y con Supervisor, la herramienta se muestra con las acciones de editar y asignar o devolver; con Soporte se muestra sin acciones; con Usuario y sin sesión se rechaza.
  - S3: sin sesión, las cuatro pantallas redirigen igual que la línea base.
  - S4: con sesión revocada o usuario desactivado, el rechazo ocurre en la siguiente petición, igual que la línea base.
  - S5: la respuesta no contiene datos que la línea base no mostraba.
- **Idioma** (principio 6): todos los textos visibles siguen en español.
- **Compatibilidad**: se mantiene el comportamiento en escritorio y en móvil (anchos de la definición de diferencia visible).
- **Rendimiento**: la apertura de cada pantalla se valida manualmente por equivalencia perceptiva, sin umbrales numéricos, probándola con el registro con más elementos relacionados disponible.
- **Dependencias** (principio 1): no se añade ningún paquete; si resultara imprescindible, requiere justificación escrita en la memoria del proyecto antes de añadirlo.
- **Lógica e interfaz** (principio 3): el cambio no traslada reglas de negocio a la interfaz.

## Casos límite
- Registro completo y registro vacío (definidos arriba) en cada pantalla.
- Registro con muchos elementos relacionados (movimientos, consumos, asignaciones): el orden y el contenido no cambian.
- Registro relacionado que ya no existe, relación incompleta, o técnico/asignado desactivado o eliminado (RF-8).
- Valor de estado desconocido o no previsto: se muestra como en la línea base.
- Textos con acentos, caracteres especiales, marcas de formato o saltos de línea, y textos muy largos en nombres, descripciones y observaciones.
- Importes y fechas vacíos o extremos (precio cero, fechas muy antiguas), y fechas mostradas en hora local con el mismo resultado que la línea base.
- Identificador inexistente, cero, negativo o no numérico (RF-2), y combinación de rechazos (RF-6).
- Dos usuarios abriendo el mismo detalle mientras otro lo modifica: se muestra el estado vigente al abrirlo; y acciones simultáneas sobre el mismo registro (ajustar stock, asignar, devolver) con el resultado de la línea base.
- Contenido distinto según el rol dentro de una misma pantalla.
- Recarga o vuelta atrás tras una acción.
- Estado intermedio con pantallas migradas y sin migrar (RF-12).
- Pantalla que ya cumple total o parcialmente RF-9 y RF-10 antes de empezar (RF-14).
- Datos que cambian solos entre línea base y resultado (fechas relativas, contadores, "hace X", orden por fecha actual): se comparan por su comportamiento, no por el valor puntual.

## Fuera de alcance
- Rediseñar o mejorar visualmente las pantallas.
- Corregir defectos preexistentes detectados al migrar (salvo la excepción de RF-13): se registran como deuda o incidencia independiente.
- Corregir errores derivados de inconsistencias de datos (relaciones incompletas o registros relacionados inexistentes): requieren una incidencia o spec independiente.
- Auditar o corregir otras pantallas, formularios o respuestas que puedan exponer registros internos, incluidas las listas que una pantalla pide por su cuenta desde el navegador (técnicos, áreas por sede, repuestos): no se modifican.
- Cambiar permisos, roles o reglas de acceso.
- Optimizar el rendimiento más allá de no empeorarlo.
- Añadir tests automatizados o frameworks de test.

## Criterios de finalización
Se entrega **pantalla a pantalla**: cada una en un commit independiente que referencia esta spec, y el orden de implementación es libre. Una pantalla se da por terminada cuando:
- Todos los RF aplicables se cumplen.
- La construcción del proyecto termina sin errores.
- Antes de tocarla se anotó en la checklist su inventario: datos sueltos, campos opcionales, y acciones y contenido por rol.
- Se verificó a mano con Chrome DevTools, comparando línea base y resultado con un registro completo y otro vacío, con cada rol que tiene acceso, con la consola sin errores nuevos respecto de la línea base y en escritorio y móvil, sin diferencias visibles.
- Se ejecutaron las comprobaciones de seguridad S1–S5 que le aplican.
- La checklist de verificación acompaña el commit de entrega (obligatoria en el commit, pero no es un archivo obligatorio del repositorio); las capturas de la línea base se recomiendan pero no son entregable obligatorio.
- Los defectos preexistentes encontrados quedan registrados como deuda o incidencia independiente.
- La búsqueda de verificación no encuentra esa pantalla y una revisión manual confirma que su modelo es propio (según la definición) y que no recibe datos sueltos.
- Si el inventario previo mostró cumplimiento parcial, solo se completó lo pendiente (RF-14).
- Se marca su casilla en el registro de avance de esta spec.

### Registro de avance
- [ ] Detalle de repuesto
- [ ] Detalle de activo
- [ ] Detalle de visita técnica
- [ ] Detalle de herramienta

La spec se cierra cuando las cuatro casillas están marcadas, la búsqueda de verificación da 0 resultados, se retira la deuda de la constitución y se actualiza la memoria del proyecto.

## Dudas abiertas
Ninguna. Aclaradas por el responsable en la rev. 3: definición de modelo propio, búsqueda de verificación fija, tolerancia por tiempo y datos dinámicos, y conservación de lo ya cumplido (RF-14). Supuestos de la revisión QA, confirmados por el responsable:
- Las pruebas visuales se realizan en 375 px, 768 px y 1280 px o superior.
- Cada pantalla se entrega en un commit independiente; el orden de implementación es libre.
- El rendimiento se valida manualmente por equivalencia perceptiva, sin umbrales numéricos.
- La checklist de verificación acompaña el commit de entrega y no forma parte obligatoria del repositorio.
