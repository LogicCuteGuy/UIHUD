using System.Collections.Generic;
using UdonSharp;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Components;

namespace LogicCuteGuy.UIHUD.Editor
{
    /// <summary>
    /// PowerPoint-style entrance animations: per-widget settings, the order on a page
    /// (With previous / After previous -> delay), an animation pane with a timeline, and a preview player.
    /// </summary>
    internal static class UIHudAnimGUI
    {
        private static readonly string[] EffectNames =
            { "Fade", "Fly in from left", "Fly in from right", "Fly in from bottom", "Fly in from top", "Zoom", "Pop", "Spin", "Typewriter (text)" };
        private static readonly string[] StartNames = { "With previous", "After previous" };
        private static readonly Color Bar = new Color(0.3f, 0.6f, 1f, 0.9f);

        // ---------- Order ----------

        /// <summary>Where an animation's timeline belongs: its page (menu or nested page group) or its canvas.</summary>
        internal static Transform GroupRoot(Transform t)
        {
            for (Transform p = t.parent; p != null; p = p.parent)
            {
                if (p.GetComponent<Canvas>() != null) return p;
                var sr = p.parent != null ? p.parent.GetComponent<ScrollRect>() : null;
                if (sr != null && sr.viewport == p.parent) return p; // a page in a scrolling page area
            }
            return t.root;
        }

        /// <summary>Animations of one page in play order (Hierarchy order); nested page groups keep their own.</summary>
        internal static List<UIHudAnim> InGroup(Transform root)
        {
            var list = new List<UIHudAnim>();
            foreach (var a in root.GetComponentsInChildren<UIHudAnim>(true))
                if (GroupRoot(a.transform) == root) list.Add(a);
            return list;
        }

        /// <summary>Delay of each animation from the order: With previous = same start, After previous = when the previous one ends.</summary>
        internal static void Retime(Transform root)
        {
            float prevStart = 0f, prevEnd = 0f;
            foreach (var a in InGroup(root))
            {
                float start = (a.startMode == 0 ? prevStart : prevEnd) + Mathf.Max(0f, a.extraDelay);
                if (!Mathf.Approximately(a.delay, start))
                {
                    Undo.RecordObject(a, "Animation Order");
                    a.delay = start;
                    UIHudBuilder.Sync(a);
                }
                prevStart = start;
                prevEnd = start + Mathf.Max(0f, a.duration);
            }
        }

        // ---------- Per widget ----------

        internal static void Section(GameObject go)
        {
            var a = go.GetComponent<UIHudAnim>();
            EditorGUILayout.LabelField("Animation", EditorStyles.miniBoldLabel);
            if (a == null)
            {
                var names = new string[EffectNames.Length + 1];
                names[0] = "None";
                EffectNames.CopyTo(names, 1);
                int pick = EditorGUILayout.Popup("Entrance", 0, names);
                if (pick > 0) { Add(go, (UIHudAnimEffect)(pick - 1)); GUIUtility.ExitGUI(); }
                return;
            }

            EditorGUI.BeginChangeCheck();
            var effect = (UIHudAnimEffect)EditorGUILayout.Popup("Entrance", (int)a.effect, EffectNames);
            int start = EditorGUILayout.Popup("Start", a.startMode, StartNames);
            float extra = EditorGUILayout.FloatField("Delay (s)", a.extraDelay);
            float dur = EditorGUILayout.FloatField("Duration (s)", a.duration);
            var ease = (VRCTweenEase)EditorGUILayout.EnumPopup("Easing (VRCTween)", a.ease);
            float dist = a.distance;
            bool fly = effect == UIHudAnimEffect.FlyFromLeft || effect == UIHudAnimEffect.FlyFromRight || effect == UIHudAnimEffect.FlyFromBottom || effect == UIHudAnimEffect.FlyFromTop;
            if (fly) dist = EditorGUILayout.FloatField("Distance (px)", dist);
            bool fade = a.fade;
            if (effect != UIHudAnimEffect.Fade && effect != UIHudAnimEffect.Typewriter) fade = EditorGUILayout.Toggle("Also fade", fade);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(a, "Edit Animation");
                if (effect == UIHudAnimEffect.Pop && a.effect != UIHudAnimEffect.Pop) ease = VRCTweenEase.OutBack;
                a.effect = effect; a.startMode = start; a.extraDelay = Mathf.Max(0f, extra); a.duration = Mathf.Max(0f, dur);
                a.ease = ease; a.distance = dist; a.fade = fade;
                UIHudBuilder.Sync(a);
                Retime(GroupRoot(go.transform));
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Starts " + a.delay.ToString("0.##") + " s after the page shows", EditorStyles.miniLabel);
                if (GUILayout.Button("▶ Play", EditorStyles.miniButtonLeft, GUILayout.Width(52))) UIHudAnimPreview.Play(GroupRoot(go.transform));
                if (GUILayout.Button("Remove", EditorStyles.miniButtonRight, GUILayout.Width(60))) { Remove(a); GUIUtility.ExitGUI(); }
            }
        }

