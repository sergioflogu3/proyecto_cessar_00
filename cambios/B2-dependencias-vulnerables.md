# B2 — Dependencias sin revisión de CVEs / sin escaneo automático

**Severidad:** Baja · **Estado:** ⚠️ Parcial (dependencias actualizadas y sin CVEs conocidas hoy · CI/Dependabot pendiente de decisión del usuario)

## Hallazgo original

- **Evidencia:** `SistemaTickets.csproj` (NHibernate 5.5.2, SqlClient 7.0.0, QuestPDF
  2024.12.0...); no hay CI ni `dotnet list package --vulnerable` automatizado.
- **Remediación pedida:** actualizar minor versions y añadir análisis de dependencias (GitHub
  Dependabot / `dotnet list package --vulnerable` en CI).

## Parte 1: actualizar minor versions (hecho)

### Diagnóstico inicial

`dotnet list SistemaTickets.sln package --vulnerable --include-transitive` mostró una
vulnerabilidad real, no solo versiones desactualizadas:

```
Paquete transitivo         Resuelto   Gravedad   Advisory
> System.IO.Packaging      6.0.0      High       GHSA-f32c-w444-8ppv, GHSA-qj66-m88j-hmgj
```

Traída transitivamente por `ClosedXML 0.102.3` → `DocumentFormat.OpenXml 2.16.0`.

### Qué se actualizó, y qué no

`dotnet list package --outdated --highest-minor` (que ignora saltos de versión mayor) dio la
lista segura de objetivos — 8 paquetes directos, todos dentro de su misma línea mayor:

| Paquete | Antes | Después |
|---|---|---|
| NHibernate | 5.5.2 | 5.7.0 |
| FluentNHibernate | 3.4.1 | 3.5.0 |
| Microsoft.Data.SqlClient | 7.0.0 | 7.1.0 |
| Microsoft.AspNetCore.Authentication.JwtBearer | 8.0.11 | 8.0.31 |
| QuestPDF | 2024.12.0 | 2024.12.3 |
| ClosedXML | 0.102.3 | 0.105.1 |
| Azure.Storage.Blobs | 12.28.0 | 12.29.2 |
| BCrypt.Net-Next | 4.0.3 | 4.2.0 |

Dos decisiones deliberadas de **no** actualizar a lo que `dotnet list --outdated` (sin
`--highest-minor`) mostraba como "más reciente":

- **`Microsoft.AspNetCore.Authentication.JwtBearer`**: el "más reciente" sin filtro es
  `10.0.12` — un salto de versión mayor (8→10, saltándose 9) que además no tiene sentido con
  `TargetFramework=net8.0` del proyecto. Se quedó en `8.0.31`, la última dentro de la línea 8.x
  (.NET 8 sigue en soporte LTS hasta noviembre 2026, ver `planes/02-ci-cd-plan.md`).
- **`QuestPDF`**: el "más reciente" sin filtro es `2026.9.0`. QuestPDF versiona por
  año.mes (no semver clásico), y ese salto representa ~2 años de cambios — incluye,
  potencialmente, cambios de licenciamiento/API que quedan fuera del alcance de "actualizar
  minor versions". Se quedó en `2024.12.3`, el último patch dentro de la misma línea
  `2024.12.x`.

### La vulnerabilidad real: dos rondas

**Ronda 1** — actualizar `ClosedXML` a `0.105.1` (que arrastra una versión más nueva de
`DocumentFormat.OpenXml`) resolvió el `System.IO.Packaging` original, pero
`dotnet list package --vulnerable --include-transitive` mostró dos nuevas:

```
> System.Net.Http                                    4.1.0   High   GHSA-7jgj-8wvc-jh57
> System.Security.Cryptography.X509Certificates      4.1.0   High   GHSA-7mfr-774f-w5r9
```

`dotnet nuget why SistemaTickets.csproj System.Net.Http` mostró el origen: no vienen de
ClosedXML, sino de la propia actualización de NHibernate —

```
NHibernate (v5.7.0) → Antlr3.Runtime (v3.5.1) → NETStandard.Library (v1.6.0) → System.Net.Http (v4.1.0)
```

`Antlr3.Runtime` (el parser HQL de NHibernate) depende de un paquete `NETStandard.Library`
viejo (netstandard1.x-era) que a su vez fija versiones antiguas de estos dos paquetes. En
tiempo de ejecución sobre .NET 8 esto es casi seguro inerte (el runtime usa las implementaciones
del framework compartido, no estos paquetes viejos vía unificación de ensamblados), pero
`dotnet list --vulnerable` los sigue marcando porque son los que figuran en el grafo de
dependencias resuelto — y no depende de nosotros arreglarlo en el origen (es una dependencia
transitiva de NHibernate, no algo que este proyecto controle).

**Ronda 2** — se agregaron **pines transitivos explícitos** (un `PackageReference` directo que
NuGet respeta por sobre el mínimo transitivo) a las últimas versiones publicadas de esos dos
paquetes (ambos ya descontinuados/congelados, no reciben versiones nuevas hace años):

