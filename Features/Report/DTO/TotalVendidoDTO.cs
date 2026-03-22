namespace proyecto_venta_stock.Report.DTO
{
    public class TotalVendidoDTO
    {
        public decimal TotalVendido { get; set; }
        public int CantidadVentas { get; set; }
        public DateTime FechaDesde { get; set; }
        public DateTime FechaHasta { get; set; }
    }
}
