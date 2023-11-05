using CommonILRuntime.BindingModule;
using CommonILRuntime.Module;
using Binding;
using UnityEngine.UI;
using UnityEngine;
using System.Collections.Generic;
using CommonService;
using UniRx;
using System;
using Services;
using UnityEngine.Purchasing;
using Service;
using LobbyLogic.NetWork.ResponseStruct;
using Network;
using Lobby.UI;
using CommonILRuntime.Services;
using Lobby.Common;
using CommonPresenter;
using LobbyLogic.Audio;
using HighRoller;
using System.Threading.Tasks;

namespace Shop
{
    enum SelectBtn
    {
        Coin,
        Item,
    }

    class CouponInfo
    {
        public string id;
        public int bonus;
        public DateTime expiredAt;  //先記錄下來，若有需要日後可以使用
    }

    public class ShopMainPresenter : SystemUIBasePresenter
    {
        public override string objPath => "prefab/lobby_shop/shop_main";
        public override UiLayer uiLayer { get { return UiLayer.GameMessage; } }
        protected override BackHideBehaviour hideBehaviour => BackHideBehaviour.CanDoBoth;

        #region Marquee
        readonly string DiamondClubKey = "Store_Billboard_DiamondClub";
        readonly string FirstPurchaseKey = "Store_Billboard_FirstPurchase";
        List<string> marqueeMsgs = new List<string>();
        int marqueeMsgID = 0;
        #endregion

        #region UIs
        Button closeBtn;
        CustomTextSizeChange giftNumText;
        Button tipsBtn;

        Button coin_btn;
        Button item_btn;
        RectTransform giftRect;
        Button giftBtn;
        Animator giftAnim;
        GameObject giftRoot;
        //Animator boxGroupAnim;
        //Image giftLightImg;

        RectTransform buffGroupTrans;
        Transform playerMoneyPoint;

        List<Text> marqueeMsgTxts = new List<Text>();

        BindingNode holeBuffNode;
        BindingNode moneyItemNode;
        BindingNode itemNode;
        BindingNode itemInfoNode;
        Button infoTapBtn;

        ScrollRect shopScroll;
        RectTransform shopScrollRect;
        GameObject scrollDivider;

        GameObject giftBoosterObj;
        BindingNode medalNode;
        #endregion

        public Subject<bool> isCloseSub { get; private set; } = new Subject<bool>();

        List<IDisposable> storeSubscribe = new List<IDisposable>();
        List<IDisposable> itemBuySubscribe = new List<IDisposable>();
        //ShopInfoPresenter infoPresenter;
        ItemInfoPresenter itemInfoPresenter;
        float coinItemCount { get { return coinProducts.Count; } }
        float itemCount { get { return itemProducts.Count; } }

        float scrollItemWidth;
        float scrollTotalWidth;
        string shopScrollTween;

        bool bonusTimeOver;

        string giftNumTween;
        StoreItemData buyItemData = null;

        TimerService timerService = new TimerService();
        StoreBouns bonus;

        List<StoreItemData> coinProducts = new List<StoreItemData>();
        List<StoreItemData> itemProducts = new List<StoreItemData>();

        Dictionary<string, Action<StoreItemData>> sortStoreItems = new Dictionary<string, Action<StoreItemData>>();
        Dictionary<BuffState, HoldBuffPresenter> buffPresenters = new Dictionary<BuffState, HoldBuffPresenter>();
        List<ShopItemBasePresenter> shopItemBasePresenters = new List<ShopItemBasePresenter>();
        public Dictionary<StoreKind, List<PoolObject>> itemPools { get; private set; } = new Dictionary<StoreKind, List<PoolObject>>();
        MedalCollentPresenter medalCollentPresenter;

        CouponInfo selectedCoupon = null;
        IDisposable initItemDis;

