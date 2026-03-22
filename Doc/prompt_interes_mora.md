# Prompt — Implementación: Interés por Mora en Cuenta Corriente


```
Contexto del proyecto:
  - ASP.NET Core 8, EF Core, PostgreSQL, patrón Repository + Service + Result<T>
  - El módulo de CC ya tiene RegisterDebitNote (POST register-debit-note) que registra
    Notas de Débito con motivo predefinido (tabla motivo_nota_debito).
  - Ya existe DebitNoteStrategy que calcula: saldo + importe, límite - importe.
  - El último movimiento de cada cliente en movimiento_cc contiene siempre el
    saldo deudor actual (SaldoActual) y el límite disponible (LimiteCuenta).
  - Ya existe ConfiguracionCc para límites de crédito predefinidos (NO tocar).

  ANTES DE IMPLEMENTAR, lee la documentación del proyecto:
  - Doc/modulo_clientes.md
  - Doc/modulo_cuenta_corriente.md

  Y luego lee el código fuente completo de Features/CurrentAccount completo,
  incluyendo todos los controllers, services, repositories, DTOs y profiles.

  ══════════════════════════════════════════════════════════
  OBJETIVO
  ══════════════════════════════════════════════════════════

  Implementar el sistema de interés por mora con tres componentes:

  1. CRUD de ConfiguracionInteres: tabla nueva para gestionar configuraciones
     de interés (porcentaje + día de vencimiento). Solo una puede estar activa a la vez.

  2. Panel de morosos: endpoint que lista clientes con deuda vencida y sin
     interés aplicado en el mes en curso.

  3. Aplicar interés: endpoints para aplicar la ND de interés a un cliente
     individual o a todos los morosos en un solo llamado.

  ══════════════════════════════════════════════════════════
  PARTE A — Modelo y Migración: ConfiguracionInteres
  ══════════════════════════════════════════════════════════

  1. Crear modelo Models/ConfiguracionInteres.cs:

     namespace proyecto_venta_stock.Models;

     public class ConfiguracionInteres
     {
         public int IdConfig { get; set; }

         /// <summary>Nombre descriptivo, ej. "Interés Marzo 2025"</summary>
         public string Nombre { get; set; } = null!;

         /// <summary>Porcentaje a aplicar sobre el saldo deudor. Ej: 5.00 = 5%</summary>
         public decimal PorcentajeInteres { get; set; }

         /// <summary>
         /// Día del mes hasta el cual el cliente puede pagar sin mora.
         /// El sistema considera vencida la deuda a partir del día siguiente.
         /// Ej: 10 → vence el día 11.
         /// </summary>
         public int DiaVencimiento { get; set; }

         /// <summary>
         /// Solo UNA configuración puede tener EsActual = true a la vez.
         /// Es la configuración vigente del sistema.
         /// </summary>
         public bool EsActual { get; set; } = false;
     }

  2. Registrar en Data/VentaStockContext.cs:

     public virtual DbSet<ConfiguracionInteres> ConfiguracionIntereses { get; set; }

     En OnModelCreating, agregar la configuración de la tabla:

     modelBuilder.Entity<ConfiguracionInteres>(entity =>
     {
         entity.HasKey(e => e.IdConfig);
         entity.ToTable("configuracion_interes");
         entity.Property(e => e.IdConfig)
               .HasColumnName("id_config")
               .ValueGeneratedOnAdd();
         entity.Property(e => e.Nombre)
               .HasColumnName("nombre")
               .HasMaxLength(100)
               .IsRequired();
         entity.Property(e => e.PorcentajeInteres)
               .HasColumnName("porcentaje_interes")
               .HasPrecision(5, 2)
               .IsRequired();
         entity.Property(e => e.DiaVencimiento)
               .HasColumnName("dia_vencimiento")
               .IsRequired();
         entity.Property(e => e.EsActual)
               .HasColumnName("es_actual")
               .HasDefaultValue(false);
     });

  3. Crear migración:

     dotnet ef migrations add AddConfiguracionInteres --output-dir Migrations

     Verificar que la migración cree la tabla configuracion_interes con las
     columnas id_config, nombre, porcentaje_interes, dia_vencimiento, es_actual.

  4. Aplicar migración:

     dotnet ef database update

  ══════════════════════════════════════════════════════════
  PARTE B — CRUD de ConfiguracionInteres
  ══════════════════════════════════════════════════════════

  Sigue el mismo patrón estructural que AccountConfig/DebitNoteReason.

  ─────────────────────────────────────────────────────────
  B.1 — DTOs
  ─────────────────────────────────────────────────────────

  Crear en Features/CurrentAccount/DTO/InterestConfigDTO/:

  1. InterestConfigDTO.cs (respuesta):

     public class InterestConfigDTO
     {
         public int IdConfig { get; set; }
         public string Nombre { get; set; } = null!;
         public decimal PorcentajeInteres { get; set; }
         public int DiaVencimiento { get; set; }
         public bool EsActual { get; set; }
     }

  2. CreateInterestConfigDTO.cs:

     public class CreateInterestConfigDTO
     {
         [Required(ErrorMessage = "El nombre es obligatorio.")]
         [MaxLength(100)]
         public string Nombre { get; set; } = null!;

         [Required(ErrorMessage = "El porcentaje de interés es obligatorio.")]
         [Range(0.01, 100, ErrorMessage = "El porcentaje debe estar entre 0.01 y 100.")]
         public decimal PorcentajeInteres { get; set; }

         [Required(ErrorMessage = "El día de vencimiento es obligatorio.")]
         [Range(1, 28, ErrorMessage = "El día de vencimiento debe estar entre 1 y 28.")]
         public int DiaVencimiento { get; set; }
     }

  3. UpdateInterestConfigDTO.cs:

     public class UpdateInterestConfigDTO
     {
         [Required]
         public int IdConfig { get; set; }

         [Required(ErrorMessage = "El nombre es obligatorio.")]
         [MaxLength(100)]
         public string Nombre { get; set; } = null!;

         [Required(ErrorMessage = "El porcentaje de interés es obligatorio.")]
         [Range(0.01, 100, ErrorMessage = "El porcentaje debe estar entre 0.01 y 100.")]
         public decimal PorcentajeInteres { get; set; }

         [Required(ErrorMessage = "El día de vencimiento es obligatorio.")]
         [Range(1, 28, ErrorMessage = "El día de vencimiento debe estar entre 1 y 28.")]
         public int DiaVencimiento { get; set; }
     }

  ─────────────────────────────────────────────────────────
  B.2 — Repositorio
  ─────────────────────────────────────────────────────────

  Crear en Features/CurrentAccount/Repository/InterestConfigRepository/:

  1. IInterestConfigRepository.cs:

     public interface IInterestConfigRepository
     {
         Task<ConfiguracionInteres?> GetByIdAsync(int idConfig);
         Task<List<ConfiguracionInteres>> GetAllAsync();
         Task<ConfiguracionInteres?> GetCurrentAsync();
         Task CreateAsync(ConfiguracionInteres config);
         Task UpdateAsync(ConfiguracionInteres config);
         Task SetAsCurrentAsync(int idConfig);
         Task<bool> ExistsByNameAsync(string nombre);
         Task<bool> ExistsByNameAsync(int id, string nombre);
     }

  2. InterestConfigRepository.cs — implementar con EF Core:

     - GetByIdAsync: FirstOrDefaultAsync por IdConfig.
     - GetAllAsync: ordenados por Nombre ASC. AsNoTracking.
     - GetCurrentAsync: FirstOrDefaultAsync donde EsActual == true. AsNoTracking.
     - CreateAsync: AddAsync + SaveChangesAsync.
     - UpdateAsync: ExecuteUpdateAsync sobre Nombre, PorcentajeInteres, DiaVencimiento.
     - SetAsCurrentAsync: dos pasos en una transacción:
         a. ExecuteUpdateAsync → poner EsActual = false en TODOS los registros.
         b. ExecuteUpdateAsync → poner EsActual = true en el IdConfig dado.
     - ExistsByNameAsync(string): AnyAsync nombre == valor.
     - ExistsByNameAsync(int, string): AnyAsync nombre == valor && id != valor.

  ─────────────────────────────────────────────────────────
  B.3 — Servicio
  ─────────────────────────────────────────────────────────

  Crear en Features/CurrentAccount/Services/InterestConfigService/:

  1. IInterestConfigService.cs:

     public interface IInterestConfigService
     {
         Task<Result<InterestConfigDTO>> GetById(int idConfig);
         Task<Result<List<InterestConfigDTO>>> GetAll();
         Task<Result<InterestConfigDTO>> GetCurrent();
         Task<Result<string>> Create(CreateInterestConfigDTO dto);
         Task<Result<string>> Update(UpdateInterestConfigDTO dto);
         Task<Result<string>> SetAsCurrent(int idConfig);
     }

  2. InterestConfigService.cs — implementar:

     - GetById: obtener por ID, mapear, error si no existe.
     - GetAll: listar todas, mapear.
     - GetCurrent: retornar la config activa (EsActual = true).
       Si no hay ninguna activa, retornar Failure(no_active_config).
     - Create: validar unicidad de nombre, mapear (EsActual = false por defecto), crear.
     - Update: validar existencia y unicidad de nombre excluyendo ID actual, actualizar.
     - SetAsCurrent: verificar existencia del config, llamar a SetAsCurrentAsync.

  ─────────────────────────────────────────────────────────
  B.4 — AutoMapper Profile
  ─────────────────────────────────────────────────────────

  Crear Features/CurrentAccount/Profile/InterestConfigProfile.cs:

     public class InterestConfigProfile : AutoMapper.Profile
     {
         public InterestConfigProfile()
         {
             CreateMap<CreateInterestConfigDTO, ConfiguracionInteres>()
                 .ForMember(dest => dest.IdConfig, opt => opt.Ignore())
                 .ForMember(dest => dest.EsActual, opt => opt.MapFrom(_ => false));

             CreateMap<UpdateInterestConfigDTO, ConfiguracionInteres>()
                 .ForMember(dest => dest.EsActual, opt => opt.Ignore());

             CreateMap<ConfiguracionInteres, InterestConfigDTO>();
         }
     }

  ─────────────────────────────────────────────────────────
  B.5 — Códigos de Error
  ─────────────────────────────────────────────────────────

  Crear Features/CurrentAccount/Message/InterestConfigErrorCode.cs:

     public enum InterestConfigCode
     {
         config_not_found,
         config_name_exists,
         no_active_config,
         unexpected_error
     }

     public static class InterestConfigDictionary
     {
         public static readonly Dictionary<InterestConfigCode, string> Messages = new()
         {
             { InterestConfigCode.config_not_found,
               "La configuración de interés indicada no existe." },
             { InterestConfigCode.config_name_exists,
               "Ya existe una configuración de interés con ese nombre." },
             { InterestConfigCode.no_active_config,
               "No hay ninguna configuración de interés activa. Configure una antes de continuar." },
             { InterestConfigCode.unexpected_error,
               "Ocurrió un error inesperado, por favor intente nuevamente." }
         };
     }

  ─────────────────────────────────────────────────────────
  B.6 — Controller: CRUD de configuración
  ─────────────────────────────────────────────────────────

  Crear Features/CurrentAccount/Controllers/InterestConfigController.cs:

     [ApiController]
     [Route("api/[controller]")]
     [Authorize]
     public class InterestConfigController : ControllerBase

     Endpoints:

     | Método | Ruta                        | Policy         | Acción                             |
     |--------|-----------------------------|----------------|------------------------------------|
     | GET    | interest-configs            | PERM:CC_VIEW   | Listar todas las configuraciones   |
     | GET    | interest-configs/{idConfig} | PERM:CC_VIEW   | Obtener configuración por ID       |
     | GET    | interest-configs/current    | PERM:CC_VIEW   | Obtener la configuración activa    |
     | POST   | interest-configs            | PERM:CC_MANAGE | Crear nueva configuración          |
     | PUT    | interest-configs            | PERM:CC_MANAGE | Actualizar configuración           |
     | PUT    | interest-configs/set-current/{idConfig} | PERM:CC_MANAGE | Marcar como configuración activa |

  ─────────────────────────────────────────────────────────
  B.7 — Inyección de Dependencias (Program.cs)
  ─────────────────────────────────────────────────────────

     builder.Services.AddScoped<IInterestConfigRepository, InterestConfigRepository>();
     builder.Services.AddScoped<IInterestConfigService, InterestConfigService>();

  ══════════════════════════════════════════════════════════
  PARTE C — Panel de Morosos y Aplicar Interés
  ══════════════════════════════════════════════════════════

  ─────────────────────────────────────────────────────────
  C.1 — DTO de respuesta para cliente moroso
  ─────────────────────────────────────────────────────────

  Crear Features/CurrentAccount/DTO/MovementDTO/OverdueClientDTO.cs:

     public class OverdueClientDTO
     {
         public int IdCliente { get; set; }
         public string NombreCliente { get; set; } = null!;  // Nombre+Apellido o RazonSocial
         public string? Dni { get; set; }
         public string? Cuit { get; set; }
         public decimal SaldoDeudor { get; set; }           // SaldoActual del último movimiento
         public decimal ImporteInteres { get; set; }        // SaldoDeudor × (PorcentajeInteres/100)
         public decimal PorcentajeInteres { get; set; }     // de la ConfiguracionInteres activa
         public int DiaVencimiento { get; set; }            // de la ConfiguracionInteres activa
     }

  ─────────────────────────────────────────────────────────
  C.2 — Repositorio: nuevos métodos en IAccountMovementRepository
  ─────────────────────────────────────────────────────────

  Agregar a IAccountMovementRepository e implementar en AccountMovementRepository:

  1. Task<List<(int IdCliente, decimal SaldoActual)>> GetClientsWithDebt()
     - Obtiene el ÚLTIMO movimiento de cada cliente (MAX IdMovimiento por IdCliente).
     - Filtra clientes cuyo SaldoActual > 0.
     - Retorna pares (IdCliente, SaldoActual).
     - AsNoTracking.

     Implementación sugerida:
     _context.MovimientoCcs
         .GroupBy(m => m.IdCliente)
         .Select(g => g.OrderByDescending(m => m.Fecha)
                       .ThenByDescending(m => m.IdMovimiento)
                       .First())
         .Where(m => m.SaldoActual > 0)
         .Select(m => new { m.IdCliente, m.SaldoActual })
         .AsNoTracking()
         .ToListAsync()

  2. Task<bool> HasInterestAppliedThisMonth(int clientId)
     - Verifica si existe un movimiento de tipo NOTA_DEBITO (IdTipoMovimiento == 3)
       para el cliente, cuya Fecha esté en el mes y año actual (DateTime.Now).
     - Retorna true si ya se aplicó el interés este mes.

     Implementación sugerida:
     var now = DateTime.Now;
     return await _context.MovimientoCcs.AnyAsync(m =>
         m.IdCliente == clientId &&
         m.IdTipoMovimiento == 3 &&
         m.Fecha.HasValue &&
         m.Fecha.Value.Month == now.Month &&
         m.Fecha.Value.Year == now.Year);

  ─────────────────────────────────────────────────────────
  C.3 — Servicio: nuevos métodos en ICurrentAccountService
  ─────────────────────────────────────────────────────────

  Agregar a ICurrentAccountService:

     Task<Result<List<OverdueClientDTO>>> GetOverdueClients();
     Task<Result<string>>                ApplyInterestToClient(int clientId, int idUsuarioRegistra);
     Task<Result<string>>                ApplyInterestToAll(int idUsuarioRegistra);

  Implementar en CurrentAccountService.
  Requiere inyectar IInterestConfigRepository e IClientRepository.

  ──────────────────
  GetOverdueClients()
  ──────────────────
  1. Obtener la ConfiguracionInteres activa (GetCurrentAsync).
     Si no existe → Failure(no_active_config).

  2. Verificar si hoy es día de mora:
     if (DateTime.Now.Day <= config.DiaVencimiento)
         return Result<List<OverdueClientDTO>>.Success(new List<OverdueClientDTO>());
     // Antes del vencimiento → no hay morosos aún

  3. Obtener todos los clientes con deuda (GetClientsWithDebt).

  4. Para cada cliente con deuda:
     a. Verificar si ya tiene interés aplicado este mes (HasInterestAppliedThisMonth).
     b. Si NO tiene interés este mes → es moroso → agregar a la lista.
     c. Obtener datos del cliente (nombre, DNI, CUIT) via _clientRepository.GetByIdAsync.
     d. Calcular ImporteInteres = SaldoDeudor × (config.PorcentajeInteres / 100).

  5. Retornar lista de OverdueClientDTO.

  ────────────────────────────
  ApplyInterestToClient(clientId, idUsuarioRegistra)
  ────────────────────────────
  1. Obtener la ConfiguracionInteres activa.
     Si no existe → Failure(no_active_config).

  2. Verificar que el cliente tiene deuda (GetLastMovement, SaldoActual > 0).
     Si no tiene deuda → Failure(account_not_found) o success con mensaje "Sin deuda".

  3. Verificar que no tiene interés ya aplicado este mes (HasInterestAppliedThisMonth).
     Si ya tiene → Failure(interest_already_applied_this_month).

  4. Calcular importe: decimal importe = (lastMovement.SaldoActual ?? 0) × (config.PorcentajeInteres / 100).
     Redondear a 2 decimales: Math.Round(importe, 2).
     Si importe <= 0 → retornar Success sin crear movimiento.

  5. Calcular nuevo saldo/límite con DebitNoteStrategy.

  6. Obtener el IdMotivo del motivo "Interés por mora" de la tabla motivo_nota_debito.
     Buscar el primer motivo activo cuyo Nombre == "Interés por mora".
     Si no existe → crear el movimiento igualmente con IdMotivoNd = null pero
     con Detalle = $"Interés por mora — {config.PorcentajeInteres}% — {config.Nombre}".

  7. Crear el movimiento tipo NOTA_DEBITO:

     var newMovement = new MovimientoCc
     {
         IdCliente     = clientId,
         Importe       = importe,
         Detalle       = $"Interés por mora — {config.PorcentajeInteres}% — {config.Nombre}",
         IdEstado      = 2,   // Aprobado
         IdTipoMovimiento = (int)TypeMovement.NOTA_DEBITO,
         IdUsuarioRegistra = idUsuarioRegistra,
         SaldoActual   = calculationResult.NewBalance,
         LimiteCuenta  = calculationResult.NewLimit,
         Fecha         = DateTime.Now,
         IdMotivoNd    = idMotivo  // null si no se encontró el motivo
     };

  8. Persistir y retornar Success.

  ────────────────────────────
  ApplyInterestToAll(idUsuarioRegistra)
  ────────────────────────────
  1. Obtener la lista de morosos (reutilizar GetOverdueClients).
  2. Para cada cliente moroso, llamar a ApplyInterestToClient(clientId, idUsuarioRegistra).
  3. Acumular resultados: contar cuántos fueron aplicados y cuántos fallaron.
  4. Retornar Success con mensaje:
     $"Interés aplicado a {exitosos} clientes. {fallidos} clientes con error."

  IMPORTANTE: No usar transacción global para el bulk. Si uno falla, continuar
  con los siguientes. Esto garantiza que un error puntual no revierta todo el proceso.

  ─────────────────────────────────────────────────────────
  C.4 — Nuevos códigos de error en CurrentAccountCode
  ─────────────────────────────────────────────────────────

  Agregar a ContentAccountCode (enum existente):

     no_active_config,
     interest_already_applied_this_month,
     client_has_no_debt

  Agregar a CurrentAccountDictionary:

     { CurrentAccountCode.no_active_config,
       "No hay ninguna configuración de interés activa. Configure una antes de continuar." },
     { CurrentAccountCode.interest_already_applied_this_month,
       "El interés por mora ya fue aplicado a este cliente en el mes en curso." },
     { CurrentAccountCode.client_has_no_debt,
       "El cliente no tiene deuda pendiente." }

  ─────────────────────────────────────────────────────────
  C.5 — DTO para la solicitud de apply interest individual
  ─────────────────────────────────────────────────────────

  Crear Features/CurrentAccount/DTO/MovementDTO/ApplyInterestDTO.cs:

     public class ApplyInterestDTO
     {
         [Required]
         public int IdUsuarioRegistra { get; set; }
     }

  ─────────────────────────────────────────────────────────
  C.6 — Controller: endpoints de morosos e interés
  ─────────────────────────────────────────────────────────

  Agregar en CurrentAccountController:

  1. GET overdue-clients — Lista morosos:

     [Authorize(Policy = "PERM:CC_VIEW")]
     [HttpGet("overdue-clients")]
     public async Task<IActionResult> GetOverdueClients()
     {
         var result = await _currentAccountService.GetOverdueClients();
         if (!result.IsSuccess)
         {
             var code = (CurrentAccountCode)result.ErrorCode;
             var errorMessage = MessageProvider.Get(CurrentAccountDictionary.Messages, code);
             return BadRequest(errorMessage);
         }
         return Ok(result.Value);
     }

  2. POST apply-interest/{clientId} — Aplicar a un cliente:

     [Authorize(Policy = "PERM:CC_MANAGE")]
     [HttpPost("apply-interest/{clientId}")]
     public async Task<IActionResult> ApplyInterestToClient(int clientId, [FromBody] ApplyInterestDTO dto)
     {
         if (!ModelState.IsValid) return BadRequest(ModelState);

         var result = await _currentAccountService.ApplyInterestToClient(clientId, dto.IdUsuarioRegistra);
         if (!result.IsSuccess)
         {
             var code = (CurrentAccountCode)result.ErrorCode;
             var errorMessage = MessageProvider.Get(CurrentAccountDictionary.Messages, code);
             return BadRequest(errorMessage);
         }
         return Ok(result.Value);
     }

  3. POST apply-interest/bulk — Aplicar a todos los morosos:

     [Authorize(Policy = "PERM:CC_MANAGE")]
     [HttpPost("apply-interest/bulk")]
     public async Task<IActionResult> ApplyInterestToAll([FromBody] ApplyInterestDTO dto)
     {
         if (!ModelState.IsValid) return BadRequest(ModelState);

         var result = await _currentAccountService.ApplyInterestToAll(dto.IdUsuarioRegistra);
         if (!result.IsSuccess)
         {
             var code = (CurrentAccountCode)result.ErrorCode;
             var errorMessage = MessageProvider.Get(CurrentAccountDictionary.Messages, code);
             return BadRequest(errorMessage);
         }
         return Ok(result.Value);
     }

  ══════════════════════════════════════════════════════════
  PARTE D — Inyección de IInterestConfigRepository en CurrentAccountService
  ══════════════════════════════════════════════════════════

  CurrentAccountService ya tiene IDebitNoteReasonRepository inyectado.
  Agregar también IInterestConfigRepository:

     private readonly IInterestConfigRepository _interestConfigRepository;

     // En el constructor:
     public CurrentAccountService(
         ...,
         IInterestConfigRepository interestConfigRepository)
     {
         ...
         _interestConfigRepository = interestConfigRepository;
     }

  ══════════════════════════════════════════════════════════
  RESUMEN DE ARCHIVOS A CREAR/MODIFICAR
  ══════════════════════════════════════════════════════════

  ── Nuevos archivos ──
  Models/ConfiguracionInteres.cs
  Features/CurrentAccount/DTO/InterestConfigDTO/InterestConfigDTO.cs
  Features/CurrentAccount/DTO/InterestConfigDTO/CreateInterestConfigDTO.cs
  Features/CurrentAccount/DTO/InterestConfigDTO/UpdateInterestConfigDTO.cs
  Features/CurrentAccount/DTO/MovementDTO/OverdueClientDTO.cs
  Features/CurrentAccount/DTO/MovementDTO/ApplyInterestDTO.cs
  Features/CurrentAccount/Repository/InterestConfigRepository/IInterestConfigRepository.cs
  Features/CurrentAccount/Repository/InterestConfigRepository/InterestConfigRepository.cs
  Features/CurrentAccount/Services/InterestConfigService/IInterestConfigService.cs
  Features/CurrentAccount/Services/InterestConfigService/InterestConfigService.cs
  Features/CurrentAccount/Profile/InterestConfigProfile.cs
  Features/CurrentAccount/Controllers/InterestConfigController.cs
  Features/CurrentAccount/Message/InterestConfigErrorCode.cs

  ── Archivos a modificar ──
  Data/VentaStockContext.cs                              → DbSet + configuración de tabla
  Program.cs                                             → DI de nuevos servicios
  Features/CurrentAccount/Controllers/CurrentAccountController.cs  → 3 nuevos endpoints
  Features/CurrentAccount/Services/CurrentAccountService/ICurrentAccountService.cs  → 3 nuevos métodos
  Features/CurrentAccount/Services/CurrentAccountService/CurrentAccountService.cs   → implementar + nueva dependencia
  Features/CurrentAccount/Repository/AccountMovementRepository/IAccountMovementRepository.cs → 2 nuevos métodos
  Features/CurrentAccount/Repository/AccountMovementRepository/AccountMovementRepository.cs  → implementar
  Features/CurrentAccount/Message/CurrentAccountCode.cs  → 3 nuevos códigos de error

  ── Migración ──
  Migrations/                                            → nueva migración AddConfiguracionInteres

  ══════════════════════════════════════════════════════════
  VALIDACIONES CRÍTICAS (checklist)
  ══════════════════════════════════════════════════════════

  ✅ No se puede aplicar interés si no hay ConfiguracionInteres activa
  ✅ No se puede aplicar interés si el cliente no tiene deuda (SaldoActual <= 0)
  ✅ No se puede aplicar interés dos veces en el mismo mes (HasInterestAppliedThisMonth)
  ✅ El importe del interés se calcula automáticamente (no lo ingresa el operario)
  ✅ El panel de morosos solo lista clientes con deuda vencida Y sin interés del mes
  ✅ GetOverdueClients retorna lista vacía si no venció el plazo (hoy <= DiaVencimiento)
  ✅ Solo UNA ConfiguracionInteres puede tener EsActual = true a la vez
  ✅ En bulk apply: un error en un cliente no detiene el proceso del resto
  ✅ El día de vencimiento se restringe entre 1 y 28 (evitar problemas con meses cortos)

  ══════════════════════════════════════════════════════════
  ORDEN DE IMPLEMENTACIÓN RECOMENDADO
  ══════════════════════════════════════════════════════════

  1. Modelo + Migración (Parte A)
  2. CRUD de ConfiguracionInteres (Parte B) — verificar que compila y funciona
  3. Nuevos métodos en AccountMovementRepository (Parte C.2)
  4. Nuevos métodos en CurrentAccountService (Parte C.3) — con inyección (Parte D)
  5. Nuevos endpoints en CurrentAccountController (Parte C.6)
  6. Compilar y verificar que el proyecto arranca sin errores

  ══════════════════════════════════════════════════════════
  NOTAS FINALES
  ══════════════════════════════════════════════════════════

  - Los pagos NO se bloquean si hay mora sin interés aplicado. Esto es intencional:
    el estándar contable es que el interés se acredita independientemente del pago.
    El operario debe aplicar el interés antes de que el cliente pague, pero si el
    cliente paga primero, el interés se aplica igual después.

  - El PDF de la ND de interés ya está soportado por GeneratePaymentReceiptAsync
    (tipo 3 = "NOTA DE DÉBITO", color rojo). No requiere cambios en el PDF.

  - La detección de "¿ya se aplicó el interés este mes?" se hace buscando cualquier
    movimiento de tipo ND (tipo 3) en el mes actual para ese cliente. Esto cubre
    tanto ND manuales (ajuste de precio) como ND de interés. Si se quiere distinguir
    específicamente las ND de interés, se puede filtrar por IdMotivoNd igual al
    motivo "Interés por mora". Implementar el filtro más estricto (por IdMotivoNd)
    si el motivo "Interés por mora" existe en la tabla motivo_nota_debito.

  - La auditoría se registra automáticamente si el interceptor de auditoría
    del proyecto está habilitado para la tabla movimiento_cc.
```
