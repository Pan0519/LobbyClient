using CommonILRuntime.Module;
using CommonILRuntime.BindingModule;
using UnityEngine;
using UnityEngine.UI;
using UniRx;
using System;
using CommonPresenter;
using LobbyLogic.Common;

namespace EventActivity
{
    class ActivityRewardTipPresenter : SystemUIBasePresenter
    {
        public override string objPath => "prefab/game/game_common_tip";
        public override UiLayer uiLayer { get => UiLayer.System; }
        public Subject<bool> notifyToggleSub { get; private set; } = new Subject<bool>();

        Button closeBtn;
        Button playNowBtn;
        Image activityIconImg;
        Toggle notifyToggle;
        Image activityItemImg;
        GameObject maxTipObj;
        CustomTextSizeChange amountTxt;

        public Action openActivityPage = null;
        bool isOpenActivityPage;
        bool alreadyMaxAmount;
        RewardTipData tipData;
        public override void initUIs()
        {
            base.initUIs();
            closeBtn = getBtnData("close_btn");
            playNowBtn = getBtnData("playnow_btn");
            activityIconImg = getImageData("activity_icon_img");
            notifyToggle = getBindingData<Toggle>("notify_toggle");
            activityItemImg = getImageData("activity_item_img");
            maxTipObj = getGameObjectData("max_tip_obj");
            amountTxt = getBindingData<CustomTextSizeChange>("ticket_amount_txt");
        }

        public override void init()
        {
            base.init();
            notifyToggle.isOn = true;
            closeBtn.onClick.AddListener(closePage);
            playNowBtn.onClick.AddListener(openActivtyPage);
            notifyToggle.onValueChanged.AddListener(notifyToggleValueChange);
        }

        public override void animOut()
        {
            clear();
        }

        public override void closePresenter()
        {
            base.closePresenter();
            if (!isOpenActivityPage)
            {
                if (alreadyMaxAmount)
                {
                    var tipCheckPresenter = UiManager.getPresenter<RewardMaxTipCheckPresenter>();
                    tipCheckPresenter.openActivityPageCB = openActivityPage;
                    tipCheckPresenter.openCheckPage(tipData.iconSprite);
                }
            }
            else
            {
                openActivityPage();
            }
            GamePauseManager.gameResume();
        }

        public void openTipPage(RewardTipData tipData, bool isNotifyOn)
        {
            GamePauseManager.gamePause();
            notifyToggle.isOn = isNotifyOn;
            this.tipData = tipData;
            activityIconImg.sprite = tipData.iconSprite;
            activityItemImg.sprite = tipData.itemSprite;
            amountTxt.text = tipData.amount.ToString();
            alreadyMaxAmount = tipData.amount >= tipData.maxAmount;
            maxTipObj.setActiveWhenChange(alreadyMaxAmount);
            open();
        }

        void closePage()
        {
            isOpenActivityPage = false;
            closeBtnClick();
        }

        void notifyToggleValueChange(bool isOn)
        {
            notifyToggleSub.OnNext(isOn);
        }

        void openActivtyPage()
        {
            isOpenActivityPage = true;
            closeBtnClick();
        }
    }

    public class RewardTipData
    {
        public Sprite iconSprite;
        public Sprite itemSprite;
        public int amount;
        public int maxAmount;
    }
}