        public override void initUIs()
        {
            closeBtn = getBtnData("close_btn");
            giftNumText = getBindingData<CustomTextSizeChange>("gift_num_txt");
            tipsBtn = getBtnData("tips_btn");

            coin_btn = getBtnData("coin_selec_btn");
            item_btn = getBtnData("item_select_btn");

            buffGroupTrans = getBindingData<RectTransform>("buff_group");
            playerMoneyPoint = getGameObjectData("money_point").transform;

            marqueeMsgTxts.Add(getTextData("marquee_txt_1"));
            marqueeMsgTxts.Add(getTextData("marquee_txt_2"));

            holeBuffNode = getNodeData("hold_buff_node");
            moneyItemNode = getNodeData("money_item_node");
            itemNode = getNodeData("item_node");
            itemInfoNode = getNodeData("item_info_node");
            infoTapBtn = getBtnData("info_tap_btn");

            shopScroll = getBindingData<ScrollRect>("shop_scroll");
            scrollDivider = getGameObjectData("shop_divider");

            giftBtn = getBtnData("gift_btn");
            giftAnim = getAnimatorData("gift_anim");
            giftRoot = getGameObjectData("buy_icon_obj");
            //giftLightImg = getImageData("gift_img");
            giftRect = getBindingData<RectTransform>("gift_layout_trans");
            giftBoosterObj = getGameObjectData("gift_booster_obj");
            medalNode = getNodeData("medal_collect_node");
        }
        public override void init()
        {
            for (int i = 0; i < marqueeMsgTxts.Count; ++i)
            {
                marqueeMsgTxts[i].text = string.Empty;
            }

            base.init();
            timerService.setAddToGo(uiGameObject);
            shopScrollRect = shopScroll.GetComponent<RectTransform>();
            DataStore.getInstance.playerMoneyPresenter.addTo(playerMoneyPoint);
            giftRoot.setActiveWhenChange(false);
            sortStoreItems.Add(ShopDataStore.StoreCoinKind, sortCoinItem);
            sortStoreItems.Add(ShopDataStore.StoreItemKind, sortItem);

            itemPools.Add(StoreKind.Coin, new List<PoolObject>());
            itemPools.Add(StoreKind.Divider, new List<PoolObject>());
            itemPools.Add(StoreKind.Item, new List<PoolObject>());

            IAPSDKServices.instance.receiptSub.Subscribe(receiptSubscribe).AddTo(uiGameObject);
            IAPSDKServices.instance.iapFailed.Subscribe(iapFailed).AddTo(uiGameObject);
            initItemDis = IAPSDKServices.instance.initProducts.Subscribe(initItems).AddTo(uiGameObject);
            IAPSDKServices.instance.init();

            tipsBtn.onClick.AddListener(openTipsInfoPage);
            closeBtn.onClick.AddListener(closeBtnClick);

            coin_btn.onClick.AddListener(selectCoin);
            item_btn.onClick.AddListener(selectItem);
            giftBtn.onClick.AddListener(getGift);

            infoTapBtn.onClick.AddListener(closeHoldBuff);
            infoTapBtn.gameObject.setActiveWhenChange(false);

            changeSelectBtns(SelectBtn.Coin);
            shopScroll.normalizedPosition = new Vector2(0, 1.0f);
            medalCollentPresenter = UiManager.bindNode<MedalCollentPresenter>(medalNode.cachedGameObject);
            updateMedals();

            scrollItemWidth = moneyItemNode.cachedRectTransform.sizeDelta.x;
            scrollTotalWidth = shopScroll.content.sizeDelta.x;

            scrollDivider.setActiveWhenChange(false);
            moneyItemNode.cachedGameObject.setActiveWhenChange(false);
            itemNode.cachedGameObject.setActiveWhenChange(false);

            itemInfoNode.cachedGameObject.setActiveWhenChange(false);
            itemInfoPresenter = UiManager.bindNode<ItemInfoPresenter>(itemInfoNode.cachedGameObject);

            holeBuffNode.cachedGameObject.setActiveWhenChange(false);

            giftNumText.text = string.Empty;
            selectedCoupon = null;
            DataStore.getInstance.playerInfo.expBoostEndSubject.Subscribe(updateExpRedeemTime).AddTo(uiGameObject);
        }

