using proyecto_venta_stock.Models;

namespace venta_stock_webapi.Sale.Repository;

public interface ICreditNoteReasonRepository
{
    Task<MotivoNotaCredito?> GetByIdAsync(int idMotivo);
    Task<List<MotivoNotaCredito>> GetAllAsync(bool? activo = null);
    Task CreateAsync(MotivoNotaCredito motivo);
    Task UpdateAsync(MotivoNotaCredito motivo);
    Task ToggleStateAsync(int idMotivo, bool activo);
    Task<bool> ExistsByNameAsync(string nombre);
    Task<bool> ExistsByNameAsync(int id, string nombre);
}
