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

        private async Task<decimal> CalcularTotalAsync(List<SaleItemDTO> items)
        {
            decimal total = 0;
            foreach (var item in items)
            {
                var product = await _productRepository.GetById(item.IdProducto);
                total += (product?.Precio ?? 0) * item.Cantidad;
            }
            return total;
        }

        private static VentaPendiente BuildVentaPendienteEntity(
            CreateSaleDTO dto, string codigoVenta, decimal total, decimal saldoActual, decimal limiteCuenta)
        {
            decimal saldoDespuesVenta = saldoActual + total;
            decimal excedente        = saldoDespuesVenta - limiteCuenta;

            return new VentaPendiente
            {
                CodigoVenta       = codigoVenta,
                IdCliente         = dto.idCliente,
                IdUsuarioVendedor = dto.idUsuarioVendedor,
                IdMedioPago       = dto.idMedioPago,
                Total             = total,
                SaldoActual       = saldoActual,
                LimiteCuenta      = limiteCuenta,
                SaldoDespuesVenta = saldoDespuesVenta,
                Excedente         = excedente,
                IdEstado          = 1,
                FechaRegistro     = DateTime.Now
            };
        }

        private async Task CrearDetallesVentaPendienteAsync(VentaPendiente ventaPendiente, List<SaleItemDTO> items)
        {
            foreach (var item in items)
            {
                var product = await _productRepository.GetById(item.IdProducto);
                await _context.DetalleVentaPendiente.AddAsync(new DetalleVentaPendiente
                {
                    IdVentaPendiente = ventaPendiente.IdVentaPendiente,
                    IdProducto       = item.IdProducto,
                    Cantidad         = item.Cantidad,
                    PrecioVenta      = product?.Precio ?? 0,
                    Subtotal         = (product?.Precio ?? 0) * item.Cantidad
                });
            }
            await _context.SaveChangesAsync();
        }

        private static PendingSaleResponseDTO MapToPendingSaleResponseDTO(VentaPendiente venta)
        {
            return new PendingSaleResponseDTO
            {
                IdVentaPendiente  = venta.IdVentaPendiente,
                CodigoVenta       = venta.CodigoVenta,
                Total             = venta.Total,
                Cliente           = !string.IsNullOrEmpty(venta.IdClienteNavigation.RazonSocial)
                    ? venta.IdClienteNavigation.RazonSocial
                    : (venta.IdClienteNavigation.Nombre + " " + venta.IdClienteNavigation.Apellido),
                ClienteDni        = venta.IdClienteNavigation.Dni ?? venta.IdClienteNavigation.Cuit ?? "N/A",
                ClienteTelefono   = venta.IdClienteNavigation.Telefono ?? "N/A",
                Vendedor          = venta.IdUsuarioVendedorNavigation.Nombre + " " +
                                    venta.IdUsuarioVendedorNavigation.Apellido,
                MedioPago         = venta.IdMedioPagoNavigation.MedioPago1 ?? "N/A",
                Estado            = venta.IdEstadoNavigation.Estado1 ?? "N/A",
                FechaRegistro     = venta.FechaRegistro,
                SaldoActual       = venta.SaldoActual ?? 0,
                LimiteCuenta      = venta.LimiteCuenta ?? 0,
                SaldoDespuesVenta = venta.SaldoDespuesVenta ?? 0,
                Excedente         = venta.Excedente ?? 0,
                PorcentajeExcedente = (venta.LimiteCuenta ?? 0) > 0
                    ? ((venta.Excedente ?? 0) / (venta.LimiteCuenta ?? 1)) * 100
                    : 0,
                Items = venta.DetalleVentaPendientes.Select(d => new SaleItemDetailDTO
                {
                    IdProducto     = d.IdProducto,
                    NombreProducto = d.IdProductoNavigation.Nombre ?? "N/A",
                    MarcaProducto  = d.IdProductoNavigation.Marca ?? "N/A",
                    Cantidad       = d.Cantidad,
                    PrecioUnitario = d.PrecioVenta,
                    Subtotal       = d.Subtotal
                }).ToList()
            };
        }

        public async Task<Result<PendingSaleResponseDTO>> CreatePendingSaleAsync(
            CreateSaleDTO saleDTO,
            decimal saldoActual,
            decimal limiteCuenta)
        {
            try
            {
                _logger.LogInformation(
                    "Creando venta pendiente - Cliente: {id}, Monto: {monto}, Límite: {limite}",
                    saleDTO.idCliente, saleDTO.items.Sum(i => i.Cantidad), limiteCuenta);

                var codigoVenta = await GenerarCodigoVentaAsync();
                decimal total   = await CalcularTotalAsync(saleDTO.items);

                var ventaPendiente = BuildVentaPendienteEntity(saleDTO, codigoVenta, total, saldoActual, limiteCuenta);

                await _context.VentaPendiente.AddAsync(ventaPendiente);
                await _context.SaveChangesAsync();

                await CrearDetallesVentaPendienteAsync(ventaPendiente, saleDTO.items);

                var ventaCompleta = await _pendingSaleRepository.GetByIdAsync(ventaPendiente.IdVentaPendiente);
                if (ventaCompleta == null)
                {
                    _logger.LogError("No se pudo recargar la venta pendiente creada");
                    return Result<PendingSaleResponseDTO>.Failure(SaleErrorCode.unexpected_error);
                }

                _logger.LogInformation(
                    "Venta pendiente creada: {codigo}, Excedente: ${excedente}",
                    codigoVenta, ventaCompleta.Excedente ?? 0);

                return Result<PendingSaleResponseDTO>.Success(MapToPendingSaleResponseDTO(ventaCompleta));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creando venta pendiente");
                return Result<PendingSaleResponseDTO>.Failure(SaleErrorCode.unexpected_error);
            }
        }

        public async Task<Result<PendingSaleResponseDTO>> GetPendingSaleByIdAsync(int idVentaPendiente)
        {
            try
            {
                var ventaPendiente = await _pendingSaleRepository.GetByIdAsync(idVentaPendiente);

                if (ventaPendiente == null)
                    return Result<PendingSaleResponseDTO>.Failure(SaleErrorCode.pending_sale_not_found);

                return Result<PendingSaleResponseDTO>.Success(MapToPendingSaleResponseDTO(ventaPendiente));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo venta pendiente {id}", idVentaPendiente);
                return Result<PendingSaleResponseDTO>.Failure(SaleErrorCode.unexpected_error);
            }
        }

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

        private static (bool flowControl, Result<AuthorizeSaleResponseDTO> value) ValidateAuthorizeSale(VentaPendiente? ventaPendiente)
        {
            if (ventaPendiente == null)
                return (false, Result<AuthorizeSaleResponseDTO>.Failure(SaleErrorCode.pending_sale_not_found));

            if (ventaPendiente.IdEstado != 1)
                return (false, Result<AuthorizeSaleResponseDTO>.Failure(SaleErrorCode.pending_sale_already_processed));

            return (true, null);
        }

        private static Ventum BuildVentaDesdeAprobacion(VentaPendiente ventaPendiente, string codigoVenta)
        {
            return new Ventum
            {
                CodigoVenta = codigoVenta,
                IdCliente   = ventaPendiente.IdCliente,
                IdMedioPago = ventaPendiente.IdMedioPago,
                IdUsuario   = ventaPendiente.IdUsuarioVendedor,
                Fecha       = DateTime.Now,
                Total       = ventaPendiente.Total,
                IdEstado    = 2
            };
        }

        private static MovimientoCc BuildMovimientoCCAprobacion(
            VentaPendiente ventaPendiente, Ventum venta, MovimientoCc? lastMovement, decimal saldoAntes, int idUsuario)
        {
            decimal? montoPagado    = saldoAntes < 0 ? Math.Min(Math.Abs(saldoAntes), ventaPendiente.Total) : null;
            decimal  saldoAFavor    = Math.Max(0, -saldoAntes);
            decimal  amountFromLimit = Math.Max(0, ventaPendiente.Total - saldoAFavor);
            decimal  limiteDespues  = (lastMovement?.LimiteCuenta ?? 0) - amountFromLimit;

            return new MovimientoCc
            {
                IdCliente         = ventaPendiente.IdCliente,
                IdVenta           = venta.IdVenta,
                IdTipoMovimiento  = 5,
                Importe           = ventaPendiente.Total,
                Fecha             = DateTime.Now,
                Detalle           = $"Venta {venta.CodigoVenta} (aprobada por autorización)",
                SaldoActual       = ventaPendiente.SaldoDespuesVenta,
                LimiteCuenta      = limiteDespues,
                IdUsuarioRegistra = idUsuario,
                IdEstado          = 2,
                MontoPagado       = montoPagado
            };
        }

        private async Task ActualizarStockAprobacionAsync(ICollection<DetalleVentaPendiente> detalles)
        {
            foreach (var detalle in detalles)
            {
                await _context.Productos
                    .Where(p => p.IdProducto == detalle.IdProducto)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(p => p.Stock, p => p.Stock - detalle.Cantidad));

                _logger.LogDebug("Stock actualizado - Producto: {id}, Cantidad: -{cantidad}",
                    detalle.IdProducto, detalle.Cantidad);
            }
        }

        private async Task<AuthorizeSaleResponseDTO> AprobarVentaAsync(
            VentaPendiente ventaPendiente, AuthorizeSaleDTO dto, int idUsuarioAutoriza)
        {
            _logger.LogInformation("Aprobando venta pendiente {codigo} por usuario {usuario}",
                ventaPendiente.CodigoVenta, idUsuarioAutoriza);

            var codigoVenta = await GenerarCodigoVentaAsync();
            var venta       = BuildVentaDesdeAprobacion(ventaPendiente, codigoVenta);

            await _context.Venta.AddAsync(venta);

            await _context.SaveChangesAsync();

            _logger.LogDebug("Venta definitiva creada: {codigo}", venta.CodigoVenta);

            foreach (var dp in ventaPendiente.DetalleVentaPendientes)
                await _context.DetalleVenta.AddAsync(new DetalleVentum
                {
                    IdVenta     = venta.IdVenta,
                    IdProducto  = dp.IdProducto,
                    Cantidad    = dp.Cantidad,
                    PrecioVenta = dp.PrecioVenta,
                    SubTotal    = dp.Subtotal
                });

            await _context.SaveChangesAsync();

            _logger.LogDebug("Detalles creados: {count} items", ventaPendiente.DetalleVentaPendientes.Count);

            var lastMovement = await _context.MovimientoCcs
                .AsNoTracking()
                .Where(m => m.IdCliente == ventaPendiente.IdCliente)
                .OrderByDescending(m => m.Fecha)
                .ThenByDescending(m => m.IdMovimiento)
                .FirstOrDefaultAsync();

            decimal saldoAntes = lastMovement?.SaldoActual ?? 0;

            var movimiento = BuildMovimientoCCAprobacion(ventaPendiente, venta, lastMovement, saldoAntes, idUsuarioAutoriza);

            await _context.MovimientoCcs.AddAsync(movimiento);

            await _context.SaveChangesAsync();

            _logger.LogDebug("Movimiento CC creado para venta {codigo}", venta.CodigoVenta);

            await ActualizarStockAprobacionAsync(ventaPendiente.DetalleVentaPendientes);

            ventaPendiente.IdEstado                  = 2;
            ventaPendiente.IdUsuarioAutoriza         = idUsuarioAutoriza;
            ventaPendiente.FechaAutorizacion         = DateTime.Now;
            ventaPendiente.ObservacionesAutorizacion = dto.Observaciones;
            ventaPendiente.IdVentaGenerada           = venta.IdVenta;

            await _pendingSaleRepository.UpdateAsync(ventaPendiente);

            _logger.LogInformation("Venta pendiente {codigo} aprobada. Venta generada: {ventaGenerada}",
                ventaPendiente.CodigoVenta, venta.CodigoVenta);

            return new AuthorizeSaleResponseDTO
            {
                IdVentaPendiente    = ventaPendiente.IdVentaPendiente,
                CodigoVenta         = ventaPendiente.CodigoVenta,
                Aprobada            = true,
                IdVentaGenerada     = venta.IdVenta,
                CodigoVentaGenerada = venta.CodigoVenta,
                Mensaje             = $"Venta aprobada y procesada exitosamente. Se generó la venta {venta.CodigoVenta}."
            };
        }

        private async Task<AuthorizeSaleResponseDTO> RechazarVentaAsync(
            VentaPendiente ventaPendiente, AuthorizeSaleDTO dto, int idUsuarioAutoriza)
        {
            _logger.LogInformation("Rechazando venta pendiente {codigo} por usuario {usuario}",
                ventaPendiente.CodigoVenta, idUsuarioAutoriza);

            ventaPendiente.IdEstado                  = 3;
            ventaPendiente.IdUsuarioAutoriza         = idUsuarioAutoriza;
            ventaPendiente.FechaAutorizacion         = DateTime.Now;
            ventaPendiente.ObservacionesAutorizacion = dto.Observaciones ?? "Rechazada por administrador";

            await _pendingSaleRepository.UpdateAsync(ventaPendiente);

            _logger.LogInformation("Venta pendiente {codigo} rechazada", ventaPendiente.CodigoVenta);

            return new AuthorizeSaleResponseDTO
            {
                IdVentaPendiente    = ventaPendiente.IdVentaPendiente,
                CodigoVenta         = ventaPendiente.CodigoVenta,
                Aprobada            = false,
                IdVentaGenerada     = null,
                CodigoVentaGenerada = null,
                Mensaje             = "Venta rechazada. El vendedor será notificado."
            };
        }

        public async Task<Result<AuthorizeSaleResponseDTO>> AuthorizeSaleAsync(
            AuthorizeSaleDTO dto,
            int idUsuarioAutoriza)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                await _context.Database.SetAuditContextAsync(idUsuarioAutoriza);

                var ventaPendiente = await _pendingSaleRepository.GetByIdAsync(dto.IdVentaPendiente);

                (bool flowControl, Result<AuthorizeSaleResponseDTO> value) = ValidateAuthorizeSale(ventaPendiente);
                
                if (!flowControl) return value;

                var responseDto = dto.Aprobar
                    ? await AprobarVentaAsync(ventaPendiente!, dto, idUsuarioAutoriza)
                    : await RechazarVentaAsync(ventaPendiente!, dto, idUsuarioAutoriza);

                await transaction.CommitAsync();

                return Result<AuthorizeSaleResponseDTO>.Success(responseDto);
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
