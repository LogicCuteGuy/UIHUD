using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace LogicCuteGuy.UIHUD
{
    /// <summary>Players in the instance, updated on join/leave. Marks you and the instance master.</summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class UIHudPlayerList : UdonSharpBehaviour
    {
        public TextMeshProUGUI list;
        [Tooltip("Optional: '{0}' = player count.")]
        public TextMeshProUGUI countLabel;
        public string countFormat = "Players: {0}";
        public string youMark = " <color=#7fd0ff>(you)</color>";
        public string masterMark = " <color=#ffd060>(master)</color>";

        private VRCPlayerApi[] _players = new VRCPlayerApi[100];

        private void Start() { Refresh(); }
        public override void OnPlayerJoined(VRCPlayerApi player) { Refresh(); }
        // The leaving player is still listed during OnPlayerLeft: refresh a moment later.
        public override void OnPlayerLeft(VRCPlayerApi player) { SendCustomEventDelayedFrames(nameof(Refresh), 1); }

        public void Refresh()
        {
            int n = VRCPlayerApi.GetPlayerCount();
            if (_players.Length < n) _players = new VRCPlayerApi[n];
            VRCPlayerApi.GetPlayers(_players);
            string s = "";
            for (int i = 0; i < n; i++)
            {
                VRCPlayerApi p = _players[i];
                if (!Utilities.IsValid(p)) continue;
                s += p.displayName;
                if (p.isLocal) s += youMark;
                if (p.isMaster) s += masterMark;
                s += "\n";
            }
            if (list != null) list.text = s;
            if (countLabel != null) countLabel.text = string.Format(countFormat, n);
        }
    }
}
