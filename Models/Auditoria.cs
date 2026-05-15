using System.Text.Json;
namespace proyecto_venta_stock.Models;

public class Auditoria
{
    public long IdAuditoria { get; set; }
    public DateTimeOffset FechaHora { get; set; }
    public int IdUsuario { get; set; }
    public string? UsuarioNombre { get; set; }
    public string Accion { get; set; } = null!;
    public string EntidadTipo { get; set; } = null!;
    public string? EntidadId { get; set; }
    public string? Detalle { get; set; }
    public string? ValoresAnteriores { get; set; }
    public string? ValoresNuevos { get; set; }

    // JSONB (PostgreSQL): EF Core + Npgsql lo soporta bien con JsonDocument
    //public JsonDocument? ValoresAnteriores { get; set; }
    //public JsonDocument? ValoresNuevos { get; set; }
}
