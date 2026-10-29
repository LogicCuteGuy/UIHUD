using TMPro;
using UdonSharp;
using UnityEngine;

namespace LogicCuteGuy.UIHUD
{
    /// <summary>
    /// Text input. VRChat shows its keyboard when the field is clicked. Wire
    /// TMP_InputField.onEndEdit -> _OnSubmit (the builder does this). Reports the text.
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class UIHudInput : UIHudWidget
    {
        public TMP_InputField input;
        public bool clearOnSubmit;
        [Tooltip("Optional label that shows the last submitted text.")]
        public TextMeshProUGUI echoLabel;
        public string echoFormat = "{0}";

        [System.NonSerialized] public string text = "";

        public void _OnSubmit()
        {
            if (input == null) return;
            text = input.text;
            if (echoLabel != null) echoLabel.text = string.Format(echoFormat, text);
            Notify(text);
            if (clearOnSubmit) input.SetTextWithoutNotify("");
        }

        public void SetText(string value)
        {
            text = value;
            if (input != null) input.SetTextWithoutNotify(value);
        }
    }
}
