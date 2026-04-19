using System.ComponentModel.DataAnnotations;

namespace proyecto_venta_stock.Proveedor.DTO
{
    public sealed class UpdateProveedorDTO
    {
        [Required(ErrorMessage = "El campo 'IdProveedor' es obligatorio.")]
        public int IdProveedor { get; set; }

        [Required(ErrorMessage = "El campo 'Nombre' es obligatorio.")]
        [StringLength(200, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 200 caracteres.")]
        public string Nombre { get; set; } = null!;

        // Opcional — el frontend envía null si está vacío
        [StringLength(300, ErrorMessage = "La dirección no puede superar los 300 caracteres.")]
        public string? Direccion { get; set; }

        // Opcional — el frontend envía null si está vacío
        [RegularExpression(@"^\d{7,15}$", ErrorMessage = "El teléfono debe contener solo dígitos (7 a 15).")]
        [StringLength(15, MinimumLength = 7, ErrorMessage = "El teléfono debe tener entre 7 y 15 dígitos.")]
        public string? Telefono { get; set; }
    }
}
