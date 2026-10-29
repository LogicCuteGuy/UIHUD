using UdonSharp;
using UnityEngine;

namespace LogicCuteGuy.UIHUD
{
    public enum UIHudSide { Left, Right, Top, Bottom, Center }
    public enum UIHudEase { Smooth, Linear, EaseIn, EaseOut, Back, Bounce }

    /// <summary>
    /// A menu panel on its OWN World Space canvas (put this on the Canvas object, next to
    /// VRC Ui Shape). It slides in from a view edge when opened. Optional pages are switched
    /// with UIHudTab buttons.
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [RequireComponent(typeof(Canvas), typeof(CanvasGroup))]
    public class UIHudMenu : UdonSharpBehaviour
    {
        public UIHudManager manager;
        [Tooltip("World: a standalone panel that stays where you place it in the scene (no manager needed). Off: follows the player's view.")]
        public bool worldSpace;
        [Tooltip("World panels: visible when the world loads.")]
        public bool startOpen = true;
        public UIHudSide side = UIHudSide.Left;
        [Tooltip("Opened/closed by the manager's main button (desktop key, wrist, mobile button).")]
        public bool openWithMainButton = true;
        [Tooltip("Desktop key that opens/closes only this menu (None = no own key).")]
        public KeyCode openKey = KeyCode.None;
        [Tooltip("Gap between the panel and the view edge, in canvas pixels.")]
        public float margin = 40f;

        [Header("Animation")]
        [Tooltip("Open time in seconds. 0 = instant.")]
        public float animSeconds = 0.2f;
        [Tooltip("Close time in seconds. 0 = instant.")]
        public float closeSeconds = 0.15f;
        public UIHudEase ease = UIHudEase.Smooth;
        [Tooltip("Slide in from the side (Center menus don't slide).")]
        public bool animSlide = true;
        public bool animFade = true;
        [Tooltip("Grow from Scale From to full size.")]
        public bool animZoom = true;
        [Range(0f, 1.5f)] public float scaleFrom = 0.85f;
        [Tooltip("Page switch: fade time in seconds (pages need a CanvasGroup; the builder adds one). 0 = instant.")]
        public float pageFadeSeconds = 0.15f;
        [Tooltip("Page switch: the new page slides up from this many canvas pixels.")]
        public float pageSlide = 24f;

        [Header("Pages (optional)")]
        public GameObject[] pages;
        public int startPage;
        [Tooltip("Optional highlight objects, one per page (e.g. the selected look of each tab).")]
        public GameObject[] pageHighlights;
        [Tooltip("Scrolls the shown page when it is taller than the menu.")]
        public UnityEngine.UI.ScrollRect pageScroll;

        [Header("Callbacks (optional)")]
        public UdonSharpBehaviour callbackTarget;
        public string onOpenEvent = "";
        public string onCloseEvent = "";
        public string onPageChangedEvent = "";

        [System.NonSerialized] public bool isOpen;
        [System.NonSerialized] public int currentPage = -1;
        [System.NonSerialized] public Vector3 openedAt;

        private RectTransform _rect;
        private CanvasGroup _group;
        private float _t;       // 0 = hidden, 1 = shown
        private float _target;
        private bool _ready;
        private float _pageT = 1f;
        private CanvasGroup _pageGroup;
        private RectTransform _pageRect;

        // View frame, read from the manager on open (and every frame while following).
        private Vector3 _baseScale = Vector3.one;
        private Vector3 _pos;
        private Quaternion _rot = Quaternion.identity;
        private float _dist = 0.5f, _halfW = 0.4f, _halfH = 0.25f, _px = 0.0005f;

        private void Init()
        {
            if (_ready) return;
            _ready = true;
            _rect = (RectTransform)transform;
            _group = GetComponent<CanvasGroup>();
            _baseScale = transform.localScale; // world panels keep their scene scale
            if (pages != null && pages.Length > 0) ShowPage(startPage);
        }

        // The menu hides itself once its own program is running. Other behaviours must not
        // drive it before that: Udon calls into a not-yet-initialized behaviour are lost.
        private void Start()
        {
            Init();
            if (worldSpace && startOpen && !isOpen)
            {
                isOpen = true;
                _t = 1f;
                _target = 1f;
                Apply();
                return;
            }
            if (!isOpen) HideImmediate();
        }