        public override void closeEvent()
        {
            LobbyLogic.Common.GamePauseManager.gameResume();
        }

        public override void animOut()
        {
            IAPSDKServices.instance.clearSubscribes();
            TweenManager.tweenKill(marqueeTweenKey);
            LimitTimeShop.LimitTimeShopManager.getInstance.openLimitTimeFirstPage();
            clear();
        }

        public void updateMedals()
        {
            medalCollentPresenter.open();
        }

        public override void closePresenter()
        {
            DataStore.getInstance.playerMoneyPresenter.returnToLastParent();
            base.closePresenter();
        }

        public override void open()
        {
            AudioManager.instance.playAudioOnce(CommonAudioPathProvider.getAudioPath(BasicCommonSound.InfoBtn));
            base.open();
            BindingLoadingPage.instance.open();
            isCloseSub.OnNext(false);
        }

        public void openWithCoupon(string couponId, int bonus, DateTime expiredAt)
        {
            selectedCoupon = new CouponInfo() { id = couponId, bonus = bonus, expiredAt = expiredAt };
            open();
        }

        async void initItems(Product[] platformProducts)
        {
            var storeResponse = await AppManager.lobbyServer.getStore();
            bonus = storeResponse.bonus;
            checkGetBounsTime();
            giftBtn.interactable = bonusTimeOver;
            showShopItems(storeResponse.products);
            showBuff();
            BindingLoadingPage.instance.close();
            initItemDis.Dispose();
        }

        public RectTransform getScrollContent()
        {
            return shopScroll.content;
        }

        public virtual void showShopItems(StoreProduct[] products)
        {
            resetMarqueeMsg();
            shopItemBasePresenters.Clear();
            coinProducts.Clear();
            itemProducts.Clear();
            var storeItemDatas = StoreItemServices.convertProductToStoreItem(products);
            for (int i = 0; i < storeItemDatas.Count; ++i)
            {
                StoreItemData itemData = storeItemDatas[i];
                Action<StoreItemData> itemDataAction;
                if (sortStoreItems.TryGetValue(itemData.product.category, out itemDataAction))
                {
                    itemDataAction(itemData);
                }
            }
            var coinData = storeItemDatas.Find(storeItem => storeItem.product.category.Equals("coin"));
            if (null != coinData && coinData.product.boosts.ContainsKey("first"))
            {
                marqueeMsgs.Add(LanguageService.instance.getLanguageValue(FirstPurchaseKey));
            }
            showCoinItem();
            showItem();
            startCarouselMarqueeMsg();
        }

        void showBuff()
        {
            holeBuffNode.cachedGameObject.setActiveWhenChange(false);
            setBuffItem(BuffState.Exp, DataStore.getInstance.playerInfo.expBoostEndTime);
            setBuffItem(BuffState.Lvup, DataStore.getInstance.playerInfo.lvUpBoostEndTime);
        }

        void setBuffItem(BuffState buffState, DateTime endTime)
        {
            if (DateTime.UtcNow > endTime)
            {
                return;
            }

            HoldBuffPresenter buffPresenter;

            if (buffPresenters.TryGetValue(buffState, out buffPresenter))
            {
                buffPresenter.updateBuffTime(endTime);
                return;
            }

            PoolObject buffItem = ResourceManager.instance.getObjectFromPool(holeBuffNode.cachedGameObject, buffGroupTrans.transform);
            buffPresenter = UiManager.bindNode<HoldBuffPresenter>(buffItem.cachedGameObject).openBuff(buffState, endTime);
            storeSubscribe.Add(buffPresenter.openTimeSub.Subscribe(isOpenInfoTapBtn));
            buffPresenters.Add(buffState, buffPresenter);
        }
        HoldBuffPresenter openingHoldBuff;
        void isOpenInfoTapBtn(HoldBuffPresenter openBuffTime)
        {
            openingHoldBuff = openBuffTime;
            infoTapBtn.gameObject.setActiveWhenChange(null != openBuffTime);
        }

        void closeHoldBuff()
        {
            if (null == openingHoldBuff)
            {
                return;
            }
            openingHoldBuff.switchBuffTimeActive();
        }

