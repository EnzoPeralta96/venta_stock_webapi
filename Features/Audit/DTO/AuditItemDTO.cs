namespace venta_stock_webapi.Features.Audit.DTO
{
    public class AuditItemDTO
    {
        public long IdAuditoria { get; set; }
        public DateTimeOffset FechaHora { get; set; }
        public int IdUsuario { get; set; }
        public string? UsuarioNombre { get; set; }
        public string Accion { get; set; } = string.Empty;
        public string EntidadTipo { get; set; } = string.Empty;
        public string? EntidadId { get; set; }
        public string? Detalle { get; set; }
        public string? ValoresAnteriores { get; set; } // si querés devolver JSON “raw”
        public string? ValoresNuevos { get; set; }
    }
}