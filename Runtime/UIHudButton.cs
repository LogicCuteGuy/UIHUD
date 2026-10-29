using UdonSharp;
using UnityEngine;

namespace LogicCuteGuy.UIHUD
{
    /// <summary>
    /// Button helper. Wire Button.onClick -> UdonBehaviour.SendCustomEvent("_OnClick")
    /// (the UIHUD builder does this for you). Can also toggle objects and open/close menus.
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class UIHudButton : UIHudWidget
    {
        [Header("Built-in actions")]
        public GameObject[] toggleObjects;
        public UIHudManager manager;
        public bool toggleMenu;
        public bool closeMenu;
        public UIHudMenu menuToToggle;
        public AudioSource clickSound;

        [Header("More actions")]
        [Tooltip("Teleport the local player here.")]
        public Transform teleportTo;
        public bool respawn;
        [Tooltip("Show a toast message.")]
        public UIHudNotification notification;
        public string notificationText = "";
        [Tooltip("Open this menu at pageIndex.")]
        public UIHudMenu openMenu;
        public int openPage;

        [Tooltip("Set by the hotbar this button is a slot of.")]
        [HideInInspector] public UIHudHotbar hotbar;

        public void _OnClick()
        {
            if (clickSound != null) clickSound.Play();
            VRC.SDKBase.VRCPlayerApi local = VRC.SDKBase.Networking.LocalPlayer;
            if (local != null)
            {
                if (teleportTo != null) local.TeleportTo(teleportTo.position, teleportTo.rotation);
                else if (respawn) local.Respawn();
            }
            if (notification != null && !string.IsNullOrEmpty(notificationText)) notification.Show(notificationText);
            if (openMenu != null) { openMenu.Open(); openMenu.ShowPage(openPage); }
            if (toggleObjects != null)
                for (int i = 0; i < toggleObjects.Length; i++)
                    if (toggleObjects[i] != null) toggleObjects[i].SetActive(!toggleObjects[i].activeSelf);
            if (menuToToggle != null) menuToToggle.Toggle();
            if (manager != null)
            {
                if (toggleMenu) manager.ToggleMenu();
                else if (closeMenu) manager.CloseMenu();
            }
            if (hotbar != null) hotbar._Pick(this);
            Notify(true);
        }
    }
}
