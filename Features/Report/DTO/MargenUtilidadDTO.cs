namespace proyecto_venta_stock.Report.DTO
{
    public class MargenUtilidadDTO
    {
        public decimal TotalVentas { get; set; }
        public decimal TotalCostos { get; set; }
        public decimal UtilidadBruta { get; set; }
        public decimal MargenPorcentaje { get; set; }
        public DateTime FechaDesde { get; set; }
        public DateTime FechaHasta { get; set; }
    }
}
