namespace TelegramPanel.Module.BotCheckinAssistant.Services;

public sealed class BotCheckinTaskRunLog
{
    public DateTimeOffset StartedAtUtc { get; set; }
    public DateTimeOffset? FinishedAtUtc { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string? Error { get; set; }
    public bool Canceled { get; set; }
    public int TotalAccounts { get; set; }
    public int TotalMessages { get; set; }
    public int TotalOperations { get; set; }
    public int Completed { get; set; }
    public int Failed { get; set; }
    public List<BotCheckinTaskRunLogEntry> Entries { get; set; } = new();
}

public sealed class BotCheckinTaskRunLogEntry
{
    public int AccountId { get; set; }
    public string AccountLabel { get; set; } = string.Empty;
    public int StepNumber { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool SendSuccess { get; set; }
    public bool ReplyCaptured { get; set; }
    public bool ReplyMarkedAsRead { get; set; }
    public string? ReplyPreview { get; set; }
    public string? Error { get; set; }
    public DateTimeOffset ExecutedAtUtc { get; set; }
}
