using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace LogicCuteGuy.UIHUD.Editor
{
    /// <summary>
    /// Editor-only look of a UIHUD: colors, font, sprites and spacing. Applying it repaints the
    /// scene objects, so nothing references this asset at runtime (safe for VRChat upload).
    /// </summary>
    [CreateAssetMenu(menuName = "UIHUD/Style", fileName = "UIHUD Style")]
    public class UIHudStyle : ScriptableObject
    {
        [Header("Text")]
        [Tooltip("Empty = keep the current font.")]
        public TMP_FontAsset font;
        public Color text = Color.white;

        [Header("Panels")]
        public Color panel = new Color(0.08f, 0.09f, 0.12f, 0.92f);
        [Tooltip("Optional (e.g. a rounded 9-slice sprite). Empty = plain rectangle.")]
        public Sprite panelSprite;

        [Header("Buttons")]
        public Color button = new Color(1f, 1f, 1f, 0.08f);
        public Color buttonHover = new Color(1f, 1f, 1f, 0.2f);
        public Color buttonPressed = new Color(0.25f, 0.6f, 1f, 1f);
        [Tooltip("Empty = Unity's default rounded sprite.")]
        public Sprite buttonSprite;

        [Header("Accents")]
        [Tooltip("Selected tab, toggle check, slider fill.")]
        public Color accent = new Color(0.25f, 0.6f, 1f, 1f);
        [Tooltip("Toggle box and slider track.")]
        public Color track = new Color(1f, 1f, 1f, 0.15f);
        public Color barBack = new Color(0f, 0f, 0f, 0.6f);
        [Tooltip("Used when the bar's 'Use Gradient' is off.")]
        public Color barFill = new Color(0.3f, 0.85f, 0.4f, 1f);

        [Header("Spacing (canvas px)")]
        public int padding = 24;
        public float spacing = 12f;

        // ---------- Presets ----------

        internal static readonly string[] PresetNames = { "Dark", "Light", "Glass", "Neon" };

        internal void ApplyPreset(int i)
        {
            Undo.RecordObject(this, "Style Preset");
            switch (i)
            {
                case 1: // Light
                    Set(new Color(0.96f, 0.96f, 0.98f, 0.96f), new Color(0.1f, 0.1f, 0.13f), new Color(0f, 0f, 0f, 0.06f),
                        new Color(0f, 0f, 0f, 0.14f), new Color(0.15f, 0.45f, 0.95f), new Color(0f, 0f, 0f, 0.12f), new Color(1f, 1f, 1f, 0.75f));
                    break;
                case 2: // Glass
                    Set(new Color(1f, 1f, 1f, 0.14f), Color.white, new Color(1f, 1f, 1f, 0.14f),
                        new Color(1f, 1f, 1f, 0.32f), new Color(0.6f, 0.9f, 1f), new Color(1f, 1f, 1f, 0.2f), new Color(0f, 0f, 0f, 0.35f));
                    break;
                case 3: // Neon
                    Set(new Color(0.05f, 0.02f, 0.1f, 0.92f), new Color(0.9f, 0.95f, 1f), new Color(1f, 0.2f, 0.8f, 0.14f),
                        new Color(1f, 0.2f, 0.8f, 0.35f), new Color(0f, 1f, 0.9f), new Color(1f, 1f, 1f, 0.12f), new Color(0f, 0f, 0f, 0.6f));
                    break;
                default: // Dark
                    Set(new Color(0.08f, 0.09f, 0.12f, 0.92f), Color.white, new Color(1f, 1f, 1f, 0.08f),
                        new Color(1f, 1f, 1f, 0.2f), new Color(0.25f, 0.6f, 1f), new Color(1f, 1f, 1f, 0.15f), new Color(0f, 0f, 0f, 0.6f));
                    break;
            }
            EditorUtility.SetDirty(this);
        }

        private void Set(Color panel, Color text, Color button, Color hover, Color accent, Color track, Color barBack)
        {
            this.panel = panel; this.text = text; this.button = button; buttonHover = hover;
            this.accent = accent; buttonPressed = accent; this.track = track; this.barBack = barBack;
        }

        // ---------- Current style (per project, remembered by the editor) ----------

        private static string Key => "UIHUD.Style." + Application.dataPath;

        internal static UIHudStyle Current
        {
            get
            {
                string guid = EditorPrefs.GetString(Key, "");
                return guid == "" ? null : AssetDatabase.LoadAssetAtPath<UIHudStyle>(AssetDatabase.GUIDToAssetPath(guid));
            }
            set => EditorPrefs.SetString(Key, value == null ? "" : AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(value)));
        }

        internal static UIHudStyle CreateAsset()
        {
            string path = AssetDatabase.GenerateUniqueAssetPath("Assets/UIHUD Style.asset");
            var s = CreateInstance<UIHudStyle>();
            AssetDatabase.CreateAsset(s, path);
            AssetDatabase.SaveAssets();
            return s;
        }

        // ---------- Apply ----------

        /// <summary>Repaints every UIHUD in the open scenes.</summary>
        internal void ApplyToScene()
        {
            UIHudPreview.ClearThumbs(); // widget pictures follow the style
            foreach (var m in Object.FindObjectsOfType<UIHudManager>(true)) Apply(m.transform);
            foreach (var w in Object.FindObjectsOfType<UIHudMenu>(true))
                if (w.GetComponentInParent<UIHudManager>(true) == null) Apply(w.transform); // standalone world panels
        }

        /// <summary>Repaints everything under root. Roles come from the UIHUD structure, so no marker components are needed.</summary>
        internal void Apply(Transform root)
        {
            if (root == null) return;
            Sprite defaultButton = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

            foreach (var menu in root.GetComponentsInChildren<UIHudMenu>(true))
            {
                Paint(menu.GetComponent<Image>(), panel, panelSprite, true);
                var vl = menu.GetComponent<VerticalLayoutGroup>();
                if (vl != null) { Rec(vl); vl.padding = new RectOffset(padding, padding, padding, padding); vl.spacing = spacing + 4f; }
                if (menu.pages != null)
                    foreach (var p in menu.pages)
                    {
                        var pl = p != null ? p.GetComponent<VerticalLayoutGroup>() : null;
                        if (pl != null) { Rec(pl); pl.spacing = spacing; }
                    }
                if (menu.pageHighlights != null)
                    foreach (var h in menu.pageHighlights) if (h != null) Paint(h.GetComponent<Image>(), accent, null, false);
            }

            foreach (var n in root.GetComponentsInChildren<UIHudNotification>(true))
                Paint(n.GetComponent<Image>(), panel, panelSprite, true);

            foreach (var bar in root.GetComponentsInChildren<UIHudBar>(true))
            {
                Paint(bar.GetComponent<Image>(), barBack, null, false);
                if (!bar.useGradient && bar.fill != null) { Rec(bar.fill); bar.fill.color = barFill; }
            }

            foreach (var sel in root.GetComponentsInChildren<Selectable>(true))
            {
                ColorBlock cb = sel.colors;
                Rec(sel);
                if (sel is Slider slider)
                {
                    cb.normalColor = text;
                    cb.highlightedColor = Color.Lerp(text, accent, 0.5f);
                    if (slider.fillRect != null) Paint(slider.fillRect.GetComponent<Image>(), accent, null, false);
                    Transform bg = slider.transform.Find("Background");
                    if (bg != null) Paint(bg.GetComponent<Image>(), track, null, false);
                }
                else
                {
                    // Lone buttons (mobile / wrist canvases) float in the world: give them the panel color.
                    bool lone = sel is Button && sel.GetComponentInParent<UIHudMenu>(true) == null && sel.GetComponentInParent<UIHudNotification>(true) == null;
                    cb.normalColor = sel is Toggle ? track : lone ? panel : button;
                    cb.highlightedColor = buttonHover;
                    if (sel is Toggle t && t.graphic != null) { Rec(t.graphic); t.graphic.color = accent; }
                    if (sel is Scrollbar) { cb.normalColor = accent; Paint(sel.GetComponent<Image>(), track, null, false); }
                }
                cb.pressedColor = buttonPressed;
                cb.selectedColor = cb.normalColor;
                sel.colors = cb;
                if (sel.targetGraphic is Image img)
                {
                    Rec(img);
                    img.color = Color.white; // the color block tints it
                    if (sel is Button) { img.sprite = buttonSprite != null ? buttonSprite : defaultButton; img.type = Image.Type.Sliced; }
                }
            }

            foreach (var t in root.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                Rec(t);
                var field = t.GetComponentInParent<TMP_InputField>(true);
                t.color = field != null && field.placeholder == t ? new Color(text.r, text.g, text.b, text.a * 0.5f) : text;
                if (font != null) t.font = font;
            }
        }

        private static void Paint(Image img, Color c, Sprite sprite, bool setSprite)
        {
            if (img == null) return;
            Rec(img);
            img.color = c;
            if (!setSprite) return;
            img.sprite = sprite;
            img.type = sprite != null && sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
        }

        private static void Rec(Object o)
        {
            Undo.RecordObject(o, "Apply UIHUD Style");
            EditorUtility.SetDirty(o);
        }
    }

    [CustomEditor(typeof(UIHudStyle))]
    internal class UIHudStyleEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var s = (UIHudStyle)target;
            bool current = UIHudStyle.Current == s;

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Preset", GUILayout.Width(50));
                int p = GUILayout.Toolbar(-1, UIHudStyle.PresetNames);
                if (p >= 0) { s.ApplyPreset(p); if (current) s.ApplyToScene(); }
            }

            serializedObject.Update();
            EditorGUI.BeginChangeCheck();
            SerializedProperty it = serializedObject.GetIterator();
            for (bool enter = true; it.NextVisible(enter); enter = false)
                if (it.name != "m_Script") EditorGUILayout.PropertyField(it, true);
            bool changed = EditorGUI.EndChangeCheck();
            serializedObject.ApplyModifiedProperties();
            if (changed && current) s.ApplyToScene(); // live preview

            EditorGUILayout.Space(4);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (!current && GUILayout.Button("Use for this project")) { UIHudStyle.Current = s; s.ApplyToScene(); }
                if (GUILayout.Button("Apply to scene")) s.ApplyToScene();
            }
            if (current) EditorGUILayout.HelpBox("Current style: changes apply to the scene live, and new widgets use it.", MessageType.None);
        }
    }
}
