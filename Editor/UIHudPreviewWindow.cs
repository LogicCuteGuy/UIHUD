using System.Collections.Generic;
using UdonSharp;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LogicCuteGuy.UIHUD.Editor
{
    /// <summary>
    /// Window > UIHUD Preview: shows the HUD, a menu or a world panel as it looks in game (menus are
    /// hidden in the scene until opened). Follows the selection; click a widget to select it.
    /// </summary>
    internal class UIHudPreviewWindow : EditorWindow
    {
        private static readonly Color Background = new Color(0.16f, 0.17f, 0.2f, 1f);
        private static readonly Color Outline = new Color(1f, 0.8f, 0.2f, 1f);
        private RectTransform _canvas;
        private RectTransform _drag; // widget being moved with the mouse
        [SerializeField] private bool _test; // clicks use the UI instead of selecting (Play Mode)
        private Selectable _press;           // control pressed in Test mode
        private TMPro.TMP_InputField _typing;
        private string _typed = "";
        private readonly Vector3[] _corners = new Vector3[4];

        [MenuItem("Window/UIHUD Preview")]
        private static void OpenMenu() => Open(null);

        internal static void Open(RectTransform canvas)
        {
            var w = GetWindow<UIHudPreviewWindow>("UIHUD Preview");
            if (canvas != null) w._canvas = canvas;
            w.Repaint();
        }

        /// <summary>Inspector edits set fields directly: redraw the preview and the Scene view now.</summary>
        internal static void Changed()
        {
            foreach (var w in Resources.FindObjectsOfTypeAll<UIHudPreviewWindow>()) w.Repaint();
            SceneView.RepaintAll();
        }

        private void OnInspectorUpdate() => Repaint(); // ~10 fps, catches edits made anywhere

        private void Update() { if (EditorApplication.isPlaying) Repaint(); } // smooth tweens in game

        private void OnSelectionChange()
        {
            RectTransform owner = Owner(Selection.activeTransform);
            if (owner != null) { _canvas = owner; Repaint(); }
        }

        /// <summary>Every UIHUD canvas in the open scenes: HUD, menus, open buttons, world panels.</summary>
        private static List<RectTransform> Canvases()
        {
            var list = new List<RectTransform>();
            foreach (var m in FindObjectsOfType<UIHudManager>())
                foreach (Transform c in m.transform)
                    if (c is RectTransform rt && c.GetComponent<Canvas>() != null) list.Add(rt);
            foreach (var w in FindObjectsOfType<UIHudMenu>())
                if (w.worldSpace && w.GetComponentInParent<UIHudManager>() == null) list.Add((RectTransform)w.transform);
            return list;
        }

        private static RectTransform Owner(Transform t)
        {
            if (t == null) return null;
            foreach (var c in Canvases()) if (t.IsChildOf(c)) return c;
            return null;
        }

        private void OnGUI()
        {
            List<RectTransform> list = Canvases();
            if (list.Count == 0)
            {
                EditorGUILayout.HelpBox("No UIHUD in the open scene. Make one with GameObject > UIHUD.", MessageType.Info);
                return;
            }
            if (_canvas == null || !list.Contains(_canvas)) _canvas = list[0];

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                var names = new string[list.Count];
                for (int i = 0; i < list.Count; i++) names[i] = list[i].name;
                int ni = EditorGUILayout.Popup(list.IndexOf(_canvas), names, EditorStyles.toolbarPopup, GUILayout.Width(200));
                _canvas = list[ni];
                _test = GUILayout.Toggle(_test, "Test", EditorStyles.toolbarButton, GUILayout.Width(44));
                GUILayout.FlexibleSpace();
                GUILayout.Label(_test ? "Click / drag / scroll the UI like in game" : "Click: select   Drag: move (absolute / HUD widgets)   Alt+Click: exact part", EditorStyles.miniLabel);
                if (GUILayout.Button("Select", EditorStyles.toolbarButton)) Selection.activeGameObject = _canvas.gameObject;
            }
            if (_test && !EditorApplication.isPlaying)
            {
                // Buttons call Udon, and Udon only runs in game.
                using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    GUILayout.Label("Test mode needs Play Mode (Udon only runs in game).", EditorStyles.wordWrappedMiniLabel);
                    if (GUILayout.Button("▶ Play", GUILayout.Width(60))) EditorApplication.isPlaying = true;
                }
            }
            if (_typing != null)
            {
                using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    GUILayout.Label(_typing.name, GUILayout.ExpandWidth(false));
                    GUI.SetNextControlName("uihud.typing");
                    _typed = EditorGUILayout.TextField(_typed);
                    EditorGUI.FocusTextInControl("uihud.typing");
                    bool enter = Event.current.type == EventType.KeyDown && (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter);
                    if (GUILayout.Button("Send", GUILayout.Width(50)) || enter)
                    {
                        if (_typing != null) { _typing.text = _typed; _typing.onEndEdit.Invoke(_typed); _typing.onSubmit.Invoke(_typed); }
                        _typing = null;
                        GUIUtility.ExitGUI();
                    }
                    if (GUILayout.Button("✕", GUILayout.Width(22))) { _typing = null; GUIUtility.ExitGUI(); }
                }
            }

            Rect area = GUILayoutUtility.GetRect(10f, 10f, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            EditorGUI.DrawRect(area, Background);
            Rect cr = _canvas.rect;
            if (cr.width < 1f || cr.height < 1f || !_canvas.gameObject.activeInHierarchy) return;
            area = new Rect(area.x + 8f, area.y + 8f, area.width - 16f, area.height - 16f);
            float k = Mathf.Min(area.width / cr.width, area.height / cr.height);
            var img = new Rect(0f, 0f, cr.width * k, cr.height * k) { center = area.center };

            Event e = Event.current;
            if (e.type == EventType.Repaint)
            {
                float ppp = EditorGUIUtility.pixelsPerPoint;
                int w = Mathf.Clamp(Mathf.RoundToInt(img.width * ppp), 16, 2048);
                int h = Mathf.Clamp(Mathf.RoundToInt(img.height * ppp), 16, 2048);
                GUI.DrawTexture(img, UIHudPreview.RenderToTexture(_canvas, w, h), ScaleMode.StretchToFill, false);
                if (!_test) DrawSelection(img);
            }
            else if (_test)
            {
                if (EditorApplication.isPlaying) TestInput(e, img);
            }
            else if (e.type == EventType.MouseDown && e.button == 0 && img.Contains(e.mousePosition))
            {
                RectTransform hit = Pick(img, e.mousePosition, e.alt);
                // Drag the nearest movable widget (absolute, or on the HUD); the rest only selects.
                _drag = null;
                for (Transform t = hit; t != null && t != _canvas; t = t.parent)
                    if (UIHudGUI.Draggable((RectTransform)t)) { _drag = (RectTransform)t; break; }
                if (_drag != null) { hit = _drag; Undo.IncrementCurrentGroup(); }
                if (hit != null) Selection.activeGameObject = hit.gameObject;
                e.Use();
            }
            else if (e.type == EventType.MouseDrag && _drag != null)
            {
                // GUI points -> canvas px (y flipped) -> the widget's parent space.
                Vector2 d = e.delta / k;
                float s = _canvas.lossyScale.x / Mathf.Max(1e-6f, _drag.parent.lossyScale.x);
                Undo.RecordObject(_drag, "Move Widget");
                _drag.anchoredPosition += new Vector2(d.x, -d.y) * s;
                e.Use();
                Repaint();
            }
            else if (e.type == EventType.MouseUp) _drag = null;
        }

        /// <summary>Test mode: sends the mouse to the control under it, as the game's pointer would.</summary>
        private void TestInput(Event e, Rect img)
        {
            if (e.type == EventType.MouseDown && e.button == 0 && img.Contains(e.mousePosition))
            {
                _press = Hit<Selectable>(World(img, e.mousePosition));
                if (_press is Slider || _press is Scrollbar) SetValue(_press, World(img, e.mousePosition));
                e.Use();
            }
            else if (e.type == EventType.MouseDrag && (_press is Slider || _press is Scrollbar))
            {
                SetValue(_press, World(img, e.mousePosition));
                e.Use();
            }
            else if (e.type == EventType.MouseUp && _press != null)
            {
                Selectable up = img.Contains(e.mousePosition) ? Hit<Selectable>(World(img, e.mousePosition)) : null;
                if (up == _press)
                {
                    if (up is TMPro.TMP_InputField field) { _typing = field; _typed = field.text; }
                    else if (up is TMPro.TMP_Dropdown dd && dd.options.Count > 0) dd.value = (dd.value + 1) % dd.options.Count;
                    else if (!(up is Slider) && !(up is Scrollbar))
                        ExecuteEvents.Execute(up.gameObject, new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left }, ExecuteEvents.pointerClickHandler);
                }
                _press = null;
                e.Use();
            }
            else if (e.type == EventType.ScrollWheel && img.Contains(e.mousePosition))
            {
                ScrollRect sr = Hit<ScrollRect>(World(img, e.mousePosition));
                if (sr != null)
                    ExecuteEvents.Execute(sr.gameObject, new PointerEventData(EventSystem.current) { scrollDelta = new Vector2(e.delta.x, -e.delta.y) * 10f }, ExecuteEvents.scrollHandler);
                e.Use();
            }
        }

        /// <summary>Slider/scrollbar value from where the mouse is along its track.</summary>
        private static void SetValue(Selectable s, Vector3 world)
        {
            if (s is Slider sl)
            {
                var track = (RectTransform)(sl.handleRect != null ? sl.handleRect.parent : sl.fillRect != null ? sl.fillRect.parent : sl.transform);
                bool vertical = sl.direction == Slider.Direction.BottomToTop || sl.direction == Slider.Direction.TopToBottom;
                bool reverse = sl.direction == Slider.Direction.RightToLeft || sl.direction == Slider.Direction.TopToBottom;
                sl.normalizedValue = Along(track, world, vertical, reverse);
            }
            else if (s is Scrollbar sb)
            {
                bool vertical = sb.direction == Scrollbar.Direction.BottomToTop || sb.direction == Scrollbar.Direction.TopToBottom;
                bool reverse = sb.direction == Scrollbar.Direction.RightToLeft || sb.direction == Scrollbar.Direction.TopToBottom;
                sb.value = Along((RectTransform)sb.transform, world, vertical, reverse);
            }
        }

        private static float Along(RectTransform r, Vector3 world, bool vertical, bool reverse)
        {
            Vector3 l = r.InverseTransformPoint(world);
            float t = vertical ? Mathf.InverseLerp(r.rect.yMin, r.rect.yMax, l.y) : Mathf.InverseLerp(r.rect.xMin, r.rect.xMax, l.x);
            return reverse ? 1f - t : t;
        }

        /// <summary>Topmost active T at the point (later in the hierarchy draws on top), outside any mask clip.</summary>
        private T Hit<T>(Vector3 world) where T : Behaviour
        {
            T top = null;
            foreach (var c in _canvas.GetComponentsInChildren<T>())
            {
                if (!c.isActiveAndEnabled || !Inside((RectTransform)c.transform, world)) continue;
                if (c is Selectable s && !s.IsInteractable()) continue;
                bool clipped = false;
                for (Transform t = c.transform.parent; t != null && t != _canvas && !clipped; t = t.parent)
                    if ((t.GetComponent<RectMask2D>() != null || t.GetComponent<Mask>() != null) && !Inside((RectTransform)t, world)) clipped = true;
                if (!clipped) top = c;
            }
            return top;
        }

        private static bool Inside(RectTransform r, Vector3 world) => r.rect.Contains(r.InverseTransformPoint(world));

        private Vector3 World(Rect img, Vector2 mouse)
        {
            Rect cr = _canvas.rect;
            float u = (mouse.x - img.x) / img.width, v = 1f - (mouse.y - img.y) / img.height;
            return _canvas.TransformPoint(new Vector3(cr.x + u * cr.width, cr.y + v * cr.height, 0f));
        }

        /// <summary>Canvas point under the mouse, then the deepest rect there (or its widget).</summary>
        private RectTransform Pick(Rect img, Vector2 mouse, bool exact)
        {
            Vector3 world = World(img, mouse);
            RectTransform best = null;
            int bestDepth = -1;
            foreach (var r in _canvas.GetComponentsInChildren<RectTransform>())
            {
                if (!r.rect.Contains(r.InverseTransformPoint(world))) continue;
                int d = 0;
                for (Transform t = r; t != _canvas; t = t.parent) d++;
                if (d >= bestDepth) { best = r; bestDepth = d; }
            }
            if (exact || best == null) return best;
            // A click on a button's text selects the button: nearest part with a script or control.
            for (Transform t = best; t != null && t != _canvas; t = t.parent)
                if (t.GetComponent<UdonSharpBehaviour>() != null || t.GetComponent<Selectable>() != null) return (RectTransform)t;
            return best;
        }

        private void DrawSelection(Rect img)
        {
            var sel = Selection.activeTransform as RectTransform;
            if (sel == null || sel == _canvas || !sel.IsChildOf(_canvas)) return;
            Rect cr = _canvas.rect;
            sel.GetWorldCorners(_corners);
            float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
            foreach (Vector3 p in _corners)
            {
                Vector3 l = _canvas.InverseTransformPoint(p);
                float gx = img.x + (l.x - cr.x) / cr.width * img.width;
                float gy = img.y + (1f - (l.y - cr.y) / cr.height) * img.height;
                x0 = Mathf.Min(x0, gx); x1 = Mathf.Max(x1, gx); y0 = Mathf.Min(y0, gy); y1 = Mathf.Max(y1, gy);
            }
            EditorGUI.DrawRect(new Rect(x0, y0, x1 - x0, 2f), Outline);
            EditorGUI.DrawRect(new Rect(x0, y1 - 2f, x1 - x0, 2f), Outline);
            EditorGUI.DrawRect(new Rect(x0, y0, 2f, y1 - y0), Outline);
            EditorGUI.DrawRect(new Rect(x1 - 2f, y0, 2f, y1 - y0), Outline);
        }
    }

    /// <summary>In builds and Play Mode the menus start hidden (alpha 0) until opened.</summary>
    internal class UIHudHideMenusOnBuild : IProcessSceneWithReport
    {
        public int callbackOrder => 0;

        public void OnProcessScene(Scene scene, BuildReport report)
        {
            foreach (var root in scene.GetRootGameObjects())
                foreach (var menu in root.GetComponentsInChildren<UIHudMenu>(true))
                {
                    if (menu.worldSpace && menu.startOpen) continue;
                    var g = menu.GetComponent<CanvasGroup>();
                    if (g != null) g.alpha = 0f;
                }
        }
    }
}
