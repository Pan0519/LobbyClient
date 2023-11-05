using Service;
using Services;
using Lobby.UI;
using LitJson;
using UnityEngine.UI;
using UnityEngine;
using CommonService;

using Debug = UnityLogUtility.Debug;
using UnityEngine.Purchasing;
using System.Threading.Tasks;
using System;

namespace Shop.LimitTimeShop
{
    class LimitTimeFirstPresenter : LimitTimeShopBasePresenter
    {
        public override string objPath => UtilServices.getOrientationObjPath("prefab/limit_sale/limit_sale_first");
        protected override BackHideBehaviour hideBehaviour => BackHideBehaviour.CanDoBoth;

        Text wasPriceTxt;
        Text newPriceTxt;
        RectTransform priceParentRect;

        public override void initUIs()
        {
            base.initUIs();
            wasPriceTxt = getTextData("old_price_txt");
            newPriceTxt = getTextData("new_price_txt");

            priceParentRect = newPriceTxt.transform.parent.GetComponent<RectTransform>();
        }

        public void openPage(Action closeCallback = null)
        {
            BindingLoadingPage.instance.open();
            setCloseCB(closeCallback);
            storeProduct = LimitTimeShopManager.getInstance.firstPurchase.product;
            setInfoDatas(PurchaseInfoCover.rewardConvertToPurchase(storeProduct.additions));
            initIAPSDK();
            open();
        }

        async Task initData()
        {
            var firstSaleData = await WebRequestText.instance.loadTextFromServer("flash_first_sale");
            var saleDatas = JsonMapper.ToObject<FlashFirstSaleDatas>(firstSaleData);
            for (int i = 0; i < saleDatas.Flash_First_Sale.Length; ++i)
            {
                var saleData = saleDatas.Flash_First_Sale[i];
                if (saleData.Product_ID.Equals(storeProduct.sku))
                {
                    setUSPriceTxt(saleData.Was_Price, saleData.New_Price);
                    break;
                }
            }

            setMoneyTxt((ulong)(storeProduct.price * DataStore.getInstance.playerInfo.coinExchangeRate), storeProduct.getAmount);
        }

        public override async void initItems(Product[] products)
        {
            await initData();
            buyProduct = IAPSDKServices.instance.getMatchProduct(storeProduct.productId);
            setPriceTxt();
        }

        void setUSPriceTxt(string wasPrice, string newPrice)
        {
            wasPriceTxt.text = $"US ${wasPrice}";
            newPriceTxt.text = $"US ${newPrice}";

            LayoutRebuilder.ForceRebuildLayoutImmediate(priceParentRect);
            BindingLoadingPage.instance.close();
        }

    }

    public class FlashFirstSaleDatas
    {
        public FlashFirstSale[] Flash_First_Sale;
    }

    public class FlashFirstSale
    {
        public string Product_ID;
        public string Was_Price;
        public string New_Price;
    }
}
