using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace proyecto_venta_stock.Product.DTO
{
    public class ProductDTO
    {
        public string Nombre { get; set; }
        public string Marca { get; set; }
        public string? Descripcion { get; set; }
        public decimal? Precio { get; set; }
        public int? Stock { get; set; }
        public int? StockMinimo { get; set; }
        public bool? VentaSinStock { get; set; }
        public int? IdUbicacion { get; set; }
        public int? IdCategoria { get; set; }
        public List<string> CodigoBarras { get; set; } = [];
    }
}