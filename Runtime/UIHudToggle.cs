using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace LogicCuteGuy.UIHUD
{
    /// <summary>
    /// Toggle helper. Wire Toggle.onValueChanged -> SendCustomEvent("_OnValueChanged").
    /// Shows/hides objects to match the toggle state and reports the bool to the target.
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class UIHudToggle : UIHudWidget
    {
        public Toggle toggle;
        [Tooltip("Active while ON.")]
        public GameObject[] onObjects;
        [Tooltip("Active while OFF.")]
        public GameObject[] offObjects;

        [System.NonSerialized] public bool isOn;

        private void Start()
        {
            if (toggle == null) toggle = GetComponent<Toggle>();
            if (toggle != null) isOn = toggle.isOn;
            ApplyObjects();
        }

        public void _OnValueChanged()
        {
            if (toggle == null) return;
            isOn = toggle.isOn;
            ApplyObjects();
            Notify(isOn);
        }

        /// <summary>Set from code without firing the callback.</summary>
        public void SetIsOn(bool value)
        {
            isOn = value;
            if (toggle != null) toggle.SetIsOnWithoutNotify(value);
            ApplyObjects();
        }

        private void ApplyObjects()
        {
            if (onObjects != null) for (int i = 0; i < onObjects.Length; i++) if (onObjects[i] != null) onObjects[i].SetActive(isOn);
            if (offObjects != null) for (int i = 0; i < offObjects.Length; i++) if (offObjects[i] != null) offObjects[i].SetActive(!isOn);
        }
    }
}
