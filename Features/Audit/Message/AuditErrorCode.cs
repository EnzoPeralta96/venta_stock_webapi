namespace proyecto_venta_stock.Message;

public enum AuditErrorCode
{
    unexpected_error
}

public static class AuditErrorDictionary
{
    public static readonly Dictionary<AuditErrorCode, string> Messages = new()
    {
        { AuditErrorCode.unexpected_error, "Ocurrió un error inesperado, por favor intente nuevamente." }
    };
}