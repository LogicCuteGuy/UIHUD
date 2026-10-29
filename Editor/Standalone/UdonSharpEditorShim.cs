// Standalone (no VRChat SDK) stand-in for the UdonSharpEditor calls UIHUD's editor uses. Without Udon there is
// no backing UdonBehaviour: the behaviour is its own. Compiles only without com.vrchat.worlds.
using UdonSharp;
using UnityEditor;
using UnityEngine;

namespace UdonSharpEditor
{
    public static class UdonSharpUndo
    {
        public static T AddComponent<T>(GameObject go) where T : UdonSharpBehaviour { return Undo.AddComponent<T>(go); }
    }

    public static class UdonSharpGUI
    {
        /// <summary>Draws nothing; false = keep drawing the inspector.</summary>
        public static bool DrawDefaultUdonSharpBehaviourHeader(Object target, bool skipLine = false, bool drawScript = true) { return false; }
    }

    public static class UdonSharpEditorUtility
    {
        public static UdonSharpBehaviour GetBackingUdonBehaviour(UdonSharpBehaviour behaviour) { return behaviour; }
        public static void CopyProxyToUdon(UdonSharpBehaviour behaviour) { }
        public static void CopyUdonToProxy(UdonSharpBehaviour behaviour) { }
    }
}
