namespace venta_stock_webapi.CurrentAccount.Message
{
    public enum CurrentAccountCode
    {
        account_not_found,
        account_already_active,
        unexpected_error
    }

    public static class CurrentAccountDictionary
    {
        public static readonly Dictionary<CurrentAccountCode, string> Messages = new()
        {
            { CurrentAccountCode.account_not_found, "La cuenta indicada no existe." },
            { CurrentAccountCode.account_already_active, "La cuenta corriente ya está activa." },
            { CurrentAccountCode.unexpected_error, "Ocurrió un error inesperado, por favor intente nuevamente." }
        };
    }
}