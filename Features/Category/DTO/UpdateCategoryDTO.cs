using System.ComponentModel.DataAnnotations;

namespace venta_stock_webapi.Features.Category.DTO
{
    public sealed class UpdateCategoryDTO
    {
        [Required(ErrorMessage = "El campo 'IdCategoria' es obligatorio.")]
        public int IdCategoria { get; set; }

        [Required(ErrorMessage = "El campo 'Categoria' es obligatorio.")]
        public string Categoria { get; set; }

        [Required(ErrorMessage = "El campo 'Descripcion' es obligatorio.")]
        public string? Descripcion { get; set; }
    }
}