// Standalone (no VRChat SDK) stand-ins for the VRChat APIs UIHUD uses. Single player: the local player is the
// main camera (or a UIHudStandalonePlayer in the scene). Compiles only without com.vrchat.worlds.
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.XR;

namespace VRC.SDKBase
{
    public enum VRCInputMethod
    {
        Keyboard = 0, Mouse = 1, Controller = 2, Gaze = 3, Vive = 5, Oculus = 6, ViveXr = 7, Index = 10,
        HPMotionController = 11, Osc = 12, QuestHands = 13, Generic = 14, Touch = 15, OpenXRGeneric = 16,
        Pico = 17, SteamVR2 = 18, Embodied = 19, Count = 20
    }

    public class VRCPlayerApi
    {
        public enum TrackingDataType { Head, LeftHand, RightHand, Origin, AvatarRoot }

        public struct TrackingData
        {
            public Vector3 position;
            public Quaternion rotation;
            public TrackingData(Vector3 position, Quaternion rotation) { this.position = position; this.rotation = rotation; }
        }

        internal static readonly VRCPlayerApi Local = new VRCPlayerApi();

        public bool isLocal = true;
        public bool isMaster = true;
        public int playerId = 1;
        public string displayName
        {
            get { var r = Rig; return r != null && !string.IsNullOrEmpty(r.displayName) ? r.displayName : "Player"; }
        }

        private Vector3 _spawnPos;
        private Quaternion _spawnRot;
        private bool _spawnSet;
        private LogicCuteGuy.UIHUD.UIHudStandalonePlayer _rig;
        private float _nextRigSearch;

        private LogicCuteGuy.UIHUD.UIHudStandalonePlayer Rig
        {
            get
            {
                if (_rig == null && Time.unscaledTime >= _nextRigSearch)
                {
                    _nextRigSearch = Time.unscaledTime + 1f;
                    _rig = Object.FindObjectOfType<LogicCuteGuy.UIHUD.UIHudStandalonePlayer>();
                }
                return _rig;
            }
        }

        private Transform Head
        {
            get
            {
                var r = Rig;
                if (r != null && r.head != null) return r.head;
                Camera c = Camera.main;
                return c != null ? c.transform : null;
            }
        }

        /// <summary>Feet: the rig's body, else the main camera dropped by the eye height.</summary>
        private Transform Body
        {
            get { var r = Rig; return r != null && r.body != null ? r.body : null; }
        }

        public static int GetPlayerCount() { return 1; }

        public static VRCPlayerApi[] GetPlayers(VRCPlayerApi[] players)
        {
            if (players != null && players.Length > 0) players[0] = Local;
            return players;
        }

        public bool IsUserInVR() { return XRSettings.isDeviceActive; }

        public float GetAvatarEyeHeightAsMeters() { var r = Rig; return r != null ? r.eyeHeight : 1.6f; }

        public Vector3 GetPosition()
        {
            Transform b = Body;
            if (b != null) return b.position;
            Transform h = Head;
            return h != null ? h.position - Vector3.up * GetAvatarEyeHeightAsMeters() : Vector3.zero;
        }

        public Quaternion GetRotation()
        {
            Transform b = Body;
            if (b != null) return b.rotation;
            Transform h = Head;
            return h != null ? Quaternion.Euler(0f, h.eulerAngles.y, 0f) : Quaternion.identity;
        }

        public TrackingData GetTrackingData(TrackingDataType type)
        {
            Transform h = Head;
            var head = h != null ? new TrackingData(h.position, h.rotation) : new TrackingData(Vector3.zero, Quaternion.identity);
            if (type == TrackingDataType.Head) return head;
            if (type == TrackingDataType.Origin || type == TrackingDataType.AvatarRoot) return new TrackingData(GetPosition(), GetRotation());

            // Hands: XR controllers, in the head's tracking space (the camera's parent).
            InputDevice dev = InputDevices.GetDeviceAtXRNode(type == TrackingDataType.LeftHand ? XRNode.LeftHand : XRNode.RightHand);
            if (dev.isValid && dev.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 p)
                && dev.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion q))
            {
                Transform space = h != null ? h.parent : null;
                return space != null ? new TrackingData(space.TransformPoint(p), space.rotation * q) : new TrackingData(p, q);
            }
            return head;
        }

        public void TeleportTo(Vector3 position, Quaternion rotation)
        {
            RememberSpawn();
            Transform root = Body != null ? Body : (Head != null ? Head.root : null);
            if (root == null) return;
            var cc = root.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false; // else it snaps back
            root.SetPositionAndRotation(position, Quaternion.Euler(0f, rotation.eulerAngles.y, 0f));
            if (cc != null) cc.enabled = true;
            var rb = root.GetComponent<Rigidbody>();
            if (rb != null) rb.velocity = Vector3.zero;
        }

        public void Respawn()
        {
            RememberSpawn();
            TeleportTo(_spawnPos, _spawnRot);
        }

        internal void RememberSpawn()
        {
            if (_spawnSet) return;
            var r = Rig;
            Transform root = r != null && r.spawn != null ? r.spawn : (Body != null ? Body : (Head != null ? Head.root : null));
            if (root == null) return;
            _spawnSet = true;
            _spawnPos = root.position;
            _spawnRot = root.rotation;
        }
    }

    public static class Networking
    {
        public static VRCPlayerApi LocalPlayer { get { VRCPlayerApi.Local.RememberSpawn(); return VRCPlayerApi.Local; } }
        public static bool IsMaster => true;
        public static bool IsOwner(GameObject obj) { return true; }
        public static bool IsOwner(VRCPlayerApi player, GameObject obj) { return true; }
        public static VRCPlayerApi GetOwner(GameObject obj) { return VRCPlayerApi.Local; }
        public static void SetOwner(VRCPlayerApi player, GameObject obj) { }
    }

    public static class Utilities
    {
        public static bool IsValid(object obj)
        {
            if (obj is Object o) return o != null;
            return obj != null;
        }
    }
}