        void updateExpRedeemTime(DateTime endTime)
        {
            setBuffItem(BuffState.Exp, endTime);
        }

        void checkGetBounsTime()
        {
            DateTime bounsTime = UtilServices.strConvertToDateTime(bonus.availableAfter, DateTime.UtcNow);
            if (DateTime.UtcNow < bounsTime)
            {
                bonusTimeOver = false;
                changeGiftAnim(isEnable: false);
                timerService.StartTimer(bounsTime, updateBounsTime);
                giftBoosterObj.setActiveWhenChange(false);
                return;
            }
            bonusTimeOver = true;
            changeGiftAnim(isEnable: true);
            giftNumText.text = bonus.amount.ToString("N0");
            giftBoosterObj.setActiveWhenChange(true);
            LayoutRebuilder.ForceRebuildLayoutImmediate(giftRect);
        }

        void updateBounsTime(TimeSpan bonusTime)
        {
            giftNumText.text = UtilServices.formatCountTimeSpan(bonusTime);

            if (bonusTime <= TimeSpan.Zero)
            {
                fadeGiftNumText();
            }
        }

        void sortCoinItem(StoreItemData itemData)
        {
            coinProducts.Add(itemData);
        }

        void sortItem(StoreItemData itemData)
        {
            itemProducts.Add(itemData);
        }

        void showCoinItem()
        {
            coinProducts.Sort(delegate (StoreItemData x, StoreItemData y)
            {
                return y.product.price.CompareTo(x.product.price);
            });

            List<PoolObject> pools;
            itemPools.TryGetValue(StoreKind.Coin, out pools);

            var couponId = checkUseCoupon() ? selectedCoupon.id : string.Empty;
            var couponBonus = checkUseCoupon() ? selectedCoupon.bonus : 0;

            int coinCount = coinProducts.Count > 8 ? 8 : coinProducts.Count;
            for (int i = 0; i < coinCount; ++i)
            {
                PoolObject moneyItem = ResourceManager.instance.getObjectFromPool(moneyItemNode.cachedGameObject, shopScroll.content);
                moneyItem.cachedGameObject.setActiveWhenChange(true);
                MoneyItemPresenter moneyItemPresenter = UiManager.bindNode<MoneyItemPresenter>(moneyItem.cachedGameObject);
                itemBuySubscribe.Add(moneyItemPresenter.buySubscribe.Subscribe(setBuyItemData));
                moneyItemPresenter.setCoinInfo(coinProducts[i], coinProducts.Count - i - 1, couponId, couponBonus);
                pools.Add(moneyItem);
                shopItemBasePresenters.Add(moneyItemPresenter);
            }
            itemPools[StoreKind.Coin] = pools;
        }

        void showItem()
        {
            if (itemProducts.Count > 0)
            {
                addDivider();
            }
            else
            {
                coin_btn.gameObject.setActiveWhenChange(false);
                item_btn.gameObject.setActiveWhenChange(false);
            }
            List<PoolObject> pools;
            itemPools.TryGetValue(StoreKind.Item, out pools);

            var itemEnum = itemProducts.GetEnumerator();
            while (itemEnum.MoveNext())
            {
                PoolObject item = ResourceManager.instance.getObjectFromPool(itemNode.cachedGameObject, shopScroll.content);
                ShopItemPresenter itemPresenter = UiManager.bindNode<ShopItemPresenter>(item.cachedGameObject).setOpenItemAction(openItemInfo);
                itemBuySubscribe.Add(itemPresenter.buySubscribe.Subscribe(setBuyItemData));
                itemPresenter.setItemInfo(itemEnum.Current);
                pools.Add(item);
                shopItemBasePresenters.Add(itemPresenter);
            }

            itemPools[StoreKind.Item] = pools;
        }

        void setBuyItemData(StoreItemData itemData)
        {
            buyItemData = itemData;
            setItemBtnEnable(false);
        }

        void setItemBtnEnable(bool enable)
        {
            for (int i = 0; i < shopItemBasePresenters.Count; ++i)
            {
                shopItemBasePresenters[i].setBuyBtnInteractable(enable);
            }
        }

