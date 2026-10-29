using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LogicCuteGuy.UIHUD.Editor
{
    /// <summary>
    /// Renders UIHUD canvases into textures for the Inspector: live previews of a menu/HUD and
    /// thumbnails of each widget kind. Nothing here is saved or recorded for undo.
    /// </summary>
    internal static class UIHudPreview
    {
        private static Camera _cam;
        private static readonly Dictionary<int, RenderTexture> Live = new Dictionary<int, RenderTexture>();
        private static readonly Dictionary<UIHudWidgetKind, Texture2D> Thumbs = new Dictionary<UIHudWidgetKind, Texture2D>();
        private static readonly Color Background = new Color(0.16f, 0.17f, 0.2f, 1f);

        private static Camera Cam
        {
            get
            {
                if (_cam != null) return _cam;
                var go = new GameObject("UIHUD Preview Camera") { hideFlags = HideFlags.HideAndDontSave };
                _cam = go.AddComponent<Camera>();
                _cam.enabled = false;
                _cam.orthographic = true;
                _cam.clearFlags = CameraClearFlags.SolidColor;
                _cam.backgroundColor = Background;
                return _cam;
            }
        }

        /// <summary>Renders a canvas into a cached texture (for custom drawing, e.g. the animation preview).</summary>
        internal static RenderTexture RenderToTexture(RectTransform canvas, int w, int h)
        {
            RenderTexture rt = GetRt(canvas.GetInstanceID() ^ 0x5A5A5A, w, h);
            RenderCanvas(canvas, rt, true);
            return rt;
        }

        private static RenderTexture GetRt(int key, int w, int h)
        {
            if (Live.TryGetValue(key, out var rt) && rt != null && rt.width == w && rt.height == h) return rt;
            if (rt != null) Object.DestroyImmediate(rt);
            rt = new RenderTexture(w, h, 16) { hideFlags = HideFlags.HideAndDontSave, antiAliasing = 4 };
            Live[key] = rt;
            return rt;
        }

        /// <summary>
        /// Points a hidden ortho camera at the canvas and renders only that canvas. Other canvases
        /// and the menu's hidden alpha are switched off/on just for the render (not saved).
        /// </summary>
        private static void RenderCanvas(RectTransform canvas, RenderTexture rt, bool isolate)
        {
            Camera cam = Cam;
            Vector3 center = canvas.TransformPoint(canvas.rect.center);
            float halfH = canvas.rect.height * 0.5f * canvas.lossyScale.y;
            float halfW = canvas.rect.width * 0.5f * canvas.lossyScale.x;
            float dist = Mathf.Max(halfH, halfW) + 0.01f;
            cam.transform.SetPositionAndRotation(center - canvas.forward * dist, canvas.rotation);
            cam.orthographicSize = halfH;
            cam.aspect = (float)rt.width / rt.height;
            cam.nearClipPlane = dist * 0.5f;
            cam.farClipPlane = dist * 1.5f;
            cam.cullingMask = 1 << canvas.gameObject.layer;
            cam.scene = canvas.gameObject.scene;
            cam.targetTexture = rt;

            var hidden = new List<Canvas>();
            if (isolate)
                foreach (var c in Object.FindObjectsOfType<Canvas>())
                    if (c.enabled && c.isRootCanvas && c.transform != canvas && !c.transform.IsChildOf(canvas))
                    { c.enabled = false; hidden.Add(c); }
            var group = canvas.GetComponent<CanvasGroup>();
            float alpha = group != null ? group.alpha : 1f;
            if (group != null) group.alpha = 1f;
            try
            {
                Canvas.ForceUpdateCanvases();
                cam.Render();
            }
            finally
            {
                if (group != null) group.alpha = alpha;
                foreach (var c in hidden) c.enabled = true;
                cam.targetTexture = null;
                Canvas.ForceUpdateCanvases();
            }
        }

        internal static void ClearThumbs()
        {
            foreach (var t in Thumbs.Values) if (t != null) Object.DestroyImmediate(t);
            Thumbs.Clear();
        }

        /// <summary>Picture of a widget kind in the current style, built in an isolated preview scene.</summary>
        internal static Texture2D Thumb(UIHudWidgetKind kind)
        {
            if (Thumbs.TryGetValue(kind, out var cached) && cached != null) return cached;
            Scene scene = EditorSceneManager.NewPreviewScene();
            var created = new List<Object>();
            try
            {
                var root = new GameObject("Preview", typeof(RectTransform));
                SceneManager.MoveGameObjectToScene(root, scene);
                root.layer = 31;
                var canvas = root.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                var rt = (RectTransform)root.transform;
                rt.sizeDelta = new Vector2(600f, 200f);
                rt.localScale = Vector3.one * 0.001f;
                var vl = root.AddComponent<VerticalLayoutGroup>();
                vl.padding = new RectOffset(20, 20, 20, 20);
                vl.childControlWidth = true; vl.childControlHeight = true; vl.childForceExpandHeight = false;
                vl.childAlignment = TextAnchor.MiddleCenter;

                GameObject w = UIHudBuilder.AddWidget(root.transform, kind);
                if (UIHudStyle.Current != null) UIHudStyle.Current.Apply(root.transform);
                foreach (var g in root.GetComponentsInChildren<CanvasGroup>(true)) g.alpha = 1f;
                foreach (var t in root.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 31;

                // Fit the canvas height to the widget.
                LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
                float h = Mathf.Clamp(LayoutUtility.GetPreferredHeight((RectTransform)w.transform), 2f, 600f);
                if (h < 8f) h = ((RectTransform)w.transform).rect.height;
                rt.sizeDelta = new Vector2(600f, Mathf.Clamp(h + 40f, 80f, 640f));
                LayoutRebuilder.ForceRebuildLayoutImmediate(rt);

                var tmp = RenderTexture.GetTemporary(300, Mathf.RoundToInt(300f * rt.sizeDelta.y / rt.sizeDelta.x), 16);
                RenderCanvas(rt, tmp, false);
                var prev = RenderTexture.active;
                RenderTexture.active = tmp;
                var tex = new Texture2D(tmp.width, tmp.height, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
                tex.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
                tex.Apply();
                RenderTexture.active = prev;
                RenderTexture.ReleaseTemporary(tmp);
                Thumbs[kind] = tex;

                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    created.Add(t.gameObject);
                    foreach (var c in t.GetComponents<Component>()) created.Add(c);
                }
                return tex;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("UIHUD: preview of " + kind + " failed: " + e.Message);
                return null;
            }
            finally
            {
                // The builder records undo steps; drop them, they belong to a throwaway scene.
                foreach (var o in created) if (o != null) Undo.ClearUndo(o);
                // UdonSharp finishes new behaviours in a delayCall; close after it, or it touches destroyed objects.
                EditorApplication.delayCall += () => EditorSceneManager.ClosePreviewScene(scene);
            }
        }
    }

    /// <summary>"+ Add widget" popup: list on the left, picture of the hovered widget on the right.</summary>
    internal class UIHudAddWidgetPopup : PopupWindowContent
    {
        private readonly Transform _parent;
        private readonly UIHudWidgetKind[] _kinds;
        private readonly System.Action<GameObject> _onAdded;
        private UIHudWidgetKind _hover;
        private Vector2 _scroll;

        internal UIHudAddWidgetPopup(Transform parent, UIHudWidgetKind[] kinds, System.Action<GameObject> onAdded)
        {
            _parent = parent; _kinds = kinds; _onAdded = onAdded; _hover = kinds[0];
        }

        public override Vector2 GetWindowSize() => new Vector2(470f, Mathf.Clamp(_kinds.Length * 22f + 12f, 240f, 420f));

        public override void OnOpen() { editorWindow.wantsMouseMove = true; }

        public override void OnGUI(Rect rect)
        {
            if (Event.current.type == EventType.MouseMove) editorWindow.Repaint();
            var list = new Rect(rect.x + 4f, rect.y + 4f, 150f, rect.height - 8f);
            var view = new Rect(list.xMax + 8f, rect.y + 6f, rect.width - list.width - 20f, rect.height - 12f);

            _scroll = GUI.BeginScrollView(list, _scroll, new Rect(0f, 0f, list.width - 16f, _kinds.Length * 22f));
            for (int i = 0; i < _kinds.Length; i++)
            {
                var r = new Rect(0f, i * 22f, list.width - (_kinds.Length * 22f > list.height ? 16f : 0f), 20f);
                UIHudWidgetKind k = _kinds[i];
                if (r.Contains(Event.current.mousePosition)) _hover = k;
                var style = k == _hover ? EditorStyles.miniButtonMid : EditorStyles.miniButton;
                if (GUI.Button(r, UIHudGUI.KindLabel(k), style))
                {
                    GameObject go = UIHudBuilder.AddWidget(_parent, k);
                    _onAdded?.Invoke(go);
                    editorWindow.Close();
                    GUIUtility.ExitGUI();
                }
            }
            GUI.EndScrollView();

            GUI.Label(new Rect(view.x, view.y, view.width, 18f), UIHudGUI.KindLabel(_hover), EditorStyles.boldLabel);
            GUI.Label(new Rect(view.x, view.y + 18f, view.width, 34f), UIHudGUI.KindHelp(_hover), EditorStyles.wordWrappedMiniLabel);
            var pic = new Rect(view.x, view.y + 56f, view.width, view.height - 56f);
            EditorGUI.DrawRect(pic, new Color(0.16f, 0.17f, 0.2f, 1f));
            Texture2D tex = UIHudPreview.Thumb(_hover);
            if (tex != null) GUI.DrawTexture(pic, tex, ScaleMode.ScaleToFit);
        }
    }
}
