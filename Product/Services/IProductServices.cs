using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using proyecto_venta_stock.Shared.ResultPattern;
using proyecto_venta_stock.Product.DTO;
using venta_stock_webapi.Shared.Paged;

namespace proyecto_venta_stock.Product.Services
{
    public interface IProductServices
    {
        Task<Result<bool>> Create(ProductDTO productDTO);
        Task<Result<bool>> Update(ProductDTO productDTO);
        Task<Result<List<ProductDTO>>> GetAll();
        Task<Result<List<ProductDetailDTO>>> GetAllWithCategoryAndLocation(bool? activo = true);
        Task<Result<ProductDTO>> GetById(int idProducto);

        Task<Result<bool>> Delete(int idProducto);
        Task<Result<bool>> ToggleEstado(int idProducto);

        Task<Result<PagedList<ProductDetailDTO>>> GetAllWithCategoryAndLocationPaged(
    int pageIndex,
    int pageSize,
    bool? activo = true,
    string? search = null
);
        public Task<byte[]> ExportarCsvAsync();
        public Task<byte[]> ExportarExcelAsync();
    }
}