        void addDivider()
        {
            List<PoolObject> pools;
            itemPools.TryGetValue(StoreKind.Divider, out pools);
            PoolObject dividerItem = ResourceManager.instance.getObjectFromPool(scrollDivider, shopScroll.content);
            dividerItem.cachedGameObject.setActiveWhenChange(true);
            pools.Add(dividerItem);
            itemPools[StoreKind.Divider] = pools;
        }

        public void openItemInfo(StoreItemExplanationData explanationData)
        {
            itemInfoPresenter.OpenItem(explanationData);
        }

        #region SelectItems
        void selectCoin()
        {
            changeSelectBtns(SelectBtn.Coin);
            tweenScollPos(0);
        }
        void selectItem()
        {
            changeSelectBtns(SelectBtn.Item);
            float endPos = Math.Abs(scrollTotalWidth / (scrollItemWidth * itemCount));
            tweenScollPos(endPos);
        }
        void tweenScollPos(float endPosX)
        {
            if (endPosX == shopScroll.normalizedPosition.x)
            {
                return;
            }

            if (!string.IsNullOrEmpty(shopScrollTween))
            {
                TweenManager.tweenKill(shopScrollTween);
            }
            shopScrollTween = TweenManager.tweenToFloat(shopScroll.normalizedPosition.x, endPosX, (float)TimeSpan.FromSeconds(0.3f).TotalSeconds, onUpdate: (val) =>
            {
                Vector2 oldPos = shopScroll.normalizedPosition;
                oldPos.Set(val, 1.0f);
                shopScroll.normalizedPosition = oldPos;
            }, onComplete: () =>
            {
                shopScroll.normalizedPosition = new Vector2(endPosX, 1.0f);
            });
            TweenManager.tweenPlay(shopScrollTween);
        }
        void changeSelectBtns(SelectBtn select)
        {
            item_btn.interactable = SelectBtn.Coin == select;
            coin_btn.interactable = SelectBtn.Item == select;
        }

        #endregion

        #region gift
        async void getGift()
        {
            giftBtn.interactable = false;
            var getGiftResponse = await AppManager.lobbyServer.patchBouns();
            if (Result.OK != getGiftResponse.result)
            {
                giftBtn.interactable = true;
                return;
            }
            HighRollerDataManager.instance.addPassPoints(getGiftResponse.passPoints);
            CoinFlyHelper.obverseSFly(giftBtn.gameObject.GetComponent<RectTransform>(), DataStore.getInstance.playerInfo.playerMoney, getGiftResponse.wallet.getCoin(), onComplete: () =>
            {
                DataStore.getInstance.playerInfo.setExpBoostEndTime(getGiftResponse.expBoostEndedAt);
                bonus.availableAfter = getGiftResponse.availableAfter;
                DataStore.getInstance.dataInfo.setAfterBonusTime(bonus.availableAfter);
                DataStore.getInstance.playerInfo.myWallet.commitAndPush(getGiftResponse.wallet);
                fadeGiftNumText();
                Observable.Timer(TimeSpan.FromSeconds(0.5f)).Subscribe(_ =>
                {
                    HighRollerRewardManager.openReward(getGiftResponse.highRoller);
                }).AddTo(uiGameObject);
            });
        }

        void fadeGiftNumText()
        {
            float tweenTime = (float)TimeSpan.FromSeconds(1).TotalSeconds;
            giftBtn.interactable = false;
            giftNumTween = TweenManager.tweenToFloat(1, 0, tweenTime, onUpdate: changeGiftNumAlpha, onComplete: () =>
             {
                 checkGetBounsTime();
                 giftNumTween = TweenManager.tweenToFloat(0, 1, tweenTime, delayTime: 0.5f, onUpdate: changeGiftNumAlpha, onComplete: () =>
                  {
                      giftBtn.interactable = bonusTimeOver;
                      giftNumTween = string.Empty;
                  });
                 TweenManager.tweenPlay(giftNumTween);
             });
            TweenManager.tweenPlay(giftNumTween);
        }

