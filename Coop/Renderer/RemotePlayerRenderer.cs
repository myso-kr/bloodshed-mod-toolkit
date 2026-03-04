using System;
using System.Collections.Generic;
using UnityEngine;
using Steamworks;
using BloodshedModToolkit.Coop.Bots;
using BloodshedModToolkit.Coop.Sync;

namespace BloodshedModToolkit.Coop.Renderer
{
    public class RemotePlayerRenderer : MonoBehaviour
    {
        public RemotePlayerRenderer(IntPtr ptr) : base(ptr) { }
        public static RemotePlayerRenderer? Instance { get; private set; }

        private readonly Dictionary<ulong, GameObject> _avatars       = new();
        private readonly Dictionary<ulong, Vector3>    _lastPos       = new();
        private readonly Dictionary<ulong, Transform>  _labels        = new();
        private readonly Dictionary<ulong, byte>       _weaponClasses = new();
        private readonly Dictionary<ulong, byte>       _charIds       = new();

        // HP 바 추적
        private readonly Dictionary<ulong, Transform>  _hpRoots  = new();  // 빌보드 루트
        private readonly Dictionary<ulong, Transform>  _hpFgBars = new();  // 전경 바 Transform
        private readonly Dictionary<ulong, Material>   _hpFgMats = new();  // 전경 바 Material

        private ulong _localId;

        private static readonly Color BotColor = new(0f, 1f, 1f);  // 봇: 시안

        // 16색 캐릭터 팔레트 — CharacterId % 16 으로 인덱스
        private static readonly Color[] CharPalette = {
            new(0.2f, 1f,   0.2f), new(1f,   0.5f, 0.1f), new(0.3f, 0.7f, 1f),   new(1f,   0.2f, 0.8f),
            new(1f,   1f,   0.2f), new(0.2f, 0.9f, 0.9f), new(0.8f, 0.4f, 1f),   new(1f,   0.4f, 0.4f),
            new(0.5f, 1f,   0.5f), new(1f,   0.8f, 0.3f), new(0.4f, 0.6f, 1f),   new(1f,   0.5f, 0.7f),
            new(0.9f, 0.9f, 0.5f), new(0.3f, 1f,   0.8f), new(0.9f, 0.6f, 0.3f), new(0.6f, 0.8f, 1f),
        };

        void Awake() { Instance = this; Plugin.Log.LogInfo("[RemotePlayerRenderer] loaded"); }

        void Start()
        {
            try { _localId = (ulong)SteamUser.GetSteamID(); } catch { }
        }

        void OnDestroy()
        {
            foreach (var kv in _avatars) if (kv.Value != null) UnityEngine.Object.Destroy(kv.Value);
            _avatars.Clear();
            Instance = null;
        }

        void LateUpdate()
        {
            var states = PlayerSyncHandler.States;

            // 아바타 삭제 (states에서 사라진 ID)
            var toRemove = new List<ulong>();
            foreach (var id in _avatars.Keys)
                if (!states.ContainsKey(id)) toRemove.Add(id);
            foreach (var id in toRemove)
                DestroyAvatar(id);

            // 생성/갱신
            foreach (var (id, pkt) in states)
            {
                if (id == _localId && _localId != 0) continue;
                if (!_avatars.ContainsKey(id)) CreateAvatar(id, pkt);
                else UpdateAvatar(id, pkt);
            }

            // 레이블 + HP 바 빌보드 — 항상 카메라 정방향으로 회전
            var cam = Camera.main;
            if (cam != null)
            {
                var camRot = cam.transform.rotation;
                foreach (var (_, labelTr) in _labels)
                    if (labelTr != null) labelTr.rotation = camRot;
                foreach (var (_, hpRootTr) in _hpRoots)
                    if (hpRootTr != null) hpRootTr.rotation = camRot;
            }
        }

