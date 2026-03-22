# Prompt — Implementación: Anular Pago + Nota de Débito + Nota de Crédito

> Copiar este prompt completo y pegarlo en Claude para que implemente los cambios.

---

```
Contexto del proyecto:
  - ASP.NET Core 8, EF Core, PostgreSQL, patrón Repository + Service + Result<T>
  - El módulo de CC ya tiene RegisterMovement que maneja pagos (pago_global, pago_factura)
    y actualiza MontoPagado en los consumos (movimiento_cc) mediante AllocarPagoGlobal
    y AllocarPagoFactura.
  - Ya existen DebitNoteStrategy (tipo 3) y CreditNoteStrategy (tipo 4) en MovementStrategy.cs.
  - Ya existe GeneratePaymentReceiptAsync que genera PDF con PaymentReceiptDocument.

  Lee la documentación del proyecto en:
  - Doc/modulo_clientes.md
  - Doc/modulo_cuenta_corriente.md

  Y luego lee el código fuente de los módulos Features/Client y Features/CurrentAccount
  completos, antes de implementar nada.

  ══════════════════════════════════════════════════════════
  FUNCIONALIDAD 1 — ANULAR PAGO
  ══════════════════════════════════════════════════════════

  Problema: cuando un operario registra un pago por error (monto incorrecto,
  cliente incorrecto, etc.), no existe forma de revertirlo. El MontoPagado
  de los consumos queda incorrecto y el saldo/límite del cliente no refleja
  la realidad.

  Solución: implementar "Anular Pago" como funcionalidad dedicada que:
  1. Crea un contra-movimiento (tipo ANULACION_PAGO) que revierte saldo/límite.
  2. Marca el pago original como anulado (campo EsAnulado).
  3. Recomputa MontoPagado desde cero excluyendo el pago anulado.

  ─────────────────────────────────────────────────────────
  PARTE A — Migración y modelo: campo EsAnulado
  ─────────────────────────────────────────────────────────

  1. Agregar a Models/MovimientoCc.cs:

     public bool EsAnulado { get; set; }

  2. Crear migración EF Core:

     dotnet tool run dotnet-ef migrations add AddEsAnuladoToMovimientoCc

     La migración debe contener SOLO:
     Up:   migrationBuilder.AddColumn<bool>("EsAnulado", "movimiento_cc",
               nullable: false, defaultValue: false);
     Down: migrationBuilder.DropColumn("EsAnulado", "movimiento_cc");

  3. Aplicar la migración:

     dotnet tool run dotnet-ef database update

  ─────────────────────────────────────────────────────────
  PARTE B — Nuevo tipo de movimiento y estrategia
  ─────────────────────────────────────────────────────────

  1. Agregar al enum TypeMovement en TypeMovements.cs:

     ANULACION_PAGO = 9

  2. IMPORTANTE: Insertar el tipo en la tabla de base de datos tipo_movimiento:

     INSERT INTO tipo_movimiento (id_movimiento, nombre, accion)
     VALUES (9, 'anulacion_pago', 'Anulación de pago registrado por error');

     (Puede ser vía migración de datos o script SQL manual. Documentar el approach elegido.)

  3. Agregar estrategia en MovementStrategyFactory.cs:

     TypeMovement.ANULACION_PAGO => new DebitNoteStrategy(),
     // Mismo efecto que una ND: saldo + amount, límite - amount
     // (revertir un pago = sumar la deuda de vuelta)

  ─────────────────────────────────────────────────────────
  PARTE C — DTO para anulación
  ─────────────────────────────────────────────────────────

  1. Crear nuevo DTO: Features/CurrentAccount/DTO/MovementDTO/AnnulPaymentDTO.cs

     public class AnnulPaymentDTO
     {
         [Required(ErrorMessage = "El ID del movimiento a anular es obligatorio.")]
         public int IdMovimientoPago { get; set; }

         [Required(ErrorMessage = "El ID del usuario es obligatorio.")]
         public int IdUsuarioRegistra { get; set; }

         [Required(ErrorMessage = "El motivo de anulación es obligatorio.")]
         [MinLength(10, ErrorMessage = "El motivo debe tener al menos 10 caracteres.")]
         public string Motivo { get; set; }
     }

  2. Agregar a AccountMovementDTO:

     public bool EsAnulado { get; set; }

  3. En CurrentAccountProfile, agregar al mapeo MovimientoCc → AccountMovementDTO:

     .ForMember(dest => dest.EsAnulado, opt => opt.MapFrom(src => src.EsAnulado))

  ─────────────────────────────────────────────────────────
  PARTE D — Repositorio: nuevos métodos
  ─────────────────────────────────────────────────────────

  Agregar a IAccountMovementRepository e implementar en AccountMovementRepository:

  1. Task<MovimientoCc> GetPaymentMovementById(int idMovimiento)
     - Busca por IdMovimiento
     - Solo devuelve el movimiento si es de tipo pago_global (6) o pago_factura (8)
     - Sin AsNoTracking (necesitamos tracking para modificarlo)
     - Retorna null si no existe o no es un pago

  2. Task<List<MovimientoCc>> GetAllConsumptionsTracked(int clientId)
     - Devuelve todos los movimiento_cc (IdTipoMovimiento == 5) del cliente
     - Ordenados por Fecha ASC, ThenBy IdMovimiento ASC
     - CON tracking (sin AsNoTracking) para poder modificarlos y guardar

  3. Task<List<MovimientoCc>> GetAllValidPayments(int clientId)
     - Devuelve todos los movimientos de tipo pago_global (6) y pago_factura (8)
       donde EsAnulado == false
     - Ordenados por Fecha ASC, ThenBy IdMovimiento ASC
     - AsNoTracking (solo lectura para el recompute)

  4. Modificar GetPendingSalesForPayment:
     En el GroupJoin que busca movimientos tipo PAGO_FACTURA (8), agregar filtro:

     _context.MovimientoCcs.Where(m => m.IdTipoMovimiento == 8 && !m.EsAnulado)

     Un pago_factura anulado NO debe contar como pago de esa venta.

  ─────────────────────────────────────────────────────────
  PARTE E — Servicio: endpoint de anulación
  ─────────────────────────────────────────────────────────

  1. Agregar a ICurrentAccountService:

     Task<Result<int>> AnnulPayment(AnnulPaymentDTO dto);

  2. Implementar en CurrentAccountService.AnnulPayment:

     public async Task<Result<int>> AnnulPayment(AnnulPaymentDTO dto)
     {
         // ── Todo el flujo dentro de una transacción ──
         using var transaction = await _context.Database.BeginTransactionAsync();
         try
         {
             // 1. Obtener el pago a anular CON TRACKING
             var pagoAAnular = await _accountMovementRepository
                 .GetPaymentMovementById(dto.IdMovimientoPago);

             if (pagoAAnular is null)
                 return Result<int>.Failure(CurrentAccountCode.movement_not_found);

             // 2. Validar que el pago NO esté ya anulado
             if (pagoAAnular.EsAnulado)
                 return Result<int>.Failure(CurrentAccountCode.payment_already_annulled);

             // 3. Obtener último movimiento del cliente para calcular nuevo saldo/límite
             int clientId = pagoAAnular.IdCliente ?? 0;
             var lastMovement = await _accountMovementRepository.GetLastMovement(clientId);

             if (lastMovement is null)
                 return Result<int>.Failure(CurrentAccountCode.account_not_found);

             decimal balanceBase = lastMovement.SaldoActual ?? 0;
             decimal limitBase = lastMovement.LimiteCuenta ?? 0;
             decimal importePago = pagoAAnular.Importe ?? 0;

             // 4. Calcular nuevo saldo/límite (revertir el pago = sumar deuda)
             var strategy = _movementStrategyFactory.GetStrategy(TypeMovement.ANULACION_PAGO);
             var result = strategy.Calculate(balanceBase, limitBase, importePago);

             // 5. Crear el contra-movimiento de anulación
             var contraMovimiento = new MovimientoCc
             {
                 IdCliente = clientId,
                 Importe = importePago,
                 Detalle = $"Anulación de pago #{dto.IdMovimientoPago}: {dto.Motivo}",
                 IdEstado = 2,  // Aprobado
                 IdTipoMovimiento = (int)TypeMovement.ANULACION_PAGO,
                 IdUsuarioRegistra = dto.IdUsuarioRegistra,
                 IdVenta = pagoAAnular.IdVenta,  // misma venta si era pago_factura
                 SaldoActual = result.NewBalance,
                 LimiteCuenta = result.NewLimit,
                 Fecha = DateTime.Now
             };

             await _accountMovementRepository.CreateMovement(contraMovimiento);

             // 6. Marcar el pago original como anulado
             pagoAAnular.EsAnulado = true;
             await _accountMovementRepository
                 .UpdateMovimientos(new List<MovimientoCc> { pagoAAnular });

             // 7. Recomputar MontoPagado de todos los consumos del cliente
             await RecomputeMontoPagado(clientId);

             await transaction.CommitAsync();

             return Result<int>.Success(contraMovimiento.IdMovimiento);
         }
         catch (Exception ex)
         {
             await transaction.RollbackAsync();
             _logger.LogError(ex, "Error al anular pago {IdMov}", dto.IdMovimientoPago);
             return Result<int>.Failure(CurrentAccountCode.unexpected_error);
         }
     }

  3. Agregar método privado RecomputeMontoPagado:

     private async Task RecomputeMontoPagado(int clientId)
     {
         // 1. Obtener todos los consumos CON tracking
         var consumos = await _accountMovementRepository.GetAllConsumptionsTracked(clientId);

         // 2. Resetear MontoPagado a 0 en todos
         foreach (var consumo in consumos)
             consumo.MontoPagado = 0;

         // 3. Obtener todos los pagos válidos (EsAnulado == false) ordenados cronológicamente
         var pagos = await _accountMovementRepository.GetAllValidPayments(clientId);

         // 4. Re-aplicar asignación para cada pago en orden cronológico
         foreach (var pago in pagos)
         {
             if (pago.IdTipoMovimiento == (int)TypeMovement.PAGO_FACTURA
                 && pago.IdVenta.HasValue)
                 AllocarPagoFacturaEnMemoria(pago.IdVenta.Value, pago.Importe ?? 0, consumos);
             else if (pago.IdTipoMovimiento == (int)TypeMovement.PAGO_GLOBAL)
                 AllocarPagoGlobalEnMemoria(pago.Importe ?? 0, consumos);
         }

         // 5. Guardar todos los consumos actualizados en batch
         await _accountMovementRepository.UpdateMovimientos(consumos);
     }

  4. Refactorizar AllocarPago* para evitar duplicación de lógica.
     Crear versiones in-memory + hacer que las existentes las usen:

     // ── Versiones in-memory (para recompute) ──

     private void AllocarPagoGlobalEnMemoria(decimal importePago, List<MovimientoCc> consumos)
     {
         decimal restante = importePago;
         foreach (var consumo in consumos.OrderBy(c => c.Fecha).ThenBy(c => c.IdMovimiento))
         {
             if (restante <= 0) break;
             decimal pendiente = (consumo.Importe ?? 0) - (consumo.MontoPagado ?? 0);
             if (pendiente <= 0) continue;

             if (restante >= pendiente)
             { consumo.MontoPagado = consumo.Importe; restante -= pendiente; }
             else
             { consumo.MontoPagado = (consumo.MontoPagado ?? 0) + restante; restante = 0; }
         }
     }

     private void AllocarPagoFacturaEnMemoria(
         int idVenta, decimal importePago, List<MovimientoCc> consumos)
     {
         var consumo = consumos.FirstOrDefault(c => c.IdVenta == idVenta);
         if (consumo is null) return;
         consumo.MontoPagado = Math.Min(
             (consumo.MontoPagado ?? 0) + importePago, consumo.Importe ?? 0);
     }

     // ── Versiones existentes refactorizadas (para uso normal) ──

     private async Task AllocarPagoGlobal(int clientId, decimal importePago)
     {
         var consumos = await _accountMovementRepository.GetUnpaidConsumptions(clientId);
         if (consumos.Count == 0) return;
         AllocarPagoGlobalEnMemoria(importePago, consumos);
         await _accountMovementRepository.UpdateMovimientos(consumos);
     }

     private async Task AllocarPagoFactura(int idVenta, decimal importePago)
     {
         var consumo = await _accountMovementRepository.GetConsumptionByVentaId(idVenta);
         if (consumo is null) return;
         AllocarPagoFacturaEnMemoria(idVenta, importePago, new List<MovimientoCc> { consumo });
         await _accountMovementRepository.UpdateMovimientos(new List<MovimientoCc> { consumo });
     }

  5. Agregar nuevos códigos de error a CurrentAccountCode:

     movement_not_found,
     payment_already_annulled

     Agregar a CurrentAccountDictionary:

     { CurrentAccountCode.movement_not_found,
       "El movimiento indicado no existe o no es un pago." },
     { CurrentAccountCode.payment_already_annulled,
       "El pago indicado ya fue anulado previamente." }

  ─────────────────────────────────────────────────────────
  PARTE F — Controller: endpoint de anulación
  ─────────────────────────────────────────────────────────

  Agregar en CurrentAccountController:

     [Authorize(Policy = "PERM:CC_ANNUL_PAYMENT")]
     [HttpPost("annul-payment")]
     public async Task<IActionResult> AnnulPayment([FromBody] AnnulPaymentDTO dto)
     {
         if (!ModelState.IsValid)
             return BadRequest(ModelState);

         var result = await _currentAccountService.AnnulPayment(dto);

         if (!result.IsSuccess)
         {
             var code = (CurrentAccountCode)result.ErrorCode;
             var errorMessage = MessageProvider.Get(CurrentAccountDictionary.Messages, code);
             return BadRequest(errorMessage);
         }

         return Ok(new { idMovimiento = result.Value });
     }

  NOTA: El permiso "PERM:CC_ANNUL_PAYMENT" debe registrarse en la tabla
  de permisos del sistema. Si no existe infraestructura para agregar permisos
  dinámicamente, puede reutilizarse "PERM:CC_MANAGE" temporalmente.

  ══════════════════════════════════════════════════════════
  FUNCIONALIDAD 2 — NOTA DE DÉBITO Y NOTA DE CRÉDITO
  ══════════════════════════════════════════════════════════

  Las ND (tipo 3) y NC (tipo 4) ya funcionan vía:
    POST /api/CurrentAccount/register-movement
    con IdTipoMovimiento: 3 (ND) o 4 (NC)

  Las estrategias DebitNoteStrategy y CreditNoteStrategy ya existen y calculan
  correctamente el saldo/límite. No hay que cambiar nada en la lógica de registro.

  Lo que falta es adaptar el comprobante PDF para soportar ND y NC con
  presentación visual diferenciada.

  ─────────────────────────────────────────────────────────
  PARTE G — PDF adaptado por tipo de movimiento
  ─────────────────────────────────────────────────────────

  1. Modificar PaymentReceiptDataSource:
     Reemplazar el campo TipoPago (string) por:

     public string TipoComprobante { get; set; }  // título del header
     public string ColorHeader     { get; set; }  // "green" | "red" | "blue"

     Eliminar TipoPago.

  2. Modificar PaymentReceiptDocument:
     En ComposeHeader, el color del título y del borde inferior deben venir
     de _data.ColorHeader. Mapear:

     "green" → Colors.Green.Darken2
     "red"   → Colors.Red.Darken2
     "blue"  → Colors.Blue.Darken2

     En el bloque derecho del header, mostrar _data.TipoComprobante
     en lugar del texto hardcodeado "COMPROBANTE DE PAGO".

  3. Modificar GeneratePaymentReceiptAsync en CurrentAccountService:
     Determinar TipoComprobante y ColorHeader según movement.IdTipoMovimiento:

     var (tipoComprobante, colorHeader) = movement.IdTipoMovimiento switch
     {
         6 or 8 => ("COMPROBANTE DE PAGO",          "green"),
         3      => ("NOTA DE DÉBITO",                "red"),
         4      => ("NOTA DE CRÉDITO",               "blue"),
         9      => ("ANULACIÓN DE PAGO",             "red"),
         _      => ("COMPROBANTE DE MOVIMIENTO",     "green")
     };

     Asignar en dataSource:
     dataSource.TipoComprobante = tipoComprobante;
     dataSource.ColorHeader     = colorHeader;

     Mantener el subtítulo "CUENTA CORRIENTE" en el PDF.

  4. El endpoint GET /api/CurrentAccount/payment-receipt/{idMovimiento}
     funciona sin cambios: el servicio detecta el tipo y adapta el PDF.

  ══════════════════════════════════════════════════════════
  RESUMEN DE VALIDACIONES CRÍTICAS
  ══════════════════════════════════════════════════════════

  La anulación de pago DEBE validar:

  1. ✅ El movimiento existe y es de tipo pago (6 o 8)
     → GetPaymentMovementById retorna null si no es pago → movement_not_found

  2. ✅ El pago NO está ya anulado
     → if (pagoAAnular.EsAnulado) → payment_already_annulled

  3. ✅ El importe de la anulación es EXACTAMENTE igual al del pago original
     → No se pide importe al usuario; se toma automáticamente de pagoAAnular.Importe

  4. ✅ Todo el flujo está envuelto en una transacción
     → BeginTransactionAsync → CommitAsync / RollbackAsync

  ══════════════════════════════════════════════════════════
  NOTAS FINALES
  ══════════════════════════════════════════════════════════

  - ND y NC básicos (ajustes manuales) no requieren ningún cambio adicional:
    ya funcionan vía POST /api/CurrentAccount/register-movement con
    idTipoMovimiento: 3 (ND) o 4 (NC). El frontend solo necesita exponer
    esos tipos en el formulario de ajustes.

  - La anulación de pago es una funcionalidad SEPARADA de ND/NC.
    Tiene su propio endpoint, DTO y tipo de movimiento.
    No se reutiliza ND para anular — es más claro y auditable.

  - El campo EsAnulado se expone en AccountMovementDTO para que el frontend
    pueda mostrar visualmente los pagos anulados en el historial (badge
    "Anulado", texto tachado, etc.).

  - El motivo de anulación queda registrado en el Detalle del contra-movimiento:
    "Anulación de pago #123: [motivo ingresado por el usuario]".

  - La auditoría se registra automáticamente si el interceptor de auditoría
    del proyecto está habilitado para la tabla movimiento_cc.

  - Registrar en la tabla de permisos la policy "PERM:CC_ANNUL_PAYMENT"
    o reutilizar "PERM:CC_MANAGE" según la decisión del equipo.
```
