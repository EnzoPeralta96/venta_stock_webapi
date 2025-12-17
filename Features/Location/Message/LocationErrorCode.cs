namespace proyecto_venta_stock.Message;

public enum LocationErrorCode
{
    location_not_found,
    duplicate_location,
    unexpected_error
}

public static class LocationErrorDictionary
{
    public static readonly Dictionary<LocationErrorCode, string> Messages = new()
    {
        { LocationErrorCode.location_not_found, "La ubicación no fue encontrada." },
        { LocationErrorCode.duplicate_location, "Ya existe una ubicación con estas características." },
        { LocationErrorCode.unexpected_error, "Ocurrió un error inesperado. Por favor, intenta nuevamente." }
    };
}
