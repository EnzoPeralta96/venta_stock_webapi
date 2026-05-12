using AutoMapper;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using proyecto_venta_stock.Data;
using proyecto_venta_stock.Models;
using proyecto_venta_stock.Shared.ResultPattern;
using venta_stock_webapi.Client.Repository;
using venta_stock_webapi.Sale.DTO;
using venta_stock_webapi.Sale.Message;
using venta_stock_webapi.Sale.Repository;
using venta_stock_webapi.Sale.Strategies;
using venta_stock_webapi.Shared.Extensions;
using venta_stock_webapi.Shared.Paged;
using proyecto_venta_stock.Product.ProductRepository;
using venta_stock_webapi.CurrentAccount.Services.CurrentAccountService;
using venta_stock_webapi.CurrentAccount.DTO.MovementDTO;
using venta_stock_webapi.CurrentAccount.Services.CurrentAccountService.StrategyCurrentAccount;
using venta_stock_webapi.CurrentAccount.Repository;
using venta_stock_webapi.Features.Audit.Repository;
using venta_stock_webapi.Features.StockMovement.Services;
using proyecto_venta_stock.Features.Ferreteria.Repository;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using QuestPDF.Fluent;
using venta_stock_webapi.Sale.PDF;
using venta_stock_webapi.Shared.Identity;

namespace venta_stock_webapi.Sale.Services
{

    public class SaleService : ISaleServices
    {
        private readonly ISaleRepository _saleRepository;
        private readonly IClientRepository _clientRepository;
        private readonly IProductRepository _productRepository;
        private readonly VentaStockContext _context;
        private readonly IMapper _mapper;
        private readonly ISaleStrategyFactory _strategyFactory;
        private readonly ILogger<SaleService> _logger;
        private readonly ICurrentAccountService _currentAccountService;
        private readonly IAccountMovementRepository _accountMovementRepository;
        private readonly ICreditNoteReasonRepository _creditNoteReasonRepository;
        private readonly MovementStrategyFactory _movementStrategyFactory;
        private readonly IStockMovementService _stockMovementService;
        private readonly IFerreteriaRepository _ferreteriaRepository;
        private readonly IAuditRepository _auditRepository;
        private readonly IUserContext _userContext;

        public SaleService(
            ISaleRepository saleRepository,
            IClientRepository clientRepository,
            IProductRepository productRepository,
            VentaStockContext context,
            IMapper mapper,
            ISaleStrategyFactory strategyFactory,
            ILogger<SaleService> logger,
            ICurrentAccountService currentAccountService,
            IAccountMovementRepository accountMovementRepository,
            ICreditNoteReasonRepository creditNoteReasonRepository,
            MovementStrategyFactory movementStrategyFactory,
            IStockMovementService stockMovementService,
            IFerreteriaRepository ferreteriaRepository,
            IAuditRepository auditRepository,
            IUserContext userContext)
        {
            _saleRepository = saleRepository;
            _clientRepository = clientRepository;
            _productRepository = productRepository;
            _mapper = mapper;
            _context = context;
            _strategyFactory = strategyFactory;
            _logger = logger;
            _currentAccountService = currentAccountService;
            _accountMovementRepository = accountMovementRepository;
            _creditNoteReasonRepository = creditNoteReasonRepository;
            _movementStrategyFactory = movementStrategyFactory;
            _stockMovementService = stockMovementService;
            _ferreteriaRepository = ferreteriaRepository;
            _auditRepository = auditRepository;
            _userContext = userContext;
        }

