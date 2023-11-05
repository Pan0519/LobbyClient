using UnityEngine;
using System;
using System.Collections.Generic;
using LobbyLogic.NetWork.ResponseStruct;
using CommonILRuntime.BindingModule;
using CommonService;
using Service;
using System.Threading.Tasks;
using Lobby.Jigsaw;
using CommonILRuntime.Outcome;
using Services;
using Debug = UnityLogUtility.Debug;

namespace LoginReward
{
    public class LoginRewardServices
    {
        public static LoginRewardServices instance
        {
            get
            {
                if (null == _instance)
                {
                    _instance = new LoginRewardServices();
                }
                return _instance;
            }
        }
        static LoginRewardServices _instance;

        LoginRewardPresenter rewardPresenter
        {
            get
            {

                return UiManager.getPresenter<LoginRewardPresenter>();
            }
        }

        const string dailyWeekType = "daily-week";
        const string dailyMonthType = "daily-month";
        public List<int> rewardDaysNum { get; private set; } = new List<int>();
        public int rewardFinalDay { get; private set; }
        //public Sprite dayReceivedBG { get; private set; }

        Dictionary<int, Action> showRewardOrder = new Dictionary<int, Action>();
        int showOrderID = 0;

        Dictionary<int, List<DayItemData>> seventItemDatas = new Dictionary<int, List<DayItemData>>();
        Dictionary<int, List<DayItemData>> dayItemDatas = new Dictionary<int, List<DayItemData>>();
        public int totalMonthDays { get; private set; } = 0;

        Dictionary<RewardType, DailyRewardDatas> dailyRewards = new Dictionary<RewardType, DailyRewardDatas>();
        public ulong totalCoinAmount { get; private set; }
        public int resettableCumulativeDays { get; private set; }
        public async Task initDailyData(DailyReward dailyReward, bool hasReward)
        {
            totalCoinAmount = 0;
            parseDailyReward(dailyReward.rewards);
            await initRewardData();
            if (hasReward)
            {
                initShowOrder();
            }
            resettableCumulativeDays = dailyReward.resettableCumulativeDays;
            totalMonthDays = dailyReward.cumulativeDays % rewardFinalDay;
            if (totalMonthDays <= 0)
            {
                totalMonthDays = rewardFinalDay;
            }
            getDailyTotalAmount();
        }

        void getDailyTotalAmount()
        {
            DailyRewardDatas dailyReward = getDailyReward(RewardType.Week);
            if (null == dailyReward)
            {
                return;
            }
            List<DayItemData> dailyData;
            if (seventItemDatas.TryGetValue(dailyReward.dailyRewards.cumulativeDays, out dailyData))
            {
                totalCoinAmount += getTotalRewardAmount(dailyData);
            }

            dailyReward = getDailyReward(RewardType.Month);
            if (null != dailyReward && dayItemDatas.TryGetValue(dailyReward.dailyRewards.cumulativeDays, out dailyData))
            {
                totalCoinAmount += getTotalRewardAmount(dailyData);
            }
        }

        ulong getTotalRewardAmount(List<DayItemData> dailyData)
        {
            ulong amount = 0;
            for (int i = 0; i < dailyData.Count; ++i)
            {
                var data = dailyData[i];
                if (DayItemType.Coin == data.itemType)
                {
                    amount += data.amount;
                }
            }
            return amount;
        }

        public async void startRunReward(DailyReward dailyRewardd)
        {
            await initDailyData(dailyRewardd, dailyRewardd.rewards.Length > 0);
            showNextStep();
        }

