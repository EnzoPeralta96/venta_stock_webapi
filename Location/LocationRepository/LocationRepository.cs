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

        // 🔍 Devuelve un IQueryable filtrado y ordenado
        public IQueryable<Ubicacion> LocationsQueryable(string searchTerm, bool activos)
        {
            var query = _dbContext.Ubicacions
                .AsNoTracking()
                .Where(u => u.Activo == activos);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim();

                // Soporte patrón "Seccion-Fila-Nivel" (ej: A-3-2)
                var parts = term.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (parts.Length == 3)
                {
                    var sec = parts[0];
                    int? fila = int.TryParse(parts[1], out var f) ? f : null;
                    int? nivel = int.TryParse(parts[2], out var n) ? n : null;

                    query = query.Where(u =>
                        (string.IsNullOrEmpty(sec) || (u.Seccion != null && EF.Functions.ILike(u.Seccion, $"%{sec}%"))) &&
                        (!fila.HasValue || (u.Fila.HasValue && u.Fila == fila.Value)) &&
                        (!nivel.HasValue || (u.Nivel.HasValue && u.Nivel == nivel.Value))
                    );
                }
                else
                {
                    // Búsqueda general: Seccion contiene, o Fila/Nivel igual al número si term es numérico
                    int? number = int.TryParse(term, out var n) ? n : null;

                    query = query.Where(u =>
                        (u.Seccion != null && EF.Functions.ILike(u.Seccion, $"%{term}%")) ||
                        (number.HasValue && u.Fila.HasValue && u.Fila == number.Value) ||
                        (number.HasValue && u.Nivel.HasValue && u.Nivel == number.Value)
                    );
                }
            }

            return query
                .OrderBy(u => u.Seccion)
                .ThenBy(u => u.Fila)
                .ThenBy(u => u.Nivel);
        }

        // 🧱 Crear
        public async Task CreateAsync(Ubicacion ubicacion)
        {
            _dbContext.Ubicacions.Add(ubicacion);
            await _dbContext.SaveChangesAsync();
        }

        // ✏️ Actualizar
        public async Task UpdateAsync(Ubicacion ubicacion)
        {
            _dbContext.Ubicacions.Update(ubicacion);
            await _dbContext.SaveChangesAsync();
        }

        // 🔍 Obtener por ID
        public async Task<Ubicacion?> GetByIdAsync(int id)
        {
            return await _dbContext.Ubicacions
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.IdUbicacion == id);
        }

        // ❌ Borrado lógico
        public async Task DeleteAsync(int id)
        {
            var ubicacion = await _dbContext.Ubicacions.FindAsync(id);
            if (ubicacion != null)
            {
                ubicacion.Activo = false;
                await _dbContext.SaveChangesAsync();
            }
        }

        // 🔄 Toggle Activo/Inactivo
        public async Task ToggleActivoAsync(int id)
        {
            var ubicacion = await _dbContext.Ubicacions.FindAsync(id);
            if (ubicacion != null)
            {
                ubicacion.Activo = !ubicacion.Activo;
                await _dbContext.SaveChangesAsync();
            }
        }

        public async Task<bool> ExistsAsync(int fila, string seccion, int nivel)
        {
            return await _dbContext.Ubicacions.AnyAsync(u =>
                u.Fila == fila &&
                u.Seccion.ToLower() == seccion.ToLower() &&
                u.Nivel == nivel &&
                u.Activo);
        }

        public async Task<bool> ExistsExceptIdAsync(int id, int fila, string seccion, int nivel)
        {
            return await _dbContext.Ubicacions.AnyAsync(u =>
                u.IdUbicacion != id &&
                u.Fila == fila &&
                u.Seccion.ToLower() == seccion.ToLower() &&
                u.Nivel == nivel &&
                u.Activo);
        }


    }
}