        private void CreateAvatar(ulong id, Net.PlayerStatePacket pkt)
        {
            bool isBot = BotState.IsBot(id);
            var  color = isBot ? BotColor : CharPalette[pkt.CharacterId % CharPalette.Length];
            _charIds[id] = pkt.CharacterId;

            var go = new GameObject(GetName(id));
            go.transform.position = new Vector3(pkt.PosX, pkt.PosY, pkt.PosZ);

            // 무기 클래스 (봇: 배정값, 피어: 패킷에서 읽기)
            var wc = GetWeaponClass(id, pkt);
            _weaponClasses[id] = pkt.WeaponClassId;

            // 절차적 아바타 + 애니메이터
            var anim = go.AddComponent<BotAvatarAnimator>();
            if (anim != null)
            {
                anim.Init(id);
                anim.WeaponClass = wc;
                BotAvatarBuilder.BuildProcedural(go, color, anim, wc);
            }

            // BotPhysicsBody: 봇 전용 (피어는 패킷으로 위치 제어)
            if (isBot)
            {
                var pb = go.AddComponent<BotPhysicsBody>();
                if (pb != null) pb.Init(id);
            }

            // 부유 이름 레이블
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform);
            labelGo.transform.localPosition = new Vector3(0f, 1.1f, 0f);
            labelGo.transform.localScale    = new Vector3(0.03125f, 0.03125f, 0.03125f);
            var tm = labelGo.AddComponent<TextMesh>();
            if (tm != null)
            {
                tm.text     = go.name;
                tm.fontSize = 96;
                tm.anchor   = TextAnchor.LowerCenter;
                tm.color    = color;
            }

            // HP 바
            BuildHpBar(id, go, color, pkt);

            _avatars[id]       = go;
            _lastPos[id]       = go.transform.position;
            _labels[id]        = labelGo.transform;

            Plugin.Log.LogInfo($"[Renderer] 아바타 생성: {go.name}  wc={wc}");
        }

        private void BuildHpBar(ulong id, GameObject avatarRoot, Color nameColor, Net.PlayerStatePacket pkt)
        {
            // HP 바 루트 (빌보드 대상)
            var hpRoot = new GameObject("HpBar");
            hpRoot.transform.SetParent(avatarRoot.transform);
            hpRoot.transform.localPosition = new Vector3(0f, 0.98f, 0f);

            // 배경 (반투명 어두운 회색)
            var bgGo = GameObject.CreatePrimitive(PrimitiveType.Quad);
            bgGo.name = "HpBg";
            bgGo.transform.SetParent(hpRoot.transform, false);
            bgGo.transform.localPosition = Vector3.zero;
            bgGo.transform.localScale    = new Vector3(0.52f, 0.07f, 1f);
            RemoveCollider(bgGo);
            ApplyFlatColor(bgGo, new Color(0.08f, 0.08f, 0.08f, 0.85f));

            // 전경 (HP 비율로 스케일)
            var fgGo = GameObject.CreatePrimitive(PrimitiveType.Quad);
            fgGo.name = "HpFg";
            fgGo.transform.SetParent(hpRoot.transform, false);
            fgGo.transform.localPosition = new Vector3(-0.001f, 0f, -0.001f); // bg보다 살짝 앞
            RemoveCollider(fgGo);
            var fgMat = ApplyFlatColor(fgGo, HpColor(pkt.CurrentHp, pkt.MaxHp));

            _hpRoots[id]  = hpRoot.transform;
            _hpFgBars[id] = fgGo.transform;
            if (fgMat != null) _hpFgMats[id] = fgMat;

            UpdateHpBarScale(id, pkt.CurrentHp, pkt.MaxHp);
        }

        private void UpdateAvatar(ulong id, Net.PlayerStatePacket pkt)
        {
            if (!_avatars.TryGetValue(id, out var go) || go == null)
            {
                _avatars.Remove(id); _lastPos.Remove(id);
                CreateAvatar(id, pkt); return;
            }

            // 무기 클래스 또는 캐릭터 ID 변경 시 아바타 재생성
            bool wcChanged   = _weaponClasses.TryGetValue(id, out var prevWc)   && prevWc != pkt.WeaponClassId;
            bool charChanged = _charIds.TryGetValue(id, out var prevCharId) && prevCharId != pkt.CharacterId;
            if (wcChanged || charChanged)
            {
                DestroyAvatar(id);
                CreateAvatar(id, pkt);
                return;
            }

            bool isBot = BotState.IsBot(id);
            bool physicsOwned = isBot && BotPhysicsBody.Instances.ContainsKey(id);
            var newPos = physicsOwned
                ? go.transform.position
                : new Vector3(pkt.PosX, pkt.PosY, pkt.PosZ);

            if (!physicsOwned) go.transform.position = newPos;

            // 피어: 패킷 RotY로 회전 / 봇: 이동 방향으로 회전
            bool hasPrev = _lastPos.TryGetValue(id, out var prev);
            if (!isBot)
            {
                // 실제 피어 — 전송된 Y축 회전각 적용
                go.transform.rotation = Quaternion.Euler(0f, pkt.RotY, 0f);
            }
            else if (hasPrev)
            {
                float fdx = newPos.x - prev.x, fdz = newPos.z - prev.z;
                if (fdx * fdx + fdz * fdz > 0.0001f)
                    go.transform.rotation = Quaternion.LookRotation(new Vector3(fdx, 0f, fdz));
            }
            _lastPos[id] = newPos;

            // 애니메이터에 이동속도 공급
            if (hasPrev && BotAvatarAnimator.Instances.TryGetValue(id, out var avatarAnim))
            {
                float dx = newPos.x - prev.x, dz = newPos.z - prev.z;
                float speed = Time.deltaTime > 0f
                    ? (float)Math.Sqrt(dx * dx + dz * dz) / Time.deltaTime : 0f;
                avatarAnim.SetMoveSpeed(speed);

                if (BotPhysicsBody.Instances.TryGetValue(id, out var pb2))
                    avatarAnim.SetGrounded(pb2.IsGrounded);
                else
                    avatarAnim.SetGrounded(true);
            }

            // 사망 처리
            bool isDead = pkt.MaxHp > 0f && pkt.CurrentHp <= 0f;
            go.SetActive(!isDead);

            // HP 바 갱신
            UpdateHpBarScale(id, pkt.CurrentHp, pkt.MaxHp);
        }

