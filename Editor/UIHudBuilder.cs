using TMPro;
using UdonSharp;
using UdonSharpEditor;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using VRC.SDK3.Components;

namespace LogicCuteGuy.UIHUD.Editor
{
    /// <summary>
    /// GameObject > UIHUD menu: one click builds a full HUD + menu system, or adds single
    /// widgets under the selected UI object with all events already wired.
    /// </summary>
    internal enum UIHudWidgetKind { Button, Toggle, Slider, Label, Bar, Notification, Image, Space, Selector, Input, PlayerList, Info,
        PagesTabs, PagesSelector, Row, Grid, Scroll, Header, Divider, Radial, Value, Table, Graph, Stat, Pie, BarList, Gauge }

    public static class UIHudBuilder
    {
        private const string Root = "GameObject/UIHUD/";
        private const int Prio = 10;

        private static readonly Color Panel = new Color(0.08f, 0.09f, 0.12f, 0.92f);
        private static readonly Color Accent = new Color(0.25f, 0.6f, 1f, 1f);
        private static readonly Color Soft = new Color(1f, 1f, 1f, 0.08f);

        private static TMP_DefaultControls.Resources _res;
        private static TMP_DefaultControls.Resources Res
        {
            get
            {
                if (_res.standard != null) return _res;
                _res.standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
                _res.background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
                _res.inputField = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/InputFieldBackground.psd");
                _res.knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
                _res.checkmark = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd");
                _res.dropdown = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/DropdownArrow.psd");
                _res.mask = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UIMask.psd");
                return _res;
            }
        }

        // =====================================================================
        // Full system
        // =====================================================================

        [MenuItem(Root + "UIHUD System (HUD + Menu)", false, Prio)]
        private static void CreateSystem(MenuCommand cmd)
        {
            var root = new GameObject("UIHUD");
            Undo.RegisterCreatedObjectUndo(root, "Create UIHUD");
            GameObjectUtility.SetParentAndAlign(root, cmd.context as GameObject);
            var manager = UdonSharpUndo.AddComponent<UIHudManager>(root);

            // HUD canvas: display only (no Ui Shape), never blocks world interaction.
            RectTransform hud = CreateCanvas("HUD Canvas", root.transform, false, new Vector2(1920f, 1080f));
            hud.GetComponent<Canvas>().sortingOrder = 0;
            var bar = CreateBar(hud, "Health");
            Anchor((RectTransform)bar.transform, new Vector2(0f, 0f), new Vector2(60f, 60f), new Vector2(420f, 44f));
            var toast = CreateNotification(hud);
            Anchor((RectTransform)toast.transform, new Vector2(0.5f, 1f), new Vector2(0f, -80f), new Vector2(700f, 90f));

            // Mobile open button: its own small canvas, so only the button is tappable.
            RectTransform mobile = CreateCanvas("MobileButton Canvas", root.transform, true, new Vector2(140f, 140f));
            mobile.GetComponent<Canvas>().sortingOrder = 20;
            var mobileBtn = CreateButton(mobile, "MENU");
            Stretch((RectTransform)mobileBtn.transform);
            mobileBtn.manager = manager;
            mobileBtn.toggleMenu = true;
            Sync(mobileBtn);

            // VR wrist button: one small canvas on one hand, pressed with the other.
            RectTransform wrist = CreateWristButton(root.transform, manager);

            // Main menu: big and centered, on its own canvas separate from the HUD.
            UIHudMenu menu = CreateMenuPanel(root.transform, UIHudSide.Center, manager, true);

            manager.hudCanvas = hud;
            manager.menus = new[] { menu };
            manager.wristButton = wrist;
            manager.mobileButton = mobile;
            Sync(manager);

            EnsureEventSystem();
            Selection.activeGameObject = root;
        }

        // =====================================================================
        // Menu panels
        // =====================================================================

        [MenuItem(Root + "Menu Panel/Left", false, Prio + 20)] private static void MenuLeft() { AddMenu(UIHudSide.Left); }
        [MenuItem(Root + "Menu Panel/Right", false, Prio + 21)] private static void MenuRight() { AddMenu(UIHudSide.Right); }
        [MenuItem(Root + "Menu Panel/Top", false, Prio + 22)] private static void MenuTop() { AddMenu(UIHudSide.Top); }
        [MenuItem(Root + "Menu Panel/Bottom", false, Prio + 23)] private static void MenuBottom() { AddMenu(UIHudSide.Bottom); }
        [MenuItem(Root + "Menu Panel/Center", false, Prio + 24)] private static void MenuCenter() { AddMenu(UIHudSide.Center); }

        private static void AddMenu(UIHudSide side)
        {
            var manager = Object.FindObjectOfType<UIHudManager>();
            if (manager == null)
            {
                EditorUtility.DisplayDialog("UIHUD", "Create a UIHUD System first (GameObject > UIHUD > UIHUD System).", "OK");
                return;
            }
            Selection.activeGameObject = AddMenu(manager, side).gameObject;
        }

        internal static UIHudMenu AddMenu(UIHudManager manager, UIHudSide side)
        {
            UIHudMenu menu = CreateMenuPanel(manager.transform, side, manager, false);
            Undo.RecordObject(manager, "Add UIHUD Menu");
            manager.menus = Append(manager.menus, menu);
            Sync(manager);
            return menu;
        }

        /// <summary>One entry point for the inspectors: adds a widget of the given kind under parent.</summary>
        internal static GameObject AddWidget(Transform parent, UIHudWidgetKind kind)
        {
            switch (kind)
            {
                case UIHudWidgetKind.Button: return CreateButton(parent, "Button").gameObject;
                case UIHudWidgetKind.Toggle: return CreateToggle(parent, "Toggle").gameObject;
                case UIHudWidgetKind.Slider: return CreateSlider(parent, "Slider").transform.parent.gameObject;
                case UIHudWidgetKind.Bar:
                    var bar = CreateBar(parent, "Value").gameObject;
                    bar.name = "Bar";
                    return bar;
                case UIHudWidgetKind.Notification: return CreateNotification(parent).gameObject;
                case UIHudWidgetKind.Image:
                    var img = CreateRect("Image", parent);
                    img.sizeDelta = new Vector2(160f, 160f);
                    var image = img.gameObject.AddComponent<Image>();
                    image.preserveAspect = true;
                    image.raycastTarget = false;
                    Layout(img.gameObject, -1f, 160f);
                    return img.gameObject;
                case UIHudWidgetKind.Selector: return CreateSelector(parent, "Quality").gameObject;
                case UIHudWidgetKind.Input: return CreateInput(parent).gameObject;
                case UIHudWidgetKind.PlayerList: return CreatePlayerList(parent).gameObject;
                case UIHudWidgetKind.Info: return CreateInfo(parent).gameObject;
                case UIHudWidgetKind.Value: return CreateValue(parent).gameObject;
                case UIHudWidgetKind.Table: return CreateTable(parent).gameObject;
                case UIHudWidgetKind.Graph: return CreateGraph(parent).gameObject;
                case UIHudWidgetKind.Stat: return CreateStat(parent).gameObject;
                case UIHudWidgetKind.Pie: return CreatePie(parent).gameObject;
                case UIHudWidgetKind.BarList: return CreateBarList(parent).gameObject;
                case UIHudWidgetKind.Gauge: return CreateGauge(parent).gameObject;
                case UIHudWidgetKind.PagesTabs: return CreatePages(parent, false).gameObject;
                case UIHudWidgetKind.PagesSelector: return CreatePages(parent, true).gameObject;
                case UIHudWidgetKind.Row: return CreateRow(parent).gameObject;
                case UIHudWidgetKind.Grid: return CreateGrid(parent).gameObject;
                case UIHudWidgetKind.Scroll: return CreateScroll(parent).gameObject;
                case UIHudWidgetKind.Header:
                    var h = CreateText(parent, "Header", 40f);
                    h.gameObject.name = "Header";
                    h.fontStyle = FontStyles.Bold;
                    h.rectTransform.sizeDelta = new Vector2(400f, 56f);
                    Layout(h.gameObject, -1f, 56f);
                    return h.gameObject;
                case UIHudWidgetKind.Divider:
                    var d = CreateRect("Divider", parent);
                    d.sizeDelta = new Vector2(400f, 2f);
                    var di = d.gameObject.AddComponent<Image>();
                    di.color = new Color(1f, 1f, 1f, 0.15f);
                    di.raycastTarget = false;
                    Layout(d.gameObject, -1f, 2f);
                    return d.gameObject;
                case UIHudWidgetKind.Radial: return CreateRadial(parent).gameObject;
                case UIHudWidgetKind.Space:
                    var space = CreateRect("Space", parent);
                    Layout(space.gameObject, -1f, 32f);
                    return space.gameObject;
                default:
                    var t = CreateText(parent, "Label", 32f);
                    t.gameObject.name = "Label";
                    Layout(t.gameObject, -1f, 48f);
                    return t.gameObject;
            }
        }

        /// <summary>
        /// (Re)builds the hidden "Widget Templates" under the manager: one of every widget kind, which
        /// UIHudManager.AddWidget copies at runtime (Udon cannot add components, only copy objects).
        /// </summary>
        internal static void MakeTemplates(UIHudManager manager)
        {
            Transform old = manager.transform.Find("Widget Templates");
            if (old != null) Undo.DestroyObjectImmediate(old.gameObject);
            RectTransform root = CreateRect("Widget Templates", manager.transform);
            Undo.RegisterCreatedObjectUndo(root.gameObject, "Make Runtime Templates");
            root.sizeDelta = new Vector2(600f, 400f);
            var list = new System.Collections.Generic.List<GameObject>();
            foreach (UIHudWidgetKind k in System.Enum.GetValues(typeof(UIHudWidgetKind)))
            {
                if (k == UIHudWidgetKind.PagesTabs || k == UIHudWidgetKind.PagesSelector) continue; // need page wiring
                GameObject go = AddWidget(root, k);
                go.name = k.ToString();
                go.SetActive(false);
                list.Add(go);
            }
            if (UIHudStyle.Current != null) UIHudStyle.Current.Apply(root);
            root.gameObject.SetActive(false);
            Undo.RecordObject(manager, "Make Runtime Templates");
            manager.widgetTemplates = list.ToArray();
            Sync(manager);
        }

