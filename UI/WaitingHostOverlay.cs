using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using BloodshedModToolkit.Coop;
using BloodshedModToolkit.Coop.Mission;

namespace BloodshedModToolkit.UI
{
    /// <summary>
    /// Guest가 WaitingForHost 상태일 때 전체 화면을 덮는 로딩 오버레이.
    /// Host의 MissionStart 수신 전까지 게임 화면 접근을 차단해 로딩 화면 잠금 효과를 냅니다.
    /// CheatMenu/ChatWindow 위에 그려지지 않도록 GUI.depth = 10 사용.
    /// </summary>
    public class WaitingHostOverlay : MonoBehaviour
    {
        public WaitingHostOverlay(IntPtr ptr) : base(ptr) { }

        private GUIStyle? _stTitle;
        private GUIStyle? _stSub;
        private bool _stylesReady;

        private void EnsureStyles()
        {
            if (_stylesReady) return;
            _stylesReady = true;

            _stTitle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 22,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
            };
            _stTitle.normal.textColor = Color.white;

            _stSub = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 13,
                alignment = TextAnchor.MiddleCenter,
            };
            _stSub.normal.textColor = new Color(0.65f, 0.65f, 0.65f);
        }

        void OnGUI()
        {
            if (!CoopState.IsConnected || CoopState.IsHost) return;
            if (MissionState.Status != MissionStatus.WaitingForHost) return;
            if (SceneManager.GetActiveScene().name == "MetaGame") return;

            EnsureStyles();

            // CheatMenu(depth=0) 뒤에서 게임 화면을 덮도록 depth를 높게 설정
            GUI.depth = 10;

            // 전체 화면 불투명 검정 배경
            GUI.color = new Color(0f, 0f, 0f, 0.92f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            float cx = Screen.width  * 0.5f;
            float cy = Screen.height * 0.5f;

            GUI.Label(new Rect(cx - 220f, cy - 36f, 440f, 40f),
                "Waiting for host to start...", _stTitle!);

            string scene = MissionState.PendingSceneName;
            if (scene.Length > 0)
                GUI.Label(new Rect(cx - 220f, cy + 12f, 440f, 24f),
                    $"Scene: {scene}", _stSub!);

            GUI.depth = 0;
        }
    }
}
