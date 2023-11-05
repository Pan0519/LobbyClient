using Services;
using CommonILRuntime.Module;
using UnityEngine.UI;
using UnityEngine;
using CommonPresenter;
using System.Collections.Generic;

namespace Shop
{
    public class ShopInfoPresenter : SystemUIBasePresenter
    {
        public override string objPath { get { return UtilServices.getOrientationObjPath("prefab/lobby_shop/shop_tip_info"); } }
        public override UiLayer uiLayer { get { return UiLayer.System; } }
        #region UIs
        Button closeBtn;
        Button preBtn;
        Button nextBtn;
        #endregion

        //GameObject[] pages;
        //Image[] points;

        int showIdx = 0;
        Sprite pointOff;
        Sprite pointOn;

        List<GameObject> pages = new List<GameObject>();
        List<Image> points = new List<Image>();

        public override void initUIs()
        {
            closeBtn = getBtnData("close_btn");
            preBtn = getBtnData("pre_btn");
            nextBtn = getBtnData("next_btn");
        }

        public override void init()
        {
            base.init();
            closeBtn.onClick.AddListener(closeBtnClick);
            preBtn.onClick.AddListener(preClick);
            nextBtn.onClick.AddListener(nextClick);
            pointOff = ShopDataStore.getShopSprite("page_dot_off");
            pointOn = ShopDataStore.getShopSprite("page_dot_on");
        }

        public void shopOpen()
        {
            for (int i = 0; i < 4; ++i)
            {
                getImageData($"point_img_{i + 1}").gameObject.setActiveWhenChange(false);
                var pageObj = getGameObjectData($"page_{i + 1}_obj");
                if (i <= 0)
                {
                    pageObj.setActiveWhenChange(true);
                    pages.Add(pageObj);
                    continue;
                }
                pageObj.setActiveWhenChange(false);
            }

            preBtn.gameObject.setActiveWhenChange(false);
            nextBtn.gameObject.setActiveWhenChange(false);
        }

        public override void animOut()
        {
            clear();
        }

        public void openStartPage(int pageId)
        {
            for (int i = 0; i < 4; ++i)
            {
                var pageObj = getGameObjectData($"page_{i + 1}_obj");
                var point = getImageData($"point_img_{i + 1}");
                if (i > pageId)
                {
                    pageObj.setActiveWhenChange(true);
                    pages.Add(pageObj);
                    points.Add(point);
                    continue;
                }
                pageObj.setActiveWhenChange(false);
            }

            showPage();
        }

        void preClick()
        {
            showIdx--;
            showPage();
        }

        void nextClick()
        {
            showIdx++;
            showPage();
        }

        void showPage()
        {
            preBtn.interactable = showIdx > 0;
            nextBtn.interactable = showIdx < pages.Count - 1;

            for (int i = 0; i < pages.Count; ++i)
            {
                bool isItemShow = i == showIdx;
                pages[i].setActiveWhenChange(isItemShow);
                points[i].sprite = (isItemShow) ? pointOn : pointOff;
            }
        }

        public override void close()
        {
            base.close();
            showIdx = 0;
        }
    }
}
