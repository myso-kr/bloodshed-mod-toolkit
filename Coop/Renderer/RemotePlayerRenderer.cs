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
            var go  = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = GetName(id);
            go.transform.localScale = new Vector3(0.5f, 0.9f, 0.5f);
            go.transform.position   = new Vector3(pkt.PosX, pkt.PosY, pkt.PosZ);

            // URP 호환 material 적용
            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                var mat = ResolveUrpMaterial(BotState.IsBot(id) ? BotColor : PeerColor);
                if (mat != null) mr.material = mat;
            }

            // 콜라이더 비활성화 (게임 물리 충돌 방지)
            var col = go.GetComponent<Collider>();
            if (col != null) col.enabled = false;

            // 봇 전용: 무기 비주얼 + BotPhysicsBody
            if (BotState.IsBot(id))
            {
                var wGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wGo.name = "WeaponVisual";
                wGo.transform.SetParent(go.transform);
                wGo.transform.localPosition = new Vector3(0.35f, 0f, 0.45f);
                wGo.transform.localScale    = new Vector3(0.08f, 0.08f, 0.55f);
                var wMr = wGo.GetComponent<MeshRenderer>();
                if (wMr != null)
                {
                    var wMat = ResolveUrpMaterial(new Color(0.3f, 0.3f, 0.35f));
                    if (wMat != null) wMr.material = wMat;
                }
                var wCol = wGo.GetComponent<Collider>();
                if (wCol != null) wCol.enabled = false;

                var pb = go.AddComponent<BotPhysicsBody>();
                if (pb != null) pb.Init(id);
            }

            // 부유 이름 레이블 (TextMesh 자식)
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform);
            labelGo.transform.localPosition = new Vector3(0f, 1.2f, 0f);
            // 고해상도 렌더링(fontSize=96) + 스케일 다운(0.03125)으로 선명도 확보
            labelGo.transform.localScale = new Vector3(0.03125f, 0.03125f, 0.03125f);
            var tm = labelGo.AddComponent<TextMesh>();
            if (tm != null)
            {
                tm.text     = go.name;
                tm.fontSize = 96;
                tm.anchor   = TextAnchor.LowerCenter;
                tm.color    = BotState.IsBot(id) ? BotColor : PeerColor;
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
            if (_lastPos.TryGetValue(id, out var prev))
            {
                var delta = new Vector3(newPos.x - prev.x, newPos.y - prev.y, newPos.z - prev.z);
                if (delta.x*delta.x + delta.y*delta.y + delta.z*delta.z > 0.0001f)
                    go.transform.rotation = Quaternion.LookRotation(delta);
            }
            _lastPos[id] = newPos;
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
