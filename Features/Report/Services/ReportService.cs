using proyecto_venta_stock.Models;
using proyecto_venta_stock.Report.DTO;
using proyecto_venta_stock.Report.Message;
using proyecto_venta_stock.Report.Repository;
using proyecto_venta_stock.Shared.ResultPattern;
using venta_stock_webapi.Features.Audit.Repository;
using venta_stock_webapi.Shared.Identity;

namespace proyecto_venta_stock.Report.Services
{
    public class ReportService : IReportService
    {
        private readonly IReportRepository   _reportRepository;
        private readonly IAuditRepository    _auditRepository;
        private readonly IUserContext        _userContext;
        private readonly ILogger<ReportService> _logger;

        private static readonly HashSet<string> AgrupacionesValidas =
            new(StringComparer.OrdinalIgnoreCase) { "dia", "semana", "mes" };

        // Normaliza fechaHasta al último instante del día para incluir todas las ventas de esa fecha
        private static DateTime FinDelDia(DateTime fecha) => fecha.Date.AddDays(1).AddTicks(-1);

        public ReportService(
            IReportRepository reportRepository,
            IAuditRepository auditRepository,
            IUserContext userContext,
            ILogger<ReportService> logger)
        {
            _reportRepository = reportRepository;
            _auditRepository  = auditRepository;
            _userContext      = userContext;
            _logger           = logger;
        }

        // ──────────────────────────────────────────────
        // 1. Total vendido en un período
        // ──────────────────────────────────────────────
        public async Task<Result<TotalVendidoDTO>> GetTotalVendidoAsync(DateTime fechaDesde, DateTime fechaHasta)
        {
            if (fechaDesde.Date > fechaHasta.Date)
                return Result<TotalVendidoDTO>.Failure(ReportErrorCode.fecha_invalida);

            fechaHasta = FinDelDia(fechaHasta);

            try
            {
                var dto = await _reportRepository.GetTotalVendidoAsync(fechaDesde, fechaHasta);

                await LogAsync("CONSULTA_REPORTE", "Reporte",
                    $"Total vendido: desde {fechaDesde:yyyy-MM-dd} hasta {fechaHasta:yyyy-MM-dd}. " +
                    $"Resultado: ${dto.TotalVendido:N2} en {dto.CantidadVentas} ventas.");

                return Result<TotalVendidoDTO>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar reporte de total vendido.");
                return Result<TotalVendidoDTO>.Failure(ReportErrorCode.unexpected_error);
            }
        }

