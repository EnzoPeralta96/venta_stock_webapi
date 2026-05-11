using proyecto_venta_stock.Shared.ResultPattern;
using venta_stock_webapi.Sale.DTO;
using venta_stock_webapi.Shared.Paged;

namespace venta_stock_webapi.Sale.Services
{
    public interface ISaleServices
    {
        // Crear venta
        Task<Result<SaleResponseDTO>> CreateSaleAsync(CreateSaleDTO createSaleDTO, int idUsuarioVendedor);

        // Obtener venta por id
        Task<Result<SaleResponseDTO>> GetSaleByIdAsync(int idVenta);

        // Listar ventas con paginacion y filtros
        Task<Result<PagedList<SaleListDTO>>> GetSalesPagedAsync(int pageNumber, int pageSize, string? clienteFilter, DateTime? fechaDesde, DateTime? fechaHasta, string? estadoFilter, int? idCliente = null);

        // Anular una venta generando NC en CC si corresponde
        Task<Result<AnnulSaleResponseDTO>> AnnulSaleAsync(int idVenta, AnnulSaleDTO dto, int idUsuarioRegistra);

        // Exportaciones PDF/Excel — Listado general
        Task<Result<byte[]>> ExportSalesExcelAsync(DateOnly? fechaDesde, DateOnly? fechaHasta, string? estadoVenta);
        Task<Result<byte[]>> ExportSalesPdfAsync(DateOnly? fechaDesde, DateOnly? fechaHasta, string? estadoVenta);

        // Exportaciones PDF/Excel — Historial por cliente
        Task<Result<byte[]>> ExportClientSalesExcelAsync(int idCliente, DateOnly? fechaDesde, DateOnly? fechaHasta, string? estadoVenta);
        Task<Result<byte[]>> ExportClientSalesPdfAsync(int idCliente, DateOnly? fechaDesde, DateOnly? fechaHasta, string? estadoVenta);

        // Exportación Excel — Comprobante individual
        Task<Result<byte[]>> ExportSaleTicketExcelAsync(int idVenta);
    }
}