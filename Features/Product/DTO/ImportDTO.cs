using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace proyecto_venta_stock.Product.DTO
{
    public class ProductImportRowDTO : IValidatableObject
    {
        public string CodigoBarra { get; set; }
        public string Nombre { get; set; }       // Obligatorio si es nuevo
        public string Marca { get; set; }        // Obligatorio si es nuevo
        public decimal? Precio { get; set; }     // Siempre obligatorio
        public decimal? Stock { get; set; }      // Solo para INSERTs nuevos; se ignora en UPDATEs
        public int? IdCategoria { get; set; }
        public int? IdUbicacion { get; set; }
        public int? IdUnidadMedida { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext context)
        {
            if (string.IsNullOrWhiteSpace(CodigoBarra))
            {
                yield return new ValidationResult(
                    "El Código de Barra es obligatorio.",
                    new[] { nameof(CodigoBarra) }
                );
            }

            if (Precio == null || Precio <= 0)
            {
                yield return new ValidationResult(
                    "El Precio es obligatorio y debe ser mayor a 0.",
                    new[] { nameof(Precio) }
                );
            }

            if (Stock.HasValue && Stock < 0)
            {
                yield return new ValidationResult(
                    "El Stock inicial no puede ser negativo.",
                    new[] { nameof(Stock) }
                );
            }
        }
    }

    /* DTO para manejar errores por filas */
    public class RowErrorDTO
    {
        public int LineNumber { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    /* DTO para el resultado final de la importacion */
    public class BulkImportResultDTO
    {
        public int ProductosCreados { get; set; }
        public int ProductosActualizados { get; set; }
        public List<RowErrorDTO> Errores { get; set; } = new();
    }
}
