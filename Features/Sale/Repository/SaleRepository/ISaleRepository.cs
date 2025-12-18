using proyecto_venta_stock.Models;

namespace venta_stock_webapi.Sale.Repository
{
    public interface ISaleRepository
    {
        // Crear venta
        Task<Ventum?> CreateSaleAsync(Ventum venta);
        // Agregar item al detalle
        Task AddSaleItemsAsync(List<DetalleVentum> items);
        //Obtener venta por id
        Task<Ventum?> GetSaleByIdAsync(int idVenta);
        // Listas ventas con paginacion
        IQueryable<Ventum> SalesQueryable();

        //Generar codigo de venta unico
        Task<string> GenerateSaleCodeAsync();

        // Actualizar stock de producto
        Task UpdateProductStockAsync(int idProducto, int quantitySold);
    }
}