namespace VRC.SDK3.Rendering
{
    public class VRCCameraSettings
    {
        public static readonly VRCCameraSettings ScreenCamera = new VRCCameraSettings();

        private static Camera Cam => Camera.main;
        public float FieldOfView => Cam != null ? Cam.fieldOfView : 60f;
        public int PixelWidth => Cam != null ? Cam.pixelWidth : Screen.width;
        public int PixelHeight => Cam != null ? Cam.pixelHeight : Screen.height;
    }
}

namespace VRC.SDK3.Data
{
    public enum TokenType { Null, Boolean, SByte, Byte, Short, UShort, Int, UInt, Long, ULong, Float, Double, String, DataList, DataDictionary, Reference, Error }

    public struct DataToken : System.IEquatable<DataToken>
    {
        private readonly TokenType _type;
        private readonly double _num;
        private readonly object _ref;

        public DataToken(bool v) { _type = TokenType.Boolean; _num = v ? 1 : 0; _ref = null; }
        public DataToken(int v) { _type = TokenType.Int; _num = v; _ref = null; }
        public DataToken(long v) { _type = TokenType.Long; _num = v; _ref = null; }
        public DataToken(float v) { _type = TokenType.Float; _num = v; _ref = null; }
        public DataToken(double v) { _type = TokenType.Double; _num = v; _ref = null; }
        public DataToken(string v) { _type = v == null ? TokenType.Null : TokenType.String; _num = 0; _ref = v; }
        public DataToken(DataList v) { _type = v == null ? TokenType.Null : TokenType.DataList; _num = 0; _ref = v; }
        public DataToken(DataDictionary v) { _type = v == null ? TokenType.Null : TokenType.DataDictionary; _num = 0; _ref = v; }
        public DataToken(object v) { _type = v == null ? TokenType.Null : TokenType.Reference; _num = 0; _ref = v; }

        public static implicit operator DataToken(bool v) => new DataToken(v);
        public static implicit operator DataToken(int v) => new DataToken(v);
        public static implicit operator DataToken(long v) => new DataToken(v);
        public static implicit operator DataToken(float v) => new DataToken(v);
        public static implicit operator DataToken(double v) => new DataToken(v);
        public static implicit operator DataToken(string v) => new DataToken(v);
        public static implicit operator DataToken(DataList v) => new DataToken(v);
        public static implicit operator DataToken(DataDictionary v) => new DataToken(v);

        public TokenType TokenType => _type;
        public bool IsNull => _type == TokenType.Null;
        public bool IsNumber => _type >= TokenType.SByte && _type <= TokenType.Double;
        public double Number => _num;
        public bool Boolean => _num != 0;
        public int Int => (int)_num;
        public long Long => (long)_num;
        public float Float => (float)_num;
        public double Double => _num;
        public string String => _ref as string;
        public DataList DataList => _ref as DataList;
        public DataDictionary DataDictionary => _ref as DataDictionary;
        public object Reference => _ref;

        public override string ToString()
        {
            switch (_type)
            {
                case TokenType.Null: return "Null";
                case TokenType.Boolean: return Boolean ? "True" : "False";
                case TokenType.String: return (string)_ref;
                case TokenType.DataList: return "DataList";
                case TokenType.DataDictionary: return "DataDictionary";
                case TokenType.Reference: return _ref.ToString();
                default: return _num.ToString(CultureInfo.InvariantCulture);
            }
        }

        public bool Equals(DataToken o)
        {
            if (IsNumber && o.IsNumber) return _num == o._num;
            return _type == o._type && _num == o._num && Equals(_ref, o._ref);
        }
        public override bool Equals(object obj) => obj is DataToken t && Equals(t);
        public override int GetHashCode() => IsNumber ? _num.GetHashCode() : (_ref != null ? _ref.GetHashCode() : (int)_type);
    }

    public class DataList
    {
        private readonly List<DataToken> _items = new List<DataToken>();

        public DataList() { }
        public DataList(params DataToken[] items) { _items.AddRange(items); }

