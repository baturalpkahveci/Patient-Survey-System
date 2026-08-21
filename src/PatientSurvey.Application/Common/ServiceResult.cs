namespace PatientSurvey.Application.Common;

public sealed class ServiceResult
{
    private ServiceResult(bool isSuccess, string? errorCode, string? message)
    {
        IsSuccess = isSuccess;
        ErrorCode = errorCode;
        Message = message;
    }

    public bool IsSuccess { get; }
    public string? ErrorCode { get; }
    public string? Message { get; }

    // Factory methods for creating success and failure results
    public static ServiceResult Success() => new(true, null, null);
    public static ServiceResult Failure(string errorCode, string message) => new(false, errorCode, message);
}
