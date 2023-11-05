using CommonILRuntime.BindingModule;
using CommonILRuntime.Module;
using UnityEngine.UI;
using UnityEngine;
using UniRx;
using Network;
using UnityEngine.Purchasing;
using Lobby.UI;
using Service;
using LobbyLogic.NetWork.ResponseStruct;
using System;
using System.Threading.Tasks;
using Services;
using CommonILRuntime.Outcome;
using CommonPresenter;

namespace GoldenEgg
{
    public class GoldenEggMainPresenter : SystemUIBasePresenter
    {
        public override string objPath
        {
            get
            {
                return UtilServices.getOrientationObjPath("prefab/golden_egg/golden_egg_main");
            }
        }
        public override UiLayer uiLayer { get { return UiLayer.GameMessage; } }
        protected override BackHideBehaviour hideBehaviour => BackHideBehaviour.CanDoBoth;

        #region UIs
        Button closeBtn;

        Text chickenPriceTxt;
        Text goosePriceTxt;
        Text precentTxt;
        Transform moneyPoint;
        #endregion

        BuyModelNodePresenter chickenNodePresenter;
        BuyModelNodePresenter gooseNodePresenter;

        ItemProductData chickenData;
        ItemProductData gooseData;

        ChooseMode chooseMode;

        string orderID = string.Empty;
        public override void initUIs()
        {
            closeBtn = getBtnData("close_btn");

            chickenPriceTxt = getTextData("first_price_txt");
            goosePriceTxt = getTextData("egg_price_txt");
            precentTxt = getTextData("precent_txt");
            moneyPoint = getGameObjectData("money_point").transform;
        }

        public override void init()
        {
            base.init();
            closeBtn.onClick.AddListener(closeBtnClick);

            UiManager.bindNode<MedalCollentPresenter>(getNodeData("medal_collect_node").cachedGameObject).open();
            chickenNodePresenter = UiManager.bindNode<BuyModelNodePresenter>(getNodeData("chicken_node").cachedGameObject).setOpenCallback(chooseChicken);
            gooseNodePresenter = UiManager.bindNode<BuyModelNodePresenter>(getNodeData("goose_node").cachedGameObject).setOpenCallback(chooseGoose);
            chickenPriceTxt.text = "0";
            goosePriceTxt.text = "0";

            IAPSDKServices.instance.initProducts.Subscribe(initItems).AddTo(uiGameObject);
            IAPSDKServices.instance.receiptSub.Subscribe(receiptSubscribe).AddTo(uiGameObject);
            IAPSDKServices.instance.iapFailed.Subscribe(iapFailed).AddTo(uiGameObject);
            IAPSDKServices.instance.init();
        }

        public override void closeEvent()
        {
            LobbyLogic.Common.GamePauseManager.gameResume();
        }

        public override void open()
        {
            BindingLoadingPage.instance.open();
            base.open();
        }

        void initItems(Product[] products)
        {
            showEggData();
        }

        async void showEggData()
        {
            GoldEggResponse response = await AppManager.lobbyServer.getCoinBank();
            chickenData = await getItemProductData(response.highPool);
            chickenPriceTxt.text = chickenData.eggData.amount.ToString("N0");
            chickenNodePresenter.setItemData(chickenData);

            gooseData = await getItemProductData(response.lowPool);
            goosePriceTxt.text = gooseData.eggData.amount.ToString("N0");
            gooseNodePresenter.setItemData(gooseData);
            setBuyBtnsEnable(true);
            BindingLoadingPage.instance.close();
        }

        async Task<ItemProductData> getItemProductData(GoldEggData data)
        {
            ProductResponse productResponse = await AppManager.lobbyServer.getProductID(data.sku);
            return new ItemProductData()
            {
                eggData = data,
                productData = IAPSDKServices.instance.getMatchProduct(productResponse.purchaseProductId),
                serverProductData = productResponse,
            };
        }

        void setBuyBtnsEnable(bool enable)
        {
            chickenNodePresenter.setBuyBtnEnable(enable);
            gooseNodePresenter.setBuyBtnEnable(enable);
        }

        void chooseChicken()
        {
            setBuyBtnsEnable(false);
            chooseMode = ChooseMode.Chicken;
            buyProduct(chickenData.eggData.sku);
        }

        void chooseGoose()
        {
            setBuyBtnsEnable(false);
            chooseMode = ChooseMode.Goose;
            buyProduct(gooseData.eggData.sku);
        }

        async void buyProduct(string sku)
        {
            BuyProductResponse productResponse = await AppManager.lobbyServer.sendStoreOrder(sku);
            orderID = productResponse.id;
            IAPSDKServices.instance.buyProduct(getChooseItemData().serverProductData.purchaseProductId, productResponse.id);
        }

        ItemProductData getChooseItemData()
        {
            switch (chooseMode)
            {
                case ChooseMode.Chicken:
                    return chickenData;

                case ChooseMode.Goose:
                    return gooseData;

                default:
                    return null;
            }
        }

        public override void animOut()
        {
            clear();
        }

        public override void clear()
        {
            IAPSDKServices.instance.clearSubscribes();
            base.clear();
        }

        async void receiptSubscribe(string receipt)
        {
            if (string.IsNullOrEmpty(orderID))
            {
                return;
            }
            if (ApplicationConfig.environment == ApplicationConfig.Environment.Prod)
            {
                AppsFlyerSDKService.instance.sendPurchaseEvent(getChooseItemData().serverProductData.purchaseProductId);
            }
                
            OnlyResultResponse receiptResponse = await AppManager.lobbyServer.patchReceipt(orderID, receipt);
            if (Result.OK != receiptResponse.result)
            {
                Debug.LogError($"receiptResponse result is error {receiptResponse.result}");
                IAPSDKServices.instance.showErrorReceipt(receipt);
                return;
            }
            var redeemResponse = await AppManager.lobbyServer.sendStoreRedeem(orderID);
            if (Result.OK != redeemResponse.result)
            {
                Debug.LogError($"redeemResponse result is error {redeemResponse.result}");
                IAPSDKServices.instance.showErrorReceipt(receipt);
                return;
            }
            getChooseItemData().commonRewards = redeemResponse.rewards;
            openChooseMode();
            IAPSDKServices.instance.confirmPendingPurchase(getChooseItemData().productData);

        }

        async void iapFailed(string errorMsg)
        {
            if (string.IsNullOrEmpty(orderID))
            {
                return;
            }
            await AppManager.lobbyServer.sendStoreCancel(orderID);
            setBuyBtnsEnable(true);
        }

        void openChooseMode()
        {
            bool isMax = false;
            switch (chooseMode)
            {
                case ChooseMode.Chicken:
                    isMax = chickenData.eggData.amount >= chickenData.eggData.maximum;
                    break;

                case ChooseMode.Goose:
                    isMax = gooseData.eggData.amount >= gooseData.eggData.maximum;
                    break;
            }
            UiManager.getPresenter<ChooseModelPresenter>().openChooseMode(chooseMode, getChooseItemData(), isMax);

            Observable.Timer(TimeSpan.FromSeconds(1.0f)).Subscribe(_ =>
            {
                showEggData();
            }).AddTo(uiGameObject);
        }
    }

    public class ItemProductData
    {
        public GoldEggData eggData;
        public Product productData;
        public ProductResponse serverProductData;
        public CommonReward[] commonRewards;
    }

    public enum ChooseMode
    {
        Chicken,
        Goose,
    }
}
