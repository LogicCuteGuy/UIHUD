using TMPro;
using UdonSharp;
using UnityEngine;

namespace LogicCuteGuy.UIHUD
{
    /// <summary>
    /// A group of pages inside a page, switched by tabs (UIHudTab.group) or by a selector row
    /// (&lt; name &gt;, wired to _OnPrev/_OnNext). Pages can hold more groups: nest as deep as you like.
    /// Reports the page index.
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class UIHudPages : UIHudWidget
    {
        public GameObject[] pages;
        [Tooltip("Optional, one per page (the selected look of each tab).")]
        public GameObject[] highlights;
        public int startPage;
        [Tooltip("Selector mode: shows the current page name.")]
        public TextMeshProUGUI titleLabel;
        public bool loop = true;
        [Tooltip("Scrolls the shown page when it is taller than the group.")]
        public UnityEngine.UI.ScrollRect pageScroll;

        [System.NonSerialized] public int currentPage = -1;

        private void Start() { if (currentPage < 0) ShowPageWithoutNotify(startPage); }

        public void _OnNext() { Step(1); }
        public void _OnPrev() { Step(-1); }

        public void ShowPage(int index)
        {
            int before = currentPage;
            ShowPageWithoutNotify(index);
            if (currentPage != before) Notify(currentPage);
        }

        public void ShowPageWithoutNotify(int index)
        {
            if (pages == null || pages.Length == 0) return;
            index = Mathf.Clamp(index, 0, pages.Length - 1);
            for (int i = 0; i < pages.Length; i++)
            {
                if (pages[i] != null) pages[i].SetActive(i == index);
                if (highlights != null && i < highlights.Length && highlights[i] != null) highlights[i].SetActive(i == index);
            }
            currentPage = index;
            if (pageScroll != null && pages[index] != null)
            {
                pageScroll.content = (RectTransform)pages[index].transform;
                pageScroll.verticalNormalizedPosition = 1f; // top
            }
            if (titleLabel != null && pages[index] != null) titleLabel.text = pages[index].name;
        }

        private void Step(int d)
        {
            if (pages == null || pages.Length == 0) return;
            int n = pages.Length;
            int i = currentPage < 0 ? startPage : currentPage;
            i = loop ? (i + d + n) % n : Mathf.Clamp(i + d, 0, n - 1);
            ShowPage(i);
        }
    }
}
