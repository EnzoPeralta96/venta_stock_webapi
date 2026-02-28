namespace proyecto_venta_stock.Message;

public enum ProveedorErrorCode
{
    proveedor_not_found,
    proveedor_name_in_use,
    proveedor_in_use,
    error_inesperado
}

public static class ProveedorErrorDictionary
{
    public static readonly Dictionary<ProveedorErrorCode, string> Messages = new()
    {
        { ProveedorErrorCode.proveedor_not_found, "El proveedor no fue encontrado." },
        { ProveedorErrorCode.proveedor_name_in_use, "Este nombre de proveedor ya está en uso." },
        { ProveedorErrorCode.proveedor_in_use, "No se puede eliminar el proveedor porque está en uso (tiene compras y/o listas asociadas)." },
        { ProveedorErrorCode.error_inesperado, "Ocurrió un error inesperado. Por favor, intenta nuevamente." }
    };
}