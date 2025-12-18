using AutoMapper;
using proyecto_venta_stock.Data;
using proyecto_venta_stock.Models;
using proyecto_venta_stock.Shared.ResultPattern;
using venta_stock_webapi.Client.Repository;
using venta_stock_webapi.Sale.DTO;
using venta_stock_webapi.Sale.Message;
using venta_stock_webapi.Sale.Repository;
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

        private readonly ILogger<SaleService> _logger;

        public SaleService(ISaleRepository saleRepository, IClientRepository clientRepository, IProductRepository productRepository,
        VentaStockContext context,
         IMapper mapper, ILogger<SaleService> logger)
        {
            _saleRepository = saleRepository;
            _clientRepository = clientRepository;
            _productRepository = productRepository;
            _mapper = mapper;
            _context = context;
            _logger = logger;
        }

        public async Task<Result<SaleResponseDTO>> CreateSaleAsync(CreateSaleDTO createSaleDTO)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // ===== VALIDACIONES =====

                // 1. Validar que el cliente existe y está activo
                var client = await _clientRepository.GetByIdAsync(createSaleDTO.idCliente);
                if (client == null)
                {
                    return Result<SaleResponseDTO>.Failure(SaleErrorCode.client_not_found);
                }
                // 2. Validar medio de pago (1 = Efectivo para esta fase)
                if (createSaleDTO.idMedioPago != 1)
                {
                    return Result<SaleResponseDTO>.Failure(SaleErrorCode.invalid_payment_method);
                }
                // 3. Validar que hay productos en el carrito
                if (createSaleDTO.items == null || !createSaleDTO.items.Any())
                {
                    return Result<SaleResponseDTO>.Failure(SaleErrorCode.empty_cart);
                }
                // 4. Validar cada producto y stock
                var productosValidados = new List<(Producto producto, int cantidad)>();

                foreach (var item in createSaleDTO.items)
                {
                    var producto = await _productRepository.GetById(item.IdProducto);

                    if (producto == null)
                    {
                        _logger.LogWarning($"Producto {item.IdProducto} no encontrado");
                        return Result<SaleResponseDTO>.Failure(SaleErrorCode.product_not_found);
                    }

                    if (!producto.Activo)
                    {
                        _logger.LogWarning($"Producto {item.IdProducto} está inactivo");
                        return Result<SaleResponseDTO>.Failure(SaleErrorCode.product_inactive);
                    }

                    // Validar stock (considerando venta sin stock)
                    if (!producto.VentaSinStock.GetValueOrDefault())
                    {
                        if (producto.Stock < item.Cantidad)
                        {
                            _logger.LogWarning($"Stock insuficiente para producto {producto.Nombre}. Stock: {producto.Stock}, Solicitado: {item.Cantidad}");
                            return Result<SaleResponseDTO>.Failure(SaleErrorCode.insufficient_stock);
                        }
                    }

                    productosValidados.Add((producto, item.Cantidad));
                }

                // ===== CREAR LA VENTA =====

                // Generar código de venta único
                var codigoVenta = await _saleRepository.GenerateSaleCodeAsync();

                // Calcular total
                decimal total = productosValidados.Sum(pv =>
                    (pv.producto.Precio ?? 0) * pv.cantidad
                );

                // Crear entidad Venta
                var venta = new Ventum
                {
                    CodigoVenta = codigoVenta,
                    Fecha = DateTime.Now,
                    Total = total,
                    IdMedioPago = createSaleDTO.idMedioPago,
                    IdCliente = createSaleDTO.idCliente,
                    IdUsuario = createSaleDTO.idUsuarioVendedor,
                    IdEstado = 2 // 2 = Completada/Aprobada (ajustar según tu DB)
                };

                // Guardar venta
                var ventaCreada = await _saleRepository.CreateSaleAsync(venta);

                // ===== CREAR DETALLE DE VENTA =====

                var detalles = new List<DetalleVentum>();

                foreach (var (producto, cantidad) in productosValidados)
                {
                    var precioVenta = producto.Precio ?? 0;
                    var subtotal = precioVenta * cantidad;

                    detalles.Add(new DetalleVentum
                    {
                        IdVenta = ventaCreada.IdVenta,
                        IdProducto = producto.IdProducto,
                        Cantidad = cantidad,
                        PrecioVenta = precioVenta,
                        SubTotal = subtotal
                    });

                    // Actualizar stock del producto
                    await _saleRepository.UpdateProductStockAsync(producto.IdProducto, cantidad);
                }

                await _saleRepository.AddSaleItemsAsync(detalles);

                // ===== COMMIT TRANSACTION =====
                await transaction.CommitAsync();

                // ===== PREPARAR RESPUESTA =====

                // Recargar venta con todas las relaciones
                var ventaCompleta = await _saleRepository.GetSaleByIdAsync(ventaCreada.IdVenta);

                // Usar AutoMapper para mapear la respuesta
                var response = _mapper.Map<SaleResponseDTO>(ventaCompleta);

                _logger.LogInformation($"Venta {codigoVenta} creada exitosamente por usuario {createSaleDTO.idUsuarioVendedor}");

                return Result<SaleResponseDTO>.Success(response);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError($"Error al crear venta: {ex}");
                return Result<SaleResponseDTO>.Failure(SaleErrorCode.unexpected_error);
            }
        }

      /*   public Task<Result<SaleResponseDTO>> GetSaleByIdAsync(int idVenta)
        {
            throw new NotImplementedException();
        } */

    }

}

