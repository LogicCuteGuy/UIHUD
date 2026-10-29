using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace LogicCuteGuy.UIHUD
{
    /// <summary>
    /// Progress / health / stamina bar. Call SetValue(current, max) from your scripts.
    /// The fill Image must use Image Type = Filled.
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class UIHudBar : UdonSharpBehaviour
    {
        public Image fill;
        public TextMeshProUGUI label;
        [Tooltip("{0} = current, {1} = max, {2} = percent")]
        public string labelFormat = "{0:0}/{1:0}";
        [Tooltip("Fill speed per second. 0 = instant.")]
        public float smoothSpeed = 3f;
        public bool useGradient;
        public Gradient colorByValue;

        public float current = 100f;
        public float max = 100f;

        private float _shown = -1f;

        private void Start() { Refresh(true); }

        public void SetValue(float newCurrent, float newMax)
        {
            max = Mathf.Max(0.0001f, newMax);
            current = Mathf.Clamp(newCurrent, 0f, max);
            Refresh(false);
        }

        public void SetCurrent(float newCurrent) { SetValue(newCurrent, max); }
        public void Add(float delta) { SetValue(current + delta, max); }

        private void Update()
        {
            float target = current / max;
            if (_shown == target) return;
            _shown = smoothSpeed > 0f ? Mathf.MoveTowards(_shown, target, smoothSpeed * Time.unscaledDeltaTime) : target;
            ApplyFill();
        }

        private void Refresh(bool instant)
        {
            if (instant || _shown < 0f) { _shown = current / max; ApplyFill(); }
            if (label != null) label.text = string.Format(labelFormat, current, max, current / max * 100f);
        }

        private void ApplyFill()
        {
            if (fill == null) return;
            fill.fillAmount = _shown;
            if (useGradient && colorByValue != null) fill.color = colorByValue.Evaluate(_shown);
        }
    }
}
