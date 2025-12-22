using proyecto_venta_stock.Models;

namespace proyecto_venta_stock.Features.Ferreteria.Repository
{
    public interface IFerreteriaRepository
    {
        Task<Models.Ferreteria?> GetAsync();
        Task UpdateAsync(Models.Ferreteria ferreteria);
    }
}
