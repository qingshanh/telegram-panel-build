using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TelegramPanel.Core.Services;
using TelegramPanel.Core.Services.Telegram;
using TelegramPanel.Data.Entities;
using TelegramPanel.Modules;

namespace TelegramPanel.Module.BotCheckinAssistant.Services;

public sealed class BotCheckinModuleTaskHandler : IModuleTaskHandler
{
    public string TaskType => BotCheckinModuleTaskConstants.TaskType;

    public async Task ExecuteAsync(IModuleTaskExecutionHost host, CancellationToken cancellationToken)
    {
        var logger = host.Services.GetRequiredService<ILogger<BotCheckinModuleTaskHandler>>();
        var accountManagement = host.Services.GetRequiredService<AccountManagementService>();
        var accountTelegramTools = host.Services.GetRequiredService<AccountTelegramToolsService>();
        var botCompat = host.Services.GetRequiredService<BotCheckinTelegramCompatService>();
        var presetStore = host.Services.GetRequiredService<BotCheckinAssistantPresetStore>();
        var taskManagement = host.Services.GetRequiredService<BatchTaskManagementService>();

        var config = BotCheckinTaskConfig.Deserialize(host.Config);
        config.AutoContinueAllSteps = true;

        var validationError = config.Validate();
        if (!string.IsNullOrWhiteSpace(validationError))
            throw new InvalidOperationException(validationError);

        var messages = config.GetMessages();
        var selectedAccountIds = config.SelectedAccountIds.ToHashSet();
        var selectedAccounts = (await accountManagement.GetActiveAccountsAsync())
            .Where(x => selectedAccountIds.Contains(x.Id))
            .OrderBy(x => x.Id)
            .ToList();

        if (selectedAccounts.Count == 0)
            throw new InvalidOperationException("No runnable selected accounts were found.");

        await presetStore.RememberCommandsAsync(messages, cancellationToken);

        var resolvedTargets = new Dictionary<int, AccountTelegramToolsService.ResolvedChatTarget>();
        var resolvedBotUsernames = new Dictionary<int, string>();
        var successfulAccountIds = new List<int>();
        var completed = 0;
        var failed = 0;
        var runLog = new BotCheckinTaskRunLog
        {
            StartedAtUtc = DateTimeOffset.UtcNow,
            TotalAccounts = selectedAccounts.Count,
            TotalMessages = messages.Count,
            TotalOperations = selectedAccounts.Count * messages.Count,
            Summary = "\u4EFB\u52A1\u5F00\u59CB\u6267\u884C\u3002"
        };

        await host.UpdateProgressAsync(0, 0, cancellationToken);

        try
        {
            for (var accountIndex = 0; accountIndex < selectedAccounts.Count; accountIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!await host.IsStillRunningAsync(cancellationToken))
                {
                    await PersistStoppedRunLogAsync(taskManagement, host.TaskId, config, runLog, completed, failed, "\u4EFB\u52A1\u5DF2\u88AB\u505C\u6B62\u3002");
                    return;
                }

                var account = selectedAccounts[accountIndex];
                var accountHadSuccessfulSend = false;

                for (var stepIndex = 0; stepIndex < messages.Count; stepIndex++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (!await host.IsStillRunningAsync(cancellationToken))
                    {
                        await PersistStoppedRunLogAsync(taskManagement, host.TaskId, config, runLog, completed, failed, "\u4EFB\u52A1\u5DF2\u88AB\u505C\u6B62\u3002");
                        return;
                    }

                    var stepResult = await ExecuteAccountMessageAsync(
                        account,
                        messages[stepIndex],
                        stepIndex,
                        config,
                        accountTelegramTools,
                        botCompat,
                        resolvedTargets,
                        resolvedBotUsernames,
                        cancellationToken);

                    runLog.Entries.Add(new BotCheckinTaskRunLogEntry
                    {
                        AccountId = account.Id,
                        AccountLabel = BuildAccountLabel(account),
                        StepNumber = stepIndex + 1,
                        Message = messages[stepIndex],
                        SendSuccess = stepResult.SendSuccess,
                        ReplyCaptured = stepResult.ReplyCaptured,
                        ReplyMarkedAsRead = stepResult.MarkedAsRead,
                        ReplyPreview = Shorten(stepResult.ReplyPreview, 160),
                        Error = stepResult.Error,
                        ExecutedAtUtc = DateTimeOffset.UtcNow
                    });

                    if (stepResult.Error == null)
                        accountHadSuccessfulSend = true;
                    else
                        failed++;

                    completed++;
                    runLog.Completed = completed;
                    runLog.Failed = failed;
                    await host.UpdateProgressAsync(completed, failed, cancellationToken);
                }

                if (accountHadSuccessfulSend)
                    successfulAccountIds.Add(account.Id);

                if (accountIndex < selectedAccounts.Count - 1)
                {
                    var delaySeconds = config.GetNextAccountDelaySeconds();
                    if (delaySeconds > 0 && !await DelayWithHostCheckAsync(host, delaySeconds, cancellationToken))
                    {
                        await PersistStoppedRunLogAsync(taskManagement, host.TaskId, config, runLog, completed, failed, "\u4EFB\u52A1\u5728\u5EF6\u8FDF\u7B49\u5F85\u9636\u6BB5\u88AB\u505C\u6B62\u3002");
                        return;
                    }
                }
            }

            if (successfulAccountIds.Count > 0)
                await presetStore.RememberBotAccountUsageAsync(config.BotTarget, successfulAccountIds, cancellationToken);

            runLog.Completed = completed;
            runLog.Failed = failed;
            runLog.Summary = $"\u6267\u884C\u5B8C\u6210\u3002\u6210\u529F\u8FDB\u5EA6 {completed - failed}/{completed}\uFF0C\u5931\u8D25 {failed}\u3002";
            runLog.FinishedAtUtc = DateTimeOffset.UtcNow;
            await PersistRunLogAsync(taskManagement, host.TaskId, config, runLog);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            runLog.Canceled = true;
            runLog.Summary = "\u4EFB\u52A1\u5DF2\u53D6\u6D88\u3002";
            runLog.Completed = completed;
            runLog.Failed = failed;
            runLog.FinishedAtUtc = DateTimeOffset.UtcNow;
            await PersistRunLogAsync(taskManagement, host.TaskId, config, runLog);
            throw;
        }
        catch (Exception ex)
        {
            runLog.Error = ex.Message;
            runLog.Summary = "\u4EFB\u52A1\u6267\u884C\u5931\u8D25\u3002";
            runLog.Completed = completed;
            runLog.Failed = failed;
            runLog.FinishedAtUtc = DateTimeOffset.UtcNow;
            await PersistRunLogAsync(taskManagement, host.TaskId, config, runLog);
            throw;
        }

