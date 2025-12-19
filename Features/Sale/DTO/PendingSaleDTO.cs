namespace venta_stock_webapi.Sale.DTO
{
    /// <summary>
    /// Respuesta cuando una venta queda pendiente de autorización
    /// </summary>
    public class PendingSaleResponseDTO
    {
        public int IdVentaPendiente { get; set; }
        public string CodigoVenta { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public string Cliente { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public DateTime FechaRegistro { get; set; }
        
        // Información de crédito
        public decimal SaldoActual { get; set; }
        public decimal LimiteCuenta { get; set; }
        public decimal SaldoDespuesVenta { get; set; }
        public decimal Excedente { get; set; }
        public decimal PorcentajeExcedente { get; set; }
        
        // Items
        public List<SaleItemDetailDTO> Items { get; set; } = new();
        
        // Mensaje para el usuario
        public string Mensaje => 
            $"Venta registrada como PENDIENTE. Excede el límite de crédito en ${Excedente:N2}. " +
            $"Requiere autorización del administrador.";
    }

    /// <summary>
    /// DTO para listar ventas pendientes (para el administrador)
    /// </summary>
    public class PendingSaleListDTO
    {
        public int IdVentaPendiente { get; set; }
        public string CodigoVenta { get; set; } = string.Empty;
        public DateTime FechaRegistro { get; set; }
        public decimal Total { get; set; }
        
        // Cliente
        public int IdCliente { get; set; }
        public string Cliente { get; set; } = string.Empty;
        
        // Vendedor
        public string Vendedor { get; set; } = string.Empty;
        
        // Información de crédito
        public decimal SaldoActual { get; set; }
        public decimal LimiteCuenta { get; set; }
        public decimal Excedente { get; set; }
        public decimal PorcentajeExcedente { get; set; }
        
        // Estado
        public string Estado { get; set; } = string.Empty;
        public int CantidadItems { get; set; }
        
        // Días pendiente
        public int DiasPendiente => (DateTime.Now - FechaRegistro).Days;
    }

    /// <summary>
    /// DTO para autorizar o rechazar una venta pendiente
    /// </summary>
    public class AuthorizeSaleDTO
    {
        public int IdVentaPendiente { get; set; }
        public bool Aprobar { get; set; }
        public string? Observaciones { get; set; }
    }

    /// <summary>
    /// Respuesta después de autorizar/rechazar
    /// </summary>
    public class AuthorizeSaleResponseDTO
    {
        public int IdVentaPendiente { get; set; }
        public string CodigoVenta { get; set; } = string.Empty;
        public bool Aprobada { get; set; }
        public int? IdVentaGenerada { get; set; }  // Si fue aprobada
        public string? CodigoVentaGenerada { get; set; }  // Si fue aprobada
        public string Mensaje { get; set; } = string.Empty;
    }
}