        private void UpdateHpBarScale(ulong id, float hp, float maxHp)
        {
            if (!_hpFgBars.TryGetValue(id, out var fgTr) || fgTr == null) return;

            float ratio = maxHp > 0f ? Math.Max(0f, Math.Min(hp / maxHp, 1f)) : 0f;
            // 전경 바: 왼쪽 끝을 고정하고 오른쪽으로 늘어남 (좌→우 감소)
            fgTr.localScale    = new Vector3(0.5f * ratio, 0.055f, 1f);
            fgTr.localPosition = new Vector3(-0.001f + 0.25f * (ratio - 1f), 0f, -0.001f);

            // 색 갱신
            if (_hpFgMats.TryGetValue(id, out var mat) && mat != null)
            {
                var c = HpColor(hp, maxHp);
                mat.SetColor("_BaseColor", c);
                mat.color = c;
            }
        }

        private void DestroyAvatar(ulong id)
        {
            if (_avatars.TryGetValue(id, out var go) && go != null) UnityEngine.Object.Destroy(go);
            _avatars.Remove(id);
            _lastPos.Remove(id);
            _labels.Remove(id);
            _weaponClasses.Remove(id);
            _charIds.Remove(id);
            _hpRoots.Remove(id);
            _hpFgBars.Remove(id);
            _hpFgMats.Remove(id);
        }

        // ── 헬퍼 ──────────────────────────────────────────────────────────────

        private static WeaponClass GetWeaponClass(ulong id, Net.PlayerStatePacket pkt)
        {
            if (BotState.IsBot(id))
            {
                for (int i = 0; i < BotState.BotSteamIds.Length; i++)
                    if (BotState.BotSteamIds[i] == id) return BotState.BotWeaponClasses[i];
                return WeaponClass.Melee;
            }
            return (WeaponClass)(pkt.WeaponClassId % 4);
        }

        private static Color HpColor(float hp, float maxHp)
        {
            if (maxHp <= 0f) return Color.green;
            float ratio = hp / maxHp;
            if (ratio > 0.6f) return new Color(0.15f, 0.85f, 0.15f);
            if (ratio > 0.3f) return new Color(0.90f, 0.75f, 0.05f);
            return new Color(0.90f, 0.12f, 0.12f);
        }

        private static string GetName(ulong id)
        {
            if (BotState.IsBot(id))
                for (int i = 0; i < BotState.BotSteamIds.Length; i++)
                    if (BotState.BotSteamIds[i] == id) return BotState.BotNames[i];
            try { return SteamFriends.GetFriendPersonaName(new CSteamID(id)); }
            catch { return $"Peer_{id:X8}"; }
        }

        private static void RemoveCollider(GameObject go)
        {
            var col = go.GetComponent<Collider>();
            if (col != null) col.enabled = false;
        }

        /// <summary>단색 URP Unlit Material을 생성하고 MeshRenderer에 적용. Material을 반환.</summary>
        private static Material? ApplyFlatColor(GameObject go, Color color)
        {
            var mr = go.GetComponent<MeshRenderer>();
            if (mr == null) return null;

            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Simple Lit");

            Material mat;
            if (shader != null)
            {
                mat = new Material(shader);
                mat.SetColor("_BaseColor", color);
                mat.color = color;
            }
            else
            {
                // 폴백: 기존 MeshRenderer material 복사
                var existing = UnityEngine.Object.FindObjectOfType<MeshRenderer>();
                if (existing?.sharedMaterial == null) return null;
                mat = new Material(existing.sharedMaterial);
                mat.color = color;
            }

            mr.material = mat;
            return mat;
        }
    }
}
