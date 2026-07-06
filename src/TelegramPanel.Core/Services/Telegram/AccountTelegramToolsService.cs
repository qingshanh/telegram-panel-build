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
/// 闂傚倸鍊峰ù鍥х暦閸偅鍙忛柣銏㈩焾缁€澶屾喐韫囨洖鍨濆┑鐘宠壘缁狅綁鏌ㄥ┑鍡楁殨婵顨婇弻锝夋偐閸忓懓鍩呴梺鍛婃煥缁夊爼鍩€椤掍胶顣查柤褰掔畺閸╃偤骞嬮敂钘変汗闂佸湱绮敮妤€鈻撻弻銉︹拺?/ 缂傚倸鍊搁崐鎼佸磹妞嬪孩顐介柨鐔哄Т閸ㄥ倿姊婚崼鐔恒€掗柡鍡畵閺岋綁濮€閵堝棙閿梺鎼炲妽缁诲啰鎹㈠☉姗嗗晠妞ゆ棁宕甸崙褰掓⒑缂佹ê绗х紒顕呭灦楠炲牓濡搁妷搴ｅ枑瀵板嫮绮欓幐搴″壍闂傚倷绀侀幉锟犳偡閵忋倕纾?/ 闂傚倸鍊搁崐椋庢濮橆剦鐒介柤濮愬€栫€氬鏌ｉ弮鍌氬付缂佲偓婢舵劕绠规繛锝庡墮婵″ジ鏌涘顒傜Ш妤犵偞鐗楀蹇涘礈瑜忚摫濠电姵顔栭崹浼村Χ閹间礁钃熼柣鏃傗拡閺佸秹鏌涢埄鍐炬畷婵犫偓娴兼潙鍐€闁挎稑瀚壕浠嬫煕鐏炲墽鎳嗛柛蹇撹嫰閳规垿顢涘鐓庢濠碘€冲级閸旀瑥顕ｉ幘顔碱潊闁绘鏁搁弳?/// </summary>
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
    /// 闂傚倸鍊搁崐椋庣矆娓氣偓楠炲鏁嶉崟顒佹闂佽法鍠撴慨瀵哥磼閳哄懏鈷戞い鎺嗗亾缂佸鎸抽幏鎴︽偄鐏忎焦鏂€闂佺粯蓱瑜板啴顢旈鍫熷€垫慨姗嗗墻閻撳ジ鏌＄仦璇插鐎殿喗娼欒灃闁逞屽墯缁傚秵銈ｉ崘鈺佷画濠电姴锕ょ€氼厾娆㈤崣澶岀缁绢參顥撶弧鈧Δ鐘靛仜閸熸挳宕洪敓鐘插窛妞ゆ柨鍚嬮悿鍌滅磽娴ｇ鈷斿褎顨婇弫瀣箾鐎涙鐭嬮柛搴㈠▕婵℃挳骞掗幋顓熷兊濡炪倖甯掗敃銈嗘叏濞戞氨纾藉ù锝嗗絻娴滈箖姊洪崨濠冨闁搞劑浜堕幃锟犲即閻旇櫣顔曢梺绯曞墲閳笺倝顢旈崼鐔封偓鍫曟煥閺囩偛鈧綊鎮￠崘顏呭枑婵犲﹤鐗嗙粈鍫熺箾閸℃ɑ瀚撮柛銉墮缁犮儲銇勯弮鍥棄閹兼潙锕ョ换娑欐綇閸撗呅氬銈庡亜椤﹂潧鐣烽弴銏″亜闁稿繗鍋愰崣鍡涙⒑閸濆嫬鏆欐繛璇х畵钘熷鑸靛姈閸嬶絿鎲稿澶嬪仱闁靛ě鍕簥濠电偞鍨崹鍦棯瑜旈弻鐔煎箹椤撶偛绠归梺鍛婃煛閸嬫挾绱撻崒姘偓宄懊归崶銊ｄ粓缂佸顑欓悢鍡樹繆椤栨繃纭跺ù婊堜憾濮婄粯绗熼埀顒勫焵椤掑倸浠滈柤娲诲灡閺呭爼顢氶埀顒€螞娴ｇ懓绶為柟閭﹀幖閳ь剛鏁婚弻銊モ槈濞嗗浚鏆㈤梺绋款儐閹瑰洭寮诲☉姘ｅ亾閿濆骸浜濋悘蹇ｅ幗閵囧嫰顢樺鍐潎閻庤娲橀敃銏′繆濮濆矈妲洪梺姹囧灩椤兘寮婚敐鍡樺劅闁挎繂鎳嶉崠鏍⒑缁嬪尅鏀婚柣妤佹礋閳ユ棃宕橀埡鍐炬祫闁诲函绲婚崝澶愬磻閹捐纾奸柣鎰叀閸炲爼姊洪崫鍕窛闁哥姵鎹囧畷婵嬪醇閺囩啿鎷洪梺闈╁瘜閸樺吋绂嶉悙顒傜闁稿繗鍋愰幊鍕偓鍨緲鐎氼剟顢橀崗鐓庣窞閻庯綆鍓欓獮鍫熺節閻㈤潧浠滄俊顐ｇ懇閹柉顦寸紒顔界懅缁辨帒螣閸︻厾鐣鹃梻浣告啞閻熴儵藝娴兼潙纾归柟閭﹀枓閸嬫挾鎲撮崟顒傦紭闂佺娴烽弫璇差嚕椤愶箑绀冮柍鍝勫暟閹虫繈姊洪幖鐐插姌闁告柨绻樺鑼额樄婵﹦绮幏鍛村川婵犲倹娈橀梻浣告啞濮婂綊鏁嬮梺浼欑悼閸忔﹢骞冩禒瀣棃婵炴垶顭囨禍浼存⒒娓氣偓濞佳嗗闂佸搫鎳忛惄顖炲箖濮椻偓閹煎綊顢曢妶鍥╂闂備礁澹婇悡鍫ュ磻閸涱垯鐒婇柣銏㈡暩绾惧ジ鏌嶈閸撴艾顕ラ崟顒傜闁圭儤鍨圭粔铏光偓瑙勬礃椤ㄥ﹪骞婂┑瀣骇婵炲棛鍋撻ˉ銈夋⒒閸屾瑧鍔嶉柟顔肩埣瀹曟繄浠﹂悾灞炬濡炪倖甯掔€氱兘寮€ｎ偆绠鹃柛鈩兩戠亸顓灻归懖鈺佲枅闁哄本鐩鎾Ω閵夛妇浜梻浣虹帛缁诲秹宕圭捄渚綎婵炲樊浜滃婵嗏攽閻樻彃鈧顢栭崒娑氱瘈闁汇垽娼ф禍褰掓煕鐎ｎ偅灏柍瑙勫灴閹瑩宕ｆ径濠冾仩闂佽瀛╃粙鍫ュ疾濠靛牆鍨濇繛鍡樺姇缁剁偤鏌熼柇锕€澧伴柛鏃撶畱椤啴濡堕崱妤冪憪闂佺厧鍟块悥濂稿春閵忋倕绫嶉柍褜鍓熸俊鐢稿礋椤斿墽鏉搁梺鍦亾濞兼瑥鈻嶈濮婃椽妫冨☉姘杸闂佺懓鎲℃繛濠囩嵁閸愵厹浜归柟鐑樺灩閸婄偤姊洪棃娑辩叚濠碘€虫喘钘濋柡澶嬵儥濞撳鏌曢崼婵堢闁告帊鍗抽弻锝夊冀瑜嬮崑銏⑩偓娈垮枛椤嘲鐣烽崡鐐╂婵☆垳鈷堥崯宀勬⒒娴ｅ憡鍟炴繛璇х畵瀹曘垺銈ｉ崗?
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
                "闂傚倸鍊搁崐鐑芥嚄閸洖绠犻柟鎹愵嚙鎼村﹪鏌＄仦璇插壐闁搞儺鍓﹂弫宥嗙節闂堟稑顏ф慨瑙勵殜濮婃椽骞栭悙鎻掑闂佸搫鎳忕划搴ㄥ焵椤掍浇澹樻い顓犲厴瀵濡搁埡浣稿祮濠德板€愰崑鎾趁瑰鍫㈢暫闁诡喖鍢查…銊╁礋椤愵偀鍋撳鍕濠㈣泛顑嗙粈瀣殽閻愭潙濮屾い锕€婀遍埀顒冾潐濞叉粓宕伴弽顓溾偓浣肝旀担鐟邦€撻柡澶屽仧婢ф鎯?,
                () => client.Users_GetUsers(InputUser.Self),
                cancellationToken,
                resetClientOnTimeout: true);
            cancellationToken.ThrowIfCancellationRequested();
            var self = users.OfType<User>().FirstOrDefault();

            if (self == null)
            {
                var missingProfile = new TelegramAccountStatusResult(
                    Ok: false,
                    Summary: "闂傚倸鍊搁崐椋庣矆娓氣偓楠炴牠顢曢敃鈧悿顕€鏌ｅΔ鈧悧濠囧矗韫囨稒鐓熼柕蹇曞Х娴犳盯鏌涜箛鏃傜煉闁哄本鐩、鏇㈡晲閸℃瑯妲伴梻浣规偠閸婃牕顫忔繝姘劦妞ゆ帒鍊归崵鈧柣搴㈠嚬閸樺ジ鈥﹂崹顔ョ喖鎮℃惔锝囩摌闂備胶顫嬮崟鍨暦闂佺顑戠紞渚€寮婚悢鐓庣骇闁割煈鍣弳銏ゆ⒑娴兼瑧绉ù婊庡墴楠炲骞栨担鍝ヮ唵闁诲繒鍋涙晶浠嬪Υ婵犲洦鈷戦柛锔诲弾濞兼帡鏌涢妷鎴濇媼濡茶淇婇悙顏勨偓鏍暜婵犲洦鍤勯柤鍝ユ暩椤?,
                    Details: "Users_GetUsers(Self) 闂傚倸鍊搁崐椋庣矆娓氣偓楠炴牠顢曢敂钘変罕濠电姴锕ょ€氼噣銆呴弻銉︾叆婵犻潧妫欐径鍕偓瑙勬礃閻擄繝寮婚悢鍛婄秶闁告挆鍛闂?User",
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

            var summary = "濠电姷鏁告慨鐢割敊閺嶎厼绐楁俊銈呭暞瀹曟煡鏌熼柇锕€鏋涚紒韬插€曢湁闁绘ê妯婇崕鎰版煕?;
            if (profile.IsDeleted)
                summary = "闂傚倸鍊峰ù鍥х暦閸偅鍙忛柣銏㈩焾缁€澶屾喐韫囨洖鍨濆┑鐘宠壘缁狅綁鏌ㄥ┑鍡楁殨婵顨婂娲礈閼碱剙甯ラ梺闈╃秵閸ｏ絽鐣烽鐐插窛閻庢稒顭囬崢鍗炩攽閻愬弶顥滄繛瀛樿壘鍗辩憸鐗堝笚閻撴盯鏌涢埄鍐炬畼濠⒀嶉檮閹便劍绻濋崟顐㈠闂佽鍨卞Λ鍐嵁濮椻偓瀹曟宕ㄩ褎顥?闂傚倸鍊峰ù鍥х暦閻㈢纾婚柣鎰惈缁€鍕喐閻楀牆绗掔痪鎯ь煼閺屾盯寮撮妸銉т哗闂佹悶鍔岄崐濠氬箟閹间焦鍋嬮柛顐ｇ箘閻熸煡姊?;
            else if (profile.IsRestricted)
                summary = "闂傚倸鍊峰ù鍥х暦閸偅鍙忛柣銏㈩焾缁€澶屾喐韫囨洖鍨濆┑鐘宠壘缁狅綁鏌ㄥ┑鍡楁殨婵顨婂铏圭矓閸℃顏存繛鍫熸閺岋綁顢橀悤浣圭暦婵烇絽娲ら敃顏呬繆閸洖宸濇い鏂垮悑椤忊剝淇婇悙顏勨偓鏇犫偓娑掓櫊閹兘鍩￠崨顓℃憰闂侀潧顭堥崕顔嘉ｉ崼鐔剁箚妞ゆ牗绻嶉崬铏圭磼閸撲降浠弒tricted闂?;

            if (probeCreateChannel)
            {
                var probe = await ProbeCreateChannelCapabilityAsync(client, accountId, cancellationToken);
                if (probe.IsFrozen)
                {
                    var frozen = new TelegramAccountStatusResult(
                        Ok: false,
                        Summary: "闂傚倸鍊峰ù鍥х暦閸偅鍙忛柣銏㈩焾缁€澶屾喐韫囨洖鍨濆┑鐘宠壘缁狅綁鏌ㄥ┑鍡楁殨婵顨婇弻锝夋偐閸忓懓鍩呴梺鍛婃煥缁夌懓鐣烽幋锕€鍐€闁靛绲肩花濠氭⒑鐟欏嫬顥愰柡鍛洴閹﹢鍩￠崨顔惧幐闁诲函缍嗛崑鍡涘箠閸涱垳纾奸弶鍫涘妽鐏忎即鏌熷畡鐗堝櫧闁瑰弶鎸冲畷鐔碱敆閳ь剟宕滈悽鐢电＝闁稿本鐟︾粊鏉款渻鐎涙ɑ鍊愰柟顔惧厴閸┾剝鎷呴悜妯活啎婵犳鍠楅敃鈺呭礂濡綍锝夊醇閻斿墎绠氬銈嗙墬缁诲倹绂嶉悷閭︾唵閻犲搫鎼顏勄庨崶褝韬い銏℃礋婵″爼宕担瑙勭様闂傚倷鐒﹂幃鍫曞礉瀹ュ洠鍋撶粭娑樻噽閻鈧箍鍎卞Λ搴ㄥ磻閸涘瓨鐓曢柟鎵虫櫅婵℃寧銇勯弬鍖¤含婵﹥妞藉Λ鍐ㄢ槈鏉堛剱銈夋⒑缁嬫寧鍞夊ù婊庡墰濡叉劙鎮欓崫鍕啋缂傚倷鐒﹂…鍥储閸楃偐鏀介柣鎰级椤ョ偤鏌涢弮鎾剁暠闁崇粯鎸搁…銊╁礋椤忓棛鐣鹃梻浣虹帛閸旓箓宕滃璺何︽繝闈涱儐閻?,
                        Details: $"闂傚倸鍊搁崐椋庣矆娓氣偓楠炲鏁嶉崟顒佹濠德板€曢崯浼存儗濞嗘挻鐓欓悗鐢殿焾鍟哥紒鎯у綖缁瑩寮婚悢鐓庣闁归偊鍘煎☉褏绱撴担鐟版暰缂傚秳绶氬濠氭偄閸涘﹦绉堕梺鍛婃寙閸涱垳宕舵繝鐢靛О閸ㄥジ锝炴径濞掓椽鎮㈤悡搴ゆ憰闂佺粯姊婚崢褏绮婚幒妤佺厵闁绘垶锚閻忊晛霉閻樻彃鈷旂紒杈ㄦ尰閹峰懏顦版惔鈥愁瀴闂備胶鎳撻幉鈩冪箾婵犲偆鍤曟い鎰╁焺閸氬鏌涘鈧悞锕€顩奸妸褏纾藉ù锝堝Г椤忕姵顨ラ悙鑼崠be.Message}{Environment.NewLine}{BuildProfileDetails(profile)}",
                        CheckedAtUtc: checkedAt,
                        Profile: profile);
                    await TryPersistStatusAsync(accountId, frozen, account, persistProfile: true, cancellationToken: cancellationToken);
                    return frozen;
                }

                if (!probe.Success)
                {
                    var failed = new TelegramAccountStatusResult(
                        Ok: false,
                        Summary: "闂傚倸鍊搁崐椋庣矆娓氣偓楠炲鏁嶉崟顒佹濠德板€曢崯浼存儗濞嗘挻鐓欓悗鐢殿焾鍟哥紒鎯у綖缁瑩寮婚悢鐓庣闁归偊鍘煎☉褏绱撴担鐟版暰缂傚秳绶氬濠氭偄閸涘﹦绉堕梺鍛婃寙閸涱垳宕舵繝鐢靛О閸ㄥジ锝炴径濞掓椽鎮㈤悡搴ゆ憰闂佺粯姊婚崢褏绮婚幒妤佺厵闁绘垶锚閻忊晛霉閻樻彃鈷旂紒杈ㄦ尰閹峰懏顦版惔顔叫滈梻浣割吔閺夊灝顬堝銈嗘磸閸庨潧鐣烽悢纰辨晢濞达綀娅ｈ倴濠碉紕鍋戦崐鏍礉閹达箑纾?,
                        Details: $"闂傚倸鍊搁崐椋庣矆娓氣偓楠炲鏁嶉崟顒佹濠德板€曢崯浼存儗濞嗘挻鐓欓悗鐢殿焾鍟哥紒鎯у綖缁瑩寮婚悢鐓庣闁归偊鍘煎☉褏绱撴担鐟版暰缂傚秳绶氬濠氭偄閸涘﹦绉堕梺鍛婃寙閸涱垳宕舵繝鐢靛О閸ㄥジ锝炴径濞掓椽鎮㈤悡搴ゆ憰闂佺粯姊婚崢褏绮婚幒妤佺厵闁绘垶锚閻忊晛霉閻樻彃鈷旂紒杈ㄦ尰閹峰懏顦版惔鈥愁瀴闂備胶鎳撻幉鈩冪箾婵犲偆鍤曟い鎰╁焺閸氬鏌涘鈧悞锕€顩奸妸褏纾藉ù锝堝Г椤忕姵顨ラ悙鑼崠be.Message}{Environment.NewLine}{BuildProfileDetails(profile)}",
                        CheckedAtUtc: checkedAt,
                        Profile: profile);
                    await TryPersistStatusAsync(accountId, failed, account, persistProfile: true, cancellationToken: cancellationToken);
                    return failed;
                }

                // 闂傚倸鍊搁崐宄懊归崶顒婄稏濠㈣泛顑囬々鎻捗归悩宸剰闁搞劌鍊块弻褑绠涢幘纾嬬闂佹椿鍘奸悧鎾诲蓟閻旇櫣纾奸柕蹇曞У閻忓牏绱撴担鍝勵€撶紓宥勭窔瀵鎮㈤崗鑲╁姺闂佹寧娲嶉崑鎾愁熆瑜滈崰妤呭Φ閸曨垼鏁冮柕鍫濇噳閺嬪懘姊洪崫鍕潶闁告柨鐭傞崺銉﹀緞婵炪垻鍠愮粭鐔碱敍濞戝崬鏁崇紓鍌氬€搁崐鐑芥倿閿曞倶鈧啴宕ㄥ銈呮喘楠炲酣鎸婃竟鈺嬬畵閺岀喖鎮ч崼鐔哄嚒缂佺虎鍘搁崑鎾绘⒒娴ｈ櫣甯涢柛鏃€娲熼幃娲Ω閳轰胶锛熼梺闈涱槴閺呮粓鍩涢幋锔界厱婵炴垶锕╅悡顒勬煟閹哄秶鐭欓柡灞剧洴閹晠鎼归銏ょ€洪梻渚€鈧稓鈹掗柛鏃€鍨块悰顕€寮介妸锕€顎撻梺鍛婄缚閸庢娊寮抽悩娴嬫斀闁绘劕鐡ㄧ亸浼存煠瑜版帞鐣洪柡浣稿暣閺佸倿宕滆閻掑吋绻濋姀锝嗙【闁愁垱娲濋妵鎰板箳閹惧厖姹楅梻浣告啞閻熴儵藝椤栨埃鏋旂€光偓閸曨剛鍙冨┑鈽嗗灣閸樠囧几濞戙垺鐓涢悘鐐插⒔椤偐绱掗悩宕囨创妤犵偞锕㈠鍫曞箣閻愬灚娈繝纰夌磿閸嬫垿宕愰弽顬″搫顫滈埀顒勫箚閸曨垼鏁嶉柣鎰版涧缁?                var okWithProbe = new TelegramAccountStatusResult(
                    Ok: true,
                    Summary: summary,
                    Details: $"闂傚倸鍊搁崐椋庣矆娓氣偓楠炲鏁嶉崟顒佹濠德板€曢崯浼存儗濞嗘挻鐓欓悗鐢殿焾鍟哥紒鎯у綖缁瑩寮婚悢鐓庣闁归偊鍘煎☉褏绱撴担鐟版暰缂傚秳绶氬濠氭偄閸涘﹦绉堕梺鍛婃寙閸涱垳宕舵繝鐢靛О閸ㄥジ锝炴径濞掓椽鎮㈤悡搴ゆ憰闂佺粯姊婚崢褏绮婚幒妤佺厵闁绘垶锚閻忊晛霉閻樻彃鈷旂紒杈ㄦ尰閹峰懏顦版惔鈥愁瀴闂備胶鎳撻幉鈩冪箾婵犲偆鍤曟い鎰╁焺閸氬鏌涘鈧悞锕傚磽闂堟侗娓婚柕鍫濇閸у﹪鏌涚€ｎ偅宕岄柟顔款潐閹峰懘宕烽鐐茬哗闂備礁鎼惌澶岀礊娴ｈ鍙忛柍褜鍓熼弻宥夊传閸曨偅娈柣銏╁灛閸斿海妲愰幘瀛樺濞寸姴顑呴幗鐢告⒑閼姐倕鏋傞柛搴ｆ暬閵嗕礁鈻庨幒鏃傛澑濠电偞鍨堕悷銉モ枔閵堝鐓熼柣妯煎劋椤忕娀鏌涙惔娑樷偓婵嬨€佸▎鎾崇倞妞ゆ帊璁查幏娲⒑閸涘﹦鈽夐柨鏇樺劦閹繝鎮㈤崗鑲╁幈闂佸搫鍊藉▔鏇″€寸紓鍌欑贰閸犳帡寮插鍛床婵犻潧妫鈺傘亜閹捐泛浠掗柛婵囶殜濮婄粯鎷呴搹骞库偓濠囨煕閹惧绠為柍銉畵瀹曞爼顢楅埀顒€娲块梻浣告贡閾忓酣宕板Δ鍛；闁挎繂顦伴悡鏇㈡煏婢舵稓鍒板┑锛勬櫕缁辨帡鍩€椤掑倵鍋撻敐搴′簴濞存粍绮撻弻銊モ攽閸℃ê娅ч梺绯曟櫔缂嶄線寮婚敍鍕勃闁兼亽鍎哄Λ锕€螖閻橀潧浠﹂柣鐔叉櫅閻ｇ兘鎮㈢喊杈ㄦ櫍闂佺粯鐟㈤崑鎾翠繆閼碱剦妫〦nvironment.NewLine}{BuildProfileDetails(profile)}",
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
                Summary: "闂傚倷娴囬褍霉閻戣棄绠犻柟鎹愵嚙缁犵喖姊介崶顒€桅闁圭増婢樼粈鍐┿亜閹惧鐭嗘慨瑙勵殜濮婃椽骞栭悙鎻掑Η闂侀€炲苯澧崡?,
                Details: "闂傚倸鍊搁崐鐑芥嚄閸洖绠犻柟鍓х帛閸婅埖绻濋棃娑氬ⅱ闁活厽鎸鹃埀顒冾潐濞叉牕煤閵娧呯焼濠电姴娲﹂悡娑㈡煕鐏炲彞绶遍柛搴涘劦閺屾盯鎮㈤柨瀣畻闂佸搫鐬奸崰鎾舵閹烘嚦鐔兼惞閸︻厽鍣紓鍌氬€风粈浣割嚕閸撲讲鍋撶粭娑樻噺瀹曞弶绻涢幋娆忕仼缂佺媴绲剧换婵嬫濞戞艾顣洪梺鍐插槻閿曪妇妲愰幘瀛樺濞寸姴顑呴幗鐢告⒑閼姐倕娅愮紒鐘虫尰娣囧﹪鎮界粙璺槹濡炪倖鐗楃粙鎴炵濞差亝鈷戦柛娑橈功閳藉鏌ㄩ弴妯哄姦濠碘剝鎸冲畷姗€顢欑憴锝嗗缂傚倷绀侀鍫濃枖閺囩姷涓嶉柟缁樺坊閺€浠嬫煟閹邦垼鍤嬬€规悶鍎甸幗?闂傚倸鍊搁崐椋庣矆娓氣偓楠炲鏁嶉崟顒佹闂佽法鍠撴慨瀵哥磼閳哄懏鈷戞い鎺嗗亾缂佸鎸抽幏鎴︽偄鐏忎焦鏂€闂佺粯蓱瑜板啴顢旈妶澶嬬厱閻庯綆鍋呯亸浼存煙瀹勭増鍤囩€规洘锕㈤垾锕傚箣閻戝棙顥嶉梻鍌氬€烽悞锕傚箖閼搁潧顕遍柛銉墮閻愬﹦鎲歌箛娑欏亗闁硅揪闄勯埛鎴︽煕濠靛棗顏悮顏堟⒑閸涘﹥鐓ュΔ鐘虫倐瀵娊鍩￠崨顔规嫽?,
                CheckedAtUtc: checkedAt);
        }
        catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested)
        {
            // Blazor 婵犵數濮烽。顔炬閺囥垹纾婚柟杈剧畱绾捐淇婇妶鍛櫣闁哄绶氶幃褰掑炊瑜庨埢鏇㈡煟閹烘洘顥夐棁澶愭煕韫囨挸鎮戠紓宥嗗灩缁辨帡鍩€椤掑嫬骞㈡繛鎴炵憿閹锋椽姊洪崜鑼帥闁革綆鍣ｅ畷鏇㈠箛閻楀牏鍘介梺鍦檸閸ㄧ増绂?闂傚倸鍊搁崐椋庣矆娓氣偓楠炴牠顢曢敃鈧壕濠氭煕閳╁啰鈽夐柣顓燁殜閺屽秷顧侀柛鎾寸〒濡叉劙骞掗弮鍌滐紲濠碘槅鍨伴崥瀣礆濞戙垺鍊垫繛鍫濈仢濞呮﹢鏌涢悩鍐插摵妤犵偛绻掗埀顒婄秵閸犳牠骞戦懜鐐逛簻闁规崘娉涢灞句繆閼碱優绛﹐ped 闂?DbContext 闂傚倸鍊搁崐椋庣矆娓氣偓楠炲鏁撻悩鍐蹭画濡炪倖鐗滈崑娑㈠垂閸岀偞鐓熼柕蹇嬪焺閻掑墽绱掗埀顒勫磼濞戞绠氬銈嗙墬閻熴劑顢楅悢鍏肩厱闁绘棃鏀遍崵鍥煛鐏炶鈧牜缂撴禒瀣闁告瑥顦扮欢顓熺節閻㈤潧浠ч柛妯犲洦鍋夐柣鎾冲瘨濞兼牗绻涘顔荤盎缂佺姷鏁婚弻鐔兼惞閻熼娌梺杞扮窔缁犳牕顫忓ú顏勪紶闁告洦鍋€閸嬫挻顦版惔銏╁仺濠殿喗顭堝▔娑㈠几娴ｈ　鍋撻獮鍨姎闁瑰啿顦嵄闁割偆鍠撶粻楣冩煕閳╁喚娈樼紒銊ユ健閺屽秹宕崟顒€娅ら梺绋匡功閺佸寮婚敐澶嬪亜闁告稑锕﹂崙锟犳⒑閸濆嫭顥撻柛銊︽そ婵＄敻宕熼锝嗘櫇闂侀潧绻嗛弲娑㈠礈鏉堚晝纾藉ù锝呭閸庡繑銇勯敃鍌涙锭闁伙絿鍏橀、鏇㈡晝閳ь剛绮堢€ｎ偁浜滈柟鎵虫櫅閳ь剚鐗犲畷鎴ｎ槼缂佺粯绻堥幃浠嬫濞戞鍕冮梻浣规偠閸斿酣寮繝姘畺鐟滄棃鐛崶顒€绾ч悹鎭掑妼婵附淇婇悙顏勨偓鏍ь啅婵犳艾纾婚柟鐐灱閺€浠嬫煕鐏炲墽鈯曢柍閿嬪笧缁辨帡顢氶崨顓炵閻庡灚婢樼€氫即鐛崶顒夋晢闁逞屽墮椤曪絽鐣￠柇锔藉瘜闂侀潧鐗嗗Λ娑欐櫠闁秵鐓曢悗锝庡墮娴犺京鈧鍠楅悡锟犲春閿熺姴宸濇い鏃堟？閸栨牠姊绘担瑙勫仩闁稿孩娼欓埢鏂库槈閵忊€斥偓鑸电箾閹存瑥鐏柣?            return new TelegramAccountStatusResult(
                Ok: false,
                Summary: "闂傚倷娴囬褍霉閻戣棄绠犻柟鎹愵嚙缁犵喖姊介崶顒€桅闁圭増婢樼粈鍐┿亜閹惧鐭嗘慨瑙勵殜濮婃椽骞栭悙鎻掑Η闂侀€炲苯澧崡?,
                Details: "婵犵數濮烽。顔炬閺囥垹纾婚柟杈剧畱绾捐淇婇妶鍛櫣闁哄绶氶幃褰掑炊瑜庨埢鏇㈡煟閹哄秶鐭欓柡灞稿墲瀵板嫮鈧綁娼ч崝宀勬⒑閸濄儱鏋庨柟铏耿瀵鎮㈤搹鍦紲濠碘槅鍨靛▍锝夋偡閵娾晜鍋℃繝濠傚椤ュ鈹戦悙鈺佷壕婵?闂傚倸鍊搁崐椋庣矆娓氣偓楠炲鏁嶉崟顒佹闂佽法鍠撴慨瀵哥磼閳哄懏鈷戞い鎺嗗亾缂佸鎸抽幏鎴︽偄鐏忎焦鏂€闂佺粯锚瀵埖寰勯崟顖涚厸闁告粈绀佹晶鎾煙椤旀枻鑰块柟顔界懇濡啫鈽夊Δ鈧ˉ姘舵⒒娴ｇ瓔鍤冮柛銊╀憾閹兘濡烽埡鍌滅暢闂傚倷鐒︾€笛呮崲閸屾娑樷槈閵忊€冲壒闁瑰吋鎯屽鎸庣濠婂嫨浜滈柟浼存涧婢у瓨淇婄紒銏犳灓缂佽鲸甯為埀顒婄秵娴滆泛螣閳ь剙顪冮妶鍐ㄧ仾闁挎洏鍨介獮鍐閵堝懍绱堕梺闈涱槶閸庤鲸顨?,
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
            // 婵犵數濮烽。顔炬閺囥垹纾婚柟杈剧畱绾捐淇婇妶鍛櫣闁哄绶氶幃褰掑炊瑜庨埢鏇㈡煟?婵犵數濮烽弫鎼佸磻閻樿绠垫い蹇撴缁躲倝鏌涢幘妤€鍟悘濠囨煙閸忚偐鏆橀柛鏂跨焸瀵憡绗熼埀顒勫蓟閺囥垹閱囨繝鍨姈绗戦梻浣告惈鐎氥劑宕曢悽绋胯摕闁挎繂顦Λ姗€鏌涢…鎴濇灍娴滄盯姊绘担瑙勫仩闁稿﹥鐗滈弫顕€鍨鹃幇浣圭稁闂佺粯鍨惰摫闁稿海鍠栭弻鐔煎箚瑜嶇敮鐘差熆鐟欏嫭绀冪紒缁樼箓閳绘捇宕归鐣屽讲缂傚倷娴囩亸顏堝磻閹邦喖鍨濋柛顐ゅ枔閻熷綊鏌嶈閸撶喖濡存担鍓叉僵闁煎摜鏁搁崝鍫曟倵楠炲灝鍔氭俊顐ｎ殔閳绘挸顭ㄩ崼鐔哄幗闁瑰吋鐣崝宥呪槈瑜旈弻鐔烘嫚瑜忕弧鈧梺?DbContext 闂傚倸鍊搁崐鎼佸磹閻戣姤鍊块柨鏇氶檷娴滃綊鏌涢幇闈涙灍闁哄懏绻堥弻鏇熷緞閸℃ɑ鐝斿┑鈽嗗亝閿氶柕鍡樺笒椤繈鏁愰崨顒€顥氶梻鍌欐祰濡椼劎绮堟担铏圭煋闁煎摜鏁搁々鐑芥煥閺囩偛鈧綊宕靛澶嬬厪濠㈣泛鐗嗛崝鏉懨归崗鍏煎磳婵﹦绮幏鍛槹鎼存繆顩紓鍌欑劍瑜板啴鈥﹀畡鎵殾闁哄洢鍨圭涵鈧梺缁樺姇閻忔艾鈻撴總鍛娾拺缂侇垱娲栨晶鑼磼鐎ｎ偄鐏撮柟?        }
        catch (Exception ex)
        {
            // 闂傚倸鍊搁崐椋庣矆娓氣偓楠炲鏁撻悩鍐蹭画闂佹寧娲栭崐褰掑磻鐎ｎ偂绻嗛柕鍫濇噹閺嗙喖鏌ｉ鐔稿磳闁哄矉缍侀獮瀣晲閸♀晜顥夐梻浣告贡閹虫捇鎮ユ總绋胯摕闁挎繂顦粻锝夋煟閹邦剚顥為柛銊ユ健楠炲啴鎮介崜鍙夋櫖濠殿喗锕徊楣冿綖瀹ュ鈷戦悷娆忓閸斻倗鐥紒銏犲箺闁哄懌鍎茬换婵嬫偨闂堟稐娌梺鍦焾閹诧繝寮查懜鍨劅闁靛鍎抽悿鍛存⒑缂佹﹩娈旈柣妤€锕ラ崚濠囧箻椤旂晫鍘卞┑鐐村灦閿曨偊宕濋悢鐑樺枑閻㈩垼鍠氱粙鑽ょ磼缂佹绠炲┑顔瑰亾闂佺粯锚閻ゅ洨娑甸埀顒€鈹戦悩顐ｅ闁告洖鐏氶悾鍫曟煣閼姐倕浠遍柡灞界Ч瀹曨偊宕熼顐＄磻闂備焦鐪归崐鎰板磻?            if (!cancellationToken.IsCancellationRequested)
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
                    "闂傚倸鍊峰ù鍥х暦閸偅鍙忛柡澶嬪殮濞差亜鐓涢柛婊€鐒﹂弲顏堟偡濠婂嫬鐏村┑?777000 缂傚倸鍊搁崐鎼佸磹妞嬪孩顐介柨鐔哄Т閸ㄥ倿姊婚崼鐔恒€掗柡鍡畵閺岋綁濮€閵堝棙閿梺鎼炲妽缁诲啰鎹㈠☉姗嗗晠妞ゆ棁宕甸崙褰掓⒑缂佹ê绗х紒顕呭灦楠炲牓濡搁妷搴ｅ枑瀵板嫮绮欓幐搴″壍闂傚倷绀侀幉锟犳偡閵忋倕纾婚柟鍓х帛閳锋帒霉閿濆懏鍟為柛鐔哄仱閺屻劌顫濋婵堢畾濡炪倖鍔х紞鍡椻枔濞嗘挻鐓?,
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
    /// 婵犵數濮烽弫鎼佸磿閹寸姴绶ら柦妯侯棦瑜版帒纾奸柣鎰皺閻涖儱鈹戞幊閸婃洟骞婃惔銊ュ嚑?Telegram 婵犵數濮烽弫鎼佸磻閻愬搫鍨傞柛顐ｆ礀缁犱即鏌涢锝嗙缁炬儳缍婇弻鈥愁吋鎼粹€冲闂佽桨绀佸Λ婵嬪蓟閿濆绠涙い鎾跺枎閸撶懓鈹戦悙鑼ⅱ闁哄拋鍋婇崺鈧い鎺嗗亾闁告ɑ绮撳畷鎴﹀箻缂佹鍘遍梺瑙勬緲閸氣偓缂併劌鍚嬮妵鍕敃閵忊懣褎鎱ㄦ繝鍐┿仢妤犵偞鍔栭幆鏃堟晲閸屾侗娼旈梻鍌欒兌缁垶銆冮崨鏉戠疇闁规壆澧楅崵宥夋⒑椤掆偓缁夋挳鎮″☉銏＄厱闁规澘鍚€缁ㄥジ鏌ㄥ☉妯肩闁诡喖鍢查…銊╁礃椤庮垎鍥ㄧ厵妞ゆ牗鐟х粣鏃€顨ラ悙宸剶闁轰礁鍊块幐濠冨緞婵犲偆妫欏┑鐘垫暩閸嬬偤骞愭繝姘殞濡わ絽鍟弲婵嬫煥閺傚灝鈷旈柣顓炴閺屾盯鍩勯崘顏佸闂?    /// </summary>
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
                return (false, "闂傚倸鍊搁崐椋庣矆娓氣偓楠炴牠顢曢敃鈧壕褰掓煕瑜庨妵婊堝焵椤掆偓閸婂潡寮崒鐐茬闁归偊鍎烽敓鐘斥拺闁告劕寮堕幆鍫ユ煕閻斿憡绶查摶鐐淬亜閺嶃劎鐭嬮柛鐘冲姉閳ь剙绠嶉崕閬嵥囨导鏉戠？濠电姴娲﹂悡娆撳级閸儳鐣烘俊缁㈠櫍閺屾稒鎯旈埛姘间邯濠€渚€姊洪幐搴ｇ畵妞わ箒妫勮灋闁硅揪闄勯悡鐔兼煟閺傛寧鎲哥紒鐘靛仱閺岋紕浠︾拠娴嬪亾濠靛绠圭憸鐗堝俯閺佸啴鏌曡箛锝嗙窔闁哥姴锕娲嚒閵堝憛锝夋煕閺冣偓椤ㄥ﹤鐣锋导鏉戠閻犲搫鎼悘?);

            currentPassword = (currentPassword ?? string.Empty).Trim();
            newPassword = newPassword.Trim();
            hint = (hint ?? string.Empty).Trim();

            var client = await GetOrCreateConnectedClientAsync(accountId, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            // 闂傚倸鍊搁崐椋庣矆娓氣偓楠炲鏁撻悩鍐蹭画闂侀潧顦弲娑氬閻熸噴褰掓偐瀹割喖鍓伴梺?WTelegramClient 闂傚倸鍊峰ù鍥敋瑜嶉～婵嬫晝閸岋妇绋忔繝銏ｆ硾鐎涒晠骞婂畝鍕拻濞达絽鎲￠幆鍫ユ煟椤掆偓閵堢鐣锋导鏉戠閻犲搫鎼悘濠囨⒑閸撴彃浜為柛鐘冲姈閹便劌顭ㄩ崼鐔哄幍濠电偛鐗嗛悘婵嬪几濞嗘垹纾奸柟鎻掝儑缁夘喗鎱ㄦ繝鍐┿仢妤犵偞鍔栭幆鏃堟晲閸屾凹娼撳┑锛勫亼閸婃牠宕归悽鍛婂仒婵繃绫島nt_UpdatePasswordSettings 闂傚倸鍊搁崐鎼佸磹閹间礁纾圭紒瀣紩濞差亝鍋愰悹鍥皺閿涙盯姊洪悷鏉库挃缂侇噮鍨跺畷?SRP 闂傚倸鍊搁崐椋庣矆娓氣偓楠炴牠顢曢妶鍥╃厯闂佸憡娲﹂崢钘夌暦閸欏绡€闂傚牊渚楅崕鎰版煕鐎ｎ倖鎴犳崲濞戙垹骞㈡俊顖濐嚙绾板秶绱掗悙顒€鍔ゆ繝鈧柆宥嗗剦妞ゅ繐鐗滈弫鍥煏韫囧﹥娅嗘い銉︾墱缁辨挻鎷呴幓鎺嶅闂備礁鎲″ú锕傚垂闁秵鍋傛繛鎴欏灪閻撴洟鏌熼柇锕€澧い蹇婃櫊閺屾盯鎮㈤崨濠勫嚒闁告浜堕弻锟犲炊閵夈儳浠鹃梺缁樻尵閸犳劙濡甸崟顖氱睄闁搞儜鍐╁劒缂傚倷鐒︾粙鎴犳崲閹烘梹顫曢柟鐑樻尰缂嶅洭鏌曟繛褍鏈▓鍦磽閸屾瑧顦︽い鎴濇嚇钘濆ù鍏兼綑缁犳岸鏌￠崘銊у妞ゎ偄鎳橀弻銊モ槈濡警浠奸柣銏╁灠闁帮絽顫忛搹瑙勫珰闁哄被鍎卞鏉库攽閻愭澘灏冮柛蹇曞亾缁嬫垿鍩ユ径鎰潊闁炽儱鍘栭崙?settings
            var accountPwd = await client.Account_GetPassword();
            cancellationToken.ThrowIfCancellationRequested();

            // 闂傚倸鍊搁崐椋庣矆娓氣偓瀹曘儳鈧綆鍠楅崕鎴犳喐閻楀牆绗掔痪鎯ф健閹鈽夊▍顓т簽濞嗐垽鎮欓悜妯煎幈闂佸搫娲㈤崝宀勬倶閻樼粯鐓熸い鎾跺櫏濞堟﹢鏌嶈閸撴盯骞婇幘鍨涘亾濮樼厧骞栨い顓炵仢椤粓鍩€椤掆偓閻ｇ兘鎮界粙璺唺闂佸搫鍊搁幖顐ｇ閻愵剛绠鹃柛顐ｇ箘娴犮垽鏌＄€ｎ偅宕岄柡灞剧〒閳ь剨缍嗛崑鍛暦瀹€鍕厸閻忕偛澧介埥澶嬨亜椤愶絿绠為柟顔瑰墲閹棃鏁愰崟顒€鐓曢梻鍌欐祰瀹曠敻宕戦悙鐑樺殞闁诡垎鍛濡炪倖甯掔€氬摜绱為弽顓熺厪闊洤顑呴埀顒佹礀閺侇噣姊绘担绋挎毐闁圭⒈鍋婂畷顖烆敃閿旇棄鍓瑰┑掳鍊曢幊蹇涙偂閻斿吋鐓熸俊顖氭惈缁狙囨煟椤撶噥娈曞ǎ鍥э躬閹瑩寮堕幋鐙€鍎岄柣搴＄仛濠㈡﹢鏁冮妷鈺佄ч柨婵嗩槸缁€鍐煏婵炲灝鍔ょ憸鏉款槹娣囧﹪鎮欓鍕ㄥ亾閺囩姵宕查柟鐗堟緲缁犵娀骞栨潏鍓у埌缂佽翰鍊濋弻锝夋偄鐠囪尙鍔烽梺閫炲苯澧悽顖涘浮閿濈偛鈹戠€ｅ灚鏅ｉ梺缁橈供閸嬪嫰锝炵仦鍓х瘈缁剧増蓱椤﹪鏌涚€ｎ偄娴€规洘鍨垮畷鍗炩槈濡厧濮︽俊鐐€栫敮鎺斺偓姘煎幖閳藉顦冲ǎ鍥э躬椤㈡洟鏁愰崶銊ュ灡闂備浇顕栭崰鎾诲磿闁秮鈧箓濡搁埡渚€鍞堕梺缁樻煥閹诧繝鎮甸锔解拺閻犲洩灏欑粻鎶芥煕鐎ｎ剙孝閾荤偤鏌涢弴銊ョ仭闁稿﹤鐖奸弻锝夊棘閸喗鍊梺缁樻尵閸犳牠寮诲鍡樺闁瑰嘲鑻崢锟犳⒑閸︻厽娅曞┑鐐╁亾闂佽鍠曠划娆撱€佸☉妯锋婵炲棗绻愰弨顓熶繆閵堝洤啸闁稿鍋ら獮鎴﹀炊椤掑倸绁﹂梺鎼炲労閸擄箓寮繝鍥х骇闁绘劖娼欓ˉ瀣渻?
            TL.InputCheckPasswordSRP? oldCheck = null;
            if (accountPwd.current_algo != null)
            {
                if (string.IsNullOrWhiteSpace(currentPassword))
                    return (false, "闂傚倸鍊峰ù鍥х暦閸偅鍙忛柡澶嬪殮濞差亜顫呴柕鍫濇噽閿涚喖鏌ｆ惔銏⑩姇闁挎氨绱掗崜浣镐槐闁哄本鐩鎾Ω閵壯傚摋闂備焦妞块崢浠嬨€冮崼銉ョ劦妞ゆ帒鍊归崵鈧柣搴㈠嚬閸樺ジ鈥﹂崹顕呮建闁逞屽墮閻ｇ兘鎮界粙璺唺闂佸搫鍊搁幖顐ｇ閻愵剛绠鹃柛顐ｇ箘娴犮垽鏌＄€ｎ偅宕岄柡灞剧〒閳ь剨缍嗛崑鍛暦瀹€鍕厸閻忕偛澧介埥澶嬨亜椤愶絿绠為柟顔瑰墲閹棃鏁愰崟顒€鐓曢梻鍌欐祰瀹曠敻宕戦悙鐑樺殞闁诡垎鍛濡炪倖甯掔€氬摜绱為弽顓熺厪闊洤顑呴埀顒佹礀閺侇噣姊绘担绋挎毐闁圭⒈鍋婂畷顖烆敃閿旇棄鍓瑰┑掳鍊曢幊蹇涙偂閻斿吋鐓熸俊顖氭惈缁狙囨煟椤撶噥娈樼紒杈ㄥ浮閻擃剟骞撻幒鍡椾壕婵°倐鍋撻崡閬嶆煙闁箑鍘撮柡鈧禒瀣厱妞ゆ劑鍊曢弸搴ㄦ煟濠靛懎宓嗛柟顔筋殜閹兘寮跺▎鍙ユ偅闂佸湱鍘ч悺銊у垝鎼达絾顫曢柣鎰惈閸愨偓濡炪倖鎸鹃崰鎾诲储闁秵鈷戠憸鐗堝笒娴滀即鏌涘鈧粻鏍嵁婵犲洤绀冩い鏃囨娴犲搫顪冮妶鍡欏缂佽尪濮ょ粋宥呪攽閸垻锛滈梺绋挎湰濮樸劌鐡梻浣风串缁插墽鎹㈤崼銉ユ槬闁逞屽墯閵囧嫰骞掑鍥獥濠电偛鐗呯划娆撳蓟濞戙垹唯闁靛繆鍓濆鎺楁⒑缁嬫鍎愰柟鐟版搐铻為柛鎰╁妷濡插牊绻涢崱妯曟垿鏁撻妷鈺傗拻?);

                oldCheck = await WTelegram.Client.InputCheckPassword(accountPwd, currentPassword);
            }

            // 闂?InputCheckPassword 闂傚倸鍊搁崐鐑芥倿閿曞倹鍎戠憸鐗堝笒閸ㄥ倸鈹戦悩瀹犲缂佹劖顨婇弻鐔兼偋閸喓鍑￠梺?new_password_hash闂傚倸鍊搁崐鐑芥倿閿旈敮鍋撶粭娑樻噽閻瑩鏌熸潏楣冩闁稿顑夐弻鐔煎礈瑜嶆禒娲煃瑜滈崜姘辨暜閳ュ磭鏆︽繝濠傚婵挳鏌ｉ悢鍝勵暭婵絽娲ら埞鎴︽倷閼搁潧娑х紓浣藉紦缁瑩鐛Δ鈧…銊╁礃閿濆棛浜?current_algo 缂傚倸鍊搁崐鎼佸磹閹间礁纾归柟闂寸缁愭鎱ㄥΟ鎸庣【缂佹劖顨嗛幈銊ノ熼幐搴ｃ€愰梺缁樻尰閿曘垽寮婚垾鎰佸悑閹肩补鈧磭顔戦梻?            accountPwd.current_algo = null;
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
            var msg = string.IsNullOrWhiteSpace(details) ? summary : $"{summary}闂傚倸鍊搁崐鐑芥倿閿旈敮鍋撶粭娑樻噽閻瑩鏌熼悜姗嗘闁轰礁妫涚槐鎺楀棘閸喗缍巘ails}";
            return (false, msg);
        }
    }

    /// <summary>
    /// 闂傚倸鍊搁崐鎼佲€﹂鍕；闁告洦鍊嬪ú顏呮櫇闁稿本銇涢崑鎾绘晝閸屾岸鍞堕梺闈涱樈閸犳寮查埡鍛拺闁硅偐鍋涢崝姗€鏌涢弬鍧楀弰闁靛棗鎳橀幊鐘活敄閽樺澹曢梺绋跨箰椤︻垱绂嶆ィ鍐┾拺闂侇偆鍋涢懟顖涙櫠椤栨稏浜滈柕濠忕到閸旓妇鈧娲栭悥濂稿春閻愬搫绠氱憸灞剧珶閺囥垺鈷掗柛灞炬皑婢ф稓绱掔€ｎ偄娴挊鐔兼煟濡偐甯涢柣鎾跺枛閺屸€愁吋閸愩劌顬嬮梺娲诲幗鐢繝寮婚埄鍐╁闁告縿鍎涢姀掳浜滄い鎰剁悼缁犵偞銇勯姀鈽嗘畷闁瑰嘲鎳愰崠鏍即閻愭澘顥?Telegram 闂傚倸鍊搁崐椋庣矆娓氣偓楠炲鏁撻悩鍐蹭画闂佹寧娲栭崐鎼佸垂閸屾埃鏀介柛灞剧矤閻掑墽绱掗悩宕囧弨闁哄被鍔岄埞鎴﹀幢閳哄倐锕傛⒑鐠囪尙绠烘繛鍛礈閹广垹鈹戦崶鈺冪槇闂佺鏈划宀勩€傞崫鍕ㄦ斀闁绘劕寮堕崳鐑芥煕閵娿劌鍚规俊鍙夊姍楠炴鎷犻懠顒夋О闂備浇顕栭崹搴ㄥ礋椤撴繄鏁栭梻鍌欐祰瀹曠敻宕戦悙鐑樺殞闁诡垎鍛濡炪倖甯掔€氬摜绱為弽顓熺厪闊洤顑呴埀顒佹礀閺侇噣姊绘担绋挎毐闁圭⒈鍋婂畷顖烆敃閿旇棄鍓瑰┑掳鍊曢幊蹇涙偂閻斿吋鐓熸俊顖氭惈缁狙囨煟椤撶噥娈樼紒杈ㄥ浮閸┾偓妞ゆ帊鑳堕悷褰掓煃瑜滈崜鐔肩嵁閸℃鐟归柍褜鍓熼悰顔锯偓锝庡枟閺呮粓鏌﹀Ο渚Ц濠㈣鎸搁埞鎴︽倷閼搁潧娑х紓浣瑰絻婢х晫鍒掓繝鍥х妞ゆ棁鍋愰敍娑㈡⒑閸︻厼浜鹃柡瀣偢瀵劍绂掔€ｎ偆鍘卞銈嗗姂閸婃洟寮搁幋鐐电闁割偅绺块崑銏ゆ煛瀹€瀣М闁诡喒鏅犲畷锝嗗緞鐎ｎ偄鈧兘姊绘担瑙勫仩闁稿﹥鐗犻幃褍顭ㄩ崗鎾呯秮楠炲洭顢栭懞銉р偓濠氭⒑鐟欏嫬鍔ゆい鏇ㄥ弮瀹曟繈妫冨☉鎺撴杸濡炪倖姊归弸缁樼瑹濞戙垺鐓曢柟鎯ь嚟濞插鈧娲栭幖顐ョ亙闂佸憡渚楅崰鎺楀箯婵犳碍鈷戦柟鑲╁仜閸旂數绱掗懠璺盒撶紒鍌氱У閵堬綁宕橀埡鍐ㄥ箺闂備胶绮弻銊╁箟閿涘嫬顥氬┑鐘崇閻撶娀鏌涢幇顖氱毢闁糕晪缍侀弻鐔碱敊閼恒儯浠㈠Δ鐘靛仜椤戝懘鍩為幋锕€绠涙い鎺嶇贰閸?7 婵犵數濮烽弫鍛婃叏娴兼潙鍨傜憸鐗堝笚閸婂爼鏌涢鐘插姎闁汇倗鍋撻妵鍕疀閹炬惌妫″銈庡亝濞叉﹢骞堥妸銉庣喖骞愭惔锝冣偓鎰板级?    /// </summary>
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
                    return (true, "婵犵數濮烽弫鎼佸磻濞戙垺鍋ら柕濞у啫鐏婇悗鍏夊亾闁告洦鍓欓崜鍫曟⒑缁嬭法绠抽柍宄扮墕閳绘挻绂掔€ｎ偆鍘遍梺闈涱槶閸ㄦ椽寮惰ぐ鎺撶厱閻庯綆鍋呭畷灞句繆椤愩垹鏆欓柍钘夘槸閳诲骸螣娴兼瑩鐛滈梻鍌氬€烽懗鍓佸垝椤栫偞鍋嬫俊銈呭暙閸ㄦ繄鈧厜鍋撻柛鏇炵仛閺呪晠妫呴銏″缂佸鍨块幏鎴︽偄閸忚偐鍘繝銏ｆ硾閻楀棝宕濆鍕╀簻闊洦娲栭弸鏃傜磼鏉堛劍宕岀€规洘甯掗～婵嬵敆娴ｈ桨澹曢梻鍌欑閹芥粍鎱ㄩ悽鎼炩偓鍐╃節閸屾粍娈鹃梺鑽ゅ枛閸嬪﹤顭囬埡鍛仯濡わ附瀵ч鐘炽亜閿旇娅嶆慨濠呮缁辨帒螣閼姐値妲梻浣侯焾閿曘劌鐣烽悽闈涘灊闁割偁鍎辩粈瀣亜閺嶃劎銆掗柛妯挎閳规垿鎮╃拠褍浼愰柣搴㈠嚬閸樻儳鈻庨姀銈呯煑濠㈣泛鐬奸惁鍫ユ⒑闁偛鑻晶瀛樸亜閵忊剝顥堢€规洜鍘ч埞鎴﹀醇閵忕姴绠戞繝纰夌磿閸嬫垿宕愰弽顓熷亱婵°倕鎳忛崑锟犳煏閸繃绌垮┑顔煎暱闇夐柣鎾虫捣閹界娀鏌ｉ幘璺烘灈闁哄苯绉靛顏堝箯鐏炶棄甯梻浣告贡閺咁偅绻涢埀顒勬煙椤旇崵鐭欐い銏＄☉閳藉鈻庤箛鎾存暘闂傚倷绀侀幖顐︽嚐椤栫偞鍤愭い鏍剱閺佸洤鈹戦崒婊庣劸妤犵偑鍨烘穱濠囶敍濠婂啫浠橀柣銏╁灠闁帮絽顫忓ú顏呭殥闁靛牆鎲涢敍鍕＜闁逞屽墯閹峰懐鎲撮崟顒傚娇濠电偛顕慨鎾敄閸℃稑纾绘い蹇撶墛閻撴洟鏌嶉埡浣告灓闁绘帟濮ら妵鍕煛閸曨偆姣㈤梺閫炲苯澧柛妯荤矒瀹曟垿骞樼紒妯煎帗閻熸粍绮撳畷婊冾潩闊祴鍋撻崘顔嘉ㄩ柍杞拌兌閸婄偤姊虹€圭媭娼愰柛搴ゆ珪缁傚秵銈ｉ崘鈹炬嫽闂佸壊鍋嗛崰鎾诲煀閺囩喆浜滈柕澹啠鏋呴梺鍝勭焿缂嶄礁顕ｆ禒瀣╃憸蹇涙偂婢舵劖鈷?, null);

                case TL.Account_ResetPasswordRequestedWait wait:
                {
                    var untilUtc = ToUtcDateTimeOffset(wait.until_date);
                    return (true, $"闂傚倷娴囬褎顨ョ粙鍖¤€块梺顒€绉埀顒婄畵瀹曠厧鈹戦幇顒侇吙闂備浇顕栭崹鐢稿春閸ャ劎顩查柣鐔稿櫞瑜版帗鏅查柛娑卞枦绾偓婵＄偑鍊愰弲婊堟偂閿熺姴钃熼柡鍥ュ灩闁卞洦绻濋崹顐㈠缁楁垶绻濋悽闈涗粶闁瑰啿鐭傚畷褰掓嚒閵堝棭娼熼梺瑙勫礃椤曆呯矆閸愵喗鐓欐い鏍ф鐎氼喗绂嶆ィ鍐┾拻濞达綀濮ら妴鍐煠閸愯尙鍩ｇ€殿噮鍋婇獮鏍ㄦ媴閸濄儺鍞甸梻浣虹帛椤牓顢氳缁牓宕熼娑氬帾闂佸壊鍋呯换鍐啅濠靛洢浜滈柨鏃囶嚙濞呭秹鏌″畝瀣М闁诡喒鏅犲畷锝嗗緞婵犲啰浜峰┑锛勫亼閸婃垿宕曢弻銉ョ闁搞儺鍓欑粻鏍ㄧ箾瀹割喕绨荤紒鐙呯稻缁绘繈宕归銏狀潓濠电偛鐗滈崑濠傤潖濞差亜宸濆┑鐘插婵洤鈹戦悩顔肩仾闁挎岸鏌?{untilUtc:yyyy-MM-dd HH:mm:ss} UTC 闂傚倸鍊搁崐椋庣矆娓氣偓楠炲鏁撻悩鍐叉疄闂佺粯鍔﹂崜锕€鈻撴禒瀣厵闂傚倸顕ˇ锕傛煟閵堝骸娅嶉柡灞界Х椤т線鏌涢幘璺烘灈妤犵偛鍟灃闁告侗鍠栨禒顓㈡⒑缂佹﹩娈旈柣妤€锕獮蹇涙焼瀹ュ棌鎷洪梺纭呭亹閸嬫稒淇婇悾灞稿亾閸忓浜鹃梺褰掓？缁€渚€鎷戦悢鍏煎€堕柣鎰暩閹藉倿鏌涘顒夊剶闁哄本鐩俊鐑藉閳╁啰褰嗛梻?闂傚倸鍊搁崐鎼佸磹閻戣姤鍊块柨鏇氶檷娴滃綊鏌涢幇鍏哥敖闁活厽鎹囬弻锝夊閵忊晝鍔搁梺钘夊暟閸犲酣鍩為幋锔藉亹闁圭粯甯╂禒鐐節濞堝灝鐏犻柕鍫熸倐瀵鈽夐姀鐘殿啋濠德板€愰崑鎾绘煙閸愯尙绠婚柟顔筋殜椤㈡﹢鎮㈤搹璇″晭闂佽瀛╃粙鎺椻€﹂崶顒佸剹濠电姴娲﹂悡鐔搞亜閹哄棗浜鹃梺鍛婅壘椤戝濡撮崘顔嘉ㄩ柍杞拌兌閸婄偤姊虹€圭媭娼愰柛搴ゆ珪缁傚秵銈ｉ崘鈹炬嫽闂佸壊鍋嗛崰鎾诲煀閺囩喆浜滈柕澹啠鏋呴梺?, untilUtc);
                }

                case TL.Account_ResetPasswordFailedWait failed:
                {
                    var retryUtc = ToUtcDateTimeOffset(failed.retry_date);
                    return (false, $"闂傚倸鍊风粈渚€骞栭位鍥敃閿曗偓閻ょ偓绻濇繝鍌滃闁搞劌鍊块幃褰掓惞閻熸壆娈ら梺鍏兼緲濞硷繝寮婚妸鈺傚亜闁告繂瀚呴姀銈嗗€垫慨姗嗗墻濡插憡銇勯鈩冪《闁圭懓瀚板畷顐﹀礋椤撶啘鐐寸節閻㈤潧浠ч柛妯犲洦鍋夊┑鍌滎焾閽冪喐绻涢幋鐐冩岸寮告惔銊︾厵闂侇叏绠戦弸娑欍亜椤愬骸鎳愮壕钘壝归敐鍛棌婵″弶妞介弻娑滅疀閺冩捁鈧寧顨ラ悙鍙夘棦鐎规洘锕㈤、娆撴嚃閳哄﹥效濠碉紕鍋戦崐鏍偋濡ゅ啫鏋堢€广儱顦介弫鍌炴煟閺冨洦顏犵痪鍙ョ矙閺屾稓浠﹂幑鎰棟闂佸搫顑冮崐婵嬪蓟濞戙垺鍋愰柛鎰絻閹界敻鎮楀▓鍨珮闁稿锕獮鍐╃鐎ｎ亜鐎銈嗗姂閸ㄦ槒銇愰幘顔解拻濞达絽鎲￠崯鐐烘煕椤垵鐏︾€规洘鍔曡灃闁告劦浜為悞鍏肩節閵忥絽鐓愰柛鏃€鐗犻崺娑㈠箳濡や胶鍘遍梺闈涱樈閸ㄦ娊宕氶弶妫电懓顭ㄩ崘銊㈡寖缂備浇椴搁幐濠氬箯閸涙潙鎹舵い鎾寸⊕椤秵淇婇悙顏勨偓褏鈧潧鐭傚畷褰掑礃濞村鐎婚梺闈涚箞閸婃牠宕戦崟顖涚厱闊洦娲栫敮鑸电箾?{retryUtc:yyyy-MM-dd HH:mm:ss} UTC 闂傚倸鍊搁崐椋庣矆娓氣偓楠炲鏁撻悩鍐叉疄闂佸憡鎸嗛埀顒勫磻閹炬枼妲堥柟鐑樻尰閻濇洟姊洪崫鍕櫣闁绘牕銈搁獮鍐煛閸愵亞锛滃┑鈽嗗灣閸庛倝鎮鹃柆宥嗏拻濞达綀妫勮闂佹寧纰嶉妵鍕晜鐠囪尙浠搁悗娈垮枟瑜板啴鍩㈡惔銊ョ疀妞ゆ梻铏庨崯搴ㄦ⒒娴ｈ櫣銆婃俊鎻掓嚇瀹曘垽宕滆椤ユ岸鏌涜椤ㄥ棝鎮″☉妯忓綊鏁愰崶褍濡洪梺鍝勬閺呯娀寮?, retryUtc);
                }

                default:
                    return (false, $"闂傚倸鍊搁崐椋庣矆娓氣偓楠炴牠顢曢敂钘変罕濠电姴锕ょ€氼噣銆呴弻銉︾厽闁归偊鍠栭崝瀣煕鐎ｎ亜鈧潡寮诲澶婄厸濞达絽鎲″▓鍙夌箾鐎涙ê娈犻柛濠冪箞瀵鏁撻悩鑼紲濠电偞鍨堕敃鈺呭吹鐎ｎ剛纾藉ù锝嗗絻娴滈箖姊洪柅鐐茶嫰婢ф壆绱掓潏銊﹀鞍缂佹鍠栧畷鎯邦槺闁告垼妫勯—鍐Χ閸℃鍋侀梺鎼炲劘閸斿矂鍩€椤掑倹鏆柡宀嬬秮楠炲鏁愰崨鍛崌閺岋繝宕担绋款潽闂侀€涚┒閸斿矂鍩為幋锕€閱囬柨婵嗘噹缁犳吂sult.GetType().Name}", null);
            }
        }
        catch (Exception ex)
        {
            var (summary, details) = MapTelegramException(ex);
            var msg = string.IsNullOrWhiteSpace(details) ? summary : $"{summary}闂傚倸鍊搁崐鐑芥倿閿旈敮鍋撶粭娑樻噽閻瑩鏌熼悜姗嗘闁轰礁妫涚槐鎺楀棘閸喗缍巘ails}";
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
    /// 闂傚倸鍊搁崐椋庣矆娓氣偓瀹曘儳鈧綆鍠栫壕鍧楁煙閹増顥夐幖鏉戯躬閺屻倝鎳濋幍顔肩墯婵炲瓨绮岀紞濠囧蓟濞戙垹唯闁挎繂鎳庨‖瀣⒑瀹曞洨甯涙俊顐㈠暣瀵寮撮姀鐘诲敹濠电娀娼уù鍕閻愮儤鈷戦柛娑橈攻閻撱儵鎮楀鐓庡⒉闁诲繐鍟村铏圭磼濮楀棛鍔搁柣蹇撶箲閻熲晠骞冮悙鐑樺€婚柤鎭掑劤閸樹粙姊洪幐搴㈢５闁稿鎸婚妵鍕晜閻愵剚姣堥梺缁樹緱閸犳牠顢橀崗鐓庣窞濠电姴瀚弳浼存煟閻斿摜鐭婃繛鎾棑閸掓帡宕奸妷銉у姦濡炪倖甯掔€氼參鍩涢幒鎴欌偓鎺戭潩椤掍焦鎮欓悗娈垮櫍缁犳牠寮诲☉銏″亹闁告劖褰冮幗闈涱渻閵堝啫鐏柣鐔叉櫅閻ｉ攱绺界粙璇俱劑鏌ㄩ弴妤€浜炬繛瀛樼矊缂嶅﹤顫忓ú顏呭亗閹兼番鍨洪崰鎰渻閵堝繒瀵肩紒顔界懃閻ｇ兘骞嬮敃鈧粻濠氭倵闂堟稒鎲搁柟铏箖缁绘繈鎮介棃娴躲垽鎮楀鐓庢珝閽樻繈鏌嶉崫鍕櫤闁抽攱鍨块弻娑樷攽閸℃浠煎銈呯箲閹告娊寮诲☉銏犖╅柕鍫濇噹缁侇喖螖閻橀潧浠滄俊顐ｇ懇楠炲繘宕ㄩ弶鎴狀槶闂佸湱绮敮妤€鈻嶆繝鍥ㄢ拻濞达綀妫勬禍褰掓煃瀹勬壆澧︾€规洘绮岄埥澶愬閻樻鍞甸梻渚€娼ц墝闁哄懏绮撳畷鎰版偨閸涘﹤浠┑鐐叉缁绘劙顢旈埡鍛厵闁稿繐鍚嬮崐鎰版煛鐏炵晫啸妞ぱ傜窔閺屾稖绠涢弮鍌涘垱闂佽鍠氶崑銈夊极閸愵喖鐒垫い鎺嗗亾鐎规挸瀚板楦裤亹閹烘搫绱电紓浣插亾濞达絿鍎ら崰鍡涙煥閺囩偛鈧綊鎮￠妷鈺傜厸闁搞儺鐓堝▓鏂棵瑰鍫㈢暫婵﹨娅ｉ埀顒傛闂勫嫬顭垮Ο缁樺弿闁稿本澹曢崑鎾舵喆閸曨剛顦梺绋跨箲钃卞ǎ鍥э躬楠炴牗鎷呴懖婵勫妿閹叉瓕绠涘☉杈ㄦ櫅闂侀€炲苯澧扮紒杈ㄦ尰閹峰懏銈﹂幐搴偓妤呮⒑閸涘﹦鎳冮梺甯到閻ｇ兘寮舵惔鎾搭潔闂侀潧绻嗛弲婊堝疾閿濆棛绡€闁汇垽娼ф牎缂佺偓婢樼粔鐟扮暦閺囩儐鍚嬪璺侯儑閸橀亶姊洪崫鍕偍闁告柨鐭傞幃姗€鎮╃憗浣哥秺閹晠鎳犻鍌ゅ敽缂傚倷娴囨ご鍝ユ暜閿熺姴绠栭柍鍝勬噹缁€鍐煕濞嗗浚妲规慨锝呮捣缁辨捇宕掑▎鎰偘婵＄偛顕…鍫ユ偩濠靛宸濆┑鐘辫兌缁?    /// </summary>
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
            var msg = string.IsNullOrWhiteSpace(details) ? summary : $"{summary}闂傚倸鍊搁崐鐑芥倿閿旈敮鍋撶粭娑樻噽閻瑩鏌熼悜姗嗘闁轰礁妫涚槐鎺楀棘閸喗缍巘ails}";
            return (false, msg, false, false, null);
        }
    }

    /// <summary>
    /// 缂傚倸鍊搁崐鎼佸磹閹间礁纾归柣鎴ｅГ閸婂潡鏌ㄩ弴鐐测偓鎼佸垂閸岀偞鐓熼柕蹇曞У閸熺偤鏌?闂傚倸鍊搁崐鐑芥嚄閸洖绠犻柟鎯у娑撳秶鈧箍鍎遍ˇ顖炲垂閸岀偞鐓犲┑顔藉姇閳ь剚娲熷畷鐟扳攽鐎ｎ偆鍙嗛梺鍝勫暙閸婄懓鈻嶉弴銏＄厓缂備焦蓱椤ュ牓鏌＄仦鐐鐎规洜鍘ч埞鎴﹀炊妞嬪海顦梻鍌欑閹诧繝寮婚妸褉鍋撳鐓庡⒉闁诲繐鍟村铏圭磼濮楀棛鍔搁柣蹇撶箲閻熲晠骞冮悙鐑樺€婚柤鎭掑劤閸樹粙姊洪幐搴㈢５闁稿鎸婚妵鍕晜閻愵剚姣堥梺缁樹緱閸犳牠顢橀崗鐓庣窞濠电姴瀚弳浼存煟閻斿摜鐭婃繛鎾棑閸掓帡宕奸妷銉у姦濡炪倖甯掔€氼參鍩涢幒鎴欌偓鎺戭潩椤掍焦鎮欓悗娈垮櫍缁犳牠寮诲☉銏″亹闁告劖褰冮幗闈涱渻閵堝啫鐏柣鐔叉櫅閻ｉ攱绺界粙璇俱劑鏌ㄩ弮鍥撻柣婵愬亰濮婄粯鎷呯粙娆炬闂佺顑呴幊鎰板礆閹烘挻鍎熼柨婵嗘川楠炴挸鈹戞幊閸婃洟骞婅箛娑欏亗闁哄洨鍠撶粻楣冩煕閳╁厾顏呮叏閸ヮ剚鐓熼煫鍥ㄦ⒐鐏忥箓鏌＄仦鐣屝ユい褌绶氶弻娑滅疀閺冨倶鈧帗绻涢崱鎰仼妞ゎ偅绻堥幊婊堟濞戞閽靛┑鐘垫暩閸嬫稑螞濞嗘挸鍨傞柛鎾茶閸嬫挸顫濋鈥愁棟闂傚洤顦甸弻銊モ攽閸℃瑥顤€濡炪倕绻掓繛鈧柡灞剧洴婵℃悂鏁冮埀顒勫煝閺囥垺鐓涢悘鐐插⒔濞插瓨顨ラ悙鏉戠瑨妞ゆ挸銈稿畷銊╊敇閻橆喖浠忛梻鍌氬€烽懗鍫曗€﹂崼銉晞闁糕剝绋掗崑瀣節婵犲倹鍣归柛銊︾箓閳规垿鎮╃€圭姴顥濈紓浣哄Х閸犳牠骞冨Δ鍛櫜閹肩补鈧剚娼炬俊鐐€х€靛矂宕抽敐鍜佹綎濠电姵鑹剧壕鍏兼叏濮楀棗骞樻い锕備憾濮婅櫣绮欏▎鎯у壉闂佸湱鎳撳ú顓㈢嵁閸愵喖鎹舵い鎾寸☉娴滈箖鏌ㄥ┑鍡欏嚬缂併劌顭烽弻锟犲幢濞嗗繑鐏堥梺?ConfirmTwoFactorRecoveryEmailAsync 缂傚倸鍊搁崐鐑芥嚄閸洘鎯為幖娣妼閻骞栧ǎ顒€濡肩紒鎰殕缁绘盯骞嬪▎蹇曞姶闂佽桨绀佸ú銈夊煘閹达附鍋愮€规洖娴傞弳锟犳⒑閸濆嫭濯奸柛鎾寸懇閳ワ妇鎹勯妸锕€纾梺鎯х箳閹虫捇銆傞悽鍛娾拺?    /// </summary>
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
                return (false, "闂傚倸鍊搁崐鎼佸磹妞嬪孩顐介柨鐔哄Т閻骞栧ǎ顒€濡肩紒鎰殕閹便劌顫滈埀顒劼烽崒鐐叉瀬濠电姴娲﹂悡鏇熴亜閹扳晛鈧洟寮搁弮鍫熺厓缂備焦蓱閳锋帡鏌嶈閸撴瑧绮诲澶婄？闂侇剙绉甸崑瀣煕閳╁啰鈽夐柛灞诲姂閺屸剝寰勭€ｎ亞浜跺┑鈩冨絻閻楁捇寮婚悢鐓庣闁规澘鐏氱紞鍫濐渻閵堝懐绠冲┑鐐╁亾闂?, null);

            try
            {
                _ = new MailAddress(email);
            }
            catch
            {
                return (false, "闂傚倸鍊搁崐鎼佸磹妞嬪孩顐介柨鐔哄Т閻骞栧ǎ顒€濡肩紒鎰殕閹便劌顫滈埀顒劼烽崒鐐叉瀬濠电姴娲﹂悡鏇熴亜閹邦喖孝闁告梹宀搁弻娑㈠箛閵娿儰澹曢梻鍌氬€烽懗鍓佸垝椤栫偛绠扮紒瀣紩鐟欏嫷妲归幖娣灩閸樺綊姊洪柅鐐茶嫰婢ь垳绱掔紒妯兼创妤犵偛顑夐幃娆撳级閹寸媭鏆＄紓鍌氬€风粈渚€藝闁秴鏋佸┑鐘冲搸閳ь兛绀佽灃闁逞屽墴閳ワ箓濡搁埡浣哥獩濡炪倖姊婚崢褔鐛?, null);
            }

            currentPassword = (currentPassword ?? string.Empty).Trim();

            var client = await GetOrCreateConnectedClientAsync(accountId, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            var pwd = await client.Account_GetPassword();
            cancellationToken.ThrowIfCancellationRequested();

            if (pwd.current_algo == null)
                return (false, "闂傚倸鍊峰ù鍥х暦閸偅鍙忛柡澶嬪殮濞差亜顫呴柕鍫濇噽閿涚喖鏌ｆ惔銏⑩姇闁挎氨绱掗崜浣镐槐闁哄本鐩鎾Ω閵壯傚摋闂備焦妞块崢浠嬨€冩繝鍌ゆ綎婵炲樊浜滈崹鍌涖亜閺囩偞鍣归柛鎾讳憾閹鈻撻崹顔界亶闂佽鎮傜粻鏍春閳ь剚銇勯幒宥囪窗闁哥喎绻橀弻娑㈡偐閸愬弶璇為悗瑙勬礃閸ㄥ潡鐛Ο鑲╃＜婵☆垳鍘ч獮鍫熺節濞堝灝鏋熸い顓炴喘瀹曘垼顦归柟顔斤耿閹垽宕楅悡搴濆摋婵犳鍠楅敃鈺呭礈濞嗘挻鍤嬮悷娆忓娴滄粍銇勯幇顔夹㈤柣蹇斿絻閳规垿鏁嶉崟顐㈠箣闂佺硶鏅换婵嗙暦濮椻偓婵℃瓕顦撮柛姗嗗墴濮婄粯鎷呴挊澶婃優婵犳鍠楀娆戝弲闂佹寧绻傚ú锕傤敃娴犲鐓忛柛顐ｇ箖婢跺嫭銇勯锝嗙缂佺粯绻堝Λ鍐ㄢ槈閸楃偛澹堢紓鍌欑贰閸嬪嫮绮旇ぐ鎺戣摕闁挎繂鎲橀悢鍏兼優闁谎冩憸缁€濠囨⒒娓氣偓濞佳兾涘Δ鍛櫇闁挎梻鍋撻～鏇㈡煙閹呮憼濠殿垱鎸抽弻锝夋偄缁嬫妫嗘繛瀵稿У閹稿啿顫忛悜妯侯嚤婵炲棙鍨硅ⅵ缂傚倷鑳舵慨鐢告偋閻樼粯鍋樻い鏃傗拡閸氬顭跨捄鐚村姛闁伙綁绠栭幃宄邦煥閸愨晜宕崇紓渚囧枛閻楁捇宕洪埀顒併亜閹烘垵顏柍閿嬪笒閵嗘帒顫濋浣规倷閻庢鍣ｇ粻鏍蓟濞戙垺鍋愰柛鎰絻閹介潧顪冮妶鍐ㄧ仾闁荤啿鏅涢悾閿嬬附缁嬭銊╂煥閺冨洤袚闁绘繍鍋婇弻锝嗘償閳ュ啿杈呴梺绋款儐閹瑰洭寮诲☉銏犵疀妞ゆ挾鍋涙慨銏犫攽閻愯尙澧涢柣鎾偓鎰佹綎闁惧繐婀遍惌娆撴偣閹帒濡挎い鏂挎喘濮婅櫣绱掑鍡樼暦闂佸憡姊归悷鈺呭Υ娴ｇ硶鏋庨柟鎯у暱瀹撳棝姊虹紒姗嗙劷闁轰焦鎮傞弫宥夋倷瀹割喗瀵岄梺闈涚墕濡瑩藟閸℃瑢鍋撶憴鍕闁轰礁顭烽獮鍡涘磼濮樿鲸娈曢梺閫炲苯澧版俊鍙夊姍楠炴帒螖娴ｉ晲鏉繝鐢靛仜濡鎹㈤幇鏉挎辈闁绘棃鏅茬换鍡涙煕濞嗗浚妲圭紒鈧€ｎ喗鐓?, null);

            if (string.IsNullOrWhiteSpace(currentPassword))
                return (false, "闂傚倸鍊峰ù鍥х暦閸偅鍙忛柡澶嬪殮濞差亜围濠㈣泛锕ょ花銉╂⒑閸濆嫯顫﹂柛搴㈢叀瀵煡寮婚妷锔惧幗闂佸綊鍋婇崜姘跺箚閸垻纾奸柍褜鍓熷畷姗€鍩￠崘顏嶅晭闂備胶纭堕崜婵嬨€冮崼銏笉濞寸厧鐡ㄩ悡娑㈡煕鐏炶棄鏆欓柛姗€绠栭幃鐑芥焽閿旀儳寮抽梻浣告啞閸旓附绂嶉悙鏉戭嚤闁搞儯鍔嬬换鍡涙煏閸繃鎼愮€涙繂鈹戦悙鑼勾闁告梹鍨垮畷娲焵椤掍降浜滈柟鐑樺灥椤忣亝绻涢幊宄板缁诲棙銇勯幇鍓佹偧婵炲弶鎸抽弻?, null);

            var oldCheck = await WTelegram.Client.InputCheckPassword(pwd, currentPassword);

            var settings = new TL.Account_PasswordInputSettings
            {
                flags = TL.Account_PasswordInputSettings.Flags.has_email,
                email = email
            };

            await client.Account_UpdatePasswordSettings(oldCheck, settings);

            // 闂傚倸鍊搁崐椋庣矆娓氣偓楠炴牠顢曢埛姘そ婵¤埖寰勭€ｎ亙妲愰梻渚€娼ц墝闁哄懏鐩幏鎴︽偄鐏忎焦鏂€闂佺粯锚瀵爼骞栭幇鐗堝仭婵炲棙鐟ч悾鍨殽閻愬澧遍柛鎺撳浮瀹曟寰勬繝浣割棜闂佽楠稿﹢杈ㄥ垔椤撱垹鑸圭憸鐗堝笚閳锋帡鏌涚仦鍓ф噮閻犳劒鍗抽弻娑氣偓锝庝簼閸ｈ櫣绱掑畝鍕喚闁圭厧婀遍幉鎾礋椤愩垻鍝?getPassword 闂傚倸鍊搁崐椋庣矆娓氣偓瀹曘儳鈧綆鍠栫壕鍧楁煙閹増顥夐幖鏉戯躬閺屻倝鎳濋幍顔肩墯婵炲瓨绮岀紞濠囧蓟濞戙垹唯妞ゆ梻鍘ч～鈺呮⒑鐠囪尙绠烘繛鍛礈閹广垹鈹戦崶鈺冪槇闂佺鏈喊宥呪枔閸撲胶纾藉ù锝囨嚀婵呯磼鏉堛劍绀嬮柟顖楀亾濡炪倕绻愰悧婊堝极閸モ晜鍠愮€广儱顦崙鐘绘煥閺傛娼熷ù婊勭矒閺岀喖鎮欓鈧晶顖炴煟閹剧偨鍋㈤柡宀嬬磿娴狅箓鎳為妷銉︾亷婵＄偑鍊戦崹娲偡瑜忓Σ鎰板箳濡も偓椤懘鏌ｅΟ鎸庣彧婵絽鐗撳濠氬磼濞嗘帒鍘￠梺绋款儍閸旀垵鐣烽弴銏″殤妞ゆ帊绀佹惔濠囨⒑閸涘﹤濮﹀ù婊勭箞瀵啿顭ㄩ崟顓狀啎閻庣懓澹婇崰鏇犺姳婵傚憡鐓曢幖绮规寣椤忓牊绠掗梻浣瑰缁诲倿鎮ф繝鍥ㄥ殘闁革富鍘剧壕濂告偣閸パ冪骇妞ゃ儯鍨介弻?            var after = await client.Account_GetPassword();
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
            var msg = string.IsNullOrWhiteSpace(details) ? summary : $"{summary}闂傚倸鍊搁崐鐑芥倿閿旈敮鍋撶粭娑樻噽閻瑩鏌熼悜姗嗘闁轰礁妫涚槐鎺楀棘閸喗缍巘ails}";
            return (false, msg, null);
        }
    }

    /// <summary>
    /// 缂傚倸鍊搁崐鐑芥嚄閸洘鎯為幖娣妼閻骞栧ǎ顒€濡肩紒鎰殕缁绘盯骞嬪▎蹇曞姶闂佽桨绀佸ú銊ф閹惧瓨濯撮悹鍥风磿閸旂兘姊哄畷鍥╁笡婵☆偄鍟村濠氬即閵忕娀鍞跺┑鐘绘涧濞村嫮妲愰悙鐑樷拺闁告稑锕ラ悡銉╂倵濮樼厧澧查柣蹇撳暣濮婅櫣绱掑鍡欏姼闁诲繐绻戦悷鈺呭箖閻愮儤鍊婚柤鎭掑劤閸樹粙姊洪幐搴㈢５闁稿鎸婚妵鍕晜閻愵剚姣堥梺缁樹緱閸犳牠顢橀崗鐓庣窞濠电姴瀚弳浼存煟閻斿摜鐭婃繛鎾棑閸掓帡宕奸妷銉у姦濡炪倖甯掔€氼參鍩涢幒鎴欌偓鎺戭潩椤掍焦鎮欓悗娈垮櫍缁犳牠寮诲☉銏″亹闁告劖褰冮幗闈涱渻閵堝啫鐏柣鐔叉櫅閻ｉ攱绺介崨濠備簽婵炶揪缍€椤曟牠宕澶嬬厽閹肩补鈧啿杈呴梺绋款儐閹瑰洭寮诲☉銏犵疀闁稿繐鎽滈崙鐟邦渻閵堝骸浜滅紒澶屾嚀椤繘宕崝鍊熸閹风娀宕ｆ径灞肩敖闂傚倸鍊搁崐鍝モ偓姘煎墰缁棁銇愰幒鎴炴К闂?    /// </summary>
    public async Task<(bool Success, string? Error)> ConfirmTwoFactorRecoveryEmailAsync(
        int accountId,
        string code,
        CancellationToken cancellationToken = default)
    {
        try
        {
            code = (code ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(code))
                return (false, "婵犵數濮撮惀澶愬级鎼存挸浜炬俊銈勭劍閸欏繘鏌熺紒銏犳灍闁稿孩顨呴妴鎺戭潩閿濆懍澹曢梻浣筋嚃閸垶鎮為敃鈧銉╁礋椤栨氨鐤€濡炪倖鎸鹃崑娑欑珶閹炬枼鏀介柣鎰煐瑜把呯磽瀹ュ嫮绐旈挊鐔兼煙閸撗呭笡闁稿鍓濈换婵嬫濞戝崬鍓遍梺缁樻尵婵炩偓闁哄睙鍡欑杸闁挎繂鎳嶇花濠氭⒑鐠囪尙绠伴柨鏇ㄤ邯楠炲啳銇愰幒鎴犵杸濡炪倖鏌ㄩ崥瀣倶娴ｈ櫣纾?);

            var client = await GetOrCreateConnectedClientAsync(accountId, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            _ = await client.Account_ConfirmPasswordEmail(code);
            return (true, null);
        }
        catch (Exception ex)
        {
            var (summary, details) = MapTelegramException(ex);
            var msg = string.IsNullOrWhiteSpace(details) ? summary : $"{summary}闂傚倸鍊搁崐鐑芥倿閿旈敮鍋撶粭娑樻噽閻瑩鏌熼悜姗嗘闁轰礁妫涚槐鎺楀棘閸喗缍巘ails}";
            return (false, msg);
        }
    }

    /// <summary>
    /// 闂傚倸鍊搁崐鎼佸磹閻戣姤鍊块柨鏇氶檷娴滃綊鏌涢幇鍏哥敖闁活厽鎸鹃惀顏堝箚瑜滈崕宥吤瑰鍐Ш闁哄本绋撴禒锕傚礈瑜嬮埀顒佸浮閺屽秶绱掑Ο娲绘闂佸搫鏈ú鐔风暦閻撳簶鏀介柛銉╊棑缁€濠囨⒒娴ｅ憡鍟為柡灞诲妿閳ь剚鍑归崜婵嬫倶閸愵喗鈷戠紓浣癸供閻掗箖鎮樿箛鏃傛噰闁诡喚鍋ら幃婊堟嚍閵壯冨箺闂備焦瀵уú宥夊磻閹炬番浜滈柨鏃傚亾閺嗩剟鏌ｅ☉鍗炴灈妞ゆ挸鍚嬪鍕節閸曨厽姣庨梺鑽ゅ枑缁苯霉妞嬪骸鍨濋柛顐犲劚閻掑灚銇勯幒鎴濐仾闁抽攱甯掗妴鎺戭潩椤掍焦鎮欓悗娈垮櫍缁犳牠寮诲☉銏″亹闁告劖褰冮幗闈涱渻閵堝啫鐏柣鐔叉櫅閻ｉ攱绺介崨濠備簽婵炶揪缍€椤曟牠宕澶嬬厽閹肩补鈧啿杈呴梺绋款儐閹瑰洭寮诲☉銏犵疀闁稿繐鎽滈崙鐟邦渻閵堝骸浜滅紒澶屾嚀椤繘宕崝鍊熸閹风娀宕ｆ径灞肩敖缂傚倸鍊峰ù鍥╃礄娴兼潙纾规俊銈傚亾閸楅亶鏌熼柇锕€鍘撮柡瀣叄閺岀喖鎮欓浣稿壒濠殿喖锕ゅ鈥愁潖濞差亝鐒婚柣鎰蔼鐎氭澘顭胯閸楁娊寮婚悢鍏煎殤妞ゆ巻鍋撴い锝呫偢閺岀喎鐣￠悧鍫濇缂備緡鍠楅悷鈺佺暦瑜版帩鏁婇柤娴嬫櫈閸╃偤姊婚崒娆愮グ鐎规洖鐏氶幈銊ョ暦閸モ晝鐒兼繛杈剧秬椤鈻嶉悩瑁佸綊鎮╁顔煎壉闂佺顑愭禍顏堝蓟濞戙垹鐒洪柛鎰典簴婵洭姊虹紒妯虹濠殿喓鍊濇俊鐢稿礋椤栨氨鐫勯梺绋挎湰缁诲啴顢撳Δ鍛拺闁告縿鍎辨禒锕傛煙椤旂厧鈧灝顕ｆ繝姘╅柕澶堝灪椤秴鈹戦绛嬫當婵☆偅鐟ㄩ崐鎾⒒?    /// </summary>
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
                return (false, "闂傚倸鍊搁崐鎼佸磹閻戣姤鍊块柨鏇氶檷娴滃綊鏌涢幇鍏哥敖闁活厽鎸鹃惀顏堝箚瑜滈崕宥吤瑰鍐Ш闁哄本绋撴禒锕傚礈瑜夊Σ鍫㈢磽娴ｇ顣抽柛瀣ㄥ€曢锝夊醇閺囩偤鍞跺銈嗗姦濠⑩偓闁告垳绮欓幃?, null, null);

            var pwd = await client.Account_GetPassword();
            var pattern = pwd.flags.HasFlag(TL.Account_Password.Flags.has_email_unconfirmed_pattern)
                ? (pwd.email_unconfirmed_pattern ?? "").Trim()
                : null;

            if (string.IsNullOrWhiteSpace(pattern))
                pattern = null;

            // 闂?API 婵犵數濮烽弫鎼佸磻閻愬搫鍨傞柛顐ｆ礀缁犱即鏌涘┑鍕姢闁活厽鎸搁—鍐偓锝庝簻椤掋垻鈧娲橀悡锟犲蓟閻斿憡缍囬柛鎾楀懏娈搁梻浣虹帛閸旀洟鏁冮鍫濊摕闁挎繂顦粻娑欍亜閺嶃劋绶卞ù鐘櫊閹鎲撮崟顒傗敍缂傚倸绉崇欢姘剁嵁閸愵厹浜归柟鐑樺灩閸婄偤姊洪幐搴㈢；缂佲偓娓氣偓瀹曘垽鏁撻悩鏂ユ嫼缂傚倷鐒﹂…鍥ㄦ櫠椤掑倻纾兼い鏃囧Г鐏忣厽銇勯銏㈢闁靛洦鍔欓獮鎺楀箣濠靛牃鍋撻鐑嗘富闁靛牆妫欓埛鎺楁煃瀹勬壆澧︾€规洦鍓欑叅妞ゅ繐鎳夐幏娲⒑閸︻収鐒炬繛瀵稿厴閸┿儲寰勬繛鐐杸闂佺鏈粙鎺楀煕閹烘鐓曢柟鐑樻尭缁楁岸鏌熼娑欘棃濠殿喒鍋撻梺鐐藉劚瀵爼宕径鎰拻濞达絽鎲￠幆鍫ユ煕婵犲倹璐￠柍褜鍓涢悷鎶藉川椤栨粎鍔堕梻渚€鈧偛鑻晶瀛樻叏婵犲嫮甯涚紒妤冨枛瀹曟儼顦查悗娑崇秮濮婃椽宕妷銉愶絿鈧厜鍋撻柟闂撮檷閳ь兛绀侀埢搴ㄥ箻閹惰棄鏁归梻渚€娼чˇ顓㈠磿閹剁瓔鏁婂┑鐘叉处閳锋垿鏌涢幇顒€绾ч柟顖氱墦閺屾盯鎮㈢捄鍝勭ギ閻庤娲栫紞濠傜暦濮椻偓閹牓顢楅埀顒勬煀閿濆懐鏆﹂柛顐ｆ礃閺呮煡鏌涘☉鍗炲箰闁规灚鍊曢埞?            return (true, null, pattern, null);
        }
        catch (Exception ex)
        {
            var (summary, details) = MapTelegramException(ex);
            var msg = string.IsNullOrWhiteSpace(details) ? summary : $"{summary}闂傚倸鍊搁崐鐑芥倿閿旈敮鍋撶粭娑樻噽閻瑩鏌熼悜姗嗘闁轰礁妫涚槐鎺楀棘閸喗缍巘ails}";
            return (false, msg, null, null);
        }
    }

    /// <summary>
    /// 闂傚倸鍊搁崐椋庣矆娓氣偓楠炲鏁撻悩鍐蹭画闂佹寧娲栭崐褰掑磻鐎ｎ偂绻嗛柕鍫濇噹閺嗙喖鏌ｉ鐔稿暗闁逞屽墰閹虫挾鈧矮鍗冲畷鎴炵節閸パ冩優濠德板€曢幊蹇涙偂閺囩喓绠鹃柟瀛樼箓閼稿綊鏌ｈ箛鏃傛噰闁哄苯绉烽¨渚€鏌涢幘鏉戝摵闁炽儻绠撳畷鍫曨敆閳ь剛绮婚鐐村€甸柨婵嗛閺嬫盯宕堕幘顔解拺闁硅偐鍋涢崝姗€鏌涢弬鍧楀弰鐎殿喗濞婇幃鈺冪磼濡攱瀚奸梻浣藉吹閸犳劖绔熼崱妯碱浄婵炴垯鍨洪悡鏇㈠箹缁厜鍋撻搹顐ｇ槪闂佺粯鎸堕崐婵嬪蓟閳ユ剚鍚嬮幖绮光偓宕囶啈闂備胶绮敮鐔告叏閵堝桅闁告洦鍨扮猾宥夋煕鐏炲墽绠栨い搴㈩殜濮婃椽宕妷銉ゆ埛濡炪値鍘奸悧鎰版倶閸愵喗鈷戠紓浣癸供閻掗箖鎮樿箛鏃傛噰闁诡喚鍋ら幃婊堟嚍閵壯冨箺闂備焦瀵уú宥夊磻閹炬番浜滈柨鏃傚亾閺嗩剚顨ラ悙鍙夘棞妞ゆ挸銈稿畷鍗炍熸笟顖楀亾閹惧墎纾奸柛鎾楀喚鏆梺鎸庤壘闇?    /// </summary>
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
            var msg = string.IsNullOrWhiteSpace(details) ? summary : $"{summary}闂傚倸鍊搁崐鐑芥倿閿旈敮鍋撶粭娑樻噽閻瑩鏌熼悜姗嗘闁轰礁妫涚槐鎺楀棘閸喗缍巘ails}";
            return (false, msg);
        }
    }

    /// <summary>
    /// 闂傚倸鍊搁崐椋庣矆娓氣偓瀹曘儳鈧綆鍠栫壕鍧楁煙閹増顥夐幖鏉戯躬閺屻倝鎳濋幍顔肩墯婵炲瓨绮岀紞濠囧蓟濞戙垹唯妞ゆ梻鍘ч～顏堟⒑缁嬪潡顎楅柣顓炲€块獮鍐亹閹烘垹鍊炲銈呯箰鐎氼喖袙閵忋垻纾藉ù锝呭濡插憡绻涚亸鏍ゅ亾閹颁焦缍庡┑鐐叉▕娴滃爼寮崶鈺傚枑鐎广儱顦崙鐘绘煏閸繃顥撳ù婊勭矒閺岋繝宕掑┑鍥┿€婃繝鈷€宥囩暤闁哄瞼鍠栭、娆戞喆閸曨剛褰嬮梻浣侯攰濞呮洜鍒掓惔銊ョ獥濠电姴浼ｉ悢鍛婂闁哄顑欏Λ婊勭節閻㈤潧浠﹂柟绋款煼閹虫繃銈ｉ崘鈺傛珖闂佹寧鏌ㄦ晶浠嬫儗閸℃稒鐓曢柡鍥ュ妼閳ь剙顑堥妵鎰板箳閹惧厖姹楅梻浣告啞閻熴儵藝椤栨埃鏋旂€光偓閳ь剛妲愰幘瀛樺闁惧繒鎳撶粭锟犳⒑閹肩偛濡奸柣蹇旂箞閹箖鎮滈挊澶愬敹闂佸搫娲ㄩ崰搴ㄥ焵椤掑倸鍘撮柡灞稿墲瀵板嫮鈧綆浜炴导鍥╃磽娴ｆ彃浜鹃柣搴秵閸犳鍩?Pattern闂傚倸鍊搁崐鐑芥倿閿旈敮鍋撶粭娑樻噽閻瑩鏌熺€电浠ч梻鍕閺岋繝宕橀妸銉㈠亾閼姐倗涓嶉柡鍌涱儥閻斿棝鎮规潪鎷岊劅闁稿孩鍔曢湁婵犙冪仢閳ь剚绻堝濠氭晸閻樿尙锛滃┑鐐村灦閿曗晠宕电€ｎ剛纾藉ù锝嗗絻娴滈箖姊洪柅鐐茶嫰婢у瓨鎱ㄦ繝鍌涙儓閻撱倝鎮规担鍝ワ紞闁轰焦鐗犲娲传閵夈儛銏ゆ⒑鐢喚绉鐐插暣瀹曠螖閳ь剛绮堢€ｎ偁浜滈柡宥冨妿閵嗘帡鏌涢弬鍨Щ妞ゎ亜鍟存俊鍫曞幢濡攱瀚介梻浣瑰劤缁绘劙鏌婇敐鍛殾妞ゆ帒瀚粻鐟懊归敐鍛础闁告瑥妫楅埞鎴︽倷閺夋垹浠搁梺鎸庢磵閺呮稑宓勯梺鍓插亝濞叉﹢鎮?    /// </summary>
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
            var msg = string.IsNullOrWhiteSpace(details) ? summary : $"{summary}闂傚倸鍊搁崐鐑芥倿閿旈敮鍋撶粭娑樻噽閻瑩鏌熼悜姗嗘闁轰礁妫涚槐鎺楀棘閸喗缍巘ails}";
            return (false, msg, false, null);
        }
    }

    /// <summary>
    /// 闂傚倸鍊搁崐椋庣矆娓氣偓楠炲鏁撻悩鍐蹭画闂佹寧娲栭崐鎼佸垂閸岀偞鐓曠憸搴ㄣ€冮崨瀛樺€块柛顭戝亖娴滄粓鏌熸潏鍓у埌闁告梻鏁婚弻娑滅疀閹惧瓨鎷辩紓浣虹帛閻╊垰鐣烽妸鈺婃晝闁靛繒濮磋闂傚倷绶氬褍煤閿曞倸绀夋繛鍡楁禋閸ゆ洟鏌熺紒銏犳灈妞ゎ偄鎳橀弻锝呂熸径绋挎儓闂佹眹鍎遍幊鎰閹捐纾兼繛鍡樺笒閸樷剝绻濋悽闈涗汗闁稿鎸荤换娑氣偓娑欘焽閻﹪鏌ｉ弽褋鍋㈡鐐插暣瀵埖鎯旈幘瀵糕偓濠氭⒑閻熼偊鍤熼柛瀣洴閹敻鎮ч崼銏㈩啎闁哄鐗嗘晶鐣岀矓椤掍胶绠鹃柛婊冨暟缁夘喚鈧娲栫紞濠囩嵁鎼淬劍鍤嶉柕澹啫姹插┑鐘垫暩閸嬬偤宕归崼鏇椻偓锕傚炊閵婏附鐝烽梺闈涚箞閸婃牠鎮″☉妯忓綊鏁愭径瀣敪闂侀€炲苯澧柣妤佺矌閸掓帞鈧綆浜堕崥瀣煕閳╁厾顏堝礉閸涱厸鏀介柍钘夋閻忕娀鏌涢姀鐘茬骇缂佸倸绉归幃娆擃敆閸屾粎妲囧┑鐘垫暩婵挳宕愯ぐ鎺戦棷闁绘垶菧娴滄粓鐓崶褜鍎忛柣蹇ｅ櫍閺岀喖宕ｆ径瀣偓鎰版煙椤斻劌娲ら柋鍥煟閺傛崘顒熸俊鎻掔秺濮婄粯鎷呮笟顖滃姼濡炪倖鍨靛Λ婵嬪箖瑜嶉…銊╁川椤撶偟浜板┑鐘垫暩婵敻鎳濋崜褎鍏滈柍褜鍓熷铏圭磼濡搫顫戦柣蹇撶箲閻熝呭垝閺冨牜鏁嬮柍褜鍓欓～?闂傚倸鍊峰ù鍥х暦閸偅鍙忕€规洖娲ㄩ惌鍡椕归敐鍫綈婵炲懐濮撮湁闁绘ê妯婇崕鎰版煕鐎ｅ吀閭柡灞剧洴閸╁嫰宕橀浣诡潔闂備浇顕х换妤€鈻嶉弴鐘冲床婵犻潧顑呴崹鍌涖亜閹板墎鍒伴柡鍡橆殘缁辨挻鎷呴幓鎺嶅闂備胶纭堕崜婵堢矙閹烘鍋?    /// 濠电姷鏁告慨鐑藉极閹间礁纾绘繛鎴旀嚍閸ヮ剦鏁囬柕蹇曞Х椤︻噣鎮楅獮鍨姎妞わ富鍨崇划鍫ュ醇濠㈡繂缍婇幃鈩冩償閿濆棙鍠栭梻浣告惈閹峰宕滃璺虹疄闁靛鍎欓悢鐓庣閹艰揪绲婚埀顒佸姍濮婃椽宕ㄦ繝鍐幗闂佺娴烽弫璇差嚕鐠囨祴妲堥柕蹇曞Х椤斿﹪姊洪悷閭﹀殶闁稿孩鍔曡灋闁靛濡囩弧鈧梺闈涢獜缁插墽娑垫ィ鍐╁€垫慨姗嗗幗缁舵煡鏌ｉ敐鍥у幋闁轰焦鍔欏畷銊╊敍濞戞瑧鏉洪梻鍌欑閹碱偊藝閹惰棄纾垮┑鐘冲嚬閺佸﹤顭块懜闈涘闁抽攱鍨块弻锝夊箻閾忣偅宕抽梺鍝勵儏閸燁偊鍩為幋锔绘晩闁告繂瀚ч崑鎾诲即閿涘嫪缃曢梻鍌欑窔濞佳囨偋閸℃稑绠犲鑸靛姇閸戠娀鏌″搴″箺闁绘挻鐩弻娑㈠Ψ閵忊剝鐝旀繛瀵稿缁犳挸螞閸涙惌鏁嗗璺猴工閳峰姊洪崫鍕缂佸顫夋穱濠囨倻閼恒儲娅嗛梺鍛婃寙閸涱厼绗岄梻鍌氬€风粈渚€骞夐敓鐘叉槬濠电姴瀚畷鍙夌節闂堟侗鍎忕紒鐘冲哺閺屾稓浠﹂崜褏鐓侀柣搴㈢瀹€鎼佸蓟閵堝洤鏋堥柛妤冨仜椤偊鏌涘Δ鈧换妯侯潖缂佹ɑ濯撮柧蹇曟嚀缁椻€愁渻閵堝骸骞栭柣妤佹尭閻ｅ嘲煤椤忓嫬鍞ㄥ銈嗘尵閸嬬喖宕濋敃鈧—鍐Χ閸℃浼囬梺绋块椤兘銆佸▎鎰窞闁归偊鍘鹃崢鎾绘偡濠婂嫮鐭掔€规洘绮岄～婵囨綇閵娿儱绨ラ梻渚€鈧偛鑻晶鎵磼鏉堛劌绗掗摶鏍煃瑜滈崜鐔肩嵁婵犲伣鐔哥瑹椤栨碍顓挎俊鐐€栫敮鎺斺偓姘煎墰閻ヮ亣顦崇紒缁樼洴瀹曞崬鈻庤箛搴☆棜婵犵數濮伴崹鍝勎涢崘顭戞綎闁惧繗顫夌€氼剟骞栫划鐟板⒉闁挎稑绉归弻娑㈠Χ閸℃瑦鍣紓浣介哺閹告悂顢樻總绋垮窛妞ゆ牕鎲為崶銊у幐闁诲繒鍋犳慨銈夊窗濡眹浜滈柕蹇ョ磿閹冲懐绱掓潏銊ユ诞濠殿喒鍋撻梺缁橆焾濞呮洖袙閸儲鈷掗柛灞剧懅閸斿秹鎮楃粭娑樻噽閻瑩鏌熸潏楣冩闁稿顑夐弻鐔煎礈瑜嶆禒娲煃瑜滈崜姘辨暜閳ュ磭鏆︽繝濠傚婵挳鏌ｉ悢鍝勵暭婵絽娲ら埞鎴︽倷閼搁潧娑х紓浣瑰絻濞尖€崇暦閺囥垹围濠㈣埖蓱閺呮粓姊洪棃娑氱疄闁搞劍妞藉畷浼村箛閻楀牏鍘藉┑掳鍊愰崑鎾绘煟濡も偓閿曪妇鍒掗崼銉ョ妞ゅ繐妫涢敍婊勭箾閹剧澹橀柨鏇樺€濋妴鍛搭敆閸屾粎锛滃銈嗘⒒閺咁偊骞婇崨瀛樼厓鐟滄粓宕滈妸褏绀婇柛鈩冪☉閸屻劍绻濇繝鍌滃闁告濞婇弻鏇＄疀閺囩倫銉╂煟閹炬潙鐏存慨濠冩そ閹兘鏌囬敃鈧▓鑸电節濞堝灝鏋ら柡浣割煼楠?setup闂傚倸鍊搁崐鐑芥倿閿旈敮鍋撶粭娑樻噽閻瑩鏌熷▎陇顕уú顓€佸鈧慨鈧柣姗€娼ф慨?    /// </summary>
    public async Task<(bool Success, string? Error, string? EmailPattern)> SetLoginEmailAsync(
        int accountId,
        string email,
        CancellationToken cancellationToken = default)
    {
        try
        {
            email = (email ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(email))
                return (false, "闂傚倸鍊搁崐鎼佸磹妞嬪孩顐介柨鐔哄Т閻骞栧ǎ顒€濡肩紒鎰殕閹便劌顫滈埀顒劼烽崒鐐叉瀬濠电姴娲﹂悡鏇熴亜閹扳晛鈧洟寮搁弮鍫熺厓缂備焦蓱閳锋帡鏌嶈閸撴瑧绮诲澶婄？闂侇剙绉甸崑瀣煕閳╁啰鈽夐柛灞诲姂閺屸剝寰勭€ｎ亞浜跺┑鈩冨絻閻楁捇寮婚悢鐓庣闁规澘鐏氱紞鍫濐渻閵堝懐绠冲┑鐐╁亾闂?, null);

            try
            {
                _ = new MailAddress(email);
            }
            catch
            {
                return (false, "闂傚倸鍊搁崐鎼佸磹妞嬪孩顐介柨鐔哄Т閻骞栧ǎ顒€濡肩紒鎰殕閹便劌顫滈埀顒劼烽崒鐐叉瀬濠电姴娲﹂悡鏇熴亜閹邦喖孝闁告梹宀搁弻娑㈠箛閵娿儰澹曢梻鍌氬€烽懗鍓佸垝椤栫偛绠扮紒瀣紩鐟欏嫷妲归幖娣灩閸樺綊姊洪柅鐐茶嫰婢ь垳绱掔紒妯兼创妤犵偛顑夐幃娆撳级閹寸媭鏆＄紓鍌氬€风粈渚€藝闁秴鏋佸┑鐘冲搸閳ь兛绀佽灃闁逞屽墴閳ワ箓濡搁埡浣哥獩濡炪倖姊婚崢褔鐛?, null);
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
            var msg = string.IsNullOrWhiteSpace(details) ? summary : $"{summary}闂傚倸鍊搁崐鐑芥倿閿旈敮鍋撶粭娑樻噽閻瑩鏌熼悜姗嗘闁轰礁妫涚槐鎺楀棘閸喗缍巘ails}";
            return (false, msg, null);
        }
    }

    /// <summary>
    /// 缂傚倸鍊搁崐鐑芥嚄閸洘鎯為幖娣妼閻骞栧ǎ顒€濡肩紒鎰殕缁绘盯骞嬪▎蹇曞姶闂佽桨绀佸ú銈夊煘閹达附鍋愮€规洖娴傞弳锟犳⒑缁嬪潡顎楅柣顓炲€块獮鍐亹閹烘垹鍊炲銈呯箰鐎氼喖袙閵忋垻纾藉ù锝呭濡插憡绻涚亸鏍ゅ亾閹颁焦缍庡┑鐐叉▕娴滃爼寮崶鈺傚枑鐎广儱顦崙鐘绘煏閸繃顥撳ù婊勭矒閺岋繝宕掑┑鍥┿€婃繝鈷€鍕姇闁靛洤瀚版俊鎼佹晲閸涱厼顫撻梻浣风串缁插墽鎹㈤崼婵堟殾婵せ鍋撴い銏℃瀹曨亝鎷呴崷顓犘梻鍌氬€搁崐鐑芥倿閿曗偓椤啴宕稿Δ鈧粣妤呮煛閸ャ儱鐏柍閿嬫⒒閳ь剙绠嶉崕閬嵥囬鐐村€?    /// </summary>
    public async Task<(bool Success, string? Error)> ConfirmLoginEmailAsync(
        int accountId,
        string code,
        CancellationToken cancellationToken = default)
    {
        try
        {
            code = (code ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(code))
                return (false, "闂傚倸鍊峰ù鍥х暦閸偅鍙忛柡澶嬪殮濞差亜围濠㈣泛锕ょ花銉╂⒑閸濆嫯顫﹂柛搴㈢叀瀵煡寮婚妷锔惧幗闂佸綊鍋婇崜姘跺箚閸垻纾奸柍褜鍓熷畷姗€鍩￠崘顏嶅晭闂備胶纭堕崜婵婃懌闁诲繐娴氶崢濂告箒闂佺粯顭堢亸娆戠不瀹曞洨纾奸弶鍫涘妼濞搭噣鏌熼瑙勬珖闁瑰嘲顑夐幖褰掝敃閵忥紕浜鹃梻鍌氬€烽懗鍓佸垝椤栨粎鐭欓柟鐑橆殕閸嬶紕鎲搁弮鍫㈠祦闊洦绋掗崑鎰版煣韫囷絽浜滄い搴㈡崌濮婃椽骞嗚缁犳娊鏌熼搹顐ｅ磳闁?);

            var client = await GetOrCreateConnectedClientAsync(accountId, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            _ = await client.Account_VerifyEmail(new EmailVerifyPurposeLoginChange(), new EmailVerificationCode { code = code });
            return (true, null);
        }
        catch (Exception ex)
        {
            var (summary, details) = MapTelegramException(ex);
            var msg = string.IsNullOrWhiteSpace(details) ? summary : $"{summary}闂傚倸鍊搁崐鐑芥倿閿旈敮鍋撶粭娑樻噽閻瑩鏌熼悜姗嗘闁轰礁妫涚槐鎺楀棘閸喗缍巘ails}";
            return (false, msg);
        }
    }

    /// <summary>
    /// 闂傚倸鍊搁崐椋庣矆娓氣偓楠炴牠顢曢埛姘そ婵¤埖寰勭€ｎ亙妲愰梻渚€娼ц墝闁哄懏鐩幏鎴︽偄鐏忎焦鏂€闂佺粯锚瀵爼宕冲畡鎳婄懓顭ㄩ崘顏喰ㄩ梺鍝勭灱閸犳牠鐛崱姘兼Щ闂佸搫妫滄ご鎼佸Φ閸曨垰围闁告侗鍠栧▓妤呮⒑鐠団€虫灈缂傚秴锕悰顕€宕堕鈧粈鍫澝归敐鍥у妺婵炲牊鎮傞弻锝夋偄閸濄儳鐓佸┑鐘灪閿氭い顓炴喘閺佹捇鎮╅懠顒傛毎闂備礁鎲￠崝锕傚窗閺嶎偆鐭嗛悗锝庡亖娴滄粓鏌熼崫鍕ユい鏂款樀閺岋紕鈧綆浜濋崳鐣岀磼?缂傚倸鍊搁崐鎼佸磹閻戣姤鍤勯柤绋跨仛閸欏繑銇勯幘鍗炵仼缁炬儳顭烽弻鐔煎箲閹邦啩婊呮喐閻楀牆绗氶柛瀣姉閳ь剛鎳撴竟濠囧窗閺嶎厼绀夐柍褜鍓涚槐鎾诲磼濞嗘劗銈版俊鐐茬摠閹倿鍨鹃敂鍓у暗闁荤姷鍏樺濠氬磼濞嗘垵濡介柣搴ｇ懗閸涱垳鐓撻梺瑙勵偧鐠囧弶鍠樻い銏★耿婵偓闁绘﹢娼ф慨?    /// 濠电姷鏁告慨鐑藉极閹间礁纾绘繛鎴旀嚍閸ヮ剦鏁囬柕蹇曞Х椤︻噣鎮楅獮鍨姎妞わ富鍨崇划鍫ュ醇濠㈡繂缍婇幃鈩冩償閿濆棙鍠栭梻浣告惈閹峰宕滃璺虹疄闁靛鍎欓悢鐓庨敜婵°倐鍋撻柟鐣屾暬濮婃椽宕崟顒夋缂備胶绮敃銏狀嚕婵犳碍鍋勯柣鎾虫捣妤犲洭姊洪悷鎵憼缂佽鍟╃花娲⒒閸屾瑦绁版い鏇熺墵瀹曟澘螖閳ь剛鍙呭┑鈽嗗灥閸嬫劙鎯屽▎蹇ｇ唵閻犺桨璀﹂崕鎰版煕鎼达絽鏋涙鐐村浮閹煎綊顢曢妶搴㈢カ闂備礁澹婇崑渚€宕曢弻銉﹀亗闁哄洨濮崑鎾荤嵁閸喖濮庨梺纭呮珪閸旀鍒掔紒妯侯嚤閻庢稒顭囬崢閬嶆⒑闂堟侗鐒鹃柛搴ｆ暬椤㈡挸螖娴ｅ吀绨婚梺鍝勬祩娴滅偟绮旈鈧弻娑㈠煘閹傚濠碉紕鍋戦崐鏍暜婵犲洦鍊块柨鏃囨閸ㄦ棃鏌熺紒銏犳灍闁稿缍侀弻鐔碱敇閻旈鐟ㄦ繝纰樷偓鑼煓闁?UpdateUsernameAsync / UpdateProfilePhotoAsync闂?    /// </summary>
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

            // account.updateProfile 闂傚倸鍊搁崐鐑芥倿閿曞倹鍎戠憸鐗堝笒缁€澶屸偓鍏夊亾闁逞屽墴閸┾偓妞ゆ帊绀侀崵顒勬煕閹捐泛鏋涙鐐插暞閵堬箑鈻庨悙顑晠姊绘担鍝ワ紞缂侇噮鍨堕獮鎴﹀炊瑜忛弳锔炬喐閻楀牆淇柡浣稿暣閺屻劌鈽夊Ο铏圭泿缂備胶濮甸幑鍥ь嚕鐠囨祴妲堟俊顖炴敱閻庤鈹戦埥鍡楃仴婵炲拑绲剧粋鎺楀箹娴ｅ厜鎷洪梻渚囧亞閸嬫盯鎳熼娑欐珷妞ゆ柨顫曟禍婊堟煥閺囨浜鹃悗瑙勬处閸撶喖宕洪姀銈呯閻犲洩灏欓娲⒑閹稿孩顥嗗┑顔哄€曢埢宥夊冀瑜夐弨鑺ャ亜閺冣偓閺嬬粯绗熷☉銏＄厱閻庯綆浜濋ˉ銏ゆ煙?null 闂傚倸鍊峰ù鍥х暦閻㈢纾婚柣鎰暩閻瑩鐓崶銊р槈缂佲偓婢舵劕绠规繛锝庡墮婵＄厧顩奸崨顓涙斀妞ゆ梹鏋绘笟娑㈡煕濮椻偓缁犳牠宕洪姀銈呯睄闁稿本顨呮禍鐐殽閻愯尙浠㈤柛鏃€宀搁幃妤€顫濋悡搴＄睄閻庤娲╃紞鈧紒鐘崇☉閳藉鈻庨幋婵嗙闂傚倷绶氶埀顒傚仜閼活垱鏅堕悧鍫㈢闁瑰濮甸弳顒傗偓瑙勬处娴滄繈骞忛崨鏉戝窛濠电姴瀚В宀勬⒒閸屾瑦绁版い鏇嗗應鍋撳☉鎺撴珖缂侇喗鐟╅獮鎺戭渻鐏忔牕浜鹃柛鎰靛枛楠炪垺绻涢幋锝夊摵闁?            string? firstName = null;
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
            var msg = string.IsNullOrWhiteSpace(details) ? summary : $"{summary}闂傚倸鍊搁崐鐑芥倿閿旈敮鍋撶粭娑樻噽閻瑩鏌熼悜姗嗘闁轰礁妫涚槐鎺楀棘閸喗缍巘ails}";
            return (false, msg);
        }
    }

    /// <summary>
    /// 闂傚倸鍊搁崐椋庣矆娓氣偓楠炴牠顢曢埛姘そ婵¤埖寰勭€ｎ亙妲愰梻渚€娼ц墝闁哄懏鐩幏鎴︽偄鐏忎焦鏂€闂佺粯锚瀵爼宕冲畡鎳婄懓顭ㄩ崘顏喰ㄩ梺鍝勭灱閸犳牠鐛崱姘兼Щ闂佸搫妫滄ご鎼佸Φ閸曨垰围闁告侗鍠栧▓妤呮⒑鐠団€虫灈缂傚秴锕悰顕€宕堕鈧粈鍫澝归敐鍥у妺婵炲牊鎮傞弻锝夋偄閸濄儳鐓佸┑鐘灪閿氭い顓炴喘閺佹捇鎮╅懠顒傛毇濠电偛顕慨鎾敄閸涱喖鍔旈梻鍌欑劍鐎笛兠哄澶婄；闁瑰墽绮悡鐔兼煙閹呮憼缂佲偓鐎ｎ喗鐓涢悘鐐插⒔閳藉銇勯锝囩疄妞ゃ垺顨婂畷鎺懳旀担琛″亾閸濄儳纾介柛灞剧懄缁佹澘顪冪€涙ɑ鍊愰柟?me/xxx闂傚倸鍊搁崐鐑芥倿閿旈敮鍋撶粭娑樻噽閻瑩鏌熷▎陇顕уú顓€佸鈧慨鈧柣姗€娼ф慨锔戒繆閻愵亜鈧牕顔忔繝姘；闁瑰墽绮悡鏇炩攽閻樺弶绁╂俊顐ｅ灦娣囧﹪宕ｆ径濠傤潚閻庤娲樼划宥夊箯閸涘瓨鎯為柛锔诲幖楠炲洭姊婚崒娆戭槮闁硅绱曢幑銏ゅ磼濠ф儳浜炬慨妯煎帶濞呭秵顨ラ悙鎼疁闁诡喓鍨介幖褰掑捶椤撱劎搴婂┑鐘茬棄閺夊簱鍋撻弴銏╂晞闁搞儮鏅滈～鏇灻归悡搴ｆ憼闁稿绻濋弻锟犲炊閳轰椒娌銈冨劤婵炩偓闁哄瞼鍠栭、娆戞嫚閹绘帞銈┑鐘殿暯閸撴繈鎮洪弴鈶哄洭寮跺Λ闈涚秺閹晛鐣烽崶锝咁潓缂傚倷鑳剁划顖炴儎椤栫偟宓佹慨妞诲亾闁圭厧缍婇、鏇㈠閳藉棔妲愰梻鍌氬€风粈渚€鎮块崶顒婄稏濠㈣埖鍔栭崑瀣煟濡鍤欑紒鐘崇墪闇夐柣妯烘▕閸庡繘骞嗛悢鍏尖拺闁圭瀛╃壕鐢告煕鐎ｎ偅宕岄柡宀嬬秮楠炴鈧稒顭囬ˇ浼存⒑閸濆嫭婀扮紒瀣灴閿濈偛鈹戠€ｅ灚鏅㈤梺绋胯嫰閸婄粯绻涢埀顒勬煛?    /// </summary>
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

            // result 闂傚倸鍊搁崐椋庣矆娓氣偓楠炲鏁撻悩鍐蹭画濡炪倖鐗滈崑娑㈠垂閸岀偞鐓熼柕蹇嬪焺閻掑墽绱掗埀顒勫磼濞戞瑥寮垮┑锛勫仩椤曆勭妤ｅ啯鍊?User 闂?bool闂傚倸鍊搁崐鐑芥倿閿旈敮鍋撶粭娑樻噽閻瑩鏌熸潏楣冩闁稿孩妫冮弻銈吤圭€ｎ偅鐝旈梺鎼炲妽缁诲牓寮婚敐澶婄闁绘垵妫涢崝鐑芥⒑瀹曞洨甯涙俊顐㈠暣瀵顓奸崼顐ｎ€囬梻浣告啞閹歌顫濋妸褍鍨濋悗锝庡枛缁犳娊鏌熼悙顒佺稇闁告柨鎳樺娲濞戞艾顣哄┑鐐茬湴閸旀垿骞冨▎鎾冲嵆闁靛繆妾ч幏铏圭磽娴ｅ壊鍎撴繛澶嬫礈缁鎮欓悜妯煎幈闁硅偐琛ラ埀顒€鐤囬崺鐐烘煟閹惧崬鈧繈寮婚垾鎰佸悑闁告劑鍔岄‖瀣磽娴ｅ搫校闁荤噦绠撴俊鐢稿礋椤栨氨鐤€闂佸疇妗ㄩ懗鑸电閼测晝纾藉ù锝夘棑鐠愪即鏌涢敐蹇曠М鐎?            var normalized = string.IsNullOrWhiteSpace(username) ? null : username;
            return (true, null, normalized);
        }
        catch (Exception ex)
        {
            var (summary, details) = MapTelegramException(ex);
            var msg = string.IsNullOrWhiteSpace(details) ? summary : $"{summary}闂傚倸鍊搁崐鐑芥倿閿旈敮鍋撶粭娑樻噽閻瑩鏌熼悜姗嗘闁轰礁妫涚槐鎺楀棘閸喗缍巘ails}";
            return (false, msg, null);
        }
    }

    /// <summary>
    /// 闂傚倸鍊搁崐鎼佸磹妞嬪孩顐介柨鐔哄Т绾惧鏌涘☉鍗炴灓闁崇懓绉归弻褑绠涘鍏肩秷閻庤娲橀悡锟犲蓟濞戙垹绠涢梻鍫熺⊕閻忓秹姊虹紒妯绘儓缂傚秳绶氬濠氭晲婢跺﹦鐤€闂傚倸鐗婄粙鎴︼綖閳哄倻绡€?闂傚倸鍊搁崐鐑芥倿閿曗偓椤啴宕归鍛姺闂佺鍕垫當缂佲偓婢跺备鍋撻獮鍨姎妞わ富鍨跺浼村Ψ閿斿墽顔曢梺鐟扮摠閻熴儵鎮橀埡鍛仭婵炲棙鐟ラ々顒勬煃瑜滈崜娆戠不瀹ュ纾块梺顒€绉寸粻鐘诲箹閹碱厾鍘滅紓宥嗙墵閺屻劌鈹戦崱鈺傂ч梺缁樻尰濞茬喖鎮￠锕€鐐婄憸婊堝磿韫囨稒鐓曢柡鍌濐嚙閳ь剚绻堝璇测槈濠婂懐鏉搁柣搴秵娴滅偞瀵奸崟顓犵＝濞达絼绮欓崫娲偨椤栨せ鍋撳畷鍥ㄦ闂佺粯姊婚埛鍫ュ极閸℃稒鐓冪憸婊堝礈閻旂厧绠栨俊顖欑秿閺冨牆宸濇い鏃堟？閸栨牕鈹戦悙鑸靛涧缂傚秳绀侀…鍥樄闁诡喗锕㈠畷锝嗗緞婢跺瞼鐣鹃梻浣告贡缁垳鏁幒妤婃晜妞ゅ繐鐗婇悡銉︾箾閹寸偟鎳冨┑顔肩У椤ㄣ儵鎮欏顔煎壎闂佽桨绀侀崐鍨暦瑜版帩鏁嬮柛娑卞櫘濡儵姊婚崒姘偓鐑芥倿閿旈敮鍋撶粭娑樻噽閻瑩鏌熸潏楣冩闁稿顑呴埞鎴︽偐閸欏鎮欓梺鍝勵儎閻掞箓濡甸崟顖氬嵆婵°倐鍋撳ù婊勫劤铻?https://t.me/xxx闂傚倸鍊搁崐椋庢濮橆剦鐒界憸宥堢亱闂佸搫鍟犻崑鎾寸箾?me/+hash闂傚倸鍊搁崐椋庢濮橆剦鐒界憸宥堢亱闂佸搫鍟崐濠氬础濮樿京纾煎璺侯儑椤㈩柅rname闂傚倸鍊搁崐椋庢濮橆剦鐒界憸宥堢亱闂佸搫鍟悧濠囧吹閺囥垺鐓曢柨婵嗘閹嵐name闂傚倸鍊搁崐椋庢濮橆剦鐒界憸宥堢亱闂佸搫鍟犻崑鎾寸箾閻撳海绠伴柕?//join?invite=hash 缂傚倸鍊搁崐鎼佸磹閻戣姤鍊块柨鏂垮⒔閻瑩鏌熷▎鈥崇湴閸旀垿宕洪埀顒併亜閹烘垵鈧崵澹曟總绋跨骇闁割偅绋戞俊濂告煕濠靛棙鎯堥柍?    /// </summary>
    public async Task<(bool Success, string? Error, string? JoinedTitle)> JoinChatOrChannelAsync(
        int accountId,
        string linkOrUsername,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var raw = (linkOrUsername ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(raw))
                return (false, "闂傚倸鍊搁崐鎼佸磹妞嬪海鐭嗗ù锝堫潐濞呯姴霉閻樺樊鍎忕痪鎯ь煼閺岀喖骞戦幇顒傚帿闂?闂傚倸鍊搁崐鐑芥倿閿曗偓椤啴宕归鍛姺闂佺鍕垫當缂佲偓婢跺备鍋撻獮鍨姎妞わ富鍨跺浼村Ψ閿斿墽顔曢梺鐟扮摠閻熴儵鎮橀埡鍛仭婵炲棙鐟ラ々顒勬煃瑜滈崜娆戠不瀹ュ纾块弶鍫氭杹閸嬫挸顫濋悡搴ｄ化缂備緡鍠栭悧蹇涘焵椤掑﹦绉甸柛鎾寸〒婢?, null);

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
            return (true, null, "闂傚倷娴囬褍霉閻戣棄绠犻柟鎹愵嚙缁犵喖姊介崶顒€桅闁圭増婢樼粈鍐┿亜椤撶喎绗╅柍褜鍓氱€笛囧Φ閸曨喚鐤€闁圭偓鎯屽Λ銈囩磽娴ｆ彃浜鹃梺鍛婂姦閸犳鎮¤箛娑欑厱妞ゆ劧绲跨粻鏍偓鐟版啞缁诲牓寮?婵犵數濮烽。钘壩ｉ崨鏉戠；闁糕剝蓱濞呯姵淇婇妶鍛櫣闁搞劌鍊块弻鐔虹矙閹稿孩宕冲銈傛櫆閻擄繝寮婚敐澶婄疀闂傚牊绋戦～顐︽⒑?);
        }
        catch (Exception ex)
        {
            var (summary, details) = MapTelegramException(ex);
            var msg = string.IsNullOrWhiteSpace(details) ? summary : $"{summary}闂傚倸鍊搁崐鐑芥倿閿旈敮鍋撶粭娑樻噽閻瑩鏌熼悜姗嗘闁轰礁妫涚槐鎺楀棘閸喗缍巘ails}";
            return (false, msg, null);
        }
    }

    /// <summary>
    /// 闂傚倸鍊搁崐鎼佸磹妞嬪孩顐介柨鐔哄Т绾惧鏌涘☉鍗炴灓闁崇懓绉归弻褑绠涘鍏肩秷閻庤娲橀悡锟犲蓟濞戙垹绠涢梻鍫熺⊕閻忓秹姊虹紒妯绘儓缂傚秳绶氬濠氭晲婢跺﹦鐤€闂傚倸鐗婄粙鎴︼綖閳哄倻绡€?闂傚倸鍊搁崐鐑芥倿閿曗偓椤啴宕归鍛姺闂佺鍕垫當缂佲偓婢跺备鍋撻獮鍨姎妞わ富鍨跺浼村Ψ閿斿墽顔曢梺鐟扮摠閻熴儵鎮橀埡鍛仭婵炲棙鐟ラ々顒勬煃瑜滈崜娆戠不瀹ュ纾块梺顒€鍗曢崶顒夋晝闁挎洍鍋撶痪鎯ь煼閺岀喖骞戦幇闈涙闂佺粯甯掗悘姘跺Φ閸曨垰绠抽柟瀛樼箥娴犺偐绱撴担鎻掍壕闂佸憡娲﹂崹閬嶆偂閿濆鍙撻柛銉ｅ妽缁€鍐煕閵堝倸浜鹃梻鍌欑閹诧繝鎮烽妷鈺佺柈闁规鍠楅～鏇㈡煙閹呮憼濠殿垰顕槐鎺斺偓锝庡亽閸庛儱霉閻樿崵鐣烘慨濠冩そ楠炴牠鎮欓幓鎹ㄢ晠姊洪崨濠冣拹闁搞劎鏁婚幃楣冩倻閼恒儲娅滄繝銏ｆ硾閿曘倝宕妸鈺傗拺闂傚牊渚楅悡顒勬煟韫囨梻绠炲┑锛勫厴閸╋繝宕掑顐㈠婵犵數鍋犻幓顏嗙礊娴ｅ壊鐒界憸鏃堝箖濮椻偓瀹曪絾寰勬径宀€鐣鹃梻浣告贡缁垳鏁幒妤婃晜妞ゅ繐鐗婇悡銉︾箾閹寸偟鎳冨┑顔肩У椤ㄣ儵鎮欏顔煎壎闂佽桨绀侀崐鍨暦瑜版帩鏁嬮柛娑卞櫘濡儵姊婚崒姘偓鐑芥倿閿旈敮鍋撶粭娑樻噽閻瑩鏌熸潏楣冩闁稿顑呴埞鎴︽偐閸欏鎮欓梺鍝勵儎閻掞箓濡甸崟顖氬嵆婵°倐鍋撳ù婊勫劤铻?https://t.me/xxx闂傚倸鍊搁崐椋庢濮橆剦鐒界憸宥堢亱闂佸搫鍟犻崑鎾寸箾?me/+hash闂傚倸鍊搁崐椋庢濮橆剦鐒界憸宥堢亱闂佸搫鍟崐濠氬础濮樿京纾煎璺侯儑椤㈩柅rname闂傚倸鍊搁崐椋庢濮橆剦鐒界憸宥堢亱闂佸搫鍟悧濠囧吹閺囥垺鐓曢柨婵嗘閹嵐name闂傚倸鍊搁崐椋庢濮橆剦鐒界憸宥堢亱闂佸搫鍟犻崑鎾寸箾閻撳海绠伴柕?//join?invite=hash 缂傚倸鍊搁崐鎼佸磹閻戣姤鍊块柨鏂垮⒔閻瑩鏌熷▎鈥崇湴閸旀垿宕洪埀顒併亜閹烘垵鈧崵澹曟總绋跨骇闁割偅绋戞俊濂告煕濠靛棙鎯堥柍?    /// </summary>
    public async Task<(bool Success, string? Error, string? LeftTitle)> LeaveChatOrChannelAsync(
        int accountId,
        string linkOrUsername,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var raw = (linkOrUsername ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(raw))
                return (false, "闂傚倸鍊搁崐鎼佸磹妞嬪海鐭嗗ù锝堫潐濞呯姴霉閻樺樊鍎忕痪鎯ь煼閺岀喖骞戦幇顒傚帿闂?闂傚倸鍊搁崐鐑芥倿閿曗偓椤啴宕归鍛姺闂佺鍕垫當缂佲偓婢跺备鍋撻獮鍨姎妞わ富鍨跺浼村Ψ閿斿墽顔曢梺鐟扮摠閻熴儵鎮橀埡鍛仭婵炲棙鐟ラ々顒勬煃瑜滈崜娆戠不瀹ュ纾块弶鍫氭杹閸嬫挸顫濋悡搴ｄ化缂備緡鍠栭悧蹇涘焵椤掑﹦绉甸柛鎾寸〒婢?, null);

            var url = NormalizeTelegramJoinUrl(raw);

            var client = await GetOrCreateConnectedClientAsync(accountId, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            // 闂傚倸鍊峰ù鍥х暦閻㈢绐楅柟鎵閸嬶繝寮堕崼姘珔缂佽翰鍊曡灃闁挎繂鎳庨弳鐐烘煕婵犲洦娑ч棁澶愭煟濡儤鈻曢柛搴㈠姍閺屾稒鎯旈垾鎰佸妷闂侀潧娲ょ€氭澘顕ｉ鈧畷鎺戔槈濞嗘垵娑ч梻鍌欑劍閹爼宕濆畝鍕ч柟闂寸閽冪喖鏌曟繛鍨姉婵℃彃鐗婃穱濠囶敍濠婂啫浠樺銈忕到濠€杈╂閹惧瓨濯撮悹鍥ｅ墲閻ｈ泛顪冮妶蹇曠暤婵炰匠鍥ㄥ仼鐎瑰嫭澹嬮弨浠嬫倵閿濆骸浜滃ù婊冪秺濮婃椽宕烽鐘茬闁汇埄鍨辩敮鎺撴櫏闂佸搫琚崕鏌ュ煕?            var chat = await client.AnalyzeInviteLink(url, join: false);
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
                return (false, "闂傚倸鍊搁崐椋庣矆娓氣偓楠炴牠顢曢敃鈧悿顕€鏌ｅΔ鈧悧濠囧矗韫囨稒鐓熼柕蹇曞Х娴犳盯鏌涜箛鏃傜煉闁哄本鐩獮姗€鎳犻鈧俊浠嬫⒑閸濄儱孝婵☆偅绻堝濠氬Ω閵夈垺鏂€闂佺硶鍓濋敃鈺佄涢妶澶嬧拺闂侇偆鍋涢懟顖炲储閸濄儳纾兼い鏃傛櫕閹冲洦顨ラ悙鍙夘棦鐎规洜鍠栭、娑㈡晲閸℃ɑ鐝﹂梻鍌欐缁鳖喚寰婃禒瀣殣妞ゆ牜鍋為崐鍫曞级閸碍娅囩痪鍙ョ矙閺屾稓浠﹂崜褜鏆￠梺绋胯閸旀垿寮诲☉娆戠瘈闁告劗鍋撻悾濂告⒑?婵犵數濮烽。钘壩ｉ崨鏉戠；闁糕剝蓱濞呯姵淇婇妶鍛櫣闁搞劌鍊块弻鐔虹矙閹稿孩宕冲?, null);

            await client.LeaveChat(peer);
            return (true, null, title);
        }
        catch (RpcException ex) when (ex.Code == 400 && string.Equals(ex.Message, "USER_NOT_PARTICIPANT", StringComparison.OrdinalIgnoreCase))
        {
            return (true, null, "闂傚倸鍊搁崐椋庣矆娓氣偓楠炴牠顢曢敂钘変罕濠电姴锕ょ€氼噣銆呴崣澶岀瘈濠电姴鍊搁弸锕傛煠閻楀牆顕滈柕鍥у缁犳盯骞橀幖顓燂骏缂傚倷璁查崑鎾绘煕閹伴潧鏋熼柣鎾崇箻閺屾盯顢曢敐鍥╃暫閻庣懓鎲＄换鍫ュ蓟?婵犵數濮烽。钘壩ｉ崨鏉戠；闁糕剝蓱濞呯姵淇婇妶鍛櫣闁搞劌鍊块弻鐔虹矙閹稿孩宕冲銈傛櫆閻擄繝寮婚敐澶婄疀闂傚牊绋戦～顐︽⒑?);
        }
        catch (Exception ex)
        {
            var (summary, details) = MapTelegramException(ex);
            var msg = string.IsNullOrWhiteSpace(details) ? summary : $"{summary}闂傚倸鍊搁崐鐑芥倿閿旈敮鍋撶粭娑樻噽閻瑩鏌熼悜姗嗘闁轰礁妫涚槐鎺楀棘閸喗缍巘ails}";
            return (false, msg, null);
        }
    }

    /// <summary>
    /// 闂傚倸鍊搁崐椋庣矆娓氣偓楠炲鏁撻悩鑼槱閻熸粎澧楃敮鎺楀垂閸岀偞鐓欓柟顖滃椤ュ鏌＄€ｂ晝绐旈柡灞炬礋瀹曠厧鈹戦崶鑸殿棧缂傚倷绀佹晶搴ㄥ磻閵堝钃熼柍銉﹀墯閸氬骞栫划鍏夊亾瀹曞浂鍟囬梻?Bot闂傚倸鍊搁崐鐑芥倿閿旈敮鍋撶粭娑樻噽閻瑩鏌熸潏楣冩闁稿顑夐弻娑㈠焺閸愵亖妲堢紓?Bot 闂傚倸鍊搁崐椋庣矆娓氣偓楠炲鏁撻悩鍐蹭画闂佹寧娲栭崐鎼佸垂閸岀偞鐓曠憸搴ㄣ€冮崨瀛樺€?/start闂傚倸鍊搁崐鐑芥倿閿旈敮鍋撶粭娑樻噽閻瑩鏌熸潏楣冩闁搞倖鍔栭妵鍕冀閵娧呯窗婵炲瓨绮岀紞濠囧蓟閵堝洤鏋堥柛妤冨仜椤亪姊洪崫鍕棡缂侇喗鎹囧濠氬Ω閳轰礁宓嗗┑掳鍊愰崑鎾趁瑰鍫㈢暫闁诡喗顨呴埢鎾诲垂椤旂晫褰梻浣告啞閹搁箖宕版惔顭戞晪闁挎繂顦介弫鍐煏閸繃顥炴繛鍫熸緲椤啴濡堕崱妯煎弳婵犫拃鍌滅煓閽樻繃銇勯幇鍫曟闁?    /// 闂傚倸鍊搁崐宄懊归崶顒€违闁逞屽墴閺屾稓鈧綆鍋呯亸浼存煏閸パ冾伃鐎殿喕绮欐俊姝岊槷婵℃彃鐗撳鐑樺濞嗗繒妲ｉ梺闈╃秶缂嶄礁顕ｆ繝姘╅柕澶堝灪椤秴鈹戦悙鍙夘棡闁挎岸鏌涢幘棰濈唵xxbot闂傚倸鍊搁崐椋庢濮橆剦鐒界憸宥堢亱闂佸搫鍟崐鑺ユ叏閾忣偆绠惧璺侯煬閳ь剙鍨簅t闂傚倸鍊搁崐椋庢濮橆剦鐒界憸宥堢亱闂佸搫鍊归娆撳吹閺囥垺鐓欓柛蹇撳悑椤庡窋s://t.me/xxxbot闂傚倸鍊搁崐椋庢濮橆剦鐒界憸宥堢亱闂佸搫鍟犻崑鎾寸箾閻撳海绠伴柕?//resolve?domain=xxxbot&start=abc
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
                return (false, "闂傚倸鍊搁崐椋庣矆娓氣偓楠炲鏁撻悩鑼槱閻熸粎澧楃敮鎺楀垂閸岀偞鐓熸俊銈傚亾闁绘锕畷锝堢疀濞戞瑧鍘撻梺鍛婄箓鐎氼參宕宠ぐ鎺撶厽妞ゆ挾鍣ュ▓婊堟煛鐏炲墽銆掑ù鐙呯畵瀹曟粏顦┑顔兼搐閳规垿鎮欓懠顒€顣洪梺缁樼墱閸樠囶敋閿濆閱囬柡鍥╁仧閸樺憡绻涙潏鍓ф偧闁硅櫕鎹囧鎼佹偐缂佹ǚ鎷洪梺鍛婄箓鐎氼參藟閵忕妴鐟邦煥閸愵亜鐓熼悗娈垮櫘閸嬪嫰顢橀崗鐓庣窞濠电姴娲ら弫褰掓⒒娴ｈ櫣甯涙俊顐㈠暣瀵煡鎳犻鈧崹婵囥亜閺嶃劎銆掔紒鈾€鍋?64 闂傚倸鍊峰ù鍥敋瑜忛埀顒佺▓閺呮繄鍒掑▎鎾崇婵＄偛鐨烽崑鎾诲礃椤斿ジ鍞堕梺闈涱樈閸犳寮查悙瀵哥闁圭偓娼欓悞褰掓煕鐎ｎ偅宕岄柡?, null);

            var client = await GetOrCreateConnectedClientAsync(accountId, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            var resolved = await client.Contacts_ResolveUsername(username);
            var user = resolved.User;
            if (user.access_hash == 0)
                return (false, "闂傚倸鍊搁崐椋庣矆娓氣偓楠炴牠顢曢敃鈧悿顕€鏌ｅΔ鈧悧濠囧矗韫囨稒鐓熼柕蹇曞Х娴犳盯鏌涜箛鏃傜煉闁哄本鐩、鏇㈡晲閸℃瑯妲伴梻浣规偠閸婃牕顫忔繝姘劦妞ゆ帒鍊归崵鈧柣搴㈠嚬閸樺ジ鈥﹂崹顔ョ喖鎮℃惔锝囩摌?Bot access_hash", null);

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
            return (false, "闂傚倸鍊搁崐鐑芥嚄閸洖纾块柣銏㈩焾閻ら箖鏌嶉崫鍕櫣缂佹劖顨嗘穱濠囧Χ閸涱喖娅ら梺绋款儏椤︾敻寮婚妸銉㈡斀闁糕剝锕╁Λ銈夋⒑瀹曞洨甯涙慨濠傤煼閸┾偓妞ゆ巻鍋撶紒鐘茬Ч瀹曟洟宕￠悙宥嗙☉閳诲酣骞嬮悩鍐叉珮闂傚倸鍊搁崐椋庣矆娓氣偓楠炲鏁撻悩鍐蹭画濡炪倖鐗滈崑娑㈠垂閸屾褰掑礂閸忚偐绋囩紓浣哄У瀹€鎼佺嵁閺嶎灔搴敆閳ь剚淇婇懖鈺冪＜闁逞屽墯瀵板嫰骞囬鐘插妇濠电姷鏁搁崐顖炲礃椤垳绱戦梻?Bot闂傚倸鍊搁崐鐑芥倿閿旈敮鍋撶粭娑樻噽閻瑩鏌熺€涙绠伴柤鏉挎健閺岀喖顢楅崒娑樹簵_APP_INVALID闂?, null);
        }
        catch (RpcException ex) when (ex.Code == 400 && string.Equals(ex.Message, "PEER_FLOOD", StringComparison.OrdinalIgnoreCase))
        {
            return (false, "闂傚倸鍊峰ù鍥х暦閻㈢绐楅柟鎵閸嬶繝寮堕崼姘珖濞戞挸绉归弻鐔告綇閸撗呫偡婵炲瓨绮岀紞濠囧蓟濞戞矮娌柛娆嶅劤缁讳礁鈹戦埄鍐ㄧ祷缂傚秴锕悰顕€寮介‖銉ラ叄椤㈡鍩€椤掑嫭鍊堕柍杞版€ヨぐ鎺撳亹缂佹稓顢婇埀顒€娼￠弻锛勪沪鐠囨彃顬堥梺瀹犳椤︻垵鐏掔紓鍌欑劍鐪夌紒杈ㄦ憲ER_FLOOD闂傚倸鍊搁崐鐑芥倿閿旈敮鍋撶粭娑樻噽閻瑩鏌熷▎鈥崇湴閸旀垿宕洪埀顒併亜閹烘垵鈧崵澹曟總绋跨骇闁割偅绋戞俊璺ㄧ磼閻樼鑰块柡宀€鍠愰ˇ鐗堟償閳锯偓閺嬪懘姊虹拠鈥虫灍缂侇喗鎹囬獮濠囨倷閸濆嫀銊︺亜閺冨倹鍤€濞存粓绠栭弻鈥愁吋鎼达絼姹楅悷婊呭鐢寮查弻銉︾厱婵炴垶鐟︾紞鎴犫偓娈垮枟濠㈡﹢鈥旈崘顔嘉ч煫鍥ㄦ礀閸╁矂姊虹紒妯煎ⅹ闁靛牊鎮傞悰顕€宕橀鑲╊唴闂佽姤锚椤﹂亶宕愰悙鐑樺仭婵犲﹤鍟撮崣鍕偓瑙勬礃閸旀瑩骞冩禒瀣窛濠电偟鍋撶€氬ジ姊绘担鍛婅础缂侇噮鍨抽弫顕€鎮欓崣澶嬬槑闂?, null);
        }
        catch (Exception ex)
        {
            var (summary, details) = MapTelegramException(ex);
            var msg = string.IsNullOrWhiteSpace(details) ? summary : $"{summary}闂傚倸鍊搁崐鐑芥倿閿旈敮鍋撶粭娑樻噽閻瑩鏌熼悜姗嗘闁轰礁妫涚槐鎺楀棘閸喗缍巘ails}";
            return (false, msg, null);
        }
    }

    /// <summary>
    /// 闂傚倸鍊搁崐鐑芥嚄閸洍鈧箓宕奸妷顔芥櫈闂佺硶鍓濋悷銉╁垂濠靛绠规繛锝庡墮婵″ジ鏌＄€ｂ晝绐旈柡灞炬礋瀹曠厧鈹戦崶鑸殿棧缂傚倷绀佹晶搴ㄥ磻閵堝钃熼柍銉﹀墯閸氬骞栫划鍏夊亾瀹曞浂鍟囬梻?Bot闂傚倸鍊搁崐鐑芥倿閿旈敮鍋撶粭娑樻噽閻瑩鏌熸潏楣冩闁稿顑夐弻鐔煎箲閹伴潧娈梺缁樺笒閻忔岸濡甸崟顖氱鐎广儱妫Ο鍌涚箾鐎涙ê娈犻柛銊ㄥ吹濡叉劙骞掗弮鈧€氭岸鏌熺紒妯虹瑲婵炲牐顕ц灃闁绘﹢娼ф禒婊呯磼缂佹ê鐏ユい鏇樺劦瀹曠喖顢涘杈╂澑婵＄偑鍊栧Λ浣肝涢崟顖ｆ晜?Bot 闂傚倸鍊峰ù鍥敋瑜庨〃銉х矙閸柭も偓鍧楁⒑椤掆偓缁夊澹曟繝姘厽闁哄啫娲ゆ禍鍦偓瑙勬尫缁舵岸寮诲☉銏犖ㄦい鏃傚帶椤亪姊洪崫鍕闁告挻鐟╅垾锔炬崉閵婏箑纾梺鎯х箳閹虫捇銆傞悽鍛娾拺?    /// 闂傚倸鍊搁崐宄懊归崶顒€违闁逞屽墴閺屾稓鈧綆鍋呯亸浼存煏閸パ冾伃鐎殿喕绮欐俊姝岊槷婵℃彃鐗撳鐑樺濞嗗繒妲ｉ梺闈╃秶缂嶄礁顕ｆ繝姘╅柕澶堝灪椤秴鈹戦悙鍙夘棡闁挎岸鏌涢幘棰濈唵xxbot闂傚倸鍊搁崐椋庢濮橆剦鐒界憸宥堢亱闂佸搫鍟崐鑺ユ叏閾忣偆绠惧璺侯煬閳ь剙鍨簅t闂傚倸鍊搁崐椋庢濮橆剦鐒界憸宥堢亱闂佸搫鍊归娆撳吹閺囥垺鐓欓柛蹇撳悑椤庡窋s://t.me/xxxbot闂傚倸鍊搁崐椋庢濮橆剦鐒界憸宥堢亱闂佸搫鍟犻崑鎾寸箾閻撳海绠伴柕?//resolve?domain=xxxbot
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
                return (false, "闂傚倸鍊搁崐椋庣矆娓氣偓楠炴牠顢曢敃鈧悿顕€鏌ｅΔ鈧悧濠囧矗韫囨稒鐓熼柕蹇曞Х娴犳盯鏌涜箛鏃傜煉闁哄本鐩、鏇㈡晲閸℃瑯妲伴梻浣规偠閸婃牕顫忔繝姘劦妞ゆ帒鍊归崵鈧柣搴㈠嚬閸樺ジ鈥﹂崹顔ョ喖鎮℃惔锝囩摌?Bot access_hash", null);

            await client.Contacts_Block(new InputPeerUser(user.id, user.access_hash));
            return (true, null, "@" + username);
        }
        catch (RpcException ex) when (ex.Code == 400 && string.Equals(ex.Message, "USER_NOT_MUTUAL_CONTACT", StringComparison.OrdinalIgnoreCase))
        {
            // 闂傚倸鍊搁崐椋庣矆娓氣偓楠炴牠顢曢敃鈧€氬銇勯幒鎴濐仼缂佲偓婢舵劖鐓欓柣鎴炆戦ˉ鍡椻攽椤旇棄鈻曢柡灞熷棛鐤€婵ê鍚嬬紞鍫ユ煟鎼淬垼澹樻い顓犲厴瀵濡搁埡浣稿祮濠德板€愰崑鎾趁瑰鍫㈢暫闁诡喖鍢查…銊╁礋椤掑倸鍤掓繝鐢靛仧閵嗗鎹㈠┑瀣祦闁告劦鍠栭悡娑㈡煕濞戝崬鏋涙繛鍫熺箘缁辨帡宕滆椤ｅジ鏌ㄥ顑炵懓顭ㄩ崟顐㈠Б闂傚洤顦甸弻銊モ攽閸♀晜鈻撳┑鈽嗗亝閸ㄥ潡寮婚敐澶嬪亜闁告稑锕ょ喊宥夋⒒閸パ屾█闁哄本绋栫粻娑樷槈濞嗘瑧鍚瑰┑鐐茬摠閸ゅ酣宕归懡銈嗩潟闁圭儤姊荤壕鍏间繆椤栨繃顏犲ù鐓庣焸閹鎲撮崟顒傤槬闂佺粯鐗曢妶鎼佺嵁閸愩劉鏋庨柟鏉垮閻鈹戦悩缁樻锭婵☆偅鐟╅幃鐐鐎ｎ偀鎷婚梺绋挎湰閻熝囧礉瀹ュ鍊电紒妤佺☉濞诧箓宕戦敓鐘崇厪濠电偟鍋撳▍鎾绘煛娴ｅ壊鍎旈柡灞炬礋瀹曠厧鈹戦幇顓壯囨⒑閸濆嫭濯奸柛鎾跺枛楠炲啫顫滈埀顒勫箖濞嗘挸绾ч柟瀛樼箓琚橀梻鍌欐缁鳖喚绮婚幋锔光偓锕傛倻閽樺鎽曢悗骞垮劚椤︻垰鏁梻浣瑰閺屻劍鏅舵禒瀣亗闁逞屽墴濮婄粯鎷呴崨濠傛殘闂佺硶鏅涢敃锕傚箲閵忕姭鏀介柛鈩冪懄濞堥箖姊洪悷鎵憼缂佽绉归幃锟犲Ψ閳哄倻鍘卞┑鐐村灥瀹曨剟寮搁悢鍝ョ闁割偅绋戦悘鍙夋叏婵犲啯銇濋柟顔界懇閹稿﹥寰勬繝鍌ゆ渐缂傚倸鍊峰ù鍥敋瑜斿畷鎰版嚄椤栨侗妫ㄩ梻鍌欑閸熷潡骞栭锕€绠犻煫鍥ㄧ☉绾惧鏌ｅΟ鑲╁笡闁绘挻娲熼弻宥夊煛娴ｅ憡鐏撳┑鐐茬墛濡啴骞冪憴鍕闁革富鍘煎銊モ攽椤旂》鏀绘俊鐐扮矙閵嗕礁螖閸涱厾鍔﹀銈嗗笒鐎氼參宕愰懜鐢电瘈闂傚牊渚楅崕蹇涙煕鐏炶濮傞柡灞炬礋瀹曠厧鈹戦幇顓夛妇绱撴担鍝勑為柛搴㈠▕楠炲骞栨担鐟颁罕闂佸壊鍋呯换鍕偡閵娿儮鏀介柣鎰级閸ｇ兘鏌涙繝鍐╃婵犫偓娓氣偓濮婅櫣绱掑Ο蹇氬亹閹峰啴鏁冮崒姘優濠电偛妫楃换鍡涘绩閼恒儯浜滈柡鍐ｅ亾闁稿孩濞婂鎼佸箣閻樼數锛滈柣搴秵娴滆泛螣閳ь剙顪冮妶鍐ㄧ仾闁挎洏鍨介獮鍐閵堝懍绱堕梺闈涱槶閸庤鲸顨欓梻?            return (true, null, null);
        }
        catch (Exception ex)
        {
            var (summary, details) = MapTelegramException(ex);
            var msg = string.IsNullOrWhiteSpace(details) ? summary : $"{summary}闂傚倸鍊搁崐鐑芥倿閿旈敮鍋撶粭娑樻噽閻瑩鏌熼悜姗嗘闁轰礁妫涚槐鎺楀棘閸喗缍巘ails}";
            return (false, msg, null);
        }
    }

    /// <summary>
    /// 闂傚倸鍊峰ù鍥х暦閻㈢绐楅柟鎵閸嬶繝寮堕崼姘珔缂佽翰鍊曡灃闁挎繂鎳庨弳鐐烘煕婵犲嫭鏆柡灞诲€濋獮渚€骞掗幋婵嗩潛缂傚倷绀佹晶搴ㄥ磻閵堝钃熼柍銉﹀墯閸氬骞栫划鍏夊亾瀹曞浂鍟囬梻?Bot 婵犵數濮烽弫鎼佸磻閻愬樊鐒芥繛鍡樻尭鐟欙箓鎮楅敐搴′簽闁崇懓绉电换娑橆啅椤旇崵鍑归梺绋块閿曘倝婀侀梺缁樏Ο濠囧磿閹扮増鐓曢幖绮光偓鎰佸妷闂侀潧娲ょ€氭澘顕ｉ鈧畷鎺戔槈濞嗘垵娑ч梻鍌欑劍閹爼宕濆畝鍕ч柟闂寸閽冪喖鏌曟繛鍨姉婵℃彃鐗婃穱濠囶敍濠婂啫浠橀梺鎰佷邯娴滆泛顫忛悜妯诲濞寸厧鐡ㄩ鏍⒑缁嬪尅鏀婚柣妤佺矌閸掓帞鈧綆浜堕崥瀣煕閳╁厾顏堝礉閸涘瓨鈷戦柡鍌樺劜濞呭懘鏌涢悤浣镐喊闁糕斁鍋撳銈嗗笒閸婂綊寮抽埡鍐＜閺夊牄鍔嶇亸鎵磼濡ゅ啫鏋涚€规洘鍎奸ˇ鑼磼閻樿尙绉洪柟顔煎槻椤劑宕熼鍌氬殥闂備胶顭堥敃銈咁焽閳ユ剚鍤曞┑鐘宠壘瀹告繃銇勯弮鍥棄妞ゅ孩鎹囧娲閳轰胶妲ｉ梺鍛娚戠划鎾翠繆閻戣棄鐓涢柛灞剧矊楠?缂傚倸鍊搁崐鎼佸磹閻戣姤鍊块柨鏂垮⒔閻瑩鏌熷▎陇顕уú顓€佸☉姗嗙叆闁告洟娼ф禒娲⒒娴ｈ櫣甯涙慨濠傤煼瀹曘垼顦归柟顔界懄缁绘繈宕堕妸褍骞楅梻浣哥秺閸嬪﹪宕滃璺虹厺闁哄洨鍠嶇换鍡涙煙缂佹ê淇柣鎾炽偢閺岋紕浠︾拠鎻掝瀳闂佸疇妫勯ˇ顖烇綖濠婂牆骞㈡俊顖滃亼閸婃繂顫?    /// 闂傚倸鍊搁崐宄懊归崶顒€违闁逞屽墴閺屾稓鈧綆鍋呯亸浼存煏閸パ冾伃鐎殿喕绮欐俊姝岊槷婵℃彃鐗撳鐑樺濞嗗繒妲ｉ梺闈╃秶缂嶄礁顕ｆ繝姘╅柕澶堝灪椤秴鈹戦悙鍙夘棡闁挎岸鏌涢幘棰濈唵xxbot闂傚倸鍊搁崐椋庢濮橆剦鐒界憸宥堢亱闂佸搫鍟崐鑺ユ叏閾忣偆绠惧璺侯煬閳ь剙鍨簅t闂傚倸鍊搁崐椋庢濮橆剦鐒界憸宥堢亱闂佸搫鍊归娆撳吹閺囥垺鐓欓柛蹇撳悑椤庡窋s://t.me/xxxbot闂傚倸鍊搁崐椋庢濮橆剦鐒界憸宥堢亱闂佸搫鍟犻崑鎾寸箾閻撳海绠伴柕?//resolve?domain=xxxbot&start=abc
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
                return (false, "闂傚倸鍊搁崐椋庣矆娓氣偓楠炴牠顢曢敃鈧悿顕€鏌ｅΔ鈧悧濠囧矗韫囨稒鐓熼柕蹇曞Х娴犳盯鏌涜箛鏃傜煉闁哄本鐩、鏇㈡晲閸℃瑯妲伴梻浣规偠閸婃牕顫忔繝姘劦妞ゆ帒鍊归崵鈧柣搴㈠嚬閸樺ジ鈥﹂崹顔ョ喖鎮℃惔锝囩摌?Bot access_hash", null, null);

            var target = new ResolvedChatTarget(
                new InputPeerUser(user.id, user.access_hash),
                "@" + username,
                user.id.ToString(CultureInfo.InvariantCulture));
            return (true, null, target, "@" + username);
        }
        catch (Exception ex)
        {
            var (summary, details) = MapTelegramException(ex);
            var msg = string.IsNullOrWhiteSpace(details) ? summary : $"{summary}闂傚倸鍊搁崐鐑芥倿閿旈敮鍋撶粭娑樻噽閻瑩鏌熸潏楣冩闁稿骸锕ラ幈銊モ攽閸艾浜剧€瑰嫮銇慽ls}";
            return (false, msg, null, null);
        }
    }

    public sealed record ResolvedChatTarget(InputPeer Peer, string Title, string CanonicalId);

    /// <summary>
    /// 闂傚倸鍊峰ù鍥х暦閻㈢绐楅柟鎵閸嬶繝寮堕崼姘珔缂佽翰鍊曡灃闁挎繂鎳庨弳鐐烘煕婵犲嫭鏆柡宀嬬秮閺佹劙宕ㄩ婊€绱樼紓鍌欒閸嬫捇鏌涢幇闈涙灍闁绘挸绻橀弻娑㈩敃閿濆洨鐣洪悗鐟版啞缁诲牓寮?婵犵數濮烽。钘壩ｉ崨鏉戠；闁糕剝蓱濞呯姵淇婇妶鍛櫣闁搞劌鍊块弻鐔虹矙閹稿孩宕冲銈傛櫆瑜板啳鐏冮梺鎸庣箓閹冲酣寮抽悙鐑樼厱閹肩补鈧剚鍔夐梺闈涙搐鐎氭澘顕ｉ鈧畷鎺戔槈濞嗘垵娑ч梻鍌欑劍閹爼宕濆畝鍕ч柟闂寸閽冪喖鏌曟繛鍨姉婵℃彃鐗婃穱濠囶敍濠婂懎绗￠梺鍦厴娴滆泛顫忛悜妯诲闁规鍠涚粣妤呮⒑闂堚晝绉剁紓宥勭窔閹即顢氶埀顒勭嵁閸ヮ剦鏁囬柣妯虹仛閸ゅ苯鈹戦悩鍨毄濠殿喖顕埀顒佸嚬閸ｏ綁骞?
    /// - 闂傚倸鍊搁崐鐑芥倿閿曗偓椤啴宕归鍛姺闂佺鍕垫當缂佲偓婢跺备鍋撻獮鍨姎妞わ富鍨跺浼村Ψ閿斿墽顔曢梺鐟扮摠閻熴儵鎮橀埡鍛仭?闂傚倸鍊搁崐鎼佸磹妞嬪海鐭嗗ù锝堫潐濞呯姴霉閻樺樊鍎忕痪鎯ь煼閺岀喖骞戦幇顒傚帿闂佸搫妫欑划鎾诲蓟閻旇櫣纾奸柕蹇曞У閻忓秹姊洪崫鍕闁告挻姘ㄩ幑銏犫槈閵忕姴鑰垮┑鐐叉閸旓箑危閻ㄥズrname闂傚倸鍊搁崐椋庢濮橆剦鐒界憸宥堢亱闂佸搫鍟悧濠囧吹閺囥垺鐓曢柨婵嗘閹嵐name闂傚倸鍊搁崐椋庢濮橆剦鐒界憸宥堢亱闂佸搫鍊归娆撳吹閺囥垺鐓欓柛蹇撳悑椤庡窋s://t.me/xxx闂傚倸鍊搁崐椋庢濮橆剦鐒界憸宥堢亱闂佸搫鍟犻崑鎾寸箾?me/xxx闂傚倸鍊搁崐椋庢濮橆剦鐒界憸宥堢亱闂佸搫鍟犻崑鎾寸箾閻撳海绠伴柕?//join?invite=hash
    /// - 婵犵數濮烽。钘壩ｉ崨鏉戠；闁糕剝蓱濞呯姵淇婇妶鍛櫣闁搞劌鍊块弻鐔虹矙閹稿孩宕冲?缂傚倸鍊搁崐鎼佸磹閹间礁纾圭紒瀣紩濞差亜顫呴柕鍫濇噽閿涙瑥鈹戞幊閸婃洟宕锕€鍨?ID闂?23456闂?123456闂?1001234567890
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
                return (false, "闂傚倸鍊搁崐鐑芥嚄閸洖纾块柣銏㈩焾閻ら箖鏌嶉崫鍕櫣缂佹劖顨嗘穱濠囧Χ閸涱喖娅ら梺绋款儏椤︾敻寮婚妸銉㈡斀闁糕剝锕╁Λ銈夋⒑瀹曞洨甯涙俊顐㈠暣瀵槒顦剁紒鐘崇洴楠炴﹢宕橀懠鍓佺闂?, null);

            var client = await GetOrCreateConnectedClientAsync(accountId, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            if (TryParseChatIdCandidate(raw, out var normalizedId))
            {
                var resolvedById = await TryResolveChatByIdFromDialogsAsync(client, normalizedId, cancellationToken);
                if (resolvedById != null)
                    return (true, null, resolvedById);

                return (false, $"闂傚倸鍊搁崐椋庣矆娓氣偓楠炴牠顢曢敂钘変罕濠电姴锕ょ€氼噣銆呴崣澶岀瘈闂傚牊绋掑婵嬫煟濠靛﹤鐓愰柟渚垮妼椤啰鎷犻煫顓烆棜闂?chatId={raw} 闂傚倸鍊峰ù鍥敋瑜嶉湁闁绘垼妫勯弸渚€鏌熼梻瀵割槮缂佺姷濞€閺岀喖鎮欓鈧崝璺衡攽椤旇棄鈻曢柡宀嬬磿娴狅妇鎷犻幓鎺戭潥闂備胶顭堟鍝ョ矓瑜版帒钃熸繛鎴欏灩閸楁娊鏌曟繛鍨姎缂佺姴缍婂娲川婵犲嫭鍣ч梺鍝ュ枑婢瑰棗危?婵犵數濮烽。钘壩ｉ崨鏉戠；闁糕剝蓱濞呯姵淇婇妶鍛櫣闁搞劌鍊块弻鐔虹矙閹稿孩宕冲銈傛櫆瑜板啳鐏冮梺鎸庣箓閹冲酣寮抽悙鐑樼厸闁告粈绀佹晶鎾煛鐏炲墽娲存い銏℃礋椤㈡宕掑┃鐑囩秮濮婃椽骞栭悙娴嬪亾濡ゅ懎纾婚柣鏂垮悑閸庡銇勮箛鎾跺⒈闁轰礁娲ㄩ幉绋款吋婢跺﹤鍤戦梺鎸庢煥椤洘绂嶅鍫熺厵闁告挆鍕畬闂佸憡鑹剧紞濠囧蓟閻斿壊妲归幖绮规閺嬪懏绻濈喊澶岀？闁稿繑蓱娣囧﹪骞栨担鑲濄劍銇勯弬鍨缓闁绘柨鍚嬮埛鎺懨归敐鍛暈闁哥喓鍋ら弻锝夋偄閺夋垵濮﹂悗瑙勬礃閻撯€愁嚕婵犳艾唯闁靛／灞拘熼梻鍌欐祰濞夋洟宕伴幘瀛樺弿閻庨潧鎽滈惌鍡楊熆閼搁潧濮堥柣鎾跺枑娣囧﹪顢涘┑鎰缂備浇灏畷鐢稿焵椤掍緡鍟忛柛鐘愁殕缁绘稒绻濋崶褑鎽曢梺鎸庣箓椤︻垶鎮″☉妯锋斀闁绘ɑ褰冮埀顒€缍婇幃妯侯吋婢跺鎷虹紓鍌欑劍椤洦绔熼崟顓犵＝鐎广儱瀚粣鏃傗偓?, null);
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
                return (false, "闂傚倸鍊搁崐椋庣矆娓氣偓楠炴牠顢曢敃鈧悿顕€鏌ｅΔ鈧悧濠囧矗韫囨稒鐓熼柕蹇曞Х娴犳盯鏌涜箛鏃傜煉闁哄本鐩獮姗€鎳犻鈧俊浠嬫⒑閸濄儱孝婵☆偅绻堝濠氬Ω閵夈垺鏂€闂佺硶鍓濋敃鈺佄涢妶澶嬧拺闂侇偆鍋涢懟顖炲储閸濄儳纾兼い鏃傛櫕閹冲洦顨ラ悙鍙夘棦鐎规洜鍠栭、娑㈡晲閸℃ɑ鐝﹂梻鍌欐缁鳖喚寰婃禒瀣殣妞ゆ牜鍋為崐鍫曞级閸碍娅囩痪鍙ョ矙閺屾稓浠﹂崜褜鏆￠梺绋胯閸旀垿寮诲☉娆戠瘈闁告劗鍋撻悾濂告⒑?婵犵數濮烽。钘壩ｉ崨鏉戠；闁糕剝蓱濞呯姵淇婇妶鍛櫣闁搞劌鍊块弻鐔虹矙閹稿孩宕冲?, null);

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
            var msg = string.IsNullOrWhiteSpace(details) ? summary : $"{summary}闂傚倸鍊搁崐鐑芥倿閿旈敮鍋撶粭娑樻噽閻瑩鏌熼悜姗嗘闁轰礁妫涚槐鎺楀棘閸喗缍巘ails}";
            return (false, msg, null);
        }
    }

    /// <summary>
    /// 闂傚倸鍊搁崐椋庣矆娓氣偓楠炲鏁嶉崟顐ｇ€抽悗骞垮劚椤︻垶宕归崒鐐寸厽闁靛繈鍩勯悞鍓х磼閳ь剟宕熼娑氬弳濠电偞鍨堕敋妞ゅ浚浜弻娑㈡偐閸欏妫﹂梺鍝勮閸斿矂鍩為幋锕€骞㈡慨妤€妫欓敓銉╂⒒娓氣偓閳ь剛鍋涢懟顖炲储閸濄儳纾兼い鏃傛櫕閹冲洦顨ラ悙鍙夘棦鐎规洘锕㈤、娆撴嚃閳哄﹥效濠碉紕鍋戦崐鏍箰妤ｅ啫纾绘繛鎴烇供閸ゆ洘銇勯弬鍨挃缁?婵犵數濮烽。钘壩ｉ崨鏉戠；闁糕剝蓱濞呯姵淇婇妶鍛櫣闁搞劌鍊块弻鐔虹矙閹稿孩宕冲銈傛櫆瑜板啳鐏冮梺鎸庣箓閹冲酣寮抽悙鐑樼厱閹肩补鈧剚鍔夐梺闈涙搐鐎氭澘顕ｉ鈧畷鎺戔槈濞嗘垵娑ч梻鍌欑劍閹爼宕濆畝鍕ч柟闂寸閽冪喐绻涢幋鐐冩岸寮告惔銊︾厵闂侇叏绠戞晶鏉棵归悡搴ｇ劯婵﹥妞介弻鍛存倷閼艰泛顏梺鍛娚戦幃鍌炲蓟閿濆绠抽柣鎰暩閺嗐倝鎮楀▓鍨灍闁诡喖鍊搁悾宄邦潨閳ь剟銆佸▎鎾村亗閹艰揪绲界紞浣糕攽閿涘嫬浜奸柛濠冪墱閺侇喗绻濋崶銊ユ畱闁荤姾娅ｉ崕銈呅掗崟顖涒拻闁稿本鐟︾粊鐗堛亜閺囩喓澧电€规洩绲鹃幆鏃堝Ω閵夈儳鈧厼顪冮妶鍡楃瑨閻庢凹鍓熼幃?    /// </summary>
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
                return (false, "濠电姷鏁告慨鐑藉极閹间礁纾婚柣鎰惈閸ㄥ倿鏌ｉ姀鐘冲暈闁稿顑呴埞鎴︽偐閹绘帗娈銈嗘礋娴滃爼寮诲☉妯锋婵炲棙鍔楃粙鍥╃磽娴ｆ彃浜鹃梺鍛婂姦閸犳鎮￠弴鐔虹闁瑰瓨绻傞懜鍦偓娈垮櫍缁犳牠寮诲☉銏″亜闂佸灝顑嗛幃娆撴⒑鐎圭姵顥夋い锔炬暬閻涱喖螣閸忕厧鏋傞梺鍛婃礀閸氣偓闁告繃顨婂?, null);

            var client = await GetOrCreateConnectedClientAsync(accountId, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            var sent = await client.SendMessageAsync(target.Peer, text, null, replyToMessageId ?? 0);
            return (true, null, sent.id);
        }
        catch (Exception ex)
        {
            var (summary, details) = MapTelegramException(ex);
            var msg = string.IsNullOrWhiteSpace(details) ? summary : $"{summary}闂傚倸鍊搁崐鐑芥倿閿旈敮鍋撶粭娑樻噽閻瑩鏌熼悜姗嗘闁轰礁妫涚槐鎺楀棘閸喗缍巘ails}";
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
                return (false, $"缂傚倸鍊搁崐鎼佸磹閻戣姤鍊块柨鏂垮⒔閻瑩鏌熷▎陇顕уú顓€佸☉姗嗙叆闁告洟娼ф禒娲⒒娴ｈ櫣甯涢柡灞诲妽缁旂喐绻濋崒妤佺亖闂佷紮绲芥径鍥磻閹捐绀傚璺猴梗婢规洟姊绘担鍛婂暈闁告棑闄勭粋宥呪攽鐎ｅ墎绋忛悗骞垮劚閹冲寮ㄦ禒瀣厽闁归偊鍘界紞鎴︽煟韫囧鍔﹂柡宀嬬到铻栭柍褜鍓熼弫鍐Ψ瑜夐崑鎾绘濞戞牕浠悗瑙勬磸閸旀垿銆佸☉妯锋婵炲棗绻愭慨搴ㄦ⒒閸屾瑧顦﹂柟娴嬪墲缁楃喎螖閸涱厾鐛ュ┑掳鍊曢幆銈夊炊椤忓秵鈻岄梻浣告惈鐞氼偊宕濆畝鍕叀濠㈣泛谩閻斿吋鍤掗柕鍫濐槹鐎氱但imeoutSeconds} 缂傚倸鍊搁崐鎼佸磹妞嬪海鐭嗗〒姘ｅ亾閽樻繈鏌熷▓鍨灍闁哄棙绮嶉妵鍕疀閹炬惌妫″?, null);

            if (messageFilter != null
                && stopOnUnmatchedMention
                && !messageFilter(update)
                && IsMentionOrReply(update, currentUsername, sentMessageId))
            {
                return (false, "婵犵數濮撮惀澶愬级鎼存挸浜炬俊銈勭劍閸欏繘鏌熺紒銏犳灍闁稿孩顨呴妴鎺戭潩閿濆懍澹曢梻浣筋嚃閸垶鎮為敃鈧銉╁礋椤掑倻顔曢梺缁樺姦閸擄箑顭囬弮鍫熲拻濞达絿顭堥ˉ蹇涙煕鐎ｎ亝顥㈢€规洘婢橀濂稿炊椤垶缍楅梻浣告惈缁嬩線宕㈡禒瀣亗婵炴垯鍨洪悡鏇熴亜閹伴潧浜滃ù婊勵殘缁辨帡濡搁妷顔惧悑闂佸搫鐬奸崰鏍ь嚕椤掑嫬唯闁靛鍎幏銈囩磽閸屾瑧顦︽い鎴濇嚇钘濋柣銏℃偠閳ь兛绀侀～婊堝焵椤掑嫬绠栨繛鍡樻尭缁犵敻鏌熼悜妯烩拻闁宠棄顦靛濠氬磼濮橆兘鍋撻悜鑺ュ€块柨鏇炲亞閺佸嫭绻涢崱妯诲碍缂佹劖顨嗙换娑㈠箣濞嗗繒浠鹃梺?濠电姷鏁告慨鐢割敊閺嶎厼绐楁俊銈呭暞瀹曟煡鏌熼柇锕€鏋涚紒韬插€曢湁闁绘ê妯婇崕蹇涙煕閵娿儱鈧綊濡甸崟顖氬唨妞ゆ劦婢€濞岊亪姊洪崫鍕闁告挾鍠栭獮鍐潨閳ь剟骞冨▎鎾村殝婵炲牊瀵чˉ澶愭⒒娴ｈ鍋犻柛濠冪墱閺侇噣鏁撻悩鑼暫閻熸粎澧楃敮鎺撳閻樼粯鐓曢柡鍥ュ妼楠炴鏌ｆ惔顔兼灓缂?, null);
            }

            var candidate = await BuildVerificationCandidateAsync(
                client,
                update.Message,
                currentUsername,
                sentMessageId,
                cancellationToken);

            return candidate == null
                ? (false, "闂傚倸鍊搁崐椋庣矆娓氣偓楠炲鏁撻悩鍐蹭罕闂佸搫娲ㄦ慨椋庣矆婵犲倵鏀介柣妯哄级閹兼劙鏌涚仦璇插闁哄本娲熷畷鐓庘攽閹邦厜锔剧磽娴ｆ彃浜鹃梺绯曞墲缁嬫帡鎮″▎鎰闁割偅绻勬禒銏ゆ煛鐎ｎ剙鏋涢柡宀嬬秮閺佹劙宕ㄩ鈥崇厴闂佸墽绮悧鐘诲蓟閿濆憘鐔煎垂椤旂偓顕楅梻浣虹帛鐢偤宕楀鈧濠氭晲婢跺娅囬梺閫炲苯澧撮挊婵囥亜閺嶎偄浠滈柣鎾偓鎰佺唵闁兼悂娼ф慨鍥⒑閸楃偞鍠橀柡宀嬬秮瀵剟宕归鍛缂傚倷璁查崑鎾绘煃瑜滈崜娆撳煘閹达附鍊烽柡澶嬪灩娴犳悂姊鸿ぐ鎺嗗亾濞戞凹妲梺瀹狀嚙缁夊綊寮幇顓炵窞濠电姴瀚獮瀣⒒娴ｅ憡鎯堢紒瀣╃窔瀹曘垺绂掔€ｎ亞鏌у銈嗗笒鐎氼參宕愰崼鏇熺厽闁规壆澧楀☉褍霉濠婂牏鐣洪柡灞诲姂閹垽寮堕幋婵嗩潥缂傚倷鑳剁划顖滅矙閹烘埈鍤楅柛鏇ㄥ幐閸嬫捇鏁愭惔鈥茬敖濡炪倧璐熼崝宥囨崲濠靛鐓曢柍褜鍓熷畷銊╊敊閻ｅ苯娑ч梻?AI 闂傚倸鍊峰ù鍥х暦閸偅鍙忛柡澶嬪殮濞差亜围闁告稑鍊圭粙鎴ｇ亙闂佸憡渚楅崢钘壩?, null)
                : (true, null, candidate);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            var (summary, details) = MapTelegramException(ex);
            var msg = string.IsNullOrWhiteSpace(details) ? summary : $"{summary}闂傚倸鍊搁崐鐑芥倿閿旈敮鍋撶粭娑樻噽閻瑩鏌熼悜姗嗘闁轰礁妫涚槐鎺楀棘閸喗缍巘ails}";
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
                return (false, "闂傚倸鍊搁崐椋庣矆娴ｉ潻鑰块梺顒€绉埀顒婄畵瀹曞ジ濡风€ｎ亝鍠橀柟顔炬櫕缁瑧鎹勯…鎴濇暪闂佽姘﹂～澶娒洪弽顬℃椽濡歌椤ユ艾霉閻樺樊鍎愰柣鎾存礃缁绘繈妫冨☉娆樻濡炪倕娴氶崑鍛村箞?callback_data");

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
            var msg = string.IsNullOrWhiteSpace(details) ? summary : $"{summary}闂傚倸鍊搁崐鐑芥倿閿旈敮鍋撶粭娑樻噽閻瑩鏌熼悜姗嗘闁轰礁妫涚槐鎺楀棘閸喗缍巘ails}";
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

        if (ContainsAny(text, "闂傚倸鍊搁崐鐑芥倿閿曞倸绠栭柛顐ｆ礀缁€澶屸偓鍏夊亾闁告洦鍋嗛弻鍫濐渻閵堝棗濮€闁瑰啿娲銊╂晝閸屾稓鍘遍梺闈涱樈閸犳牗鏅堕幍顔瑰亾鐟欏嫭灏紒鑸靛哺瀵鈽夐埗鈹惧亾閿曞倸绠ｆ繝闈涙川娴滃爼姊?, "濠电姴鐥夐弶搴撳亾濡や焦鍙忛柣鎴ｆ绾剧粯绻涢幋娆忕仾闁稿﹨鍩栫换婵嬫濞戝崬鍓扮紓?, "婵犵數濮烽弫鎼佸磻閻愬搫鍨傞柛顐ｆ礀缁犱即鏌涘┑鍕姢闁活厽鎸鹃埀顒冾潐濞叉牕煤閳哄倹鍙忛柟鎯板Г閻撱儲绻濋棃娑欘棡妞ゃ儲绮庣槐鎺楀礈娴ｉ晲妲愰梺鍝勬湰閻╊垰顕ｉ幘顔嘉╅柕澶堝劤椤旀垿姊?, "闂傚倷娴囬褍霉閻戣棄绠犻柟鎹愵嚙缁犵喖姊介崶顒€桅闁圭増婢樼粈鍐┿亜閺冨倸甯堕柣搴弮閹嘲顭ㄩ崨顓ф毉闁汇埄鍨遍〃濠囧春?, "闂傚倸鍊风粈渚€骞栭位鍥敃閿曗偓閻ょ偓绻濇繝鍌涘櫤鐎规洘鐓￠弻娑㈠焺閸愩劌鏋欑紓?, "闂傚倸鍊峰ù鍥敋瑜忛幑銏ゅ箛椤旇棄搴婇柣搴秵閸犳宕甸崟顖涚厱妞ゆ劧绲剧壕鎼佹煃?)
            && !ContainsAny(text, "婵犵數濮撮惀澶愬级鎼存挸浜炬俊銈勭劍閸欏繘鏌熺紒銏犳灍闁稿孩顨呴妴鎺戭潩閿濆懍澹曢梻?, "婵犵數濮撮惀澶愬级鎼存挸浜炬俊銈勭劍閸欏繘鏌熺紒銏犳灍闁稿孩顨呴妴鎺戭潩閿濆懍澹曢梻浣筋嚃閸垶鎮為敃鈧銉╁礋椤栨氨鐤€濡炪倖鎸鹃崑娑欑珶?, "闂傚倸鍊搁崐椋庣矆娓氣偓楠炴牠顢曢妶鍥╃厯闂佸憡娲﹂崢钘夌暦閸欏绡€闂傚牊渚楅崕鎰版煕?, "captcha"))
        {
            return false;
        }

        if (ContainsAny(text,
                "婵犵數濮撮惀澶愬级鎼存挸浜炬俊銈勭劍閸欏繘鏌熺紒銏犳灍闁稿孩顨呴妴鎺戭潩閿濆懍澹曢梻?,
                "婵犵數濮撮惀澶愬级鎼存挸浜炬俊銈勭劍閸欏繘鏌熺紒銏犳灍闁稿孩顨呴妴鎺戭潩閿濆懍澹曢梻浣筋嚃閸垶鎮為敃鈧銉╁礋椤栨氨鐤€濡炪倖鎸鹃崑娑欑珶?,
                "闂傚倸鍊搁崐椋庣矆娓氣偓楠炴牠顢曢妶鍥╃厯闂佸憡娲﹂崢钘夌暦閸欏绡€闂傚牊渚楅崕鎰版煕?,
                "闂傚倸鍊峰ù鍥х暦閸偅鍙忛柡澶嬪殮濞差亜惟闁宠桨鑳堕ˇ褍鈹戦濮愪粶闁稿鎹囬弻娑㈠煘閹傚濠碉紕鍋戦崐鏍ь啅婵犳艾纾婚柟鐐暘娴滄粓鏌ㄩ弮鍥棄妞ゃ儱顑夐弻?,
                "闂傚倸鍊搁崐鐑芥嚄閸撲礁鍨濇い鏍仜缁€澶嬩繆閵堝懏鍣圭紒鐘靛█閺岀喖骞戦幇闈涙闂?,
                "闂傚倸鍊搁崐椋庣矆娴ｉ潻鑰块梺顒€绉埀顒婄畵瀹曞ジ濡风€ｎ亝鍠橀柟顔炬櫕缁瑧鎹勯…鎴濇暪?,
                "闂傚倸鍊峰ù鍥敋瑜嶉湁闁绘垼妫勭粻鐘绘煙閹规劦鍤欓悗姘槹閵囧嫰骞掗幋婵愪患闂佹悶鍔岄崐褰掑箞閵娿儺娼ㄩ柛鈩冾殔鐎涳絽鈹戦悙鑼ⅱ闁哄拋鍋婇崺鈧い鎺嗗亾闁告ɑ绮撳畷鎴﹀箻缂佹鍘遍梺瑙勬緲閸氣偓缂併劌鍚嬮妵?,
                "闂傚倸鍊峰ù鍥х暦閸偅鍙忛柡澶嬪殮濞差亜围濠㈣泛锕ょ花銉╂⒑閸︻厾甯涚€规洍鈧秮娲敂閸曨偄鏁ら梻渚€娼ч…鍫ュ磿瀹曞洨鐜?,
                "缂傚倸鍊搁崐鎼佸磹閻戣姤鍊块柨鏂垮⒔閻瑩鏌熼悜妯虹劸婵炲吋鐗滈幉鎼佹偋閸繄顏婚梺?,
                "缂傚倸鍊搁崐鎼佸磹閻戣姤鍤勯柤绋跨仛閸欏繘鏌ｉ姀鈩冨仩闁逞屽墮閸熸潙鐣烽妸鈺婃晬婵炴垼椴哥紞?,
                "缂傚倸鍊搁崐鎼佸磹閻戣姤鍊块柨鏂垮⒔閻瑩鏌熷▎陇顕уú顓€佸▎鎾崇闁归偊鍎烽垾鎰佹富闁靛牆妫楃粭鍌滅磼閳ь剚鎷呴悜姗嗗仺闂佺粯鍔曢悺銊╁矗韫囨梹鍙忔俊銈傚亾婵☆偅顨嗛弲鍫曟偋閸喎寮?,
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
               || text.Contains("婵?, StringComparison.Ordinal)
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
            throw new ArgumentException("闂傚倸鍊搁崐鎼佸磹妞嬪海鐭嗗ù锝堫潐濞呯姴霉閻樺樊鍎忕痪鎯ь煼閺岀喖骞戦幇顒傚帿闂?闂傚倸鍊搁崐鐑芥倿閿曗偓椤啴宕归鍛姺闂佺鍕垫當缂佲偓婢跺备鍋撻獮鍨姎妞わ富鍨跺浼村Ψ閿斿墽顔曢梺鐟扮摠閻熴儵鎮橀埡鍛仭婵炲棙鐟ラ々顒勬煃瑜滈崜娆戠不瀹ュ纾块弶鍫氭杹閸嬫挸顫濋悡搴ｄ化缂備緡鍠栭悧蹇涘焵椤掑﹦绉甸柛鎾寸〒婢?, nameof(input));

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

        // 闂傚倸鍊搁崐鐑芥嚄閸洖纾块柣銏㈩焾閻ょ偓绻涢幋娆忕仾闁稿鍊濋弻鏇熺箾瑜嶇€氼厼鈻撴导瀛樷拺闁革富鍙€濡炬悂鏌涢悩宕囧⒌鐎?t.me/xxx
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
            throw new ArgumentException("Bot 闂傚倸鍊搁崐鐑芥倿閿曗偓椤啴宕归鍛姺闂佺鍕垫當缂佲偓婢跺备鍋撻獮鍨姎妞わ富鍨跺浼村Ψ閿斿墽顔曢梺鐟扮摠閻熴儵鎮橀埡鍛仭婵炲棙鐟ラ々顒勬煃瑜滈崜娆戠不瀹ュ纾块弶鍫氭杹閸嬫挸顫濋悡搴ｄ化缂備緡鍠栭悧蹇涘焵椤掑﹦绉甸柛鎾寸〒婢?, nameof(input));

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
                throw new ArgumentException("Bot 闂傚倸鍊搁崐鎼佸磹妞嬪海鐭嗗ù锝堫潐濞呯姴霉閻樺樊鍎忕痪鎯ь煼閺岀喖骞戦幇顒傚帿闂佸搫妫欑划鎾诲蓟閻旇櫣纾奸柕蹇曞У閻忓牓姊洪崨濠傚闁稿鎹囧缁樻媴閾忕懓绗￠梺鐟版憸椤牓婀佹俊鐐差儏鐎涒晠宕楀鍫熺厓鐟滄粓宕滈悢濂夋綎婵炲樊浜滄导鐘绘煕閺囥劌浜濇繛鍫ｅ煐缁绘繂鈻撻崹顔界亾闂佽桨娴囬褔顢?, nameof(input));

            var path = (uri.AbsolutePath ?? string.Empty).Trim('/');
            var firstSeg = path.Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(firstSeg))
                throw new ArgumentException("Bot 闂傚倸鍊搁崐鎼佸磹妞嬪海鐭嗗ù锝堫潐濞呯姴霉閻樺樊鍎忕痪鎯ь煼閺岀喖骞戦幇顒傚帿闂佸搫妫欑划鎾诲箖濡も偓閳藉鈻庡Ο鐓庡Ш闂備礁纾划顖氼潖瑜版帒桅闁告洦鍨扮粻濠氭偣閾忚纾柕蹇嬪灮绾惧ジ鎮规担鍝ワ紞缂佹劖妫冮弻鐔碱敊缁涘鍔哥紓浣哄У閻╊垰顕ｉ鍌涘磯闁靛鍊栭崑鍛存⒒閸屾瑦绁版俊妞煎姂濮婅棄顓兼径濠勶紮濠电娀娼ч鍥х暤娓氣偓閹鏁愭惔婵堟晼闂佸搫妫楅澶愬蓟閿濆憘鏃堝焵椤掑嫭鍋嬮柛鈩冪☉閻?, nameof(input));

            s = firstSeg;

            var query = ParseQueryString(uri.Query);
            if (query.TryGetValue("start", out var start) && !string.IsNullOrWhiteSpace(start))
                startFromLink = NormalizeBotStartParameter(start);
        }

        s = s.Trim().TrimStart('@');

        // 闂傚倸鍊搁崐宄懊归崶顒€违闁逞屽墴閺屾稓鈧綆鍋呯亸浼存煏閸パ冾伃鐎殿喕绮欐俊姝岊槷婵℃彃鐗撳鐑樺濞嗗繒妲ｉ梺闈╃秶缂嶄礁顕ｆ繝姘╅柕澶堝灪椤秴鈹戦悙鍙夘棡闁挎岸鏌涢幘棰濈唫sername?start=abc闂傚倸鍊搁崐鐑芥倿閿旈敮鍋撶粭娑樻噽閻瑩鏌熸潏楣冩闁稿顑呴埞鎴︽偐闊叀鎶?http/tg 闂傚倸鍊搁崐椋庣矆娓氣偓楠炲鏁撻悩顔瑰亾閸愵喖宸濇い鏍ㄧ☉鎼村﹤鈹戞幊閸婃洟骞忕€ｎ喖鏋佸┑鐘叉处閻撴洘绻涢幋鐑嗙劷闁圭晫濞€閺?        var question = s.IndexOf('?');
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
            throw new ArgumentException("Bot 闂傚倸鍊搁崐鐑芥倿閿曗偓椤啴宕归鍛姺闂佺鍕垫當缂佲偓婢跺备鍋撻獮鍨姎妞わ富鍨跺浼村Ψ閿斿墽顔曢梺鐟扮摠閻熴儵鎮橀埡鍛仭婵炲棙鐟ラ々顒勬煃瑜滈崜娆戠不瀹ュ纾块弶鍫氭杹閸嬫挸顫濋悡搴ｄ化缂備緡鍠栭悧蹇涘焵椤掑﹦绉甸柛鎾寸〒婢?, nameof(input));

        if (s.StartsWith("+", StringComparison.Ordinal))
            throw new ArgumentException("闂傚倸鍊搁崐鎼佸磹妞嬪孩顐介柨鐔哄Т閸屻劑鏌熼梻瀵割槮缁炬儳顭烽弻鐔煎礈瑜忕敮娑㈡煕鐎ｅ墎绉柡灞剧洴婵＄兘濮€閳╁啰褰呴梻浣告啞钃遍柟鍛婂▕瀵鎮㈤崗鐓庣檮婵犮垼娉涢悧鍡樼椤斿槈鏃堟偐闂堟稐娌柣銏╁灙閸撴繃绌辨繝鍥ㄥ€婚柦妯猴級瑜旈弻娑㈠Ψ閵忊剝鐝曢梺鍝ュУ鐢€愁潖?Bot 闂傚倸鍊搁崐鐑芥倿閿曗偓椤啴宕归鍛姺闂佺鍕垫當缂佲偓婢跺备鍋撻獮鍨姎妞わ富鍨跺浼村Ψ閿斿墽顔曢梺鐟扮摠閻熴儵鎮橀埡鍛仭婵炲棙鐟ラ々顒勬煃瑜滈崜娆戠不瀹ュ纾块弶鍫氭櫆瀹曟煡鏌熼悜妯烩拻闁活厼妫楅…鍧楁嚋闂堟稑顫嶉梺绋款儜缂嶄線寮诲☉銏犖ㄩ柕蹇婂墲閻濇洟姊虹紒妯虹婵炲娲滃Σ鎰板箳閹垮嫮鐭楀┑鐘绘涧濡鐚惧澶嬪€?@xxxbot 闂?t.me/xxxbot", nameof(input));

        if (!System.Text.RegularExpressions.Regex.IsMatch(s, "^[A-Za-z0-9_]{5,64}$"))
            throw new ArgumentException("Bot 闂傚倸鍊搁崐鐑芥倿閿曗偓椤啴宕归鍛姺闂佺鍕垫當缂佲偓婢跺备鍋撻獮鍨姎妞わ富鍨跺浼村Ψ閿斿墽顔曢梺鐟扮摠閻熴儵鎮橀埡鍛仭婵炲棙鐟ラ々顒勬煃瑜滈崜娆戠不瀹ュ纾块柛妤冨€ｅ☉妯锋婵炲棙鍨硅ぐ楣冩⒑瑜版帒浜伴柛鐘冲浮瀹曟垿骞橀弬銉︾亖闂佸壊鐓堥崰妤呮倶閸垻纾藉ù锝堟鐢盯鏌涢妸銈囩シ闁诲繐顑夊娲濞戞艾顣哄銈忓瘜閸ㄨ泛顕?, nameof(input));

        // 闂傚倸鍊烽悞锕傛儑瑜版帒鏄ラ柛鏇ㄥ灠閸ㄥ倿姊洪鈧粔鐢稿磻閵堝鐓涢柛銉ㄥ煐缁舵稓绱撳鍡欏ⅹ妞ゎ厼娼￠幊婊堟濞戞﹩娼撶紓鍌欒兌婵潧鐣濋幖浣歌摕婵炴垶菤濡插牓鏌涘Δ鍐ㄤ粶缂佺姴缍婂娲传閸曢潧鍓伴梺娲诲幖閸婂灝顕ｆ繝姘╅柕澶堝灪椤秴鈹戦悙鍙夘棞缂併劏鍋愰埀顒佺▓閺呮盯鈥旈崘顔嘉ч柛灞剧⊕閻濄劎绱撴担鍝勑ｉ柛銊ョ埣瀵煡濡烽埡鍌楁嫼缂備礁顑嗙€笛冿耿娴煎瓨鐓熼柣鏃€娼欓埀顒侇殜閹?bot 缂傚倸鍊搁崐鎼佸磹閹间礁纾归柣鎴ｅГ閸婂潡鏌ㄩ弴鐐测偓鍝ョ不娴煎瓨鐓欓梻鍌氼嚟閸斿秹鏌?        // 婵犵數濮烽弫鎼佸磻閻愬搫绠板┑鐘崇閸ゅ苯螖閿濆懎鏋ら柡浣稿閺屾稑鈽夊▎妯哄Π闂佹悶鍎崝搴ｅ姬閳ь剙鈹戦鏂や緵闁告妫勯埢?        // 1) 闂傚倸鍊搁崐椋庣矆娓氣偓楠炴牠顢曢妶鍌氫壕婵鍘у顔锯偓瑙勬礃濞叉牠鎮鹃悜钘夌倞妞ゎ厽鍨剁紞妤呮⒒娴ｇ懓顕滅紒瀣灩閳ь剚鍑归崰姘珶閺囥垹绀傞柤娴嬫櫇椤旀洟姊虹化鏇炲⒉妞ゃ劌妫濋獮鎴︽晲婢跺鍘?start 闂傚倸鍊搁崐椋庣矆娓氣偓楠炲鏁撻悩鍐蹭画闂侀潧顦弲娑氬閸︻厽鍠愰柣妤€鐗嗙粭鎺撴叏鐟欏嫮鍙€闁哄矉缍佸顒勫垂椤旇棄鈧垶姊洪崫鍕闁告挾鍠栧璇测槈閵忊晜鏅濋梺鎸庣箓濞层劑鎮炬總鍛娾拺缂侇垱娲樺▍鍡樸亜閵娿儻韬鐐插暙閻ｏ繝骞嶉搹顐も偓濠氭煙閸忚偐鏆橀柛鈺佸铻ｉ柛灞剧矌绾?t.me/xxx?start=abc 闂?@xxx?start=abc闂?        // 2) 闂傚倸鍊峰ù鍥х暦閸偅鍙忛柟缁㈠櫘閺佸嫰鏌涘☉娆愮稇闁汇値鍠栭湁闁稿繐鍚嬬紞鎴︽煛鐎ｂ晝绐旈柡灞炬礋瀹曠厧鈹戦幇顓夛箑鈹戦埥鍡椾簼闁挎洏鍨介獮鍐ㄧ暋閹佃櫕鐎婚棅顐㈡处閹尖晜绂掗幆褜娓婚柕鍫濋娴滄繈鏌ｅΔ浣虹煉闁诡垪鍋撳銈呯箰閻楁粓寮崶鈺傚枑鐎广儱顦崙鐘绘煙閸撗呭笡闁绘挸鍟伴幉绋款煥閸繄顦┑顔矫畷顒勫垂濠靛牃鍋撻獮鍨姎婵炶尙濞€瀹?Bot 婵犵數濮烽弫鍛婃叏娴兼潙鍨傞柣鎾崇岸閺嬫牗绻涢幋鐐茬劰闁稿鎸搁～婵嬫偂鎼淬垻褰庢俊銈囧Х閸嬫盯宕婊勫床婵犻潧顑呴悙濠勬喐韫囨柨顕?        if (!s.EndsWith("bot", StringComparison.OrdinalIgnoreCase)
            && string.IsNullOrWhiteSpace(startFromLink)
            && !assumeBotUsername)
            throw new ArgumentException("闂傚倸鍊搁崐鐑芥嚄閸洖纾块柣銏㈩焾閻ら箖鏌嶉崫鍕櫣缂佹劖顨嗘穱濠囧Χ閸涱喖娅ら梺绋款儏椤︾敻寮婚妸銉㈡斀闁糕檧鏅滄晥闂備胶顭堢€涒晠宕归崸妤€绠栨俊銈呮噺閺呮煡骞栫划鍏夊亾瀹曞浂鍟堥梺璇叉唉椤煤濡崵绠惧┑鐘叉搐閽冪喖鏌嶉埡浣告殭缂佸墎鍋ら弻娑㈩敃閿濆洨鐣垫繝娈垮枛缁夊墎妲愰幘瀛樺閻犲洠鍓濋悾璺侯渻閵堝繒鐣垫繛浣冲浂鏁?Bot 闂傚倸鍊搁崐鐑芥倿閿曗偓椤啴宕归鍛姺闂佺鍕垫當缂佲偓婢跺备鍋撻獮鍨姎妞わ富鍨跺浼村Ψ閿斿墽顔曢梺鐟扮摠閻熴儵鎮橀埡鍛仭婵炲棙鐟ラ々顒勬煃瑜滈崜娆戠不瀹ュ纾块弶鍫氭櫆瀹曟煡鏌熼悜妯烩拻闁活厼妫濋弻娑㈠即閵娿儳浠梺鎶芥敱鐢繝寮诲☉銏犖ㄦい鏍ㄧ矊閸╁本绻涚€涙鐭岄柛瀣尵閹?bot 缂傚倸鍊搁崐鎼佸磹閹间礁纾归柣鎴ｅГ閸婂潡鏌ㄩ弴鐐测偓鍝ョ不娴煎瓨鐓欓梻鍌氼嚟閸斿秹鏌涚€ｎ亜顏柕鍥у楠炴帡骞嬪┑鎰棯闂?, nameof(input));

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
    /// 闂傚倸鍊搁崐椋庣矆娓氣偓楠炴牠顢曢埛姘そ婵¤埖寰勭€ｎ亙妲愰梻渚€娼ц墝闁哄懏鐩幏鎴︽偄鐏忎焦鏂€闂佺粯锚瀵爼宕冲畡鎳婄懓顭ㄩ崘顏喰ㄩ梺鍝勭灱閸犳牠鐛崱姘兼Щ闂佸搫妫滄ご鎼佸Φ閸曨垰围闁告侗鍠栧▓妤呮⒑鐠団€虫灈缂傚秴锕悰顕€宕堕鈧粈鍫澝归敐鍥у妺婵炲牊鎮傞弻锝夋偄閸濄儳鐓傛繝鈷€鍕垫畼闁逞屽墰閻熸娊宕橀幆褎顔傞梻浣告啞濞诧箓宕归幍顔炬／鐟滄棃寮婚悢琛″亾濞戞瑯鐒界紒鐘虫尭椤法鎲撮崟顓炩拰濠殿喖锕ュ浠嬬嵁閹邦厽鍎熼柨婵嗗€归～宥夋⒒娴ｈ櫣甯涙い銊ユ楠炴劙骞栨担娴嬪亾閿曞倸鐐婃い鎺嗗亾閸ュ瓨绻濋姀锝嗙【闁挎洩绠撻幃鐐綇閵娧咁啎闁哄鐗嗘晶浠嬪礆娴煎瓨鐓欓悹鍥囧懐锛熺紓渚囧枛椤戝鐣锋總绋课ㄩ柨鏃€鍎抽獮妤佺節瀵伴攱婢橀埀顒佸姍瀹曟垿骞樼€靛摜顔曢梺鍝勵槹閸ㄧ敻顢旈锝勭箚闁告瑥顦慨鍥ㄣ亜椤愶絿绠橀柟鐟板閹?    /// </summary>
    public async Task<(bool Success, string? Error)> UpdateProfilePhotoAsync(
        int accountId,
        Stream fileStream,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (fileStream == null)
                return (false, "婵犵數濮烽弫鍛婃叏娴兼潙鍨傚┑鍌滎焾閺勩儵鏌″畵顔肩灱閸斿嘲鈹戦悙鍙夘棡闁圭鎽滄竟鏇㈠箰鎼淬垹寮垮┑鈽嗗灡鐎笛呮兜閻愵兛绻嗘い鎰剁秶閼板潡鏌＄仦鍓ф创濠碉紕鍏橀、娑㈡倷閹碱厸鍋撳鍜佹富闁靛牆妫楁慨灞句繆椤愩垹顏柛鈹垮劜瀵板嫰骞囬鍌ゅ晪闂佽崵鍠愰悷銉р偓姘舵敱缁傛帡鍩￠崨顔规嫼?);

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

            throw new TimeoutException($"Telegram request timed out: {operation} exceeded {timeout.TotalSeconds:0} seconds.");
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

            throw new TimeoutException($"Telegram request timed out: {operation} exceeded {timeout.TotalSeconds:0} seconds.");
        }
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
        var title = $"tp-check-{DateTime.UtcNow:MMddHHmmss}";
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
                var input = new InputChannel(channel.id, channel.access_hash);
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