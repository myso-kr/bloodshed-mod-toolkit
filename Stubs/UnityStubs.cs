// CI 컴파일 전용 스텁 — 런타임에서는 실제 BepInEx/interop/ DLL이 사용됩니다.
// csproj의 <Compile Remove="Stubs\**"> 로 로컬 빌드에서는 자동 제외됩니다.
using System;
using Il2CppInterop.Runtime.InteropTypes;   // Il2CppObjectBase (BepInEx core에 포함)

namespace UnityEngine
{
    // IL2CPP 오브젝트 계층 — AddComponent<T>() 에서 T : Il2CppObjectBase 제약 충족
    public class Object : Il2CppObjectBase
    {
        public Object(IntPtr ptr) : base(ptr) { }
        public bool isActiveAndEnabled => false;
        public static T? FindObjectOfType<T>() where T : Object => null!;
        public static void Destroy(Object obj) { }
    }

    public class Component  : Object { public Component(IntPtr ptr) : base(ptr) { } }
    public class Behaviour  : Component { public Behaviour(IntPtr ptr) : base(ptr) { } }
    public class Transform  : Component { public Transform(IntPtr ptr) : base(ptr) { } }

    public class MonoBehaviour : Behaviour
    {
        public MonoBehaviour(IntPtr ptr) : base(ptr) { }

        public Coroutine? StartCoroutine(System.Collections.IEnumerator routine) => null;
        public void StopCoroutine(Coroutine? routine) { }
        public void StopAllCoroutines() { }
    }

    public class Coroutine { }

    public class Texture2D : Object
    {
        public Texture2D(IntPtr ptr) : base(ptr) { }
        public static Texture2D whiteTexture => null!;
    }

    public class GameObject : Object
    {
        public GameObject(IntPtr ptr) : base(ptr) { }
        public T? GetComponent<T>() where T : Component => null!;
    }

    public struct Vector2
    {
        public float x, y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
        public static Vector2 zero => new Vector2(0f, 0f);
    }

    public struct Vector3
    {
        public float x, y, z;
        public Vector3 normalized => this;
    }

    public struct Rect
    {
        public float x, y, width, height;
        public Rect(float x, float y, float width, float height)
        {
            this.x = x; this.y = y; this.width = width; this.height = height;
        }
    }

    public struct Color
    {
        public float r, g, b, a;
        public Color(float r, float g, float b, float a = 1f)
        {
            this.r = r; this.g = g; this.b = b; this.a = a;
        }
        public static Color green => new Color(0f, 1f, 0f);
        public static Color white => new Color(1f, 1f, 1f);
    }

    public static class Screen
    {
        public static int width  => 1920;
        public static int height => 1080;
    }

    public static class Time
    {
        public static float time      => 0f;
        public static float timeScale { get; set; }
    }

    public static class Input
    {
        public static bool GetKeyDown(KeyCode key) => false;
        public static bool GetKey(KeyCode key)     => false;
    }

    public enum KeyCode
    {
        None = 0,
        Backspace = 8, Tab = 9, Return = 13, Escape = 27, Space = 32,
        Alpha0 = 48, Alpha1, Alpha2, Alpha3, Alpha4,
        Alpha5, Alpha6, Alpha7, Alpha8, Alpha9,
        A = 97, B, C, D, E, F, G, H, I, J, K, L, M,
        N, O, P, Q, R, S, T, U, V, W, X, Y, Z,
        Delete = 127,
        Insert = 277,
        F1 = 282, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12,
    }

    public enum FontStyle { Normal = 0, Bold = 1, Italic = 2, BoldAndItalic = 3 }

    public class GUIStyleState
    {
        public Color textColor { get; set; }
    }

    public class GUIStyle
    {
        public GUIStyle() { }
        public GUIStyle(GUIStyle other) { }
        public int       fontSize  { get; set; }
        public FontStyle fontStyle { get; set; }
        public bool      wordWrap  { get; set; }
        public GUIStyleState normal { get; } = new GUIStyleState();
        public GUIStyleState hover  { get; } = new GUIStyleState();
        public static GUIStyle none => new GUIStyle();
    }

    public class GUISkin
    {
        public GUIStyle label  { get; } = new GUIStyle();
        public GUIStyle toggle { get; } = new GUIStyle();
        public GUIStyle button { get; } = new GUIStyle();
        public GUIStyle window { get; } = new GUIStyle();
    }

    public class GUIContent
    {
        public GUIContent() { }
        public GUIContent(string text) { }
        public static GUIContent none => new GUIContent();
    }

    public class GUILayoutOption { }

    public static class GUI
    {
        // 실제 UnityEngine.GUI.WindowFunction 과 동일한 중첩 delegate
        public delegate void WindowFunction(int id);

        public static Color   color { get; set; }
        public static GUISkin skin  => new GUISkin();

        public static Rect Window(int id, Rect clientRect, WindowFunction func, string text)
            => clientRect;
        public static void DragWindow() { }
        public static void DragWindow(Rect position) { }
        public static void Label(Rect position, string text) { }
        public static void Label(Rect position, string text, GUIStyle style) { }
        public static bool Button(Rect position, GUIContent content, GUIStyle style) => false;
        public static void DrawTexture(Rect position, Texture2D image) { }
    }

    public static class GUILayout
    {
        public static bool Toggle(bool value, string text) => value;
        public static bool Toggle(bool value, string text, GUIStyle style) => value;
        public static bool Button(string text) => false;
        public static bool Button(string text, GUIStyle style) => false;
        public static bool Button(string text, params GUILayoutOption[] options) => false;
        public static bool Button(string text, GUIStyle style, params GUILayoutOption[] options) => false;
        public static void Label(string text) { }
        public static void Label(string text, params GUILayoutOption[] options) { }
        public static void Label(string text, GUIStyle style, params GUILayoutOption[] options) { }
        public static void Space(float pixels) { }
        public static void FlexibleSpace() { }
        public static void BeginHorizontal(params GUILayoutOption[] options) { }
        public static void EndHorizontal() { }
        public static Vector2 BeginScrollView(Vector2 scrollPosition, params GUILayoutOption[] options) => scrollPosition;
        public static void EndScrollView() { }
        public static float HorizontalSlider(float value, float leftValue, float rightValue,
            params GUILayoutOption[] options) => value;
        public static GUILayoutOption Width(float width)    => new GUILayoutOption();
        public static GUILayoutOption Height(float height)  => new GUILayoutOption();
        public static GUILayoutOption ExpandHeight(bool expand) => new GUILayoutOption();
    }
}
