using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
    }
}