// Standalone stand-in for VRCTween and VRCUiShape. Tweens run on unscaled time, so menus still animate
// while a game is paused with Time.timeScale = 0. Compiles only without com.vrchat.worlds.
using System;
using System.Collections.Generic;
using UdonSharp;
using UnityEngine;

namespace VRC.SDK3.Components
{
    public enum VRCTweenEase
    {
        None, Linear,
        InSine, OutSine, InOutSine, InQuad, OutQuad, InOutQuad, InCubic, OutCubic, InOutCubic,
        InQuart, OutQuart, InOutQuart, InQuint, OutQuint, InOutQuint, InExpo, OutExpo, InOutExpo,
        InCirc, OutCirc, InOutCirc, InElastic, OutElastic, InOutElastic, InBack, OutBack, InOutBack,
        InBounce, OutBounce, InOutBounce
    }

    public struct VRCTweenHandle
    {
        internal VRCTween.Tween tween;
        internal int id;

        public bool IsValid => tween != null && tween.id == id && !tween.dead;
        public void Kill() { if (IsValid) tween.dead = true; }

        public VRCTweenHandle SetDelay(float seconds)
        {
            if (IsValid) tween.delay = seconds;
            return this;
        }

        public VRCTweenHandle OnComplete(UdonSharpBehaviour receiver, string eventName)
        {
            if (IsValid) { tween.doneReceiver = receiver; tween.doneEvent = eventName; }
            return this;
        }
    }

    public static class VRCTween
    {
        internal class Tween
        {
            public int id;
            public bool dead, started;
            public GameObject target; // for KillAllTweens; null for value tweens (as in VRChat)
            public float delay, duration, time;
            public VRCTweenEase ease;
            public Action begin;
            public Action<float> apply;
            public UdonSharpBehaviour doneReceiver;
            public string doneEvent;
        }

        private static readonly List<Tween> Tweens = new List<Tween>();
        private static int _ids;

        private static VRCTweenHandle Add(GameObject target, float duration, VRCTweenEase ease, Action begin, Action<float> apply)
        {
            _ = UdonRunner.Instance; // make sure something steps us
            var t = new Tween { id = ++_ids, target = target, duration = duration, ease = ease, begin = begin, apply = apply };
            Tweens.Add(t);
            return new VRCTweenHandle { tween = t, id = t.id };
        }

        public static VRCTweenHandle TweenScale(Transform t, Vector3 to, float duration, VRCTweenEase ease = VRCTweenEase.Linear)
        {
            Vector3 from = Vector3.zero;
            return Add(t.gameObject, duration, ease, () => from = t.localScale, k => { if (t != null) t.localScale = Vector3.LerpUnclamped(from, to, k); });
        }

        public static VRCTweenHandle TweenLocalPosition(Transform t, Vector3 to, float duration, VRCTweenEase ease = VRCTweenEase.Linear)
        {
            Vector3 from = Vector3.zero;
            return Add(t.gameObject, duration, ease, () => from = t.localPosition, k => { if (t != null) t.localPosition = Vector3.LerpUnclamped(from, to, k); });
        }

        /// <summary>Euler angles are interpolated as numbers, so a 180 degree turn spins instead of flipping.</summary>
        public static VRCTweenHandle TweenLocalRotation(Transform t, Vector3 toEuler, float duration, VRCTweenEase ease = VRCTweenEase.Linear)
        {
            Vector3 from = Vector3.zero;
            return Add(t.gameObject, duration, ease, () => from = t.localEulerAngles, k =>
            {
                if (t == null) return;
                Vector3 d = new Vector3(Mathf.DeltaAngle(from.x, toEuler.x), Mathf.DeltaAngle(from.y, toEuler.y), Mathf.DeltaAngle(from.z, toEuler.z));
                if (Mathf.Approximately(Mathf.Abs(d.z), 180f)) d.z = toEuler.z - from.z; // keep the spin direction
                t.localEulerAngles = from + d * k;
            });
        }

        public static VRCTweenHandle TweenFade(CanvasGroup g, float to, float duration, VRCTweenEase ease = VRCTweenEase.Linear)
        {
            float from = 0f;
            return Add(g.gameObject, duration, ease, () => from = g.alpha, k => { if (g != null) g.alpha = Mathf.LerpUnclamped(from, to, k); });
        }

        public static VRCTweenHandle TweenFloat(float from, float to, float duration, UdonSharpBehaviour receiver, string variable, string onUpdate, VRCTweenEase ease = VRCTweenEase.Linear)
        {
            return Add(null, duration, ease, null, k =>
            {
                if (receiver == null) return;
                receiver.SetProgramVariable(variable, Mathf.LerpUnclamped(from, to, k));
                if (!string.IsNullOrEmpty(onUpdate)) receiver.SendCustomEvent(onUpdate);
            });
        }

