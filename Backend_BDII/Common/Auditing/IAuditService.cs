namespace Backend_BDII.Common.Auditing;

public interface IAuditService
{
    void Record(string action, string? actorEmail, object? details = null);
}