        /// <summary>The button on a wrist/mobile canvas that opens this menu only, or null.</summary>
        internal static UIHudButton OwnButton(Transform canvas, UIHudMenu menu)
        {
            if (canvas == null) return null;
            foreach (Transform c in canvas)
            {
                var b = c.GetComponent<UIHudButton>();
                if (b != null && b.menuToToggle == menu) return b;
            }
            return null;
        }

        /// <summary>
        /// Adds (or removes) a button for this menu next to the main button on the VR wrist and the
        /// mobile corner canvases. Each canvas grows by one square per button.
        /// </summary>
        internal static void SetOwnButtons(UIHudManager manager, UIHudMenu menu, bool on, string label)
        {
            foreach (Transform canvas in new[] { manager.wristButton, manager.mobileButton })
            {
                if (canvas == null) continue;
                UIHudButton own = OwnButton(canvas, menu);
                if (on == (own != null)) continue;
                var rt = (RectTransform)canvas;
                var hl = canvas.GetComponent<HorizontalLayoutGroup>();
                if (hl == null)
                {
                    hl = Undo.AddComponent<HorizontalLayoutGroup>(canvas.gameObject);
                    hl.spacing = 12f;
                    hl.childControlWidth = hl.childControlHeight = true;
                    hl.childForceExpandWidth = hl.childForceExpandHeight = true;
                }
                if (on)
                {
                    UIHudButton b = CreateButton(canvas, label);
                    Undo.RegisterCreatedObjectUndo(b.gameObject, "Add Menu Button");
                    b.gameObject.name = label;
                    b.menuToToggle = menu;
                    Sync(b);
                }
                else Undo.DestroyObjectImmediate(own.gameObject);
                int n = canvas.GetComponentsInChildren<UIHudButton>(true).Length;
                float square = rt.sizeDelta.y;
                Undo.RecordObject(rt, "Menu Button Size");
                rt.sizeDelta = new Vector2(square * n + hl.spacing * (n - 1), square);
            }
        }

        private static RectTransform CreateWristButton(Transform parent, UIHudManager manager)
        {
            RectTransform canvas = CreateCanvas("WristButton Canvas", parent, true, new Vector2(160f, 160f));
            var btn = CreateButton(canvas, "Menu");
            Stretch((RectTransform)btn.transform);
            btn.manager = manager;
            btn.toggleMenu = true;
            Sync(btn);
            return canvas;
        }

        /// <summary>A menu is its own canvas: Canvas + VRC Ui Shape + UIHudMenu on one object.</summary>
        internal static UIHudMenu CreateMenuPanel(Transform parent, UIHudSide side, UIHudManager manager, bool withDemo)
        {
            // Center = big main menu (fits the 1.3 aspect VR view: 1404 x 1080 canvas px).
            Vector2 size = side == UIHudSide.Center ? new Vector2(1200f, 820f)
                : side == UIHudSide.Left || side == UIHudSide.Right ? new Vector2(560f, 820f)
                : new Vector2(1100f, 380f);
            var panel = CreateCanvas(side + "Menu Canvas", parent, true, size);
            panel.GetComponent<Canvas>().sortingOrder = 10;
            panel.gameObject.AddComponent<Image>().color = Panel;
            panel.gameObject.AddComponent<CanvasGroup>().alpha = 0f; // hidden until opened
            var layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(24, 24, 24, 24);
            layout.spacing = 16f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;

            // Header: title + close
            var header = CreateRect("Header", panel);
            var hl = header.gameObject.AddComponent<HorizontalLayoutGroup>();
            hl.childControlWidth = true; hl.childControlHeight = true; hl.childForceExpandWidth = false;
            Layout(header.gameObject, -1f, 64f);
            var title = CreateText(header, "Menu", 40f);
            title.fontStyle = FontStyles.Bold;
            Layout(title.gameObject, -1f, 64f, 1f);
            var close = CreateButton(header, "X");
            Layout(close.gameObject, 64f, 64f);

            // Tabs row + pages
            var tabs = CreateRect("Tabs", panel);
            var tl = tabs.gameObject.AddComponent<HorizontalLayoutGroup>();
            tl.spacing = 8f; tl.childControlWidth = true; tl.childControlHeight = true;
            Layout(tabs.gameObject, -1f, 56f);

            var pages = CreateRect("Pages", panel);
            Layout(pages.gameObject, -1f, -1f, 0f, 1f);

            var menu = UdonSharpUndo.AddComponent<UIHudMenu>(panel.gameObject);
            menu.manager = manager;
            menu.side = side;
            menu.pages = new GameObject[0];
            menu.pageHighlights = new GameObject[0];
            close.menuToToggle = menu; // closes this menu only
            Sync(close);

            if (withDemo)
            {
                RectTransform p0 = AddPage(menu, "Main");
                var showHud = CreateToggle(p0, "Show HUD");
                showHud.toggle.isOn = true;
                showHud.target = manager;
                showHud.eventName = nameof(UIHudManager.ToggleHud);
                Sync(showHud);
                var vol = CreateSlider(p0, "Volume");
                vol.format = "P0";
                Sync(vol);
                CreateButton(p0, "Button");

                RectTransform p1 = AddPage(menu, "Settings");
                CreateText(p1, "Settings page.\nAdd widgets here with GameObject > UIHUD > Widgets.", 28f);
            }
            else
            {
                AddPage(menu, "Page 1");
            }
            Sync(menu);
            return menu;
        }

        // =====================================================================
        // Hotbar
        // =====================================================================

        [MenuItem(Root + "Hotbar (45° below, locked to your body)", false, Prio + 2)]
        private static void MenuHotbar()
        {
            var manager = Object.FindObjectOfType<UIHudManager>();
            if (manager == null)
            {
                EditorUtility.DisplayDialog("UIHUD", "Create a UIHUD System first (GameObject > UIHUD > UIHUD System).", "OK");
                return;
            }
            Selection.activeGameObject = CreateHotbar(manager, 6).gameObject;
        }

        /// <summary>Widgets that fit in a hotbar slot.</summary>
        internal static readonly UIHudWidgetKind[] HotbarKinds =
        {
            UIHudWidgetKind.Button, UIHudWidgetKind.Toggle, UIHudWidgetKind.Slider, UIHudWidgetKind.Selector, UIHudWidgetKind.Input,
            UIHudWidgetKind.Label, UIHudWidgetKind.Info, UIHudWidgetKind.Value, UIHudWidgetKind.Stat, UIHudWidgetKind.Graph, UIHudWidgetKind.Bar, UIHudWidgetKind.Radial,
            UIHudWidgetKind.Image, UIHudWidgetKind.Space,
        };

        /// <summary>Hotbar canvas under the manager: a row of slots (any widget), as wide as its slots.</summary>
        internal static UIHudHotbar CreateHotbar(UIHudManager manager, int count)
        {
            RectTransform canvas = CreateCanvas("Hotbar Canvas", manager.transform, true, new Vector2(140f, 140f));
            canvas.GetComponent<Canvas>().sortingOrder = 15;
            var bar = UdonSharpUndo.AddComponent<UIHudHotbar>(canvas.gameObject);
            bar.manager = manager;
            RectTransform content = CreateHotbarContent(canvas);
            var bg = content.gameObject.AddComponent<Image>();
            bg.sprite = Res.background;
            bg.type = Image.Type.Sliced;
            bg.color = Panel;
            var hl = content.gameObject.AddComponent<HorizontalLayoutGroup>();
            hl.childControlWidth = hl.childControlHeight = true;
            hl.childForceExpandWidth = false;
            hl.childForceExpandHeight = true;
            content.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            bar.content = content;
            Sync(bar);
            for (int i = 0; i < count; i++) AddHotbarSlot(bar, UIHudWidgetKind.Button);
            canvas.position = manager.transform.position + new Vector3(0f, 1f, 0.5f);
            canvas.rotation = Quaternion.Euler(45f, 0f, 0f);
            return bar;
        }

        /// <summary>Adds a widget as the last slot. Buttons are square and named by their number.</summary>
        internal static GameObject AddHotbarSlot(UIHudHotbar bar, UIHudWidgetKind kind)
        {
            RectTransform slots = HotbarSlots(bar);
            int n = slots.childCount + 1;
            GameObject go = AddWidget(slots, kind);
            var le = go.GetComponent<LayoutElement>();
            if (le == null) le = Undo.AddComponent<LayoutElement>(go);
            float square = slots.sizeDelta.y - 2f * bar.gap;
            if (kind == UIHudWidgetKind.Button)
            {
                go.name = "Slot " + n;
                go.GetComponentInChildren<TextMeshProUGUI>().text = n.ToString();
                le.preferredWidth = square;
            }
            else if (le.preferredWidth < square) le.preferredWidth = kind == UIHudWidgetKind.Space ? square * 0.5f : 300f;
            if (kind == UIHudWidgetKind.Toggle && go.transform.Find("Background") is RectTransform box)
            {
                // Slots are as tall as the bar: center the check box instead of pinning it to the top.
                box.anchorMin = new Vector2(box.anchorMin.x, 0.5f);
                box.anchorMax = new Vector2(box.anchorMax.x, 0.5f);
                box.pivot = new Vector2(box.pivot.x, 0.5f);
                box.anchoredPosition = new Vector2(box.anchoredPosition.x, 0f);
            }
            le.flexibleWidth = 0f;
            FitHotbar(bar);
            return go;
        }

