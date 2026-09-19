# Plan de Mejora Arquitectónica — SistemaTickets

> Objetivo: evolucionar el proyecto hacia una arquitectura escalable y mantenible **sin romper la funcionalidad actual**.
> Principio rector: cambios incrementales ("strangler fig"), cada fase entrega valor y deja el sistema funcionando.
> Complementa: `problemas-de-seguridad.md` (hallazgos C1-C3, A1-A5) y `ci-cd-plan.md`.

---

## 1. Diagnóstico del estado actual

**Lo que ya está bien (mantener y presumir en la defensa):**
- Separación en capas: `Controllers/` → `Domain/` (entidades, interfaces, servicios de negocio) → `Infrastructure/` (NHibernate, Azure, Email).
- Interfaces en Domain, implementaciones en Infrastructure (inversión de dependencias en lo esencial).
- ViewModels por feature, sesiones NHibernate por request vía DI (`OpenSession` scoped).
- Configuración centralizada en `Program.cs`, atributos de seguridad por acción.

**Deudas que limitan escalabilidad/mantenimiento:**
1. **SessionFactory como singleton + sesión por request compartida implícitamente** en todos los repositorios de un request: funcional hoy, pero no hay unidades de trabajo explícitas ni transacciones encapsuladas en servicios.
2. **Servicios de dominio orquestan repos + email + storage mezclados** (ej. `TicketService` probablemente envía correos directamente) → difícil testear y reciclar.
3. **Sin pruebas de ningún tipo** → cualquier refactor es riesgosos a ciegas.
4. **Consultas y filtros duplicados** entre servicios y controladores (paginación, filtros por rol).
5. **ViewModels y mapeo manual** entidad↔VM disperso (riesgo de over-posting ya mitigado parcialmente, pero por repetición manual).
6. **`Program.cs` monolítico**: todo el cableado DI en un método de 150 líneas.
7. **Sin abstracción de transacciones ni manejo de concurrencia** (dos soportes tomando el mismo ticket puede sufrir race conditions si es por UPDATE ciego).
8. **Sin gestión de migraciones** de BD (`SchemaUpdate` todo-o-nada).

---

## 2. Estrategia: evolución por fases, sin romper nada

Cada fase es independiente, se puede pausar, y ninguna exige reescribir la app. El orden va de "menos riesgo / más valor inmediato" a "más estructural".

---

### Fase 1 — Redes de seguridad (prerrequisito de todo refactor)

Nada se refactorea sin red. Antes de tocar código:

1. **Proyecto de pruebas `SistemaTickets.Tests`** (xUnit + FluentAssertions):
   - Tests unitarios de servicios de dominio (TicketService: reglas de tomar ticket, cambios de estado, permisos).
   - Tests de integración ligeros con **SQLite in-memory** de NHibernate (los mappings FluentNHibernate sirven para cualquier dialecto) para repos.
   - Meta inicial modesta: cobertura en las 3-4 reglas de negocio más críticas (no 80%).
2. **Contratos estables**: congelar los ViewModels públicos que usan las vistas (son el "contrato") para poder refactorizar por dentro.
3. **CI** ya existe (ver `ci-cd-plan.md`): añadir job `test` apenas nazca el proyecto de tests.

**Criterio de salida:** repositorio con CI verde + primer círculo de tests pasando.

---

### Fase 2 — Ordenar el dominio (lógica de negocio fuera de controladores)

Sonido bajo ("move method"), alto valor:

1. **Extraer lógica de permisos** repetida (`EsAdminOSupervisor`, fingir roles, `PuedeVerTicket`) a un solo lugar:
   - Crear `Domain/Services/ITicketPermissionsService` (o un servicio `AutorizacionService`) y usarlo en controladores y servicios.
   - Beneficio inmediato: una sola definición de "quién puede ver/editar qué" — hoy vivo en N controladores.
2. **Extraer construcción de ViewModels y mapping**:
   - Introducir **Mapster o AutoMapper** SOLO para mapeos ViewModels↔entidades, empezando por los más repetidos (Ticket, Usuario). No es obligatorio usar librería: un `TicketMapper` propio también cumple.
