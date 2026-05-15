using proyecto_venta_stock.Report.DTO;
using proyecto_venta_stock.Shared.ResultPattern;

namespace proyecto_venta_stock.Report.Services
{
    public interface IReportService
    {
        Task<Result<TotalVendidoDTO>> GetTotalVendidoAsync(DateTime fechaDesde, DateTime fechaHasta);
        Task<Result<List<VentasPorPeriodoItemDTO>>> GetVentasPorPeriodoAsync(DateTime fechaDesde, DateTime fechaHasta, string agrupacion);
        Task<Result<ProductoVendidoDTO>> GetArticuloMasVendidoAsync(DateTime fechaDesde, DateTime fechaHasta);
        Task<Result<List<ProductoVendidoDTO>>> GetProductosMasVendidosAsync(DateTime fechaDesde, DateTime fechaHasta, int topN, int? idCategoria = null);
        Task<Result<List<CategoriaVendidaDTO>>> GetCategoriasMasVendidasAsync(DateTime fechaDesde, DateTime fechaHasta);
        Task<Result<MargenUtilidadDTO>> GetMargenUtilidadAsync(DateTime fechaDesde, DateTime fechaHasta);
        Task<Result<List<ClienteFrecuenteDTO>>> GetClientesMasFrecuentesAsync(DateTime fechaDesde, DateTime fechaHasta, int topN);
        Task<Result<TiempoCobroDTO>> GetTiempoPromedioCobroAsync(DateTime fechaDesde, DateTime fechaHasta);
        Task<Result<decimal>> GetMontoTotalAdeudadoAsync();
        Task<Result<List<ClienteSaldoDeudorDTO>>> GetClientesSaldoDeudorAsync();
    }
}
