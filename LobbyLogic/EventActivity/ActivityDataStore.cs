using FarmBlast;
using CasinoCrush;
using FrenzyJourney;
using System;
using UnityEngine;
using CommonILRuntime.BindingModule;
using System.Collections.Generic;
using LobbyLogic.NetWork.ResponseStruct;
using CommonService;
using CommonILRuntime.Services;
using UniRx;
using LobbyLogic.Audio;
using Services;
using Event.Common;
using Debug = UnityLogUtility.Debug;

namespace EventActivity
{
    public static class ActivityDataStore
    {
        public const string CommonPrefabPath = "prefab/activity/activity_item_common/";
        public const string CasinoCrushPrefabPath = "prefab/activity/rookie/";
        public const string FarmBlastPrefabPath = "prefab/activity/farm_blast/";
        public const string GrandKey = "Grand";
        public const string MajorKey = "Major";
        public const string MinorKey = "Minor";
        public const string MiniKey = "Mini";

        public static Activity nowActivityInfo;

        public static bool isPrizeBooster = false;
        public static Subject<int> totalTicketCountUpdateSub = new Subject<int>();
        public static Subject<bool> activityCloseSub = new Subject<bool>();
        public static Subject<bool> isEndSub = new Subject<bool>();

        public static Subject<bool> isEndErrorSub = new Subject<bool>();

        public static bool isOpenShop = false;

        static Dictionary<BoosterType, string> boosterSpriteNames = new Dictionary<BoosterType, string>()
        {
            {BoosterType.Coin,"coin_booster" },
            {BoosterType.Dice ,"dice_booster" },
            {BoosterType.FrenzyDice,"frenzy_dice" },
            {BoosterType.GoldenTicket, "golden_ticket"},
            {BoosterType.Prize,"prize_booster" },
            {BoosterType.Ticket, "ticket_booster"},
            {BoosterType.GoldenMallet,"golden_mallet" },
            {BoosterType.Magnifire,"magnifire_booster" }
        };

        static readonly Dictionary<ActivityID, string> ticketIDs = new Dictionary<ActivityID, string>()
        {
            { ActivityID.Rookie,"newbie"},
            { ActivityID.FarmBlast,"flop"},
            { ActivityID.FrenzyJourney,"nabob"},
            { ActivityID.MagicForest,"magicForest"}
        };
        public static readonly Dictionary<ActivityID, string> ticketPurchaseSpriteNames = new Dictionary<ActivityID, string>()
        {
            { ActivityID.Rookie,"ticket"},
            { ActivityID.FarmBlast,"ticket"},
            { ActivityID.FrenzyJourney,"dice"},
            { ActivityID.MagicForest,"magnifier"}
        };

        static readonly Dictionary<ActivityID, string> lobbyBarEntryPrefabName = new Dictionary<ActivityID, string>
        {
            { ActivityID.Rookie,"casino_crush"},
            { ActivityID.FarmBlast,"farm_blast"},
            { ActivityID.FrenzyJourney,"frenzy_journey"},
            { ActivityID.MagicForest,"magic_forest"},
        };

        static readonly Dictionary<string, AwardKind> awardType = new Dictionary<string, AwardKind>()
        {
            { "coin",AwardKind.Coin},
            { "buff",AwardKind.BuffMore},
            { "box",AwardKind.Box},
            { "boss",AwardKind.Boss},
            { "prop",AwardKind.Ticket},
            { "activity-prop",AwardKind.Ticket},
            { "token",AwardKind.CollectTarget},
            { "jp",AwardKind.Jackpot},
            { "boost",AwardKind.Booster},
            { "coinBoost",AwardKind.PrizeBooster},
            { "spinBoost",AwardKind.TicketBooster},
            { "pickBoost",AwardKind.GoldenTicket},
            { "puzzle-pack",AwardKind.PuzzlePack},
            { "puzzle-voucher",AwardKind.PuzzleVoucher},
            { "coupon",AwardKind.Coupon},
            { "exp_up",AwardKind.Exp},
            { "diamond_club_point",AwardKind.HighRollerPoint},
            { "high-roller-pass-point",AwardKind.HighRollerPassPoint}
        };

        public static AwardKind getAwardKind(string awardKind)
        {
            if (string.IsNullOrEmpty(awardKind))
            {
                return AwardKind.None;
            }

            AwardKind result;
            if (awardType.TryGetValue(awardKind, out result))
            {
                return result;
            }
            return AwardKind.None;
        }

