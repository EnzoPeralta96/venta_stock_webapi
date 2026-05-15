namespace venta_stock_webapi.Features.UnidadMedida.Messages;

public enum UnidadMedidaErrorCode
{
    unidad_not_found,
    nombre_duplicado,
    abreviatura_duplicada,
    unidad_en_uso,
    unexpected_error
}

public static class UnidadMedidaErrorDictionary
{
    public static readonly Dictionary<UnidadMedidaErrorCode, string> Messages = new()
    {
        { UnidadMedidaErrorCode.unidad_not_found,     "La unidad de medida indicada no existe." },
        { UnidadMedidaErrorCode.nombre_duplicado,      "Ya existe una unidad de medida con ese nombre." },
        { UnidadMedidaErrorCode.abreviatura_duplicada, "Ya existe una unidad de medida con esa abreviatura." },
        { UnidadMedidaErrorCode.unidad_en_uso,         "No se puede desactivar la unidad porque está siendo utilizada por uno o más productos." },
        { UnidadMedidaErrorCode.unexpected_error,      "Ocurrió un error inesperado. Por favor intente nuevamente." }
    };
}
