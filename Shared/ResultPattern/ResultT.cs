namespace proyecto_venta_stock.Shared.ResultPattern
{
     public class Result<T>
    {
        public bool IsSuccess { get; }
        public T Value { get; }
        public Enum ErrorCode { get; }

        private Result(T value, bool isSuccess)
        {
            Value = value;
            IsSuccess = isSuccess;
        }

        private Result(T value, bool isSuccess, Enum errorMessage)
        {
            Value = value;
            IsSuccess = isSuccess;
            ErrorCode = errorMessage;
        }

        public static Result<T> Success(T value) => new Result<T>(value, true);
        public static Result<T> Success() => new Result<T>(default, true);
        public static Result<T> Failure(Enum code) => new Result<T>(default, false, code);
    }
    
}