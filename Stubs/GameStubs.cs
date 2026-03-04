// CI 컴파일 전용 스텁 — 런타임에서는 실제 BepInEx/interop/ DLL이 사용됩니다.
// csproj의 <Compile Remove="Stubs\**"> 로 로컬 빌드에서는 자동 제외됩니다.
using System;
using UnityEngine;

namespace com8com1.SCFPS
{
    public class PlayableCharacterData : UnityEngine.ScriptableObject
    {
        public PlayableCharacterData(IntPtr ptr) : base(ptr) { }
        // name 은 UnityEngine.Object 에서 상속 (= ScriptableObject asset 이름)
    }

    public class MissionAsset : UnityEngine.ScriptableObject
    {
        public MissionAsset(IntPtr ptr) : base(ptr) { }
        public string strMissionTitle { get; set; } = "";
        public string strScene        { get; set; } = "";
        // name 은 UnityEngine.Object 에서 상속
    }

    public class EpisodeAsset : UnityEngine.ScriptableObject
    {
        public EpisodeAsset(IntPtr ptr) : base(ptr) { }
        public string strEpisodeTitle { get; set; } = "";
    }

    public class PlayerStats : MonoBehaviour
    {
        public PlayerStats(IntPtr ptr) : base(ptr) { }

        public float money           { get; set; }
        public float experience      { get; set; }
        public float experienceCap   { get; set; }
        public float curHp           { get; set; }
        public float maxHp           { get; set; }
        public float MaxHp           { get; set; }
        public float CurrentHp       { get; set; }
        public float Armor           { get; set; }
        public float Agility         { get; set; }
        public float Might           { get; set; }
        public float Area            { get; set; }
        public float Duration        { get; set; }
        public float Speed           { get; set; }
        public float Cooldown        { get; set; }
        public float Luck            { get; set; }
        public float Bloodthirst     { get; set; }
        public float Accuracy        { get; set; }
        public float Attraction      { get; set; }
        public int   LevelUpAway     { get; set; }
        public int   revivals        { get; set; }
        public int   level           { get; set; }

        public void SetMoney(float amount)     { }
        public void RestoreHp(float amount)    { }
        public void AddXp(float amount)        { }
        public void SetRevivals(int count)     { }
        public void TakeDamage(float damage, GameObject instigator) { }
        public void RecalculateStats()         { }
        public void LevelUpChecker()           { }
        public void SetLevel(int newLevel)     { }
    }

    public class ShotAction : MonoBehaviour
    {
        public ShotAction(IntPtr ptr) : base(ptr) { }

        public float CooldownEnd   { get; set; }
        public float shotDelay     { get; set; }
        public bool  IsOnCooldown  => UnityEngine.Time.time < CooldownEnd;

        public void    SetCooldownEnd(float value)          => CooldownEnd = value;
        public Vector3 GetSpreadDirection(Vector3 direction) => direction;
    }

    public class PlayerInventory : MonoBehaviour
    {
        public PlayerInventory(IntPtr ptr) : base(ptr) { }
        public bool HasLevelUpAway() => false;
    }

    public class AimPrecisionHandler : MonoBehaviour
    {
        public AimPrecisionHandler(IntPtr ptr) : base(ptr) { }
        public void ReducePrecision() { }
    }

    public class Health : MonoBehaviour
    {
        public Health(IntPtr ptr) : base(ptr) { }
        public bool   isPlayer      { get; set; }
        public float  initialHealth { get; set; }
        public float  maximumHealth { get; set; }
        public float  currentHealth { get; set; }
        public void   Initialization() { }
        public void   Damage(float damage, GameObject instigator,
                            float flickerDuration, float invincibilityDuration,
                            Vector3 damageDirection, Vector3 damagePosition,
                            bool suppressReaction) { }
    }

    public class Q3PlayerController : MonoBehaviour
    {
        public Q3PlayerController(IntPtr ptr) : base(ptr) { }
        public void Accelerate(Vector3 targetDir, float targetSpeed, float accel) { }
        public void AirControl(Vector3 targetDir, float targetSpeed) { }
    }

    public class SpawnProcessor : MonoBehaviour
    {
        public SpawnProcessor(IntPtr ptr) : base(ptr) { }
        public int currentWaveIndex { get; set; }
        public int GetMaxEnemyCount() => 0;
        public void NextWave()            { }
        public void StartNewWaveGroup()   { }
    }

    public class EnemyIdentityCard : MonoBehaviour
    {
        public EnemyIdentityCard(IntPtr ptr) : base(ptr) { }
    }

    public class SpawnDirector : MonoBehaviour
    {
        public SpawnDirector(IntPtr ptr) : base(ptr) { }
        public void SpawnEnemies(Transform transToSpawn, int spawnAmount, bool tryForceSpawn) { }
    }

    public class WeaponData
    {
        public bool magazineBased { get; set; }
        public int  magazineSize  { get; set; }
    }

    public class Weapon : MonoBehaviour
    {
        public Weapon(IntPtr ptr) : base(ptr) { }
        public WeaponData?                    weaponData         { get; set; }
        public bool                           isReloading        { get; set; }
        public Coroutine?                     coroutineReloading { get; set; }
        public int                            mag                { get; set; }
        public float                          shotDelay          { get; set; }
        public UnityEngine.Animator?          animator           { get; set; }
        public string                         strReloadSpeed     { get; set; } = "";
    }

