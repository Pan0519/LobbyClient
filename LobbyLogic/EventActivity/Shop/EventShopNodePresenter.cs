using CommonILRuntime.Module;
using UnityEngine.UI;
using UnityEngine;
using System;
using CommonILRuntime.BindingModule;
using Shop;
using System.Collections.Generic;
using Service;
using Services;
using UniRx;
using CommonILRuntime.Outcome;
using Lobby.Common;
using LobbyLogic.NetWork.ResponseStruct;
using CommonILRuntime.SpriteProvider;
using Event.Common;
using EventActivity;

namespace Event.Shop
{
    class EventShopNodePresenter : NodePresenter
    {
        #region [UIs]
        public Text itemEffectTxt;
        public Text rewradCoinTxt;
        public Text priceTxt;
        public CustomTextSizeChange ticketAmountCount;

        public CustomBtn buyBtn;
        public GameObject tapLightObj;
        public Button detailBtn;
        #endregion
        #region WasUIs
        Text wasEffectTxt;
        Text wasRewardTxt;
        Text wasPriceTxt;
        GameObject effectMoreObj;
        GameObject rewardMoreObj;
        Text effectMoreTxt;
        Text rewardMoreTxt;
        GameObject onSaleObj;
        GameObject wasRewardObj;
        GameObject wasPriceObj;
        #endregion
        public StoreItemData storeItem { get; private set; }
        public Subject<EventShopNodePresenter> selectNodePresenter = new Subject<EventShopNodePresenter>();
        public Action handlerRedeem { get; set; } = null;

        List<Reward> additions = new List<Reward>();
        BoosterType selfBoosterType;
        string unitStr = string.Empty;
        SaleType nowSaleType;

        float productIncrementPrecent = 0;
        
        public override void initUIs()
        {
            itemEffectTxt = getTextData("item_effect_txt");
            rewradCoinTxt = getTextData("reward_coin_txt");
            priceTxt = getTextData("price_txt");
            ticketAmountCount = getBindingData<CustomTextSizeChange>("reward_ticket_txt");
            buyBtn = getCustomBtnData("buy_btn");
            tapLightObj = getGameObjectData("tap_light_obj");
            detailBtn = getBtnData("detail_btn");

            wasEffectTxt = getTextData("item_effect_txt_was");
            wasRewardTxt = getTextData("reward_coin_txt_was");
            wasPriceTxt = getTextData("price_txt_was");
            effectMoreObj = getGameObjectData("effect_sale_group");
            rewardMoreObj = getGameObjectData("money_sale_group");
            effectMoreTxt = getTextData("effect_sale_txt");
            rewardMoreTxt = getTextData("money_sale_txt");
            onSaleObj = getGameObjectData("sale_obj");
            wasRewardObj = getGameObjectData("reward_was_group");
            wasPriceObj = getGameObjectData("price_was_group");
        }

        public override void init()
        {
            buyBtn.clickHandler = buyClick;
            buyBtn.pointerDownHandler = () => { openTapLightObj(true); };
            buyBtn.pointerUPHandler = () => { openTapLightObj(false); };
            detailBtn.onClick.AddListener(openAdditionPage);
        }
        public void setBoosterType(BoosterType boosterType)
        {
            selfBoosterType = boosterType;
        }

