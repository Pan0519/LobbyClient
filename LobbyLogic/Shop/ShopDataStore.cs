using UnityEngine;
using System;
using LobbyLogic.NetWork.ResponseStruct;
using UnityEngine.Purchasing;
using System.Collections.Generic;
using Services;
using Lobby.Common;

namespace Shop
{
    public static class ShopDataStore
    {
        public static string[] shopGiftMedalIconNames = new string[] { "btn_collect_start", "btn_collect_half", "btn_collect_last", };

        public static string[] shopPopularIconNames = new string[] { "pic_most_popular", "pic_best_value" };

        //public static string[] shopBuffIconNames = new string[] { "icon_exp", "icon_exp_booster" };

        static Dictionary<BuffState, string> buffIconNames = new Dictionary<BuffState, string>()
        {
            { BuffState.Exp,"exp"},
            { BuffState.Lvup,"level_booster"},
        };

        static Dictionary<PurchaseItemType, StoreItemExplanationData> storeItemExplanationDict = new Dictionary<PurchaseItemType, StoreItemExplanationData>()
        {
            { PurchaseItemType.LvUp,new StoreItemExplanationData(){
                 titleSpriteName = "level_bang",
                 iconSpriteName = "icon_item_level_bang",
                 contentKey = "Store_LevelBang_Description",
            }}
        };

        //public static int medalCount = 2;
        public static string boostFirst { get { return "first"; } }
        public static string boostActivity { get { return "activity"; } }
        public static string boostCoupon { get { return "coupon"; } }

        static string _coinKind = string.Empty;
        static string _itemKind = string.Empty;
        public static string StoreCoinKind
        {
            get
            {
                if (string.IsNullOrEmpty(_coinKind))
                {
                    _coinKind = StoreKind.Coin.ToString().ToLower();
                }
                return _coinKind;
            }
        }
        public static string StoreItemKind
        {
            get
            {
                if (string.IsNullOrEmpty(_itemKind))
                {
                    _itemKind = StoreKind.Item.ToString().ToLower();
                }
                return _itemKind;
            }
        }

        public static Sprite getShopSprite(string spriteName)
        {
            return LobbySpriteProvider.instance.getSprite<ShopSpriteProvider>(LobbySpriteType.Shop, spriteName);
        }

        public static Sprite getGiftStateSprite(GiftState state, bool isChoose)
        {
            string spriteName = $"box_{Enum.GetName(typeof(GiftState), state).ToLower()}";

            if (isChoose)
            {
                spriteName = String.Concat(spriteName, "_choose");
            }

            return getShopSprite(spriteName);
        }

        public static Sprite getBuffSprite(BuffState buffState)
        {
            string spriteName;
            if (buffIconNames.TryGetValue(buffState, out spriteName))
            {
                return getShopSprite($"icon_{spriteName}");
            }
            return null;
        }

        public static StoreItemExplanationData getItemExplanation(PurchaseItemType purchaseType)
        {
            StoreItemExplanationData result = null;

            if (!storeItemExplanationDict.TryGetValue(purchaseType, out result))
            {
                Debug.LogError($"get {purchaseType} explanation data is null");
            }

            return result;
        }
        public static DateTime getBounsTime(string bounsTime)
        {
            return UtilServices.strConvertToDateTime(bounsTime, DateTime.MinValue);
        }
    }

    public enum GiftState
    {
        Gold,
        Silver,
    }

    public enum PopularState : int
    {
        Most,
        Best,
    }

    public enum StoreKind
    {
        Coin,
        Item,
        Divider,
    }
    public enum BuffState
    {
        Exp,
        Lvup,
    }

    public class GiftInfoData
    {
        public GiftState giftState;
        public int num;
    }

    public class StoreItemData
    {
        public StoreProduct product;
        public Product platformProduct;
        public string orderID;
    }
    public class StoreItemExplanationData
    {
        public string titleSpriteName;
        public string contentKey;
        public string iconSpriteName;

        //public StoreItemExplanationData(string titleSpriteName, string iconSpriteName, string contentKey)
        //{
        //    this.iconSpriteName = iconSpriteName;
        //    this.titleSpriteName = titleSpriteName;
        //    this.contentKey = contentKey;
        //}
    }
}
