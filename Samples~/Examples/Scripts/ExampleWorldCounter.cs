using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace LogicCuteGuy.UIHUD.Examples
{
    /// <summary>
    /// Example 4: a world panel (kiosk) whose buttons change a synced number everyone sees.
    /// The UI itself is local; this script syncs the value (take ownership, change, serialize).
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class ExampleWorldCounter : UdonSharpBehaviour
    {
        public TextMeshProUGUI countLabel;
        [UdonSynced] private int _count;

        private void Start() { Show(); }

        public void Plus() { Set(_count + 1); }
        public void Minus() { Set(_count - 1); }
        public void ResetCount() { Set(0); }

        private void Set(int value)
        {
            if (!Networking.IsOwner(gameObject)) Networking.SetOwner(Networking.LocalPlayer, gameObject);
            _count = value;
            RequestSerialization();
            Show();
        }

        public override void OnDeserialization() { Show(); }

        private void Show() { if (countLabel != null) countLabel.text = _count.ToString(); }
    }
}
