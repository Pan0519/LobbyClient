using CommonILRuntime.BindingModule;
using CommonILRuntime.Module;
using Lobby.Common;
using UnityEngine;
using UnityEngine.UI;
using Binding;
using Shop;
using LobbyLogic.Common;
using Service;
using Services;
using UniRx;
using System;
using System.Collections.Generic;
using CommonService;
using LobbyLogic.Audio;
using LobbyLogic.NetWork.ResponseStruct;
using UnityEngine.Purchasing;
using CommonPresenter;

namespace HighRoller
{
    class HighRollerVaultPresenter : SystemUIBasePresenter
    {
        public override string objPath
        {
            get
            {
                return UtilServices.getOrientationObjPath("prefab/diamond_club/dc_vault");
            }
        }
        public override UiLayer uiLayer { get => UiLayer.System; }

        GameObject lockObj;
        Text backTimeTxt;
        Text backCoinTxt;
        RectTransform buyBtnsGroup;
        Button closeBtn;
        BindingNode buyBtnNode;

        DateTime backTime;
        TimerService vaultTimeService = new TimerService();
        IDisposable initItemDis;
        public override void initContainerPresenter()
        {
            resOrder = new string[] { AssetBundleData.getBundleName(BundleType.Crown) };
            base.initContainerPresenter();
        }
        public override void initUIs()
        {
            lockObj = getGameObjectData("lock_obj");
            backCoinTxt = getTextData("back_coin_txt");
            backTimeTxt = getTextData("back_time_txt");
            buyBtnsGroup = getRectData("buy_btn_group");
            closeBtn = getBtnData("close_btn");
            buyBtnNode = getNodeData("buy_btn_node");
        }
        public override void init()
        {
            base.init();
            closeBtn.onClick.AddListener(closeBtnClick);
            buyBtnNode.cachedGameObject.setActiveWhenChange(false);
            IAPSDKServices.instance.receiptSub.Subscribe(receiptSubscribe).AddTo(uiGameObject);
            IAPSDKServices.instance.iapFailed.Subscribe(iapFailed).AddTo(uiGameObject);
            initItemDis = IAPSDKServices.instance.initProducts.Subscribe(initItems).AddTo(uiGameObject);
            IAPSDKServices.instance.init();
        }

        public async void setUserRecord(HighRollerUserRecordResponse record)
        {
            backCoinTxt.text = "0";
            backTime = UtilServices.strConvertToDateTime(record.vault.expiredAt, DateTime.MinValue);
            CompareTimeResult compareTimeResult = UtilServices.compareTimeWithNow(backTime);
            if (CompareTimeResult.Later == compareTimeResult)
            {
                var returnToPayResponse = await AppManager.lobbyServer.getCurrentReturnToPay();
                backCoinTxt.text = returnToPayResponse.highRoller.getReturnToPay.ToString("N0");
                startCountVaultTime();
                return;
            }
            lockObj.setActiveWhenChange(true);
            updateVaultTimeStr(backTime.Subtract(UtilServices.nowTime));
        }

        void updateVaultTimeStr(TimeSpan updateTime)
        {
            TimeStruct backTimeStruct = UtilServices.toTimeStruct(updateTime);
            if (updateTime <= TimeSpan.Zero)
            {
                lockObj.setActiveWhenChange(true);
                vaultTimeService.ExecuteTimer();
            }
            backTimeTxt.text = backTimeStruct.toTimeString(LanguageService.instance.getLanguageValue("Time_Days"));
        }

        async void initItems(Product[] platformProducts)
        {
            initItemDis.Dispose();
            var vaultStore = await AppManager.lobbyServer.getHighRollerStore();
            List<StoreItemData> storeItems = StoreItemServices.convertProductToStoreItem(vaultStore.products);
            for (int i = 0; i < storeItems.Count; ++i)
            {
                var buyNode = UiManager.bindNode<BuyBtnNode>(GameObject.Instantiate(buyBtnNode.cachedGameObject, buyBtnsGroup));
                buyNode.setProductData(storeItems[i]);
                buyNode.buyClickSub.Subscribe(buyItemClickSub).AddTo(buyNode.uiGameObject);
            }
        }
        StoreItemData buyItemData = null;
        async void buyItemClickSub(StoreItemData clickItemData)
        {
            buyItemData = await StoreItemServices.sendBuyItem(clickItemData);
        }

