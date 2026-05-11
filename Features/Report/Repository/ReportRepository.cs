using Microsoft.EntityFrameworkCore;
using proyecto_venta_stock.Data;
using proyecto_venta_stock.Report.DTO;

namespace proyecto_venta_stock.Report.Repository
{
    public class ReportRepository : IReportRepository
    {
        private readonly VentaStockContext _dbContext;
        private const int EstadoAprobado = 2;
        private const int TipoPagoFactura = 8;

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
        // 3 & 4. Productos más vendidos (top N, con filtro opcional de categoría)
        // ──────────────────────────────────────────────
        public async Task<List<ProductoVendidoDTO>> GetProductosMasVendidosAsync(
            DateTime fechaDesde, DateTime fechaHasta, int topN, int? idCategoria = null)
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
                   && (idCategoria == null || p.IdCategoria == idCategoria)
                group new { d, cat } by new
                {
                    p.IdProducto,
                    NombreProducto  = p.Nombre,
                    Categoria       = cat != null ? cat.Categoria : null,
                    p.IdUnidadMedida
                } into g
                select new ProductoVendidoDTO
                {
                    IdProducto      = g.Key.IdProducto,
                    NombreProducto  = g.Key.NombreProducto,
                    Categoria       = g.Key.Categoria ?? "Sin categoría",
                    IdUnidadMedida  = g.Key.IdUnidadMedida,
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
        // 6. Margen de utilidad bruta
        // ──────────────────────────────────────────────
        public async Task<MargenUtilidadDTO> GetMargenUtilidadAsync(DateTime fechaDesde, DateTime fechaHasta)
        {
            // Materializar líneas de venta aprobadas del período
            var lineasVenta = await (
                from d in _dbContext.DetalleVenta.AsNoTracking()
                join v in _dbContext.Venta on d.IdVenta equals v.IdVenta
                where v.IdEstado == EstadoAprobado
                   && v.Fecha >= fechaDesde
                   && v.Fecha <= fechaHasta
                select new
                {
                    d.IdProducto,
                    Cantidad   = d.Cantidad ?? 0,
                    SubTotal   = d.SubTotal ?? 0m,
                    VentaFecha = v.Fecha!.Value
                }
            ).ToListAsync();

            if (!lineasVenta.Any())
                return new MargenUtilidadDTO { FechaDesde = fechaDesde, FechaHasta = fechaHasta };

            // Cargar historial de compras y costo actual para los productos involucrados
            var productIds = lineasVenta.Select(x => x.IdProducto).Distinct().ToList();

            var compras = await (
                from cpd in _dbContext.ComprasProveedorDetalle.AsNoTracking()
                join cp  in _dbContext.ComprasProveedor on cpd.IdCompraProveedor equals cp.IdCompraProveedor
                where productIds.Contains(cpd.IdProducto) && cp.Activo
                select new
                {
                    cpd.IdProducto,
                    cpd.PrecioUnitario,
                    cp.Fecha
                }
            ).ToListAsync();

            // Costo actual del producto como fallback si no hay historial de compras
            var costoActual = await _dbContext.Productos
                .AsNoTracking()
                .Where(p => productIds.Contains(p.IdProducto))
                .Select(p => new { p.IdProducto, p.Costo })
                .ToDictionaryAsync(p => p.IdProducto, p => p.Costo);

            // Para cada línea de venta, buscar el último costo de compra anterior o igual a la fecha de la venta.
            // Si no hay historial de compra, se usa Producto.Costo como fallback.
            decimal totalCostos = 0;
            foreach (var linea in lineasVenta)
            {
                var fechaVenta  = DateOnly.FromDateTime(linea.VentaFecha);
                var ultimoCosto = compras
                    .Where(c => c.IdProducto == linea.IdProducto && c.Fecha <= fechaVenta)
                    .OrderByDescending(c => c.Fecha)
                    .FirstOrDefault()?.PrecioUnitario
                    ?? costoActual.GetValueOrDefault(linea.IdProducto, 0m);

                totalCostos += linea.Cantidad * ultimoCosto;
            }

            var totalVentas   = lineasVenta.Sum(x => x.SubTotal);
            var utilidadBruta = totalVentas - totalCostos;
            var margenPct     = totalVentas > 0
                ? Math.Round(utilidadBruta / totalVentas * 100, 2)
                : 0m;

            return new MargenUtilidadDTO
            {
                TotalVentas      = totalVentas,
                TotalCostos      = totalCostos,
                UtilidadBruta    = utilidadBruta,
                MargenPorcentaje = margenPct,
                FechaDesde       = fechaDesde,
                FechaHasta       = fechaHasta
            };
        }

        // ──────────────────────────────────────────────
        // 7. Clientes más frecuentes
        // ──────────────────────────────────────────────
        public async Task<List<ClienteFrecuenteDTO>> GetClientesMasFrecuentesAsync(
            DateTime fechaDesde, DateTime fechaHasta, int topN)
        {
            var rows = await (
                from v in _dbContext.Venta.AsNoTracking()
                join c in _dbContext.Clientes on v.IdCliente equals c.IdCliente
                where v.IdEstado == EstadoAprobado
                   && v.Fecha >= fechaDesde
                   && v.Fecha <= fechaHasta
                select new
                {
                    c.IdCliente,
                    c.Nombre,
                    c.Apellido,
                    c.RazonSocial,
                    VentaTotal = v.Total ?? 0m
                }
            ).ToListAsync();

            return rows
                .GroupBy(x => x.IdCliente)
                .Select(g =>
                {
                    var first = g.First();
                    return new ClienteFrecuenteDTO
                    {
                        IdCliente      = first.IdCliente,
                        NombreCliente  = first.RazonSocial ?? $"{first.Nombre} {first.Apellido}".Trim(),
                        CantidadVentas = g.Count(),
                        TotalComprado  = g.Sum(x => x.VentaTotal)
                    };
                })
                .OrderByDescending(x => x.CantidadVentas)
                .Take(topN)
                .ToList();
        }

        // ──────────────────────────────────────────────
        // 8. Tiempo promedio de cobro de CC
        // ──────────────────────────────────────────────
        public async Task<TiempoCobroDTO> GetTiempoPromedioCobroAsync(
            DateTime fechaDesde, DateTime fechaHasta)
        {
            // Cruzar PAGO_FACTURA (tipo 8, no anulado) con su Venta para obtener la diferencia de fechas
            var pares = await (
                from pago in _dbContext.MovimientoCcs.AsNoTracking()
                join v    in _dbContext.Venta on pago.IdVenta equals v.IdVenta
                where pago.IdTipoMovimiento == TipoPagoFactura
                   && !pago.EsAnulado
                   && pago.IdVenta != null
                   && v.Fecha >= fechaDesde
                   && v.Fecha <= fechaHasta
                select new
                {
                    VentaFecha = v.Fecha,
                    PagoFecha  = pago.Fecha
                }
            ).ToListAsync();

            var paresValidos = pares
                .Where(x => x.VentaFecha.HasValue && x.PagoFecha.HasValue)
                .ToList();

            if (!paresValidos.Any())
                return new TiempoCobroDTO { PromedioDiasCobro = 0, CantidadFacturasPagas = 0 };

            var promedio = paresValidos
                .Select(x => (x.PagoFecha!.Value - x.VentaFecha!.Value).TotalDays)
                .Average();

            return new TiempoCobroDTO
            {
                PromedioDiasCobro     = Math.Round(promedio, 1),
                CantidadFacturasPagas = paresValidos.Count
            };
        }

        // ──────────────────────────────────────────────
        // 9. Monto total adeudado (KPI global)
        // ──────────────────────────────────────────────
        public async Task<decimal> GetMontoTotalAdeudadoAsync()
        {
            // Obtener el id del último movimiento por cliente
            var maxIds = await _dbContext.MovimientoCcs
                .AsNoTracking()
                .GroupBy(m => m.IdCliente)
                .Select(g => g.Max(x => x.IdMovimiento))
                .ToListAsync();

            // Sumar SaldoActual de esos movimientos donde la deuda es positiva
            return await _dbContext.MovimientoCcs
                .AsNoTracking()
                .Where(m => maxIds.Contains(m.IdMovimiento) && m.SaldoActual > 0)
                .SumAsync(m => m.SaldoActual ?? 0);
        }

        // ──────────────────────────────────────────────
        // 10. Clientes con saldo deudor (lista detallada)
        // ──────────────────────────────────────────────
        public async Task<List<ClienteSaldoDeudorDTO>> GetClientesSaldoDeudorAsync()
        {
            var maxIds = await _dbContext.MovimientoCcs
                .AsNoTracking()
                .GroupBy(m => m.IdCliente)
                .Select(g => g.Max(x => x.IdMovimiento))
                .ToListAsync();

            var rows = await (
                from m in _dbContext.MovimientoCcs.AsNoTracking()
                join c in _dbContext.Clientes on m.IdCliente equals c.IdCliente
                where maxIds.Contains(m.IdMovimiento) && m.SaldoActual > 0
                select new
                {
                    c.IdCliente,
                    c.Nombre,
                    c.Apellido,
                    c.RazonSocial,
                    SaldoDeudor      = m.SaldoActual ?? 0m,
                    LimiteDisponible = m.LimiteCuenta ?? 0m
                }
            ).ToListAsync();

            return rows
                .Select(x => new ClienteSaldoDeudorDTO
                {
                    IdCliente        = x.IdCliente,
                    NombreCliente    = x.RazonSocial ?? $"{x.Nombre} {x.Apellido}".Trim(),
                    SaldoDeudor      = x.SaldoDeudor,
                    LimiteDisponible = x.LimiteDisponible
                })
                .OrderByDescending(x => x.SaldoDeudor)
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