        // ---------- Public API ----------

        public void Open()
        {
            Init();
            gameObject.SetActive(true);
            if (!worldSpace)
            {
                PullFrame();
                VRC.SDKBase.VRCPlayerApi local = VRC.SDKBase.Networking.LocalPlayer;
                if (local != null) openedAt = local.GetPosition();
            }
            isOpen = true;
            _target = 1f;
            Apply();
            if (manager != null) manager._OnMenuStateChanged();
            Notify(onOpenEvent);
        }

        public void Close()
        {
            if (!isOpen) return;
            Init();
            isOpen = false;
            _target = 0f;
            Notify(onCloseEvent);
        }

        public void Toggle() { if (isOpen) Close(); else Open(); }

        /// <summary>Instantly hidden and disabled (no animation, no callbacks).</summary>
        public void HideImmediate()
        {
            Init();
            isOpen = false;
            _t = 0f;
            _target = 0f;
            gameObject.SetActive(false);
        }

        /// <summary>Re-frame at the current head pose (the manager calls this each frame on mobile).</summary>
        public void _Follow()
        {
            if (!_ready || worldSpace) return;
            PullFrame();
            Apply();
        }

        /// <summary>Reads the head pose and view size from the manager.</summary>
        private void PullFrame()
        {
            VRC.SDKBase.VRCPlayerApi local = VRC.SDKBase.Networking.LocalPlayer;
            if (manager == null || local == null) return;
            VRC.SDKBase.VRCPlayerApi.TrackingData head = local.GetTrackingData(VRC.SDKBase.VRCPlayerApi.TrackingDataType.Head);
            _pos = head.position;
            _rot = head.rotation;
            _dist = manager.ViewDistance();
            _halfW = manager.HalfWidth();
            _halfH = manager.HalfHeight();
            _px = manager.PixelScale();
        }

        public void ShowPage(int index)
        {
            if (pages == null || pages.Length == 0) return;
            index = Mathf.Clamp(index, 0, pages.Length - 1);
            for (int i = 0; i < pages.Length; i++)
            {
                if (pages[i] != null) pages[i].SetActive(i == index);
                if (pageHighlights != null && i < pageHighlights.Length && pageHighlights[i] != null)
                    pageHighlights[i].SetActive(i == index);
            }
            if (pageScroll != null && pages[index] != null)
            {
                pageScroll.content = (RectTransform)pages[index].transform;
                pageScroll.verticalNormalizedPosition = 1f; // top
            }
            bool changed = currentPage != index;
            if (changed && currentPage >= 0 && pageFadeSeconds > 0f && pages[index] != null)
            {
                _pageT = 1f;
                ApplyPage(); // finish an interrupted transition first
                _pageGroup = pages[index].GetComponent<CanvasGroup>();
                _pageRect = (RectTransform)pages[index].transform;
                _pageT = 0f;
                ApplyPage();
            }
            currentPage = index;
            if (changed) Notify(onPageChangedEvent);
        }

        public void NextPage() { if (pages != null && pages.Length > 0) ShowPage((currentPage + 1) % pages.Length); }
        public void PrevPage() { if (pages != null && pages.Length > 0) ShowPage((currentPage - 1 + pages.Length) % pages.Length); }

        /// <summary>Change the side at runtime (e.g. from a settings button).</summary>
        public void SetSide(UIHudSide newSide) { side = newSide; if (_ready) Apply(); }
        public void SetSideLeft() { SetSide(UIHudSide.Left); }
        public void SetSideRight() { SetSide(UIHudSide.Right); }
        public void SetSideTop() { SetSide(UIHudSide.Top); }
        public void SetSideBottom() { SetSide(UIHudSide.Bottom); }
        public void SetSideCenter() { SetSide(UIHudSide.Center); }

        // ---------- Animation ----------

