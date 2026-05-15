using proyecto_venta_stock.Models;

namespace venta_stock_webapi.CurrentAccount.Repository;

public interface IDebitNoteReasonRepository
{
    Task<MotivoNotaDebito?> GetByIdAsync(int idMotivo);
    Task<List<MotivoNotaDebito>> GetAllAsync(bool? activo = null, string? categoria = null);
    Task CreateAsync(MotivoNotaDebito motivo);
    Task<int> UpdateAsync(MotivoNotaDebito motivo);
    Task ToggleStateAsync(int idMotivo, bool activo);
    Task<bool> ExistsByNameAsync(string nombre);
    Task<bool> ExistsByNameAsync(int id, string nombre);
}
