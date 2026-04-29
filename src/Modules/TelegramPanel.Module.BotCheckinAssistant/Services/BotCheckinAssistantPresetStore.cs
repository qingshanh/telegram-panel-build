using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
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
    string MessageScript,
    IReadOnlyList<int> SelectedAccountIds,
    DateTimeOffset UpdatedAtUtc);

public sealed class BotCheckinAssistantPresetStore
{
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
        try
        {
            if (!File.Exists(_configFilePath))
                return Array.Empty<BotCheckinAssistantPreset>();

            var json = await File.ReadAllTextAsync(_configFilePath, cancellationToken);
            var root = JsonNode.Parse(json)?.AsObject();
            if (root?["BotCheckinAssistant"] is not JsonObject section
                || section["Presets"] is not JsonObject presetsObj)
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
                var delayBetweenAccountsSeconds = Math.Clamp(presetObj["DelayBetweenAccountsSeconds"]?.GetValue<int>() ?? 2, 0, 60);
                var messageScript = presetObj["MessageScript"]?.GetValue<string>() ?? string.Empty;
                var updatedAtUtcRaw = presetObj["UpdatedAtUtc"]?.GetValue<string>();
                var updatedAtUtc = DateTimeOffset.TryParse(updatedAtUtcRaw, out var parsedUpdatedAtUtc)
                    ? parsedUpdatedAtUtc
                    : DateTimeOffset.MinValue;

                var selectedAccountIds = new List<int>();
                if (presetObj["SelectedAccountIds"] is JsonArray accountIdArray)
                {
                    foreach (var item in accountIdArray)
                    {
                        if (item is JsonValue value && value.TryGetValue<int>(out var accountId) && accountId > 0)
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
                    messageScript,
                    selectedAccountIds,
                    updatedAtUtc));
            }

            return list
                .OrderByDescending(x => x.UpdatedAtUtc)
                .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch
        {
            return Array.Empty<BotCheckinAssistantPreset>();
        }
    }

    public async Task SavePresetAsync(BotCheckinAssistantPreset preset, CancellationToken cancellationToken = default)
    {
        var name = (preset.Name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("记录名称不能为空", nameof(preset));

        var accountIds = (preset.SelectedAccountIds ?? Array.Empty<int>())
            .Where(x => x > 0)
            .Distinct()
            .OrderBy(x => x)
            .ToList();

        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            await EnsureConfigExistsAsync(cancellationToken);

            var json = await File.ReadAllTextAsync(_configFilePath, cancellationToken);
            var root = JsonNode.Parse(json)?.AsObject() ?? new JsonObject();
            var section = root["BotCheckinAssistant"] as JsonObject ?? new JsonObject();
            var presetsObj = section["Presets"] as JsonObject ?? new JsonObject();

            var accountIdArray = new JsonArray();
            foreach (var accountId in accountIds)
                accountIdArray.Add(accountId);

            presetsObj[name] = new JsonObject
            {
                ["BotTarget"] = preset.BotTarget,
                ["StartParameter"] = preset.StartParameter,
                ["AutoStartBeforeFirstMessage"] = preset.AutoStartBeforeFirstMessage,
                ["WaitTimeoutSeconds"] = Math.Clamp(preset.WaitTimeoutSeconds, 3, 300),
                ["DelayBetweenAccountsSeconds"] = Math.Clamp(preset.DelayBetweenAccountsSeconds, 0, 60),
                ["MessageScript"] = preset.MessageScript,
                ["SelectedAccountIds"] = accountIdArray,
                ["UpdatedAtUtc"] = preset.UpdatedAtUtc
            };

            section["Presets"] = presetsObj;
            root["BotCheckinAssistant"] = section;

            await WriteJsonAtomicallyAsync(ToIndentedJson(root), cancellationToken);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public async Task DeletePresetAsync(string name, CancellationToken cancellationToken = default)
    {
        name = (name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
            return;

        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            if (!File.Exists(_configFilePath))
                return;

            var json = await File.ReadAllTextAsync(_configFilePath, cancellationToken);
            var root = JsonNode.Parse(json)?.AsObject();
            if (root?["BotCheckinAssistant"] is not JsonObject section
                || section["Presets"] is not JsonObject presetsObj
                || !presetsObj.Remove(name))
                return;

            section["Presets"] = presetsObj;
            root["BotCheckinAssistant"] = section;
            await WriteJsonAtomicallyAsync(ToIndentedJson(root), cancellationToken);
        }
        finally
        {
            _writeLock.Release();
        }
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

    private static string ToIndentedJson(JsonNode node)
    {
        return node.ToJsonString(new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            WriteIndented = true
        });
    }
}
