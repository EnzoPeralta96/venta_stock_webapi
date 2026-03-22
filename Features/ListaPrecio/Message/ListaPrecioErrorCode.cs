namespace proyecto_venta_stock.Message;

public enum ListaPrecioErrorCode
{
    lista_not_found,
    lista_name_in_use,
    lista_tiene_items,
    proveedor_not_found,
    error_inesperado
}

public static class ListaPrecioErrorDictionary
{
    public static readonly Dictionary<ListaPrecioErrorCode, string> Messages = new()
    {
        { ListaPrecioErrorCode.lista_not_found, "La lista de precios no fue encontrada." },
        { ListaPrecioErrorCode.lista_name_in_use, "Ya existe una lista con ese nombre para este proveedor." },
        { ListaPrecioErrorCode.lista_tiene_items, "No se puede eliminar una lista que tiene productos asociados. Desactivála en su lugar." },
        { ListaPrecioErrorCode.proveedor_not_found, "El proveedor indicado no existe." },
        { ListaPrecioErrorCode.error_inesperado, "Ocurrió un error inesperado. Por favor, intenta nuevamente." }
    };
}