    public class WeaponItem : MonoBehaviour
    {
        public WeaponItem(IntPtr ptr) : base(ptr) { }
        public float GetRecoilTotal()           => 0f;
        public float GetDamageTotal()           => 0f;
        public float GetCooldownTotal()         => 0f;
        public float GetReloadDurationTotal()   => 0f;
    }

    public class PersistentData : MonoBehaviour
    {
        public PersistentData(IntPtr ptr) : base(ptr) { }
        public float currentMoney        { get; set; }
        public int   currentSuperTickets { get; set; }
        public int   currentAways        { get; set; }
    }

    public class GameSettings : MonoBehaviour
    {
        public GameSettings(IntPtr ptr) : base(ptr) { }
        public LocalizationManager.Language languageText { get; set; }
    }

    /// <summary>
    /// MetaGame 씬의 최상위 화면 전환 매니저.
    /// goMetaGame* 필드는 각 화면 root GameObject 참조.
    /// goMetaGameStatisticsMenu / FillCharacterRoster는 실제 interop에 없어 제외.
    /// </summary>
    public class MetaGameManager : MonoBehaviour
    {
        public MetaGameManager(IntPtr ptr) : base(ptr) { }

        public GameObject? goMetaGameMainMenu           { get; set; }
        public GameObject? goMetaGameEpisodeSelection   { get; set; }
        public GameObject? goMetaGameMissionSelection   { get; set; }
        public GameObject? goMetaGameCharacterSelection { get; set; }
        public GameObject? goMetaGameMissionStart       { get; set; }
        public GameObject? goMetaGameCustomSession      { get; set; }
        public GameObject? goSelectSavegameNote         { get; set; }

        public void OpenMetaGameMainMenu()   { }
        public void UpdateMetaGameContent()  { }
        public void MarkNewMetaGameItems()   { }
        public void ReturnToMetaGame()       { }
    }

    /// <summary>
    /// 캐릭터 로스터 UI 매니저. FillCharacterRoster()로 캐릭터 목록을 갱신.
    /// </summary>
    public class CharacterRosterManager : MonoBehaviour
    {
        public CharacterRosterManager(IntPtr ptr) : base(ptr) { }
        public void FillCharacterRoster() { }
    }

    /// <summary>
    /// MetaGame 내 미션 선택 UI 매니저 (Mission list / info panel).
    /// 세이브 슬롯/미션 선택 상태는 SaveDataManager / SessionSettings가 보유.
    /// </summary>
    public class MetaGameMissionSelectionManager : MonoBehaviour
    {
        public MetaGameMissionSelectionManager(IntPtr ptr) : base(ptr) { }
        public void ReturnToMissionSelectionPanel() { }
        public void BuildMissionInfoPanel(MissionAsset mission) { }
    }

    /// <summary>
    /// MetaGame 내 캐릭터 선택 UI 매니저.
    /// </summary>
    public class MetaGameCharacterSelectionManager : MonoBehaviour
    {
        public MetaGameCharacterSelectionManager(IntPtr ptr) : base(ptr) { }
        public PlayableCharacterData? selectedCharacter { get; set; }
        public void SetSelectedCharacter(PlayableCharacterData data) { }
        public void FillCharacterList() { }
    }

    /// <summary>
    /// 세이브 슬롯 + 진행 데이터 매니저.
    /// </summary>
    public class SaveDataManager : MonoBehaviour
    {
        public SaveDataManager(IntPtr ptr) : base(ptr) { }
        public int activeSaveSlot { get; set; }
        public void SetActiveSaveSlot(int slot) { }
    }

    /// <summary>
    /// 미션 시작 시 선택된 에피소드/미션/캐릭터 상태를 보유하는 컴포넌트.
    /// </summary>
    public class SessionSettings : MonoBehaviour
    {
        public SessionSettings(IntPtr ptr) : base(ptr) { }
        public EpisodeAsset?          selectedEpisode       { get; set; }
        public MissionAsset?          selectedMission       { get; set; }
        public PlayableCharacterData? selectedCharacterData { get; set; }
    }
}

// Enemies.EnemyAi 네임스페이스
namespace Enemies.EnemyAi
{
    using System;
    using UnityEngine;

    public class EnemyAbilityController : MonoBehaviour
    {
        public EnemyAbilityController(IntPtr ptr) : base(ptr) { }
        public void SetBehaviorWalkable(float totalSpeed) { }
        public void SetWalkModifier(float walkModifier) { }
        public void SetCustomModifier(float customModifier) { }
        public void RefreshAgentSpeed() { }
    }
}

// com8com1.SCFPS 외부에 있는 전역 클래스
public class LocalizationManager
{
    public enum Language
    {
        English,
        EnglishUnitedKingdom,
        Korean,
        Japanese,
        ChineseSimplified,
        ChineseTraditional,
        Russian,
        German,
        French,
        FrenchCanadian,
        Spanish,
        SpanishMexican,
        SpanishArgentina,
        Polish,
        PortugueseBrazilian,
        Portuguese,
        Italian,
        Dutch,
        Turkish,
    }
}

