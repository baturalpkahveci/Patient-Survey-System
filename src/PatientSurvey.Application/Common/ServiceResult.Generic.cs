namespace PatientSurvey.Application.Common;

public sealed class ServiceResult<T>
{
    private ServiceResult(bool isSuccess, T? value, string? errorCode, string? message)
    {
        IsSuccess = isSuccess;
        Value = value;
        ErrorCode = errorCode;
        Message = message;
    }

    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? ErrorCode { get; }
    public string? Message { get; }

    // Factory methods for creating success and failure results
    public static ServiceResult<T> Success(T value) => new(true, value, null, null);
    public static ServiceResult<T> Failure(string errorCode, string message) => new(false, default, errorCode, message);
}
