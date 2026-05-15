namespace proyecto_venta_stock.Report.DTO
{
    public class ClienteSaldoDeudorDTO
    {
        public int IdCliente { get; set; }
        public string NombreCliente { get; set; } = null!;
        public decimal SaldoDeudor { get; set; }
        public decimal LimiteDisponible { get; set; }
    }
}
