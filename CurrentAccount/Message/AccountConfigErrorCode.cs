namespace venta_stock_webapi.CurrentAccount.Message
{
    public enum AccountConfigCode
    {
        account_config_not_found,
        account_config_already_active,
        account_config_name_exists,
        account_config_limit_exists,
        account_config_already_inactive,
        account_config_creation_failed,
        account_config_update_failed,
        unexpected_error
    }

    public static class AccountConfigDictionary
    {
        public static readonly Dictionary<AccountConfigCode, string> Messages = new()
        {
            { AccountConfigCode.account_config_not_found, "La configuración de cuenta indicada no existe." },
            { AccountConfigCode.account_config_already_active, "La configuración de cuenta ya está activa." },
            { AccountConfigCode.account_config_name_exists, "El nombre de la configuración de cuenta ya existe." },
            { AccountConfigCode.account_config_limit_exists, "El límite de la configuración de cuenta ya existe." },
            { AccountConfigCode.account_config_already_inactive, "La configuración de cuenta ya está inactiva." },
            { AccountConfigCode.account_config_creation_failed, "La creación de la configuración de cuenta falló." },
            { AccountConfigCode.account_config_update_failed, "La actualización de la configuración de cuenta falló." },
            { AccountConfigCode.unexpected_error, "Ocurrió un error inesperado, por favor intente nuevamente." }
        };
    }
}