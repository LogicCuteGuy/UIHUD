using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;

namespace LogicCuteGuy.UIHUD
{
    public enum UIHudAnimEffect { Fade, FlyFromLeft, FlyFromRight, FlyFromBottom, FlyFromTop, Zoom, Pop, Spin, Typewriter }

    /// <summary>
    /// PowerPoint-style entrance animation of one widget, played with VRCTween each time the widget
    /// is shown (menu opened, page switched). The editor sets delay from the order on the page
    /// (With previous / After previous); Play() replays it from a button or your script.
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class UIHudAnim : UdonSharpBehaviour
    {
        public UIHudAnimEffect effect = UIHudAnimEffect.Fade;
        [Tooltip("Seconds.")] public float duration = 0.4f;
        public VRCTweenEase ease = VRCTweenEase.OutCubic;
        [Tooltip("Fade in together with the movement.")] public bool fade = true;
        [Tooltip("Fly distance in canvas px.")] public float distance = 160f;
        public bool playWhenShown = true;

        [Header("Order on the page (the editor computes Delay)")]
        [Tooltip("0 = With previous, 1 = After previous.")] public int startMode = 1;
        [Tooltip("Extra wait before this one, seconds.")] public float extraDelay;
        [Tooltip("Seconds after the page is shown. Computed by the editor from the order.")] public float delay;

        [HideInInspector] public int _chars; // written by the typewriter tween
        [HideInInspector] public float _fly; // fly-in distance left, 1 -> 0, written by its tween

        private CanvasGroup _group;
        private TextMeshProUGUI _text;
        private Vector3 _scale = Vector3.one;
        private Vector3 _euler;
        private Vector3 _flyTo, _flyOff;
        private bool _ready, _moving, _flying;
        // Value tweens are not tied to this GameObject, so KillAllTweens(gameObject) misses them: kill by handle.
        private VRCTweenHandle _flyTween, _charTween;

        private void Init()
        {
            if (_ready) return;
            _ready = true;
            _group = GetComponent<CanvasGroup>();
            _text = GetComponentInChildren<TextMeshProUGUI>(true);
            _scale = transform.localScale;
            _euler = transform.localEulerAngles;
        }

        private void OnEnable()
        {
            if (!playWhenShown) return;
            Init();
            if (_group != null) _group.alpha = 0f; // hidden until it starts
            // A list layout places a just-shown widget at the end of this frame: start after that.
            SendCustomEventDelayedFrames(nameof(Play), 1);
        }

        private void OnDisable()
        {
            if (!_ready) return;
            StopTweens();
            Finish();
        }

        /// <summary>Plays the entrance (after its delay). Call it from a button or your script to replay.</summary>
        public void Play()
        {
            Init();
            StopTweens();
            Finish();

            bool fly = effect == UIHudAnimEffect.FlyFromLeft || effect == UIHudAnimEffect.FlyFromRight
                || effect == UIHudAnimEffect.FlyFromBottom || effect == UIHudAnimEffect.FlyFromTop;
            if (fly)
            {
                Vector3 off = Vector3.zero;
                if (effect == UIHudAnimEffect.FlyFromLeft) off.x = -distance;
                else if (effect == UIHudAnimEffect.FlyFromRight) off.x = distance;
                else if (effect == UIHudAnimEffect.FlyFromBottom) off.y = -distance;
                else off.y = distance;
                _flyOff = off;
                _moving = true;
                _flying = false;
                // Not TweenLocalPosition from an offset start: a list/grid layout rebuild during the delay would snap
                // the widget home (it would fly home to home), and a home read before the layout ran is wrong.
                // It waits in its slot (hidden) and _OnFly reads the slot when the movement starts.
                _flyTween = VRCTween.TweenFloat(1f, 0f, duration, this, nameof(_fly), nameof(_OnFly), ease).SetDelay(delay).OnComplete(this, nameof(_Moved));
            }
            else if (effect == UIHudAnimEffect.Zoom || effect == UIHudAnimEffect.Pop)
            {
                transform.localScale = Vector3.zero;
                VRCTween.TweenScale(transform, _scale, duration, ease).SetDelay(delay);
            }
            else if (effect == UIHudAnimEffect.Spin)
            {
                transform.localEulerAngles = _euler + new Vector3(0f, 0f, 180f);
                VRCTween.TweenLocalRotation(transform, _euler, duration, ease).SetDelay(delay);
            }
            else if (effect == UIHudAnimEffect.Typewriter && _text != null)
            {
                _chars = 0;
                _text.maxVisibleCharacters = 0;
                _charTween = VRCTween.TweenInt(0, _text.text.Length, duration, this, nameof(_chars), nameof(_OnChars), VRCTweenEase.Linear)
                    .SetDelay(delay).OnComplete(this, nameof(Finish));
            }

            if (_group == null) return;
            if (effect == UIHudAnimEffect.Fade || effect == UIHudAnimEffect.Typewriter || fade)
            {
                _group.alpha = 0f;
                float d = effect == UIHudAnimEffect.Typewriter ? Mathf.Min(0.15f, duration) : duration; // box first, then letters
                VRCTween.TweenFade(_group, 1f, d, effect == UIHudAnimEffect.Typewriter ? VRCTweenEase.Linear : ease).SetDelay(delay);
            }
            else
            {
                _group.alpha = 0f; // pops in when it starts moving
                if (delay > 0f) VRCTween.DelayedCall(this, nameof(_Show), delay);
                else _Show();
            }
        }

        /// <summary>Places the fly-in. The first step (after the delay) reads the slot the layout gave the widget.</summary>
        public void _OnFly()
        {
            if (!_moving) return;
            if (!_flying) { _flyTo = transform.localPosition; _flying = true; }
            transform.localPosition = _flyTo + _flyOff * _fly;
        }

        private void StopTweens()
        {
            VRCTween.KillAllTweens(gameObject);
            if (_flyTween.IsValid) _flyTween.Kill();
            if (_charTween.IsValid) _charTween.Kill();
        }

        public void _Moved() { Land(); }

        /// <summary>Back in its slot (only if it had started moving; before that it never left).</summary>
        private void Land()
        {
            if (_moving && _flying) transform.localPosition = _flyTo;
            _moving = false;
            _flying = false;
        }

        public void _Show() { if (_group != null) _group.alpha = 1f; }

        public void _OnChars() { if (_text != null) _text.maxVisibleCharacters = _chars; }

        /// <summary>Final look (as edited); a fly-in that was cut short lands on its target.</summary>
        public void Finish()
        {
            Land();
            if (_group != null) _group.alpha = 1f;
            if (_text != null) _text.maxVisibleCharacters = 99999;
            transform.localScale = _scale;
            transform.localEulerAngles = _euler;
        }
    }
}
