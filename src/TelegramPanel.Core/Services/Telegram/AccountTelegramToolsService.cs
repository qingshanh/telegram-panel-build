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
/// 闂傚倷娴囧畷鍨叏閻㈢绀夌憸蹇曞垝婵犳艾绠ｉ柨婵嗗暕濮规鏌ｉ悩鍏呰埅闁告柨绉堕埀顒佺閼归箖鍩為幋锔藉亹闁圭粯甯楀▓鏌ユ⒑?/ 缂傚倸鍊搁崐椋庢閿熺姴鍨傞梻鍫熺〒閺嗭箓鏌ｉ姀銈嗘锭闁搞劍绻冪换娑橆啅椤旇崵鍑归梺缁樺笧缁垶骞堥妸銉庣喐寰勭粙鎸庡創闂備礁鎲￠悷銏ゅ磻?/ 闂傚倸鍊风欢姘焽閼姐倖瀚婚柣鏃傚帶缁€澶愬箹濞ｎ剙濡奸柛姘秺楠炴牗娼忛崜褏蓱婵犳鍨伴妶鎼佸蓟閻旂⒈鏁嶉柛鈩冾殕濠€浼村冀閿涘嫮纾介柛灞剧懆閸忓苯鈹戦姘煎殶婵″弶鍔欏鎾閻樼數鏆?/// </summary>
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
    /// 闂傚倸鍊风粈渚€骞夐敍鍕殰闁跨喓濮寸紒鈺呮⒑椤掆偓缁夋挳鎷戦悢灏佹斀闁绘ɑ褰冮顏堟倵濮橆剦鐓奸柡灞诲姂瀵潙螖閳ь剚绂嶆ィ鍐╁仭婵犲﹤瀚欢鍙夌箾绾绡€妤犵偛鍟撮崺锟犲川椤斿吋鐤傜紓浣稿⒔婢ф鏁嬫繛瀛樼矋閸庢娊濡撮幒鎴僵妞ゆ帒锕ゆ慨娑氱磽娴ｆ彃浜鹃梺鍛婃处閸ㄩ亶鎮￠弴鐔虹闁糕剝鈼ら鍫熷€堕柨鏇炲€归悡鍐喐濠婂牆绀堟繛鍡樻嫴閸ヮ剙绠ユい鏃囨鎼村﹥绻涙潏鍓хК妞ゎ偄顦靛畷鏇㈡偄閸忚偐鍙嗛梺鍝勫暙濞诧箓藟婢舵劖鍋ｇ憸宥夋偤閵娧勫床婵炴垯鍨圭痪褔鏌熼幖顓炲箹闁告柡鍋撶紓鍌氬€峰ù鍥ㄣ仈缁嬫鐔嗘俊顖濇硶娴滈亶姊绘笟鈧埀顒傚仜閼活垱鏅堕鈧Λ浣瑰緞閹邦厼鈧敻鏌ㄥ☉娆忣暢闁稿鎹囬弻娑氣偓锝庡亝鐏忣厽銇勯婊冨鐎规洘锕㈡俊姝岊槺闁汇垹顭烽弻锝嗘償閿濆懍鍖栭梺绋匡攻閻楁洟鈥旈崘鈺冾浄閻庯絻鍔夐崑鎾诲磼閻愭煡鍞堕梺鍝勬川閸犳捇宕濋崼鏇熲拺闁革富鍘兼禍鐐箾閸忚偐鎳勭€垫澘瀚鍏煎緞鐎ｎ剙骞堟繝鐢靛仜濡瑩鎮疯缁瑧绱掑Ο鍦畾闂佸憡鐟ラˇ浼村磹閹邦喒鍋撶憴鍕８闁稿海鏁诲顐﹀礃閳哄啰鎳濋梺鎼炲劘閸斿繘寮茶濮婄粯鎷呴崨濠傛殘闂佸憡姊归敋闁伙絿鍏橀幃浠嬫濞戞浜伴梻渚€娼ц墝闁哄懏鐩幃姗€鎼归銈囩槇闂佸壊鐓堥崑鍛焊閻㈢數纾奸柍褜鍓氬鍕箾閹烘垹绉虹€规洘顨婇幊婵嬪级濞嗙偓顥ら梻鍌欑劍閹爼宕濈仦鐣屾殾妞ゆ帒瀚烽弫瀣箾閸℃ɑ灏ù鑲╁█閺屾盯寮撮妸銉︾亪闂佺粯绻嶉崹璺侯潖濞差亜宸濆┑鐘插€婚鍌涚節閻㈤潧浜归柛瀣尭閳规垿鎮欓崣澶婃闁诲孩绋堥弲婵堝垝濞嗘劕绶為柟閭﹀墰閸旓箑顪冮妶鍡楃瑐闁煎啿鐖奸崺銏ゅ籍閳ь剟濡甸崟顔剧杸闁圭偓娼欏▍褔姊洪棃娑氬闁瑰憡濞婇獮鍐亹閹烘垹鍊為梺闈涱煭婵″洭藝閺夋娓婚柕鍫濈箳閸掍即鏌ｉ弽褋鍋㈢€殿喖顭峰畷鍗炩槈濡⒈鍟岄梻浣告啞濞诧箓宕㈡ィ鍏?
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
                "闂傚倸鍊烽懗鍫曞箠閹捐搴婇柡灞诲剸閸ヮ剦鏁嶆繝闈涘濮规姊洪幖鐐插姉闁哄懏绮庨埀顒佽壘椤兘寮婚妸鈺佸嵆婵°倐鍋撳ù婊堢畺閹嘲顭ㄩ崟顐偓婊勭箾婢跺绀嬫鐐村姌椤﹀湱鈧娲滈崰鏍€佸Δ浣瑰閺夌偟澧楅惈?,
                () => client.Users_GetUsers(InputUser.Self),
                cancellationToken,
                resetClientOnTimeout: true);
            cancellationToken.ThrowIfCancellationRequested();
            var self = users.OfType<User>().FirstOrDefault();

            if (self == null)
            {
                var missingProfile = new TelegramAccountStatusResult(
                    Ok: false,
                    Summary: "闂傚倸鍊风粈渚€骞栭锕€鐤柣妤€鐗婇崣蹇涙煟閵忕姷浠涢柛蹇旂矒閺屾盯顢曢敐鍡欘槰闂佹悶鍊栧濠氬焵椤掑倹鍤€閻庢凹鍘奸…鍨熼悡搴ｇ瓘闂佺鍕垫畷闁稿缍侀弻鐔煎级閸噮鏆㈤梺浼欑秬娴滎剟骞夐幖浣哥閻忕偛澧介妴濠囨⒑閸︻収娼掗柛銉戝拋妲规俊鐐€栫敮濠囨嚄閼哥數顩?,
                    Details: "Users_GetUsers(Self) 闂傚倸鍊风粈渚€骞栭锔藉亱婵犲﹤瀚々鏌ユ煥濠靛棙澶勭€规洘鐓￠弻鐔告綇閸撗呮殸闂?User",
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

            var summary = "婵犵數濮甸鏍窗濡ゅ啯宕查柟閭﹀枛缁躲倕霉閻樺樊鍎愰柛?;
            if (profile.IsDeleted)
                summary = "闂傚倷娴囧畷鍨叏閻㈢绀夌憸蹇曞垝婵犳艾绠ｉ柨婵嗗暕濮规姊洪崜鑼帥闁革綆鍣ｅ畷顖炲川鐎涙鍘卞┑鐐叉濞存艾危瑜版帗鐓涢柛鈩冾殘婢э附鎱ㄦ繝鍕妺闁诡垱妫冮獮姗€宕楅崨顓ф/闂傚倷娴囧畷鐢稿磻閻愬搫绀勭憸鐗堝笒绾惧鏌涢弴銊ョ仩闁搞劌鍊婚幉鎼佹偋閸繄鐟查梺?;
            else if (profile.IsRestricted)
                summary = "闂傚倷娴囧畷鍨叏閻㈢绀夌憸蹇曞垝婵犳艾绠ｉ柨婵嗗暕濮规姊虹粔鍡楀濞堟棃鏌ｉ鐕佹畷濞ｅ洤锕俊鍫曞川椤斿吋顏℃俊鐐€曠€涒晠鎮烽埡鍛摕闁靛鍎Σ鍫熶繆椤栨繍鍞虹紒鍓併仏stricted闂?;

            if (probeCreateChannel)
            {
                var probe = await ProbeCreateChannelCapabilityAsync(client, accountId, cancellationToken);
                if (probe.IsFrozen)
                {
                    var frozen = new TelegramAccountStatusResult(
                        Ok: false,
                        Summary: "闂傚倷娴囧畷鍨叏閻㈢绀夌憸蹇曞垝婵犳艾绠ｉ柨婵嗗暕濮规鏌ｉ悩鍏呰埅闁告柨绉瑰畷鎴﹀冀閵婏絼绨婚梺瑙勫閺呮盯鎮橀埡鍛厸閻庯綆鍋嗛幊鍛磼鏉堛劍灏伴柟宄版噽閹叉挳宕熼鈧崜鐢电磽閸屾瑦绁板瀛樻倐閹兘鍩℃担鐑樻濠殿喗锕╅崗妯何ｉ崼鐔剁箚妞ゆ牗绻傛禍鐟邦熆鐠哄搫顏ǎ鍥э躬椤㈡洟濡堕崪浣规瘔闂備焦鎮堕崝宥団偓绗涘懐鐭夌€广儱妫庨崑鍛存煕閹扳晛濡挎い鏂匡躬濮婃椽妫冨☉杈ㄐら梺绋挎唉娴滎剛妲愰悙鍝勭缂備焦顭囬崢鍗炩攽閻愬弶顥為柛鏃撶畱閳绘挸顭ㄩ崟顏嗙畾闂佺粯鍔﹂崜姘跺Φ濠靛鐓?,
                        Details: $"闂傚倸鍊风粈渚€骞夐敍鍕殰婵°倕鍟伴惌娆撴煙鐎电啸缁惧彞绮欓弻鐔煎箲閹邦厼娑х紓浣瑰敾缂嶄線寮婚悢鍛婄秶闁告挆鍛崶濠电姰鍨奸～澶娒洪悢鐓庤摕闁绘梻鍘х粻鎺楁煙閻戞ê鐏╁ù鐘插⒔缁辨挻鎷呮搴″闂佺懓鎲℃繛濠傤嚕椤愩埄鍚嬮柛婊€鐒﹀銊х磽娴ｈ姤顏犳鐐茬獖be.Message}{Environment.NewLine}{BuildProfileDetails(profile)}",
                        CheckedAtUtc: checkedAt,
                        Profile: profile);
                    await TryPersistStatusAsync(accountId, frozen, account, persistProfile: true, cancellationToken: cancellationToken);
                    return frozen;
                }

                if (!probe.Success)
                {
                    var failed = new TelegramAccountStatusResult(
                        Ok: false,
                        Summary: "闂傚倸鍊风粈渚€骞夐敍鍕殰婵°倕鍟伴惌娆撴煙鐎电啸缁惧彞绮欓弻鐔煎箲閹邦厼娑х紓浣瑰敾缂嶄線寮婚悢鍛婄秶闁告挆鍛崶濠电姰鍨奸～澶娒洪悢鐓庤摕闁绘梻鍘х粻鎺楁煙閻戞ê鐏╁ù鐘插⒔缁辨挻鎷呮搴М闂佸鏉垮妞ゆ洏鍎靛畷鐔碱敆娴ｈ櫣肖婵＄偑鍊栭崝鎴﹀磿?,
                        Details: $"闂傚倸鍊风粈渚€骞夐敍鍕殰婵°倕鍟伴惌娆撴煙鐎电啸缁惧彞绮欓弻鐔煎箲閹邦厼娑х紓浣瑰敾缂嶄線寮婚悢鍛婄秶闁告挆鍛崶濠电姰鍨奸～澶娒洪悢鐓庤摕闁绘梻鍘х粻鎺楁煙閻戞ê鐏╁ù鐘插⒔缁辨挻鎷呮搴″闂佺懓鎲℃繛濠傤嚕椤愩埄鍚嬮柛婊€鐒﹀銊х磽娴ｈ姤顏犳鐐茬獖be.Message}{Environment.NewLine}{BuildProfileDetails(profile)}",
                        CheckedAtUtc: checkedAt,
                        Profile: profile);
                    await TryPersistStatusAsync(accountId, failed, account, persistProfile: true, cancellationToken: cancellationToken);
                    return failed;
                }

                // 闂傚倸鍊峰ù鍥綖婢跺顩插ù鐘差儏閸ㄥ倿鏌ц箛鎾磋础闁活厼鐗撻弻鐔虹磼閵忕姵鐏堢紓浣哄缂嶄線寮婚悢鍏肩劷闁挎洍鍋撳褜鍠楅妵鍕敃閵堝應鏋呴梺鍝勮閸斿矂鍩ユ径濞㈢喐绗熼娑卞敳缂傚倸鍊烽悞锕傘€冮崨姝ゅ洭骞庨挊澹╋箓鏌熼悧鍫熺凡缁绢厸鍋撻梻浣虹帛閸旀洟鎮洪妸鈺佺？闁靛鏅滈埛鎴︽煕濞戞﹩鐓柣鎺嶇矙閺屾盯鎮╅搹顐㈤瀺闂侀€涚┒閸旀垿鐛弽銊﹀闁告縿鍎抽弳鐘测攽閻愬瓨灏伴柤褰掔畺閺佸啴鏁傞崜褏鐒兼繝銏ｆ硾閳洝銇愰幒鎾充汗闂佸憡鐟ラˇ顖氣枔瀹€鍕參婵☆垳鍘ч弸娑㈡煛鐏炲墽顬肩紒鐘崇洴楠炴﹢寮堕幋鐐垫殮濠碉紕鍋戦崐鏍哄鈧幆鍕敍閻愰潧绁?                var okWithProbe = new TelegramAccountStatusResult(
                    Ok: true,
                    Summary: summary,
                    Details: $"闂傚倸鍊风粈渚€骞夐敍鍕殰婵°倕鍟伴惌娆撴煙鐎电啸缁惧彞绮欓弻鐔煎箲閹邦厼娑х紓浣瑰敾缂嶄線寮婚悢鍛婄秶闁告挆鍛崶濠电姰鍨奸～澶娒洪悢鐓庤摕闁绘梻鍘х粻鎺楁煙閻戞ê鐏╁ù鐘插⒔缁辨挻鎷呮搴″闂佺懓鎲℃繛濠傤嚕椤愩埄鍚嬮柛婊€鐒﹂崓闈涱渻閵堝棗鍧婇柛瀣崌閹鎷呴崷顓炲绩闂佸搫鐭夌紞浣规叏閳ь剟鏌嶉崫鍕殭閻㈩垬鍔庣槐鎾存媴娴犲鎽甸梺鑽ゅ枂閸庣敻銆佸▎鎺旂杸婵炴垶鐟ュ▓銈夋煟閻樼儤顏犻柛搴涘€濋、娆撳炊椤掍讲鎷洪梺鍛婄☉閿曘儵鎮￠悢鍏肩厱闁哄倽娉曡倴缂備緡鍠掗弲婊呮崲濠靛棭娼╂い鎾跺仒閸濇姊绘担铏广€婇柛鎾寸箞閳ワ箓宕堕鈧洿闂佸湱铏庨崰妤呭磻閿濆鐓曢柕澶涚到婵＄晫绱掗埀顒傗偓锝庡亖娴滄粓鏌ㄥ┑鍡樺櫧闁糕晪缍侀弻锛勪沪閼恒儺妫﹀Δ鐘靛仦閻熲晛鐣烽悢纰辨晣闁绘瑢鍋撴俊鑼棩Environment.NewLine}{BuildProfileDetails(profile)}",
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
                Summary: "闂備浇顕уù鐑藉箠閹捐绠熼梽鍥Φ閹版澘绀冩い鎾寸矆濮规姊洪幖鐐插妧闁逞屽墮鍗?,
                Details: "闂傚倸鍊烽懗鍫曞箠閹剧粯鍊舵繝闈涚墢閻挾鈧娲栧ú銊х矆婵犲洦鐓涢柛灞句緱閸庛儵鏌涢悢閿嬪殗闁哄瞼鍠撶槐鎺懳熼搹鍦噯缂傚倷绀佸鍓佲偓绗涘懏宕叉繛鎴欏灩缁狅絾绻濋棃娑氬闁冲嘲锕︾槐鎾存媴娴犲鎽甸梺鑽ゅ櫐缁犳挻淇婇悽绋跨妞ゆ牗绋戞禒娲⒑閸涘﹦鈽夐柨鏇樺劦婵℃挳宕橀瑙ｆ嫼缂備礁顑堝▔鏇犵不閹绘巻鏀介柣鎰嚋瀹搞儵鎽?闂傚倸鍊风粈渚€骞夐敍鍕殰闁跨喓濮寸紒鈺呮⒑椤掆偓缁夋挳鎷戦悢灏佹斀闁绘ɑ褰冮銈夋煕鐎ｎ偅灏伴柟宄版嚇瀹曟﹢鈥﹂幋鐑嗘闂傚倷鐒﹂幃鑸靛閸ヮ剙鐐婄憸蹇涙偂閹达附鈷戦柛婵嗗琚梺鍛婃煥妤犳悂寮抽埡鍛拻?,
                CheckedAtUtc: checkedAt);
        }
        catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested)
        {
            // Blazor 濠电姷顣槐鏇㈠磻閹达箑纾规俊銈呮噹閺嬩線鎮归崶褎鈻曢柣鎺曟闇夐柛蹇撳悑缂嶆垹绱掗埀顒勫幢濞戞瑢鎷洪梺鍓茬厛閸ｎ噣宕曢幇鐗堢厽闁圭虎鍨版禍?闂傚倸鍊风粈渚€骞栭锕€纾婚柛鈩冪☉閻鏌嶈閸撴稓妲愰幒鏃傜＜婵☆垰鍚嬮崚娑㈡倵濞堝灝娅橀柛鐘冲哺楠炲繒鈧綆鍠栭幑鑸点亜閹捐泛顎屾俊鑼筩oped 闂?DbContext 闂傚倸鍊风粈渚€骞夐敓鐘冲仭妞ゆ牜鍋涢崹鍌炴煟閵忋埄鐒剧紒鈧崒娑楃箚妞ゆ牗鐟ㄩ鐔兼煕閻旈攱鍤囬柡灞诲€栫缓浠嬪礈閸欏绶繝鐢靛仧閸樠囨偉閻撳寒娼栨繛宸簻缁犵敻鏌熼搹鐟颁沪闁轰線绠栧娲传閸曨偀鍋撴搴㈩偨婵娉涢弸浣衡偓骞垮劚閹冲危閸喓绠鹃柛鈩冾殘缁ㄥ潡鏌嶉崫鍕櫤闁稿﹦鏁婚弻锝夋偄閸涘﹦鍑￠梺鍝勬閸ㄦ椽濡甸崟顖ｆ晝闁靛繆鏅涢崜杈╃磽娴ｅ壊鍎忔い锕傛涧閻ｇ兘顢曢敃鈧粈瀣亜閹扳晛鈧牠宕戣缁绘繈鎮介棃娑楁勃闂佹悶鍔庨弫濠氬箖瑜旈獮鍥级鐠恒劌濮︽俊鐐€栧濠氬磻閹炬枼鏀介柛灞剧⊕閳锋帞绱掗鍛箺鐎垫澘瀚伴獮鍥敆閳ь剙顕ｅ畡閭︽富闁靛牆妫涙晶閬嶆煕鐎ｎ剙浠辩€殿喗鐓￠崺锟犲川椤旈棿鍖栭梻浣规偠閸庢潙鈻斿☉銏″€舵繛鎴欏灪閻?            return new TelegramAccountStatusResult(
                Ok: false,
                Summary: "闂備浇顕уù鐑藉箠閹捐绠熼梽鍥Φ閹版澘绀冩い鎾寸矆濮规姊洪幖鐐插妧闁逞屽墮鍗?,
                Details: "濠电姷顣槐鏇㈠磻閹达箑纾规俊銈呮噹閺嬩線鎮归崶褎鈻曢柣鎺嶇矙閺屸剝寰勭€ｉ潧鍔岄梺鍝ュ枎閹虫﹢寮婚悢铏圭＜婵☆垵娅ｉ悷銊╂偡濠婂嫬顥嬪┑鐐╁亾濠?闂傚倸鍊风粈渚€骞夐敍鍕殰闁跨喓濮寸紒鈺呮⒑椤掆偓缁夋挳鎷戦悢灏佹斀闁绘ê寮舵径鍕煛閸滀礁澧撮柟顔斤耿閹瑩妫冨☉妤€顥氶梻浣筋嚃閸ㄩ亶鎮烽妷鈺傜畳闂備焦瀵х换鍌毼涘☉銏″剹閹兼惌娼挎禍婊勩亜閹伴潧澧存俊缁㈠枤缁辨帞鈧綆浜跺Ο鈧銈冨灪閿曘垽骞冮姀銈呬紶闁靛鍎辨",
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
            // 濠电姷顣槐鏇㈠磻閹达箑纾规俊銈呮噹閺嬩線鎮归崶褎鈻曢柣?濠电姷鏁搁崑鐘诲箵椤忓棗绶ら柛鎾楀啫鐏婇柟鍏肩暘閸斿矂寮告笟鈧弻鏇㈠醇濠垫劖笑闂佸搫瀚ㄩ崕鐢稿蓟閿濆妫橀柛顭戝枟浜涢梻浣规偠閸婃牜鏁垾鎰佹綎闁绘垶蓱閸庣喖鏌熼幆褍甯犲瑙勬礃缁绘繂鈻撻崹顔界彲缂備浇灏崑鎰垝閸喓鐟归柍褜鍓熼妴浣割潩閼哥數鍔堕悗骞垮劚濡鈻撳鍫熺厽閹兼番鍔嶅☉褔鏌熺拠褏绡€闁?DbContext 闂傚倸鍊搁崐鐑芥倿閿曚降浜归柛鎰靛枟閺呮繈鏌曟径鍡樻珔婵☆偅锚閵嗘帒顫濋敐鍛闂備浇妗ㄧ粈浣虹矓閼哥數顩烽柨鏇炲€归崵宥夋煏婢跺牆鍔村ù鍏兼崌濮婄粯鎷呮搴濊缂備焦褰冮…宄扮暦閺囥垹纭€闁绘劕鐏氬▓濂告⒑缁洖澧茬紒瀣灴閹?        }
        catch (Exception ex)
        {
            // 闂傚倸鍊风粈渚€骞夐敓鐘冲仭闁挎洖鍊归崑瀣繆閵堝懎鏆熼柣顓熸崌閺岋綁骞嬮敐鍡╂闂佸湱鎳撻悥濂稿蓟閿濆绠ｉ柣鎰閸ㄥ潡骞冮悽鍓叉晬婵﹫绲鹃～宥夋⒑鐟欏嫬鍔ょ痪缁㈠幗閺呫儲绻濋悽闈涗沪闁圭鎲￠弲鑸垫償閵娿儳鐤呴梺缁橆殔閻楀﹥鍒婇幘顔界厱婵炴垶锕崝鐔烘喐鐢喚绋荤紒缁樼箞婵偓闁绘ê鐤囩涵鈧┑鐘媰閸曞灚鐣堕柧鑽ゅ仱閺屽秹宕崟顐紑闂佹眹鍊愰崑?            if (!cancellationToken.IsCancellationRequested)
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
                    "闂傚倷娴囧畷鍨叏閺夋嚚娲煛閸滀焦鏅悷婊勫灴婵?777000 缂傚倸鍊搁崐椋庢閿熺姴鍨傞梻鍫熺〒閺嗭箓鏌ｉ姀銈嗘锭闁搞劍绻冪换娑橆啅椤旇崵鍑归梺缁樺笧缁垶骞堥妸銉庣喐寰勭粙鎸庡創闂備礁鎲￠悷銏ゅ磻閹剧粯鈷掑ù锝呮啞閸熺偤鏌ㄥ顓濈箚妞ゆ劧缍嗗▓娆撴煏?,
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
    /// 濠电姷鏁搁崕鎴犲緤閽樺褰掑磼閻愯尙鐛ュ┑掳鍊曢幊搴ㄥ几?Telegram 濠电姷鏁搁崑鐐哄垂閸洖绠伴柛顐ｆ礀绾惧綊鏌″搴″季闁轰礁妫濋弻锝夊箛椤撶喎鍓瑰┑鐐茬墢閺咁偊鍩€椤掆偓閸樻粓宕戦幘缁樼厱闁规澘鍚€缁ㄥ吋銇勯銏⑿ф慨濠冩そ楠炴劖鎯旈敐鍌涱潔闂備胶绮〃鍛村箠閹扮増鍤嶉梺顒€绉撮悡娑㈡煕閹板吀绨奸柨娑樼箻閹嘲顭ㄩ崘顎囨煙椤栨瑧绐旀鐐差儔閺佸倿鎸婃径濠傤棙婵犵數鍋為幐濠氭嚌妤ｅ啯鏅濋柨鏂垮⒔閻棝鏌涢埄鍐姇闁?    /// </summary>
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
                return (false, "闂傚倸鍊风粈渚€骞栭锕€纾归柛褎銇滈埀顒€鍊块弫鍌炲箲閹邦儷锟犳⒑閸愬弶鎯堥柛鐔告緲铻炴い鏍ㄧ矋閸犳劗鈧箍鍎遍ˇ浼村磿婵犲洦鐓欓弶鍫ョ畺濡绢噣鏌涙惔鈷氼亪婀侀梺鎸庣箓椤﹁棄螞閹达附鐓熼柣鏂挎憸缁犵偤鏌＄仦璇测偓婵嬪箹瑜版帩鏁冮柕蹇ｆ線閸犲﹪姊洪懡銈呅ｉ柛鏃€顨婂畷浼村箻鐠哄搫鐏?);

            currentPassword = (currentPassword ?? string.Empty).Trim();
            newPassword = newPassword.Trim();
            hint = (hint ?? string.Empty).Trim();

            var client = await GetOrCreateConnectedClientAsync(accountId, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            // 闂傚倸鍊风粈渚€骞夐敓鐘冲仭闁靛鏅涚壕鐟懊归悩宸剰闁?WTelegramClient 闂傚倷娴囬褍顫濋敃鍌︾稏濠㈣泛瀛╅幊宀勬⒒娴ｅ憡鎯堥柣顒€銈稿畷浼村箻鐠哄搫鐏婇梺鍓插亞閸犳劖鎱ㄥ鍫熺厵婵炲牆鐏濋弸娆戠磼閹插绉慨濠冩そ楠炴劖鎯旈敐鍌氼潓婵＄偑鍊栭崹鐢告偘濮濇籍unt_UpdatePasswordSettings 闂傚倸鍊搁崐鎼佸磹缁嬫５娲偐鐠囪尙锛涢梺鐟板⒔缁垶宕?SRP 闂傚倸鍊风粈渚€骞栭銈囩煓闁告洦鍘藉畷鍙夌節闂堟侗鍎愰柛瀣戠换娑㈠幢濡纰嶇紒鐐劤濠€閬嶆儉椤忓牜鏁囬柕蹇婃櫆椤ユ牜绱撴担鎻掍壕闂佸憡娲﹂崹閬嶆偂濞戙垺鐓曢柟閭﹀墮椤忊晠鏌涢悢鍛婄凡閸楅亶鏌￠崶銉ョ仾闁绘挾鍠愰妵鍕籍閸パ冩優缂備焦绋戠换鎺旀閹烘挻缍囬柕濞у本娈圭紓鍌欑椤戝懘藝娴兼潙绠氶柡鍐ㄧ墕椤懘鏌ㄥ☉妯侯仼閻㈩垰閰ｅ铏规嫚閺屻儱寮板┑鐐板尃閸忕偓绋戦埥澶愬閳ュ厖鍑?settings
            var accountPwd = await client.Account_GetPassword();
            cancellationToken.ThrowIfCancellationRequested();

            // 闂傚倸鍊风粈渚€宕ョ€ｎ喗鍎戠憸鐗堝笒绾惧潡鎮楀☉娅亞娆㈤悙鐑樼厱闁哄洢鍔岄悘鐘绘煟椤撶噥娈橀柍褜鍓涢幊鎾垛偓姘煎幖椤灝顫滈埀顒€鐣烽悽绋跨闁哄倸鎼禍鐐箾閸繄浠㈤柡瀣崌閺屾稓鈧綆鍋呭畷宀勬煛鐏炲墽鈽夋い顐ｇ箞閹剝鎯旈敐鍕煕闂備浇宕甸崑鐐烘嚌閹呮殾妞ゆ帒瀚哥紞鏍煏韫囧鈧洖鏁梻浣稿暱閹碱偊宕锔藉剹婵°倕鎳忛悡鐔兼煟濡搫绾ч柣顓燁殕娣囧﹪鎮欓弶鎴狀儌閻庡灚婢橀敃銉╁Χ閿濆绀冮柕濞垮劤瑜板淇婇悙顏勨偓鏇犳崲閹版澘绠犻幖杈剧到缁躲倝鏌ｉ悢璇茬劷闁逞屽墯鐢帡锝炲┑瀣垫晣闁绘﹩鍋勯～灞剧節绾版ɑ顫婇柛瀣浮瀹曟垿宕卞☉妯煎姦濡炪倖甯掔€氼厼鈽夎娣囧﹪顢曢敐鍥ㄥ垱闂佽鍠撻崕閬嶁€﹂妸鈺侀唶闁绘柨鎲￠悵顐︽⒑鐠囪尙绠抽柛瀣Т铻為柛鏇ㄥ灡閸婂爼鏌ｉ弬鍨倯闁绘挾鍠栭弻宥嗘姜閹峰苯鍘￠梺鍦櫕婵炩偓闁诡喕绮欓、娑樷槈濞嗗繐鏀俊銈囧Х閸嬬偤骞戦崶顒傚祦闁搞儺鍓﹂弫濠囧级閻愭潙顥嬪?
            TL.InputCheckPasswordSRP? oldCheck = null;
            if (accountPwd.current_algo != null)
            {
                if (string.IsNullOrWhiteSpace(currentPassword))
                    return (false, "闂傚倷娴囧畷鍨叏閺夋嚚娲閵堝懐锛熼柣搴㈢⊕閿氱紒鍓佸仱閺屾盯寮撮妸銉т哗闂佹椿鍘介〃鍫ュ焵椤掑倹鍤€閻庢凹鍘奸…鍨潨閳ь剙鐣烽悽绋跨闁哄倸鎼禍鐐箾閸繄浠㈤柡瀣崌閺屾稓鈧綆鍋呭畷宀勬煛鐏炲墽鈽夋い顐ｇ箞閹剝鎯旈敐鍕煕闂備浇宕甸崑鐐烘嚌閹呮殾妞ゆ帒瀚哥紞鏍煏韫囧鈧洖鏁梻浣稿暱閹碱偊宕锔藉剹婵°倕鎳忛悡鐔兼煟濡搫绾ч柣顓燁殘缁辨帡鐓幓鎺嗗亾濡も偓鍗遍柟閭﹀厴閺€浠嬫煕椤愩倕鏋庨柣婵呭嵆閹鎮烽弶娆句患闁圭厧鐡ㄧ划搴ｆ閻愬搫鍐€妞ゆ挾鍠撻崢閬嶆⒑瑜版帒浜伴柛姗€绠栭獮濠囧礃椤旇棄浠哄銈嗙壄缁茶姤绂嶅┑鍫㈢＜闁稿本姘ㄥ瓭闂佷紮绲剧换鍫ュ春閳ь剚銇勯幒宥囪窗婵炲牅绮欓弻娑㈠Ψ閵忊剝婢掗梺绋款儐閹瑰洤螞閸愩劉妲堟繛鍡樕戦敓銉╂⒒?);

                oldCheck = await WTelegram.Client.InputCheckPassword(accountPwd, currentPassword);
            }

            // 闂?InputCheckPassword 闂傚倸鍊烽悞锕傛儑瑜版帒鍨傚┑鐘宠壘缁愭鏌熼悧鍫熺凡闁?new_password_hash闂傚倸鍊烽悞锔锯偓绗涘懐鐭欓柟杈鹃檮閸嬪鏌熼崜褍浠洪柍褜鍓氱敮鈥崇暦濠婂嫭濯撮柣鐔哄濮ｅ洤鈹戦悙鑸靛涧缂佽弓绮欓獮妤€顭ㄩ崘锝嗙亖?current_algo 缂傚倸鍊搁崐鎼佸磹閹间礁绐楁慨妯挎硾缁愭鎱ㄥΟ鎸庣【闁绘挻锕㈤弻鈥愁吋鎼粹€崇闂?            accountPwd.current_algo = null;
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
            var msg = string.IsNullOrWhiteSpace(details) ? summary : $"{summary}闂傚倸鍊烽悞锔锯偓绗涘懐鐭欓柟鐑橆殢閺佸棛绱掗弬鍨綎tails}";
            return (false, msg);
        }
    }

    /// <summary>
    /// 闂傚倸鍊搁…顒勫磻閸曨個娲晝閸屾せ鍋撻敃鍌氶唶闁靛鍠楅弲鈺呮⒑閹肩偛鍔橀柛鏂块叄閵嗗懘鎳犻钘変壕闁稿繐顦禍楣冩⒑闁偛鑻晶顖涖亜閵婏絽鍔︾€规洖鐖奸崺鐐哄箚瑜屾竟鏇㈡⒒閸屾氨澧涚紒瀣浮钘熼柣妯肩帛閻撶喖鏌″鍐ㄥ闁活厽甯￠弻鈩冩媴閸濄儛銏°亜椤愶絿绠炴い銏☆殕閹峰懐鍖栭弴鐐板?Telegram 闂傚倸鍊风粈渚€骞夐敓鐘冲仭闁挎洖鍊搁崹鍌氣攽閸屾粠鐒剧紒鐘崇叀閺屻劌鈹戦崱鈺傂﹂梺璇茬箺濞呮洜鎹㈠┑鍥╃瘈闁稿本绮岄。鍝勨攽閻愬弶鍣烽柛銊ㄥ吹濡叉劙骞樼拠鑼槰闂佽鍨庨崟顓濈敖闂備浇宕甸崑鐐烘嚌閹呮殾妞ゆ帒瀚哥紞鏍煏韫囧鈧洖鏁梻浣稿暱閹碱偊宕锔藉剹婵°倕鎳忛悡鐔兼煟濡搫绾ч柣顓燁殘缁辨帡鍩€椤掍胶鐟归柍褜鍓熼獮鍡樼瑹閳ь剟鐛€ｎ喗鏅滈柦妯侯槸婢规挸鈹戦悙鑸靛涧缂佹彃澧界划濠囧箻椤旇偐锛涢梺鍦亾閺嬪ジ寮ㄦ禒瀣厱妞ゆ劑鍊曢弸鎴炵箾閸縿鍋㈤柡宀嬬秮閹晠宕ｆ径瀣€烽梻浣规偠閸婃牠鎮у鍏撅綁骞囬鑺ョ€婚梺瑙勫劤椤曨參宕濋棃娑掓斀妞ゆ梹鏋绘笟娑㈡煕閹惧娲寸€规洖鎼灃闁告侗鍠掗幏濠氭⒑閹肩偛鍔电紒鑼跺Г缁傚秵銈ｉ崘鈺冨幗闂佺粯鏌ㄩ幉锛勫婵犳碍鐓犻柛鎰皺閸╋綁鏌熼鑺ャ仢妤犵偛顑呴埞鎴﹀箛椤掍緡鍞?7 濠电姷鏁告慨浼村垂瑜版帗鍊堕柛顐犲劚閻ょ偓銇勮箛鎾搭棡妞ゎ偅娲橀幈銊ノ熼幐搴ｃ€愰弶?    /// </summary>
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
                    return (true, "濠电姷鏁搁崑娑㈡偤閵娧冨灊鐎光偓閸曨剙鍓堕梺绋跨箳閳峰牆鈻撴禒瀣厱闁靛鍨洪弶褰掓煕鐎ｎ偅宕屾俊顐㈠暙閳藉鈻庡Ο浼欓獜闂傚倷鑳剁划顖炴偋濡ゅ啫鍨濈€光偓閸曞灚鏅╅棅顐㈡处缁嬫垿鎷戦悢鍏肩厪濠㈣泛鐗嗛崝婊勩亜韫囨洖鏋旂紒杈ㄦ崌瀹曟帒顫濋浣轰壕闂備礁鎽滄慨鐢搞€冩繝鍌滄殾闁荤喖鍋婂鈺呮偣妤︽寧顏犳い锔诲櫍濮婅櫣绱掑Ο鑽ゎ槬闂佺锕ㄥ畷鐢靛垝閸儱绀嬫い鏍ㄧ〒閸樿棄鈹戦悩璇у伐閻庢凹鍘惧▎銏ゅ矗婢跺瞼鐦堥梺閫炲苯澧存い銏℃瀹曠厧鈹戦崼銏犲箑濠碉紕鍋戦崐鏍偋濡ゅ懏鍋￠柕鍫濇穿婵啿霉閻撳海鎽犻柣鎾跺枛閺屽秵娼幏灞藉帯闂佸湱鏅繛鈧柟顔荤矙椤㈡稑鈽夊▎蹇撴敪闂備礁鎼懟顖炴嚐椤栫儐鏁囧┑鍌滎焾楠炪垺淇婇婊冨付閻㈩垰閰ｅ娲嚒閵堝憛锛勭磼閳ь剚鎷呯憴鍕彿婵炲濮撮鍡涘磻椤忓牊鐓曢柍鈺佸枤閻掕姤銇勯埡鍕毢闁逞屽墮閸樻粓宕戦幘缁樼厓鐟滄粓宕滃韬测偓鍐Ψ閳轰胶鍊為梺瀹狀潐閸庤櫕绂嶆ィ鍐┾拻闁割偆鍠撻埊鏇熴亜閵壯冣枅闁哄矉缍佸浠嬪Ω瑜忛悡澶愭⒑?, null);

                case TL.Account_ResetPasswordRequestedWait wait:
                {
                    var untilUtc = ToUtcDateTimeOffset(wait.until_date);
                    return (true, $"闂備浇顕ф绋匡耿闁秮鈧箓宕煎┑鎰闂佽鍨甸崺鍥ㄧ閻熸噴褰掓晲閸涱喛纭€濡炪倐鏅滈悡锟犲蓟閺囥垹閱囨繝鍨姈绗戞繝鐢靛仜閹冲矂宕归懡銈嗩潟闁规崘顕х粈鍐煙椤栧棗瀚禍楣冩⒒娴ｈ姤銆冮柤鍐茬埣瀵偊骞栨担鍝ヮ唵闂佺粯顭堥褏绮堥崟顖涚厪闁割偅绻冨婵囥亜閿旇娅嶉柡宀嬬秮閹晠宕ｆ径濠冪亷婵＄偑鍊戦崕鏌ュ箲閸ヮ剙绠栨繛宸簻缁狅絾绻濋崹顐㈠婵炲牜鍋婂娲川婵犲嫧濮囧┑鐘灪閿氶柍?{untilUtc:yyyy-MM-dd HH:mm:ss} UTC 闂傚倸鍊风粈渚€骞夐敓鐘冲殞闁绘劦鍓﹀▓浠嬫煙闂傚顦﹂柣銈庡櫍閺屽秷顧侀柛鎾跺枛楠炲啫螖閸涱喖浠梺缁橆殔閻楀﹪骞忛柆宥嗏拺闁硅偐鍋涙俊鐣屸偓鍏夊亾闁归棿绀侀拑鐔兼倶閻愮數鎽傞柛姘儔閺屾盯濡烽姀鈩冪彆闂?闂傚倸鍊搁崐鐑芥倿閿曚降浜归柛鎰典簽閻捇鏌ｉ姀銏╃劸闁藉啰鍠庨埞鎴︽偐閹绘帩浠炬繝娈垮灠閵堟悂寮诲☉銏犵婵°倐鍋撻柟鍐茬箻閹顢橀悢铏诡啎闁诲孩绋掗…鍥儗婵犲洦鐓熸い鎺嗗亾闁告艾顑夐妴鍐Ψ閳轰胶鍊為梺瀹狀潐閸庤櫕绂嶆ィ鍐┾拻闁割偆鍠撻埊鏇熴亜閵壯冣枅闁?, untilUtc);
                }

                case TL.Account_ResetPasswordFailedWait failed:
                {
                    var retryUtc = ToUtcDateTimeOffset(failed.retry_date);
                    return (false, $"闂傚倷绀侀幖顐λ囬锕€鐤炬繝濠傜墕閸ㄥ倿鎮归搹鐟扮殤闁兼澘娼￠弻銊╂偄閸濆嫅銏ゆ倵濮橆剦妲告い顓℃硶閹瑰嫰宕崟顓熜炴繝鐢靛仧閸樠囨偉婵傜钃熸繛鎴炃氶弸搴ㄦ煙闁箑鏋涙い顐庡懐纾藉ù锝呭濡叉椽鏌涜箛鏃撹€挎鐐叉瀹曟﹢顢欓懖鈺婃Ч婵＄偑鍊栭悧妤冨枈瀹ュ鏁傞柣鏃囨绾句粙鏌涚仦鎹愬闁哄鍊濋弻娑㈡偐閸愭彃鎽甸悗娈垮櫘閸嬪﹪骞冩禒瀣瀭妞ゆ劑鍨昏ぐ鎾⒒娴ｅ憡鍟為柛顭戝灦瀹曟劕螖閸愵亞鐒兼繝銏ｅ煐閸旀牠鍩涢幒妤佺厱闁靛鍨抽崚鏉棵瑰鍐ㄢ挃缂佽鲸鎸婚幏鍛村捶椤撴稒顫嶆俊鐐€х€靛矂宕归崘娴嬫瀻闁靛繈鍊栭崑鍕煕韫囨洖甯舵繛?{retryUtc:yyyy-MM-dd HH:mm:ss} UTC 闂傚倸鍊风粈渚€骞夐敓鐘冲殞闁告挆鈧崑鎾斥槈閹烘挻鐝曢梺鍝勬噹閻栧ジ骞冮埡鍐＜婵☆垳鍎ら悾閬嶆⒒娴ｈ棄袚闁挎碍銇勯敂璇茬仸鐎殿喗褰冮埢搴ㄥ箛椤旂虎鍟庨梻浣虹《濡插懘宕㈤崜褍顥氶柛褎顨嗛悡娑樏归敐鍥у妺闁哄棌鏅犻弻?, retryUtc);
                }

                default:
                    return (false, $"闂傚倸鍊风粈渚€骞栭锔藉亱婵犲﹤瀚々鏌ユ煟閹邦喖鍔嬮柛瀣€块弻宥夊煛娴ｅ憡娈叉繛瀛樺殠閸婃繈寮婚敓鐘茬＜婵炴垶锕╅崵瀣磽娴ｆ彃浜鹃梺閫炲苯澧扮紒杈ㄦ尰缁楃喖宕惰閸戣棄顪冮妶鍡樿偁闁搞儯鍔岄埀顒傛暬閺岋綁骞嬮敐鍛呮捇鏌￠崪浣稿闁逛究鍔岄埞鎴﹀醇閿濆懎绠慹sult.GetType().Name}", null);
            }
        }
        catch (Exception ex)
        {
            var (summary, details) = MapTelegramException(ex);
            var msg = string.IsNullOrWhiteSpace(details) ? summary : $"{summary}闂傚倸鍊烽悞锔锯偓绗涘懐鐭欓柟鐑橆殢閺佸棛绱掗弬鍨綎tails}";
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
    /// 闂傚倸鍊风粈渚€宕ョ€ｎ喖纾块柟鎯版鎼村﹪鏌ら懝鎵牚濞存粌缍婇弻娑㈠Ψ閿濆懎顬嬮梺宕囩帛濡啴寮婚弴銏犻唶婵犻潧娴勭槐鐐烘⒑閸涘﹥鐓ラ悗姘煎墲閻忓啴姊虹紒姗嗙劸閻忓繑鐟╅幃鐐烘倻閼恒儳鍘介梺鎸庢磵閸嬫挻銇勯敂鐐毈闁绘侗鍠栭鍏煎緞婵犲嫮鏆伴柣鐔哥矊濞撮鍒掗崼銉ョ劦妞ゆ帒瀚埛鎺戙€掑顒佹悙鐎殿噣绠栭弻娑㈡偐閸愭彃鎽靛銈冨灪閻熲晛鐣锋總绋课ㄩ柨鏇楀亾濞存粌缍婂娲偂鎼淬垺鍠愬銈忕导缁瑥鐣烽幋锕€绠婚悗闈涙憸閹虫繃绻濋悽闈浶㈤悗姘煎櫍钘濋柍鍝勬噺閳锋垿鏌涘┑鍡楊仼妞ゅ繑鎸抽弻娑㈠Ω閵堝懎绁Δ鐘靛仜濡瑩骞忛崨鏉戠闁圭粯甯楀▍濠囨⒒娴ｈ棄浜归柍宄扮墦瀹曟粌鈽夐姀鐘殿唵闂侀潧艌閺呮粓宕愰悽鍛婂仭婵炲棗绻愰鈺呮煙閸忓吋鍊愰柡灞界Х椤т線鏌涜箛鏃傛创闁诡喚鍋ら弫鍐焵椤掆偓瀹撳嫰姊鸿ぐ鎺擄紵缂佲偓娴ｇ儤鍠嗛柨鏇炲€归悡銉╂煛閸ヮ煈娈斿ù婊堢畺濮婅櫣鈧櫢闄勫妯绘叏閸屾壕鍋撶憴鍕闁稿繑蓱娣囧﹪骞栨担鑲濄劎鎲歌箛娑辨晛闁逞屽墰缁辨挻鎷呮ウ鎸庮€楅梺鍛婄懃闁帮絽鐣烽弶搴撴闁靛繆鏅滈弲锝嗙節閻㈤潧校缁炬澘绉瑰畷鏇烆吋婢跺鍘遍梺鍝勬储閸斿矂鎮橀悩瑁佸綊鎮╅懠顒傤唺缂備浇椴哥敮锟犲箖閳哄懎绀冮柛娆忣槹濮ｅ海绱撻崒娆愮グ濡炲顭堥悾婵嬪川婵犱胶绠?    /// </summary>
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
            var msg = string.IsNullOrWhiteSpace(details) ? summary : $"{summary}闂傚倸鍊烽悞锔锯偓绗涘懐鐭欓柟鐑橆殢閺佸棛绱掗弬鍨綎tails}";
            return (false, msg, false, false, null);
        }
    }

    /// <summary>
    /// 缂傚倸鍊搁崐鎼佸磹閻戣姤鍊块柨鏇炲€搁崹鍌炴煟閵忕姵鍟為柛?闂傚倸鍊烽懗鍫曞箠閹惧墎涓嶇€广儱顦崹鍌炴煠婵劕鈧洟宕瑰┑瀣叆闁哄啫鍊瑰▍鏇㈡煃缂佹ɑ顥堥柡灞炬礋瀹曠厧鈹戦崶椋庣闂備礁鎲￠弻銊р偓姘煎墲閻忓啴姊虹紒姗嗙劸閻忓繑鐟╅幃鐐烘倻閼恒儳鍘介梺鎸庢磵閸嬫挻銇勯敂鐐毈闁绘侗鍠栭鍏煎緞婵犲嫮鏆伴柣鐔哥矊濞撮鍒掗崼銉ョ劦妞ゆ帒瀚埛鎺戙€掑顒佹悙鐎殿噣绠栭弻娑㈡偐閸愭彃鎽靛銈冨灪閻熲晛鐣锋總绋课ㄩ柨鏃囧Г閻濐偊姊绘担绋款棌闁稿鎳愰崚鎺撴償閿濆洨骞撳┑掳鍊曢幊蹇涙偂閺囩喓绠鹃柛鈩兠慨鍥煟韫囨梹灏﹂柡灞界Х椤т線鏌涜箛鏃傘€掓繛鍡愬灩椤繈鎳滈棃娑楃钵婵犵數鍋涘Λ娆撳垂閸撲讲鍋撳顓″闂囧鏌ㄥ┑鍡欏妞ゅ繒濞€閺屾盯濡搁敃鈧埢鏇㈡煛鐏炲墽娲存鐐村笒椤撳ジ宕ㄩ鐘仏闂傚倷鑳堕…鍫ヮ敄閸℃稒鍋嬫繝濠傛噹閸ㄦ繂鈹戦悩瀹犲缂佺姷鍠栭幃妤呮晲鎼粹€愁潾濡炪倧瀵岄崳锝咁潖婵犳艾纾兼慨姗嗗幘椤﹂亶姊虹粙娆惧剱闁圭懓娲獮鍐捶椤撴稑浜鹃柨婵嗙凹缁ㄥ鏌￠崱娆忔灈闁?ConfirmTwoFactorRecoveryEmailAsync 缂傚倸鍊烽懗鍫曟惞鎼淬劌鐭楅幖娣妼缁愭绻涢幋娆忕労闁轰礁娲ら埞鎴︽偐瀹曞浂鏆￠梺鍝勬媼閸撴瑩鈥︾捄銊﹀磯闁惧繒鎳撻。鐢告⒑?    /// </summary>
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
                return (false, "闂傚倸鍊搁崐椋庢閿熺姴鐭楅幖娣妼缁愭鎱ㄥ鈧·鍌炲极婵犲洦鐓曟い鎰╁€曢弸鏃堟煃缂佹ɑ鈷掗柍褜鍓欑粻宥夊磿闁秵鍋嬮柛鈩冪☉閸屻劑鏌℃径瀣亶婵℃彃鐗撻弻鐔煎箲閹板灚缍堝銈呯箳婵炩偓闁?, null);

            try
            {
                _ = new MailAddress(email);
            }
            catch
            {
                return (false, "闂傚倸鍊搁崐椋庢閿熺姴鐭楅幖娣妼缁愭鎱ㄥ鈧·鍌炲极婵犲洦鐓曟い鎰Т閸旀岸鏌涢幇銊ヤ壕闂傚倷鑳剁划顖炲箰缁嬫５瑙勵槹鎼淬垹鍘归梺閫炲苯澧紒缁樼洴楠炲鎮欓弶鎴狀暡缂傚倷绀侀ˇ閬嶅极婵犳哎鈧礁螖閳ь剟鈥﹂妸鈺佸窛妞ゆ梻鍘ч獮?, null);
            }

            currentPassword = (currentPassword ?? string.Empty).Trim();

            var client = await GetOrCreateConnectedClientAsync(accountId, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            var pwd = await client.Account_GetPassword();
            cancellationToken.ThrowIfCancellationRequested();

            if (pwd.current_algo == null)
                return (false, "闂傚倷娴囧畷鍨叏閺夋嚚娲閵堝懐锛熼柣搴㈢⊕閿氱紒鍓佸仱閺屾盯寮撮妸銉т哗闂佹椿鍘介〃濠傤潖濞差亜鍨傛い鏇炴噹閸撻亶鎮楀▓鍨灍闁规悂绠栭崺鈧い鎺嶇贰閸熷繘鏌涢悩鍐叉诞鐎规洘鍨块獮妯肩磼濡厧骞堟繝娈垮枟椤洭宕㈣閹﹢鎮㈤崗鐓庝哗濠殿喗锕╅崜娆撴嚋鐟欏嫨浜滄い鎰Т閻忔彃鈹戦敍鍕幋闁糕晪绻濆畷姗€濡歌閸橆剟姊绘担钘夊惞濠殿喗娼欑叅闁挎繂娲﹂浠嬫煏閸繃澶勬い顐ｆ礃缁绘繈妫冨☉鍗炲壈缂備緡鍋勭粔褰掑蓟閿濆憘鐔兼惞閻у摜绀婇梻渚€娼уΛ妤呮晝閿旂偓顫曢柟鎯х摠婵挳鏌ｉ悢绋款棆濞寸姵鎸冲鐑樺濞嗘垹袦缂備胶濮甸悧鐘绘偘椤旂⒈鍚嬪璺猴功閻ｉ箖鎮峰鍐╂崳缂侇喖鐗撻崺鈧い鎺戝閳锋帒銆掑顒佹悙鐎殿噣绠栭弻娑㈡偐閸愭彃鎽靛銈冨灪閻熲晛鐣锋總绋课ㄩ柨鏃囧Г閻濐偊鏌ｆ惔鈥冲辅闁稿鎹囬弻娑㈠箛椤撶偛濮㈠┑鐐茬墛閻撯€愁潖閾忓湱鐭欓悹鎭掑妿椤斿洭姊虹紒姗嗘畷闁告梹鐟╅妴浣糕枎閹惧啿宓嗛梺缁橆焽閺佹悂鏁嶉悙宸富闁靛牆妫欓ˉ鍡欌偓瑙勬礈閺佸骞嗛崒姘辨殕闁逞屽墰濡叉劙骞掑Δ浣镐杭濠电偛妫楃换鎰板汲閻旈晲绻嗛柛娆忣槹缁€瀣煛?, null);

            if (string.IsNullOrWhiteSpace(currentPassword))
                return (false, "闂傚倷娴囧畷鍨叏閺夋嚚娲Χ婢跺﹤绨ラ梺鍝勮閸庢煡寮查弻銉︾厽闁归偊鍓氶幆鍫㈢磼閳ь剟宕橀埡鍐啎闂佺硶鍓濋〃鍫㈢不娴煎瓨鐓涢柛灞藉暙閸橀箖鎮烽柇锔惧弳闂佸憡鍔︽禍鐐村閸ャ劋绻嗛柕鍫濇搐瀛濆┑鐐茬湴閸旀垿宕洪埀顒併亜閹烘垵顏繛鎳峰嫪绻嗘い鎰剁悼濞叉挳鏌?, null);

            var oldCheck = await WTelegram.Client.InputCheckPassword(pwd, currentPassword);

            var settings = new TL.Account_PasswordInputSettings
            {
                flags = TL.Account_PasswordInputSettings.Flags.has_email,
                email = email
            };

            await client.Account_UpdatePasswordSettings(oldCheck, settings);

            // 闂傚倸鍊风粈渚€骞栭鈷氭椽濡舵径瀣槐闂侀潧艌閺呮盯鎷戦悢灏佹斀闁绘ê寮堕幖鎰版偡濞嗘瑧鐣垫鐐寸墱閸掓帡宕楁径濠佸闁诲骸婀辨刊顓㈠船瑜版帗鈷掗柛灞剧懅鐠愪即鏌涚€ｎ亝鍣虹紒宀勪憾閹煎湱鎲撮崟顐㈢哎?getPassword 闂傚倸鍊风粈渚€宕ョ€ｎ喖纾块柟鎯版鎼村﹪鏌ら懝鎵牚濞存粌缍婇弻娑㈠Ψ椤旂厧顫╅梺璇茬箺濞呮洜鎹㈠┑鍥╃瘈闁稿本纰嶅▓鍓佺磽娴ｇ懓濮х紒杈ㄦ礋閹偓妞ゅ繐鐗滈弫鍥╂喐瀹ュ鍑犻柨鏂款潟娴滄粓鏌熼悙顒€澧柣鎾炽偢閺岋紕浠﹂懞銉ユ灎濡炪們鍨洪悷褏妲愰幒妤€顫呴柣妯挎珪濮ｅ牓姊婚崒娆掑厡闁稿鍔戝畷鏇㈡嚑椤掍礁搴婇梺鍛婂姦娴滄繈寮冲鍕鐎瑰壊鍠曠花濂告煕鎼粹挌顏堟箒闂佹寧绻傞悧濠囨嚈閸︻厾纾奸悹鍥у级椤ャ垽鏌?            var after = await client.Account_GetPassword();
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
            var msg = string.IsNullOrWhiteSpace(details) ? summary : $"{summary}闂傚倸鍊烽悞锔锯偓绗涘懐鐭欓柟鐑橆殢閺佸棛绱掗弬鍨綎tails}";
            return (false, msg, null);
        }
    }

    /// <summary>
    /// 缂傚倸鍊烽懗鍫曟惞鎼淬劌鐭楅幖娣妼缁愭绻涢幋娆忕労闁轰礁娲ㄧ槐鎾存媴鐠囷紕鍔烽梺宕囩帛濡啴寮婚弴銏犻唶婵犻潧娴勭槐鐐烘⒑閸涘﹥鐓ラ悗姘煎墲閻忓啴姊虹紒姗嗙劸閻忓繑鐟╅幃鐐烘倻閼恒儳鍘介梺鎸庢磵閸嬫挻銇勯敂鐐毈闁绘侗鍠栭鍏煎緞婵犲嫮鏆伴柣鐔哥矊濞撮鍒掗崼銉ョ劦妞ゆ帒瀚埛鎺戙€掑顒佹悙鐎殿噣绠栭弻娑㈡偐閸愭彃鎽靛銈冨灪閻熲晛鐣锋總鍛婂亞濞达綀顕栭崯宥夋煟鎼粹€冲辅闁稿鎹囬弻娑㈠箛閸忓摜鍑瑰銈庡亜缁夌懓顫忛崫鍔借櫣鎷犻崣澶屼簽闂傚倸鍊哥€氼剛绮旇ぐ鎺戞槬闁?    /// </summary>
    public async Task<(bool Success, string? Error)> ConfirmTwoFactorRecoveryEmailAsync(
        int accountId,
        string code,
        CancellationToken cancellationToken = default)
    {
        try
        {
            code = (code ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(code))
                return (false, "濠电姴鐥夐弶搴撳亾濡や焦鍙忛柟缁㈠枟閸庢銆掑锝呬壕闂佽鍨悞锕€顕ラ崟顖氱疀妞ゆ挾鍋涙竟鎾斥攽閻愯埖褰х紓宥勭窔钘熼柟鍓х帛閸嬧剝绻濋棃娑卞剱闁绘挾濞€閺屟嗙疀閿濆懍绨婚梺璇茬箰閿曨亪骞冭ぐ鎺戠疀妞ゆ柨鍚嬮悘浣虹磽?);

            var client = await GetOrCreateConnectedClientAsync(accountId, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            _ = await client.Account_ConfirmPasswordEmail(code);
            return (true, null);
        }
        catch (Exception ex)
        {
            var (summary, details) = MapTelegramException(ex);
            var msg = string.IsNullOrWhiteSpace(details) ? summary : $"{summary}闂傚倸鍊烽悞锔锯偓绗涘懐鐭欓柟鐑橆殢閺佸棛绱掗弬鍨綎tails}";
            return (false, msg);
        }
    }

    /// <summary>
    /// 闂傚倸鍊搁崐鐑芥倿閿曚降浜归柛鎰典簽閻挾鐥幆褜鍎嶅ù婊冪秺閺屾稓浠﹂崜褋鈧帡鏌嶇紒妯活棃闁哄本娲熷畷鐓庘攽閸ラ绀婇梻浣告啞閺屻劎鈧凹鍓濋悘鍐⒑缂佹﹩鐒鹃悘蹇旂懇閹偤鎮滈懞銉у幗闂佹寧娲嶉崑鎾淬亜閿旂偓鏆柣娑卞枛椤撳吋寰勬繝鍕毎闁荤喐绮屽ù椋庡垝閸儱鐒垫い鎺戝閳锋帒銆掑顒佹悙鐎殿噣绠栭弻娑㈡偐閸愭彃鎽靛銈冨灪閻熲晛鐣锋總鍛婂亞濞达綀顕栭崯宥夋煟鎼粹€冲辅闁稿鎹囬弻娑㈠箛閸忓摜鍑瑰銈庡亜缁夌懓顫忛崫鍔借櫣鎷犻崣澶屼簽缂傚倷娴囩紙浼村磹濡も偓鍗遍柟閭﹀厴閺嬪酣鏌熼悙顒佸剹婵﹤娼″娲焻閻愯尪瀚板褌鍗抽弻鐔兼嚑椤掆偓椤ｅジ鏌熷畡鐗堝櫧缂侇喗鐟╁畷褰掝敊閼测晞鍩為梻鍌欐祰瀹曞灚鎱ㄥ畷鍥╃焼濞达綀顫夊▍鐘裁归悩宸剱闁稿浜弻娑㈠焺閸愵亖濮囬梺缁樺笩婵倝濡甸崟顖氱睄闁稿本绻冮妤呮⒑閸濄儱浠﹂柟顔煎€垮濠氬Ω閵夈垺顫嶅┑顔筋殔濡瑨鍊撮梻?    /// </summary>
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
                return (false, "闂傚倸鍊搁崐鐑芥倿閿曚降浜归柛鎰典簽閻挾鐥幆褜鍎嶅ù婊冪秺閺屾稓浠﹂崜褉妲堢紓浣稿閸嬨倕顕ｉ崼鏇為唶妞ゆ劦婢€閸戜粙鎮?, null, null);

            var pwd = await client.Account_GetPassword();
            var pattern = pwd.flags.HasFlag(TL.Account_Password.Flags.has_email_unconfirmed_pattern)
                ? (pwd.email_unconfirmed_pattern ?? "").Trim()
                : null;

            if (string.IsNullOrWhiteSpace(pattern))
                pattern = null;

            // 闂?API 濠电姷鏁搁崑鐐哄垂閸洖绠伴柛婵勫劤閻挸顪冪€ｎ亜顒㈢€规洘鐓￠弻鐔告綇閸撗呮殸闂佺粯鍔曢敃顏堝蓟閿濆绠涙い鏍ㄤ緱娴犫晠鎮楃憴鍕┛缂傚秳绶氶獮鍐亹閹烘垹鍊為梺鎸庢磻缁€渚€宕㈤敓鐘斥拺缂備焦顭囨晶顒傜磼椤旇姤灏い顐㈢箺閵囨劙骞掗幋婵堚偓顓烆渻閵堝棙鈷掗柍宄扮墦瀹曨剙煤椤忓應鎷洪梺鍦焾濞寸兘鍩ユ径濞炬斀闁稿本绋掗埛鎺楁煕閹烘挸绗氶柟顖涙婵偓闁炽儱寮堕崬澶愭⒒娴ｅ憡鎯堥柛濠傛贡閳ь剛鐟抽崨顖滅劶闂侀€炲苯澧存慨濠勭帛缁楃喖宕惰鐎涳綁姊洪崫銉バｇ€光偓閹间降鈧礁鈻庨幘鎶藉敹闂侀潧顦崕鎶筋敊婵犲洦鈷戦柛鎰级閹牓鏌涢悢璺哄祮鐎规洖缍婂畷姗€鎯堥鈧柊锝呯暦閸洘鏅查柛娑卞幐閹枫倕鈹?            return (true, null, pattern, null);
        }
        catch (Exception ex)
        {
            var (summary, details) = MapTelegramException(ex);
            var msg = string.IsNullOrWhiteSpace(details) ? summary : $"{summary}闂傚倸鍊烽悞锔锯偓绗涘懐鐭欓柟鐑橆殢閺佸棛绱掗弬鍨綎tails}";
            return (false, msg, null, null);
        }
    }

    /// <summary>
    /// 闂傚倸鍊风粈渚€骞夐敓鐘冲仭闁挎洖鍊归崑瀣繆閵堝懎鏆熼柣顓熸尵閳ь剛鎳撶€氫即宕戞繝鍥у惞婵°倕鎳忛悡鏇熺箾閹存繂鑸归柣蹇旂懇閺屽秷顧侀柛鎾村哺閳ワ箓宕堕鈧粻顖炴倵閿濆骸鏋涢崶鎾⒑閹肩偛鍔橀柛鏂块叄瀵娊鎮╃紒妯锋嫼闂佽崵鍠愭竟鍡樼濞戙垺鐓曢幖绮光偓铏瘣闁绘挶鍊濋弻鈥愁吋鎼粹€崇闂佺粯甯熸慨銈夊Φ閸曨垰绫嶉柛灞剧箖椤庢姊洪崫銉ヤ户妞ゎ厼鐗愰悘鍐⒑缂佹﹩鐒鹃悘蹇旂懇閹偤鎮滈懞銉у幗闂佹寧娲嶉崑鎾淬亜閿旂偓鏆鐐叉椤撳ジ宕卞Ο渚偓鎾剁磼閸撗冾暭闁挎艾霉?    /// </summary>
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
            var msg = string.IsNullOrWhiteSpace(details) ? summary : $"{summary}闂傚倸鍊烽悞锔锯偓绗涘懐鐭欓柟鐑橆殢閺佸棛绱掗弬鍨綎tails}";
            return (false, msg);
        }
    }

    /// <summary>
    /// 闂傚倸鍊风粈渚€宕ョ€ｎ喖纾块柟鎯版鎼村﹪鏌ら懝鎵牚濞存粌缍婇弻娑㈠Ψ椤旂厧顫梺绋块閻倿骞冭ぐ鎺戠倞妞ゅ繐瀚В銏㈢磽娴ｅ壊妲告繛灏栤偓鎰佹綎婵炲樊浜堕弫鍥╂喐瀹ュ鍑犻柕鍫濇娴滄粓鏌￠崒婵囩《濠⒀嶇畵閺岀喖顢欑憴鍕彋闂佺娅曠划搴ㄥ窗婵犲伣鐔告姜閺夋妫滄繝鐢靛仦閹稿鎳濇ィ鍐╂櫇闁挎柨澧介惌鍡涙煕閺囥劌鈧銇愰幒鎾充汗闂佸憡鐟ラˇ顖氣枔瀹€鈧槐鎾存媴閾忕懓绗￠梺鎼炲妼閻忔繈鎮鹃悜钘夐唶闁哄洨鍠庨埀顒傚厴閺屸剝寰勭€ｎ亞浼囩紓浣插亾閻庯綆鍠楅埛?Pattern闂傚倸鍊烽悞锔锯偓绗涘懐鐭欓柟瀵稿仧闂勫嫰鏌￠崘銊モ偓鑽ょ不閺傛鐔嗛悹杞拌閸庢劕霉濠у灝鈧繈寮婚敓鐘茬＜婵炴垶锕╅崵瀣磽娴ｆ彃浜鹃梺閫炲苯澧存慨濠傛惈鐓ら悹浣哥－閺佹牠姊洪崫銉バ㈤梺甯秮楠炲啴宕稿Δ鈧粈瀣亜閺嶃劎銆掗柛鏂垮槻椤啴濡堕崱妯锋嫽闂佹儳绻愰柊锝呯暦椤掑嫬绠瑰ù锝呮贡閸欏棗鈹戦悙鏉戠仸闁挎洍鏅涘嵄闁割偅娲橀悡?    /// </summary>
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
            var msg = string.IsNullOrWhiteSpace(details) ? summary : $"{summary}闂傚倸鍊烽悞锔锯偓绗涘懐鐭欓柟鐑橆殢閺佸棛绱掗弬鍨綎tails}";
            return (false, msg, false, null);
        }
    }

    /// <summary>
    /// 闂傚倸鍊风粈渚€骞夐敓鐘冲仭闁挎洖鍊搁崹鍌炴煕瑜庨〃鍛存倿閸偁浜滈柟杈剧到閸旂敻鏌涜箛鎾存拱缂佺粯鐩畷銊╊敃閵忕姴袚闂備線娼уú锕傚礉濞嗗浚鍤曢柟缁㈠枛椤懘鏌ｅΟ澶稿惈闁汇儱鎳愮槐鎾诲磼濞嗘帒鍘℃繝鐢靛亹閸嬫挻绻涚€涙鐭婇柣鏍с偢楠炲啴寮舵惔鎾寸€婚梺鐟邦嚟閸嬫盯鎮甸悧鍫㈢閺夊牆澧界粔顒佺箾閸滃啰绉€规洖缍婇獮搴ㄦ嚍閵壯冨汲婵犵數鍋為崹鍫曗€﹂崶銊︽珷闁靛繈鍊栭悡娑樏归敐澶嬫锭闁逞屽墯閻楁粎鍒掔€ｎ亶鍚嬮柛鈩兠崝鍛攽閳藉棗鐏犻柛銏犲级缁傚秹鎮欓鍌滅槇婵犵數濮撮崐褰掑闯閻戞ǜ浜滈煫鍥ь儏閻忣噣鏌熼崣澶嬪€愰柟顔ㄥ洤閱囬柣鏂捐濡插綊姊绘担渚劸妞ゆ垵妫濋幃褍顭ㄩ崨顓炵亰婵犵數濮甸懝鍓ф兜閳ь剟姊虹紒妯哄閻忓繑鐟х划鏃堫敋閳ь剙顫?闂傚倷娴囧畷鍨叏瀹曞洨鐭嗗ù锝堫潐濞呯姴霉閻樺樊鍎愰柛瀣典邯閺屾盯鍩勯崘顏佹闂佽绻楀▍鏇犳崲濠靛鍨傛い鎰剁到閺嗘绱撴担鎻掍壕闂佺硶鍓濈粙鎺楁偂?    /// 婵犵數濮烽弫鎼佸磻濞戔懞鍥敇閵忕姷顦悗骞垮劚椤︻垳绮堥崼婢濆綊鎮℃惔锝嗘喖闂佸搫鎷嬮崜姘跺箞閵娿儙鐔煎箰鎼达絻鈧劙姊洪崨濠冪厽闁稿海鏁诲璇测槈閵忕姷顔婇梺鐟邦嚟閸庢劕螞閵夛妇绡€闁靛骏绲剧涵楣冩倵濮橆厽绶查柣锝囧厴閺佹劙宕ㄩ娑欑杺闂備礁鎼ˇ鎶藉磿婵犳凹鏁婂鑸靛姈閳锋垿鏌ｉ幘铏崳闁哄鍟埞鎴︻敊閸濆嫧鍋撻弴锛勪罕闂備線娼ч悧鍡涘箠婢舵劕鍑犻柡宥庡幗閻撴盯鏌涢妷銏℃珔濞寸媴绠撳Λ鍛搭敆婢跺﹤鈷嬮梺鍝勮嫰缁夎淇婇悜鑺ユ櫆闁告挆鍛笌闂傚倷绀侀幉锟犲春婵犲嫭宕叉繝闈涱儏缁犳岸鏌涚仦鍓х煁閻庢碍宀搁弻銈囧枈閸楃偛顫柛妤€绻樺缁樻媴閾忕懓绗″銈庡幖閻楁挸鐣峰ú顏勫唨妞ゆ挾鍋熼崝锕€顪冮妶鍡楃伇闁稿骸顭烽、娆愬緞閹邦厾鍘撻悷婊勭矒瀹曟粌顫濇潏銊ュ簥闂侀€炲苯澧扮紒杈ㄥ笒铻栭柍褜鍓熼獮濠偽熸笟顖氭濡炪倖甯掔€氼剛鐥缁绘盯宕卞▎蹇庡濠电姰鍨哄Λ鍐潖閾忚瀚幖绮瑰墲閿涘秹鏌涢妶鍡欐噮缂佽鲸鎸搁濂稿川椤栧憞鍥ㄧ厸閻忕偠濮ら崰妯汇亜閵忥紕鎳呯紒杈ㄥ浮婵偓闁绘娅曞В鍫ユ⒒閸屾瑧鍔嶉悗绗涘懐鐭欓柟杈鹃檮閸嬪鏌熼崜褍浠洪柍褜鍓氱敮鈥崇暦濠婂嫭濯撮柣鐔哄濮ｅ洤鈹戦悙鑸靛涧缂佹彃娼″畷鏇㈠Χ婢舵ɑ鏅滈梺闈涚箞閸ㄦ椽宕伴幇鐗堢厽婵°倐鍋撻柣妤€锕︾划鍫ュ礃椤忓棛锛滄繛鎾磋壘閿曘倝銆呴鍌滅＜妞ゆ梻鏅幊鍛存煃瑜滈崜銊х礊閸℃稑鍌ㄦ繝濠傜墕閸楁娊鏌曡箛鏇炐ラ柣鎾村灴濮婃椽鎮烽柇锕€娈舵繝娈垮枤閺佸骞?setup闂傚倸鍊烽悞锔锯偓绗涘懐鐭欓柟娆¤娲、姗€濮€閻橀潧濮?    /// </summary>
    public async Task<(bool Success, string? Error, string? EmailPattern)> SetLoginEmailAsync(
        int accountId,
        string email,
        CancellationToken cancellationToken = default)
    {
        try
        {
            email = (email ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(email))
                return (false, "闂傚倸鍊搁崐椋庢閿熺姴鐭楅幖娣妼缁愭鎱ㄥ鈧·鍌炲极婵犲洦鐓曟い鎰╁€曢弸鏃堟煃缂佹ɑ鈷掗柍褜鍓欑粻宥夊磿闁秵鍋嬮柛鈩冪☉閸屻劑鏌℃径瀣亶婵℃彃鐗撻弻鐔煎箲閹板灚缍堝銈呯箳婵炩偓闁?, null);

            try
            {
                _ = new MailAddress(email);
            }
            catch
            {
                return (false, "闂傚倸鍊搁崐椋庢閿熺姴鐭楅幖娣妼缁愭鎱ㄥ鈧·鍌炲极婵犲洦鐓曟い鎰Т閸旀岸鏌涢幇銊ヤ壕闂傚倷鑳剁划顖炲箰缁嬫５瑙勵槹鎼淬垹鍘归梺閫炲苯澧紒缁樼洴楠炲鎮欓弶鎴狀暡缂傚倷绀侀ˇ閬嶅极婵犳哎鈧礁螖閳ь剟鈥﹂妸鈺佸窛妞ゆ梻鍘ч獮?, null);
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
            var msg = string.IsNullOrWhiteSpace(details) ? summary : $"{summary}闂傚倸鍊烽悞锔锯偓绗涘懐鐭欓柟鐑橆殢閺佸棛绱掗弬鍨綎tails}";
            return (false, msg, null);
        }
    }

    /// <summary>
    /// 缂傚倸鍊烽懗鍫曟惞鎼淬劌鐭楅幖娣妼缁愭绻涢幋娆忕労闁轰礁娲ら埞鎴︽偐瀹曞浂鏆￠梺绋块閻倿骞冭ぐ鎺戠倞妞ゅ繐瀚В銏㈢磽娴ｅ壊妲告繛灏栤偓鎰佹綎婵炲樊浜堕弫鍥╂喐瀹ュ鍑犻柕鍫濇娴滄粓鏌￠崒婵囩《濠⒀勭⊕閵囧嫰濡搁敐鍛闂佷紮绲剧换鍫濈暦濮椻偓椤㈡棃宕担鍦Ь闂傚倸鍊烽悞锕€顪冮崸妤€绐楅柡鍥ュ灪閳锋梻鈧箍鍎遍ˇ顖炴倿?    /// </summary>
    public async Task<(bool Success, string? Error)> ConfirmLoginEmailAsync(
        int accountId,
        string code,
        CancellationToken cancellationToken = default)
    {
        try
        {
            code = (code ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(code))
                return (false, "闂傚倷娴囧畷鍨叏閺夋嚚娲Χ婢跺﹤绨ラ梺鍝勮閸庢煡寮查弻銉︾厽闁归偊鍓氶幆鍫㈢磼閳ь剟宕橀埡鍐啎闂佺硶鍓濊摫閻忓浚鍘奸湁闁绘灏欑粻宕囩磼鏉堛劌娴柟顔规櫇閹峰鎼归銏＄亾闂傚倷鑳剁划顖滅矙閹烘鍋＄憸鏃堢嵁韫囨稒鍋愰柧蹇ｅ亜椤庢捇姊洪幆褏绠抽柟铏崌閹?);

            var client = await GetOrCreateConnectedClientAsync(accountId, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            _ = await client.Account_VerifyEmail(new EmailVerifyPurposeLoginChange(), new EmailVerificationCode { code = code });
            return (true, null);
        }
        catch (Exception ex)
        {
            var (summary, details) = MapTelegramException(ex);
            var msg = string.IsNullOrWhiteSpace(details) ? summary : $"{summary}闂傚倸鍊烽悞锔锯偓绗涘懐鐭欓柟鐑橆殢閺佸棛绱掗弬鍨綎tails}";
            return (false, msg);
        }
    }

    /// <summary>
    /// 闂傚倸鍊风粈渚€骞栭鈷氭椽濡舵径瀣槐闂侀潧艌閺呮盯鎷戦悢灏佹斀闁绘ê寮堕崳宄懊瑰鍐Ш闁哄瞼鍠栭獮鍡氼槻闁哄棜椴搁妵鍕Χ閸涱喖娈楅梺璇″枛缂嶅﹪鐛崶顒€绀堝ù锝囧劋濞堟悂鏌ｉ悢鍝ョ煁婵犮垺锚椤洭鏁撻悩鑼暰闂佸憡鍔﹂崰鏍矆鐎ｎ偁浜滈柟鍝勭Х椤斿鏌＄€ｎ亝鍣界紒?缂傚倸鍊搁崐鐑芥嚄閼稿灚鍙忔い鎾卞灩绾惧鏌熼幑鎰滅憸鐗堝笚閸嬫劗鈧懓澹婇崰鏍礉閳ь剛绱撻崒娆愮グ濡炲瓨鎮傞垾锔剧尵閻犵兘姊婚崒娆戝妽閻庣瑳鍛煓闁规璇叉喘椤㈡﹢濮€閻橀潧濮?    /// 婵犵數濮烽弫鎼佸磻濞戔懞鍥敇閵忕姷顦悗骞垮劚椤︻垳绮堥崼婢濆綊鎮℃惔锝嗘喖闂佸搫鎷嬮崜姘跺箞閵娿儙鐔煎锤濡も偓閹界敻姊洪崫鍕棞缂佺粯锕㈠濠氭偄閻撳海楠囬梺鐟扮摠缁诲啩绨洪梻鍌欐祰椤曟牠宕板Δ鈧叅婵☆垵鍋愰惌娆忣熆鐠轰警鍎愰柛搴ｅ枛楠炴帡鎼归銈庢祫闂佸壊鍋侀崕鏌ユ偂閺囩姭鍋撻獮鍨姎闁硅櫕鍔楃划缁樺鐎涙鍘遍梺闈涱焾閸庣敻顢撳Δ浣典簻闁哄浂浜炵粔顕€鏌涢埞鎯т壕婵＄偑鍊栫敮濠囨倿閿旇棄鍨旈柟缁㈠枟閸嬶綁鏌熼鐔风瑨濠碘€茬矙閺?UpdateUsernameAsync / UpdateProfilePhotoAsync闂?    /// </summary>
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

            // account.updateProfile 闂傚倸鍊烽悞锕傛儑瑜版帒绀夌€光偓閳ь剟鍩€椤掍礁鍤柛鎾跺枛楠炲啯銈﹀▎鐐╅梻浣哥－缁垶骞戦崶褏鏆︾憸鐗堝俯閺佸啴鏌ㄥ☉妯虹盎缂佺姵鎹囧璇测槈濡攱鐎诲┑鈽嗗灥濞咃絾绂掗幖浣光拺闂侇偆鍋涢懟顖涙櫠椤斿浜滈柨鏇楀亾鐎规洦鍓熼崺銏ゅ箻鐠囪尙顓洪梺鎸庢婵倕鈻嶉弽褉鏀芥い鏃€鏋绘笟娑㈡煕鐎ｎ亝顥㈤柟?null 闂傚倷娴囧畷鐢稿磻閻愮數鐭欓煫鍥ㄧ☉缁€澶愬箹濞ｎ剙濡煎鍛攽椤旀枻渚涢柛姗€绠栭崺銏ゅ籍閸屾浜炬鐐茬仢閸旀岸鎮楀鐓庡籍鐎规洩缍€缁犳稑鈽夊▎鎴濆箰闂備線鈧偛鑻晶鐗堢箾閹寸姵鏆€规洦浜濋幏鍛村川婵犲嫭姣岄梻鍌欐祰椤曆呪偓娑掓櫇缁瑩骞掑灏栧亾閸愵喖骞㈡繛鎴ｉ哺閺?            string? firstName = null;
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
            var msg = string.IsNullOrWhiteSpace(details) ? summary : $"{summary}闂傚倸鍊烽悞锔锯偓绗涘懐鐭欓柟鐑橆殢閺佸棛绱掗弬鍨綎tails}";
            return (false, msg);
        }
    }

    /// <summary>
    /// 闂傚倸鍊风粈渚€骞栭鈷氭椽濡舵径瀣槐闂侀潧艌閺呮盯鎷戦悢灏佹斀闁绘ê寮堕崳宄懊瑰鍐Ш闁哄瞼鍠栭獮鍡氼槻闁哄棜椴搁妵鍕Χ閸涱喖娈楅梺璇″枛缂嶅﹪鐛崶顒€绀堝ù锝囧劋濞堟悂鏌ｉ悢鍝ョ煁婵犮垺锚椤洭鏁撻悩鑼暫婵炲濮撮鍛劔闂備焦瀵уú宥夊磻閹剧粯鐓熼柟鎯х摠缁€瀣煛鐏炲墽鈽夋い顐ｇ箞椤㈡宕掑Δ浣衡偓鍝ョ磽閸屾瑦绁板瀛樻倐閹?me/xxx闂傚倸鍊烽悞锔锯偓绗涘懐鐭欓柟娆¤娲、姗€濮€閻橀潧濮︽俊鐐€栧濠氬磻閹剧粯鐓曞┑鐘叉祩濡垶淇婇崣澶婂鐎规洘绮嶉幏鍛存惞閸︻厼骞囬梻鍌欑閹诧紕鎹㈤崒婧惧亾濮樼厧娅嶆鐐搭殜閹垽鎼归崷顓ㄧ床婵犲痉鏉库偓鏇㈩敄閸モ晜顫曞ù鐓庣摠閸嬶繝鏌￠崶鈺佷沪妞ゃ儳濞€閺岀喖顢欑拠鎻掔ギ婵犵鍓濋悺鏇⑺囬弶妫靛綊鎮╁畷鍥ｅ缂備胶绮惄顖炵嵁濮椻偓閹煎綊顢曢姀鈽嗕槐闂傚倷绀侀悿鍥綖婢舵劖鍋嬮柣妯款嚙缁犳牕霉閻樺樊鍎忛幆鐔兼⒑閹稿孩纾甸柛瀣崌閺岋綁骞樼€涙顦伴梺鍝勬湰缁嬫垿锝炲┑瀣垫晢闁稿苯鍊绘繛鈧柡?    /// </summary>
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

            // result 闂傚倸鍊风粈渚€骞夐敓鐘冲仭妞ゆ牜鍋涢崹鍌炴煟閵忋埄鐒剧紒鈧崒娑欏弿婵＄偠顕ф禍楣冩倵?User 闂?bool闂傚倸鍊烽悞锔锯偓绗涘懐鐭欓柟杈鹃檮閸庢棃鏌ゅù瀣珔闁搞劍绻堥弻锝夊箣閻戝棛鍔烽梺宕囩帛濡啴寮诲鍫闂佸憡鎸诲銊у垝鐎ｎ喖绠抽柟鐐綑閸斿懘姊洪棃娑氬婵炲眰鍔戦幃娆撳即閵忊檧鎷虹紓浣割儓濞夋洜绮婚悙鐑樼厱閹肩补鈧疇鍩為柣鎾卞€濋弻鈥愁吋閸愩劌顬嬬紓浣哄У閻燂箓濡甸崟顖氱疀闁宠桨鑳舵禒鑲╃磽娴ｉ璐伴柛锝忕秮瀵?            var normalized = string.IsNullOrWhiteSpace(username) ? null : username;
            return (true, null, normalized);
        }
        catch (Exception ex)
        {
            var (summary, details) = MapTelegramException(ex);
            var msg = string.IsNullOrWhiteSpace(details) ? summary : $"{summary}闂傚倸鍊烽悞锔锯偓绗涘懐鐭欓柟鐑橆殢閺佸棛绱掗弬鍨綎tails}";
            return (false, msg, null);
        }
    }

    /// <summary>
    /// 闂傚倸鍊搁崐椋庢閿熺姴纾婚柛娑卞枤閳瑰秹鏌ц箛姘兼綈鐎规洘鐓￠弻娑㈠箛闂堟稒鐏嶉梺缁樻惈缂嶄線寮婚敐澶婄疀闂傚牊绋戦～鈺傜節?闂傚倸鍊烽悞锕€顪冮崹顕呯劷闁秆勵殔缁€澶屸偓骞垮劚椤︻垶寮伴妷锔剧闁瑰瓨鐟ラ悘鈺呮偡濞嗘瑥顩柍褜鍓欑粻宥夊磿闁秴绠犻幖鎼厜缂嶆牠鏌ㄥ┑鍡╂Ч闁绘挻娲熼悡顐﹀炊瑜滈崕蹇涙煕閺傝鈧繈寮诲☉婊呯杸閻庯綆浜炴导鍕磽娴ｄ粙鍝洪悽顖椻偓宕囨殾闁绘梻鈷堥弫鍡涙煃瑜滈崜鐔煎箖濡綍鏃堝川椤旈棿鍖栧┑鐐舵彧缂嶄礁顭囪閹﹢宕ｆ径澶岀畾闂佸湱绮敮鎺楊敂椤忓牊鐓ユ繛鎴炵懃婵秵顨ラ悙宸剶闁轰礁鍊垮畷褰掝敋閸涱噮妫ラ梻鍌氬€烽悞锔锯偓绗涘懐鐭欓柟杈鹃檮閸嬪鈹戦悩鍙夋悙闁哄鐒﹂妵鍕即濡も偓娴滄儳螖?https://t.me/xxx闂傚倸鍊风欢姘焽瑜嶈灋闁哄啠鍋撴繛?me/+hash闂傚倸鍊风欢姘焽瑜嶈灋闁哄啫鍊婚崡姘辩磼婢跺顢rname闂傚倸鍊风欢姘焽瑜嶈灋闁哄啫鐗婇崵鏇㈡煕閿濆棗鎮卹name闂傚倸鍊风欢姘焽瑜嶈灋闁哄啠鍋撴繛鐓庣箰閵?//join?invite=hash 缂傚倸鍊搁崐鐑芥倿閿斿墽鐭欓柟娆″眰鍔戦崺鈧い鎺戝€荤壕濂稿级閸稑濡奸柛婵嗘惈閳?    /// </summary>
    public async Task<(bool Success, string? Error, string? JoinedTitle)> JoinChatOrChannelAsync(
        int accountId,
        string linkOrUsername,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var raw = (linkOrUsername ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(raw))
                return (false, "闂傚倸鍊搁崐椋庣矆娴ｈ娅犲ù鐘差儏绾惧鏌熼幑鎰厫闁?闂傚倸鍊烽悞锕€顪冮崹顕呯劷闁秆勵殔缁€澶屸偓骞垮劚椤︻垶寮伴妷锔剧闁瑰瓨鐟ラ悘鈺呮偡濞嗘瑥顩柍褜鍓欑粻宥夊磿鏉堚斁鍋撳鐓庣仯缂侇喖鐗忛埀顒婄秵閸撴稓澹?, null);

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
            return (true, null, "闂備浇顕уù鐑藉箠閹捐绠熼梽鍥Φ閹版澘绀冩い顓熷笩閳ь剚瀵ч妵鍕疀閹炬惌妫ょ紓浣插亾闁告劦鍠楅悡蹇涙煕椤愶絿绠栫€瑰憡绻堥弻?濠电姷顣藉Σ鍛村磻閸℃ɑ娅犳俊銈呮噹閸ㄥ倿鏌熺粙鎸庢崳妞も晜鐓￠弻锝夊箛闂堟稑顫梺?);
        }
        catch (Exception ex)
        {
            var (summary, details) = MapTelegramException(ex);
            var msg = string.IsNullOrWhiteSpace(details) ? summary : $"{summary}闂傚倸鍊烽悞锔锯偓绗涘懐鐭欓柟鐑橆殢閺佸棛绱掗弬鍨綎tails}";
            return (false, msg, null);
        }
    }

    /// <summary>
    /// 闂傚倸鍊搁崐椋庢閿熺姴纾婚柛娑卞枤閳瑰秹鏌ц箛姘兼綈鐎规洘鐓￠弻娑㈠箛闂堟稒鐏嶉梺缁樻惈缂嶄線寮婚敐澶婄疀闂傚牊绋戦～鈺傜節?闂傚倸鍊烽悞锕€顪冮崹顕呯劷闁秆勵殔缁€澶屸偓骞垮劚椤︻垶寮伴妷锔剧闁瑰瓨鐟ラ悘鈺呮偡濞嗘瑥顩柍褜鍓欑粻宥夊磿闁单鍥敃閿曗偓绾惧鏌熼幑鎰靛殭闁绘帒鐏氶妵鍕箳閹存繍浠肩紓浣插亾闁告洦鍨遍悡锝夌叓閸ャ劍绀冮柛銈傚亾闂備礁鎲￠悷銉╁疮閹殿喗顫曢柟鎯х摠婵绱掔€ｎ偒鍎ュù鐘荤畺濮婃椽骞栭悙鎻捨╅梺鍛婃⒐閸ㄧ敻鎮鹃悜鑺ユ櫜濠㈣泛锕ら崬銊╂⒑闂堟侗鐓柣蹇旂箞婵＄兘鍩￠崒姘寲濠电偠鎻紞浣割焽瑜旈幃姗€宕ｆ径澶岀畾闂佸湱绮敮鎺楊敂椤忓牊鐓ユ繛鎴炵懃婵秵顨ラ悙宸剶闁轰礁鍊垮畷褰掝敋閸涱噮妫ラ梻鍌氬€烽悞锔锯偓绗涘懐鐭欓柟杈鹃檮閸嬪鈹戦悩鍙夋悙闁哄鐒﹂妵鍕即濡も偓娴滄儳螖?https://t.me/xxx闂傚倸鍊风欢姘焽瑜嶈灋闁哄啠鍋撴繛?me/+hash闂傚倸鍊风欢姘焽瑜嶈灋闁哄啫鍊婚崡姘辩磼婢跺顢rname闂傚倸鍊风欢姘焽瑜嶈灋闁哄啫鐗婇崵鏇㈡煕閿濆棗鎮卹name闂傚倸鍊风欢姘焽瑜嶈灋闁哄啠鍋撴繛鐓庣箰閵?//join?invite=hash 缂傚倸鍊搁崐鐑芥倿閿斿墽鐭欓柟娆″眰鍔戦崺鈧い鎺戝€荤壕濂稿级閸稑濡奸柛婵嗘惈閳?    /// </summary>
    public async Task<(bool Success, string? Error, string? LeftTitle)> LeaveChatOrChannelAsync(
        int accountId,
        string linkOrUsername,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var raw = (linkOrUsername ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(raw))
                return (false, "闂傚倸鍊搁崐椋庣矆娴ｈ娅犲ù鐘差儏绾惧鏌熼幑鎰厫闁?闂傚倸鍊烽悞锕€顪冮崹顕呯劷闁秆勵殔缁€澶屸偓骞垮劚椤︻垶寮伴妷锔剧闁瑰瓨鐟ラ悘鈺呮偡濞嗘瑥顩柍褜鍓欑粻宥夊磿鏉堚斁鍋撳鐓庣仯缂侇喖鐗忛埀顒婄秵閸撴稓澹?, null);

            var url = NormalizeTelegramJoinUrl(raw);

            var client = await GetOrCreateConnectedClientAsync(accountId, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            // 闂傚倷娴囧畷鐢稿窗閹扮増鍋￠弶鍫氭櫅缁躲倕螖閿濆懎鏆為柛濠囨涧闇夐柣妯烘▕閸庢劙鏌涙惔鈥愁劉闁靛洤瀚板顕€宕掑☉娆戝涧闂備焦鎮堕崝宀勫Χ閹间礁钃熼柕濞垮劗濡插牊淇婇婊冨付妞わ絽婀辩槐鎾存媴鐠団剝鐣跺銈忕畵濞佳囨偩瀹勬壋鏀介悗锝庡亜娴滃綊姊洪崷顓犲笡閻㈩垱甯掓晥闁哄被鍎查埛?            var chat = await client.AnalyzeInviteLink(url, join: false);
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
                return (false, "闂傚倸鍊风粈渚€骞栭锕€鐤柣妤€鐗婇崣蹇涙煟閵忕姷浠涢柛蹇旂矒閺屾盯骞橀懠顒€濡介梺鍝ュТ濡繈寮婚妸銉㈡斀闁糕剝锕╁Λ銈夋⒑闁偛鑻崢鍝ョ磼椤旂晫鎳囨鐐叉瀹曠喖顢涢敐鍡樻珦闂備椒绱徊浠嬫嚐椤栫偞鍊堕弶鍫氭櫇绾句粙鏌涚仦鍓ь暡闁稿被鍔戦弻娑欑節閸愮偓鐣奸梺?濠电姷顣藉Σ鍛村磻閸℃ɑ娅犳俊銈呮噹閸ㄥ倿鏌熺粙鎸庢崳妞?, null);

            await client.LeaveChat(peer);
            return (true, null, title);
        }
        catch (RpcException ex) when (ex.Code == 400 && string.Equals(ex.Message, "USER_NOT_PARTICIPANT", StringComparison.OrdinalIgnoreCase))
        {
            return (true, null, "闂傚倸鍊风粈渚€骞栭锔藉亱婵犲﹤瀚々鍙夌節婵犲倸鏋﹂柤鐗堝閵囧嫯绠涢幘鎼￥缂備讲鍋撻柛鎰靛枟閻撳繘鏌涢锝囩畺鐎瑰憡绻堥弻?濠电姷顣藉Σ鍛村磻閸℃ɑ娅犳俊銈呮噹閸ㄥ倿鏌熺粙鎸庢崳妞も晜鐓￠弻锝夊箛闂堟稑顫梺?);
        }
        catch (Exception ex)
        {
            var (summary, details) = MapTelegramException(ex);
            var msg = string.IsNullOrWhiteSpace(details) ? summary : $"{summary}闂傚倸鍊烽悞锔锯偓绗涘懐鐭欓柟鐑橆殢閺佸棛绱掗弬鍨綎tails}";
            return (false, msg, null);
        }
    }

    /// <summary>
    /// 闂傚倸鍊风粈渚€骞夐敓鐘茬鐟滅増甯掗崹鍌炴煙閹増顥夐柡瀣╃窔閺屾洟宕煎┑鍥舵缂備礁澧庨崑銈夊蓟閳ユ剚鍚嬮幖绮光偓宕囶啇闂?Bot闂傚倸鍊烽悞锔锯偓绗涘懐鐭欓柟杈鹃檮閸嬪鏌涢埄鍐槈缂?Bot 闂傚倸鍊风粈渚€骞夐敓鐘冲仭闁挎洖鍊搁崹鍌炴煕瑜庨〃鍛存倿?/start闂傚倸鍊烽悞锔锯偓绗涘懐鐭欓柟杈鹃檮閸ゆ劖銇勯弽銊х細濞存粌缍婇弻銈囧枈閸楃偛顫梺鍝勬缁捇寮婚妸鈺佸嵆婵°倐鍋撳ù婊堢畺閹鈻撻崹顔界彯闂佸憡鎸鹃崰搴敋閿濆鏁冮柕鍫濇濞堟澘顪冮妶鍡樼叆濠⒀傜矙钘濇い鎰堕檮閳?    /// 闂傚倸鍊峰ù鍥Υ閳ь剟鏌涚€ｎ偅灏伴柕鍥у瀵粙濡歌濡插牓姊烘导娆忕槣闁革綇缍佸濠氬Ω閵夈垺顫嶅┑鐐叉閿氶柛鎾额熆xxbot闂傚倸鍊风欢姘焽瑜嶈灋闁哄啫鍊芥慨铏箾婢跺鈧垺ot闂傚倸鍊风欢姘焽瑜嶈灋闁哄倹顑欓崵鏇㈡煙閸忓吋顎巔s://t.me/xxxbot闂傚倸鍊风欢姘焽瑜嶈灋闁哄啠鍋撴繛鐓庣箰閵?//resolve?domain=xxxbot&start=abc
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
                return (false, "闂傚倸鍊风粈渚€骞夐敓鐘茬鐟滅増甯掗崹鍌炴煟濡も偓閻楀﹪宕ｈ箛娑欑厓闁告繂瀚崳褰掓煟椤撶噥娈滈柡灞剧〒娴狅箓宕滆婵洤鈹戦悙鑼闁绘牜鍘ч锝夊醇閺囩偟鍘告繛杈剧悼閹虫捇寮搁悩缁樷拺闁告繂瀚ˉ銏犆瑰鍐煟鐎殿噮鍋勯鍏煎緞婵犲洤鏁归梻浣虹帛濡啴寮查懠顒€鍨濇い鏍ㄧ〒缁♀偓?64 闂傚倷娴囬褏鈧稈鏅濈划娆撳箳濡炲皷鍋撻崘顔奸唶闁靛鍠楅弲鐐寸箾閹炬潙鐒归柛瀣崌閺?, null);

            var client = await GetOrCreateConnectedClientAsync(accountId, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            var resolved = await client.Contacts_ResolveUsername(username);
            var user = resolved.User;
            if (user.access_hash == 0)
                return (false, "闂傚倸鍊风粈渚€骞栭锕€鐤柣妤€鐗婇崣蹇涙煟閵忕姷浠涢柛蹇旂矒閺屾盯顢曢敐鍡欘槰闂佹悶鍊栧濠氬焵椤掑倹鍤€閻庢凹鍘奸…鍨熼悡搴ｇ瓘?Bot access_hash", null);

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
            return (false, "闂傚倸鍊烽懗鍫曞磿閻㈢鐤鹃柍鍝勬噹缁愭淇婇妶鍛櫤闁稿顦甸弻銊モ攽閸℃﹩妫ら梺宕囩帛濮婂鍩€椤掆偓缁犲秹宕曢崡鐐嶆稑鈻庨幋鐘冲櫘闂傚倸鍊风粈渚€骞夐敓鐘冲仭妞ゆ牜鍋涢崹鍌毭归崗鍏肩稇缂佺姵宀搁獮鏍庨鈧俊鑲╃磼閳ь剚寰勯幇顓犲帾婵犵數鍊崘顭戜紑闂?Bot闂傚倸鍊烽悞锔锯偓绗涘懐鐭欓柟瀛樼箰閼板潡鏌熼鍌涘亗_APP_INVALID闂?, null);
        }
        catch (RpcException ex) when (ex.Code == 400 && string.Equals(ex.Message, "PEER_FLOOD", StringComparison.OrdinalIgnoreCase))
        {
            return (false, "闂傚倷娴囧畷鐢稿窗閹扮増鍋￠弶鍫氭櫇娑撳秹鏌熸潏鍓хシ濞存粌缍婇弻娑氫沪閸欍儳绻佸┑鈩冨絻缂嶅﹪鐛弽顬ュ酣顢楅埀顒勬倶閳轰急褰掓偐缁涚鈧潡鏌＄仦璇插闁宠棄顦灒缂備焦眉缁辨摗ER_FLOOD闂傚倸鍊烽悞锔锯偓绗涘懐鐭欓柟娆″眰鍔戦崺鈧い鎺戝€荤壕濂稿级閸稑濡跨紒鐘筹耿閺岀喐顦版惔鈾€鏋呴梺璇″枟缁捇骞婇悙鍝勎ㄦい鏃傛嚀娴滈箖鏌″搴ｄ汗鐟滅増甯楅弲鏌ユ煕濞戞瑦缍戠€殿喗婢橀—鍐Χ韫囨洖鍩岄梺缁樼墪閵堟悂鐛崘顔肩闁芥ê顦遍崐鐐烘偡濠婂啴鍙勭€规洘鍔欓幃浠嬪川婵炵偓瀚奸梻浣告贡缁垳鏁悙鍙夋瘎闂?, null);
        }
        catch (Exception ex)
        {
            var (summary, details) = MapTelegramException(ex);
            var msg = string.IsNullOrWhiteSpace(details) ? summary : $"{summary}闂傚倸鍊烽悞锔锯偓绗涘懐鐭欓柟鐑橆殢閺佸棛绱掗弬鍨綎tails}";
            return (false, msg, null);
        }
    }

    /// <summary>
    /// 闂傚倸鍊烽懗鍫曗€﹂崼銉晞闁糕剝鐟ラ崹婵嬪箹濞ｎ剙濡奸柡瀣╃窔閺屾洟宕煎┑鍥舵缂備礁澧庨崑銈夊蓟閳ユ剚鍚嬮幖绮光偓宕囶啇闂?Bot闂傚倸鍊烽悞锔锯偓绗涘懐鐭欓柟杈鹃檮閸嬪鏌熼幑鎰靛殭闁绘帒鐏氶妵鍕箳瀹ュ棭妯傛繛瀛樺殠閸ㄨ崵妲愰幒鏃€瀚氶柟缁樺笚濞堣螖閻橀潧浠滅紒缁樺灥椤曘儵宕熼姘辩杸濡炪倖妫佸Λ鍕敂?Bot 闂傚倷娴囬褎顨ョ粙鍖¤€块梺顒€绉寸壕濠氭煟閺冨洤浜圭€规挷绶氶弻娑㈠Ψ椤旂厧顫梺鍝勬媼閸撴瑩鈥︾捄銊﹀磯闁惧繒鎳撻。鐢告⒑?    /// 闂傚倸鍊峰ù鍥Υ閳ь剟鏌涚€ｎ偅灏伴柕鍥у瀵粙濡歌濡插牓姊烘导娆忕槣闁革綇缍佸濠氬Ω閵夈垺顫嶅┑鐐叉閿氶柛鎾额熆xxbot闂傚倸鍊风欢姘焽瑜嶈灋闁哄啫鍊芥慨铏箾婢跺鈧垺ot闂傚倸鍊风欢姘焽瑜嶈灋闁哄倹顑欓崵鏇㈡煙閸忓吋顎巔s://t.me/xxxbot闂傚倸鍊风欢姘焽瑜嶈灋闁哄啠鍋撴繛鐓庣箰閵?//resolve?domain=xxxbot
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
                return (false, "闂傚倸鍊风粈渚€骞栭锕€鐤柣妤€鐗婇崣蹇涙煟閵忕姷浠涢柛蹇旂矒閺屾盯顢曢敐鍡欘槰闂佹悶鍊栧濠氬焵椤掑倹鍤€閻庢凹鍘奸…鍨熼悡搴ｇ瓘?Bot access_hash", null);

            await client.Contacts_Block(new InputPeerUser(user.id, user.access_hash));
            return (true, null, "@" + username);
        }
        catch (RpcException ex) when (ex.Code == 400 && string.Equals(ex.Message, "USER_NOT_MUTUAL_CONTACT", StringComparison.OrdinalIgnoreCase))
        {
            // 闂傚倸鍊风粈渚€骞栭锕€瀚夋い鎺戝缁€澶愭煙閻戞ɑ顥嗗┑顔藉▕閺屟嗙疀濮樺吋缍堥柣搴㈣壘椤兘寮婚妸鈺佸嵆婵°倐鍋撳ù婊堢畺閹嘲顭ㄩ崟顒傚嚒濠电偟銆嬬换婵嬬嵁閸愵喖鐓涢柛娑卞枛濞堟繄绱掗崜褍顣奸柨姘瑰鍕姢闂囧鏌ㄥ┑鍡╂▓婵☆偅鍨块弻锝夋偄閸涘﹤纰嶉梻鍥ь樀閺屾稖绠涘☉娆欑吹婵炲瓨鍤庨崹鑽ゆ閹烘梻纾兼俊顖濇娴煎矂鎮楃憴鍕闁绘牕銈搁獮鍐ㄢ枎閹板墎鐭楀┑鐘绘涧濡瑩鎮炬禒瀣拻闁稿本鐟ч崝宥夋倵缁楁稑娲﹂崑锟犳煏婵炵偓娅撻柡浣割儔閺屾洟宕煎┑鎰ч梺鍝勬媼閸撶喖骞冨鈧幃娆撳级閹存繂袘闂備椒绱粻鎴︹€﹂悜钘夎摕鐎广儱顦敮闂佹寧鏌ㄦ晶浠嬫偂閳ь剟姊绘担鍛婂暈闁糕晛锕﹂幑銏犫攽閸℃瑦娈鹃梺鐟扮摠缁诲秹鎮￠妷鈺傜厱婵炴垵宕弸鐔哥箾閸稑鐏叉慨濠冩そ閹瑩鎸婃径濠傤潥缂傚倷娴囬褔宕愰懗顖涱棨闂備礁鍟块幖顐﹀箠韫囨稑纾婚柣妯肩帛閻撴洟鏌嶉埡浣告灓婵炲牊妫冮幃瑙勬媴閸︻厼寮ㄥ┑顔硷攻濡炰粙銆佸Δ鍛劦妞ゆ帒瀚崐鑸电節闂堟侗鍎忛柛灞诲姂閺屾洟宕煎┑鎰︾紓浣哄О閸庢娊骞夐幖浣瑰亱闁割偅绻勯悷銊モ攽閻愬弶鍣烽柛濠冩礋濠€渚€姊虹紒妯忚偐鎷冮敃鍌氬惞婵炲棗绻嗛弨鑺ャ亜閺冣偓閸庢娊寮搁幋鐘电＜閻庯綆浜跺Ο鈧銈冨灪閿曘垽骞冮姀銈呬紶闁靛鍎辨闂?            return (true, null, null);
        }
        catch (Exception ex)
        {
            var (summary, details) = MapTelegramException(ex);
            var msg = string.IsNullOrWhiteSpace(details) ? summary : $"{summary}闂傚倸鍊烽悞锔锯偓绗涘懐鐭欓柟鐑橆殢閺佸棛绱掗弬鍨綎tails}";
            return (false, msg, null);
        }
    }

    /// <summary>
    /// 闂傚倷娴囧畷鐢稿窗閹扮増鍋￠弶鍫氭櫅缁躲倕螖閿濆懎鏆為柛濠勬暬閺屻倝骞侀幒鎴濆缂備礁澧庨崑銈夊蓟閳ユ剚鍚嬮幖绮光偓宕囶啇闂?Bot 濠电姷鏁搁崑鐐差焽濞嗘挸瑙﹂悗锝庡亞閳瑰秵绻涘顔荤凹闁稿骸锕ら湁闁绘ê妯婇崕鎰版煕鎼粹€愁劉闁靛洤瀚板顕€宕掑☉娆戝涧闂備焦鎮堕崝宀勫Χ閹间礁钃熼柕濞垮劗濡插牊淇婇婊冨付闁愁亪浜跺鐑樻姜娴煎瓨顎栭梺绋匡攻閻楁粎鍒掔€ｎ亶鍚嬮柛鈩兠崝鍛存⒑閺傘儲娅呴柛鐕佸亰閸┾偓妞ゆ帒鍊归弳鈺冪磼鏉堛劍灏扮紒妤冨枛瀹曟儼顦茬紒鐘茬秺閹嘲顭ㄩ崟顒傚嚒闂佺锕ゅ鈥愁嚕婵犳艾宸濇い鏃囨椤庢捇姊洪棃鈺佺槣闁告ɑ绮撴俊鐑藉煛閸屾粌骞?缂傚倸鍊搁崐鐑芥倿閿斿墽鐭欓柟娆¤娲、娑橆煥閸曢潧浠洪梻浣虹帛濮婂宕㈣閹瑦绻濋崶銊у幗闂佸綊鍋婇崜姘跺煝閺囩喍绻嗛柟缁樺俯閻撳ジ鏌＄仦璇插闁宠棄顦～婊堝幢濡偑鍊濆?    /// 闂傚倸鍊峰ù鍥Υ閳ь剟鏌涚€ｎ偅灏伴柕鍥у瀵粙濡歌濡插牓姊烘导娆忕槣闁革綇缍佸濠氬Ω閵夈垺顫嶅┑鐐叉閿氶柛鎾额熆xxbot闂傚倸鍊风欢姘焽瑜嶈灋闁哄啫鍊芥慨铏箾婢跺鈧垺ot闂傚倸鍊风欢姘焽瑜嶈灋闁哄倹顑欓崵鏇㈡煙閸忓吋顎巔s://t.me/xxxbot闂傚倸鍊风欢姘焽瑜嶈灋闁哄啠鍋撴繛鐓庣箰閵?//resolve?domain=xxxbot&start=abc
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
                return (false, "闂傚倸鍊风粈渚€骞栭锕€鐤柣妤€鐗婇崣蹇涙煟閵忕姷浠涢柛蹇旂矒閺屾盯顢曢敐鍡欘槰闂佹悶鍊栧濠氬焵椤掑倹鍤€閻庢凹鍘奸…鍨熼悡搴ｇ瓘?Bot access_hash", null, null);

            var target = new ResolvedChatTarget(
                new InputPeerUser(user.id, user.access_hash),
                "@" + username,
                user.id.ToString(CultureInfo.InvariantCulture));
            return (true, null, target, "@" + username);
        }
        catch (Exception ex)
        {
            var (summary, details) = MapTelegramException(ex);
            var msg = string.IsNullOrWhiteSpace(details) ? summary : $"{summary}闂傚倸鍊烽悞锔锯偓绗涘懐鐭欓柟杈鹃檮閸庡﹥鎱ㄥ┑鍫氬亾瀹勭けils}";
            return (false, msg, null, null);
        }
    }

    public sealed record ResolvedChatTarget(InputPeer Peer, string Title, string CanonicalId);

    /// <summary>
    /// 闂傚倷娴囧畷鐢稿窗閹扮増鍋￠弶鍫氭櫅缁躲倕螖閿濆懎鏆為柛濠勬暬閺岋綁鏁愰崨顖滀紘缂備讲鍋撻柛鎰靛枟閻撳繘鏌涢锝囩畺鐎瑰憡绻堥弻?濠电姷顣藉Σ鍛村磻閸℃ɑ娅犳俊銈呮噹閸ㄥ倿鏌熺粙鎸庢崳妞も晜褰冭灃闁挎繂鎳庨弳鐐烘煕鎼粹€愁劉闁靛洤瀚板顕€宕掑☉娆戝涧闂備焦鎮堕崝宀勫Χ閹间礁钃熼柕濞垮劗濡插牊淇婇婊呭笡闁圭兘浜跺鐑樻姜閹殿喛绐楅梺闈╃秶缂嶄線鎮伴鈧獮鍥敇閻樺灚鍤屽┑鐘垫暩婵鈧凹鍣ｉ幃?
    /// - 闂傚倸鍊烽悞锕€顪冮崹顕呯劷闁秆勵殔缁€澶屸偓骞垮劚椤︻垶寮伴妷锔剧闁瑰瓨鐟ラ悘鈺呮偡?闂傚倸鍊搁崐椋庣矆娴ｈ娅犲ù鐘差儏绾惧鏌熼幑鎰厫闁哄棙绮撻弻鐔虹磼閵忕姵鐏嶉梺鍝勬媼閸撴氨鎹㈠☉銏犲耿婵炲棙鍔﹀Σ鐨奺rname闂傚倸鍊风欢姘焽瑜嶈灋闁哄啫鐗婇崵鏇㈡煕閿濆棗鎮卹name闂傚倸鍊风欢姘焽瑜嶈灋闁哄倹顑欓崵鏇㈡煙閸忓吋顎巔s://t.me/xxx闂傚倸鍊风欢姘焽瑜嶈灋闁哄啠鍋撴繛?me/xxx闂傚倸鍊风欢姘焽瑜嶈灋闁哄啠鍋撴繛鐓庣箰閵?//join?invite=hash
    /// - 濠电姷顣藉Σ鍛村磻閸℃ɑ娅犳俊銈呮噹閸ㄥ倿鏌熺粙鎸庢崳妞?缂傚倸鍊搁崐鎼佸磹缁嬫５娲閵堝懐锛欏┑掳鍊曢崯顐﹀垂?ID闂?23456闂?123456闂?1001234567890
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
                return (false, "闂傚倸鍊烽懗鍫曞磿閻㈢鐤鹃柍鍝勬噹缁愭淇婇妶鍛櫤闁稿顦甸弻銊モ攽閸℃﹩妫ら梺宕囩帛濡啴寮昏缁犳盯骞橀崘鑼剁窡闂?, null);

            var client = await GetOrCreateConnectedClientAsync(accountId, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            if (TryParseChatIdCandidate(raw, out var normalizedId))
            {
                var resolvedById = await TryResolveChatByIdFromDialogsAsync(client, normalizedId, cancellationToken);
                if (resolvedById != null)
                    return (true, null, resolvedById);

                return (false, $"闂傚倸鍊风粈渚€骞栭锔藉亱婵犲﹤瀚々鍙夌節闂堟稒宸濋柣婵婂煐閹便劌顪冪拠韫闁?chatId={raw} 闂傚倷娴囬褍霉閻戣棄鏋侀柟闂寸缁犵娀鏌熼悙顒€鍔跺┑顔藉▕閺岋紕浠︾拠鎻掑闂佺楠哥粔褰掑蓟濞戙垹鍗抽柕濞垮劚缁犲綊姊洪崨濠勬噧闁哥喐澹嗗Σ?濠电姷顣藉Σ鍛村磻閸℃ɑ娅犳俊銈呮噹閸ㄥ倿鏌熺粙鎸庢崳妞も晜褰冭灃闁挎繂鎳庨弳鐐烘煛閸滀礁澧撮柡灞剧洴椤㈡洟顢楅崒婧烇綁姊洪幖鐐测偓妤呭磻閻斿吋鍎夋い蹇撶墱閺佸洨鎲稿澶婂嚑闁挎柨顫曟禍婊堟煙閸撗勫殌闁告艾缍婇弻鐔割槹鎼粹檧鏋呮繝纰夌磿閸忔ɑ淇婇幖浣肝ㄦい鏂垮建閻斿吋鈷掑ù锝呮啞閸熺偤鏌ｉ悢鏉戝姦鐎规洘鐓″濠氬Ψ閵壯屾П闂備浇娉曢崰鎾存叏鐎靛摜鐭嗗鑸靛姈閻撶喐淇婇婵愬殭缂佽尪宕甸埀顒侇問閸犳绻涙繝鍥ц摕闁挎繂顦悡娑樷攽閻樻彃鈧綊鎮樺澶嬧拺缂備焦顭囨竟鍕磽瀹ュ嫮绐旂€?, null);
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
                return (false, "闂傚倸鍊风粈渚€骞栭锕€鐤柣妤€鐗婇崣蹇涙煟閵忕姷浠涢柛蹇旂矒閺屾盯骞橀懠顒€濡介梺鍝ュТ濡繈寮婚妸銉㈡斀闁糕剝锕╁Λ銈夋⒑闁偛鑻崢鍝ョ磼椤旂晫鎳囨鐐叉瀹曠喖顢涢敐鍡樻珦闂備椒绱徊浠嬫嚐椤栫偞鍊堕弶鍫氭櫇绾句粙鏌涚仦鍓ь暡闁稿被鍔戦弻娑欑節閸愮偓鐣奸梺?濠电姷顣藉Σ鍛村磻閸℃ɑ娅犳俊銈呮噹閸ㄥ倿鏌熺粙鎸庢崳妞?, null);

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
            var msg = string.IsNullOrWhiteSpace(details) ? summary : $"{summary}闂傚倸鍊烽悞锔锯偓绗涘懐鐭欓柟鐑橆殢閺佸棛绱掗弬鍨綎tails}";
            return (false, msg, null);
        }
    }

    /// <summary>
    /// 闂傚倸鍊风粈渚€骞夐敍鍕瀳鐎广儱顦崹鍌炴煟閵忋埄鐒剧紒鈧崟顖涚叆婵炴垶锚椤忣亪鏌涢悩鍙夘棦闁哄被鍔岄埞鎴﹀幢濮楀棙锟ラ梻渚€鈧偛鑻崢鍝ョ磼椤旂晫鎳囨鐐叉瀹曟﹢顢欓懖鈺婃Ч婵＄偑鍊栭幐楣冨磻濞戞﹩鍤曟い鏂垮⒔绾?濠电姷顣藉Σ鍛村磻閸℃ɑ娅犳俊銈呮噹閸ㄥ倿鏌熺粙鎸庢崳妞も晜褰冭灃闁挎繂鎳庨弳鐐烘煕鎼粹€愁劉闁靛洤瀚板顕€宕掑☉娆戝涧闂備焦鎮堕崝宀勫Χ閹间礁钃熸繛鎴炃氶弸搴ㄦ煙闁箑澧板ù鐓庣焸濮婃椽鏌呴悙鑼跺闁告ɑ鎮傞弻锝夊箳閻愮數鏆ら悗娈垮枟閹倸鐣峰鈧、娆撴偂鎼达絽缍佸┑锛勫亼閸婃牜鏁繝鍥ㄥ殑閻犺櫣鍎ゅВ鍕⒒閸屾瑦绁版い鏇熺墵瀹曪絾鎯旈妸銉ョ€銈嗗笒鐎氼剟鎮?    /// </summary>
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
                return (false, "婵犵數濮烽弫鎼佸磻閻愬搫鍨傞柣銏犳啞閸嬪鈹戦悩鎻掓殭妞ゆ洟浜堕弻娑樷槈濞嗘劗绋囩紓浣插亾闁告劦鍠楅悡鏇熺箾閹存繂鑸圭€殿噣绠栭弻娑㈡偄闁垮鎮欓梺瀹犳椤︾敻鐛Ο鍏煎枂闁告洖鍚€閸濇姊?, null);

            var client = await GetOrCreateConnectedClientAsync(accountId, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            var sent = await client.SendMessageAsync(target.Peer, text, null, replyToMessageId ?? 0);
            return (true, null, sent.id);
        }
        catch (Exception ex)
        {
            var (summary, details) = MapTelegramException(ex);
            var msg = string.IsNullOrWhiteSpace(details) ? summary : $"{summary}闂傚倸鍊烽悞锔锯偓绗涘懐鐭欓柟鐑橆殢閺佸棛绱掗弬鍨綎tails}";
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
                return (false, $"缂傚倸鍊搁崐鐑芥倿閿斿墽鐭欓柟娆¤娲、娑橆煥閸曢潧浠洪梻浣虹帛閺屻劍绔熸繝鍌楁灁闁伙絽澶囬崑鎾诲礂婢跺﹣澹曢梻浣告啞閸旓附绂嶅┑瀣剁稏鐎广儱鎳夐弨浠嬫煟閹邦厽缍戦柣蹇嬪劦閺岋絽螖閳ь剟鏁冮妷褉鍋撻棃娑栧仮鐎规洏鍔戦、娑樷槈濞嗗繐濮庨梻鍌欑閹测剝绗熷Δ鍛獥婵°倕鎯ら崶顏嶆▌闂佸搫琚崝宀勫煡婢跺á鐔兼嚒閵堝瀚絫imeoutSeconds} 缂傚倸鍊搁崐椋庣矆娓氣偓钘濋柟娈垮枟閺嗘粍銇勮箛鎾搭棡妞?, null);

            if (messageFilter != null
                && stopOnUnmatchedMention
                && !messageFilter(update)
                && IsMentionOrReply(update, currentUsername, sentMessageId))
            {
                return (false, "濠电姴鐥夐弶搴撳亾濡や焦鍙忛柟缁㈠枟閸庢銆掑锝呬壕闂佽鍨悞锕€顕ラ崟顒傜闁绘劦鍓﹀鏃堟⒒娴ｇ顥忛柛瀣瀹曟澘顓奸崶顭戞綗闂佸搫绋侀崢浠嬫偂濞戙垺鐓曟い鎰靛亜娴滄绱掗妸銉吋闁哄瞼鍠栧顒勫Ψ閵夈儮鎷ょ紓鍌欑椤戝懘藝閻㈡悶鈧礁顫滈埀顒勫箖濞嗘挸绠甸柟鐑樻⒒閳藉姊婚崒姘偓鐑芥倿閿曞偆鏁勬繛鍡樻尭缁愭绻涢幋娆忕仾闁?婵犵數濮甸鏍窗濡ゅ啯宕查柟閭﹀枛缁躲倕霉閻樺樊鍎忛柛銊ュ€归妵鍕冀椤愵澀娌梺鍝勬媼閸撶喖骞冨鈧幃娆撴嚋濞堟寧顥夐梻浣规偠閸婃牜鏁敓鐘茬畺鐟滅増甯掓导鐘绘煕閺囥劌骞楅柣搴枤缁?, null);
            }

            var candidate = await BuildVerificationCandidateAsync(
                client,
                update.Message,
                currentUsername,
                sentMessageId,
                cancellationToken);

            return candidate == null
                ? (false, "闂傚倸鍊风粈渚€骞夐敓鐘冲亱闁哄洨濮风粈濠傗攽閻樺弶鎼愰柛灞诲姂閺屾洟宕煎┑鎰︾紓浣插亾闁糕剝绋掗悡娆愮箾閸繄浠㈤柡瀣枛閺岋綁鏁愰崨顓″煘闁剧粯鐗犻弻锝呂熼崹顔炬闂佺粯甯為崗姗€寮婚敐澶嬫櫇闁逞屽墴钘濇い鏍仜閻撯€愁熆閼搁潧濮囬梺鍗炴喘閺岋綁寮崹顕呮殺缂備讲鍋撻柍褜鍓欓埞鎴︽倷閺夋垹浠搁梺褰掆偓娑氼槮闁宠绉归弫鎰緞婵犲嫬骞嬮梻浣告惈缁嬩線宕㈡禒瀣柧妞ゆ帒瀚崐鍫曟煟閹扮増娑уù婊堢畺閺屻劑鎮㈤弶鎴濆缂備胶绮粙鎺戭嚗閸曨厸鍋撻敐搴′簽妞わ负鍔嶇换婵嬪煕閳ь剟宕ㄩ鐣屽涧闂?AI 闂傚倷娴囧畷鍨叏閺夋嚚娲Χ閸涘倹绋戣灃闁告侗鍘藉Σ?, null)
                : (true, null, candidate);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            var (summary, details) = MapTelegramException(ex);
            var msg = string.IsNullOrWhiteSpace(details) ? summary : $"{summary}闂傚倸鍊烽悞锔锯偓绗涘懐鐭欓柟鐑橆殢閺佸棛绱掗弬鍨綎tails}";
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
                return (false, "闂傚倸鍊风粈浣革耿闁秮鈧箓宕奸妷瀣喘閹晫绮欑捄顭戝敹闁诲氦顫夊ú鏍洪妸褍顥氬ù鐘差儐閻撴洘绻濋棃娑欘棞妞ゅ浚鍋呴幈?callback_data");

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
            var msg = string.IsNullOrWhiteSpace(details) ? summary : $"{summary}闂傚倸鍊烽悞锔锯偓绗涘懐鐭欓柟鐑橆殢閺佸棛绱掗弬鍨綎tails}";
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

        if (ContainsAny(text, "闂傚倸鍊烽悞锕傚箖閸洖绀夌€光偓閸曨偆鏌堝銈嗗姀閹冲洭寮ㄩ敃鍌涚厱闁靛鍠栨晶鎵偓瑙勬尫缁舵岸寮诲☉鈶┾偓锕傚箣濠靛洨浜堕梻?, "婵犲痉鏉库偓妤佹叏閻戣棄纾绘繛鎴欏灪閸婅埖绻濋棃娑卞剰缂?, "濠电姷鏁搁崑鐐哄垂閸洖绠伴柛婵勫劤閻挾鈧娲栧ú鈺傛叏閹惰姤鐓ユ繝闈涙椤ユ粎绱掗崜浣镐槐闁哄本鐩鎾Ω閵夈儳顔戦梻?, "闂備浇顕уù鐑藉箠閹捐绠熼梽鍥Φ閹版澘绀冩い鏃傚帶閻庮參鎮峰鍛暭閻㈩垱顨婇崺?, "闂傚倷绀侀幖顐λ囬锕€鐤炬繝濠傛噺瀹曟煡鏌涢埄鍐ㄥ枙缂?, "闂傚倷娴囬褏鎹㈤幇顔藉床閻庯綆鍠楅崵鍕煕椤愶絾纾搁柍?)
            && !ContainsAny(text, "濠电姴鐥夐弶搴撳亾濡や焦鍙忛柟缁㈠枟閸庢銆掑锝呬壕闂?, "濠电姴鐥夐弶搴撳亾濡や焦鍙忛柟缁㈠枟閸庢銆掑锝呬壕闂佽鍨悞锕€顕ラ崟顖氱疀妞ゆ挾鍋涙竟?, "闂傚倸鍊风粈渚€骞栭銈囩煓闁告洦鍘藉畷鍙夌節闂堟侗鍎愰柛?, "captcha"))
        {
            return false;
        }

        if (ContainsAny(text,
                "濠电姴鐥夐弶搴撳亾濡や焦鍙忛柟缁㈠枟閸庢銆掑锝呬壕闂?,
                "濠电姴鐥夐弶搴撳亾濡や焦鍙忛柟缁㈠枟閸庢銆掑锝呬壕闂佽鍨悞锕€顕ラ崟顖氱疀妞ゆ挾鍋涙竟?,
                "闂傚倸鍊风粈渚€骞栭銈囩煓闁告洦鍘藉畷鍙夌節闂堟侗鍎愰柛?,
                "闂傚倷娴囧畷鍨叏閺夋嚚娲Ω閳轰胶顦у┑顔姐仜閸嬫捇鏌涢埞鎯т壕婵＄偑鍊栧濠氬磻閹炬番浜滈柨鏃囨椤ュ鏌?,
                "闂傚倸鍊烽懗鍓佸垝椤栫偛绀夋俊銈呮噹缁犵娀鏌熼幑鎰靛殭闁?,
                "闂傚倸鍊风粈浣革耿闁秮鈧箓宕奸妷瀣喘閹晫绮欑捄顭戝敹?,
                "闂傚倷娴囬褍霉閻戣棄绠犻柟鎹愵嚙鐎氬銇勯幒鎴濐仼闁搞劌鍊归幈銊ヮ潨閸℃瀛ｅ┑鐐茬墢閺咁偊鍩€椤掆偓閸樻粓宕戦幘缁樼厱闁规澘鍚€缁ㄥ吋銇?,
                "闂傚倷娴囧畷鍨叏閺夋嚚娲Χ婢跺﹤绨ラ梺鍦帛瀹曗€趁洪鍕敤闂侀潧顭堥崕宕囩玻?,
                "缂傚倸鍊搁崐鐑芥倿閿斿墽鐭欓柟鐑樺焾濞兼牜鎲搁悧鍫濈闁?,
                "缂傚倸鍊搁崐鐑芥嚄閼稿灚鍙忛柣銏℃偠閳ь剙鍟村畷銊╊敍濞戣鲸缍?,
                "缂傚倸鍊搁崐鐑芥倿閿斿墽鐭欓柟娆¤娲、娆撳箲閹邦儷鈥愁渻閵堝棗绗傜紒鈧担鐑橆偨闁绘劕鐡ㄩ崣蹇旀叏濡も偓濡鏅堕悧鍫熷弿?,
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
               || text.Contains("闂?, StringComparison.Ordinal)
               || text.Contains("濠?, StringComparison.Ordinal)
               || text.Contains("闂?, StringComparison.Ordinal)
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
            throw new ArgumentException("闂傚倸鍊搁崐椋庣矆娴ｈ娅犲ù鐘差儏绾惧鏌熼幑鎰厫闁?闂傚倸鍊烽悞锕€顪冮崹顕呯劷闁秆勵殔缁€澶屸偓骞垮劚椤︻垶寮伴妷锔剧闁瑰瓨鐟ラ悘鈺呮偡濞嗘瑥顩柍褜鍓欑粻宥夊磿鏉堚斁鍋撳鐓庣仯缂侇喖鐗忛埀顒婄秵閸撴稓澹?, nameof(input));

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

        // 闂傚倸鍊烽懗鍫曞磿閻㈢鐤炬繛鎴欏灪閸嬨倝鏌曟繛褍瀚▓浼存⒑閸︻叀妾搁柛鐘崇墵瀹?t.me/xxx
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
            throw new ArgumentException("Bot 闂傚倸鍊烽悞锕€顪冮崹顕呯劷闁秆勵殔缁€澶屸偓骞垮劚椤︻垶寮伴妷锔剧闁瑰瓨鐟ラ悘鈺呮偡濞嗘瑥顩柍褜鍓欑粻宥夊磿鏉堚斁鍋撳鐓庣仯缂侇喖鐗忛埀顒婄秵閸撴稓澹?, nameof(input));

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

        // https://t.me/xxxbot?start=abc 闂?t.me/xxxbot?start=abc
        if (s.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || s.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            || s.StartsWith("t.me/", StringComparison.OrdinalIgnoreCase)
            || s.StartsWith("telegram.me/", StringComparison.OrdinalIgnoreCase))
        {
            var url = s.Contains("://", StringComparison.Ordinal) ? s : "https://" + s;
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                throw new ArgumentException("Bot 闂傚倸鍊搁崐椋庣矆娴ｈ娅犲ù鐘差儏绾惧鏌熼幑鎰厫闁哄棙绮撻弻鐔虹磼閵忕姵鐏堥梺鍛婂姀閸嬫捇姊绘担铏瑰笡闁瑰摜顭堥湁濡炲瀛╅崗婊堟煃瑜滈崜鐔奉潖濞差亜浼犻柛鏇ㄥ亝濞堣埖绻濆▓鍨灓闁轰浇顕ч?, nameof(input));

            var path = (uri.AbsolutePath ?? string.Empty).Trim('/');
            var firstSeg = path.Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(firstSeg))
                throw new ArgumentException("Bot 闂傚倸鍊搁崐椋庣矆娴ｈ娅犲ù鐘差儏绾惧鏌熼幑鎰厫闁哄棙绮撻幃妤€鈽夊▎妯煎姺闂佸磭绮褰掑Φ閸曨垰绠婚悹铏规磪閵忋垻纾奸悹浣哥－缁愭棃鏌熼绛嬬劸缂佺姵鐩顒傛崉閵娿倖鍋呴梻鍌欐祰濡椼劑姊藉澶婄９婵犻潧顑囧畵渚€鎮楅敐搴濈敖闁哄棗顑夐弻锝呂旈埀顒勬偋閸℃稑鐒?, nameof(input));

            s = firstSeg;

            var query = ParseQueryString(uri.Query);
            if (query.TryGetValue("start", out var start) && !string.IsNullOrWhiteSpace(start))
                startFromLink = NormalizeBotStartParameter(start);
        }

        s = s.Trim().TrimStart('@');

        // 闂傚倸鍊峰ù鍥Υ閳ь剟鏌涚€ｎ偅灏伴柕鍥у瀵粙濡歌濡插牓姊烘导娆忕槣闁革綇缍佸濠氬Ω閵夈垺顫嶅┑鐐叉閿氶柛鎾额熂sername?start=abc闂傚倸鍊烽悞锔锯偓绗涘懐鐭欓柟杈鹃檮閸嬪鈹戦悩韫抗?http/tg 闂傚倸鍊风粈渚€骞夐敓鐘偓鍐川椤栨稑搴婂┑掳鍊曢幏瀣极婵犲洦鐓曟繛鎴烆焽閹界娀鏌?        var question = s.IndexOf('?');
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
            throw new ArgumentException("Bot 闂傚倸鍊烽悞锕€顪冮崹顕呯劷闁秆勵殔缁€澶屸偓骞垮劚椤︻垶寮伴妷锔剧闁瑰瓨鐟ラ悘鈺呮偡濞嗘瑥顩柍褜鍓欑粻宥夊磿鏉堚斁鍋撳鐓庣仯缂侇喖鐗忛埀顒婄秵閸撴稓澹?, nameof(input));

        if (s.StartsWith("+", StringComparison.Ordinal))
            throw new ArgumentException("闂傚倸鍊搁崐椋庢閿熺姴鍌ㄩ柟闂寸绾惧鏌熼崜褏甯涢柛瀣剁秮閺屾盯濡烽姀鈩冪彅闂佸憡蓱閹告娊寮婚悢鍏煎癄濠㈣泛鐗嗘禒顔嘉旈悩闈涗沪閻㈩垪鍓濇穱濠囨倻閽樺）褔鏌涢妷銏℃珕闁哥姵甯″?Bot 闂傚倸鍊烽悞锕€顪冮崹顕呯劷闁秆勵殔缁€澶屸偓骞垮劚椤︻垶寮伴妷锔剧闁瑰瓨鐟ラ悘鈺呮偡濞嗘瑥顩柍褜鍓欑粻宥夊磿鏉堚晜宕查柟鐑樻⒒閻棗顭块懜闈涘闁稿缍侀弻娑㈠Ψ閵忊剝鐝曢梺缁樺笩濞夋洜妲愰幒鎿勭矗婵犻潧妫楃猾宥夋倵?@xxxbot 闂?t.me/xxxbot", nameof(input));

        if (!System.Text.RegularExpressions.Regex.IsMatch(s, "^[A-Za-z0-9_]{5,64}$"))
            throw new ArgumentException("Bot 闂傚倸鍊烽悞锕€顪冮崹顕呯劷闁秆勵殔缁€澶屸偓骞垮劚椤︻垶寮伴妷锔剧闁瑰瓨鐟ラ悘鈺呮偡濞嗘瑥顩柍褜鍓欑粻宥夊磿閸楃倣娑樷槈濞嗘垹褰鹃梺褰掑亰閸犳帡宕戦幘鏂ユ灁闁割煈鍠楅悘鍫㈢磽娴ｈ櫣甯涢柛銊ょ祷閻忓姊洪棃娑氬妞わ富鍨跺?, nameof(input));

        // 闂傚倷鐒﹂惇褰掑春閸曨垰鍨傞梺顒€绉甸崑銈夋煛閸ヨ埖绶涚紓宥嗙墪椤潡鎳滈棃娑橆潓缂備胶濮靛畝鎼佸蓟濞戞ǚ妲堥柛妤冨仜缁犲綊姊洪崫鍕靛剰闁活厼鍊垮濠氬Ω閵夈垺顫嶅┑鐐叉缁ㄨ偐鈧稈鏅涢—鍐Χ閸屾稒鐝ㄧ紓浣哄У閸ㄥ爼寮查妷鈺傗拺缂佸瀵у﹢浼存煟閻旀潙鈧鎳?bot 缂傚倸鍊搁崐鎼佸磹閻戣姤鍊块柨鏇炲€哥粻浼存煙闂傚鍔嶉柛?        // 濠电姷鏁搁崑鐐哄箰婵犳碍鍤屽Δ锝呭枤閺佸嫰鏌涘☉娆樺妰闁搞儯鍔庣弧鈧┑顔斤供閸樿棄鈻?        // 1) 闂傚倸鍊风粈渚€骞栭銈傚亾濮樼厧寮€规洘娲栭悾鐑藉炊椤垶缍楅梻浣瑰缁嬫垹鈧凹鍠氭竟鏇㈠礂閼测晝顔曢梺绯曞墲椤ㄥ棝骞戦敐澶嬬厱?start 闂傚倸鍊风粈渚€骞夐敓鐘冲仭闁靛鏅涚壕鍦喐閻楀牆绗掓慨瑙勭叀閺岋綁寮崹顔藉€梺鍝勬媼閸撶喖寮诲☉銏╂晝闁挎繂娲ㄩ悾濂告⒑缁洘娅嗘い銊ワ躬楠炲啫鐣￠幍铏€婚柟鍏肩暘閸╁嫬螣閸屾粎纾?t.me/xxx?start=abc 闂?@xxx?start=abc闂?        // 2) 闂傚倷娴囧畷鍨叏閹绢噮鏁勯柛娑欐綑閻ゎ喖霉閸忓吋缍戦柡瀣╃窔閺屾洟宕煎┑鎰﹀┑鈽嗗亝閿曘垽骞冨畡鎵虫瀻闊洦鎼╂禒鎯ь渻閵堝骸浜濋柣妤佺矒閹偓妞ゅ繐鐗滈弫鍥╂喐瀹ュ鍑犻柟鍓х帛閻撳啰鎲稿鍫濈婵ê宕崹婵堚偓骞垮劚濞茬娀宕?Bot 濠电姷鏁告慨浼村垂閻撳簶鏋栨繛鎴炲焹閸嬫挸顫濋悡搴㈢彎濡ょ姷鍋涢崯顖滄崲濠靛鐐婄憸蹇斿?        if (!s.EndsWith("bot", StringComparison.OrdinalIgnoreCase)
            && string.IsNullOrWhiteSpace(startFromLink)
            && !assumeBotUsername)
            throw new ArgumentException("闂傚倸鍊烽懗鍫曞磿閻㈢鐤鹃柍鍝勬噹缁愭淇婇妶鍛櫤闁稿顦甸弻銊モ攽閸♀晜效闂佺瀛╅崹鍧楀箖濡ゅ懏鏅查幖绮光偓宕囶啈闁诲氦顫夊ú妯荤箾婵犲洤钃熼柍鈺佸暙缁剁偤鏌涢锝囩畵濠殿喖绉剁槐鎾存媴鐠団剝鐣跺銈忕畵濞佳囶敋?Bot 闂傚倸鍊烽悞锕€顪冮崹顕呯劷闁秆勵殔缁€澶屸偓骞垮劚椤︻垶寮伴妷锔剧闁瑰瓨鐟ラ悘鈺呮偡濞嗘瑥顩柍褜鍓欑粻宥夊磿鏉堚晜宕查柟鐑樻⒒閻棝鏌涢弴銊ョ仭闁抽攱甯￠弻娑㈠Ψ椤栨粌鍩屾繛瀛樼矌閸嬫挾鎹?bot 缂傚倸鍊搁崐鎼佸磹閻戣姤鍊块柨鏇炲€哥粻浼存煙闂傚鍔嶉柛瀣閵囧嫰骞掗幋婵愪痪闂?, nameof(input));

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
    /// 闂傚倸鍊风粈渚€骞栭鈷氭椽濡舵径瀣槐闂侀潧艌閺呮盯鎷戦悢灏佹斀闁绘ê寮堕崳宄懊瑰鍐Ш闁哄瞼鍠栭獮鍡氼槻闁哄棜椴搁妵鍕Χ閸涱喖娈楅梺璇″枛缂嶅﹪鐛崶顒€绀堝ù锝囧劋濞堟悂鏌ｉ悢鍝ョ煂濠⒀勵殘閳ь剛鐟抽崘鎯ф闂佸憡娲﹂崹鎵棯瑜旈弻鐔衡偓娑欘焽缁犳挸顭跨憴鍕⒌婵﹥妞介獮鎰償閿濆倹顫嶉梻浣虹帛椤ㄥ棝骞愰幖浣测偓锕傚炊椤掆偓鍥存繝銏ｆ硾閿曪箓鎮炬潏銊х閺夊牆澧介崚浼存煙鐠囇呯？缂侇喖顑夊畷濂稿Ψ閿旀儳骞楁繝寰锋澘鈧劙宕戦幘瀵哥闁哄鍨甸顐ｄ繆閸欏濮囨い顐ｇ箘閹瑰嫭鎷?    /// </summary>
    public async Task<(bool Success, string? Error)> UpdateProfilePhotoAsync(
        int accountId,
        Stream fileStream,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (fileStream == null)
                return (false, "濠电姷鏁告慨浼村垂婵傜鏄ラ柡宓瞼鍔峰┑鐐叉閹稿摜澹曢幐搴㈠弿婵☆垱瀵х涵鐐繆椤愶綇鑰块柡灞剧洴婵＄兘顢涢悙鎼偓宥咁渻閵堝棗濮屾俊顐㈠閸┿儲寰勯幇顒傤啋闁荤喐鐟ョ€氶攱绂掗埡鍛拺?);

            fileName = (fileName ?? "avatar.jpg").Trim();
            if (string.IsNullOrWhiteSpace(fileName))
                fileName = "avatar.jpg";

            var client = await GetOrCreateConnectedClientAsync(accountId, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            await using var encoded = await TelegramImageProcessor.PrepareAvatarJpegAsync(fileStream, cancellationToken);
            var inputFile = await client.UploadFileAsync(encoded, "avatar.jpg");
            cancellationToken.ThrowIfCancellationRequested();

            if (inputFile == null)
                return (false, "Avatar upload failed: upload result was empty.");

            await client.Photos_UploadProfilePhoto(inputFile, video: null, video_start_ts: null, video_emoji_markup: null, bot: null, fallback: false);
            return (true, null);
        }
        catch (UnknownImageFormatException)
        {
            return (false, "Avatar upload failed: unsupported image format. Please use JPG or PNG.");
        }
        catch (Exception ex)
        {
            var (summary, details) = MapTelegramException(ex);
            var msg = string.IsNullOrWhiteSpace(details) ? summary : $"{summary}: {details}";
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

            throw new TimeoutException($"Telegram 闂傚倷娴囧畷鍨叏閺夋嚚娲敇閵忕姷鍝楅梻渚囧墮缁夌敻宕曢幋锔界厵婵炲牆鐏濋弸鐔兼煙椤栨ê鍔﹂柡灞剧☉閳藉宕￠悙瀵镐憾婵＄偑浼呮担鍛婂闁绘挶鍎茬换娑㈠箣閻愬灚鍣介梺鍛婃⒒濡eration} 闂傚倷鑳堕崕鐢稿礈濠靛牊鏆滈柟鐑橆殔缁犵娀鏌熼幑鎰厫鐎?{timeout.TotalSeconds:0} 缂傚倸鍊搁崐椋庣矆娓氣偓钘濋柟娈垮枟閺嗘粍銇勮箛鎾搭棡妞ゎ偅娲樼换婵嬫濞戝崬鍓伴梺娲诲幗椤ㄥ棝濡甸崟顖氬唨闁靛ě鍛殼闂備礁鎲¤摫闁瑰憡濞婂?Session 濠电姷鏁告慨浼村垂濞差亜纾块柛蹇曨儠娴犲牓鏌熼梻瀵割槮闁汇倝绠栭弻鏇熷緞閸℃ɑ鐝旈弶鈺傜箖缁绘稒娼忛崜褏袣濠碘槅鍋勭€氭媽妫熼梺鍐叉惈閹冲繘鎮￠弴鐔虹闁糕剝顨堢粻鐗堜繆閼艰泛鍚归柟鍙夋倐瀵墎鎹勯妸銉㈠徍婵犳鍠栭敃锝囧垝濞嗘挾宓侀柛銉墮閻撴﹢鏌熼鍡楀€搁ˉ姘攽閻愯埖褰х紓宥佸亾濠电偛鎷戠徊璺ㄥ垝閺冨牆鎹舵い鎾跺Х閿涙繃绻涙潏鍓у埌婵犫偓闁秵鍎楀┑鐘插绾惧ジ寮堕崼娑樺閻忓繒鏁婚弻鈩冪瑹閸パ勭彎閻庤娲忛崝宥囨崲濠靛纾兼繝濞惧亾缂侇噯绲介埞鎴︽倷瀹割喖娈舵繝娈垮灠椤曨厼顕ユ繝鍥ч敜婵°倐鍋撶紒鐘崇墪閳规垿鎮╅幓鎺嶇敖闂佹悶鍊栧ú鐔煎蓟濞戞矮娌柣鎰靛墰濞堛倝姊?);
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

            throw new TimeoutException($"Telegram 闂傚倷娴囧畷鍨叏閺夋嚚娲敇閵忕姷鍝楅梻渚囧墮缁夌敻宕曢幋锔界厵婵炲牆鐏濋弸鐔兼煙椤栨ê鍔﹂柡灞剧☉閳藉宕￠悙瀵镐憾婵＄偑浼呮担鍛婂闁绘挶鍎茬换娑㈠箣閻愬灚鍣介梺鍛婃⒒濡eration} 闂傚倷鑳堕崕鐢稿礈濠靛牊鏆滈柟鐑橆殔缁犵娀鏌熼幑鎰厫鐎?{timeout.TotalSeconds:0} 缂傚倸鍊搁崐椋庣矆娓氣偓钘濋柟娈垮枟閺嗘粍銇勮箛鎾搭棡妞ゎ偅娲樼换婵嬫濞戝崬鍓伴梺娲诲幗椤ㄥ棝濡甸崟顖氬唨闁靛ě鍛殼闂備礁鎲¤摫闁瑰憡濞婂?Session 濠电姷鏁告慨浼村垂濞差亜纾块柛蹇曨儠娴犲牓鏌熼梻瀵割槮闁汇倝绠栭弻鏇熷緞閸℃ɑ鐝旈弶鈺傜箖缁绘稒娼忛崜褏袣濠碘槅鍋勭€氭媽妫熼梺鍐叉惈閹冲繘鎮￠弴鐔虹闁糕剝顨堢粻鐗堜繆閼艰泛鍚归柟鍙夋倐瀵墎鎹勯妸銉㈠徍婵犳鍠栭敃锝囧垝濞嗘挾宓侀柛銉墮閻撴﹢鏌熼鍡楀€搁ˉ姘攽閻愯埖褰х紓宥佸亾濠电偛鎷戠徊璺ㄥ垝閺冨牆鎹舵い鎾跺Х閿涙繃绻涙潏鍓у埌婵犫偓闁秵鍎楀┑鐘插绾惧ジ寮堕崼娑樺閻忓繒鏁婚弻鈩冪瑹閸パ勭彎閻庤娲忛崝宥囨崲濠靛纾兼繝濞惧亾缂侇噯绲介埞鎴︽倷瀹割喖娈舵繝娈垮灠椤曨厼顕ユ繝鍥ч敜婵°倐鍋撶紒鐘崇墪閳规垿鎮╅幓鎺嶇敖闂佹悶鍊栧ú鐔煎蓟濞戞矮娌柣鎰靛墰濞堛倝姊?);
        }
    }

    private int ResolveApiId(Account account)
    {
        if (int.TryParse(_configuration["Telegram:ApiId"], out var globalApiId) && globalApiId > 0)
            return globalApiId;
        if (account.ApiId > 0)
            return account.ApiId;
        throw new InvalidOperationException("闂傚倸鍊风粈渚€骞栭锔藉亱婵犲﹤瀚々鍙夈亜韫囨挾澧曢柛灞诲姂閺屾洟宕煎┑鍥х獩缂佹儳澧介崑鎾诲Φ閸曨垰绫嶉柛灞捐壘娴犳﹢鎮楀▓鍨灈婵炵》绻濋獮鍐潨閳ь剟骞冨▎鎾崇厸濞达絽鍢查ˉ?ApiId闂傚倸鍊烽悞锔锯偓绗涘懐鐭欓柟瀵稿仧闂勫嫰鏌￠崘銊モ偓鑽ょ不閺冨牊鐓涚€广儱楠告禍婵嬫倵濮橆剦鐓奸柡灞诲姂瀵潙螖閳ь剚绂嶆ィ鍐╁仭婵犲﹤瀚婊勭箾婢跺绀堟俊鍙夊姍楠炴鎷犻煫顓犵倞闂備礁鎲″ú锕傚储娴犲鍎?ApiId");
    }

    private string ResolveApiHash(Account account)
    {
        var global = _configuration["Telegram:ApiHash"];
        if (!string.IsNullOrWhiteSpace(global))
            return global.Trim();
        if (!string.IsNullOrWhiteSpace(account.ApiHash))
            return account.ApiHash.Trim();
        throw new InvalidOperationException("闂傚倸鍊风粈渚€骞栭锔藉亱婵犲﹤瀚々鍙夈亜韫囨挾澧曢柛灞诲姂閺屾洟宕煎┑鍥х獩缂佹儳澧介崑鎾诲Φ閸曨垰绫嶉柛灞捐壘娴犳﹢鎮楀▓鍨灈婵炵》绻濋獮鍐潨閳ь剟骞冨▎鎾崇厸濞达絽鍢查ˉ?ApiHash闂傚倸鍊烽悞锔锯偓绗涘懐鐭欓柟瀵稿仧闂勫嫰鏌￠崘銊モ偓鑽ょ不閺冨牊鐓涚€广儱楠告禍婵嬫倵濮橆剦鐓奸柡灞诲姂瀵潙螖閳ь剚绂嶆ィ鍐╁仭婵犲﹤瀚婊勭箾婢跺绀堟俊鍙夊姍楠炴鎷犻煫顓犵倞闂備礁鎲″ú锕傚储娴犲鍎?ApiHash");
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

        var flagText = flags.Count == 0 ? "-" : string.Join(", ", flags);
        return $"{profile.DisplayName} / @{profile.Username ?? "-"} / UserId={profile.UserId} / Flags={flagText}";
    }

    private async Task<CreateChannelProbeResult> ProbeCreateChannelCapabilityAsync(Client client, int accountId, CancellationToken cancellationToken = default)
    {
        // 婵犵數濮烽弫鎼佸磻濞戔懞鍥敇閵忕姷顦悗骞垮劚椤︻垳绮堥崼婢濆綊鎮℃惔锝嗘喖闂佸搫鎷嬮崜姘跺箞閵娿儙鐔兼惞閻у摜绀婄紓鍌欒兌婵绱炴繝鍥ц摕闁跨喓濮寸粈瀣⒒閸喓銆掑ù鐘冲哺濮婃椽鏌呴悙鑼跺濠⒀屽灣缁辨帡顢欓懖鈹倝鏌熼獮鍨仾闁瑰嘲鎳愰幉鎾礋椤愩垹韦闂傚倷鐒︾€笛兾涙担鑲濇盯宕熼鐔风毇闁诲孩绋掗…鍥╃不妤ｅ啯鐓涘璺侯儏閻掗箖鏌涙惔锛勭劯闁哄苯绉烽¨渚€鏌涢幘瀛樼殤闁瑰箍鍨介獮鍥敊缁涘缍楁繝鐢靛█濞佳兠洪妶澶樻晪閺夊牄鍔嶉崣蹇旀叏濡も偓濡鏅堕鈧弻锝堢疀閵壯咃紵闂侀€炲苯澧伴柛瀣剁秮瀹曟劙寮撮姀鐘殿啇濡炪倖鍔ч梽鍕吹閹扮増鐓熼柡鍐ㄥ€哥敮鍓佺磼閳ь剟宕卞☉娆戝幗濠碘槅鍨甸崑鎰暜濞戙垺鐓熸繝闈涚墕閺嬪酣鏌嶉挊澶樻█妤犵偞锕㈤、娑橆潩椤愩埄妫滃┑鐘垫暩閸嬬偤宕归崼鏇炵闁冲搫鍊婚々鍙夌節闂堟稓澧㈤柣顓炵墦閹妫冨☉娆忔殘闁诲孩纰嶅畝鎼佸蓟閿熺姴鐐婇柕澶堝劤閸旀挳姊洪崫鍕棑闁稿酣娼ч～蹇撁洪宥嗘櫌婵炶揪绲块幊鎾绘嫊婵傚憡鈷?        var title = $"tp-check-{DateTime.UtcNow:MMddHHmmss}";
        const string about = "Telegram Panel create-channel probe (auto delete)";

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            UpdatesBase updates;
            try
            {
                updates = await ExecuteTelegramRequestAsync(
                    accountId,
                    "Create channel probe",
                    () => client.Channels_CreateChannel(title: title, about: about, broadcast: true),
                    cancellationToken,
                    resetClientOnTimeout: true);
            }
            catch (RpcException ex) when (ex.Code == 420 && string.Equals(ex.Message, "FROZEN_METHOD_INVALID", StringComparison.OrdinalIgnoreCase))
            {
                return new CreateChannelProbeResult(false, true, "The current ApiId is frozen for create-channel related Telegram APIs (FROZEN_METHOD_INVALID).");
            }

            cancellationToken.ThrowIfCancellationRequested();

            var channel = updates.Chats.Values.OfType<TL.Channel>().FirstOrDefault();
            if (channel == null)
                return new CreateChannelProbeResult(false, false, "Create channel probe returned no channel entity.");

            try
            {
                // 缂傚倸鍊搁崐鐑芥倿閿曞倸钃熼柕濞炬櫓閺佸嫰鏌涘☉鍗炲箻闁哄棎鍊濋弻娑㈠焺閸愵亖妲堢紓浣插亾闁糕剝绋掗悡鐔镐繆椤栨繂浜归悽顖涚洴閺岋絽鈹戦崼婵囩亪闂佸搫琚崝宀勫煡婢跺á鐔兼嚃閳轰礁袩婵犵數鍋炲娆撴儍閻戣棄鐤鹃柣妯垮皺閺嗭附淇婇妶鍌氫壕濡炪倖鎸搁妶绋跨暦閸洖惟闁挎梻鍋撳▓顔界節閻㈤潧浠﹂柛銊ョ埣楠炴劙骞栨笟鍥ㄦ櫈闂佸憡绋戦悺銊╁磹閸偁浜滈柡鍥殔娴滈箖鎮楃憴鍕闁衡偓闁秴围闁挎繂顦粈鍐煟閹伴潧澧い鏃€妫冨?                var input = new InputChannel(channel.id, channel.access_hash);
                await ExecuteTelegramRequestAsync(
                    accountId,
                    $"Delete probe channel ({channel.id})",
                    () => client.Channels_DeleteChannel(input),
                    cancellationToken,
                    resetClientOnTimeout: false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Probe channel created but failed to delete (account {AccountId}, channel {ChannelId})", accountId, channel.id);
                return new CreateChannelProbeResult(false, false, $"Channel probe created but cleanup failed: {ex.Message}. Created title: {title}");
            }

            return new CreateChannelProbeResult(true, false, "Create channel probe succeeded.");
        }
        catch (Exception ex)
        {
            var msg = ex.Message ?? "Create channel probe failed.";
            return new CreateChannelProbeResult(false, false, msg);
        }
    }

    private sealed record CreateChannelProbeResult(bool Success, bool IsFrozen, string Message);

    /// <summary>
    /// 闂?Telegram 闂備浇顕х€涒晠顢欓弽顓炵獥闁哄稁鍘肩壕褰掓煙闂傚鍔嶉柛瀣樀閺屾盯顢曢敐鍡欘槬闂佸憡鐟ラ敃顏堝蓟閿濆憘鐔煎垂椤旂偓顕楁繝鐢靛仜閻楀繘鈥﹂崼銉ョ叀濠㈣埖鍔曠粻濠氭煣韫囷絽浜濋柛搴㈡崌閹嘲顭ㄩ崟顐偓婊堟倶韫囨梻鎳囨鐐茬箻閹晝绱掑Ο鐑橆吋闂備線娼ч悧鍡椢涘☉銏犳辈闁绘绮埛鎴犵棯椤撶偞鍣圭悮姘渻閵堝骸骞栭柣妤佹崌婵℃挳宕ㄧ€涙ɑ娅囬梺绋挎湰绾板秹濡靛┑瀣€甸柛蹇擃槸娴滈箖姊洪崨濠傚闁稿骸鍟块埢鎾愁煥閸啿鎷洪柣鐘叉礌閳ь剙纾禒鈺呮⒑閻熸澘鏆遍柣顓炲€垮畷娲焵?    /// </summary>
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
            return ("Timeout", msg);

        if (msg.Contains("EMAIL_HASH_EXPIRED", StringComparison.OrdinalIgnoreCase))
            return ("Email code expired", msg);

        if (msg.Contains("EMAIL_NOT_SETUP", StringComparison.OrdinalIgnoreCase))
            return ("Email verification not enabled", msg);

        if (msg.Contains("EMAIL_UNCONFIRMED", StringComparison.OrdinalIgnoreCase))
            return ("Email unconfirmed", msg);

        if (msg.Contains("EMAIL_TOKEN_INVALID", StringComparison.OrdinalIgnoreCase))
            return ("Invalid email token", msg);

        if (msg.Contains("EMAIL_INVALID", StringComparison.OrdinalIgnoreCase))
            return ("Invalid email", msg);

        if (msg.Contains("EMAIL_NOT_ALLOWED", StringComparison.OrdinalIgnoreCase))
            return ("Email not allowed", msg);

        if (msg.Contains("FROZEN_METHOD_INVALID", StringComparison.OrdinalIgnoreCase))
            return ("Frozen API method", msg);

        if (msg.Contains("FLOOD_WAIT", StringComparison.OrdinalIgnoreCase))
            return ("Flood wait", msg);

        if (msg.Contains("CHANNEL_MONOFORUM_UNSUPPORTED", StringComparison.OrdinalIgnoreCase))
            return ("Unsupported channel API", msg);

        if (msg.Contains("AUTH_KEY_UNREGISTERED", StringComparison.OrdinalIgnoreCase))
            return ("Session invalid", msg);

        if (msg.Contains("AUTH_KEY_DUPLICATED", StringComparison.OrdinalIgnoreCase))
            return ("Session conflict", msg);

        if (msg.Contains("SESSION_REVOKED", StringComparison.OrdinalIgnoreCase))
            return ("Session revoked", msg);

        if (msg.Contains("SESSION_PASSWORD_NEEDED", StringComparison.OrdinalIgnoreCase))
            return ("Two-factor password required", msg);

        if (msg.Contains("CODE_INVALID", StringComparison.OrdinalIgnoreCase))
            return ("Invalid verification code", msg);

        if (msg.Contains("PHOTO_FILE_MISSING", StringComparison.OrdinalIgnoreCase))
            return ("Photo upload failed", msg);

        if (msg.Contains("PHONE_NUMBER_BANNED", StringComparison.OrdinalIgnoreCase)
            || msg.Contains("USER_DEACTIVATED_BAN", StringComparison.OrdinalIgnoreCase))
            return ("Account banned", msg);

        if (msg.Contains("Can't read session block", StringComparison.OrdinalIgnoreCase))
            return ("Session read failed", msg);

        return ("Connection failed", msg);
    }
}
