namespace proyecto_venta_stock.Message;

public enum ProductErrorCode
{
    product_not_found,
    product_name_in_use,
    categoria_invalida,
    ubicacion_invalida,
    error_inesperado
}

public static class ProductErrorDictionary
{
    public static readonly Dictionary<ProductErrorCode, string> Messages = new()
    {
        { ProductErrorCode.product_not_found, "El producto no fue encontrado." },
        { ProductErrorCode.product_name_in_use, "Este nombre de producto ya está en uso." },
        { ProductErrorCode.categoria_invalida, "La categoría especificada no es válida." },
        { ProductErrorCode.ubicacion_invalida, "La ubicación especificada no es válida." },
        { ProductErrorCode.error_inesperado, "Ocurrió un error inesperado. Por favor, intenta nuevamente." }
    };
}
