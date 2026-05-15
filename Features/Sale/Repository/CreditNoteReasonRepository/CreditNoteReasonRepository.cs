using Microsoft.EntityFrameworkCore;
using proyecto_venta_stock.Data;
using proyecto_venta_stock.Models;

namespace venta_stock_webapi.Sale.Repository;

public class CreditNoteReasonRepository : ICreditNoteReasonRepository
{
    private readonly VentaStockContext _context;

    public CreditNoteReasonRepository(VentaStockContext context)
    {
        _context = context;
    }

    public Task<MotivoNotaCredito?> GetByIdAsync(int idMotivo)
        => _context.MotivoNotaCreditos.FirstOrDefaultAsync(m => m.IdMotivo == idMotivo);

    public Task<List<MotivoNotaCredito>> GetAllAsync(bool? activo = null)
    {
        var query = _context.MotivoNotaCreditos.AsQueryable();
        if (activo.HasValue)
            query = query.Where(m => m.Activo == activo.Value);
        return query.OrderBy(m => m.Nombre).ToListAsync();
    }

    public async Task CreateAsync(MotivoNotaCredito motivo)
    {
        await _context.MotivoNotaCreditos.AddAsync(motivo);
        await _context.SaveChangesAsync();
    }

    public Task UpdateAsync(MotivoNotaCredito motivo)
        => _context.MotivoNotaCreditos
            .Where(m => m.IdMotivo == motivo.IdMotivo)
            .ExecuteUpdateAsync(s => s.SetProperty(m => m.Nombre, motivo.Nombre));

    public Task ToggleStateAsync(int idMotivo, bool activo)
        => _context.MotivoNotaCreditos
            .Where(m => m.IdMotivo == idMotivo)
            .ExecuteUpdateAsync(s => s.SetProperty(m => m.Activo, activo));

    public Task<bool> ExistsByNameAsync(string nombre)
        => _context.MotivoNotaCreditos.AnyAsync(m => m.Nombre == nombre);

    public Task<bool> ExistsByNameAsync(int id, string nombre)
        => _context.MotivoNotaCreditos.AnyAsync(m => m.Nombre == nombre && m.IdMotivo != id);
}
