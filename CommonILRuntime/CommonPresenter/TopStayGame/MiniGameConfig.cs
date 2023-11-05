using UniRx;
using System.Collections.Generic;
using System;
using Services;
using Debug = UnityLogUtility.Debug;

namespace CommonPresenter
{
    public class MiniGameConfig
    {
        static MiniGameConfig _instance = new MiniGameConfig();
        public static MiniGameConfig instance { get { return _instance; } }

        public const float DELAY_TIME = 5.0f;
        public const float TRANSITIONS_DELAY_TIME = 0.5f;

        public Dictionary<StayGameType, StayGameData> stayGameDatas { get; private set; } = new Dictionary<StayGameType, StayGameData>();
        public List<int> expList { get; private set; } = new List<int>() { 0, 250, 1250, 4750, 10750, 22750 };
        public List<int> bonusList { get; private set; } = new List<int>() { 1, 2, 3, 4, 5, 10 };
        public List<string> boxRedeemStr { get; private set; } = new List<string>() { "silver-box", "golden-box", "wheel", "dice" };
        List<float> expUnitAmounts = new List<float>();

        public TopBarGameBonusInfo gameBonusInfo { get; private set; }

        public Subject<TopBarGameBonusInfo> topbarGameBonusSub { get; private set; } = new Subject<TopBarGameBonusInfo>();
        public Subject<ulong> gameBonusRedeemAmount { get; private set; } = new Subject<ulong>();
        public Subject<int> addBonusEnergySub { get; private set; } = new Subject<int>();

        CompareBonusTimeResult compareBonusTimeResult = new CompareBonusTimeResult();
        public List<StayGameType> loopTypes { get; private set; } = new List<StayGameType>();
        public Subject<List<StayGameType>> stayGameNoticeEvent = new Subject<List<StayGameType>>();

        public void setTopBarGameInfo(TopBarGameBonusInfo info)
        {
            gameBonusInfo = info;
            addStayGameDatas(StayGameType.gold, new StayGameData(endTimeStr: info.goldenBoxAvailableAfter));
            addStayGameDatas(StayGameType.silver, new StayGameData(endTimeStr: info.silverBoxAvailableAfter));
            topbarGameBonusSub.OnNext(info);
        }

        public void setBonusRedeemAmount(ulong amount)
        {
            gameBonusRedeemAmount.OnNext(amount);
        }

        public void addBonusEnergy(int amount)
        {
            addBonusEnergySub.OnNext(amount);
        }

        public void addStayGameDatas(StayGameType gameType, StayGameData stayGameData)
        {
            if (stayGameDatas.ContainsKey(gameType))
            {
                stayGameDatas[gameType] = stayGameData;
                return;
            }

            stayGameDatas.Add(gameType, stayGameData);
        }
        public StayGameData getStayGameData(StayGameType gameType)
        {
            StayGameData gameData = null;
            if (!stayGameDatas.TryGetValue(gameType, out gameData))
            {
                Debug.LogError($"get {gameType} StayGameData is null");
            }
            return gameData;
        }

        public CompareBonusTimeResult compareBonusTime()
        {
            compareBonusTimeResult.countdownTime = DateTime.MaxValue;
            compareBonusTimeResult.getRewardGameType = StayGameType.none;
            loopTypes.Clear();

            var stayGamesEnum = stayGameDatas.GetEnumerator();
            while (stayGamesEnum.MoveNext())
            {
                var gameData = stayGamesEnum.Current.Value;
                StayGameType stayGameType = stayGamesEnum.Current.Key;
                switch (stayGameType)
                {
                    case StayGameType.silver:
                    case StayGameType.gold:
                        DateTime time = stayGamesEnum.Current.Value.endTime;
                        if (CompareTimeResult.Earlier == UtilServices.compareTimeWithNow(time))
                        {
                            loopTypes.Add(stayGameType);
                            compareBonusTimeResult.getRewardGameType = stayGameType;
                            continue;
                        }
                        if (CompareTimeResult.Later == UtilServices.compareTimes(time, compareBonusTimeResult.countdownTime))
                        {
                            continue;
                        }
                        setCompareTimeResult(time);
                        break;

                    case StayGameType.wheel:
                        if (gameData.progress >= 5)
                        {
                            loopTypes.Add(stayGameType);
                        }
                        break;
                    case StayGameType.dice:
                        if (gameData.progress >= 3)
                        {
                            loopTypes.Add(stayGameType);
                        }
                        break;
                }
            }
            loopTypes.Add(StayGameType.none);
            loopTypes.Sort();
            stayGameNoticeEvent.OnNext(loopTypes);
            compareBonusTimeResult.isCountdownTime = CompareTimeResult.Earlier == UtilServices.compareTimes(compareBonusTimeResult.countdownTime, DateTime.MaxValue) && CompareTimeResult.Later == UtilServices.compareTimeWithNow(compareBonusTimeResult.countdownTime);
            return compareBonusTimeResult;
        }

        public void removeLoopType(StayGameType removeType)
        {
            loopTypes.Remove(removeType);
        }

        void setCompareTimeResult(DateTime time)
        {
            compareBonusTimeResult.countdownTime = time;
        }

        public float getExpAmout(int expID)
        {
            if (null == expUnitAmounts || expUnitAmounts.Count <= 0)
            {
                for (int i = 0; i < expList.Count - 1; ++i)
                {
                    expUnitAmounts.Add(expList[i + 1] - expList[i]);
                }
            }

            return expUnitAmounts[expID];
        }
    }
    public enum StayGameType : int
    {
        silver,
        gold,
        wheel,
        dice,
        none,
    }

    public class TopBarGameBonusInfo
    {
        public string silverBoxAvailableAfter;
        public string goldenBoxAvailableAfter;
        public int wheelEnergy;
        public int diceEnergy;
        public int multiplierEnergy;

        //public DateTime getSilverBoxTime()
        //{
        //    return UtilServices.strConvertToDateTime(silverBoxAvailableAfter, DateTime.MaxValue);
        //}

        //public DateTime getGoldenBoxTime()
        //{
        //    return UtilServices.strConvertToDateTime(goldenBoxAvailableAfter, DateTime.MaxValue);
        //}
    }
    public class BonusLevel
    {
        public string level;
        public int energy;

        public BonusLevel(string lv, int exp)
        {
            level = lv;
            energy = exp;
        }
    }

    public class CompareBonusTimeResult
    {
        public bool isCountdownTime;
        public DateTime countdownTime;
        //public StayGameType countTimeGameType;
        public StayGameType getRewardGameType;
    }
    public class StayGameData
    {
        public DateTime endTime { get; private set; }
        public int progress { get; private set; }

        public StayGameData(string endTimeStr = "", int nowProgress = 0)
        {
            if (!string.IsNullOrEmpty(endTimeStr))
            {
                endTime = UtilServices.strConvertToDateTime(endTimeStr, DateTime.MaxValue);
            }
            progress = nowProgress;
        }
    }
}
