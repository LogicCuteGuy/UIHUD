using UdonSharp;
using UnityEngine;

namespace LogicCuteGuy.UIHUD.Examples
{
    /// <summary>
    /// Example 1: buttons call methods here (Button → Target = this, Event = "Damage"),
    /// which change the HUD health bar and show a notification when health gets low.
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class ExampleHealth : UdonSharpBehaviour
    {
        public UIHudBar healthBar;
        public UIHudNotification notification;
        public float maxHealth = 100f;
        public float lowHealth = 30f;

        private float _health;

        private void Start()
        {
            _health = maxHealth;
            if (healthBar != null) healthBar.SetValue(_health, maxHealth);
        }

        public void Damage() { Change(-10f); }
        public void Heal() { Change(25f); }
        public void FullHeal() { Change(maxHealth); }

        private void Change(float delta)
        {
            float old = _health;
            _health = Mathf.Clamp(_health + delta, 0f, maxHealth);
            if (healthBar != null) healthBar.SetCurrent(_health);
            if (notification == null) return;
            if (_health <= 0f) notification.Show("Out of health!");
            else if (_health <= lowHealth && old > lowHealth) notification.Show("Low health!");
            else if (_health >= maxHealth && old < maxHealth) notification.Show("Fully healed");
        }
    }
}
