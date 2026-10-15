# Tareas 002 — Botones de acción solo con icono

Estado: aprobado. Una tarea cada vez; al terminar cada una: `dotnet build`, verificación en navegador (escritorio y 375x800) y parar.

- [x] **T1. Tickets/Index (Ver) y Campo/Visitas, Campo/Repuestos (Ver); confirmar que los iconos existen en la versión de Font Awesome incluida.** RF-1, RF-2, RF-3, RF-5
- Hecho cuando: los 3 botones "Ver" son `fa-eye` con `title`/`aria-label` "Ver", navegan al detalle correcto, y los iconos de la tabla del plan existen en la librería (si alguno no, se anota el sustituto en el plan).

- [x] **T2. Tickets/Nuevos: Tomar ticket.** RF-1, RF-2, RF-4, RF-5, RF-7
- Hecho cuando: el botón `btn-tomar` es solo icono, abre el modal con el título del ticket y "Confirmar" sigue funcionando y retira la fila.

- [x] **T3. Tickets/Details: Editar y Cerrar ticket.** RF-1..RF-7
- Hecho cuando: ambos son solo icono, `btnCerrar` abre el modal de cierre y cierra el ticket, y Editar solo aparece con los permisos de antes.

- [x] **T4. Campo/VisitaDetails y Campo/RepuestoDetails: Editar y Ajustar Stock.** RF-1..RF-7
- Hecho cuando: los 3 botones son solo icono, `btnAjustarStock` abre el modal y guarda el ajuste, y no aparece sin sedes.

- [x] **T5. Inventario/Activos: Ver, Editar, Dar de baja.** RF-1..RF-6
- Hecho cuando: los 3 son solo icono, la baja abre el modal de confirmación y no aparece en activos ya de baja.

- [x] **T6. Inventario/Herramientas: Ver, Editar, Asignar, Devolver.** RF-1..RF-6, RF-9
- Hecho cuando: los 4 son solo icono, Asignar/Devolver abren sus modales según el estado, y la fila no se desborda en 375 px.

- [x] **T7. Inventario/ActivoDetails: Editar y Asignar.** RF-1..RF-7
- Hecho cuando: ambos son solo icono y `btnAsignarActivo` abre el modal y asigna.

- [x] **T8. Inventario/HerramientaDetails: Editar, Asignar y Devolver.** RF-1..RF-7
- Hecho cuando: los 3 son solo icono y `btnAsignarH` y `btnDevolver` siguen funcionando según el estado.

- [x] **T9. Revisión global y cierre.** RF-1..RF-10, CF-1..CF-5
- Hecho cuando: la búsqueda de verificación da 0 botones de acción con texto, los 21 tienen `title`/`aria-label`, los ids siguen presentes, el build es limpio y la checklist de navegador por rol (escritorio y móvil, consola limpia) queda en el commit; la spec pasa a "implementada".

## Verificación T9 (15/10/2026)
- `dotnet build` limpio; grep de botones de acción con texto: 0 (solo quedan títulos y botones de confirmación de modales, fuera de alcance).
- Los 21 botones tienen `title`/`aria-label` e icono correcto; ids de JS intactos.
- Navegador (rol Administrador, escritorio): modales de Asignar, Devolver, Ajustar stock, Cerrar ticket y Dar de baja abren bien. Móvil 375x800: sin desborde de página; la tabla de herramientas desplaza dentro de su contenedor. Consola sin errores propios.
- No verificado: "Tomar ticket" (no había tickets nuevos), roles Supervisor/Soporte, y las acciones de confirmar (no se ejecutaron para no modificar datos).
