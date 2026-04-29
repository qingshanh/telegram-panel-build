using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace TelegramPanel.Module.BotCheckinAssistant.Services;

public sealed record BotCheckinAssistantPreset(
    string Name,
    string BotTarget,
    string StartParameter,
    bool AutoStartBeforeFirstMessage,
    int WaitTimeoutSeconds,
    int DelayBetweenAccountsSeconds,
    bool EnableRandomDelay,
    int RandomDelayMinSeconds,
    int RandomDelayMaxSeconds,
    bool AutoContinueAllSteps,
    bool MarkRepliesAsRead,
    string MessageScript,
    IReadOnlyList<int> SelectedAccountIds,
    DateTimeOffset UpdatedAtUtc);

public sealed record BotCommandHistoryItem(string Command, DateTimeOffset UpdatedAtUtc);

public sealed class BotCheckinAssistantPresetStore
{
    private const string RootSectionName = "BotCheckinAssistant";
    private const string PresetsSectionName = "Presets";
    private const string RecentCommandsSectionName = "RecentCommands";
    private const string BotUsageSectionName = "BotUsage";

    private readonly string _configFilePath;
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    public BotCheckinAssistantPresetStore(IConfiguration configuration, IHostEnvironment environment)
    {
        var configured = (configuration["LocalConfig:Path"] ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(configured))
            _configFilePath = configured;
        else if (Directory.Exists("/data"))
            _configFilePath = "/data/appsettings.local.json";
        else
            _configFilePath = Path.Combine(environment.ContentRootPath, "appsettings.local.json");
    }

