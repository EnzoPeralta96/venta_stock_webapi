namespace venta_stock_webapi.Cliente.DTO
{
    public class ClienteResponseDTO
    {
        public int IdCliente { get; set; }
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public string RazonSocial { get; set; }
        public string Dni { get; set; }
        public string Cuit { get; set; }
        public string Telefono { get; set; }
        public string Mail { get; set; }
        public bool TieneCuentaCorriente { get; set; }
    }
}
