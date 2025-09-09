namespace Capturer.Api.DTOs;

/// <summary>
/// DTO estándar para respuestas de la API
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public string? ErrorCode { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    public static ApiResponse<T> SuccessResponse(T data, string message = "Operation completed successfully")
    {
        return new ApiResponse<T>
        {
            Success = true,
            Message = message,
            Data = data
        };
    }
    
    public static ApiResponse<T> ErrorResponse(string message, string? errorCode = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            ErrorCode = errorCode
        };
    }
}

/// <summary>
/// Resultado de sincronización con Dashboard
/// </summary>
public class SyncResult
{
    public bool Success { get; set; }
    public string? ReportId { get; set; }
    public string? Error { get; set; }
    public DateTime SyncTimestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Resultado de comandos ejecutados
/// </summary>
public class CommandResult
{
    public bool Success { get; set; }
    public string Command { get; set; } = string.Empty;
    public string? Output { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime ExecutionTime { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Health check result
/// </summary>
public class HealthCheckResult
{
    public string Status { get; set; } = "Healthy";
    public Dictionary<string, object> Details { get; set; } = new();
    public TimeSpan ResponseTime { get; set; }
    public DateTime CheckTime { get; set; } = DateTime.UtcNow;
}