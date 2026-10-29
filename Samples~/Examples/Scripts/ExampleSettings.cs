using UdonSharp;
using UnityEngine;

namespace LogicCuteGuy.UIHUD.Examples
{
    /// <summary>
    /// Example 2: each widget writes its value into a variable here (Value Variable), then sends an
    /// event (Event). Toggle → showHud, Slider → volume, Selector → qualityIndex, Input → playerName.
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class ExampleSettings : UdonSharpBehaviour
    {
        public UIHudManager manager;
        public UIHudNotification notification;
        public UIHudSelector quality;
        [Tooltip("Optional: the slider sets its volume.")]
        public AudioSource music;

        // Written by the widgets before they send their event.
        public bool showHud = true;
        public float volume = 0.5f;
        public int qualityIndex;
        public string playerName = "";

        public void OnShowHud() { manager.SetHudVisible(showHud); }

        public void OnVolume() { if (music != null) music.volume = volume; }

        public void OnQuality() { notification.Show("Quality: " + quality.Selected() + " (index " + qualityIndex + ")"); }

        public void OnName()
        {
            if (playerName.Trim().Length == 0) return;
            notification.Show("Hello, " + playerName.Trim() + "!");
        }
    }
}
