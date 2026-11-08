using TMPro;
using UdonSharp;
using UnityEngine;

namespace LogicCuteGuy.UIHUD.Examples
{
    /// <summary>
    /// Example 7: a shop kiosk that starts closed and opens from a sign next to it.
    /// Local only: your coins and items are yours (save them with PlayerData if you need them to last).
    /// Buy buttons call _Buy0.._Buy2; a Value widget on the panel watches the private _coins.
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class ExampleShop : UdonSharpBehaviour
    {
        public string[] items = { "Hat", "Cape", "Pet" };
        public int[] prices = { 30, 80, 150 };
        public int startCoins = 100;
        public int earnAmount = 25;
        public UIHudNotification toast;
        public TextMeshProUGUI inventory;

        private int _coins;
        private int[] _owned;

        private void Start()
        {
            _coins = startCoins;
            _owned = new int[items != null ? items.Length : 0];
            ShowInventory();
        }

        public void _Buy0() { Buy(0); }
        public void _Buy1() { Buy(1); }
        public void _Buy2() { Buy(2); }

        public void _Earn()
        {
            _coins += earnAmount;
            Toast("+" + earnAmount + " coins");
        }

        private void Buy(int i)
        {
            if (_owned == null || i < 0 || i >= _owned.Length || i >= prices.Length) return;
            if (_coins < prices[i]) { Toast("Not enough coins for the " + items[i] + " (" + prices[i] + ")"); return; }
            _coins -= prices[i];
            _owned[i]++;
            Toast("Bought a " + items[i] + "!");
            ShowInventory();
        }

        private void ShowInventory()
        {
            if (inventory == null || _owned == null) return;
            string s = "";
            for (int i = 0; i < _owned.Length; i++)
                if (_owned[i] > 0) s += (s.Length > 0 ? ", " : "") + items[i] + (_owned[i] > 1 ? " x" + _owned[i] : "");
            inventory.text = s.Length > 0 ? "You own: " + s : "You own nothing yet.";
        }

        private void Toast(string message) { if (toast != null) toast.Show(message); }
    }
}
