using UdonSharp;
using UnityEngine;

namespace LogicCuteGuy.UIHUD.Examples
{
    /// <summary>
    /// Example 3: add, rename and remove widgets while playing. AddWidget copies the Manager's
    /// runtime templates (Manager Inspector → Make runtime templates).
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class ExampleRuntimeList : UdonSharpBehaviour
    {
        public UIHudManager manager;
        [Tooltip("Where new widgets go: a page, row, or a scroll list's content.")]
        public Transform list;
        public UIHudNotification notification;

        // Set by the toggles and sliders made here (their Value Variable).
        public bool toggled;
        public float sliderValue;

        private GameObject[] _items = new GameObject[50];
        private int _count;
        private int _made;

        public void AddButton() { Add("Button", "Button " + (_made + 1), nameof(OnItemButton), ""); }
        public void AddToggle() { Add("Toggle", "Toggle " + (_made + 1), nameof(OnItemToggle), nameof(toggled)); }
        public void AddSlider() { Add("Slider", "Slider " + (_made + 1), nameof(OnItemSlider), nameof(sliderValue)); }

        public void RenameLast()
        {
            if (_count == 0) return;
            manager.SetLabel(_items[_count - 1], "Renamed at " + System.DateTime.Now.ToString("HH:mm:ss"));
        }

        public void RemoveLast()
        {
            if (_count == 0) return;
            _count--;
            manager.RemoveWidget(_items[_count]);
            _items[_count] = null;
        }

        public void Clear() { while (_count > 0) RemoveLast(); }

        private void Add(string kind, string label, string eventName, string valueVariable)
        {
            if (_count >= _items.Length) { notification.Show("The list is full"); return; }
            GameObject go = manager.AddWidget(kind, list, label);
            if (go == null) { notification.Show("No '" + kind + "' template: Manager Inspector > Make runtime templates"); return; }
            manager.SetEvent(go, this, eventName, valueVariable);
            _items[_count] = go;
            _count++;
            _made++;
        }

        public void OnItemButton() { notification.Show("A runtime button was clicked"); }
        public void OnItemToggle() { notification.Show("A runtime toggle is now " + (toggled ? "on" : "off")); }
        public void OnItemSlider() { }
    }
}
