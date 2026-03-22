namespace proyecto_venta_stock.Models;

public class MotivoNotaDebito
{
    public int IdMotivo { get; set; }
    public string Nombre { get; set; } = null!;
    public bool Activo { get; set; } = true;

    /// <summary>
    /// Categoría del motivo: "general" | "ajuste_precio" | "interes_mora"
    /// Permite al frontend filtrar y pre-seleccionar motivos según el contexto.
    /// </summary>
    public string Categoria { get; set; } = "general";
}
