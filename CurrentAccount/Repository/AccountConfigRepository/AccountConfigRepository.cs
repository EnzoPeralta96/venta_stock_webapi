using Microsoft.EntityFrameworkCore;
using proyecto_venta_stock.Data;
using proyecto_venta_stock.Models;

namespace venta_stock_webapi.CurrentAccount.Repository
{
    public class AccountConfigRepository : IAccountConfigRepository
    {
        private readonly VentaStockContext _context;

        public AccountConfigRepository(VentaStockContext context)
        {
            _context = context;
        }

        public Task<ConfiguracionCc> GetAccountConfigByIdAsync(int configId)
        {
            return _context.ConfiguracionCcs.FirstOrDefaultAsync(c => c.IdConfig == configId);
        }
        public Task<List<ConfiguracionCc>> GetAccountConfigsAsync()
        {
            return _context.ConfiguracionCcs.ToListAsync();
        }

        public async Task CreateAccountConfigAsync(ConfiguracionCc config)
        {
            await _context.ConfiguracionCcs.AddAsync(config);
            await _context.SaveChangesAsync();
        }

        public Task<int> UpdateAccountConfigAsync(ConfiguracionCc config)
        {
            return _context.ConfiguracionCcs
                .Where(c => c.IdConfig == config.IdConfig)
                .ExecuteUpdateAsync(setters => 
                    setters
                        .SetProperty(c => c.Nombre, config.Nombre)
                        .SetProperty(c => c.MontoLimite, config.MontoLimite)
                );
        }

        public Task ToggleStateAccountConfigAsync(int configId, bool active)
        {
            return _context.ConfiguracionCcs
                .Where(c => c.IdConfig == configId)
                .ExecuteUpdateAsync(setters => 
                    setters.SetProperty(c => c.Activo, active)
                );
        }

        public Task<bool> AccountConfigExistsByNameAsync(string name)
        {
            return _context.ConfiguracionCcs
                .AnyAsync(c => c.Nombre == name);
        }

        public Task<bool> AccountConfigExistsByLimitAsync(decimal limit)
        {
            return _context.ConfiguracionCcs
                .AnyAsync(c => c.MontoLimite == limit);
        }
          public Task<bool> AccountConfigExistsByNameAsync(int id,string name)
        {
            return _context.ConfiguracionCcs
                .AnyAsync(c => c.Nombre == name && c.IdConfig != id);
        }

        public Task<bool> AccountConfigExistsByLimitAsync(int id, decimal limit)
        {
            return _context.ConfiguracionCcs
                .AnyAsync(c => c.MontoLimite == limit && c.IdConfig != id);
        }
    }
}