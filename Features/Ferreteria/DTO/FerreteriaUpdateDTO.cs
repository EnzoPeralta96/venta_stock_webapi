using System.ComponentModel.DataAnnotations;

namespace venta_stock_webapi.Features.Ferreteria.DTO
{
    public class FerreteriaUpdateDTO
    {
        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(150, ErrorMessage = "El nombre no puede superar los 150 caracteres")]
        public string Nombre { get; set; } = null!;

        [Required(ErrorMessage = "La dirección es obligatoria")]
        [StringLength(200, ErrorMessage = "La dirección no puede superar los 200 caracteres")]
        public string Direccion { get; set; } = null!;

        [Required(ErrorMessage = "El teléfono es obligatorio")]
        [StringLength(30, ErrorMessage = "El teléfono no puede superar los 30 caracteres")]
        public string Telefono { get; set; } = null!;

        [Required(ErrorMessage = "El email es obligatorio")]
        [EmailAddress(ErrorMessage = "Formato de email inválido")]
        [StringLength(150, ErrorMessage = "El email no puede superar los 150 caracteres")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "El CUIT es obligatorio")]
        [RegularExpression(@"^\d{2}-\d{8}-\d{1}$",
            ErrorMessage = "Formato de CUIT inválido (XX-XXXXXXXX-X)")]
        public string Cuit { get; set; } = null!;

        [Url(ErrorMessage = "La URL del logo no es válida")]
        public string? LogoUrl { get; set; }
    }
}