3. **DTO de salida para detalles de ticket** para evitar exponer entidades en JSON (`GetForEdit` ya devuelve DTO inline — generalizar ese patrón).
4. **Unificar el patrón de paginación/filtros**: un pequeño `PagedRequest`/`PagedResult<T>` genérico en Domain en lugar de parámetros sueltos en cada `GetPagedAsync`.

**Riesgo: bajo. Tests de Fase 1 protegen el movimiento.**

---

### Fase 3 — Transacciones y concurrencia (corrección bajo carga)

1. **Unidad de trabajo explícita:**
   - `IUnitOfWork` en Domain (`Infrastructure` implementa con `ITransaction` de NHibernate).
   - Los servicios de negocio abren/cierran transacción; los repos dejan de hacer `session.Flush()` a lo disperso.
   - Migrar servicio por servicio empezando por `TicketService` (el más crítico).
2. **Concurrencia optimista en `Ticket`:**
   - Columna `RowVersion` (mapping FluentNHibernate `.Version(...)`) para detectar ediciones simultáneas y devolver 409/aviso al usuario.
3. **Regla de negocio de "Tomar ticket" atómica:**
   - `TomarTicketAsync` debe actualizar con `WHERE EstadoId = Nuevo AND AsignadoA IS NULL` y verificar filas afectadas (o lock pesimista en esa operación puntual); hoy puede ser un simple UPDATE que dos soportes corren a la vez.
4. **Aislar efectos secundarios (correo):**
   - Los correos NO deben pixelarse dentro de la transacción de negocio.
   - Introducir `IEventoTicket`/`IDomainEventDispatcher` mínimo (un `List<Action<TicketEvento>>` en handler único post-commit) o, más simple: llamar a `IEmailService` DESPUÉS de `Commit()`, con try/catch que no revierte el negocio. (Siguiendo la nota del README: "el fallo del email no bloquea la operación principal" — ahora hacerlo estructural).

**Riesgo: medio. Requiere los tests de Fase 1 para validar reglas de estado.**

---

### Fase 4 — Configuración y opciones tipadas

1. **Options pattern:** reemplazar `IConfiguration.GetString(...)` disperso por clases:
   - `JwtOptions`, `SmtpOptions`, `BlobStorageOptions`, `EmailOptions` con `builder.Services.Configure<X>(builder.Configuration.GetSection(...))`.
   - Fuera placeholders: si `JwtOptions.Key` es el placeholder de fábrica o < 32 chars, la app **falla al arrancar** con mensaje claro (cierra el hallazgo **C1** del plan de seguridad).
2. **Secrets fuera de la config:** variables de entorno / User Secrets en dev, GitHub Environments/Key Vault en despliegue (cierra **C2**).
3. **Feature flags ligeros:** `appsettings.json` → sección `Features:` (p. ej. `NotificacionesEmail`, `Adjuntos`) para activar/desactivar módulos sin recompilar — útil en producción y en la demo de la defensa.

---

### Fase 5 — Composición del arranque y organización de controllers

1. **Partir `Program.cs`** en "extension methods de registro":
   - `ServiceCollectionExtensions.AddPersistence()`, `AddDomainServices()`, `AddAuth()`, `AddInfrastructureServices()`, `AddSwaggerDocs()` (futuro).
   - `Program.cs` queda ~30 líneas legibles: el jurado lee la arquitectura en 10 segundos.
2. **Nombrar un lugar por concepto:**
   - `Models/` ya está por feature; consolidar que Controllers usen SOLO interfaces de Domain (auditar: TicketsController hoy usa ITicketService/IEmailService/IStorageService del Domain — bien; quitar referencias directas a implementaciones de Infrastructure).
3. **Middleware reutilizable:** extraer el `UseStatusCodePages` inline de `Program.cs` a una clase `UnauthorizedRedirectMiddleware` con tests propios.

---

### Fase 6 — Datos: migraciones controladas y catálogos

1. **Abandonar `SchemaUpdate` como mecanismo de evolución:** dejarlo solo para el primer boot (o eliminar).
2. **Adoptar una de:**
   - **Opción A (recomendada por coherencia con NHibernate):** scripts versionados en `ScriptsDB/` con convención `NNN_descripcion.sql` + tabla `SchemaMigrations` aplicada por un pequeño runner en el arranque (o por CI).
   - **Opción B:** usar los scripts como "migrations manuales" documentadas y ejecutadas a mano con checklist (estado actual, formalizado).