        public static string getNowActivityTicketID()
        {
            ActivityID activityID = getNowActivityID();
            string result;
            if (ticketIDs.TryGetValue(activityID, out result))
            {
                return result;
            }

            return string.Empty;
        }

        public static Subject<int> pageAmountChangedSub = new Subject<int>();

        public static ActivityID parseActivityID(string activityID)
        {
            int activityIDInt;
            if (!int.TryParse(activityID, out activityIDInt))
            {
                return ActivityID.None;
            }

            return (ActivityID)activityIDInt;
        }

        public static ActivityID getNowActivityID()
        {
            return parseActivityID(nowActivityInfo.activityId);
        }

        public static void coinFly(RectTransform target, ulong reward, Action complete)
        {
            ulong playCoin = DataStore.getInstance.playerInfo.myWallet.coin;
            CoinFlyHelper.frontSFly(target, playCoin, playCoin + reward, onComplete: () =>
           {
               DataStore.getInstance.playerInfo.myWallet.unsafeAdd(reward);
               if (null != complete)
               {
                   complete();
               }
           });
        }

        public static string getActivityPurchaseItemName(string activityID)
        {
            string result = string.Empty;
            ActivityID id = parseActivityID(activityID);
            if (id != ActivityID.None)
            {
                ticketPurchaseSpriteNames.TryGetValue(id, out result);
            }
            return result;
        }

        public static void playClickAudio()
        {
            AudioManager.instance.playAudioOnce(CommonAudioPathProvider.getAudioPath(BasicCommonSound.InfoBtn));
        }

        public static void pageAmountChange(int changedValue)
        {
            pageAmountChangedSub.OnNext(changedValue);
        }

        public static GameObject getTreasureChestGO(RectTransform parentTrans)
        {
            var tempGO = ResourceManager.instance.getGameObject($"{CommonPrefabPath}group_treasure_chest");
            return GameObject.Instantiate(tempGO, parentTrans);
        }

        static GameObject boosterTemp = null;

        public static T getBoosterGO<T>(RectTransform parentTrans) where T : BoosterNodePresenter, new()
        {
            if (null == boosterTemp)
            {
                boosterTemp = ResourceManager.instance.getGameObject($"{CommonPrefabPath}booster_item");
            }

            GameObject boosterGO = GameObject.Instantiate(boosterTemp, parentTrans);
            return UiManager.bindNode<T>(boosterGO);
        }

        public static string getBoosterSpriteName(BoosterType boosterType)
        {
            string result = string.Empty;
            if (!boosterSpriteNames.TryGetValue(boosterType, out result))
            {
                Debug.LogError($"get {boosterType} BoosterSpriteName is error");
            }
            return result;
        }

        public static void updateTotalTicketCount(int totalCount)
        {
            totalTicketCountUpdateSub.OnNext(totalCount);
        }

        public static void activityPageCloseCall()
        {
            activityCloseSub.OnNext(true);
        }

        public static void activtyCallIsEnd(bool isEnd)
        {
            isEndSub.OnNext(isEnd);
        }

        public static void activityCallErrorComplete(bool activityError)
        {
            isEndErrorSub.OnNext(activityError);
        }

        public static string getAcitivtyEntryPrefabPath(string activityID)
        {
            ActivityID id = parseActivityID(activityID);
            string prefabName;
            if (!lobbyBarEntryPrefabName.TryGetValue(id, out prefabName))
            {
                return string.Empty;
            }

            return $"prefab/lobby/activity_enrty/entry_{prefabName}";
        }

        public static string getAcitivtyEntryPrefabName()
        {
            ActivityID id = parseActivityID(nowActivityInfo.activityId);
            string prefabName;
            if (!lobbyBarEntryPrefabName.TryGetValue(id, out prefabName))
            {
                return string.Empty;
            }

            return $"lobby_{prefabName}";
        }

        public static string getActivityEntryPrefabName(string activityID)
        {
            ActivityID id = parseActivityID(activityID);
            string prefabName;
            if (!lobbyBarEntryPrefabName.TryGetValue(id, out prefabName))
            {
                return string.Empty;
            }

            return prefabName;
        }
        public static string getActivityEntryPrefabName(ActivityID activityID)
        {
            string prefabName;
            if (!lobbyBarEntryPrefabName.TryGetValue(activityID, out prefabName))
            {
                return string.Empty;
            }

            return prefabName;
        }
    }

