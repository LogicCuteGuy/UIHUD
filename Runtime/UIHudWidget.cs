using UdonSharp;
using UnityEngine;

namespace LogicCuteGuy.UIHUD
{
    /// <summary>
    /// Shared callback for all widgets: optionally writes the widget value into a variable
    /// on the target UdonBehaviour, then sends it a custom event.
    /// </summary>
    public abstract class UIHudWidget : UdonSharpBehaviour
    {
        [Header("Callback (optional)")]
        public UdonSharpBehaviour target;
        [Tooltip("Custom event sent to the target.")]
        public string eventName = "";
        [Tooltip("Variable on the target that receives the value before the event (leave empty to skip).")]
        public string valueVariable = "";

        protected void Notify(object value)
        {
            if (target == null) return;
            if (!string.IsNullOrEmpty(valueVariable)) target.SetProgramVariable(valueVariable, value);
            if (!string.IsNullOrEmpty(eventName)) target.SendCustomEvent(eventName);
        }
    }
}
