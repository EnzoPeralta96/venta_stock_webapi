namespace proyecto_venta_stock.Message;

public enum CategoryErrorCode
{
    category_already_exists,
    category_not_found,
    category_name_in_use,
    category_in_use,
    error_inesperado
}

public static class CategoryErrorDictionary
{
    public static readonly Dictionary<CategoryErrorCode, string> Messages = new()
    {
        { CategoryErrorCode.category_already_exists, "Esta categoría ya existe en el sistema." },
        { CategoryErrorCode.category_not_found, "La categoría no fue encontrada." },
        { CategoryErrorCode.category_name_in_use, "Este nombre ya está siendo usado por otra categoría." },
        { CategoryErrorCode.category_in_use, "No se puede eliminar esta categoría porque está siendo usada por productos." },
        { CategoryErrorCode.error_inesperado, "Ocurrió un error inesperado. Por favor, intenta nuevamente." }
    };
}