        private async Task LogAsync(string accion, string entidadTipo, string detalle,
            object? anterior = null, object? nuevo = null)
        {
            try
            {
                await _auditRepository.LogAsync(new Auditoria
                {
                    FechaHora         = DateTimeOffset.UtcNow,
                    IdUsuario         = _userContext.UserId,
                    UsuarioNombre     = _userContext.UserName,
                    Accion            = accion,
                    EntidadTipo       = entidadTipo,
                    Detalle           = detalle,
                    ValoresAnteriores = anterior != null ? JsonSerializer.Serialize(anterior) : null,
                    ValoresNuevos     = nuevo    != null ? JsonSerializer.Serialize(nuevo)    : null,
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo registrar auditoría.");
            }
        }

        private async Task<Result<List<(Producto producto, decimal cantidad)>>> ValidateAndLoadProductsAsync(List<SaleItemDTO> items)
        {
            var result = new List<(Producto producto, decimal cantidad)>();
            foreach (var item in items)
            {
                var producto = await _productRepository.GetById(item.IdProducto);
                if (producto == null)
                    return Result<List<(Producto producto, decimal cantidad)>>.Failure(SaleErrorCode.product_not_found);
                if (!producto.Activo)
                    return Result<List<(Producto producto, decimal cantidad)>>.Failure(SaleErrorCode.product_inactive);
                if (!producto.VentaSinStock.GetValueOrDefault() && producto.Stock < item.Cantidad)
                    return Result<List<(Producto producto, decimal cantidad)>>.Failure(SaleErrorCode.insufficient_stock);
                result.Add((producto, item.Cantidad));
            }
            return Result<List<(Producto producto, decimal cantidad)>>.Success(result);
        }

        private static Ventum BuildVentaEntity(CreateSaleDTO dto, string codigoVenta, decimal total, int idUsuario)
        {
            return new Ventum
            {
                CodigoVenta = codigoVenta,
                Fecha       = DateTime.Now,
                Total       = total,
                IdMedioPago = dto.idMedioPago,
                IdCliente   = dto.idCliente,
                IdUsuario   = idUsuario,
                IdEstado    = 2
            };
        }

        private static List<DetalleVentum> BuildDetalles(List<(Producto producto, decimal cantidad)> productosValidados)
        {
            return productosValidados.Select(pv => new DetalleVentum
            {
                IdProducto  = pv.producto.IdProducto,
                Cantidad    = pv.cantidad,
                PrecioVenta = pv.producto.Precio ?? 0,
                SubTotal    = (pv.producto.Precio ?? 0) * pv.cantidad
            }).ToList();
        }

        private async Task<bool> RegisterCCMovementAsync(CreateSaleDTO dto, Ventum ventaCreada, decimal total, int idUsuario)
        {
            var movementResult = await _currentAccountService.RegisterMovement(new AddMovementDTO
            {
                IdCliente         = dto.idCliente,
                IdTipoMovimiento  = (int)TypeMovement.MOVIMIENTO_CC,
                Importe           = total,
                Detalle           = $"Venta {ventaCreada.CodigoVenta}",
                IdVenta           = ventaCreada.IdVenta,
                IdUsuarioRegistra = idUsuario
            });

            if (!movementResult.IsSuccess)
            {
                _logger.LogError(
                    "No se pudo registrar el movimiento de cuenta corriente para la venta {CodigoVenta} (cliente {ClienteId})",
                    ventaCreada.CodigoVenta, dto.idCliente);
                return false;
            }

            _logger.LogInformation(
                "Movimiento CC registrado - Venta: {CodigoVenta}, Cliente: {ClienteId}",
                ventaCreada.CodigoVenta, dto.idCliente);
            return true;
        }

        private async Task UpdateStockAfterSaleAsync(List<DetalleVentum> detalles, string codigoVenta, int idUsuario)
        {
            foreach (var detalle in detalles)
                await _stockMovementService.RegistrarMovimientoAsync(
                    detalle.IdProducto,
                    TipoMovimientoStockEnum.EgresoVenta,
                    -(detalle.Cantidad ?? 0),
                    $"VENTA:{codigoVenta}",
                    idUsuario);
        }

        private async Task LogCreateSaleAsync(string codigoVenta, string clienteNombre, decimal total, string medioPago)
        {
            await LogAsync("VENTA_REGISTRADA", "VENTA",
                $"Venta: {codigoVenta} | Cliente: '{clienteNombre}' | Total: ${total:N2} | Pago: {medioPago}",
                null,
                new { Codigo = codigoVenta, Cliente = clienteNombre, Total = total, MedioPago = medioPago, Estado = "Completada" });
        }

        public async Task<Result<SaleResponseDTO>> CreateSaleAsync(CreateSaleDTO createSaleDTO, int idUsuarioVendedor)
        {
            createSaleDTO.idUsuarioVendedor = idUsuarioVendedor;

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var client = await _clientRepository.GetByIdAsync(createSaleDTO.idCliente);
                if (client == null)
                    return Result<SaleResponseDTO>.Failure(SaleErrorCode.client_not_found);

                if (createSaleDTO.items == null || !createSaleDTO.items.Any())
                    return Result<SaleResponseDTO>.Failure(SaleErrorCode.empty_cart);

                var productosResult = await ValidateAndLoadProductsAsync(createSaleDTO.items);

                if (!productosResult.IsSuccess)
                    return Result<SaleResponseDTO>.Failure(productosResult.ErrorCode);

                var productosValidados = productosResult.Value!;

                var codigoVenta = await _saleRepository.GenerateSaleCodeAsync();

                decimal total   = productosValidados.Sum(pv => (pv.producto.Precio ?? 0) * pv.cantidad);

                var venta    = BuildVentaEntity(createSaleDTO, codigoVenta, total, idUsuarioVendedor);

                var detalles = BuildDetalles(productosValidados);

                var strategyResult = await _strategyFactory.GetStrategy(createSaleDTO.idMedioPago)
                    .ProcessSaleAsync(createSaleDTO, venta, detalles);

                if (!strategyResult.IsSuccess)
                {
                    if ((SaleErrorCode)strategyResult.ErrorCode == SaleErrorCode.credit_limit_exceeded)
                    {
                        await transaction.CommitAsync();
                        _logger.LogWarning(
                            "Venta excede límite de crédito - Cliente: {Cliente}, Código: {Codigo}",
                            createSaleDTO.idCliente, codigoVenta);

                        var ventaPendiente = await _context.VentaPendiente
                            .Where(vp => vp.IdCliente == createSaleDTO.idCliente)
                            .OrderByDescending(vp => vp.FechaRegistro)
                            .FirstOrDefaultAsync();

                        string nombreClientePendiente = !string.IsNullOrWhiteSpace(client.RazonSocial)
                            ? client.RazonSocial
                            : $"{client.Nombre} {client.Apellido}".Trim();

                        if (string.IsNullOrWhiteSpace(nombreClientePendiente)) nombreClientePendiente = "N/A";

                        string codigoPendiente = ventaPendiente?.CodigoVenta ?? "PENDING";

                        await LogAsync("VENTA_PENDIENTE", "VENTA",
                            $"Venta pendiente de autorización: {codigoPendiente} | Cliente: '{nombreClientePendiente}' | Total: ${total:N2} | Excedente CC",
                            null,
                            new { Codigo = codigoPendiente, Cliente = nombreClientePendiente, Total = total, Estado = "Pendiente de Autorización" });

                        return Result<SaleResponseDTO>.Success(new SaleResponseDTO
                        {
                            IdVenta          = 0,
                            IdVentaPendiente = ventaPendiente?.IdVentaPendiente,
                            CodigoVenta      = codigoPendiente,
                            Fecha            = DateTime.Now,
                            Total            = total,
                            Cliente          = nombreClientePendiente,
                            ClienteDni       = client.Dni ?? client.Cuit ?? "N/A",
                            ClienteTelefono  = client.Telefono ?? "N/A",
                            VendedorNombre   = "N/A",
                            MedioPago        = "Cuenta Corriente",
                            Estado           = "Pendiente de Autorización",
                            Items            = detalles.Select(d => new SaleItemDetailDTO
                            {
                                IdProducto     = d.IdProducto,
                                NombreProducto = productosValidados.First(pv => pv.producto.IdProducto == d.IdProducto).producto.Nombre ?? "N/A",
                                MarcaProducto  = productosValidados.First(pv => pv.producto.IdProducto == d.IdProducto).producto.Marca ?? "N/A",
                                Cantidad       = d.Cantidad ?? 0,
                                PrecioUnitario = d.PrecioVenta ?? 0,
                                Subtotal       = d.SubTotal ?? 0
                            }).ToList()
                        });
                    }

                    await transaction.RollbackAsync();

                    return Result<SaleResponseDTO>.Failure(strategyResult.ErrorCode);
                }

                var ventaCreada = await _saleRepository.CreateSaleAsync(venta);

                foreach (var detalle in detalles)
                    detalle.IdVenta = ventaCreada.IdVenta;

                await _saleRepository.AddSaleItemsAsync(detalles);

                if (createSaleDTO.idMedioPago == 2)
                {
                    bool ccOk = await RegisterCCMovementAsync(createSaleDTO, ventaCreada, total, idUsuarioVendedor);

                    if (!ccOk)
                    {
                        await transaction.RollbackAsync();
                        return Result<SaleResponseDTO>.Failure(SaleErrorCode.unexpected_error);
                    }
                }

                await UpdateStockAfterSaleAsync(detalles, ventaCreada.CodigoVenta, idUsuarioVendedor);

                await transaction.CommitAsync();

                var ventaCompleta = await _saleRepository.GetSaleByIdAsync(ventaCreada.IdVenta);

                var response = _mapper.Map<SaleResponseDTO>(ventaCompleta);

                string clienteNombre = !string.IsNullOrWhiteSpace(client.RazonSocial)
                    ? client.RazonSocial
                    : $"{client.Nombre} {client.Apellido}".Trim();

                if (string.IsNullOrWhiteSpace(clienteNombre)) clienteNombre = "N/A";

                string medioPagoNombre = createSaleDTO.idMedioPago == 2 ? "Cuenta Corriente" : "Contado";

                await LogCreateSaleAsync(ventaCreada.CodigoVenta, clienteNombre, total, medioPagoNombre);

                return Result<SaleResponseDTO>.Success(response);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error inesperado al crear venta");
                return Result<SaleResponseDTO>.Failure(SaleErrorCode.unexpected_error);
            }
        }


