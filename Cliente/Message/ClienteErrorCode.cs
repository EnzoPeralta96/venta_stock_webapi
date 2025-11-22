namespace venta_stock_webapi.Cliente.Message
{
    public enum ClienteErrorCode
    {
        cliente_not_found,
        dni_in_use,
        cuit_in_use,
        email_in_use,
        invalid_persona_fisica_data,
        invalid_empresa_data,
        limite_cuenta_required,
        configuracion_cc_not_found,
        unexpected_error
    }

    public static class ClienteErrorDictionary
    {
        public static readonly Dictionary<ClienteErrorCode, string> Messages = new()
        {
            { ClienteErrorCode.cliente_not_found, "El cliente indicado no existe." },
            { ClienteErrorCode.dni_in_use, "El DNI ya está registrado." },
            { ClienteErrorCode.cuit_in_use, "El CUIT ya está registrado." },
            { ClienteErrorCode.email_in_use, "El correo electrónico ya está en uso." },
            { ClienteErrorCode.invalid_persona_fisica_data, "Para persona física se requiere DNI, Nombre y Apellido." },
            { ClienteErrorCode.invalid_empresa_data, "Para empresa se requiere CUIT y Razón Social." },
            { ClienteErrorCode.limite_cuenta_required, "El límite de cuenta es obligatorio cuando se crea una cuenta corriente." },
            { ClienteErrorCode.configuracion_cc_not_found, "No se encontró la configuración de cuenta corriente." },
            { ClienteErrorCode.unexpected_error, "Ocurrió un error inesperado, por favor intente nuevamente." }
        };
    }
}