        void parseDailyReward(DailyRewards[] rewards)
        {
            dailyRewards.Clear();
            for (int i = 0; i < rewards.Length; ++i)
            {
                var reward = rewards[i];
                switch (reward.type)
                {
                    case dailyWeekType:
                        dailyRewards.Add(RewardType.Week, new DailyRewardDatas()
                        {
                            dailyRewards = reward
                        });
                        break;
                    case dailyMonthType:
                        dailyRewards.Add(RewardType.Month, new DailyRewardDatas()
                        {
                            dailyRewards = reward
                        });
                        break;
                }
            }
        }
        async Task initRewardData()
        {
            seventItemDatas.Clear();
            dayItemDatas.Clear();
            rewardDaysNum.Clear();

            var dailyJson = await WebRequestText.instance.loadTextFromServer("daily_reward_setting");
            var rewardSettings = LitJson.JsonMapper.ToObject<DailyRewardSettings>(dailyJson);

            for (int i = 0; i < rewardSettings.DailyReward.Length; ++i)
            {
                var setting = rewardSettings.DailyReward[i];
                switch (setting.type)
                {
                    case dailyWeekType:
                        seventItemDatas.Add(setting.cumulativeDays, parseDailySetting(setting));
                        break;

                    case dailyMonthType:
                        dayItemDatas.Add(setting.cumulativeDays, parseDailySetting(setting));
                        rewardDaysNum.Add(setting.cumulativeDays);
                        break;
                }
            }

            rewardDaysNum.Sort();
            rewardFinalDay = rewardDaysNum[rewardDaysNum.Count - 1];
        }

        void initShowOrder()
        {
            addShowOrder(ShowOrderStep.ShowDailyPage, showStepReward);
            addShowOrder(ShowOrderStep.ShowDailyResult, showWeekResult);
            addShowOrder(ShowOrderStep.ShowDailyRewardPack, showWeekRewardPack);
            addShowOrder(ShowOrderStep.AddStamp, addStampToItem);

            if (dailyRewards.ContainsKey(RewardType.Month))
            {
                addShowOrder(ShowOrderStep.ShowDayResult, showMonthResult);
                addShowOrder(ShowOrderStep.UpdateDaysGoal, updateDaysGoal);
                addShowOrder(ShowOrderStep.ShowDayRewardPack, showMonthRewards);
            }

        }

        void addShowOrder(ShowOrderStep step, Action orderEvent)
        {
            int stepKey = (int)step;
            if (showRewardOrder.ContainsKey(stepKey))
            {
                //Debug.LogError($"addShowOrder with the same key has already been added. Key: {step}");
                return;
            }
            showRewardOrder.Add(stepKey, orderEvent);
        }

        List<DayItemData> parseDailySetting(DailyRewardSetting rewardSetting)
        {
            List<DayItemData> itemDatas = new List<DayItemData>();
            itemDatas.Add(new DayItemData()
            {
                itemType = DayItemType.Coin,
                amount = (ulong)(rewardSetting.rewardMoney.odds * DataStore.getInstance.playerInfo.coinExchangeRate),
            });
            //Debug.Log($"setting parse {rewardSetting.cumulativeDays} - {rewardSetting.rewardItems.Length}");
            for (int i = 0; i < rewardSetting.rewardItems.Length; ++i)
            {
                var dayItemDate = new DayItemData();
                dayItemDate.parseItemType(rewardSetting.rewardItems[i]);
                itemDatas.Add(dayItemDate);
            }
            return itemDatas;
        }

        #region showRewrdOrder
        async void showWeekResult()
        {
            DailyRewardDatas weekReward = getDailyReward(RewardType.Week);
            if (null == weekReward)
            {
                return;
            }
            int sevenDay = weekReward.dailyRewards.cumulativeDays;
            if (string.IsNullOrEmpty(weekReward.dailyRewards.rewardPackId))
            {
                return;
            }
            var rewardPacket = await AppManager.lobbyServer.getRewardPacks(weekReward.dailyRewards.rewardPackId);
            weekReward.commonRewards = rewardPacket.rewards;
            List<DayItemData> dailyData;
            if (seventItemDatas.TryGetValue(sevenDay, out dailyData))
            {
                getResultPresenter().openDailyPage(dailyData, Outcome.process(rewardPacket.rewards));
            }
        }

        void showWeekRewardPack()
        {
            showRewardPacks(RewardType.Week);
        }

        public void addStampToItem()
        {
            rewardPresenter.showNowDailyData();
            //rewardPresenter.setDayItemDatas(dayItemDatas);
        }

        public void showTestStamp()
        {
            rewardPresenter.showAllDaysItem();
        }