        public async Task<Result<SaleResponseDTO>> GetSaleByIdAsync(int idVenta)
        {
            try
            {
                _logger.LogInformation("Obteniendo venta por ID: {IdVenta}", idVenta);

                var venta = await _saleRepository.GetSaleByIdAsync(idVenta);

                if (venta == null)
                {
                    _logger.LogWarning("Venta {IdVenta} no encontrada", idVenta);
                    return Result<SaleResponseDTO>.Failure(SaleErrorCode.sale_not_found);
                }

                var response = _mapper.Map<SaleResponseDTO>(venta);

                return Result<SaleResponseDTO>.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo venta {IdVenta}", idVenta);
                return Result<SaleResponseDTO>.Failure(SaleErrorCode.unexpected_error);
            }
        }

        public async Task<Result<PagedList<SaleListDTO>>> GetSalesPagedAsync(
            int pageNumber,
            int pageSize,
            string? clienteFilter,
            DateTime? fechaDesde,
            DateTime? fechaHasta,
            string? estadoFilter,
            int? idCliente = null)
        {
            try
            {
                _logger.LogInformation(
                    "Obteniendo ventas paginadas - Página: {Page}, Tamaño: {Size}, Cliente: {Cliente}, Desde: {Desde}, Hasta: {Hasta}, Estado: {Estado}",
                    pageNumber, pageSize, clienteFilter, fechaDesde, fechaHasta, estadoFilter
                );

                // Normaliza el valor enviado por el frontend al string exacto guardado en la DB.
                // DB usa: "aprobado", "rechazado", "cancelado", "pendiente", "Anulada"
                string? dbEstado = estadoFilter?.ToLower() switch
                {
                    "aprobada"  => "aprobado",
                    "rechazada"                => "rechazado",
                    "anulada"                  => "Anulada",
                    "cancelada"                => "cancelado",
                    "pendiente"                => "pendiente",
                    null or ""                 => null,
                    var other                  => other  // fallback: usar tal cual
                };

                // ===== 1. OBTENER VENTAS NORMALES (TABLA VENTA) =====
                var queryVentas = _saleRepository.SalesQueryable();

                // Aplicar filtros a ventas normales
                if (idCliente.HasValue)
                    queryVentas = queryVentas.Where(v => v.IdCliente == idCliente.Value);

                if (!string.IsNullOrWhiteSpace(clienteFilter))
                {
                    var lowerFilter = clienteFilter.ToLower();
                    queryVentas = queryVentas.Where(v =>
                        (v.IdClienteNavigation.Nombre != null &&
                         v.IdClienteNavigation.Nombre.ToLower().Contains(lowerFilter)) ||
                        (v.IdClienteNavigation.RazonSocial != null &&
                         v.IdClienteNavigation.RazonSocial.ToLower().Contains(lowerFilter))
                    );
                }

                if (fechaDesde.HasValue)
                {
                    queryVentas = queryVentas.Where(v => v.Fecha >= fechaDesde.Value);
                }

                if (fechaHasta.HasValue)
                {
                    var fechaHastaFin = fechaHasta.Value.Date.AddDays(1).AddTicks(-1);
                    queryVentas = queryVentas.Where(v => v.Fecha <= fechaHastaFin);
                }

                if (dbEstado != null)
                {
                    queryVentas = queryVentas.Where(v => v.IdEstadoNavigation.Estado1 == dbEstado);
                }

                // Proyectar ventas normales a DTO
                var ventasDTO = queryVentas.Select(v => new SaleListDTO
                {
                    IdVenta = v.IdVenta,
                    CodigoVenta = v.CodigoVenta,
                    Fecha = v.Fecha ?? DateTime.MinValue,
                    Total = v.Total ?? 0,
                    Cliente = !string.IsNullOrEmpty(v.IdClienteNavigation.RazonSocial)
                        ? v.IdClienteNavigation.RazonSocial
                        : (v.IdClienteNavigation.Nombre + " " + v.IdClienteNavigation.Apellido),
                    MedioPago = v.IdMedioPagoNavigation.MedioPago1 ?? "N/A",
                    Estado = v.IdEstadoNavigation.Estado1 ?? "N/A",
                    Vendedor = v.IdUsuarioNavigation.Nombre + " " +
                              v.IdUsuarioNavigation.Apellido
                });

                // ===== 2. OBTENER VENTAS PENDIENTES RECHAZADAS =====
                // Solo se incluyen si no hay filtro de estado, o si el filtro es "rechazado"
                var queryRechazadas = _context.VentaPendiente
                    .Where(vp => vp.IdEstado == 3 && (dbEstado == null || dbEstado == "rechazado")) // 3 = Rechazada
                    .Include(vp => vp.IdClienteNavigation)
                    .Include(vp => vp.IdMedioPagoNavigation)
                    .Include(vp => vp.IdEstadoNavigation)
                    .Include(vp => vp.IdUsuarioVendedorNavigation)
                    .AsNoTracking();

                // Aplicar filtros a ventas rechazadas
                if (idCliente.HasValue)
                    queryRechazadas = queryRechazadas.Where(vp => vp.IdCliente == idCliente.Value);

                if (!string.IsNullOrWhiteSpace(clienteFilter))
                {
                    var lowerFilter = clienteFilter.ToLower();

                    queryRechazadas = queryRechazadas.Where(vp =>
                        (vp.IdClienteNavigation.Nombre != null &&
                         vp.IdClienteNavigation.Nombre.ToLower().Contains(lowerFilter)) ||
                        (vp.IdClienteNavigation.RazonSocial != null &&
                         vp.IdClienteNavigation.RazonSocial.ToLower().Contains(lowerFilter))
                    );

                }

                if (fechaDesde.HasValue)
                {
                    queryRechazadas = queryRechazadas.Where(vp => vp.FechaRegistro >= fechaDesde.Value);
                }

                if (fechaHasta.HasValue)
                {
                    var fechaHastaFin = fechaHasta.Value.Date.AddDays(1).AddTicks(-1);
                    queryRechazadas = queryRechazadas.Where(vp => vp.FechaRegistro <= fechaHastaFin);
                }

                // Proyectar ventas rechazadas a DTO
                var rechazadasDTO = queryRechazadas.Select(vp => new SaleListDTO
                {
                    IdVenta = vp.IdVentaPendiente, // Usar IdVentaPendiente como IdVenta
                    CodigoVenta = vp.CodigoVenta,
                    Fecha = vp.FechaRegistro,
                    Total = vp.Total,
                    Cliente = !string.IsNullOrEmpty(vp.IdClienteNavigation.RazonSocial)
                        ? vp.IdClienteNavigation.RazonSocial
                        : (vp.IdClienteNavigation.Nombre + " " + vp.IdClienteNavigation.Apellido),
                    MedioPago = vp.IdMedioPagoNavigation.MedioPago1 ?? "N/A",
                    Estado = vp.IdEstadoNavigation.Estado1 ?? "Rechazada",
                    Vendedor = vp.IdUsuarioVendedorNavigation.Nombre + " " +
                              vp.IdUsuarioVendedorNavigation.Apellido
                });

                // ===== 3. COMBINAR AMBOS RESULTADOS =====
                var combinedQuery = ventasDTO.Concat(rechazadasDTO)
                    .OrderByDescending(s => s.Fecha);

                // ===== 4. APLICAR PAGINACIÓN =====
                var pagedList = await PagedList<SaleListDTO>.CreateAsync(
                    combinedQuery,
                    pageNumber,
                    pageSize
                );

                _logger.LogInformation(
                    "Ventas obtenidas - Total: {Total}, Página actual: {Page}",
                    pagedList.TotalCount, pagedList.PagedIndex
                );

                return Result<PagedList<SaleListDTO>>.Success(pagedList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo ventas paginadas");
                return Result<PagedList<SaleListDTO>>.Failure(SaleErrorCode.unexpected_error);
            }
        }

        private async Task RestituirStockAsync(ICollection<DetalleVentum> detalles, string codigoVenta, int idUsuario)
        {
            foreach (var item in detalles)
            {
                decimal cantidad = item.Cantidad ?? 0;

                if (cantidad > 0)
                    await _stockMovementService.RegistrarMovimientoAsync(
                        item.IdProducto,
                        TipoMovimientoStockEnum.ReingresoAnulacionVenta,
                        cantidad,
                        $"ANULACION-VENTA:{codigoVenta}",
                        idUsuario);
            }
        }

        private static MovimientoCc BuildNotaCreditoMovimiento(
            int clientId, Ventum venta, AnnulSaleDTO dto, MotivoNotaCredito motivo, CalculationResult calc, int idUsuario)
        {
            string detalle = motivo.Nombre;

            if (!string.IsNullOrWhiteSpace(dto.DetalleAdicional))
                detalle += $" — {dto.DetalleAdicional}";

            detalle += $" — Venta {venta.CodigoVenta}";

            return new MovimientoCc
            {
                IdCliente         = clientId,
                Importe           = venta.Total ?? 0,
                Detalle           = detalle,
                IdEstado          = 2,
                IdTipoMovimiento  = (int)TypeMovement.NOTA_CREDITO,
                IdUsuarioRegistra = idUsuario,
                IdVenta           = venta.IdVenta,
                SaldoActual       = calc.NewBalance,
                LimiteCuenta      = calc.NewLimit,
                Fecha             = DateTime.Now,
                IdMotivoNc        = dto.IdMotivo
            };
        }

        private async Task<int?> CreateNotaCreditoAsync(Ventum venta, AnnulSaleDTO dto, MotivoNotaCredito motivo, int idUsuario)
        {
            int clientId = venta.IdCliente ?? 0;

            var lastMovement = await _accountMovementRepository.GetLastMovement(clientId);
            if (lastMovement is null) return null;

            decimal importeNc = venta.Total ?? 0;

            var calc = _movementStrategyFactory.GetStrategy(TypeMovement.NOTA_CREDITO)
                .Calculate(lastMovement.SaldoActual ?? 0, lastMovement.LimiteCuenta ?? 0, importeNc);

            var movimientoNc = BuildNotaCreditoMovimiento(clientId, venta, dto, motivo, calc, idUsuario);

            await _accountMovementRepository.CreateMovement(movimientoNc);

            await _currentAccountService.RecomputeMontoPagadoPublic(clientId);

            return movimientoNc.IdMovimiento;
        }

        private async Task LogAnnulSaleAsync(Ventum venta, string nombreCliente)
        {
            await LogAsync("VENTA_ANULADA", "VENTA",
                $"Venta anulada: {venta.CodigoVenta} | Cliente: '{nombreCliente}' | Total: ${venta.Total:N2}",
                new { Codigo = venta.CodigoVenta, Cliente = nombreCliente, Total = venta.Total, Estado = "Completada" },
                new { Estado = "Anulada" });
        }

        public async Task<Result<AnnulSaleResponseDTO>> AnnulSaleAsync(int idVenta, AnnulSaleDTO dto, int idUsuarioRegistra)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                await _context.Database.SetAuditContextAsync(idUsuarioRegistra);

                var venta = await _saleRepository.GetSaleWithDetailsAsync(idVenta);

                if (venta is null)
                    return Result<AnnulSaleResponseDTO>.Failure(SaleErrorCode.sale_not_found);

                if (venta.IdEstadoNavigation?.Estado1?.ToLower().Contains("anulad") == true)
                    return Result<AnnulSaleResponseDTO>.Failure(SaleErrorCode.sale_already_annulled);

                var motivo = await _creditNoteReasonRepository.GetByIdAsync(dto.IdMotivo);
                if (motivo is null)
                    return Result<AnnulSaleResponseDTO>.Failure(SaleErrorCode.credit_note_reason_not_found);
                if (!motivo.Activo)
                    return Result<AnnulSaleResponseDTO>.Failure(SaleErrorCode.credit_note_reason_inactive);

                const int idEstadoAnulada = 5;
                await _saleRepository.AnnulSaleInDbAsync(idVenta, idEstadoAnulada, dto.IdMotivo, dto.DetalleAdicional);

                await RestituirStockAsync(venta.DetalleVenta, venta.CodigoVenta, idUsuarioRegistra);

                int? idMovimientoNc = null;
                if (venta.IdMedioPago == 2)
                    idMovimientoNc = await CreateNotaCreditoAsync(venta, dto, motivo, idUsuarioRegistra);

                await transaction.CommitAsync();

                var clienteAnulacion = await _clientRepository.GetByIdAsync(venta.IdCliente ?? 0);

                string nombreCliente = !string.IsNullOrWhiteSpace(clienteAnulacion?.RazonSocial)
                    ? clienteAnulacion.RazonSocial
                    : $"{clienteAnulacion?.Nombre} {clienteAnulacion?.Apellido}".Trim();

                if (string.IsNullOrWhiteSpace(nombreCliente)) nombreCliente = "N/A";

                await LogAnnulSaleAsync(venta, nombreCliente);

                return Result<AnnulSaleResponseDTO>.Success(new AnnulSaleResponseDTO
                {
                    IdVenta        = idVenta,
                    CodigoVenta    = venta.CodigoVenta,
                    Estado         = "Anulada",
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

        // ═══════════════════════════════════════════════════════════════════════
        // EXPORTACIONES
        // ═══════════════════════════════════════════════════════════════════════

        public async Task<Result<byte[]>> ExportSalesExcelAsync(DateOnly? fechaDesde, DateOnly? fechaHasta, string? estadoVenta)
        {
            try
            {
                var ventas = await BuildSalesListAsync(null, fechaDesde, fechaHasta, estadoVenta);
                var bytes = GenerateSalesExcel(ventas, null, fechaDesde, fechaHasta, estadoVenta);
                return Result<byte[]>.Success(bytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exportando ventas a Excel");
                return Result<byte[]>.Failure(SaleErrorCode.unexpected_error);
            }
        }

        public async Task<Result<byte[]>> ExportSalesPdfAsync(DateOnly? fechaDesde, DateOnly? fechaHasta, string? estadoVenta)
        {
            try
            {
                var ventas = await BuildSalesListAsync(null, fechaDesde, fechaHasta, estadoVenta);
                var ferreteria = await _ferreteriaRepository.GetAsync();
                var bytes = GenerateSalesPdf(ventas, ferreteria, "LISTADO DE VENTAS", null, fechaDesde, fechaHasta, estadoVenta);
                return Result<byte[]>.Success(bytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exportando ventas a PDF");
                return Result<byte[]>.Failure(SaleErrorCode.unexpected_error);
            }
        }

        public async Task<Result<byte[]>> ExportClientSalesExcelAsync(int idCliente, DateOnly? fechaDesde, DateOnly? fechaHasta, string? estadoVenta)
        {
            try
            {
                var ventas = await BuildSalesListAsync(idCliente, fechaDesde, fechaHasta, estadoVenta);
                var bytes = GenerateSalesExcel(ventas, idCliente, fechaDesde, fechaHasta, estadoVenta);
                return Result<byte[]>.Success(bytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exportando historial de cliente {Id} a Excel", idCliente);
                return Result<byte[]>.Failure(SaleErrorCode.unexpected_error);
            }
        }

        public async Task<Result<byte[]>> ExportClientSalesPdfAsync(int idCliente, DateOnly? fechaDesde, DateOnly? fechaHasta, string? estadoVenta)
        {
            try
            {
                var ventas = await BuildSalesListAsync(idCliente, fechaDesde, fechaHasta, estadoVenta);
                var ferreteria = await _ferreteriaRepository.GetAsync();
                var clienteNombre = ventas.FirstOrDefault()?.Cliente ?? $"Cliente #{idCliente}";
                var bytes = GenerateSalesPdf(ventas, ferreteria, $"HISTORIAL DE VENTAS — {clienteNombre}", clienteNombre, fechaDesde, fechaHasta, estadoVenta);
                return Result<byte[]>.Success(bytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exportando historial de cliente {Id} a PDF", idCliente);
                return Result<byte[]>.Failure(SaleErrorCode.unexpected_error);
            }
        }

        public async Task<Result<byte[]>> ExportSaleTicketExcelAsync(int idVenta)
        {
            try
            {
                var venta = await _saleRepository.GetSaleByIdAsync(idVenta);
                if (venta is null)
                    return Result<byte[]>.Failure(SaleErrorCode.sale_not_found);

                var ferreteria = await _ferreteriaRepository.GetAsync();
                var bytes = GenerateSaleTicketExcel(venta, ferreteria);
                return Result<byte[]>.Success(bytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exportando comprobante de venta {Id} a Excel", idVenta);
                return Result<byte[]>.Failure(SaleErrorCode.unexpected_error);
            }
        }

        // ─── Helpers privados ────────────────────────────────────────────────

        private async Task<List<SaleListItemPdf>> BuildSalesListAsync(
            int? idCliente,
            DateOnly? fechaDesde,
            DateOnly? fechaHasta,
            string? estadoVenta)
        {
            string? dbEstado = estadoVenta?.ToLower() switch
            {
                "aprobada"  => "aprobado",
                "rechazada" => "rechazado",
                "anulada"   => "Anulada",
                "cancelada" => "cancelado",
                "pendiente" => "pendiente",
                null or ""  => null,
                var other   => other
            };

            var query = _saleRepository.SalesQueryable();

            if (idCliente.HasValue)
                query = query.Where(v => v.IdCliente == idCliente.Value);

            if (fechaDesde.HasValue)
                query = query.Where(v => v.Fecha >= fechaDesde.Value.ToDateTime(TimeOnly.MinValue));

            if (fechaHasta.HasValue)
                query = query.Where(v => v.Fecha <= fechaHasta.Value.ToDateTime(TimeOnly.MaxValue));

            if (!string.IsNullOrWhiteSpace(dbEstado))
                query = query.Where(v => v.IdEstadoNavigation.Estado1 == dbEstado);

            var list = await query.ToListAsync();

            return list.Select((v, i) => new SaleListItemPdf
            {
                Numero = i + 1,
                CodigoVenta = v.CodigoVenta,
                Cliente = !string.IsNullOrEmpty(v.IdClienteNavigation?.RazonSocial)
                    ? v.IdClienteNavigation.RazonSocial
                    : $"{v.IdClienteNavigation?.Nombre} {v.IdClienteNavigation?.Apellido}".Trim(),
                Fecha = v.Fecha?.ToString("dd/MM/yyyy HH:mm") ?? "-",
                MedioPago = v.IdMedioPagoNavigation?.MedioPago1 ?? "-",
                Estado = v.IdEstadoNavigation?.Estado1 ?? "-",
                Vendedor = $"{v.IdUsuarioNavigation?.Nombre} {v.IdUsuarioNavigation?.Apellido}".Trim(),
                Total = v.Total ?? 0
            }).ToList();
        }

        private static byte[] GenerateSalesPdf(
            List<SaleListItemPdf> ventas,
            Ferreteria ferreteria,
            string titulo,
            string? filtroCliente,
            DateOnly? fechaDesde,
            DateOnly? fechaHasta,
            string? estadoVenta)
        {
            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

            var ds = new SaleListDataSource
            {
                DatosFerreteria  = ferreteria,
                Titulo           = titulo,
                FechaGeneracion  = DateTime.Now,
                FiltroCliente    = filtroCliente,
                FiltroFechaDesde = fechaDesde?.ToString("dd/MM/yyyy"),
                FiltroFechaHasta = fechaHasta?.ToString("dd/MM/yyyy"),
                FiltroEstado     = estadoVenta,
                TotalVentas      = ventas.Count,
                TotalRecaudado   = ventas.Sum(v => v.Total),
                Ventas           = ventas
            };

            return new SaleListDocument(ds).GeneratePdf();
        }

        private static byte[] GenerateSalesExcel(
            List<SaleListItemPdf> ventas,
            int? idCliente,
            DateOnly? fechaDesde,
            DateOnly? fechaHasta,
            string? estadoVenta)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("Ventas");

            // Headers
            var headers = new[] { "#", "Código", "Cliente", "Fecha", "Vendedor", "Medio Pago", "Estado", "Total" };
            for (int c = 0; c < headers.Length; c++)
            {
                ws.Cells[1, c + 1].Value = headers[c];
                ws.Cells[1, c + 1].Style.Font.Bold = true;
                ws.Cells[1, c + 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                ws.Cells[1, c + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(30, 86, 160));
                ws.Cells[1, c + 1].Style.Font.Color.SetColor(System.Drawing.Color.White);
            }

            // Rows
            for (int i = 0; i < ventas.Count; i++)
            {
                var v = ventas[i];
                int row = i + 2;
                ws.Cells[row, 1].Value = v.Numero;
                ws.Cells[row, 2].Value = v.CodigoVenta;
                ws.Cells[row, 3].Value = v.Cliente;
                ws.Cells[row, 4].Value = v.Fecha;
                ws.Cells[row, 5].Value = v.Vendedor;
                ws.Cells[row, 6].Value = v.MedioPago;
                ws.Cells[row, 7].Value = v.Estado;
                ws.Cells[row, 8].Value = v.Total;
                ws.Cells[row, 8].Style.Numberformat.Format = "#,##0.00";

                if (i % 2 == 1)
                {
                    ws.Cells[row, 1, row, 8].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    ws.Cells[row, 1, row, 8].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(240, 244, 250));
                }
            }

            // Total row
            int totalRow = ventas.Count + 2;
            ws.Cells[totalRow, 7].Value = "TOTAL:";
            ws.Cells[totalRow, 7].Style.Font.Bold = true;
            ws.Cells[totalRow, 8].Value = ventas.Sum(v => v.Total);
            ws.Cells[totalRow, 8].Style.Font.Bold = true;
            ws.Cells[totalRow, 8].Style.Numberformat.Format = "#,##0.00";

            ws.Cells[ws.Dimension.Address].AutoFitColumns();
            return package.GetAsByteArray();
        }

        private static byte[] GenerateSaleTicketExcel(Ventum venta, Ferreteria ferreteria)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("Comprobante");

            var clienteNombre = !string.IsNullOrEmpty(venta.IdClienteNavigation?.RazonSocial)
                ? venta.IdClienteNavigation.RazonSocial
                : $"{venta.IdClienteNavigation?.Nombre} {venta.IdClienteNavigation?.Apellido}".Trim();

            var vendedorNombre = $"{venta.IdUsuarioNavigation?.Nombre} {venta.IdUsuarioNavigation?.Apellido}".Trim();

            // Header ferretería
            ws.Cells["A1"].Value = ferreteria.Nombre;
            ws.Cells["A1"].Style.Font.Bold = true;
            ws.Cells["A1"].Style.Font.Size = 14;

            ws.Cells["A2"].Value = "COMPROBANTE DE VENTA";
            ws.Cells["A2"].Style.Font.Bold = true;
            ws.Cells["A2"].Style.Font.Size = 12;

            ws.Cells["A3"].Value = $"Código: {venta.CodigoVenta}";
            ws.Cells["D3"].Value = $"Fecha: {venta.Fecha:dd/MM/yyyy HH:mm}";

            ws.Cells["A4"].Value = $"Cliente: {clienteNombre}";
            ws.Cells["D4"].Value = $"DNI/CUIT: {venta.IdClienteNavigation?.Dni ?? venta.IdClienteNavigation?.Cuit ?? "-"}";

            ws.Cells["A5"].Value = $"Vendedor: {vendedorNombre}";
            ws.Cells["D5"].Value = $"Medio de Pago: {venta.IdMedioPagoNavigation?.MedioPago1 ?? "-"}";

            ws.Cells["A6"].Value = $"Estado: {venta.IdEstadoNavigation?.Estado1 ?? "-"}";

            // Separador
            ws.Cells["A7"].Value = "";

            // Headers productos
            int headerRow = 8;
            var prodHeaders = new[] { "#", "Producto", "Marca", "Cantidad [Unidad]", "P. Unitario", "Subtotal" };
            for (int c = 0; c < prodHeaders.Length; c++)
            {
                ws.Cells[headerRow, c + 1].Value = prodHeaders[c];
                ws.Cells[headerRow, c + 1].Style.Font.Bold = true;
                ws.Cells[headerRow, c + 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                ws.Cells[headerRow, c + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(30, 86, 160));
                ws.Cells[headerRow, c + 1].Style.Font.Color.SetColor(System.Drawing.Color.White);
            }

            // Rows productos
            int dataRow = headerRow + 1;
            int idx = 1;
            foreach (var d in venta.DetalleVenta)
            {
                var unidad = d.IdProductoNavigation?.IdUnidadMedidaNavigation?.Abreviatura;
                var cantTexto = string.IsNullOrWhiteSpace(unidad)
                    ? (d.Cantidad ?? 0).ToString("G29")
                    : $"{d.Cantidad ?? 0:G29} [{unidad}]";

                ws.Cells[dataRow, 1].Value = idx++;
                ws.Cells[dataRow, 2].Value = d.IdProductoNavigation?.Nombre ?? "-";
                ws.Cells[dataRow, 3].Value = d.IdProductoNavigation?.Marca ?? "-";
                ws.Cells[dataRow, 4].Value = cantTexto;
                ws.Cells[dataRow, 5].Value = d.PrecioVenta ?? 0;
                ws.Cells[dataRow, 5].Style.Numberformat.Format = "#,##0.00";
                ws.Cells[dataRow, 6].Value = d.SubTotal ?? 0;
                ws.Cells[dataRow, 6].Style.Numberformat.Format = "#,##0.00";
                dataRow++;
            }

            // Total
            ws.Cells[dataRow, 5].Value = "TOTAL:";
            ws.Cells[dataRow, 5].Style.Font.Bold = true;
            ws.Cells[dataRow, 6].Value = venta.Total ?? 0;
            ws.Cells[dataRow, 6].Style.Font.Bold = true;
            ws.Cells[dataRow, 6].Style.Numberformat.Format = "#,##0.00";

            ws.Cells[ws.Dimension.Address].AutoFitColumns();
            return package.GetAsByteArray();
        }
    }

}

