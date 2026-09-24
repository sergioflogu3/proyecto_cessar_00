# M2 — Política de contraseñas débil/no visible

**Severidad:** Media · **Estado:** ✅ Resuelto

## Hallazgo original

- **Evidencia:** `UsersController.Create` solo valida que la contraseña no esté vacía (:113); no
  se observan requisitos de longitud/complejidad ni lista de contraseñas filtradas.
- **Remediación pedida:** mínimo 10-12 caracteres, validación de complejidad o contraseñas
  pass-phrase, y comparar contra top-10k filtradas.

## Lo que había en realidad

No era del todo cierto que "solo valida que no esté vacía" — `UsuarioFormViewModel.Password`
ya tenía `[StringLength(100, MinimumLength = 6)]` y `CambiarPasswordViewModel.NuevoPassword`
tenía `[MinLength(6)]`, aplicados automáticamente por el model binding de ASP.NET Core. Pero:
- 6 caracteres está muy por debajo del mínimo recomendado (10-12).
- No había ninguna validación de complejidad ni de contraseñas comunes/filtradas.
- El requisito no era **visible**: no aparecía como texto de ayuda en el formulario, solo como
  error después de fallar el envío — y en el modal de "Cambiar contraseña" el placeholder decía
  literalmente "Mínimo 6 caracteres", reforzando la política débil.

## Decisión: passphrase (longitud) en vez de complejidad forzada — **actualizado**

La remediación ofrecía dos caminos para el primer punto: "validación de complejidad **o**
contraseñas pass-phrase". La primera versión de este cambio eligió longitud sin reglas de
composición forzadas, siguiendo la guía de NIST 800-63B (las reglas de complejidad obligatoria
tienden a producir patrones predecibles sin mejorar la seguridad real). A pedido explícito, se
agregó **además** la validación de complejidad clásica (minúscula + mayúscula + número + carácter
especial) — ver sección "4. Complejidad obligatoria" más abajo. Queda entonces la política más
estricta posible dentro de lo que pedía la remediación: longitud alta **y** complejidad **y**
lista de contraseñas comunes, las tres a la vez.

## Cambios realizados

### 1. Longitud mínima: 12 caracteres

- `UsuarioFormViewModel.Password`: `MinimumLength` de 6 → 12.
- `CambiarPasswordViewModel.NuevoPassword`: `MinLength` de 6 → 12.
- Placeholder y chequeo rápido en JS del modal "Cambiar contraseña" (`_Layout.cshtml`)
  actualizados de 6 a 12 (ese chequeo en JS es solo una comodidad de UX; la validación real y
  autoritativa sigue siendo server-side vía los `ValidationAttribute`).

### 2. Lista de contraseñas comunes/filtradas (top ~10k)

- Se descargó `Passwords/Common-Credentials/10k-most-common.txt` del repositorio
  [SecLists](https://github.com/danielmiessler/SecLists) (MIT license, 10.000 contraseñas,
  verificado sin líneas vacías ni duplicados) y se agregó como
  `SistemaTickets/Infrastructure/Security/CommonPasswords.txt`.
- Se embebió en el ensamblado (no como archivo suelto en el output) vía
  `SistemaTickets.csproj`:
  ```xml
  <None Remove="Infrastructure/Security/CommonPasswords.txt" />
  <EmbeddedResource Include="Infrastructure/Security/CommonPasswords.txt">
    <LogicalName>SistemaTickets.CommonPasswords.txt</LogicalName>
  </EmbeddedResource>
  ```
- Nuevo `Infrastructure/Security/PasswordPolicy.cs`: `MinLength = 12` y
  `EsContraseñaComun(password)`, que carga el recurso embebido una sola vez (`Lazy<HashSet<string>>`,
  comparación case-insensitive) y lo consulta en memoria.
- Nuevo `Models/Validation/NotCommonPasswordAttribute.cs`: `ValidationAttribute` declarativo que
  usa `PasswordPolicy.EsContraseñaComun` — se aplica igual que cualquier otro atributo de
  `System.ComponentModel.DataAnnotations`, sin lógica nueva en los controladores. Devuelve
  válido para `null`/vacío a propósito (ese caso lo cubre `[Required]` donde corresponda, y en
  edición la contraseña es opcional).
- Aplicado en `UsuarioFormViewModel.Password` y `CambiarPasswordViewModel.NuevoPassword`.

### 3. Política visible en la UI

- `Views/Users/Create.cshtml`, `Views/Users/Index.cshtml` (modal de edición) y
  `Views/Shared/_Layout.cshtml` (modal de "Cambiar contraseña"): se agregó un
  `<div class="form-text">Mínimo 12 caracteres. Evitá contraseñas comunes o muy simples.</div>`
  visible **antes** de escribir, no solo como error después de fallar.
