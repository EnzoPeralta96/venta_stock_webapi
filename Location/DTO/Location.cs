using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace proyecto_venta_stock.Location.DTO
{
    public class LocationDTO
    {
        public int IdUbicacion { get; set; }
        public int Fila { get; set; }
        public string Seccion { get; set; }
        public int Nivel { get; set; }
    }
}