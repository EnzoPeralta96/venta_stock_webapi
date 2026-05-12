using System.ComponentModel.DataAnnotations;

namespace proyecto_venta_stock.Location.DTO
{
    public class LocationCreateUpdateDTO
    {
        [Required(ErrorMessage = "La fila es obligatoria")]
        [Range(1, int.MaxValue, ErrorMessage = "La fila debe ser un número positivo")]
        public int Fila { get; set; }

        [Required(ErrorMessage = "La sección es obligatoria")]
        [StringLength(5, ErrorMessage = "La sección debe tener hasta 5 caracteres")]
        public string Seccion { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nivel es obligatorio")]
        [Range(1, int.MaxValue, ErrorMessage = "El nivel debe ser un número positivo")]
        public int Nivel { get; set; }
    }
}