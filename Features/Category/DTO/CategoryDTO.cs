namespace proyecto_venta_stock.Category.DTO
{
    public class CategoryBasicDTO
    {
        public int IdCategoria { get; set; }
        public string Categoria { get; set; }

    }
     public class CategoryDetailDTO
    {
        public int IdCategoria { get; set; }
        public string Categoria { get; set; }
        public string? Descripcion { get; set; }
    }
}