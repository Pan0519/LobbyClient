using UnityEngine.UI;
using UnityEngine;
using UniRx;
using CommonService;
using Shop.LimitTimeShop;
using CommonILRuntime.BindingModule;
using System;
using Services;
using Shop;
using Service;
using CommonPresenter;
using GoldenEgg;

namespace Lobby
{
    class LobbyTopBarPresenter : TopBarBasePresenter
    {
        public override string objPath => "prefab/lobby/lobby_top_bar";
        protected override BackHideBehaviour hideBehaviour => BackHideBehaviour.HideMe;

        #region UIs
        Button aboutBtn;
        Button headBtn;
        Image headImg;
        GameObject headTip;
        #endregion

        IDisposable headImageDispos;
        public override void initUIs()
        {
            base.initUIs();
            aboutBtn = getBtnData("about_btn");
            headBtn = getBtnData("head_btn");
            headImg = getImageData("head_img");
            headTip = getGameObjectData("haed_tip");
        }

        public override void init()
        {
            base.init();
            setMaxObjActive();
            setBGImgSprite(playerInfo.hasHighRollerPermission);
            playerInfo.checkHighRollerPermissionSub.Subscribe(setBGImgSprite).AddTo(uiGameObject);
            headImageDispos = playerInfo.headImageSubject.Subscribe(setHeadImage);
            playerInfo.callHeadChanged();
            headBtn.onClick.AddListener(openPlayerInfoPage);
            updateGetBonusTime();
            initExpBar();
        }

        async void setMaxObjActive()
        {
            var eggData = await AppManager.lobbyServer.getCoinBank();
            bool isMax = eggData.highPool.amount >= eggData.highPool.maximum || eggData.lowPool.amount >= eggData.lowPool.maximum;
            UiManager.bindNode<LobbyGoldenNode>(goldenPresenter.cachedGameObject).setEggMaxActive(isMax);
        }

        async void updateGetBonusTime()
        {
            var bonusTime = await AppManager.lobbyServer.getBouns();
            DataStore.getInstance.dataInfo.setAfterBonusTime(bonusTime.availableAfter);
        }

        public void initExpBar()
        {
            if (!string.IsNullOrEmpty(expBarTween))
            {
                TweenManager.tweenKill(expBarTween);
            }
            PlayerInfo playerInfo = DataStore.getInstance.playerInfo;
            setLvUpExp(playerInfo.LvUpExp);
            previousExp = playerInfo.playerExp;
            setPlayerExpValue(playerInfo.playerExp);
        }

        void setHeadImage(Sprite headSprite)
        {
            headImg.sprite = headSprite;
        }

        public override void openSettingPage()
        {
            UiManager.getPresenter<LobbySettingPresneter>().open();
            base.openSettingPage();
        }

        public override void openOptionList()
        {
            base.openOptionList();
            lobbyTempOpenClick();
        }

        void openPlayerInfoPage()
        {
            audioManagerPlay(BasicCommonSound.InfoBtn);
            UiManager.getPresenter<PlayerInfoPage.PlayerInfoPresenter>().open();
            closeOptionListObj();
        }

        bool isClosedShopMain = true;
        IDisposable isClosedShopDis;
        public override void openShopPage()
        {
            if (!isClosedShopMain)
            {
                return;
            }
            base.openShopPage();
            ShopMainPresenter shopMain = UiManager.getPresenter<ShopMainPresenter>();
            UtilServices.disposeSubscribes(isClosedShopDis);
            isClosedShopDis = shopMain.isCloseSub.Subscribe(value =>
             {
                 isClosedShopMain = value;
             });
            shopMain.open();
            closeOptionListObj();
        }

        public override void countdownBonusTime(TimeSpan timeSpan)
        {
            if (timeSpan <= TimeSpan.Zero)
            {
                updateGetBonusTime();
            }
        }

        public override void openLimitShop()
        {
            base.openLimitShop();
            LimitTimeShopManager.getInstance.openLimitTimeFirstPage();
        }

        public override void destory()
        {
            UtilServices.disposeSubscribes(headImageDispos);
            base.destory();
        }
    }

    public class LobbyGoldenNode : GoldenTopBarNode
    {

        public override void openGoldenEgg()
        {
            base.openGoldenEgg();
            UiManager.getPresenter<GoldenEggMainPresenter>().open();
        }
    }
}
