namespace venta_stock_webapi.Features.StockMovement.Messages;

public enum StockMovementErrorCode
{
    producto_not_found,
    tipo_movimiento_invalido,
    unexpected_error
}

public static class StockMovementErrorDictionary
{
    public static readonly Dictionary<StockMovementErrorCode, string> Messages = new()
    {
        { StockMovementErrorCode.producto_not_found, "El producto indicado no existe." },
        { StockMovementErrorCode.tipo_movimiento_invalido, "El tipo de movimiento no es válido para ajustes manuales. Debe ser 5 (AjustePositivo), 6 (AjusteNegativo) o 7 (ConsumoInternoDueno)." },
        { StockMovementErrorCode.unexpected_error, "Ocurrió un error inesperado al registrar el movimiento de stock." }
    };
}
