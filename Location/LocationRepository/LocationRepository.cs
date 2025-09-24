using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using proyecto_venta_stock.Data;
using proyecto_venta_stock.Models;
using proyecto_venta_stock.Location.DTO;

namespace proyecto_venta_stock.Location.LocationRepository
{
    public class LocationRepository : ILocationRepository
    {
        private readonly VentaStockContext _dbContext;

        public LocationRepository(VentaStockContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task Create(Ubicacion ubicacion)
        {
            await _dbContext.Ubicacions.AddAsync(ubicacion);
            await _dbContext.SaveChangesAsync();
        }

        

        public Task<bool> Exists(int fila, string seccion, int nivel)
        {
            var sec = (seccion ?? string.Empty).Trim().ToUpper();
            return _dbContext.Ubicacions.AnyAsync(u =>
                u.Fila == fila &&
                u.Nivel == nivel &&
                u.Seccion != null && u.Seccion.ToUpper() == sec);
        }

        public Task<bool> ExistsExceptId(int idUbicacion, int fila, string seccion, int nivel)
        {
            var sec = (seccion ?? string.Empty).Trim().ToUpper();
            return _dbContext.Ubicacions.AnyAsync(u =>
                u.IdUbicacion != idUbicacion &&
                u.Fila == fila &&
                u.Nivel == nivel &&
                u.Seccion != null && u.Seccion.ToUpper() == sec);
        }


        public async Task<List<Ubicacion>> GetAll()
        {
            return await _dbContext.Ubicacions.ToListAsync();
        }

        public Task<Ubicacion> GetById(int idUbicacion)
        {
            return _dbContext.Ubicacions.FirstOrDefaultAsync(u => u.IdUbicacion == idUbicacion);
        }

        public async Task Update(Ubicacion ubicacion)
        {
            _dbContext.Ubicacions.Update(ubicacion);
            await _dbContext.SaveChangesAsync();
        }
        public async Task Delete(Ubicacion ubicacion)
        {
            _dbContext.Ubicacions.Remove(ubicacion);
            await _dbContext.SaveChangesAsync();
        }

        
    }
}