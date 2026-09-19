# SistemaTickets — Guía para Defensa de Proyecto de Grado

> **Universidad Mayor de San Andrés (UMSA)** — Carrera de Informática
> **Proyecto de Grado**
> Documento de apoyo para la sustentación ante tribunal y audiencia.

---

## 1. ¿Qué es SistemaTickets?

**SistemaTickets** es una aplicación web para la **gestión de tickets de mesa de ayuda** (help desk) con un **módulo integrado de trabajo de campo**: visitas técnicas, préstamo de herramientas y consumo de repuestos con control de inventario.

Implementa un **flujo de vida completo** del ticket:

1. Un **Usuario** crea un ticket (categoría, prioridad, descripción y adjuntos).
2. Un **Soporte** toma el ticket nuevo, lo atiende, agrega comentarios y puede cerrarlo.
3. Un **Supervisor** supervisa y gestiona la configuración (estados, prioridades, categorías, sedes, áreas).
4. Un **Administrador** gestiona usuarios y toda la configuración del sistema.

Todo acción relevante queda registrada en el **historial del ticket** y genera **notificaciones por correo**.

---

## 2. Stack tecnológico (y por qué)

| Componente | Tecnología | Versión | Justificación para la defensa |
|---|---|---|---|
| Framework web | ASP.NET Core 8 (MVC) | 8.0 | Framework moderno, multiplataforma (corre en Azure/Linux), con autenticación y autorización integradas |
| ORM | NHibernate + FluentNHibernate | 5.5.2 / 3.4.1 | ORM maduro con mapeo fluido (clases `*Map.cs`), separa el modelo de objetos del esquema de BD |
| Base de datos | SQL Server / Azure SQL | — | Relacional, robusto, soporta el esquema transaccional del sistema |
| Autenticación | JWT Bearer + Cookie | 8.0.11 | Token para futuras APIs/clientes + cookie para la navegación MVC |
| Hash de contraseñas | BCrypt.Net-Next | 4.0.3 | Algoritmo con *salt* integrado, estándar de facto para almacenar contraseñas |
| Archivos adjuntos | Azure Blob Storage | 12.28.0 | Almacenamiento escalable en la nube para los adjuntos de tickets |
| Correo | SMTP (configurable, p. ej. Gmail) | — | Notificaciones de asignación/cambio de estado |
| Reportes PDF | QuestPDF | 2024.12.0 | Generación de documentos PDF |
| Reportes Excel | ClosedXML | 0.102.3 | Exportación a .xlsx |

**Pregunta probable:** *"¿Por qué NHibernate y no Entity Framework?"*
→ Respuesta sugerida: ambos son ORMs válidos; NHibernate es maduro, multiplataforma y con FluentNHibernate los mapeos quedan explícitos en código (una clase `Map` por entidad, en `Infrastructure/Persistence/Maps/`), lo que da control fino sobre columnas, claves foráneas y cascadas. Además demuestra dominio de un patrón de mapeo objeto-relacional distinto al "por defecto" de .NET.

**Pregunta probable:** *"¿Por qué MVC y no una API + SPA?"*
→ MVC con Razor sirve HTML directamente en el servidor: menos complejidad, mejor para el alcance del proyecto, y el JWT deja abierta la puerta a clientes móviles/API en el futuro (de hecho el pipeline ya acepta `Authorization: Bearer`).

---

## 3. Arquitectura

El proyecto sigue una **arquitectura en capas** con **inversión de dependencias** (la capa de dominio define interfaces; la infraestructura las implementa):

