using proyecto_venta_stock.Data;
using proyecto_venta_stock.Models;
using proyecto_venta_stock.Shared.ResultPattern;
using venta_stock_webapi.Client.Message;
using venta_stock_webapi.Client.Repository;
using venta_stock_webapi.CurrentAccount.DTO.MovementDTO;
using venta_stock_webapi.CurrentAccount.Message;
using venta_stock_webapi.CurrentAccount.PDF;
using venta_stock_webapi.CurrentAccount.Repository;
using venta_stock_webapi.CurrentAccount.Services.CurrentAccountService.StrategyCurrentAccount;
using proyecto_venta_stock.Features.Ferreteria.Repository;
using QuestPDF.Fluent;
using venta_stock_webapi.CurrentAccount.Services.InterestConfigService;
using venta_stock_webapi.Shared.Paged;
using venta_stock_webapi.Shared.Utils;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace venta_stock_webapi.CurrentAccount.Services.CurrentAccountService
{
    public class CurrentAccountService : ICurrentAccountService
    {
        private readonly IAccountMovementRepository _accountMovementRepository;
        private readonly IClientRepository _clientRepository;
        private readonly IFerreteriaRepository _ferreteriaRepository;
        private readonly VentaStockContext _context;
        private readonly AutoMapper.IMapper _mapper;
        private readonly ILogger<CurrentAccountService> _logger;
        private readonly MovementStrategyFactory _movementStrategyFactory;
        private readonly IDebitNoteReasonRepository _debitNoteReasonRepository;
        private readonly IInterestConfigRepository _interestConfigRepository;
        private readonly IAccountConfigRepository _accountConfigRepository;

        public CurrentAccountService(
            IAccountMovementRepository accountMovementRepository,
            AutoMapper.IMapper mapper,
            ILogger<CurrentAccountService> logger,
            IClientRepository clientRepository,
            MovementStrategyFactory movementStrategyFactory,
            IFerreteriaRepository ferreteriaRepository,
            VentaStockContext context,
            IDebitNoteReasonRepository debitNoteReasonRepository,
            IInterestConfigRepository interestConfigRepository,
            IAccountConfigRepository accountConfigRepository)
        {
            _accountMovementRepository = accountMovementRepository;
            _clientRepository = clientRepository;
            _ferreteriaRepository = ferreteriaRepository;
            _context = context;
            _mapper = mapper;
            _logger = logger;
            _movementStrategyFactory = movementStrategyFactory;
            _debitNoteReasonRepository = debitNoteReasonRepository;
            _interestConfigRepository = interestConfigRepository;
            _accountConfigRepository = accountConfigRepository;
        }

        public async Task<Result<List<AccountMovementDTO>>> GetAccountMovementsByClientId(int clientId)
        {
            try
            {
                var client = await _clientRepository.ExistsByIdAsync(clientId);

                if (!client)
                {
                    _logger.LogWarning("Client with ID {ClientId} not found", clientId);
                    return Result<List<AccountMovementDTO>>.Failure(CurrentAccountCode.account_not_found);
                }

                var movements = await _accountMovementRepository.GetMovements(clientId);
                var movementDTOs = _mapper.Map<List<AccountMovementDTO>>(movements);

                return Result<List<AccountMovementDTO>>.Success(movementDTOs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting account movements for client {ClientId}", clientId);
                return Result<List<AccountMovementDTO>>.Failure(CurrentAccountCode.unexpected_error);
            }
        }

        public async Task<Result<AccountSummaryDTO>> GetAccountSummaryAsync(int clientId)
        {
            try
            {
                var clientExists = await _clientRepository.ExistsByIdAsync(clientId);
                if (!clientExists)
                {
                    _logger.LogWarning("Client with ID {ClientId} not found", clientId);
                    return Result<AccountSummaryDTO>.Failure(CurrentAccountCode.account_not_found);
                }

                var (opening, latest) = await _accountMovementRepository.GetAccountSummaryAsync(clientId);

                if (opening == null)
                    return Result<AccountSummaryDTO>.Failure(CurrentAccountCode.account_not_found);

                var dto = new AccountSummaryDTO
                {
                    Opening = _mapper.Map<AccountMovementDTO>(opening),
                    Latest = latest != null ? _mapper.Map<AccountMovementDTO>(latest) : null
                };

                return Result<AccountSummaryDTO>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting account summary for client {ClientId}", clientId);
                return Result<AccountSummaryDTO>.Failure(CurrentAccountCode.unexpected_error);
            }
        }

        public async Task<Result<PagedList<AccountMovementDTO>>> GetAccountMovementsPagedAsync(
            int clientId,
            int pageIndex,
            int pageSize,
            string searchTerm,
            DateTime? fechaDesde,
            DateTime? fechaHasta,
            int? idTipoMovimiento)
        {
            try
            {
                var clientExists = await _clientRepository.ExistsByIdAsync(clientId);
                if (!clientExists)
                {
                    _logger.LogWarning("Client with ID {ClientId} not found", clientId);
                    return Result<PagedList<AccountMovementDTO>>.Failure(CurrentAccountCode.account_not_found);
                }

                var pagedMovements = await _accountMovementRepository.GetMovementsPagedAsync(
                    clientId, pageIndex, pageSize, searchTerm, fechaDesde, fechaHasta, idTipoMovimiento);

                var mappedItems = _mapper.Map<List<AccountMovementDTO>>(pagedMovements.Items);
                var pagedDtos = new PagedList<AccountMovementDTO>(mappedItems, pagedMovements.TotalCount, pageIndex, pageSize);

                return Result<PagedList<AccountMovementDTO>>.Success(pagedDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting paged account movements for client {ClientId}", clientId);
                return Result<PagedList<AccountMovementDTO>>.Failure(CurrentAccountCode.unexpected_error);
            }
        }

        public async Task<Result<string>> CreateAccountMovement(CreateCurrentAccountDTO accountMovementDTO)
        {
            try
            {
                var clientExists = await _clientRepository.ExistsByIdAsync(accountMovementDTO.IdCliente);

                if (!clientExists)
                {
                    _logger.LogWarning("Client with ID {ClientId} not found", accountMovementDTO.IdCliente);
                    return Result<string>.Failure(CurrentAccountCode.account_not_found);
                }

                var accountMovement = _mapper.Map<MovimientoCc>(accountMovementDTO);
                accountMovement.Detalle = await _accountMovementRepository.GetDetailMovement((int)accountMovement.IdTipoMovimiento);

                await _accountMovementRepository.CreateMovement(accountMovement);

                return Result<string>.Success("Creacion de cuenta exitosa");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding account movement for client {ClientId}", accountMovementDTO.IdCliente);
                return Result<string>.Failure(CurrentAccountCode.unexpected_error);
            }
        }

        public async Task<Result<int>> RegisterMovement(AddMovementDTO addMovementDTO)
        {
            try
            {
                var lastMovement = await _accountMovementRepository.GetLastMovement(addMovementDTO.IdCliente);

                if (lastMovement is null)
                    return Result<int>.Failure(CurrentAccountCode.account_not_found);

                decimal balanceBase = lastMovement.SaldoActual ?? 0;
                decimal limitBase = lastMovement.LimiteCuenta ?? 0;

                var typeMovement = (TypeMovement)addMovementDTO.IdTipoMovimiento;

                // PAGO_GLOBAL solo se permite cuando el importe cubre la deuda total
                if (typeMovement == TypeMovement.PAGO_GLOBAL && addMovementDTO.Importe < balanceBase)
                    return Result<int>.Failure(CurrentAccountCode.global_payment_must_cover_full_debt);

                IMovementStrategy movementStrategy = _movementStrategyFactory.GetStrategy(typeMovement);
                CalculationResult calculationResult = movementStrategy.Calculate(balanceBase, limitBase, addMovementDTO.Importe);

                var newMovement = new MovimientoCc
                {
                    IdCliente = addMovementDTO.IdCliente,
                    Importe = addMovementDTO.Importe,
                    Detalle = addMovementDTO.Detalle,
                    IdEstado = 2,
                    IdTipoMovimiento = addMovementDTO.IdTipoMovimiento,
                    IdUsuarioRegistra = addMovementDTO.IdUsuarioRegistra,
                    IdVenta = (addMovementDTO.IdVenta == 0) ? null : addMovementDTO.IdVenta,
                    SaldoActual = calculationResult.NewBalance,
                    LimiteCuenta = calculationResult.NewLimit,
                    Fecha = DateTime.Now
                };

                await _accountMovementRepository.CreateMovement(newMovement);

                if (typeMovement == TypeMovement.PAGO_FACTURA && addMovementDTO.IdVenta.HasValue)
                {
                    await AllocarPagoFactura(addMovementDTO.IdVenta.Value, addMovementDTO.Importe);
                }
                else if (typeMovement == TypeMovement.PAGO_GLOBAL || typeMovement == TypeMovement.PAGO_PARCIAL)
                {
                    await AllocarPagoGlobal(addMovementDTO.IdCliente, addMovementDTO.Importe);
                }

                return Result<int>.Success(newMovement.IdMovimiento);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering movement for client {ClientId}", addMovementDTO.IdCliente);
                return Result<int>.Failure(CurrentAccountCode.unexpected_error);
            }
        }

        public async Task<Result<List<TypeMovementDTO>>> GetMovementTypes()
        {
            try
            {
                var types = await _accountMovementRepository.GetMovementType();
                var typesDTO = _mapper.Map<List<TypeMovementDTO>>(types);
                return Result<List<TypeMovementDTO>>.Success(typesDTO);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting movement types");
                return Result<List<TypeMovementDTO>>.Failure(CurrentAccountCode.unexpected_error);
            }
        }

        public async Task<Result<List<PendingSalePaymentDTO>>> GetPendingSalesForPayment(int clientId)
        {
            try
            {
                var clientExists = await _clientRepository.ExistsByIdAsync(clientId);

                if (!clientExists)
                {
                    _logger.LogWarning("Client with ID {ClientId} not found", clientId);
                    return Result<List<PendingSalePaymentDTO>>.Failure(CurrentAccountCode.account_not_found);
                }

                var lastMovement = await _accountMovementRepository.GetLastMovement(clientId);
                if (lastMovement is null || lastMovement.SaldoActual <= 0)
                    return Result<List<PendingSalePaymentDTO>>.Success(new List<PendingSalePaymentDTO>());

                var pendingSales = await _accountMovementRepository.GetPendingSalesForPayment(clientId);
                return Result<List<PendingSalePaymentDTO>>.Success(pendingSales);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending sales for payment for client {ClientId}", clientId);
                return Result<List<PendingSalePaymentDTO>>.Failure(CurrentAccountCode.unexpected_error);
            }
        }

        public async Task<Result<byte[]>> GeneratePaymentReceiptAsync(int idMovimiento)
        {
            try
            {
                var movement = await _accountMovementRepository.GetMovementById(idMovimiento);

                if (movement is null)
                    return Result<byte[]>.Failure(CurrentAccountCode.account_not_found);

                var ferreteriaInfo = await _ferreteriaRepository.GetAsync();

                var cliente = movement.IdClienteNavigation;
                var nombreCliente = !string.IsNullOrEmpty(cliente?.Nombre)
                    ? $"{cliente.Nombre} {cliente.Apellido}"
                    : (cliente?.RazonSocial ?? "N/A");

                var (tipoComprobante, colorHeader) = movement.IdTipoMovimiento switch
                {
                    6 or 8 or 11 => ("COMPROBANTE DE PAGO", "green"),
                    3            => ("NOTA DE DÉBITO",       "red"),
                    4            => ("NOTA DE CRÉDITO",      "blue"),
                    9            => ("ANULACIÓN DE PAGO",    "red"),
                    _            => ("COMPROBANTE DE MOVIMIENTO", "green")
                };

                var importe = movement.Importe ?? 0;
                var dataSource = new PaymentReceiptDataSource
                {
                    DatosFerreteria = ferreteriaInfo,
                    Fecha = movement.Fecha ?? DateTime.Now,
                    ClienteNombre = nombreCliente,
                    ClienteDni = !string.IsNullOrWhiteSpace(cliente?.Dni)
                        ? cliente.Dni
                        : (cliente?.Cuit ?? "N/A"),
                    ClienteTelefono = cliente?.Telefono ?? "-",
                    TipoComprobante = tipoComprobante,
                    ColorHeader = colorHeader,
                    Detalle = movement.Detalle ?? "-",
                    Importe = importe,
                    ImporteEnLetras = NumberToTextConverter.ConvertirATexto(importe),
                    SaldoResultante = movement.SaldoActual ?? 0,
                    UsuarioRegistra = movement.IdUsuarioRegistraNavigation is not null
                        ? $"{movement.IdUsuarioRegistraNavigation.Nombre} {movement.IdUsuarioRegistraNavigation.Apellido}"
                        : "N/A",
                    CodigoVenta = movement.IdVentaNavigation?.CodigoVenta,
                    TotalVenta = movement.IdVentaNavigation?.Total,
                    MotivoNd = movement.IdMotivoNdNavigation?.Nombre,
                    MotivoNc = movement.IdMotivoNcNavigation?.Nombre
                };

                QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
                var document = new PaymentReceiptDocument(dataSource);
                var pdfBytes = document.GeneratePdf();

                return Result<byte[]>.Success(pdfBytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating payment receipt for movement {IdMovimiento}", idMovimiento);
                return Result<byte[]>.Failure(CurrentAccountCode.unexpected_error);
            }
        }

        public async Task<Result<int>> AnnulPayment(AnnulPaymentDTO dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Obtener el pago a anular con tracking
                var pagoAAnular = await _accountMovementRepository.GetPaymentMovementById(dto.IdMovimientoPago);

                if (pagoAAnular is null)
                    return Result<int>.Failure(CurrentAccountCode.movement_not_found);

                // 2. Validar que no esté ya anulado
                if (pagoAAnular.EsAnulado)
                    return Result<int>.Failure(CurrentAccountCode.payment_already_annulled);

                int clientId = pagoAAnular.IdCliente ?? 0;
                var lastMovement = await _accountMovementRepository.GetLastMovement(clientId);

                if (lastMovement is null)
                    return Result<int>.Failure(CurrentAccountCode.account_not_found);

                decimal balanceBase = lastMovement.SaldoActual ?? 0;
                decimal limitBase = lastMovement.LimiteCuenta ?? 0;
                decimal importePago = pagoAAnular.Importe ?? 0;

                // 3. Calcular nuevo saldo/límite (revertir el pago = sumar deuda de vuelta)
                var strategy = _movementStrategyFactory.GetStrategy(TypeMovement.ANULACION_PAGO);
                var calculationResult = strategy.Calculate(balanceBase, limitBase, importePago);

                // 4. Crear el contra-movimiento de anulación
                var contraMovimiento = new MovimientoCc
                {
                    IdCliente = clientId,
                    Importe = importePago,
                    Detalle = $"Anulación de pago #{dto.IdMovimientoPago}: {dto.Motivo}",
                    IdEstado = 2,
                    IdTipoMovimiento = (int)TypeMovement.ANULACION_PAGO,
                    IdUsuarioRegistra = dto.IdUsuarioRegistra,
                    IdVenta = pagoAAnular.IdVenta,
                    SaldoActual = calculationResult.NewBalance,
                    LimiteCuenta = calculationResult.NewLimit,
                    Fecha = DateTime.Now
                };

                await _accountMovementRepository.CreateMovement(contraMovimiento);

                // 5. Marcar el pago original como anulado
                pagoAAnular.EsAnulado = true;
                await _accountMovementRepository.UpdateMovimientos(new List<MovimientoCc> { pagoAAnular });

                // 6. Recomputar MontoPagado de todos los consumos del cliente
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

        // ─── Helpers privados ───────────────────────────────────────────────────────

        private async Task RecomputeMontoPagado(int clientId)
        {
            // 1. Todos los consumos con tracking, reseteados a 0
            var consumos = await _accountMovementRepository.GetAllConsumptionsTracked(clientId);
            foreach (var consumo in consumos)
                consumo.MontoPagado = 0;

            // 2. Todos los pagos válidos (no anulados) en orden cronológico
            var pagos = await _accountMovementRepository.GetAllValidPayments(clientId);

            // 3. Re-aplicar asignación para cada pago en orden cronológico
            foreach (var pago in pagos)
            {
                if (pago.IdTipoMovimiento == (int)TypeMovement.PAGO_FACTURA && pago.IdVenta.HasValue)
                    AllocarPagoFacturaEnMemoria(pago.IdVenta.Value, pago.Importe ?? 0, consumos);
                else if (pago.IdTipoMovimiento == (int)TypeMovement.PAGO_GLOBAL
                      || pago.IdTipoMovimiento == (int)TypeMovement.PAGO_PARCIAL)
                    AllocarPagoGlobalEnMemoria(pago.Importe ?? 0, consumos);
            }

            // 4. Guardar en batch
            await _accountMovementRepository.UpdateMovimientos(consumos);
        }

        private void AllocarPagoGlobalEnMemoria(decimal importePago, List<MovimientoCc> consumos)
        {
            decimal restante = importePago;
            foreach (var consumo in consumos.OrderBy(c => c.Fecha).ThenBy(c => c.IdMovimiento))
            {
                if (restante <= 0) break;
                decimal pendiente = (consumo.Importe ?? 0) - (consumo.MontoPagado ?? 0);
                if (pendiente <= 0) continue;

                if (restante >= pendiente)
                {
                    consumo.MontoPagado = consumo.Importe;
                    restante -= pendiente;
                }
                else
                {
                    consumo.MontoPagado = (consumo.MontoPagado ?? 0) + restante;
                    restante = 0;
                }
            }
        }

        private void AllocarPagoFacturaEnMemoria(int idVenta, decimal importePago, List<MovimientoCc> consumos)
        {
            var consumo = consumos.FirstOrDefault(c => c.IdVenta == idVenta);
            if (consumo is null) return;
            consumo.MontoPagado = Math.Min((consumo.MontoPagado ?? 0) + importePago, consumo.Importe ?? 0);
        }

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
                }

                // 4. Calcular nuevo saldo/límite usando DebitNoteStrategy
                decimal balanceBase = lastMovement.SaldoActual ?? 0;
                decimal limitBase = lastMovement.LimiteCuenta ?? 0;

                var strategy = _movementStrategyFactory.GetStrategy(TypeMovement.NOTA_DEBITO);
                var calculationResult = strategy.Calculate(balanceBase, limitBase, dto.Importe);

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
                    SaldoActual = calculationResult.NewBalance,
                    LimiteCuenta = calculationResult.NewLimit,
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

        public async Task<Result<List<OverdueClientDTO>>> GetOverdueClients()
        {
            try
            {
                var config = await _interestConfigRepository.GetCurrentAsync();
                if (config is null)
                    return Result<List<OverdueClientDTO>>.Failure(CurrentAccountCode.no_active_config);

                var now = DateTime.Now;

                // Antes del vencimiento → no hay morosos
                int daysInMonth = DateTime.DaysInMonth(now.Year, now.Month);
                int diaVenc = Math.Min(config.DiaVencimiento, daysInMonth);
                if (now.Day <= diaVenc)
                    return Result<List<OverdueClientDTO>>.Success(new List<OverdueClientDTO>());

                // Fecha de vencimiento del mes actual (para comparar con fecha de apertura por cliente)
                var dueDateThisMonth = new DateTime(now.Year, now.Month, diaVenc);

                var clientsWithDebt = await _accountMovementRepository.GetClientsWithDebt();
                var overdueClients = new List<OverdueClientDTO>();

                foreach (var (idCliente, saldoDeudor) in clientsWithDebt)
                {
                    // Si la CC abrió en o después del vencimiento de este mes, el primer vencimiento
                    // es el del mes siguiente → no está en mora todavía
                    var openingDate = await _accountMovementRepository.GetAccountOpeningDateAsync(idCliente);
                    if (openingDate.HasValue && openingDate.Value.Date >= dueDateThisMonth.Date)
                        continue;

                    if (await _accountMovementRepository.HasInterestAppliedThisMonth(idCliente))
                        continue;

                    var cliente = await _clientRepository.GetByIdAsync(idCliente);
                    if (cliente is null) continue;

                    var nombreCliente = !string.IsNullOrEmpty(cliente.Nombre)
                        ? $"{cliente.Nombre} {cliente.Apellido}"
                        : (cliente.RazonSocial ?? "N/A");

                    overdueClients.Add(new OverdueClientDTO
                    {
                        IdCliente = idCliente,
                        NombreCliente = nombreCliente,
                        Dni = cliente.Dni,
                        Cuit = cliente.Cuit,
                        SaldoDeudor = saldoDeudor,
                        ImporteInteres = Math.Round(saldoDeudor * (config.PorcentajeInteres / 100), 2),
                        PorcentajeInteres = config.PorcentajeInteres,
                        DiaVencimiento = config.DiaVencimiento
                    });
                }

                return Result<List<OverdueClientDTO>>.Success(overdueClients);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting overdue clients");
                return Result<List<OverdueClientDTO>>.Failure(CurrentAccountCode.unexpected_error);
            }
        }

        public async Task<Result<string>> ApplyInterestToClient(int clientId, int idUsuarioRegistra)
        {
            try
            {
                var config = await _interestConfigRepository.GetCurrentAsync();
                if (config is null)
                    return Result<string>.Failure(CurrentAccountCode.no_active_config);

                var lastMovement = await _accountMovementRepository.GetLastMovement(clientId);
                if (lastMovement is null || (lastMovement.SaldoActual ?? 0) <= 0)
                    return Result<string>.Failure(CurrentAccountCode.client_has_no_debt);

                if (await _accountMovementRepository.HasInterestAppliedThisMonth(clientId))
                    return Result<string>.Failure(CurrentAccountCode.interest_already_applied_this_month);

                decimal saldoDeudor = lastMovement.SaldoActual ?? 0;
                decimal importe = Math.Round(saldoDeudor * (config.PorcentajeInteres / 100), 2);

                if (importe <= 0)
                    return Result<string>.Success("Sin interés a aplicar (importe calculado es 0).");

                var strategy = _movementStrategyFactory.GetStrategy(TypeMovement.NOTA_DEBITO);
                var calculationResult = strategy.Calculate(saldoDeudor, lastMovement.LimiteCuenta ?? 0, importe);

                // Buscar el motivo "Interés por mora" si existe
                var motivos = await _debitNoteReasonRepository.GetAllAsync(activo: true);
                var motivoInteres = motivos.FirstOrDefault(m => m.Nombre == "Interés por mora");

                var detalle = $"Interés por mora — {config.PorcentajeInteres}% — {config.Nombre}";

                var newMovement = new MovimientoCc
                {
                    IdCliente = clientId,
                    Importe = importe,
                    Detalle = detalle,
                    IdEstado = 2,
                    IdTipoMovimiento = (int)TypeMovement.NOTA_DEBITO,
                    IdUsuarioRegistra = idUsuarioRegistra,
                    SaldoActual = calculationResult.NewBalance,
                    LimiteCuenta = calculationResult.NewLimit,
                    Fecha = DateTime.Now,
                    IdMotivoNd = motivoInteres?.IdMotivo
                };

                await _accountMovementRepository.CreateMovement(newMovement);

                return Result<string>.Success($"Interés aplicado: ${importe:F2}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying interest to client {ClientId}", clientId);
                return Result<string>.Failure(CurrentAccountCode.unexpected_error);
            }
        }

        public async Task<Result<string>> ApplyInterestToAll(int idUsuarioRegistra)
        {
            try
            {
                var overdueResult = await GetOverdueClients();
                if (!overdueResult.IsSuccess)
                    return Result<string>.Failure((CurrentAccountCode)overdueResult.ErrorCode!);

                int exitosos = 0;
                int fallidos = 0;

                foreach (var cliente in overdueResult.Value!)
                {
                    var result = await ApplyInterestToClient(cliente.IdCliente, idUsuarioRegistra);
                    if (result.IsSuccess) exitosos++;
                    else fallidos++;
                }

                return Result<string>.Success(
                    $"Interés aplicado a {exitosos} clientes. {fallidos} clientes con error.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying interest to all clients");
                return Result<string>.Failure(CurrentAccountCode.unexpected_error);
            }
        }

        public async Task RecomputeMontoPagadoPublic(int clientId)
            => await RecomputeMontoPagado(clientId);

        // ═══════════════════════════════════════════════════════════════════════
        // EXPORTACIONES PDF / EXCEL — Estado de cuenta de cliente
        // ═══════════════════════════════════════════════════════════════════════

        public async Task<Result<byte[]>> ExportClientAccountPdfAsync(
            int idCliente,
            DateOnly? fechaDesde,
            DateOnly? fechaHasta,
            int? idTipoMovimientoCc)
        {
            try
            {
                var (movimientos, cliente, ferreteria, lastMovement) =
                    await LoadExportDataAsync(idCliente, fechaDesde, fechaHasta, idTipoMovimientoCc);

                QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

                var items = MapToStatementItems(movimientos);

                var ds = new AccountStatementDataSource
                {
                    DatosFerreteria  = ferreteria,
                    ClienteNombre    = GetClientName(cliente),
                    ClienteDni       = !string.IsNullOrWhiteSpace(cliente?.Dni) ? cliente.Dni : (cliente?.Cuit ?? "-"),
                    FechaGeneracion  = DateTime.Now,
                    FiltroFechaDesde = fechaDesde?.ToString("dd/MM/yyyy"),
                    FiltroFechaHasta = fechaHasta?.ToString("dd/MM/yyyy"),
                    SaldoActual      = lastMovement?.SaldoActual ?? 0,
                    LimiteCredito    = (lastMovement?.SaldoActual ?? 0) + (lastMovement?.LimiteCuenta ?? 0),
                    Movimientos      = items
                };

                var bytes = new AccountStatementDocument(ds).GeneratePdf();
                return Result<byte[]>.Success(bytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exportando estado de cuenta de cliente {Id} a PDF", idCliente);
                return Result<byte[]>.Failure(CurrentAccountCode.unexpected_error);
            }
        }

        public async Task<Result<byte[]>> ExportClientAccountExcelAsync(
            int idCliente,
            DateOnly? fechaDesde,
            DateOnly? fechaHasta,
            int? idTipoMovimientoCc)
        {
            try
            {
                var (movimientos, cliente, ferreteria, lastMovement) =
                    await LoadExportDataAsync(idCliente, fechaDesde, fechaHasta, idTipoMovimientoCc);

                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using var package = new ExcelPackage();
                var ws = package.Workbook.Worksheets.Add("Estado de Cuenta");

                // Header info
                ws.Cells["A1"].Value = ferreteria.Nombre;
                ws.Cells["A1"].Style.Font.Bold = true;
                ws.Cells["A1"].Style.Font.Size = 13;
                ws.Cells["A2"].Value = "ESTADO DE CUENTA CORRIENTE";
                ws.Cells["A2"].Style.Font.Bold = true;
                ws.Cells["A3"].Value = $"Cliente: {GetClientName(cliente)}";
                ws.Cells["A4"].Value = $"DNI/CUIT: {(!string.IsNullOrWhiteSpace(cliente?.Dni) ? cliente.Dni : (cliente?.Cuit ?? "-"))}";
                ws.Cells["A5"].Value = $"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}";
                if (fechaDesde.HasValue || fechaHasta.HasValue)
                    ws.Cells["A6"].Value = $"Período: {fechaDesde?.ToString("dd/MM/yyyy") ?? "–"} al {fechaHasta?.ToString("dd/MM/yyyy") ?? "–"}";
                ws.Cells["A7"].Value = $"Saldo deudor: ${lastMovement?.SaldoActual ?? 0:N2}";

                // Column headers
                int headerRow = 9;
                var headers = new[] { "#", "Fecha", "Tipo", "Detalle / Comprobante", "Debe", "Haber", "Saldo" };
                for (int c = 0; c < headers.Length; c++)
                {
                    ws.Cells[headerRow, c + 1].Value = headers[c];
                    ws.Cells[headerRow, c + 1].Style.Font.Bold = true;
                    ws.Cells[headerRow, c + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    ws.Cells[headerRow, c + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(30, 86, 160));
                    ws.Cells[headerRow, c + 1].Style.Font.Color.SetColor(System.Drawing.Color.White);
                }

                // Data rows
                var items = MapToStatementItems(movimientos);
                int dataRow = headerRow + 1;
                foreach (var item in items)
                {
                    ws.Cells[dataRow, 1].Value = item.Numero;
                    ws.Cells[dataRow, 2].Value = item.Fecha;
                    ws.Cells[dataRow, 3].Value = item.TipoMovimiento;
                    ws.Cells[dataRow, 4].Value = item.Detalle;

                    if (item.Debe.HasValue)
                    {
                        ws.Cells[dataRow, 5].Value = item.Debe.Value;
                        ws.Cells[dataRow, 5].Style.Numberformat.Format = "#,##0.00";
                        ws.Cells[dataRow, 5].Style.Font.Color.SetColor(System.Drawing.Color.FromArgb(180, 30, 30));
                    }

                    if (item.Haber.HasValue)
                    {
                        ws.Cells[dataRow, 6].Value = item.Haber.Value;
                        ws.Cells[dataRow, 6].Style.Numberformat.Format = "#,##0.00";
                        ws.Cells[dataRow, 6].Style.Font.Color.SetColor(System.Drawing.Color.FromArgb(30, 130, 30));
                    }

                    if (item.Saldo.HasValue)
                    {
                        ws.Cells[dataRow, 7].Value = item.Saldo.Value;
                        ws.Cells[dataRow, 7].Style.Numberformat.Format = "#,##0.00";
                        ws.Cells[dataRow, 7].Style.Font.Bold = true;
                    }

                    if (item.Numero % 2 == 0)
                    {
                        ws.Cells[dataRow, 1, dataRow, 7].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        ws.Cells[dataRow, 1, dataRow, 7].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(240, 244, 250));
                    }

                    dataRow++;
                }

                // Saldo final
                ws.Cells[dataRow, 6].Value = "SALDO FINAL:";
                ws.Cells[dataRow, 6].Style.Font.Bold = true;
                ws.Cells[dataRow, 7].Value = lastMovement?.SaldoActual ?? 0;
                ws.Cells[dataRow, 7].Style.Font.Bold = true;
                ws.Cells[dataRow, 7].Style.Numberformat.Format = "#,##0.00";

                ws.Cells[ws.Dimension.Address].AutoFitColumns();
                return Result<byte[]>.Success(package.GetAsByteArray());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exportando estado de cuenta de cliente {Id} a Excel", idCliente);
                return Result<byte[]>.Failure(CurrentAccountCode.unexpected_error);
            }
        }

        // ─── Helpers privados export ─────────────────────────────────────────

        private async Task<(List<MovimientoCc> movimientos, Cliente? cliente, Ferreteria ferreteria, MovimientoCc? lastMovement)>
            LoadExportDataAsync(int idCliente, DateOnly? fechaDesde, DateOnly? fechaHasta, int? idTipo)
        {
            DateTime? desde = fechaDesde?.ToDateTime(TimeOnly.MinValue);
            DateTime? hasta = fechaHasta?.ToDateTime(TimeOnly.MaxValue);

            var movimientos = await _accountMovementRepository.GetMovementsForExportAsync(idCliente, desde, hasta, idTipo);
            var cliente = await _clientRepository.GetByIdAsync(idCliente);
            var ferreteria = await _ferreteriaRepository.GetAsync();
            var lastMovement = await _accountMovementRepository.GetLastMovement(idCliente);

            return (movimientos, cliente, ferreteria, lastMovement);
        }

        private static List<AccountStatementItemPdf> MapToStatementItems(List<MovimientoCc> movimientos)
        {
            // Tipos que van en DEBE (incrementan la deuda)
            var tiposDebe = new HashSet<int> { 3, 5, 7, 9 };
            // Tipos que van en HABER (reducen la deuda)
            var tiposHaber = new HashSet<int> { 4, 6, 8, 11 };

            return movimientos.Select((m, i) =>
            {
                decimal? debe = tiposDebe.Contains(m.IdTipoMovimiento ?? 0) ? m.Importe : null;
                decimal? haber = tiposHaber.Contains(m.IdTipoMovimiento ?? 0) ? m.Importe : null;

                string detalle = m.Detalle ?? "";
                if (m.IdVentaNavigation != null)
                    detalle = $"{m.IdVentaNavigation.CodigoVenta} — {detalle}";

                return new AccountStatementItemPdf
                {
                    Numero         = i + 1,
                    Fecha          = m.Fecha?.ToString("dd/MM/yyyy HH:mm") ?? "-",
                    TipoMovimiento = m.IdTipoMovimientoNavigation?.Nombre ?? "-",
                    Detalle        = detalle,
                    Debe           = debe,
                    Haber          = haber,
                    Saldo          = m.SaldoActual
                };
            }).ToList();
        }

        private static string GetClientName(Cliente? cliente)
        {
            if (cliente is null) return "N/A";
            return !string.IsNullOrEmpty(cliente.RazonSocial)
                ? cliente.RazonSocial
                : $"{cliente.Nombre} {cliente.Apellido}".Trim();
        }

        public async Task<Result<int>> UpdateAccountLimitAsync(UpdateAccountLimitDTO dto)
        {
            try
            {
                var clientExists = await _clientRepository.ExistsByIdAsync(dto.IdCliente);
                if (!clientExists)
                {
                    _logger.LogWarning("Client with ID {ClientId} not found", dto.IdCliente);
                    return Result<int>.Failure(CurrentAccountCode.account_not_found);
                }

                var configuracion = await _accountConfigRepository.GetAccountConfigByIdAsync(dto.IdConfiguracion);
                if (configuracion == null || !configuracion.Activo)
                    return Result<int>.Failure(CurrentAccountCode.configuracion_cc_not_found);

                var nuevoLimite = configuracion.MontoLimite;

                var ultimoMovimiento = await _accountMovementRepository.GetLastMovement(dto.IdCliente);
                if (ultimoMovimiento == null)
                    return Result<int>.Failure(CurrentAccountCode.account_not_found);

                var limiteGlobalActual = (ultimoMovimiento.SaldoActual ?? 0) + (ultimoMovimiento.LimiteCuenta ?? 0);
                if (nuevoLimite == limiteGlobalActual)
                    return Result<int>.Failure(CurrentAccountCode.limit_already_set);

                var strategy = _movementStrategyFactory.GetStrategy(TypeMovement.MODIFICACION_LIMITE);
                var calcResult = strategy.Calculate(
                    ultimoMovimiento.SaldoActual ?? 0,
                    ultimoMovimiento.LimiteCuenta ?? 0,
                    nuevoLimite);

                var mov = new MovimientoCc
                {
                    IdCliente = dto.IdCliente,
                    IdUsuarioRegistra = dto.IdUsuarioRegistra,
                    IdTipoMovimiento = (int)TypeMovement.MODIFICACION_LIMITE,
                    Fecha = DateTime.Now,
                    Importe = 0,
                    Detalle = dto.Motivo,
                    IdEstado = 2,
                    SaldoActual = calcResult.NewBalance,
                    LimiteCuenta = calcResult.NewLimit
                };

                await _accountMovementRepository.CreateMovement(mov);

                return Result<int>.Success(mov.IdMovimiento);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating account limit for client {ClientId}", dto.IdCliente);
                return Result<int>.Failure(CurrentAccountCode.unexpected_error);
            }
        }
    }
}