        public static VRCTweenHandle TweenInt(int from, int to, float duration, UdonSharpBehaviour receiver, string variable, string onUpdate, VRCTweenEase ease = VRCTweenEase.Linear)
        {
            return Add(null, duration, ease, null, k =>
            {
                if (receiver == null) return;
                receiver.SetProgramVariable(variable, Mathf.RoundToInt(Mathf.LerpUnclamped(from, to, k)));
                if (!string.IsNullOrEmpty(onUpdate)) receiver.SendCustomEvent(onUpdate);
            });
        }

        public static VRCTweenHandle DelayedCall(UdonSharpBehaviour receiver, string eventName, float delay)
        {
            return Add(null, 0f, VRCTweenEase.Linear, null, null).SetDelay(delay).OnComplete(receiver, eventName);
        }

        public static void KillAllTweens(GameObject target)
        {
            foreach (var t in Tweens) if (t.target == target) t.dead = true;
        }

        /// <summary>Called once per frame by UdonRunner.</summary>
        internal static void Step()
        {
            float dt = Time.unscaledDeltaTime;
            for (int i = 0; i < Tweens.Count; i++) // callbacks may add tweens: index loop
            {
                Tween t = Tweens[i];
                if (t.dead) continue;
                if (t.delay > 0f) { t.delay -= dt; continue; }
                if (!t.started) { t.started = true; t.begin?.Invoke(); }
                t.time += dt;
                float k = t.duration <= 0f ? 1f : Mathf.Clamp01(t.time / t.duration);
                t.apply?.Invoke(Ease(t.ease, k));
                if (k < 1f) continue;
                t.dead = true;
                if (t.doneReceiver != null && !string.IsNullOrEmpty(t.doneEvent)) t.doneReceiver.SendCustomEvent(t.doneEvent);
            }
            Tweens.RemoveAll(t => t.dead);
        }

        public static float Ease(VRCTweenEase e, float t)
        {
            if (e <= VRCTweenEase.Linear) return t;
            int family = ((int)e - 2) / 3, kind = ((int)e - 2) % 3; // In, Out, InOut
            if (kind == 0) return In(family, t);
            if (kind == 1) return 1f - In(family, 1f - t);
            return t < 0.5f ? In(family, 2f * t) * 0.5f : 1f - In(family, 2f - 2f * t) * 0.5f;
        }

        private static float In(int family, float t)
        {
            switch (family)
            {
                case 0: return 1f - Mathf.Cos(t * Mathf.PI * 0.5f);                                  // Sine
                case 1: return t * t;                                                                  // Quad
                case 2: return t * t * t;                                                              // Cubic
                case 3: return t * t * t * t;                                                          // Quart
                case 4: return t * t * t * t * t;                                                      // Quint
                case 5: return t <= 0f ? 0f : Mathf.Pow(2f, 10f * t - 10f);                            // Expo
                case 6: return 1f - Mathf.Sqrt(1f - t * t);                                            // Circ
                case 7: return t <= 0f || t >= 1f ? t : -Mathf.Pow(2f, 10f * t - 10f) * Mathf.Sin((t * 10f - 10.75f) * (2f * Mathf.PI / 3f)); // Elastic
                case 8: return 2.70158f * t * t * t - 1.70158f * t * t;                                // Back
                default: return 1f - BounceOut(1f - t);                                                // Bounce
            }
        }

        private static float BounceOut(float t)
        {
            const float n = 7.5625f, d = 2.75f;
            if (t < 1f / d) return n * t * t;
            if (t < 2f / d) { t -= 1.5f / d; return n * t * t + 0.75f; }
            if (t < 2.5f / d) { t -= 2.25f / d; return n * t * t + 0.9375f; }
            t -= 2.625f / d;
            return n * t * t + 0.984375f;
        }
    }

    /// <summary>VRChat's pointer target marker. Standalone: makes the world canvas clickable with the main camera.</summary>
    [RequireComponent(typeof(Canvas))]
    public class VRCUiShape : MonoBehaviour
    {
        public bool AllowFocusView = true;

        private void Start()
        {
            var c = GetComponent<Canvas>();
            if (c.renderMode == RenderMode.WorldSpace && c.worldCamera == null) c.worldCamera = Camera.main;
        }
    }
}

namespace LogicCuteGuy.UIHUD
{
    /// <summary>
    /// Standalone games only (no VRChat SDK): tells UIHUD where your player is. Without one, the main camera
    /// is the head and the feet are eye height below it.
    /// </summary>
    public class UIHudStandalonePlayer : MonoBehaviour
    {
        [Tooltip("Camera / head. Empty = the main camera.")] public Transform head;
        [Tooltip("Feet / character root: hotbar, teleport and respawn move this. Empty = eye height below the head.")] public Transform body;
        [Tooltip("Respawn point. Empty = where the player started.")] public Transform spawn;
        [Tooltip("Head height above the feet, meters. UIHUD scales its VR placement by this / 1.6.")] public float eyeHeight = 1.6f;
        public string displayName = "Player";
    }
}
