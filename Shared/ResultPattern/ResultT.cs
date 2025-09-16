namespace proyecto_venta_stock.Shared.ResultPattern
{
    public class Result<T>
    {
        public bool IsSucces { get; }
        public T Value { get; }
        public string ErrosMessage { get; }

        private Result(T value, bool isSucces)
        {
            Value = value;
            IsSucces = isSucces;
        }

        private Result(T value, bool isSucces, string errorMessage)
        {
            Value = value;
            IsSucces = isSucces;
            ErrosMessage = errorMessage;
        }

        public static Result<T> Succes(T value) => new Result<T>(value, true);
        public static Result<T> Succes() => new Result<T>(default, true);
        public static Result<T> Failure(string message) => new Result<T>(default, false, message);
    }
}