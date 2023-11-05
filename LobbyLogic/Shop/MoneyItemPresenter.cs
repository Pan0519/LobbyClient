using UnityEngine.UI;
using UnityEngine;
using TMPro;
using Service;
using System.Collections.Generic;
using CommonService;

namespace Shop
{
    class MoneyItemPresenter : ShopItemBasePresenter
    {
        #region UIs
        public GameObject firstBuyObj;
        public Text firstBuyTxt;
        public GameObject buyMoreObj;
        public Text buyMoreText;
        public CustomTextSizeChange itemMoneyText;
        public Text originalMoneyText;
        public Text priceText;

        //public GameObject couponObj;
        //public Text couponText;

        public GameObject rightIcon;
        Animator moneyAnim;
        Image moneyLvImg;
        #endregion

        string couponId = string.Empty;

        public StoreItemData storeItem { get; private set; }

        public override void initUIs()
        {
            firstBuyObj = getGameObjectData("first_buy_obj");
            firstBuyTxt = getTextData("first_buy_txt");

            buyMoreObj = getGameObjectData("buy_more_obj");
            buyMoreText = getTextData("buy_more_txt");

            itemMoneyText = getBindingData<CustomTextSizeChange>("item_money_txt");
            originalMoneyText = getTextData("origin_money_txt");

            priceText = getTextData("price_txt");
            rightIcon = getGameObjectData("icon_right");
            moneyAnim = getAnimatorData("money_anim");
            moneyLvImg = getImageData("money_lv_img");

            //couponObj = getGameObjectData("coupon_obj");
            //couponText = getTextData("coupon_txt");
            base.initUIs();
        }

        public override void init()
        {
            leftIconImg.gameObject.setActiveWhenChange(false);
            rightIcon.setActiveWhenChange(false);
            setCouponObjActivty(false);
            base.init();
        }

        public void setCoinInfo(StoreItemData itemData, int lvID, string couponId = "", int couponBonus = 0)
        {
            this.couponId = couponId;
            setCouponObjActivty(!string.IsNullOrEmpty(couponId));
            setCouponTxt(couponBonus);
            moneyLvImg.sprite = ShopDataStore.getShopSprite($"icon_money_{lvID}");
            storeItem = itemData;
            priceText.text = IAPSDKServices.instance.substringPriceTxt(itemData.platformProduct.metadata.localizedPriceString);
            var boost = storeItem.product.boosts;
            int boostTotalPercent = 0;
            var boostEnum = boost.GetEnumerator();
            while (boostEnum.MoveNext())
            {
                boostTotalPercent += boostEnum.Current.Value;
            }
            if (boost.Count > 0)
            {
                setRightIconActive(boost.ContainsKey(ShopDataStore.boostFirst), boostTotalPercent);
            }
            boostTotalPercent += couponBonus;
            boostTotalPercent += 100;
            ulong money = storeItem.product.getAmount * (ulong)boostTotalPercent / 100;
            itemMoneyText.text = money.ToString("N0");
            originalMoneyText.gameObject.setActiveWhenChange(boostTotalPercent > 100);
            originalMoneyText.text = string.Format(LanguageService.instance.getLanguageValue("Was"), storeItem.product.getAmount.ToString("N0"));

            setAdditionalItemInfos(itemData.product.additions);
            showLabelsImage(itemData.product.labels);
            open();
            string lvAnimTriggerName = lvID <= 3 ? "small" : "big";
            moneyAnim.SetTrigger(lvAnimTriggerName);
            setStoreItem(storeItem);
        }

        bool withCoupon()
        {
            return !string.IsNullOrEmpty(couponId);
        }

        void setRightIconActive(bool isFirst, int percent)
        {
            rightIcon.setActiveWhenChange(true);
            firstBuyObj.setActiveWhenChange(isFirst);
            buyMoreObj.setActiveWhenChange(!isFirst);
            if (!isFirst)
            {
                buyMoreText.text = $"+{percent}%";
                return;
            }
            firstBuyTxt.text = $"+{percent}%";
        }
    }
}

