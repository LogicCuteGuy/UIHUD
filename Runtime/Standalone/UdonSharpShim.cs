// Standalone (no VRChat SDK) stand-in for the bits of UdonSharp that UIHUD uses.
// This assembly only compiles when com.vrchat.worlds is NOT installed (asmdef define constraint !UIHUD_VRC),
// so the UIHUD scripts compile unchanged in a plain Unity game.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRC.SDK3.Rendering;
using VRC.SDKBase;

namespace UdonSharp
{
    public enum BehaviourSyncMode { Any, None, NoVariableSync, Continuous, Manual }

    [AttributeUsage(AttributeTargets.Class)]
    public class UdonBehaviourSyncModeAttribute : Attribute
    {
        public UdonBehaviourSyncModeAttribute(BehaviourSyncMode mode) { }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class UdonSyncedAttribute : Attribute
    {
        public UdonSyncedAttribute() { }
        public UdonSyncedAttribute(object mode) { }
    }

    public enum EventTiming { Update, LateUpdate }

    /// <summary>
    /// MonoBehaviour with the Udon API surface: custom events and program variables by name (reflection),
    /// delayed events, and the VRChat callbacks UIHUD overrides (driven by UdonRunner).
    /// </summary>
    public abstract class UdonSharpBehaviour : MonoBehaviour
    {
        private static readonly Dictionary<string, MethodInfo> Methods = new Dictionary<string, MethodInfo>();
        private static readonly Dictionary<string, FieldInfo> Fields = new Dictionary<string, FieldInfo>();
        private const BindingFlags Any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        public void SendCustomEvent(string eventName)
        {
            MethodInfo m = Find(Methods, eventName, t => t.GetMethod(eventName, Any, null, Type.EmptyTypes, null));
            if (m != null) m.Invoke(this, null);
            else Debug.LogWarning($"[UIHUD] {GetType().Name} has no method '{eventName}'.", this);
        }

        public void SendCustomEventDelayedFrames(string eventName, int delayFrames, EventTiming timing = EventTiming.Update)
        {
            UdonRunner.Instance.StartCoroutine(DelayFrames(eventName, delayFrames));
        }

        public void SendCustomEventDelayedSeconds(string eventName, float delaySeconds, EventTiming timing = EventTiming.Update)
        {
            UdonRunner.Instance.StartCoroutine(DelaySeconds(eventName, delaySeconds));
        }

        public void SendCustomNetworkEvent(object target, string eventName) { SendCustomEvent(eventName); }

        public object GetProgramVariable(string name)
        {
            FieldInfo f = FindField(name);
            return f != null ? f.GetValue(this) : null;
        }

        public void SetProgramVariable(string name, object value)
        {
            FieldInfo f = FindField(name);
            if (f != null) f.SetValue(this, value);
        }

        public void RequestSerialization() { } // single player: nothing to send

        // VRChat callbacks UIHUD overrides. UdonRunner calls them.
        public virtual void PostLateUpdate() { }
        public virtual void OnPlayerJoined(VRCPlayerApi player) { }
        public virtual void OnPlayerLeft(VRCPlayerApi player) { }
        public virtual void OnInputMethodChanged(VRCInputMethod inputMethod) { }
        public virtual void OnAvatarEyeHeightChanged(VRCPlayerApi player, float prevEyeHeightAsMeters) { }
        public virtual void OnVRCCameraSettingsChanged(VRCCameraSettings cameraSettings) { }
        public virtual void OnDeserialization() { }
        public virtual void OnPreSerialization() { }
        public virtual void Interact() { }

        private IEnumerator DelayFrames(string e, int frames)
        {
            for (int i = 0; i < Mathf.Max(1, frames); i++) yield return null;
            if (this != null) SendCustomEvent(e);
        }

        private IEnumerator DelaySeconds(string e, float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
            if (this != null) SendCustomEvent(e);
        }

        private FieldInfo FindField(string name)
        {
            return Find(Fields, name, t =>
            {
                for (Type b = t; b != null && b != typeof(UdonSharpBehaviour); b = b.BaseType)
                {
                    FieldInfo f = b.GetField(name, Any | BindingFlags.DeclaredOnly);
                    if (f != null) return f;
                }
                return null;
            });
        }

        private T Find<T>(Dictionary<string, T> cache, string name, Func<Type, T> lookup) where T : class
        {
            string key = GetType().FullName + "." + name;
            if (!cache.TryGetValue(key, out T v)) cache[key] = v = lookup(GetType());
            return v;
        }
    }

    /// <summary>
    /// Hidden driver: runs delayed events and tweens, calls PostLateUpdate after every LateUpdate,
    /// OnPlayerJoined once per behaviour, and OnInputMethodChanged when the player switches mouse / touch.
    /// </summary>
    [DefaultExecutionOrder(32000)]
    public class UdonRunner : MonoBehaviour
    {
        private static UdonRunner _instance;
        private readonly List<UdonSharpBehaviour> _all = new List<UdonSharpBehaviour>();
        private readonly HashSet<UdonSharpBehaviour> _joined = new HashSet<UdonSharpBehaviour>();
        private float _nextScan;
        private VRCInputMethod _input = VRCInputMethod.Mouse;
        private int _w, _h;

        public static UdonRunner Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("[UIHUD Runner]") { hideFlags = HideFlags.HideInHierarchy };
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<UdonRunner>();
                }
                return _instance;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot() { _ = Instance; }

        private void OnEnable() { SceneManager.sceneLoaded += OnScene; }
        private void OnDisable() { SceneManager.sceneLoaded -= OnScene; }
        private void OnScene(Scene s, LoadSceneMode m) { _nextScan = 0f; }

        private void Update()
        {
            // ponytail: rescans the scene every second for behaviours; register explicitly if you spawn thousands.
            if (Time.unscaledTime >= _nextScan)
            {
                _nextScan = Time.unscaledTime + 1f;
                _all.Clear();
                _all.AddRange(FindObjectsOfType<UdonSharpBehaviour>(true));
                VRCPlayerApi local = Networking.LocalPlayer;
                foreach (var b in _all)
                    if (b.isActiveAndEnabled && _joined.Add(b)) b.OnPlayerJoined(local);
            }

            VRCInputMethod input = _input;
            if (Input.touchCount > 0) input = VRCInputMethod.Touch;
            else if (Input.GetMouseButtonDown(0) || Input.mousePresent && Input.GetAxisRaw("Mouse X") != 0f) input = VRCInputMethod.Mouse;
            if (input != _input)
            {
                _input = input;
                foreach (var b in _all) if (b != null) b.OnInputMethodChanged(input);
            }

            if (Screen.width != _w || Screen.height != _h)
            {
                bool first = _w == 0;
                _w = Screen.width; _h = Screen.height;
                if (!first) foreach (var b in _all) if (b != null) b.OnVRCCameraSettingsChanged(VRCCameraSettings.ScreenCamera);
            }

            VRC.SDK3.Components.VRCTween.Step();
        }

        private void LateUpdate()
        {
            foreach (var b in _all)
                if (b != null && b.isActiveAndEnabled) b.PostLateUpdate();
        }
    }
}
