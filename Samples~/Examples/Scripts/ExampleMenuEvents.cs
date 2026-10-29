using UdonSharp;
using UnityEngine;

namespace LogicCuteGuy.UIHUD.Examples
{
    /// <summary>
    /// Example 5: the Manager's callbacks (Callback Target = this). Reacts to the menu opening and
    /// closing, and to the player switching platform (desktop / VR / mobile).
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class ExampleMenuEvents : UdonSharpBehaviour
    {
        public UIHudManager manager;
        public UIHudNotification notification;
        [Tooltip("Optional label on the HUD that shows the menu state.")]
        public TMPro.TextMeshProUGUI stateLabel;

        private int _opened;

        public void OnMenuOpened()
        {
            _opened++;
            if (stateLabel != null) stateLabel.text = "Menu open (opened " + _opened + "x)";
        }

        public void OnMenuClosed()
        {
            if (stateLabel != null) stateLabel.text = "Menu closed (opened " + _opened + "x)";
        }

        public void OnPlatformChanged()
        {
            string name = "Desktop";
            if (manager.platform == UIHudPlatform.VR) name = "VR";
            else if (manager.platform == UIHudPlatform.Mobile) name = "Mobile";
            notification.Show("Platform: " + name);
        }
    }
}
