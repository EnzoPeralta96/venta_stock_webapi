namespace proyecto_venta_stock.Message;

public enum ListaPrecioItemErrorCode
{
    lista_not_found,
    producto_not_found,
    item_already_exists,
    item_not_found,
    formato_no_soportado,
    error_inesperado
}

public static class ListaPrecioItemErrorDictionary
{
    public static readonly Dictionary<ListaPrecioItemErrorCode, string> Messages = new()
    {
        { ListaPrecioItemErrorCode.lista_not_found, "La lista de precios no existe." },
        { ListaPrecioItemErrorCode.producto_not_found, "El producto no existe." },
        { ListaPrecioItemErrorCode.item_already_exists, "El producto ya existe en la lista." },
        { ListaPrecioItemErrorCode.item_not_found, "El producto no está en la lista." },
        { ListaPrecioItemErrorCode.formato_no_soportado, "El formato del archivo no es soportado. Use .xlsx o .csv." },
        { ListaPrecioItemErrorCode.error_inesperado, "Ocurrió un error inesperado." },
    };
}
