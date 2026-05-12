namespace proyecto_venta_stock.Location.DTO
{
     public class LocationDTO
    {
        public int IdUbicacion { get; set; }

        public int Fila { get; set; }

        public string Seccion { get; set; } = string.Empty;

        public int Nivel { get; set; }

        public bool Activo { get; set; } = true;
    }
}