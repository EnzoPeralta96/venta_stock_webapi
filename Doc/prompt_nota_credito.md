# Prompt — Implementación: Nota de Crédito (Anulación de Venta)

```
Contexto del proyecto:
  - ASP.NET Core 8, EF Core, PostgreSQL, patrón Repository + Service + Result<T>
  - El módulo de Ventas (Features/Sale/) ya tiene CreateSale, GetSaleById, GetSalesPaged y PDF.
  - El módulo de CC ya tiene RegisterDebitNote, AnnulPayment, applyInterest, y RecomputeMontoPagado.
  - Ya existe CreditNoteStrategy (tipo 4) en MovementStrategy.cs:
      calcula newSaldo = saldo - importe, newLimite = limite + importe.
  - Ventum.IdMedioPago == 2 indica que la venta fue pagada con CC (cuenta corriente).
  - Ventum.IdEstado: estado "Aprobada" = id a verificar en la BD (consultar tabla estados).
    En la tabla Estado buscar el id cuyo nombre sea "Aprobada" o equivalente para ventas.
  - UpdateProductStockAsync(idProducto, quantity) ya existe en ISaleRepository:
      descuenta stock. Para restituir, usar la misma operación con cantidad negativa (o crear
      un método nuevo RestoreProductStockAsync con cantidad positiva).
  - RecomputeMontoPagado(int clientId) ya existe en CurrentAccountService como método privado.
    Se deberá refactorizar a un método que pueda ser reutilizado o bien duplicar la lógica
    en SaleService inyectando el repositorio de movimientos.

  ANTES DE IMPLEMENTAR, lee la documentación del proyecto:
  - Doc/modulo_clientes.md
  - Doc/modulo_cuenta_corriente.md

  Y luego lee el código fuente completo de:
  - Features/Sale/ (todos los archivos)
  - Features/CurrentAccount/Services/CurrentAccountService/CurrentAccountService.cs
  - Features/CurrentAccount/Repository/AccountMovementRepository/
  - Models/Ventum.cs, Models/DetalleVentum.cs, Models/Producto.cs, Models/Estado.cs

  ══════════════════════════════════════════════════════════
  OBJETIVO
  ══════════════════════════════════════════════════════════

  Implementar la anulación de ventas mediante Nota de Crédito con tres componentes:

  1. CRUD de MotivoNotaCredito: tabla nueva con motivos predefinidos.

  2. Endpoint de anulación de venta: POST /api/Sale/{idVenta}/annul
     Realiza en una transacción: cambio de estado + restitución de stock +
     NC en CC (si corresponde) + recompute de MontoPagado.

  3. Comprobante PDF de NC: usando el endpoint ya existente de payment-receipt.

  ══════════════════════════════════════════════════════════
  PARTE A — Modelo y Migración: MotivoNotaCredito
  ══════════════════════════════════════════════════════════

  1. Crear modelo Models/MotivoNotaCredito.cs:

     namespace proyecto_venta_stock.Models;

     public class MotivoNotaCredito
     {
         public int IdMotivo { get; set; }
         public string Nombre { get; set; } = null!;
         public bool Activo { get; set; } = true;
     }

  2. Registrar en Data/VentaStockContext.cs:

     public virtual DbSet<MotivoNotaCredito> MotivoNotaCreditos { get; set; }

     En OnModelCreating:

     modelBuilder.Entity<MotivoNotaCredito>(entity =>
     {
         entity.HasKey(e => e.IdMotivo);
         entity.ToTable("motivo_nota_credito");
         entity.Property(e => e.IdMotivo)
               .HasColumnName("id_motivo")
               .ValueGeneratedOnAdd();
         entity.Property(e => e.Nombre)
               .HasColumnName("nombre")
               .HasMaxLength(100)
               .IsRequired();
         entity.Property(e => e.Activo)
               .HasColumnName("activo")
               .HasDefaultValue(true);
     });

  3. Agregar campo al modelo MovimientoCc (Models/MovimientoCc.cs):

     public int? IdMotivoNc { get; set; }
     public virtual MotivoNotaCredito? IdMotivoNcNavigation { get; set; }

  4. Registrar en VentaStockContext (dentro de la configuración de MovimientoCc):

     entity.Property(e => e.IdMotivoNc).HasColumnName("id_motivo_nc");
     entity.HasOne(d => d.IdMotivoNcNavigation)
           .WithMany()
           .HasForeignKey(d => d.IdMotivoNc)
           .HasConstraintName("fk_movimiento_cc_motivo_nc");

  5. Crear migración:

     dotnet ef migrations add AddMotivoNotaCredito --output-dir Migrations

     Verificar que cree:
     - Tabla motivo_nota_credito (id_motivo, nombre, activo)
     - Columna id_motivo_nc en movimiento_cc (nullable int, FK)

  6. Aplicar migración:

     dotnet ef database update

  7. Seed inicial (ejecutar SQL directamente o en la migración):

     INSERT INTO motivo_nota_credito (nombre, activo) VALUES ('Devolución de producto', true);
     INSERT INTO motivo_nota_credito (nombre, activo) VALUES ('Error en la venta', true);
     INSERT INTO motivo_nota_credito (nombre, activo) VALUES ('Producto defectuoso', true);

  ══════════════════════════════════════════════════════════
  PARTE B — CRUD de MotivoNotaCredito
  ══════════════════════════════════════════════════════════

  Sigue EXACTAMENTE el mismo patrón que MotivoNotaDebito (Features/CurrentAccount/).
  El CRUD de NC se ubica también dentro de Features/Sale/ para mantener cohesión.

  ─────────────────────────────────────────────────────────
  B.1 — DTOs (Features/Sale/DTO/CreditNoteReasonDTO/)
  ─────────────────────────────────────────────────────────

  1. CreditNoteReasonDTO.cs:
     { IdMotivo, Nombre, Activo }

  2. CreateCreditNoteReasonDTO.cs:
     { [Required][MaxLength(100)] Nombre }

  3. UpdateCreditNoteReasonDTO.cs:
     { [Required] IdMotivo, [Required][MaxLength(100)] Nombre }

  ─────────────────────────────────────────────────────────
  B.2 — Repositorio (Features/Sale/Repository/CreditNoteReasonRepository/)
  ─────────────────────────────────────────────────────────

  ICreditNoteReasonRepository:
     Task<MotivoNotaCredito?> GetByIdAsync(int idMotivo);
     Task<List<MotivoNotaCredito>> GetAllAsync(bool? activo = null);
     Task CreateAsync(MotivoNotaCredito motivo);
     Task UpdateAsync(MotivoNotaCredito motivo);
     Task ToggleStateAsync(int idMotivo, bool activo);
     Task<bool> ExistsByNameAsync(string nombre);
     Task<bool> ExistsByNameAsync(int id, string nombre);

  CreditNoteReasonRepository: implementar con EF Core (igual que DebitNoteReasonRepository).

  ─────────────────────────────────────────────────────────
  B.3 — Servicio (Features/Sale/Services/CreditNoteReasonService/)
  ─────────────────────────────────────────────────────────

  ICreditNoteReasonService:
     Task<Result<CreditNoteReasonDTO>> GetById(int idMotivo);
     Task<Result<List<CreditNoteReasonDTO>>> GetAll(bool? activo = null);
     Task<Result<string>> Create(CreateCreditNoteReasonDTO dto);
     Task<Result<string>> Update(UpdateCreditNoteReasonDTO dto);
     Task<Result<string>> ToggleState(int idMotivo, bool activo);

  ─────────────────────────────────────────────────────────
  B.4 — AutoMapper Profile (Features/Sale/Profile/CreditNoteReasonProfile.cs)
  ─────────────────────────────────────────────────────────

     CreateMap<CreateCreditNoteReasonDTO, MotivoNotaCredito>()
         .ForMember(dest => dest.IdMotivo, opt => opt.Ignore())
         .ForMember(dest => dest.Activo, opt => opt.MapFrom(_ => true));
     CreateMap<UpdateCreditNoteReasonDTO, MotivoNotaCredito>()
         .ForMember(dest => dest.Activo, opt => opt.Ignore());
     CreateMap<MotivoNotaCredito, CreditNoteReasonDTO>();

  ─────────────────────────────────────────────────────────
  B.5 — Códigos de Error (Features/Sale/Message/CreditNoteReasonErrorCode.cs)
  ─────────────────────────────────────────────────────────

     public enum CreditNoteReasonCode
     {
         reason_not_found,
         reason_name_exists,
         unexpected_error
     }

     Mensajes:
     - reason_not_found: "El motivo de nota de crédito indicado no existe."
     - reason_name_exists: "Ya existe un motivo de nota de crédito con ese nombre."
     - unexpected_error: "Ocurrió un error inesperado, por favor intente nuevamente."

  ─────────────────────────────────────────────────────────
  B.6 — Controller (Features/Sale/Controllers/CreditNoteReasonController.cs)
  ─────────────────────────────────────────────────────────

     [ApiController]
     [Route("api/[controller]")]
     [Authorize]
     public class CreditNoteReasonController : ControllerBase

     Endpoints:
     | Método | Ruta                              | Policy          | Acción                       |
     |--------|-----------------------------------|-----------------|------------------------------|
     | GET    | credit-note-reasons               | PERM:VEN_READ   | Listar motivos               |
     | GET    | credit-note-reasons/{id}          | PERM:VEN_READ   | Obtener motivo por ID        |
     | POST   | credit-note-reasons               | PERM:VEN_MANAGE | Crear nuevo motivo           |
     | PUT    | credit-note-reasons               | PERM:VEN_MANAGE | Actualizar motivo            |
     | PUT    | toggle-state/{idMotivo}/{activo}  | PERM:VEN_MANAGE | Activar/desactivar motivo    |

  ─────────────────────────────────────────────────────────
  B.7 — Inyección de Dependencias (Program.cs)
  ─────────────────────────────────────────────────────────

     builder.Services.AddScoped<ICreditNoteReasonRepository, CreditNoteReasonRepository>();
     builder.Services.AddScoped<ICreditNoteReasonService, CreditNoteReasonService>();

  ══════════════════════════════════════════════════════════
  PARTE C — Anulación de Venta (Endpoint Principal)
  ══════════════════════════════════════════════════════════

  ─────────────────────────────────────────────────────────
  C.1 — DTO de anulación (Features/Sale/DTO/AnnulSaleDTO.cs)
  ─────────────────────────────────────────────────────────

     using System.ComponentModel.DataAnnotations;

     public class AnnulSaleDTO
     {
         [Required(ErrorMessage = "El ID del motivo es obligatorio.")]
         public int IdMotivo { get; set; }

         /// <summary>Detalle adicional opcional.</summary>
         public string? DetalleAdicional { get; set; }

         [Required(ErrorMessage = "El ID del usuario que registra es obligatorio.")]
         public int IdUsuarioRegistra { get; set; }
     }

  ─────────────────────────────────────────────────────────
  C.2 — DTO de respuesta de anulación (Features/Sale/DTO/AnnulSaleResponseDTO.cs)
  ─────────────────────────────────────────────────────────

     public class AnnulSaleResponseDTO
     {
         public int IdVenta { get; set; }
         public string CodigoVenta { get; set; } = null!;
         public string Estado { get; set; } = null!;  // "Anulada"

         /// <summary>
         /// ID del movimiento NC generado en CC.
         /// Null si la venta fue pagada en efectivo (no genera NC en CC).
         /// </summary>
         public int? IdMovimientoNc { get; set; }
     }

  ─────────────────────────────────────────────────────────
  C.3 — Nuevos códigos de error en SaleErrorCode
  ─────────────────────────────────────────────────────────

  Agregar al enum SaleErrorCode existente:

     sale_already_annulled,
     credit_note_reason_not_found,
     credit_note_reason_inactive

  Agregar al diccionario SaleErrorDictionary:

     { SaleErrorCode.sale_already_annulled,
       "La venta indicada ya fue anulada previamente." },
     { SaleErrorCode.credit_note_reason_not_found,
       "El motivo de nota de crédito indicado no existe." },
     { SaleErrorCode.credit_note_reason_inactive,
       "El motivo de nota de crédito indicado no está activo." }

  ─────────────────────────────────────────────────────────
  C.4 — Repositorio: nuevos métodos en ISaleRepository
  ─────────────────────────────────────────────────────────

  Agregar a ISaleRepository e implementar en SaleRepository:

  1. Task<Ventum?> GetSaleWithDetailsAsync(int idVenta)
     - Obtiene la venta con tracking (NO AsNoTracking) para poder modificarla.
     - Incluye: .Include(v => v.DetalleVenta).ThenInclude(d => d.IdProductoNavigation)
     - Incluye: .Include(v => v.IdEstadoNavigation)
     - Retorna null si no existe.

  2. Task RestoreProductStockAsync(int idProducto, int quantity)
     - Aumenta el stock del producto: Stock += quantity.
     - Usar ExecuteUpdateAsync para eficiencia.

     Ejemplo:
     await _context.Productos
         .Where(p => p.IdProducto == idProducto)
         .ExecuteUpdateAsync(s => s.SetProperty(p => p.Stock, p => (p.Stock ?? 0) + quantity));

  3. Task UpdateSaleStateAsync(int idVenta, int idEstado)
     - Actualiza el IdEstado de la venta.
     - Usar ExecuteUpdateAsync.

  ─────────────────────────────────────────────────────────
  C.5 — Repositorio: nuevos métodos en IAccountMovementRepository
  ─────────────────────────────────────────────────────────

  Agregar a IAccountMovementRepository e implementar en AccountMovementRepository:

  1. Task<MovimientoCc?> GetConsumptionByVentaIdTracked(int idVenta)
     - Igual que GetConsumptionByVentaId pero con tracking (sin AsNoTracking).
     - Busca el movimiento de tipo MOVIMIENTO_CC (tipo 5) vinculado a la venta.

  NOTA: RecomputeMontoPagado(int clientId) ya existe en CurrentAccountService
  como método privado. Para reutilizarlo en SaleService, hay dos opciones:
  
  OPCIÓN RECOMENDADA: Mover RecomputeMontoPagado a un servicio utilitario
  compartido IAccountAllocationService o bien exponerlo como método público
  en ICurrentAccountService agregándolo a la interfaz. Esta segunda opción es
  más simple dado el estado actual del proyecto.

  Agregar a ICurrentAccountService:

     Task RecomputeMontoPagadoPublic(int clientId);

  Implementar en CurrentAccountService como wrapper del método privado existente:

     public async Task RecomputeMontoPagadoPublic(int clientId)
         => await RecomputeMontoPagado(clientId);

  ─────────────────────────────────────────────────────────
  C.6 — Servicio: AnnulSaleAsync en ISaleServices
  ─────────────────────────────────────────────────────────

  Agregar a ISaleServices:

     Task<Result<AnnulSaleResponseDTO>> AnnulSaleAsync(int idVenta, AnnulSaleDTO dto);

  Implementar en SaleService.
  Requiere inyectar adicionalmente:
  - IAccountMovementRepository (del módulo CC)
  - ICurrentAccountService (para RecomputeMontoPagadoPublic)
  - ICreditNoteReasonRepository
  - MovementStrategyFactory
  - VentaStockContext (para la transacción)

  IMPLEMENTACIÓN:

     public async Task<Result<AnnulSaleResponseDTO>> AnnulSaleAsync(int idVenta, AnnulSaleDTO dto)
     {
         using var transaction = await _context.Database.BeginTransactionAsync();
         try
         {
             // 1. Obtener la venta con detalle y tracking
             var venta = await _saleRepository.GetSaleWithDetailsAsync(idVenta);
             if (venta is null)
                 return Result<AnnulSaleResponseDTO>.Failure(SaleErrorCode.sale_not_found);

             // 2. Validar que la venta esté "Aprobada" (no ya anulada)
             //    Buscar el IdEstado cuyo nombre sea "Anulada" o similar.
             //    El estado "Anulada" para ventas debe ser identificado en la BD.
             //    Consultar _context.Estados para obtener el id correcto.
             //    Alternativa robusta: comparar el nombre del estado:
             if (venta.IdEstadoNavigation?.Nombre?.ToLower().Contains("anulad") == true)
                 return Result<AnnulSaleResponseDTO>.Failure(SaleErrorCode.sale_already_annulled);

             // 3. Validar que el motivo de NC existe y está activo
             var motivo = await _creditNoteReasonRepository.GetByIdAsync(dto.IdMotivo);
             if (motivo is null)
                 return Result<AnnulSaleResponseDTO>.Failure(SaleErrorCode.credit_note_reason_not_found);
             if (!motivo.Activo)
                 return Result<AnnulSaleResponseDTO>.Failure(SaleErrorCode.credit_note_reason_inactive);

             // 4. Obtener el IdEstado de "Anulada" para ventas
             //    Buscar en _context.Estados el estado correcto para ventas anuladas.
             //    Obtener el id del estado "Anulada" de la tabla estados.
             var estadoAnulada = await _context.Estados
                 .Where(e => e.Nombre.ToLower().Contains("anulad"))
                 .FirstOrDefaultAsync();
             int idEstadoAnulada = estadoAnulada?.IdEstado
                 ?? throw new Exception("Estado 'Anulada' no encontrado en la BD.");

             // 5. Cambiar estado de la venta → Anulada
             await _saleRepository.UpdateSaleStateAsync(idVenta, idEstadoAnulada);

             // 6. Restituir stock de cada producto del detalle
             foreach (var item in venta.DetalleVenta)
             {
                 int cantidad = item.Cantidad ?? 0;
                 if (cantidad > 0)
                     await _saleRepository.RestoreProductStockAsync(item.IdProducto, cantidad);
             }

             // 7. Si la venta fue con CC → crear movimiento NC y recomputar MontoPagado
             int? idMovimientoNc = null;
             if (venta.IdMedioPago == 2)  // 2 = Cuenta Corriente
             {
                 int clientId = venta.IdCliente ?? 0;

                 // Obtener último movimiento del cliente para calcular nuevo saldo/límite
                 var lastMovement = await _accountMovementRepository.GetLastMovement(clientId);
                 if (lastMovement is not null)
                 {
                     decimal importeNc = venta.Total ?? 0;
                     decimal balanceBase = lastMovement.SaldoActual ?? 0;
                     decimal limitBase = lastMovement.LimiteCuenta ?? 0;

                     // CreditNoteStrategy: saldo - importe, limite + importe
                     var strategy = _movementStrategyFactory.GetStrategy(TypeMovement.NOTA_CREDITO);
                     var calc = strategy.Calculate(balanceBase, limitBase, importeNc);

                     // Construir detalle del movimiento
                     string detalle = motivo.Nombre;
                     if (!string.IsNullOrWhiteSpace(dto.DetalleAdicional))
                         detalle += $" — {dto.DetalleAdicional}";
                     detalle += $" — Venta {venta.CodigoVenta}";

                     var movimientoNc = new MovimientoCc
                     {
                         IdCliente        = clientId,
                         Importe          = importeNc,
                         Detalle          = detalle,
                         IdEstado         = 2,   // Aprobado
                         IdTipoMovimiento = (int)TypeMovement.NOTA_CREDITO,
                         IdUsuarioRegistra = dto.IdUsuarioRegistra,
                         IdVenta          = idVenta,
                         SaldoActual      = calc.NewBalance,
                         LimiteCuenta     = calc.NewLimit,
                         Fecha            = DateTime.Now,
                         IdMotivoNc       = dto.IdMotivo
                     };

                     await _accountMovementRepository.CreateMovement(movimientoNc);
                     idMovimientoNc = movimientoNc.IdMovimiento;

                     // Recomputar MontoPagado del cliente
                     // (excluye automáticamente la venta anulada porque su consumo
                     //  sigue en la tabla pero queda "cubierto" por el NC en el saldo)
                     await _currentAccountService.RecomputeMontoPagadoPublic(clientId);
                 }
             }

             await transaction.CommitAsync();

             return Result<AnnulSaleResponseDTO>.Success(new AnnulSaleResponseDTO
             {
                 IdVenta       = idVenta,
                 CodigoVenta   = venta.CodigoVenta,
                 Estado        = "Anulada",
                 IdMovimientoNc = idMovimientoNc
             });
         }
         catch (Exception ex)
         {
             await transaction.RollbackAsync();
             _logger.LogError(ex, "Error al anular venta {IdVenta}", idVenta);
             return Result<AnnulSaleResponseDTO>.Failure(SaleErrorCode.unexpected_error);
         }
     }

  ─────────────────────────────────────────────────────────
  C.7 — Controller: endpoint de anulación
  ─────────────────────────────────────────────────────────

  Agregar en SaleController:

     [Authorize(Policy = "PERM:VEN_MANAGE")]
     [HttpPost("{idVenta:int}/annul")]
     public async Task<IActionResult> AnnulSale(int idVenta, [FromBody] AnnulSaleDTO dto)
     {
         if (!ModelState.IsValid)
             return BadRequest(ModelState);

         var result = await _saleService.AnnulSaleAsync(idVenta, dto);

         if (!result.IsSuccess)
         {
             var code = (SaleErrorCode)result.ErrorCode;
             var errorMessage = MessageProvider.Get(SaleErrorDictionary.Messages, code);
             return BadRequest(errorMessage);
         }

         // Si hubo NC, el front puede solicitar el PDF vía:
         // GET /api/CurrentAccount/payment-receipt/{idMovimientoNc}
         return Ok(result.Value);
     }

  ─────────────────────────────────────────────────────────
  C.8 — Actualizar AccountMovementDTO y Profile para incluir MotivoNc
  ─────────────────────────────────────────────────────────

  Agregar a AccountMovementDTO:

     public int? IdMotivoNc { get; set; }
     public string? MotivoNc { get; set; }

  Actualizar CurrentAccountProfile:

     .ForMember(dest => dest.IdMotivoNc, opt => opt.MapFrom(src => src.IdMotivoNc))
     .ForMember(dest => dest.MotivoNc, opt => opt.MapFrom(src =>
         src.IdMotivoNcNavigation != null ? src.IdMotivoNcNavigation.Nombre : null))

  Actualizar GetMovements en AccountMovementRepository:
  Agregar .Include(m => m.IdMotivoNcNavigation) en la query.

  Actualizar GetMovementById en AccountMovementRepository:
  Agregar .Include(m => m.IdMotivoNcNavigation) en la query.

  Actualizar GeneratePaymentReceiptAsync en CurrentAccountService:
  Agregar campo MotivoNc al PaymentReceiptDataSource (igual que se hizo con MotivoNd).

  Actualizar PaymentReceiptDocument:
  Si _data.MotivoNc no es null, mostrar fila "Motivo:" en el contenido.

  ══════════════════════════════════════════════════════════
  RESUMEN DE ARCHIVOS A CREAR/MODIFICAR
  ══════════════════════════════════════════════════════════

  ── Nuevos archivos ──
  Models/MotivoNotaCredito.cs
  Features/Sale/DTO/CreditNoteReasonDTO/CreditNoteReasonDTO.cs
  Features/Sale/DTO/CreditNoteReasonDTO/CreateCreditNoteReasonDTO.cs
  Features/Sale/DTO/CreditNoteReasonDTO/UpdateCreditNoteReasonDTO.cs
  Features/Sale/DTO/AnnulSaleDTO.cs
  Features/Sale/DTO/AnnulSaleResponseDTO.cs
  Features/Sale/Repository/CreditNoteReasonRepository/ICreditNoteReasonRepository.cs
  Features/Sale/Repository/CreditNoteReasonRepository/CreditNoteReasonRepository.cs
  Features/Sale/Services/CreditNoteReasonService/ICreditNoteReasonService.cs
  Features/Sale/Services/CreditNoteReasonService/CreditNoteReasonService.cs
  Features/Sale/Profile/CreditNoteReasonProfile.cs
  Features/Sale/Controllers/CreditNoteReasonController.cs
  Features/Sale/Message/CreditNoteReasonErrorCode.cs

  ── Archivos a modificar ──
  Models/MovimientoCc.cs                              → IdMotivoNc + navegación
  Data/VentaStockContext.cs                           → DbSet + config tabla + FK
  Program.cs                                          → DI nuevos servicios + inyecciones en SaleService
  Features/Sale/Controllers/SaleController.cs         → nuevo endpoint POST {id}/annul
  Features/Sale/Services/SaleServices/ISaleServices.cs → agregar AnnulSaleAsync
  Features/Sale/Services/SaleServices/SaleService.cs   → implementar AnnulSaleAsync + nuevas dependencias
  Features/Sale/Repository/SaleRepository/ISaleRepository.cs → 3 nuevos métodos
  Features/Sale/Repository/SaleRepository/SaleRepository.cs  → implementar
  Features/Sale/Message/SaleErrorCode.cs              → 3 nuevos errores
  Features/CurrentAccount/Services/CurrentAccountService/ICurrentAccountService.cs → RecomputeMontoPagadoPublic
  Features/CurrentAccount/Services/CurrentAccountService/CurrentAccountService.cs  → wrapper público
  Features/CurrentAccount/Repository/AccountMovementRepository/IAccountMovementRepository.cs → GetConsumptionByVentaIdTracked
  Features/CurrentAccount/Repository/AccountMovementRepository/AccountMovementRepository.cs  → implementar + Includes NC
  Features/CurrentAccount/DTO/MovementDTO/AccountMovementDTO.cs → IdMotivoNc, MotivoNc
  Features/CurrentAccount/Profile/CurrentAccountProfile.cs      → mapeo MotivoNc
  Features/CurrentAccount/PDF/PaymentReceiptDataSource.cs        → MotivoNc
  Features/CurrentAccount/PDF/PaymentReceiptDocument.cs          → mostrar motivo NC

  ── Migración ──
  Migrations/                                          → AddMotivoNotaCredito

  ══════════════════════════════════════════════════════════
  VALIDACIONES CRÍTICAS (checklist)
  ══════════════════════════════════════════════════════════

  ✅ La venta debe estar en estado "Aprobada" (no se puede anular lo ya anulado)
  ✅ El motivo de NC debe existir y estar activo
  ✅ Todo ocurre en una transacción: estado + stock + NC + recompute o todo falla
  ✅ El stock se restituye por CADA producto del detalle con su respectiva cantidad
  ✅ El movimiento NC solo se crea si IdMedioPago == 2 (CC). Si fue al contado, no hay NC
  ✅ El importe del NC es el Total completo de la venta (aunque estuviera parcialmente pagada)
  ✅ RecomputeMontoPagado se ejecuta después de crear el NC para recalcular MontoPagado
  ✅ La respuesta incluye IdMovimientoNc para que el frontend pueda solicitar el PDF

  ══════════════════════════════════════════════════════════
  ORDEN DE IMPLEMENTACIÓN RECOMENDADO
  ══════════════════════════════════════════════════════════

  1. Modelo + Migración (Parte A)
  2. CRUD de MotivoNotaCredito (Parte B) — verificar que compila y funciona
  3. Exponer RecomputeMontoPagadoPublic en ICurrentAccountService (Parte C.5)
  4. Nuevos métodos en SaleRepository (Parte C.4)
  5. Nuevo GetConsumptionByVentaIdTracked en AccountMovementRepository (Parte C.5)
  6. Implementar AnnulSaleAsync en SaleService (Parte C.6)
  7. Endpoint POST {idVenta}/annul en SaleController (Parte C.7)
  8. Actualizar AccountMovementDTO + Profile + PDF para MotivoNc (Parte C.8)
  9. Compilar y verificar que el proyecto arranca sin errores

  ══════════════════════════════════════════════════════════
  NOTAS FINALES
  ══════════════════════════════════════════════════════════

  - El PDF del NC ya está soportado por GeneratePaymentReceiptAsync:
    tipo 4 → "NOTA DE CRÉDITO", color azul. Solo se agrega el campo MotivoNc.

  - El campo MontoPagado del consumo (movimiento_cc tipo 5) de la venta anulada
    queda intacto en la tabla. Lo que cambia es el saldo global del cliente,
    que baja gracias al NC. Esto es correcto contablemente: el historial
    muestra la compra, los pagos, y la NC que revierte todo.

  - IdMedioPago == 2 identifica las ventas pagadas con CC. Verificar en la BD
    que este ID sea efectivamente el correspondiente a "Cuenta Corriente".
    Si el ID difiere, ajustar la condición o hacer una consulta dinámica a la tabla medios_pago.

  - PERM:VEN_MANAGE es la policy para anular ventas. Si no existe, crear una
    nueva política igual a PERM:CC_MANAGE pero para el alcance de ventas.
    Verificar en Program.cs las políticas existentes y agregar si hace falta.
```
