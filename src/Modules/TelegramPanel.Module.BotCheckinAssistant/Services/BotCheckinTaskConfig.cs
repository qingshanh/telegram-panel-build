using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace TelegramPanel.Module.BotCheckinAssistant.Services;

public sealed class BotCheckinTaskConfig
{
    public string TaskName { get; set; } = string.Empty;
    public string BotTarget { get; set; } = string.Empty;
    public string StartParameter { get; set; } = string.Empty;
    public bool AutoStartBeforeFirstMessage { get; set; } = true;
    public int WaitTimeoutSeconds { get; set; } = 25;
    public int DelayBetweenAccountsSeconds { get; set; } = 2;
    public bool EnableRandomDelay { get; set; }
    public int RandomDelayMinSeconds { get; set; }
    public int RandomDelayMaxSeconds { get; set; }
    public bool AutoContinueAllSteps { get; set; }
    public bool MarkRepliesAsRead { get; set; } = true;
    public string MessageScript { get; set; } = string.Empty;
    public List<int> SelectedAccountIds { get; set; } = new();

    public static BotCheckinTaskConfig CreateDefault() => new();

    public static BotCheckinTaskConfig FromPreset(BotCheckinAssistantPreset preset)
    {
        return new BotCheckinTaskConfig
        {
            TaskName = preset.Name,
            BotTarget = preset.BotTarget,
            StartParameter = preset.StartParameter,
            AutoStartBeforeFirstMessage = preset.AutoStartBeforeFirstMessage,
            WaitTimeoutSeconds = preset.WaitTimeoutSeconds,
            DelayBetweenAccountsSeconds = preset.DelayBetweenAccountsSeconds,
            EnableRandomDelay = preset.EnableRandomDelay,
            RandomDelayMinSeconds = preset.RandomDelayMinSeconds,
            RandomDelayMaxSeconds = preset.RandomDelayMaxSeconds,
            AutoContinueAllSteps = preset.AutoContinueAllSteps,
            MarkRepliesAsRead = preset.MarkRepliesAsRead,
            MessageScript = preset.MessageScript,
            SelectedAccountIds = preset.SelectedAccountIds.Distinct().OrderBy(x => x).ToList()
        };
    }

    public BotCheckinAssistantPreset ToPreset(string name, DateTimeOffset updatedAtUtc)
    {
        return new BotCheckinAssistantPreset(
            name.Trim(),
            (BotTarget ?? string.Empty).Trim(),
            (StartParameter ?? string.Empty).Trim(),
            AutoStartBeforeFirstMessage,
            NormalizeWaitTimeout(WaitTimeoutSeconds),
            NormalizeDelay(DelayBetweenAccountsSeconds),
            EnableRandomDelay,
            NormalizeDelay(RandomDelayMinSeconds),
            NormalizeDelay(RandomDelayMaxSeconds),
            AutoContinueAllSteps,
            MarkRepliesAsRead,
            MessageScript ?? string.Empty,
            NormalizeAccountIds(SelectedAccountIds),
            updatedAtUtc);
    }

    public static BotCheckinTaskConfig Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return CreateDefault();

        var config = JsonSerializer.Deserialize<BotCheckinTaskConfig>(json, CreateJsonOptions()) ?? CreateDefault();
        config.Normalize();
        return config;
    }

    public string Serialize()
    {
        Normalize();
        return JsonSerializer.Serialize(this, CreateJsonOptions());
    }

    public void Normalize()
    {
        TaskName = (TaskName ?? string.Empty).Trim();
        BotTarget = (BotTarget ?? string.Empty).Trim();
        StartParameter = (StartParameter ?? string.Empty).Trim();
        MessageScript = MessageScript ?? string.Empty;
        WaitTimeoutSeconds = NormalizeWaitTimeout(WaitTimeoutSeconds);
        DelayBetweenAccountsSeconds = NormalizeDelay(DelayBetweenAccountsSeconds);
        RandomDelayMinSeconds = NormalizeDelay(RandomDelayMinSeconds);
        RandomDelayMaxSeconds = NormalizeDelay(RandomDelayMaxSeconds);
        SelectedAccountIds = NormalizeAccountIds(SelectedAccountIds);
    }

    public IReadOnlyList<string> GetMessages()
    {
        return ParseMessages(MessageScript);
    }

    public int GetTotalOperations()
    {
        return GetMessages().Count * NormalizeAccountIds(SelectedAccountIds).Count;
    }

    public string? Validate()
    {
        Normalize();

        if (string.IsNullOrWhiteSpace(BotTarget))
            return "Please fill in the bot username or link first.";

        var messages = GetMessages();
        if (messages.Count == 0)
            return "Please enter at least one message to send.";

        if (SelectedAccountIds.Count == 0)
            return "Please select at least one account.";

        if (WaitTimeoutSeconds < 3 || WaitTimeoutSeconds > 300)
            return "Reply timeout must be between 3 and 300 seconds.";

        if (DelayBetweenAccountsSeconds < 0 || DelayBetweenAccountsSeconds > 600)
            return "Account delay must be between 0 and 600 seconds.";

        if (EnableRandomDelay)
        {
            if (RandomDelayMinSeconds < 0 || RandomDelayMinSeconds > 600 || RandomDelayMaxSeconds < 0 || RandomDelayMaxSeconds > 600)
                return "Random delay range must be between 0 and 600 seconds.";

            if (RandomDelayMaxSeconds < RandomDelayMinSeconds)
                return "Random delay max cannot be smaller than min.";
        }

        return null;
    }

    public int GetNextAccountDelaySeconds()
    {
        Normalize();

        if (!EnableRandomDelay)
            return DelayBetweenAccountsSeconds;

        var min = Math.Min(RandomDelayMinSeconds, RandomDelayMaxSeconds);
        var max = Math.Max(RandomDelayMinSeconds, RandomDelayMaxSeconds);
        return Random.Shared.Next(min, max + 1);
    }

    public static List<string> ParseMessages(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return new List<string>();

        return raw
            .Replace("\r", "\n", StringComparison.Ordinal)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        return new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            WriteIndented = true,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver()
        };
    }

    private static int NormalizeWaitTimeout(int value) => Math.Clamp(value, 3, 300);

    private static int NormalizeDelay(int value) => Math.Clamp(value, 0, 600);

    private static List<int> NormalizeAccountIds(IEnumerable<int>? accountIds)
    {
        return (accountIds ?? Array.Empty<int>())
            .Where(x => x > 0)
            .Distinct()
            .OrderBy(x => x)
            .ToList();
    }
}
