namespace proyecto_venta_stock.Report.DTO
{
    public class CategoriaVendidaDTO
    {
        public string Categoria { get; set; } = null!;
        public decimal CantidadVendida { get; set; }
        public decimal TotalFacturado { get; set; }
    }
}