        public void showHistoryRewardExpectToDay()
        {
            rewardPresenter.showHistoryDailyExpectToday(seventItemDatas);
            rewardPresenter.setDayItemDatas(dayItemDatas);
        }

        public void showHistoryRewardBesideToDay()
        {
            rewardPresenter.showHistoryDailyBesideToday(seventItemDatas);
            rewardPresenter.setDayItemDatas(dayItemDatas);
        }

        async void showStepReward()
        {
            showHistoryRewardExpectToDay();
            await Task.Delay(TimeSpan.FromSeconds(0.5f));
            showNextStep();
        }

        async void showMonthResult()
        {
            DailyRewardDatas monthReward = getDailyReward(RewardType.Month);
            if (null == monthReward)
            {
                return;
            }
            //var dayRewardNode = rewardPresenter.getDayRewardNode(totalMonthDays);
            if (string.IsNullOrEmpty(monthReward.dailyRewards.rewardPackId))
            {
                return;
            }
            var rewardPacket = await AppManager.lobbyServer.getRewardPacks(monthReward.dailyRewards.rewardPackId);
            monthReward.commonRewards = rewardPacket.rewards;
            await Task.Delay(TimeSpan.FromSeconds(1.0f));
            List<DayItemData> dayItemDatas;
            if (this.dayItemDatas.TryGetValue(totalMonthDays, out dayItemDatas))
            {
                getResultPresenter().openDaysPage(totalMonthDays, dayItemDatas, Outcome.process(rewardPacket.rewards));
            }
        }

        void showMonthRewards()
        {
            showRewardPacks(RewardType.Month);
        }

        void showRewardPacks(RewardType rewardType)
        {
            DailyRewardDatas rewardPacket = getDailyReward(rewardType);
            if (null == rewardPacket)
            {
                showNextStep();
                return;
            }

            OpenPackWildProcess.openPackWild(rewardPacket.commonRewards, showNextStep);
        }

        LoginRewardResultPresenter getResultPresenter()
        {
            var resultPresenter = UiManager.getPresenter<LoginRewardResultPresenter>();
            resultPresenter.closeCB = showNextStep;
            return resultPresenter;
        }

        void updateDaysGoal()
        {
            rewardPresenter.setNowDayGoal();
        }

        #endregion
        public void showNextStep()
        {
            //Debug.Log($"showNextStep showOrder {showOrderID} , {showRewardOrder.Count}");
            showOrderID++;
            if (showOrderID > showRewardOrder.Count)
            {
                return;
            }
            Action showOrder;
            if (showRewardOrder.TryGetValue(showOrderID, out showOrder))
            {
                showOrder();
            }
        }

        public DailyRewardDatas getDailyReward(RewardType rewardType)
        {
            DailyRewardDatas result = null;
            dailyRewards.TryGetValue(rewardType, out result);
            return result;
        }

        public void setRewardInfos(List<DayItemData> rewardItemDatas, RectTransform rewardInfoGroup, float itemScale)
        {
            for (int i = 0; i < rewardItemDatas.Count; ++i)
            {
                var rewardItem = LoginRewardItemData.addDayItem(rewardItemDatas[i], rewardInfoGroup, itemScale);
                rewardItem.cachedRectTransform.localPosition = Vector3.zero;
                if (i < rewardItemDatas.Count - 1)
                {
                    LoginRewardItemData.addPlusItem(rewardInfoGroup);
                }
            }
        }
    }

    enum ShowOrderStep
    {
        ShowDailyPage = 1,
        ShowDailyResult,
        ShowDailyRewardPack,
        AddStamp,
        ShowDayResult,
        ShowDayRewardPack,
        UpdateDaysGoal,
    }

    public class LoginRewardItemData
    {
        static Dictionary<DayItemType, string> iconObjNames = new Dictionary<DayItemType, string>()
        {
            { DayItemType.Coin,"coin"},
            { DayItemType.Coupon,"coupon"},
            { DayItemType.Exp,"exp_up"},
            { DayItemType.Puzzle,"puzzle_pack"},
            { DayItemType.LvUp,"exp_up"},
            { DayItemType.HighRollerPoint,"diamond_club_pass"},
            { DayItemType.HighRollerPassPoint,"diamond_club_point"}
        };

