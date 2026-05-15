namespace proyecto_venta_stock.Message;

public enum ProductErrorCode
{
    product_not_found,
    product_name_in_use,
    categoria_invalida,
    ubicacion_invalida,
    error_inesperado,
    product_in_use,
    barcode_in_use,
    formato_no_soportado
}

public static class ProductErrorDictionary
{
    public static readonly Dictionary<ProductErrorCode, string> Messages = new()
    {
        { ProductErrorCode.product_not_found, "El producto no fue encontrado." },
        { ProductErrorCode.product_name_in_use, "Este nombre de producto ya está en uso." },
        { ProductErrorCode.categoria_invalida, "La categoría especificada no es válida." },
        { ProductErrorCode.ubicacion_invalida, "La ubicación especificada no es válida." },
        { ProductErrorCode.product_in_use, "No se puede eliminar el producto porque está asociado a otros registros." },
        { ProductErrorCode.barcode_in_use, "Uno o más códigos de barra ya están registrados en otro producto." },
        { ProductErrorCode.error_inesperado, "Ocurrió un error inesperado. Por favor, intenta nuevamente." },
        { ProductErrorCode.formato_no_soportado, "El formato del archivo no es compatible. Use .xlsx, .xls o .csv." }
    };
}
