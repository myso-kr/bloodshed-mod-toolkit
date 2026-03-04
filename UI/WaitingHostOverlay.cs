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

            var status    = MissionState.Status;
            var sceneName = SceneManager.GetActiveScene().name;

            if (status == MissionStatus.WaitingForHost &&
                sceneName != Coop.Mission.MissionState.MetaGameScene)
            {
                // 전체 화면 차단 오버레이 — 호스트 신호 대기
                EnsureStyles();
                GUI.depth = 10;

                GUI.color = new Color(0f, 0f, 0f, 0.92f);
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
                GUI.color = Color.white;

                float cx = Screen.width  * 0.5f;
                float cy = Screen.height * 0.5f;

                GUI.Label(new Rect(cx - 220f, cy - 36f, 440f, 40f),
                    "Waiting for host to start...", _stTitle!);

                string pending = MissionState.PendingSceneName;
                if (pending.Length > 0)
                    GUI.Label(new Rect(cx - 220f, cy + 12f, 440f, 24f),
                        $"Scene: {pending}", _stSub!);

                GUI.depth = 0;
            }
            else if (Coop.Mission.ItemSelectState.IsWaiting)
            {
                // 상단 배너 — Host가 업그레이드 선택 중
                EnsureStyles();
                GUI.depth = 5;

                float bh = 48f;
                GUI.color = new Color(0.1f, 0.55f, 0.1f, 0.88f);
                GUI.DrawTexture(new Rect(0, 0, Screen.width, bh), Texture2D.whiteTexture);
                GUI.color = Color.white;

                float cx = Screen.width * 0.5f;
                GUI.Label(new Rect(cx - 260f, 12f, 520f, 28f),
                    "Host is picking an upgrade...", _stTitle!);

                GUI.depth = 0;
            }
            else if (status == MissionStatus.NeedsCharacterSelect &&
                     sceneName == Coop.Mission.MissionState.MetaGameScene)
            {
                // MetaGame 상단 배너 — 캐릭터 선택 안내 (게임 UI를 가리지 않도록 좁게)
                EnsureStyles();
                GUI.depth = 5;

                float bh = 48f;
                GUI.color = new Color(0.8f, 0.35f, 0f, 0.88f);
                GUI.DrawTexture(new Rect(0, 0, Screen.width, bh), Texture2D.whiteTexture);
                GUI.color = Color.white;

                float cx = Screen.width * 0.5f;
                GUI.Label(new Rect(cx - 260f, 4f,  520f, 24f),
                    "Co-op: Select your character and start the mission", _stTitle!);

                string target = MissionState.PendingSceneName;
                if (target.Length > 0)
                    GUI.Label(new Rect(cx - 260f, 26f, 520f, 20f),
                        $"Target scene: {target}", _stSub!);

                GUI.depth = 0;
            }
        }
    }
}
