using TMPro;
using UdonSharp;
using UnityEngine;

namespace LogicCuteGuy.UIHUD
{
    /// <summary>
    /// Toast messages on the HUD. Call Show("text") from any script; messages queue up.
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [RequireComponent(typeof(CanvasGroup))]
    public class UIHudNotification : UdonSharpBehaviour
    {
        public TextMeshProUGUI label;
        public float showSeconds = 2.5f;
        public float fadeSeconds = 0.25f;
        public AudioSource sound;

        // ponytail: fixed 16-slot ring queue, when full the oldest message is dropped.
        private string[] _queue = new string[16];
        private int _head, _count;
        private CanvasGroup _group;
        private float _timer;
        private bool _showing;

        private void Start()
        {
            _group = GetComponent<CanvasGroup>();
            _group.alpha = 0f;
        }

        public void Show(string message)
        {
            if (_count == _queue.Length) { _head = (_head + 1) % _queue.Length; _count--; }
            _queue[(_head + _count) % _queue.Length] = message;
            _count++;
            if (!_showing) Next();
        }

        private void Next()
        {
            if (_count == 0) { _showing = false; return; }
            if (label != null) label.text = _queue[_head];
            _head = (_head + 1) % _queue.Length;
            _count--;
            _showing = true;
            _timer = 0f;
            if (sound != null) sound.Play();
        }

        private void Update()
        {
            if (!_showing) return;
            _timer += Time.deltaTime;
            float total = showSeconds + fadeSeconds * 2f;
            if (_timer >= total) { _group.alpha = 0f; Next(); return; }
            float a = 1f;
            if (fadeSeconds > 0f)
            {
                if (_timer < fadeSeconds) a = _timer / fadeSeconds;
                else if (_timer > total - fadeSeconds) a = (total - _timer) / fadeSeconds;
            }
            _group.alpha = a;
        }
    }
}
