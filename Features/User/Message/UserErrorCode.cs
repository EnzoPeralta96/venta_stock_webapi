namespace proyecto_venta_stock.Message;

public enum UserErrorCode
{
    user_name_in_use,
    user_mail_in_use,
    permission_not_found,
    unexpected_error,
    username_not_found,
    user_not_found,
    user_already_active,
    operation_not_permitted
}

public static class UserErrorDictionary
{
    public static readonly Dictionary<UserErrorCode, string> Messages = new()
    {
        { UserErrorCode.user_name_in_use, "El nombre de usuario ya está en uso." },
        { UserErrorCode.user_mail_in_use, "El correo electrónico ya está en uso." },
        { UserErrorCode.permission_not_found, "El permiso indicado no existe o no está asignado." },
        { UserErrorCode.unexpected_error, "Ocurrió un error inesperado, por favor intente nuevamente." },
        { UserErrorCode.username_not_found, "El usuario y/o la contraseña son incorrectos." },
        { UserErrorCode.user_not_found, "El usuario indicado no existe." },
        { UserErrorCode.user_already_active, "El usuario ya se encuentra activo." },
        { UserErrorCode.operation_not_permitted, "No tiene permisos para realizar esta operación sobre este usuario." }
    };
}