        void changeGiftNumAlpha(float alpha)
        {
            Color textColor = giftNumText.color;
            textColor.a = alpha;
            giftNumText.color = textColor;
        }

        void changeGiftAnim(bool isEnable)
        {
            if (isEnable)
            {
                giftRoot.setActiveWhenChange(true);
                return;
            }

            giftAnim.SetTrigger("out");
            Observable.TimerFrame(30).Subscribe(_ =>
            {
                giftRoot.setActiveWhenChange(false);
            });
        }
        #endregion

        void openTipsInfoPage()
        {
            UiManager.getPresenter<ShopInfoPresenter>().shopOpen();
        }

        HighRollerBoardResultResponse boardResult;
        async void receiptSubscribe(string receipt)
        {
            boardResult = null;
            if (null == buyItemData)
            {
                Debug.LogError("get orderID is empty");
                return;
            }
            if (ApplicationConfig.environment == ApplicationConfig.Environment.Prod)
            {
                AppsFlyerSDKService.instance.sendPurchaseEvent(buyItemData.product.productId);
            }
            
            OnlyResultResponse receiptResponse;
            if (checkUseCoupon())
            {
                receiptResponse = await AppManager.lobbyServer.patchReceiptWithCoupon(buyItemData.orderID, receipt, selectedCoupon.id);
            }
            else
            {
                receiptResponse = await AppManager.lobbyServer.patchReceipt(buyItemData.orderID, receipt);
            }
            CommonRewardsResponse redeemResponse = await StoreItemServices.sendStoreRedeem(receiptResponse, buyItemData.orderID);
            if (null == redeemResponse)
            {
                IAPSDKServices.instance.showErrorReceipt(receipt);
                return;
            }

            boardResult = redeemResponse.highRoller;
            IAPSDKServices.instance.confirmPendingPurchase(buyItemData.platformProduct);
            getPurchasePage().openPurchase(redeemResponse.rewards, openHighRollerReward);
            HighRollerDataManager.instance.getHighUserRecordAndCheck();
            resetShopItems();
        }

        void openHighRollerReward()
        {
            if (null == boardResult)
            {
                return;
            }
            HighRollerRewardManager.openReward(boardResult);
        }

        PurchasePagePresenter getPurchasePage()
        {
            return UiManager.getPresenter<PurchasePagePresenter>();
        }

        async void resetShopItems()
        {
            unselectCoupon();
            returnPoolItems();
            var storeResponse = await AppManager.lobbyServer.getStore();
            showShopItems(storeResponse.products);
        }

        void iapFailed(string errorMsg)
        {
            StoreItemServices.iapFailed(errorMsg, buyItemData.orderID);
            setItemBtnEnable(true);
        }

        public override void clear()
        {
            TweenManager.tweenKill(giftNumTween);
            returnPoolItems();
            ResourceManager.instance.releasePoolWithObj(holeBuffNode.cachedGameObject);
            var buffInfo = buffPresenters.GetEnumerator();
            while (buffInfo.MoveNext())
            {
                GameObject.DestroyImmediate(buffInfo.Current.Value.uiGameObject);
            }

            UtilServices.disposeSubscribes(storeSubscribe.ToArray());

            unselectCoupon();   //關閉商城就取消選擇 coupon, 需到信箱重新選
            isCloseSub.OnNext(true);

            base.clear();
        }

        public virtual void returnPoolItems()
        {
            for (int i = 0; i < shopItemBasePresenters.Count; ++i)
            {
                shopItemBasePresenters[i].clearAdditionalItems();
            }

            var itemPoolsEnum = itemPools.GetEnumerator();
            while (itemPoolsEnum.MoveNext())
            {
                var poolList = itemPoolsEnum.Current.Value;
                for (int i = 0; i < poolList.Count; ++i)
                {
                    ResourceManager.instance.returnObjectToPool(poolList[i].cachedGameObject);
                }
                poolList.Clear();
            }

            UtilServices.disposeSubscribes(itemBuySubscribe.ToArray());
            itemBuySubscribe.Clear();
        }

