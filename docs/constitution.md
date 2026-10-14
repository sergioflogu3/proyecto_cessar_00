# Constitución de SistemaTickets

Seis principios innegociables. Si un cambio los rompe, se cambia el cambio, no el principio.

1. **Stack mínimo.** ASP.NET Core 8 MVC + Razor + NHibernate + SQL Server, sin frameworks
   de frontend ni paso de build de JS. Un paquete NuGet nuevo exige justificación escrita
   en `MEMORY.md`. *Verificable:* `git diff` de `SistemaTickets.csproj` y ausencia de `package.json`.
2. **La spec manda.** El comportamiento está descrito en `/specs` (raíz del repo). Se cambia
   primero la spec, o en el mismo commit que el código. Si spec y código discrepan, se corrige
   uno antes de seguir. *Verificable:* todo commit que cambia comportamiento toca `/specs`.
3. **Lógica separada de interfaz.** Las reglas de negocio viven solo en `Infrastructure/Services`;
   los controladores no usan repositorios ni `ISession`; las vistas se enlazan a
   ViewModels, nunca a entidades. *Verificable:* `grep "@model SistemaTickets.Domain" Views`
   debe dar 0.
4. **Tests sin dependencias nuevas.** No se instala ningún framework de tests. Cada cambio
   pasa `dotnet build` sin errores y se verifica en el navegador con Chrome DevTools
   (funcionalidad, consola, vista móvil). Las reglas críticas de seguridad llevan su
   comprobación manual escrita en la spec. *Verificable:* build limpio y checklist en el commit.
5. **Datos del usuario protegidos.** Email y teléfono se guardan cifrados, las contraseñas
   con BCrypt; ningún secreto, token ni dato personal en el repo, en logs ni en
   `MEMORY.md`. *Verificable:* `grep` de secretos y revisión de `_logger.Log*` en cada cambio.
6. **Todo en español.** Dominio, textos de interfaz, mensajes de error, commits y docs
   en español; solo los sufijos técnicos (`Service`, `Repository`, `Controller`) y los
   prefijos de commit (`feat:`, `fix:`) van en inglés. *Verificable:* revisión de textos
   visibles y del `git log`.

## Deuda conocida
- `/specs` solo tiene `001-deudaTecnica`; el resto del comportamiento no está especificado (principio 2).
