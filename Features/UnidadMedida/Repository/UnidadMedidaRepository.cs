using Microsoft.EntityFrameworkCore;
using proyecto_venta_stock.Data;

namespace venta_stock_webapi.Features.UnidadMedida.Repository;

public class UnidadMedidaRepository : IUnidadMedidaRepository
{
    private readonly VentaStockContext _context;

    public UnidadMedidaRepository(VentaStockContext context)
    {
        _context = context;
    }

    public Task<List<proyecto_venta_stock.Models.UnidadMedida>> GetAllAsync() =>
        _context.UnidadMedidas
            .AsNoTracking()
            .OrderBy(u => u.IdUnidadMedida)
            .ToListAsync();

    public Task<List<proyecto_venta_stock.Models.UnidadMedida>> GetActivosAsync() =>
        _context.UnidadMedidas
            .AsNoTracking()
            .Where(u => u.Activo)
            .OrderBy(u => u.IdUnidadMedida)
            .ToListAsync();

    public Task<proyecto_venta_stock.Models.UnidadMedida?> GetByIdAsync(int id) =>
        _context.UnidadMedidas
            .FirstOrDefaultAsync(u => u.IdUnidadMedida == id);

    public Task<bool> NombreDuplicadoAsync(string nombre, int? excludeId = null) =>
        _context.UnidadMedidas
            .AnyAsync(u => u.Nombre == nombre && (excludeId == null || u.IdUnidadMedida != excludeId));

    public Task<bool> AbreviaturaDuplicadaAsync(string abreviatura, int? excludeId = null) =>
        _context.UnidadMedidas
            .AnyAsync(u => u.Abreviatura == abreviatura && (excludeId == null || u.IdUnidadMedida != excludeId));

    public Task<bool> EstaEnUsoAsync(int id) =>
        _context.Productos
            .AnyAsync(p => p.IdUnidadMedida == id);

    public void Add(proyecto_venta_stock.Models.UnidadMedida unidad) =>
        _context.UnidadMedidas.Add(unidad);

    public Task SaveChangesAsync() =>
        _context.SaveChangesAsync();
}
