using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;

namespace LogicCuteGuy.UIHUD
{
    /// <summary>
    /// Row of slots on its own World Space canvas. Every direct child is a slot: any widget
    /// (button, toggle, slider, bar...). Buttons select their slot when pressed.
    /// VR and desktop (mobile optional): floats below eye level, tilted down (45 deg by default), follows the local player's
    /// position but keeps its direction while they turn (swings back after followTurnAfter degrees).
    /// Desktop / mobile: pinned to the bottom of the screen. Desktop keys 1-9 press slots 1-9.
    /// Sends the callback with the selected index (int) as value.
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class UIHudHotbar : UIHudWidget
    {
        public UIHudManager manager;
        [Tooltip("Slot selected at start. -1 = none.")]
        public int selectedIndex = 0;
        [Tooltip("Tint of the selected slot.")]
        public Color selectedColor = new Color(0.25f, 0.6f, 1f, 1f);
        [Tooltip("Pressing the selected slot again deselects it.")]
        public bool allowDeselect;
        [Tooltip("Pixels around and between slots (the canvas Horizontal Layout Group uses the same).")]
        public float gap = 12f;
        [Tooltip("Nested canvas holding the slots (made by the builder). Empty = the slots are direct children of this canvas.")]
        public RectTransform content;
        [Tooltip("Put the slots canvas on the UI layer: the VRChat camera skips it unless its UI option is on. The pointer still hits this canvas's collider.")]
        public bool hideFromCamera = true;

        [Header("Show on")]
        public bool showInVR = true;
        public bool showOnDesktop = true;
        public bool showOnMobile = true;

        [Header("Locked to the body")]
        [Tooltip("Desktop: lock to the body like VR (look down to aim at it, or use keys 1-9). Off = bottom of the screen.")]
        public bool desktopLockToBody = true;
        [Tooltip("Mobile: lock to the body like VR. Off = bottom of the screen.")]
        public bool mobileLockToBody = false;
        [Tooltip("Degrees below eye level.")]
        public float vrAngle = 45f;
        [Tooltip("Distance from the eyes for a 1.6m avatar (scales with avatar height).")]
        public float vrDistance = 0.6f;
        [Tooltip("Meters per canvas pixel in VR (1.6m avatar).")]
        public float vrPixelSize = 0.0005f;
        [Tooltip("It follows your position but keeps its direction while you turn. Only when you have turned more than this many degrees away does it swing back in front of you. 0 = always turn with you, 180 = never.")]
        public float followTurnAfter = 90f;
        [Tooltip("How smoothly it swings back in front of you. 0 = snap.")]
        public float vrTurnSmoothing = 8f;
        private bool _turning;

        [Header("Bottom of the screen (desktop / mobile when not locked to the body)")]
        [Tooltip("Gap from the bottom of the screen in canvas pixels.")]
        public float screenMargin = 40f;
        [Tooltip("Desktop: keys 1-9 select slots 1-9.")]
        public bool numberKeys = true;

        [System.NonSerialized] public Transform[] slots;
        private Color[] _colors;
        private Image[] _images;
        private RectTransform _rt;
        private BoxCollider _col;
        private VRCPlayerApi _local;
        private float _yaw;
        private bool _placed;

        private void Start()
        {
            _local = Networking.LocalPlayer;
            // A world space canvas renders on its own layer; 5 = UI. Only the nested slots canvas moves,
            // this canvas (collider + Ui Shape) stays on its layer so the pointer still finds it.
            if (hideFromCamera && content != null && content != transform) content.gameObject.layer = 5;
            RefreshSlots();
        }

        // ---------- Public API ----------

        /// <summary>Re-reads the slots (the direct children). Call after adding/removing one at runtime.</summary>
        public void RefreshSlots()
        {
            if (_rt == null) { _rt = content != null ? content : (RectTransform)transform; _col = GetComponent<BoxCollider>(); }
            if (_images != null) for (int i = 0; i < _images.Length; i++) if (_images[i] != null) _images[i].color = _colors[i];
            int n = _rt.childCount;
            slots = new Transform[n];
            _images = new Image[n];
            _colors = new Color[n];
            float square = _rt.sizeDelta.y - 2f * gap;
            for (int i = 0; i < n; i++)
            {
                Transform t = _rt.GetChild(i);
                slots[i] = t;
                UIHudButton[] buttons = t.GetComponentsInChildren<UIHudButton>(true);
                for (int b = 0; b < buttons.Length; b++) buttons[b].hotbar = this;
                // A button added at runtime is only as wide as its text; make it at least square.
                LayoutElement le = t.GetComponent<LayoutElement>();
                if (le != null && le.preferredWidth < square) le.preferredWidth = square;
                _images[i] = t.GetComponent<Image>();
                if (_images[i] != null) _colors[i] = _images[i].color;
            }
            if (selectedIndex >= n) selectedIndex = n - 1;
            Highlight();
        }

        public void Select(int index)
        {
            if (slots == null) return;
            if (index < -1 || index >= slots.Length) return;
            selectedIndex = index;
            Highlight();
            Notify(selectedIndex);
        }

        public void SelectNext() { if (slots != null && slots.Length > 0) Select((selectedIndex + 1) % slots.Length); }
        public void SelectPrevious() { if (slots != null && slots.Length > 0) Select((selectedIndex - 1 + slots.Length) % slots.Length); }
        public void Deselect() { Select(-1); }

        public Transform SelectedSlot() { return slots != null && selectedIndex >= 0 && selectedIndex < slots.Length ? slots[selectedIndex] : null; }

        public void Show() { gameObject.SetActive(true); _placed = false; }
        public void Hide() { gameObject.SetActive(false); }
        public void ToggleVisible() { if (gameObject.activeSelf) Hide(); else Show(); }

        /// <summary>Called by a UIHudButton inside a slot when pressed.</summary>
        public void _Pick(UIHudButton button)
        {
            Transform t = button.transform;
            while (t != null && t.parent != _rt) t = t.parent;
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] != t) continue;
                Select(allowDeselect && i == selectedIndex ? -1 : i);
                return;
            }
        }

        // ---------- Per-frame ----------

        private void Update()
        {
            if (!numberKeys || manager == null || manager.platform != UIHudPlatform.Desktop || slots == null) return;
            int n = Mathf.Min(slots.Length, 9);
            for (int i = 0; i < n; i++)
                if (Input.GetKeyDown((i + 1).ToString())) { Press(i); return; }
        }

        public override void PostLateUpdate()
        {
            if (_local == null || manager == null) return;
            UIHudPlatform p = manager.platform;
            bool show = p == UIHudPlatform.VR ? showInVR : p == UIHudPlatform.Desktop ? showOnDesktop : showOnMobile;
            if (!show) { transform.localScale = Vector3.zero; return; } // stays active so Show/Hide stays the user's switch

            VRCPlayerApi.TrackingData head = _local.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
            bool body = p == UIHudPlatform.VR || (p == UIHudPlatform.Desktop ? desktopLockToBody : mobileLockToBody);
            if (body) PlaceOnBody(head);
            else PlaceScreen(head);
            // Width comes from the layout (Content Size Fitter); keep the pointer collider matching.
            if (_col != null && _col.size.x != _rt.sizeDelta.x) _col.size = new Vector3(_rt.sizeDelta.x, _rt.sizeDelta.y, 0f);
        }

        /// <summary>
        /// Locked to the body: the player's position + eye height and the player's (body) facing,
        /// never the head, so looking around or down never moves it.
        /// </summary>
        private void PlaceOnBody(VRCPlayerApi.TrackingData head)
        {
            float bodyYaw = _local.GetRotation().eulerAngles.y;
            float off = Mathf.DeltaAngle(_yaw, bodyYaw);
            if (!_placed) { _yaw = bodyYaw; _placed = true; off = 0f; }
            // Keep its direction while you turn; swing back once you have turned too far away.
            if (Mathf.Abs(off) > followTurnAfter) _turning = true;
            if (_turning)
            {
                if (vrTurnSmoothing <= 0f) _yaw = bodyYaw;
                else _yaw += off * (1f - Mathf.Exp(-vrTurnSmoothing * Time.deltaTime));
                if (Mathf.Abs(Mathf.DeltaAngle(_yaw, bodyYaw)) < 2f) _turning = false;
            }
            float scale = manager.Scale();
            Vector3 eye = _local.GetPosition() + Vector3.up * _local.GetAvatarEyeHeightAsMeters();
            Quaternion rot = Quaternion.Euler(vrAngle, _yaw, 0f);
            transform.SetPositionAndRotation(eye + rot * Vector3.forward * (vrDistance * scale), rot);
            transform.localScale = Vector3.one * (vrPixelSize * scale);
        }

        private void PlaceScreen(VRCPlayerApi.TrackingData head)
        {
            _placed = false;
            float px = manager.PixelScale();
            float y = -(manager.HalfHeight() - _rt.sizeDelta.y * 0.5f * px - screenMargin * px);
            Vector3 local = new Vector3(0f, y, manager.ViewDistance() * 0.97f); // in front of the HUD
            transform.SetPositionAndRotation(head.position + head.rotation * local, head.rotation);
            transform.localScale = Vector3.one * px;
        }

        // ---------- Helpers ----------

        private void Highlight()
        {
            if (_images == null) return;
            for (int i = 0; i < _images.Length; i++)
                if (_images[i] != null) _images[i].color = i == selectedIndex ? selectedColor : _colors[i];
        }

        /// <summary>Desktop key: presses the slot's button (its actions + select), or just selects it.</summary>
        private void Press(int i)
        {
            UIHudButton b = slots[i].GetComponentInChildren<UIHudButton>();
            if (b != null) b._OnClick();
            else Select(i);
        }
    }
}
