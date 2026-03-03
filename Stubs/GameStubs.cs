// CI 컴파일 전용 스텁 — 런타임에서는 실제 BepInEx/interop/ DLL이 사용됩니다.
// csproj의 <Compile Remove="Stubs\**"> 로 로컬 빌드에서는 자동 제외됩니다.
using System;
using UnityEngine;

namespace com8com1.SCFPS
{
    public class PlayerStats : MonoBehaviour
    {
        public PlayerStats(IntPtr ptr) : base(ptr) { }

        public float money           { get; set; }
        public float experience      { get; set; }
        public float experienceCap   { get; set; }
        public float curHp           { get; set; }
        public float maxHp           { get; set; }
        public float MaxHp           { get; set; }
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

        public void SetMoney(float amount)     { }
        public void RestoreHp(float amount)    { }
        public void AddXp(float amount)        { }
        public void SetRevivals(int count)     { }
        public void TakeDamage(float damage, GameObject instigator) { }
        public void RecalculateStats()         { }
    }

    public class ShotAction : MonoBehaviour
    {
        public ShotAction(IntPtr ptr) : base(ptr) { }

        public float CooldownEnd   { get; set; }
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
        public bool isPlayer { get; set; }
        public void Damage(float damage) { }
    }

    public class Q3PlayerController : MonoBehaviour
    {
        public Q3PlayerController(IntPtr ptr) : base(ptr) { }
        public void Accelerate(float targetSpeed, float accel) { }
        public void AirControl(float targetSpeed) { }
    }

    public class WeaponData
    {
        public bool magazineBased { get; set; }
        public int  magazineSize  { get; set; }
    }

    public class Weapon : MonoBehaviour
    {
        public Weapon(IntPtr ptr) : base(ptr) { }
        public WeaponData?  weaponData         { get; set; }
        public bool         isReloading        { get; set; }
        public Coroutine?   coroutineReloading { get; set; }
        public int          mag                { get; set; }
    }

    public class WeaponItem : MonoBehaviour
    {
        public WeaponItem(IntPtr ptr) : base(ptr) { }
        public float GetRecoilTotal() => 0f;
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

