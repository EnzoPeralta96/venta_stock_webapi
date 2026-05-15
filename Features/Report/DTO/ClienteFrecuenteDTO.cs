namespace proyecto_venta_stock.Report.DTO
{
    public class ClienteFrecuenteDTO
    {
        public int IdCliente { get; set; }
        public string NombreCliente { get; set; } = null!;
        public int CantidadVentas { get; set; }
        public decimal TotalComprado { get; set; }
    }
}
