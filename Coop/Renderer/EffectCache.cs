using UnityEngine;

namespace BloodshedModToolkit.Coop.Renderer
{
    /// <summary>
    /// 씬에서 ParticleSystem / AudioClip을 스캔해 캐싱.
    /// 실제 이펙트를 복제·재생할 때 사용.
    /// </summary>
    public static class EffectCache
    {
        public static ParticleSystem? ExplosionPs  { get; private set; }
        public static ParticleSystem? MuzzlePs     { get; private set; }
        public static ParticleSystem? ImpactPs     { get; private set; }
        public static AudioClip?      SfxShoot     { get; private set; }
        public static AudioClip?      SfxMelee     { get; private set; }
        public static AudioClip?      SfxExplosion { get; private set; }

        public static void ScanScene()
        {
            // ParticleSystem 스캔
            var psList = Object.FindObjectsOfType<ParticleSystem>();
            if (psList != null)
            {
                foreach (var ps in psList)
                {
                    if (ps == null) continue;
                    string n = ps.gameObject.name.ToLowerInvariant();
                    if (ExplosionPs == null && (n.Contains("explo") || n.Contains("blast")))
                        ExplosionPs = ps;
                    else if (MuzzlePs == null && (n.Contains("muzzle") || n.Contains("flash")))
                        MuzzlePs = ps;
                    else if (ImpactPs == null && (n.Contains("impact") || n.Contains("hit")))
                        ImpactPs = ps;
                }
            }

            // AudioSource → AudioClip 스캔
            var srcList = Object.FindObjectsOfType<AudioSource>();
            if (srcList != null)
            {
                foreach (var src in srcList)
                {
                    if (src?.clip == null) continue;
                    string n = src.clip.name.ToLowerInvariant();
                    if (SfxShoot == null && (n.Contains("shot") || n.Contains("fire") || n.Contains("gun")))
                        SfxShoot = src.clip;
                    if (SfxMelee == null && (n.Contains("melee") || n.Contains("swing") || n.Contains("slash")))
                        SfxMelee = src.clip;
                    if (SfxExplosion == null && (n.Contains("explo") || n.Contains("boom")))
                        SfxExplosion = src.clip;
                }
            }

            Plugin.Log.LogInfo(
                $"[EffectCache] 스캔 완료: ExplosionPs={ExplosionPs?.gameObject.name} " +
                $"MuzzlePs={MuzzlePs?.gameObject.name} SfxShoot={SfxShoot?.name}");
        }

        /// <summary>ParticleSystem을 지정 위치에 복제하고 일정 시간 후 파괴.</summary>
        public static void TrySpawnPs(ParticleSystem? src, Vector3 pos)
        {
            if (src == null) return;
            var go = Object.Instantiate(src.gameObject);
            go.transform.position = pos;
            var ps = go.GetComponent<ParticleSystem>();
            ps?.Play();
            var ad = go.AddComponent<AutoDestroy>();
            if (ad != null) ad.Delay = 3f;
        }

        public static void PlaySfx(AudioClip? clip, Vector3 pos, float vol = 1f)
        {
            if (clip != null) AudioSource.PlayClipAtPoint(clip, pos, vol);
        }
    }
}
