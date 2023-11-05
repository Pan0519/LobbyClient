using CommonILRuntime.BindingModule;
using CommonILRuntime.Module;
using UnityEngine.UI;
using UnityEngine;
using Services;
using Service;
using CommonPresenter;
using LobbyLogic.NetWork.ResponseStruct;
using System.Collections.Generic;
using UnityEngine.Purchasing;
using UniRx;
using Network;
using System;
using LobbyLogic.Common;
using System.Threading.Tasks;
using CommonService;

namespace Shop.LimitTimeShop
{
    public class LimitTimeShopBasePresenter : SystemUIBasePresenter
    {
        public override UiLayer uiLayer { get => UiLayer.TopRoot; }

        #region UIs
        Button closeBtn;
        Animator limitAnim;

        Text wasMoneyTxt;
        Text newMoneyTxt;

        Text timeTxt;
        Text buyPriceTxt;
        Button buyBtn;
        Button detailBtn;
        RectTransform wasGroupRect;
        #endregion

        List<PurchaseInfoData> infoDatas = new List<PurchaseInfoData>();
        Action closeCB;
        public string orderID = string.Empty;
        public StoreProduct storeProduct;
        public Product buyProduct = null;
        TimerService timerService = new TimerService();
        TimeStruct saleTimeStruct;
        public override void initUIs()
        {
            closeBtn = getBtnData("close_btn");
            limitAnim = getAnimatorData("limit_anim");

            wasMoneyTxt = getTextData("reward_coin_txt_was");
            newMoneyTxt = getTextData("money_txt");

            buyPriceTxt = getTextData("price_txt");
            buyBtn = getBtnData("buy_btn");
            detailBtn = getBtnData("detail_btn");
            timeTxt = getTextData("discount_time_txt");
            wasGroupRect = getGameObjectData("reward_was_group").GetComponent<RectTransform>();
        }

        public override Animator getUiAnimator()
        {
            return limitAnim;
        }

        public override void init()
        {
            base.init();
            wasMoneyTxt.text = string.Empty;
            newMoneyTxt.text = string.Empty;
            buyPriceTxt.text = string.Empty;

            closeBtn.onClick.AddListener(closeBtnClick);
            detailBtn.onClick.AddListener(openAdditionPage);
            buyBtn.onClick.AddListener(buyClick);

            //UiManager.bindNode<MedalCollentPresenter>(medalCollectNode.cachedGameObject).open();
        }

        public void setCloseCB(Action closeCB)
        {
            this.closeCB = closeCB;
        }

        public override void open()
        {
            getSaleTimes();
            base.open();
        }

        public void initIAPSDK()
        {
            IAPSDKServices.instance.initProducts.Subscribe(initItems).AddTo(uiGameObject);
            IAPSDKServices.instance.receiptSub.Subscribe(receiptSubscribe).AddTo(uiGameObject);
            IAPSDKServices.instance.iapFailed.Subscribe(iapFailed).AddTo(uiGameObject);
            IAPSDKServices.instance.init();
        }

        void getSaleTimes()
        {
            timerService.setAddToGo(uiGameObject);
            timerService.StartTimer(DataStore.getInstance.limitTimeServices.getLimitEndTime(), updateSaleTime);
        }

        void updateSaleTime(TimeSpan endTime)
        {
            if (endTime <= TimeSpan.Zero)
            {
                timerService.ExecuteTimer();
                DataStore.getInstance.limitTimeServices.limitSaleTimeFinish();
                closePresenter();
                return;
            }

            saleTimeStruct = UtilServices.toTimeStruct(endTime);
            timeTxt.text = saleTimeStruct.toTimeString(LanguageService.instance.getLanguageValue("Time_Days"));
        }

        public virtual void initItems(Product[] products)
        {

        }

        public void setPriceTxt()
        {
            buyPriceTxt.text = IAPSDKServices.instance.substringPriceTxt(buyProduct.metadata.localizedPriceString);
        }

        async void buyClick()
        {
            if (null == storeProduct)
            {
                return;
            }

            BuyProductResponse productResponse = await AppManager.lobbyServer.sendStoreOrder(storeProduct.sku);
            orderID = productResponse.id;
            IAPSDKServices.instance.buyProduct(storeProduct.productId, orderID);
        }

        async void receiptSubscribe(string receipt)
        {
            if (string.IsNullOrEmpty(orderID) || null == buyProduct)
            {
                return;
            }
            if (ApplicationConfig.environment == ApplicationConfig.Environment.Prod)
            {
                AppsFlyerSDKService.instance.sendPurchaseEvent(storeProduct.productId);
            }

            OnlyResultResponse receiptResponse = await AppManager.lobbyServer.patchReceipt(orderID, receipt);
            if (Result.OK != receiptResponse.result)
            {
                IAPSDKServices.instance.showErrorReceipt(receipt);
                return;
            }
            var redeemResponse = await AppManager.lobbyServer.sendStoreRedeem(orderID);
            if (Result.OK != redeemResponse.result)
            {
                IAPSDKServices.instance.showErrorReceipt(receipt);
                return;
            }
            DataStore.getInstance.limitTimeServices.limitSaleFinish();
            DataStore.getInstance.limitTimeServices.setHasLimitData(false);
            handlerRedeemResponse(redeemResponse);
            IAPSDKServices.instance.confirmPendingPurchase(buyProduct);
        }

        void handlerRedeemResponse(CommonRewardsResponse response)
        {
            closePresenter();
            UiManager.getPresenter<PurchasePagePresenter>().openPurchase(response.rewards);
        }

        async void iapFailed(string errorMsg)
        {
            if (string.IsNullOrEmpty(orderID))
            {
                return;
            }
            await AppManager.lobbyServer.sendStoreCancel(orderID);
        }

        public void setInfoDatas(List<PurchaseInfoData> datas)
        {
            infoDatas = datas;
        }

        void openAdditionPage()
        {
            UiManager.getPresenter<AdditionalItemInfos>().openItemInfos(infoDatas);
        }

        public async void setMoneyTxt(ulong wasMoney, ulong newMoney)
        {
            newMoneyTxt.text = newMoney.ToString("N0");
            wasMoneyTxt.text = wasMoney.ToString("N0");
            await Task.Delay(TimeSpan.FromSeconds(0.3f));
            LayoutRebuilder.ForceRebuildLayoutImmediate(newMoneyTxt.transform.parent.GetComponent<RectTransform>());
            LayoutRebuilder.ForceRebuildLayoutImmediate(wasGroupRect);
        }

        public override void animOut()
        {
            clear();
            IAPSDKServices.instance.clearSubscribes();
            if (null != closeCB)
            {
                closeCB();
            }
        }

        public override void clear()
        {
            GamePauseManager.gameResume();
            base.clear();
        }
    }


}