        public void setStoreData(StoreItemData itemData, SaleType saleType, bool isFirstPurchase)
        {
            additions.Clear();
            nowSaleType = saleType;
            storeItem = itemData;
            priceTxt.text = IAPSDKServices.instance.substringPriceTxt(itemData.platformProduct.metadata.localizedPriceString);
            if (SaleType.Increment == nowSaleType)
            {
                wasRewardTxt.text = storeItem.product.getAmount.ToString("N0");
                int incrementValue = 0;
                if (storeItem.product.boosts.TryGetValue("activity-product", out incrementValue))
                {
                    effectMoreTxt.text = $"{incrementValue}%";
                    productIncrementPrecent = (incrementValue / 100.0f) + 1;
                }

                if (storeItem.product.boosts.TryGetValue("activity-coin", out incrementValue))
                {
                    rewardMoreTxt.text = $"{incrementValue}%";
                    float coinIncrementPrecent = (incrementValue / 100.0f) + 1;
                    rewradCoinTxt.text = (storeItem.product.getAmount * coinIncrementPrecent).ToString("N0");
                }
            }
            else
            {
                rewradCoinTxt.text = storeItem.product.getAmount.ToString("N0");
            }
            for (int i = 0; i < itemData.product.additions.Length; ++i)
            {
                Reward reward = itemData.product.additions[i];
                if (reward.kind.StartsWith("activity"))
                {
                    setActivityAdditions(reward);
                    continue;
                }
                additions.Add(reward);
            }

            setNowSaleType(isFirstPurchase);
        }

        async void setNowSaleType(bool isFirstPurchase)
        {
            bool isDiscount = SaleType.Discount == nowSaleType;
            onSaleObj.setActiveWhenChange(isDiscount || isFirstPurchase);
            wasPriceObj.setActiveWhenChange(isDiscount);
            if (isDiscount)
            {
                string wasProductID = await ShopProductDiscountManager.getNormalID(storeItem.product.productId);
                var wasProduct = IAPSDKServices.instance.getMatchProduct(wasProductID);
                wasPriceTxt.text = IAPSDKServices.instance.substringPriceTxt(wasProduct.metadata.localizedPriceString);
            }

            bool isIncrement = SaleType.Increment == nowSaleType;
            effectMoreObj.setActiveWhenChange(isIncrement);
            rewardMoreObj.setActiveWhenChange(isIncrement);
            wasRewardObj.setActiveWhenChange(isIncrement);
            wasEffectTxt.gameObject.setActiveWhenChange(isIncrement);
        }

        public virtual void setActivityAdditions(Reward reward)
        {
            if (reward.kind.Equals("activity-prop"))
            {
                ticketAmountCount.text = reward.amount.ToString();
                return;
            }

            if (reward.type.EndsWith("prop") || reward.type.EndsWith("coin"))
            {
                unitStr = LanguageService.instance.getLanguageValue("Goods_EffectUnit_1");
            }

            if (reward.type.EndsWith("item"))
            {
                unitStr = LanguageService.instance.getLanguageValue("Goods_EffectUnit_2");
            }
            itemEffectTxt.text = $"{reward.amount} {unitStr}";
            if (SaleType.Increment == nowSaleType && productIncrementPrecent > 0)
            {
                wasEffectTxt.text = $"{reward.getAmount() / productIncrementPrecent} {unitStr}";
            }
        }

        async void buyClick()
        {
            storeItem = await StoreItemServices.sendBuyItem(storeItem);
            selectNodePresenter.OnNext(this);
        }

        public virtual void handlerRedeemResponse(CommonRewardsResponse response)
        {
            var page = UiManager.getPresenter<PurchasePagePresenter>();
            page.changeUILayout(UiLayer.System);
            page.setActivityBoosterData(selfBoosterType, unitStr);
            page.openPurchase(response.rewards);

            var propReward = Array.Find(response.rewards, reward => reward.kind.Equals("activity-prop"));
            if (null != propReward)
            {
                var totalCount = (int)propReward.outcome.bag["amount"];
                ActivityDataStore.updateTotalTicketCount(totalCount);
            }

            if (null != handlerRedeem)
            {
                handlerRedeem();
            }
        }

        void openTapLightObj(bool isOpen)
        {
            tapLightObj.transform.localScale = buyBtn.transform.localScale;
            tapLightObj.setActiveWhenChange(isOpen);
        }

        void openAdditionPage()
        {
            var addItemInfo = UiManager.getPresenter<AdditionalItemInfos>();
            addItemInfo.uiRectTransform.SetParent(UiRoot.instance.systemUiRoot);
            addItemInfo.openItemInfos(PurchaseInfoCover.rewardConvertToPurchase(additions.ToArray()));
        }
    }
}
