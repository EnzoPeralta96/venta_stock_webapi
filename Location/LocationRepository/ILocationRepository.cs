using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using proyecto_venta_stock.Models;
using proyecto_venta_stock.Location.DTO;


namespace proyecto_venta_stock.Location.LocationRepository
{
    public interface ILocationRepository
    {

        IQueryable<Ubicacion> LocationsQueryable(string searchTerm, bool activos);
        Task<Ubicacion?> GetByIdAsync(int id);
        Task CreateAsync(Ubicacion ubicacion);
        Task UpdateAsync(Ubicacion ubicacion);
        Task DeleteAsync(int id);
        Task ToggleActivoAsync(int id);

        /* Validar Duplicados */
        Task<bool> ExistsAsync(int fila, string seccion, int nivel);
        Task<bool> ExistsExceptIdAsync(int id, int fila, string seccion, int nivel);
    }
}