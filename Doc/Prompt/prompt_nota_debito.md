# Prompt — Implementación: Nota de Débito (CRUD Motivos + Registro de ND)

> Copiar este prompt completo y pegarlo en Claude para que implemente los cambios.

---

```
Contexto del proyecto:
  - ASP.NET Core 8, EF Core, PostgreSQL, patrón Repository + Service + Result<T>
  - El módulo de CC ya tiene RegisterMovement que maneja movimientos y calcula saldo/límite
    mediante el Strategy Pattern (MovementStrategyFactory + DebitNoteStrategy/CreditNoteStrategy, etc.)
  - Ya existe ANULACION_PAGO (tipo 9) implementada con endpoint dedicado (POST annul-payment).
  - Ya existe GeneratePaymentReceiptAsync que genera PDF adaptado por tipo de movimiento
    con TipoComprobante y ColorHeader (verde para pagos, rojo para ND/anulación, azul para NC).
  - El tipo INTERES_SALDO_GLOBAL (id=7) existe pero queda DEPRECADO.
    A partir de ahora, todo interés por mora se registra como Nota de Débito (tipo 3)
    con motivo "Interés por mora".

  ANTES DE IMPLEMENTAR, lee la documentación del proyecto:
  - Doc/modulo_clientes.md
  - Doc/modulo_cuenta_corriente.md

  Y luego lee el código fuente completo de Features/CurrentAccount y Features/Client.

  ══════════════════════════════════════════════════════════
  OBJETIVO
  ══════════════════════════════════════════════════════════

  Implementar la funcionalidad de Nota de Débito con dos componentes:

  1. CRUD de Motivos de ND: tabla nueva con motivos predefinidos (ej. "Interés por mora",
     "Ajuste de precio"). Sigue exactamente el mismo patrón de AccountConfig
     (Controller + Service + Repository + DTO + Profile + ErrorCodes).

  2. Registro de ND dedicado: endpoint específico para generar una Nota de Débito,
     con validaciones propias. Reemplaza el uso de RegisterMovement con tipo 3.

  ══════════════════════════════════════════════════════════
  PARTE A — Modelo y Migración: MotivoNotaDebito
  ══════════════════════════════════════════════════════════

  1. Crear modelo Models/MotivoNotaDebito.cs:

     namespace proyecto_venta_stock.Models;

     public class MotivoNotaDebito
     {
         public int IdMotivo { get; set; }
         public string Nombre { get; set; } = null!;
         public bool Activo { get; set; } = true;
     }

  2. Registrar en Data/VentaStockContext.cs:

     public virtual DbSet<MotivoNotaDebito> MotivoNotaDebitos { get; set; }

     En OnModelCreating, agregar la configuración de la tabla:

     modelBuilder.Entity<MotivoNotaDebito>(entity =>
     {
         entity.HasKey(e => e.IdMotivo);
         entity.ToTable("motivo_nota_debito");
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

     public int? IdMotivoNd { get; set; }
     public virtual MotivoNotaDebito? IdMotivoNdNavigation { get; set; }

  4. Registrar la relación en VentaStockContext.OnModelCreating,
     dentro de la configuración de MovimientoCc:

     entity.Property(e => e.IdMotivoNd).HasColumnName("id_motivo_nd");
     entity.HasOne(d => d.IdMotivoNdNavigation)
           .WithMany()
           .HasForeignKey(d => d.IdMotivoNd)
           .HasConstraintName("fk_movimiento_cc_motivo_nd");

  5. Crear migración:

     dotnet tool run dotnet-ef migrations add AddMotivoNotaDebito

     Verificar que la migración cree:
     - Tabla motivo_nota_debito con columnas id_motivo, nombre, activo
     - Columna id_motivo_nd en movimiento_cc (nullable int, FK)

  6. Aplicar migración:

     dotnet tool run dotnet-ef database update

  7. Seed de datos iniciales — insertar motivos predefinidos.
     Esto puede hacerse con un script SQL o en la migración:

     INSERT INTO motivo_nota_debito (nombre, activo) VALUES ('Interés por mora', true);
     INSERT INTO motivo_nota_debito (nombre, activo) VALUES ('Ajuste de precio', true);

  ══════════════════════════════════════════════════════════
  PARTE B — CRUD de Motivos de Nota de Débito
  ══════════════════════════════════════════════════════════

  IMPORTANTE: Este CRUD sigue EXACTAMENTE el mismo patrón que el CRUD de
  AccountConfig (Features/CurrentAccount/). Replicar la estructura de archivos
  y la lógica, cambiando los nombres de las clases y propiedades.

  Ubicar todos los archivos dentro de Features/CurrentAccount/ para mantenerlos
  en el mismo módulo.

  ─────────────────────────────────────────────────────────
  B.1 — DTOs
  ─────────────────────────────────────────────────────────

  Crear en Features/CurrentAccount/DTO/DebitNoteReasonDTO/:

  1. DebitNoteReasonDTO.cs (respuesta):

     namespace venta_stock_webapi.CurrentAccount.DTO.DebitNoteReasonDTO;

     public class DebitNoteReasonDTO
     {
         public int IdMotivo { get; set; }
         public string Nombre { get; set; } = null!;
         public bool Activo { get; set; }
     }

  2. CreateDebitNoteReasonDTO.cs:

     using System.ComponentModel.DataAnnotations;

     namespace venta_stock_webapi.CurrentAccount.DTO.DebitNoteReasonDTO;

     public class CreateDebitNoteReasonDTO
     {
         [Required(ErrorMessage = "El nombre del motivo es obligatorio.")]
         [MaxLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
         public string Nombre { get; set; } = null!;
     }

  3. UpdateDebitNoteReasonDTO.cs:

     using System.ComponentModel.DataAnnotations;

     namespace venta_stock_webapi.CurrentAccount.DTO.DebitNoteReasonDTO;

     public class UpdateDebitNoteReasonDTO
     {
         [Required(ErrorMessage = "El ID del motivo es obligatorio.")]
         public int IdMotivo { get; set; }

         [Required(ErrorMessage = "El nombre del motivo es obligatorio.")]
         [MaxLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
         public string Nombre { get; set; } = null!;
     }

  ─────────────────────────────────────────────────────────
  B.2 — Repositorio
  ─────────────────────────────────────────────────────────

  Crear en Features/CurrentAccount/Repository/DebitNoteReasonRepository/:

  1. IDebitNoteReasonRepository.cs:

     using proyecto_venta_stock.Models;

     namespace venta_stock_webapi.CurrentAccount.Repository;

     public interface IDebitNoteReasonRepository
     {
         Task<MotivoNotaDebito?> GetByIdAsync(int idMotivo);
         Task<List<MotivoNotaDebito>> GetAllAsync(bool? activo = null);
         Task CreateAsync(MotivoNotaDebito motivo);
         Task<int> UpdateAsync(MotivoNotaDebito motivo);
         Task ToggleStateAsync(int idMotivo, bool activo);
         Task<bool> ExistsByNameAsync(string nombre);
         Task<bool> ExistsByNameAsync(int id, string nombre);
     }

  2. DebitNoteReasonRepository.cs:

     Implementar con EF Core siguiendo el mismo patrón de AccountConfigRepository:

     - GetByIdAsync: FirstOrDefaultAsync
     - GetAllAsync: con filtro opcional por Activo, OrderBy Nombre
     - CreateAsync: AddAsync + SaveChangesAsync
     - UpdateAsync: ExecuteUpdateAsync (solo campo Nombre)
     - ToggleStateAsync: ExecuteUpdateAsync (solo campo Activo)
     - ExistsByNameAsync(string): AnyAsync nombre == nombre
     - ExistsByNameAsync(int, string): AnyAsync nombre == nombre && id != id

  ─────────────────────────────────────────────────────────
  B.3 — Servicio
  ─────────────────────────────────────────────────────────

  Crear en Features/CurrentAccount/Services/DebitNoteReasonService/:

  1. IDebitNoteReasonService.cs:

     using proyecto_venta_stock.Shared.ResultPattern;
     using venta_stock_webapi.CurrentAccount.DTO.DebitNoteReasonDTO;

     namespace venta_stock_webapi.CurrentAccount.Services.DebitNoteReasonService;

     public interface IDebitNoteReasonService
     {
         Task<Result<DebitNoteReasonDTO>> GetById(int idMotivo);
         Task<Result<List<DebitNoteReasonDTO>>> GetAll(bool? activo = null);
         Task<Result<string>> Create(CreateDebitNoteReasonDTO dto);
         Task<Result<string>> Update(UpdateDebitNoteReasonDTO dto);
         Task<Result<string>> ToggleState(int idMotivo, bool activo);
     }

  2. DebitNoteReasonService.cs:

     Implementar siguiendo el mismo patrón de AccountConfigService:

     - GetById: obtener por ID, mapear a DTO, error si no existe
     - GetAll: listar con filtro opcional, mapear a DTOs
     - Create: validar unicidad de nombre, mapear, crear
     - Update: validar unicidad de nombre excluyendo ID actual, actualizar
     - ToggleState: verificar existencia, cambiar estado

  ─────────────────────────────────────────────────────────
  B.4 — AutoMapper Profile
  ─────────────────────────────────────────────────────────

  Crear Features/CurrentAccount/Profile/DebitNoteReasonProfile.cs:

     using proyecto_venta_stock.Models;
     using venta_stock_webapi.CurrentAccount.DTO.DebitNoteReasonDTO;

     namespace venta_stock_webapi.CurrentAccount.Profile;

     public class DebitNoteReasonProfile : AutoMapper.Profile
     {
         public DebitNoteReasonProfile()
         {
             CreateMap<CreateDebitNoteReasonDTO, MotivoNotaDebito>()
                 .ForMember(dest => dest.IdMotivo, opt => opt.Ignore())
                 .ForMember(dest => dest.Activo, opt => opt.MapFrom(src => true));

             CreateMap<UpdateDebitNoteReasonDTO, MotivoNotaDebito>()
                 .ForMember(dest => dest.Activo, opt => opt.Ignore());

             CreateMap<MotivoNotaDebito, DebitNoteReasonDTO>();
         }
     }

  ─────────────────────────────────────────────────────────
  B.5 — Códigos de Error
  ─────────────────────────────────────────────────────────

  Crear Features/CurrentAccount/Message/DebitNoteReasonErrorCode.cs:

     namespace venta_stock_webapi.CurrentAccount.Message;

     public enum DebitNoteReasonCode
     {
         reason_not_found,
         reason_name_exists,
         unexpected_error
     }

     public static class DebitNoteReasonDictionary
     {
         public static readonly Dictionary<DebitNoteReasonCode, string> Messages = new()
         {
             { DebitNoteReasonCode.reason_not_found,
               "El motivo de nota de débito indicado no existe." },
             { DebitNoteReasonCode.reason_name_exists,
               "Ya existe un motivo de nota de débito con ese nombre." },
             { DebitNoteReasonCode.unexpected_error,
               "Ocurrió un error inesperado, por favor intente nuevamente." }
         };
     }

  ─────────────────────────────────────────────────────────
  B.6 — Controller
  ─────────────────────────────────────────────────────────

  Crear Features/CurrentAccount/Controllers/DebitNoteReasonController.cs:

     [ApiController]
     [Route("api/[controller]")]
     [Authorize]
     public class DebitNoteReasonController : ControllerBase

     Endpoints (seguir el mismo patrón de AccountConfigController):

     | Método  | Ruta                               | Policy         | Acción                              |
     |---------|-------------------------------------|----------------|-------------------------------------|
     | GET     | reasons/{idMotivo}                  | PERM:CC_VIEW   | Obtener motivo por ID               |
     | GET     | reasons                             | PERM:CC_VIEW   | Listar motivos (?activo=true/false) |
     | POST    | reasons                             | PERM:CC_MANAGE | Crear nuevo motivo                  |
     | PUT     | reasons                             | PERM:CC_MANAGE | Actualizar nombre de motivo         |
     | DELETE  | toggle-state/{idMotivo}/{activo}    | PERM:CC_MANAGE | Activar/desactivar motivo           |

  ─────────────────────────────────────────────────────────
  B.7 — Inyección de Dependencias (Program.cs)
  ─────────────────────────────────────────────────────────

  Agregar en Program.cs, en la sección donde se registran los servicios de CurrentAccount:

     builder.Services.AddScoped<IDebitNoteReasonRepository, DebitNoteReasonRepository>();
     builder.Services.AddScoped<IDebitNoteReasonService, DebitNoteReasonService>();

  ══════════════════════════════════════════════════════════
  PARTE C — Registro de Nota de Débito (endpoint dedicado)
  ══════════════════════════════════════════════════════════

  La Nota de Débito ya NO se registra vía RegisterMovement genérico.
  Se crea un endpoint dedicado con validaciones específicas.

  ─────────────────────────────────────────────────────────
  C.1 — DTO para registrar ND
  ─────────────────────────────────────────────────────────

  Crear Features/CurrentAccount/DTO/MovementDTO/RegisterDebitNoteDTO.cs:

     using System.ComponentModel.DataAnnotations;

     namespace venta_stock_webapi.CurrentAccount.DTO.MovementDTO;

     public class RegisterDebitNoteDTO : IValidatableObject
     {
         [Required(ErrorMessage = "El ID del cliente es obligatorio.")]
         public int IdCliente { get; set; }

         [Required(ErrorMessage = "El importe es obligatorio.")]
         public decimal Importe { get; set; }

         [Required(ErrorMessage = "El ID del motivo es obligatorio.")]
         public int IdMotivo { get; set; }

         /// <summary>
         /// Opcional. Si se especifica, la ND está vinculada a una venta
         /// (caso "Ajuste de precio").
         /// </summary>
         public int? IdVenta { get; set; }

         /// <summary>
         /// Detalle adicional opcional ingresado por el operario.
         /// </summary>
         public string? DetalleAdicional { get; set; }

         [Required(ErrorMessage = "El ID del usuario que registra es obligatorio.")]
         public int IdUsuarioRegistra { get; set; }

         public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
         {
             if (Importe <= 0)
             {
                 yield return new ValidationResult(
                     "El importe debe ser mayor que 0.",
                     new[] { nameof(Importe) });
             }
         }
     }

  ─────────────────────────────────────────────────────────
  C.2 — Agregar códigos de error al módulo de CC
  ─────────────────────────────────────────────────────────

  Agregar a CurrentAccountCode (enum existente en CurrentAccountCode.cs):

     debit_note_reason_not_found,
     debit_note_reason_inactive,
     sale_not_found,
     sale_does_not_belong_to_client

  Agregar a CurrentAccountDictionary (diccionario existente):

     { CurrentAccountCode.debit_note_reason_not_found,
       "El motivo de nota de débito indicado no existe." },
     { CurrentAccountCode.debit_note_reason_inactive,
       "El motivo de nota de débito indicado no está activo." },
     { CurrentAccountCode.sale_not_found,
       "La venta indicada no existe." },
     { CurrentAccountCode.sale_does_not_belong_to_client,
       "La venta indicada no pertenece al cliente." }

  ─────────────────────────────────────────────────────────
  C.3 — Repositorio: método para verificar venta
  ─────────────────────────────────────────────────────────

  Agregar a IAccountMovementRepository e implementar en AccountMovementRepository:

  Task<Ventum?> GetSaleByIdAndClient(int idVenta, int idCliente)
  - Busca en _context.Venta
  - Filtra por IdVenta == idVenta AND IdCliente == idCliente
  - AsNoTracking (solo lectura para validación)
  - Retorna null si no existe o si la venta no es del cliente

  ─────────────────────────────────────────────────────────
  C.4 — Servicio: método RegisterDebitNote
  ─────────────────────────────────────────────────────────

  Agregar a ICurrentAccountService:

     Task<Result<int>> RegisterDebitNote(RegisterDebitNoteDTO dto);

  Implementar en CurrentAccountService:

     public async Task<Result<int>> RegisterDebitNote(RegisterDebitNoteDTO dto)
     {
         try
         {
             // 1. Verificar que el cliente tiene cuenta corriente
             var lastMovement = await _accountMovementRepository.GetLastMovement(dto.IdCliente);
             if (lastMovement is null)
                 return Result<int>.Failure(CurrentAccountCode.account_not_found);

             // 2. Verificar que el motivo de ND existe y está activo
             var motivo = await _debitNoteReasonRepository.GetByIdAsync(dto.IdMotivo);
             if (motivo is null)
                 return Result<int>.Failure(CurrentAccountCode.debit_note_reason_not_found);
             if (!motivo.Activo)
                 return Result<int>.Failure(CurrentAccountCode.debit_note_reason_inactive);

             // 3. Si viene IdVenta, validar que la venta exista y pertenezca al cliente
             if (dto.IdVenta.HasValue)
             {
                 var venta = await _accountMovementRepository
                     .GetSaleByIdAndClient(dto.IdVenta.Value, dto.IdCliente);
                 if (venta is null)
                     return Result<int>.Failure(CurrentAccountCode.sale_not_found);
                 if (venta.IdCliente != dto.IdCliente)
                     return Result<int>.Failure(CurrentAccountCode.sale_does_not_belong_to_client);
             }

             // 4. Calcular nuevo saldo/límite usando DebitNoteStrategy
             decimal balanceBase = lastMovement.SaldoActual ?? 0;
             decimal limitBase = lastMovement.LimiteCuenta ?? 0;

             var strategy = _movementStrategyFactory.GetStrategy(TypeMovement.NOTA_DEBITO);
             var result = strategy.Calculate(balanceBase, limitBase, dto.Importe);

             // 5. Construir el detalle del movimiento
             string detalle = motivo.Nombre;
             if (!string.IsNullOrWhiteSpace(dto.DetalleAdicional))
                 detalle += $" — {dto.DetalleAdicional}";

             // 6. Crear el movimiento tipo ND
             var newMovement = new MovimientoCc
             {
                 IdCliente = dto.IdCliente,
                 Importe = dto.Importe,
                 Detalle = detalle,
                 IdEstado = 2,  // Aprobado
                 IdTipoMovimiento = (int)TypeMovement.NOTA_DEBITO,
                 IdUsuarioRegistra = dto.IdUsuarioRegistra,
                 IdVenta = dto.IdVenta,
                 SaldoActual = result.NewBalance,
                 LimiteCuenta = result.NewLimit,
                 Fecha = DateTime.Now,
                 IdMotivoNd = dto.IdMotivo
             };

             await _accountMovementRepository.CreateMovement(newMovement);

             return Result<int>.Success(newMovement.IdMovimiento);
         }
         catch (Exception ex)
         {
             _logger.LogError(ex, "Error registering debit note for client {ClientId}", dto.IdCliente);
             return Result<int>.Failure(CurrentAccountCode.unexpected_error);
         }
     }

  IMPORTANTE: Agregar IDebitNoteReasonRepository como dependencia del constructor
  de CurrentAccountService:

     private readonly IDebitNoteReasonRepository _debitNoteReasonRepository;

     // Agregar al constructor:
     public CurrentAccountService(
         ...,
         IDebitNoteReasonRepository debitNoteReasonRepository)
     {
         ...
         _debitNoteReasonRepository = debitNoteReasonRepository;
     }

  ─────────────────────────────────────────────────────────
  C.5 — Controller: endpoint de Nota de Débito
  ─────────────────────────────────────────────────────────

  Agregar en CurrentAccountController:

     [Authorize(Policy = "PERM:CC_MANAGE")]
     [HttpPost("register-debit-note")]
     public async Task<IActionResult> RegisterDebitNote([FromBody] RegisterDebitNoteDTO dto)
     {
         if (!ModelState.IsValid)
             return BadRequest(ModelState);

         var result = await _currentAccountService.RegisterDebitNote(dto);

         if (!result.IsSuccess)
         {
             var code = (CurrentAccountCode)result.ErrorCode;
             var errorMessage = MessageProvider.Get(CurrentAccountDictionary.Messages, code);
             return BadRequest(errorMessage);
         }

         // Retorna el IdMovimiento para que el front pueda solicitar el PDF
         return Ok(new { idMovimiento = result.Value });
     }

  ─────────────────────────────────────────────────────────
  C.6 — Incluir motivo en el DTO de respuesta y el PDF
  ─────────────────────────────────────────────────────────

  1. Agregar a AccountMovementDTO:

     public int? IdMotivoNd { get; set; }
     public string? MotivoNd { get; set; }

  2. Actualizar CurrentAccountProfile (mapeo MovimientoCc → AccountMovementDTO):

     .ForMember(dest => dest.IdMotivoNd, opt => opt.MapFrom(src => src.IdMotivoNd))
     .ForMember(dest => dest.MotivoNd, opt => opt.MapFrom(src =>
         src.IdMotivoNdNavigation != null ? src.IdMotivoNdNavigation.Nombre : null))

  3. Actualizar GetMovements en AccountMovementRepository para incluir
     la navegación al motivo:

     Agregar .Include(m => m.IdMotivoNdNavigation) en la query de GetMovements.

  4. Actualizar GetMovementById en AccountMovementRepository:

     Agregar .Include(m => m.IdMotivoNdNavigation) en la query.

  5. Actualizar GeneratePaymentReceiptAsync en CurrentAccountService:

     En el PDF, si movement.IdTipoMovimiento == 3 (ND) y movement.IdMotivoNdNavigation
     no es null, incluir el motivo en el detalle o como campo adicional del dataSource.

     Agregar a PaymentReceiptDataSource:
     public string? MotivoNd { get; set; }

     Popular en el dataSource:
     MotivoNd = movement.IdMotivoNdNavigation?.Nombre

  6. Actualizar PaymentReceiptDocument para mostrar el motivo de ND:

     En ComposeContent, antes de la descripción, si _data.MotivoNd no es null,
     agregar una fila:

     if (!string.IsNullOrEmpty(_data.MotivoNd))
     {
         col.Item().Row(row =>
         {
             row.ConstantItem(160).Text("Motivo:").Bold();
             row.RelativeItem().Text(_data.MotivoNd);
         });
     }

  ══════════════════════════════════════════════════════════
  PARTE D — Excluir ND del endpoint genérico RegisterMovement
  ══════════════════════════════════════════════════════════

  Ahora que la ND tiene su propio endpoint, NO debe poder registrarse
  como tipo 3 vía POST /api/CurrentAccount/register-movement.

  Modificar AddMovementDTO.Validate (IValidatableObject):

  Agregar validación:

     if (IdTipoMovimiento == 3)
     {
         yield return new ValidationResult(
             "La Nota de Débito debe registrarse mediante el endpoint dedicado.",
             new[] { nameof(IdTipoMovimiento) });
     }

  Esta validación también debe aplicar al tipo 7 (INTERES_SALDO_GLOBAL, deprecado):

     if (IdTipoMovimiento == 7)
     {
         yield return new ValidationResult(
             "El tipo Interés Saldo Global está deprecado. Use Nota de Débito con motivo 'Interés por mora'.",
             new[] { nameof(IdTipoMovimiento) });
     }

  ══════════════════════════════════════════════════════════
  PARTE E — Deprecar INTERES_SALDO_GLOBAL
  ══════════════════════════════════════════════════════════

  1. En AccountMovementRepository.GetMovementType(), el tipo 7 ya debería
     estar excluido del listado (verificar que el filtro existente lo excluya).
     El filtro actual excluye ids 2 y 5:

     .Where(t => t.IdMovimiento != 2 && t.IdMovimiento != 5)

     Agregar exclusión del tipo 7:

     .Where(t => t.IdMovimiento != 2 && t.IdMovimiento != 5 && t.IdMovimiento != 7)

  2. NO borrar el tipo 7 del enum TypeMovement ni de la tabla tipo_movimiento
     (puede haber datos históricos). Solo se oculta de la selección.

  ══════════════════════════════════════════════════════════
  RESUMEN DE ARCHIVOS A CREAR/MODIFICAR
  ══════════════════════════════════════════════════════════

  ── Nuevos archivos ──
  Models/MotivoNotaDebito.cs
  Features/CurrentAccount/DTO/DebitNoteReasonDTO/DebitNoteReasonDTO.cs
  Features/CurrentAccount/DTO/DebitNoteReasonDTO/CreateDebitNoteReasonDTO.cs
  Features/CurrentAccount/DTO/DebitNoteReasonDTO/UpdateDebitNoteReasonDTO.cs
  Features/CurrentAccount/DTO/MovementDTO/RegisterDebitNoteDTO.cs
  Features/CurrentAccount/Repository/DebitNoteReasonRepository/IDebitNoteReasonRepository.cs
  Features/CurrentAccount/Repository/DebitNoteReasonRepository/DebitNoteReasonRepository.cs
  Features/CurrentAccount/Services/DebitNoteReasonService/IDebitNoteReasonService.cs
  Features/CurrentAccount/Services/DebitNoteReasonService/DebitNoteReasonService.cs
  Features/CurrentAccount/Profile/DebitNoteReasonProfile.cs
  Features/CurrentAccount/Controllers/DebitNoteReasonController.cs
  Features/CurrentAccount/Message/DebitNoteReasonErrorCode.cs

  ── Archivos a modificar ──
  Models/MovimientoCc.cs                    → agregar IdMotivoNd + navegación
  Data/VentaStockContext.cs                 → DbSet + configuración de MotivoNotaDebito + FK
  Program.cs                                → DI de nuevos servicios
  Features/CurrentAccount/Controllers/CurrentAccountController.cs  → nuevo endpoint register-debit-note
  Features/CurrentAccount/Services/CurrentAccountService/ICurrentAccountService.cs → agregar RegisterDebitNote
  Features/CurrentAccount/Services/CurrentAccountService/CurrentAccountService.cs  → implementar RegisterDebitNote + nueva dependencia
  Features/CurrentAccount/Repository/AccountMovementRepository/IAccountMovementRepository.cs → GetSaleByIdAndClient
  Features/CurrentAccount/Repository/AccountMovementRepository/AccountMovementRepository.cs  → implementar + Include al motivo
  Features/CurrentAccount/DTO/MovementDTO/AccountMovementDTO.cs    → agregar IdMotivoNd, MotivoNd
  Features/CurrentAccount/DTO/MovementDTO/AddMovementDTO.cs        → bloquear tipo 3 y 7
  Features/CurrentAccount/Profile/CurrentAccountProfile.cs         → mapeo de motivo
  Features/CurrentAccount/Message/CurrentAccountCode.cs            → nuevos códigos de error
  Features/CurrentAccount/PDF/PaymentReceiptDataSource.cs          → agregar MotivoNd
  Features/CurrentAccount/PDF/PaymentReceiptDocument.cs            → mostrar motivo en PDF

  ── Migración ──
  Migrations/                               → nueva migración AddMotivoNotaDebito

  ══════════════════════════════════════════════════════════
  VALIDACIONES CRÍTICAS (checklist)
  ══════════════════════════════════════════════════════════

  ✅ El motivo de ND debe existir y estar activo
  ✅ Si se vincula a una venta, la venta debe existir Y pertenecer al cliente
  ✅ El importe debe ser mayor a 0
  ✅ El cliente debe tener cuenta corriente (último movimiento != null)
  ✅ El tipo 3 (ND) no puede registrarse vía RegisterMovement genérico
  ✅ El tipo 7 (INTERES_SALDO_GLOBAL) queda deprecado y bloqueado
  ✅ El CRUD de motivos valida unicidad de nombre
  ✅ Los motivos no se borran, se desactivan (toggle)

  ══════════════════════════════════════════════════════════
  ORDEN DE IMPLEMENTACIÓN RECOMENDADO
  ══════════════════════════════════════════════════════════

  1. Modelo + Migración (Parte A)
  2. CRUD de Motivos completo (Parte B) — verificar que compila y funciona
  3. Endpoint de ND dedicado (Parte C) — verificar que compila y funciona
  4. Bloquear tipo 3 y 7 en RegisterMovement genérico (Parte D)
  5. Deprecar tipo 7 del listado (Parte E)
  6. Verificar PDF con motivo incluido

  ══════════════════════════════════════════════════════════
  NOTAS FINALES
  ══════════════════════════════════════════════════════════

  - La Nota de Crédito (tipo 4) se implementará desde el módulo de Ventas
    en un flujo separado. NO tocar la NC en esta implementación.

  - El PDF de comprobante de ND ya está soportado por GeneratePaymentReceiptAsync
    (tipo 3 → "NOTA DE DÉBITO", color rojo). Solo se agrega el campo MotivoNd.

  - La auditoría se registra automáticamente si el interceptor de auditoría
    del proyecto está habilitado para la tabla movimiento_cc.

  - El endpoint retorna el idMovimiento para que el frontend pueda solicitar
    el PDF de inmediato vía GET /api/CurrentAccount/payment-receipt/{idMovimiento}.
```
