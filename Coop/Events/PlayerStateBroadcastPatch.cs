using System;
using HarmonyLib;
using UnityEngine;
using com8com1.SCFPS;
using Steamworks;
using BloodshedModToolkit.Coop;

namespace BloodshedModToolkit.Coop.Events
{
    [HarmonyPatch(typeof(Q3PlayerController), "Accelerate")]
    public static class PlayerStateBroadcastPatch
    {
        private static float _lastBroadcast;
        private const  float BroadcastInterval = 0.05f;  // 20 Hz

        // 무기 클래스 캐시 (2초마다 갱신)
        private static float _weaponScanTimer;
        private static byte  _cachedWeaponClassId;

        // 캐릭터 ID 캐시 (5초마다 갱신)
        private static float _charScanTimer = -1f;
        private static byte  _cachedCharId;

        /// <summary>봇이 Co-op 비활성 상태에서도 로컬 플레이어 위치를 참조할 수 있도록 항상 갱신.</summary>
        public static Vector3 LastKnownLocalPos;

        static void Postfix(Q3PlayerController __instance)
        {
            var pos = __instance.transform.position;
            LastKnownLocalPos = pos;

            if (!CoopState.IsConnected) return;
            float now = Time.time;
            if (now - _lastBroadcast < BroadcastInterval) return;
            _lastBroadcast = now;

            var ps = UnityEngine.Object.FindObjectOfType<PlayerStats>();
            if (ps == null) return;

            // 무기 클래스 2초 캐시 갱신
            _weaponScanTimer -= BroadcastInterval;
            if (_weaponScanTimer <= 0f)
            {
                _weaponScanTimer = 2f;
                var shot = __instance.GetComponentInChildren<ShotAction>();
                if (shot == null)
                    _cachedWeaponClassId = 0;
                else if (shot.shotDelay < 0.12f)
                    _cachedWeaponClassId = 2;
                else if (shot.shotDelay < 0.35f)
                    _cachedWeaponClassId = 1;
                else
                    _cachedWeaponClassId = 3;
            }

            // 캐릭터 ID 5초 캐시 갱신
            if (_charScanTimer < 0f || now - _charScanTimer > 5f)
            {
                _charScanTimer = now;
                _cachedCharId  = DetectCharId();
            }

            float rotY = __instance.transform.eulerAngles.y;

            EventBridge.OnPlayerStateChanged(
                steamId: (ulong)SteamUser.GetSteamID(),
                px: pos.x, py: pos.y, pz: pos.z,
                hp:    ps.CurrentHp,
                maxHp: ps.MaxHp,
                level: ps.level,
                xp:    ps.experience,
                xpCap: ps.experienceCap,
                rotY:  rotY,
                weaponClassId: _cachedWeaponClassId,
                charId: _cachedCharId
            );
        }

        private static byte DetectCharId()
        {
            try
            {
                var ss = UnityEngine.Object.FindObjectOfType<SessionSettings>();
                if (ss?.selectedCharacterData?.name is string n && n.Length > 0)
                    return (byte)(Math.Abs(n.GetHashCode()) % 16);
            }
            catch { }
            return 0;
        }
    }
}
