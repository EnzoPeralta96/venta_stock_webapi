using Microsoft.EntityFrameworkCore;
using proyecto_venta_stock.Data;
using proyecto_venta_stock.Models;

namespace venta_stock_webapi.CurrentAccount.Repository;

public class InterestConfigRepository : IInterestConfigRepository
{
    private readonly VentaStockContext _context;

    public InterestConfigRepository(VentaStockContext context)
    {
        _context = context;
    }

    public Task<ConfiguracionInteres?> GetByIdAsync(int idConfig)
    {
        return _context.ConfiguracionIntereses
            .FirstOrDefaultAsync(c => c.IdConfig == idConfig);
    }

    public Task<List<ConfiguracionInteres>> GetAllAsync()
    {
        return _context.ConfiguracionIntereses
            .AsNoTracking()
            .OrderBy(c => c.Nombre)
            .ToListAsync();
    }

    public Task<ConfiguracionInteres?> GetCurrentAsync()
    {
        return _context.ConfiguracionIntereses
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.EsActual);
    }

    public async Task CreateAsync(ConfiguracionInteres config)
    {
        await _context.ConfiguracionIntereses.AddAsync(config);
        await _context.SaveChangesAsync();
    }

    public Task UpdateAsync(ConfiguracionInteres config)
    {
        return _context.ConfiguracionIntereses
            .Where(c => c.IdConfig == config.IdConfig)
            .ExecuteUpdateAsync(setters =>
                setters
                    .SetProperty(c => c.Nombre, config.Nombre)
                    .SetProperty(c => c.PorcentajeInteres, config.PorcentajeInteres)
                    .SetProperty(c => c.DiaVencimiento, config.DiaVencimiento)
            );
    }

    public async Task SetAsCurrentAsync(int idConfig)
    {
        // a. Desmarcar todos
        await _context.ConfiguracionIntereses
            .ExecuteUpdateAsync(setters => setters.SetProperty(c => c.EsActual, false));

        // b. Marcar el elegido
        await _context.ConfiguracionIntereses
            .Where(c => c.IdConfig == idConfig)
            .ExecuteUpdateAsync(setters => setters.SetProperty(c => c.EsActual, true));
    }

    public Task<bool> ExistsByNameAsync(string nombre)
    {
        return _context.ConfiguracionIntereses
            .AnyAsync(c => c.Nombre == nombre);
    }

    public Task<bool> ExistsByNameAsync(int id, string nombre)
    {
        return _context.ConfiguracionIntereses
            .AnyAsync(c => c.Nombre == nombre && c.IdConfig != id);
    }
}
