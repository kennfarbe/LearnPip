namespace LearnPip.Data.Domain;

public sealed class SystemSetting
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class AdministrationAuditEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? ActorAccountId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;
    public string? PreviousValue { get; set; }
    public string? NewValue { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