        private static void Add(GameObject go, UIHudAnimEffect effect)
        {
            if (go.GetComponent<CanvasGroup>() == null) Undo.AddComponent<CanvasGroup>(go); // for fading
            var a = UdonSharpUndo.AddComponent<UIHudAnim>(go);
            a.effect = effect;
            if (effect == UIHudAnimEffect.Pop) a.ease = VRCTweenEase.OutBack;
            UIHudBuilder.Sync(a);
            Retime(GroupRoot(go.transform));
        }

        private static void Remove(UIHudAnim a)
        {
            Transform root = GroupRoot(a.transform);
            var backing = UdonSharpEditorUtility.GetBackingUdonBehaviour(a);
            Undo.DestroyObjectImmediate(a);
            if (backing != null) Undo.DestroyObjectImmediate(backing);
            Retime(root);
        }

        // ---------- Animation pane ----------

        /// <summary>All animations of a page in order, with a timeline like PowerPoint's Animation Pane.</summary>
        internal static void Pane(Transform root)
        {
            if (root == null) return;
            List<UIHudAnim> list = InGroup(root);
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Animation pane", EditorStyles.miniBoldLabel);
                using (new EditorGUI.DisabledScope(list.Count == 0))
                    if (GUILayout.Button("▶ Play all", EditorStyles.miniButton, GUILayout.Width(70))) UIHudAnimPreview.Play(root);
            }
            if (list.Count == 0)
            {
                EditorGUILayout.LabelField("No animations. Open a widget (▸) and pick an Entrance.", EditorStyles.miniLabel);
                return;
            }
            float total = 0.01f;
            foreach (var a in list) total = Mathf.Max(total, a.delay + a.duration);
            for (int i = 0; i < list.Count; i++)
            {
                UIHudAnim a = list[i];
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField((i + 1) + ".", GUILayout.Width(22));
                    if (GUILayout.Button(new GUIContent(a.name, "Select"), EditorStyles.label, GUILayout.Width(100))) UIHudGUI.Ping(a.gameObject);
                    EditorGUILayout.LabelField(EffectNames[(int)a.effect], EditorStyles.miniLabel, GUILayout.Width(100));
                    int s = EditorGUILayout.Popup(a.startMode, StartNames, GUILayout.Width(96));
                    if (s != a.startMode) { Undo.RecordObject(a, "Animation Order"); a.startMode = s; UIHudBuilder.Sync(a); Retime(root); }
                    Rect r = GUILayoutUtility.GetRect(40f, 14f, GUILayout.ExpandWidth(true));
                    EditorGUI.DrawRect(r, new Color(0f, 0f, 0f, 0.2f));
                    EditorGUI.DrawRect(new Rect(r.x + r.width * a.delay / total, r.y + 2f, Mathf.Max(2f, r.width * a.duration / total), r.height - 4f), Bar);
                }
            }
            EditorGUILayout.LabelField("Order = order in the widget list (▲▼). Total " + total.ToString("0.##") + " s.", EditorStyles.miniLabel);
        }
    }

    /// <summary>
    /// Plays a page's animations in the editor (VRCTween only runs in game): the same effects and
    /// easing, sampled here, then everything is put back as edited.
    /// </summary>
    internal static class UIHudAnimPreview
    {
        private class State
        {
            public UIHudAnim a; public CanvasGroup group; public TMPro.TextMeshProUGUI text;
            public Vector3 pos, scale, euler; public float alpha; public int chars;
        }

        private static List<State> _states;
        private static double _start;
        private static float _end;

        internal static void Play(Transform root)
        {
            Stop();
            _states = new List<State>();
            _end = 0f;
            foreach (var a in UIHudAnimGUI.InGroup(root))
            {
                var st = new State { a = a, group = a.GetComponent<CanvasGroup>(), text = a.GetComponentInChildren<TMPro.TextMeshProUGUI>(true) };
                st.pos = a.transform.localPosition; st.scale = a.transform.localScale; st.euler = a.transform.localEulerAngles;
                st.alpha = st.group != null ? st.group.alpha : 1f;
                st.chars = st.text != null ? st.text.maxVisibleCharacters : 0;
                _states.Add(st);
                _end = Mathf.Max(_end, a.delay + a.duration);
            }
            if (_states.Count == 0) return;
            var canvas = root.GetComponentInParent<Canvas>(true);
            if (canvas != null) UIHudPreviewWindow.Open((RectTransform)canvas.rootCanvas.transform);
            _start = EditorApplication.timeSinceStartup;
            Tick();
            EditorApplication.update += Tick;
        }

        private static void Tick()
        {
            if (_states == null) return;
            float t = (float)(EditorApplication.timeSinceStartup - _start);
            foreach (var st in _states) if (st.a != null) Sample(st, t);
            UIHudPreviewWindow.Changed();
            if (t > _end + 0.3f) Stop();
        }

        internal static void Sample(UIHudAnim a, float t)
        {
            // One-off sample (used by tests): capture, show the frame, keep it.
            var st = new State { a = a, group = a.GetComponent<CanvasGroup>(), text = a.GetComponentInChildren<TMPro.TextMeshProUGUI>(true),
                pos = a.transform.localPosition, scale = a.transform.localScale, euler = a.transform.localEulerAngles };
            Sample(st, t);
        }

        private static void Sample(State st, float t)
        {
            UIHudAnim a = st.a;
            float k = a.duration > 0f ? Mathf.Clamp01((t - a.delay) / a.duration) : (t >= a.delay ? 1f : 0f);
            float e = Ease(a.ease, k), hide = 1f - e;
            Transform tr = a.transform;
            Vector3 off = Vector3.zero;
            switch (a.effect)
            {
                case UIHudAnimEffect.FlyFromLeft: off.x = -a.distance * hide; break;
                case UIHudAnimEffect.FlyFromRight: off.x = a.distance * hide; break;
                case UIHudAnimEffect.FlyFromBottom: off.y = -a.distance * hide; break;
                case UIHudAnimEffect.FlyFromTop: off.y = a.distance * hide; break;
            }
            tr.localPosition = st.pos + off;
            tr.localScale = a.effect == UIHudAnimEffect.Zoom || a.effect == UIHudAnimEffect.Pop ? st.scale * e : st.scale;
            tr.localEulerAngles = a.effect == UIHudAnimEffect.Spin ? st.euler + new Vector3(0f, 0f, 180f * hide) : st.euler;
            if (st.group != null)
            {
                if (a.effect == UIHudAnimEffect.Typewriter) st.group.alpha = Mathf.Clamp01((t - a.delay) / Mathf.Max(0.0001f, Mathf.Min(0.15f, a.duration)));
                else if (a.effect == UIHudAnimEffect.Fade || a.fade) st.group.alpha = Mathf.Clamp01(e);
                else st.group.alpha = t >= a.delay ? 1f : 0f;
            }
            if (st.text != null && a.effect == UIHudAnimEffect.Typewriter)
                st.text.maxVisibleCharacters = k >= 1f ? 99999 : Mathf.FloorToInt(st.text.text.Length * k);
        }

        internal static void Stop()
        {
            EditorApplication.update -= Tick;
            if (_states == null) return;
            foreach (var st in _states)
            {
                if (st.a == null) continue;
                Transform tr = st.a.transform;
                tr.localPosition = st.pos; tr.localScale = st.scale; tr.localEulerAngles = st.euler;
                if (st.group != null) st.group.alpha = st.alpha;
                if (st.text != null) st.text.maxVisibleCharacters = st.chars;
            }
            _states = null;
            UIHudPreviewWindow.Changed();
        }

        /// <summary>VRCTween's easing curves (standard Penner equations), for the editor preview.</summary>
        internal static float Ease(VRCTweenEase ease, float t)
        {
            const float c1 = 1.70158f, c2 = c1 * 1.525f, c3 = c1 + 1f, c4 = 2f * Mathf.PI / 3f, c5 = 2f * Mathf.PI / 4.5f;
            switch (ease)
            {
                case VRCTweenEase.InSine: return 1f - Mathf.Cos(t * Mathf.PI / 2f);
                case VRCTweenEase.OutSine: return Mathf.Sin(t * Mathf.PI / 2f);
                case VRCTweenEase.InOutSine: return -(Mathf.Cos(Mathf.PI * t) - 1f) / 2f;
                case VRCTweenEase.InQuad: return t * t;
                case VRCTweenEase.OutQuad: return 1f - (1f - t) * (1f - t);
                case VRCTweenEase.InOutQuad: return t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;
                case VRCTweenEase.InCubic: return t * t * t;
                case VRCTweenEase.OutCubic: return 1f - Mathf.Pow(1f - t, 3f);
                case VRCTweenEase.InOutCubic: return t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
                case VRCTweenEase.InQuart: return Mathf.Pow(t, 4f);
                case VRCTweenEase.OutQuart: return 1f - Mathf.Pow(1f - t, 4f);
                case VRCTweenEase.InOutQuart: return t < 0.5f ? 8f * Mathf.Pow(t, 4f) : 1f - Mathf.Pow(-2f * t + 2f, 4f) / 2f;
                case VRCTweenEase.InQuint: return Mathf.Pow(t, 5f);
                case VRCTweenEase.OutQuint: return 1f - Mathf.Pow(1f - t, 5f);
                case VRCTweenEase.InOutQuint: return t < 0.5f ? 16f * Mathf.Pow(t, 5f) : 1f - Mathf.Pow(-2f * t + 2f, 5f) / 2f;
                case VRCTweenEase.InExpo: return t <= 0f ? 0f : Mathf.Pow(2f, 10f * t - 10f);
                case VRCTweenEase.OutExpo: return t >= 1f ? 1f : 1f - Mathf.Pow(2f, -10f * t);
                case VRCTweenEase.InOutExpo: return t <= 0f ? 0f : t >= 1f ? 1f : t < 0.5f ? Mathf.Pow(2f, 20f * t - 10f) / 2f : (2f - Mathf.Pow(2f, -20f * t + 10f)) / 2f;
                case VRCTweenEase.InCirc: return 1f - Mathf.Sqrt(1f - t * t);
                case VRCTweenEase.OutCirc: return Mathf.Sqrt(1f - Mathf.Pow(t - 1f, 2f));
                case VRCTweenEase.InOutCirc: return t < 0.5f ? (1f - Mathf.Sqrt(1f - Mathf.Pow(2f * t, 2f))) / 2f : (Mathf.Sqrt(1f - Mathf.Pow(-2f * t + 2f, 2f)) + 1f) / 2f;
                case VRCTweenEase.InElastic: return t <= 0f ? 0f : t >= 1f ? 1f : -Mathf.Pow(2f, 10f * t - 10f) * Mathf.Sin((t * 10f - 10.75f) * c4);
                case VRCTweenEase.OutElastic: return t <= 0f ? 0f : t >= 1f ? 1f : Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * c4) + 1f;
                case VRCTweenEase.InOutElastic: return t <= 0f ? 0f : t >= 1f ? 1f : t < 0.5f ? -(Mathf.Pow(2f, 20f * t - 10f) * Mathf.Sin((20f * t - 11.125f) * c5)) / 2f : Mathf.Pow(2f, -20f * t + 10f) * Mathf.Sin((20f * t - 11.125f) * c5) / 2f + 1f;
                case VRCTweenEase.InBack: return c3 * t * t * t - c1 * t * t;
                case VRCTweenEase.OutBack: return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
                case VRCTweenEase.InOutBack: return t < 0.5f ? Mathf.Pow(2f * t, 2f) * ((c2 + 1f) * 2f * t - c2) / 2f : (Mathf.Pow(2f * t - 2f, 2f) * ((c2 + 1f) * (t * 2f - 2f) + c2) + 2f) / 2f;
                case VRCTweenEase.InBounce: return 1f - OutBounce(1f - t);
                case VRCTweenEase.OutBounce: return OutBounce(t);
                case VRCTweenEase.InOutBounce: return t < 0.5f ? (1f - OutBounce(1f - 2f * t)) / 2f : (1f + OutBounce(2f * t - 1f)) / 2f;
                default: return t; // None, Linear
            }
        }

        private static float OutBounce(float t)
        {
            const float n = 7.5625f, d = 2.75f;
            if (t < 1f / d) return n * t * t;
            if (t < 2f / d) { t -= 1.5f / d; return n * t * t + 0.75f; }
            if (t < 2.5f / d) { t -= 2.25f / d; return n * t * t + 0.9375f; }
            t -= 2.625f / d; return n * t * t + 0.984375f;
        }
    }
}
