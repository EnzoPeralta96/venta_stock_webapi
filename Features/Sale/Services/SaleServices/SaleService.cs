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

        public SaleService(
            ISaleRepository saleRepository,
            IClientRepository clientRepository,
            IProductRepository productRepository,
            VentaStockContext context,
            IMapper mapper,
            ISaleStrategyFactory strategyFactory,
            ILogger<SaleService> logger)
        {
            _saleRepository = saleRepository;
            _clientRepository = clientRepository;
            _productRepository = productRepository;
            _mapper = mapper;
            _context = context;
            _strategyFactory = strategyFactory;
            _logger = logger;
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

                var productosValidados = new List<(Producto producto, int cantidad)>();
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

                        return Result<SaleResponseDTO>.Success(new SaleResponseDTO
                        {
                            IdVenta = 0,
                            CodigoVenta = "PENDING",
                            Fecha = DateTime.Now,
                            Total = total,
                            Cliente = client.Nombre ?? client.RazonSocial ?? "N/A",
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
                    var creditInfo = await _clientRepository.ObtenerInfoCreditoAsync(createSaleDTO.idCliente);
                    var nuevoSaldo = (creditInfo?.SaldoActual ?? 0) + total;

                    var movimiento = new MovimientoCc
                    {
                        IdCliente = createSaleDTO.idCliente,
                        IdVenta = ventaCreada.IdVenta,  // Ahora la venta ya tiene ID
                        IdTipoMovimiento = 5,  // movimiento_cc (consumo por venta)
                        Importe = total,
                        Fecha = DateTime.Now,
                        Detalle = $"Venta {ventaCreada.CodigoVenta}",
                        IdEstado = 2,  // Completada
                        SaldoActual = nuevoSaldo,
                        LimiteCuenta = creditInfo?.LimiteCuenta - total,
                        IdUsuarioRegistra = createSaleDTO.idUsuarioVendedor,
                        FechaAutorizacion = null,
                        IdUsuarioAutoriza = null
                    };

                    await _context.MovimientoCcs.AddAsync(movimiento);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation(
                        "Movimiento CC creado - Venta: {codigo}, Saldo nuevo: ${saldo}",
                        ventaCreada.CodigoVenta, nuevoSaldo
                    );
                }

                // ===== ACTUALIZAR STOCK =====
                foreach (var detalle in detalles)
                    await _saleRepository.UpdateProductStockAsync(detalle.IdProducto, detalle.Cantidad ?? 0);

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
            DateTime? fechaHasta)
        {
            try
            {
                _logger.LogInformation(
                    "Obteniendo ventas paginadas - Página: {Page}, Tamaño: {Size}, Cliente: {Cliente}, Desde: {Desde}, Hasta: {Hasta}",
                    pageNumber, pageSize, clienteFilter, fechaDesde, fechaHasta
                );

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
                var queryRechazadas = _context.VentaPendiente
                    .Where(vp => vp.IdEstado == 3) // 3 = Rechazada
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
    }

}

