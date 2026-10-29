using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace LogicCuteGuy.UIHUD
{
    /// <summary>
    /// Slider helper. Wire Slider.onValueChanged -> SendCustomEvent("_OnValueChanged").
    /// Shows the value in a label and reports the float to the target.
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class UIHudSlider : UIHudWidget
    {
        public Slider slider;
        public TextMeshProUGUI valueLabel;
        [Tooltip("C# number format, e.g. 0.00, 0, P0 (percent).")]
        public string format = "0.00";
        public string prefix = "";
        public string suffix = "";
        [Tooltip("Optional: drive an AudioSource volume directly.")]
        public AudioSource audioVolume;

        [System.NonSerialized] public float value;

        private void Start()
        {
            if (slider == null) slider = GetComponent<Slider>();
            if (slider != null) value = slider.value;
            UpdateLabel();
        }

        public void _OnValueChanged()
        {
            if (slider == null) return;
            value = slider.value;
            UpdateLabel();
            if (audioVolume != null) audioVolume.volume = value;
            Notify(value);
        }

        /// <summary>Set from code without firing the callback.</summary>
        public void SetValue(float v)
        {
            value = v;
            if (slider != null) slider.SetValueWithoutNotify(v);
            UpdateLabel();
        }

        private void UpdateLabel()
        {
            if (valueLabel != null) valueLabel.text = prefix + value.ToString(format) + suffix;
        }
    }
}
