using Microsoft.EntityFrameworkCore;
using proyecto_venta_stock.Data;
using proyecto_venta_stock.Models;

namespace venta_stock_webapi.CurrentAccount.Repository;

public class DebitNoteReasonRepository : IDebitNoteReasonRepository
{
    private readonly VentaStockContext _context;

    public DebitNoteReasonRepository(VentaStockContext context)
    {
        _context = context;
    }

    public Task<MotivoNotaDebito?> GetByIdAsync(int idMotivo)
    {
        return _context.MotivoNotaDebitos
            .FirstOrDefaultAsync(m => m.IdMotivo == idMotivo);
    }

    public Task<List<MotivoNotaDebito>> GetAllAsync(bool? activo = null, string? categoria = null)
    {
        var query = _context.MotivoNotaDebitos.AsQueryable();

        if (activo.HasValue)
            query = query.Where(m => m.Activo == activo.Value);

        if (!string.IsNullOrEmpty(categoria))
            query = query.Where(m => m.Categoria == categoria);

        return query.OrderBy(m => m.Nombre).ToListAsync();
    }

    public async Task CreateAsync(MotivoNotaDebito motivo)
    {
        await _context.MotivoNotaDebitos.AddAsync(motivo);
        await _context.SaveChangesAsync();
    }

    public Task<int> UpdateAsync(MotivoNotaDebito motivo)
    {
        return _context.MotivoNotaDebitos
            .Where(m => m.IdMotivo == motivo.IdMotivo)
            .ExecuteUpdateAsync(setters =>
                setters
                    .SetProperty(m => m.Nombre, motivo.Nombre)
                    .SetProperty(m => m.Categoria, motivo.Categoria)
            );
    }

    public Task ToggleStateAsync(int idMotivo, bool activo)
    {
        return _context.MotivoNotaDebitos
            .Where(m => m.IdMotivo == idMotivo)
            .ExecuteUpdateAsync(setters =>
                setters.SetProperty(m => m.Activo, activo)
            );
    }

    public Task<bool> ExistsByNameAsync(string nombre)
    {
        return _context.MotivoNotaDebitos
            .AnyAsync(m => m.Nombre == nombre);
    }

    public Task<bool> ExistsByNameAsync(int id, string nombre)
    {
        return _context.MotivoNotaDebitos
            .AnyAsync(m => m.Nombre == nombre && m.IdMotivo != id);
    }
}
