using System;
using UnityEngine;
using BloodshedModToolkit.Coop;
using BloodshedModToolkit.Coop.Mission;
using BloodshedModToolkit.Coop.Sync;

namespace BloodshedModToolkit.UI
{
    /// <summary>
    /// VoteRequested 상태일 때 화면 중앙에 표시되는 모달 투표 UI.
    /// 실제 게스트(IsConnected &amp;&amp; !IsHost) 또는 Debug 게스트 모드에서만 표시됩니다.
    /// </summary>
    public class VoteModal : MonoBehaviour
    {
        public VoteModal(IntPtr ptr) : base(ptr) { }

        private const float WinW = 420f;
        private const float WinH = 220f;
        private const int   WinId = 42;

        private GUIStyle? _stTitle;
        private GUIStyle? _stBody;
        private GUIStyle? _stCountdown;
        private GUIStyle? _stSub;
        private GUIStyle? _stAcceptBtn;
        private bool _stylesReady;

        private void EnsureStyles()
        {
            if (_stylesReady) return;
            _stylesReady = true;

            _stTitle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 15,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
            };
            _stTitle.normal.textColor = new Color(1f, 0.72f, 0f);

            _stBody = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 12,
                alignment = TextAnchor.MiddleCenter,
                wordWrap  = true,
            };
            _stBody.normal.textColor = new Color(0.85f, 0.85f, 0.85f);

            _stCountdown = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 36,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
            };
            _stCountdown.normal.textColor = new Color(1f, 0.35f, 0.35f);

            _stSub = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 11,
                alignment = TextAnchor.MiddleCenter,
            };
            _stSub.normal.textColor = new Color(0.65f, 0.65f, 0.65f);

            _stAcceptBtn = new GUIStyle(GUI.skin.button)
            {
                fontSize  = 14,
                fontStyle = FontStyle.Bold,
            };
            _stAcceptBtn.normal.textColor = new Color(0.2f, 1f, 0.3f);
            _stAcceptBtn.hover.textColor  = new Color(0.4f, 1f, 0.5f);
        }

        void OnGUI()
        {
            bool show = (CoopState.IsConnected && !CoopState.IsHost) || CoopState.DebugGuestMode;
            if (!show) return;
            if (MissionState.Status != MissionStatus.VoteRequested) return;

            EnsureStyles();

            // 화면 전체 어둡게
            GUI.color = new Color(0f, 0f, 0f, 0.55f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            float wx = (Screen.width  - WinW) * 0.5f;
            float wy = (Screen.height - WinH) * 0.5f;
            GUI.Window(WinId, new Rect(wx, wy, WinW, WinH), (GUI.WindowFunction)DrawModal, "");
        }

        private void DrawModal(int id)
        {
            GUILayout.Space(12f);

            // 제목
            string title = CoopState.DebugGuestMode
                ? "\u25c8  GAME START VOTE  [DEBUG]  \u25c8"
                : "\u25c8  GAME START VOTE  \u25c8";
            GUILayout.Label(title, _stTitle!);

            GUILayout.Space(8f);

            // 씬 이름
            string scene = MissionState.PendingSceneName;
            string body  = scene.Length > 0 ? $"Mission: {scene}" : "Host wants to start the game";
            GUILayout.Label(body, _stBody!);

            GUILayout.Space(6f);

            // 카운트다운 숫자
            int cd = Math.Max(0, (int)MissionState.VoteCountdown);
            GUILayout.Label(cd.ToString(), _stCountdown!);
            GUILayout.Label("seconds until auto-accept", _stSub!);

            GUILayout.Space(10f);

            // ACCEPT 버튼
            if (GUILayout.Button("ACCEPT", _stAcceptBtn!, GUILayout.Height(36f)))
                MissionSyncHandler.OnGuestVoteResponse(true);
        }
    }
}
