using CommonILRuntime.Module;
using CommonPresenter;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Lobby.Jigsaw
{
    /// <summary>
    /// 拼圖系統提示頁
    /// </summary>
    public class Hint : SystemUIBasePresenter
    {
        public override string objPath => "prefab/lobby_puzzle/puzzle_tip_info";
        public override UiLayer uiLayer { get { return UiLayer.System; } }

        Button closeButton;
        Button nextPageButton;
        Button previousPageButton;

        GameObject pageRoot;
        List<GameObject> pageGameObjects;

        int currentPageIdx = 0;
        public override void initContainerPresenter()
        {
            resOrder = new string[] { AssetBundleData.getBundleName(BundleType.LobbyPuzzle)};
            base.initContainerPresenter();
        }
        public override void initUIs()
        {
            closeButton = getBtnData("closeButton");
            nextPageButton = getBtnData("nextPageButton");
            previousPageButton = getBtnData("previousPageButton");

            pageRoot = getGameObjectData("pageRoot");
            
            pageGameObjects = new List<GameObject>();
            int nameIdx = 1;
            while (true)
            {
                var pageName = $"page_{nameIdx}";
                var pageObject = pageRoot.transform.Find(pageName);
                if (null == pageObject)
                {
                    break;
                }
                pageGameObjects.Add(pageObject.gameObject);
                nameIdx++;
            }
        }

        public override void init()
        {
            base.init();
            closeButton.onClick.AddListener(closeBtnClick);
            nextPageButton.onClick.AddListener(nextPage);
            previousPageButton.onClick.AddListener(previousPage);

            changePage(currentPageIdx);
        }

        public override void animOut()
        {
            clear();
        }

        int TOTAL_PAGES { get { return pageGameObjects.Count; } }

        /// <summary>
        /// 規則頁可以循環撥放
        /// </summary>
        /// <param name="idx"></param>
        /// <returns></returns>
        int wrapPageIdx(int idx)
        {
            return (idx + TOTAL_PAGES) % TOTAL_PAGES;
        }

        /// <summary>
        /// 規則頁不循環撥放
        /// </summary>
        /// <param name="idx"></param>
        /// <returns></returns>
        int clampPageIdx(int idx)
        {
            idx = Mathf.Max(Mathf.Min(idx, pageGameObjects.Count - 1), 0);
            previousPageButton.gameObject.setActiveWhenChange(!(0 == idx));
            nextPageButton.gameObject.setActiveWhenChange(!(pageGameObjects.Count-1 == idx));
            return idx;
        }

        void changePage(int idx)
        {
            //currentPageIdx = wrapPageIdx(idx);
            currentPageIdx = clampPageIdx(idx);
            for (int i = 0; i < pageGameObjects.Count; i++)
            {
                pageGameObjects[i].setActiveWhenChange(i == currentPageIdx);
            }
        }

        void nextPage()
        {
            changePage(currentPageIdx + 1);
        }

        void previousPage()
        {
            changePage(currentPageIdx - 1);
        }
    }
}
