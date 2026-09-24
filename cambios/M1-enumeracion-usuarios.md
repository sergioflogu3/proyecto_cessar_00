# M1 — Enumeración de usuarios por mensajes de error diferenciados

**Severidad:** Media · **Estado:** ✅ Resuelto

## Hallazgo original

- **Evidencia:** `UsersController.cs:47` ("Credenciales inválidas...") vs
  `UsersController.cs:54` ("No se encontró el usuario.") — mensajes distintos permiten saber si
  un login existe.
- **Remediación pedida:** un único mensaje genérico para ambos casos; mismo tiempo de respuesta.

## Los dos problemas reales (mensaje y temporización)

Al revisar el flujo completo de `Login` (POST) aparecían dos vectores distintos, ambos cubiertos
por la remediación pedida:

1. **Mensaje**: efectivamente había dos strings distintos — "Credenciales inválidas o usuario
   inactivo." (cuando `ValidateCredentialsAsync` devuelve `false`) y "No se encontró el usuario."
   (en un chequeo posterior, tras un login ya validado como correcto, al volver a buscar el
   usuario para armar el JWT). Este segundo camino es casi inalcanzable en la práctica —si
   `ValidateCredentialsAsync` ya confirmó que el usuario existe y está activo, `GetByLoginAsync`
   no debería devolver `null` después— pero seguía siendo un mensaje distinto y por lo tanto un
   posible canal de fuga si algún día ese camino se activa por un bug o una condición de carrera.
2. **Temporización** (el motivo real y explotable, más allá del texto del mensaje):
   `UsuarioService.ValidateCredentialsAsync` buscaba al usuario y, si no existía o estaba
   inactivo, retornaba `false` **de inmediato**. Si existía, corría `BCrypt.Verify` contra el
   hash real — una operación deliberadamente costosa (decenas de milisegundos, por diseño de
   BCrypt). Esa diferencia de tiempo es un canal de enumeración de usuarios clásico,
   independiente de qué texto se muestre: un atacante puede medir la latencia de la respuesta
   para saber si un username existe, sin necesitar leer ningún mensaje.

## Cambios realizados

### 1. Mensaje único (`UsersController.cs`)

Se agregó una constante `LoginGenericoError = "Credenciales inválidas o usuario inactivo."` y
se usa en **los dos** lugares donde antes había mensajes distintos. El camino "no debería pasar
nunca" (`GetByLoginAsync` devuelve `null` después de una validación exitosa) ahora también
muestra el mismo mensaje genérico — solo que además se loggea como `Warning` server-side, porque
si esto llega a pasar de verdad indica un bug o una condición de carrera real que vale la pena
investigar (desactivación de cuenta a mitad de una request, por ejemplo).

### 2. Tiempo de respuesta equiparado (`UsuarioService.cs`)

```csharp
private static readonly string DummyPasswordHash =
    EncryptionHelper.HashPassword(Guid.NewGuid().ToString());

public async Task<bool> ValidateCredentialsAsync(string login, string password)
{
    var user = await GetRawByLoginAsync(login);

    if (user is null || !user.Activo)
    {
        EncryptionHelper.VerifyPassword(password, DummyPasswordHash);
        return false;
    }

    return EncryptionHelper.VerifyPassword(password, user.PasswordHaseado);
}
```

`DummyPasswordHash` es un hash BCrypt válido (mismo `workFactor` que los reales, ya que usa el
mismo `EncryptionHelper.HashPassword`) generado una sola vez, a partir de un GUID aleatorio — no
corresponde a ninguna cuenta ni contraseña real. Cuando el login no resuelve a un usuario activo,
igual se ejecuta `BCrypt.Verify` contra este hash señuelo antes de devolver `false`, en vez de
cortar camino de inmediato. Así el costo de CPU (que es la parte lenta y medible de este método)
es prácticamente el mismo tanto si el usuario existe como si no.

## Archivos modificados

- `SistemaTickets/Controllers/UsersController.cs` — constante `LoginGenericoError`, aplicada en
  los dos lugares que antes tenían mensajes distintos; log de advertencia en el caso "no debería
  pasar nunca".
- `SistemaTickets/Infrastructure/Services/UsuarioService.cs` — `DummyPasswordHash` +
  verificación BCrypt equiparada en `ValidateCredentialsAsync`.
- `README.md`, `AGENTS.md`, `CLAUDE.md` — documentación del mecanismo.

## Cómo verificar

1. Intentar login con un usuario que no existe → mismo mensaje que con una contraseña
   incorrecta para un usuario real ("Credenciales inválidas o usuario inactivo.").
2. Medir el tiempo de respuesta de ambos casos (usuario inexistente vs. contraseña incorrecta
   para un usuario real) → debería ser comparable, porque los dos casos ahora pagan el costo de
   un `BCrypt.Verify`.
3. `dotnet build SistemaTickets.sln` — compila limpio, 0 errores.

No se hizo una medición de microbenchmark real (no hay infraestructura de tests en el repo) —
la garantía viene del diseño: ambos caminos ejecutan la misma operación (`BCrypt.Verify` con el
mismo `workFactor`), que es la parte dominante del tiempo total del método frente a una consulta
a la base de datos.

## Limitaciones conocidas

- El tiempo de la consulta a la base de datos en sí (buscar por username/email/teléfono) puede
  diferir mínimamente entre "usuario existe" y "usuario no existe" (por ejemplo, un índice que
  encuentra una fila vs. uno que no encuentra ninguna). Esa diferencia es del orden de
  microsegundos/bajos milisegundos y queda dominada por el costo de BCrypt (decenas de
  milisegundos con `workFactor=12`), así que no se consideró necesario mitigarla aparte.
- Esto cubre el formulario de login (`/Users/Login`). Si en el futuro se agrega un flujo de
  "olvidé mi contraseña" o similar, hay que aplicar el mismo criterio (mensaje único + tiempo
  equiparado) ahí también.
