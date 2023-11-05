using UnityEngine.UI;
using UnityEngine;
using CommonILRuntime.Module;
using CommonILRuntime.BindingModule;
using System;
using CommonPresenter;

namespace HighRoller
{
    class HighRollerEarningPoints : SystemUIBasePresenter
    {
        public override string objPath => "prefab/diamond_club/page_earning_points";
        public override UiLayer uiLayer { get => UiLayer.System; }

        #region UIs
        Animator showAnim;
        Button playBtn;
        GameObject playObj;
        GameObject playCloseObj;
        Button storeBtn;
        GameObject storeObj;
        GameObject storeCloseObj;
        Button closeBtn;
        Text spinNum;
        Text betNum;
        Image spinScheduleBar;
        Button getPointBtn;
        #endregion
        PageType pageType = PageType.Play;
        Action openShopCB;
        public override void initContainerPresenter()
        {
            resOrder = new string[] { AssetBundleData.getBundleName(BundleType.Crown) };
            base.initContainerPresenter();
        }
        public override void initUIs()
        {
            showAnim = getAnimatorData("show_anim");
            playBtn = getBtnData("play_btn");
            playObj = getGameObjectData("play_obj");
            playCloseObj = getGameObjectData("play_close");
            storeBtn = getBtnData("store_btn");
            storeObj = getGameObjectData("store_obj");
            storeCloseObj = getGameObjectData("store_close");
            closeBtn = getBtnData("btn_close_page");
            spinNum = getTextData("spin_num");
            betNum = getTextData("bet_num");
            spinScheduleBar = getImageData("spin_schedule_bar");
            getPointBtn = getBtnData("get_point_btn");
        }
        public override void init()
        {
            base.init();
            closeBtn.onClick.AddListener(closeBtnClick);
            playBtn.onClick.AddListener(openPlayObj);
            storeBtn.onClick.AddListener(openStoreObj);
            getPointBtn.onClick.AddListener(openShopPresenter);
        }

        public override Animator getUiAnimator()
        {
            return showAnim;
        }
        float maxSpinSchedule { get { return 300.0f; } }
        public void openPage(int spinSchedule, Action openShopCB = null)
        {
            var betNumValue = Mathf.CeilToInt(HighRollerDataManager.instance.getHighRollerCoinExchangeRate / 10000) * 10000;
            betNum.text = betNumValue.ToString("N0");
            spinNum.text = $"{spinSchedule}/{(int)maxSpinSchedule}";
            spinScheduleBar.fillAmount = (float)spinSchedule / maxSpinSchedule;
            showPage();
            this.openShopCB = openShopCB;
            open();
        }

        public override void animOut()
        {
            clear();
        }

        void openPlayObj()
        {
            pageType = PageType.Play;
            showPage();
        }
        void openStoreObj()
        {
            pageType = PageType.Store;
            showPage();
        }
        void openShopPresenter()
        {
            UiManager.getPresenter<Shop.ShopMainPresenter>().open();
            closePresenter();
            if (null != openShopCB)
            {
                openShopCB();
            }
        }

        void showPage()
        {
            playObj.setActiveWhenChange(PageType.Play == pageType);
            storeObj.setActiveWhenChange(PageType.Store == pageType);
            playCloseObj.setActiveWhenChange(PageType.Store == pageType);
            storeCloseObj.setActiveWhenChange(PageType.Play == pageType);
        }
    }

    enum PageType
    {
        Store,
        Play,
    }
}