        // ──────────────────────────────────────────────
        // 2. Ventas agrupadas por período
        // ──────────────────────────────────────────────
        public async Task<Result<List<VentasPorPeriodoItemDTO>>> GetVentasPorPeriodoAsync(
            DateTime fechaDesde, DateTime fechaHasta, string agrupacion)
        {
            if (fechaDesde.Date > fechaHasta.Date)
                return Result<List<VentasPorPeriodoItemDTO>>.Failure(ReportErrorCode.fecha_invalida);

            if (!AgrupacionesValidas.Contains(agrupacion))
                return Result<List<VentasPorPeriodoItemDTO>>.Failure(ReportErrorCode.agrupacion_invalida);

            fechaHasta = FinDelDia(fechaHasta);

            try
            {
                var items = await _reportRepository.GetVentasPorPeriodoAsync(fechaDesde, fechaHasta, agrupacion);

                await LogAsync("CONSULTA_REPORTE", "Reporte",
                    $"Ventas por período ({agrupacion}): desde {fechaDesde:yyyy-MM-dd} hasta {fechaHasta:yyyy-MM-dd}. " +
                    $"{items.Count} períodos devueltos.");

                return Result<List<VentasPorPeriodoItemDTO>>.Success(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar reporte de ventas por período.");
                return Result<List<VentasPorPeriodoItemDTO>>.Failure(ReportErrorCode.unexpected_error);
            }
        }

        // ──────────────────────────────────────────────
        // 3. Artículo más vendido
        // ──────────────────────────────────────────────
        public async Task<Result<ProductoVendidoDTO>> GetArticuloMasVendidoAsync(
            DateTime fechaDesde, DateTime fechaHasta)
        {
            if (fechaDesde.Date > fechaHasta.Date)
                return Result<ProductoVendidoDTO>.Failure(ReportErrorCode.fecha_invalida);

            fechaHasta = FinDelDia(fechaHasta);

            try
            {
                var items = await _reportRepository.GetProductosMasVendidosAsync(fechaDesde, fechaHasta, topN: 1);

                if (items.Count == 0)
                    return Result<ProductoVendidoDTO>.Failure(ReportErrorCode.sin_datos);

                var top = items[0];

                await LogAsync("CONSULTA_REPORTE", "Reporte",
                    $"Artículo más vendido: desde {fechaDesde:yyyy-MM-dd} hasta {fechaHasta:yyyy-MM-dd}. " +
                    $"Resultado: {top.NombreProducto} ({top.CantidadVendida} unidades).");

                return Result<ProductoVendidoDTO>.Success(top);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar reporte de artículo más vendido.");
                return Result<ProductoVendidoDTO>.Failure(ReportErrorCode.unexpected_error);
            }
        }

        // ──────────────────────────────────────────────
        // 4. Productos más vendidos (top N)
        // ──────────────────────────────────────────────
        public async Task<Result<List<ProductoVendidoDTO>>> GetProductosMasVendidosAsync(
            DateTime fechaDesde, DateTime fechaHasta, int topN)
        {
            if (fechaDesde.Date > fechaHasta.Date)
                return Result<List<ProductoVendidoDTO>>.Failure(ReportErrorCode.fecha_invalida);

            fechaHasta = FinDelDia(fechaHasta);
            if (topN <= 0) topN = 10;

            try
            {
                var items = await _reportRepository.GetProductosMasVendidosAsync(fechaDesde, fechaHasta, topN);

                await LogAsync("CONSULTA_REPORTE", "Reporte",
                    $"Productos más vendidos (top {topN}): desde {fechaDesde:yyyy-MM-dd} hasta {fechaHasta:yyyy-MM-dd}. " +
                    $"{items.Count} productos devueltos.");

                return Result<List<ProductoVendidoDTO>>.Success(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar reporte de productos más vendidos.");
                return Result<List<ProductoVendidoDTO>>.Failure(ReportErrorCode.unexpected_error);
            }
        }

        // ──────────────────────────────────────────────
        // 5. Ventas por categoría (para donut chart)
        // ──────────────────────────────────────────────
        public async Task<Result<List<CategoriaVendidaDTO>>> GetCategoriasMasVendidasAsync(
            DateTime fechaDesde, DateTime fechaHasta)
        {
            if (fechaDesde.Date > fechaHasta.Date)
                return Result<List<CategoriaVendidaDTO>>.Failure(ReportErrorCode.fecha_invalida);

            fechaHasta = FinDelDia(fechaHasta);

            try
            {
                var items = await _reportRepository.GetCategoriasMasVendidasAsync(fechaDesde, fechaHasta);

                await LogAsync("CONSULTA_REPORTE", "Reporte",
                    $"Categorías más vendidas: desde {fechaDesde:yyyy-MM-dd} hasta {fechaHasta:yyyy-MM-dd}. " +
                    $"{items.Count} categorías devueltas.");

                return Result<List<CategoriaVendidaDTO>>.Success(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar reporte de categorías más vendidas.");
                return Result<List<CategoriaVendidaDTO>>.Failure(ReportErrorCode.unexpected_error);
            }
        }

        // ──────────────────────────────────────────────
        // Helper: escribe entrada de auditoría
        // ──────────────────────────────────────────────
        private async Task LogAsync(string accion, string entidadTipo, string detalle)
        {
            try
            {
                await _auditRepository.LogAsync(new Auditoria
                {
                    FechaHora    = DateTimeOffset.UtcNow,
                    IdUsuario    = _userContext.UserId,
                    UsuarioNombre = _userContext.UserName,
                    Accion       = accion,
                    EntidadTipo  = entidadTipo,
                    Detalle      = detalle
                });
            }
            catch (Exception ex)
            {
                // La auditoría no debe romper el flujo principal
                _logger.LogWarning(ex, "No se pudo registrar la auditoría del reporte.");
            }
        }
    }
}