        public static string iconPath { get { return "prefab/login_reward"; } }

        public static PoolObject addDayItem(DayItemData itemData, RectTransform parent, float scale = 1.0f, bool animatorEnable = false)
        {
            var pool = ResourceManager.instance.getObjectFromPoolWithResOrder($"{iconPath}/login_item_{iconObjNames[itemData.itemType]}", parent,resNames: AssetBundleData.getBundleName(BundleType.LoginReward));
            pool.cachedRectTransform.GetComponentInChildren<Animator>().enabled = animatorEnable;
            if (DayItemType.Puzzle == itemData.itemType)
            {
                UiManager.bindNode<LoginRewardStarItemNode>(pool.cachedGameObject).setPuzzlePack(itemData.type);
            }
            else
            {
                UiManager.bindNode<LoginRewardItemNode>(pool.cachedGameObject).setNum(itemData.itemType, itemData.amount);
            }
            var poolScale = pool.cachedRectTransform.localScale;
            poolScale.Set(scale, scale, scale);
            pool.cachedRectTransform.localScale = poolScale;
            pool.cachedGameObject.setActiveWhenChange(true);
            return pool;
        }

        public static StampNode addStampItem(RectTransform parent)
        {
            var poolObj = ResourceManager.instance.getObjectFromPoolWithResOrder($"{iconPath}/login_item_stamp", parent,resNames: AssetBundleData.getBundleName(BundleType.LoginReward));
            var newPos = poolObj.cachedRectTransform.anchoredPosition3D;
            newPos.Set(0, -18, 0);
            poolObj.cachedRectTransform.anchoredPosition3D = newPos;
            return UiManager.bindNode<StampNode>(poolObj.cachedGameObject);
        }

        public static void addPlusItem(RectTransform parent)
        {
            ResourceManager.instance.getObjectFromPoolWithResOrder($"{iconPath}/login_item_plus", parent, resNames: AssetBundleData.getBundleName(BundleType.LoginReward));
        }

    }

    public enum RewardType
    {
        Week,
        Month,
    }

    public class DayItemData
    {
        public DayItemType itemType;
        public ulong amount;
        public string type;
        public int level;

        Dictionary<string, DayItemType> dayItemType = new Dictionary<string, DayItemType>()
        {
            { UtilServices.outcomeCoinKey,DayItemType.Coin},
            { UtilServices.outcomeExpBoost,DayItemType.Exp},
            { UtilServices.outcomePuzzlePack,DayItemType.Puzzle },
            { UtilServices.outcomePuzzleVoucher,DayItemType.Puzzle },
            { "level-up-boost",DayItemType.LvUp},
            { "high-roller-point",DayItemType.HighRollerPoint},
            { UtilServices.outcomeHighPassPoint,DayItemType.HighRollerPassPoint}
        };
        public void parseItemType(RewardItem rewardItem)
        {
            amount = rewardItem.amount;
            type = rewardItem.type;
            level = rewardItem.level;

            if (!dayItemType.TryGetValue(rewardItem.kind, out itemType))
            {
                Debug.LogError($"get {rewardItem.kind} to ItemType is Error");
            }
        }
    }

    public enum DayItemType
    {
        Coin,
        Coupon,
        Exp,
        Puzzle,
        LvUp,
        HighRollerPoint,
        HighRollerPassPoint
    }

    public class DailyRewardSettings
    {
        public DailyRewardSetting[] DailyReward;
    }

    public class DailyRewardSetting
    {
        public string type;
        public int cumulativeDays;
        public DailyRewardMoney rewardMoney;
        public RewardItem[] rewardItems;
    }

    public class DailyRewardMoney
    {
        public int odds;
        public int amount;
    }

    public class RewardItem
    {
        public string kind;
        public string type;
        public ulong amount;
        public int level;
    }

    public class DailyRewardDatas
    {
        public DailyRewards dailyRewards;
        public CommonReward[] commonRewards;
    }
}
