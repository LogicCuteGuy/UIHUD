using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.UdonNetworkCalling;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;

namespace LogicCuteGuy.UIHUD.Examples
{
    /// <summary>
    /// Example 6: a synced poll on a world panel. One vote per player; changing your vote moves it.
    /// Buttons call _Vote0.._Vote3 locally. The vote is sent to the object owner as a network call,
    /// and only the owner writes the synced voter list (no ownership fights when two people vote at once).
    /// Everyone, late joiners included, redraws the Pie and Bar List from that list.
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class ExamplePoll : UdonSharpBehaviour
    {
        public string[] options = { "Pizza", "Sushi", "Tacos", "Ramen" };
        public UIHudPie pie;
        public UIHudBarList bars;
        public TextMeshProUGUI status;

        private const int MaxVoters = 100;
        // Who voted (player id) and for which option; _voters entries count.
        [UdonSynced] private int[] _ids = new int[MaxVoters];
        [UdonSynced] private int[] _choice = new int[MaxVoters];
        [UdonSynced] private int _voters;

        private void Start() { Apply(); }

        // ---------- Local button targets (underscore: not callable over the network) ----------

        public void _Vote0() { Vote(0); }
        public void _Vote1() { Vote(1); }
        public void _Vote2() { Vote(2); }
        public void _Vote3() { Vote(3); }

        public void _RequestReset() { SendCustomNetworkEvent(NetworkEventTarget.Owner, nameof(_OwnerReset)); }

        private void Vote(int option)
        {
            if (options == null || option < 0 || option >= options.Length) return;
            SendCustomNetworkEvent(NetworkEventTarget.Owner, nameof(_OwnerVote), option);
        }

        // ---------- Owner side ----------

        [NetworkCallable(5)]
        public void _OwnerVote(int option)
        {
            if (!NetworkCalling.InNetworkCall) return;
            VRCPlayerApi caller = NetworkCalling.CallingPlayer;
            if (caller == null || !caller.IsValid()) return;
            if (!Networking.IsOwner(gameObject)) return;
            if (options == null || option < 0 || option >= options.Length) return; // caller-controlled data

            int i = Find(caller.playerId);
            if (i < 0)
            {
                if (_voters >= MaxVoters) return;
                i = _voters++;
                _ids[i] = caller.playerId;
            }
            _choice[i] = option;
            RequestSerialization();
            Apply();
        }

        // Anyone may clear the poll (it's a demo); the owner applies it.
        [NetworkCallable(1)]
        public void _OwnerReset()
        {
            if (!NetworkCalling.InNetworkCall) return;
            if (!Networking.IsOwner(gameObject)) return;
            _voters = 0;
            RequestSerialization();
            Apply();
        }

        // A player who leaves takes their vote with them.
        public override void OnPlayerLeft(VRCPlayerApi player)
        {
            if (player == null || !Networking.IsOwner(gameObject)) return;
            int i = Find(player.playerId);
            if (i < 0) return;
            _voters--;
            _ids[i] = _ids[_voters];
            _choice[i] = _choice[_voters];
            RequestSerialization();
            Apply();
        }

        public override void OnDeserialization() { Apply(); }

        // ---------- Everyone ----------

        private int Find(int playerId)
        {
            for (int i = 0; i < _voters; i++) if (_ids[i] == playerId) return i;
            return -1;
        }

        /// <summary>Redraws the charts and "you voted" line from the synced list.</summary>
        private void Apply()
        {
            if (options == null) return;
            int n = options.Length;
            int[] counts = new int[n];
            for (int i = 0; i < _voters; i++) if (_choice[i] >= 0 && _choice[i] < n) counts[_choice[i]]++;
            for (int k = 0; k < n; k++)
            {
                if (pie != null) pie.SetEntry(options[k], counts[k]);
                if (bars != null) bars.SetEntry(options[k], counts[k]);
            }
            if (status == null) return;
            VRCPlayerApi me = Networking.LocalPlayer;
            int mine = me != null ? Find(me.playerId) : -1;
            status.text = (mine >= 0 ? "You voted <b>" + options[_choice[mine]] + "</b>" : "Tap an option to vote")
                + "  ·  " + _voters + (_voters == 1 ? " vote" : " votes");
        }
    }
}
