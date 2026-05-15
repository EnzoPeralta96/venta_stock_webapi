using proyecto_venta_stock.Models;

namespace venta_stock_webapi.CurrentAccount.Repository;

public interface IInterestConfigRepository
{
    Task<ConfiguracionInteres?> GetByIdAsync(int idConfig);
    Task<List<ConfiguracionInteres>> GetAllAsync();
    Task<ConfiguracionInteres?> GetCurrentAsync();
    Task CreateAsync(ConfiguracionInteres config);
    Task UpdateAsync(ConfiguracionInteres config);
    Task SetAsCurrentAsync(int idConfig);
    Task<bool> ExistsByNameAsync(string nombre);
    Task<bool> ExistsByNameAsync(int id, string nombre);
}
