namespace proyecto_venta_stock.User.DTO
{
    public class UserDTO
    {
        public int IdUsuario { get; set; }
        public string Usuario { get; set; }
        public string Password { get; set; }
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public string Email { get; set; }
        public string Rol { get; set; }
        public bool Root { get; set; }
        public List<PermissionsCategoryDTO> Permisos { get; set; } = [];
    }
}