    public async Task<IReadOnlyList<BotCheckinAssistantPreset>> GetPresetsAsync(CancellationToken cancellationToken = default)
    {
        var root = await LoadRootAsync(cancellationToken);
        if (GetSection(root, PresetsSectionName) is not JsonObject presetsObj)
            return Array.Empty<BotCheckinAssistantPreset>();

        var list = new List<BotCheckinAssistantPreset>();
        foreach (var pair in presetsObj)
        {
            var name = (pair.Key ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(name) || pair.Value is not JsonObject presetObj)
                continue;

            var botTarget = presetObj["BotTarget"]?.GetValue<string>()?.Trim() ?? string.Empty;
            var startParameter = presetObj["StartParameter"]?.GetValue<string>()?.Trim() ?? string.Empty;
            var autoStart = presetObj["AutoStartBeforeFirstMessage"]?.GetValue<bool>() ?? true;
            var waitTimeoutSeconds = Math.Clamp(presetObj["WaitTimeoutSeconds"]?.GetValue<int>() ?? 25, 3, 300);
            var delayBetweenAccountsSeconds = Math.Clamp(presetObj["DelayBetweenAccountsSeconds"]?.GetValue<int>() ?? 2, 0, 600);
            var enableRandomDelay = presetObj["EnableRandomDelay"]?.GetValue<bool>() ?? false;
            var randomDelayMinSeconds = Math.Clamp(presetObj["RandomDelayMinSeconds"]?.GetValue<int>() ?? 0, 0, 600);
            var randomDelayMaxSeconds = Math.Clamp(presetObj["RandomDelayMaxSeconds"]?.GetValue<int>() ?? 0, 0, 600);
            var autoContinueAllSteps = presetObj["AutoContinueAllSteps"]?.GetValue<bool>() ?? false;
            var markRepliesAsRead = presetObj["MarkRepliesAsRead"]?.GetValue<bool>() ?? true;
            var messageScript = presetObj["MessageScript"]?.GetValue<string>() ?? string.Empty;
            var updatedAtUtc = ParseDateTimeOffset(presetObj["UpdatedAtUtc"]);

            var selectedAccountIds = new List<int>();
            if (presetObj["SelectedAccountIds"] is JsonArray accountIdArray)
            {
                foreach (var item in accountIdArray)
                {
                    if (TryGetInt(item, out var accountId) && accountId > 0)
                        selectedAccountIds.Add(accountId);
                }
            }

            selectedAccountIds = selectedAccountIds.Distinct().OrderBy(x => x).ToList();
            if (string.IsNullOrWhiteSpace(botTarget) || string.IsNullOrWhiteSpace(messageScript))
                continue;

            list.Add(new BotCheckinAssistantPreset(
                name,
                botTarget,
                startParameter,
                autoStart,
                waitTimeoutSeconds,
                delayBetweenAccountsSeconds,
                enableRandomDelay,
                randomDelayMinSeconds,
                randomDelayMaxSeconds,
                autoContinueAllSteps,
                markRepliesAsRead,
                messageScript,
                selectedAccountIds,
                updatedAtUtc));
        }

        return list
            .OrderByDescending(x => x.UpdatedAtUtc)
            .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task SavePresetAsync(BotCheckinAssistantPreset preset, CancellationToken cancellationToken = default)
    {
        var name = (preset.Name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Preset name cannot be empty.", nameof(preset));

        var accountIds = (preset.SelectedAccountIds ?? Array.Empty<int>())
            .Where(x => x > 0)
            .Distinct()
            .OrderBy(x => x)
            .ToList();

        await UpdateRootAsync(root =>
        {
            var presetsObj = GetOrCreateSection(root, PresetsSectionName);
            var accountIdArray = new JsonArray();
            foreach (var accountId in accountIds)
                accountIdArray.Add(accountId);

            presetsObj[name] = new JsonObject
            {
                ["BotTarget"] = preset.BotTarget,
                ["StartParameter"] = preset.StartParameter,
                ["AutoStartBeforeFirstMessage"] = preset.AutoStartBeforeFirstMessage,
                ["WaitTimeoutSeconds"] = Math.Clamp(preset.WaitTimeoutSeconds, 3, 300),
                ["DelayBetweenAccountsSeconds"] = Math.Clamp(preset.DelayBetweenAccountsSeconds, 0, 600),
                ["EnableRandomDelay"] = preset.EnableRandomDelay,
                ["RandomDelayMinSeconds"] = Math.Clamp(preset.RandomDelayMinSeconds, 0, 600),
                ["RandomDelayMaxSeconds"] = Math.Clamp(preset.RandomDelayMaxSeconds, 0, 600),
                ["AutoContinueAllSteps"] = preset.AutoContinueAllSteps,
                ["MarkRepliesAsRead"] = preset.MarkRepliesAsRead,
                ["MessageScript"] = preset.MessageScript,
                ["SelectedAccountIds"] = accountIdArray,
                ["UpdatedAtUtc"] = preset.UpdatedAtUtc
            };
        }, cancellationToken);
    }

    public async Task DeletePresetAsync(string name, CancellationToken cancellationToken = default)
    {
        name = (name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
            return;

        await UpdateRootAsync(root =>
        {
            if (GetSection(root, PresetsSectionName) is JsonObject presetsObj)
                presetsObj.Remove(name);
        }, cancellationToken);
    }

    public async Task<IReadOnlyList<BotCommandHistoryItem>> GetRecentCommandsAsync(int limit = 20, CancellationToken cancellationToken = default)
    {
        if (limit <= 0)
            limit = 20;

        var root = await LoadRootAsync(cancellationToken);
        if (GetSection(root, RecentCommandsSectionName) is not JsonObject commandsObj)
            return Array.Empty<BotCommandHistoryItem>();

        var list = new List<BotCommandHistoryItem>();
        foreach (var pair in commandsObj)
        {
            var command = (pair.Key ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(command))
                continue;

            var updatedAtUtc = ParseDateTimeOffset(pair.Value);
            list.Add(new BotCommandHistoryItem(command, updatedAtUtc));
        }

        return list
            .OrderByDescending(x => x.UpdatedAtUtc)
            .ThenBy(x => x.Command, StringComparer.OrdinalIgnoreCase)
            .Take(limit)
            .ToList();
    }

    public async Task RememberCommandsAsync(IEnumerable<string> commands, CancellationToken cancellationToken = default)
    {
        var normalized = (commands ?? Array.Empty<string>())
            .Select(x => (x ?? string.Empty).Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (normalized.Count == 0)
            return;

        await UpdateRootAsync(root =>
        {
            var commandsObj = GetOrCreateSection(root, RecentCommandsSectionName);
            var now = DateTimeOffset.UtcNow;
            foreach (var command in normalized)
                commandsObj[command] = now;
        }, cancellationToken);
    }

    public async Task<IReadOnlyList<int>> GetRememberedBotAccountIdsAsync(string botTarget, CancellationToken cancellationToken = default)
    {
        var key = NormalizeBotKey(botTarget);
        if (key.Length == 0)
            return Array.Empty<int>();

        var root = await LoadRootAsync(cancellationToken);
        if (GetSection(root, BotUsageSectionName) is not JsonObject usageObj
            || usageObj[key] is not JsonArray accountArray)
            return Array.Empty<int>();

        return accountArray
            .Select(x => TryGetInt(x, out var id) ? id : 0)
            .Where(x => x > 0)
            .Distinct()
            .OrderBy(x => x)
            .ToList();
    }

    public async Task RememberBotAccountUsageAsync(string botTarget, IEnumerable<int> accountIds, CancellationToken cancellationToken = default)
    {
        var key = NormalizeBotKey(botTarget);
        if (key.Length == 0)
            return;

        var ids = (accountIds ?? Array.Empty<int>())
            .Where(x => x > 0)
            .Distinct()
            .OrderBy(x => x)
            .ToList();

        if (ids.Count == 0)
            return;

        await UpdateRootAsync(root =>
        {
            var usageObj = GetOrCreateSection(root, BotUsageSectionName);
            var merged = new SortedSet<int>(ids);
            if (usageObj[key] is JsonArray existingArray)
            {
                foreach (var item in existingArray)
                {
                    if (TryGetInt(item, out var existingId) && existingId > 0)
                        merged.Add(existingId);
                }
            }

            var array = new JsonArray();
            foreach (var id in merged)
                array.Add(id);

            usageObj[key] = array;
        }, cancellationToken);
    }

    public static string NormalizeBotKey(string? raw)
    {
        var value = (raw ?? string.Empty).Trim();
        if (value.Length == 0)
            return string.Empty;

        if (Uri.TryCreate(value, UriKind.Absolute, out var uri))
        {
            if (string.Equals(uri.Host, "t.me", StringComparison.OrdinalIgnoreCase)
                || string.Equals(uri.Host, "telegram.me", StringComparison.OrdinalIgnoreCase))
            {
                var path = uri.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(path))
                    value = path;
            }
        }

        if (value.StartsWith("tg://resolve", StringComparison.OrdinalIgnoreCase))
        {
            var marker = "domain=";
            var index = value.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (index >= 0)
            {
                var domain = value[(index + marker.Length)..];
                var ampIndex = domain.IndexOf('&');
                if (ampIndex >= 0)
                    domain = domain[..ampIndex];
                value = domain;
            }
        }

        return value.Trim().TrimStart('@').Trim('/').ToLowerInvariant();
    }

    private async Task<JsonObject> LoadRootAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (!File.Exists(_configFilePath))
                return new JsonObject();

            var json = await File.ReadAllTextAsync(_configFilePath, cancellationToken);
            return JsonNode.Parse(json)?.AsObject() ?? new JsonObject();
        }
        catch
        {
            return new JsonObject();
        }
    }

    private async Task UpdateRootAsync(Action<JsonObject> update, CancellationToken cancellationToken)
    {
        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            await EnsureConfigExistsAsync(cancellationToken);
            var root = await LoadRootAsync(cancellationToken);
            update(root);
            await WriteJsonAtomicallyAsync(ToIndentedJson(root), cancellationToken);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    private static JsonObject GetOrCreateSection(JsonObject root, string sectionName)
    {
        var moduleRoot = root[RootSectionName] as JsonObject ?? new JsonObject();
        root[RootSectionName] = moduleRoot;

        var section = moduleRoot[sectionName] as JsonObject ?? new JsonObject();
        moduleRoot[sectionName] = section;
        return section;
    }

    private static JsonObject? GetSection(JsonObject root, string sectionName)
    {
        return root[RootSectionName] as JsonObject is { } moduleRoot
            ? moduleRoot[sectionName] as JsonObject
            : null;
    }

    private async Task EnsureConfigExistsAsync(CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(_configFilePath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        if (File.Exists(_configFilePath))
            return;

        await File.WriteAllTextAsync(_configFilePath, "{}", new UTF8Encoding(false), cancellationToken);
    }

    private async Task WriteJsonAtomicallyAsync(string json, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(_configFilePath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        var tempPath = $"{_configFilePath}.tmp";
        await File.WriteAllTextAsync(tempPath, json, new UTF8Encoding(false), cancellationToken);
        File.Move(tempPath, _configFilePath, overwrite: true);
    }

    private static bool TryGetInt(JsonNode? node, out int value)
    {
        value = 0;
        return node is JsonValue jsonValue && (jsonValue.TryGetValue(out value) || (jsonValue.TryGetValue(out string? raw) && int.TryParse(raw, out value)));
    }

    private static DateTimeOffset ParseDateTimeOffset(JsonNode? node)
    {
        if (node is JsonValue value)
        {
            if (value.TryGetValue(out string? raw) && DateTimeOffset.TryParse(raw, out var parsed))
                return parsed;

            if (value.TryGetValue(out DateTimeOffset dto))
                return dto;
        }

        return DateTimeOffset.MinValue;
    }

    private static string ToIndentedJson(JsonNode node)
    {
        return node.ToJsonString(new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            WriteIndented = true,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver()
        });
    }
}