```
┌──────────────────────────────────────────────┐
│                  NAVEGADOR                    │
│        (Razor Views + JS en wwwroot)          │
└──────────────────────┬───────────────────────┘
                       │
┌──────────────────────▼───────────────────────┐
│              Controllers/ (MVC)               │
│ TicketsController, UsersController, etc.      │
│  → reciben HTTP, validan roles, llaman a      │
│    servicios, devuelven View() o Json()       │
└──────────────────────┬───────────────────────┘
                       │ (depende de interfaces)
┌──────────────────────▼───────────────────────┐
│              Domain/                          │
│  Entities/   → 19 entidades POCO              │
│  Enums/      → enums del dominio              │
│  Repositories/ → interfaces (ITicketRepository…)│
│  Services/   → interfaces (ITicketService…)   │
│                  + lógica de negocio          │
└──────────────────────┬───────────────────────┘
                       │ (implementado por)
┌──────────────────────▼───────────────────────┐
│           Infrastructure/                     │
│  Persistence/       → NHibernateBootstrap,    │
│    Repositories/      implementaciones con    │
│    Maps/              FluentNHibernate        │
│  Security/          → helper JWT              │
│  Services/          → Email, Azure Blob,      │
│                       Reportes, servicios      │
└──────────────────────┬───────────────────────┘
                       │
        ┌──────────────▼───────────────┐
        │  SQL Server / Azure SQL       │
        │  Azure Blob Storage           │
        │  SMTP                         │
        └───────────────────────────────┘
```

**Puntos clave que el tribunal puede pedir explicar:**

