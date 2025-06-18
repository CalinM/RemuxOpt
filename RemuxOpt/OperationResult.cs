namespace RemuxOpt
{
    public class OperationResult<T>
    {
        public bool Success { get; private set; } = true;
        public string CustomErrorMessage { get; private set; }
        public Exception Exception { get; private set; }
        public T AdditionalDataReturn { get; private set; }

        // Constructors
        public OperationResult() { }

        public OperationResult(T result) => AdditionalDataReturn = result;

        public OperationResult(Exception ex, bool includeStackTrace = true) => FailWithMessage(ex, includeStackTrace);

        // Factory Methods (Renamed to Avoid Conflict)
        public static OperationResult<T> CreateSuccess(T data) => new() { AdditionalDataReturn = data };

        public static OperationResult<T> CreateFailure(string message) => new() { Success = false, CustomErrorMessage = message };

        public static OperationResult<T> CreateFailure(Exception ex, bool includeStackTrace = true) =>
            new OperationResult<T> { Success = false, Exception = ex, CustomErrorMessage = GetErrorMessage(ex, includeStackTrace) };

        // Error Message Handling
        private static string GetErrorMessage(Exception ex, bool includeStackTrace = true)
        {
            if (ex == null) return string.Empty;

            var messages = new List<string>();
            while (ex != null)
            {
                messages.Add(ex.Message);
                ex = ex.InnerException;
            }

            string result = string.Join(" → ", messages);
            return includeStackTrace ? result + "\n" + ex?.StackTrace : result;
        }

        // Restore FailWithMessage Methods
        public OperationResult<T> FailWithMessage(string message)
        {
            Success = false;
            CustomErrorMessage = message;
            return this;
        }

        public OperationResult<T> FailWithMessage(Exception ex, bool includeStackTrace = true)
        {
            Success = false;
            Exception = ex;
            CustomErrorMessage = GetErrorMessage(ex, includeStackTrace);
            return this;
        }

        // Implicit Conversions for Easy Use
        public static implicit operator T(OperationResult<T> result) => result.AdditionalDataReturn;
        public static implicit operator bool(OperationResult<T> result) => result.Success;
    }
}
