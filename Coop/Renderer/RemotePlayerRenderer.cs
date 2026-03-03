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
        }

        private void CreateAvatar(ulong id, Net.PlayerStatePacket pkt)
        {
            var go  = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = GetName(id);
            go.transform.localScale = new Vector3(0.5f, 0.9f, 0.5f);
            go.transform.position   = new Vector3(pkt.PosX, pkt.PosY, pkt.PosZ);

            // 색상 설정
            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null) mr.material.color = BotState.IsBot(id) ? BotColor : PeerColor;

            // 콜라이더 비활성화 (게임 물리 충돌 방지)
            var col = go.GetComponent<Collider>();
            if (col != null) col.enabled = false;

            // 부유 이름 레이블 (TextMesh 자식)
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform);
            labelGo.transform.localPosition = new Vector3(0f, 1.2f, 0f);
            var tm = labelGo.AddComponent<TextMesh>();
            if (tm != null)
            {
                tm.text     = go.name;
                tm.fontSize = 24;
                tm.anchor   = TextAnchor.LowerCenter;
                tm.color    = BotState.IsBot(id) ? BotColor : PeerColor;
            }

            _avatars[id] = go;
            _lastPos[id] = go.transform.position;
            Plugin.Log.LogInfo($"[Renderer] 아바타 생성: {go.name}");
        }

        private void UpdateAvatar(ulong id, Net.PlayerStatePacket pkt)
        {
            if (!_avatars.TryGetValue(id, out var go) || go == null)
            {
                _avatars.Remove(id); _lastPos.Remove(id);
                CreateAvatar(id, pkt); return;
            }
            var newPos = new Vector3(pkt.PosX, pkt.PosY, pkt.PosZ);

            // 이동 방향으로 회전 (0.01m 이상 이동 시)
            if (_lastPos.TryGetValue(id, out var prev))
            {
                var delta = new Vector3(newPos.x - prev.x, newPos.y - prev.y, newPos.z - prev.z);
                if (delta.x*delta.x + delta.y*delta.y + delta.z*delta.z > 0.0001f)
                    go.transform.rotation = Quaternion.LookRotation(delta);
            }
            go.transform.position = newPos;
            _lastPos[id] = newPos;
        }

        private void DestroyAvatar(ulong id)
        {
            if (_avatars.TryGetValue(id, out var go) && go != null) UnityEngine.Object.Destroy(go);
            _avatars.Remove(id); _lastPos.Remove(id);
        }

        private static string GetName(ulong id)
        {
            if (BotState.IsBot(id))
                for (int i = 0; i < BotState.BotSteamIds.Length; i++)
                    if (BotState.BotSteamIds[i] == id) return BotState.BotNames[i];
            try { return SteamFriends.GetFriendPersonaName(new CSteamID(id)); }
            catch { return $"Peer_{id:X8}"; }
        }
    }
}
