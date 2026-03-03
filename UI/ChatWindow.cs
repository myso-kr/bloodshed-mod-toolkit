using System;
using System.Collections.Generic;
using UnityEngine;
using BloodshedModToolkit.Coop.Events;

namespace BloodshedModToolkit.UI
{
    public class ChatWindow : MonoBehaviour
    {
        public ChatWindow(IntPtr ptr) : base(ptr) { }
        public static ChatWindow? Instance { get; private set; }
        public static bool IsTyping { get; private set; }

        private struct ChatEntry { public string Sender; public string Text; public float Time; }
        private readonly List<ChatEntry> _messages = new(32);
        private string _inputText = "";
        private bool _skipFrame;   // T키 누른 프레임의 inputString 무시
        private const int MaxMessages = 30;
        private const float FadeAfter = 5f;
        private const float WinW = 420f;
        private const float MsgH = 160f;
        private const float InputH = 28f;

        void Awake() { Instance = this; }
        void OnDestroy() { Instance = null; IsTyping = false; }

        void Update()
        {
            // ── 채팅창 열기 ─────────────────────────────────────────────────
            if (!IsTyping && Input.GetKeyDown(KeyCode.T))
            {
                IsTyping = true;
                _inputText = "";
                _skipFrame = true;
                Input.imeCompositionMode = IMECompositionMode.On;
                return;
            }

            if (!IsTyping) return;

            // ── T 누른 프레임은 inputString 건너뜀 ────────────────────────
            if (_skipFrame) { _skipFrame = false; return; }

            // ── 취소 / 전송 ────────────────────────────────────────────────
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CancelChat();
                return;
            }

            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                // IME 조합 중이면 Enter를 조합 확정으로 처리 (전송 보류)
                if (string.IsNullOrEmpty(Input.compositionString))
                {
                    SendChat();
                    return;
                }
            }

            // ── 문자 누적 (inputString = 이 프레임에 확정된 문자들) ────────
            foreach (char c in Input.inputString)
            {
                if (c == '\b')           { if (_inputText.Length > 0) _inputText = _inputText[..^1]; }
                else if (c == '\r' || c == '\n' || c == '\u001b') { /* 무시 */ }
                else                     { _inputText += c; }
            }
        }

        void OnGUI()
        {
            float sh = Screen.height;
            float x = 10f;
            float msgY = sh - 50f - MsgH - (IsTyping ? InputH + 4f : 0f);

            DrawMessageOverlay(x, msgY);
            if (IsTyping) DrawInputField(x, sh - 50f - InputH);
        }

        private void DrawMessageOverlay(float x, float y)
        {
            float now = Time.time;
            int start = Math.Max(0, _messages.Count - 8);
            int visibleCount = 0;
            for (int i = start; i < _messages.Count; i++)
            {
                if (!IsTyping && now - _messages[i].Time > FadeAfter) continue;
                visibleCount++;
            }
            if (visibleCount == 0 && !IsTyping) return;

            GUI.color = new Color(0, 0, 0, 0.45f);
            GUI.DrawTexture(new Rect(x, y, WinW, MsgH), Texture2D.whiteTexture);
            GUI.color = Color.white;

            float lineH = MsgH / 8f;
            float ly = y + 4f;
            for (int i = start; i < _messages.Count; i++)
            {
                float age = now - _messages[i].Time;
                if (!IsTyping && age > FadeAfter) continue;
                float alpha = IsTyping ? 1f : Math.Max(0f, 1f - (age - (FadeAfter - 1f)));
                GUI.color = new Color(1, 1, 1, alpha);
                GUI.Label(new Rect(x + 4f, ly, WinW - 8f, lineH),
                    $"[{_messages[i].Sender}] {_messages[i].Text}");
                ly += lineH;
            }
            GUI.color = Color.white;
        }

        private void DrawInputField(float x, float y)
        {
            GUI.color = new Color(0, 0, 0, 0.7f);
            GUI.DrawTexture(new Rect(x, y, WinW, InputH), Texture2D.whiteTexture);
            GUI.color = Color.white;

            // IME 조합 중이면 [조합중] 표시, 아니면 커서(_) 표시
            string comp = Input.compositionString;
            string display = string.IsNullOrEmpty(comp)
                ? _inputText + "_"
                : _inputText + "[" + comp + "]_";

            GUI.Label(new Rect(x + 4f, y + 4f, WinW - 8f, InputH - 4f), "> " + display);
        }

        private void SendChat()
        {
            string msg = _inputText.Trim();
            if (msg.Length > 0)
            {
                string name = Steamworks.SteamFriends.GetPersonaName() ?? "Me";
                AddMessage(name, msg);
                EventBridge.OnChatMessage(name, msg);
            }
            _inputText = "";
            IsTyping = false;
            Input.imeCompositionMode = IMECompositionMode.Auto;
        }

        private void CancelChat()
        {
            _inputText = "";
            IsTyping = false;
            Input.imeCompositionMode = IMECompositionMode.Auto;
        }

        public void AddMessage(string sender, string text)
        {
            if (_messages.Count >= MaxMessages) _messages.RemoveAt(0);
            _messages.Add(new ChatEntry { Sender = sender, Text = text, Time = Time.time });
            Plugin.Log.LogInfo($"[Chat] {sender}: {text}");
        }
    }
}
