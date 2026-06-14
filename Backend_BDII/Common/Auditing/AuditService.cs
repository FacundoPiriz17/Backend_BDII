namespace Backend_BDII.Common.Auditing;

public sealed class AuditService : IAuditService
{
    private readonly ILogger<AuditService> _logger;

    public AuditService(ILogger<AuditService> logger)
    {
        _logger = logger;
    }
//Este método permite que se guarde cada acción en los logs del sistema
    public void Record(string action, string? actorEmail, object? details = null)
    {
        _logger.LogInformation(
            "AUDIT {Action} actor={ActorEmail} details={@Details}",
            action,
            string.IsNullOrWhiteSpace(actorEmail) ? "unknown" : actorEmail.Trim().ToLowerInvariant(),
            details);
    }
}
