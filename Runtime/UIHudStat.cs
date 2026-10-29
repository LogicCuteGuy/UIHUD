using TMPro;
using UdonSharp;
using UnityEngine;

namespace LogicCuteGuy.UIHUD
{
    /// <summary>
    /// Stat tile: small title, big number and the change since the previous value (+5 green / -3 red).
    /// SetValue(v), or point it at a number variable on any script.
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class UIHudStat : UdonSharpBehaviour
    {
        public TextMeshProUGUI title;
        public TextMeshProUGUI valueLabel;
        public TextMeshProUGUI deltaLabel;
        [Tooltip("{0} = value.")]
        public string format = "{0:N0}";
        [Tooltip("{0} = change. The default shows + or -.")]
        public string deltaFormat = "{0:+0;-0}";
        [Tooltip("Off = going down is good (green), e.g. lap time or deaths.")]
        public bool higherIsBetter = true;
        public Color upColor = new Color(0.45f, 0.9f, 0.5f);
        public Color downColor = new Color(1f, 0.45f, 0.45f);
        [Tooltip("Hide the change after this many seconds. 0 = keep it.")]
        public float deltaSeconds = 0f;
        public float value;

        [Header("Source (optional)")]
        [Tooltip("Script with a number variable (int / float / double).")]
        public UdonSharpBehaviour source;
        public string variable = "";
        public float interval = 0.2f;

        private bool _started;
        private float _next;
        private float _hideAt;

        private void Start()
        {
            _started = true;
            if (valueLabel != null) valueLabel.text = string.Format(format, value);
            if (deltaLabel != null) deltaLabel.text = "";
        }

        public void SetValue(float newValue)
        {
            float old = value;
            value = newValue;
            if (valueLabel != null) valueLabel.text = string.Format(format, value);
            if (!_started || deltaLabel == null || newValue == old) return;
            float d = newValue - old;
            deltaLabel.text = string.Format(deltaFormat, d);
            deltaLabel.color = (d > 0f) == higherIsBetter ? upColor : downColor;
            _hideAt = deltaSeconds > 0f ? Time.time + deltaSeconds : 0f;
        }

        private void Update()
        {
            if (_hideAt > 0f && Time.time >= _hideAt) { _hideAt = 0f; if (deltaLabel != null) deltaLabel.text = ""; }
            if (source == null || string.IsNullOrEmpty(variable) || Time.time < _next) return;
            _next = Time.time + interval;
            object v = source.GetProgramVariable(variable);
            if (v == null) return;
            System.Type t = v.GetType();
            float f = value;
            if (t == typeof(float)) f = (float)v;
            else if (t == typeof(int)) f = (int)v;
            else if (t == typeof(double)) f = (float)(double)v;
            if (f != value) SetValue(f);
        }
    }
}
