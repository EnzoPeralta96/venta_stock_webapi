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
        private readonly IReportRepository      _reportRepository;
        private readonly IAuditRepository       _auditRepository;
        private readonly IUserContext           _userContext;
        private readonly ILogger<ReportService> _logger;

        private static readonly HashSet<string> AgrupacionesValidas =
            new(StringComparer.OrdinalIgnoreCase) { "dia", "semana", "mes" };

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
        // 4. Productos más vendidos (top N, categoría opcional)
        // ──────────────────────────────────────────────
        public async Task<Result<List<ProductoVendidoDTO>>> GetProductosMasVendidosAsync(
            DateTime fechaDesde, DateTime fechaHasta, int topN, int? idCategoria = null)
        {
            if (fechaDesde.Date > fechaHasta.Date)
                return Result<List<ProductoVendidoDTO>>.Failure(ReportErrorCode.fecha_invalida);

            fechaHasta = FinDelDia(fechaHasta);
            if (topN <= 0) topN = 10;

            try
            {
                var items = await _reportRepository.GetProductosMasVendidosAsync(fechaDesde, fechaHasta, topN, idCategoria);

                await LogAsync("CONSULTA_REPORTE", "Reporte",
                    $"Productos más vendidos (top {topN}): desde {fechaDesde:yyyy-MM-dd} hasta {fechaHasta:yyyy-MM-dd}. " +
                    $"{(idCategoria.HasValue ? $"Categoría: {idCategoria}. " : "")}{items.Count} productos devueltos.");

                return Result<List<ProductoVendidoDTO>>.Success(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar reporte de productos más vendidos.");
                return Result<List<ProductoVendidoDTO>>.Failure(ReportErrorCode.unexpected_error);
            }
        }

        // ──────────────────────────────────────────────
        // 5. Ventas por categoría
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
        // 6. Margen de utilidad bruta
        // ──────────────────────────────────────────────
        public async Task<Result<MargenUtilidadDTO>> GetMargenUtilidadAsync(
            DateTime fechaDesde, DateTime fechaHasta)
        {
            if (fechaDesde.Date > fechaHasta.Date)
                return Result<MargenUtilidadDTO>.Failure(ReportErrorCode.fecha_invalida);

            fechaHasta = FinDelDia(fechaHasta);

            try
            {
                var dto = await _reportRepository.GetMargenUtilidadAsync(fechaDesde, fechaHasta);

                await LogAsync("CONSULTA_REPORTE", "Reporte",
                    $"Margen de utilidad: desde {fechaDesde:yyyy-MM-dd} hasta {fechaHasta:yyyy-MM-dd}. " +
                    $"Ventas: ${dto.TotalVentas:N2}, Utilidad: ${dto.UtilidadBruta:N2}, Margen: {dto.MargenPorcentaje:N2}%.");

                return Result<MargenUtilidadDTO>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar reporte de margen de utilidad.");
                return Result<MargenUtilidadDTO>.Failure(ReportErrorCode.unexpected_error);
            }
        }

        // ──────────────────────────────────────────────
        // 7. Clientes más frecuentes
        // ──────────────────────────────────────────────
        public async Task<Result<List<ClienteFrecuenteDTO>>> GetClientesMasFrecuentesAsync(
            DateTime fechaDesde, DateTime fechaHasta, int topN)
        {
            if (fechaDesde.Date > fechaHasta.Date)
                return Result<List<ClienteFrecuenteDTO>>.Failure(ReportErrorCode.fecha_invalida);

            fechaHasta = FinDelDia(fechaHasta);
            if (topN <= 0) topN = 10;

            try
            {
                var items = await _reportRepository.GetClientesMasFrecuentesAsync(fechaDesde, fechaHasta, topN);

                await LogAsync("CONSULTA_REPORTE", "Reporte",
                    $"Clientes más frecuentes (top {topN}): desde {fechaDesde:yyyy-MM-dd} hasta {fechaHasta:yyyy-MM-dd}. " +
                    $"{items.Count} clientes devueltos.");

                return Result<List<ClienteFrecuenteDTO>>.Success(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar reporte de clientes más frecuentes.");
                return Result<List<ClienteFrecuenteDTO>>.Failure(ReportErrorCode.unexpected_error);
            }
        }

        // ──────────────────────────────────────────────
        // 8. Tiempo promedio de cobro de CC
        // ──────────────────────────────────────────────
        public async Task<Result<TiempoCobroDTO>> GetTiempoPromedioCobroAsync(
            DateTime fechaDesde, DateTime fechaHasta)
        {
            if (fechaDesde.Date > fechaHasta.Date)
                return Result<TiempoCobroDTO>.Failure(ReportErrorCode.fecha_invalida);

            fechaHasta = FinDelDia(fechaHasta);

            try
            {
                var dto = await _reportRepository.GetTiempoPromedioCobroAsync(fechaDesde, fechaHasta);

                await LogAsync("CONSULTA_REPORTE", "Reporte",
                    $"Tiempo promedio de cobro CC: desde {fechaDesde:yyyy-MM-dd} hasta {fechaHasta:yyyy-MM-dd}. " +
                    $"Promedio: {dto.PromedioDiasCobro} días sobre {dto.CantidadFacturasPagas} facturas.");

                return Result<TiempoCobroDTO>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar reporte de tiempo promedio de cobro.");
                return Result<TiempoCobroDTO>.Failure(ReportErrorCode.unexpected_error);
            }
        }

        // ──────────────────────────────────────────────
        // 9. Monto total adeudado (KPI global)
        // ──────────────────────────────────────────────
        public async Task<Result<decimal>> GetMontoTotalAdeudadoAsync()
        {
            try
            {
                var total = await _reportRepository.GetMontoTotalAdeudadoAsync();

                await LogAsync("CONSULTA_REPORTE", "Reporte",
                    $"Monto total adeudado consultado. Total: ${total:N2}.");

                return Result<decimal>.Success(total);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar reporte de monto total adeudado.");
                return Result<decimal>.Failure(ReportErrorCode.unexpected_error);
            }
        }

        // ──────────────────────────────────────────────
        // 10. Clientes con saldo deudor (lista detallada)
        // ──────────────────────────────────────────────
        public async Task<Result<List<ClienteSaldoDeudorDTO>>> GetClientesSaldoDeudorAsync()
        {
            try
            {
                var items = await _reportRepository.GetClientesSaldoDeudorAsync();

                await LogAsync("CONSULTA_REPORTE", "Reporte",
                    $"Clientes con saldo deudor consultados. {items.Count} clientes con deuda activa.");

                return Result<List<ClienteSaldoDeudorDTO>>.Success(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar reporte de clientes con saldo deudor.");
                return Result<List<ClienteSaldoDeudorDTO>>.Failure(ReportErrorCode.unexpected_error);
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
                    FechaHora     = DateTimeOffset.UtcNow,
                    IdUsuario     = _userContext.UserId,
                    UsuarioNombre = _userContext.UserName,
                    Accion        = accion,
                    EntidadTipo   = entidadTipo,
                    Detalle       = detalle
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo registrar la auditoría del reporte.");
            }
        }
    }
}
