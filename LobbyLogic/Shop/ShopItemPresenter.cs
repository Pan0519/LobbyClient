using UnityEngine.UI;
using LobbyLogic.NetWork.ResponseStruct;
using System;
using Services;
using Service;
using TMPro;

namespace Shop
{
    class ShopItemPresenter : ShopItemBasePresenter
    {
        #region UIs
        Image iconImg;
        Button infoBtn;
        Text daysTxt;
        TextMeshProUGUI originalDaysTxt;
        Text priceTxt;
        #endregion

        Action<StoreItemExplanationData> openItemInfoAction = null;
        StoreItemData storeItem;
        StoreItemExplanationData explanationData;
        public override void initUIs()
        {
            base.initUIs();
            iconImg = getImageData("item_icon_img");
            daysTxt = getTextData("days_txt");
            originalDaysTxt = getBindingData<TextMeshProUGUI>("origin_days_txt");
            priceTxt = getTextData("price_txt");
            infoBtn = getBtnData("info_btn");
        }

        public override void init()
        {
            infoBtn.onClick.AddListener(openItemInfo);
            originalDaysTxt.gameObject.setActiveWhenChange(false);
            base.init();
        }

        public ShopItemPresenter setOpenItemAction(Action<StoreItemExplanationData> openItemInfoAction)
        {
            this.openItemInfoAction = openItemInfoAction;
            return this;
        }

        public void setItemInfo(StoreItemData itemData)
        {
            storeItem = itemData;
            setStoreItem(storeItem);
            StoreProduct itemProduct = storeItem.product;
            setAdditionalItemInfos(itemProduct.additions);
            showLabelsImage(itemProduct.labels);

            PurchaseItemType itemType = PurchaseInfo.getItemType(itemProduct.kind);
            explanationData = ShopDataStore.getItemExplanation(itemType);
            iconImg.sprite = ShopDataStore.getShopSprite(explanationData.iconSpriteName);
            daysTxt.text = UtilServices.setDays((long)itemProduct.getAmount);
            priceTxt.text = IAPSDKServices.instance.substringPriceTxt(itemData.platformProduct.metadata.localizedPriceString);
            open();
        }

        void openItemInfo()
        {
            if (null == openItemInfoAction)
            {
                return;
            }
            openItemInfoAction(explanationData);

        }
    }
}
