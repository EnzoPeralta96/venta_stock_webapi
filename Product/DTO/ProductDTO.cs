using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace proyecto_venta_stock.Product.DTO
{
    public class ProductDTO
    {
        public int IdProducto { get; set; }
        public string Nombre { get; set; }
        public string Marca { get; set; }
        public string? Descripcion { get; set; }
        public decimal? Precio { get; set; }
        public int? Stock { get; set; }
        public int? StockMinimo { get; set; }
        public bool? VentaSinStock { get; set; }
        public int? IdUbicacion { get; set; }
        public int? IdCategoria { get; set; }
        public bool Activo { get; set; }
        public List<CodigoBarraDTO> CodigoBarras { get; set; } = [];
    }

    public class ProductDetailDTO
    {
        public int IdProducto { get; set; }
        public string Nombre { get; set; }
        public string Marca { get; set; }
        public string? Descripcion { get; set; }
        public decimal? Precio { get; set; }
        public int? Stock { get; set; }
        public int? StockMinimo { get; set; }
        public bool? VentaSinStock { get; set; }
        public int? IdUbicacion { get; set; }
        public int? IdCategoria { get; set; }
        public bool Activo { get; set; }

        // Info extra para “with category and ubication”
        public string? Categoria { get; set; }
        public string? Ubicacion { get; set; }

        public List<CodigoBarraDTO> CodigoBarras { get; set; } = [];
    }
}