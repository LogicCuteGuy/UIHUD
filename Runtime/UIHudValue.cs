using TMPro;
using UdonSharp;
using UnityEngine;

namespace LogicCuteGuy.UIHUD
{
    /// <summary>
    /// Shows one variable of any UdonBehaviour (score, coins, ammo...) and updates by itself.
    /// Or push it yourself with SetValue(value).
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class UIHudValue : UdonSharpBehaviour
    {
        public TextMeshProUGUI label;
        [Tooltip("Script that holds the value. Empty = only SetValue updates it.")]
        public UdonSharpBehaviour source;
        [Tooltip("Variable name on the source (public or private).")]
        public string variable = "";
        [Tooltip("{0} = the value. Number formats work too: {0:0.0}, {0:N0}, {0:P0}.")]
        public string format = "Coins: {0}";
        [Tooltip("Seconds between reads of the source.")]
        public float interval = 0.2f;

        private object _last;
        private bool _shown;
        private float _next;

        private void Start() { Refresh(); }

        private void Update()
        {
            if (source == null || Time.unscaledTime < _next) return;
            _next = Time.unscaledTime + interval;
            Refresh();
        }

        /// <summary>Reads the source now.</summary>
        public void Refresh()
        {
            if (source != null && !string.IsNullOrEmpty(variable)) Show(source.GetProgramVariable(variable));
            else if (!_shown) Show(null);
        }

        public void SetValue(object value) { Show(value); }

        private void Show(object value)
        {
            if (_shown && (value == null ? _last == null : value.Equals(_last))) return;
            _last = value;
            _shown = true;
            if (label != null) label.text = string.Format(format, value);
        }
    }
}
