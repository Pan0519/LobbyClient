using System.Collections.Generic;


namespace Services
{
    public static class PurchaseInfoMapDataConfig
    {
        #region MappingDictionary
        public readonly static Dictionary<PurchaseItemType, PurchaseMappingData> purchaseMappingData = new Dictionary<PurchaseItemType, PurchaseMappingData>()
        {
            {PurchaseItemType.Coin,new PurchaseMappingData()},
            {PurchaseItemType.LvUp,new PurchaseMappingData(){
                spriteName = "level_booster"
            } },
            {PurchaseItemType.Vip,new PurchaseMappingData(){
                spriteName = "vip_point",
                titleKey = "Store_SeeMore_VipPoints",
            } },
            {PurchaseItemType.DiamondPoint,new PurchaseMappingData(){
                spriteName = "black_card",
                titleKey = "Store_SeeMore_DiamondPoints"
            } },
            {PurchaseItemType.MedalGold,new PurchaseMappingData(){
                spriteName = "medal_gold",
                titleKey = "Store_SeeMore_GoldChip"
            }},
            {PurchaseItemType.MedalSilver,new PurchaseMappingData(){
                spriteName = "medal_silver",
                titleKey = "Store_SeeMore_SilverChip"
            }},
            {PurchaseItemType.Exp,new PurchaseMappingData(){
                spriteName = "exp",
                titleKey = "Store_SeeMore_XPX2"
            }},
            {PurchaseItemType.Coupon,new PurchaseMappingData(){
                spriteName = "coupon",
                titleKey = "Store_SeeMore_Coupon"
            }},
            {PurchaseItemType.PuzzlePack,new PurchaseMappingData(){
                spriteName = string.Empty,
                titleKey = "Store_SeeMore_PuzzlePack",
            }},
            { PurchaseItemType.HighRollerPassPoint,new PurchaseMappingData(){
                spriteName ="club_point",
                titleKey = "Store_SeeMore_DiamondPoints"
            }},
            { PurchaseItemType.ActivityProp,new PurchaseMappingData()}
        };

        public readonly static Dictionary<string, PurchaseItemType> mappingServerKind = new Dictionary<string, PurchaseItemType>()
        {
            {UtilServices.outcomeCoinKey,PurchaseItemType.Coin},
            {UtilServices.outcomeLvupBoost,PurchaseItemType.LvUp },
            {UtilServices.outcomeVIPPointKey,PurchaseItemType.Vip},
            {UtilServices.outcomeDiamondPoint,PurchaseItemType.DiamondPoint},
            {UtilServices.outcomeExpBoost,PurchaseItemType.Exp},
            {UtilServices.outcomePuzzlePack,PurchaseItemType.PuzzlePack},
            {UtilServices.outcomePuzzleVoucher,PurchaseItemType.PuzzlePack},
            {"gold",PurchaseItemType.MedalGold},
            {"silver",PurchaseItemType.MedalSilver},
            {"coupon",PurchaseItemType.Coupon},
            {"high-roller-pass-point",PurchaseItemType.HighRollerPassPoint },
            {"high-roller-vault",PurchaseItemType.HighRollerVault },
            {"activity-boost",PurchaseItemType.ActivityBoost},
            {"activity-prop",PurchaseItemType.ActivityProp},
        };
        #endregion
    }

    public enum PurchaseItemType : int
    {
        None = -3,
        Coin = -2,
        LvUp,
        Vip = 0,
        DiamondPoint,
        MedalGold,
        MedalSilver,
        Exp,
        Coupon,
        PuzzlePack,
        HighRollerPassPoint,
        HighRollerVault,
        ActivityBoost,
        ActivityProp,
    }

}