```xml
<PackageReference Include="System.Net.Http" Version="4.3.4" />
<PackageReference Include="System.Security.Cryptography.X509Certificates" Version="4.3.2" />
```

Con esto, `dotnet list package --vulnerable --include-transitive` quedó en:

```
El proyecto "SistemaTickets" especificado no tiene paquetes vulnerables en los orígenes actuales.
```

## Parte 2: análisis de dependencias en CI (pendiente, por decisión explícita)

La remediación también pedía "añadir análisis de dependencias (GitHub Dependabot /
`dotnet list package --vulnerable` en CI)". Ya existe exactamente ese job preparado en
`planes/ci.yml` (`security`, corre `dotnet list package --vulnerable --include-transitive` y
sube el reporte como artifact, en modo `continue-on-error: true` hasta pasar a bloqueante — ver
`planes/02-ci-cd-plan.md`), pero **sigue sin instalarse** (`CLAUDE.md` ya documentaba esto como
un estado deliberado: "a CI workflow exists as a draft... but is not installed").

Se consultó explícitamente antes de tocar esto — activar el workflow (copiarlo a
`.github/workflows/ci.yml`) haría que corra de verdad en GitHub Actions en cada push/PR, porque
el repo tiene remote real (`sergioflogu3/proyecto_cessar_00`), consumiendo minutos de Actions y
apareciendo como checks visibles en PRs futuros — una acción con impacto en un sistema
compartido, no solo un cambio de archivo local. Se decidió **dejarlo como borrador** por ahora;
la parte de "actualizar minor versions" queda resuelta, la de "análisis automatizado en CI"
queda pendiente de que el usuario decida activarla (ver sección siguiente).

Tampoco se configuró GitHub Dependabot (`.github/dependabot.yml`) — es la otra opción que
ofrecía la remediación, y tiene el mismo problema: en un repo con remote real, agregarlo hace
que GitHub empiece a abrir PRs automáticamente. Queda con el mismo criterio que el punto
anterior.

## Archivos modificados

- `SistemaTickets/SistemaTickets.csproj` — 8 paquetes directos actualizados (ver tabla) + 2
  pines transitivos nuevos (`System.Net.Http`, `System.Security.Cryptography.X509Certificates`).
- `README.md` — tabla de "Tecnologías y Dependencias" con las versiones nuevas.
- `AGENTS.md`, `CLAUDE.md` — nota de estado (parcial) y detalle de qué se ancló y por qué.
- `cambios/README.md` — fila B2 en la tabla.

## Cómo se verificó

1. `dotnet list SistemaTickets.sln package --vulnerable --include-transitive` — de 1
   vulnerabilidad (High) a 0, pasando por un estado intermedio de 2 (documentado arriba, no fue
   un callejón sin salida, cada ronda achicó el problema).
2. `dotnet build SistemaTickets.sln` — compila limpio, 0 errores (mismas 2 warnings
   preexistentes de nulabilidad, no relacionadas).
3. Se reconstruyó la imagen de `app` en Docker (`docker-compose up -d --build app`) con las 10
   dependencias nuevas y se confirmó que arranca sano: `GET /Users/Login` → `200`,
   `docker logs` sin errores ni excepciones al inicio.
4. **No se probó manualmente** la generación de reportes PDF (QuestPDF) ni Excel (ClosedXML) —
   ambas librerías cambiaron de versión y son las que más superficie de API tocan en este
   cambio. El build compiló sin tocar código de `ReporteService`/`ReportesController`, lo cual es
   buena señal (la superficie pública que usa este proyecto no cambió entre esas versiones),
   pero vale una prueba manual real: generar un reporte PDF y uno Excel desde `/Reportes` antes
   de dar esto por completamente cerrado.

## Limitaciones conocidas

- Como con M6, no hay manera de que una herramienta automatizada garantice "estas versiones
  seguirán sin CVEs conocidas para siempre" — esto es una foto del momento
  (`2026-09-24`). Sin el job de CI o Dependabot activado, alguien tiene que volver a correr
  `dotnet list package --vulnerable --include-transitive` manualmente de tanto en tanto.
- Los pines transitivos a `System.Net.Http 4.3.4` /
  `System.Security.Cryptography.X509Certificates 4.3.2` son un parche sobre una dependencia de
  NHibernate que este proyecto no controla (`Antlr3.Runtime`) — si una futura versión de
  NHibernate actualiza esa cadena por su cuenta, estos pines se vuelven innecesarios (pero no
  dañinos: NuGet toma el máximo entre el pin y lo que pida la dependencia transitiva).
- Falta la verificación manual de generación de reportes PDF/Excel mencionada arriba.
- Activar el job `security` de CI (o Dependabot) sigue pendiente de decisión del usuario — no se
  tocó `.github/workflows/` ni se creó `.github/dependabot.yml` en este cambio.
