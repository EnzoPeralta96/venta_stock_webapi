using System.ComponentModel.DataAnnotations;

namespace proyecto_venta_stock.Proveedor.DTO
{
    public sealed class CreateProveedorDTO
    {
        [Required(ErrorMessage = "El campo 'Nombre' es obligatorio.")]
        public string Nombre { get; set; } = null!;

        [Required(ErrorMessage = "El campo 'Direccion' es obligatorio.")]
        public string Direccion { get; set; }
        [Required(ErrorMessage = "El campo 'Telefono' es obligatorio.")]
        public string Telefono { get; set; }
    }

}