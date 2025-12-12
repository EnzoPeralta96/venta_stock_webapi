using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using proyecto_venta_stock.Models;
namespace proyecto_venta_stock.Product.DTO
{
    public class CodigoBarraDTO
    {
        public int IdCodigo { get; set; }

        public string Codigo { get; set; }
        // Clave foránea
        public int IdProducto { get; set; }

        // Propiedad de navegación
        public Producto Producto { get; set; }
    }
}
