namespace proyecto_venta_stock.Report.DTO
{
    public class ProductoVendidoDTO
    {
        public int IdProducto { get; set; }
        public string NombreProducto { get; set; } = null!;
        public string Categoria { get; set; } = null!;
        public int? IdUnidadMedida { get; set; }
        public decimal CantidadVendida { get; set; }
        public decimal TotalFacturado { get; set; }
    }
}
