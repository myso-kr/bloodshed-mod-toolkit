using System;
using UnityEngine;
using BloodshedModToolkit.Coop.Sync;
using BloodshedModToolkit.Coop.Bots;

namespace BloodshedModToolkit.Coop.Renderer
{
    /// <summary>무기 클래스별 공격 이펙트를 아바타 위치에서 생성.</summary>
    public static class AttackEffectSpawner
    {
        public static void Play(ulong steamId, WeaponClass wc)
        {
            if (!PlayerSyncHandler.States.TryGetValue(steamId, out var pkt)) return;

            var avatarPos = new Vector3(pkt.PosX, pkt.PosY, pkt.PosZ);
            float yRad    = pkt.RotY * (float)(Math.PI / 180.0);
            var forward   = new Vector3((float)Math.Sin(yRad), 0f, (float)Math.Cos(yRad));
            var muzzle    = avatarPos + forward * 0.4f + new Vector3(0f, 0.52f, 0f);

            switch (wc)
            {
                case WeaponClass.Melee:
                    EffectCache.PlaySfx(EffectCache.SfxMelee, muzzle);
                    break;
                case WeaponClass.Pistol:
                    SpawnBullet(muzzle, forward, 22f);
                    EffectCache.PlaySfx(EffectCache.SfxShoot, muzzle);
                    break;
                case WeaponClass.Rifle:
                    SpawnBullet(muzzle, forward, 45f);
                    EffectCache.PlaySfx(EffectCache.SfxShoot, muzzle);
                    break;
                case WeaponClass.Launcher:
                    SpawnProjectile(muzzle, forward);
                    EffectCache.PlaySfx(EffectCache.SfxShoot, muzzle);
                    break;
            }
        }

        private static void SpawnBullet(Vector3 origin, Vector3 dir, float speed)
        {
            var go = new GameObject("BulletTracer");
            go.transform.position = origin;
            var tr = go.AddComponent<BulletTracerEffect>();
            tr?.Init(dir, speed);
        }

        private static void SpawnProjectile(Vector3 origin, Vector3 dir)
        {
            var go = new GameObject("LauncherProjectile");
            go.transform.position = origin;
            var pr = go.AddComponent<LauncherProjectileEffect>();
            pr?.Init(dir);
        }
    }
}
