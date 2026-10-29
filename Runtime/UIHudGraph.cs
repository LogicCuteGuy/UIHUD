using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace LogicCuteGuy.UIHUD
{
    public enum UIHudGraphStyle { Line, Bars }

    /// <summary>
    /// Line or bar chart of a value over time (fps, score, players...), newest on the right.
    /// Feed it with AddSample(value) / SetSamples(values), or point it at a number variable on any script.
    /// The builder makes one bar and one line segment per point (Udon cannot add objects at runtime).
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class UIHudGraph : UdonSharpBehaviour
    {
        public UIHudGraphStyle style = UIHudGraphStyle.Line;
        [Tooltip("Plot area. Points are placed inside it.")]
        public RectTransform plot;
        [Tooltip("One per point, made by the builder.")]
        public RectTransform[] bars;
        [Tooltip("One per point (the first one is unused), made by the builder.")]
        public RectTransform[] lines;
        public float lineWidth = 4f;
        public Color color = new Color(0.35f, 0.75f, 1f, 1f);

        [Header("Range")]
        [Tooltip("Fit min/max to the samples. Off = use min and max below.")]
        public bool autoRange = true;
        public float min = 0f;
        public float max = 100f;
        [Tooltip("Optional: shows the top / bottom of the range and the newest value.")]
        public TextMeshProUGUI maxLabel;
        public TextMeshProUGUI minLabel;
        public TextMeshProUGUI valueLabel;
        [Tooltip("{0} = value. For the three labels.")]
        public string format = "{0:0}";

        [Header("Source (optional)")]
        [Tooltip("Script with a number variable (int / float / double). Sampled every interval.")]
        public UdonSharpBehaviour source;
        public string variable = "";
        [Tooltip("Seconds between samples. Also used by sampleFps.")]
        public float interval = 1f;
        [Tooltip("Sample the frame rate instead of a variable.")]
        public bool sampleFps;

        private float[] _values;
        private int _count;
        private float _next;
        private int _frames;
        private float _fpsTime;

        private void Start()
        {
            _values = new float[Mathf.Max(2, bars != null ? bars.Length : 2)];
            Render();
        }

        private void Update()
        {
            if (sampleFps) { _frames++; _fpsTime += Time.unscaledDeltaTime; }
            if ((source == null || string.IsNullOrEmpty(variable)) && !sampleFps) return;
            if (Time.time < _next) return;
            _next = Time.time + interval;
            if (sampleFps)
            {
                AddSample(_fpsTime > 0f ? _frames / _fpsTime : 0f);
                _frames = 0; _fpsTime = 0f;
                return;
            }
            object v = source.GetProgramVariable(variable);
            if (v == null) return;
            System.Type t = v.GetType();
            if (t == typeof(float)) AddSample((float)v);
            else if (t == typeof(int)) AddSample((int)v);
            else if (t == typeof(double)) AddSample((float)(double)v);
        }

        // ---------- Public API ----------

        /// <summary>Adds the newest point on the right; the oldest falls off the left.</summary>
        public void AddSample(float value)
        {
            if (_values == null) Start();
            if (_count < _values.Length) _values[_count++] = value;
            else
            {
                for (int i = 1; i < _values.Length; i++) _values[i - 1] = _values[i];
                _values[_values.Length - 1] = value;
            }
            Render();
        }

        /// <summary>Replaces all points (only the last ones that fit are shown).</summary>
        public void SetSamples(float[] values)
        {
            if (_values == null) Start();
            _count = 0;
            int from = Mathf.Max(0, values.Length - _values.Length);
            for (int i = from; i < values.Length; i++) _values[_count++] = values[i];
            Render();
        }

        public void Clear() { _count = 0; Render(); }

        public void SetStyle(int newStyle) { style = (UIHudGraphStyle)newStyle; Render(); }

        public void Render()
        {
            if (plot == null || bars == null || _values == null) return;
            int n = bars.Length;
            float lo = min, hi = max;
            if (autoRange && _count > 0)
            {
                lo = hi = _values[0];
                for (int i = 1; i < _count; i++) { lo = Mathf.Min(lo, _values[i]); hi = Mathf.Max(hi, _values[i]); }
                if (lo > 0f && lo < hi * 0.5f) lo = 0f; // keep 0 in view unless the values sit far above it
            }
            if (hi - lo < 0.0001f) { hi = lo + 1f; }
            Rect area = plot.rect;
            float step = n > 1 ? area.width / n : area.width;
            bool line = style == UIHudGraphStyle.Line;
            // Points fill from the right so the newest is always at the edge.
            int offset = n - _count;
            Vector2 prev = Vector2.zero;
            for (int i = 0; i < n; i++)
            {
                int k = i - offset;
                bool has = k >= 0;
                float t = has ? Mathf.Clamp01((_values[k] - lo) / (hi - lo)) : 0f;
                float x = step * (i + 0.5f);
                float y = t * area.height;
                RectTransform b = bars[i];
                if (b != null)
                {
                    b.gameObject.SetActive(has && !line);
                    b.anchoredPosition = new Vector2(x, 0f);
                    b.sizeDelta = new Vector2(step * 0.7f, Mathf.Max(2f, y));
                    Image img = b.GetComponent<Image>();
                    if (img != null) img.color = color;
                }
                RectTransform l = lines != null && i < lines.Length ? lines[i] : null;
                Vector2 p = new Vector2(x, y);
                if (l != null)
                {
                    bool show = line && has && k > 0;
                    l.gameObject.SetActive(show);
                    if (show)
                    {
                        Vector2 d = p - prev;
                        l.anchoredPosition = (p + prev) * 0.5f;
                        l.sizeDelta = new Vector2(d.magnitude + lineWidth * 0.5f, lineWidth);
                        l.localEulerAngles = new Vector3(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
                        Image img = l.GetComponent<Image>();
                        if (img != null) img.color = color;
                    }
                }
                prev = p;
            }
            if (maxLabel != null) maxLabel.text = string.Format(format, hi);
            if (minLabel != null) minLabel.text = string.Format(format, lo);
            if (valueLabel != null) valueLabel.text = _count > 0 ? string.Format(format, _values[_count - 1]) : "";
        }
    }
}
