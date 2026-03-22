using proyecto_venta_stock.Shared.ResultPattern;
using venta_stock_webapi.Sale.DTO;
using venta_stock_webapi.Shared.Paged;

namespace venta_stock_webapi.Sale.Services
{
    public interface ISaleServices
    {
        // Crear venta
        Task<Result<SaleResponseDTO>> CreateSaleAsync(CreateSaleDTO createSaleDTO);

        // Obtener venta por id
        Task<Result<SaleResponseDTO>> GetSaleByIdAsync(int idVenta);

        // Listar ventas con paginacion y filtros
        Task<Result<PagedList<SaleListDTO>>> GetSalesPagedAsync(int pageNumber, int pageSize, string? clienteFilter, DateTime? fechaDesde, DateTime? fechaHasta, string? estadoFilter);

        // Anular una venta generando NC en CC si corresponde
        Task<Result<AnnulSaleResponseDTO>> AnnulSaleAsync(int idVenta, AnnulSaleDTO dto);
    }
}