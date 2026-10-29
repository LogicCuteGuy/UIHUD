using System.Collections.Generic;
using TMPro;
using UdonSharp;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace LogicCuteGuy.UIHUD.Editor
{
    /// <summary>Small shared GUI helpers for the UIHUD inspectors.</summary>
    internal static class UIHudGUI
    {
        private static GUIStyle _box;
        internal static GUIStyle Box => _box ?? (_box = new GUIStyle("HelpBox") { padding = new RectOffset(8, 8, 6, 6) });

        internal static void Section(string title)
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        }

        internal static bool Mini(string label, float width = 24f) => GUILayout.Button(label, EditorStyles.miniButton, GUILayout.Width(width));

        internal static void Rename(GameObject go, string newName)
        {
            if (go == null || string.IsNullOrEmpty(newName) || go.name == newName) return;
            Undo.RecordObject(go, "Rename");
            go.name = newName;
        }

        internal static bool ConfirmDelete(string what) =>
            EditorUtility.DisplayDialog("UIHUD", "Delete " + what + "?\n(You can undo with Ctrl+Z.)", "Delete", "Cancel");

        internal static void Ping(Object o)
        {
            if (o == null) return;
            Selection.activeObject = o;
            EditorGUIUtility.PingObject(o);
        }

        /// <summary>The widget's own script (not its animation); with children = also look inside (slider row).</summary>
        internal static UdonSharpBehaviour WidgetScript(GameObject go, bool children)
        {
            var all = children ? go.GetComponentsInChildren<UdonSharpBehaviour>(true) : go.GetComponents<UdonSharpBehaviour>();
            foreach (var b in all) if (!(b is UIHudAnim)) return b;
            return null;
        }

        /// <summary>Widget type shown in lists, from its components.</summary>
        internal static string Kind(GameObject go)
        {
            if (go.GetComponent<UIHudPages>()) return "Pages";
            if (go.GetComponent<ScrollRect>()) return "Scroll";
            if (go.GetComponent<GridLayoutGroup>()) return "Grid";
            if (go.GetComponent<HorizontalLayoutGroup>() && WidgetScript(go, false) == null) return "Row";
            if (go.GetComponentInChildren<UIHudSlider>(true)) return "Slider";
            if (go.GetComponent<UIHudToggle>()) return "Toggle";
            if (go.GetComponent<UIHudButton>()) return "Button";
            if (go.GetComponent<UIHudBar>()) return "Bar";
            if (go.GetComponent<UIHudNotification>()) return "Toast";
            if (go.GetComponent<UIHudSelector>()) return "Select";
            if (go.GetComponentInChildren<UIHudInput>(true)) return "Input";
            if (go.GetComponent<UIHudPlayerList>()) return "Players";
            if (go.GetComponent<UIHudInfo>()) return "Info";
            if (go.GetComponent<UIHudValue>()) return "Value";
            if (go.GetComponent<UIHudTable>()) return "Table";
            if (go.GetComponent<UIHudGraph>()) return "Graph";
            if (go.GetComponent<UIHudStat>()) return "Stat";
            if (go.GetComponent<UIHudPie>()) return "Pie";
            if (go.GetComponent<UIHudBarList>()) return "Bar List";
            if (go.GetComponent<UIHudGauge>()) return "Gauge";
            if (go.GetComponent<TextMeshProUGUI>()) return "Label";
            if (go.GetComponent<Image>()) return "Image";
            if (go.transform.childCount == 0 && go.GetComponent<Graphic>() == null) return "Space";
            return "Group";
        }

        private static readonly HashSet<int> Open = new HashSet<int>();

        internal static readonly UIHudWidgetKind[] PageKinds =
        {
            UIHudWidgetKind.Button, UIHudWidgetKind.Toggle, UIHudWidgetKind.Slider, UIHudWidgetKind.Selector, UIHudWidgetKind.Input,
            UIHudWidgetKind.Header, UIHudWidgetKind.Label, UIHudWidgetKind.Info, UIHudWidgetKind.PlayerList,
            UIHudWidgetKind.Value, UIHudWidgetKind.Table, UIHudWidgetKind.Graph,
            UIHudWidgetKind.Stat, UIHudWidgetKind.Pie, UIHudWidgetKind.BarList, UIHudWidgetKind.Gauge,
            UIHudWidgetKind.Bar, UIHudWidgetKind.Radial, UIHudWidgetKind.Image, UIHudWidgetKind.Divider, UIHudWidgetKind.Space,
            UIHudWidgetKind.PagesTabs, UIHudWidgetKind.PagesSelector, UIHudWidgetKind.Row, UIHudWidgetKind.Grid, UIHudWidgetKind.Scroll,
        };

        internal static readonly UIHudWidgetKind[] HudKinds =
        {
            UIHudWidgetKind.Bar, UIHudWidgetKind.Radial, UIHudWidgetKind.Header, UIHudWidgetKind.Label, UIHudWidgetKind.Info,
            UIHudWidgetKind.Value, UIHudWidgetKind.Table, UIHudWidgetKind.Graph,
            UIHudWidgetKind.Stat, UIHudWidgetKind.Pie, UIHudWidgetKind.BarList, UIHudWidgetKind.Gauge,
            UIHudWidgetKind.Notification, UIHudWidgetKind.Image, UIHudWidgetKind.Row,
        };

        internal static string KindLabel(UIHudWidgetKind k)
        {
            switch (k)
            {
                case UIHudWidgetKind.PagesTabs: return "Pages (tabs)";
                case UIHudWidgetKind.PagesSelector: return "Pages (< selector >)";
                case UIHudWidgetKind.PlayerList: return "Player List";
                case UIHudWidgetKind.Radial: return "Radial Bar";
                case UIHudWidgetKind.Notification: return "Notification (toast)";
                case UIHudWidgetKind.Input: return "Text Input";
                case UIHudWidgetKind.Value: return "Value (variable)";
                case UIHudWidgetKind.Table: return "Table (data)";
                case UIHudWidgetKind.Stat: return "Stat Tile";
                case UIHudWidgetKind.Pie: return "Pie Chart";
                case UIHudWidgetKind.BarList: return "Bar List";
                default: return k.ToString();
            }
        }

        internal static string KindHelp(UIHudWidgetKind k)
        {
            switch (k)
            {
                case UIHudWidgetKind.Button: return "Click: toggle objects, open/close menus, teleport, respawn, toast, call your script.";
                case UIHudWidgetKind.Toggle: return "On/off switch. Shows/hides objects and reports the bool.";
                case UIHudWidgetKind.Slider: return "Value slider with a live label. Can drive an AudioSource volume.";
                case UIHudWidgetKind.Selector: return "< option > picker. Can show only the matching object.";
                case UIHudWidgetKind.Input: return "Text field. VRChat opens its keyboard; sends the text on submit.";
                case UIHudWidgetKind.Header: return "Big bold title text.";
                case UIHudWidgetKind.Label: return "Plain text.";
                case UIHudWidgetKind.Info: return "Live text: {time} {date} {players} {fps} {name} {uptime} {height}.";
                case UIHudWidgetKind.PlayerList: return "Players in the instance, updates on join/leave.";
                case UIHudWidgetKind.Value: return "Shows a variable of any script (\"Coins: {0}\"), updates by itself. Or SetValue(v).";
                case UIHudWidgetKind.Table: return "Rows/columns from a DataList, DataDictionary or JSON. Sort + rank for leaderboards.";
                case UIHudWidgetKind.Graph: return "Line or bar chart over time. AddSample(v), or watch a number variable / fps.";
                case UIHudWidgetKind.Stat: return "Big number + title + change since last (+5 / -3). SetValue(v) or watch a variable.";
                case UIHudWidgetKind.Pie: return "Donut of named numbers (votes, teams) with legend. SetJson / SetEntry(name, v).";
                case UIHudWidgetKind.BarList: return "Bar per entry (team scores, polls), sorted. SetJson / SetEntry(name, v).";
                case UIHudWidgetKind.Gauge: return "Half-circle dial with needle. SetValue(v) or watch a number variable.";
                case UIHudWidgetKind.Bar: return "Health/progress bar. SetValue(cur, max), Add(delta).";
                case UIHudWidgetKind.Radial: return "Round progress ring, same API as the bar.";
                case UIHudWidgetKind.Notification: return "Toast messages. Show(\"text\").";
                case UIHudWidgetKind.Image: return "Picture or icon.";
                case UIHudWidgetKind.Divider: return "Thin separator line.";
                case UIHudWidgetKind.Space: return "Empty gap.";
                case UIHudWidgetKind.PagesTabs: return "Pages inside this page, switched by tabs. Its pages can hold more pages.";
                case UIHudWidgetKind.PagesSelector: return "Pages inside this page, switched by < name >. Nest as deep as you like.";
                case UIHudWidgetKind.Row: return "Puts widgets side by side. Holds any widget.";
                case UIHudWidgetKind.Grid: return "Grid of tiles (buttons, images...). Holds any widget.";
                case UIHudWidgetKind.Scroll: return "Scrollable list for long content. Holds any widget.";
                default: return "";
            }
        }

        /// <summary>
        /// Editable list of a transform's children: show/hide, rename, quick-edit (▸), move, duplicate, delete.
        /// free = children are placed by anchors (HUD) instead of a layout group (menu page).
        /// </summary>
        internal static void ChildList(Transform parent, string emptyText, bool free = false)
        {
            if (parent == null) return;
            if (parent.childCount == 0) { EditorGUILayout.LabelField(emptyText, EditorStyles.miniLabel); return; }
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform c = parent.GetChild(i);
                int id = c.GetInstanceID();
                bool open = Open.Contains(id);
                using (new EditorGUILayout.HorizontalScope())
                {
                    bool o = GUILayout.Toggle(open, new GUIContent("", "Quick edit"), EditorStyles.foldout, GUILayout.Width(12));
                    if (o != open) { if (o) Open.Add(id); else Open.Remove(id); open = o; }
                    bool on = EditorGUILayout.Toggle(c.gameObject.activeSelf, GUILayout.Width(16));
                    if (on != c.gameObject.activeSelf) { Undo.RecordObject(c.gameObject, "Toggle"); c.gameObject.SetActive(on); }
                    EditorGUILayout.LabelField(Kind(c.gameObject), EditorStyles.miniLabel, GUILayout.Width(46));
                    Rename(c.gameObject, EditorGUILayout.DelayedTextField(c.name));
                    using (new EditorGUI.DisabledScope(i == 0))
                        if (Mini("▲")) { Undo.SetTransformParent(c, parent, "Move"); c.SetSiblingIndex(i - 1); }
                    using (new EditorGUI.DisabledScope(i == parent.childCount - 1))
                        if (Mini("▼")) { Undo.SetTransformParent(c, parent, "Move"); c.SetSiblingIndex(i + 1); }
                    if (Mini(new GUIContent(EditorGUIUtility.IconContent("TreeEditor.Duplicate").image, "Duplicate"))) { Duplicate(c); GUIUtility.ExitGUI(); }
                    if (Mini(new GUIContent("◎", "Select in Hierarchy"))) Ping(c.gameObject);
                    if (Mini("✕") && ConfirmDelete("'" + c.name + "'")) { Undo.DestroyObjectImmediate(c.gameObject); GUIUtility.ExitGUI(); }
                }
                if (open)
                    using (new EditorGUILayout.VerticalScope(Box))
                        WidgetDetails(c.gameObject, free);
            }
        }

        internal static bool Mini(GUIContent label, float width = 24f) => GUILayout.Button(label, EditorStyles.miniButton, GUILayout.Width(width));

        /// <summary>Copy next to the original. Instantiate remaps the inner references (event listeners, U# links).</summary>
        internal static GameObject Duplicate(Transform c)
        {
            GameObject copy = Object.Instantiate(c.gameObject, c.parent);
            copy.name = c.name;
            Undo.RegisterCreatedObjectUndo(copy, "Duplicate");
            copy.transform.SetSiblingIndex(c.GetSiblingIndex() + 1);
            foreach (var b in copy.GetComponentsInChildren<UdonSharpBehaviour>(true)) UIHudBuilder.Sync(b);
            Ping(copy);
            return copy;
        }

        // ---------- Quick edit ----------

        // Wiring set up by the builder: hidden so it can't be broken by accident.
        private static readonly HashSet<string> Wiring = new HashSet<string> { "toggle", "slider", "valueLabel", "fill", "label", "menu", "pageIndex", "input", "list", "pages", "highlights", "titleLabel", "group", "pageScroll" };
        private static readonly string[] Anchors = { "Top Left", "Top", "Top Right", "Left", "Center", "Right", "Bottom Left", "Bottom", "Bottom Right" };

        /// <summary>The most useful settings of one widget, in one box.</summary>
        internal static void WidgetDetails(GameObject go, bool free)
        {
            string kind = Kind(go);
            var rt = (RectTransform)go.transform;

            // Text
            var input = go.GetComponent<TMP_InputField>();
            bool live = kind == "Info" || kind == "Players" || kind == "Value"; // text is written at runtime
            TextMeshProUGUI tmp = input != null ? input.placeholder as TextMeshProUGUI
                : kind == "Label" || kind == "Info" || kind == "Value" ? go.GetComponent<TextMeshProUGUI>()
                : kind == "Button" || kind == "Toggle" || kind == "Slider" || kind == "Select" || kind == "Players" ? go.GetComponentInChildren<TextMeshProUGUI>(true) : null;
            if (tmp != null)
            {
                EditorGUI.BeginChangeCheck();
                string text = live ? tmp.text : EditorGUILayout.TextField(input != null ? "Placeholder" : "Text", tmp.text);
                float size = EditorGUILayout.Slider("Font size", tmp.fontSize, 8f, 120f);
                Color color = EditorGUILayout.ColorField("Text color", tmp.color);
                var style = (FontStyles)EditorGUILayout.EnumFlagsField("Font style", tmp.fontStyle);
                var align = (TextAlignmentOptions)EditorGUILayout.EnumPopup("Alignment", tmp.alignment);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(tmp, "Edit Text");
                    tmp.text = text; tmp.fontSize = size; tmp.color = color; tmp.fontStyle = style; tmp.alignment = align;
                }
            }

            // Image
            if (kind == "Image")
            {
                var img = go.GetComponent<Image>();
                EditorGUI.BeginChangeCheck();
                var sprite = (Sprite)EditorGUILayout.ObjectField("Sprite", img.sprite, typeof(Sprite), false);
                Color c = EditorGUILayout.ColorField("Color", img.color);
                bool keep = EditorGUILayout.Toggle("Keep aspect", img.preserveAspect);
                if (EditorGUI.EndChangeCheck()) { Undo.RecordObject(img, "Edit Image"); img.sprite = sprite; img.color = c; img.preserveAspect = keep; }
            }

            // Unity controls
            var toggle = go.GetComponent<Toggle>();
            if (toggle != null)
            {
                bool isOn = EditorGUILayout.Toggle("On at start", toggle.isOn);
                if (isOn != toggle.isOn) { Undo.RecordObject(toggle, "Toggle"); toggle.SetIsOnWithoutNotify(isOn); EditorUtility.SetDirty(toggle); }
            }
            var slider = go.GetComponentInChildren<Slider>(true);
            if (slider != null)
            {
                EditorGUI.BeginChangeCheck();
                float min = EditorGUILayout.FloatField("Min", slider.minValue);
                float max = EditorGUILayout.FloatField("Max", slider.maxValue);
                float val = EditorGUILayout.Slider("Start value", slider.value, min, Mathf.Max(min, max));
                bool whole = EditorGUILayout.Toggle("Whole numbers", slider.wholeNumbers);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(slider, "Slider");
                    slider.minValue = min; slider.maxValue = max; slider.wholeNumbers = whole; slider.SetValueWithoutNotify(val);
                    EditorUtility.SetDirty(slider);
                }
            }

            // Size / position
            if (!free && go.transform.parent != null && go.transform.parent.GetComponent<LayoutGroup>() != null)
            {
                var leAbs = go.GetComponent<LayoutElement>();
                bool abs = leAbs != null && leAbs.ignoreLayout;
                bool na = EditorGUILayout.Toggle(new GUIContent("Absolute position", "Leave the list layout: own position and size. Drag it in the Preview window."), abs);
                // The box below changes shape: redraw from scratch instead of finishing this event.
                if (na != abs) { SetAbsolute(rt, na); GUIUtility.ExitGUI(); }
                free = na;
            }
            if (free)
            {
                int cur = System.Array.FindIndex(AnchorPoints, a => a == rt.anchorMin && a == rt.anchorMax);
                int ni = EditorGUILayout.Popup("Anchor", cur, Anchors);
                EditorGUI.BeginChangeCheck();
                Vector2 pos = ni == cur ? EditorGUILayout.Vector2Field("Offset", rt.anchoredPosition) : rt.anchoredPosition;
                Vector2 sz = EditorGUILayout.Vector2Field("Size", rt.sizeDelta);
                if (ni != cur && ni >= 0)
                {
                    Vector2 a = AnchorPoints[ni];
                    pos = new Vector2(a.x == 0f ? 60f : a.x == 1f ? -60f : 0f, a.y == 0f ? 60f : a.y == 1f ? -60f : 0f);
                    Undo.RecordObject(rt, "Anchor");
                    rt.anchorMin = rt.anchorMax = rt.pivot = a;
                    rt.anchoredPosition = pos;
                }
                if (EditorGUI.EndChangeCheck()) { Undo.RecordObject(rt, "Move"); rt.anchoredPosition = pos; rt.sizeDelta = sz; }
            }
            else
            {
                var le = go.GetComponent<LayoutElement>();
                if (le != null)
                {
                    float h = EditorGUILayout.FloatField("Height", le.preferredHeight);
                    if (h != le.preferredHeight) { Undo.RecordObject(le, "Height"); le.preferredHeight = Mathf.Max(0f, h); }
                    var strip = go.transform.parent != null ? go.transform.parent.GetComponent<HorizontalLayoutGroup>() : null;
                    if (strip != null && !strip.childForceExpandWidth)
                    {
                        float wd = EditorGUILayout.FloatField("Width", le.preferredWidth);
                        if (wd != le.preferredWidth) { Undo.RecordObject(le, "Width"); le.preferredWidth = Mathf.Max(0f, wd); }
                    }
                }
            }

            bool container = kind == "Pages" || kind == "Row" || kind == "Grid" || kind == "Scroll";
            if (kind == "Grid")
            {
                var grid = go.GetComponent<GridLayoutGroup>();
                EditorGUI.BeginChangeCheck();
                Vector2 cell = EditorGUILayout.Vector2Field("Cell size", grid.cellSize);
                Vector2 gap = EditorGUILayout.Vector2Field("Spacing", grid.spacing);
                if (EditorGUI.EndChangeCheck()) { Undo.RecordObject(grid, "Grid"); grid.cellSize = cell; grid.spacing = gap; }
            }

            // Colors and pictures of every part; widgets inside containers have their own.
            var inner = new List<Transform>();
            if (kind == "Row" || kind == "Grid") foreach (Transform c in go.transform) inner.Add(c);
            else if (kind == "Scroll" && go.GetComponent<ScrollRect>().viewport != null) inner.Add(go.GetComponent<ScrollRect>().viewport);
            else if (kind == "Pages" && go.transform.Find("Pages") != null) inner.Add(go.transform.Find("Pages"));
            Looks(go.transform, inner);

            UIHudAnimGUI.Section(go);

            // Udon settings (actions, callbacks, formats)
            var b = WidgetScript(go, !container);
            if (b != null) DrawBehaviour(b);

            // Children: the same list again, so containers and pages nest as deep as you like.
            if (kind == "Pages") PagesGroup(go.GetComponent<UIHudPages>());
            else if (container)
            {
                var sr = go.GetComponent<ScrollRect>();
                if (sr != null)
                {
                    bool horizontal = sr.horizontal && !sr.vertical;
                    int d = EditorGUILayout.Popup("Direction", horizontal ? 1 : 0, new[] { "Vertical (list)", "Horizontal (strip)" });
                    if ((d == 1) != horizontal) { UIHudBuilder.SetScrollDirection(sr, d == 1); GUIUtility.ExitGUI(); }
                }
                if (sr != null && sr.verticalScrollbar == null && sr.horizontalScrollbar == null && GUILayout.Button("+ Add scrollbar", EditorStyles.miniButton))
                {
                    var rv = sr.viewport;
                    if (rv != null) { Undo.RecordObject(rv, "Add Scrollbar"); rv.offsetMax = new Vector2(-28f, rv.offsetMax.y); }
                    Scrollbar bar = UIHudBuilder.AddScrollbar(sr);
                    Undo.RegisterCreatedObjectUndo(bar.gameObject, "Add Scrollbar");
                    if (UIHudStyle.Current != null) UIHudStyle.Current.Apply(go.transform);
                }
                Transform content = sr != null ? sr.content : go.transform;
                if (content == null) return;
                EditorGUILayout.LabelField("Inside " + go.name, EditorStyles.miniBoldLabel);
                ChildList(content, "Empty.");
                AddWidgetBar(content, PageKinds);
            }
        }

        /// <summary>Takes a widget out of (or back into) its list layout. It stays where it is when taken out.</summary>
        internal static void SetAbsolute(RectTransform rt, bool on)
        {
            var le = rt.GetComponent<LayoutElement>();
            if (le == null) le = Undo.AddComponent<LayoutElement>(rt.gameObject);
            Undo.RecordObject(le, "Absolute Position");
            Undo.RecordObject(rt, "Absolute Position");
            le.ignoreLayout = on;
            if (!on) return; // the layout places it again
            var corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            Vector2 size = rt.rect.size;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = size;
            rt.position = corners[1]; // top-left corner stays put
        }

        /// <summary>Can be moved freely: absolute, or placed directly on a canvas without a layout (HUD).</summary>
        internal static bool Draggable(RectTransform r)
        {
            var le = r.GetComponent<LayoutElement>();
            if (le != null && le.ignoreLayout) return true;
            Transform p = r.parent;
            return p != null && p.GetComponent<Canvas>() != null && p.GetComponent<LayoutGroup>() == null;
        }

        private static readonly HashSet<int> LooksOpen = new HashSet<int>();

        /// <summary>
        /// "Colors and textures": color and picture (sprite or texture) of every part, font of every text,
        /// and normal / hover / pressed tints of buttons and other controls. Parts under skip are left out.
        /// </summary>
        internal static void Looks(Transform root, ICollection<Transform> skip)
        {
            int id = root.GetInstanceID();
            bool open = EditorGUILayout.Foldout(LooksOpen.Contains(id), "Colors & textures", true);
            if (open) LooksOpen.Add(id); else LooksOpen.Remove(id);
            if (!open) return;

            var parts = new List<Transform>();
            CollectParts(root, skip, parts);
            using (new EditorGUILayout.VerticalScope(Box))
            {
                EditorGUILayout.LabelField("Applying a style repaints these. Textures must be imported as Sprite (2D and UI).", EditorStyles.wordWrappedMiniLabel);
                foreach (Transform t in parts)
                {
                    string label = t == root ? "Background" : t.name.StartsWith("Text") && t.parent != null ? (t.parent == root ? "" : t.parent.name + " ") + "text" : t.name;
                    foreach (var g in t.GetComponents<Graphic>()) PartRow(label, g);
                    var sel = t.GetComponent<Selectable>();
                    if (sel != null) TintRow(label, sel);
                }
            }
        }

        private static void CollectParts(Transform t, ICollection<Transform> skip, List<Transform> parts)
        {
            if (skip != null && skip.Contains(t)) return;
            parts.Add(t);
            foreach (Transform c in t) CollectParts(c, skip, parts);
        }

        private static void PartRow(string label, Graphic g)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(new GUIContent(label, g.GetType().Name), GUILayout.Width(110));
                EditorGUI.BeginChangeCheck();
                Color color = EditorGUILayout.ColorField(GUIContent.none, g.color, GUILayout.Width(60));
                Object pic = g is Image img ? EditorGUILayout.ObjectField(img.sprite, typeof(Sprite), false)
                    : g is RawImage raw ? EditorGUILayout.ObjectField(raw.texture, typeof(Texture), false)
                    : g is TextMeshProUGUI tmp ? EditorGUILayout.ObjectField(tmp.font, typeof(TMP_FontAsset), false)
                    : null;
                if (!EditorGUI.EndChangeCheck()) return;
                Undo.RecordObject(g, "Edit Look");
                g.color = color;
                if (g is Image i)
                {
                    i.sprite = (Sprite)pic;
                    // Bordered sprites stretch nicely when sliced; fills (bars, rings) keep their type.
                    if (i.type != Image.Type.Filled) i.type = i.sprite != null && i.sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
                }
                else if (g is RawImage r) r.texture = (Texture)pic;
                else if (g is TextMeshProUGUI tx && pic != null) tx.font = (TMP_FontAsset)pic;
            }
        }

        private static void TintRow(string label, Selectable sel)
        {
            ColorBlock cb = sel.colors;
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(new GUIContent(label + " tint", "Normal / Hover / Pressed. Multiplies the part's color."), GUILayout.Width(110));
                EditorGUI.BeginChangeCheck();
                cb.normalColor = EditorGUILayout.ColorField(GUIContent.none, cb.normalColor, GUILayout.MinWidth(40));
                cb.highlightedColor = EditorGUILayout.ColorField(GUIContent.none, cb.highlightedColor, GUILayout.MinWidth(40));
                cb.pressedColor = EditorGUILayout.ColorField(GUIContent.none, cb.pressedColor, GUILayout.MinWidth(40));
                if (!EditorGUI.EndChangeCheck()) return;
                cb.selectedColor = cb.normalColor;
                Undo.RecordObject(sel, "Edit Tint");
                sel.colors = cb;
            }
        }

        /// <summary>Pages of a nested page group: pick, rename, add, delete; the shown page lists its widgets.</summary>
        private static void PagesGroup(UIHudPages g)
        {
            if (g == null) return;
            if (g.pageScroll == null) UIHudBuilder.SetupPageScroll(g); // groups made before page scrolling
            var pages = g.pages ?? new GameObject[0];
            if (g.transform.Find("Tabs") != null)
            {
                bool side = UIHudBuilder.TabsOnSide(g);
                int s = EditorGUILayout.Popup("Tabs", side ? 1 : 0, new[] { "Top (horizontal)", "Left side (vertical)" });
                if ((s == 1) != side) { UIHudBuilder.SetTabsSide(g, s == 1); GUIUtility.ExitGUI(); }
            }
            EditorGUILayout.LabelField("Pages", EditorStyles.miniBoldLabel);
            for (int i = 0; i < pages.Length; i++)
            {
                GameObject page = pages[i];
                if (page == null) continue;
                using (new EditorGUILayout.HorizontalScope())
                {
                    bool start = GUILayout.Toggle(g.startPage == i, new GUIContent("", "Start page"), EditorStyles.radioButton, GUILayout.Width(16));
                    if (start && g.startPage != i) { Undo.RecordObject(g, "Start Page"); g.startPage = i; UIHudBuilder.Sync(g); }
                    string n = EditorGUILayout.DelayedTextField(page.name);
                    if (n != page.name) RenameGroupPage(g, i, n);
                    bool shown = GUILayout.Toggle(page.activeSelf, "Edit", EditorStyles.miniButton, GUILayout.Width(40));
                    if (shown && !page.activeSelf) ShowGroupPage(g, i);
                    if (Mini("✕") && ConfirmDelete("page '" + page.name + "'")) { RemoveGroupPage(g, i); GUIUtility.ExitGUI(); }
                }
                if (page.activeSelf)
                    using (new EditorGUILayout.VerticalScope(Box))
                    {
                        ChildList(page.transform, "Page is empty.");
                        AddWidgetBar(page.transform, PageKinds);
                        UIHudAnimGUI.Pane(page.transform);
                    }
            }
            if (GUILayout.Button("+ Add Page", EditorStyles.miniButton))
            {
                UIHudBuilder.AddGroupPage(g, "Page " + (pages.Length + 1));
                ShowGroupPage(g, g.pages.Length - 1);
                if (UIHudStyle.Current != null) UIHudStyle.Current.Apply(g.transform);
            }
        }

        private static GameObject GroupTab(UIHudPages g, int i) =>
            g.highlights != null && i < g.highlights.Length && g.highlights[i] != null ? g.highlights[i].transform.parent.gameObject : null;

        private static void ShowGroupPage(UIHudPages g, int index)
        {
            for (int i = 0; i < g.pages.Length; i++)
            {
                if (g.pages[i] != null && g.pages[i].activeSelf != (i == index)) { Undo.RecordObject(g.pages[i], "Show Page"); g.pages[i].SetActive(i == index); }
                var hl = g.highlights != null && i < g.highlights.Length ? g.highlights[i] : null;
                if (hl != null && hl.activeSelf != (i == index)) { Undo.RecordObject(hl, "Show Page"); hl.SetActive(i == index); }
            }
            if (g.titleLabel != null && index < g.pages.Length && g.pages[index] != null) { Undo.RecordObject(g.titleLabel, "Show Page"); g.titleLabel.text = g.pages[index].name; }
            UIHudBuilder.SetupPageScroll(g);
        }

        private static void RenameGroupPage(UIHudPages g, int i, string name)
        {
            if (string.IsNullOrEmpty(name)) return;
            Rename(g.pages[i], name);
            GameObject tab = GroupTab(g, i);
            if (tab != null)
            {
                Rename(tab, name);
                var label = tab.GetComponentInChildren<TextMeshProUGUI>(true);
                if (label != null) { Undo.RecordObject(label, "Rename"); label.text = name; }
            }
            if (g.titleLabel != null && g.pages[i].activeSelf) { Undo.RecordObject(g.titleLabel, "Rename"); g.titleLabel.text = name; }
        }

        private static void RemoveGroupPage(UIHudPages g, int index)
        {
            GameObject page = g.pages[index];
            GameObject tab = GroupTab(g, index);
            Undo.RecordObject(g, "Remove Page");
            var pages = new List<GameObject>(g.pages); pages.RemoveAt(index); g.pages = pages.ToArray();
            if (g.highlights != null && index < g.highlights.Length)
            {
                var hls = new List<GameObject>(g.highlights); hls.RemoveAt(index); g.highlights = hls.ToArray();
            }
            if (g.startPage >= g.pages.Length) g.startPage = Mathf.Max(0, g.pages.Length - 1);
            if (page != null) Undo.DestroyObjectImmediate(page);
            if (tab != null) Undo.DestroyObjectImmediate(tab);
            for (int i = 0; i < g.pages.Length; i++)
            {
                GameObject t = GroupTab(g, i);
                var tab2 = t != null ? t.GetComponent<UIHudTab>() : null;
                if (tab2 == null || tab2.pageIndex == i) continue;
                Undo.RecordObject(tab2, "Reindex");
                tab2.pageIndex = i;
                UIHudBuilder.Sync(tab2);
            }
            UIHudBuilder.Sync(g);
            if (g.pages.Length > 0) ShowGroupPage(g, Mathf.Min(index, g.pages.Length - 1));
        }

        private static readonly Vector2[] AnchorPoints =
        {
            new Vector2(0f, 1f), new Vector2(0.5f, 1f), new Vector2(1f, 1f),
            new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(0f, 0f), new Vector2(0.5f, 0f), new Vector2(1f, 0f),
        };

        private static void DrawBehaviour(UdonSharpBehaviour b)
        {
            var so = new SerializedObject(b);
            so.Update();
            SerializedProperty p = so.GetIterator();
            for (bool enter = true; p.NextVisible(enter); enter = false)
            {
                if (p.name == "m_Script" || Wiring.Contains(p.name)) continue;
                if (p.name == "eventName" && b is UIHudWidget w && w.target != null) { EventPopup(w.target, p); continue; }
                EditorGUILayout.PropertyField(p, true);
            }
            if (so.ApplyModifiedProperties()) UIHudBuilder.Sync(b);
        }

        /// <summary>Pick the event from the target's public methods instead of typing it.</summary>
        private static void EventPopup(UdonSharpBehaviour target, SerializedProperty p)
        {
            var names = new List<string> { "(type a name)" };
            for (System.Type t = target.GetType(); t != null && t != typeof(UdonSharpBehaviour); t = t.BaseType)
                foreach (var m in t.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly))
                    if (m.ReturnType == typeof(void) && m.GetParameters().Length == 0 && !m.IsSpecialName && m.GetBaseDefinition() == m && !names.Contains(m.Name))
                        names.Add(m.Name);
            int cur = Mathf.Max(0, names.IndexOf(p.stringValue));
            int ni = EditorGUILayout.Popup("Event Name", cur, names.ToArray());
            if (ni > 0) p.stringValue = names[ni];
            else EditorGUILayout.PropertyField(p, new GUIContent(" "));
        }

        /// <summary>"+ Add widget" button: opens a list with a picture of each widget. New widgets get the current style and open for editing.</summary>
        internal static void AddWidgetBar(Transform parent, params UIHudWidgetKind[] kinds)
        {
            Rect r = GUILayoutUtility.GetRect(new GUIContent("+ Add widget"), EditorStyles.popup);
            if (!EditorGUI.DropdownButton(r, new GUIContent("+ Add widget"), FocusType.Passive, EditorStyles.popup)) return;
            PopupWindow.Show(r, new UIHudAddWidgetPopup(parent, kinds, created =>
            {
                if (created == null) return;
                UIHudBuilder.FitWidth(created.transform);
                if (UIHudStyle.Current != null) UIHudStyle.Current.Apply(created.transform);
                Open.Add(created.transform.GetInstanceID());
            }));
        }

        internal static void AllSettings(SerializedObject so, ref bool open)
        {
            EditorGUILayout.Space(6);
            open = EditorGUILayout.Foldout(open, "All settings", true);
            if (!open) return;
            so.Update();
            SerializedProperty p = so.GetIterator();
            for (bool enter = true; p.NextVisible(enter); enter = false)
                if (p.name != "m_Script") EditorGUILayout.PropertyField(p, true);
            if (so.ApplyModifiedProperties()) UIHudBuilder.Sync(so.targetObject as UdonSharpBehaviour);
        }
    }

    // =========================================================================
    // UIHudManager: menus, HUD items, platform buttons
    // =========================================================================

    [CustomEditor(typeof(UIHudManager))]
    internal class UIHudManagerEditor : UnityEditor.Editor
    {
        private UIHudSide _newSide = UIHudSide.Center;
        private bool _all, _styleOpen = true;
        private UnityEditor.Editor _styleEditor;

        private void OnDisable() { if (_styleEditor != null) DestroyImmediate(_styleEditor); }

        public override void OnInspectorGUI() { Draw(); if (GUI.changed) UIHudPreviewWindow.Changed(); }

        private void Draw()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;
            var m = (UIHudManager)target;

            // ---- Scene view ----
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(new GUIContent("Open Preview window", "See the HUD and menus as in game. Click a widget there to select it."))) UIHudPreviewWindow.Open(m.hudCanvas);
            }

            // ---- Style ----
            UIHudGUI.Section("Style");
            using (new EditorGUILayout.VerticalScope(UIHudGUI.Box))
            {
                var style = UIHudStyle.Current;
                using (new EditorGUILayout.HorizontalScope())
                {
                    var picked = (UIHudStyle)EditorGUILayout.ObjectField(style, typeof(UIHudStyle), false);
                    if (picked != style) { UIHudStyle.Current = picked; if (picked != null) picked.ApplyToScene(); style = picked; }
                    if (GUILayout.Button("New", EditorStyles.miniButton, GUILayout.Width(40)))
                    {
                        style = UIHudStyle.CreateAsset();
                        UIHudStyle.Current = style;
                        style.ApplyToScene();
                    }
                }
                if (style == null) EditorGUILayout.LabelField("Click New to make a style: colors, font, sprites, spacing, presets.", EditorStyles.wordWrappedMiniLabel);
                else
                {
                    _styleOpen = EditorGUILayout.Foldout(_styleOpen, "Edit style (applies live)", true);
                    if (_styleOpen)
                    {
                        CreateCachedEditor(style, typeof(UIHudStyleEditor), ref _styleEditor);
                        _styleEditor.OnInspectorGUI();
                    }
                }
            }

            // ---- Menus ----
            UIHudGUI.Section("Menus");
            using (new EditorGUILayout.VerticalScope(UIHudGUI.Box))
            {
                var menus = m.menus ?? new UIHudMenu[0];
                if (menus.Length == 0) EditorGUILayout.LabelField("No menus yet.", EditorStyles.miniLabel);
                for (int i = 0; i < menus.Length; i++)
                {
                    UIHudMenu menu = menus[i];
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (menu == null)
                        {
                            EditorGUILayout.LabelField("(missing)", EditorStyles.miniLabel);
                            if (UIHudGUI.Mini("✕")) { RemoveMenuAt(m, i, false); GUIUtility.ExitGUI(); }
                            continue;
                        }
                        UIHudGUI.Rename(menu.gameObject, EditorGUILayout.DelayedTextField(menu.name));
                        var side = (UIHudSide)EditorGUILayout.EnumPopup(menu.side, GUILayout.Width(70));
                        if (side != menu.side) { Undo.RecordObject(menu, "Menu Side"); menu.side = side; UIHudBuilder.Sync(menu); }
                        bool main = GUILayout.Toggle(menu.openWithMainButton, new GUIContent("Main", "Opens with the main button (M key / wrist / mobile)"), EditorStyles.miniButton, GUILayout.Width(40));
                        if (main != menu.openWithMainButton) { Undo.RecordObject(menu, "Menu Main"); menu.openWithMainButton = main; UIHudBuilder.Sync(menu); }
                        if (UIHudGUI.Mini("Edit", 38)) UIHudGUI.Ping(menu.gameObject);
                        if (UIHudGUI.Mini("✕") && UIHudGUI.ConfirmDelete("menu '" + menu.name + "'")) { RemoveMenuAt(m, i, true); GUIUtility.ExitGUI(); }
                    }
                }
                using (new EditorGUILayout.HorizontalScope())
                {
                    _newSide = (UIHudSide)EditorGUILayout.EnumPopup(_newSide, GUILayout.Width(80));
                    if (GUILayout.Button("+ Add Menu"))
                    {
                        UIHudMenu added = UIHudBuilder.AddMenu(m, _newSide);
                        if (UIHudStyle.Current != null) UIHudStyle.Current.Apply(added.transform);
                        UIHudGUI.Ping(added.gameObject);
                    }
                }
            }

            // ---- HUD ----
            UIHudGUI.Section("HUD (always visible)");
            using (new EditorGUILayout.VerticalScope(UIHudGUI.Box))
            {
                if (m.hudCanvas == null) EditorGUILayout.HelpBox("No HUD canvas assigned.", MessageType.Info);
                else
                {
                    bool show = EditorGUILayout.Toggle("Show HUD at start", m.startWithHud);
                    if (show != m.startWithHud) { Undo.RecordObject(m, "HUD"); m.startWithHud = show; UIHudBuilder.Sync(m); }
                    UIHudGUI.ChildList(m.hudCanvas, "HUD is empty.", true);
                    UIHudGUI.AddWidgetBar(m.hudCanvas, UIHudGUI.HudKinds);
                    UIHudAnimGUI.Pane(m.hudCanvas);
                }
            }

            // ---- Open buttons ----
            UIHudGUI.Section("Open the menu with");
            using (new EditorGUILayout.VerticalScope(UIHudGUI.Box))
            {
                var key = (KeyCode)EditorGUILayout.EnumPopup("Desktop key", m.desktopKey);
                if (key != m.desktopKey) { Undo.RecordObject(m, "Key"); m.desktopKey = key; UIHudBuilder.Sync(m); }
                PlatformRow("VR wrist", m.wristButton);
                if (m.wristButton != null)
                {
                    int hand = GUILayout.Toolbar(m.wristOnRightHand ? 1 : 0, new[] { "Left hand", "Right hand" });
                    if (hand != (m.wristOnRightHand ? 1 : 0)) { Undo.RecordObject(m, "Wrist Hand"); m.wristOnRightHand = hand == 1; UIHudBuilder.Sync(m); }
                }
                PlatformRow("Mobile button", m.mobileButton);
                if (m.wristButton != null) UIHudGUI.Looks(m.wristButton, null);
                if (m.mobileButton != null) UIHudGUI.Looks(m.mobileButton, null);
            }

            // ---- Hotbar ----
            UIHudGUI.Section("Hotbar");
            using (new EditorGUILayout.VerticalScope(UIHudGUI.Box))
            {
                var bar = m.GetComponentInChildren<UIHudHotbar>(true);
                if (bar == null)
                {
                    EditorGUILayout.LabelField("Row of slots (any widget). VR + desktop: 45° below your eyes, locked to your body (not your head). Mobile: bottom of the screen. Desktop keys 1-9.", EditorStyles.wordWrappedMiniLabel);
                    if (GUILayout.Button("Add hotbar")) { Selection.activeGameObject = UIHudBuilder.CreateHotbar(m, 6).gameObject; GUIUtility.ExitGUI(); }
                }
                else using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(UIHudBuilder.HotbarSlots(bar).childCount + " slots");
                    if (UIHudGUI.Mini("Edit", 38)) UIHudGUI.Ping(bar.gameObject);
                }
            }

            // ---- Runtime widgets ----
            UIHudGUI.Section("Add widgets at runtime (Udon)");
            using (new EditorGUILayout.VerticalScope(UIHudGUI.Box))
            {
                int n = m.widgetTemplates != null ? m.widgetTemplates.Length : 0;
                EditorGUILayout.LabelField(n == 0
                    ? "No templates yet. Make them to use manager.AddWidget(\"Button\", page, \"Hi\") from Udon."
                    : n + " templates: " + string.Join(", ", System.Array.ConvertAll(m.widgetTemplates, t => t != null ? t.name : "?")),
                    EditorStyles.wordWrappedMiniLabel);
                if (GUILayout.Button(n == 0 ? "Make runtime templates" : "Rebuild templates (after a style change)"))
                {
                    UIHudBuilder.MakeTemplates(m);
                    GUIUtility.ExitGUI();
                }
            }

            UIHudGUI.AllSettings(serializedObject, ref _all);
        }

        private static void PlatformRow(string label, Component c)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(label, GUILayout.Width(110));
                if (c == null) { EditorGUILayout.LabelField("off", EditorStyles.miniLabel); return; }
                bool on = EditorGUILayout.Toggle(c.gameObject.activeSelf, GUILayout.Width(16));
                if (on != c.gameObject.activeSelf) { Undo.RecordObject(c.gameObject, "Toggle"); c.gameObject.SetActive(on); }
                EditorGUILayout.LabelField(on ? "" : "(hidden in scene, still enabled per platform at runtime)", EditorStyles.miniLabel);
                if (UIHudGUI.Mini("Edit", 38)) UIHudGUI.Ping(c.gameObject);
            }
        }

        private static void RemoveMenuAt(UIHudManager m, int index, bool destroy)
        {
            var list = new List<UIHudMenu>(m.menus);
            UIHudMenu menu = list[index];
            Undo.RecordObject(m, "Remove Menu");
            list.RemoveAt(index);
            m.menus = list.ToArray();
            UIHudBuilder.Sync(m);
            if (destroy && menu != null) Undo.DestroyObjectImmediate(menu.gameObject);
        }
    }

    // =========================================================================
    // UIHudMenu: side, size, title, pages, widgets
    // =========================================================================

    [CustomEditor(typeof(UIHudMenu))]
    internal class UIHudMenuEditor : UnityEditor.Editor
    {
        private int _page;
        private bool _all, _animOpen = true;
        private double _playStart = -1;
        private float _scrub = 1f;

        public override bool RequiresConstantRepaint() => _playStart >= 0;
        private UIHudStyle _style;
        private static readonly string[] SideNames = { "Left", "Right", "Top", "Bottom", "Center" };

        public override void OnInspectorGUI() { Draw(); if (GUI.changed) UIHudPreviewWindow.Changed(); }

        private void Draw()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;
            var menu = (UIHudMenu)target;
            var rt = (RectTransform)menu.transform;
            if (menu.pageScroll == null && menu.transform.Find("Pages") != null) UIHudBuilder.SetupPageScroll(menu); // menus made before page scrolling

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Open Preview window")) UIHudPreviewWindow.Open(rt);
            }

            // ---- Look ----
            UIHudGUI.Section("Menu");
            using (new EditorGUILayout.VerticalScope(UIHudGUI.Box))
            {
                TextMeshProUGUI title = FindTitle(menu);
                if (title != null)
                {
                    string t = EditorGUILayout.DelayedTextField("Title", title.text);
                    if (t != title.text) { Undo.RecordObject(title, "Title"); title.text = t; }
                }

                int place = GUILayout.Toolbar(menu.worldSpace ? 1 : 0, new[] { "Player view", "World (standalone)" });
                if (place != (menu.worldSpace ? 1 : 0)) SetWorldSpace(menu, place == 1);

                if (menu.worldSpace)
                {
                    bool startOpen = EditorGUILayout.Toggle("Visible at start", menu.startOpen);
                    if (startOpen != menu.startOpen) { Undo.RecordObject(menu, "Start Open"); menu.startOpen = startOpen; UIHudBuilder.Sync(menu); }
                    Vector3 sc = menu.transform.localScale;
                    EditorGUILayout.LabelField("Size in world", (rt.sizeDelta.x * sc.x).ToString("0.00") + " x " + (rt.sizeDelta.y * sc.y).ToString("0.00") + " m", EditorStyles.miniLabel);
                    float mm = EditorGUILayout.Slider("Scale (mm per px)", sc.x * 1000f, 0.1f, 5f);
                    if (!Mathf.Approximately(mm, sc.x * 1000f)) { Undo.RecordObject(menu.transform, "Scale"); menu.transform.localScale = Vector3.one * (mm / 1000f); }
                    if (GUILayout.Button("Move in front of the Scene camera", EditorStyles.miniButton)) UIHudBuilder.PlaceInFrontOfSceneCamera(menu.transform);
                }
                else
                {
                    int side = GUILayout.Toolbar((int)menu.side, SideNames);
                    if (side != (int)menu.side) { Undo.RecordObject(menu, "Menu Side"); menu.side = (UIHudSide)side; UIHudBuilder.Sync(menu); }
                }

                Vector2 size = EditorGUILayout.Vector2Field("Size (px)", rt.sizeDelta);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Small", EditorStyles.miniButtonLeft)) size = new Vector2(560f, 820f);
                    if (GUILayout.Button("Wide bar", EditorStyles.miniButtonMid)) size = new Vector2(1100f, 380f);
                    if (GUILayout.Button("Big", EditorStyles.miniButtonRight)) size = new Vector2(1200f, 820f);
                }
                if (size != rt.sizeDelta) SetSize(rt, size);

                bool main = menu.worldSpace ? menu.openWithMainButton : EditorGUILayout.Toggle(new GUIContent("Open with main button", "M key / wrist / mobile button"), menu.openWithMainButton);
                if (main != menu.openWithMainButton) { Undo.RecordObject(menu, "Menu Main"); menu.openWithMainButton = main; UIHudBuilder.Sync(menu); }
                if (!menu.worldSpace)
                {
                    var key = (KeyCode)EditorGUILayout.EnumPopup(new GUIContent("Own desktop key", "Opens/closes only this menu. None = no own key."), menu.openKey);
                    if (key != menu.openKey) { Undo.RecordObject(menu, "Menu Key"); menu.openKey = key; UIHudBuilder.Sync(menu); }
                    UIHudManager mgr = menu.manager;
                    if (mgr != null && key != KeyCode.None && key == mgr.desktopKey)
                        EditorGUILayout.HelpBox("Same as the main key (" + key + "): use another key.", MessageType.Warning);
                    if (mgr != null && (mgr.wristButton != null || mgr.mobileButton != null))
                    {
                        bool has = UIHudBuilder.OwnButton(mgr.wristButton, menu) != null || UIHudBuilder.OwnButton(mgr.mobileButton, menu) != null;
                        bool want = EditorGUILayout.Toggle(new GUIContent("Own wrist & mobile button", "Adds a button for this menu next to the main one on the VR wrist and the mobile corner."), has);
                        if (want != has)
                        {
                            string label = title != null && title.text.Length > 0 ? title.text.Split(' ')[0] : menu.name;
                            UIHudBuilder.SetOwnButtons(mgr, menu, want, label);
                            GUIUtility.ExitGUI();
                        }
                    }
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (_style == null) _style = UIHudStyle.Current;
                    _style = (UIHudStyle)EditorGUILayout.ObjectField(new GUIContent("Style for this menu", "Give this menu its own look. Re-applying the project style repaints it again."), _style, typeof(UIHudStyle), false);
                    using (new EditorGUI.DisabledScope(_style == null))
                        if (GUILayout.Button("Apply", EditorStyles.miniButton, GUILayout.Width(50))) _style.Apply(menu.transform);
                }
                // Panel, header, tabs, close button; the widgets on the pages have their own.
                var pageSet = new List<Transform>();
                if (menu.pages != null) foreach (var p in menu.pages) if (p != null) pageSet.Add(p.transform);
                UIHudGUI.Looks(menu.transform, pageSet);
            }

            // ---- Animation ----
            UIHudGUI.Section("Animation");
            _animOpen = EditorGUILayout.Foldout(_animOpen, "Open / close / page switch", true);
            if (_animOpen)
                using (new EditorGUILayout.VerticalScope(UIHudGUI.Box))
                    AnimationGUI(menu);

            // ---- Pages ----
            UIHudGUI.Section("Pages (tabs)");
            var pages = menu.pages ?? new GameObject[0];
            using (new EditorGUILayout.VerticalScope(UIHudGUI.Box))
            {
                if (menu.transform.Find("Pages") == null || menu.transform.Find("Tabs") == null)
                {
                    EditorGUILayout.HelpBox("This menu has no Pages/Tabs children (not made by the UIHUD builder).", MessageType.Info);
                }
                else
                {
                    _page = Mathf.Clamp(_page, 0, Mathf.Max(0, pages.Length - 1));
                    if (System.Array.IndexOf(pages, null) >= 0)
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUILayout.HelpBox("A page was deleted in the Hierarchy; its tab is still there.", MessageType.Warning);
                            if (GUILayout.Button("Clean up", GUILayout.Width(70), GUILayout.Height(38)))
                            {
                                for (int i = pages.Length - 1; i >= 0; i--) if (pages[i] == null) RemovePage(menu, i);
                                GUIUtility.ExitGUI();
                            }
                        }
                    for (int i = 0; i < pages.Length; i++)
                    {
                        if (pages[i] == null) continue;
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            bool start = GUILayout.Toggle(menu.startPage == i, new GUIContent("", "Start page"), EditorStyles.radioButton, GUILayout.Width(16));
                            if (start && menu.startPage != i) { Undo.RecordObject(menu, "Start Page"); menu.startPage = i; UIHudBuilder.Sync(menu); }
                            string n = EditorGUILayout.DelayedTextField(pages[i].name);
                            if (n != pages[i].name) RenamePage(menu, i, n);
                            using (new EditorGUI.DisabledScope(i == 0)) if (UIHudGUI.Mini("▲")) { MovePage(menu, i, i - 1); _page = i - 1; }
                            using (new EditorGUI.DisabledScope(i == pages.Length - 1)) if (UIHudGUI.Mini("▼")) { MovePage(menu, i, i + 1); _page = i + 1; }
                            bool sel = GUILayout.Toggle(_page == i, "Widgets", EditorStyles.miniButton, GUILayout.Width(60));
                            if (sel && _page != i) { _page = i; PreviewPage(menu, i); }
                            if (UIHudGUI.Mini("✕") && UIHudGUI.ConfirmDelete("page '" + pages[i].name + "' and its tab")) { RemovePage(menu, i); GUIUtility.ExitGUI(); }
                        }
                    }
                    if (GUILayout.Button("+ Add Page"))
                    {
                        RectTransform p = UIHudBuilder.AddPage(menu, "Page " + (pages.Length + 1));
                        _page = menu.pages.Length - 1;
                        PreviewPage(menu, _page);
                        if (p != null && UIHudStyle.Current != null) UIHudStyle.Current.Apply(menu.transform);
                    }
                }
            }

            // ---- Widgets of the selected page ----
            pages = menu.pages ?? new GameObject[0];
            if (pages.Length > 0 && _page < pages.Length && pages[_page] != null)
            {
                UIHudGUI.Section("Widgets on '" + pages[_page].name + "'");
                using (new EditorGUILayout.VerticalScope(UIHudGUI.Box))
                {
                    UIHudGUI.ChildList(pages[_page].transform, "Page is empty.");
                    UIHudGUI.AddWidgetBar(pages[_page].transform, UIHudGUI.PageKinds);
                    UIHudAnimGUI.Pane(pages[_page].transform);
                }
            }

            UIHudGUI.AllSettings(serializedObject, ref _all);
        }

        private static readonly string[] AnimPresets = { "Slide", "Pop", "Fade", "Drop", "Instant" };

        private void AnimationGUI(UIHudMenu menu)
        {
            // Presets
            int preset = GUILayout.Toolbar(-1, AnimPresets, EditorStyles.miniButton);
            if (preset >= 0)
            {
                Undo.RecordObject(menu, "Animation Preset");
                switch (preset)
                {
                    case 0: Set(menu, true, true, false, 1f, UIHudEase.EaseOut, 0.22f, 0.15f); break;
                    case 1: Set(menu, false, true, true, 0.6f, UIHudEase.Back, 0.3f, 0.15f); break;
                    case 2: Set(menu, false, true, false, 1f, UIHudEase.Smooth, 0.2f, 0.15f); break;
                    case 3: Set(menu, true, true, false, 1f, UIHudEase.Bounce, 0.6f, 0.2f); break;
                    default: Set(menu, false, false, false, 1f, UIHudEase.Linear, 0f, 0f); break;
                }
                UIHudBuilder.Sync(menu);
                Play();
            }

            EditorGUI.BeginChangeCheck();
            bool slide, fade, zoom;
            using (new EditorGUILayout.HorizontalScope())
            {
                slide = EditorGUILayout.ToggleLeft(menu.side == UIHudSide.Center ? "Slide (n/a center)" : "Slide", menu.animSlide, GUILayout.Width(120));
                fade = EditorGUILayout.ToggleLeft("Fade", menu.animFade, GUILayout.Width(60));
                zoom = EditorGUILayout.ToggleLeft("Zoom", menu.animZoom, GUILayout.Width(60));
            }
            float from = menu.scaleFrom;
            if (zoom) from = EditorGUILayout.Slider("Zoom from", menu.scaleFrom, 0f, 1.5f);
            var ease = (UIHudEase)EditorGUILayout.EnumPopup("Easing", menu.ease);
            float open = EditorGUILayout.Slider("Open (s)", menu.animSeconds, 0f, 1.5f);
            float close = EditorGUILayout.Slider("Close (s)", menu.closeSeconds, 0f, 1.5f);
            float pageFade = EditorGUILayout.Slider("Page switch (s)", menu.pageFadeSeconds, 0f, 1f);
            float pageSlide = EditorGUILayout.Slider("Page slide (px)", menu.pageSlide, 0f, 200f);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(menu, "Animation");
                Set(menu, slide, fade, zoom, from, ease, open, close);
                menu.pageFadeSeconds = pageFade;
                menu.pageSlide = pageSlide;
                UIHudBuilder.Sync(menu);
                Play();
            }

            // Pages made before page fades existed have no CanvasGroup.
            if (menu.pageFadeSeconds > 0f && menu.pages != null && System.Array.Exists(menu.pages, pg => pg != null && pg.GetComponent<CanvasGroup>() == null)
                && GUILayout.Button("Enable fade on old pages", EditorStyles.miniButton))
                foreach (var pg in menu.pages) if (pg != null && pg.GetComponent<CanvasGroup>() == null) Undo.AddComponent<CanvasGroup>(pg);

            // Curve + animated mock view
            float t = AnimTime(menu);
            Rect row = GUILayoutUtility.GetRect(10f, 10000f, 200f, 200f);
            var curve = new Rect(row.x, row.y, 160f, row.height);
            var view = new Rect(curve.xMax + 8f, row.y, row.width - curve.width - 8f, row.height);
            DrawCurve(curve, menu.ease, t);
            DrawAnimView(menu, view, t);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(_playStart >= 0 ? "■ Stop" : "▶ Play", EditorStyles.miniButton, GUILayout.Width(60)))
                    if (_playStart >= 0) _playStart = -1; else Play();
                using (new EditorGUI.DisabledScope(_playStart >= 0))
                    _scrub = EditorGUILayout.Slider(_scrub, 0f, 1f);
            }
        }

        private static void Set(UIHudMenu m, bool slide, bool fade, bool zoom, float from, UIHudEase ease, float open, float close)
        {
            m.animSlide = slide; m.animFade = fade; m.animZoom = zoom; m.scaleFrom = from;
            m.ease = ease; m.animSeconds = open; m.closeSeconds = close;
        }

        private void Play() { _playStart = EditorApplication.timeSinceStartup; }

        /// <summary>Animation state 0..1 (open then hold then close), or the scrub value.</summary>
        private float AnimTime(UIHudMenu m)
        {
            if (_playStart < 0) return _scrub;
            float open = Mathf.Max(0.0001f, m.animSeconds), close = Mathf.Max(0.0001f, m.closeSeconds);
            float e = (float)(EditorApplication.timeSinceStartup - _playStart) - 0.25f;
            if (e < 0f) return 0f;
            if (e < open) return e / open;
            e -= open;
            if (e < 0.7f) return 1f;
            e -= 0.7f;
            if (e < close) return 1f - e / close;
            if (e > close + 0.3f) { _playStart = -1; _scrub = 1f; }
            return 0f;
        }

        private static void DrawCurve(Rect r, UIHudEase ease, float t)
        {
            EditorGUI.DrawRect(r, new Color(0.13f, 0.13f, 0.15f));
            if (Event.current.type != EventType.Repaint) return;
            System.Func<float, float, Vector3> pt = (x, y) => new Vector3(r.x + 6f + x * (r.width - 12f), r.yMax - 14f - (y + 0.0f) / 1.25f * (r.height - 28f));
            Handles.color = new Color(1f, 1f, 1f, 0.15f);
            Handles.DrawLine(pt(0f, 0f), pt(1f, 0f));
            Handles.DrawLine(pt(0f, 1f), pt(1f, 1f));
            var pts = new Vector3[41];
            for (int i = 0; i <= 40; i++) { float x = i / 40f; pts[i] = pt(x, UIHudMenu.EaseValue(ease, x)); }
            Handles.color = new Color(0.35f, 0.7f, 1f);
            Handles.DrawAAPolyLine(2.5f, pts);
            Vector3 dot = pt(t, UIHudMenu.EaseValue(ease, t));
            EditorGUI.DrawRect(new Rect(dot.x - 3f, dot.y - 3f, 6f, 6f), Color.white);
        }

        /// <summary>The menu moving inside a 16:9 "view", the same math as UIHudMenu.Apply.</summary>
        private static void DrawAnimView(UIHudMenu m, Rect frame, float t)
        {
            EditorGUI.DrawRect(frame, new Color(0.08f, 0.09f, 0.1f));
            if (Event.current.type != EventType.Repaint) return;
            float viewH = m.manager != null ? m.manager.canvasHeight : 1080f, viewW = viewH * 16f / 9f;
            float k = Mathf.Min(frame.width / viewW, frame.height / viewH);
            var inner = new Rect(frame.center.x - viewW * k * 0.5f, frame.center.y - viewH * k * 0.5f, viewW * k, viewH * k);
            EditorGUI.DrawRect(inner, new Color(0.22f, 0.26f, 0.3f));

            var rt = (RectTransform)m.transform;
            Vector2 size = rt.sizeDelta;
            float halfW = viewW * 0.5f, halfH = viewH * 0.5f, mg = m.margin;
            Vector2 shown, hidden;
            switch (m.worldSpace ? UIHudSide.Center : m.side)
            {
                case UIHudSide.Left: shown = new Vector2(-halfW + size.x * 0.5f + mg, 0f); hidden = shown - new Vector2(size.x + mg, 0f); break;
                case UIHudSide.Right: shown = new Vector2(halfW - size.x * 0.5f - mg, 0f); hidden = shown + new Vector2(size.x + mg, 0f); break;
                case UIHudSide.Top: shown = new Vector2(0f, halfH - size.y * 0.5f - mg); hidden = shown + new Vector2(0f, size.y + mg); break;
                case UIHudSide.Bottom: shown = new Vector2(0f, -halfH + size.y * 0.5f + mg); hidden = shown - new Vector2(0f, size.y + mg); break;
                default: shown = hidden = Vector2.zero; break;
            }
            float e = UIHudMenu.EaseValue(m.ease, t);
            Vector2 p = m.animSlide ? Vector2.LerpUnclamped(hidden, shown, e) : shown;
            float s = m.animZoom ? Mathf.Max(0.01f, Mathf.LerpUnclamped(m.scaleFrom, 1f, e)) : 1f;
            float a = m.animFade ? Mathf.Clamp01(e) : (t > 0f ? 1f : 0f);
            if (a <= 0f) return;

            Vector2 c = inner.center + new Vector2(p.x * k, -p.y * k);
            Vector2 sz = size * k * s;
            int texW = Mathf.Clamp(Mathf.RoundToInt(size.x * k * 2f), 32, 512);
            var tex = UIHudPreview.RenderToTexture(rt, texW, Mathf.Max(16, Mathf.RoundToInt(texW * size.y / size.x)));
            GUI.BeginClip(inner);
            var old = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, a);
            GUI.DrawTexture(new Rect(c.x - sz.x * 0.5f - inner.x, c.y - sz.y * 0.5f - inner.y, sz.x, sz.y), tex);
            GUI.color = old;
            GUI.EndClip();
        }

        /// <summary>Switch between a player-view menu and a standalone world panel.</summary>
        private static void SetWorldSpace(UIHudMenu menu, bool world)
        {
            Undo.RecordObject(menu, "Placement");
            menu.worldSpace = world;
            if (world) { menu.openWithMainButton = false; menu.animSlide = false; }
            UIHudBuilder.Sync(menu);
            var group = menu.GetComponent<CanvasGroup>();
            if (group != null) { Undo.RecordObject(group, "Placement"); group.alpha = world ? 1f : 0f; }
            var shape = menu.GetComponent<VRC.SDK3.Components.VRCUiShape>();
            if (shape != null) { Undo.RecordObject(shape, "Placement"); shape.AllowFocusView = world; }
            Undo.RecordObject(menu.transform, "Placement");
            menu.transform.localScale = Vector3.one * (world ? 0.001f : 0.0005f);
            if (world) UIHudBuilder.PlaceInFrontOfSceneCamera(menu.transform);
            else menu.transform.localPosition = Vector3.zero;
        }

        private static TextMeshProUGUI FindTitle(UIHudMenu menu)
        {
            Transform header = menu.transform.Find("Header");
            return header != null ? header.GetComponentInChildren<TextMeshProUGUI>(true) : null;
        }

        private static void SetSize(RectTransform rt, Vector2 size)
        {
            size = Vector2.Max(size, new Vector2(100f, 100f));
            Undo.RecordObject(rt, "Menu Size");
            rt.sizeDelta = size;
            var col = rt.GetComponent<BoxCollider>();
            if (col != null) { Undo.RecordObject(col, "Menu Size"); col.size = new Vector3(size.x, size.y, 0f); }
        }

        /// <summary>The tab button of page i (the highlight's parent).</summary>
        private static GameObject TabOf(UIHudMenu menu, int i)
        {
            var hl = menu.pageHighlights;
            return hl != null && i < hl.Length && hl[i] != null ? hl[i].transform.parent.gameObject : null;
        }

        /// <summary>Show only page i in the editor so you can see what you edit.</summary>
        private static void PreviewPage(UIHudMenu menu, int index)
        {
            for (int i = 0; i < menu.pages.Length; i++)
            {
                if (menu.pages[i] != null && menu.pages[i].activeSelf != (i == index)) { Undo.RecordObject(menu.pages[i], "Preview"); menu.pages[i].SetActive(i == index); }
                var hl = menu.pageHighlights != null && i < menu.pageHighlights.Length ? menu.pageHighlights[i] : null;
                if (hl != null && hl.activeSelf != (i == index)) { Undo.RecordObject(hl, "Preview"); hl.SetActive(i == index); }
            }
            UIHudBuilder.SetupPageScroll(menu); // scroll the shown page
        }

        private static void RenamePage(UIHudMenu menu, int i, string name)
        {
            if (string.IsNullOrEmpty(name)) return;
            UIHudGUI.Rename(menu.pages[i], name);
            GameObject tab = TabOf(menu, i);
            if (tab == null) return;
            UIHudGUI.Rename(tab, name);
            var label = tab.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null) { Undo.RecordObject(label, "Rename"); label.text = name; }
        }

        private static void MovePage(UIHudMenu menu, int from, int to)
        {
            Undo.RecordObject(menu, "Move Page");
            GameObject tabA = TabOf(menu, from), tabB = TabOf(menu, to);
            Swap(menu.pages, from, to);
            if (menu.pageHighlights != null && to < menu.pageHighlights.Length) Swap(menu.pageHighlights, from, to);
            if (menu.startPage == from) menu.startPage = to; else if (menu.startPage == to) menu.startPage = from;
            SwapSiblings(menu.pages[to].transform, menu.pages[from].transform);
            if (tabA != null && tabB != null) SwapSiblings(tabA.transform, tabB.transform);
            Reindex(menu);
        }

        private static void RemovePage(UIHudMenu menu, int index)
        {
            GameObject page = menu.pages[index];
            GameObject tab = TabOf(menu, index);
            Undo.RecordObject(menu, "Remove Page");
            var pages = new List<GameObject>(menu.pages);
            pages.RemoveAt(index);
            menu.pages = pages.ToArray();
            if (menu.pageHighlights != null && index < menu.pageHighlights.Length)
            {
                var hls = new List<GameObject>(menu.pageHighlights);
                hls.RemoveAt(index);
                menu.pageHighlights = hls.ToArray();
            }
            if (menu.startPage >= menu.pages.Length) menu.startPage = Mathf.Max(0, menu.pages.Length - 1);
            if (page != null) Undo.DestroyObjectImmediate(page);
            if (tab != null) Undo.DestroyObjectImmediate(tab);
            Reindex(menu);
        }

        /// <summary>Tab i must open page i: rewrite every tab's pageIndex after a change.</summary>
        private static void Reindex(UIHudMenu menu)
        {
            for (int i = 0; i < menu.pages.Length; i++)
            {
                GameObject tabGo = TabOf(menu, i);
                var tab = tabGo != null ? tabGo.GetComponent<UIHudTab>() : null;
                if (tab == null || tab.pageIndex == i) continue;
                Undo.RecordObject(tab, "Reindex");
                tab.pageIndex = i;
                UIHudBuilder.Sync(tab);
            }
            UIHudBuilder.Sync(menu);
        }

        private static void Swap(GameObject[] a, int i, int j) { var t = a[i]; a[i] = a[j]; a[j] = t; }

        private static void SwapSiblings(Transform a, Transform b)
        {
            int ia = a.GetSiblingIndex(), ib = b.GetSiblingIndex();
            Undo.SetTransformParent(a, a.parent, "Move");
            Undo.SetTransformParent(b, b.parent, "Move");
            a.SetSiblingIndex(ib);
            b.SetSiblingIndex(ia);
        }
    }

    // =========================================================================
    // Each widget's own Inspector: the same quick edit as in the menu / HUD lists
    // =========================================================================

    internal abstract class UIHudWidgetEditorBase : UnityEditor.Editor
    {
        private bool _all;

        public override void OnInspectorGUI() { Draw(); if (GUI.changed) UIHudPreviewWindow.Changed(); }

        private void Draw()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;
            var b = (UdonSharpBehaviour)target;
            if (b is UIHudAnim && UIHudGUI.WidgetScript(b.gameObject, false) != null)
            {
                EditorGUILayout.LabelField("Edited in the widget's Inspector above (Animation).", EditorStyles.miniLabel);
                return;
            }
            Transform root = b.transform;
            if (b is UIHudSlider && root.parent != null && root.parent.Find("Header") != null) root = root.parent; // slider row
            var canvas = root.GetComponentInParent<Canvas>(true);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(UIHudGUI.Kind(root.gameObject) + " widget", EditorStyles.boldLabel);
                if (root != b.transform && GUILayout.Button("Select widget", EditorStyles.miniButtonLeft)) UIHudGUI.Ping(root.gameObject);
                if (canvas != null && GUILayout.Button("Open Preview window", root != b.transform ? EditorStyles.miniButtonRight : EditorStyles.miniButton))
                    UIHudPreviewWindow.Open((RectTransform)canvas.rootCanvas.transform);
            }
            // Placed by anchors when its parent has no layout (HUD); otherwise by the list (or Absolute position).
            bool free = root.parent != null && root.parent.GetComponent<LayoutGroup>() == null && root.parent.GetComponent<Canvas>() != null;
            using (new EditorGUILayout.VerticalScope(UIHudGUI.Box))
                UIHudGUI.WidgetDetails(root.gameObject, free);
            UIHudGUI.AllSettings(serializedObject, ref _all);
        }
    }

    [CustomEditor(typeof(UIHudHotbar))]
    internal class UIHudHotbarEditor : UnityEditor.Editor
    {
        private bool _all;

        public override void OnInspectorGUI() { Draw(); if (GUI.changed) UIHudPreviewWindow.Changed(); }

        private void Draw()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;
            var bar = (UIHudHotbar)target;
            UIHudGUI.Section("Slots");
            using (new EditorGUILayout.VerticalScope(UIHudGUI.Box))
            {
                if (bar.content == null)
                {
                    EditorGUILayout.HelpBox("Old hotbar: the VRChat camera can see it. Move the slots into a nested canvas so it can hide from the camera.", MessageType.Info);
                    if (GUILayout.Button("Hide from VRChat camera")) { UIHudBuilder.MoveHotbarIntoContent(bar); GUIUtility.ExitGUI(); }
                }
                EditorGUILayout.LabelField("Each child of the Slots canvas is a slot: any widget. Pressing a button slot selects it "
                    + "(and still does its own actions). Selecting sends the event below with the slot index (int, -1 = none). Desktop keys 1-9 press slots 1-9.",
                    EditorStyles.wordWrappedMiniLabel);
                Transform t = UIHudBuilder.HotbarSlots(bar);
                for (int i = 0; i < t.childCount; i++)
                {
                    GameObject slot = t.GetChild(i).gameObject;
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField((i + 1) + ".  " + slot.name + "  (" + UIHudGUI.Kind(slot) + ")" + (i == bar.selectedIndex ? "  selected at start" : ""));
                        using (new EditorGUI.DisabledScope(i == 0))
                            if (UIHudGUI.Mini("▲", 22)) { Undo.SetTransformParent(slot.transform, t, "Move Slot"); slot.transform.SetSiblingIndex(i - 1); GUIUtility.ExitGUI(); }
                        if (UIHudGUI.Mini("Edit", 38)) UIHudGUI.Ping(slot);
                        if (UIHudGUI.Mini("X", 22)) { Undo.DestroyObjectImmediate(slot); UIHudBuilder.FitHotbar(bar); GUIUtility.ExitGUI(); }
                    }
                }
                if (GUILayout.Button("Add slot ▾"))
                {
                    var menu = new GenericMenu();
                    foreach (var k in UIHudBuilder.HotbarKinds)
                    {
                        var kind = k;
                        menu.AddItem(new GUIContent(UIHudGUI.KindLabel(kind)), false, () => Selection.activeGameObject = UIHudBuilder.AddHotbarSlot(bar, kind));
                    }
                    menu.ShowAsContext();
                }
            }

            UIHudGUI.Section("Selected slot");
            using (new EditorGUILayout.VerticalScope(UIHudGUI.Box))
            {
                serializedObject.Update();
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(UIHudHotbar.selectedIndex)), new GUIContent("Start index"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(UIHudHotbar.selectedColor)));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(UIHudHotbar.allowDeselect)));
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(UIHudHotbar.gap)));
                bool refit = EditorGUI.EndChangeCheck();
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(UIHudWidget.target)), new GUIContent("Event target"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(UIHudWidget.eventName)));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(UIHudWidget.valueVariable)), new GUIContent("Index variable"));
                serializedObject.ApplyModifiedProperties();
                if (refit) UIHudBuilder.FitHotbar(bar);
            }
            UIHudGUI.AllSettings(serializedObject, ref _all);
        }
    }

    // One attribute per class: UdonSharp reads it as a single (inherited) attribute.
    [CustomEditor(typeof(UIHudWidget), true)] internal class UIHudWidgetEditor : UIHudWidgetEditorBase { }
    [CustomEditor(typeof(UIHudBar))] internal class UIHudBarEditor : UIHudWidgetEditorBase { }
    [CustomEditor(typeof(UIHudInfo))] internal class UIHudInfoEditor : UIHudWidgetEditorBase { }
    [CustomEditor(typeof(UIHudNotification))] internal class UIHudNotificationEditor : UIHudWidgetEditorBase { }
    [CustomEditor(typeof(UIHudPlayerList))] internal class UIHudPlayerListEditor : UIHudWidgetEditorBase { }
    [CustomEditor(typeof(UIHudValue))] internal class UIHudValueEditor : UIHudWidgetEditorBase { }
    [CustomEditor(typeof(UIHudTable))] internal class UIHudTableEditor : UIHudWidgetEditorBase { }
    [CustomEditor(typeof(UIHudGraph))] internal class UIHudGraphEditor : UIHudWidgetEditorBase { }
    [CustomEditor(typeof(UIHudStat))] internal class UIHudStatEditor : UIHudWidgetEditorBase { }
    [CustomEditor(typeof(UIHudPie))] internal class UIHudPieEditor : UIHudWidgetEditorBase { }
    [CustomEditor(typeof(UIHudBarList))] internal class UIHudBarListEditor : UIHudWidgetEditorBase { }
    [CustomEditor(typeof(UIHudGauge))] internal class UIHudGaugeEditor : UIHudWidgetEditorBase { }
    [CustomEditor(typeof(UIHudAnim))] internal class UIHudAnimEditor : UIHudWidgetEditorBase { }
}
