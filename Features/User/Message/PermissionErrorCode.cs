namespace venta_stock_webapi.User.Message
{
    public enum PermissionErrorCode
    {
        unexpected_error
    }

    public static class PermissionErrorDictionary
    {
        public static readonly Dictionary<PermissionErrorCode, string> Messages = new()
        {
            { PermissionErrorCode.unexpected_error, "Ocurrió un error inesperado, por favor intente nuevamente." },
        };
    }
    
}