namespace proyecto_venta_stock.Report.DTO
{
    public class VentasPorPeriodoItemDTO
    {
        public string Periodo { get; set; } = null!;
        public decimal TotalVendido { get; set; }
        public int CantidadVentas { get; set; }
    }
}
