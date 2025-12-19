using AutoMapper;
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

                // Obtener query base
                var query = _saleRepository.SalesQueryable();

                // Aplicar filtros
                if (!string.IsNullOrWhiteSpace(clienteFilter))
                {
                    var lowerFilter = clienteFilter.ToLower();
                    query = query.Where(v =>
                        (v.IdClienteNavigation.Nombre != null &&
                         v.IdClienteNavigation.Nombre.ToLower().Contains(lowerFilter)) ||
                        (v.IdClienteNavigation.RazonSocial != null &&
                         v.IdClienteNavigation.RazonSocial.ToLower().Contains(lowerFilter))
                    );
                }

                if (fechaDesde.HasValue)
                {
                    query = query.Where(v => v.Fecha >= fechaDesde.Value);
                }

                if (fechaHasta.HasValue)
                {
                    // Incluir todo el día hasta las 23:59:59
                    var fechaHastaFin = fechaHasta.Value.Date.AddDays(1).AddTicks(-1);
                    query = query.Where(v => v.Fecha <= fechaHastaFin);
                }

                // Ordenar por fecha descendente (más recientes primero)
                query = query.OrderByDescending(v => v.Fecha);

                // Proyectar a DTO
                var dtoQuery = query.Select(v => new SaleListDTO
                {
                    IdVenta = v.IdVenta,
                    CodigoVenta = v.CodigoVenta,
                    Fecha = v.Fecha ?? DateTime.MinValue,
                    Total = v.Total ?? 0,
                    Cliente = v.IdClienteNavigation.Nombre ??
                             v.IdClienteNavigation.RazonSocial ?? "N/A",
                    MedioPago = v.IdMedioPagoNavigation.MedioPago1 ?? "N/A",
                    Estado = v.IdEstadoNavigation.Estado1 ?? "N/A",
                    Vendedor = v.IdUsuarioNavigation.Nombre + " " +
                              v.IdUsuarioNavigation.Apellido
                });

                // Aplicar paginación
                var pagedList = await PagedList<SaleListDTO>.CreateAsync(
                    dtoQuery,
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

