using System;
using System.Collections.Generic;

namespace proyecto_venta_stock.Models;

public partial class Cliente
{
    public int IdCliente { get; set; }

    public string? Nombre { get; set; }

    public string? Apellido { get; set; }

    public string? RazonSocial { get; set; }

    public string? Cuit { get; set; }

    public string? Dni { get; set; }

    public string? Telefono { get; set; }

    public string? Mail { get; set; }

    public virtual ICollection<MovimientoCc> MovimientoCcs { get; set; } = new List<MovimientoCc>();

    public virtual ICollection<Ventum> Venta { get; set; } = new List<Ventum>();
}
