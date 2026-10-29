using UdonSharp;
using UnityEngine;
using VRC.SDK3.Rendering;
using VRC.SDKBase;

namespace LogicCuteGuy.UIHUD
{
    public enum UIHudPlatform { Desktop, VR, Mobile }

    /// <summary>
    /// Root of the UIHUD system. HUD, every menu, the mobile button and the wrist buttons are
    /// separate World Space canvases; this behaviour places them in the player's view.
    /// HUD fills the view. Menus are framed at a view edge (see UIHudMenu.side).
    /// Everything is local-only; nothing is synced.
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class UIHudManager : UdonSharpBehaviour
    {
        [Header("Canvases (each its own World Space canvas)")]
        [Tooltip("Always-visible HUD canvas. No VRC Ui Shape, so it never blocks world interaction.")]
        public RectTransform hudCanvas;
        [Tooltip("Menu canvases. Each has its own VRC Ui Shape sized to the panel only.")]
        public UIHudMenu[] menus;

        [Header("Open Buttons")]
        public KeyCode desktopKey = KeyCode.M;
        [Tooltip("Small canvas on one VR wrist, pressed with the other hand. Shown in VR only. Leave empty to disable.")]
        public Transform wristButton;
        [Tooltip("Wear the wrist button on the right hand (press it with the left).")]
        public bool wristOnRightHand;
        [Tooltip("Offset in hand space (left hand). X is mirrored for the right hand.")]
        public Vector3 wristOffset = new Vector3(0f, 0.05f, -0.03f);
        [Tooltip("Small canvas pinned to a screen corner. Shown on mobile only.")]
        public RectTransform mobileButton;
        [Tooltip("-1..1 view position of the mobile button. (1,1) = top-right.")]
        public Vector2 mobileButtonAnchor = new Vector2(1f, 1f);
        [Tooltip("Gap from the view edge in canvas pixels.")]
        public float mobileButtonMargin = 60f;

        [Header("Layout")]
        [Tooltip("Distance of the canvases from the eyes for a 1.6m avatar. Scales with avatar height.")]
        public float viewDistance = 0.5f;
        [Tooltip("Reference view height in canvas pixels. All canvases use this pixel size.")]
        public float canvasHeight = 1080f;
        [Tooltip("Vertical FOV the UI covers in VR (headset FOV is not readable, keep it comfortable).")]
        public float vrFov = 50f;
        public float vrAspect = 1.3f;

        [Header("Behaviour")]
        [Tooltip("VR HUD rotation smoothing. 0 = rigid.")]
        public float vrHudSmoothing = 10f;
        [Tooltip("Mobile menus follow the head (tap anywhere). Desktop/VR menus stay where they opened so you can aim at them.")]
        public bool menuFollowsHeadOnMobile = true;
        [Tooltip("Close a world-locked menu when the player walks this far away. 0 = never.")]
        public float autoCloseDistance = 2.5f;
        public bool startWithHud = true;
        [Tooltip("Put the HUD on the UI layer: the VRChat camera and streams skip it unless their UI option is on. (Menus stay visible: the pointer ignores the UI layer. The hotbar has its own hideFromCamera.)")]
        public bool hideHudFromCamera = true;

        [Header("Runtime widgets")]
        [Tooltip("Hidden widgets that AddWidget copies, named by kind (Button, Toggle, Slider...). Made with the Inspector's Make runtime templates.")]
        public GameObject[] widgetTemplates;

        [Header("Callbacks (optional)")]
        public UdonSharpBehaviour callbackTarget;
        public string onMenuOpenedEvent = "";
        public string onMenuClosedEvent = "";
        public string onPlatformChangedEvent = "";

        [System.NonSerialized] public UIHudPlatform platform = UIHudPlatform.Desktop;
        [System.NonSerialized] public bool isMenuOpen;

        private VRCPlayerApi _local;
        private float _scale = 1f;
        private float _fov = 60f, _aspect = 16f / 9f;
        private int _lastPixelW, _lastPixelH;
        private bool _touch;

        private void Start()
        {
            _local = Networking.LocalPlayer;
            if (_local == null) return;
            _scale = _local.GetAvatarEyeHeightAsMeters() / 1.6f;
            // Menus hide themselves in their own Start (see UIHudMenu.Start).
            DetectPlatform();
            // A world space canvas renders on its own layer; 5 = UI (display only, the HUD has no Ui Shape).
            if (hideHudFromCamera && hudCanvas != null) hudCanvas.gameObject.layer = 5;
            SetHudVisible(startWithHud);
        }

        // ---------- View frame ----------

        /// <summary>Avatar eye height / 1.6m.</summary>
        public float Scale() { return _scale; }
        public float ViewDistance() { return viewDistance * _scale; }
        public float HalfHeight() { return ViewDistance() * Mathf.Tan(_fov * 0.5f * Mathf.Deg2Rad); }
        public float HalfWidth() { return HalfHeight() * _aspect; }
        /// <summary>Meters per canvas pixel at the view distance.</summary>
        public float PixelScale() { return 2f * HalfHeight() / canvasHeight; }

        // ---------- Public API ----------

        /// <summary>Opens every menu with openWithMainButton, or closes all open menus.</summary>
        public void ToggleMenu() { if (AnyMenuOpen()) CloseMenu(); else OpenMenu(); }

        public void OpenMenu()
        {
            for (int i = 0; i < menus.Length; i++)
                if (menus[i] != null && menus[i].openWithMainButton) menus[i].Open();
        }

        public void CloseMenu()
        {
            for (int i = 0; i < menus.Length; i++)
                if (menus[i] != null && menus[i].isOpen) menus[i].Close();
        }

        /// <summary>Move the VR wrist button to the other hand (e.g. from a settings button or toggle).</summary>
        public void SetWristLeft() { wristOnRightHand = false; }
        public void SetWristRight() { wristOnRightHand = true; }
        public void SwapWristHand() { wristOnRightHand = !wristOnRightHand; }

        public void ShowHud() { SetHudVisible(true); }
        public void HideHud() { SetHudVisible(false); }
        public void ToggleHud() { if (hudCanvas != null) SetHudVisible(!hudCanvas.gameObject.activeSelf); }

        public void SetHudVisible(bool visible)
        {
            if (hudCanvas == null) return;
            hudCanvas.gameObject.SetActive(visible);
            if (visible) { ResizeHud(); if (_local != null) PlaceHud(false); }
        }

        // ---------- Runtime widgets (local only) ----------

        /// <summary>
        /// Copies the template named kind ("Button", "Toggle", "Slider", "Label"...) into parent (a page,
        /// row, the HUD...) and sets its label. Returns the new widget, or null if there is no such template.
        /// </summary>
        public GameObject AddWidget(string kind, Transform parent, string label)
        {
            if (widgetTemplates == null || parent == null) return null;
            for (int i = 0; i < widgetTemplates.Length; i++)
            {
                GameObject t = widgetTemplates[i];
                if (t == null || t.name != kind) continue;
                GameObject go = Instantiate(t);
                go.transform.SetParent(parent, false);
                go.name = kind;
                go.SetActive(true);
                if (!string.IsNullOrEmpty(label)) SetLabel(go, label);
                return go;
            }
            return null;
        }

        public void RemoveWidget(GameObject widget) { if (widget != null) Destroy(widget); }

        /// <summary>Text of the widget's first label (button text, toggle label, slider name, header...).</summary>
        public void SetLabel(GameObject widget, string text)
        {
            if (widget == null) return;
            TMPro.TextMeshProUGUI t = widget.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
            if (t != null) t.text = text;
        }

        /// <summary>Where the widget reports clicks / changes: eventName is sent to target (value into valueVariable).</summary>
        public void SetEvent(GameObject widget, UdonSharpBehaviour eventTarget, string eventName, string valueVariable)
        {
            if (widget == null) return;
            UIHudWidget w = widget.GetComponentInChildren<UIHudWidget>(true);
            if (w == null) return;
            w.target = eventTarget;
            w.eventName = eventName;
            w.valueVariable = valueVariable;
        }

        /// <summary>Called by UIHudMenu after it opened or closed.</summary>
        public void _OnMenuStateChanged()
        {
            bool open = AnyMenuOpen();
            if (open == isMenuOpen) return;
            isMenuOpen = open;
            Fire(open ? onMenuOpenedEvent : onMenuClosedEvent);
        }

        private bool AnyMenuOpen()
        {
            for (int i = 0; i < menus.Length; i++) if (menus[i] != null && menus[i].isOpen) return true;
            return false;
        }

        // ---------- Platform ----------

        public override void OnInputMethodChanged(VRCInputMethod inputMethod)
        {
            _touch = inputMethod == VRCInputMethod.Touch;
            DetectPlatform();
        }

        public override void OnAvatarEyeHeightChanged(VRCPlayerApi player, float prevEyeHeightAsMeters)
        {
            if (player == null || !player.isLocal) return;
            _scale = player.GetAvatarEyeHeightAsMeters() / 1.6f;
            RefreshView();
        }

        public override void OnVRCCameraSettingsChanged(VRCCameraSettings cameraSettings)
        {
            RefreshView();
        }

        private void DetectPlatform()
        {
            UIHudPlatform old = platform;
            bool mobileBuild = false;
#if UNITY_ANDROID || UNITY_IOS
            mobileBuild = true;
#endif
            if (_local.IsUserInVR()) platform = UIHudPlatform.VR;
            else if (mobileBuild || _touch) platform = UIHudPlatform.Mobile;
            else platform = UIHudPlatform.Desktop;

            if (wristButton != null) wristButton.gameObject.SetActive(platform == UIHudPlatform.VR);
            if (mobileButton != null) mobileButton.gameObject.SetActive(platform == UIHudPlatform.Mobile);
            RefreshView();
            if (old != platform) Fire(onPlatformChangedEvent);
        }

        private void RefreshView()
        {
            _fov = vrFov;
            _aspect = vrAspect;
            if (platform != UIHudPlatform.VR)
            {
                VRCCameraSettings cam = VRCCameraSettings.ScreenCamera;
                _fov = cam.FieldOfView;
                if (cam.PixelHeight > 0) _aspect = (float)cam.PixelWidth / cam.PixelHeight;
            }
            if (hudCanvas != null && hudCanvas.gameObject.activeSelf) ResizeHud();
        }

        // ---------- Per-frame ----------

        private void Update()
        {
            if (platform != UIHudPlatform.Desktop) return;
            if (Input.GetKeyDown(desktopKey)) ToggleMenu();
            // Menus with their own key (a closed menu is inactive, so it can't listen itself).
            for (int i = 0; i < menus.Length; i++)
            {
                UIHudMenu m = menus[i];
                if (m != null && m.openKey != KeyCode.None && m.openKey != desktopKey && Input.GetKeyDown(m.openKey)) m.Toggle();
            }
        }

        public override void PostLateUpdate()
        {
            if (_local == null) return;

            if (platform != UIHudPlatform.VR)
            {
                // Mobile rotation / desktop window resize changes the aspect.
                VRCCameraSettings cam = VRCCameraSettings.ScreenCamera;
                if (cam.PixelWidth != _lastPixelW || cam.PixelHeight != _lastPixelH)
                {
                    _lastPixelW = cam.PixelWidth;
                    _lastPixelH = cam.PixelHeight;
                    RefreshView();
                }
            }

            if (hudCanvas != null && hudCanvas.gameObject.activeSelf)
                PlaceHud(platform == UIHudPlatform.VR && vrHudSmoothing > 0f);

            if (isMenuOpen)
            {
                bool follow = platform == UIHudPlatform.Mobile && menuFollowsHeadOnMobile;
                Vector3 me = _local.GetPosition();
                for (int i = 0; i < menus.Length; i++)
                {
                    UIHudMenu m = menus[i];
                    if (m == null || !m.isOpen || m.worldSpace) continue;
                    if (follow) m._Follow();
                    else if (autoCloseDistance > 0f && Vector3.Distance(me, m.openedAt) > autoCloseDistance * _scale) m.Close();
                }
            }

            if (mobileButton != null && platform == UIHudPlatform.Mobile) PlaceMobileButton();

            if (platform == UIHudPlatform.VR && wristButton != null)
            {
                if (wristOnRightHand) PlaceOnHand(wristButton, VRCPlayerApi.TrackingDataType.RightHand, new Vector3(-wristOffset.x, wristOffset.y, wristOffset.z));
                else PlaceOnHand(wristButton, VRCPlayerApi.TrackingDataType.LeftHand, wristOffset);
            }
        }

        private void PlaceOnHand(Transform t, VRCPlayerApi.TrackingDataType hand, Vector3 offset)
        {
            VRCPlayerApi.TrackingData data = _local.GetTrackingData(hand);
            t.SetPositionAndRotation(data.position + data.rotation * (offset * _scale), data.rotation);
            t.localScale = Vector3.one * (0.0005f * _scale);
        }

        // ---------- Helpers ----------

        private void PlaceHud(bool smooth)
        {
            VRCPlayerApi.TrackingData head = _local.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
            Quaternion rot = smooth
                ? Quaternion.Slerp(hudCanvas.rotation, head.rotation, 1f - Mathf.Exp(-vrHudSmoothing * Time.unscaledDeltaTime))
                : head.rotation;
            hudCanvas.SetPositionAndRotation(head.position + rot * Vector3.forward * ViewDistance(), rot);
        }

        /// <summary>The HUD canvas exactly fills the view, so it can be laid out like a screen.</summary>
        private void ResizeHud()
        {
            hudCanvas.sizeDelta = new Vector2(canvasHeight * _aspect, canvasHeight);
            hudCanvas.localScale = Vector3.one * PixelScale();
        }

        private void PlaceMobileButton()
        {
            VRCPlayerApi.TrackingData head = _local.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
            float px = PixelScale();
            Vector2 half = mobileButton.sizeDelta * (0.5f * px);
            float m = mobileButtonMargin * px;
            Vector3 local = new Vector3(
                mobileButtonAnchor.x * (HalfWidth() - half.x - m),
                mobileButtonAnchor.y * (HalfHeight() - half.y - m),
                ViewDistance() * 0.98f); // slightly in front of the HUD
            mobileButton.SetPositionAndRotation(head.position + head.rotation * local, head.rotation);
            mobileButton.localScale = Vector3.one * px;
        }

        private void Fire(string eventName)
        {
            if (callbackTarget != null && !string.IsNullOrEmpty(eventName)) callbackTarget.SendCustomEvent(eventName);
        }
    }
}
