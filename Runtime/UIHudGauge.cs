using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace LogicCuteGuy.UIHUD
{
    /// <summary>
    /// Half-circle dial with a needle for one number (speed, temperature, volume).
    /// SetValue(v), or point it at a number variable on any script.
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class UIHudGauge : UdonSharpBehaviour
    {
        [Tooltip("Radial 360 filled Image starting on the left; half of it is the full dial.")]
        public Image fill;
        [Tooltip("Pivot at its bottom center, pointing up at 0 degrees.")]
        public RectTransform needle;
        public TextMeshProUGUI valueLabel;
        public TextMeshProUGUI minLabel;
        public TextMeshProUGUI maxLabel;
        [Tooltip("{0} = value.")]
        public string format = "{0:0}";
        public float min = 0f;
        public float max = 100f;
        public float value = 0f;
        [Tooltip("Needle speed (fraction of the dial per second). 0 = instant.")]
        public float smoothSpeed = 1.5f;
        public bool useGradient = true;
        public Gradient colorByValue;

        [Header("Source (optional)")]
        [Tooltip("Script with a number variable (int / float / double).")]
        public UdonSharpBehaviour source;
        public string variable = "";
        public float interval = 0.2f;

        private float _shown = -1f;
        private float _next;

        private void Start()
        {
            if (minLabel != null) minLabel.text = string.Format(format, min);
            if (maxLabel != null) maxLabel.text = string.Format(format, max);
            SetValue(value);
        }

        public void SetValue(float newValue)
        {
            value = newValue;
            if (valueLabel != null) valueLabel.text = string.Format(format, value);
            if (_shown < 0f || smoothSpeed <= 0f) { _shown = Target(); Apply(); }
        }

        private void Update()
        {
            if (source != null && !string.IsNullOrEmpty(variable) && Time.unscaledTime >= _next)
            {
                _next = Time.unscaledTime + interval;
                object v = source.GetProgramVariable(variable);
                if (v != null)
                {
                    System.Type t = v.GetType();
                    float f = value;
                    if (t == typeof(float)) f = (float)v;
                    else if (t == typeof(int)) f = (int)v;
                    else if (t == typeof(double)) f = (float)(double)v;
                    if (f != value) SetValue(f);
                }
            }
            float target = Target();
            if (_shown == target) return;
            _shown = Mathf.MoveTowards(_shown, target, smoothSpeed * Time.unscaledDeltaTime);
            Apply();
        }

        private float Target() { return max > min ? Mathf.Clamp01((value - min) / (max - min)) : 0f; }

        private void Apply()
        {
            if (fill != null)
            {
                fill.fillAmount = _shown * 0.5f;
                if (useGradient && colorByValue != null) fill.color = colorByValue.Evaluate(_shown);
            }
            if (needle != null) needle.localEulerAngles = new Vector3(0f, 0f, 90f - 180f * _shown);
        }
    }
}
