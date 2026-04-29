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

        await host.UpdateProgressAsync(0, 0, cancellationToken);

        for (var accountIndex = 0; accountIndex < selectedAccounts.Count; accountIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!await host.IsStillRunningAsync(cancellationToken))
                return;

            var account = selectedAccounts[accountIndex];
            var accountHadSuccessfulSend = false;

            for (var stepIndex = 0; stepIndex < messages.Count; stepIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!await host.IsStillRunningAsync(cancellationToken))
                    return;

                var stepError = await ExecuteAccountMessageAsync(
                    account,
                    messages[stepIndex],
                    stepIndex,
                    config,
                    accountTelegramTools,
                    botCompat,
                    resolvedTargets,
                    resolvedBotUsernames,
                    cancellationToken);

                if (stepError == null)
                    accountHadSuccessfulSend = true;
                else
                    failed++;

                completed++;
                await host.UpdateProgressAsync(completed, failed, cancellationToken);
            }

            if (accountHadSuccessfulSend)
                successfulAccountIds.Add(account.Id);

            if (accountIndex < selectedAccounts.Count - 1)
            {
                var delaySeconds = config.GetNextAccountDelaySeconds();
                if (delaySeconds > 0 && !await DelayWithHostCheckAsync(host, delaySeconds, cancellationToken))
                    return;
            }
        }

        if (successfulAccountIds.Count > 0)
            await presetStore.RememberBotAccountUsageAsync(config.BotTarget, successfulAccountIds, cancellationToken);

        logger.LogInformation(
            "Bot check-in module task completed. taskId={TaskId}, completed={Completed}, failed={Failed}",
            host.TaskId,
            completed,
            failed);
    }

    private static async Task<string?> ExecuteAccountMessageAsync(
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
        if (!resolvedTargets.TryGetValue(account.Id, out var target))
        {
            var resolve = await botCompat.ResolveExternalBotTargetAsync(
                account.Id,
                config.BotTarget,
                cancellationToken,
                assumeBotUsername: true);

            if (!resolve.Success || resolve.Target == null)
                return resolve.Error ?? "Resolve bot failed.";

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
                    return start.Error ?? "Send /start with parameter failed.";
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
                    return plainStart.Error ?? "Send /start failed.";
            }
        }

        var send = await accountTelegramTools.SendMessageToResolvedChatAsync(
            account.Id,
            target,
            normalizedMessage,
            replyToMessageId: null,
            cancellationToken: cancellationToken);

        if (!send.Success || !send.MessageId.HasValue)
            return send.Error ?? "Send message failed.";

        var wait = await botCompat.WaitForBotReplyAsync(
            account.Id,
            target,
            send.MessageId.Value,
            account.Username,
            config.WaitTimeoutSeconds,
            resolvedBotUsernames.GetValueOrDefault(account.Id),
            config.MarkRepliesAsRead,
            cancellationToken);

        return wait.Success ? null : (wait.Error ?? "Bot reply was not captured.");
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
}
