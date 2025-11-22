namespace venta_stock_webapi.Cliente.DTO
{
    public class ClienteCreateDTO
    {
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public string RazonSocial { get; set; }
        public string Dni { get; set; }
        public string Cuit { get; set; }
        public string Telefono { get; set; } = string.Empty;
        public string Mail { get; set; } = string.Empty;
        public bool TieneCuentaCorriente { get; set; }
        public decimal? LimiteCuenta { get; set; }
        public decimal? SaldoInicial { get; set; }
        public int idUsuarioRegistra {get; set; }
    }
}
