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
        public bool   isActiveAndEnabled => false;
        public string name { get; set; } = "";
        public static T?   FindObjectOfType<T>()  where T : Object => null!;
        public static T[]? FindObjectsOfType<T>() where T : Object => null;
        public static void Destroy(Object obj) { }
        public static T    Instantiate<T>(T original)                    where T : Object => original;
        public static T    Instantiate<T>(T original, Transform parent)  where T : Object => original;
    }

    public class Component : Object
    {
        public Component(IntPtr ptr) : base(ptr) { }
        public Transform  transform  => null!;
        public GameObject gameObject => null!;
        public Transform? parent     => null;
        public int GetInstanceID() => 0;
        public T?   GetComponent<T>()                           where T : Component => null!;
        public T?   GetComponentInChildren<T>()                 where T : Component => null!;
        public T?   GetComponentInParent<T>()                   where T : Component => null!;
        public T[]  GetComponentsInChildren<T>()                where T : Component => Array.Empty<T>();
        public T[]  GetComponentsInChildren<T>(bool includeInactive) where T : Component => Array.Empty<T>();
    }
    public class Behaviour : Component
    {
        public Behaviour(IntPtr ptr) : base(ptr) { }
        public bool enabled { get; set; }
    }
    public class Transform  : Component
    {
        public Transform(IntPtr ptr) : base(ptr) { }
        public Vector3    position           { get; set; }
        public Vector3    localPosition      { get; set; }
        public Vector3    localScale         { get; set; }
        public Quaternion rotation           { get; set; }
        public Quaternion localRotation      { get; set; }
        public Vector3    localEulerAngles   { get; set; }
        public Vector3    eulerAngles        { get; set; }
        public void SetParent(Transform parent) { }
        public void SetParent(Transform parent, bool worldPositionStays) { }
    }

    public class MonoBehaviour : Behaviour
    {
        public MonoBehaviour(IntPtr ptr) : base(ptr) { }

        public Coroutine? StartCoroutine(System.Collections.IEnumerator routine) => null;
        public void StopCoroutine(Coroutine? routine) { }
        public void StopAllCoroutines() { }
    }

    public class Coroutine { }

    public enum FilterMode { Bilinear = 0, Point = 1, Trilinear = 2 }

    public class Texture2D : Object
    {
        public Texture2D(IntPtr ptr) : base(ptr) { }
        public Texture2D(int width, int height) : base(IntPtr.Zero) { }
        public static Texture2D whiteTexture => null!;
        public FilterMode filterMode { get; set; }
        public void SetPixels(Color[] colors) { }
        public void Apply(bool updateMipmaps) { }
    }

    public class GameObject : Object
    {
        public GameObject(IntPtr ptr) : base(ptr) { }
        public GameObject(string name) : base(IntPtr.Zero) { }
        public Transform transform  => null!;
        public int       layer      { get; set; }
        public bool      activeSelf { get; }
        public void SetActive(bool value) { }
        public T?  GetComponent<T>()                              where T : Component => null!;
        public T?  GetComponentInChildren<T>()                    where T : Component => null!;
        public T[] GetComponentsInChildren<T>()                   where T : Component => Array.Empty<T>();
        public T[] GetComponentsInChildren<T>(bool includeInactive) where T : Component => Array.Empty<T>();
        public T?  AddComponent<T>()                              where T : Component => null!;
        public static GameObject?  Find(string name) => null;
        public static GameObject CreatePrimitive(PrimitiveType type) => null!;
    }

    public class ScriptableObject : Object
    {
        public ScriptableObject(IntPtr ptr) : base(ptr) { }
        public ScriptableObject() : base(IntPtr.Zero) { }
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
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public float   sqrMagnitude => x * x + y * y + z * z;
        public float   magnitude    => (float)Math.Sqrt(sqrMagnitude);
        public Vector3 normalized   => this;
        public static Vector3 zero  => new Vector3(0f, 0f, 0f);
        public static float   Distance(Vector3 a, Vector3 b) => (a - b).magnitude;
        public static Vector3 operator +(Vector3 a, Vector3 b) => new Vector3(a.x + b.x, a.y + b.y, a.z + b.z);
        public static Vector3 operator -(Vector3 a, Vector3 b) => new Vector3(a.x - b.x, a.y - b.y, a.z - b.z);
    }

    public struct RaycastHit
    {
        public Vector3 point;
        public Vector3 normal;
        public float   distance;
    }

    public static class Physics
    {
        public static bool Raycast(Vector3 origin, Vector3 direction,
            float maxDistance = float.PositiveInfinity) => false;

        public static bool Raycast(Vector3 origin, Vector3 direction,
            out RaycastHit hitInfo,
            float maxDistance = float.PositiveInfinity)
        { hitInfo = default; return false; }

        public static bool Linecast(Vector3 start, Vector3 end) => false;
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
        public static Color Lerp(Color a, Color b, float t)
        {
            t = t < 0f ? 0f : t > 1f ? 1f : t;
            return new Color(a.r + (b.r - a.r) * t, a.g + (b.g - a.g) * t,
                             a.b + (b.b - a.b) * t, a.a + (b.a - a.a) * t);
        }
    }

    public static class Mathf
    {
        public const float Rad2Deg = 57.29578f;
        public const float Deg2Rad = 0.01745329f;
        public static float Min(float a, float b)         => a < b ? a : b;
        public static float Max(float a, float b)         => a > b ? a : b;
        public static float Max(float a, float b, float c) => Max(Max(a, b), c);
        public static float Clamp01(float v)              => v < 0f ? 0f : v > 1f ? 1f : v;
        public static float Clamp(float v, float min, float max)
            => v < min ? min : v > max ? max : v;
        public static float Lerp(float a, float b, float t) => a + (b - a) * Clamp01(t);
        public static float Abs(float v)  => v < 0f ? -v : v;
        public static int   Abs(int v)    => v < 0 ? -v : v;
        public static int   RoundToInt(float v) => (int)System.Math.Round(v);
    }

    public enum TextAnchor
    {
        UpperLeft = 0, UpperCenter, UpperRight,
        MiddleLeft, MiddleCenter, MiddleRight,
        LowerLeft, LowerCenter, LowerRight,
    }

    public static class Screen
    {
        public static int width  => 1920;
        public static int height => 1080;
    }

    public static class Time
    {
        public static float time      => 0f;
        public static float deltaTime => 0f;
        public static float timeScale { get; set; }
    }

    public enum IMECompositionMode { Auto = 0, On = 1, Off = 2 }

    public static class Input
    {
        public static bool   GetKeyDown(KeyCode key)   => false;
        public static bool   GetKey(KeyCode key)       => false;
        public static string compositionString         => "";
        public static string inputString               => "";
        public static IMECompositionMode imeCompositionMode { get; set; }
    }

    public static class GUIUtility
    {
        public static string systemCopyBuffer { get; set; } = "";
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
        KeypadEnter = 271,
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
        public int        fontSize  { get; set; }
        public FontStyle  fontStyle { get; set; }
        public bool       wordWrap  { get; set; }
        public TextAnchor alignment { get; set; }
        public GUIStyleState normal { get; } = new GUIStyleState();
        public GUIStyleState hover  { get; } = new GUIStyleState();
        public static GUIStyle none => new GUIStyle();

        /// <summary>
        /// 주어진 콘텐츠를 렌더링하는 데 필요한 최소 높이를 반환합니다.
        /// 스텁에서는 fontSize × 1.5f 를 반환 (보수적 추정값).
        /// 런타임에서는 Unity 가 실제 폰트 메트릭으로 계산합니다.
        /// </summary>
        public float CalcHeight(GUIContent content, float width) => fontSize * 1.5f;
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

        public static bool    enabled { get; set; } = true;
        public static Color   color { get; set; }
        public static int     depth { get; set; }
        public static GUISkin skin  => new GUISkin();

        public static Rect Window(int id, Rect clientRect, WindowFunction func, string text)
            => clientRect;
        public static void DragWindow() { }
        public static void DragWindow(Rect position) { }
        public static void Label(Rect position, string text) { }
        public static void Label(Rect position, string text, GUIStyle style) { }
        public static bool Button(Rect position, GUIContent content, GUIStyle style) => false;
        public static void DrawTexture(Rect position, Texture2D image) { }
        public static string TextField(Rect position, string text) => text;
        public static void SetNextControlName(string name) { }
        public static void FocusControl(string name) { }
    }

    public enum EventType
    {
        Used = 0, MouseDown = 1, MouseUp = 2, MouseMove = 3,
        KeyDown = 4, KeyUp = 5, ScrollWheel = 6, Repaint = 7, Layout = 8
    }

    public class Event
    {
        public static Event current { get; } = new Event();
        public EventType type     { get; set; }
        public KeyCode   keyCode  { get; set; }
        public char      character { get; set; }
        public void Use() { type = EventType.Used; }
    }

    public static class Random
    {
        public static int   Range(int   min, int   max) => min;
        public static float Range(float min, float max) => min;
    }

    public static class Resources
    {
        public static T[] FindObjectsOfTypeAll<T>() where T : Object => System.Array.Empty<T>();
    }

    // ── PrimitiveType / TextAlignment ──────────────────────────────────────────
    public enum PrimitiveType { Sphere=0, Capsule=1, Cylinder=2, Cube=3, Plane=4, Quad=5 }
    public enum TextAlignment  { Left=0, Center=1, Right=2 }

    // ── Quaternion ─────────────────────────────────────────────────────────────
    public struct Quaternion
    {
        public float x, y, z, w;
        public static Quaternion identity => new Quaternion { w = 1f };
        public static Quaternion LookRotation(Vector3 forward)                          => identity;
        public static Quaternion Euler(float x, float y, float z)                       => identity;
        public static Quaternion Euler(Vector3 angles)                                  => identity;
        public static Quaternion Slerp(Quaternion a, Quaternion b, float t)             => identity;
        public static Quaternion operator *(Quaternion a, Quaternion b)                 => identity;
    }

    // ── Camera ──────────────────────────────────────────────────────────────────
    public class Camera : Behaviour
    {
        public Camera(IntPtr ptr) : base(ptr) { }
        public static Camera? main => null;
    }

    // ── Shader ──────────────────────────────────────────────────────────────────
    public class Shader : Object
    {
        public Shader(IntPtr ptr) : base(ptr) { }
        public static Shader? Find(string name) => null;
        public new string name => "";
    }

    // ── Material / MeshRenderer / Collider ─────────────────────────────────────
    public class Material
    {
        public Material() { }
        public Material(Shader shader) { }
        public Material(Material source) { }
        public Color color { get; set; }
        public Shader? shader { get; set; }
        public void SetColor(string name, Color value) { }
        public void SetTexture(string name, Texture2D texture) { }
    }
    public class Renderer : Component
    {
        public Renderer(IntPtr ptr) : base(ptr) { }
        public Material  material       { get; set; } = new Material();
        public Material? sharedMaterial { get; set; }
        public bool      enabled        { get; set; }
    }
    public class MeshRenderer : Renderer
    {
        public MeshRenderer(IntPtr ptr) : base(ptr) { }
    }

    public class SkinnedMeshRenderer : Renderer
    {
        public SkinnedMeshRenderer(IntPtr ptr) : base(ptr) { }
    }

    // ── Animator ────────────────────────────────────────────────────────────────
    public enum AnimatorCullingMode { AlwaysAnimate = 0, CullUpdateTransforms, CullCompletely }
    public enum AnimatorControllerParameterType { Float = 1, Int = 3, Bool = 4, Trigger = 9 }

    public class AnimatorControllerParameter
    {
        public string                          name { get; } = "";
        public AnimatorControllerParameterType type { get; }
    }

    public class Animator : Behaviour
    {
        public Animator(IntPtr ptr) : base(ptr) { }
        public bool                      applyRootMotion { get; set; }
        public AnimatorCullingMode        cullingMode     { get; set; }
        public bool                       isHuman         { get; }
        public float                      speed           { get; set; } = 1f;
        public AnimatorControllerParameter[] parameters  => Array.Empty<AnimatorControllerParameter>();
        public void  SetFloat(string name,   float value) { }
        public void  SetBool(string name,    bool  value) { }
        public void  SetTrigger(string name)              { }
        public void  SetInteger(string name, int   value) { }
        public float GetFloat(string name)  => 0f;
        public bool  GetBool(string name)   => false;
    }

    // ── CharacterController ──────────────────────────────────────────────────────
    public enum CollisionFlags { None = 0, Sides = 1, Above = 2, Below = 4 }

    public class CharacterController : Behaviour
    {
        public CharacterController(IntPtr ptr) : base(ptr) { }
        public bool    isGrounded { get; }
        public float   height     { get; set; }
        public float   radius     { get; set; }
        public Vector3 center     { get; set; }
        public float   stepOffset { get; set; }
        public float   slopeLimit { get; set; }
        public CollisionFlags Move(Vector3 motion) => CollisionFlags.None;
    }

    public class Collider : Component
    {
        public Collider(IntPtr ptr) : base(ptr) { }
        public bool enabled { get; set; }
    }

    // ── TextMesh ────────────────────────────────────────────────────────────────
    public class TextMesh : Component
    {
        public TextMesh(IntPtr ptr) : base(ptr) { }
        public string        text      { get; set; } = "";
        public int           fontSize  { get; set; }
        public TextAlignment alignment { get; set; }
        public TextAnchor    anchor    { get; set; }
        public Color         color     { get; set; }
    }

    public static class GUILayout
    {
        public static void BeginVertical(params GUILayoutOption[] options) { }
        public static void BeginVertical(string style, params GUILayoutOption[] options) { }
        public static void BeginVertical(GUIStyle style, params GUILayoutOption[] options) { }
        public static void EndVertical() { }
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
        public static string TextField(string text, params GUILayoutOption[] options) => text;
        public static GUILayoutOption Width(float width)    => new GUILayoutOption();
        public static GUILayoutOption Height(float height)  => new GUILayoutOption();
        public static GUILayoutOption ExpandHeight(bool expand) => new GUILayoutOption();
    }
}

namespace UnityEngine.SceneManagement
{
    public struct Scene
    {
        public string name;
        public int    buildIndex;
        public bool   isLoaded;
        public bool   IsValid() => buildIndex >= 0 && name != null;
        public UnityEngine.GameObject[] GetRootGameObjects() => System.Array.Empty<UnityEngine.GameObject>();
    }

    public enum LoadSceneMode { Single, Additive }

    public static class SceneManager
    {
        // IL2CPP interop에서 UnityAction<T1,T2>은 System.Action<T1,T2>의 암묵적 변환을 지원하므로
        // CI 스텁에서는 System.Action 이벤트로 선언합니다.
        public static event System.Action<Scene, LoadSceneMode>? sceneLoaded;
        public static event System.Action<Scene>?                sceneUnloaded;
        public static event System.Action<Scene, Scene>?         activeSceneChanged;

        public static int   sceneCount                        => 0;
        public static Scene GetActiveScene()                  => default;
        public static Scene GetSceneAt(int index)             => default;
        public static Scene GetSceneByName(string name)       => default;

        public static void LoadScene(string sceneName)        { }
        public static void LoadScene(int sceneBuildIndex)     { }
        public static void LoadScene(string sceneName, LoadSceneMode mode) { }
    }
}
