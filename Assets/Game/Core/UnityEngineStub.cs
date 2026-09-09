#if !UNITY_2021_1_OR_NEWER && !UNITY_5_3_OR_NEWER
using System;
using System.Text.Json;

namespace UnityEngine
{
    [AttributeUsage(AttributeTargets.Field)]
    public class SerializeField : Attribute { }

    [AttributeUsage(AttributeTargets.Field)]
    public class HeaderAttribute : Attribute
    {
        public HeaderAttribute(string header) { }
    }

    public class MonoBehaviour
    {
        public GameObject gameObject { get; set; } = new GameObject();
        public Transform transform { get; set; } = new Transform();
        public virtual void Awake() { }
        public virtual void Start() { }
        public virtual void Update() { }
        public virtual void OnDestroy() { }

        public T GetComponent<T>() where T : class, new() => new T();
        public static void DontDestroyOnLoad(GameObject target) { }
        public static void Destroy(GameObject target) { }
    }

    public class GameObject
    {
        public Transform transform { get; set; } = new Transform();
        public string name { get; set; } = "GameObject";
        public bool activeSelf { get; private set; } = true;
        public void SetActive(bool value) => activeSelf = value;
    }

    public class RectTransform
    {
        public Vector2 anchorMin { get; set; }
        public Vector2 anchorMax { get; set; }
    }

    public class Transform
    {
        public Vector3 position { get; set; } = Vector3.zero;
        public Vector3 localScale { get; set; } = Vector3.one;
    }

    public struct Vector2
    {
        public float x;
        public float y;

        public Vector2(float x, float y)
        {
            this.x = x;
            this.y = y;
        }

        public static Vector2 zero => new Vector2(0, 0);
        public static Vector2 one => new Vector2(1, 1);
        public float magnitude => (float)Math.Sqrt(x * x + y * y);

        public static Vector2 operator +(Vector2 a, Vector2 b) => new Vector2(a.x + b.x, a.y + b.y);
        public static Vector2 operator -(Vector2 a, Vector2 b) => new Vector2(a.x - b.x, a.y - b.y);
        public static Vector2 operator *(Vector2 a, float d) => new Vector2(a.x * d, a.y * d);
        public static Vector2 operator /(Vector2 a, float d) => new Vector2(a.x / d, a.y / d);
        public static implicit operator Vector3(Vector2 v) => new Vector3(v.x, v.y, 0f);
    }

    public struct Vector2Int
    {
        public int x;
        public int y;

        public Vector2Int(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public static Vector2Int zero => new Vector2Int(0, 0);
        public static Vector2Int one => new Vector2Int(1, 1);

        public static Vector2Int operator +(Vector2Int a, Vector2Int b) => new Vector2Int(a.x + b.x, a.y + b.y);
        public static Vector2Int operator -(Vector2Int a, Vector2Int b) => new Vector2Int(a.x - b.x, a.y - b.y);
        public static bool operator ==(Vector2Int a, Vector2Int b) => a.x == b.x && a.y == b.y;
        public static bool operator !=(Vector2Int a, Vector2Int b) => !(a == b);

        public override bool Equals(object obj) => obj is Vector2Int other && this == other;
        public override int GetHashCode() => HashCode.Combine(x, y);
    }

    public struct Vector3
    {
        public float x;
        public float y;
        public float z;

        public Vector3(float x, float y, float z = 0)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public static Vector3 zero => new Vector3(0, 0, 0);
        public static Vector3 one => new Vector3(1, 1, 1);

        public static Vector3 operator +(Vector3 a, Vector3 b) => new Vector3(a.x + b.x, a.y + b.y, a.z + b.z);
        public static Vector3 operator -(Vector3 a, Vector3 b) => new Vector3(a.x - b.x, a.y - b.y, a.z - b.z);
        public static Vector3 operator *(Vector3 a, float d) => new Vector3(a.x * d, a.y * d, a.z * d);

        public static implicit operator Vector2(Vector3 v) => new Vector2(v.x, v.y);

        public static Vector3 Lerp(Vector3 a, Vector3 b, float t)
        {
            t = Mathf.Clamp(t, 0f, 1f);
            return new Vector3(a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t, a.z + (b.z - a.z) * t);
        }
    }

    public struct Rect
    {
        public float x, y, width, height;
        public float xMin => x;
        public float xMax => x + width;
        public float yMin => y;
        public float yMax => y + height;
        public Vector2 position => new Vector2(x, y);
        public Vector2 size => new Vector2(width, height);

        public Rect(float x, float y, float width, float height)
        {
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;
        }

        public static bool operator ==(Rect a, Rect b) => a.x == b.x && a.y == b.y && a.width == b.width && a.height == b.height;
        public static bool operator !=(Rect a, Rect b) => !(a == b);
        public override bool Equals(object obj) => obj is Rect r && this == r;
        public override int GetHashCode() => HashCode.Combine(x, y, width, height);
    }

    public static class Mathf
    {
        public static float Clamp(float value, float min, float max) => value < min ? min : (value > max ? max : value);
        public static int Clamp(int value, int min, int max) => value < min ? min : (value > max ? max : value);
        public static float Lerp(float a, float b, float t) => a + (b - a) * Clamp(t, 0f, 1f);
        public static float Abs(float f) => Math.Abs(f);
    }

    public static class Screen
    {
        public static int width => 1920;
        public static int height => 1080;
        public static Rect safeArea => new Rect(0, 0, 1920, 1080);
    }

    public static class Time
    {
        public static float time => (float)(DateTime.UtcNow.Ticks / 10000000.0);
        public static float deltaTime => 0.016f;
    }

    public static class Application
    {
        public static string persistentDataPath => ".";
    }

    public enum TouchPhase
    {
        Began,
        Moved,
        Stationary,
        Ended,
        Canceled
    }

    public struct Touch
    {
        public Vector2 position;
        public Vector2 deltaPosition;
        public TouchPhase phase;
    }

    public static class Input
    {
        public static int touchCount => 0;
        public static Touch GetTouch(int index) => new Touch();
        public static bool GetMouseButtonDown(int button) => false;
        public static bool GetMouseButton(int button) => false;
        public static bool GetMouseButtonUp(int button) => false;
        public static Vector3 mousePosition => Vector3.zero;
        public static float GetAxis(string axisName) => 0f;
    }

    namespace UI
    {
        public class Text
        {
            public string text { get; set; } = string.Empty;
        }
    }

    public static class JsonUtility
    {
        private static readonly JsonSerializerOptions _options = new JsonSerializerOptions
        {
            WriteIndented = true,
            IncludeFields = true
        };

        public static string ToJson(object obj, bool prettyPrint = false)
        {
            return JsonSerializer.Serialize(obj, _options);
        }

        public static T FromJson<T>(string json)
        {
            return JsonSerializer.Deserialize<T>(json, _options);
        }
    }
}
#endif
