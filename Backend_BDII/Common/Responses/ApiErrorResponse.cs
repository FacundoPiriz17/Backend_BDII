namespace Backend_BDII.Common.Responses;

public sealed class ApiErrorResponse
{
    public required string Code { get; init; }
    public required string Message { get; init; }
    public object? Details { get; init; }
    public DateTime FechaHoraUtc { get; init; } = DateTime.UtcNow;
}
