namespace proyecto_venta_stock.Report.DTO
{
    public class CategoriaVendidaDTO
    {
        public string Categoria { get; set; } = null!;
        public int CantidadVendida { get; set; }
        public decimal TotalFacturado { get; set; }
    }
}
