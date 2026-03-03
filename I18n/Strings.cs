using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace BloodshedModToolkit.I18n
{
    /// <summary>
    /// 치트 메뉴 UI 문자열 집합.
    /// 프로퍼티 이름이 곧 TSV 키 이름이므로 오타에 주의.
    /// </summary>
    public sealed class LangStrings
    {
        private readonly Dictionary<string, string> _d;

        internal LangStrings(Dictionary<string, string> d) => _d = d;

        private string G(string key)
            => _d.TryGetValue(key, out var v) && v.Length > 0 ? v : $"[{key}]";

        public string GodMode            => G(nameof(GodMode));
        public string InfiniteGems       => G(nameof(InfiniteGems));
        public string InfiniteSkullCoins => G(nameof(InfiniteSkullCoins));
        public string MaxStats           => G(nameof(MaxStats));
        public string SpeedHack          => G(nameof(SpeedHack));
        public string OneShotKill        => G(nameof(OneShotKill));
        public string NoCooldown         => G(nameof(NoCooldown));
        public string InfiniteRevive     => G(nameof(InfiniteRevive));
        public string InfiniteAway       => G(nameof(InfiniteAway));
        /// <summary>속도 슬라이더 라벨. {0} = 배율 숫자 (e.g. "1.5")</summary>
        public string SpeedLabel         => G(nameof(SpeedLabel));
        public string ForceLevelUp       => G(nameof(ForceLevelUp));
        public string AddGems            => G(nameof(AddGems));
        public string AddSkullCoins      => G(nameof(AddSkullCoins));
        public string HealFull           => G(nameof(HealFull));
        public string NoReload           => G(nameof(NoReload));
        public string RapidFire         => G(nameof(RapidFire));
        public string NoRecoil          => G(nameof(NoRecoil));
        public string PerfectAim        => G(nameof(PerfectAim));
        public string AllCheatsOff       => G(nameof(AllCheatsOff));

        // ── 밸런스 트윅 ───────────────────────────────────────────────────────────
        public string TweakSectionHeader => G(nameof(TweakSectionHeader));
        /// <summary>활성 프리셋 표시. {0} = 프리셋 이름</summary>
        public string TweakActiveLabel   => G(nameof(TweakActiveLabel));
        public string SpawnNote          => G(nameof(SpawnNote));
        public string TweakMortal        => G(nameof(TweakMortal));
        public string TweakHunter        => G(nameof(TweakHunter));
        public string TweakSlayer        => G(nameof(TweakSlayer));
        public string TweakDemon         => G(nameof(TweakDemon));
        public string TweakApocalypse    => G(nameof(TweakApocalypse));

        // ── DPS 패널 ────────────────────────────────────────────────────────────
        /// <summary>피크 있을 때 하단 행. {0}=피크DPS, {1}=히트수, {2}=누적피해</summary>
        public string DpsSubWithPeak     => G(nameof(DpsSubWithPeak));
        /// <summary>피크 없을 때 하단 행. {0}=히트수, {1}=누적피해</summary>
        public string DpsSubNoPeak       => G(nameof(DpsSubNoPeak));

        // ── Co-op 탭 ────────────────────────────────────────────────────────────
        public string CoopStatusDisconnected => G(nameof(CoopStatusDisconnected));
        public string CoopConnected          => G(nameof(CoopConnected));
        public string CoopWaiting            => G(nameof(CoopWaiting));
        public string CoopCreateLobby        => G(nameof(CoopCreateLobby));
        public string CoopLobbyIdLabel       => G(nameof(CoopLobbyIdLabel));
        public string CoopLobbyIdEmpty       => G(nameof(CoopLobbyIdEmpty));
        public string CoopPasteClipboard     => G(nameof(CoopPasteClipboard));
        public string CoopClear              => G(nameof(CoopClear));
        public string CoopJoin               => G(nameof(CoopJoin));
        public string CoopLeave              => G(nameof(CoopLeave));
        public string CoopXpIndependent      => G(nameof(CoopXpIndependent));
        public string CoopXpReplicate        => G(nameof(CoopXpReplicate));
        public string CoopXpSplit            => G(nameof(CoopXpSplit));
        public string CoopXpIndependentDesc  => G(nameof(CoopXpIndependentDesc));
        public string CoopXpReplicateDesc    => G(nameof(CoopXpReplicateDesc));
        public string CoopXpSplitDesc        => G(nameof(CoopXpSplitDesc));
        public string CoopRefresh            => G(nameof(CoopRefresh));
        /// <summary>{0}=온라인 수</summary>
        public string CoopFriendsOnlineCount => G(nameof(CoopFriendsOnlineCount));
        public string CoopFriendsLoadPrompt  => G(nameof(CoopFriendsLoadPrompt));
        public string CoopFriendsNone        => G(nameof(CoopFriendsNone));
        public string CoopInvite             => G(nameof(CoopInvite));

        // ── 오버레이 위치 ──────────────────────────────────────────────────────────
        public string OverlayHidden    => G(nameof(OverlayHidden));
        public string OverlayTopLeft   => G(nameof(OverlayTopLeft));
        public string OverlayTopCenter => G(nameof(OverlayTopCenter));
        public string OverlayTopRight  => G(nameof(OverlayTopRight));
        public string ShortcutHint     => G(nameof(ShortcutHint));

        // ── 봇 탭 ────────────────────────────────────────────────────────────────
        public string BotPlayersOn    => G(nameof(BotPlayersOn));
        public string BotPlayersOff   => G(nameof(BotPlayersOff));
        public string BotStatusEmpty  => G(nameof(BotStatusEmpty));
        /// <summary>{0} = 추적 수</summary>
        public string BotTracking     => G(nameof(BotTracking));
        public string BotDisabledNote => G(nameof(BotDisabledNote));
    }

    /// <summary>
    /// 임베디드 TSV(I18n/strings.tsv)를 파싱해 언어별 LangStrings를 제공합니다.
    /// 인게임 언어 변경 시 Get() 재호출로 즉시 반영됩니다.
    /// </summary>
    public static class Strings
    {
        // 어셈블리 기본 네임스페이스 + 폴더 경로
        private const string ResourceName = "BloodshedModToolkit.I18n.strings.tsv";

        private static readonly Dictionary<LocalizationManager.Language, LangStrings> _table;
        private static readonly LangStrings _english;

        static Strings()
        {
            _table = LoadFromEmbeddedTsv();
            if (!_table.TryGetValue(LocalizationManager.Language.English, out _english!))
                _english = new LangStrings(new Dictionary<string, string>());
        }

        /// <summary>
        /// 지정 언어의 문자열 집합을 반환합니다. 미지원 언어는 영어로 폴백.
        /// </summary>
        public static LangStrings Get(LocalizationManager.Language lang)
            => _table.TryGetValue(lang, out var s) ? s : _english;

        // ── TSV 파서 ──────────────────────────────────────────────────────────────

        private static Dictionary<LocalizationManager.Language, LangStrings> LoadFromEmbeddedTsv()
        {
            var result = new Dictionary<LocalizationManager.Language, LangStrings>();
            try
            {
                var asm    = Assembly.GetExecutingAssembly();
                var stream = asm.GetManifestResourceStream(ResourceName);
                if (stream == null)
                {
                    Plugin.Log.LogError($"[Strings] 임베디드 리소스를 찾을 수 없음: {ResourceName}");
                    return result;
                }

                using var reader = new StreamReader(stream, Encoding.UTF8);

                // ── 헤더 행: Key | Lang1 | Lang2 | … ─────────────────────────────
                var headerLine = reader.ReadLine();
                if (headerLine == null) return result;

                var headers = headerLine.Split('\t');

                // 열 인덱스 → Language enum 매핑 (파싱 실패 열은 무시)
                var colToLang = new Dictionary<int, LocalizationManager.Language>();
                for (int i = 1; i < headers.Length; i++)
                {
                    if (Enum.TryParse<LocalizationManager.Language>(headers[i].Trim(), true, out var lang))
                        colToLang[i] = lang;
                }

                // 언어별 임시 딕셔너리 초기화
                var perLang = new Dictionary<LocalizationManager.Language, Dictionary<string, string>>();
                foreach (var lang in colToLang.Values)
                    perLang[lang] = new Dictionary<string, string>(16);

                // ── 데이터 행: Key | 값1 | 값2 | … ──────────────────────────────
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var cols = line.Split('\t');
                    if (cols.Length < 2) continue;

                    var key = cols[0].Trim();
                    foreach (var (colIdx, lang) in colToLang)
                        perLang[lang][key] = colIdx < cols.Length ? cols[colIdx] : string.Empty;
                }

                // LangStrings 인스턴스 생성
                foreach (var (lang, dict) in perLang)
                    result[lang] = new LangStrings(dict);

                bool hasEnglish = result.ContainsKey(LocalizationManager.Language.English);
                Plugin.Log.LogInfo($"[Strings] TSV 로드 완료 — {result.Count}개 언어 / English: {hasEnglish}");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[Strings] TSV 파싱 오류: {ex}");
            }
            return result;
        }
    }
}
