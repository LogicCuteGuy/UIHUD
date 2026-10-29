using UdonSharp;

namespace LogicCuteGuy.UIHUD
{
    /// <summary>Tab button of a menu or of a nested page group. Wire Button.onClick -> SendCustomEvent("_OnClick").</summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class UIHudTab : UdonSharpBehaviour
    {
        public UIHudMenu menu;
        public UIHudPages group;
        public int pageIndex;

        public void _OnClick()
        {
            if (group != null) group.ShowPage(pageIndex);
            else if (menu != null) menu.ShowPage(pageIndex);
        }
    }
}
