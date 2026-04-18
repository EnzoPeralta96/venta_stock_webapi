using AutoMapper;
using Microsoft.EntityFrameworkCore;
using proyecto_venta_stock.Data;
using proyecto_venta_stock.Models;
using proyecto_venta_stock.Product.ProductRepository;
using proyecto_venta_stock.Shared.ResultPattern;
using venta_stock_webapi.Sale.DTO;
using venta_stock_webapi.Sale.Message;
using venta_stock_webapi.Sale.Repository;
using venta_stock_webapi.Shared.Extensions;

namespace venta_stock_webapi.Sale.Services
{
    public class PendingSaleService : IPendingSaleService
    {
        private readonly IPendingSaleRepository _pendingSaleRepository;
        private readonly ISaleRepository _saleRepository;
        private readonly IProductRepository _productRepository;
        private readonly VentaStockContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<PendingSaleService> _logger;

        public PendingSaleService(
            IPendingSaleRepository pendingSaleRepository,
            ISaleRepository saleRepository,
            IProductRepository productRepository,
            VentaStockContext context,
            IMapper mapper,
            ILogger<PendingSaleService> logger)
        {
            _pendingSaleRepository = pendingSaleRepository;
            _saleRepository = saleRepository;
            _productRepository = productRepository;
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        /// <summary>
        /// Registra una venta como pendiente cuando excede el límite de crédito
        /// </summary>
        public async Task<Result<PendingSaleResponseDTO>> CreatePendingSaleAsync(
            CreateSaleDTO saleDTO,
            decimal saldoActual,
            decimal limiteCuenta)
        {
            try
            {
                _logger.LogInformation(
                    "Creando venta pendiente - Cliente: {id}, Monto: {monto}, Límite: {limite}",
                    saleDTO.idCliente, saleDTO.items.Sum(i => i.Cantidad), limiteCuenta
                );

                // Generar código de venta
                var codigoVenta = await GenerarCodigoVentaAsync();

                // Calcular total
                decimal total = 0;
                foreach (var item in saleDTO.items)
                {
                    var product = await _productRepository.GetById(item.IdProducto);
                    total += (product.Precio ?? 0) * item.Cantidad;
                }

                var saldoDespuesVenta = saldoActual + total;
                var excedente = saldoDespuesVenta - limiteCuenta;

                // Crear venta pendiente
                var ventaPendiente = new VentaPendiente
                {
                    CodigoVenta = codigoVenta,
                    IdCliente = saleDTO.idCliente,
                    IdUsuarioVendedor = saleDTO.idUsuarioVendedor,
                    IdMedioPago = saleDTO.idMedioPago,
                    Total = total,
                    SaldoActual = saldoActual,
                    LimiteCuenta = limiteCuenta,
                    SaldoDespuesVenta = saldoDespuesVenta,
                    Excedente = excedente,
                    IdEstado = 1,  // 1 = Pendiente
                    FechaRegistro = DateTime.Now
                };

                await _context.VentaPendiente.AddAsync(ventaPendiente);
                await _context.SaveChangesAsync();

                // Crear detalles
                foreach (var item in saleDTO.items)
                {
                    var product = await _productRepository.GetById(item.IdProducto);

                    var detalle = new DetalleVentaPendiente
                    {
                        IdVentaPendiente = ventaPendiente.IdVentaPendiente,
                        IdProducto = item.IdProducto,
                        Cantidad = item.Cantidad,
                        PrecioVenta = product.Precio ?? 0,
                        Subtotal = (product.Precio ?? 0) * item.Cantidad
                    };

                    await _context.DetalleVentaPendiente.AddAsync(detalle);
                }
                await _context.SaveChangesAsync();

                // Recargar con relaciones
                var ventaCompleta = await _pendingSaleRepository.GetByIdAsync(ventaPendiente.IdVentaPendiente);

                if (ventaCompleta == null)
                {
                    _logger.LogError("No se pudo recargar la venta pendiente creada");
                    return Result<PendingSaleResponseDTO>.Failure(SaleErrorCode.unexpected_error);
                }

                // Mapear respuesta
                var response = new PendingSaleResponseDTO
                {
                    IdVentaPendiente = ventaCompleta.IdVentaPendiente,
                    CodigoVenta = ventaCompleta.CodigoVenta,
                    Total = ventaCompleta.Total,
                    Cliente = !string.IsNullOrEmpty(ventaCompleta.IdClienteNavigation.RazonSocial)
                        ? ventaCompleta.IdClienteNavigation.RazonSocial
                        : (ventaCompleta.IdClienteNavigation.Nombre + " " + ventaCompleta.IdClienteNavigation.Apellido),
                    ClienteDni = ventaCompleta.IdClienteNavigation.Dni ?? ventaCompleta.IdClienteNavigation.Cuit ?? "N/A",
                    ClienteTelefono = ventaCompleta.IdClienteNavigation.Telefono ?? "N/A",
                    Vendedor = ventaCompleta.IdUsuarioVendedorNavigation.Nombre + " " +
                              ventaCompleta.IdUsuarioVendedorNavigation.Apellido,
                    MedioPago = ventaCompleta.IdMedioPagoNavigation.MedioPago1 ?? "N/A",
                    Estado = ventaCompleta.IdEstadoNavigation.Estado1 ?? "N/A",
                    FechaRegistro = ventaCompleta.FechaRegistro,
                    SaldoActual = ventaCompleta.SaldoActual ?? 0,
                    LimiteCuenta = ventaCompleta.LimiteCuenta ?? 0,
                    SaldoDespuesVenta = ventaCompleta.SaldoDespuesVenta ?? 0,
                    Excedente = ventaCompleta.Excedente ?? 0,
                    PorcentajeExcedente = limiteCuenta > 0
                        ? (excedente / limiteCuenta) * 100
                        : 0,
                    Items = ventaCompleta.DetalleVentaPendientes.Select(d => new SaleItemDetailDTO
                    {
                        IdProducto = d.IdProducto,
                        NombreProducto = d.IdProductoNavigation.Nombre ?? "N/A",
                        MarcaProducto = d.IdProductoNavigation.Marca ?? "N/A",
                        Cantidad = d.Cantidad,
                        PrecioUnitario = d.PrecioVenta,
                        Subtotal = d.Subtotal
                    }).ToList()
                };

                _logger.LogInformation(
                    "Venta pendiente creada: {codigo}, Excedente: ${excedente}",
                    codigoVenta, excedente
                );

                return Result<PendingSaleResponseDTO>.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creando venta pendiente");
                return Result<PendingSaleResponseDTO>.Failure(SaleErrorCode.unexpected_error);
            }
        }

        /// <summary>
        /// Obtiene el detalle de una venta pendiente por ID
        /// </summary>
        public async Task<Result<PendingSaleResponseDTO>> GetPendingSaleByIdAsync(int idVentaPendiente)
        {
            try
            {
                var ventaPendiente = await _pendingSaleRepository.GetByIdAsync(idVentaPendiente);

                if (ventaPendiente == null)
                {
                    return Result<PendingSaleResponseDTO>.Failure(SaleErrorCode.pending_sale_not_found);
                }

                var response = new PendingSaleResponseDTO
                {
                    IdVentaPendiente = ventaPendiente.IdVentaPendiente,
                    CodigoVenta = ventaPendiente.CodigoVenta,
                    Total = ventaPendiente.Total,
                    Cliente = !string.IsNullOrEmpty(ventaPendiente.IdClienteNavigation.RazonSocial)
                        ? ventaPendiente.IdClienteNavigation.RazonSocial
                        : (ventaPendiente.IdClienteNavigation.Nombre + " " + ventaPendiente.IdClienteNavigation.Apellido),
                    ClienteDni = ventaPendiente.IdClienteNavigation.Dni ?? ventaPendiente.IdClienteNavigation.Cuit ?? "N/A",
                    ClienteTelefono = ventaPendiente.IdClienteNavigation.Telefono ?? "N/A",
                    Vendedor = ventaPendiente.IdUsuarioVendedorNavigation.Nombre + " " +
                              ventaPendiente.IdUsuarioVendedorNavigation.Apellido,
                    MedioPago = ventaPendiente.IdMedioPagoNavigation.MedioPago1 ?? "N/A",
                    Estado = ventaPendiente.IdEstadoNavigation.Estado1 ?? "N/A",
                    FechaRegistro = ventaPendiente.FechaRegistro,
                    SaldoActual = ventaPendiente.SaldoActual ?? 0,
                    LimiteCuenta = ventaPendiente.LimiteCuenta ?? 0,
                    SaldoDespuesVenta = ventaPendiente.SaldoDespuesVenta ?? 0,
                    Excedente = ventaPendiente.Excedente ?? 0,
                    PorcentajeExcedente = ventaPendiente.LimiteCuenta > 0
                        ? ((ventaPendiente.Excedente ?? 0) / (ventaPendiente.LimiteCuenta ?? 1)) * 100
                        : 0,
                    Items = ventaPendiente.DetalleVentaPendientes.Select(d => new SaleItemDetailDTO
                    {
                        IdProducto = d.IdProducto,
                        NombreProducto = d.IdProductoNavigation.Nombre ?? "N/A",
                        MarcaProducto = d.IdProductoNavigation.Marca ?? "N/A",
                        Cantidad = d.Cantidad,
                        PrecioUnitario = d.PrecioVenta,
                        Subtotal = d.Subtotal
                    }).ToList()
                };

                return Result<PendingSaleResponseDTO>.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo venta pendiente {id}", idVentaPendiente);
                return Result<PendingSaleResponseDTO>.Failure(SaleErrorCode.unexpected_error);
            }
        }

        /// <summary>
        /// Lista ventas pendientes con filtro opcional por estado
        /// </summary>
        public async Task<Result<List<PendingSaleListDTO>>> GetPendingSalesAsync(int? idEstado = null)
        {
            try
            {
                var pendingSales = await _pendingSaleRepository.GetPendingSalesAsync(idEstado);

                return Result<List<PendingSaleListDTO>>.Success(pendingSales);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo lista de ventas pendientes");
                return Result<List<PendingSaleListDTO>>.Failure(SaleErrorCode.unexpected_error);
            }
        }

        /// <summary>
        /// Autoriza o rechaza una venta pendiente
        /// </summary>
        public async Task<Result<AuthorizeSaleResponseDTO>> AuthorizeSaleAsync(
            AuthorizeSaleDTO dto,
            int idUsuarioAutoriza)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Necesario para auditoría en triggers de BD (fn_auditoria_generica)
                await _context.Database.SetAuditContextAsync(idUsuarioAutoriza);

                // 1. Obtener venta pendiente
                var ventaPendiente = await _pendingSaleRepository.GetByIdAsync(dto.IdVentaPendiente);
                
                if (ventaPendiente == null)
                {
                    _logger.LogWarning("Venta pendiente {id} no encontrada", dto.IdVentaPendiente);
                    return Result<AuthorizeSaleResponseDTO>.Failure(SaleErrorCode.pending_sale_not_found);
                }
                
                // 2. Validar que esté pendiente
                if (ventaPendiente.IdEstado != 1)  // 1 = Pendiente
                {
                    _logger.LogWarning(
                        "Venta pendiente {id} ya fue procesada (estado: {estado})",
                        dto.IdVentaPendiente, ventaPendiente.IdEstado
                    );
                    return Result<AuthorizeSaleResponseDTO>.Failure(
                        SaleErrorCode.pending_sale_already_processed
                    );
                }
                
                // 3. Si APROBAR
                if (dto.Aprobar)
                {
                    _logger.LogInformation(
                        "Aprobando venta pendiente {codigo} por usuario {usuario}",
                        ventaPendiente.CodigoVenta, idUsuarioAutoriza
                    );

                    // 3.1. Crear venta definitiva
                    var codigoVentaDefinitiva = await GenerarCodigoVentaAsync();
                    
                    var venta = new Ventum
                    {
                        CodigoVenta = codigoVentaDefinitiva,
                        IdCliente = ventaPendiente.IdCliente,
                        IdMedioPago = ventaPendiente.IdMedioPago,
                        IdUsuario = ventaPendiente.IdUsuarioVendedor,
                        Fecha = DateTime.Now,
                        Total = ventaPendiente.Total,
                        IdEstado = 2  // 2 = Completada
                    };
                    
                    await _context.Venta.AddAsync(venta);
                    await _context.SaveChangesAsync();
                    
                    _logger.LogDebug("Venta definitiva creada: {codigo}", venta.CodigoVenta);
                    
                    // 3.2. Crear detalles
                    foreach (var detallePendiente in ventaPendiente.DetalleVentaPendientes)
                    {
                        var detalle = new DetalleVentum
                        {
                            IdVenta = venta.IdVenta,
                            IdProducto = detallePendiente.IdProducto,
                            Cantidad = detallePendiente.Cantidad,
                            PrecioVenta = detallePendiente.PrecioVenta,
                            SubTotal = detallePendiente.Subtotal
                        };

                        await _context.DetalleVenta.AddAsync(detalle);
                    }
                    await _context.SaveChangesAsync();

                    _logger.LogDebug("Detalles de venta creados: {count} items",
                        ventaPendiente.DetalleVentaPendientes.Count);
                    
                    // 3.3. Crear movimiento en cuenta corriente
                    var movimiento = new MovimientoCc
                    {
                        IdCliente = ventaPendiente.IdCliente,
                        IdVenta = venta.IdVenta,
                        IdTipoMovimiento = 5,  // 5 = movimiento_cc (consumo por venta)
                        Importe = ventaPendiente.Total,
                        Fecha = DateTime.Now,
                        Detalle = $"Venta {venta.CodigoVenta} (aprobada por autorización)",
                        SaldoActual = ventaPendiente.SaldoDespuesVenta,
                        LimiteCuenta = ventaPendiente.LimiteCuenta - ventaPendiente.Total,
                        IdUsuarioRegistra = idUsuarioAutoriza,
                        IdEstado = 2  // 2 = Completada
                    };
                    
                    await _context.MovimientoCcs.AddAsync(movimiento);
                    await _context.SaveChangesAsync();
                    
                    _logger.LogDebug("Movimiento CC creado para venta {codigo}", venta.CodigoVenta);
                    
                    // 3.4. Actualizar stock de productos
                    foreach (var detalle in ventaPendiente.DetalleVentaPendientes)
                    {
                        await _context.Productos
                            .Where(p => p.IdProducto == detalle.IdProducto)
                            .ExecuteUpdateAsync(setters => setters
                                .SetProperty(p => p.Stock, p => p.Stock - detalle.Cantidad));

                        _logger.LogDebug(
                            "Stock actualizado - Producto: {id}, Cantidad: -{cantidad}",
                            detalle.IdProducto, detalle.Cantidad
                        );
                    }
                    
                    // 3.5. Actualizar venta pendiente
                    ventaPendiente.IdEstado = 2;  // 2 = Aprobada
                    ventaPendiente.IdUsuarioAutoriza = idUsuarioAutoriza;
                    ventaPendiente.FechaAutorizacion = DateTime.Now;
                    ventaPendiente.ObservacionesAutorizacion = dto.Observaciones;
                    ventaPendiente.IdVentaGenerada = venta.IdVenta;
                    
                    await _pendingSaleRepository.UpdateAsync(ventaPendiente);
                    
                    await transaction.CommitAsync();
                    
                    _logger.LogInformation(
                        "Venta pendiente {codigo} aprobada exitosamente. Venta generada: {ventaGenerada}",
                        ventaPendiente.CodigoVenta, venta.CodigoVenta
                    );
                    
                    return Result<AuthorizeSaleResponseDTO>.Success(new AuthorizeSaleResponseDTO
                    {
                        IdVentaPendiente = dto.IdVentaPendiente,
                        CodigoVenta = ventaPendiente.CodigoVenta,
                        Aprobada = true,
                        IdVentaGenerada = venta.IdVenta,
                        CodigoVentaGenerada = venta.CodigoVenta,
                        Mensaje = $"Venta aprobada y procesada exitosamente. Se generó la venta {venta.CodigoVenta}."
                    });
                }
                
                // 4. Si RECHAZAR
                else
                {
                    _logger.LogInformation(
                        "Rechazando venta pendiente {codigo} por usuario {usuario}",
                        ventaPendiente.CodigoVenta, idUsuarioAutoriza
                    );

                    ventaPendiente.IdEstado = 3;  // 3 = Rechazada
                    ventaPendiente.IdUsuarioAutoriza = idUsuarioAutoriza;
                    ventaPendiente.FechaAutorizacion = DateTime.Now;
                    ventaPendiente.ObservacionesAutorizacion = dto.Observaciones ?? "Rechazada por administrador";
                    
                    await _pendingSaleRepository.UpdateAsync(ventaPendiente);
                    await transaction.CommitAsync();
                    
                    _logger.LogInformation(
                        "Venta pendiente {codigo} rechazada",
                        ventaPendiente.CodigoVenta
                    );
                    
                    return Result<AuthorizeSaleResponseDTO>.Success(new AuthorizeSaleResponseDTO
                    {
                        IdVentaPendiente = dto.IdVentaPendiente,
                        CodigoVenta = ventaPendiente.CodigoVenta,
                        Aprobada = false,
                        IdVentaGenerada = null,
                        CodigoVentaGenerada = null,
                        Mensaje = "Venta rechazada. El vendedor será notificado."
                    });
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error autorizando venta pendiente {id}", dto.IdVentaPendiente);
                return Result<AuthorizeSaleResponseDTO>.Failure(SaleErrorCode.unexpected_error);
            }
        }

        // ===== MÉTODOS AUXILIARES =====

        /// <summary>
        /// Genera un código único para la venta usando el repositorio
        /// </summary>
        private async Task<string> GenerarCodigoVentaAsync()
        {
            return await _saleRepository.GenerateSaleCodeAsync();
        }
    }
}
