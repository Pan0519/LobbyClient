using UnityEngine;
using System.Collections.Generic;
using System;
using CommonILRuntime.SpriteProvider;
using CommonILRuntime.Outcome;

namespace Services
{
    public static class PurchaseInfo
    {
        public static List<PurchaseItemType> ignorePurchaseItemTypes = new List<PurchaseItemType>()
        {
            PurchaseItemType.Coin,PurchaseItemType.HighRollerVault,PurchaseItemType.ActivityBoost
        };

        public static PurchaseMappingData getMappingData(string kind)
        {
            PurchaseItemType items = getItemType(kind);
            if (PurchaseItemType.None != items)
            {
                return getMappingData(items);
            }
            return null;
        }

        public static PurchaseMappingData getMappingData(PurchaseItemType itemType)
        {
            PurchaseMappingData result = null;

            if (!PurchaseInfoMapDataConfig.purchaseMappingData.TryGetValue(itemType, out result))
            {
                Debug.Log($"get {itemType} mappingData is null");
            }
            return result;
        }

        public static PurchaseItemType getItemType(string kind)
        {
            PurchaseItemType items;
            if (!PurchaseInfoMapDataConfig.mappingServerKind.TryGetValue(kind, out items))
            {
                items = PurchaseItemType.None;
                Debug.LogError($"get {kind} itemType is null");
            }
            return items;
        }

        public static Sprite getPuraseSprite(string spriteName)
        {
            if (string.IsNullOrEmpty(spriteName))
            {
                return null;
            }
            return CommonSpriteProvider.instance.getSprite<PurchaseInfoProvider>(CommonSpriteType.PurchaseInfo, $"icon_{spriteName}");
        }
    }

    #region Purchase
    public class PurchaseInfoData
    {
        public ulong num;
        public string type;
        public Sprite iconSprite { get; private set; }
        public string titleKey { get; private set; } = string.Empty;
        public PurchaseItemType itemKind { get; private set; }
        public CommonReward outcomeObj;
        public bool parseInfo(string kind)
        {
            PurchaseItemType itemKind = PurchaseInfo.getItemType(kind);
            if (PurchaseItemType.None == itemKind)
            {
                Debug.Log($"get {kind} itemKind is null");
                return false;
            }

            this.itemKind = itemKind;
            if (PurchaseInfo.ignorePurchaseItemTypes.Contains(itemKind))
            {
                return true;
            }

            PurchaseMappingData mappingData = PurchaseInfo.getMappingData(itemKind);
            if (null == mappingData)
            {
                return false;
            }
            iconSprite = PurchaseInfo.getPuraseSprite(mappingData.spriteName);
            titleKey = mappingData.titleKey;
            return true;
        }

        public void setIconSprite(string spriteName)
        {
            iconSprite = PurchaseInfo.getPuraseSprite(spriteName);
        }

        public void setTitleKey(string keyName)
        {
            titleKey = keyName;
        }

        public long getPuzzleID()
        {
            if (PurchaseItemType.PuzzlePack != itemKind)
            {
                return 0;
            }

            long packID;
            if (long.TryParse(type, out packID))
            {
                return packID;
            }
            return 0;
        }

        public bool IsMedal
        {
            get
            {
                return itemKind == PurchaseItemType.MedalGold || itemKind == PurchaseItemType.MedalSilver;
            }
        }
    }

    public class PurchaseMappingData
    {
        public string titleKey = string.Empty;
        public string spriteName = string.Empty;
    }

    public class PurchaseInfoCover
    {
        public static List<PurchaseInfoData> rewardConvertToPurchase(Reward[] rewards)
        {
            List<PurchaseInfoData> result = new List<PurchaseInfoData>();

            for (int i = 0; i < rewards.Length; ++i)
            {
                var data = rewards[i];
                var info = new PurchaseInfoData()
                {
                    num = data.getAmount(),
                    type = data.type,
                };

                if (!info.parseInfo(data.kind))
                {
                    continue;
                }
                stackInfo(result, info);
            }
            return result;
        }

        public static List<PurchaseInfoData> rewardConvertToPurchase(CommonReward[] rewards)
        {
            List<PurchaseInfoData> result = new List<PurchaseInfoData>();

            for (int i = 0; i < rewards.Length; ++i)
            {
                var data = rewards[i];
                var info = new PurchaseInfoData()
                {
                    num = data.getAmount(),
                    type = data.type,
                    outcomeObj = data,
                };
                if (!info.parseInfo(data.kind))
                {
                    continue;
                }

                stackInfo(result, info);
            }

            return result;
        }

        public static List<PurchaseInfoData> stackPurchaseInfos(List<PurchaseInfoData> infoDatas)
        {
            List<PurchaseInfoData> result = new List<PurchaseInfoData>();

            for (int i = 0; i < infoDatas.Count; ++i)
            {
                stackInfo(result, infoDatas[i]);
            }

            return result;
        }

        static void stackInfo(List<PurchaseInfoData> resultDatas, PurchaseInfoData stackData)
        {
            var data = resultDatas.Find(info => info.itemKind.Equals(stackData.itemKind) && info.type.Equals(stackData.type));
            if (null != data)
            {
                data.num++;
            }
            else
            {
                resultDatas.Add(stackData);
            }
        }

    }
    #endregion
}
