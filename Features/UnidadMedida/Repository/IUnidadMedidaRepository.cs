namespace venta_stock_webapi.Features.UnidadMedida.Repository;

public interface IUnidadMedidaRepository
{
    Task<List<proyecto_venta_stock.Models.UnidadMedida>> GetAllAsync();
    Task<List<proyecto_venta_stock.Models.UnidadMedida>> GetActivosAsync();
    Task<proyecto_venta_stock.Models.UnidadMedida?> GetByIdAsync(int id);
    Task<bool> NombreDuplicadoAsync(string nombre, int? excludeId = null);
    Task<bool> AbreviaturaDuplicadaAsync(string abreviatura, int? excludeId = null);
    Task<bool> EstaEnUsoAsync(int id);
    void Add(proyecto_venta_stock.Models.UnidadMedida unidad);
    Task SaveChangesAsync();
}
