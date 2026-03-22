namespace venta_stock_webapi.CurrentAccount.Message;

public enum InterestConfigCode
{
    config_not_found,
    config_name_exists,
    no_active_config,
    unexpected_error
}

public static class InterestConfigDictionary
{
    public static readonly Dictionary<InterestConfigCode, string> Messages = new()
    {
        { InterestConfigCode.config_not_found,
          "La configuración de interés indicada no existe." },
        { InterestConfigCode.config_name_exists,
          "Ya existe una configuración de interés con ese nombre." },
        { InterestConfigCode.no_active_config,
          "No hay ninguna configuración de interés activa. Configure una antes de continuar." },
        { InterestConfigCode.unexpected_error,
          "Ocurrió un error inesperado, por favor intente nuevamente." }
    };
}
