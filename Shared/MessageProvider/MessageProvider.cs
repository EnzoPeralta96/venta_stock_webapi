using proyecto_venta_stock.Message;

namespace venta_stock_webapi.Shared.MessageProvider
{
    public static class MessageProvider
    {
        public static string Get<TEnum>(Dictionary<TEnum, string> messages, TEnum key) where TEnum : Enum
        {
            if (messages.TryGetValue(key, out var message))
                return message;

            return "Error desconocido";
        }

        internal static object Get(object categoryErrorMessages, CategoryErrorCode errorCode)
        {
            throw new NotImplementedException();
        }
    }
}