        /// <summary>Applies the gap to the layout and sizes the canvas + pointer collider to the slots (runtime keeps the collider in sync).</summary>
        internal static void FitHotbar(UIHudHotbar bar)
        {
            var rt = HotbarSlots(bar);
            var hl = rt.GetComponent<HorizontalLayoutGroup>();
            int g = Mathf.RoundToInt(bar.gap);
            if (hl != null) { Undo.RecordObject(hl, "Hotbar Size"); hl.padding = new RectOffset(g, g, g, g); hl.spacing = g; }
            Undo.RecordObject(rt, "Hotbar Size");
            LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
            var col = bar.GetComponent<BoxCollider>();
            if (col != null) { Undo.RecordObject(col, "Hotbar Size"); col.size = new Vector3(rt.sizeDelta.x, rt.sizeDelta.y, 0f); }
        }

        /// <summary>The slots' parent: the nested slots canvas, or the hotbar canvas itself (old hotbars).</summary>
        internal static RectTransform HotbarSlots(UIHudHotbar bar)
        {
            return bar.content != null ? bar.content : (RectTransform)bar.transform;
        }

        /// <summary>
        /// Nested canvas for the slots. The hotbar canvas keeps the collider + Ui Shape (pointer layer);
        /// the hotbar moves this one to the UI layer at runtime so the VRChat camera skips it.
        /// It needs its own Graphic Raycaster: graphics register with their nearest canvas.
        /// </summary>
        private static RectTransform CreateHotbarContent(RectTransform canvas)
        {
            var rt = CreateRect("Slots", canvas);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = canvas.sizeDelta;
            rt.gameObject.AddComponent<Canvas>();
            rt.gameObject.AddComponent<GraphicRaycaster>();
            return rt;
        }

        /// <summary>Moves an old hotbar's slots, background and layout into a nested slots canvas.</summary>
        internal static void MoveHotbarIntoContent(UIHudHotbar bar)
        {
            if (bar.content != null) return;
            var canvas = (RectTransform)bar.transform;
            var content = CreateHotbarContent(canvas);
            var old = new System.Collections.Generic.List<Transform>();
            foreach (Transform c in canvas) if (c != content) old.Add(c);
            foreach (Transform c in old) Undo.SetTransformParent(c, content, "Hotbar Slots"); // keeps their order
            var bg = canvas.GetComponent<Image>();
            if (bg != null)
            {
                var nb = content.gameObject.AddComponent<Image>();
                nb.sprite = bg.sprite; nb.type = bg.type; nb.color = bg.color; nb.raycastTarget = bg.raycastTarget;
                Undo.DestroyObjectImmediate(bg);
            }
            var oldFit = canvas.GetComponent<ContentSizeFitter>();
            if (oldFit != null) Undo.DestroyObjectImmediate(oldFit);
            content.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            var oldHl = canvas.GetComponent<HorizontalLayoutGroup>();
            var hl = content.gameObject.AddComponent<HorizontalLayoutGroup>();
            hl.childControlWidth = hl.childControlHeight = true;
            hl.childForceExpandWidth = false;
            hl.childForceExpandHeight = true;
            if (oldHl != null) { hl.childAlignment = oldHl.childAlignment; Undo.DestroyObjectImmediate(oldHl); }
            // Very old hotbars stretched their slots; without a preferred width they would shrink to nothing.
            float square = content.sizeDelta.y - 2f * bar.gap;
            foreach (Transform c in content)
            {
                var le = c.GetComponent<LayoutElement>();
                if (le == null) le = Undo.AddComponent<LayoutElement>(c.gameObject);
                if (le.preferredWidth <= 0f) { Undo.RecordObject(le, "Hotbar Slots"); le.preferredWidth = square; }
            }
            Undo.RecordObject(bar, "Hotbar Slots");
            bar.content = content;
            Sync(bar);
            FitHotbar(bar);
        }

        [MenuItem(Root + "World Panel (standalone)", false, Prio + 1)]
        private static void MenuWorldPanel(MenuCommand cmd)
        {
            UIHudMenu panel = CreateWorldPanel(cmd.context as GameObject);
            EnsureEventSystem();
            Selection.activeGameObject = panel.gameObject;
        }

        /// <summary>A menu that lives in the world (kiosk, board, sign). No manager needed.</summary>
        internal static UIHudMenu CreateWorldPanel(GameObject parent)
        {
            UIHudMenu menu = CreateMenuPanel(parent != null ? parent.transform : null, UIHudSide.Center, null, false);
            menu.gameObject.name = "World Panel";
            menu.worldSpace = true;
            menu.startOpen = true;
            menu.openWithMainButton = false;
            menu.animSlide = false;
            Sync(menu);
            menu.GetComponent<CanvasGroup>().alpha = 1f;
            var shape = menu.GetComponent<VRCUiShape>();
            if (shape != null) shape.AllowFocusView = true; // fine at world distance
            menu.transform.localScale = Vector3.one * 0.001f; // 1 canvas px = 1 mm
            PlaceInFrontOfSceneCamera(menu.transform);
            return menu;
        }

        /// <summary>Puts a world panel 2 m in front of the Scene view camera, upright, facing it.</summary>
        internal static void PlaceInFrontOfSceneCamera(Transform t)
        {
            var sv = SceneView.lastActiveSceneView;
            if (sv == null || sv.camera == null) return;
            Transform cam = sv.camera.transform;
            Vector3 fwd = Vector3.ProjectOnPlane(cam.forward, Vector3.up);
            if (fwd.sqrMagnitude < 0.001f) fwd = Vector3.forward;
            fwd.Normalize();
            Undo.RecordObject(t, "Place World Panel");
            t.SetPositionAndRotation(cam.position + fwd * 2f, Quaternion.LookRotation(fwd, Vector3.up));
        }

        [MenuItem(Root + "Menu Page (add to selected menu)", false, Prio + 30)]
        private static void AddPageToSelected()
        {
            var menu = Selection.activeGameObject ? Selection.activeGameObject.GetComponentInParent<UIHudMenu>() : null;
            if (menu == null) { EditorUtility.DisplayDialog("UIHUD", "Select a UIHudMenu (or something inside it).", "OK"); return; }
            Selection.activeGameObject = AddPage(menu, "Page " + (menu.pages.Length + 1)).gameObject;
        }

        /// <summary>Adds a page under menu/Pages and a tab under menu/Tabs, wired together.</summary>
        internal static RectTransform AddPage(UIHudMenu menu, string name)
        {
            Transform pagesRoot = menu.transform.Find("Pages");
            Transform tabsRoot = menu.transform.Find("Tabs");
            if (pagesRoot == null || tabsRoot == null) { Debug.LogError("UIHUD: menu is missing Pages/Tabs children."); return null; }

            var page = CreateRect(name, pagesRoot);
            Stretch(page);
            page.gameObject.AddComponent<CanvasGroup>(); // page switch fade
            var vl = page.gameObject.AddComponent<VerticalLayoutGroup>();
            vl.spacing = 12f; vl.childControlWidth = true; vl.childControlHeight = true; vl.childForceExpandHeight = false;

            int index = menu.pages.Length;
            var tabBtn = CreateRawButton(tabsRoot, name);
            var tab = UdonSharpUndo.AddComponent<UIHudTab>(tabBtn.gameObject);
            tab.menu = menu;
            tab.pageIndex = index;
            Sync(tab);
            Wire(tabBtn.onClick, tab, nameof(UIHudTab._OnClick));

            // Highlight bar under the tab, active only on the selected page.
            var hl = CreateRect("Selected", tabBtn.transform);
            hl.anchorMin = new Vector2(0f, 0f); hl.anchorMax = new Vector2(1f, 0f);
            hl.pivot = new Vector2(0.5f, 0f); hl.sizeDelta = new Vector2(0f, 6f); hl.anchoredPosition = Vector2.zero;
            hl.gameObject.AddComponent<Image>().color = Accent;

            Undo.RecordObject(menu, "Add UIHUD Page");
            menu.pages = Append(menu.pages, page.gameObject);
            menu.pageHighlights = Append(menu.pageHighlights, hl.gameObject);
            page.gameObject.SetActive(index == menu.startPage);
            hl.gameObject.SetActive(index == menu.startPage);
            Sync(menu);
            SetupPageScroll(menu);
            return page;
        }

        // =====================================================================
        // Widgets (added under the selected RectTransform)
        // =====================================================================

        [MenuItem(Root + "Widgets/Button", false, Prio + 40)]
        private static void MenuButton() { Select(CreateButton(Parent(), "Button")); }

        [MenuItem(Root + "Widgets/Toggle", false, Prio + 41)]
        private static void MenuToggle() { Select(CreateToggle(Parent(), "Toggle")); }

        [MenuItem(Root + "Widgets/Slider", false, Prio + 42)]
        private static void MenuSlider() { Select(CreateSlider(Parent(), "Slider")); }

        [MenuItem(Root + "Widgets/Bar (Health, Progress)", false, Prio + 43)]
        private static void MenuBar() { Select(CreateBar(Parent(), "Bar")); }

        [MenuItem(Root + "Widgets/Notification (Toast)", false, Prio + 44)]
        private static void MenuToast() { Select(CreateNotification(Parent())); }

        [MenuItem(Root + "Widgets/Selector (< option >)", false, Prio + 46)]
        private static void MenuSelector() { Select(CreateSelector(Parent(), "Option")); }

        [MenuItem(Root + "Widgets/Text Input", false, Prio + 47)]
        private static void MenuInput() { Select(CreateInput(Parent())); }

        [MenuItem(Root + "Widgets/Player List", false, Prio + 48)]
        private static void MenuPlayers() { Select(CreatePlayerList(Parent())); }

        [MenuItem(Root + "Widgets/Info (time, players, fps)", false, Prio + 49)]
        private static void MenuInfo() { Select(CreateInfo(Parent())); }

        [MenuItem(Root + "Widgets/Data/Value (shows a variable)", false, Prio + 50)]
        private static void MenuValue() { Select(CreateValue(Parent())); }

        [MenuItem(Root + "Widgets/Data/Table (DataList, JSON)", false, Prio + 51)]
        private static void MenuTable() { Select(CreateTable(Parent())); }