        logger.LogInformation(
            "Bot check-in module task completed. taskId={TaskId}, completed={Completed}, failed={Failed}",
            host.TaskId,
            completed,
            failed);
    }

    private static async Task<AccountMessageExecutionResult> ExecuteAccountMessageAsync(
        Account account,
        string message,
        int stepIndex,
        BotCheckinTaskConfig config,
        AccountTelegramToolsService accountTelegramTools,
        BotCheckinTelegramCompatService botCompat,
        IDictionary<int, AccountTelegramToolsService.ResolvedChatTarget> resolvedTargets,
        IDictionary<int, string> resolvedBotUsernames,
        CancellationToken cancellationToken)
    {
        var result = new AccountMessageExecutionResult();

        if (!resolvedTargets.TryGetValue(account.Id, out var target))
        {
            var resolve = await botCompat.ResolveExternalBotTargetAsync(
                account.Id,
                config.BotTarget,
                cancellationToken,
                assumeBotUsername: true);

            if (!resolve.Success || resolve.Target == null)
            {
                result.Error = resolve.Error ?? "Resolve bot failed.";
                return result;
            }

            target = resolve.Target;
            resolvedTargets[account.Id] = target;
            if (!string.IsNullOrWhiteSpace(resolve.BotUsername))
                resolvedBotUsernames[account.Id] = resolve.BotUsername;
        }

        var normalizedMessage = (message ?? string.Empty).Trim();
        if (stepIndex == 0
            && config.AutoStartBeforeFirstMessage
            && !string.Equals(normalizedMessage, "/start", StringComparison.OrdinalIgnoreCase))
        {
            if (!string.IsNullOrWhiteSpace(config.StartParameter))
            {
                var start = await accountTelegramTools.StartExternalBotAsync(
                    account.Id,
                    config.BotTarget,
                    config.StartParameter,
                    cancellationToken,
                    assumeBotUsername: true);

                if (!start.Success)
                {
                    result.Error = start.Error ?? "Send /start with parameter failed.";
                    return result;
                }
            }
            else
            {
                var plainStart = await accountTelegramTools.SendMessageToResolvedChatAsync(
                    account.Id,
                    target,
                    "/start",
                    replyToMessageId: null,
                    cancellationToken: cancellationToken);

                if (!plainStart.Success)
                {
                    result.Error = plainStart.Error ?? "Send /start failed.";
                    return result;
                }
            }
        }

        var send = await accountTelegramTools.SendMessageToResolvedChatAsync(
            account.Id,
            target,
            normalizedMessage,
            replyToMessageId: null,
            cancellationToken: cancellationToken);

        result.SendSuccess = send.Success;
        if (!send.Success || !send.MessageId.HasValue)
        {
            result.Error = send.Error ?? "Send message failed.";
            return result;
        }

        var wait = await botCompat.WaitForBotReplyAsync(
            account.Id,
            target,
            send.MessageId.Value,
            account.Username,
            config.WaitTimeoutSeconds,
            resolvedBotUsernames.TryGetValue(account.Id, out var botUsername) ? botUsername : null,
            config.MarkRepliesAsRead,
            cancellationToken);

        if (!wait.Success || wait.Candidate == null)
        {
            result.Error = wait.Error ?? "Bot reply was not captured.";
            return result;
        }

        result.ReplyCaptured = true;
        result.MarkedAsRead = wait.MarkedAsRead;
        result.ReplyPreview = wait.Candidate.Text ?? wait.Candidate.Caption ?? string.Empty;
        return result;
    }

    private static async Task<bool> DelayWithHostCheckAsync(
        IModuleTaskExecutionHost host,
        int delaySeconds,
        CancellationToken cancellationToken)
    {
        var remaining = TimeSpan.FromSeconds(Math.Max(0, delaySeconds));
        while (remaining > TimeSpan.Zero)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!await host.IsStillRunningAsync(cancellationToken))
                return false;

            var slice = remaining > TimeSpan.FromSeconds(1) ? TimeSpan.FromSeconds(1) : remaining;
            await Task.Delay(slice, cancellationToken);
            remaining -= slice;
        }

        return true;
    }

    private static async Task PersistStoppedRunLogAsync(
        BatchTaskManagementService taskManagement,
        int taskId,
        BotCheckinTaskConfig config,
        BotCheckinTaskRunLog runLog,
        int completed,
        int failed,
        string summary)
    {
        runLog.Canceled = true;
        runLog.Summary = summary;
        runLog.Completed = completed;
        runLog.Failed = failed;
        runLog.FinishedAtUtc = DateTimeOffset.UtcNow;
        await PersistRunLogAsync(taskManagement, taskId, config, runLog);
    }

    private static async Task PersistRunLogAsync(
        BatchTaskManagementService taskManagement,
        int taskId,
        BotCheckinTaskConfig config,
        BotCheckinTaskRunLog runLog)
    {
        config.LastRunLog = runLog;
        await taskManagement.UpdateTaskConfigAsync(taskId, config.Serialize());
    }

    private static string BuildAccountLabel(Account account)
    {
        if (!string.IsNullOrWhiteSpace(account.Username))
            return $"#{account.Id} @{account.Username}";

        if (!string.IsNullOrWhiteSpace(account.Nickname))
            return $"#{account.Id} {account.Nickname}";

        if (!string.IsNullOrWhiteSpace(account.DisplayPhone))
            return $"#{account.Id} {account.DisplayPhone}";

        return $"#{account.Id}";
    }

    private static string? Shorten(string? text, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        var normalized = text.Replace("\r", " ", StringComparison.Ordinal).Replace("\n", " ", StringComparison.Ordinal).Trim();
        if (normalized.Length <= maxLength)
            return normalized;

        return normalized[..Math.Max(0, maxLength - 3)] + "...";
    }

    private sealed class AccountMessageExecutionResult
    {
        public bool SendSuccess { get; set; }
        public bool ReplyCaptured { get; set; }
        public bool MarkedAsRead { get; set; }
        public string? ReplyPreview { get; set; }
        public string? Error { get; set; }
    }
}
