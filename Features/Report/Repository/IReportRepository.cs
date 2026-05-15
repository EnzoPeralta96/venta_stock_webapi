using proyecto_venta_stock.Report.DTO;

namespace proyecto_venta_stock.Report.Repository
{
    public interface IReportRepository
    {
        Task<TotalVendidoDTO> GetTotalVendidoAsync(DateTime fechaDesde, DateTime fechaHasta);
        Task<List<VentasPorPeriodoItemDTO>> GetVentasPorPeriodoAsync(DateTime fechaDesde, DateTime fechaHasta, string agrupacion);
        Task<List<ProductoVendidoDTO>> GetProductosMasVendidosAsync(DateTime fechaDesde, DateTime fechaHasta, int topN, int? idCategoria = null);
        Task<List<CategoriaVendidaDTO>> GetCategoriasMasVendidasAsync(DateTime fechaDesde, DateTime fechaHasta);
        Task<MargenUtilidadDTO> GetMargenUtilidadAsync(DateTime fechaDesde, DateTime fechaHasta);
        Task<List<ClienteFrecuenteDTO>> GetClientesMasFrecuentesAsync(DateTime fechaDesde, DateTime fechaHasta, int topN);
        Task<TiempoCobroDTO> GetTiempoPromedioCobroAsync(DateTime fechaDesde, DateTime fechaHasta);
        Task<decimal> GetMontoTotalAdeudadoAsync();
        Task<List<ClienteSaldoDeudorDTO>> GetClientesSaldoDeudorAsync();
    }
}