        [MenuItem(Root + "Widgets/Data/Graph (value over time)", false, Prio + 52)]
        private static void MenuGraph() { Select(CreateGraph(Parent())); }

        [MenuItem(Root + "Widgets/Data/Stat Tile (number + change)", false, Prio + 53)]
        private static void MenuStat() { Select(CreateStat(Parent())); }

        [MenuItem(Root + "Widgets/Data/Pie Chart (donut)", false, Prio + 54)]
        private static void MenuPie() { Select(CreatePie(Parent())); }

        [MenuItem(Root + "Widgets/Data/Bar List (bar per entry)", false, Prio + 55)]
        private static void MenuBarList() { Select(CreateBarList(Parent())); }

        [MenuItem(Root + "Widgets/Data/Gauge (dial)", false, Prio + 56)]
        private static void MenuGauge() { Select(CreateGauge(Parent())); }

        [MenuItem(Root + "Widgets/Label", false, Prio + 45)]
        private static void MenuLabel() { Selection.activeGameObject = CreateText(Parent(), "Label", 32f).gameObject; }

        private static RectTransform Parent()
        {
            var sel = Selection.activeGameObject;
            var rt = sel ? sel.GetComponent<RectTransform>() : null;
            if (rt == null || sel.GetComponentInParent<Canvas>() == null)
            {
                EditorUtility.DisplayDialog("UIHUD", "Select a UI object inside a UIHUD canvas or menu page first.", "OK");
                return null;
            }
            return rt;
        }

        private static void Select(Component c) { if (c != null) Selection.activeGameObject = c.gameObject; }

        internal static UIHudButton CreateButton(Transform parent, string label)
        {
            if (parent == null) return null;
            Button btn = CreateRawButton(parent, label);
            Layout(btn.gameObject, -1f, 64f);
            var w = UdonSharpUndo.AddComponent<UIHudButton>(btn.gameObject);
            Wire(btn.onClick, w, nameof(UIHudButton._OnClick));
            return w;
        }

        internal static UIHudToggle CreateToggle(Transform parent, string label)
        {
            if (parent == null) return null;
            GameObject go = DefaultControls.CreateToggle(ToUgui(Res));
            Place(go, parent, label);
            // TMP has no toggle preset: swap the legacy label for a TMP one.
            var legacy = go.GetComponentInChildren<Text>();
            var labelGo = legacy.gameObject;
            Object.DestroyImmediate(legacy);
            labelGo.AddComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.MidlineLeft;
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(400f, 56f);
            Layout(go, -1f, 56f);
            // Default toggle is 20px; scale the box and label up for world-space readability.
            var bg = (RectTransform)go.transform.Find("Background");
            bg.sizeDelta = new Vector2(48f, 48f);
            bg.anchoredPosition = new Vector2(24f, -28f);
            var labelText = go.GetComponentInChildren<TextMeshProUGUI>();
            labelText.text = label;
            labelText.fontSize = 32f;
            var lrt = labelText.rectTransform;
            lrt.offsetMin = new Vector2(64f, 0f);

            labelText.raycastTarget = false;
            var toggle = go.GetComponent<Toggle>();
            NoNavigation(toggle);
            var w = UdonSharpUndo.AddComponent<UIHudToggle>(go);
            w.toggle = toggle;
            Sync(w);
            Wire(toggle.onValueChanged, w, nameof(UIHudToggle._OnValueChanged));
            return w;
        }

        internal static UIHudSlider CreateSlider(Transform parent, string label)
        {
            if (parent == null) return null;
            var row = CreateRect(label, parent);
            Layout(row.gameObject, -1f, 96f);
            row.sizeDelta = new Vector2(400f, 96f);
            var vl = row.gameObject.AddComponent<VerticalLayoutGroup>();
            vl.childControlWidth = true; vl.childControlHeight = true; vl.childForceExpandHeight = false;

            var head = CreateRect("Header", row);
            var hl = head.gameObject.AddComponent<HorizontalLayoutGroup>();
            hl.childControlWidth = true; hl.childControlHeight = true;
            Layout(head.gameObject, -1f, 44f);
            var name = CreateText(head, label, 30f);
            var val = CreateText(head, "0", 30f);
            val.alignment = TextAlignmentOptions.Right;

            GameObject go = DefaultControls.CreateSlider(ToUgui(Res));
            Place(go, row, "Slider");
            Layout(go, -1f, 40f);

            var slider = go.GetComponent<Slider>();
            NoNavigation(slider);
            var w = UdonSharpUndo.AddComponent<UIHudSlider>(go);
            w.slider = slider;
            w.valueLabel = val;
            Sync(w);
            Wire(slider.onValueChanged, w, nameof(UIHudSlider._OnValueChanged));
            return w;
        }

        internal static UIHudBar CreateBar(Transform parent, string label)
        {
            if (parent == null) return null;
            var root = CreateRect(label + "Bar", parent);
            root.sizeDelta = new Vector2(420f, 44f);
            Layout(root.gameObject, -1f, 44f);
            root.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);

            var fillRt = CreateRect("Fill", root);
            Stretch(fillRt, 4f);
            var fill = fillRt.gameObject.AddComponent<Image>();
            fill.sprite = Res.standard;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.color = new Color(0.3f, 0.85f, 0.4f, 1f);

            var text = CreateText(root, "100/100", 28f);
            text.alignment = TextAlignmentOptions.Center;
            Stretch(text.rectTransform);

