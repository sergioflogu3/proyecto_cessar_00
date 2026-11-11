# Spec 004 — Adjuntos de tickets guardados en una carpeta del servidor

**Estado:** Aprobada (2026-11-11) · Implementada (2026-11-11): verificada en Docker (`verificacion.md`) y desplegada en MonsterASP

## Contexto y objetivo
Hoy los adjuntos de los tickets solo pueden guardarse en el almacenamiento en la nube
(o en su emulador local). En un hosting sin acceso a ese servicio, como el plan gratuito de
MonsterASP.NET, la aplicación no puede usar el módulo de tickets: ninguna pantalla de
tickets abre, aunque no tenga adjuntos, porque el servicio de almacenamiento se prepara al
entrar en cualquiera de ellas. El objetivo es poder elegir, solo con configuración, guardar
los adjuntos en una carpeta privada del propio servidor, sin cambiar lo que ve el usuario y
sin alterar el comportamiento actual de quien siga usando la nube.

### Definiciones
- **Destino de adjuntos**: dónde se guarda el contenido de los adjuntos. Puede ser **nube**
  (el comportamiento actual) o **carpeta** (una carpeta del servidor).
- **Carpeta de adjuntos**: la carpeta del servidor configurada para el destino carpeta.
- **Carpeta pública**: la carpeta cuyos archivos la aplicación sirve directamente al navegador
  (estilos, scripts, imágenes).
- **Línea base**: el comportamiento de subida y descarga de adjuntos con el destino nube,
  inmediatamente antes de esta spec.

## Usuarios / actores
- **Cualquier usuario con sesión** que crea tickets con adjuntos o descarga los adjuntos de
  los tickets que puede ver (los permisos no cambian).
- **Responsable del despliegue**: elige el destino y la carpeta en la configuración.

## Historias de usuario
- HU-1. Como responsable del despliegue, quiero guardar los adjuntos en una carpeta del
  servidor para poder publicar la aplicación en un hosting sin almacenamiento en la nube.
- HU-2. Como usuario, quiero adjuntar y descargar archivos igual que hoy, sin notar dónde se
  guardan.
- HU-3. Como responsable del despliegue, quiero que la aplicación avise al arrancar si la
  carpeta está mal configurada, en lugar de fallar cuando alguien sube un archivo.

## Requisitos funcionales
- RF-1: EL SISTEMA permitirá elegir por configuración, sin recompilar, el destino de
  adjuntos: nube o carpeta.
- RF-2: SI la configuración no indica destino, ENTONCES EL SISTEMA usará el destino nube, con
  el mismo comportamiento que la línea base (incluido el entorno Docker actual).
- RF-3: MIENTRAS el destino sea carpeta, EL SISTEMA no exigirá la configuración de la nube
  para arrancar ni intentará conectarse a ella.
- RF-4: SI el destino configurado no es "nube" ni "carpeta", ENTONCES EL SISTEMA no arrancará
  y registrará un mensaje que indique el valor recibido y los valores admitidos.
- RF-5: SI el destino es carpeta y no se indica la carpeta de adjuntos, ENTONCES EL SISTEMA no
  arrancará y registrará un mensaje que lo indique.
- RF-6: CUANDO el destino es carpeta, EL SISTEMA interpretará una ruta relativa respecto de la
  carpeta de la aplicación y creará la carpeta de adjuntos al arrancar si no existe.
- RF-7: SI la carpeta de adjuntos no se puede crear o no admite escritura, ENTONCES EL SISTEMA
  no arrancará y registrará un mensaje con la ruta y la causa.
- RF-8: SI la carpeta de adjuntos es la carpeta pública o está dentro de ella, ENTONCES EL
  SISTEMA no arrancará y registrará un mensaje que lo indique.
- RF-9: CUANDO el destino es carpeta y un usuario crea un ticket con adjuntos válidos, EL
  SISTEMA guardará cada contenido dentro de la carpeta de adjuntos, agrupado por ticket, y
  registrará el adjunto igual que en la línea base (nombre original, tipo y tamaño).
- RF-10: CUANDO se adjuntan al mismo ticket dos o más archivos con el mismo nombre, EL SISTEMA
  guardará todos sin sobrescribir ninguno, y cada descarga devolverá su propio contenido con
  su nombre original.
- RF-11: CUANDO el destino es carpeta y un usuario con permiso descarga un adjunto, EL SISTEMA
  lo entregará igual que en la línea base: como descarga (nunca mostrado en el navegador),
  con el nombre original, con el tipo de contenido validado al subirlo y sin permitir que el
  navegador deduzca otro tipo.
