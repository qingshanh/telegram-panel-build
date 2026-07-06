using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Net.Mail;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using TelegramPanel.Core.Interfaces;
using TelegramPanel.Core.Models;
using TelegramPanel.Data.Entities;
using TL;
using WTelegram;

namespace TelegramPanel.Core.Services.Telegram;

/// <summary>
/// 璐﹀彿璇婃柇 / 绯荤粺閫氱煡 / 鍦ㄧ嚎璁惧绠＄悊
/// </summary>
public class AccountTelegramToolsService
{
    private const long TelegramSystemUserId = 777000;

    private readonly AccountManagementService _accountManagement;
    private readonly ITelegramClientPool _clientPool;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AccountTelegramToolsService> _logger;
    private readonly TelegramAccountUpdateHub _updateHub;

    public AccountTelegramToolsService(
        AccountManagementService accountManagement,
        ITelegramClientPool clientPool,
        IConfiguration configuration,
        ILogger<AccountTelegramToolsService> logger,
        TelegramAccountUpdateHub updateHub)
    {
        _accountManagement = accountManagement;
        _clientPool = clientPool;
        _configuration = configuration;
        _logger = logger;
        _updateHub = updateHub;
    }

    /// <summary>
    /// 鍒锋柊璐﹀彿鐘舵€侊紙鍙€夋繁搴︽帰娴嬶細妫€娴嬧€滃垱寤洪閬撴帴鍙ｆ槸鍚﹁鍐荤粨鈥濓紝浼氬垱寤哄苟鍒犻櫎涓€涓祴璇曢閬擄級
    /// </summary>
    public async Task<TelegramAccountStatusResult> RefreshAccountStatusAsync(int accountId, bool probeCreateChannel = false, CancellationToken cancellationToken = default)
    {
        var checkedAt = DateTime.UtcNow;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var client = await GetOrCreateConnectedClientAsync(accountId, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            var users = await ExecuteTelegramRequestAsync(
                accountId,
                "鎷夊彇璐﹀彿璧勬枡",
                () => client.Users_GetUsers(InputUser.Self),
                cancellationToken,
                resetClientOnTimeout: true);
            cancellationToken.ThrowIfCancellationRequested();
            var self = users.OfType<User>().FirstOrDefault();

            if (self == null)
            {
                var missingProfile = new TelegramAccountStatusResult(
                    Ok: false,
                    Summary: "鏃犳硶鑾峰彇璐﹀彿璧勬枡",
                    Details: "Users_GetUsers(Self) 鏈繑鍥?User",
                    CheckedAtUtc: checkedAt);
                await TryPersistStatusAsync(accountId, missingProfile, cancellationToken: cancellationToken);
                return missingProfile;
            }

            var profile = new TelegramAccountProfile(
                UserId: self.id,
                Phone: self.phone,
                Username: self.MainUsername,
                FirstName: self.first_name,
                LastName: self.last_name,
                IsDeleted: self.flags.HasFlag(User.Flags.deleted),
                IsScam: self.flags.HasFlag(User.Flags.scam),
                IsFake: self.flags.HasFlag(User.Flags.fake),
                IsRestricted: self.flags.HasFlag(User.Flags.restricted),
                IsVerified: self.flags.HasFlag(User.Flags.verified),
                IsPremium: self.flags.HasFlag(User.Flags.premium)
            );

            var account = await _accountManagement.GetAccountAsync(accountId);
            if (account != null)
            {
                profile.ApplyTo(account);
                await TryPopulateEstimatedRegistrationAsync(account, client, accountId, cancellationToken);
            }

            var summary = "姝ｅ父";
            if (profile.IsDeleted)
                summary = "璐﹀彿宸叉敞閿€/琚垹闄?;
            else if (profile.IsRestricted)
                summary = "璐﹀彿鍙楅檺锛圧estricted锛?;

            if (probeCreateChannel)
            {
                var probe = await ProbeCreateChannelCapabilityAsync(client, accountId, cancellationToken);
                if (probe.IsFrozen)
                {
                    var frozen = new TelegramAccountStatusResult(
                        Ok: false,
                        Summary: "璐﹀彿琚喕缁擄紙鍒涘缓棰戦亾鎺ュ彛鍙楅檺锛?,
                        Details: $"鍒涘缓棰戦亾鎺㈡祴锛歿probe.Message}{Environment.NewLine}{BuildProfileDetails(profile)}",
                        CheckedAtUtc: checkedAt,
                        Profile: profile);
                    await TryPersistStatusAsync(accountId, frozen, account, persistProfile: true, cancellationToken: cancellationToken);
                    return frozen;
                }

                if (!probe.Success)
                {
                    var failed = new TelegramAccountStatusResult(
                        Ok: false,
                        Summary: "鍒涘缓棰戦亾鎺㈡祴澶辫触",
                        Details: $"鍒涘缓棰戦亾鎺㈡祴锛歿probe.Message}{Environment.NewLine}{BuildProfileDetails(profile)}",
                        CheckedAtUtc: checkedAt,
                        Profile: profile);
                    await TryPersistStatusAsync(accountId, failed, account, persistProfile: true, cancellationToken: cancellationToken);
                    return failed;
                }

                // 鎺㈡祴鎴愬姛锛屼笉褰卞搷鍘熺姸鎬侊紝浠呰ˉ鍏呰鎯?                var okWithProbe = new TelegramAccountStatusResult(
                    Ok: true,
                    Summary: summary,
                    Details: $"鍒涘缓棰戦亾鎺㈡祴锛氬彲鐢紙宸茶嚜鍔ㄦ竻鐞嗘祴璇曢閬擄級{Environment.NewLine}{BuildProfileDetails(profile)}",
                    CheckedAtUtc: checkedAt,
                    Profile: profile);
                await TryPersistStatusAsync(accountId, okWithProbe, account, persistProfile: true, cancellationToken: cancellationToken);
                return okWithProbe;
            }

            var ok = new TelegramAccountStatusResult(
                Ok: true,
                Summary: summary,
                Details: BuildProfileDetails(profile),
                CheckedAtUtc: checkedAt,
                Profile: profile);
            await TryPersistStatusAsync(accountId, ok, account, persistProfile: true, cancellationToken: cancellationToken);
            return ok;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return new TelegramAccountStatusResult(
                Ok: false,
                Summary: "宸插彇娑?,
                Details: "鎿嶄綔宸插彇娑堬紙椤甸潰鍏抽棴/鍒锋柊瀵艰嚧鍙栨秷锛?,
                CheckedAtUtc: checkedAt);
        }
        catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested)
        {
            // Blazor 椤甸潰鍒锋柊/鏂繛鏃讹紝Scoped 鐨?DbContext 鍙兘宸茶閲婃斁锛涙妸瀹冭涓哄彇娑堣€屼笉鏄敊璇€?            return new TelegramAccountStatusResult(
                Ok: false,
                Summary: "宸插彇娑?,
                Details: "椤甸潰宸插叧闂?鍒锋柊锛屾搷浣滆涓柇",
                CheckedAtUtc: checkedAt);
        }
        catch (Exception ex)
        {
            var (summary, details) = MapTelegramException(ex);
            _logger.LogWarning(ex, "RefreshAccountStatus failed for account {AccountId}", accountId);
            var failed = new TelegramAccountStatusResult(
                Ok: false,
                Summary: summary,
                Details: details,
                CheckedAtUtc: checkedAt);
            await TryPersistStatusAsync(accountId, failed, cancellationToken: cancellationToken);
            return failed;
        }
    }

    private async Task TryPersistStatusAsync(
        int accountId,
        TelegramAccountStatusResult result,
        Account? account = null,
        bool persistProfile = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (cancellationToken.IsCancellationRequested)
                return;

            account ??= await _accountManagement.GetAccountAsync(accountId);
            if (account == null)
                return;

            if (persistProfile && result.Profile != null)
                result.Profile.ApplyTo(account);

            account.TelegramStatusOk = result.Ok;
            account.TelegramStatusSummary = result.Summary;
            account.TelegramStatusDetails = result.Details;
            account.TelegramStatusCheckedAtUtc = result.CheckedAtUtc;

            await _accountManagement.UpdateAccountAsync(account);
        }
        catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested)
        {
            // 椤甸潰/浣滅敤鍩熷凡閿€姣佸鑷寸殑 DbContext 閲婃斁锛屽拷鐣ュ嵆鍙?        }
        catch (Exception ex)
        {
            // 鍙栨秷鍦烘櫙涓嶉渶瑕佸櫔澹版棩蹇?            if (!cancellationToken.IsCancellationRequested)
                _logger.LogWarning(ex, "Failed to persist Telegram status cache for account {AccountId}", accountId);
        }
    }

    public async Task<IReadOnlyList<TelegramSystemMessage>> GetLatestSystemMessagesAsync(int accountId, int limit = 20)
    {
        if (limit <= 0) limit = 20;
        if (limit > 100) limit = 100;

        var client = await GetOrCreateConnectedClientAsync(accountId);
        var peer = await TryResolveSystemPeerAsync(client);
        if (peer == null)
            return Array.Empty<TelegramSystemMessage>();

        var history = await client.Messages_GetHistory(peer, limit: limit);
        var list = new List<TelegramSystemMessage>(history.Messages.Length);
        foreach (var msgBase in history.Messages)
        {
            if (msgBase is not Message m)
                continue;

            var text = m.message ?? string.Empty;
            if (string.IsNullOrWhiteSpace(text))
                continue;

            list.Add(new TelegramSystemMessage(
                Id: m.id,
                DateUtc: m.Date.ToUniversalTime(),
                Text: text.Trim()
            ));
        }

        return list
            .OrderByDescending(x => x.DateUtc ?? DateTime.MinValue)
            .Take(limit)
            .ToList();
    }

    public async Task EnsureEstimatedRegistrationAsync(int accountId, CancellationToken cancellationToken = default)
    {
        try
        {
            var account = await _accountManagement.GetAccountAsync(accountId);
            if (account == null)
                return;

            if (account.EstimatedRegistrationAt.HasValue || account.EstimatedRegistrationCheckedAtUtc.HasValue)
                return;

            var client = await GetOrCreateConnectedClientAsync(accountId, cancellationToken);
            await TryPopulateEstimatedRegistrationAsync(account, client, accountId, cancellationToken);

            if (account.EstimatedRegistrationAt.HasValue || account.EstimatedRegistrationCheckedAtUtc.HasValue)
                await _accountManagement.UpdateAccountAsync(account);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "EnsureEstimatedRegistrationAsync skipped for account {AccountId}", accountId);
        }
    }

    private async Task TryPopulateEstimatedRegistrationAsync(
        Account account,
        Client client,
        int accountId,
        CancellationToken cancellationToken)
    {
        if (account.EstimatedRegistrationAt.HasValue || account.EstimatedRegistrationCheckedAtUtc.HasValue)
            return;

        var (checkedSuccessfully, estimatedAtUtc) = await TryGetEstimatedRegistrationFromSystemMessagesAsync(client, accountId, cancellationToken);
        if (!checkedSuccessfully)
            return;

        if (estimatedAtUtc.HasValue)
            account.EstimatedRegistrationAt = estimatedAtUtc.Value;

        account.EstimatedRegistrationCheckedAtUtc = DateTime.UtcNow;
    }

    private async Task<(bool CheckedSuccessfully, DateTime? EstimatedAtUtc)> TryGetEstimatedRegistrationFromSystemMessagesAsync(
        Client client,
        int accountId,
        CancellationToken cancellationToken)
    {
        try
        {
            var peer = await TryResolveSystemPeerAsync(client);
            if (peer == null)
                return (true, null);

            const int pageSize = 100;
            const int maxPages = 200;
            var offsetId = 0;
            DateTime? earliest = null;

            for (var page = 0; page < maxPages; page++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var history = await ExecuteTelegramRequestAsync(
                    accountId,
                    "璇诲彇 777000 绯荤粺閫氱煡鍘嗗彶",
                    () => client.Messages_GetHistory(peer, offset_id: offsetId, limit: pageSize),
                    cancellationToken,
                    resetClientOnTimeout: true);

                if (history.Messages == null || history.Messages.Length == 0)
                    break;

                foreach (var msgBase in history.Messages)
                {
                    if (msgBase is not Message message)
                        continue;

                    if (string.IsNullOrWhiteSpace(message.message))
                        continue;

                    var messageUtc = message.Date.ToUniversalTime();
                    if (!earliest.HasValue || messageUtc < earliest.Value)
                        earliest = messageUtc;
                }

                var nextOffsetId = history.Messages
                    .Select(GetTelegramMessageId)
                    .Where(id => id > 0)
                    .DefaultIfEmpty(0)
                    .Min();

                if (nextOffsetId <= 0 || nextOffsetId == offsetId || history.Messages.Length < pageSize)
                    break;

                offsetId = nextOffsetId;
            }

            return (true, earliest);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to estimate registration time from 777000 for account {AccountId}", accountId);
            return (false, null);
        }
    }

    private static int GetTelegramMessageId(MessageBase msgBase) => msgBase switch
    {
        Message message => message.id,
        MessageService service => service.id,
        _ => 0
    };

    public async Task<IReadOnlyList<TelegramAuthorizationInfo>> GetAuthorizationsAsync(int accountId)
    {
        var client = await GetOrCreateConnectedClientAsync(accountId);
        var auths = await client.Account_GetAuthorizations();

        var list = new List<TelegramAuthorizationInfo>(auths.authorizations.Length);
        foreach (var a in auths.authorizations)
        {
            list.Add(new TelegramAuthorizationInfo(
                Hash: a.hash,
                Current: a.flags.HasFlag(Authorization.Flags.current),
                ApiId: a.api_id,
                AppName: a.app_name,
                AppVersion: a.app_version,
                DeviceModel: a.device_model,
                Platform: a.platform,
                SystemVersion: a.system_version,
                Ip: a.ip,
                Country: a.country,
                Region: a.region,
                CreatedAtUtc: a.date_created == default ? null : a.date_created.ToUniversalTime(),
                LastActiveAtUtc: a.date_active == default ? null : a.date_active.ToUniversalTime()
            ));
        }

        return list
            .OrderByDescending(x => x.Current)
            .ThenByDescending(x => x.LastActiveAtUtc ?? DateTime.MinValue)
            .ToList();
    }

    /// <summary>
    /// 淇敼 Telegram 涓ゆ楠岃瘉锛堜簩绾у瘑鐮侊級銆?    /// </summary>
    public async Task<(bool Success, string? Error)> ChangeTwoFactorPasswordAsync(
        int accountId,
        string? currentPassword,
        string newPassword,
        string? hint = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(newPassword))
                return (false, "鏂颁簩绾у瘑鐮佷笉鑳戒负绌?);

            currentPassword = (currentPassword ?? string.Empty).Trim();
            newPassword = newPassword.Trim();
            hint = (hint ?? string.Empty).Trim();

            var client = await GetOrCreateConnectedClientAsync(accountId, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            // 鍙傝€?WTelegramClient 瀹樻柟绀轰緥锛欰ccount_UpdatePasswordSettings 闇€瑕?SRP 鏍￠獙鍊硷紙鏃у瘑鐮侊級涓庢柊瀵嗙爜 settings
            var accountPwd = await client.Account_GetPassword();
            cancellationToken.ThrowIfCancellationRequested();

            // 鑻ヨ处鍙峰凡寮€鍚袱姝ラ獙璇佷絾鏈彁渚涙棫瀵嗙爜锛屽垯鐩存帴鎻愮ず
            TL.InputCheckPasswordSRP? oldCheck = null;
            if (accountPwd.current_algo != null)
            {
                if (string.IsNullOrWhiteSpace(currentPassword))
                    return (false, "璇ヨ处鍙峰凡寮€鍚袱姝ラ獙璇侊紝璇峰～鍐欏師浜岀骇瀵嗙爜");

                oldCheck = await WTelegram.Client.InputCheckPassword(accountPwd, currentPassword);
            }

            // 璁?InputCheckPassword 鐢熸垚 new_password_hash锛堥渶瑕佸皢 current_algo 缃┖锛?            accountPwd.current_algo = null;
            var newPasswordHash = await WTelegram.Client.InputCheckPassword(accountPwd, newPassword);

            var settings = new TL.Account_PasswordInputSettings
            {
                flags = TL.Account_PasswordInputSettings.Flags.has_new_algo,
                new_algo = accountPwd.new_algo,
                new_password_hash = newPasswordHash?.A,
                hint = hint
            };

            await client.Account_UpdatePasswordSettings(oldCheck, settings);
            return (true, null);
        }
        catch (Exception ex)
        {
            var (summary, details) = MapTelegramException(ex);
            var msg = string.IsNullOrWhiteSpace(details) ? summary : $"{summary}锛歿details}";
            return (false, msg);
        }
    }

    /// <summary>
    /// 蹇樿浜岀骇瀵嗙爜锛氬悜 Telegram 鍙戣捣鈥滈噸缃袱姝ラ獙璇佸瘑鐮佲€濈敵璇凤紙閫氬父闇€瑕佺瓑寰?7 澶╋級銆?    /// </summary>
    public async Task<(bool Success, string? Error, DateTimeOffset? WaitUntilUtc)> RequestTwoFactorPasswordResetAsync(
        int accountId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var client = await GetOrCreateConnectedClientAsync(accountId, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            var result = await client.Account_ResetPassword();
            cancellationToken.ThrowIfCancellationRequested();

            switch (result)
            {
                case TL.Account_ResetPasswordOk:
                    return (true, "浜岀骇瀵嗙爜宸查噸缃垚鍔燂紙鐜板湪鍙互鐩存帴閲嶆柊璁剧疆浜岀骇瀵嗙爜锛?, null);

                case TL.Account_ResetPasswordRequestedWait wait:
                {
                    var untilUtc = ToUtcDateTimeOffset(wait.until_date);
                    return (true, $"宸叉彁浜ら噸缃敵璇凤紝璇风瓑寰呰嚦 {untilUtc:yyyy-MM-dd HH:mm:ss} UTC 鍚庡啀瀹屾垚閲嶇疆/閲嶆柊璁剧疆浜岀骇瀵嗙爜", untilUtc);
                }

                case TL.Account_ResetPasswordFailedWait failed:
                {
                    var retryUtc = ToUtcDateTimeOffset(failed.retry_date);
                    return (false, $"杩戞湡鏈夎鍙栨秷鐨勯噸缃敵璇凤紝闇€绛夊緟鑷?{retryUtc:yyyy-MM-dd HH:mm:ss} UTC 鍚庢墠鑳藉啀娆＄敵璇?, retryUtc);
                }

                default:
                    return (false, $"鏈煡杩斿洖绫诲瀷锛歿result.GetType().Name}", null);
            }
        }
        catch (Exception ex)
        {
            var (summary, details) = MapTelegramException(ex);
            var msg = string.IsNullOrWhiteSpace(details) ? summary : $"{summary}锛歿details}";
            return (false, msg, null);
        }
    }

    private static DateTimeOffset ToUtcDateTimeOffset(DateTime value)
    {
        if (value.Kind == DateTimeKind.Unspecified)
            value = DateTime.SpecifyKind(value, DateTimeKind.Utc);
        else
            value = value.ToUniversalTime();

        return new DateTimeOffset(value);
    }

    /// <summary>
    /// 鑾峰彇涓ゆ楠岃瘉鎵惧洖閭鐘舵€侊紙鏄惁宸茬粦瀹氥€佹槸鍚﹀瓨鍦ㄥ緟纭鐨勯偖绠憋級銆?    /// </summary>
    public async Task<(bool Success, string? Error, bool HasTwoFactorPassword, bool HasRecoveryEmail, string? UnconfirmedEmailPattern)>
        GetTwoFactorRecoveryEmailStatusAsync(int accountId, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = await GetOrCreateConnectedClientAsync(accountId, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            var pwd = await client.Account_GetPassword();
            var hasPassword = pwd.current_algo != null;
            var hasRecoveryEmail = pwd.flags.HasFlag(TL.Account_Password.Flags.has_recovery);
            var unconfirmed = pwd.flags.HasFlag(TL.Account_Password.Flags.has_email_unconfirmed_pattern)
                ? (pwd.email_unconfirmed_pattern ?? "").Trim()
                : null;

            if (string.IsNullOrWhiteSpace(unconfirmed))
                unconfirmed = null;

            return (true, null, hasPassword, hasRecoveryEmail, unconfirmed);
        }
        catch (Exception ex)
        {
            var (summary, details) = MapTelegramException(ex);
            var msg = string.IsNullOrWhiteSpace(details) ? summary : $"{summary}锛歿details}";
            return (false, msg, false, false, null);
        }
    }

    /// <summary>
    /// 缁戝畾/鎹㈢粦涓ゆ楠岃瘉鎵惧洖閭锛堜細鍙戦€侀獙璇佺爜鍒伴偖绠憋紝闇€璋冪敤 ConfirmTwoFactorRecoveryEmailAsync 纭锛夈€?    /// </summary>
    public async Task<(bool Success, string? Error, string? EmailPattern)> SetTwoFactorRecoveryEmailAsync(
        int accountId,
        string? currentPassword,
        string email,
        CancellationToken cancellationToken = default)
    {
        try
        {
            email = (email ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(email))
                return (false, "閭涓嶈兘涓虹┖", null);

            try
            {
                _ = new MailAddress(email);
            }
            catch
            {
                return (false, "閭鏍煎紡涓嶆纭?, null);
            }

            currentPassword = (currentPassword ?? string.Empty).Trim();

            var client = await GetOrCreateConnectedClientAsync(accountId, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            var pwd = await client.Account_GetPassword();
            cancellationToken.ThrowIfCancellationRequested();

            if (pwd.current_algo == null)
                return (false, "璇ヨ处鍙锋湭寮€鍚袱姝ラ獙璇侊紝鏃犳硶缁戝畾鎵惧洖閭锛岃鍏堣缃簩绾у瘑鐮?, null);

            if (string.IsNullOrWhiteSpace(currentPassword))
                return (false, "璇峰～鍐欏師浜岀骇瀵嗙爜", null);

            var oldCheck = await WTelegram.Client.InputCheckPassword(pwd, currentPassword);

            var settings = new TL.Account_PasswordInputSettings
            {
                flags = TL.Account_PasswordInputSettings.Flags.has_email,
                email = email
            };

            await client.Account_UpdatePasswordSettings(oldCheck, settings);

            // 鏇存柊鍚庡彲閫氳繃 getPassword 鑾峰彇鈥滃緟纭閭鈥濇帺鐮佷俊鎭?            var after = await client.Account_GetPassword();
            var pattern = after.flags.HasFlag(TL.Account_Password.Flags.has_email_unconfirmed_pattern)
                ? (after.email_unconfirmed_pattern ?? "").Trim()
                : null;

            if (string.IsNullOrWhiteSpace(pattern))
                pattern = null;

            return (true, null, pattern);
        }
        catch (Exception ex)
        {
            var (summary, details) = MapTelegramException(ex);
            var msg = string.IsNullOrWhiteSpace(details) ? summary : $"{summary}锛歿details}";
            return (false, msg, null);
        }
    }

    /// <summary>
    /// 纭涓ゆ楠岃瘉鎵惧洖閭楠岃瘉鐮併€?    /// </summary>
    public async Task<(bool Success, string? Error)> ConfirmTwoFactorRecoveryEmailAsync(
        int accountId,
        string code,
        CancellationToken cancellationToken = default)
    {
        try
        {
            code = (code ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(code))
                return (false, "楠岃瘉鐮佷笉鑳戒负绌?);

            var client = await GetOrCreateConnectedClientAsync(accountId, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            _ = await client.Account_ConfirmPasswordEmail(code);
            return (true, null);
        }
        catch (Exception ex)
        {
            var (summary, details) = MapTelegramException(ex);
            var msg = string.IsNullOrWhiteSpace(details) ? summary : $"{summary}锛歿details}";
            return (false, msg);
        }
    }

    /// <summary>
    /// 閲嶅彂涓ゆ楠岃瘉鎵惧洖閭楠岃瘉鐮侊紙闇€瑕佸厛璁剧疆閭锛夈€?    /// </summary>
    public async Task<(bool Success, string? Error, string? EmailPattern, int? CodeLength)> ResendTwoFactorRecoveryEmailAsync(
        int accountId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var client = await GetOrCreateConnectedClientAsync(accountId, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            var ok = await client.Account_ResendPasswordEmail();
            if (!ok)
                return (false, "閲嶅彂澶辫触", null, null);

            var pwd = await client.Account_GetPassword();
            var pattern = pwd.flags.HasFlag(TL.Account_Password.Flags.has_email_unconfirmed_pattern)
                ? (pwd.email_unconfirmed_pattern ?? "").Trim()
                : null;

            if (string.IsNullOrWhiteSpace(pattern))
                pattern = null;

            // 璇?API 涓嶈繑鍥為獙璇佺爜闀垮害锛屼粎杩斿洖閭鎺╃爜淇℃伅
            return (true, null, pattern, null);
        }
        catch (Exception ex)
        {
            var (summary, details) = MapTelegramException(ex);
            var msg = string.IsNullOrWhiteSpace(details) ? summary : $"{summary}锛歿details}";
            return (false, msg, null, null);
        }
    }

    /// <summary>
    /// 鍙栨秷寰呯‘璁ょ殑鎵惧洖閭楠岃瘉鐮併€?    /// </summary>
    public async Task<(bool Success, string? Error)> CancelTwoFactorRecoveryEmailAsync(
        int accountId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var client = await GetOrCreateConnectedClientAsync(accountId, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            _ = await client.Account_CancelPasswordEmail();
            return (true, null);
        }
        catch (Exception ex)
        {
            var (summary, details) = MapTelegramException(ex);
            var msg = string.IsNullOrWhiteSpace(details) ? summary : $"{summary}锛歿details}";
            return (false, msg);
        }
    }

    /// <summary>
    /// 鑾峰彇鐧诲綍閭鐘舵€侊紙浠呰繑鍥炴帺鐮?Pattern锛屼笉杩斿洖鐪熷疄閭锛夈€?    /// </summary>
    public async Task<(bool Success, string? Error, bool HasLoginEmail, string? LoginEmailPattern)>
        GetLoginEmailStatusAsync(int accountId, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = await GetOrCreateConnectedClientAsync(accountId, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            var pwd = await client.Account_GetPassword();
            var hasLoginEmail = pwd.flags.HasFlag(TL.Account_Password.Flags.has_login_email_pattern);
            var pattern = hasLoginEmail ? (pwd.login_email_pattern ?? "").Trim() : null;
            if (string.IsNullOrWhiteSpace(pattern))
                pattern = null;

            return (true, null, hasLoginEmail, pattern);
        }
        catch (Exception ex)
        {
            var (summary, details) = MapTelegramException(ex);
            var msg = string.IsNullOrWhiteSpace(details) ? summary : $"{summary}锛歿details}";
            return (false, msg, false, null);
        }
    }

    /// <summary>
    /// 鍙戦€佺櫥褰曢偖绠遍獙璇佺爜锛堢敤浜庘€滅櫥褰曢偖绠卞彉鏇?璁剧疆鈥濓級銆?    /// 娉ㄦ剰锛氶儴鍒嗚处鍙峰彲鑳芥棤娉曞湪鈥滃凡鐧诲綍鐘舵€佲€濅笅鏂板鐧诲綍閭锛堥渶瑕佺櫥褰曟祦绋嬭Е鍙戠殑 setup锛夈€?    /// </summary>
    public async Task<(bool Success, string? Error, string? EmailPattern)> SetLoginEmailAsync(
        int accountId,
        string email,
        CancellationToken cancellationToken = default)
    {
        try
        {
            email = (email ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(email))
                return (false, "閭涓嶈兘涓虹┖", null);

            try
            {
                _ = new MailAddress(email);
            }
            catch
            {
                return (false, "閭鏍煎紡涓嶆纭?, null);
            }

            var client = await GetOrCreateConnectedClientAsync(accountId, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            var sent = await client.Account_SendVerifyEmailCode(new EmailVerifyPurposeLoginChange(), email);
            var pattern = (sent.email_pattern ?? "").Trim();
            if (string.IsNullOrWhiteSpace(pattern))
                pattern = null;

            return (true, null, pattern);
        }
        catch (Exception ex)
        {
            var (summary, details) = MapTelegramException(ex);
            var msg = string.IsNullOrWhiteSpace(details) ? summary : $"{summary}锛歿details}";
            return (false, msg, null);
        }
    }

    /// <summary>
    /// 纭鐧诲綍閭楠岃瘉鐮併€?    /// </summary>
    public async Task<(bool Success, string? Error)> ConfirmLoginEmailAsync(
        int accountId,
        string code,
        CancellationToken cancellationToken = default)
    {
        try
        {
            code = (code ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(code))
                return (false, "璇峰～鍐欓偖绠遍獙璇佺爜");

            var client = await GetOrCreateConnectedClientAsync(accountId, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            _ = await client.Account_VerifyEmail(new EmailVerifyPurposeLoginChange(), new EmailVerificationCode { code = code });
            return (true, null);
        }
        catch (Exception ex)
        {
            var (summary, details) = MapTelegramException(ex);
            var msg = string.IsNullOrWhiteSpace(details) ? summary : $"{summary}锛歿details}";
            return (false, msg);
        }
    }

    /// <summary>
    /// 鏇存柊褰撳墠璐﹀彿鐨勬樀绉?绠€浠嬶紙Bio锛夈€?    /// 娉ㄦ剰锛氱敤鎴峰悕涓庡ご鍍忓垎寮€浣跨敤 UpdateUsernameAsync / UpdateProfilePhotoAsync銆?    /// </summary>
    public async Task<(bool Success, string? Error)> UpdateUserProfileAsync(
        int accountId,
        string? nickname,
        string? bio,
        CancellationToken cancellationToken = default)
    {
        try
        {
            nickname = string.IsNullOrWhiteSpace(nickname) ? null : nickname.Trim();
            bio = bio == null ? null : bio.Trim();

            var client = await GetOrCreateConnectedClientAsync(accountId, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            // account.updateProfile 鐨勫瓧娈垫槸鍙€夌殑锛氫紶 null 琛ㄧず涓嶄慨鏀硅瀛楁
            string? firstName = null;
            string? lastName = null;
            if (nickname != null)
            {
                firstName = nickname;
                lastName = string.Empty;
            }

            await client.Account_UpdateProfile(firstName, lastName, bio);
            return (true, null);
        }
        catch (Exception ex)
        {
            var (summary, details) = MapTelegramException(ex);
            var msg = string.IsNullOrWhiteSpace(details) ? summary : $"{summary}锛歿details}";
            return (false, msg);
        }
    }

    /// <summary>
    /// 鏇存柊褰撳墠璐﹀彿鐢ㄦ埛鍚嶏紙t.me/xxx锛夈€備紶绌哄瓧绗︿覆琛ㄧず娓呯┖鐢ㄦ埛鍚嶃€?    /// </summary>
    public async Task<(bool Success, string? Error, string? Username)> UpdateUsernameAsync(
        int accountId,
        string? username,
        CancellationToken cancellationToken = default)
    {
        try
        {
            username = (username ?? string.Empty).Trim().TrimStart('@');

            var client = await GetOrCreateConnectedClientAsync(accountId, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            var result = await client.Account_UpdateUsername(username);

            // result 鍙兘鏄?User 鎴?bool锛岀粺涓€浠庤緭鍏ュ洖濉嵆鍙?            var normalized = string.IsNullOrWhiteSpace(username) ? null : username;
            return (true, null, normalized);
        }
        catch (Exception ex)
        {
            var (summary, details) = MapTelegramException(ex);
            var msg = string.IsNullOrWhiteSpace(details) ? summary : $"{summary}锛歿details}";
            return (false, msg, null);
        }
    }

    /// <summary>
    /// 閫氳繃閾炬帴/鐢ㄦ埛鍚嶅姞鍏ョ兢缁勬垨璁㈤槄棰戦亾锛堟敮鎸?https://t.me/xxx銆乼.me/+hash銆丂username銆乽sername銆乼g://join?invite=hash 绛夛級銆?    /// </summary>
    public async Task<(bool Success, string? Error, string? JoinedTitle)> JoinChatOrChannelAsync(
        int accountId,
        string linkOrUsername,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var raw = (linkOrUsername ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(raw))
                return (false, "閾炬帴/鐢ㄦ埛鍚嶄负绌?, null);

            var url = NormalizeTelegramJoinUrl(raw);

            var client = await GetOrCreateConnectedClientAsync(accountId, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            var chat = await client.AnalyzeInviteLink(url, join: true);
            cancellationToken.ThrowIfCancellationRequested();

            var title = chat switch
            {
                TL.Channel c => c.title,
                TL.Chat c => c.title,
                _ => null
            };

            return (true, null, title);
        }
        catch (RpcException ex) when (ex.Code == 400 && string.Equals(ex.Message, "USER_ALREADY_PARTICIPANT", StringComparison.OrdinalIgnoreCase))
        {
            return (true, null, "宸插湪缇ょ粍/棰戦亾涓?);
        }
        catch (Exception ex)
        {
            var (summary, details) = MapTelegramException(ex);
            var msg = string.IsNullOrWhiteSpace(details) ? summary : $"{summary}锛歿details}";
            return (false, msg, null);
        }
    }

    /// <summary>
    /// 閫氳繃閾炬帴/鐢ㄦ埛鍚嶉€€鍑虹兢缁勬垨鍙栨秷璁㈤槄棰戦亾锛堟敮鎸?https://t.me/xxx銆乼.me/+hash銆丂username銆乽sername銆乼g://join?invite=hash 绛夛級銆?    /// </summary>
    public async Task<(bool Success, string? Error, string? LeftTitle)> LeaveChatOrChannelAsync(
        int accountId,
        string linkOrUsername,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var raw = (linkOrUsername ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(raw))
                return (false, "閾炬帴/鐢ㄦ埛鍚嶄负绌?, null);

            var url = NormalizeTelegramJoinUrl(raw);

            var client = await GetOrCreateConnectedClientAsync(accountId, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            // 瑙ｆ瀽鐩爣锛堜笉鍔犲叆锛?            var chat = await client.AnalyzeInviteLink(url, join: false);
            cancellationToken.ThrowIfCancellationRequested();

            var title = chat switch
            {
                TL.Channel c => c.title,
                TL.Chat c => c.title,
                _ => null
            };

            var peer = chat switch
            {
                TL.Channel c => c.ToInputPeer(),
                TL.Chat c => c.ToInputPeer(),
                _ => null
            };

            if (peer == null)
                return (false, "鏃犳硶瑙ｆ瀽鐩爣缇ょ粍/棰戦亾", null);

            await client.LeaveChat(peer);
            return (true, null, title);
        }
        catch (RpcException ex) when (ex.Code == 400 && string.Equals(ex.Message, "USER_NOT_PARTICIPANT", StringComparison.OrdinalIgnoreCase))
        {
            return (true, null, "鏈湪缇ょ粍/棰戦亾涓?);
        }
        catch (Exception ex)
        {
            var (summary, details) = MapTelegramException(ex);
            var msg = string.IsNullOrWhiteSpace(details) ? summary : $"{summary}锛歿details}";
            return (false, msg, null);
        }
    }

    /// <summary>
    /// 鍚敤澶栭儴 Bot锛堝悜 Bot 鍙戦€?/start锛屽彲甯﹀弬鏁帮級銆?    /// 鏀寔锛欯xxxbot銆亁xxbot銆乭ttps://t.me/xxxbot銆乼g://resolve?domain=xxxbot&start=abc
    /// </summary>
    public async Task<(bool Success, string? Error, string? BotUsername)> StartExternalBotAsync(
        int accountId,
        string botLinkOrUsername,
        string? startParameter = null,
        CancellationToken cancellationToken = default,
        bool assumeBotUsername = false)
    {
        try
        {
            var (username, startFromLink) = NormalizeTelegramBotUsername(botLinkOrUsername, assumeBotUsername);
            var normalizedManualStart = NormalizeBotStartParameter(startParameter);
            var finalStart = string.IsNullOrWhiteSpace(normalizedManualStart) ? startFromLink : normalizedManualStart;

            if (finalStart.Length > 64)
                return (false, "鍚姩鍙傛暟杩囬暱锛堟渶澶?64 瀛楃锛?, null);

            var client = await GetOrCreateConnectedClientAsync(accountId, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            var resolved = await client.Contacts_ResolveUsername(username);
            var user = resolved.User;
            if (user.access_hash == 0)
                return (false, "鏃犳硶鑾峰彇 Bot access_hash", null);

            var inputUser = new InputUser(user.id, user.access_hash);
            var randomId = Random.Shared.NextInt64();
            await client.Messages_StartBot(
                bot: inputUser,
                peer: new InputPeerSelf(),
                random_id: randomId,
                start_param: finalStart);

            return (true, null, "@" + username);
        }
        catch (RpcException ex) when (ex.Code == 400 && string.Equals(ex.Message, "BOT_APP_INVALID", StringComparison.OrdinalIgnoreCase))
        {
            return (false, "鐩爣涓嶆槸鍙惎鍔ㄧ殑 Bot锛圔OT_APP_INVALID锛?, null);
        }
        catch (RpcException ex) when (ex.Code == 400 && string.Equals(ex.Message, "PEER_FLOOD", StringComparison.OrdinalIgnoreCase))
        {
            return (false, "瑙﹀彂椋庢帶锛圥EER_FLOOD锛夛紝璇烽檷浣庨鐜囧悗閲嶈瘯", null);
        }
        catch (Exception ex)
        {
            var (summary, details) = MapTelegramException(ex);
            var msg = string.IsNullOrWhiteSpace(details) ? summary : $"{summary}锛歿details}";
            return (false, msg, null);
        }
    }

    /// <summary>
    /// 鍋滅敤澶栭儴 Bot锛堥€氳繃鎷夐粦 Bot 瀹炵幇锛夈€?    /// 鏀寔锛欯xxxbot銆亁xxbot銆乭ttps://t.me/xxxbot銆乼g://resolve?domain=xxxbot
    /// </summary>
    public async Task<(bool Success, string? Error, string? BotUsername)> StopExternalBotAsync(
        int accountId,
        string botLinkOrUsername,
        CancellationToken cancellationToken = default,
        bool assumeBotUsername = false)
    {
        try
        {
            var (username, _) = NormalizeTelegramBotUsername(botLinkOrUsername, assumeBotUsername);

            var client = await GetOrCreateConnectedClientAsync(accountId, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            var resolved = await client.Contacts_ResolveUsername(username);
            var user = resolved.User;
            if (user.access_hash == 0)
                return (false, "鏃犳硶鑾峰彇 Bot access_hash", null);

            await client.Contacts_Block(new InputPeerUser(user.id, user.access_hash));
            return (true, null, "@" + username);
        }
        catch (RpcException ex) when (ex.Code == 400 && string.Equals(ex.Message, "USER_NOT_MUTUAL_CONTACT", StringComparison.OrdinalIgnoreCase))
        {
            // 鏌愪簺璐﹀彿鐘舵€佷笅浼氳繑鍥炶閿欒锛屾寜鈥滃凡鍋滅敤鈥濆鐞嗗彲閬垮厤鎵归噺浠诲姟涓柇銆?            return (true, null, null);
        }
        catch (Exception ex)
        {
            var (summary, details) = MapTelegramException(ex);
            var msg = string.IsNullOrWhiteSpace(details) ? summary : $"{summary}锛歿details}";
            return (false, msg, null);
        }
    }

    /// <summary>
    /// 瑙ｆ瀽澶栭儴 Bot 浼氳瘽鐩爣锛堢敤浜庡悗缁彂閫佹秷鎭?绛夊緟鍥炲锛夈€?    /// 鏀寔锛欯xxxbot銆亁xxbot銆乭ttps://t.me/xxxbot銆乼g://resolve?domain=xxxbot&start=abc
    /// </summary>
    public async Task<(bool Success, string? Error, ResolvedChatTarget? Target, string? BotUsername)> ResolveExternalBotTargetAsync(
        int accountId,
        string botLinkOrUsername,
        CancellationToken cancellationToken = default,
        bool assumeBotUsername = false)
    {
        try
        {
            var (username, _) = NormalizeTelegramBotUsername(botLinkOrUsername, assumeBotUsername);
            var client = await GetOrCreateConnectedClientAsync(accountId, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            var resolved = await client.Contacts_ResolveUsername(username);
            var user = resolved.User;
            if (user.access_hash == 0)
                return (false, "鏃犳硶鑾峰彇 Bot access_hash", null, null);

            var target = new ResolvedChatTarget(
                new InputPeerUser(user.id, user.access_hash),
                "@" + username,
                user.id.ToString(CultureInfo.InvariantCulture));
            return (true, null, target, "@" + username);
        }
        catch (Exception ex)
        {
            var (summary, details) = MapTelegramException(ex);
            var msg = string.IsNullOrWhiteSpace(details) ? summary : $"{summary}锛寋details}";
            return (false, msg, null, null);
        }
    }

    public sealed record ResolvedChatTarget(InputPeer Peer, string Title, string CanonicalId);

    /// <summary>
    /// 瑙ｆ瀽缇ょ粍/棰戦亾鐩爣锛屾敮鎸侊細
    /// - 鐢ㄦ埛鍚?閾炬帴锛欯username銆乽sername銆乭ttps://t.me/xxx銆乼.me/xxx銆乼g://join?invite=hash
    /// - 棰戦亾/缇ょ粍 ID锛?23456銆?123456銆?1001234567890
    /// </summary>
    public async Task<(bool Success, string? Error, ResolvedChatTarget? Target)> ResolveChatTargetAsync(
        int accountId,
        string target,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var raw = (target ?? string.Empty).Trim();
            if (raw.Length == 0)
                return (false, "鐩爣涓虹┖", null);

            var client = await GetOrCreateConnectedClientAsync(accountId, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            if (TryParseChatIdCandidate(raw, out var normalizedId))
            {
                var resolvedById = await TryResolveChatByIdFromDialogsAsync(client, normalizedId, cancellationToken);
                if (resolvedById != null)
                    return (true, null, resolvedById);

                return (false, $"鏈壘鍒?chatId={raw} 瀵瑰簲鐨勭兢缁?棰戦亾锛堣纭璇ヨ处鍙峰凡鍔犲叆鐩爣锛?, null);
            }

            var url = NormalizeTelegramJoinUrl(raw);
            var chat = await client.AnalyzeInviteLink(url, join: false);
            cancellationToken.ThrowIfCancellationRequested();

            var peer = chat switch
            {
                TL.Channel c => c.ToInputPeer(),
                TL.Chat c => c.ToInputPeer(),
                _ => null
            };

            if (peer == null)
                return (false, "鏃犳硶瑙ｆ瀽鐩爣缇ょ粍/棰戦亾", null);

            return chat switch
            {
                TL.Channel channel => (true, null, new ResolvedChatTarget(peer, NormalizeChatTitle(channel.title, channel.id.ToString(CultureInfo.InvariantCulture)), BuildChannelBotApiChatId(channel.id).ToString(CultureInfo.InvariantCulture))),
                TL.Chat basic => (true, null, new ResolvedChatTarget(peer, NormalizeChatTitle(basic.title, basic.id.ToString(CultureInfo.InvariantCulture)), basic.id.ToString(CultureInfo.InvariantCulture))),
                _ => (true, null, new ResolvedChatTarget(peer, raw, raw))
            };
        }
        catch (Exception ex)
        {
            var (summary, details) = MapTelegramException(ex);
            var msg = string.IsNullOrWhiteSpace(details) ? summary : $"{summary}锛歿details}";
            return (false, msg, null);
        }
    }

    /// <summary>
    /// 鍚戝凡瑙ｆ瀽鐨勭兢缁?棰戦亾鐩爣鍙戦€佹枃鏈秷鎭€?    /// </summary>
    public async Task<(bool Success, string? Error, int? MessageId)> SendMessageToResolvedChatAsync(
        int accountId,
        ResolvedChatTarget target,
        string message,
        int? replyToMessageId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var text = (message ?? string.Empty).Trim();
            if (text.Length == 0)
                return (false, "娑堟伅鍐呭涓虹┖", null);

            var client = await GetOrCreateConnectedClientAsync(accountId, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            var sent = await client.SendMessageAsync(target.Peer, text, null, replyToMessageId ?? 0);
            return (true, null, sent.id);
        }
        catch (Exception ex)
        {
            var (summary, details) = MapTelegramException(ex);
            var msg = string.IsNullOrWhiteSpace(details) ? summary : $"{summary}锛歿details}";
            return (false, msg, null);
        }
    }

    public async Task<(bool Success, string? Error, TelegramVerificationMessageCandidate? Candidate)> WaitForBotVerificationMessageAsync(
        int accountId,
        ResolvedChatTarget target,
        int sentMessageId,
        string? currentUsername,
        int timeoutSeconds,
        Func<TelegramAccountMessageUpdate, bool>? messageFilter = null,
        IReadOnlyCollection<string>? allowedSenderUsernames = null,
        bool restrictToAllowedUsernames = false,
        bool stopOnUnmatchedMention = false,
        CancellationToken cancellationToken = default)
    {
        if (timeoutSeconds < 3)
            timeoutSeconds = 3;
        if (timeoutSeconds > 300)
            timeoutSeconds = 300;

        try
        {
            var client = await GetOrCreateConnectedClientAsync(accountId, cancellationToken);
            var waitStartedAt = DateTimeOffset.UtcNow.AddSeconds(-2);
            var update = await _updateHub.WaitForAsync(
                accountId,
                x => IsCandidateVerificationMessage(
                    x,
                    target,
                    currentUsername,
                    sentMessageId,
                    messageFilter,
                    allowedSenderUsernames,
                    restrictToAllowedUsernames,
                    stopOnUnmatchedMention),
                waitStartedAt,
                TimeSpan.FromSeconds(timeoutSeconds),
                cancellationToken);

            if (update == null)
                return (false, $"绛夊緟楠岃瘉娑堟伅瓒呮椂锛坽timeoutSeconds} 绉掞級", null);

            if (messageFilter != null
                && stopOnUnmatchedMention
                && !messageFilter(update)
                && IsMentionOrReply(update, currentUsername, sentMessageId))
            {
                return (false, "楠岃瘉娑堟伅鏈懡涓叧閿瘝/姝ｅ垯锛屽凡璺宠繃", null);
            }

            var candidate = await BuildVerificationCandidateAsync(
                client,
                update.Message,
                currentUsername,
                sentMessageId,
                cancellationToken);

            return candidate == null
                ? (false, "鍖归厤鍒扮殑楠岃瘉娑堟伅涓虹┖锛屾棤娉曟墽琛?AI 璇嗗埆", null)
                : (true, null, candidate);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            var (summary, details) = MapTelegramException(ex);
            var msg = string.IsNullOrWhiteSpace(details) ? summary : $"{summary}锛歿details}";
            return (false, msg, null);
        }
    }

    public async Task<(bool Success, string? Error)> ClickInlineButtonAsync(
        int accountId,
        ResolvedChatTarget target,
        int messageId,
        byte[] callbackData,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (callbackData == null || callbackData.Length == 0)
                return (false, "鎸夐挳缂哄皯 callback_data");

            var client = await GetOrCreateConnectedClientAsync(accountId, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            _ = await client.Messages_GetBotCallbackAnswer(target.Peer, messageId, callbackData, null, false);
            return (true, null);
        }
        catch (Exception ex) when (IsBotCallbackTimeout(ex))
        {
            _logger.LogInformation(
                ex,
                "Telegram bot callback timed out after click, treat as delivered: accountId={AccountId}, chat={ChatId}, messageId={MessageId}",
                accountId,
                target.CanonicalId,
                messageId);
            return (true, null);
        }
        catch (Exception ex)
        {
            var (summary, details) = MapTelegramException(ex);
            var msg = string.IsNullOrWhiteSpace(details) ? summary : $"{summary}锛歿details}";
            return (false, msg);
        }
    }

    private async Task<ResolvedChatTarget?> TryResolveChatByIdFromDialogsAsync(
        Client client,
        long normalizedId,
        CancellationToken cancellationToken)
    {
        var dialogs = await client.Messages_GetAllDialogs();
        cancellationToken.ThrowIfCancellationRequested();

        foreach (var chat in dialogs.chats.Values)
        {
            switch (chat)
            {
                case TL.Channel channel when channel.IsActive:
                {
                    var rawId = channel.id;
                    var botApiId = BuildChannelBotApiChatId(rawId);
                    if (normalizedId != rawId && normalizedId != botApiId)
                        continue;

                    return new ResolvedChatTarget(
                        channel.ToInputPeer(),
                        NormalizeChatTitle(channel.title, rawId.ToString(CultureInfo.InvariantCulture)),
                        botApiId.ToString(CultureInfo.InvariantCulture));
                }
                case TL.Chat basic when basic.IsActive:
                {
                    var rawId = basic.id;
                    var negativeId = -rawId;
                    if (normalizedId != rawId && normalizedId != negativeId)
                        continue;

                    return new ResolvedChatTarget(
                        basic.ToInputPeer(),
                        NormalizeChatTitle(basic.title, rawId.ToString(CultureInfo.InvariantCulture)),
                        rawId.ToString(CultureInfo.InvariantCulture));
                }
            }
        }

        return null;
    }

    private bool IsCandidateVerificationMessage(
        TelegramAccountMessageUpdate update,
        ResolvedChatTarget target,
        string? currentUsername,
        int sentMessageId,
        Func<TelegramAccountMessageUpdate, bool>? messageFilter,
        IReadOnlyCollection<string>? allowedSenderUsernames,
        bool restrictToAllowedUsernames,
        bool stopOnUnmatchedMention)
    {
        if (!IsSamePeer(target.Peer, update.Message.peer_id))
            return false;

        if (restrictToAllowedUsernames)
        {
            if (!IsSenderInAllowedUsernames(update, allowedSenderUsernames))
                return false;
        }
        else
        {
            if (!update.SenderIsBot)
                return false;
        }

        if (messageFilter != null)
        {
            if (messageFilter(update))
                return true;

            if (stopOnUnmatchedMention && IsMentionOrReply(update, currentUsername, sentMessageId))
                return true;

            return false;
        }

        if (!IsMentionOrReply(update, currentUsername, sentMessageId))
            return false;

        return LooksLikeVerificationChallenge(update);
    }

    private static bool IsMentionOrReply(
        TelegramAccountMessageUpdate update,
        string? currentUsername,
        int sentMessageId)
    {
        var mentionsAccount = ContainsUsernameMention(update.Message.message, currentUsername);
        var replyToSent = update.ReplyToMessageId == sentMessageId;
        return mentionsAccount || replyToSent;
    }

    private static bool IsSenderInAllowedUsernames(
        TelegramAccountMessageUpdate update,
        IReadOnlyCollection<string>? allowedUsernames)
    {
        if (allowedUsernames == null || allowedUsernames.Count == 0)
            return false;

        var candidates = new[]
        {
            update.SenderUsername,
            update.SenderChatUsername,
            update.SenderPostAuthor
        };

        foreach (var candidate in candidates)
        {
            var normalized = (candidate ?? string.Empty).Trim().TrimStart('@');
            if (normalized.Length == 0)
                continue;

            foreach (var allowed in allowedUsernames)
            {
                if (string.IsNullOrWhiteSpace(allowed))
                    continue;

                var normalizedAllowed = allowed.Trim().TrimStart('@');
                if (normalizedAllowed.Length == 0)
                    continue;

                if (string.Equals(normalizedAllowed, normalized, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }

        foreach (var allowed in allowedUsernames)
        {
            if (!TryParseAllowedSenderId(allowed, out var id, out var kind))
                continue;

            if (kind == AllowedSenderIdKind.User)
            {
                if (update.SenderUserId.HasValue && update.SenderUserId.Value == id)
                    return true;
            }
            else if (kind == AllowedSenderIdKind.Chat)
            {
                if (update.SenderChatId.HasValue && update.SenderChatId.Value == id)
                    return true;
            }
            else
            {
                if ((update.SenderUserId.HasValue && update.SenderUserId.Value == id)
                    || (update.SenderChatId.HasValue && update.SenderChatId.Value == id))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private enum AllowedSenderIdKind
    {
        Any = 0,
        User = 1,
        Chat = 2
    }

    private static bool TryParseAllowedSenderId(
        string? raw,
        out long id,
        out AllowedSenderIdKind kind)
    {
        id = 0;
        kind = AllowedSenderIdKind.Any;

        var value = (raw ?? string.Empty).Trim();
        if (value.Length == 0)
            return false;

        if (value.StartsWith("user:", StringComparison.OrdinalIgnoreCase))
        {
            kind = AllowedSenderIdKind.User;
            value = value[5..].Trim();
        }
        else if (value.StartsWith("chat:", StringComparison.OrdinalIgnoreCase)
                 || value.StartsWith("channel:", StringComparison.OrdinalIgnoreCase))
        {
            kind = AllowedSenderIdKind.Chat;
            value = value.Contains(':')
                ? value[(value.IndexOf(':') + 1)..].Trim()
                : string.Empty;
        }
        else if (value.StartsWith("id:", StringComparison.OrdinalIgnoreCase))
        {
            kind = AllowedSenderIdKind.Any;
            value = value[3..].Trim();
        }

        if (value.StartsWith("-100", StringComparison.Ordinal) && value.Length > 4)
        {
            if (long.TryParse(value[4..], out var parsedChatId))
            {
                id = parsedChatId;
                if (kind == AllowedSenderIdKind.Any)
                    kind = AllowedSenderIdKind.Chat;
                return true;
            }
        }

        if (long.TryParse(value, out var parsed))
        {
            id = parsed;
            return true;
        }

        return false;
    }

    private async Task<TelegramVerificationMessageCandidate?> BuildVerificationCandidateAsync(
        Client client,
        Message message,
        string? currentUsername,
        int sentMessageId,
        CancellationToken cancellationToken)
    {
        var buttons = ExtractInlineButtons(message);
        var imageJpegBytes = await TryDownloadVerificationImageAsync(client, message, cancellationToken);
        var text = (message.message ?? string.Empty).Trim();

        if (buttons.Count == 0 && text.Length == 0 && (imageJpegBytes == null || imageJpegBytes.Length == 0))
            return null;

        return new TelegramVerificationMessageCandidate(
            MessageId: message.id,
            Text: text.Length == 0 ? null : text,
            ImageJpegBytes: imageJpegBytes,
            Buttons: buttons,
            MentionsAccount: ContainsUsernameMention(message.message, currentUsername),
            IsReplyToSentMessage: message.ReplyHeader?.reply_to_msg_id == sentMessageId,
            DateUtc: message.Date.ToUniversalTime());
    }

    private static bool IsSamePeer(InputPeer targetPeer, Peer actualPeer)
    {
        return (targetPeer, actualPeer) switch
        {
            (InputPeerChannel targetChannel, PeerChannel actualChannel) => targetChannel.channel_id == actualChannel.channel_id,
            (InputPeerChat targetChat, PeerChat actualChat) => targetChat.chat_id == actualChat.chat_id,
            (InputPeerUser targetUser, PeerUser actualUser) => targetUser.user_id == actualUser.user_id,
            _ => false
        };
    }

    private static bool ContainsUsernameMention(string? text, string? currentUsername)
    {
        var username = (currentUsername ?? string.Empty).Trim().TrimStart('@');
        if (username.Length == 0)
            return false;

        var messageText = (text ?? string.Empty).Trim();
        if (messageText.Length == 0)
            return false;

        return messageText.Contains($"@{username}", StringComparison.OrdinalIgnoreCase);
    }

    private static bool LooksLikeVerificationChallenge(TelegramAccountMessageUpdate update)
    {
        if (update.Buttons.Count > 0 || update.HasVisualMedia)
            return true;

        var text = update.Text;
        if (text.Length == 0)
            return false;

        if (ContainsAny(text, "鍨冨溇骞垮憡", "骞垮憡", "涓嶄簣澶勭悊", "宸插垹闄?, "杩濊", "灏佺")
            && !ContainsAny(text, "楠岃瘉", "楠岃瘉鐮?, "鏍￠獙", "captcha"))
        {
            return false;
        }

        if (ContainsAny(text,
                "楠岃瘉",
                "楠岃瘉鐮?,
                "鏍￠獙",
                "璇烽€夋嫨",
                "鐐瑰嚮",
                "鎸夐挳",
                "瀹屾垚楠岃瘉",
                "璇峰洖澶?,
                "绛旀",
                "绠楀紡",
                "绛変簬澶氬皯",
                "reply",
                "captcha"))
        {
            return true;
        }

        return LooksLikeMathChallenge(text);
    }

    private static bool LooksLikeMathChallenge(string text)
    {
        var digitCount = 0;
        foreach (var ch in text)
        {
            if (char.IsDigit(ch))
                digitCount++;
        }

        if (digitCount < 2)
            return false;

        return text.IndexOf('+') >= 0
               || text.IndexOf('-') >= 0
               || text.IndexOf('*') >= 0
               || text.IndexOf('/') >= 0
               || text.Contains("脳", StringComparison.Ordinal)
               || text.Contains("梅", StringComparison.Ordinal)
               || text.Contains("锛?, StringComparison.Ordinal)
               || text.IndexOf('=') >= 0;
    }

    private static bool ContainsAny(string text, params string[] keywords)
    {
        foreach (var keyword in keywords)
        {
            if (!string.IsNullOrWhiteSpace(keyword)
                && text.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static List<TelegramInlineButtonOption> ExtractInlineButtons(Message message)
    {
        if (message.reply_markup is not ReplyInlineMarkup markup)
            return new List<TelegramInlineButtonOption>();

        var result = new List<TelegramInlineButtonOption>();
        var index = 0;
        foreach (var row in markup.rows ?? Array.Empty<KeyboardButtonRow>())
        {
            var buttons = row?.buttons;
            if (buttons == null || buttons.Length == 0)
                continue;

            foreach (var button in buttons)
            {
                if (button is KeyboardButtonCallback callback && callback.data is { Length: > 0 })
                {
                    result.Add(new TelegramInlineButtonOption(index, callback.text ?? string.Empty, callback.data));
                    index++;
                }
            }
        }

        return result;
    }

    private async Task<byte[]?> TryDownloadVerificationImageAsync(Client client, Message message, CancellationToken cancellationToken)
    {
        try
        {
            return message.media switch
            {
                MessageMediaPhoto { photo: Photo photo } => await DownloadPhotoAsJpegAsync(client, photo, cancellationToken),
                MessageMediaDocument { document: Document document } => await DownloadDocumentPreviewAsJpegAsync(client, document, cancellationToken),
                _ => null
            };
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to download verification image from Telegram message {MessageId}", message.id);
            return null;
        }
    }

    private async Task<byte[]?> DownloadPhotoAsJpegAsync(Client client, Photo photo, CancellationToken cancellationToken)
    {
        await using var raw = new MemoryStream();
        await client.DownloadFileAsync(photo, raw, (PhotoSizeBase?)null);
        raw.Position = 0;

        await using var jpeg = await TelegramImageProcessor.PrepareStoredImageJpegAsync(raw, cancellationToken: cancellationToken);
        return jpeg.ToArray();
    }

    private async Task<byte[]?> DownloadDocumentPreviewAsJpegAsync(Client client, Document document, CancellationToken cancellationToken)
    {
        await using var raw = new MemoryStream();

        var thumb = document.thumbs?.OfType<PhotoSizeBase>().LastOrDefault();
        if (thumb != null)
        {
            await client.DownloadFileAsync(document, raw, thumb);
        }
        else if ((document.mime_type ?? string.Empty).StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            await client.DownloadFileAsync(document, raw, (PhotoSizeBase?)null);
        }
        else
        {
            return null;
        }

        raw.Position = 0;
        await using var jpeg = await TelegramImageProcessor.PrepareStoredImageJpegAsync(raw, cancellationToken: cancellationToken);
        return jpeg.ToArray();
    }

    private static bool TryParseChatIdCandidate(string raw, out long normalizedId)
    {
        normalizedId = 0;
        var s = (raw ?? string.Empty).Trim();
        if (s.Length == 0)
            return false;

        if (s.StartsWith("+", StringComparison.Ordinal))
            return false;

        if (!long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            return false;

        if (parsed < 0 && s.StartsWith("-100", StringComparison.Ordinal))
        {
            var suffix = s[4..];
            if (suffix.Length > 0 && long.TryParse(suffix, NumberStyles.Integer, CultureInfo.InvariantCulture, out var channelId) && channelId > 0)
            {
                normalizedId = parsed;
                return true;
            }
        }

        normalizedId = parsed;
        return true;
    }

    private static long BuildChannelBotApiChatId(long channelId)
    {
        var text = "-100" + channelId.ToString(CultureInfo.InvariantCulture);
        if (long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            return parsed;
        return channelId;
    }

    private static string NormalizeChatTitle(string? title, string fallback)
    {
        var text = (title ?? string.Empty).Trim();
        return text.Length == 0 ? fallback : text;
    }

    private static string NormalizeTelegramJoinUrl(string input)
    {
        var s = (input ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(s))
            throw new ArgumentException("閾炬帴/鐢ㄦ埛鍚嶄负绌?, nameof(input));

        // tg://join?invite=xxxx
        if (s.StartsWith("tg://", StringComparison.OrdinalIgnoreCase))
        {
            var inviteKey = "invite=";
            var idx = s.IndexOf(inviteKey, StringComparison.OrdinalIgnoreCase);
            if (idx >= 0)
            {
                var hash = s[(idx + inviteKey.Length)..];
                var amp = hash.IndexOf('&');
                if (amp >= 0)
                    hash = hash[..amp];
                hash = hash.Trim();
                if (!string.IsNullOrWhiteSpace(hash))
                    return $"https://t.me/+{hash}";
            }
        }

        // 鐩存帴鏄?t.me/xxx
        if (s.StartsWith("t.me/", StringComparison.OrdinalIgnoreCase) || s.StartsWith("telegram.me/", StringComparison.OrdinalIgnoreCase))
            return "https://" + s;

        // @username / username
        if (s.StartsWith("@", StringComparison.Ordinal))
            s = s.TrimStart('@');

        if (!s.Contains("://", StringComparison.Ordinal))
            return $"https://t.me/{s}";

        return s;
    }

    private static (string Username, string StartFromLink) NormalizeTelegramBotUsername(string input, bool assumeBotUsername = false)
    {
        var s = (input ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(s))
            throw new ArgumentException("Bot 鐢ㄦ埛鍚嶄负绌?, nameof(input));

        string startFromLink = string.Empty;

        // tg://resolve?domain=xxxbot&start=abc
        if (s.StartsWith("tg://", StringComparison.OrdinalIgnoreCase)
            && Uri.TryCreate(s, UriKind.Absolute, out var tgUri))
        {
            var query = ParseQueryString(tgUri.Query);
            if (query.TryGetValue("domain", out var domain) && !string.IsNullOrWhiteSpace(domain))
                s = domain.Trim();
            if (query.TryGetValue("start", out var start) && !string.IsNullOrWhiteSpace(start))
                startFromLink = NormalizeBotStartParameter(start);
        }

        // https://t.me/xxxbot?start=abc 鎴?t.me/xxxbot?start=abc
        if (s.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || s.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            || s.StartsWith("t.me/", StringComparison.OrdinalIgnoreCase)
            || s.StartsWith("telegram.me/", StringComparison.OrdinalIgnoreCase))
        {
            var url = s.Contains("://", StringComparison.Ordinal) ? s : "https://" + s;
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                throw new ArgumentException("Bot 閾炬帴鏍煎紡鏃犳晥", nameof(input));

            var path = (uri.AbsolutePath ?? string.Empty).Trim('/');
            var firstSeg = path.Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(firstSeg))
                throw new ArgumentException("Bot 閾炬帴涓己灏戠敤鎴峰悕", nameof(input));

            s = firstSeg;

            var query = ParseQueryString(uri.Query);
            if (query.TryGetValue("start", out var start) && !string.IsNullOrWhiteSpace(start))
                startFromLink = NormalizeBotStartParameter(start);
        }

        s = s.Trim().TrimStart('@');

        // 鏀寔锛欯username?start=abc锛堟棤 http/tg 鍗忚锛?        var question = s.IndexOf('?');
        if (question >= 0)
        {
            var query = ParseQueryString(s[(question + 1)..]);
            if (query.TryGetValue("start", out var start) && !string.IsNullOrWhiteSpace(start))
                startFromLink = NormalizeBotStartParameter(start);

            s = s[..question];
        }

        var slash = s.IndexOf('/');
        if (slash >= 0)
            s = s[..slash];

        if (string.IsNullOrWhiteSpace(s))
            throw new ArgumentException("Bot 鐢ㄦ埛鍚嶄负绌?, nameof(input));

        if (s.StartsWith("+", StringComparison.Ordinal))
            throw new ArgumentException("閭€璇烽摼鎺ヤ笉鏄?Bot 鐢ㄦ埛鍚嶏紝璇疯緭鍏?@xxxbot 鎴?t.me/xxxbot", nameof(input));

        if (!System.Text.RegularExpressions.Regex.IsMatch(s, "^[A-Za-z0-9_]{5,64}$"))
            throw new ArgumentException("Bot 鐢ㄦ埛鍚嶆牸寮忔棤鏁?, nameof(input));

        // 甯歌鎯呭喌锛氳姹備互 bot 缁撳熬
        // 渚嬪锛?        // 1) 鏄惧紡缁欎簡 start 鍙傛暟锛堝父瑙佷簬 t.me/xxx?start=abc 鎴?@xxx?start=abc锛?        // 2) 璋冪敤鏂规槑纭€滄寜 Bot 澶勭悊鈥?        if (!s.EndsWith("bot", StringComparison.OrdinalIgnoreCase)
            && string.IsNullOrWhiteSpace(startFromLink)
            && !assumeBotUsername)
            throw new ArgumentException("鐩爣鐪嬭捣鏉ヤ笉鏄?Bot 鐢ㄦ埛鍚嶏紙闇€浠?bot 缁撳熬锛?, nameof(input));

        return (s, startFromLink);
    }

    private static string NormalizeBotStartParameter(string? input)
    {
        var s = (input ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(s))
            return string.Empty;

        if (s.StartsWith("/start", StringComparison.OrdinalIgnoreCase))
            s = s[6..].Trim();

        if (s.StartsWith("@", StringComparison.Ordinal))
        {
            var idx = s.IndexOf(' ');
            s = idx > 0 ? s[(idx + 1)..].Trim() : string.Empty;
        }

        return s;
    }

    private static Dictionary<string, string> ParseQueryString(string query)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(query))
            return map;

        var raw = query.StartsWith("?", StringComparison.Ordinal) ? query[1..] : query;
        foreach (var part in raw.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var idx = part.IndexOf('=');
            if (idx < 0)
            {
                var kOnly = Uri.UnescapeDataString(part).Trim();
                if (!string.IsNullOrWhiteSpace(kOnly))
                    map[kOnly] = string.Empty;
                continue;
            }

            var key = Uri.UnescapeDataString(part[..idx]).Trim();
            var val = Uri.UnescapeDataString(part[(idx + 1)..]).Trim();
            if (!string.IsNullOrWhiteSpace(key))
                map[key] = val;
        }

        return map;
    }

    /// <summary>
    /// 鏇存柊褰撳墠璐﹀彿澶村儚锛堥潤鎬佸浘鐗囷級銆?    /// </summary>
    public async Task<(bool Success, string? Error)> UpdateProfilePhotoAsync(
        int accountId,
        Stream fileStream,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (fileStream == null)
                return (false, "澶村儚鏂囦欢涓虹┖");

            fileName = (fileName ?? "avatar.jpg").Trim();
            if (string.IsNullOrWhiteSpace(fileName))
                fileName = "avatar.jpg";

            var client = await GetOrCreateConnectedClientAsync(accountId, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            await using var encoded = await TelegramImageProcessor.PrepareAvatarJpegAsync(fileStream, cancellationToken);
            var inputFile = await client.UploadFileAsync(encoded, "avatar.jpg");
            cancellationToken.ThrowIfCancellationRequested();

            if (inputFile == null)
                return (false, "澶村儚涓婁紶澶辫触锛氫笂浼犵粨鏋滀负绌?);

            await client.Photos_UploadProfilePhoto(inputFile, video: null, video_start_ts: null, video_emoji_markup: null, bot: null, fallback: false);
            return (true, null);
        }
        catch (UnknownImageFormatException)
        {
            return (false, "澶村儚涓婁紶澶辫触锛氫笉鏀寔鐨勫浘鐗囨牸寮忥紙寤鸿浣跨敤 JPG/PNG锛?);
        }
        catch (Exception ex)
        {
            var (summary, details) = MapTelegramException(ex);
            var msg = string.IsNullOrWhiteSpace(details) ? summary : $"{summary}锛歿details}";
            return (false, msg);
        }
    }

    public async Task<bool> KickAuthorizationAsync(int accountId, long authorizationHash)
    {
        var client = await GetOrCreateConnectedClientAsync(accountId);
        var ok = await client.Account_ResetAuthorization(authorizationHash);
        return ok;
    }

    public async Task<bool> KickAllOtherAuthorizationsAsync(int accountId)
    {
        var client = await GetOrCreateConnectedClientAsync(accountId);
        var ok = await client.Auth_ResetAuthorizations();
        return ok;
    }

    private async Task<InputPeerUser?> TryResolveSystemPeerAsync(Client client)
    {
        try
        {
            var dialogs = await client.Messages_GetAllDialogs();
            if (!dialogs.users.TryGetValue(TelegramSystemUserId, out var userBase))
                return null;

            if (userBase is not User u || u.access_hash == 0)
                return null;

            return new InputPeerUser(u.id, u.access_hash);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to resolve system peer");
            return null;
        }
    }

    private async Task<Client> GetOrCreateConnectedClientAsync(int accountId, CancellationToken cancellationToken = default)
    {
        Exception? lastError = null;

        for (var attempt = 1; attempt <= 2; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var existing = _clientPool.GetClient(accountId);
            if (existing?.User != null)
                return existing;

            var account = await _accountManagement.GetAccountAsync(accountId)
                ?? throw new InvalidOperationException($"璐﹀彿涓嶅瓨鍦細{accountId}");

            cancellationToken.ThrowIfCancellationRequested();

            var apiId = ResolveApiId(account);
            var apiHash = ResolveApiHash(account);
            var sessionKey = ResolveSessionKey(account, apiHash);

            if (string.IsNullOrWhiteSpace(account.SessionPath))
                throw new InvalidOperationException("璐﹀彿缂哄皯 SessionPath锛屾棤娉曞垱寤?Telegram 瀹㈡埛绔?");

            var absoluteSessionPath = Path.GetFullPath(account.SessionPath);
            if (File.Exists(absoluteSessionPath) && SessionDataConverter.LooksLikeSqliteSession(absoluteSessionPath))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var converted = await SessionDataConverter.TryConvertSqliteSessionFromJsonAsync(
                    phone: account.Phone,
                    apiId: account.ApiId,
                    apiHash: account.ApiHash,
                    sqliteSessionPath: absoluteSessionPath,
                    logger: _logger
                );

                if (!converted.Ok)
                {
                    throw new InvalidOperationException(
                        $"璇ヨ处鍙风殑 Session 鏂囦欢涓?SQLite 鏍煎紡锛歿account.SessionPath}锛屾棤娉曡嚜鍔ㄨ浆鎹负鍙敤 session銆? +
                        $"鍘熷洜锛歿converted.Reason}銆傚缓璁細閲嶆柊瀵煎叆鍖呭惈 session_string 鐨?json锛屾垨鍒般€愯处鍙?鎵嬫満鍙风櫥褰曘€戦噸鏂扮櫥褰曠敓鎴愭柊鐨?sessions/*.session銆?");
                }
            }

            await _clientPool.RemoveClientAsync(accountId);
            cancellationToken.ThrowIfCancellationRequested();

            var client = await _clientPool.GetOrCreateClientAsync(
                accountId: accountId,
                apiId: apiId,
                apiHash: apiHash,
                sessionPath: account.SessionPath,
                sessionKey: sessionKey,
                phoneNumber: account.Phone,
                userId: account.UserId > 0 ? account.UserId : null);

            try
            {
                await ExecuteTelegramRequestAsync(
                    accountId,
                    "杩炴帴 Telegram",
                    () => client.ConnectAsync(),
                    cancellationToken,
                    resetClientOnTimeout: true);
                cancellationToken.ThrowIfCancellationRequested();
                if (client.User == null && (client.UserId != 0 || account.UserId != 0))
                {
                    await ExecuteTelegramRequestAsync(
                        accountId,
                        "鎭㈠ Telegram 鐧诲綍鐘舵€?",
                        () => client.LoginUserIfNeeded(reloginOnFailedResume: false),
                        cancellationToken,
                        resetClientOnTimeout: true);
                }

                if (client.User == null)
                    throw new InvalidOperationException("璐﹀彿鏈櫥褰曟垨 session 宸插け鏁堬紝璇烽噸鏂扮櫥褰曠敓鎴愭柊鐨?session");

                return client;
            }
            catch (Exception ex) when (attempt < 2 && IsRetryableTelegramBootstrapException(ex, cancellationToken))
            {
                lastError = ex;
                _logger.LogWarning(ex, "Telegram client bootstrap failed for account {AccountId} on attempt {Attempt}, retrying once", accountId, attempt);
                await _clientPool.RemoveClientAsync(accountId);
                await Task.Delay(TimeSpan.FromMilliseconds(1200), cancellationToken);
            }
            catch (Exception ex)
            {
                if (LooksLikeSessionApiMismatchOrCorrupted(ex))
                {
                    throw new InvalidOperationException(
                        "璇ヨ处鍙风殑 Session 鏂囦欢鏃犳硶瑙ｆ瀽锛堥€氬父鏄?ApiId/ApiHash 涓庣敓鎴?session 鏃朵笉涓€鑷达紝鎴?session 鏂囦欢宸叉崯鍧忥級銆? +
                        "璇峰埌銆愯处鍙?鎵嬫満鍙风櫥褰曘€戦噸鏂扮櫥褰曠敓鎴愭柊鐨?sessions/*.session 鍚庡啀璇曘€?",
                        ex);
                }

                throw new InvalidOperationException($"Telegram 浼氳瘽鍔犺浇澶辫触锛歿ex.Message}", ex);
            }
        }

        throw new InvalidOperationException($"Telegram 浼氳瘽鍔犺浇澶辫触锛歿lastError?.Message}");
    }

    private static bool IsRetryableTelegramBootstrapException(Exception ex, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
            return false;

        if (ex is TimeoutException)
            return true;

        if (ex is ObjectDisposedException disposed && disposed.ObjectName?.Contains("SemaphoreSlim", StringComparison.OrdinalIgnoreCase) == true)
            return true;

        if (ex is OperationCanceledException)
            return true;

        var message = ex.ToString();
        return message.Contains("A task was canceled", StringComparison.OrdinalIgnoreCase)
               || message.Contains("SemaphoreSlim", StringComparison.OrdinalIgnoreCase)
               || message.Contains("Cannot access a disposed object", StringComparison.OrdinalIgnoreCase);
    }

    private TimeSpan GetTelegramRequestTimeout()
    {
        var seconds = int.TryParse(_configuration["Telegram:RequestTimeoutSeconds"], out var parsedSeconds)
            ? parsedSeconds
            : 90;
        return TimeSpan.FromSeconds(Math.Clamp(seconds, 15, 600));
    }

    private async Task ExecuteTelegramRequestAsync(
        int accountId,
        string operation,
        Func<Task> action,
        CancellationToken cancellationToken,
        bool resetClientOnTimeout)
    {
        var timeout = GetTelegramRequestTimeout();

        try
        {
            await action().WaitAsync(timeout, cancellationToken);
        }
        catch (TimeoutException)
        {
            _logger.LogWarning(
                "Telegram request timed out after {TimeoutSeconds}s for account {AccountId}: {Operation}",
                timeout.TotalSeconds,
                accountId,
                operation);

            if (resetClientOnTimeout)
                await _clientPool.RemoveClientAsync(accountId);

            throw new TimeoutException($"Telegram 璇锋眰瓒呮椂锛歿operation} 瓒呰繃 {timeout.TotalSeconds:0} 绉掞紝鍙兘鏄?Session 澶辨晥銆佽处鍙峰彈闄愩€佺綉缁滃紓甯告垨浠ｇ悊寮傚父");
        }
    }

    private async Task<T> ExecuteTelegramRequestAsync<T>(
        int accountId,
        string operation,
        Func<Task<T>> action,
        CancellationToken cancellationToken,
        bool resetClientOnTimeout)
    {
        var timeout = GetTelegramRequestTimeout();

        try
        {
            return await action().WaitAsync(timeout, cancellationToken);
        }
        catch (TimeoutException)
        {
            _logger.LogWarning(
                "Telegram request timed out after {TimeoutSeconds}s for account {AccountId}: {Operation}",
                timeout.TotalSeconds,
                accountId,
                operation);

            if (resetClientOnTimeout)
                await _clientPool.RemoveClientAsync(accountId);

            throw new TimeoutException($"Telegram 璇锋眰瓒呮椂锛歿operation} 瓒呰繃 {timeout.TotalSeconds:0} 绉掞紝鍙兘鏄?Session 澶辨晥銆佽处鍙峰彈闄愩€佺綉缁滃紓甯告垨浠ｇ悊寮傚父");
        }
    }

    private int ResolveApiId(Account account)
    {
        if (int.TryParse(_configuration["Telegram:ApiId"], out var globalApiId) && globalApiId > 0)
            return globalApiId;
        if (account.ApiId > 0)
            return account.ApiId;
        throw new InvalidOperationException("鏈厤缃叏灞€ ApiId锛屼笖璐﹀彿缂哄皯 ApiId");
    }

    private string ResolveApiHash(Account account)
    {
        var global = _configuration["Telegram:ApiHash"];
        if (!string.IsNullOrWhiteSpace(global))
            return global.Trim();
        if (!string.IsNullOrWhiteSpace(account.ApiHash))
            return account.ApiHash.Trim();
        throw new InvalidOperationException("鏈厤缃叏灞€ ApiHash锛屼笖璐﹀彿缂哄皯 ApiHash");
    }

    private static string ResolveSessionKey(Account account, string apiHash)
    {
        return !string.IsNullOrWhiteSpace(account.ApiHash) ? account.ApiHash.Trim() : apiHash.Trim();
    }

    private static bool LooksLikeSessionApiMismatchOrCorrupted(Exception ex)
    {
        var msg = ex.Message ?? string.Empty;
        return msg.Contains("Can't read session block", StringComparison.OrdinalIgnoreCase)
            || msg.Contains("Use the correct api_hash", StringComparison.OrdinalIgnoreCase)
            || msg.Contains("Use the correct api_hash/id/key", StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildProfileDetails(TelegramAccountProfile profile)
    {
        var flags = new List<string>();
        if (profile.IsPremium) flags.Add("Premium");
        if (profile.IsVerified) flags.Add("Verified");
        if (profile.IsRestricted) flags.Add("Restricted");
        if (profile.IsScam) flags.Add("Scam");
        if (profile.IsFake) flags.Add("Fake");
        if (profile.IsDeleted) flags.Add("Deleted");

        var flagText = flags.Count == 0 ? "鏃? : string.Join(", ", flags);
        return $"鏄电О锛歿profile.DisplayName}锛涚敤鎴峰悕锛歿profile.Username ?? "-"}锛沀serId锛歿profile.UserId}锛涙爣璁帮細{flagText}";
    }

    private async Task<CreateChannelProbeResult> ProbeCreateChannelCapabilityAsync(Client client, int accountId, CancellationToken cancellationToken = default)
    {
        // 娉ㄦ剰锛氳繖鏄€滄繁搴︽帰娴嬧€濓紝浼氬垱寤哄苟鍒犻櫎涓€涓祴璇曢閬撱€?        var title = $"tp-check-{DateTime.UtcNow:MMddHHmmss}";
        const string about = "Telegram Panel create-channel probe (auto delete)";

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            UpdatesBase updates;
            try
            {
                updates = await ExecuteTelegramRequestAsync(
                    accountId,
                    "鍒涘缓娴嬭瘯棰戦亾鎺㈡祴璐﹀彿鐘舵€?,
                    () => client.Channels_CreateChannel(title: title, about: about, broadcast: true),
                    cancellationToken,
                    resetClientOnTimeout: true);
            }
            catch (RpcException ex) when (ex.Code == 420 && string.Equals(ex.Message, "FROZEN_METHOD_INVALID", StringComparison.OrdinalIgnoreCase))
            {
                return new CreateChannelProbeResult(false, true, "璐﹀彿/ApiId 鍙楅檺锛歍elegram 杩斿洖 FROZEN_METHOD_INVALID锛堝垱寤洪閬撴帴鍙ｈ鍐荤粨锛?);
            }

            cancellationToken.ThrowIfCancellationRequested();

            var channel = updates.Chats.Values.OfType<TL.Channel>().FirstOrDefault();
            if (channel == null)
                return new CreateChannelProbeResult(false, false, "鍒涘缓娴嬭瘯棰戦亾澶辫触锛氭湭杩斿洖 Channel");

            try
            {
                // 绔嬪嵆鍒犻櫎锛岄伩鍏嶇暀涓嬪瀮鍦鹃閬?                var input = new InputChannel(channel.id, channel.access_hash);
                await ExecuteTelegramRequestAsync(
                    accountId,
                    $"鍒犻櫎娴嬭瘯棰戦亾({channel.id})",
                    () => client.Channels_DeleteChannel(input),
                    cancellationToken,
                    resetClientOnTimeout: false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Probe channel created but failed to delete (account {AccountId}, channel {ChannelId})", accountId, channel.id);
                return new CreateChannelProbeResult(false, false, $"鍒涘缓娴嬭瘯棰戦亾鎴愬姛锛屼絾鍒犻櫎澶辫触锛歿ex.Message}锛堣鎵嬪姩鍒犻櫎棰戦亾锛歿title}锛?);
            }

            return new CreateChannelProbeResult(true, false, "鍙敤");
        }
        catch (Exception ex)
        {
            var msg = ex.Message ?? "鏈煡閿欒";
            return new CreateChannelProbeResult(false, false, msg);
        }
    }

    private sealed record CreateChannelProbeResult(bool Success, bool IsFrozen, string Message);

    /// <summary>
    /// 灏?Telegram 寮傚父鏄犲皠涓哄彲璇荤殑鎽樿鍜岃鎯呫€?    /// </summary>
    private static bool IsBotCallbackTimeout(Exception ex)
    {
        var msg = ex.Message ?? string.Empty;
        return msg.Contains("BOT_RESPONSE_TIMEOUT", StringComparison.OrdinalIgnoreCase);
    }

    public static (string summary, string details) MapTelegramException(Exception ex)
    {
        var msg = ex.Message ?? string.Empty;

        if (ex is TimeoutException
            || msg.Contains("璇锋眰瓒呮椂", StringComparison.OrdinalIgnoreCase)
            || msg.Contains("timed out", StringComparison.OrdinalIgnoreCase)
            || msg.Contains("timeout", StringComparison.OrdinalIgnoreCase))
            return ("璇锋眰瓒呮椂", msg);

        if (msg.Contains("EMAIL_HASH_EXPIRED", StringComparison.OrdinalIgnoreCase))
            return (
                "閭楠岃瘉鐮佸凡杩囨湡锛圗MAIL_HASH_EXPIRED锛?,
                "璇风偣鍑烩€滈噸鍙戦獙璇佺爜鈥濓紝骞朵娇鐢ㄦ渶鏂伴偖浠朵腑鐨勯獙璇佺爜銆? + Environment.NewLine + msg);

        if (msg.Contains("EMAIL_NOT_SETUP", StringComparison.OrdinalIgnoreCase))
            return ("鐧诲綍閭鏈惎鐢紙EMAIL_NOT_SETUP锛?, "璇ヨ处鍙锋湭澶勪簬鍙缃?鍙彉鏇寸櫥褰曢偖绠辩殑鐘舵€侊紙閫氬父闇€瑕佺櫥褰曟祦绋嬭Е鍙戣缃級銆? + Environment.NewLine + msg);

        if (msg.Contains("EMAIL_UNCONFIRMED", StringComparison.OrdinalIgnoreCase))
        {
            var m = System.Text.RegularExpressions.Regex.Match(msg, "(EMAIL_UNCONFIRMED(?:_[A-Z0-9]+)?)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            var code = m.Success ? m.Groups[1].Value.ToUpperInvariant() : "EMAIL_UNCONFIRMED";
            return (
                $"閭鏈‘璁わ紙{code}锛?,
                "璇峰湪闈㈡澘杈撳叆閭鏀跺埌鐨勯獙璇佺爜杩涜纭锛涘鎻愮ず杩囨湡璇烽噸鍙戝苟浣跨敤鏈€鏂伴獙璇佺爜銆? + Environment.NewLine + msg);
        }

        if (msg.Contains("EMAIL_TOKEN_INVALID", StringComparison.OrdinalIgnoreCase))
            return ("閭楠岃瘉鐮侀敊璇紙EMAIL_TOKEN_INVALID锛?, "楠岃瘉鐮佷笉姝ｇ‘鎴栦笉鏄渶鏂伴獙璇佺爜銆傝鐐瑰嚮鈥滈噸鍙戦獙璇佺爜鈥濓紝骞朵娇鐢ㄦ渶鏂伴偖浠朵腑鐨勯獙璇佺爜銆? + Environment.NewLine + msg);

        if (msg.Contains("EMAIL_INVALID", StringComparison.OrdinalIgnoreCase))
            return ("閭鏃犳晥锛圗MAIL_INVALID锛?, msg);

        if (msg.Contains("EMAIL_NOT_ALLOWED", StringComparison.OrdinalIgnoreCase))
            return ("閭涓嶅厑璁镐娇鐢紙EMAIL_NOT_ALLOWED锛?, msg);

        if (msg.Contains("FROZEN_METHOD_INVALID", StringComparison.OrdinalIgnoreCase))
            return ("璐﹀彿琚喕缁擄紙FROZEN_METHOD_INVALID锛?, "Telegram 鎻愮ず璇ヨ处鍙?ApiId 鐨勬煇浜涙帴鍙ｈ鍐荤粨锛堝父瑙佷负鍒涘缓棰戦亾鎺ュ彛锛夈€? + Environment.NewLine + msg);

        if (msg.Contains("FLOOD_WAIT", StringComparison.OrdinalIgnoreCase))
            return ("瑙﹀彂闄愭祦锛團LOOD_WAIT锛?, msg);

        if (msg.Contains("CHANNEL_MONOFORUM_UNSUPPORTED", StringComparison.OrdinalIgnoreCase))
            return ("缇ょ粍鎺ュ彛涓嶆敮鎸侊紙CHANNEL_MONOFORUM_UNSUPPORTED锛?, msg);

        if (msg.Contains("AUTH_KEY_UNREGISTERED", StringComparison.OrdinalIgnoreCase))
            return ("Session 澶辨晥锛圓UTH_KEY_UNREGISTERED锛?, msg);

        if (msg.Contains("AUTH_KEY_DUPLICATED", StringComparison.OrdinalIgnoreCase))
            return ("Session 鍐茬獊锛圓UTH_KEY_DUPLICATED锛?, "璇?Session 鍙兘鍦ㄥ叾浠栬澶?搴旂敤涓婂悓鏃朵娇鐢紝瀵艰嚧瀵嗛挜鍐茬獊銆? + Environment.NewLine + msg);

        if (msg.Contains("SESSION_REVOKED", StringComparison.OrdinalIgnoreCase))
            return ("Session 宸茶鎾ら攢锛圫ESSION_REVOKED锛?, "璇?Session 宸茶娉ㄩ攢鎴栨挙閿€锛岄渶瑕侀噸鏂扮櫥褰曘€? + Environment.NewLine + msg);

        if (msg.Contains("SESSION_PASSWORD_NEEDED", StringComparison.OrdinalIgnoreCase))
            return ("闇€瑕佷袱姝ラ獙璇佸瘑鐮侊紙SESSION_PASSWORD_NEEDED锛?, msg);

        if (msg.Contains("CODE_INVALID", StringComparison.OrdinalIgnoreCase))
            return ("楠岃瘉鐮侀敊璇紙CODE_INVALID锛?, "楠岃瘉鐮佷笉姝ｇ‘鎴栦笉鏄渶鏂伴獙璇佺爜銆傝鐐瑰嚮鈥滈噸鍙戦獙璇佺爜鈥濓紝骞朵娇鐢ㄦ渶鏂伴偖浠朵腑鐨勯獙璇佺爜銆? + Environment.NewLine + msg);

        if (msg.Contains("PHOTO_FILE_MISSING", StringComparison.OrdinalIgnoreCase))
            return ("澶村儚涓婁紶澶辫触锛圥HOTO_FILE_MISSING锛?, msg);

        if (msg.Contains("PHONE_NUMBER_BANNED", StringComparison.OrdinalIgnoreCase)
            || msg.Contains("USER_DEACTIVATED_BAN", StringComparison.OrdinalIgnoreCase))
            return ("璐﹀彿琚皝绂?鍋滅敤", msg);

        if (msg.Contains("Can't read session block", StringComparison.OrdinalIgnoreCase))
            return ("Session 鏃犳硶璇诲彇锛圓piHash/Key 涓嶅尮閰嶆垨鎹熷潖锛?, msg);

        return ("杩炴帴澶辫触", msg);
    }
}
