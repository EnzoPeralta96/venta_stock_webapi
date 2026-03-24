using AutoMapper;
using Microsoft.EntityFrameworkCore;
using proyecto_venta_stock.Data;
using proyecto_venta_stock.Models;
using proyecto_venta_stock.Shared.ResultPattern;
using venta_stock_webapi.Client.Repository;
using venta_stock_webapi.Sale.DTO;
using venta_stock_webapi.Sale.Message;
using venta_stock_webapi.Sale.Repository;
using venta_stock_webapi.Sale.Strategies;
using venta_stock_webapi.Shared.Paged;
using proyecto_venta_stock.Product.ProductRepository;
using venta_stock_webapi.CurrentAccount.Services.CurrentAccountService;
using venta_stock_webapi.CurrentAccount.DTO.MovementDTO;
using venta_stock_webapi.CurrentAccount.Services.CurrentAccountService.StrategyCurrentAccount;
using venta_stock_webapi.CurrentAccount.Repository;
using venta_stock_webapi.Features.StockMovement.Services;

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
            IStockMovementService stockMovementService)
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
        }

        public async Task<Result<SaleResponseDTO>> CreateSaleAsync(CreateSaleDTO createSaleDTO)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // ===== VALIDACIONES GENERALES =====
                var client = await _clientRepository.GetByIdAsync(createSaleDTO.idCliente);
                if (client == null)
                    return Result<SaleResponseDTO>.Failure(SaleErrorCode.client_not_found);

                if (createSaleDTO.items == null || !createSaleDTO.items.Any())
                    return Result<SaleResponseDTO>.Failure(SaleErrorCode.empty_cart);

                var productosValidados = new List<(Producto producto, decimal cantidad)>();
                foreach (var item in createSaleDTO.items)
                {
                    var producto = await _productRepository.GetById(item.IdProducto);
                    if (producto == null)
                        return Result<SaleResponseDTO>.Failure(SaleErrorCode.product_not_found);

                    if (!producto.Activo)
                        return Result<SaleResponseDTO>.Failure(SaleErrorCode.product_inactive);

                    if (!producto.VentaSinStock.GetValueOrDefault() && producto.Stock < item.Cantidad)
                        return Result<SaleResponseDTO>.Failure(SaleErrorCode.insufficient_stock);

                    productosValidados.Add((producto, item.Cantidad));
                }

                // ===== PREPARAR VENTA =====
                var codigoVenta = await _saleRepository.GenerateSaleCodeAsync();
                decimal total = productosValidados.Sum(pv => (pv.producto.Precio ?? 0) * pv.cantidad);

                var venta = new Ventum
                {
                    CodigoVenta = codigoVenta,
                    Fecha = DateTime.Now,
                    Total = total,
                    IdMedioPago = createSaleDTO.idMedioPago,
                    IdCliente = createSaleDTO.idCliente,
                    IdUsuario = createSaleDTO.idUsuarioVendedor,
                    IdEstado = 2 // Completada
                };

                var detalles = productosValidados.Select(pv => new DetalleVentum
                {
                    IdProducto = pv.producto.IdProducto,
                    Cantidad = pv.cantidad,
                    PrecioVenta = pv.producto.Precio ?? 0,
                    SubTotal = (pv.producto.Precio ?? 0) * pv.cantidad
                }).ToList();

                // ===== PROCESAR ESTRATEGIA =====
                var strategy = _strategyFactory.GetStrategy(createSaleDTO.idMedioPago);
                var strategyResult = await strategy.ProcessSaleAsync(createSaleDTO, venta, detalles);

                if (!strategyResult.IsSuccess)
                {
                    // Caso especial: excede límite -> ya se creó venta pendiente dentro de la transacción
                    if ((SaleErrorCode)strategyResult.ErrorCode == SaleErrorCode.credit_limit_exceeded)
                    {
                        await transaction.CommitAsync(); // conservar venta pendiente
                        _logger.LogWarning(
                            "Venta excede límite de crédito - Cliente: {Cliente}, Código: {Codigo}",
                            createSaleDTO.idCliente, codigoVenta
                        );

                        // Obtener la venta pendiente recién creada
                        var ventaPendiente = await _context.VentaPendiente
                            .Where(vp => vp.IdCliente == createSaleDTO.idCliente)
                            .OrderByDescending(vp => vp.FechaRegistro)
                            .FirstOrDefaultAsync();

                        return Result<SaleResponseDTO>.Success(new SaleResponseDTO
                        {
                            IdVenta = 0,
                            IdVentaPendiente = ventaPendiente?.IdVentaPendiente,
                            CodigoVenta = ventaPendiente?.CodigoVenta ?? "PENDING",
                            Fecha = DateTime.Now,
                            Total = total,
                            Cliente = client.Nombre ?? client.RazonSocial ?? "N/A",
                            ClienteDni = client.Dni ?? client.Cuit ?? "N/A",
                            ClienteTelefono = client.Telefono ?? "N/A",
                            VendedorNombre = "N/A",
                            MedioPago = "Cuenta Corriente",
                            Estado = "Pendiente de Autorización",
                            Items = detalles.Select(d => new SaleItemDetailDTO
                            {
                                IdProducto = d.IdProducto,
                                NombreProducto = productosValidados.First(pv => pv.producto.IdProducto == d.IdProducto).producto.Nombre ?? "N/A",
                                MarcaProducto = productosValidados.First(pv => pv.producto.IdProducto == d.IdProducto).producto.Marca ?? "N/A",
                                Cantidad = d.Cantidad ?? 0,
                                PrecioUnitario = d.PrecioVenta ?? 0,
                                Subtotal = d.SubTotal ?? 0
                            }).ToList()
                        });
                    }

                    await transaction.RollbackAsync();
                    return Result<SaleResponseDTO>.Failure(strategyResult.ErrorCode);
                }

                // ===== GUARDAR VENTA DEFINITIVA =====
                var ventaCreada = await _saleRepository.CreateSaleAsync(venta);
                foreach (var detalle in detalles)
                    detalle.IdVenta = ventaCreada.IdVenta;

                await _saleRepository.AddSaleItemsAsync(detalles);

                // ===== CREAR MOVIMIENTO CC SI ES VENTA A CUENTA CORRIENTE =====
                if (createSaleDTO.idMedioPago == 2) // 2 = Cuenta Corriente
                {
                    var movementResult = await _currentAccountService.RegisterMovement(new AddMovementDTO
                    {
                        IdCliente = createSaleDTO.idCliente,
                        IdTipoMovimiento = (int)TypeMovement.MOVIMIENTO_CC, // 5
                        Importe = total,
                        Detalle = $"Venta {ventaCreada.CodigoVenta}",
                        IdVenta = ventaCreada.IdVenta,
                        IdUsuarioRegistra = createSaleDTO.idUsuarioVendedor
                    });

                    if (!movementResult.IsSuccess)
                    {
                        // Si falla el movimiento, no debe quedar la venta creada sin impacto en CC
                        await transaction.RollbackAsync();
                        _logger.LogError(
                            "No se pudo registrar el movimiento de cuenta corriente para la venta {CodigoVenta} (cliente {ClienteId})",
                            ventaCreada.CodigoVenta, createSaleDTO.idCliente
                        );
                        return Result<SaleResponseDTO>.Failure(SaleErrorCode.unexpected_error);
                    }

                    _logger.LogInformation(
                        "Movimiento CC registrado por servicio - Venta: {CodigoVenta}, Cliente: {ClienteId}",
                        ventaCreada.CodigoVenta, createSaleDTO.idCliente
                    );
                }

                // ===== ACTUALIZAR STOCK (Ledger) =====
                foreach (var detalle in detalles)
                    await _stockMovementService.RegistrarMovimientoAsync(
                        detalle.IdProducto,
                        TipoMovimientoStockEnum.EgresoVenta,
                        -(detalle.Cantidad ?? 0),
                        $"VENTA:{ventaCreada.CodigoVenta}",
                        createSaleDTO.idUsuarioVendedor);

                await transaction.CommitAsync();

                var ventaCompleta = await _saleRepository.GetSaleByIdAsync(ventaCreada.IdVenta);
                var response = _mapper.Map<SaleResponseDTO>(ventaCompleta);

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
            string? estadoFilter)
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

        public async Task<Result<AnnulSaleResponseDTO>> AnnulSaleAsync(int idVenta, AnnulSaleDTO dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Requerido por el trigger de auditoría (fn_auditoria_generica).
                // ExecuteUpdateAsync bypasea SaveChanges, por lo que el interceptor no actúa.
                await _context.Database.ExecuteSqlRawAsync(
                    "SELECT set_config('app.user_id', {0}::text, true);",
                    dto.IdUsuarioRegistra);

                // 1. Obtener la venta con detalle y tracking
                var venta = await _saleRepository.GetSaleWithDetailsAsync(idVenta);
                
                if (venta is null)
                    return Result<AnnulSaleResponseDTO>.Failure(SaleErrorCode.sale_not_found);

                // 2. Validar que no esté ya anulada
                if (venta.IdEstadoNavigation?.Estado1?.ToLower().Contains("anulad") == true)
                    return Result<AnnulSaleResponseDTO>.Failure(SaleErrorCode.sale_already_annulled);

                // 3. Validar que el motivo de NC existe y está activo
                var motivo = await _creditNoteReasonRepository.GetByIdAsync(dto.IdMotivo);
                if (motivo is null)
                    return Result<AnnulSaleResponseDTO>.Failure(SaleErrorCode.credit_note_reason_not_found);
                if (!motivo.Activo)
                    return Result<AnnulSaleResponseDTO>.Failure(SaleErrorCode.credit_note_reason_inactive);

            
                const int idEstadoAnulada = 5; // Estado "Anulada" en tabla estado

                // 5. Cambiar estado de la venta → Anulada y guardar el motivo NC y detalle
                await _saleRepository.AnnulSaleInDbAsync(idVenta, idEstadoAnulada, dto.IdMotivo, dto.DetalleAdicional);

                // 6. Restituir stock de cada producto del detalle (Ledger)
                foreach (var item in venta.DetalleVenta)
                {
                    decimal cantidad = item.Cantidad ?? 0;
                    if (cantidad > 0)
                        await _stockMovementService.RegistrarMovimientoAsync(
                            item.IdProducto,
                            TipoMovimientoStockEnum.ReingresoAnulacionVenta,
                            cantidad,
                            $"ANULACION-VENTA:{venta.CodigoVenta}",
                            dto.IdUsuarioRegistra);
                }

                // 7. Si la venta fue con CC → crear NC y recomputar MontoPagado
                int? idMovimientoNc = null;
                if (venta.IdMedioPago == 2)
                {
                    int clientId = venta.IdCliente ?? 0;

                    var lastMovement = await _accountMovementRepository.GetLastMovement(clientId);
                    if (lastMovement is not null)
                    {
                        decimal importeNc = venta.Total ?? 0;
                        decimal balanceBase = lastMovement.SaldoActual ?? 0;
                        decimal limitBase = lastMovement.LimiteCuenta ?? 0;

                        var strategy = _movementStrategyFactory.GetStrategy(TypeMovement.NOTA_CREDITO);
                        var calc = strategy.Calculate(balanceBase, limitBase, importeNc);

                        string detalle = motivo.Nombre;
                        if (!string.IsNullOrWhiteSpace(dto.DetalleAdicional))
                            detalle += $" — {dto.DetalleAdicional}";
                        detalle += $" — Venta {venta.CodigoVenta}";

                        var movimientoNc = new MovimientoCc
                        {
                            IdCliente         = clientId,
                            Importe           = importeNc,
                            Detalle           = detalle,
                            IdEstado          = 2,
                            IdTipoMovimiento  = (int)TypeMovement.NOTA_CREDITO,
                            IdUsuarioRegistra = dto.IdUsuarioRegistra,
                            IdVenta           = idVenta,
                            SaldoActual       = calc.NewBalance,
                            LimiteCuenta      = calc.NewLimit,
                            Fecha             = DateTime.Now,
                            IdMotivoNc        = dto.IdMotivo
                        };

                        await _accountMovementRepository.CreateMovement(movimientoNc);
                        idMovimientoNc = movimientoNc.IdMovimiento;

                        await _currentAccountService.RecomputeMontoPagadoPublic(clientId);
                    }
                }

                await transaction.CommitAsync();

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
    }

}