        public int Count => _items.Count;
        public DataToken this[int i] { get => _items[i]; set => _items[i] = value; }
        public void Add(DataToken v) { _items.Add(v); }
        public void Insert(int i, DataToken v) { _items.Insert(i, v); }
        public bool Remove(DataToken v) { return _items.Remove(v); }
        public void RemoveAt(int i) { _items.RemoveAt(i); }
        public void Clear() { _items.Clear(); }
        public bool Contains(DataToken v) { return _items.Contains(v); }
        public int IndexOf(DataToken v) { return _items.IndexOf(v); }
        public DataToken[] ToArray() { return _items.ToArray(); }

        public bool TryGetValue(int i, out DataToken v)
        {
            bool ok = i >= 0 && i < _items.Count;
            v = ok ? _items[i] : new DataToken();
            return ok;
        }
    }

    /// <summary>Keeps insertion order, like VRChat's.</summary>
    public class DataDictionary
    {
        private readonly Dictionary<DataToken, DataToken> _map = new Dictionary<DataToken, DataToken>();
        private readonly List<DataToken> _keys = new List<DataToken>();

        public int Count => _keys.Count;

        public DataToken this[DataToken key]
        {
            get => _map[key];
            set => SetValue(key, value);
        }

        public void SetValue(DataToken key, DataToken value)
        {
            if (!_map.ContainsKey(key)) _keys.Add(key);
            _map[key] = value;
        }

        public void Add(DataToken key, DataToken value) { SetValue(key, value); }
        public bool ContainsKey(DataToken key) { return _map.ContainsKey(key); }
        public bool Remove(DataToken key) { _keys.Remove(key); return _map.Remove(key); }
        public void Clear() { _map.Clear(); _keys.Clear(); }
        public bool TryGetValue(DataToken key, out DataToken value) { return _map.TryGetValue(key, out value); }

        public DataList GetKeys() { return new DataList(_keys.ToArray()); }

        public DataList GetValues()
        {
            var l = new DataList();
            foreach (var k in _keys) l.Add(_map[k]);
            return l;
        }
    }

    public static class VRCJson
    {
        public static bool TryDeserializeFromJson(string json, out DataToken result)
        {
            result = new DataToken();
            if (string.IsNullOrEmpty(json)) return false;
            int i = 0;
            try
            {
                result = Value(json, ref i);
                Skip(json, ref i);
                if (i != json.Length) { result = new DataToken(); return false; }
                return result.TokenType == TokenType.DataList || result.TokenType == TokenType.DataDictionary;
            }
            catch (System.Exception)
            {
                result = new DataToken();
                return false;
            }
        }

        private static void Skip(string s, ref int i) { while (i < s.Length && char.IsWhiteSpace(s[i])) i++; }

        private static DataToken Value(string s, ref int i)
        {
            Skip(s, ref i);
            char c = s[i];
            if (c == '{')
            {
                var d = new DataDictionary();
                i++; Skip(s, ref i);
                if (s[i] == '}') { i++; return d; }
                while (true)
                {
                    Skip(s, ref i);
                    string key = Str(s, ref i);
                    Skip(s, ref i);
                    if (s[i++] != ':') throw new System.FormatException();
                    d.SetValue(key, Value(s, ref i));
                    Skip(s, ref i);
                    if (s[i] == ',') { i++; continue; }
                    if (s[i++] == '}') return d;
                    throw new System.FormatException();
                }
            }
            if (c == '[')
            {
                var l = new DataList();
                i++; Skip(s, ref i);
                if (s[i] == ']') { i++; return l; }
                while (true)
                {
                    l.Add(Value(s, ref i));
                    Skip(s, ref i);
                    if (s[i] == ',') { i++; continue; }
                    if (s[i++] == ']') return l;
                    throw new System.FormatException();
                }
            }
            if (c == '"') return Str(s, ref i);
            if (Word(s, ref i, "true")) return true;
            if (Word(s, ref i, "false")) return false;
            if (Word(s, ref i, "null")) return new DataToken();
            int start = i;
            while (i < s.Length && "+-.eE0123456789".IndexOf(s[i]) >= 0) i++;
            return double.Parse(s.Substring(start, i - start), NumberStyles.Float, CultureInfo.InvariantCulture);
        }

        private static bool Word(string s, ref int i, string w)
        {
            if (string.CompareOrdinal(s, i, w, 0, w.Length) != 0) return false;
            i += w.Length;
            return true;
        }

        private static string Str(string s, ref int i)
        {
            if (s[i++] != '"') throw new System.FormatException();
            var sb = new StringBuilder();
            while (s[i] != '"')
            {
                char c = s[i++];
                if (c != '\\') { sb.Append(c); continue; }
                c = s[i++];
                switch (c)
                {
                    case 'n': sb.Append('\n'); break;
                    case 't': sb.Append('\t'); break;
                    case 'r': sb.Append('\r'); break;
                    case 'b': sb.Append('\b'); break;
                    case 'f': sb.Append('\f'); break;
                    case 'u': sb.Append((char)System.Convert.ToInt32(s.Substring(i, 4), 16)); i += 4; break;
                    default: sb.Append(c); break; // \" \\ \/
                }
            }
            i++;
            return sb.ToString();
        }
    }
}
