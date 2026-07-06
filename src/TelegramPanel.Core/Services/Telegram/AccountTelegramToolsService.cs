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
/// 鐠愶箑褰跨拠濠冩焽 / 缁崵绮洪柅姘辩叀 / 閸︺劎鍤庣拋鎯ь槵缁狅紕鎮?/// </summary>
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
    /// 閸掗攱鏌婄拹锕€褰块悩鑸碘偓渚婄礄閸欘垶鈧绻佹惔锔藉赴濞村绱板Λ鈧ù瀣р偓婊冨灡瀵ゆ椽顣堕柆鎾村复閸欙絾妲搁崥锕侇潶閸愯崵绮ㄩ垾婵撶礉娴兼艾鍨卞鍝勮嫙閸掔娀娅庢稉鈧稉顏呯ゴ鐠囨洟顣堕柆鎿勭礆
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
                "閹峰褰囩拹锕€褰跨挧鍕灐",
                () => client.Users_GetUsers(InputUser.Self),
                cancellationToken,
                resetClientOnTimeout: true);
            cancellationToken.ThrowIfCancellationRequested();
            var self = users.OfType<User>().FirstOrDefault();

            if (self == null)
            {
                var missingProfile = new TelegramAccountStatusResult(
                    Ok: false,
                    Summary: "閺冪姵纭堕懢宄板絿鐠愶箑褰跨挧鍕灐",
                    Details: "Users_GetUsers(Self) 閺堫亣绻戦崶?User",
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

            var summary = "濮濓絽鐖?;
            if (profile.IsDeleted)
                summary = "鐠愶箑褰垮鍙夋暈闁库偓/鐞氼偄鍨归梽?;
            else if (profile.IsRestricted)
                summary = "鐠愶箑褰块崣妤呮閿涘湩estricted閿?;

            if (probeCreateChannel)
            {
                var probe = await ProbeCreateChannelCapabilityAsync(client, accountId, cancellationToken);
                if (probe.IsFrozen)
                {
                    var frozen = new TelegramAccountStatusResult(
                        Ok: false,
                        Summary: "鐠愶箑褰跨悮顐㈠枙缂佹搫绱欓崚娑樼紦妫版垿浜鹃幒銉ュ經閸欐妾洪敍?,
                        Details: $"閸掓稑缂撴０鎴︿壕閹恒垺绁撮敍姝縫robe.Message}{Environment.NewLine}{BuildProfileDetails(profile)}",
                        CheckedAtUtc: checkedAt,
                        Profile: profile);
                    await TryPersistStatusAsync(accountId, frozen, account, persistProfile: true, cancellationToken: cancellationToken);
                    return frozen;
                }

                if (!probe.Success)
                {
                    var failed = new TelegramAccountStatusResult(
                        Ok: false,
                        Summary: "閸掓稑缂撴０鎴︿壕閹恒垺绁存径杈Е",
                        Details: $"閸掓稑缂撴０鎴︿壕閹恒垺绁撮敍姝縫robe.Message}{Environment.NewLine}{BuildProfileDetails(profile)}",
                        CheckedAtUtc: checkedAt,
                        Profile: profile);
                    await TryPersistStatusAsync(accountId, failed, account, persistProfile: true, cancellationToken: cancellationToken);
                    return failed;
                }

                // 閹恒垺绁撮幋鎰閿涘奔绗夎ぐ鍗炴惙閸樼喓濮搁幀渚婄礉娴犲懓藟閸忓懓顕涢幆?                var okWithProbe = new TelegramAccountStatusResult(
                    Ok: true,
                    Summary: summary,
                    Details: $"閸掓稑缂撴０鎴︿壕閹恒垺绁撮敍姘讲閻㈩煉绱欏鑼跺殰閸斻劍绔婚悶鍡樼ゴ鐠囨洟顣堕柆鎿勭礆{Environment.NewLine}{BuildProfileDetails(profile)}",
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
                Summary: "瀹告彃褰囧☉?,
                Details: "閹垮秳缍斿鎻掑絿濞戝牞绱欐い鐢告桨閸忔娊妫?閸掗攱鏌婄€佃壈鍤ч崣鏍ㄧХ閿?,
                CheckedAtUtc: checkedAt);
        }
        catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested)
        {
            // Blazor 妞ょ敻娼伴崚閿嬫煀/閺傤叀绻涢弮璁圭礉Scoped 閻?DbContext 閸欘垵鍏樺鑼额潶闁插﹥鏂侀敍娑欏Ω鐎瑰啳顫嬫稉鍝勫絿濞戝牐鈧奔绗夐弰顖炴晩鐠囶垬鈧?            return new TelegramAccountStatusResult(
                Ok: false,
                Summary: "瀹告彃褰囧☉?,
                Details: "妞ょ敻娼板鎻掑彠闂?閸掗攱鏌婇敍灞炬惙娴ｆ粏顫︽稉顓熸焽",
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
            // 妞ょ敻娼?娴ｆ粎鏁ら崺鐔峰嚒闁库偓濮ｄ礁顕遍懛瀵告畱 DbContext 闁插﹥鏂侀敍灞芥嫹閻ｃ儱宓嗛崣?        }
        catch (Exception ex)
        {
            // 閸欐牗绉烽崷鐑樻珯娑撳秹娓剁憰浣告珨婢圭増妫╄箛?            if (!cancellationToken.IsCancellationRequested)
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
                    "鐠囪褰?777000 缁崵绮洪柅姘辩叀閸樺棗褰?,
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
    /// 娣囶喗鏁?Telegram 娑撱倖顒炴宀冪槈閿涘牅绨╃痪褍鐦戦惍渚婄礆閵?    /// </summary>
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
                return (false, "閺傞绨╃痪褍鐦戦惍浣风瑝閼虫垝璐熺粚?);

            currentPassword = (currentPassword ?? string.Empty).Trim();
            newPassword = newPassword.Trim();
            hint = (hint ?? string.Empty).Trim();

            var client = await GetOrCreateConnectedClientAsync(accountId, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            // 閸欏倽鈧?WTelegramClient 鐎规ɑ鏌熺粈杞扮伐閿涙ccount_UpdatePasswordSettings 闂団偓鐟?SRP 閺嶏繝鐛欓崐纭风礄閺冄冪槕閻緤绱氭稉搴㈡煀鐎靛棛鐖?settings
            var accountPwd = await client.Account_GetPassword();
            cancellationToken.ThrowIfCancellationRequested();

            // 閼汇儴澶勯崣宄板嚒瀵偓閸氼垯琚卞銉╃崣鐠囦椒绲鹃張顏呭絹娓氭稒妫€靛棛鐖滈敍灞藉灟閻╁瓨甯撮幓鎰仛
            TL.InputCheckPasswordSRP? oldCheck = null;
            if (accountPwd.current_algo != null)
            {
                if (string.IsNullOrWhiteSpace(currentPassword))
                    return (false, "鐠囥儴澶勯崣宄板嚒瀵偓閸氼垯琚卞銉╃崣鐠囦緤绱濈拠宄帮綖閸愭瑥甯禍宀€楠囩€靛棛鐖?);

                oldCheck = await WTelegram.Client.InputCheckPassword(accountPwd, currentPassword);
            }

            // 鐠?InputCheckPassword 閻㈢喐鍨?new_password_hash閿涘牓娓剁憰浣哥殺 current_algo 缂冾喚鈹栭敍?            accountPwd.current_algo = null;
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
            var msg = string.IsNullOrWhiteSpace(details) ? summary : $"{summary}閿涙details}";
            return (false, msg);
        }
    }

    /// <summary>
    /// 韫囨顔囨禍宀€楠囩€靛棛鐖滈敍姘倻 Telegram 閸欐垼鎹ｉ垾婊堝櫢缂冾喕琚卞銉╃崣鐠囦礁鐦戦惍浣测偓婵堟暤鐠囧嚖绱欓柅姘埗闂団偓鐟曚胶鐡戝?7 婢垛晪绱氶妴?    /// </summary>
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
                    return (true, "娴滃瞼楠囩€靛棛鐖滃鏌ュ櫢缂冾喗鍨氶崝鐕傜礄閻滄澘婀崣顖欎簰閻╁瓨甯撮柌宥嗘煀鐠佸墽鐤嗘禍宀€楠囩€靛棛鐖滈敍?, null);

                case TL.Account_ResetPasswordRequestedWait wait:
                {
                    var untilUtc = ToUtcDateTimeOffset(wait.until_date);
                    return (true, $"瀹稿弶褰佹禍銈夊櫢缂冾喚鏁电拠鍑ょ礉鐠囬鐡戝鍛板殾 {untilUtc:yyyy-MM-dd HH:mm:ss} UTC 閸氬骸鍟€鐎瑰本鍨氶柌宥囩枂/闁插秵鏌婄拋鍓х枂娴滃瞼楠囩€靛棛鐖?, untilUtc);
                }

                case TL.Account_ResetPasswordFailedWait failed:
                {
                    var retryUtc = ToUtcDateTimeOffset(failed.retry_date);
                    return (false, $"鏉╂垶婀￠張澶庮潶閸欐牗绉烽惃鍕櫢缂冾喚鏁电拠鍑ょ礉闂団偓缁涘绶熼懛?{retryUtc:yyyy-MM-dd HH:mm:ss} UTC 閸氬孩澧犻懗钘夊晙濞嗭紕鏁电拠?, retryUtc);
                }

                default:
                    return (false, $"閺堫亞鐓℃潻鏂挎礀缁鐎烽敍姝縭esult.GetType().Name}", null);
            }
        }
        catch (Exception ex)
        {
            var (summary, details) = MapTelegramException(ex);
            var msg = string.IsNullOrWhiteSpace(details) ? summary : $"{summary}閿涙details}";
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
    /// 閼惧嘲褰囨稉銈嗩劄妤犲矁鐦夐幍鎯ф礀闁喚顔堥悩鑸碘偓渚婄礄閺勵垰鎯佸鑼拨鐎规哎鈧焦妲搁崥锕€鐡ㄩ崷銊ョ窡绾喛顓婚惃鍕仏缁犳唻绱氶妴?    /// </summary>
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
            var msg = string.IsNullOrWhiteSpace(details) ? summary : $"{summary}閿涙details}";
            return (false, msg, false, false, null);
        }
    }

    /// <summary>
    /// 缂佹垵鐣?閹广垻绮︽稉銈嗩劄妤犲矁鐦夐幍鎯ф礀闁喚顔堥敍鍫滅窗閸欐垿鈧線鐛欑拠浣虹垳閸掍即鍋栫粻鎲嬬礉闂団偓鐠嬪啰鏁?ConfirmTwoFactorRecoveryEmailAsync 绾喛顓婚敍澶堚偓?    /// </summary>
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
                return (false, "闁喚顔堟稉宥堝厴娑撹櫣鈹?, null);

            try
            {
                _ = new MailAddress(email);
            }
            catch
            {
                return (false, "闁喚顔堥弽鐓庣础娑撳秵顒滅涵?, null);
            }

            currentPassword = (currentPassword ?? string.Empty).Trim();

            var client = await GetOrCreateConnectedClientAsync(accountId, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            var pwd = await client.Account_GetPassword();
            cancellationToken.ThrowIfCancellationRequested();

            if (pwd.current_algo == null)
                return (false, "鐠囥儴澶勯崣閿嬫弓瀵偓閸氼垯琚卞銉╃崣鐠囦緤绱濋弮鐘崇《缂佹垵鐣鹃幍鎯ф礀闁喚顔堥敍宀冾嚞閸忓牐顔曠純顔荤癌缁狙冪槕閻?, null);

            if (string.IsNullOrWhiteSpace(currentPassword))
                return (false, "鐠囧嘲锝為崘娆忓斧娴滃瞼楠囩€靛棛鐖?, null);

            var oldCheck = await WTelegram.Client.InputCheckPassword(pwd, currentPassword);

            var settings = new TL.Account_PasswordInputSettings
            {
                flags = TL.Account_PasswordInputSettings.Flags.has_email,
                email = email
            };

            await client.Account_UpdatePasswordSettings(oldCheck, settings);

            // 閺囧瓨鏌婇崥搴″讲闁俺绻?getPassword 閼惧嘲褰囬垾婊冪窡绾喛顓婚柇顔绢唸閳ユ繃甯洪惍浣蜂繆閹?            var after = await client.Account_GetPassword();
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
            var msg = string.IsNullOrWhiteSpace(details) ? summary : $"{summary}閿涙details}";
            return (false, msg, null);
        }
    }

    /// <summary>
    /// 绾喛顓绘稉銈嗩劄妤犲矁鐦夐幍鎯ф礀闁喚顔堟宀冪槈閻降鈧?    /// </summary>
    public async Task<(bool Success, string? Error)> ConfirmTwoFactorRecoveryEmailAsync(
        int accountId,
        string code,
        CancellationToken cancellationToken = default)
    {
        try
        {
            code = (code ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(code))
                return (false, "妤犲矁鐦夐惍浣风瑝閼虫垝璐熺粚?);

            var client = await GetOrCreateConnectedClientAsync(accountId, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            _ = await client.Account_ConfirmPasswordEmail(code);
            return (true, null);
        }
        catch (Exception ex)
        {
            var (summary, details) = MapTelegramException(ex);
            var msg = string.IsNullOrWhiteSpace(details) ? summary : $"{summary}閿涙details}";
            return (false, msg);
        }
    }

    /// <summary>
    /// 闁插秴褰傛稉銈嗩劄妤犲矁鐦夐幍鎯ф礀闁喚顔堟宀冪槈閻緤绱欓棁鈧憰浣稿帥鐠佸墽鐤嗛柇顔绢唸閿涘鈧?    /// </summary>
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
                return (false, "闁插秴褰傛径杈Е", null, null);

            var pwd = await client.Account_GetPassword();
            var pattern = pwd.flags.HasFlag(TL.Account_Password.Flags.has_email_unconfirmed_pattern)
                ? (pwd.email_unconfirmed_pattern ?? "").Trim()
                : null;

            if (string.IsNullOrWhiteSpace(pattern))
                pattern = null;

            // 鐠?API 娑撳秷绻戦崶鐐虹崣鐠囦胶鐖滈梹鍨閿涘奔绮庢潻鏂挎礀闁喚顔堥幒鈺冪垳娣団剝浼?            return (true, null, pattern, null);
        }
        catch (Exception ex)
        {
            var (summary, details) = MapTelegramException(ex);
            var msg = string.IsNullOrWhiteSpace(details) ? summary : $"{summary}閿涙details}";
            return (false, msg, null, null);
        }
    }

    /// <summary>
    /// 閸欐牗绉峰鍛€樼拋銈囨畱閹垫儳娲栭柇顔绢唸妤犲矁鐦夐惍浣碘偓?    /// </summary>
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
            var msg = string.IsNullOrWhiteSpace(details) ? summary : $"{summary}閿涙details}";
            return (false, msg);
        }
    }

    /// <summary>
    /// 閼惧嘲褰囬惂璇茬秿闁喚顔堥悩鑸碘偓渚婄礄娴犲懓绻戦崶鐐村负閻?Pattern閿涘奔绗夋潻鏂挎礀閻喎鐤勯柇顔绢唸閿涘鈧?    /// </summary>
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
            var msg = string.IsNullOrWhiteSpace(details) ? summary : $"{summary}閿涙details}";
            return (false, msg, false, null);
        }
    }

    /// <summary>
    /// 閸欐垿鈧胶娅ヨぐ鏇㈠仏缁犻亶鐛欑拠浣虹垳閿涘牏鏁ゆ禍搴樷偓婊呮瑜版洟鍋栫粻鍗炲綁閺?鐠佸墽鐤嗛垾婵撶礆閵?    /// 濞夈劍鍓伴敍姘跺劥閸掑棜澶勯崣宄板讲閼宠姤妫ゅ▔鏇炴躬閳ユ粌鍑￠惂璇茬秿閻樿埖鈧讲鈧繀绗呴弬鏉款杻閻ц缍嶉柇顔绢唸閿涘牓娓剁憰浣烘瑜版洘绁︾粙瀣曢崣鎴犳畱 setup閿涘鈧?    /// </summary>
    public async Task<(bool Success, string? Error, string? EmailPattern)> SetLoginEmailAsync(
        int accountId,
        string email,
        CancellationToken cancellationToken = default)
    {
        try
        {
            email = (email ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(email))
                return (false, "闁喚顔堟稉宥堝厴娑撹櫣鈹?, null);

            try
            {
                _ = new MailAddress(email);
            }
            catch
            {
                return (false, "闁喚顔堥弽鐓庣础娑撳秵顒滅涵?, null);
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
            var msg = string.IsNullOrWhiteSpace(details) ? summary : $"{summary}閿涙details}";
            return (false, msg, null);
        }
    }

    /// <summary>
    /// 绾喛顓婚惂璇茬秿闁喚顔堟宀冪槈閻降鈧?    /// </summary>
    public async Task<(bool Success, string? Error)> ConfirmLoginEmailAsync(
        int accountId,
        string code,
        CancellationToken cancellationToken = default)
    {
        try
        {
            code = (code ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(code))
                return (false, "鐠囧嘲锝為崘娆撳仏缁犻亶鐛欑拠浣虹垳");

            var client = await GetOrCreateConnectedClientAsync(accountId, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            _ = await client.Account_VerifyEmail(new EmailVerifyPurposeLoginChange(), new EmailVerificationCode { code = code });
            return (true, null);
        }
        catch (Exception ex)
        {
            var (summary, details) = MapTelegramException(ex);
            var msg = string.IsNullOrWhiteSpace(details) ? summary : $"{summary}閿涙details}";
            return (false, msg);
        }
    }

    /// <summary>
    /// 閺囧瓨鏌婅ぐ鎾冲鐠愶箑褰块惃鍕█缁?缁犫偓娴犲绱橞io閿涘鈧?    /// 濞夈劍鍓伴敍姘辨暏閹村嘲鎮曟稉搴°仈閸嶅繐鍨庡鈧担璺ㄦ暏 UpdateUsernameAsync / UpdateProfilePhotoAsync閵?    /// </summary>
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

            // account.updateProfile 閻ㄥ嫬鐡у▓鍨Ц閸欘垶鈧娈戦敍姘炊 null 鐞涖劎銇氭稉宥勬叏閺€纭咁嚉鐎涙顔?            string? firstName = null;
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
            var msg = string.IsNullOrWhiteSpace(details) ? summary : $"{summary}閿涙details}";
            return (false, msg);
        }
    }

    /// <summary>
    /// 閺囧瓨鏌婅ぐ鎾冲鐠愶箑褰块悽銊﹀煕閸氬稄绱檛.me/xxx閿涘鈧倷绱剁粚鍝勭摟缁楋缚瑕嗙悰銊с仛濞撳懐鈹栭悽銊﹀煕閸氬秲鈧?    /// </summary>
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

            // result 閸欘垵鍏橀弰?User 閹?bool閿涘瞼绮烘稉鈧禒搴ょ翻閸忋儱娲栨繅顐㈠祮閸?            var normalized = string.IsNullOrWhiteSpace(username) ? null : username;
            return (true, null, normalized);
        }
        catch (Exception ex)
        {
            var (summary, details) = MapTelegramException(ex);
            var msg = string.IsNullOrWhiteSpace(details) ? summary : $"{summary}閿涙details}";
            return (false, msg, null);
        }
    }

    /// <summary>
    /// 闁俺绻冮柧鐐复/閻劍鍩涢崥宥呭閸忋儳鍏㈢紒鍕灗鐠併垽妲勬０鎴︿壕閿涘牊鏁幐?https://t.me/xxx閵嗕辜.me/+hash閵嗕競username閵嗕菇sername閵嗕辜g://join?invite=hash 缁涘绱氶妴?    /// </summary>
    public async Task<(bool Success, string? Error, string? JoinedTitle)> JoinChatOrChannelAsync(
        int accountId,
        string linkOrUsername,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var raw = (linkOrUsername ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(raw))
                return (false, "闁剧偓甯?閻劍鍩涢崥宥勮礋缁?, null);

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
            return (true, null, "瀹告彃婀紘銈囩矋/妫版垿浜炬稉?);
        }
        catch (Exception ex)
        {
            var (summary, details) = MapTelegramException(ex);
            var msg = string.IsNullOrWhiteSpace(details) ? summary : $"{summary}閿涙details}";
            return (false, msg, null);
        }
    }

    /// <summary>
    /// 闁俺绻冮柧鐐复/閻劍鍩涢崥宥夆偓鈧崙铏瑰參缂佸嫭鍨ㄩ崣鏍ㄧХ鐠併垽妲勬０鎴︿壕閿涘牊鏁幐?https://t.me/xxx閵嗕辜.me/+hash閵嗕競username閵嗕菇sername閵嗕辜g://join?invite=hash 缁涘绱氶妴?    /// </summary>
    public async Task<(bool Success, string? Error, string? LeftTitle)> LeaveChatOrChannelAsync(
        int accountId,
        string linkOrUsername,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var raw = (linkOrUsername ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(raw))
                return (false, "闁剧偓甯?閻劍鍩涢崥宥勮礋缁?, null);

            var url = NormalizeTelegramJoinUrl(raw);

            var client = await GetOrCreateConnectedClientAsync(accountId, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            // 鐟欙絾鐎介惄顔界垼閿涘牅绗夐崝鐘插弳閿?            var chat = await client.AnalyzeInviteLink(url, join: false);
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
                return (false, "閺冪姵纭剁憴锝嗙€介惄顔界垼缂囥倗绮?妫版垿浜?, null);

            await client.LeaveChat(peer);
            return (true, null, title);
        }
        catch (RpcException ex) when (ex.Code == 400 && string.Equals(ex.Message, "USER_NOT_PARTICIPANT", StringComparison.OrdinalIgnoreCase))
        {
            return (true, null, "閺堫亜婀紘銈囩矋/妫版垿浜炬稉?);
        }
        catch (Exception ex)
        {
            var (summary, details) = MapTelegramException(ex);
            var msg = string.IsNullOrWhiteSpace(details) ? summary : $"{summary}閿涙details}";
            return (false, msg, null);
        }
    }

    /// <summary>
    /// 閸氼垳鏁ゆ径鏍劥 Bot閿涘牆鎮?Bot 閸欐垿鈧?/start閿涘苯褰茬敮锕€寮弫甯礆閵?    /// 閺€顖涘瘮閿涙xxxbot閵嗕簛xxbot閵嗕弓ttps://t.me/xxxbot閵嗕辜g://resolve?domain=xxxbot&start=abc
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
                return (false, "閸氼垰濮╅崣鍌涙殶鏉╁洭鏆遍敍鍫熸付婢?64 鐎涙顑侀敍?, null);

            var client = await GetOrCreateConnectedClientAsync(accountId, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            var resolved = await client.Contacts_ResolveUsername(username);
            var user = resolved.User;
            if (user.access_hash == 0)
                return (false, "閺冪姵纭堕懢宄板絿 Bot access_hash", null);

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
            return (false, "閻╊喗鐖ｆ稉宥嗘Ц閸欘垰鎯庨崝銊ф畱 Bot閿涘湐OT_APP_INVALID閿?, null);
        }
        catch (RpcException ex) when (ex.Code == 400 && string.Equals(ex.Message, "PEER_FLOOD", StringComparison.OrdinalIgnoreCase))
        {
            return (false, "鐟欙箑褰傛搴㈠付閿涘湧EER_FLOOD閿涘绱濈拠鐑芥娴ｅ酣顣堕悳鍥ф倵闁插秷鐦?, null);
        }
        catch (Exception ex)
        {
            var (summary, details) = MapTelegramException(ex);
            var msg = string.IsNullOrWhiteSpace(details) ? summary : $"{summary}閿涙details}";
            return (false, msg, null);
        }
    }

    /// <summary>
    /// 閸嬫粎鏁ゆ径鏍劥 Bot閿涘牓鈧俺绻冮幏澶愮拨 Bot 鐎圭偟骞囬敍澶堚偓?    /// 閺€顖涘瘮閿涙xxxbot閵嗕簛xxbot閵嗕弓ttps://t.me/xxxbot閵嗕辜g://resolve?domain=xxxbot
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
                return (false, "閺冪姵纭堕懢宄板絿 Bot access_hash", null);

            await client.Contacts_Block(new InputPeerUser(user.id, user.access_hash));
            return (true, null, "@" + username);
        }
        catch (RpcException ex) when (ex.Code == 400 && string.Equals(ex.Message, "USER_NOT_MUTUAL_CONTACT", StringComparison.OrdinalIgnoreCase))
        {
            // 閺屾劒绨虹拹锕€褰块悩鑸碘偓浣风瑓娴兼俺绻戦崶鐐额嚉闁挎瑨顕ら敍灞惧瘻閳ユ粌鍑￠崑婊呮暏閳ユ繂顦╅悶鍡楀讲闁灝鍘ら幍褰掑櫤娴犺濮熸稉顓熸焽閵?            return (true, null, null);
        }
        catch (Exception ex)
        {
            var (summary, details) = MapTelegramException(ex);
            var msg = string.IsNullOrWhiteSpace(details) ? summary : $"{summary}閿涙details}";
            return (false, msg, null);
        }
    }

    /// <summary>
    /// 鐟欙絾鐎芥径鏍劥 Bot 娴兼俺鐦介惄顔界垼閿涘牏鏁ゆ禍搴℃倵缂侇厼褰傞柅浣圭Х閹?缁涘绶熼崶鐐差槻閿涘鈧?    /// 閺€顖涘瘮閿涙xxxbot閵嗕簛xxbot閵嗕弓ttps://t.me/xxxbot閵嗕辜g://resolve?domain=xxxbot&start=abc
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
                return (false, "閺冪姵纭堕懢宄板絿 Bot access_hash", null, null);

            var target = new ResolvedChatTarget(
                new InputPeerUser(user.id, user.access_hash),
                "@" + username,
                user.id.ToString(CultureInfo.InvariantCulture));
            return (true, null, target, "@" + username);
        }
        catch (Exception ex)
        {
            var (summary, details) = MapTelegramException(ex);
            var msg = string.IsNullOrWhiteSpace(details) ? summary : $"{summary}閿涘瘚details}";
            return (false, msg, null, null);
        }
    }

    public sealed record ResolvedChatTarget(InputPeer Peer, string Title, string CanonicalId);

    /// <summary>
    /// 鐟欙絾鐎界紘銈囩矋/妫版垿浜鹃惄顔界垼閿涘本鏁幐渚婄窗
    /// - 閻劍鍩涢崥?闁剧偓甯撮敍娆痷sername閵嗕菇sername閵嗕弓ttps://t.me/xxx閵嗕辜.me/xxx閵嗕辜g://join?invite=hash
    /// - 妫版垿浜?缂囥倗绮?ID閿?23456閵?123456閵?1001234567890
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
                return (false, "閻╊喗鐖ｆ稉铏光敄", null);

            var client = await GetOrCreateConnectedClientAsync(accountId, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            if (TryParseChatIdCandidate(raw, out var normalizedId))
            {
                var resolvedById = await TryResolveChatByIdFromDialogsAsync(client, normalizedId, cancellationToken);
                if (resolvedById != null)
                    return (true, null, resolvedById);

                return (false, $"閺堫亝澹橀崚?chatId={raw} 鐎电懓绨查惃鍕參缂?妫版垿浜鹃敍鍫ｎ嚞绾喛顓荤拠銉ㄥ閸欏嘲鍑￠崝鐘插弳閻╊喗鐖ｉ敍?, null);
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
                return (false, "閺冪姵纭剁憴锝嗙€介惄顔界垼缂囥倗绮?妫版垿浜?, null);

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
            var msg = string.IsNullOrWhiteSpace(details) ? summary : $"{summary}閿涙details}";
            return (false, msg, null);
        }
    }

    /// <summary>
    /// 閸氭垵鍑＄憴锝嗙€介惃鍕參缂?妫版垿浜鹃惄顔界垼閸欐垿鈧焦鏋冮張顒佺Х閹垬鈧?    /// </summary>
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
                return (false, "濞戝牊浼呴崘鍛啇娑撹櫣鈹?, null);

            var client = await GetOrCreateConnectedClientAsync(accountId, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            var sent = await client.SendMessageAsync(target.Peer, text, null, replyToMessageId ?? 0);
            return (true, null, sent.id);
        }
        catch (Exception ex)
        {
            var (summary, details) = MapTelegramException(ex);
            var msg = string.IsNullOrWhiteSpace(details) ? summary : $"{summary}閿涙details}";
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
                return (false, $"缁涘绶熸宀冪槈濞戝牊浼呯搾鍛閿涘澖timeoutSeconds} 缁夋帪绱?, null);

            if (messageFilter != null
                && stopOnUnmatchedMention
                && !messageFilter(update)
                && IsMentionOrReply(update, currentUsername, sentMessageId))
            {
                return (false, "妤犲矁鐦夊☉鍫熶紖閺堫亜鎳℃稉顓炲彠闁款喛鐦?濮濓絽鍨敍灞藉嚒鐠哄疇绻?, null);
            }

            var candidate = await BuildVerificationCandidateAsync(
                client,
                update.Message,
                currentUsername,
                sentMessageId,
                cancellationToken);

            return candidate == null
                ? (false, "閸栧綊鍘ら崚鎵畱妤犲矁鐦夊☉鍫熶紖娑撹櫣鈹栭敍灞炬￥濞夋洘澧界悰?AI 鐠囧棗鍩?, null)
                : (true, null, candidate);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            var (summary, details) = MapTelegramException(ex);
            var msg = string.IsNullOrWhiteSpace(details) ? summary : $"{summary}閿涙details}";
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
                return (false, "閹稿鎸崇紓鍝勭毌 callback_data");

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
            var msg = string.IsNullOrWhiteSpace(details) ? summary : $"{summary}閿涙details}";
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

        if (ContainsAny(text, "閸ㄥ啫婧囬獮鍨啞", "楠炲灝鎲?, "娑撳秳绨ｆ径鍕倞", "瀹告彃鍨归梽?, "鏉╂繆顫?, "鐏忎胶顩?)
            && !ContainsAny(text, "妤犲矁鐦?, "妤犲矁鐦夐惍?, "閺嶏繝鐛?, "captcha"))
        {
            return false;
        }

        if (ContainsAny(text,
                "妤犲矁鐦?,
                "妤犲矁鐦夐惍?,
                "閺嶏繝鐛?,
                "鐠囩兘鈧瀚?,
                "閻愮懓鍤?,
                "閹稿鎸?,
                "鐎瑰本鍨氭宀冪槈",
                "鐠囧嘲娲栨径?,
                "缁涙梹顢?,
                "缁犳绱?,
                "缁涘绨径姘毌",
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
               || text.Contains("鑴?, StringComparison.Ordinal)
               || text.Contains("姊?, StringComparison.Ordinal)
               || text.Contains("閿?, StringComparison.Ordinal)
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
            throw new ArgumentException("闁剧偓甯?閻劍鍩涢崥宥勮礋缁?, nameof(input));

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

        // 閻╁瓨甯撮弰?t.me/xxx
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
            throw new ArgumentException("Bot 閻劍鍩涢崥宥勮礋缁?, nameof(input));

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

        // https://t.me/xxxbot?start=abc 閹?t.me/xxxbot?start=abc
        if (s.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || s.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            || s.StartsWith("t.me/", StringComparison.OrdinalIgnoreCase)
            || s.StartsWith("telegram.me/", StringComparison.OrdinalIgnoreCase))
        {
            var url = s.Contains("://", StringComparison.Ordinal) ? s : "https://" + s;
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                throw new ArgumentException("Bot 闁剧偓甯撮弽鐓庣础閺冪姵鏅?, nameof(input));

            var path = (uri.AbsolutePath ?? string.Empty).Trim('/');
            var firstSeg = path.Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(firstSeg))
                throw new ArgumentException("Bot 闁剧偓甯存稉顓犲繁鐏忔垹鏁ら幋宄版倳", nameof(input));

            s = firstSeg;

            var query = ParseQueryString(uri.Query);
            if (query.TryGetValue("start", out var start) && !string.IsNullOrWhiteSpace(start))
                startFromLink = NormalizeBotStartParameter(start);
        }

        s = s.Trim().TrimStart('@');

        // 閺€顖涘瘮閿涙username?start=abc閿涘牊妫?http/tg 閸楀繗顔呴敍?        var question = s.IndexOf('?');
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
            throw new ArgumentException("Bot 閻劍鍩涢崥宥勮礋缁?, nameof(input));

        if (s.StartsWith("+", StringComparison.Ordinal))
            throw new ArgumentException("闁偓鐠囩兘鎽奸幒銉ょ瑝閺?Bot 閻劍鍩涢崥宥忕礉鐠囩柉绶崗?@xxxbot 閹?t.me/xxxbot", nameof(input));

        if (!System.Text.RegularExpressions.Regex.IsMatch(s, "^[A-Za-z0-9_]{5,64}$"))
            throw new ArgumentException("Bot 閻劍鍩涢崥宥嗙壐瀵繑妫ら弫?, nameof(input));

        // 鐢瓕顫夐幆鍛枌閿涙俺顩﹀Ч鍌欎簰 bot 缂佹挸鐔?        // 娓氬顦婚敍?        // 1) 閺勬儳绱＄紒娆庣啊 start 閸欏倹鏆熼敍鍫濈埗鐟欎椒绨?t.me/xxx?start=abc 閹?@xxx?start=abc閿?        // 2) 鐠嬪啰鏁ら弬瑙勬绾喒鈧粍瀵?Bot 婢跺嫮鎮婇垾?        if (!s.EndsWith("bot", StringComparison.OrdinalIgnoreCase)
            && string.IsNullOrWhiteSpace(startFromLink)
            && !assumeBotUsername)
            throw new ArgumentException("閻╊喗鐖ｉ惇瀣崳閺夈儰绗夐弰?Bot 閻劍鍩涢崥宥忕礄闂団偓娴?bot 缂佹挸鐔敍?, nameof(input));

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
    /// 閺囧瓨鏌婅ぐ鎾冲鐠愶箑褰挎径鏉戝剼閿涘牓娼ら幀浣告禈閻楀浄绱氶妴?    /// </summary>
    public async Task<(bool Success, string? Error)> UpdateProfilePhotoAsync(
        int accountId,
        Stream fileStream,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (fileStream == null)
                return (false, "婢舵潙鍎氶弬鍥︽娑撹櫣鈹?);

            fileName = (fileName ?? "avatar.jpg").Trim();
            if (string.IsNullOrWhiteSpace(fileName))
                fileName = "avatar.jpg";

            var client = await GetOrCreateConnectedClientAsync(accountId, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            await using var encoded = await TelegramImageProcessor.PrepareAvatarJpegAsync(fileStream, cancellationToken);
            var inputFile = await client.UploadFileAsync(encoded, "avatar.jpg");
            cancellationToken.ThrowIfCancellationRequested();

            if (inputFile == null)
                return (false, "婢舵潙鍎氭稉濠佺炊婢惰精瑙﹂敍姘瑐娴肩姷绮ㄩ弸婊€璐熺粚?);

            await client.Photos_UploadProfilePhoto(inputFile, video: null, video_start_ts: null, video_emoji_markup: null, bot: null, fallback: false);
            return (true, null);
        }
        catch (UnknownImageFormatException)
        {
            return (false, "婢舵潙鍎氭稉濠佺炊婢惰精瑙﹂敍姘瑝閺€顖涘瘮閻ㄥ嫬娴橀悧鍥ㄧ壐瀵骏绱欏楦款唴娴ｈ法鏁?JPG/PNG閿?);
        }
        catch (Exception ex)
        {
            var (summary, details) = MapTelegramException(ex);
            var msg = string.IsNullOrWhiteSpace(details) ? summary : $"{summary}閿涙details}";
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
                ?? throw new InvalidOperationException($"Account does not exist: {accountId}");

            cancellationToken.ThrowIfCancellationRequested();

            var apiId = ResolveApiId(account);
            var apiHash = ResolveApiHash(account);
            var sessionKey = ResolveSessionKey(account, apiHash);

            if (string.IsNullOrWhiteSpace(account.SessionPath))
                throw new InvalidOperationException("The account is missing SessionPath and cannot connect to Telegram.");

            var absoluteSessionPath = Path.GetFullPath(account.SessionPath);
            if (File.Exists(absoluteSessionPath) && SessionDataConverter.LooksLikeSqliteSession(absoluteSessionPath))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var converted = await SessionDataConverter.TryConvertSqliteSessionFromJsonAsync(
                    phone: account.Phone,
                    apiId: account.ApiId,
                    apiHash: account.ApiHash,
                    sqliteSessionPath: absoluteSessionPath,
                    logger: _logger);

                if (!converted.Ok)
                {
                    throw new InvalidOperationException(
                        $"The account session file is SQLite format and could not be converted automatically: {account.SessionPath}. Reason: {converted.Reason}");
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
                    "Connect Telegram",
                    () => client.ConnectAsync(),
                    cancellationToken,
                    resetClientOnTimeout: true);
                cancellationToken.ThrowIfCancellationRequested();

                if (client.User == null && (client.UserId != 0 || account.UserId != 0))
                {
                    await ExecuteTelegramRequestAsync(
                        accountId,
                        "Resume Telegram login",
                        () => client.LoginUserIfNeeded(reloginOnFailedResume: false),
                        cancellationToken,
                        resetClientOnTimeout: true);
                }

                if (client.User == null)
                    throw new InvalidOperationException("The account is not logged in or the session is invalid. Please log in again.");

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
                        "The account session file could not be parsed. This usually means ApiId/ApiHash does not match the original session, or the session file is corrupted.",
                        ex);
                }

                throw new InvalidOperationException($"Telegram session bootstrap failed: {ex.Message}", ex);
            }
        }

        throw new InvalidOperationException($"Telegram session bootstrap failed: {lastError?.Message}", lastError);
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

            throw new TimeoutException($"Telegram 鐠囬攱鐪扮搾鍛閿涙operation} 鐡掑懓绻?{timeout.TotalSeconds:0} 缁夋帪绱濋崣顖濆厴閺?Session 婢惰鲸鏅ラ妴浣藉閸欏嘲褰堥梽鎰┾偓浣虹秹缂佹粌绱撶敮鍛婂灗娴狅絿鎮婂鍌氱埗");
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

            throw new TimeoutException($"Telegram 鐠囬攱鐪扮搾鍛閿涙operation} 鐡掑懓绻?{timeout.TotalSeconds:0} 缁夋帪绱濋崣顖濆厴閺?Session 婢惰鲸鏅ラ妴浣藉閸欏嘲褰堥梽鎰┾偓浣虹秹缂佹粌绱撶敮鍛婂灗娴狅絿鎮婂鍌氱埗");
        }
    }

    private int ResolveApiId(Account account)
    {
        if (int.TryParse(_configuration["Telegram:ApiId"], out var globalApiId) && globalApiId > 0)
            return globalApiId;
        if (account.ApiId > 0)
            return account.ApiId;
        throw new InvalidOperationException("閺堫亪鍘ょ純顔煎弿鐏炩偓 ApiId閿涘奔绗栫拹锕€褰跨紓鍝勭毌 ApiId");
    }

    private string ResolveApiHash(Account account)
    {
        var global = _configuration["Telegram:ApiHash"];
        if (!string.IsNullOrWhiteSpace(global))
            return global.Trim();
        if (!string.IsNullOrWhiteSpace(account.ApiHash))
            return account.ApiHash.Trim();
        throw new InvalidOperationException("閺堫亪鍘ょ純顔煎弿鐏炩偓 ApiHash閿涘奔绗栫拹锕€褰跨紓鍝勭毌 ApiHash");
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

        var flagText = flags.Count == 0 ? "閺? : string.Join(", ", flags);
        return $"閺勭數袨閿涙profile.DisplayName}閿涙稓鏁ら幋宄版倳閿涙profile.Username ?? "-"}閿涙瞼serId閿涙profile.UserId}閿涙稒鐖ｇ拋甯窗{flagText}";
    }

    private async Task<CreateChannelProbeResult> ProbeCreateChannelCapabilityAsync(Client client, int accountId, CancellationToken cancellationToken = default)
    {
        // 濞夈劍鍓伴敍姘崇箹閺勵垪鈧粍绻佹惔锔藉赴濞村鈧繐绱濇导姘灡瀵ゅ搫鑻熼崚鐘绘珟娑撯偓娑擃亝绁寸拠鏇㈩暥闁挶鈧?        var title = $"tp-check-{DateTime.UtcNow:MMddHHmmss}";
        const string about = "Telegram Panel create-channel probe (auto delete)";

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            UpdatesBase updates;
            try
            {
                updates = await ExecuteTelegramRequestAsync(
                    accountId,
                    "閸掓稑缂撳ù瀣槸妫版垿浜鹃幒銏＄ゴ鐠愶箑褰块悩鑸碘偓?,
                    () => client.Channels_CreateChannel(title: title, about: about, broadcast: true),
                    cancellationToken,
                    resetClientOnTimeout: true);
            }
            catch (RpcException ex) when (ex.Code == 420 && string.Equals(ex.Message, "FROZEN_METHOD_INVALID", StringComparison.OrdinalIgnoreCase))
            {
                return new CreateChannelProbeResult(false, true, "鐠愶箑褰?ApiId 閸欐妾洪敍姝峞legram 鏉╂柨娲?FROZEN_METHOD_INVALID閿涘牆鍨卞娲暥闁挻甯撮崣锝堫潶閸愯崵绮ㄩ敍?);
            }

            cancellationToken.ThrowIfCancellationRequested();

            var channel = updates.Chats.Values.OfType<TL.Channel>().FirstOrDefault();
            if (channel == null)
                return new CreateChannelProbeResult(false, false, "閸掓稑缂撳ù瀣槸妫版垿浜炬径杈Е閿涙碍婀潻鏂挎礀 Channel");

            try
            {
                // 缁斿宓嗛崚鐘绘珟閿涘矂浼╅崗宥囨殌娑撳鐎崷楣冾暥闁?                var input = new InputChannel(channel.id, channel.access_hash);
                await ExecuteTelegramRequestAsync(
                    accountId,
                    $"閸掔娀娅庡ù瀣槸妫版垿浜?{channel.id})",
                    () => client.Channels_DeleteChannel(input),
                    cancellationToken,
                    resetClientOnTimeout: false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Probe channel created but failed to delete (account {AccountId}, channel {ChannelId})", accountId, channel.id);
                return new CreateChannelProbeResult(false, false, $"閸掓稑缂撳ù瀣槸妫版垿浜鹃幋鎰閿涘奔绲鹃崚鐘绘珟婢惰精瑙﹂敍姝縠x.Message}閿涘牐顕幍瀣З閸掔娀娅庢０鎴︿壕閿涙title}閿?);
            }

            return new CreateChannelProbeResult(true, false, "閸欘垳鏁?);
        }
        catch (Exception ex)
        {
            var msg = ex.Message ?? "閺堫亞鐓￠柨娆掝嚖";
            return new CreateChannelProbeResult(false, false, msg);
        }
    }

    private sealed record CreateChannelProbeResult(bool Success, bool IsFrozen, string Message);

    /// <summary>
    /// 鐏?Telegram 瀵倸鐖堕弰鐘茬殸娑撳搫褰茬拠鑽ゆ畱閹芥顩﹂崪宀冾嚊閹懌鈧?    /// </summary>
    private static bool IsBotCallbackTimeout(Exception ex)
    {
        var msg = ex.Message ?? string.Empty;
        return msg.Contains("BOT_RESPONSE_TIMEOUT", StringComparison.OrdinalIgnoreCase);
    }

    public static (string summary, string details) MapTelegramException(Exception ex)
    {
        var msg = ex.Message ?? string.Empty;

        if (ex is TimeoutException
            || msg.Contains("timed out", StringComparison.OrdinalIgnoreCase)
            || msg.Contains("timeout", StringComparison.OrdinalIgnoreCase))
            return ("连接超时", msg);

        if (msg.Contains("EMAIL_HASH_EXPIRED", StringComparison.OrdinalIgnoreCase))
            return ("邮箱验证码已过期", msg);

        if (msg.Contains("EMAIL_NOT_SETUP", StringComparison.OrdinalIgnoreCase))
            return ("邮箱验证未启用", msg);

        if (msg.Contains("EMAIL_UNCONFIRMED", StringComparison.OrdinalIgnoreCase))
            return ("邮箱未确认", msg);

        if (msg.Contains("EMAIL_TOKEN_INVALID", StringComparison.OrdinalIgnoreCase))
            return ("邮箱验证码无效", msg);

        if (msg.Contains("EMAIL_INVALID", StringComparison.OrdinalIgnoreCase))
            return ("邮箱格式无效", msg);

        if (msg.Contains("EMAIL_NOT_ALLOWED", StringComparison.OrdinalIgnoreCase))
            return ("邮箱不允许使用", msg);

        if (msg.Contains("FROZEN_METHOD_INVALID", StringComparison.OrdinalIgnoreCase))
            return ("账号接口被冻结", msg);

        if (msg.Contains("FLOOD_WAIT", StringComparison.OrdinalIgnoreCase))
            return ("触发限流", msg);

        if (msg.Contains("CHANNEL_MONOFORUM_UNSUPPORTED", StringComparison.OrdinalIgnoreCase))
            return ("群组接口不支持", msg);

        if (msg.Contains("AUTH_KEY_UNREGISTERED", StringComparison.OrdinalIgnoreCase))
            return ("Session 失效", msg);

        if (msg.Contains("AUTH_KEY_DUPLICATED", StringComparison.OrdinalIgnoreCase))
            return ("Session 冲突", msg);

        if (msg.Contains("SESSION_REVOKED", StringComparison.OrdinalIgnoreCase))
            return ("Session 已撤销", msg);

        if (msg.Contains("SESSION_PASSWORD_NEEDED", StringComparison.OrdinalIgnoreCase))
            return ("需要两步验证密码", msg);

        if (msg.Contains("CODE_INVALID", StringComparison.OrdinalIgnoreCase))
            return ("验证码错误", msg);

        if (msg.Contains("PHOTO_FILE_MISSING", StringComparison.OrdinalIgnoreCase))
            return ("头像上传失败", msg);

        if (msg.Contains("PHONE_NUMBER_BANNED", StringComparison.OrdinalIgnoreCase)
            || msg.Contains("USER_DEACTIVATED_BAN", StringComparison.OrdinalIgnoreCase))
            return ("账号被封禁", msg);

        if (msg.Contains("Can't read session block", StringComparison.OrdinalIgnoreCase))
            return ("Session 无法读取", msg);

        return ("连接失败", msg);
    }
}