            var w = UdonSharpUndo.AddComponent<UIHudBar>(root.gameObject);
            w.fill = fill;
            w.label = text;
            w.colorByValue = new Gradient();
            w.colorByValue.SetKeys(
                new[] { new GradientColorKey(new Color(0.9f, 0.25f, 0.2f), 0f), new GradientColorKey(new Color(1f, 0.8f, 0.2f), 0.5f), new GradientColorKey(new Color(0.3f, 0.85f, 0.4f), 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            w.useGradient = true;
            Sync(w);
            return w;
        }

        internal static UIHudNotification CreateNotification(Transform parent)
        {
            if (parent == null) return null;
            var root = CreateRect("Notification", parent);
            root.sizeDelta = new Vector2(700f, 90f);
            root.gameObject.AddComponent<Image>().color = Panel;
            root.gameObject.AddComponent<CanvasGroup>().alpha = 0f;
            var text = CreateText(root, "Notification", 34f);
            text.alignment = TextAlignmentOptions.Center;
            Stretch(text.rectTransform, 12f);
            var w = UdonSharpUndo.AddComponent<UIHudNotification>(root.gameObject);
            w.label = text;
            Sync(w);
            return w;
        }

        /// <summary>Row: name, &lt; value &gt;. The arrows call the selector's _OnPrev/_OnNext.</summary>
        internal static UIHudSelector CreateSelector(Transform parent, string label)
        {
            if (parent == null) return null;
            var row = CreateRect(label, parent);
            row.sizeDelta = new Vector2(560f, 64f);
            Layout(row.gameObject, -1f, 64f);
            var hl = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            hl.spacing = 8f; hl.childControlWidth = true; hl.childControlHeight = true; hl.childForceExpandWidth = false;
            var name = CreateText(row, label, 30f);
            Layout(name.gameObject, -1f, 64f, 1f);
            var prev = CreateRawButton(row, "<");
            Layout(prev.gameObject, 64f, 64f);
            var val = CreateText(row, "Medium", 30f);
            val.alignment = TextAlignmentOptions.Center;
            Layout(val.gameObject, 200f, 64f);
            var next = CreateRawButton(row, ">");
            Layout(next.gameObject, 64f, 64f);

            var w = UdonSharpUndo.AddComponent<UIHudSelector>(row.gameObject);
            w.valueLabel = val;
            w.options = new[] { "Low", "Medium", "High" };
            w.startIndex = 1;
            Sync(w);
            Wire(prev.onClick, w, nameof(UIHudSelector._OnPrev));
            Wire(next.onClick, w, nameof(UIHudSelector._OnNext));
            return w;
        }

        /// <summary>TMP input field; VRChat opens its keyboard on click. Submit = onEndEdit.</summary>
        internal static UIHudInput CreateInput(Transform parent)
        {
            if (parent == null) return null;
            GameObject go = TMP_DefaultControls.CreateInputField(Res);
            Place(go, parent, "Input");
            ((RectTransform)go.transform).sizeDelta = new Vector2(400f, 64f);
            Layout(go, -1f, 64f);
            var input = go.GetComponent<TMP_InputField>();
            NoNavigation(input);
            input.pointSize = 30f;
            ((TextMeshProUGUI)input.placeholder).text = "Type here...";
            foreach (var t in go.GetComponentsInChildren<TextMeshProUGUI>(true)) { t.fontSize = 30f; t.color = Color.white; t.alignment = TextAlignmentOptions.MidlineLeft; }
            input.placeholder.color = new Color(1f, 1f, 1f, 0.5f);
            go.GetComponent<Image>().color = Color.white;
            var colors = input.colors;
            colors.normalColor = Soft; colors.highlightedColor = new Color(1f, 1f, 1f, 0.2f); colors.selectedColor = new Color(1f, 1f, 1f, 0.2f); colors.pressedColor = Accent;
            input.colors = colors;

            var w = UdonSharpUndo.AddComponent<UIHudInput>(go);
            w.input = input;
            Sync(w);
            Wire(input.onEndEdit, w, nameof(UIHudInput._OnSubmit));
            return w;
        }

        internal static UIHudPlayerList CreatePlayerList(Transform parent)
        {
            if (parent == null) return null;
            var root = CreateRect("Player List", parent);
            root.sizeDelta = new Vector2(500f, 320f);
            Layout(root.gameObject, -1f, 320f);
            var vl = root.gameObject.AddComponent<VerticalLayoutGroup>();
            vl.spacing = 6f; vl.childControlWidth = true; vl.childControlHeight = true; vl.childForceExpandHeight = false;
            var count = CreateText(root, "Players: 1", 30f);
            count.fontStyle = FontStyles.Bold;
            Layout(count.gameObject, -1f, 44f);
            var list = CreateText(root, "Player", 28f);
            list.alignment = TextAlignmentOptions.TopLeft;
            Layout(list.gameObject, -1f, -1f, 0f, 1f);
            var w = UdonSharpUndo.AddComponent<UIHudPlayerList>(root.gameObject);
            w.list = list;
            w.countLabel = count;
            Sync(w);
            return w;
        }

        internal static UIHudInfo CreateInfo(Transform parent)
        {
            if (parent == null) return null;
            var t = CreateText(parent, "12:00  ·  1 players  ·  60 fps", 30f);
            t.gameObject.name = "Info";
            t.rectTransform.sizeDelta = new Vector2(600f, 50f);
            Layout(t.gameObject, -1f, 48f);
            var w = UdonSharpUndo.AddComponent<UIHudInfo>(t.gameObject);
            w.label = t;
            Sync(w);
            return w;
        }

        internal static UIHudValue CreateValue(Transform parent)
        {
            if (parent == null) return null;
            var t = CreateText(parent, "Coins: 0", 32f);
            t.gameObject.name = "Value";
            t.rectTransform.sizeDelta = new Vector2(400f, 50f);
            Layout(t.gameObject, -1f, 48f);
            var w = UdonSharpUndo.AddComponent<UIHudValue>(t.gameObject);
            w.label = t;
            Sync(w);
            return w;
        }

        /// <summary>Header row + body text; columns are placed with TMP pos tags (no per-cell objects).</summary>
        internal static UIHudTable CreateTable(Transform parent)
        {
            if (parent == null) return null;
            var root = CreateRect("Table", parent);
            root.sizeDelta = new Vector2(600f, 360f);
            Layout(root.gameObject, -1f, 360f);
            var vl = root.gameObject.AddComponent<VerticalLayoutGroup>();
            vl.spacing = 6f; vl.childControlWidth = true; vl.childControlHeight = true; vl.childForceExpandHeight = false;
            var head = CreateText(root, "<pos=0%>#<pos=8%>Player<pos=54%>Score", 28f);
            head.gameObject.name = "Header";
            head.fontStyle = FontStyles.Bold;
            Layout(head.gameObject, -1f, 42f);
            var line = CreateRect("Divider", root);
            var li = line.gameObject.AddComponent<Image>();
            li.color = new Color(1f, 1f, 1f, 0.15f);
            li.raycastTarget = false;
            Layout(line.gameObject, -1f, 2f);
            var body = CreateText(root, "<pos=0%>1<pos=8%>Bo<pos=54%>30\n<pos=0%>2<pos=8%>Cy<pos=54%>21\n<pos=0%>3<pos=8%>Ann<pos=54%>12", 28f);
            body.gameObject.name = "Rows";
            body.alignment = TextAlignmentOptions.TopLeft;
            Layout(body.gameObject, -1f, -1f, 0f, 1f);
            var w = UdonSharpUndo.AddComponent<UIHudTable>(root.gameObject);
            w.header = head;
            w.body = body;
            Sync(w);
            return w;
        }

        /// <summary>Chart with one bar and one line segment per point (Udon cannot create objects).</summary>
        internal static UIHudGraph CreateGraph(Transform parent, int points = 30)
        {
            if (parent == null) return null;
            var root = CreateRect("Graph", parent);
            root.sizeDelta = new Vector2(600f, 260f);
            Layout(root.gameObject, -1f, 260f);
            var bg = root.gameObject.AddComponent<Image>();
            bg.sprite = Res.background;
            bg.type = Image.Type.Sliced;
            bg.color = Soft;
            bg.raycastTarget = false;
            var plot = CreateRect("Plot", root);
            plot.anchorMin = Vector2.zero; plot.anchorMax = Vector2.one;
            plot.offsetMin = new Vector2(80f, 16f); plot.offsetMax = new Vector2(-16f, -44f);
            var maxL = CreateText(root, "100", 24f); maxL.gameObject.name = "Max";
            Anchor(maxL.rectTransform, new Vector2(0f, 1f), new Vector2(10f, -44f), new Vector2(66f, 30f));
            maxL.alignment = TextAlignmentOptions.TopRight;
            var minL = CreateText(root, "0", 24f); minL.gameObject.name = "Min";
            Anchor(minL.rectTransform, new Vector2(0f, 0f), new Vector2(10f, 16f), new Vector2(66f, 30f));
            minL.alignment = TextAlignmentOptions.BottomRight;
            var val = CreateText(root, "", 28f); val.gameObject.name = "Value";
            Anchor(val.rectTransform, new Vector2(1f, 1f), new Vector2(-16f, -6f), new Vector2(200f, 36f));
            val.alignment = TextAlignmentOptions.TopRight;
            val.fontStyle = FontStyles.Bold;
            var bars = new RectTransform[points];
            var lines = new RectTransform[points];
            for (int i = 0; i < points; i++)
            {
                bars[i] = GraphPart(plot, "Bar " + i, new Vector2(0.5f, 0f));
                lines[i] = GraphPart(plot, "Line " + i, new Vector2(0.5f, 0.5f));
            }
            var w = UdonSharpUndo.AddComponent<UIHudGraph>(root.gameObject);
            w.plot = plot;
            w.bars = bars;
            w.lines = lines;
            w.maxLabel = maxL;
            w.minLabel = minL;
            w.valueLabel = val;
            w.sampleFps = true; // shows something right away; point it at your own variable instead
            Sync(w);
            return w;
        }

        internal static UIHudStat CreateStat(Transform parent)
        {
            if (parent == null) return null;
            var root = CreateRect("Stat", parent);
            root.sizeDelta = new Vector2(280f, 150f);
            Layout(root.gameObject, -1f, 150f);
            var bg = root.gameObject.AddComponent<Image>();
            bg.sprite = Res.background; bg.type = Image.Type.Sliced; bg.color = Soft; bg.raycastTarget = false;
            var title = CreateText(root, "Score", 24f); title.gameObject.name = "Title";
            title.color = new Color(1f, 1f, 1f, 0.7f);
            Anchor(title.rectTransform, new Vector2(0f, 1f), new Vector2(18f, -12f), new Vector2(240f, 32f));
            var val = CreateText(root, "0", 56f); val.gameObject.name = "Value";
            val.fontStyle = FontStyles.Bold;
            Anchor(val.rectTransform, new Vector2(0f, 0.5f), new Vector2(18f, -4f), new Vector2(250f, 64f));
            var delta = CreateText(root, "", 24f); delta.gameObject.name = "Change";
            Anchor(delta.rectTransform, new Vector2(0f, 0f), new Vector2(18f, 10f), new Vector2(240f, 30f));
            var w = UdonSharpUndo.AddComponent<UIHudStat>(root.gameObject);
            w.title = title; w.valueLabel = val; w.deltaLabel = delta;
            Sync(w);
            return w;
        }

        /// <summary>Donut: one Radial 360 Image per slice, a hole and a legend.</summary>
        internal static UIHudPie CreatePie(Transform parent, int sliceCount = 8)
        {
            if (parent == null) return null;
            var root = CreateRect("Pie Chart", parent);
            root.sizeDelta = new Vector2(600f, 240f);
            Layout(root.gameObject, -1f, 240f);
            var chart = CreateRect("Chart", root);
            Anchor(chart, new Vector2(0f, 0.5f), new Vector2(10f, 0f), new Vector2(220f, 220f));
            var slices = new Image[sliceCount];
            for (int i = 0; i < sliceCount; i++)
            {
                var s = CreateRect("Slice " + i, chart);
                Stretch(s);
                var img = s.gameObject.AddComponent<Image>();
                img.sprite = Res.knob; img.type = Image.Type.Filled;
                img.fillMethod = Image.FillMethod.Radial360; img.fillOrigin = (int)Image.Origin360.Top; img.fillClockwise = true;
                img.raycastTarget = false;
                slices[i] = img;
            }
            var hole = CreateRect("Hole", chart);
            hole.anchorMin = hole.anchorMax = new Vector2(0.5f, 0.5f);
            hole.sizeDelta = new Vector2(120f, 120f);
            var holeImg = hole.gameObject.AddComponent<Image>();
            holeImg.sprite = Res.knob; holeImg.color = new Color(Panel.r, Panel.g, Panel.b, 1f); holeImg.raycastTarget = false; // opaque, or the slices show through
            var center = CreateText(chart, "", 34f); center.gameObject.name = "Total";
            center.alignment = TextAlignmentOptions.Center; center.fontStyle = FontStyles.Bold;
            Stretch(center.rectTransform);
            var legend = CreateText(root, "", 26f); legend.gameObject.name = "Legend";
            legend.alignment = TextAlignmentOptions.MidlineLeft;
            legend.rectTransform.anchorMin = new Vector2(0f, 0f); legend.rectTransform.anchorMax = new Vector2(1f, 1f);
            legend.rectTransform.offsetMin = new Vector2(260f, 10f); legend.rectTransform.offsetMax = new Vector2(-10f, -10f);
            var w = UdonSharpUndo.AddComponent<UIHudPie>(root.gameObject);
            w.slices = slices; w.legend = legend; w.centerLabel = center;
            w.json = "{\"Red\":12,\"Blue\":8,\"Green\":5}";
            Sync(w);
            return w;
        }

        /// <summary>Rows of name | bar | value, made ahead of time (Udon cannot add objects).</summary>
        internal static UIHudBarList CreateBarList(Transform parent, int rowCount = 6)
        {
            if (parent == null) return null;
            var root = CreateRect("Bar List", parent);
            root.sizeDelta = new Vector2(600f, rowCount * 46f);
            Layout(root.gameObject, -1f, -1f); // height comes from the visible rows (unused rows hide)
            var vl = root.gameObject.AddComponent<VerticalLayoutGroup>();
            vl.spacing = 6f; vl.childControlWidth = true; vl.childControlHeight = true; vl.childForceExpandHeight = false;
            var rows = new GameObject[rowCount];
            var names = new TextMeshProUGUI[rowCount];
            var fills = new Image[rowCount];
            var vals = new TextMeshProUGUI[rowCount];
            for (int i = 0; i < rowCount; i++)
            {
                var row = CreateRect("Row " + i, root);
                Layout(row.gameObject, -1f, 40f);
                var hl = row.gameObject.AddComponent<HorizontalLayoutGroup>();
                hl.spacing = 12f; hl.childControlWidth = true; hl.childControlHeight = true; hl.childForceExpandWidth = false;
                var name = CreateText(row, "Name", 26f); name.gameObject.name = "Name";
                Layout(name.gameObject, 150f, 40f);
                var track = CreateRect("Track", row);
                Layout(track.gameObject, -1f, 40f, 1f);
                var back = track.gameObject.AddComponent<Image>();
                back.sprite = Res.background; back.type = Image.Type.Sliced; back.color = new Color(0f, 0f, 0f, 0.35f); back.raycastTarget = false;
                var fillRt = CreateRect("Fill", track);
                Stretch(fillRt);
                var fill = fillRt.gameObject.AddComponent<Image>();
                fillRt.anchorMax = new Vector2(0f, 1f); // the bar list sets the width by this anchor
                fill.raycastTarget = false;
                var val = CreateText(row, "0", 26f); val.gameObject.name = "Value";
                val.alignment = TextAlignmentOptions.MidlineRight;
                Layout(val.gameObject, 90f, 40f);
                rows[i] = row.gameObject; names[i] = name; fills[i] = fill; vals[i] = val;
            }
            var w = UdonSharpUndo.AddComponent<UIHudBarList>(root.gameObject);
            w.rows = rows; w.names = names; w.fills = fills; w.valueLabels = vals;
            w.json = "{\"Red team\":42,\"Blue team\":35,\"Green team\":18}";
            Sync(w);
            return w;
        }

        /// <summary>Half dial: back, fill and hole are Radial 360 Images filled half way from the left; needle pivots at the center.</summary>
        internal static UIHudGauge CreateGauge(Transform parent)
        {
            if (parent == null) return null;
            var root = CreateRect("Gauge", parent);
            root.sizeDelta = new Vector2(300f, 190f);
            Layout(root.gameObject, -1f, 190f);
            var dial = CreateRect("Dial", root);
            dial.anchorMin = dial.anchorMax = new Vector2(0.5f, 0f);
            dial.pivot = new Vector2(0.5f, 0.5f);
            dial.anchoredPosition = new Vector2(0f, 40f);
            dial.sizeDelta = new Vector2(260f, 260f);
            Image Half(string n, Transform p, Color c, float size)
            {
                var rt = CreateRect(n, p);
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(size, size);
                var img = rt.gameObject.AddComponent<Image>();
                img.sprite = Res.knob; img.type = Image.Type.Filled;
                img.fillMethod = Image.FillMethod.Radial360; img.fillOrigin = (int)Image.Origin360.Left; img.fillClockwise = true;
                img.fillAmount = 0.5f; img.color = c; img.raycastTarget = false;
                return img;
            }
            Half("Back", dial, new Color(0f, 0f, 0f, 0.45f), 260f);
            var fill = Half("Fill", dial, Accent, 260f);
            fill.fillAmount = 0f;
            Half("Hole", dial, new Color(Panel.r, Panel.g, Panel.b, 1f), 190f);
            var needle = CreateRect("Needle", dial);
            needle.anchorMin = needle.anchorMax = new Vector2(0.5f, 0.5f);
            needle.pivot = new Vector2(0.5f, 0f);
            needle.sizeDelta = new Vector2(6f, 120f);
            var ni = needle.gameObject.AddComponent<Image>();
            ni.color = Color.white; ni.raycastTarget = false;
            needle.localEulerAngles = new Vector3(0f, 0f, 90f);
            var val = CreateText(root, "0", 40f); val.gameObject.name = "Value";
            val.alignment = TextAlignmentOptions.Center; val.fontStyle = FontStyles.Bold;
            Anchor(val.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(200f, 44f));
            var minL = CreateText(root, "0", 22f); minL.gameObject.name = "Min";
            Anchor(minL.rectTransform, new Vector2(0f, 0f), new Vector2(10f, 6f), new Vector2(60f, 28f));
            minL.alignment = TextAlignmentOptions.BottomLeft;
            var maxL = CreateText(root, "100", 22f); maxL.gameObject.name = "Max";
            Anchor(maxL.rectTransform, new Vector2(1f, 0f), new Vector2(-10f, 6f), new Vector2(60f, 28f));
            maxL.alignment = TextAlignmentOptions.BottomRight;
            var w = UdonSharpUndo.AddComponent<UIHudGauge>(root.gameObject);
            w.fill = fill; w.needle = needle; w.valueLabel = val; w.minLabel = minL; w.maxLabel = maxL;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(new Color(0.45f, 0.85f, 0.45f), 0f), new GradientColorKey(new Color(1f, 0.85f, 0.3f), 0.6f), new GradientColorKey(new Color(1f, 0.4f, 0.4f), 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            w.colorByValue = g;
            w.value = 40f;
            Sync(w);
            return w;
        }

        private static RectTransform GraphPart(RectTransform plot, string name, Vector2 pivot)
        {
            var rt = CreateRect(name, plot);
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.pivot = pivot;
            rt.sizeDelta = Vector2.zero;
            var img = rt.gameObject.AddComponent<Image>();
            img.raycastTarget = false;
            rt.gameObject.SetActive(false);
            return rt;
        }

        // =====================================================================
        // Nested pages and containers (they can hold any widget, also each other)
        // =====================================================================

        /// <summary>Page group inside a page: tabs row, or a "&lt; name &gt;" selector row, plus its pages.</summary>
        internal static UIHudPages CreatePages(Transform parent, bool selector)
        {
            if (parent == null) return null;
            var root = CreateRect(selector ? "Pages (selector)" : "Pages (tabs)", parent);
            root.sizeDelta = new Vector2(600f, 400f);
            Layout(root.gameObject, -1f, 400f);
            var vl = root.gameObject.AddComponent<VerticalLayoutGroup>();
            vl.spacing = 8f; vl.childControlWidth = true; vl.childControlHeight = true; vl.childForceExpandHeight = false;

            var bar = CreateRect(selector ? "Selector" : "Tabs", root);
            var hl = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
            hl.spacing = 8f; hl.childControlWidth = true; hl.childControlHeight = true;
            Layout(bar.gameObject, -1f, 56f);
            var body = CreateRect("Pages", root);
            Layout(body.gameObject, -1f, -1f, 0f, 1f);

            var w = UdonSharpUndo.AddComponent<UIHudPages>(root.gameObject);
            w.pages = new GameObject[0];
            w.highlights = new GameObject[0];
            if (selector)
            {
                hl.childForceExpandWidth = false;
                var prev = CreateRawButton(bar, "<");
                Layout(prev.gameObject, 56f, 56f);
                var title = CreateText(bar, "Page 1", 30f);
                title.alignment = TextAlignmentOptions.Center;
                Layout(title.gameObject, -1f, 56f, 1f);
                var next = CreateRawButton(bar, ">");
                Layout(next.gameObject, 56f, 56f);
                w.titleLabel = title;
                Wire(prev.onClick, w, nameof(UIHudPages._OnPrev));
                Wire(next.onClick, w, nameof(UIHudPages._OnNext));
            }
            Sync(w);
            AddGroupPage(w, "Page 1");
            AddGroupPage(w, "Page 2");
            return w;
        }

        /// <summary>Adds a page (and a tab in tabs mode) to a page group.</summary>
        internal static RectTransform AddGroupPage(UIHudPages group, string name)
        {
            Transform body = group.transform.Find("Pages");
            if (body == null) { Debug.LogError("UIHUD: page group is missing its Pages child."); return null; }
            var page = CreateRect(name, body);
            Stretch(page);
            var vl = page.gameObject.AddComponent<VerticalLayoutGroup>();
            vl.spacing = 12f; vl.childControlWidth = true; vl.childControlHeight = true; vl.childForceExpandHeight = false;

            int index = group.pages != null ? group.pages.Length : 0;
            GameObject highlight = null;
            Transform tabs = group.transform.Find("Tabs");
            if (tabs != null)
            {
                var tabBtn = CreateRawButton(tabs, name);
                var tab = UdonSharpUndo.AddComponent<UIHudTab>(tabBtn.gameObject);
                tab.group = group;
                tab.pageIndex = index;
                Sync(tab);
                Wire(tabBtn.onClick, tab, nameof(UIHudTab._OnClick));
                var hlRt = CreateRect("Selected", tabBtn.transform);
                hlRt.anchorMin = new Vector2(0f, 0f); hlRt.anchorMax = new Vector2(1f, 0f);
                hlRt.pivot = new Vector2(0.5f, 0f); hlRt.sizeDelta = new Vector2(0f, 6f); hlRt.anchoredPosition = Vector2.zero;
                hlRt.gameObject.AddComponent<Image>().color = Accent;
                highlight = hlRt.gameObject;
                PlaceTab(tabBtn.transform, TabsOnSide(group));
            }

            Undo.RecordObject(group, "Add UIHUD Page");
            group.pages = Append(group.pages, page.gameObject);
            group.highlights = Append(group.highlights, highlight);
            page.gameObject.SetActive(index == group.startPage);
            if (highlight != null) highlight.SetActive(index == group.startPage);
            Sync(group);
            SetupPageScroll(group);
            return page;
        }

        internal static RectTransform CreateRow(Transform parent)
        {
            if (parent == null) return null;
            var row = CreateRect("Row", parent);
            row.sizeDelta = new Vector2(600f, 64f);
            Layout(row.gameObject, -1f, 64f);
            var hl = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            hl.spacing = 12f; hl.childControlWidth = true; hl.childControlHeight = true; hl.childForceExpandWidth = true;
            return row;
        }

        internal static RectTransform CreateGrid(Transform parent)
        {
            if (parent == null) return null;
            var grid = CreateRect("Grid", parent);
            grid.sizeDelta = new Vector2(600f, 260f);
            Layout(grid.gameObject, -1f, 260f);
            var g = grid.gameObject.AddComponent<GridLayoutGroup>();
            g.cellSize = new Vector2(180f, 120f);
            g.spacing = new Vector2(12f, 12f);
            return grid;
        }

        /// <summary>Scrollable list. Add widgets to its Content (the Inspector does this for you).</summary>
        internal static ScrollRect CreateScroll(Transform parent)
        {
            if (parent == null) return null;
            var root = CreateRect("Scroll", parent);
            root.sizeDelta = new Vector2(600f, 300f);
            Layout(root.gameObject, -1f, 300f);
            root.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.2f);
            var viewport = CreateRect("Viewport", root);
            Stretch(viewport, 8f);
            viewport.offsetMax = new Vector2(-28f, -8f); // room for the scrollbar
            viewport.gameObject.AddComponent<RectMask2D>();
            var content = CreateRect("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f); content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f); content.sizeDelta = Vector2.zero;
            var vl = content.gameObject.AddComponent<VerticalLayoutGroup>();
            vl.spacing = 12f; vl.childControlWidth = true; vl.childControlHeight = true; vl.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var sr = root.gameObject.AddComponent<ScrollRect>();
            sr.content = content; sr.viewport = viewport;
            sr.horizontal = false; sr.movementType = ScrollRect.MovementType.Clamped; sr.scrollSensitivity = 30f;
            AddScrollbar(sr);
            for (int i = 1; i <= 3; i++) { var t = CreateText(content, "Item " + i, 30f); t.gameObject.name = "Item " + i; Layout(t.gameObject, -1f, 48f); }
            return sr;
        }

        /// <summary>Vertical scrollbar on the right edge of a Scroll widget (drag it, or scroll the list).</summary>
        internal static Scrollbar AddScrollbar(ScrollRect sr)
        {
            var barRt = CreateRect("Scrollbar", sr.transform);
            barRt.anchorMin = new Vector2(1f, 0f); barRt.anchorMax = Vector2.one; barRt.pivot = new Vector2(1f, 0.5f);
            barRt.sizeDelta = new Vector2(16f, -16f); barRt.anchoredPosition = new Vector2(-6f, 0f);
            var back = barRt.gameObject.AddComponent<Image>();
            back.sprite = Res.background; back.type = Image.Type.Sliced; back.color = new Color(0f, 0f, 0f, 0.35f);
            var area = CreateRect("Sliding Area", barRt);
            Stretch(area);
            var handle = CreateRect("Handle", area);
            Stretch(handle);
            var handleImg = handle.gameObject.AddComponent<Image>();
            handleImg.sprite = Res.standard; handleImg.type = Image.Type.Sliced; handleImg.color = Accent;
            var bar = barRt.gameObject.AddComponent<Scrollbar>();
            bar.handleRect = handle; bar.targetGraphic = handleImg; bar.direction = Scrollbar.Direction.BottomToTop;
            Undo.RecordObject(sr, "Add Scrollbar");
            sr.verticalScrollbar = bar;
            sr.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
            if (sr.horizontal && !sr.vertical) SetScrollDirection(sr, true); // move it to the bottom
            return bar;
        }

        /// <summary>Vertical list (top to bottom) or horizontal strip (left to right); the scrollbar moves with it.</summary>
        internal static void SetScrollDirection(ScrollRect sr, bool horizontal)
        {
            RectTransform content = sr.content, viewport = sr.viewport;
            if (content == null || viewport == null) return;
            Scrollbar bar = sr.verticalScrollbar != null ? sr.verticalScrollbar : sr.horizontalScrollbar;
            Undo.RecordObject(sr, "Scroll Direction");
            sr.horizontal = horizontal; sr.vertical = !horizontal;
            sr.verticalScrollbar = horizontal ? null : bar;
            sr.horizontalScrollbar = horizontal ? bar : null;
            sr.horizontalScrollbarVisibility = sr.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;

            Undo.RecordObject(viewport, "Scroll Direction");
            viewport.offsetMin = new Vector2(8f, horizontal && bar != null ? 28f : 8f);
            viewport.offsetMax = new Vector2(!horizontal && bar != null ? -28f : -8f, -8f);

            HorizontalOrVerticalLayoutGroup g = horizontal
                ? (HorizontalOrVerticalLayoutGroup)SwapLayout<HorizontalLayoutGroup>(content.gameObject)
                : SwapLayout<VerticalLayoutGroup>(content.gameObject);
            g.childForceExpandWidth = !horizontal; g.childForceExpandHeight = horizontal;
            var fit = content.GetComponent<ContentSizeFitter>();
            if (fit == null) fit = Undo.AddComponent<ContentSizeFitter>(content.gameObject);
            Undo.RecordObject(fit, "Scroll Direction");
            fit.horizontalFit = horizontal ? ContentSizeFitter.FitMode.PreferredSize : ContentSizeFitter.FitMode.Unconstrained;
            fit.verticalFit = horizontal ? ContentSizeFitter.FitMode.Unconstrained : ContentSizeFitter.FitMode.PreferredSize;

            Undo.RecordObject(content, "Scroll Direction");
            content.anchorMin = horizontal ? Vector2.zero : new Vector2(0f, 1f);
            content.anchorMax = horizontal ? new Vector2(0f, 1f) : Vector2.one;
            content.pivot = horizontal ? new Vector2(0f, 0.5f) : new Vector2(0.5f, 1f);
            content.sizeDelta = Vector2.zero; content.anchoredPosition = Vector2.zero;
            if (horizontal) foreach (Transform c in content) FitWidth(c);

            if (bar == null) return;
            var br = (RectTransform)bar.transform;
            Undo.RecordObject(br, "Scroll Direction");
            Undo.RecordObject(bar, "Scroll Direction");
            if (horizontal)
            {
                br.anchorMin = Vector2.zero; br.anchorMax = new Vector2(1f, 0f); br.pivot = new Vector2(0.5f, 0f);
                br.sizeDelta = new Vector2(-16f, 16f); br.anchoredPosition = new Vector2(0f, 6f);
                bar.direction = Scrollbar.Direction.LeftToRight;
            }
            else
            {
                br.anchorMin = new Vector2(1f, 0f); br.anchorMax = Vector2.one; br.pivot = new Vector2(1f, 0.5f);
                br.sizeDelta = new Vector2(16f, -16f); br.anchoredPosition = new Vector2(-6f, 0f);
                bar.direction = Scrollbar.Direction.BottomToTop;
            }
        }

        /// <summary>
        /// Page area that scrolls: it clips, and each page is as tall as its widgets instead of squashing
        /// them to fit. The shown page is the scroll content (the menu / page group switches it at runtime).
        /// </summary>
        private static ScrollRect MakePagesScroll(Transform body, GameObject[] pages)
        {
            if (body == null) return null;
            var sr = body.GetComponent<ScrollRect>();
            if (sr == null)
            {
                if (body.GetComponent<RectMask2D>() == null) Undo.AddComponent<RectMask2D>(body.gameObject);
                sr = Undo.AddComponent<ScrollRect>(body.gameObject);
                sr.horizontal = false; sr.movementType = ScrollRect.MovementType.Clamped; sr.scrollSensitivity = 30f;
                sr.viewport = (RectTransform)body;
            }
            if (pages != null)
                foreach (var p in pages)
                {
                    if (p == null || p.GetComponent<ContentSizeFitter>() != null) continue;
                    var page = (RectTransform)p.transform;
                    Undo.RecordObject(page, "Scroll Pages");
                    page.anchorMin = new Vector2(0f, 1f); page.anchorMax = Vector2.one; page.pivot = new Vector2(0.5f, 1f);
                    page.sizeDelta = Vector2.zero; page.anchoredPosition = Vector2.zero;
                    Undo.AddComponent<ContentSizeFitter>(p).verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                }
            GameObject shown = pages != null ? System.Array.Find(pages, p => p != null && p.activeSelf) : null;
            RectTransform content = shown != null ? (RectTransform)shown.transform : null;
            if (sr.content != content) { Undo.RecordObject(sr, "Scroll Pages"); sr.content = content; }
            return sr;
        }

        internal static void SetupPageScroll(UIHudMenu menu)
        {
            ScrollRect sr = MakePagesScroll(menu.transform.Find("Pages"), menu.pages);
            if (sr == null || menu.pageScroll == sr) return;
            Undo.RecordObject(menu, "Scroll Pages");
            menu.pageScroll = sr;
            Sync(menu);
        }

        internal static void SetupPageScroll(UIHudPages group)
        {
            ScrollRect sr = MakePagesScroll(group.transform.Find("Pages"), group.pages);
            if (sr == null || group.pageScroll == sr) return;
            Undo.RecordObject(group, "Scroll Pages");
            group.pageScroll = sr;
            Sync(group);
        }

        /// <summary>Replaces the object's layout group with T (Unity allows only one), keeping the spacing.</summary>
        private static T SwapLayout<T>(GameObject go) where T : HorizontalOrVerticalLayoutGroup
        {
            var old = go.GetComponent<HorizontalOrVerticalLayoutGroup>();
            if (old is T same) { Undo.RecordObject(same, "Layout"); return same; }
            float spacing = old != null ? old.spacing : 12f;
            if (old != null) Undo.DestroyObjectImmediate(old);
            var g = Undo.AddComponent<T>(go);
            g.spacing = spacing; g.childControlWidth = true; g.childControlHeight = true;
            return g;
        }

        internal static bool TabsOnSide(UIHudPages group)
        {
            Transform tabs = group.transform.Find("Tabs");
            return tabs != null && tabs.GetComponent<VerticalLayoutGroup>() != null;
        }

        /// <summary>Tabs in a row on top, or in a column down the left side of the pages.</summary>
        internal static void SetTabsSide(UIHudPages group, bool side)
        {
            Transform tabs = group.transform.Find("Tabs");
            Transform body = group.transform.Find("Pages");
            if (tabs == null || body == null) return;
            HorizontalOrVerticalLayoutGroup root = side
                ? (HorizontalOrVerticalLayoutGroup)SwapLayout<HorizontalLayoutGroup>(group.gameObject)
                : SwapLayout<VerticalLayoutGroup>(group.gameObject);
            root.childForceExpandWidth = !side; root.childForceExpandHeight = side;
            HorizontalOrVerticalLayoutGroup col = side
                ? (HorizontalOrVerticalLayoutGroup)SwapLayout<VerticalLayoutGroup>(tabs.gameObject)
                : SwapLayout<HorizontalLayoutGroup>(tabs.gameObject);
            col.childForceExpandWidth = true; col.childForceExpandHeight = !side;

            var le = tabs.GetComponent<LayoutElement>();
            Undo.RecordObject(le, "Tabs Side");
            le.preferredWidth = side ? 180f : -1f;
            le.preferredHeight = side ? -1f : 56f;
            var bodyLe = body.GetComponent<LayoutElement>();
            Undo.RecordObject(bodyLe, "Tabs Side");
            bodyLe.flexibleWidth = 1f; bodyLe.flexibleHeight = 1f;
            foreach (Transform tab in tabs) PlaceTab(tab, side);
        }

        /// <summary>Tab height for the side column, and its "selected" line: under it on top, left of it on the side.</summary>
        private static void PlaceTab(Transform tab, bool side)
        {
            var le = tab.GetComponent<LayoutElement>();
            if (le == null) le = Undo.AddComponent<LayoutElement>(tab.gameObject);
            Undo.RecordObject(le, "Tabs Side");
            le.preferredHeight = 56f;
            var hl = tab.Find("Selected") as RectTransform;
            if (hl == null) return;
            Undo.RecordObject(hl, "Tabs Side");
            hl.anchorMin = Vector2.zero;
            hl.anchorMax = side ? new Vector2(0f, 1f) : new Vector2(1f, 0f);
            hl.pivot = side ? new Vector2(0f, 0.5f) : new Vector2(0.5f, 0f);
            hl.sizeDelta = side ? new Vector2(6f, 0f) : new Vector2(0f, 6f);
            hl.anchoredPosition = Vector2.zero;
        }

        /// <summary>In a horizontal strip each item needs a width (lists only set heights): 200 px if it has none.</summary>
        internal static void FitWidth(Transform item)
        {
            var hl = item.parent != null ? item.parent.GetComponent<HorizontalLayoutGroup>() : null;
            if (hl == null || hl.childForceExpandWidth) return; // Rows share the width already
            var le = item.GetComponent<LayoutElement>();
            if (le == null) le = Undo.AddComponent<LayoutElement>(item.gameObject);
            if (le.preferredWidth > 0f) return;
            Undo.RecordObject(le, "Item Width");
            le.preferredWidth = 200f;
        }

        /// <summary>Round progress ring (same API as the bar: SetValue, SetCurrent, Add).</summary>
        internal static UIHudBar CreateRadial(Transform parent)
        {
            if (parent == null) return null;
            var root = CreateRect("Radial", parent);
            root.sizeDelta = new Vector2(180f, 180f);
            Layout(root.gameObject, -1f, 180f);
            var ring = CreateRect("Ring", root);
            ring.anchorMin = ring.anchorMax = new Vector2(0.5f, 0.5f);
            ring.sizeDelta = new Vector2(170f, 170f);
            var back = ring.gameObject.AddComponent<Image>();
            back.sprite = Res.knob; back.color = new Color(0f, 0f, 0f, 0.45f); back.raycastTarget = false;
            var fillRt = CreateRect("Fill", ring);
            Stretch(fillRt);
            var fill = fillRt.gameObject.AddComponent<Image>();
            fill.sprite = Res.knob; fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Radial360; fill.fillOrigin = (int)Image.Origin360.Top; fill.fillClockwise = true;
            fill.color = new Color(0.3f, 0.85f, 0.4f, 1f); fill.raycastTarget = false;
            var hole = CreateRect("Hole", ring);
            hole.anchorMin = hole.anchorMax = new Vector2(0.5f, 0.5f);
            hole.sizeDelta = new Vector2(130f, 130f);
            var holeImg = hole.gameObject.AddComponent<Image>();
            holeImg.sprite = Res.knob; holeImg.color = Panel; holeImg.raycastTarget = false;
            var text = CreateText(ring, "100%", 34f);
            text.alignment = TextAlignmentOptions.Center;
            Stretch(text.rectTransform);
            var w = UdonSharpUndo.AddComponent<UIHudBar>(root.gameObject);
            w.fill = fill;
            w.label = text;
            w.labelFormat = "{2:0}%";
            w.useGradient = false;
            w.colorByValue = new Gradient();
            Sync(w);
            return w;
        }

        // =====================================================================
        // Low-level helpers
        // =====================================================================

        /// <summary>
        /// World Space canvas that rides with the player. Layer Walkthrough: VRChat docs say its
        /// colliders never collide with players (the UI and UiMenu layers are ignored by the pointer).
        /// </summary>
        private static RectTransform CreateCanvas(string name, Transform parent, bool interactive, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            go.transform.SetParent(parent, false);
            go.layer = WalkthroughLayer;
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            go.AddComponent<CanvasScaler>().dynamicPixelsPerUnit = 2f;
            var rt = (RectTransform)go.transform;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.localScale = Vector3.one * 0.0005f;
            if (interactive)
            {
                go.AddComponent<GraphicRaycaster>();
                // Collider covers this canvas only, so other canvases/world stay clickable.
                var col = go.AddComponent<BoxCollider>();
                col.size = new Vector3(size.x, size.y, 0f);
                var shape = go.AddComponent<VRCUiShape>();
                // Focus View needs 0.6m+ distance; it makes no sense for UI glued to the player.
                shape.AllowFocusView = false;
            }
            return rt;
        }

        private static int WalkthroughLayer
        {
            get { int l = LayerMask.NameToLayer("Walkthrough"); return l >= 0 ? l : 17; }
        }

        private static void NoNavigation(Selectable s)
        {
            s.navigation = new Navigation { mode = Navigation.Mode.None };
        }

        /// <summary>VRChat UI needs exactly one EventSystem in the scene (also for ClientSim).</summary>
        private static void EnsureEventSystem()
        {
            if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() != null) return;
            var es = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));
            Undo.RegisterCreatedObjectUndo(es, "Create EventSystem");
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            go.transform.SetParent(parent, false);
            go.layer = parent.gameObject.layer;
            return (RectTransform)go.transform;
        }

