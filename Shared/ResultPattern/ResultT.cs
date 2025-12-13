namespace proyecto_venta_stock.Shared.ResultPattern
{
     public class Result<T>
    {
        public bool IsSucces { get; }
        public T Value { get; }
        public Enum ErrorCode { get; }

        private Result(T value, bool isSucces)
        {
            Value = value;
            IsSucces = isSucces;
        }

        private Result(T value, bool isSucces, Enum errorMessage)
        {
            Value = value;
            IsSucces = isSucces;
            ErrorCode = errorMessage;
        }

        public static Result<T> Succes(T value) => new Result<T>(value, true);
        public static Result<T> Succes() => new Result<T>(default, true);
        public static Result<T> Failure(Enum code) => new Result<T>(default, false, code);
    }
    
}