- **Patrón Repositorio**: las interfaces están en `Domain/Repositories/` y las implementaciones (con NHibernate) en `Infrastructure/Persistence/Repositories/`. Los servicios de dominio NO conocen SQL.
- **Inyección de dependencias**: todo el cableado está en `Program.cs` (`builder.Services.AddScoped<IUsuarioService, UsuarioService>()`, etc.). ASP.NET Core inyecta las dependencias por constructor.
- **ViewModels**: cada vista recibe un modelo específico definido en `Models/` (por ejemplo `TicketIndexViewModel` con paginación y filtros), nunca las entidades directas.
- **Program.cs**:`). Es el punto donde se ensambla todo: MVC, NHibernate, DI, JWT, pipeline de middleware (HTTPS → archivos estáticos → routing → Authentication → Authorization → páginas de estado 401/403 → rutas).

---

## 4. Módulos del sistema

| Módulo | Controladores | Funcionalidad principal |
|---|---|---|
| **Tickets** | `TicketsController` (499 líneas) | Crear/listar/paginar tickets, tomar tickets nuevos, comentarios, historial, adjuntos (máx. 10 MB), notificación por email |
| **Usuarios** | `UsersController` | Login (JWT), gestión de usuarios, roles |
| **Dashboard** | `DashboardController` | Métricas y resumen por rol |
| **Inventario** | `InventarioController` | Herramientas, repuestos, stock, movimientos, asignación a técnicos |
| **Campo** | `CampoController` | Visitas técnicas, activos, consumo de repuestos en sitio |
| **Configuración** | `ConfiguracionController` | Estados, prioridades, categorías, sedes, áreas (catálogos) |
| **Reportes** | `ReportesController` | Exportación PDF (QuestPDF) y Excel (ClosedXML) |
| **Supervisor** | `SupervisorController` | Vista de supervision |

---

## 5. Seguridad (crítico para la defensa)

**Flujo de autenticación:**
1. El usuario envía credenciales a `/Users/Login`.
2. `UsuarioService` verifica el hash **BCrypt**.
3. Se genera un **JWT** (claims: id de usuario, nombre, rol) firmado con la clave de `appsettings.json` (`Jwt:Key`).
4. El token se guarda en una cookie `jwt`.

**Autorización:**
- Decoradores `[Authorize]` y `[Authorize(Roles = "Administrador,Supervisor,Soporte")]` por controlador/acción.
- 4 roles: **Administrador**, **Supervisor**, **Soporte**, **Usuario** (cada uno ve solo sus tickets: un "Usuario" ve los que creó, un "Soporte" los que le asignaron).

**Medidas concretas (memorizar para exposición):**
- Contraseñas con **BCrypt** (nunca texto plano).
- **Anti-forgery**: `[ValidateAntiForgeryToken]` en todos los POST (protección CSRF).
- **Validación de tamaño** de adjuntos (10 MB máximo) antes de subir a Azure.
- Redirección a login en 401/403 para peticiones HTML (middleware `UseStatusCodePages` en `Program.cs`).
- `RequireHttpsMetadata` y HSTS en producción.

**Pregunta probable:** *"¿Por qué JWT **y** cookie?"*
→ El header `Authorization: Bearer` tiene prioridad (para clientes API); si no viene, se lee el token desde la cookie `jwt` (para la navegación web con Razor). Está implementado en el evento `OnMessageReceived` de `Program.cs`.

---

## 6. Base de datos

- **19 tablas de dominio** (entidades en `Domain/Entities/`): Ticket y sus catálogos (Estado, Prioridad, Categoría), comentarios, adjuntos, historial, Usuario, Sede, Área, Activo/ActivoTipo, Herramienta, AsignaciónHerramienta, Repuesto, StockRepuesto, InventarioMovimiento, ConsumoRepuesto, VisitaTécnica.
- Esquema en `ScriptsDB/`:
  - `users.sql` → usuarios y roles
  - `tickets.sql` → tickets + catálogos + adjuntos + historial
  - `inventario.sql` → herramientas, movimientos, stock
  - `campo_repuestos.sql` → visitas técnicas y consumo de repuestos
  - `alter_tickets_prioridad_nullable.sql` → migración puntual

**Pregunta probable:** *"¿Cómo se crea el esquema?"*
→ Dos opciones: (a) manual con los scripts de `ScriptsDB/`, o (b) automática con `SchemaUpdate` de NHibernate activando `NHibernate:UpdateSchema=true` **solo en el primer deploy** (debe pasarse a `false` después, para evitar migraciones accidentales en producción).

---

## 7. Cómo ejecutar el proyecto

```bash
dotnet restore SistemaTickets.sln
dotnet run --project SistemaTickets/SistemaTickets.csproj
```

Requisitos previos:
1. SQL Server accesible y la cadena de conexión en `SistemaTickets/appsettings.json` (`DefaultConnection`).
2. Ejecutar los scripts de `ScriptsDB/` en orden (users → tickets → inventario → campo_repuestos), **o** activar `NHibernate:UpdateSchema = true` en el primer arranque.
3. Credenciales de Azure Blob y SMTP en configuración (ver `AzureBlobStorage:*` y `Email:*`).
4. Al abrir el navegador redirige a `/Users/Login` (ruta por defecto `{controller=Users}/{action=Login}`).

---

## 8. Posibles preguntas del tribunal / audiencia

### Generales
1. **¿Cuál es el problema que resuelve?** → Digitalizar y auditar el flujo de tickets de soporte técnico, eliminando el caos de solicitudes por correo/verbal, y conectarlo con el recurso técnico en campo (herramientas, repuestos).
2. **¿Qué metodología usaron?** → Modular por features (Views/Models por módulo), arquitectura en capas, desarrollo iterativo.
3. **¿Qué tecnologías usaste y por qué?** → Ver sección 2 (tiene respuestas listas).

### Arquitectura / código
4. **¿Por qué separar Domain e Infrastructure?** → Para separar responsabilidades: el dominio contiene entidades, interfaces y reglas de negocio; la infraestructura depende del dominio (nunca al revés). Esto permite cambiar BD/ORM/SMTP sin tocar la lógica de negocio.
5. **¿Qué es la inyección de dependencias y dónde se ve?** → En `Program.cs`; los controladores reciben servicios por constructor (`TicketsController(ITicketService, IUsuarioService, ...)`) y no conocen implementaciones concretas.
6. **¿Qué es un ViewModel y por qué no usas las entidades en las vistas?** → Los ViewModels (`Models/Tickets/...`) exponen solo lo que la vista necesita (paginación, filtros, catálogos) y evitan sobre-posteo (mass assignment).

### Base de datos
7. **¿Cómo se mapea objeto-relación?** → Con clases FluentNHibernate en `Infrastructure/Persistence/Maps/` (una por entidad).
8. **¿Qué pasa si cambia el esquema de la BD?** → Se usan los scripts de `ScriptsDB/` para bases nuevas; `SchemaUpdate` sólo para el primer deploy; para cambios posteriores, scripts alter manuales.
9. **¿Qué integridad referencial existe?** → Claves foráneas definidas en los mappings (ticket → usuario, comentario → ticket, consumo → repuesto/visita, etc.).

### Seguridad
10. **¿Cómo se almacenan las contraseñas?** → BCrypt con salt, nunca en texto plano.
11. **¿Qué es CSRF y cómo lo evitas?** → Falsificación de peticiones entre sitios; se evita con el token anti-forgery en cada POST.
12. **¿Un Soporte puede ver tickets de otros?** → No por defecto: el filtro de `TicketsController.Index` asigna `asignadoAId = userId` cuando el rol es Soporte y `creadoPorId` cuando es Usuario.
13. **¿Qué pasa con un token JWT robado?** → Caduca (`ValidateLifetime`, `ClockSkew = TimeSpan.Zero`); como mejora futura, revocación/refresh.

### Operación / despliegue
14. **¿Dónde corre?** → Local con `dotnet run` o publicada en Azure (connection string apunta a Azure SQL; adjuntos en Azure Blob).
15. **¿Cómo se envían los correos?** → SMTP configurable (`Email:*`), llamada asíncrona desde los servicios al cambiar estado/asignación.
16. **¿Qué pasa si Azure Blob o SMTP están caídos?** → El ticket se crea igual (el fallo no bloquea la operación principal); los servicios de correo/almacenamiento están aislados tras interfaces.

### Escenario hipotético ("¿qué pasaría si...?")
17. **¿Cómo escalarías?** → Ver sección 9 (trabajo futuro).
18. **¿Qué harías distinto si empezaras de nuevo?** → Respuesta honesta sugerida: agregaría la capa de pruebas desde el inicio (no hay suite de tests) y extraería una API REST formal si se necesita app móvil.

---

## 9. Limitaciones actuales y trabajo futuro

Ser **honesto con el tribunal genera confianza**. Estas son las limitaciones conocidas y cómo presentarlas como "hoja de ruta":

**Limitaciones que se verán en el código:**
- **Sin suite de pruebas automáticas** (no hay proyectos de test). Verificación manual + ejecución real.
- **Sin notificaciones en tiempo real** (sin SignalR/WebSockets): recarga manual o email.
- **JWT sin refresh/revocación** con lista negra.
- **Sin 2FA** ni bloqueo por intentos fallidos (existe hash BCrypt, pero no rate limiting).
- **Swagger/API docs**: no hay API REST formal documentada (OpenAPI).
- **CI/CD**: no hay pipeline (GitHub Actions / Azure DevOps) definido en el repo.
- Auditoría de configuración (quién cambió un estado/prioridad) implícita por historial de tickets, no generalizada a catálogos.

**Trabajo futuro propuesto:**
1. **Pruebas unitarias y de integración** (xUnit + integración con SQLite/EF provider de NHibernate o testcontainers con SQL Server).
2. **Notificaciones en tiempo real** con SignalR (panel del soporte actualizado al instante).
3. **App móvil o portal cliente** consumiendo una capa **API REST** (el JWT ya está preparado para Bearer).
4. **SLA y métricas** por categoría/prioridad (tiempos de primera respuesta y cierre).
5. **Base de conocimientos** (artículos con soluciones frecuentes para auto-atención del usuario).
6. **Rate limiting + bloqueo de cuenta** por intentos fallidos y 2FA (TOTP).
7. **CI/CD** con GitHub Actions (build + lint + (futuras) tests + deploy a Azure).
8. **Exportación del inventario** a BI / dashboards adicionales.

---

## 10. Datos rápidos para recordar (chuleta)

| Dato | Valor |
|---|---|
| Framework | ASP.NET Core 8 MVC |
| ORM | NHibernate 5.5 + FluentNHibernate 3.4 |
| BD | SQL Server (MsSql2012Dialect) / Azure SQL |
| Auth | JWT Bearer + cookie `jwt`, roles: Admin, Supervisor, Soporte, Usuario |
| Hash de contraseña | BCrypt |
| Adjuntos | Azure Blob (máx. 10 MB) |
| Reportes | QuestPDF (PDF), ClosedXML (Excel) |
| Entidades | 19 |
| Controladores | 9 |
| Ruta por defecto | `/Users/Login` |
| Scripts BD | `ScriptsDB/` (users, tickets, inventario, campo_repuestos) |
| Primer deploy | `NHibernate:UpdateSchema=true` → luego `false` |

---

*Documento generado para apoyar la sustentación. Cualquier detalle técnico adicional está en el propio código fuente y en los comentarios de `Program.cs`.*
