namespace proyecto_venta_stock.User.DTO
{
    public class PermissionsCategoryDTO
    {
        public int IdCategoriaPermiso { get; set; }
        public string Categoria { get; set; }
        public List<PermissionDTO> Permissions { get; set; } = [];
    }
}