using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace venta_stock_webapi.Features.Category.DTO
{
    public sealed class CreateCategoryDTO
    {
        [Required(ErrorMessage = "El campo 'Categoria' es obligatorio.")]
        public string Categoria { get; set; }

        [Required(ErrorMessage = "El campo 'Descripcion' es obligatorio.")]
        public string? Descripcion { get; set; }
    }
}