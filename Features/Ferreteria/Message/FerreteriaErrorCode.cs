namespace venta_stock_webapi.Features.Ferreteria.Message
{
    public enum FerreteriaErrorCode
    {
        ferreteria_not_found,
        update_failed,
        unexpected_error
    }

    public static class FerreteriaErrorDictionary
    {
        public static readonly Dictionary<FerreteriaErrorCode, string> Messages = new()
        {
            { FerreteriaErrorCode.ferreteria_not_found, "No se encontró la configuración de la ferretería." },
            { FerreteriaErrorCode.update_failed, "Error al actualizar la información de la ferretería." },
            { FerreteriaErrorCode.unexpected_error, "Ocurrió un error inesperado, por favor intente nuevamente." }
        };
    }
}