        internal static TextMeshProUGUI CreateText(Transform parent, string text, float size)
        {
            var rt = CreateRect("Text", parent);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.text = text;
            t.fontSize = size;
            t.color = Color.white;
            t.alignment = TextAlignmentOptions.MidlineLeft;
            t.raycastTarget = false;
            return t;
        }

        private static Button CreateRawButton(Transform parent, string label)
        {
            GameObject go = TMP_DefaultControls.CreateButton(Res);
            Place(go, parent, label);
            ((RectTransform)go.transform).sizeDelta = new Vector2(240f, 64f);
            go.GetComponent<Image>().color = Color.white;
            var text = go.GetComponentInChildren<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = 30f;
            text.color = Color.white;
            text.raycastTarget = false;
            var btn = go.GetComponent<Button>();
            NoNavigation(btn);
            var colors = btn.colors; // tint multiplies the white image
            colors.normalColor = Soft;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.2f);
            colors.selectedColor = Soft;
            colors.pressedColor = Accent;
            btn.colors = colors;
            return btn;
        }

        private static void Place(GameObject go, Transform parent, string name)
        {
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            go.name = name;
            go.transform.SetParent(parent, false);
            SetLayer(go, parent.gameObject.layer);
        }

        private static void SetLayer(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform c in go.transform) SetLayer(c.gameObject, layer);
        }

        internal static void Layout(GameObject go, float width, float height, float flexW = 0f, float flexH = 0f)
        {
            var le = go.GetComponent<LayoutElement>();
            if (le == null) le = go.AddComponent<LayoutElement>();
            if (width >= 0f) le.preferredWidth = width;
            if (height >= 0f) le.preferredHeight = height;
            le.flexibleWidth = flexW;
            le.flexibleHeight = flexH;
        }

        private static void Anchor(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = anchor; rt.anchorMax = anchor; rt.pivot = anchor;
            rt.anchoredPosition = pos; rt.sizeDelta = size;
        }

        private static void Stretch(RectTransform rt, float inset = 0f)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.one * inset; rt.offsetMax = -Vector2.one * inset;
        }

        private static DefaultControls.Resources ToUgui(TMP_DefaultControls.Resources r)
        {
            return new DefaultControls.Resources
            {
                standard = r.standard, background = r.background, inputField = r.inputField,
                knob = r.knob, checkmark = r.checkmark, dropdown = r.dropdown, mask = r.mask,
            };
        }

        private static T[] Append<T>(T[] arr, T item)
        {
            var list = new System.Collections.Generic.List<T>(arr ?? new T[0]) { item };
            return list.ToArray();
        }

        /// <summary>UI event -> UdonBehaviour.SendCustomEvent(eventName), as a persistent listener.</summary>
        private static void Wire(UnityEventBase evt, UdonSharpBehaviour behaviour, string eventName)
        {
            var udon = UdonSharpEditorUtility.GetBackingUdonBehaviour(behaviour);
            UnityEventTools.AddStringPersistentListener(evt, new UnityAction<string>(udon.SendCustomEvent), eventName);
        }

        internal static void Sync(UdonSharpBehaviour b)
        {
            if (b == null) return;
            UdonSharpEditorUtility.CopyProxyToUdon(b);
            EditorUtility.SetDirty(b);
        }
    }
}