        private void Update()
        {
            if (_pageT < 1f)
            {
                _pageT = Mathf.MoveTowards(_pageT, 1f, Time.unscaledDeltaTime / pageFadeSeconds);
                ApplyPage();
            }
            if (_t == _target) return;
            float seconds = _target > _t ? animSeconds : closeSeconds;
            _t = seconds > 0f ? Mathf.MoveTowards(_t, _target, Time.unscaledDeltaTime / seconds) : _target;
            Apply();
            if (_t == 0f && !isOpen)
            {
                gameObject.SetActive(false);
                if (manager != null) manager._OnMenuStateChanged();
            }
        }

        private void ApplyPage()
        {
            float e = EaseValue(UIHudEase.EaseOut, _pageT);
            if (_pageGroup != null) _pageGroup.alpha = e;
            if (_pageRect != null) _pageRect.anchoredPosition = new Vector2(0f, -(1f - e) * pageSlide);
        }

        /// <summary>Easing curve, 0..1 in; Back and Bounce may go past 1 on the way.</summary>
        public static float EaseValue(UIHudEase curve, float t)
        {
            t = Mathf.Clamp01(t);
            switch (curve)
            {
                case UIHudEase.Linear: return t;
                case UIHudEase.EaseIn: return t * t * t;
                case UIHudEase.EaseOut: { float u = 1f - t; return 1f - u * u * u; }
                case UIHudEase.Back: { float u = t - 1f; return 1f + 2.70158f * u * u * u + 1.70158f * u * u; }
                case UIHudEase.Bounce:
                {
                    if (t < 0.3636f) return 7.5625f * t * t;
                    if (t < 0.7273f) { t -= 0.5455f; return 7.5625f * t * t + 0.75f; }
                    if (t < 0.9091f) { t -= 0.8182f; return 7.5625f * t * t + 0.9375f; }
                    t -= 0.9545f; return 7.5625f * t * t + 0.984375f;
                }
                default: return t * t * (3f - 2f * t);
            }
        }

        private void Apply()
        {
            float e = EaseValue(ease, _t);
            if (worldSpace)
            {
                // Stays where it was placed: only zoom and fade.
                float ws = animZoom ? Mathf.LerpUnclamped(scaleFrom, 1f, e) : 1f;
                transform.localScale = _baseScale * Mathf.Max(0.01f, ws);
                _group.alpha = animFade ? Mathf.Clamp01(e) : (_t > 0f ? 1f : 0f);
                _group.interactable = _target > 0.5f;
                _group.blocksRaycasts = _t > 0.01f;
                return;
            }
            Vector2 size = _rect.sizeDelta * _px;
            float m = margin * _px;
            Vector2 shown, hidden;
            switch (side)
            {
                case UIHudSide.Left:
                    shown = new Vector2(-_halfW + size.x * 0.5f + m, 0f);
                    hidden = shown - new Vector2(size.x + m, 0f); break;
                case UIHudSide.Right:
                    shown = new Vector2(_halfW - size.x * 0.5f - m, 0f);
                    hidden = shown + new Vector2(size.x + m, 0f); break;
                case UIHudSide.Top:
                    shown = new Vector2(0f, _halfH - size.y * 0.5f - m);
                    hidden = shown + new Vector2(0f, size.y + m); break;
                case UIHudSide.Bottom:
                    shown = new Vector2(0f, -_halfH + size.y * 0.5f + m);
                    hidden = shown - new Vector2(0f, size.y + m); break;
                default:
                    shown = Vector2.zero; hidden = Vector2.zero; break;
            }
            Vector2 p = animSlide ? Vector2.LerpUnclamped(hidden, shown, e) : shown;
            transform.SetPositionAndRotation(_pos + _rot * new Vector3(p.x, p.y, _dist), _rot);
            float s = animZoom ? Mathf.LerpUnclamped(scaleFrom, 1f, e) : 1f;
            transform.localScale = Vector3.one * (_px * Mathf.Max(0.01f, s));
            _group.alpha = animFade ? Mathf.Clamp01(e) : (_t > 0f ? 1f : 0f);
            _group.interactable = _target > 0.5f;
            _group.blocksRaycasts = _t > 0.01f;
        }

        private void Notify(string eventName)
        {
            if (callbackTarget != null && !string.IsNullOrEmpty(eventName)) callbackTarget.SendCustomEvent(eventName);
        }
    }
}
