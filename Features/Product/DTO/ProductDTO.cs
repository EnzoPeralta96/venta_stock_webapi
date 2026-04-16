using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "El stock debe ser mayor o igual a 0.")]
        public decimal Stock { get; set; }
        public decimal? StockMinimo { get; set; }
        public bool? VentaSinStock { get; set; }
        public int? IdUbicacion { get; set; }
        public int? IdCategoria { get; set; }
        public int? IdUnidadMedida { get; set; }
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
        public decimal? Stock { get; set; }
        public decimal? StockMinimo { get; set; }
        public bool? VentaSinStock { get; set; }
        public int? IdUbicacion { get; set; }
        public int? IdCategoria { get; set; }
        public int? IdUnidadMedida { get; set; }
        public string? UnidadMedida { get; set; }
        public bool Activo { get; set; }

        // Info extra para "with category and ubication"
        public string? Categoria { get; set; }
        public string? Ubicacion { get; set; }

        public List<CodigoBarraDTO> CodigoBarras { get; set; } = [];
    }

    public class ProductExportDTO
    {
        public string CodigoBarra { get; set; }
        public string Nombre { get; set; }
        public string Marca { get; set; }
        public decimal Precio { get; set; }
        public int IdCategoria { get; set; }
        public int IdUbicacion { get; set; }
    }
}
