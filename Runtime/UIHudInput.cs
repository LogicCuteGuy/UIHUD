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

        [Tooltip("Optional (found in the parents). While you type here it ignores the menu keys and hotbar keys 1-9.")]
        public UIHudManager manager;

        [System.NonSerialized] public string text = "";

        private void Start() { if (manager == null) manager = GetComponentInParent<UIHudManager>(); }

        private void Update()
        {
            if (manager != null && input != null && input.isFocused) manager._Typing();
        }

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
