using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace LogicCuteGuy.UIHUD
{
    /// <summary>
    /// Pie / donut chart of named numbers (votes, team sizes...) with a legend.
    /// The builder makes the slices (Udon cannot add objects); entries beyond them are merged into "Other".
    /// Turn off the Hole object for a full pie.
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class UIHudPie : UIHudSeries
    {
        [Header("Pie")]
        [Tooltip("Radial 360 filled Images, one per slice, made by the builder.")]
        public Image[] slices;
        public TextMeshProUGUI legend;
        [Tooltip("{0} = name, {1} = value, {2} = percent.")]
        public string legendFormat = "{0}  {2:0}%";
        public TextMeshProUGUI centerLabel;
        [Tooltip("Text in the middle. {0} = total.")]
        public string centerFormat = "{0:0}";
        public string otherName = "Other";
        public string emptyText = "No data";

        public override void Render()
        {
            if (slices == null) return;
            float total = Total();
            int n = slices.Length;
            float cum = 0f;
            string text = "";
            for (int i = 0; i < n; i++)
            {
                Image img = slices[i];
                if (img == null) continue;
                bool has = i < _count && total > 0f;
                img.gameObject.SetActive(has);
                if (!has) continue;
                float value = _values[i];
                string name = _labels[i];
                if (i == n - 1 && _count > n)
                {
                    for (int k = n; k < _count; k++) value += _values[k];
                    name = otherName;
                }
                float share = value / total;
                Color c = ColorAt(i);
                img.color = c;
                img.fillAmount = share;
                img.rectTransform.localEulerAngles = new Vector3(0f, 0f, -cum * 360f); // fill starts at the top, clockwise
                cum += share;
                text += "<color=#" + Hex(c) + "><size=150%>•</size></color> " + string.Format(legendFormat, name, value, share * 100f) + "\n";
            }
            if (legend != null) legend.text = total > 0f ? text : emptyText;
            if (centerLabel != null) centerLabel.text = total > 0f ? string.Format(centerFormat, total) : "";
        }
    }
}
