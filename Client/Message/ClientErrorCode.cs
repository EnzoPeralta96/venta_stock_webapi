namespace venta_stock_webapi.Client.Message
{
    public enum ClientErrorCode
    {
        cliente_not_found,
        dni_in_use,
        cuit_in_use,
        email_in_use,
        invalid_persona_fisica_data,
        invalid_empresa_data,
        limite_cuenta_required,
        configuracion_cc_not_found,
        cliente_already_active,
        cliente_already_inactive,
        unexpected_error,
        empresa_in_use

    }

    public static class ClientErrorDictionary
    {
        public static readonly Dictionary<ClientErrorCode, string> Messages = new()
        {
            { ClientErrorCode.cliente_not_found, "El cliente indicado no existe." },
            { ClientErrorCode.dni_in_use, "El DNI ya está registrado." },
            { ClientErrorCode.cuit_in_use, "El CUIT ya está registrado." },
            { ClientErrorCode.email_in_use, "El correo electrónico ya está en uso." },
            { ClientErrorCode.invalid_persona_fisica_data, "Para persona física se requiere DNI, Nombre y Apellido." },
            { ClientErrorCode.invalid_empresa_data, "Para empresa se requiere CUIT y Razón Social." },
            { ClientErrorCode.limite_cuenta_required, "El límite de cuenta es obligatorio cuando se crea una cuenta corriente." },
            { ClientErrorCode.configuracion_cc_not_found, "No se encontró la configuración de cuenta corriente." },
            { ClientErrorCode.cliente_already_active, "El cliente ya está activo." },
            { ClientErrorCode.cliente_already_inactive, "El cliente ya está dado de baja." },
            { ClientErrorCode.unexpected_error, "Ocurrió un error inesperado, por favor intente nuevamente." },
            { ClientErrorCode.empresa_in_use, "La razón social ya está en uso." }
        };
    }
}