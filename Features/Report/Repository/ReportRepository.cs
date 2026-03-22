using Microsoft.EntityFrameworkCore;
using proyecto_venta_stock.Data;
using proyecto_venta_stock.Report.DTO;

namespace proyecto_venta_stock.Report.Repository
{
    public class ReportRepository : IReportRepository
    {
        private readonly VentaStockContext _dbContext;
        // IdEstado = 2 → Aprobada/Completada (ventas efectivamente realizadas)
        private const int EstadoAprobado = 2;

        public ReportRepository(VentaStockContext dbContext)
        {
            _dbContext = dbContext;
        }

        // ──────────────────────────────────────────────
        // 1. Total vendido en un período
        // ──────────────────────────────────────────────
        public async Task<TotalVendidoDTO> GetTotalVendidoAsync(DateTime fechaDesde, DateTime fechaHasta)
        {
            var query = _dbContext.Venta
                .AsNoTracking()
                .Where(v => v.IdEstado == EstadoAprobado
                         && v.Fecha >= fechaDesde
                         && v.Fecha <= fechaHasta);

            var total    = await query.SumAsync(v => v.Total ?? 0);
            var cantidad = await query.CountAsync();

            return new TotalVendidoDTO
            {
                TotalVendido   = total,
                CantidadVentas = cantidad,
                FechaDesde     = fechaDesde,
                FechaHasta     = fechaHasta
            };
        }

        // ──────────────────────────────────────────────
        // 2. Ventas agrupadas por período
        // ──────────────────────────────────────────────
        public async Task<List<VentasPorPeriodoItemDTO>> GetVentasPorPeriodoAsync(
            DateTime fechaDesde, DateTime fechaHasta, string agrupacion)
        {
            var baseQuery = _dbContext.Venta
                .AsNoTracking()
                .Where(v => v.IdEstado == EstadoAprobado
                         && v.Fecha >= fechaDesde
                         && v.Fecha <= fechaHasta);

            switch (agrupacion.ToLower())
            {
                case "dia":
                {
                    var raw = await baseQuery
                        .GroupBy(v => v.Fecha!.Value.Date)
                        .Select(g => new
                        {
                            Clave          = g.Key,
                            TotalVendido   = g.Sum(v => v.Total ?? 0),
                            CantidadVentas = g.Count()
                        })
                        .OrderBy(x => x.Clave)
                        .ToListAsync();

                    return raw.Select(x => new VentasPorPeriodoItemDTO
                    {
                        Periodo        = x.Clave.ToString("yyyy-MM-dd"),
                        TotalVendido   = x.TotalVendido,
                        CantidadVentas = x.CantidadVentas
                    }).ToList();
                }

                case "semana":
                {
                    // Materializa fechas y totales, luego agrupa por inicio de semana en memoria
                    var raw = await baseQuery
                        .Select(v => new { Fecha = v.Fecha!.Value.Date, Total = v.Total ?? 0 })
                        .ToListAsync();

                    return raw
                        .GroupBy(x => InicioSemana(x.Fecha))
                        .OrderBy(g => g.Key)
                        .Select(g => new VentasPorPeriodoItemDTO
                        {
                            Periodo        = $"Semana del {g.Key:dd/MM/yyyy}",
                            TotalVendido   = g.Sum(x => x.Total),
                            CantidadVentas = g.Count()
                        }).ToList();
                }

                default: // "mes"
                {
                    var raw = await baseQuery
                        .GroupBy(v => new { v.Fecha!.Value.Year, v.Fecha!.Value.Month })
                        .Select(g => new
                        {
                            g.Key.Year,
                            g.Key.Month,
                            TotalVendido   = g.Sum(v => v.Total ?? 0),
                            CantidadVentas = g.Count()
                        })
                        .OrderBy(x => x.Year)
                        .ThenBy(x => x.Month)
                        .ToListAsync();

                    return raw.Select(x => new VentasPorPeriodoItemDTO
                    {
                        Periodo        = $"{x.Year}-{x.Month:D2}",
                        TotalVendido   = x.TotalVendido,
                        CantidadVentas = x.CantidadVentas
                    }).ToList();
                }
            }
        }

        // ──────────────────────────────────────────────
        // 3 & 4. Productos más vendidos (top N)
        // ──────────────────────────────────────────────
        public async Task<List<ProductoVendidoDTO>> GetProductosMasVendidosAsync(
            DateTime fechaDesde, DateTime fechaHasta, int topN)
        {
            var query =
                from d   in _dbContext.DetalleVenta.AsNoTracking()
                join v   in _dbContext.Venta     on d.IdVenta    equals v.IdVenta
                join p   in _dbContext.Productos on d.IdProducto equals p.IdProducto
                join cat in _dbContext.Categoria on p.IdCategoria equals cat.IdCategoria into catLeft
                from cat in catLeft.DefaultIfEmpty()
                where v.IdEstado == EstadoAprobado
                   && v.Fecha >= fechaDesde
                   && v.Fecha <= fechaHasta
                group new { d, cat } by new
                {
                    p.IdProducto,
                    NombreProducto = p.Nombre,
                    Categoria      = cat != null ? cat.Categoria : null
                } into g
                select new ProductoVendidoDTO
                {
                    IdProducto      = g.Key.IdProducto,
                    NombreProducto  = g.Key.NombreProducto,
                    Categoria       = g.Key.Categoria ?? "Sin categoría",
                    CantidadVendida = g.Sum(x => x.d.Cantidad ?? 0),
                    TotalFacturado  = g.Sum(x => x.d.SubTotal ?? 0)
                };

            return await query
                .OrderByDescending(x => x.CantidadVendida)
                .Take(topN)
                .ToListAsync();
        }

        // ──────────────────────────────────────────────
        // 5. Ventas agrupadas por categoría
        // ──────────────────────────────────────────────
        public async Task<List<CategoriaVendidaDTO>> GetCategoriasMasVendidasAsync(
            DateTime fechaDesde, DateTime fechaHasta)
        {
            // Materializa primero para evitar problemas de traducción EF Core
            // con group by sobre resultados de left join (mismo patrón que agrupación por semana)
            var rows = await (
                from d   in _dbContext.DetalleVenta.AsNoTracking()
                join v   in _dbContext.Venta     on d.IdVenta    equals v.IdVenta
                join p   in _dbContext.Productos on d.IdProducto equals p.IdProducto
                join cat in _dbContext.Categoria on p.IdCategoria equals cat.IdCategoria into catLeft
                from cat in catLeft.DefaultIfEmpty()
                where v.IdEstado == EstadoAprobado
                   && v.Fecha >= fechaDesde
                   && v.Fecha <= fechaHasta
                select new
                {
                    Categoria = cat.Categoria ?? "Sin categoría",
                    Cantidad  = d.Cantidad ?? 0,
                    SubTotal  = d.SubTotal ?? 0
                }
            ).ToListAsync();

            return rows
                .GroupBy(x => x.Categoria)
                .Select(g => new CategoriaVendidaDTO
                {
                    Categoria       = g.Key,
                    CantidadVendida = g.Sum(x => x.Cantidad),
                    TotalFacturado  = g.Sum(x => x.SubTotal)
                })
                .OrderByDescending(x => x.TotalFacturado)
                .ToList();
        }

        // ──────────────────────────────────────────────
        // Helper
        // ──────────────────────────────────────────────
        private static DateTime InicioSemana(DateTime fecha)
        {
            int diff = (7 + (fecha.DayOfWeek - DayOfWeek.Monday)) % 7;
            return fecha.AddDays(-diff).Date;
        }
    }
}