- `UsersController.CambiarPassword`: cuando `ModelState` no es válido, ya no devuelve el genérico
  "Datos inválidos." — ahora junta los mensajes reales de los atributos (p. ej. "La nueva
  contraseña debe tener al menos 12 caracteres." o "Esa contraseña es demasiado
  común/fácil de adivinar. Elegí otra.") para que la persona entienda exactamente qué corregir.
  El formulario de edición de usuario (`UsersController.Edit`) ya devolvía `errors` con los
  mensajes de `ModelState` desde A4, así que no necesitó cambios — la nueva regla se propaga
  sola por ese mismo camino.

### 4. Complejidad obligatoria (actualización)

- `PasswordPolicy.TieneComplejidadSuficiente(password)`: exige al menos una minúscula
  (`char.IsLower`), una mayúscula (`char.IsUpper`), un dígito (`char.IsDigit`) y un carácter que
  no sea letra ni dígito (`!char.IsLetterOrDigit`).
- Nuevo `Models/Validation/PasswordComplexityAttribute.cs`, mismo patrón que
  `NotCommonPasswordAttribute` (válido para `null`/vacío, delega en `PasswordPolicy`). Aplicado
  junto a `[NotCommonPassword]` en `UsuarioFormViewModel.Password` y
  `CambiarPasswordViewModel.NuevoPassword`.
- Hints de la UI (los tres formularios) actualizados: "Mínimo 12 caracteres, con mayúscula,
  minúscula, número y carácter especial. Evitá contraseñas comunes."
- El modal de "Cambiar contraseña" (`_Layout.cshtml`) suma un chequeo por regex en JS
  equivalente, antes del round-trip al servidor (comodidad de UX; el atributo server-side sigue
  siendo la fuente de verdad).

## Archivos modificados / creados

- `SistemaTickets/Infrastructure/Security/CommonPasswords.txt` (nuevo, de SecLists).
- `SistemaTickets/Infrastructure/Security/PasswordPolicy.cs` (nuevo) — `EsContraseñaComun` +
  `TieneComplejidadSuficiente`.
- `SistemaTickets/Models/Validation/NotCommonPasswordAttribute.cs`,
  `PasswordComplexityAttribute.cs` (nuevos).
- `SistemaTickets/SistemaTickets.csproj` — recurso embebido.
- `SistemaTickets/Models/Users/UsuarioFormViewModel.cs`,
  `SistemaTickets/Models/Users/CambiarPasswordViewModel.cs` — longitud mínima 12 + los dos
  atributos.
- `SistemaTickets/Controllers/UsersController.cs` — mensaje real de `CambiarPassword`.
- `SistemaTickets/Views/Users/Create.cshtml`, `Views/Users/Index.cshtml`,
  `Views/Shared/_Layout.cshtml` — hint visible + placeholder/JS actualizados.
- `README.md`, `AGENTS.md`, `CLAUDE.md` — documentación.

## Cómo se verificó

1. `dotnet build SistemaTickets.sln` — compila limpio, 0 errores.
2. Se confirmó, inspeccionando el `.dll` compilado, que tanto el nombre lógico del recurso
   (`SistemaTickets.CommonPasswords.txt`) como el contenido de la lista quedaron efectivamente
   embebidos.
3. Se corrió una verificación real contra el ensamblado compilado (usando el runtime de .NET 8
   dentro de un contenedor Docker, ya que este entorno de trabajo solo tiene el SDK de .NET 10):
   `PasswordPolicy.EsContraseñaComun` reflejado y llamado directamente —
   `"password"` → `true`, `"PASSWORD"` → `true` (confirma case-insensitive),
   `"qwerty123456"` → `false`, `"Xk9#mQ2vLp8zR4tN"` → `false` (sin falsos positivos en
   contraseñas fuertes).
4. Misma verificación por reflexión para `TieneComplejidadSuficiente`: `"todominusculas12"` →
   `false`, `"TODOMAYUSCULAS12"` → `false`, `"SinNumerosAca!"` → `false`, `"SinEspecial123A"` →
   `false`, `"Valida#Pass123"` → `true`, `"abc"` → `false` — cada regla individual rechaza
   correctamente cuando falta, y una contraseña que cumple las cuatro pasa.
5. Se reconstruyó y reinició el contenedor `app` del stack de Docker (dos veces, una por cada
   verificación) — sigue arrancando y respondiendo `200` con normalidad.

## Limitaciones conocidas

- La comparación contra la lista de comunes es por **coincidencia exacta** (case-insensitive),
  no por substring/patrón — una contraseña como "MiPassword123!" no se rechaza aunque contenga
  "password", porque el objetivo es bloquear contraseñas literalmente triviales sin generar
  falsos positivos molestos sobre passphrases legítimas.
- La lista (`10k-most-common.txt` de SecLists) es genérica/en inglés; no incluye contraseñas
  específicas en español ni patrones locales. Es un punto de partida razonable, no una garantía
  exhaustiva.
- No se agregó medidor de fortaleza (zxcvbn u otro) en el cliente — el chequeo de "contraseña
  común" es exclusivamente server-side; el JS del modal de cambio de contraseña solo valida
  longitud como comodidad rápida antes del round-trip.
