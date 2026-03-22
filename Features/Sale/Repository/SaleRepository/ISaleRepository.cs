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

        // Obtiene la venta con detalle y tracking para poder modificarla.
        Task<Ventum?> GetSaleWithDetailsAsync(int idVenta);

        // Restituye el stock de un producto sumando la cantidad indicada.
        Task RestoreProductStockAsync(int idProducto, int quantity);

        // Actualiza el estado de una venta.
        Task UpdateSaleStateAsync(int idVenta, int idEstado);

        // Actualiza el estado, guarda el motivo NC y el detalle adicional al anular una venta.
        Task AnnulSaleInDbAsync(int idVenta, int idEstado, int idMotivoNc, string? detalleNc);
    }
}