        async void receiptSubscribe(string receipt)
        {
            if (null == buyItemData)
            {
                return;
            }
            if (ApplicationConfig.environment == ApplicationConfig.Environment.Prod)
            {
                AppsFlyerSDKService.instance.sendPurchaseEvent(buyItemData.product.productId);
            }

            CommonRewardsResponse redeemResponse = await StoreItemServices.receiptSubscribe(receipt, buyItemData);
            List<PurchaseInfoData> infoDatas = PurchaseInfoCover.rewardConvertToPurchase(redeemResponse.rewards);
            UiManager.getPresenter<PurchasePagePresenter>().openPurchase(redeemResponse.rewards);
            await HighRollerDataManager.instance.getHighUserRecordAndCheck();
            var vaultInfoData = infoDatas.Find(info => info.itemKind == PurchaseItemType.HighRollerVault);
            if (null != vaultInfoData)
            {
                vaultTimeService.ExecuteTimer();
                var record = HighRollerDataManager.instance.userRecord;
                backTime = UtilServices.strConvertToDateTime(record.vault.expiredAt, DateTime.MinValue);
                startCountVaultTime();
            }
        }

        void startCountVaultTime()
        {
            lockObj.setActiveWhenChange(false);
            TimeSpan totalTime = backTime.Subtract(UtilServices.nowTime);
            updateVaultTimeStr(totalTime);
            TimeStruct timeStruct = UtilServices.toTimeStruct(totalTime);
            if (timeStruct.days <= 1)
            {
                vaultTimeService.setAddToGo(uiGameObject);
                vaultTimeService.StartTimer(backTime, updateVaultTimeStr);
            }
        }

        void iapFailed(string errorMsg)
        {
            StoreItemServices.iapFailed(errorMsg, buyItemData.orderID);
        }

        public override void animOut()
        {
            GamePauseManager.gameResume();
            IAPSDKServices.instance.clearSubscribes();
            clear();
        }
    }

    class BuyBtnNode : NodePresenter
    {
        CustomBtn buyBtn;
        RectTransform daysGroup;
        Text daysTxt;
        Text priceTxt;
        GameObject tapLightObj;
        Button detailBtn;

        List<PurchaseInfoData> infoDatas;
        StoreItemData storeItemData;
        public Subject<StoreItemData> buyClickSub = new Subject<StoreItemData>();

        public override void initUIs()
        {
            buyBtn = getCustomBtnData("buy_btn");
            daysGroup = getRectData("days_group");
            daysTxt = getTextData("days_txt");
            priceTxt = getTextData("price_txt");
            tapLightObj = getGameObjectData("tap_light_obj");
            detailBtn = getBtnData("detail_btn");
        }

        public override void init()
        {
            buyBtn.pointerDownHandler = buyBtnDown;
            buyBtn.pointerUPHandler = buyBtnUp;
            buyBtn.clickHandler = buyClick;
            detailBtn.onClick.AddListener(openAdditionalItem);
        }

        void buyClick()
        {
            buyClickSub.OnNext(storeItemData);
        }

        void buyBtnDown()
        {
            tapLightObj.setActiveWhenChange(true);
        }

        void buyBtnUp()
        {
            tapLightObj.setActiveWhenChange(false);
        }

        public void setProductData(StoreItemData itemData)
        {
            storeItemData = itemData;
            StoreProduct itemProduct = itemData.product;
            infoDatas = PurchaseInfoCover.rewardConvertToPurchase(itemProduct.additions);
            daysTxt.text = (itemProduct.getAmount / 1440).ToString();
            priceTxt.text = IAPSDKServices.instance.substringPriceTxt(itemData.platformProduct.metadata.localizedPriceString);
            LayoutRebuilder.ForceRebuildLayoutImmediate(daysGroup);
            open();
        }

        void openAdditionalItem()
        {
            AudioManager.instance.playAudioOnce(CommonAudioPathProvider.getAudioPath(BasicCommonSound.InfoBtn));
            UiManager.getPresenter<AdditionalItemInfos>().openItemInfos(infoDatas);
        }
    }
}
