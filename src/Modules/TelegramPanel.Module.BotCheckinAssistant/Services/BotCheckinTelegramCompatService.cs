using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TL;
using TelegramPanel.Core.Interfaces;
using TelegramPanel.Core.Models;
using TelegramPanel.Core.Services;
using TelegramPanel.Core.Services.Telegram;
using TelegramPanel.Data.Entities;
using WTelegram;

namespace TelegramPanel.Module.BotCheckinAssistant.Services;

public sealed class BotCheckinTelegramCompatService
{
    private readonly AccountManagementService _accountManagement;
    private readonly AccountTelegramToolsService _accountTelegramTools;
    private readonly ITelegramClientPool _clientPool;
    private readonly IConfiguration _configuration;
    private readonly ILogger<BotCheckinTelegramCompatService> _logger;

    public BotCheckinTelegramCompatService(
        AccountManagementService accountManagement,
        AccountTelegramToolsService accountTelegramTools,
        ITelegramClientPool clientPool,
        IConfiguration configuration,
        ILogger<BotCheckinTelegramCompatService> logger)
    {
        _accountManagement = accountManagement;
        _accountTelegramTools = accountTelegramTools;
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
            return (false, string.IsNullOrWhiteSpace(details) ? summary : $"{summary}: {details}", null, null);
        }
    }

    public async Task<(bool Success, string? Error, TelegramVerificationMessageCandidate? Candidate)> WaitForBotReplyAsync(
        int accountId,
        AccountTelegramToolsService.ResolvedChatTarget target,
        int sentMessageId,
        string? currentUsername,
        int timeoutSeconds,
        string? botUsername,
        CancellationToken cancellationToken = default)
    {
        var waitStartedAtUtc = DateTimeOffset.UtcNow;
        var allowedSenders = BuildAllowedSenders(target, botUsername);

        try
        {
            var liveResult = await _accountTelegramTools.WaitForBotVerificationMessageAsync(
                accountId,
                target,
                sentMessageId,
                currentUsername,
                timeoutSeconds,
                messageFilter: static _ => true,
                allowedSenderUsernames: allowedSenders,
                restrictToAllowedUsernames: allowedSenders.Count > 0,
                stopOnUnmatchedMention: false,
                cancellationToken: cancellationToken);

            if (liveResult.Success && liveResult.Candidate != null)
            {
                await TryMarkChatAsReadAsync(accountId, target, liveResult.Candidate.MessageId, cancellationToken);
                return liveResult;
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Live reply wait failed, fallback to history: accountId={AccountId}", accountId);
        }

        var fallback = await TryReadRecentBotReplyFromHistoryAsync(
            accountId,
            target,
            sentMessageId,
            currentUsername,
            waitStartedAtUtc,
            botUsername,
            cancellationToken);

        if (fallback != null)
        {
            await TryMarkChatAsReadAsync(accountId, target, fallback.MessageId, cancellationToken);
            return (true, null, fallback);
        }

        return (false, $"等待机器人回复超时（{timeoutSeconds} 秒）", null);
    }

    public async Task<IReadOnlyList<int>> FindAccountsWithBotHistoryAsync(
        IReadOnlyCollection<Account> sourceAccounts,
        string botLinkOrUsername,
        CancellationToken cancellationToken = default)
    {
        if (sourceAccounts.Count == 0)
            return Array.Empty<int>();

        var result = new List<int>();
        foreach (var account in sourceAccounts.OrderBy(x => x.Id))
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var resolve = await ResolveExternalBotTargetAsync(account.Id, botLinkOrUsername, cancellationToken, assumeBotUsername: true);
                if (!resolve.Success || resolve.Target == null)
                    continue;

                var client = await GetOrCreateConnectedClientAsync(account.Id, cancellationToken);
                var history = await client.Messages_GetHistory(resolve.Target.Peer, limit: 5);
                var hasMessage = history.Messages
                    .OfType<Message>()
                    .Any(m => m.id > 0);

                if (hasMessage)
                    result.Add(account.Id);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "FindAccountsWithBotHistoryAsync skipped account {AccountId}", account.Id);
            }
        }

        return result;
    }

    public async Task TryMarkChatAsReadAsync(
        int accountId,
        AccountTelegramToolsService.ResolvedChatTarget target,
        int maxMessageId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var client = await GetOrCreateConnectedClientAsync(accountId, cancellationToken);

            var method = typeof(Client)
                .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .FirstOrDefault(x => string.Equals(x.Name, "Messages_ReadHistory", StringComparison.Ordinal));

            if (method == null)
                return;

            var parameters = method.GetParameters();
            var args = new object?[parameters.Length];
            for (var i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                if (typeof(InputPeer).IsAssignableFrom(parameter.ParameterType))
                    args[i] = target.Peer;
                else if (parameter.ParameterType == typeof(int))
                    args[i] = maxMessageId;
                else if (parameter.HasDefaultValue)
                    args[i] = parameter.DefaultValue;
                else if (parameter.ParameterType == typeof(CancellationToken))
                    args[i] = cancellationToken;
                else
                    args[i] = parameter.ParameterType.IsValueType ? Activator.CreateInstance(parameter.ParameterType) : null;
            }

            if (method.Invoke(client, args) is Task task)
                await task;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "TryMarkChatAsReadAsync failed: accountId={AccountId}, messageId={MessageId}", accountId, maxMessageId);
        }
    }

    private async Task<TelegramVerificationMessageCandidate?> TryReadRecentBotReplyFromHistoryAsync(
        int accountId,
        AccountTelegramToolsService.ResolvedChatTarget target,
        int sentMessageId,
        string? currentUsername,
        DateTimeOffset waitStartedAtUtc,
        string? botUsername,
        CancellationToken cancellationToken)
    {
        try
        {
            var client = await GetOrCreateConnectedClientAsync(accountId, cancellationToken);
            var history = await client.Messages_GetHistory(target.Peer, limit: 15);
            var botUserId = TryGetBotUserId(target);

            foreach (var message in history.Messages.OfType<Message>().OrderBy(x => x.id))
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (message.id <= sentMessageId)
                    continue;

                if (message.Date.ToUniversalTime() < waitStartedAtUtc.UtcDateTime.AddSeconds(-2))
                    continue;

                if (!IsIncomingBotMessage(message, botUserId, botUsername))
                    continue;

                var text = (message.message ?? string.Empty).Trim();
                var buttons = ExtractInlineButtons(message);
                if (text.Length == 0 && buttons.Count == 0 && message.media == null)
                    continue;

                return new TelegramVerificationMessageCandidate(
                    message.id,
                    text.Length == 0 ? null : text,
                    null,
                    buttons,
                    ContainsUsernameMention(text, currentUsername),
                    (message.reply_to as MessageReplyHeader)?.reply_to_msg_id == sentMessageId,
                    message.Date.ToUniversalTime());
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "TryReadRecentBotReplyFromHistoryAsync failed for account {AccountId}", accountId);
        }

        return null;
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

    private static IReadOnlyList<string> BuildAllowedSenders(AccountTelegramToolsService.ResolvedChatTarget target, string? botUsername)
    {
        var list = new List<string>();
        if (!string.IsNullOrWhiteSpace(botUsername))
            list.Add(botUsername);

        var botUserId = TryGetBotUserId(target);
        if (botUserId > 0)
        {
            list.Add($"user:{botUserId}");
            list.Add(botUserId.ToString());
        }

        return list
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static long TryGetBotUserId(AccountTelegramToolsService.ResolvedChatTarget target)
    {
        if (target.Peer is InputPeerUser inputPeerUser)
            return inputPeerUser.user_id;

        return long.TryParse(target.CanonicalId, out var parsed) ? parsed : 0;
    }

    private static bool IsIncomingBotMessage(Message message, long botUserId, string? botUsername)
    {
        if (botUserId > 0 && message.from_id is PeerUser peerUser && peerUser.user_id == botUserId)
            return true;

        if (!string.IsNullOrWhiteSpace(botUsername)
            && message.from_id is PeerUser
            && string.Equals(botUsername.TrimStart('@'), ExtractPostAuthorOrUsername(message), StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return botUserId == 0 && message.from_id is PeerUser;
    }

    private static string ExtractPostAuthorOrUsername(Message message)
    {
        return (message.post_author ?? string.Empty).Trim().TrimStart('@');
    }

    private static bool ContainsUsernameMention(string? text, string? currentUsername)
    {
        var username = (currentUsername ?? string.Empty).Trim().TrimStart('@');
        if (username.Length == 0 || string.IsNullOrWhiteSpace(text))
            return false;

        return text.Contains($"@{username}", StringComparison.OrdinalIgnoreCase);
    }

    private static List<TelegramInlineButtonOption> ExtractInlineButtons(Message message)
    {
        if (message.reply_markup is not ReplyInlineMarkup markup)
            return new List<TelegramInlineButtonOption>();

        var result = new List<TelegramInlineButtonOption>();
        var index = 0;
        foreach (var row in markup.rows ?? Array.Empty<KeyboardButtonRow>())
        {
            if (row?.buttons == null)
                continue;

            foreach (var button in row.buttons)
            {
                if (button is KeyboardButtonCallback callback && callback.data is { Length: > 0 })
                {
                    var text = (callback.text ?? string.Empty).Trim();
                    if (text.Length == 0)
                        continue;

                    result.Add(new TelegramInlineButtonOption(index++, text, callback.data));
                }
            }
        }

        return result;
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
