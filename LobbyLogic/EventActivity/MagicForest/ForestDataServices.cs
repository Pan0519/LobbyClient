using System;
using System.Collections.Generic;
using EventActivity;
using UniRx;
using Services;
using LobbyLogic.NetWork.ResponseStruct;

namespace MagicForest
{
    public static class ForestDataServices
    {
        public static readonly string NotOpenKey = "item-close";
        public static readonly string EmptyKey = "empty";
        public static readonly string PassKey = "pass";
        public static readonly string BossKey = "boss";
        public static readonly string prefabPath = "prefab/activity/magic_forest";
        public static Subject<bool> outDoorClearSub { get; private set; } = new Subject<bool>();
        public static Subject<bool> reduceMainBgmSub { get; private set; } = new Subject<bool>();
        public static Subject<ForestBoosterData> updateBoosterSub { get; private set; } = new Subject<ForestBoosterData>();

        public static Subject<GrassItemNodePresenter> guildGrassClickStepSub = new Subject<GrassItemNodePresenter>();
        public static bool isGuide;

        static Subject<bool> guildOutDoorToNextStep = null;

        public static void guildTreeClickStep(GrassItemNodePresenter itemNodePresenter)
        {
            guildGrassClickStepSub.OnNext(itemNodePresenter);
        }

        public static void subGuildNextStep(Action<bool> toNextStep)
        {
            guildOutDoorToNextStep = new Subject<bool>();
            guildOutDoorToNextStep.Subscribe(toNextStep);
        }
        public static void disposeGuildNextStep()
        {
            if (null == guildOutDoorToNextStep)
            {
                return;
            }
            isGuide = false;
            guildOutDoorToNextStep.Dispose();
            guildOutDoorToNextStep = null;
        }

        static Dictionary<string, GrassItemKind> grassStatus = new Dictionary<string, GrassItemKind>()
        {
            { NotOpenKey,GrassItemKind.Grass},
            { EmptyKey,GrassItemKind.None},
            { PassKey,GrassItemKind.Door},
            { BossKey,GrassItemKind.Stone},
        };

        static Dictionary<string, BagItemKind> bagKind = new Dictionary<string, BagItemKind>()
        {
            { EmptyKey,BagItemKind.None},
            { NotOpenKey,BagItemKind.Bag},
            { "token",BagItemKind.Hammer}
        };

        public static bool IsRecircle;
        public static bool isShowing;

        public static void isReduceMainBGM(bool isReduce)
        {
            reduceMainBgmSub.OnNext(isReduce);
        }
        public static void stopShowing()
        {
            isShowing = false;
            if (null != guildOutDoorToNextStep)
            {
                guildOutDoorToNextStep.OnNext(isShowing);
            }
        }

        public static Subject<long> goldenMalletCountSub { get; private set; } = new Subject<long>();
        public static void updateMalletCount(long count)
        {
            goldenMalletCountSub.OnNext(count);
        }

        public static Dictionary<string, long> jpRewards { get; private set; }
        public static void updateJPReward(Dictionary<string, long> reward)
        {
            jpRewards = reward;
        }
        public static void updateJPReward(string jpName, long reward)
        {
            jpName = jpName.toTitleCase();
            if (jpRewards.ContainsKey(jpName))
            {
                jpRewards[jpName] = reward;
            }
        }
        public static Dictionary<string, int> jpCounts { get; private set; }
        public static void updateJPCount(Dictionary<string, int> count)
        {
            jpCounts = count;
        }

        public static void addJPCount(string jpName)
        {
            jpName = jpName.toTitleCase();
            int count;
            if (jpCounts.TryGetValue(jpName, out count))
            {
                jpCounts[jpName] = count + 1;
            }
        }

        public static Subject<long> totalTicketSub { get; private set; } = new Subject<long>();

        public static long totalTicketCount { get; private set; }
        public static void updateTotalTicket(long totalCount)
        {
            totalTicketCount = totalCount;
            totalTicketSub.OnNext(totalTicketCount);
            ActivityDataStore.pageAmountChange((int)totalTicketCount);
        }

        public static void addTicketCount(int addCount)
        {
            updateTotalTicket(totalTicketCount + addCount);
        }

        public static GrassItemKind getGrassItemStatus(string kind)
        {
            AwardKind awardKind = ActivityDataStore.getAwardKind(kind);
            switch (awardKind)
            {
                case AwardKind.BuffMore:
                    return GrassItemKind.Leprechaun;
                case AwardKind.Ticket:
                case AwardKind.PuzzlePack:
                case AwardKind.PuzzleVoucher:
                    return GrassItemKind.Urn;
                case AwardKind.Coin:
                    return GrassItemKind.Coin;
            }

            GrassItemKind result;
            if (grassStatus.TryGetValue(kind, out result))
            {
                return result;
            }

            return GrassItemKind.None;
        }

        public static GrassItemKind getHistoryStatus(string kind, string type)
        {
            if (kind.Equals(BossKey) && !string.IsNullOrEmpty(type))
            {
                return GrassItemKind.Gem;
            }

            return getGrassItemStatus(kind);
        }

        public static BagItemKind getBagKind(string kind)
        {
            AwardKind awardKind = ActivityDataStore.getAwardKind(kind);
            if (AwardKind.Coin == awardKind)
            {
                return BagItemKind.Coin;
            }
            BagItemKind result;
            if (bagKind.TryGetValue(kind, out result))
            {
                return result;
            }

            return BagItemKind.None;
        }

        public static void updateBooster(ForestBoosterData data)
        {
            updateBoosterSub.OnNext(data);
        }
    }
    public enum GrassItemKind
    {
        None,
        Coin,
        Grass,
        Door,
        Stone,
        Urn,
        Leprechaun,
        Gem,
    }

    public enum BagItemKind
    {
        None,
        Coin,
        Bag,
        Hammer,
    }
}
