using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace LogicCuteGuy.UIHUD
{
    /// <summary>
    /// Horizontal bar per entry (team scores, poll results): name, bar, value. Sorted biggest first by default.
    /// The builder makes the rows (Udon cannot add objects); unused rows hide.
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class UIHudBarList : UIHudSeries
    {
        [Header("Bars")]
        [Tooltip("One per row, made by the builder.")]
        public GameObject[] rows;
        public TextMeshProUGUI[] names;
        [Tooltip("Bar Images; their width (right anchor) shows the value.")]
        public Image[] fills;
        public TextMeshProUGUI[] valueLabels;
        [Tooltip("Bar full at the biggest value. Off = full at Max.")]
        public bool autoMax = true;
        public float max = 100f;
        [Tooltip("Each bar its own color from Colors. Off = all use the first color.")]
        public bool colorEach = true;
        [Tooltip("Show the percent of the total instead of the value.")]
        public bool showPercent;
        [Tooltip("Fill speed per second. 0 = instant.")]
        public float smoothSpeed = 2f;

        private float[] _target;

        public override void Render()
        {
            if (rows == null) return;
            int n = rows.Length;
            if (_target == null || _target.Length != n) _target = new float[n];
            float top = max;
            if (autoMax) { top = 0f; for (int i = 0; i < _count; i++) top = Mathf.Max(top, _values[i]); }
            float total = Total();
            for (int i = 0; i < n; i++)
            {
                bool has = i < _count;
                if (rows[i] != null) rows[i].SetActive(has);
                if (!has) continue;
                float v = _values[i];
                _target[i] = top > 0f ? Mathf.Clamp01(v / top) : 0f;
                if (names != null && i < names.Length && names[i] != null) names[i].text = _labels[i];
                if (valueLabels != null && i < valueLabels.Length && valueLabels[i] != null)
                    valueLabels[i].text = showPercent ? (total > 0f ? (v / total * 100f).ToString("0") + "%" : "0%") : string.Format(valueFormat, v);
                if (fills != null && i < fills.Length && fills[i] != null)
                {
                    fills[i].color = ColorAt(colorEach ? i : 0);
                    if (smoothSpeed <= 0f) SetFill(fills[i], _target[i]);
                }
            }
        }

        private void LateUpdate()
        {
            if (smoothSpeed <= 0f || fills == null || _target == null) return;
            for (int i = 0; i < fills.Length && i < _target.Length; i++)
            {
                if (fills[i] == null) continue;
                float shown = fills[i].rectTransform.anchorMax.x;
                if (shown != _target[i]) SetFill(fills[i], Mathf.MoveTowards(shown, _target[i], smoothSpeed * Time.deltaTime));
            }
        }

        // Width by anchor, not fillAmount: it needs no sprite and keeps rounded sprites from smearing.
        private void SetFill(Image fill, float t)
        {
            RectTransform rt = fill.rectTransform;
            rt.anchorMax = new Vector2(t, 1f);
            rt.offsetMax = new Vector2(0f, rt.offsetMax.y);
        }
    }
}
