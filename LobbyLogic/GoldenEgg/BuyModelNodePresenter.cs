using UnityEngine;
using UnityEngine.UI;
using CommonILRuntime.Module;
using CommonILRuntime.BindingModule;
using Shop;
using System;
using System.Collections.Generic;
using Service;
using Services;
using LobbyLogic.Audio;
using CommonService;

namespace GoldenEgg
{
    class BuyModelNodePresenter : NodePresenter
    {
        #region UIs
        GameObject maxObj;
        CustomBtn buyBtn;
        Text priceText;
        Button detailBtn;
        #endregion

        Action openChooseAction;
        ItemProductData itemData;
        List<PurchaseInfoData> infoDatas;

        public override void initUIs()
        {
            maxObj = getGameObjectData("max_obj");
            buyBtn = getCustomBtnData("buy_btn");
            priceText = getTextData("price_txt");
            detailBtn = getBtnData("detail_btn");
        }

        public override void init()
        {
            detailBtn.onClick.AddListener(openAdditionalItem);
            buyBtn.clickHandler = openChoose;
            maxObj.setActiveWhenChange(false);
            priceText.text = string.Empty;
        }

        public void setBuyBtnEnable(bool enable)
        {
            buyBtn.interactable = enable;
        }

        public BuyModelNodePresenter setOpenCallback(Action openCallback)
        {
            openChooseAction = openCallback;
            return this;
        }

        public void setItemData(ItemProductData itemData)
        {
            this.itemData = itemData;
            priceText.text = IAPSDKServices.instance.substringPriceTxt(itemData.productData.metadata.localizedPriceString);
            infoDatas = PurchaseInfoCover.rewardConvertToPurchase(itemData.serverProductData.rewards);
            maxObj.setActiveWhenChange(itemData.eggData.amount >= itemData.eggData.maximum);
        }

        void openAdditionalItem()
        {
            AudioManager.instance.playAudioOnce(CommonAudioPathProvider.getAudioPath(BasicCommonSound.InfoBtn));
            UiManager.getPresenter<AdditionalItemInfos>().openItemInfos(infoDatas);
        }

        void openChoose()
        {
            if (null != openChooseAction)
            {
                openChooseAction();
            }
        }
    }
}
