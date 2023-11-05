using CommonILRuntime.BindingModule;
using CommonILRuntime.Module;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using Lobby.Common;
using Service;
using UniRx;
using UnityEngine.Purchasing;
using Shop;
using LobbyLogic.NetWork.ResponseStruct;
using Services;
using EventActivity;
using System.Threading.Tasks;
using Lobby.UI;
using CommonPresenter;
using CommonService;

namespace Event.Shop
{
    class EventShopPresenter : SystemUIBasePresenter
    {
        public override UiLayer uiLayer { get { return UiLayer.System; } }

        #region [UIs]
        Button closeBtn;
        GameObject spinObj;
        Button spinBtn;
        GameObject discountGroupObj;
        #endregion
        bool isFirstPurchase;
        Action spinBtnEvent;
        EventShopNodePresenter selectShopNode = null;
        SaleType nowSaleType = SaleType.Normal;
        List<StoreItemData> storeItemDatas;
        Dictionary<int, EventShopNodePresenter> shopNodes = new Dictionary<int, EventShopNodePresenter>();

        public override void initUIs()
        {
            closeBtn = getBtnData("close_btn");
            spinObj = getGameObjectData("go_spin_obj");
            spinBtn = getBtnData("go_spinning_btn");
            discountGroupObj = getGameObjectData("discount_group_obj");
        }

        public virtual void setShopNodePresenter()
        {

        }

        public void openShop(bool isShowSpinObj, Action spinEvent = null)
        {
            if (ActivityDataStore.isOpenShop)
            {
                return;
            }
            ActivityDataStore.isOpenShop = true;
            BindingLoadingPage.instance.open();
            spinObj.setActiveWhenChange(isShowSpinObj);
            spinBtnEvent = spinEvent;
        }

        public override void init()
        {
            base.init();
            discountGroupObj.setActiveWhenChange(false);
            closeBtn.onClick.AddListener(closeBtnClick);
            spinBtn.onClick.AddListener(spinClick);
            ActivityDataStore.isEndErrorSub.Subscribe(setActivityEnd);
            ActivityDataStore.isEndSub.Subscribe(setActivityEnd);
            IAPSDKServices.instance.receiptSub.Subscribe(receiptSubscribe).AddTo(uiGameObject);
            IAPSDKServices.instance.iapFailed.Subscribe(iapFailed).AddTo(uiGameObject);
            IAPSDKServices.instance.initProducts.Subscribe(initItems).AddTo(uiGameObject);
            IAPSDKServices.instance.init();
        }

        bool isActivityEnd;
        bool isBuying = false;

        void setActivityEnd(bool isEnd)
        {
            isActivityEnd = isEnd;

            if (isEnd && false == isBuying)
            {
                closePresenter();
            }
        }

        async void initItems(Product[] platformProducts)
        {
            await initStoreData();
            setShopNodePresenter();
            open();
            BindingLoadingPage.instance.close();
        }

        public override async void open()
        {
            base.open();
            await Task.Delay(TimeSpan.FromSeconds(0.5f));
            ActivityDataStore.isOpenShop = false;
        }

        async Task initStoreData()
        {
            isBuying = false;
            ActivityStoreResponse response = await AppManager.lobbyServer.getActivityStore();
            isFirstPurchase = response.isFirstPurchase;
            UtilServices.enumParse<SaleType>(response.salesType, out nowSaleType);
            storeItemDatas = StoreItemServices.convertProductToStoreItem(response.products);
        }

        public override void animOut()
        {
            ActivityDataStore.isOpenShop = false;
            IAPSDKServices.instance.clearSubscribes();
            clear();
        }
        public T setBoosterItem<T>(int itemNodeID, BoosterType boosterType) where T : EventShopNodePresenter, new()
        {
            var shopNode = UiManager.bindNode<T>(getNodeData($"booster_item_node_{itemNodeID}").cachedGameObject);
            setNodeData(shopNode, storeItemDatas[itemNodeID - 1], boosterType);
            shopNodes.Add(itemNodeID, shopNode);
            return shopNode;
        }
        void setNodeData(EventShopNodePresenter presenter, StoreItemData itemData, BoosterType boosterType)
        {
            presenter.setBoosterType(boosterType);
            presenter.setStoreData(itemData, nowSaleType, isFirstPurchase);
            presenter.selectNodePresenter.Subscribe(selectShopNodePresenterSub).AddTo(uiGameObject);
        }

        void selectShopNodePresenterSub(EventShopNodePresenter selectShopNode)
        {
            this.selectShopNode = selectShopNode;
            isBuying = true;
        }

        public async void receiptSubscribe(string receipt)
        {
            if (null == selectShopNode)
            {
                return;
            }
            var redeemResponse = await StoreItemServices.receiptSubscribe(receipt, selectShopNode.storeItem);
            if (ApplicationConfig.environment == ApplicationConfig.Environment.Prod)
            {
                AppsFlyerSDKService.instance.sendPurchaseEvent(selectShopNode.storeItem.product.productId);
            }

            selectShopNode.handlerRedeemResponse(redeemResponse);
            resetItemNode();
        }
        async void resetItemNode()
        {
            if (isActivityEnd)
            {
                closePresenter();
                return;
            }

            await initStoreData();
            for (int i = 0; i < storeItemDatas.Count; ++i)
            {
                shopNodes[i + 1].setStoreData(storeItemDatas[i], nowSaleType, isFirstPurchase);
            }
        }
        void iapFailed(string errorMsg)
        {
            if (null == selectShopNode)
            {
                return;
            }
            isBuying = false;
            StoreItemServices.iapFailed(errorMsg, selectShopNode.storeItem.orderID);
            if (isActivityEnd)
            {
                closePresenter();
            }
        }

        async void spinClick()
        {
            closeBtnClick();
            if (null != spinBtnEvent)
            {
                spinBtnEvent();
            }
            if (GameOrientation.Portrait == await DataStore.getInstance.dataInfo.getNowGameOrientation())
            {
                await UIRootChangeScreenServices.Instance.justChangeScreenToProp();
            }
        }
    }

    public enum SaleType
    {
        Normal,
        Discount,
        Increment,
    }

}
