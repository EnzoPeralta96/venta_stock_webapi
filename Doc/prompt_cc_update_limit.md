Contexto del proyecto:
- ASP.NET Core 8, EF Core, PostgreSQL, patrón Repository + Service + Strategy + Result<T>
- El módulo de Cuenta Corriente permite operaciones a través del `ICurrentAccountService` y `CurrentAccountController`.
- Necesitamos implementar la funcionalidad de **Actualizar Límite de Cuenta Corriente**, permitiendo aumentar o disminuir el límite de crédito (`LimiteCuenta`) de un cliente. 

Objetivo:
Crear el endpoint `PUT /api/CurrentAccount/update-limit` y toda la lógica subyacente de backend para que un usuario administrador modifique el límite.

══════════════════════════════════════════════════════════
PARTE A — Base de Datos y Modelo
══════════════════════════════════════════════════════════
1. En el enum `TypeMovement.cs` (dentro de `StrategyCurrentAccount`), agregar:
   `MODIFICACION_LIMITE = 10`

2. Crear la estrategia `LimitModificationStrategy` que implemente `IMovementStrategy`:
   - El método `Calculate(decimal oldBalance, decimal oldLimit, decimal amount)` debe retornar un `CalculationResult`.
   - Recordar que en este sistema `LimiteCuenta` funciona en la base de datos como el **Crédito Disponible**. Por lo tanto, si el `amount` recibido es el **Nuevo Límite Global**, el nuevo crédito disponible será `Nuevo Límite Global - Saldo Adedudado (oldBalance)`.
   - Lógica: `return new CalculationResult(oldBalance, amount - oldBalance);`

3. En `MovementStrategyFactory.cs`, registrar el nuevo tipo:
   `TypeMovement.MODIFICACION_LIMITE => new LimitModificationStrategy(),`

*(Nota: Deberás asegurarte de que el tipo de movimiento 10 exista en la base de datos en la tabla `tipo_movimiento` con nombre "modificacion_limite" y accion "Modificación Límite de Crédito". Podes generar una migración o indicarme que lo inserte manualmente).*

══════════════════════════════════════════════════════════
PARTE B — DTO y Service
══════════════════════════════════════════════════════════
1. Crear `UpdateAccountLimitDTO.cs` en la carpeta `Features/CurrentAccount/DTO/MovementDTO`:
```csharp
public class UpdateAccountLimitDTO
{
    [Required]
    public int IdCliente { get; set; }
    
    [Required]
    public int IdConfiguracion { get; set; }
    
    [Required]
    public int IdUsuarioRegistra { get; set; }
    
    [Required]
    [MinLength(5, ErrorMessage = "Debe proporcionar un motivo válido.")]
    public string Motivo { get; set; }
}
```

2. En `ICurrentAccountService.cs` y `CurrentAccountService.cs`, agregar el método:
   `Task<Result<int>> UpdateAccountLimitAsync(UpdateAccountLimitDTO dto)`

   Lógica del servicio:
   - Validar que el cliente exista.
   - Obtener la configuración mediante `_configuracionCcRepository.GetByIdAsync(dto.IdConfiguracion)`.
   - Si no existe o no está activa la configuración, retornar error `CurrentAccountCode.configuracion_cc_not_found` (probablemente necesites inyectar el repositorio y agregar este código de error si no existe).
   - Extraer el nuevo límite: `var nuevoLimite = configuracion.MontoLimite;`.
   - Si no hay último movimiento, retornar error `account_not_found`.
   - Calcular el Límite Global Actual: `var limiteGlobalActual = ultimoMovimiento.SaldoActual + ultimoMovimiento.LimiteCuenta;`
   - Si `nuevoLimite == limiteGlobalActual`, retornar un error `limit_already_set` (debes agregar esto al enum y diccionario de `CurrentAccountCode`).
   - Crear la entidad `MovimientoCc`:
     - `IdCliente = dto.IdCliente`
     - `IdUsuarioRegistra = dto.IdUsuarioRegistra`
     - `IdTipoMovimiento = (int)TypeMovement.MODIFICACION_LIMITE`
     - `Fecha = DateTime.Now`
     - `Importe = 0` // Es un movimiento neutro en plata
     - `Detalle = dto.Motivo`
     - `IdEstado = 2` // Aprobado
   - Obtener la estrategia con `_strategyFactory.GetStrategy(TypeMovement.MODIFICACION_LIMITE)`.
   - Calcular saldo y límite: `var result = strategy.Calculate(ultimoMovimiento.SaldoActual ?? 0, ultimoMovimiento.LimiteCuenta ?? 0, nuevoLimite)`.
     - *(Nota: Pasamos nuevoLimite como third argument 'amount' para que la estrategia sepa cuál es el nuevo límite y lo devuelva en NewLimit).*
   - Aplicar los resultados calculados al movimiento (`mov.SaldoActual = result.NewBalance; mov.LimiteCuenta = result.NewLimit;`).
   - Guardar el movimiento con `_accountMovementRepository.CreateAccountMovementAsync(mov)`.
   - Retornar el `IdMovimiento`.

══════════════════════════════════════════════════════════
PARTE C — Controlador
══════════════════════════════════════════════════════════
1. En `CurrentAccountController.cs`, agregar:
```csharp
[Authorize(Policy = "PERM:CC_MANAGE")]
[HttpPut("update-limit")]
public async Task<IActionResult> UpdateAccountLimit([FromBody] UpdateAccountLimitDTO dto)
{
    if (!ModelState.IsValid)
        return BadRequest(ModelState);

    var result = await _currentAccountService.UpdateAccountLimitAsync(dto);
    
    if (!result.IsSuccess)
    {
        var code = (CurrentAccountCode)result.ErrorCode;
        var errorMessage = MessageProvider.Get(CurrentAccountDictionary.Messages, code);
        return BadRequest(errorMessage);
    }
    
    return Ok(new { idMovimiento = result.Value });
}
```

══════════════════════════════════════════════════════════
PARTE D — Diccionario de Errores
══════════════════════════════════════════════════════════
1. En `CurrentAccountCode.cs`, añadir:
   `limit_already_set`
2. En `CurrentAccountDictionary.cs`, añadir:
   `{ CurrentAccountCode.limit_already_set, "El nuevo límite indicado es igual al límite actual." }`

Revisar cuidadosamente que el movimiento se registre sin alterar el saldo adeudado (`SaldoActual`), para que la CC siga matemáticamente correcta, y que quede un registro auditable del cambio de límite.

Luego dame un resumen para el frontend para la integracion de esta nueva funcionalidad. Específicamente, recordarme que en `ClientCurrentAccountTab.jsx` el front extrae el "Límite Global" desde `opening.limiteCuenta`. Como el límite global ahora puede cambiar en cualquier nuevo movimiento del tipo 10, indícame cómo debería el frontend buscar el **Límite Global Vigente**. Debería calcularlo a partir del `latest` movimiento haciendo `latest.saldoActual + latest.limiteCuenta`, en lugar de usar `opening.limiteCuenta`.