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

        private readonly Dictionary<ulong, GameObject> _avatars = new();
        private readonly Dictionary<ulong, Vector3>    _lastPos = new();
        private readonly Dictionary<ulong, Transform>  _labels  = new();
        private ulong _localId;

        private static readonly Color BotColor  = new(0f, 1f, 1f);      // 시안
        private static readonly Color PeerColor = new(0.2f, 1f, 0.2f);  // 녹색

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

            // 레이블 빌보드 — 항상 카메라 정방향으로 회전
            var cam = Camera.main;
            if (cam != null)
                foreach (var (_, labelTr) in _labels)
                    if (labelTr != null) labelTr.rotation = cam.transform.rotation;
        }

        private void CreateAvatar(ulong id, Net.PlayerStatePacket pkt)
        {
            bool isBot = BotState.IsBot(id);
            var  color = isBot ? BotColor : PeerColor;

            // 봇·피어 모두 빈 root (절차적 아바타 빌더가 자식 구성)
            var go = new GameObject(GetName(id));
            go.transform.position = new Vector3(pkt.PosX, pkt.PosY, pkt.PosZ);

            // 무기 클래스 (봇: 배정값, 피어: Melee 기본)
            var wc = WeaponClass.Melee;
            if (isBot)
            {
                int botIdx = 0;
                for (int i = 0; i < BotState.BotSteamIds.Length; i++)
                    if (BotState.BotSteamIds[i] == id) { botIdx = i; break; }
                wc = BotState.BotWeaponClasses[botIdx];
            }

            // 절차적 아바타 + 애니메이터 (봇·피어 공통)
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

            // 부유 이름 레이블 (TextMesh 자식)
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform);
            labelGo.transform.localPosition = new Vector3(0f, 1.1f, 0f);
            // 고해상도 렌더링(fontSize=96) + 스케일 다운(0.03125)으로 선명도 확보
            labelGo.transform.localScale = new Vector3(0.03125f, 0.03125f, 0.03125f);
            var tm = labelGo.AddComponent<TextMesh>();
            if (tm != null)
            {
                tm.text     = go.name;
                tm.fontSize = 96;
                tm.anchor   = TextAnchor.LowerCenter;
                tm.color    = color;
            }

            _avatars[id] = go;
            _lastPos[id] = go.transform.position;
            _labels[id]  = labelGo.transform;
            Plugin.Log.LogInfo($"[Renderer] 아바타 생성: {go.name}");
        }

        private void UpdateAvatar(ulong id, Net.PlayerStatePacket pkt)
        {
            if (!_avatars.TryGetValue(id, out var go) || go == null)
            {
                _avatars.Remove(id); _lastPos.Remove(id);
                CreateAvatar(id, pkt); return;
            }

            // 봇: BotPhysicsBody가 위치 소유 → 패킷으로 덮어쓰지 않음
            bool physicsOwned = BotState.IsBot(id) &&
                                BotPhysicsBody.Instances.ContainsKey(id);
            var newPos = physicsOwned
                ? go.transform.position
                : new Vector3(pkt.PosX, pkt.PosY, pkt.PosZ);

            if (!physicsOwned) go.transform.position = newPos;

            // 이동 방향으로 회전 (0.01m 이상 이동 시)
            bool hasPrev = _lastPos.TryGetValue(id, out var prev);
            if (hasPrev)
            {
                float fdx = newPos.x - prev.x, fdz = newPos.z - prev.z;
                if (fdx*fdx + fdz*fdz > 0.0001f)
                    go.transform.rotation = Quaternion.LookRotation(new Vector3(fdx, 0f, fdz));
            }
            _lastPos[id] = newPos;

            // 애니메이터에 이동속도 공급 (봇·피어 공통)
            if (hasPrev && BotAvatarAnimator.Instances.TryGetValue(id, out var avatarAnim))
            {
                float dx = newPos.x - prev.x, dz = newPos.z - prev.z;
                float speed = Time.deltaTime > 0f
                    ? (float)Math.Sqrt(dx*dx + dz*dz) / Time.deltaTime : 0f;
                avatarAnim.SetMoveSpeed(speed);

                // 봇: BotPhysicsBody 접지 상태 반영 / 피어: 항상 접지 가정
                if (BotPhysicsBody.Instances.TryGetValue(id, out var pb2))
                    avatarAnim.SetGrounded(pb2.IsGrounded);
                else
                    avatarAnim.SetGrounded(true);
            }
        }

        private void DestroyAvatar(ulong id)
        {
            if (_avatars.TryGetValue(id, out var go) && go != null) UnityEngine.Object.Destroy(go);
            _avatars.Remove(id); _lastPos.Remove(id); _labels.Remove(id);
        }

        private static string GetName(ulong id)
        {
            if (BotState.IsBot(id))
                for (int i = 0; i < BotState.BotSteamIds.Length; i++)
                    if (BotState.BotSteamIds[i] == id) return BotState.BotNames[i];
            try { return SteamFriends.GetFriendPersonaName(new CSteamID(id)); }
            catch { return $"Peer_{id:X8}"; }
        }

        /// <summary>
        /// URP 호환 Material을 생성한다.
        /// 1차: Shader.Find("Universal Render Pipeline/Unlit")
        /// 2차 폴백: 씬에서 기존 MeshRenderer의 sharedMaterial 복사
        /// 실패 시 null 반환 (기본 material 유지)
        /// </summary>
        private static Material? ResolveUrpMaterial(Color color)
        {
            // 1차: URP Unlit 셰이더 직접 검색
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Simple Lit");

            if (shader != null)
            {
                var mat = new Material(shader);
                // URP Unlit의 base color 프로퍼티 이름: "_BaseColor"
                mat.SetColor("_BaseColor", color);
                mat.color = color;  // 폴백 (일부 셰이더는 _Color도 지원)
                Plugin.Log.LogInfo($"[Renderer] URP 셰이더 적용: {shader.name}");
                return mat;
            }

            // 2차 폴백: 씬의 기존 MeshRenderer에서 material 복사
            var existing = UnityEngine.Object.FindObjectOfType<MeshRenderer>();
            if (existing?.sharedMaterial != null)
            {
                var mat = new Material(existing.sharedMaterial);
                mat.color = color;
                Plugin.Log.LogWarning("[Renderer] Shader.Find 실패 — 기존 MeshRenderer material 복사");
                return mat;
            }

            Plugin.Log.LogError("[Renderer] URP material 해결 실패 — 캡슐 렌더링 불가");
            return null;
        }
    }
}
