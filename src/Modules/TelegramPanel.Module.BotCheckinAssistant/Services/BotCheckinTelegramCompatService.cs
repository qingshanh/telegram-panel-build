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
                return (false, "Unable to resolve the bot username.", null, null);

            if (user.access_hash == 0)
                return (false, "Unable to get the bot access_hash.", null, null);

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

    public async Task<(bool Success, string? Error, TelegramVerificationMessageCandidate? Candidate, bool MarkedAsRead)> WaitForBotReplyAsync(
        int accountId,
        AccountTelegramToolsService.ResolvedChatTarget target,
        int sentMessageId,
        string? currentUsername,
        int timeoutSeconds,
        string? botUsername,
        bool markAsRead = true,
        CancellationToken cancellationToken = default)
    {
        var waitStartedAtUtc = DateTimeOffset.UtcNow;
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        var liveWaitTask = WaitForLiveReplyAsync(
            accountId,
            target,
            sentMessageId,
            currentUsername,
            timeoutSeconds,
            botUsername,
            waitStartedAtUtc,
            linkedCts.Token);

        var historyWaitTask = WaitForHistoryReplyUntilAsync(
            accountId,
            target,
            sentMessageId,
            currentUsername,
            waitStartedAtUtc,
            botUsername,
            timeoutSeconds,
            linkedCts.Token);

        try
        {
            while (true)
            {
                var completedTask = await Task.WhenAny(liveWaitTask, historyWaitTask);
                if (completedTask == historyWaitTask)
                {
                    var historyCandidate = await historyWaitTask;
                    if (historyCandidate != null)
                    {
                        linkedCts.Cancel();
                        var readApplied = markAsRead
                            && await TryMarkChatAsReadAsync(accountId, target, historyCandidate.MessageId, cancellationToken);
                        return (true, null, historyCandidate, readApplied);
                    }

                    break;
                }

                var liveResult = await liveWaitTask;
                if (liveResult.Success && liveResult.Candidate != null)
                {
                    linkedCts.Cancel();
                    var readApplied = markAsRead
                        && await TryMarkChatAsReadAsync(accountId, target, liveResult.Candidate.MessageId, cancellationToken);
                    return (liveResult.Success, liveResult.Error, liveResult.Candidate, readApplied);
                }

                if (historyWaitTask.IsCompleted)
                    break;
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "WaitForBotReplyAsync failed, fallback to final history scan: accountId={AccountId}", accountId);
        }

        var finalFallback = await TryReadRecentBotReplyFromHistoryAsync(
            accountId,
            target,
            sentMessageId,
            currentUsername,
            waitStartedAtUtc,
            botUsername,
            cancellationToken);

        if (finalFallback != null)
        {
            var readApplied = markAsRead
                && await TryMarkChatAsReadAsync(accountId, target, finalFallback.MessageId, cancellationToken);
            return (true, null, finalFallback, readApplied);
        }

        return (false, $"Timed out waiting for bot reply ({timeoutSeconds}s).", null, false);
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

    public async Task<bool> TryMarkChatAsReadAsync(
        int accountId,
        AccountTelegramToolsService.ResolvedChatTarget target,
        int maxMessageId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var client = await GetOrCreateConnectedClientAsync(accountId, cancellationToken);
            var latestMessageId = await TryGetLatestMessageIdAsync(client, target, cancellationToken);
            var readUpToMessageId = Math.Max(maxMessageId, latestMessageId);

            var invoked = await TryInvokePeerMethodAsync(client, "Messages_ReadHistory", target.Peer, readUpToMessageId, cancellationToken);
            await TryInvokePeerMethodAsync(client, "Messages_ReadMentions", target.Peer, null, cancellationToken);

            if (!invoked)
                return false;

            if (await IsChatMarkedAsReadAsync(client, target, readUpToMessageId, cancellationToken))
                return true;

            if (latestMessageId > readUpToMessageId)
            {
                invoked = await TryInvokePeerMethodAsync(client, "Messages_ReadHistory", target.Peer, latestMessageId, cancellationToken);
                await TryInvokePeerMethodAsync(client, "Messages_ReadMentions", target.Peer, null, cancellationToken);
                if (invoked && await IsChatMarkedAsReadAsync(client, target, latestMessageId, cancellationToken))
                    return true;
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "TryMarkChatAsReadAsync failed: accountId={AccountId}, messageId={MessageId}", accountId, maxMessageId);
        }

        return false;
    }

    private async Task<(bool Success, string? Error, TelegramVerificationMessageCandidate? Candidate)> WaitForLiveReplyAsync(
        int accountId,
        AccountTelegramToolsService.ResolvedChatTarget target,
        int sentMessageId,
        string? currentUsername,
        int timeoutSeconds,
        string? botUsername,
        DateTimeOffset waitStartedAtUtc,
        CancellationToken cancellationToken)
    {
        var allowedSenders = BuildAllowedSenders(target, botUsername);
        var botUserId = TryGetBotUserId(target);

        try
        {
            return await _accountTelegramTools.WaitForBotVerificationMessageAsync(
                accountId,
                target,
                sentMessageId,
                currentUsername,
                timeoutSeconds,
                messageFilter: update => IsLikelyBotReplyUpdate(update, sentMessageId, botUserId, botUsername, waitStartedAtUtc),
                allowedSenderUsernames: allowedSenders,
                restrictToAllowedUsernames: false,
                stopOnUnmatchedMention: false,
                cancellationToken: cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Live reply wait failed: accountId={AccountId}", accountId);
            return (false, ex.Message, null);
        }
    }

    private async Task<TelegramVerificationMessageCandidate?> WaitForHistoryReplyUntilAsync(
        int accountId,
        AccountTelegramToolsService.ResolvedChatTarget target,
        int sentMessageId,
        string? currentUsername,
        DateTimeOffset waitStartedAtUtc,
        string? botUsername,
        int timeoutSeconds,
        CancellationToken cancellationToken)
    {
        var deadlineUtc = waitStartedAtUtc.AddSeconds(Math.Max(timeoutSeconds, 3));
        while (DateTimeOffset.UtcNow <= deadlineUtc)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var candidate = await TryReadRecentBotReplyFromHistoryAsync(
                accountId,
                target,
                sentMessageId,
                currentUsername,
                waitStartedAtUtc,
                botUsername,
                cancellationToken);

            if (candidate != null)
                return candidate;

            var remaining = deadlineUtc - DateTimeOffset.UtcNow;
            if (remaining <= TimeSpan.Zero)
                break;

            await Task.Delay(
                remaining < TimeSpan.FromMilliseconds(1200) ? remaining : TimeSpan.FromMilliseconds(1200),
                cancellationToken);
        }

        return await TryReadRecentBotReplyFromHistoryAsync(
            accountId,
            target,
            sentMessageId,
            currentUsername,
            waitStartedAtUtc,
            botUsername,
            cancellationToken);
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
            var history = await client.Messages_GetHistory(target.Peer, limit: 20);
            var botUserId = TryGetBotUserId(target);

            foreach (var message in history.Messages.OfType<Message>().OrderBy(x => x.id))
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (message.id <= sentMessageId)
                    continue;

                if (message.Date.ToUniversalTime() < waitStartedAtUtc.UtcDateTime.AddSeconds(-3))
                    continue;

                if (!IsLikelyBotReplyMessage(message, sentMessageId, botUserId, botUsername))
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

    private static bool IsLikelyBotReplyUpdate(
        TelegramAccountMessageUpdate update,
        int sentMessageId,
        long botUserId,
        string? botUsername,
        DateTimeOffset waitStartedAtUtc)
    {
        if (update.Message.id <= sentMessageId)
            return false;

        if (update.ReceivedAtUtc < waitStartedAtUtc.AddSeconds(-3))
            return false;

        if (!HasUsefulReplyContent(update.Text, update.Buttons.Count, update.HasVisualMedia))
            return false;

        if (botUserId > 0 && update.SenderUserId.HasValue && update.SenderUserId.Value == botUserId)
            return true;

        if (!string.IsNullOrWhiteSpace(botUsername))
        {
            var normalizedBot = botUsername.Trim().TrimStart('@');
            if (string.Equals((update.SenderUsername ?? string.Empty).Trim().TrimStart('@'), normalizedBot, StringComparison.OrdinalIgnoreCase)
                || string.Equals((update.SenderChatUsername ?? string.Empty).Trim().TrimStart('@'), normalizedBot, StringComparison.OrdinalIgnoreCase)
                || string.Equals((update.SenderPostAuthor ?? string.Empty).Trim().TrimStart('@'), normalizedBot, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        if (update.SenderIsBot)
            return true;

        if (!update.SenderUserId.HasValue && !update.SenderChatId.HasValue)
            return true;

        return update.ReplyToMessageId == sentMessageId;
    }

    private static bool IsLikelyBotReplyMessage(
        Message message,
        int sentMessageId,
        long botUserId,
        string? botUsername)
    {
        if (message.id <= sentMessageId)
            return false;

        var text = (message.message ?? string.Empty).Trim();
        var buttons = ExtractInlineButtons(message);
        if (!HasUsefulReplyContent(text, buttons.Count, message.media != null))
            return false;

        if (botUserId > 0 && message.from_id is PeerUser peerUser && peerUser.user_id == botUserId)
            return true;

        if (!string.IsNullOrWhiteSpace(botUsername)
            && message.from_id is PeerUser
            && string.Equals(botUsername.TrimStart('@'), ExtractPostAuthorOrUsername(message), StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (message.reply_to is MessageReplyHeader replyHeader && replyHeader.reply_to_msg_id == sentMessageId)
            return true;

        return message.from_id is null || message.from_id is PeerUser;
    }

    private static bool HasUsefulReplyContent(string? text, int buttonCount, bool hasVisualMedia)
    {
        return !string.IsNullOrWhiteSpace(text) || buttonCount > 0 || hasVisualMedia;
    }

    private static async Task<int> TryGetLatestMessageIdAsync(
        Client client,
        AccountTelegramToolsService.ResolvedChatTarget target,
        CancellationToken cancellationToken)
    {
        try
        {
            var history = await client.Messages_GetHistory(target.Peer, limit: 1);
            return history.Messages
                .OfType<Message>()
                .Select(x => x.id)
                .DefaultIfEmpty(0)
                .Max();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return 0;
        }
    }

    private static async Task<bool> TryInvokePeerMethodAsync(
        Client client,
        string methodName,
        InputPeer peer,
        int? maxMessageId,
        CancellationToken cancellationToken)
    {
        var methods = typeof(Client)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Where(x => string.Equals(x.Name, methodName, StringComparison.Ordinal))
            .ToList();

        foreach (var method in methods)
        {
            var parameters = method.GetParameters();
            var args = new object?[parameters.Length];
            var valid = true;
            for (var i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                if (typeof(InputPeer).IsAssignableFrom(parameter.ParameterType))
                {
                    args[i] = peer;
                }
                else if ((parameter.ParameterType == typeof(int) || parameter.ParameterType == typeof(int?)) && maxMessageId.HasValue)
                {
                    args[i] = maxMessageId.Value;
                }
                else if (parameter.ParameterType == typeof(CancellationToken))
                {
                    args[i] = cancellationToken;
                }
                else if (parameter.HasDefaultValue)
                {
                    args[i] = parameter.DefaultValue;
                }
                else if (Nullable.GetUnderlyingType(parameter.ParameterType) != null)
                {
                    args[i] = null;
                }
                else
                {
                    valid = false;
                    break;
                }
            }

            if (!valid)
                continue;

            var result = method.Invoke(client, args);
            if (result is Task task)
                await task;
            return true;
        }

        return false;
    }

    private static async Task<bool> IsChatMarkedAsReadAsync(
        Client client,
        AccountTelegramToolsService.ResolvedChatTarget target,
        int readUpToMessageId,
        CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(250, cancellationToken);
            var dialogs = await client.Messages_GetAllDialogs();
            foreach (var dialog in dialogs.Dialogs)
            {
                var peer = GetMemberValue(dialog, "peer", "Peer");
                if (!IsSamePeerObject(peer, target.Peer))
                    continue;

                var unreadCount = GetIntMemberValue(dialog, "unread_count", "UnreadCount");
                var unreadMentions = GetIntMemberValue(dialog, "unread_mentions_count", "UnreadMentionsCount");
                var topMessageId = GetIntMemberValue(dialog, "top_message", "TopMessage");
                var readInboxMaxId = GetIntMemberValue(dialog, "read_inbox_max_id", "ReadInboxMaxId");

                if (unreadCount <= 0 && unreadMentions <= 0)
                    return true;

                if (topMessageId > 0 && readInboxMaxId > 0 && readInboxMaxId >= topMessageId)
                    return true;

                return topMessageId > 0 && topMessageId <= readUpToMessageId && unreadCount <= 0;
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            // ignore verification failure
        }

        return false;
    }

    private static bool IsSamePeerObject(object? left, object? right)
    {
        if (left == null || right == null)
            return false;

        return TryReadPeerIdentity(left, out var leftKind, out var leftId)
               && TryReadPeerIdentity(right, out var rightKind, out var rightId)
               && string.Equals(leftKind, rightKind, StringComparison.Ordinal)
               && leftId == rightId;
    }

    private static bool TryReadPeerIdentity(object peer, out string kind, out long id)
    {
        kind = string.Empty;
        id = 0;

        switch (peer)
        {
            case PeerUser user:
                kind = nameof(PeerUser);
                id = user.user_id;
                return true;
            case PeerChat chat:
                kind = nameof(PeerChat);
                id = chat.chat_id;
                return true;
            case PeerChannel channel:
                kind = nameof(PeerChannel);
                id = channel.channel_id;
                return true;
        }

        var type = peer.GetType();
        if (TryGetMemberValue(type, peer, out id, "user_id", "UserId"))
        {
            kind = nameof(PeerUser);
            return true;
        }

        if (TryGetMemberValue(type, peer, out id, "chat_id", "ChatId"))
        {
            kind = nameof(PeerChat);
            return true;
        }

        if (TryGetMemberValue(type, peer, out id, "channel_id", "ChannelId"))
        {
            kind = nameof(PeerChannel);
            return true;
        }

        return false;
    }

    private static object? GetMemberValue(object instance, params string[] names)
    {
        var type = instance.GetType();
        foreach (var name in names)
        {
            var property = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
            if (property != null)
                return property.GetValue(instance);

            var field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
            if (field != null)
                return field.GetValue(instance);
        }

        return null;
    }

    private static int GetIntMemberValue(object instance, params string[] names)
    {
        var value = GetMemberValue(instance, names);
        return value switch
        {
            int number => number,
            long number => (int)number,
            short number => number,
            _ => 0
        };
    }

    private static bool TryGetMemberValue(Type type, object instance, out long value, params string[] names)
    {
        value = 0;
        foreach (var name in names)
        {
            var property = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
            if (property != null)
            {
                var raw = property.GetValue(instance);
                if (TryConvertToInt64(raw, out value))
                    return true;
            }

            var field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
            if (field != null)
            {
                var raw = field.GetValue(instance);
                if (TryConvertToInt64(raw, out value))
                    return true;
            }
        }

        return false;
    }

    private static bool TryConvertToInt64(object? value, out long result)
    {
        result = 0;
        switch (value)
        {
            case long longValue:
                result = longValue;
                return true;
            case int intValue:
                result = intValue;
                return true;
            case short shortValue:
                result = shortValue;
                return true;
            case byte byteValue:
                result = byteValue;
                return true;
            default:
                return false;
        }
    }

    private async Task<Client> GetOrCreateConnectedClientAsync(int accountId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var existing = _clientPool.GetClient(accountId);
        if (existing?.User != null)
            return existing;

        var account = await _accountManagement.GetAccountAsync(accountId)
            ?? throw new InvalidOperationException($"Account does not exist: {accountId}");

        var apiId = ResolveApiId(account);
        var apiHash = ResolveApiHash(account);
        var sessionKey = !string.IsNullOrWhiteSpace(account.ApiHash) ? account.ApiHash.Trim() : apiHash.Trim();

        if (string.IsNullOrWhiteSpace(account.SessionPath))
            throw new InvalidOperationException("The account is missing SessionPath and cannot connect to Telegram.");

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
            throw new InvalidOperationException("The account is not logged in or the session is invalid. Please log in again.");

        return client;
    }

    private int ResolveApiId(Account account)
    {
        if (int.TryParse(_configuration["Telegram:ApiId"], out var globalApiId) && globalApiId > 0)
            return globalApiId;
        if (account.ApiId > 0)
            return account.ApiId;
        throw new InvalidOperationException("Global ApiId is not configured and the account ApiId is missing.");
    }

    private string ResolveApiHash(Account account)
    {
        var global = _configuration["Telegram:ApiHash"];
        if (!string.IsNullOrWhiteSpace(global))
            return global.Trim();
        if (!string.IsNullOrWhiteSpace(account.ApiHash))
            return account.ApiHash.Trim();
        throw new InvalidOperationException("Global ApiHash is not configured and the account ApiHash is missing.");
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
            throw new InvalidOperationException("Bot username or link cannot be empty.");

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
