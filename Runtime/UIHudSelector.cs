using TMPro;
using UdonSharp;
using UnityEngine;

namespace LogicCuteGuy.UIHUD
{
    /// <summary>
    /// ◀ option ▶ picker (quality, skybox, music, ...). Wire the arrows to _OnPrev/_OnNext
    /// (the builder does this). Reports the index; can also show only the selected object.
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class UIHudSelector : UIHudWidget
    {
        public TextMeshProUGUI valueLabel;
        public string[] options = { "Low", "Medium", "High" };
        public int startIndex;
        [Tooltip("Wrap around at the ends.")]
        public bool loop = true;
        [Tooltip("Optional, one per option: only the selected one is active.")]
        public GameObject[] optionObjects;

        [System.NonSerialized] public int index;

        private void Start() { SetIndexWithoutNotify(startIndex); }

        public void _OnNext() { Step(1); }
        public void _OnPrev() { Step(-1); }

        public void SetIndex(int i) { SetIndexWithoutNotify(i); Notify(index); }

        public void SetIndexWithoutNotify(int i)
        {
            int n = options != null ? options.Length : 0;
            index = n == 0 ? 0 : Mathf.Clamp(i, 0, n - 1);
            if (valueLabel != null) valueLabel.text = n == 0 ? "" : options[index];
            if (optionObjects != null)
                for (int k = 0; k < optionObjects.Length; k++)
                    if (optionObjects[k] != null) optionObjects[k].SetActive(k == index);
        }

        public string Selected() { return options != null && options.Length > 0 ? options[index] : ""; }

        private void Step(int d)
        {
            int n = options != null ? options.Length : 0;
            if (n == 0) return;
            int i = index + d;
            if (loop) i = (i + n) % n;
            else i = Mathf.Clamp(i, 0, n - 1);
            if (i != index) SetIndex(i);
        }
    }
}
