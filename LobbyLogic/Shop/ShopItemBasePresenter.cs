using CommonILRuntime.BindingModule;
using CommonILRuntime.Module;
using UnityEngine.UI;
using UnityEngine;
using Lobby.Common;
using System.Collections.Generic;
using Services;
using UniRx;
using CommonILRuntime.Outcome;
using CommonPresenter.PackItem;

namespace Shop
{
    public enum LeftIconType : int
    {
        BetValue,
        Special,
        MostPopular,
    }

    class ShopItemBasePresenter : NodePresenter
    {
        string[] leftIconNames = new string[] { "best_value", "ft_special", "most_popular" };

        #region UIs
        public Image leftIconImg;
        public Button detailBtn;
        public GameObject detailGroups;

        GameObject couponObj;
        Text couponText;
        public CustomBtn buyBtn;
        RectTransform additionalItemGroupRect;
        GameObject tapLightObj;
        #endregion

        public List<PurchaseInfoData> infoDatas { get; private set; }
        public Subject<StoreItemData> buySubscribe = new Subject<StoreItemData>();

        Dictionary<string, PopularState> popularStateDict = new Dictionary<string, PopularState>();
        StoreItemData storeItem;
        const int additionalItemMaxCount = 3;
        List<AdditionalItemNode> additionalNodes = new List<AdditionalItemNode>();

        public override void initUIs()
        {
            additionalItemGroupRect = getRectData("add_item_group");
            leftIconImg = getImageData("left_icon_img");
            detailBtn = getBtnData("detail_btn");
            couponText = getTextData("coupon_txt");
            detailGroups = getGameObjectData("detail_group");
            couponObj = getGameObjectData("coupon_obj");
            buyBtn = getCustomBtnData("buy_btn");
            tapLightObj = getGameObjectData("tap_light_obj");
        }

        public override void init()
        {
            buyBtn.clickHandler = buyProduct;
            buyBtn.pointerDownHandler = () =>
            {
                activeTapLightObj(isActive: true);
            };
            buyBtn.pointerUPHandler = () =>
            {
                activeTapLightObj(isActive: false);
            };
            buyBtn.objScale = 1;
            popularStateDict.Add("most", PopularState.Most);
            popularStateDict.Add("best", PopularState.Best);
            detailBtn.onClick.RemoveAllListeners();
            detailBtn.onClick.AddListener(openDetailPage);
            couponText.gameObject.SetActive(true);
            setCouponObjActivty(false);
        }

        public override void open()
        {
            setBuyBtnInteractable(true);
            base.open();
        }

        public void setCouponObjActivty(bool active)
        {
            couponObj.setActiveWhenChange(active);
        }

        public void setCouponTxt(int couponBonus)
        {
            couponText.text = $"+{couponBonus}%";
        }

        public void setBuyBtnInteractable(bool enable)
        {
            buyBtn.interactable = enable;
        }

        public void setAdditionalItemInfos(Reward[] additions)
        {
            if (null == additions || additions.Length <= 0)
            {
                detailGroups.setActiveWhenChange(false);
                return;
            }

            detailGroups.setActiveWhenChange(true);
            infoDatas = PurchaseInfoCover.rewardConvertToPurchase(additions);

            for (int i = 0; i < additionalItemMaxCount; ++i)
            {
                if (additions.Length <= i)
                {
                    break;
                }
                var infoData = infoDatas[i];
                var itemPoolObj = ResourceManager.instance.getObjectFromPool("prefab/lobby_shop/additional_item_group", additionalItemGroupRect);
                var itemNode = UiManager.bindNode<AdditionalItemNode>(itemPoolObj.cachedGameObject);
                itemNode.showItem(infoData);
                additionalNodes.Add(itemNode);
            }
        }

        public void showLabelsImage(string[] labels)
        {
            leftIconImg.gameObject.setActiveWhenChange(false);
            if (null == labels || labels.Length <= 0)
            {
                return;
            }
            PopularState popularState;
            for (int i = 0; i < labels.Length; ++i)
            {
                var labelStr = labels[i];
                if (popularStateDict.TryGetValue(labelStr, out popularState))
                {
                    leftIconImg.gameObject.setActiveWhenChange(true);
                    leftIconImg.sprite = ShopDataStore.getShopSprite(ShopDataStore.shopPopularIconNames[(int)popularState]);
                    break;
                }
            }
        }

        public void clearAdditionalItems()
        {
            for (int i = 0; i < additionalNodes.Count; ++i)
            {
                ResourceManager.instance.returnObjectToPool(additionalNodes[i].uiGameObject);
            }
        }

        void activeTapLightObj(bool isActive)
        {
            tapLightObj.setActiveWhenChange(isActive);
        }

        void openDetailPage()
        {
            UiManager.getPresenter<AdditionalItemInfos>().openItemInfos(infoDatas);
        }

        public string getLeftIconName(LeftIconType leftIcon)
        {
            return leftIconNames[(int)leftIcon];
        }

        public void setStoreItem(StoreItemData itemData)
        {
            storeItem = itemData;
        }

        async void buyProduct()
        {
            storeItem = await StoreItemServices.sendBuyItem(storeItem);
            buySubscribe.OnNext(storeItem);
        }
    }

    public class AdditionalItemNode : NodePresenter
    {
        Image icon;
        Text numTxt;
        RectTransform packItem;

        public override void initUIs()
        {
            icon = getImageData("item_img");
            numTxt = getTextData("item_num");
            packItem = getRectData("pack_group");
        }

        public void showItem(PurchaseInfoData infoData)
        {
            numTxt.text = $"+{infoData.num}";
            if (infoData.itemKind != PurchaseItemType.PuzzlePack)
            {
                icon.sprite = infoData.iconSprite;
                icon.gameObject.setActiveWhenChange(true);
                return;
            }
            icon.gameObject.setActiveWhenChange(false);
            PackItemPresenterServices.getSinglePackItem(infoData.type, packItem);
        }
    }
}
