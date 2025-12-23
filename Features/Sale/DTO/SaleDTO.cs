using System.ComponentModel.DataAnnotations;

namespace venta_stock_webapi.Sale.DTO
{
    public class CreateSaleDTO
    {
        [Required(ErrorMessage = "El cliente es obligatorio.")]
        public int idCliente { get; set; }

        [Required(ErrorMessage = "El medio de pago es obligatorio.")]
        public int idMedioPago { get; set; }

        [Required(ErrorMessage = "El usuario vendedor es obligatorio.")]
        public int idUsuarioVendedor { get; set; }

        [Required(ErrorMessage = "Debe agregar al menos un producto a la venta.")]
        [MinLength(1, ErrorMessage = "Debe agregar al menos un producto a la venta.")]
        public List<SaleItemDTO> items { get; set; } = new();

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (items == null || !items.Any())
            {
                yield return new ValidationResult(
                    "Debe incluir al menos un producto en la venta.",
                    new[] { nameof(items) }
                );
            }

            // Validar que las cantidades sean positivas
            if (items != null)
            {
                foreach (var item in items)
                {
                    if (item.Cantidad <= 0)
                    {
                        yield return new ValidationResult(
                            $"La cantidad del producto {item.IdProducto} debe ser mayor a 0.",
                            new[] { nameof(items) }
                        );
                    }
                }
            }
        }
    }

    // DTO para cada item/producto en la venta
    public class SaleItemDTO
    {
        [Required(ErrorMessage = "El ID del producto es obligatorio.")]
        public int IdProducto { get; set; }

        [Required(ErrorMessage = "La cantidad es obligatoria.")]
        [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor a 0.")]
        public int Cantidad { get; set; }

        // El precio se toma del producto al momento de la venta
        // No lo enviamos desde el front para evitar manipulación
    }

    // DTO de respuesta al crear una venta
    public class SaleResponseDTO
    {
        public int IdVenta { get; set; }
        public int? IdVentaPendiente { get; set; }  // ID de venta pendiente si requiere autorización
        public string CodigoVenta { get; set; }
        public DateTime Fecha { get; set; }
        public decimal Total { get; set; }

        // Información del cliente
        public string Cliente { get; set; }
        public string ClienteDni { get; set; }
        public string ClienteTelefono { get; set; }

        // Información del vendedor
        public string VendedorNombre { get; set; }

        // Medio de pago y estado
        public string MedioPago { get; set; }
        public string Estado { get; set; }

        public List<SaleItemDetailDTO> Items { get; set; } = new();
    }

    // DTO para el detalle de cada item en la respuesta
    public class SaleItemDetailDTO
    {
        public int IdProducto { get; set; }
        public string NombreProducto { get; set; }
        public string MarcaProducto { get; set; }
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Subtotal { get; set; }
    }

    // DTO para listado de ventas (historial)
    public class SaleListDTO
    {
        public int IdVenta { get; set; }
        public string CodigoVenta { get; set; }
        public DateTime Fecha { get; set; }
        public decimal Total { get; set; }
        public string Cliente { get; set; }
        public string MedioPago { get; set; }
        public string Estado { get; set; }
        public string Vendedor { get; set; }
    }

}