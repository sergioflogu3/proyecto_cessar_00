# Plan 002 — Botones de acción solo con icono

Estado: aprobado

## Archivos y responsabilidades
Solo vistas Razor en `SistemaTickets/Views/`. Sin cambios en controladores, servicios, ViewModels, JS externo ni CSS (salvo lo indicado en D3). RF cubiertos: todos.

| Vista | Botones | RF |
|---|---|---|
| Tickets/Index.cshtml | Ver | RF-1..3, 5 |
| Tickets/Nuevos.cshtml | Tomar ticket (`btn-tomar`) | RF-1..7 |
| Tickets/Details.cshtml | Editar, Cerrar ticket (`btnCerrar`) | RF-1..7 |
| Campo/Visitas.cshtml | Ver | RF-1..3 |
| Campo/Repuestos.cshtml | Ver | RF-1..3 |
| Campo/VisitaDetails.cshtml | Editar | RF-1..3 |
| Campo/RepuestoDetails.cshtml | Editar, Ajustar Stock (`btnAjustarStock`) | RF-1..7 |
| Inventario/Activos.cshtml | Ver, Editar, Dar de baja | RF-1..6 |
| Inventario/Herramientas.cshtml | Ver, Editar, Asignar, Devolver | RF-1..6, 9 |
| Inventario/ActivoDetails.cshtml | Editar, Asignar (`btnAsignarActivo`) | RF-1..7 |
| Inventario/HerramientaDetails.cshtml | Editar, Asignar (`btnAsignarH`), Devolver (`btnDevolver`) | RF-1..7 |

Referencia: `Users/Index.cshtml` (`btn btn-outline-* btn-sm`, `title`, `<i class="fa-solid fa-...">`).

## Funciones puras
No aplica: no hay lógica nueva (solo marcado). No se usa "hoy".

## Patrón de marcado
```
<a|button class="btn btn-outline-<color> btn-sm [ms-1]" title="<Texto anterior>" aria-label="<Texto anterior>" ...atributos y handlers sin tocar...>
    <i class="fa-solid fa-<icono>" aria-hidden="true"></i>
</a|button>
```
Tabla de iconos (RF-3, RF-4):
- Ver: `fa-eye`, outline-primary. Editar: `fa-pen`, conserva el color actual (primary en detalles de ticket/activo, secondary en el resto; unificar a outline-primary como en usuarios: ver D2).
- Cerrar ticket: `fa-lock`, outline-danger. Dar de baja: `fa-box-archive`, outline-danger.
- Tomar ticket: `fa-hand`, success (hoy sólido: pasa a outline-success). Asignar: `fa-user-plus`, success/primary según vista actual en contorno. Devolver: `fa-rotate-left`, outline-warning. Ajustar stock: `fa-sliders`, outline-success.

## Algoritmo (por tarea)
1. Localizar el botón en la vista; cambiar el texto interior por el `<i>`; añadir `title` y `aria-label`.
2. No tocar `id`, `onclick`, `data-*`, `asp-*` ni condiciones `@if`.
3. Pasar clases sólidas (`btn-success`, `btn-warning`, `btn-primary`) a su variante `btn-outline-*` en las cabeceras de detalle para igualar a usuarios.
4. Comprobar que el botón no cambie su texto desde JS (verificado: ninguno de los 21; solo `btnConfirmarTomar` del modal, que no se toca).

## Interfaz
Ver el patrón anterior. Foco visible y activación por teclado ya los aporta `btn` (RF-10).

## Decisiones
- D1. `title` + `aria-label` en cada botón. Alternativa descartada: texto oculto `visually-hidden`, más verboso y no da tooltip.
- D2. Unificar Editar a un solo color (outline-primary) y mismo icono. Alternativa descartada: conservar el color actual por vista (hoy mezcla primary y secondary, justo la inconsistencia que se quiere eliminar). Si el usuario prefiere conservar colores, solo cambia el color en las tareas.
- D3. Sin CSS nuevo; solo clases Bootstrap existentes. Alternativa descartada: una clase `btn-icono` propia (más código sin necesidad). Si en 375 px la fila de herramientas desborda, se evaluará `flex-nowrap` en el contenedor existente.
- D4. Iconos de Font Awesome ya incluido (`wwwroot/lib`). Comprobar en la tarea T1 que cada nombre de icono existe en esa versión; si no, elegir otro equivalente. Alternativa descartada: añadir otra librería (viola principio 1).
- D5. Sin migrar a un parcial compartido de botón-icono: solo 21 usos y distintos tipos (enlace/botón); un helper añade complejidad.

## Estrategia de pruebas
Principio 4: no hay `dotnet test` ni proyecto de tests. Verificación:
1. `dotnet build SistemaTickets.sln` limpio.
2. Búsqueda de verificación (RF-1, CF-1): grep multilínea sobre `Views/{Tickets,Campo,Inventario}` de `btn btn-(outline-)?(primary|secondary|success|danger|warning)[^>]*>\s*[A-Za-zÁ-ú]` en botones de acción, sin resultados salvo los de RF-8.
3. grep de `title=` y `aria-label=` en los 21 botones (RF-2) y de los ids intactos (RF-7: `btnCerrar|btn-tomar|btnAjustarStock|btnAsignarActivo|btnAsignarH|btnDevolver`).
4. Chrome DevTools en escritorio y 375x800, por rol (Administrador, Supervisor, Soporte): ver, editar, cerrar ticket, tomar, asignar/devolver, ajustar stock, dar de baja; consola limpia; árbol de accesibilidad con nombre correcto; sin desbordes.