        bool checkUseCoupon()
        {
            return null != selectedCoupon;
        }

        void unselectCoupon()
        {
            selectedCoupon = null;
        }
        #region Marquee
        IDisposable marqueeLoopDis;
        string marqueeTweenKey = string.Empty;
        void startCarouselMarqueeMsg()
        {
            if (DataStore.getInstance.playerInfo.hasHighRollerPermission)
            {
                marqueeMsgs.Add(string.Format(LanguageService.instance.getLanguageValue(DiamondClubKey), HighRollerDataManager.instance.accessExpireTimeStruct));
            }
            if (marqueeMsgs.Count <= 0)
            {
                return;
            }
            for (int i = 0; i < marqueeMsgTxts.Count; ++i)
            {
                marqueeMsgID = i;
                marqueeMsgTxts[i].text = marqueeMsgs[marqueeMsgID];
                if (i >= marqueeMsgs.Count - 1)
                {
                    break;
                }
            }

            if (marqueeMsgs.Count <= 1)
            {
                return;
            }
            float targetValue = 66;

            marqueeLoopDis = Observable.Timer(TimeSpan.FromSeconds(1.0f), TimeSpan.FromSeconds(5.0f)).Subscribe(repeat =>
             {
                 List<float> originalY = new List<float>();
                 for (int i = 0; i < marqueeMsgTxts.Count; ++i)
                 {
                     originalY.Add(marqueeMsgTxts[i].rectTransform.anchoredPosition.y);
                 }
                 marqueeTweenKey = TweenManager.tweenToFloat(0, targetValue, 2.0f, onUpdate: (y) =>
                  {
                      for (int i = 0; i < marqueeMsgTxts.Count; ++i)
                      {
                          setMarqueeTxt(marqueeMsgTxts[i], originalY[i] - y);
                      }
                  }, onComplete: () =>
                  {
                      var id = (int)repeat % marqueeMsgTxts.Count;
                      var marqueeTxt = marqueeMsgTxts[id];
                      setMarqueeTxt(marqueeTxt, targetValue);
                      setNextMarqueeMsg(marqueeTxt);
                      marqueeTweenKey = string.Empty;
                  });
             }).AddTo(uiGameObject);
        }

        void setMarqueeTxt(Text msgTxt, float endY)
        {
            var resetPos = msgTxt.rectTransform.anchoredPosition;
            resetPos.Set(resetPos.x, endY);
            msgTxt.rectTransform.anchoredPosition = resetPos;
        }

        void setNextMarqueeMsg(Text msgTxt)
        {
            marqueeMsgID++;
            if (marqueeMsgID >= marqueeMsgs.Count)
            {
                marqueeMsgID = 0;
            }
            msgTxt.text = marqueeMsgs[marqueeMsgID];
        }

        void resetMarqueeMsg()
        {
            UtilServices.disposeSubscribes(marqueeLoopDis);
            marqueeMsgs.Clear();
            marqueeMsgID = 0;
            for (int i = 0; i < marqueeMsgTxts.Count; ++i)
            {
                marqueeMsgTxts[i].text = string.Empty;
            }
        }
        #endregion
    }

    public class ItemInfoPresenter : NodePresenter
    {
        #region UIs
        Button closeBtn;
        Image titleImg;
        Text contentTxt;
        Image itemIcon;
        #endregion

        public override void initUIs()
        {
            closeBtn = getBtnData("close_btn");
            titleImg = getImageData("title_img");
            contentTxt = getTextData("item_content");
            itemIcon = getImageData("item_icon");
        }

        public override void init()
        {
            closeBtn.onClick.AddListener(close);
        }

        public void OpenItem(StoreItemExplanationData infoData)
        {
            itemIcon.sprite = ShopDataStore.getShopSprite(infoData.iconSpriteName);
            titleImg.sprite = ShopDataStore.getShopSprite(infoData.titleSpriteName);
            contentTxt.text = LanguageService.instance.getLanguageValue(infoData.contentKey);
            open();
        }
    }
}
