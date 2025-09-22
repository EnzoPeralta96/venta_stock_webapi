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
        public Task Create(Ubicacion ubicacion);
        Task<bool> Exists(int fila, string seccion, int nivel);
        public Task<Ubicacion> GetById(int idUbicacion);
        public Task<List<Ubicacion>> GetAll();
        public Task Update(Ubicacion nuevaUbicacion);
    }
}