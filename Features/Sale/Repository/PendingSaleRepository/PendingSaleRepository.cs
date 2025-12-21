using Microsoft.EntityFrameworkCore;
using proyecto_venta_stock.Data;
using proyecto_venta_stock.Models;
using venta_stock_webapi.Sale.DTO;

namespace venta_stock_webapi.Sale.Repository
{
    public class PendingSaleRepository : IPendingSaleRepository
    {
        private readonly VentaStockContext _context;
        private readonly ILogger<PendingSaleRepository> _logger;

        public PendingSaleRepository(
            VentaStockContext context,
            ILogger<PendingSaleRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<VentaPendiente> CreatePendingSaleAsync(VentaPendiente ventaPendiente)
        {
            await _context.VentaPendiente.AddAsync(ventaPendiente);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation(
                "Venta pendiente creada: {codigo}, Monto: {monto}, Excedente: {excedente}",
                ventaPendiente.CodigoVenta, ventaPendiente.Total, ventaPendiente.Excedente
            );
            
            return ventaPendiente;
        }

        public async Task<VentaPendiente?> GetByIdAsync(int idVentaPendiente)
        {
            return await _context.VentaPendiente
                .Include(vp => vp.IdClienteNavigation)
                .Include(vp => vp.IdUsuarioVendedorNavigation)
                .Include(vp => vp.IdMedioPagoNavigation)
                .Include(vp => vp.IdEstadoNavigation)
                .Include(vp => vp.IdUsuarioAutorizaNavigation)
                .Include(vp => vp.DetalleVentaPendientes)
                    .ThenInclude(d => d.IdProductoNavigation)
                .FirstOrDefaultAsync(vp => vp.IdVentaPendiente == idVentaPendiente);
        }

        public async Task<List<PendingSaleListDTO>> GetPendingSalesAsync(int? idEstado = null)
        {
            var query = _context.VentaPendiente
                .Include(vp => vp.IdClienteNavigation)
                .Include(vp => vp.IdUsuarioVendedorNavigation)
                .Include(vp => vp.IdEstadoNavigation)
                .Include(vp => vp.DetalleVentaPendientes)
                .AsQueryable();

            // Filtrar por estado si se especifica
            if (idEstado.HasValue)
            {
                query = query.Where(vp => vp.IdEstado == idEstado.Value);
            }

            var result = await query
                .OrderByDescending(vp => vp.FechaRegistro)
                .Select(vp => new PendingSaleListDTO
                {
                    IdVentaPendiente = vp.IdVentaPendiente,
                    CodigoVenta = vp.CodigoVenta,
                    FechaRegistro = vp.FechaRegistro,
                    Total = vp.Total,
                    IdCliente = vp.IdCliente,
                    Cliente = !string.IsNullOrEmpty(vp.IdClienteNavigation.RazonSocial)
                        ? vp.IdClienteNavigation.RazonSocial
                        : (vp.IdClienteNavigation.Nombre + " " + vp.IdClienteNavigation.Apellido),
                    Vendedor = vp.IdUsuarioVendedorNavigation.Nombre + " " + vp.IdUsuarioVendedorNavigation.Apellido,
                    SaldoActual = vp.SaldoActual ?? 0,
                    LimiteCuenta = vp.LimiteCuenta ?? 0,
                    Excedente = vp.Excedente ?? 0,
                    PorcentajeExcedente = vp.LimiteCuenta > 0
                        ? ((vp.Excedente ?? 0) / (vp.LimiteCuenta ?? 1)) * 100
                        : 0,
                    Estado = vp.IdEstadoNavigation.Estado1 ?? "N/A",
                    CantidadItems = vp.DetalleVentaPendientes.Count
                })
                .ToListAsync();

            return result;
        }

        public async Task UpdateAsync(VentaPendiente ventaPendiente)
        {
            _context.VentaPendiente.Update(ventaPendiente);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation(
                "Venta pendiente actualizada: {codigo}, Estado: {estado}",
                ventaPendiente.CodigoVenta, ventaPendiente.IdEstado
            );
        }
    }
}