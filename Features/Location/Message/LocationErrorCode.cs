namespace proyecto_venta_stock.Message;

public enum LocationErrorCode
{
    location_not_found,
    duplicate_location,
    location_in_use,
    unexpected_error
}

public static class LocationErrorDictionary
{
    public static readonly Dictionary<LocationErrorCode, string> Messages = new()
    {
        { LocationErrorCode.location_not_found, "La ubicación no fue encontrada." },
        { LocationErrorCode.duplicate_location, "Ya existe una ubicación con estas características." },
        { LocationErrorCode.location_in_use, "No se puede modificar la ubicación porque está siendo utilizada por uno o más productos." },
        { LocationErrorCode.unexpected_error, "Ocurrió un error inesperado. Por favor, intenta nuevamente." }
    };
}