    public class ActivityPageData
    {
        static ActivityPageData _instance = new ActivityPageData();
        public static ActivityPageData instance { get { return _instance; } }

        Dictionary<ActivityID, ActivityShowData> activityShowData = new Dictionary<ActivityID, ActivityShowData>();

        IActivityPage activityPage = null;
        ActivityPageData()
        {
            addActivityShowData();
        }

        void addActivityShowData()
        {
            activityShowData.Add(ActivityID.Rookie, getActivityShowData("casino_crush", openCasinoCrush));
            activityShowData.Add(ActivityID.FarmBlast, getActivityShowData("farm_blast", openFramBlast));
            activityShowData.Add(ActivityID.FrenzyJourney, getActivityShowData("frenzy_journey", openJourney));
            activityShowData.Add(ActivityID.MagicForest, getActivityShowData("magic_forest", openMagicForest));
        }

        ActivityShowData getActivityShowData(string spriteName, Action activityPage)
        {
            return new ActivityShowData()
            {
                spriteName = spriteName,
                getPage = activityPage
            };
        }

        public void openActivityPage(ActivityID activityID)
        {
            ActivityShowData showData;

            if (activityShowData.TryGetValue(activityID, out showData))
            {
                showData.getPage();
                activityPage.open();
            }
        }

        void openFramBlast()
        {
            activityPage = UiManager.getPresenter<FarmBlastPresenter>();
        }

        void openCasinoCrush()
        {
            activityPage = UiManager.getPresenter<CasinoCrushPresenter>();
        }

        void openJourney()
        {
            activityPage = JourneyPresenterManager.getInstance;
        }

        void openMagicForest()
        {
            activityPage = UiManager.getPresenter<MagicForest.MagicForestMainOutDoorPresenter>();
        }
    }

    public class ActivityAwardData
    {
        public AwardKind kind { get; private set; }
        public string type { get; private set; }
        public ulong amount { get; private set; }

        public void parseAwardData(ActivityReward activityReward)
        {
            if (null == activityReward)
            {
                kind = AwardKind.None;
                return;
            }
            kind = ActivityDataStore.getAwardKind(activityReward.Kind);
            if (AwardKind.Booster == kind)
            {
                kind = ActivityDataStore.getAwardKind(activityReward.Type);
            }
            type = activityReward.Type;
            amount = activityReward.getAmount;
        }
    }

    public class ActivityShowData
    {
        public string spriteName;
        public Action getPage;
    }

    public class RookieLevelSetting
    {
        public Dictionary<string, PosData> itemPos;
        public RookieLevelInfo[] levelsSetting;
    }

    public class PosData
    {
        public float x;
        public float y;
    }

    public class RookieLevelInfo
    {
        public int level;
        public RookieItemInfo[] itemsInfo;
    }

    public class RookieItemInfo
    {
        public int index;
        public int pic_num;
    }

    public enum ActivityID : int
    {
        None = 0,
        Rookie = 20001,
        FarmBlast,
        FrenzyJourney,
        MagicForest,
    }

    /// <summary>
    /// 獎勵類型, 目前點擊物品及箱子共用
    /// </summary>
    public enum AwardKind
    {
        None = 0,
        BuffMore,
        Box,                  // 0:木 1:銀 2:金
        Ticket,
        Coin,
        CollectTarget,
        Jackpot,
        Booster,
        TicketBooster = 7,
        PrizeBooster = 8,
        PuzzlePack,
        PuzzleVoucher,
        Boss,
        GoldenTicket,
        VipPoint,
        Coupon,
        Exp,
        HighRollerPoint,
        HighRollerPassPoint
    }

    public enum JourneyAwardType
    {
        None = 0,
        Coin,
        Box,  // 0:木 1:銀 2:金
        Card,
        Boss,
    }
    public enum TreasureBoxType
    {
        None = -1,
        Wood = 1,
        Silver = 2,
        Gold = 3
    }
    public enum BoosterType
    {
        Coin,
        Dice,
        FrenzyDice,
        GoldenTicket,
        Prize,
        Ticket,
        GoldenMallet,
        Magnifire,
    }
}