- RF-12: SI el contenido de un adjunto registrado no existe en la carpeta de adjuntos (por
  ejemplo, porque se subió con el destino nube), ENTONCES EL SISTEMA responderá "no encontrado"
  sin mostrar rutas del servidor y registrará el adjunto afectado.
- RF-13: EL SISTEMA no leerá ni escribirá nunca fuera de la carpeta de adjuntos, aunque el
  nombre del archivo subido o la ruta registrada en la base de datos contengan separadores de
  carpeta, "..", rutas absolutas o caracteres no válidos para el sistema de archivos. Si una
  ruta registrada apunta fuera, se aplica RF-12.
- RF-14: SI falla la escritura de un adjunto (por ejemplo, disco lleno), ENTONCES EL SISTEMA
  responderá como en la línea base ante un fallo de la nube y no dejará un archivo incompleto
  en la carpeta de adjuntos.
- RF-15: EL SISTEMA aplicará con ambos destinos las mismas validaciones de adjuntos que en la
  línea base: extensiones admitidas, coincidencia del contenido con la extensión y tamaño
  máximo de 10 MB.

## Requisitos no funcionales
- **Seguridad**: la carpeta de adjuntos nunca es accesible por una URL directa; solo se
  descarga a través de la acción con control de permisos (RF-8, RF-11). Los mensajes al
  usuario no exponen rutas del servidor.
- **Datos del usuario** (principio 5): los registros de actividad nuevos no incluyen datos
  personales ni el contenido de los archivos; identifican el adjunto por su número.
- **Dependencias** (principio 1): no se añade ningún paquete.
- **Lógica e interfaz** (principio 3): ninguna pantalla cambia.
- **Idioma** (principio 6): mensajes de arranque y de registro en español.
- **Compatibilidad**: el entorno Docker y cualquier despliegue que no cambie su configuración
  siguen usando la nube sin diferencias.

## Casos límite
- Nombre de archivo con acentos, espacios, caracteres especiales o muy largo.
- Nombre de archivo con "../", "\\", ruta absoluta o nombre reservado de Windows.
- Dos archivos con el mismo nombre en el mismo ticket (RF-10).
- Ruta registrada en la base de datos manipulada para apuntar fuera de la carpeta (RF-13).
- Adjuntos subidos con el destino nube y descargados tras cambiar a carpeta (RF-12).
- Carpeta de adjuntos inexistente, sin permiso de escritura, vacía en la configuración o
  dentro de la carpeta pública (RF-5 a RF-8).
- Disco lleno o cuota del hosting agotada durante la subida (RF-14).
- Ticket creado sin adjuntos con el destino carpeta.

## Fuera de alcance
- Migrar adjuntos existentes entre destinos.
- Copias de seguridad de la carpeta de adjuntos (responsabilidad del hosting o del
  responsable del despliegue).
- Cuotas de espacio propias, antivirus y limpieza de archivos huérfanos.
- Corregir el fallo preexistente con el destino nube al subir dos archivos con el mismo nombre
  (se registra como deuda; RF-10 aplica solo al destino carpeta).
- Cambiar los permisos de quién puede subir o descargar adjuntos.

## Criterios de finalización
- `dotnet build` limpio.
- Comprobación manual con el destino carpeta, en local y en MonsterASP:
  - [ ] Todas las pantallas de tickets abren sin configuración de nube (RF-3).
  - [ ] Crear un ticket con dos adjuntos del mismo nombre y descargarlos: contenido correcto y
        cabeceras de descarga iguales a la línea base (RF-9 a RF-11).
  - [ ] Una URL directa a la carpeta de adjuntos no sirve el archivo (RNF de seguridad).
  - [ ] Ruta registrada manipulada a "../appsettings.json" → "no encontrado" (RF-12, RF-13).
  - [ ] Cada configuración inválida de RF-4, RF-5, RF-7 y RF-8 impide arrancar con su mensaje.
- Comprobación manual con el destino nube (Docker): subir y descargar un adjunto como en la
  línea base (RF-2).
- Documentación de despliegue actualizada (configuración de destino y carpeta).

## Dudas abiertas
- Ninguna. Decisiones tomadas en este borrador, a confirmar al aprobar:
  - El destino por defecto sigue siendo nube (RF-2), para no cambiar Docker ni despliegues
    existentes.
  - Con el destino carpeta, la ruta es obligatoria (RF-5); no se asume una ruta por defecto
    porque depende del hosting (en MonsterASP será `../private/uploads`).
