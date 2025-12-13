using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using proyecto_venta_stock.Models;

namespace venta_stock_webapi.CurrentAccount.Repository
{
    public interface IAccountConfigRepository
    {
        Task<ConfiguracionCc?> GetAccountConfigByIdAsync(int configId);
        Task<List<ConfiguracionCc>> GetAccountConfigsAsync(bool? activo = null);
        Task CreateAccountConfigAsync(ConfiguracionCc config);
        Task<int> UpdateAccountConfigAsync(ConfiguracionCc config);
        Task ToggleStateAccountConfigAsync(int configId, bool active);
        Task<bool> AccountConfigExistsByNameAsync(string name);
        Task<bool> AccountConfigExistsByNameAsync(int id, string name);
        Task<bool> AccountConfigExistsByLimitAsync(decimal limit);
        Task<bool> AccountConfigExistsByLimitAsync(int id, decimal limit);


    }
}