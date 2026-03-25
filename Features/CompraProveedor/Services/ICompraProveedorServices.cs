using proyecto_venta_stock.CompraProveedor.DTO;
using proyecto_venta_stock.Shared.ResultPattern;
using venta_stock_webapi.Shared.Paged;

namespace proyecto_venta_stock.CompraProveedor.Services;

public interface ICompraProveedorServices
{
    Task<Result<CompraProveedorResponseDTO>> Create(CompraProveedorCreateDTO dto);
    Task<Result<PagedList<CompraProveedorDetailResponseDTO>>> GetPaged(int pageIndex, int pageSize, string? search, bool? activo, DateOnly? fechaDesde, DateOnly? fechaHasta);
    Task<Result<PagedList<CompraProveedorDetailResponseDTO>>> GetPagedByProveedor(int idProveedor, int pageIndex, int pageSize, bool? activo, DateOnly? fechaDesde, DateOnly? fechaHasta);
    Task<Result<CompraProveedorDetailResponseDTO>> GetById(int idCompraProveedor);
    Task<Result<bool>> Anular(int idCompraProveedor, AnulacionCompraDTO dto);
    Task<Result<byte[]>> ExportarExcelAsync(DateOnly? fechaDesde, DateOnly? fechaHasta);
    Task<Result<byte[]>> ExportarPdfAsync(DateOnly? fechaDesde, DateOnly? fechaHasta);
    Task<Result<byte[]>> ExportarCompraExcelAsync(int idCompraProveedor);
    Task<Result<byte[]>> ExportarCompraPdfAsync(int idCompraProveedor);
    Task<Result<byte[]>> ExportarComprasPorProveedorExcelAsync(int idProveedor, DateOnly? fechaDesde, DateOnly? fechaHasta);
    Task<Result<byte[]>> ExportarComprasPorProveedorPdfAsync(int idProveedor, DateOnly? fechaDesde, DateOnly? fechaHasta);
}