3. **Seed de catálogos** (estados, prioridades, roles) como script único idempotente — elimina la dependencia de "inserta estos INSERT a mano tras desplegar".
4. **Índices de cubrimiento** para las consultas reales (ver `TicketRepository`): índice en (`AsignadoAId`, `EstadoId`), (`CreadoPorId`, `FechaCreacion`), full-text o LIKE controlado en búsqueda por título/descripción.

---

### Fase 7 — Escalabilidad operativa (cuando haya usuarios reales)

1. **Cola de correos** (tabla `EmailPendiente` + BackgroundService `IHostedService` que envía/reintenta) → desacopla la latencia del smtp del request HTTP y da reintentos.
2. **Caché de catálogos** (estados/prioridades/categorías cambian poco): `IMemoryCache` con invalidación por tiempo (5-10 min) o invalidate-on-update en ConfiguracionController.
3. **Paginación en el servidor en TODAS las pantallas de listado** (hoy paginado en Tickets/Usuarios; auditar Dashboard/Inventario/Campo).
4. **Salud y observabilidad:**
   - Endpoint `/health` (ASP.NET Core Health Checks: BD, Blob).
   - `ILogger` estructurado ya existe; agregar Serilog (sink consola) cuando se despliegue en contenedor.
5. **Preparación API (opcional, post-defensa):** al necesitar app móvil, crear `Controllers/Api/` con endpoints JSON reutilizando los MISMOS servicios de Domain (la arquitectura ya lo permite — hablar de esto en defensa).

---

## 3. Reglas de oro durante toda la migración

1. **Un PR = un movimiento pequeño** (extraer un servicio; añadir una opción; migrar un catálogo). Nada de "gran refactor de fin de semana".
2. **Los ViewModels existentes no cambian su JSON/firmas** durante las fases 2-5 (son el contrato de las vistas y del frontend JS).
3. **Cada fase termina con CI verde** y, a partir de Fase 1, con tests que respalden lo movido.
4. **Nada diario en producción:** despliegue solo tras fase completada y aprobada.
5. Las cosas nuevas ya entran con la estructura nueva; la vieja se migra por adyacencia, no por mandato.

---

## 4. Roadmap resumido (orden y dependencias)

| Fase | Contenido | Depende de | Riesgo | Valor |
|---|---|---|---|---|
| 1 | Proyecto de tests + CI `test` | CI existente | Bajo | Alto (habilita todo lo demás) |
| 2 | Servicios de permisos, mapeo VM↔entidad, PagedResult | F1 | Bajo | Medio (menos duplicación) |
| 3 | Unit of Work, RowVersion, tomar-ticket atómico, email post-commit | F1 (fuerte) | Medio | Alto (correctitud) |
| 4 | Options pattern, fail-fast de secretos, feature flags | — (independiente) | Bajo | Alto (cierra C1/C2 seguridad) |
| 5 | Program.cs modular, middleware extraído | F4 | Bajo | Medio (legibilidad) |
| 6 | Migraciones versionadas + seed + índices | F1 | Medio | Alto (escala en datos) |
| 7 | Cola de email, caché catálogos, /health, Serilog | F3, F4 | Medio | Alto en operación |

**Sugerencia de orden real para un semestre académico:** 1 → 4 → 2 → 5 → 3 → 6 → 7.

---

## 5. Cómo venderlo en la defensa (kit de frases)

- "La arquitectura ya separa dominio e infraestructura; el plan demuestra *madurez* de esa idea: options pattern, transacciones explícitas y eventos de dominio, en orden de riesgo."
- "Cada fase es demostrable sin romper lo que ya se vio: la función de hoy garantizada por tests es el piso sobre el que crece lo nuevo."
- "El plan NO exige reescribir: es una estrategia de cuello de strangler que permite avanzar manteniendo el sistema vivo."

---

*Documento de planificación (no ejecutable). Cada fase se detallará con tareas concretas cuando se aborde.*
