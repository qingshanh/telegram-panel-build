using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TL;
using TelegramPanel.Core.Interfaces;
using TelegramPanel.Core.Services;
using TelegramPanel.Core.Services.Telegram;
using TelegramPanel.Data.Entities;
using WTelegram;

namespace TelegramPanel.Module.BotCheckinAssistant.Services;

public sealed class BotCheckinTelegramCompatService
{
    private readonly AccountManagementService _accountManagement;
    private readonly ITelegramClientPool _clientPool;
    private readonly IConfiguration _configuration;
    private readonly ILogger<BotCheckinTelegramCompatService> _logger;

    public BotCheckinTelegramCompatService(
        AccountManagementService accountManagement,
        ITelegramClientPool clientPool,
        IConfiguration configuration,
        ILogger<BotCheckinTelegramCompatService> logger)
    {
        _accountManagement = accountManagement;
        _clientPool = clientPool;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<(bool Success, string? Error, AccountTelegramToolsService.ResolvedChatTarget? Target, string? BotUsername)> ResolveExternalBotTargetAsync(
        int accountId,
        string botLinkOrUsername,
        CancellationToken cancellationToken = default,
        bool assumeBotUsername = false)
    {
        try
        {
            var username = NormalizeTelegramBotUsername(botLinkOrUsername, assumeBotUsername);
            var client = await GetOrCreateConnectedClientAsync(accountId, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            var resolved = await client.Contacts_ResolveUsername(username);
            if (resolved.User is not User user)
                return (false, "无法解析机器人用户名。", null, null);

            if (user.access_hash == 0)
                return (false, "无法获取机器人 access_hash。", null, null);

            var target = new AccountTelegramToolsService.ResolvedChatTarget(
                new InputPeerUser(user.id, user.access_hash),
                "@" + username,
                user.id.ToString());

            return (true, null, target, "@" + username);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ResolveExternalBotTargetAsync failed for account {AccountId}", accountId);
            var (summary, details) = AccountTelegramToolsService.MapTelegramException(ex);
            var message = string.IsNullOrWhiteSpace(details) ? summary : $"{summary}：{details}";
            return (false, message, null, null);
        }
    }

    private async Task<Client> GetOrCreateConnectedClientAsync(int accountId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var existing = _clientPool.GetClient(accountId);
        if (existing?.User != null)
            return existing;

        var account = await _accountManagement.GetAccountAsync(accountId)
            ?? throw new InvalidOperationException($"账号不存在：{accountId}");

        var apiId = ResolveApiId(account);
        var apiHash = ResolveApiHash(account);
        var sessionKey = !string.IsNullOrWhiteSpace(account.ApiHash) ? account.ApiHash.Trim() : apiHash.Trim();

        if (string.IsNullOrWhiteSpace(account.SessionPath))
            throw new InvalidOperationException("账号缺少 SessionPath，无法连接 Telegram。");

        var client = await _clientPool.GetOrCreateClientAsync(
            accountId: accountId,
            apiId: apiId,
            apiHash: apiHash,
            sessionPath: account.SessionPath,
            sessionKey: sessionKey,
            phoneNumber: account.Phone,
            userId: account.UserId > 0 ? account.UserId : null);

        await client.ConnectAsync();
        cancellationToken.ThrowIfCancellationRequested();

        if (client.User == null && (client.UserId != 0 || account.UserId != 0))
            await client.LoginUserIfNeeded(reloginOnFailedResume: false);

        if (client.User == null)
            throw new InvalidOperationException("账号未登录或 session 已失效，请重新登录该账号。");

        return client;
    }

    private int ResolveApiId(Account account)
    {
        if (int.TryParse(_configuration["Telegram:ApiId"], out var globalApiId) && globalApiId > 0)
            return globalApiId;
        if (account.ApiId > 0)
            return account.ApiId;
        throw new InvalidOperationException("未配置全局 ApiId，且账号缺少 ApiId。");
    }

    private string ResolveApiHash(Account account)
    {
        var global = _configuration["Telegram:ApiHash"];
        if (!string.IsNullOrWhiteSpace(global))
            return global.Trim();
        if (!string.IsNullOrWhiteSpace(account.ApiHash))
            return account.ApiHash.Trim();
        throw new InvalidOperationException("未配置全局 ApiHash，且账号缺少 ApiHash。");
    }

    private static string NormalizeTelegramBotUsername(string raw, bool assumeBotUsername)
    {
        var value = (raw ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException("机器人用户名或链接不能为空。");

        if (Uri.TryCreate(value, UriKind.Absolute, out var uri))
        {
            if (string.Equals(uri.Host, "t.me", StringComparison.OrdinalIgnoreCase)
                || string.Equals(uri.Host, "telegram.me", StringComparison.OrdinalIgnoreCase))
            {
                var path = uri.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(path))
                    return path.TrimStart('@');
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
                domain = domain.Trim().TrimStart('@');
                if (!string.IsNullOrWhiteSpace(domain))
                    return domain;
            }
        }

        if (assumeBotUsername)
            return value.TrimStart('@').Trim().Trim('/');

        return value.TrimStart('@').Trim().Trim('/');
    }
}
