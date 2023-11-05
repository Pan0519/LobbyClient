using System.Collections.Generic;
using System.Threading.Tasks;
using Debug = UnityLogUtility.Debug;
using System;
using System.Linq;
using LitJson;
using UnityEngine;
using Services;
using UniRx;
using System.IO;
using UnityEngine.SceneManagement;
using CommonILRuntime.Outcome;

namespace CommonService
{
    public class DataInfo
    {
        public string sessionSid { get; private set; } = string.Empty;
        public string dllVersion { get { return "1.0.0"; } }
        public int gameBetTotalCount { get { return 20; } }
        public int gameBetIncreaseRange { get { return 5; } }

        /// <summary>
        /// 是否為iOS送審版
        /// </summary>
        public bool isiOSSubmit { get { return ApplicationConfig.Environment.Dev != ApplicationConfig.environment && (ApplicationConfig.NowRuntimePlatform == RuntimePlatform.IPhonePlayer || ApplicationConfig.NowRuntimePlatform == RuntimePlatform.OSXEditor); } }

        public string bonusTimeStr { get; private set; } = string.Empty;
        public Subject<string> bonusTimeSuscribe = new Subject<string>();
        public Subject<string> specialTimeSubscribe = new Subject<string>();
        public Subject<RewardPacks> lvupRewardSubscribe = new Subject<RewardPacks>();
        string nowPlayGameID = string.Empty;

        //public bool isContainReleaseHideGameID(string gameId)
        //{
        //    if (releaseGameId.Length <= 0)
        //    {
        //        return false;
        //    }

        //    return Array.Exists(releaseGameId, id => id.Equals(gameId));
        //}

        public void setAfterBonusTime(string bonusTime)
        {
            bonusTimeStr = bonusTime;
            bonusTimeSuscribe.OnNext(bonusTime);
        }

        public void setLoginResponse(string sid, Dictionary<string, int> setting)
        {
            sessionSid = sid;
            settings = setting;
        }

        public Dictionary<string, int> settings { get; private set; } = new Dictionary<string, int>();

        public Dictionary<string, GameInfo> gameInfoDicts = new Dictionary<string, GameInfo>();

        public List<GameInfo> onLineGameInfos = new List<GameInfo>();

        public Dictionary<string, string> areaCode;

        JsonBetList betList = null;

        Dictionary<string, Dictionary<string, BetBase>> betBases = null;

        public string deviceId
        {
            get
            {
                return ApplicationConfig.deviceID;
            }
        }

        public int playGameBetID { get; set; } = 0;

        public void setPlayGameBetId(int id)
        {
            playGameBetID = id;
        }

        public async Task<string> getNowplayGameID()
        {
            if (string.IsNullOrEmpty(nowPlayGameID) && (ApplicationConfig.NowRuntimePlatform == RuntimePlatform.WindowsEditor || ApplicationConfig.NowRuntimePlatform == RuntimePlatform.OSXEditor))
            {
                nowPlayGameID = await getDefaultID();
            }
            //if (string.IsNullOrEmpty(_nowPlayGameID))
            //{
            //    Debug.LogError($"get now play GameID is null,check DataInfo");
            //}
            return nowPlayGameID;
        }

        public void setNowPlayGameID(string gameID)
        {
            nowPlayGameID = gameID;
        }

        public void resetNowPlayGameID()
        {
            nowPlayGameID = string.Empty;
        }

        async Task<string> getDefaultID()
        {
            string result = string.Empty;
            switch (ApplicationConfig.NowRuntimePlatform)
            {
                case RuntimePlatform.WindowsEditor:
                    string gameListJson = await WebRequestText.instance.loadTextFromServer("game_list");
                    GameInfos gameInfos = JsonMapper.ToObject<GameInfos>(gameListJson);

                    string[] filesName = Directory.GetFiles(Path.Combine(ApplicationConfig.getStreamingPath, "ILRuntime"), "*.dll");
                    for (int i = 0; i < filesName.Length; ++i)
                    {
                        string fileName = Path.GetFileName(filesName[i]);
                        GameInfo info = Array.Find(gameInfos.Game, gameInfo => fileName.StartsWith(gameInfo.name));
                        if (null != info)
                        {
                            result = info.id;
                            break;
                        }
                    }
                    break;

            }
            return result;
        }

        public ChooseBetClass chooseBetClass { get; private set; } = new ChooseBetClass()
        {
            Type = ChooseBetClass.Regular,
            BetId = 0,
        };

        public void setChooseBetClass(string type, int betID)
        {
            chooseBetClass.Type = type;
            chooseBetClass.BetId = betID;
        }

        Dictionary<string, BetClass> betClassDict = new Dictionary<string, BetClass>()
        {
            { ChooseBetClass.High_Roller,BetClass.HighRoller},
            { ChooseBetClass.Regular,BetClass.Regular},
            { ChooseBetClass.Adventure,BetClass.Adventure},
        };
        public BetClass getChooseBetClassType()
        {
            BetClass result;
            if (betClassDict.TryGetValue(chooseBetClass.Type, out result))
            {
                return result;
            }
            return BetClass.Regular;
        }

        public void setGameInfos(GameInfos infos)
        {
            for (int i = 0; i < infos.Game.Length; ++i)
            {
                GameInfo info = infos.Game[i];
                if (gameInfoDicts.ContainsKey(info.id))
                {
                    continue;
                }

                gameInfoDicts.Add(info.id, info);
            }
        }

        public void setAreaCode(Area_codes codes)
        {
            areaCode = codes.areaCode;
        }
        #region getPlayerNowBetData
        public async Task<List<long>> getNowPlayerBetList()
        {
            BetClass chooseBetClass = getChooseBetClassType();
            return await getNowPlayerBetList(chooseBetClass);
        }

        public async Task<List<long>> getNowPlayerBetList(BetClass betClass)
        {
            switch (betClass)
            {
                case BetClass.HighRoller:
                    return await getNowPlayerHighRollerBetList();
                default:
                    return await getNowPlayerRegularBetList();
            }
        }

        public async Task<List<GameBetInfo>> getPlayerNowBetDataInfos()
        {
            BetClass chooseBetClass = getChooseBetClassType();
            switch (chooseBetClass)
            {
                case BetClass.HighRoller:
                    return await getNowPlayerHighRollerBetDataInfoList();
                default:
                    return await getNowRegularBetDataInfoList();
            }
        }
        #endregion
        #region getRegularBetInfo
        public async Task<List<GameBetInfo>> getNowRegularBetDataInfoList()
        {
            return await getRegularBetDataInfos(DataStore.getInstance.playerInfo.level);
        }
        public async Task<List<GameBetInfo>> getRegularBetDataInfos(int lv)
        {
            List<GameBetInfo> result = new List<GameBetInfo>();

            var totalList = await getPlayerRegularBetList(lv);
            if (totalList.Count <= DataStore.getInstance.dataInfo.gameBetTotalCount)
            {
                for (int i = 0; i < totalList.Count; ++i)
                {
                    result.Add(new GameBetInfo()
                    {
                        bet = totalList[i],
                        totalBetID = i,
                    });
                }
                return result;
            }

            int betPercent = totalList.Count;
            int increaseNum = 0;
            for (int i = 0; i < DataStore.getInstance.dataInfo.gameBetTotalCount; ++i)
            {
                increaseNum += DataStore.getInstance.dataInfo.gameBetIncreaseRange;
                int betID = ((betPercent * increaseNum) / 100) - 1;
                var betInfo = new GameBetInfo()
                {
                    bet = totalList[betID],
                    totalBetID = betID,
                };
                result.Add(betInfo);
            }

            return result;
        }
        public async Task<List<long>> getPlayerRegularBetList(long betLv)
        {
            if (null == betList)
            {
                string jsonFile = await WebRequestText.instance.loadTextFromServer("bet_list");
                betList = JsonMapper.ToObject<JsonBetList>(jsonFile);
            }

            var lastBetId = Array.FindLastIndex(betList.level, lv => lv <= betLv) + 1;
            long[] result = new long[lastBetId];
            Array.Copy(betList.bet_list, result, lastBetId);
            return result.ToList();
        }
        public async Task<List<long>> getNowPlayerRegularBetList()
        {
            return await getPlayerRegularBetList(DataStore.getInstance.playerInfo.level);
        }
        #endregion
        #region getHighRollerBetInfo
        List<float> topFiveBetPercent = new List<float>() { 0.3f, 0.4f, 0.5f, 0.7f, 1.0f };
        public async Task<List<long>> getNowPlayerHighRollerBetList()
        {
            var betInfos = await getNowPlayerHighRollerBetDataInfoList();
            List<long> result = new List<long>();
            for (int i = 0; i < betInfos.Count; ++i)
            {
                result.Add(betInfos[i].bet);
            }

            return result;
        }
        public async Task<List<GameBetInfo>> getNowPlayerHighRollerBetDataInfoList()
        {
            return await getHighRollerBetDataInfoList(DataStore.getInstance.playerInfo.level);
        }
        public async Task<List<GameBetInfo>> getHighRollerBetDataInfoList(int playerLv)
        {
            List<GameBetInfo> result = new List<GameBetInfo>();
            if (null == betList)
            {
                string jsonFile = await WebRequestText.instance.loadTextFromServer("bet_list");
                betList = JsonMapper.ToObject<JsonBetList>(jsonFile);
            }

            var lastBetID = Array.FindLastIndex(betList.level, lv => lv <= playerLv) + 1;

            if (lastBetID < topFiveBetPercent.Count)
            {
                for (int i = 0; i < topFiveBetPercent.Count; ++i)
                {
                    result.Add(new GameBetInfo()
                    {
                        bet = betList.bet_list[i],
                        totalBetID = i
                    });
                }
            }
            else
            {
                for (int i = 0; i < topFiveBetPercent.Count; ++i)
                {
                    int betID = (int)Math.Truncate(lastBetID * topFiveBetPercent[i]) - 1;
                    result.Add(new GameBetInfo()
                    {
                        bet = betList.bet_list[betID],
                        totalBetID = betID,
                    });
                }
            }

            for (int i = 0; i < 15; ++i)
            {
                long betID = lastBetID + 7 + i - 1;
                result.Add(new GameBetInfo()
                {
                    bet = betList.bet_list[betID],
                    totalBetID = (int)betID,
                });
            }

            return result;
        }
        #endregion
        #region GameJP
        public async Task<long> getRegularMaxJP(string gameID)
        {
            var playerBet = await getNowRegularBetDataInfoList();
            var betBase = await getGameBetBaseFromID(gameID);
            return playerBet[playerBet.Count - 1].bet * betBase[ChooseBetClass.Regular].upAmount;
        }
        public async Task<long> getHighRollerMaxJP(string gameID)
        {
            var playerBet = await getNowPlayerHighRollerBetDataInfoList();
            var betBase = await getGameBetBaseFromID(gameID);
            return playerBet[playerBet.Count - 1].bet * betBase[ChooseBetClass.High_Roller].upAmount; ;
        }
        #endregion
        public async Task<Dictionary<string, GameInfo>> initGameInfos()
        {
            if (null == gameInfoDicts || gameInfoDicts.Count <= 0)
            {
                string jsonFile = await WebRequestText.instance.loadTextFromServer("game_list");
                setGameInfos(JsonMapper.ToObject<GameInfos>(jsonFile));
            }

            return gameInfoDicts;
        }
        public async Task<Dictionary<string, GameInfo>> singleGameInitGameInfo()
        {
            await getNowplayGameID();
            return await initGameInfos();
        }
        public async Task<Dictionary<string, BetBase>> getGameBetBase()
        {
            await initBetBase();
            string nowGameID = await getNowplayGameID();
            return await getGameBetBaseFromID(nowGameID);
        }
        public async Task<Dictionary<string, BetBase>> getGameBetBaseFromID(string gameID)
        {
            await initBetBase();
            Dictionary<string, BetBase> result = null;
            if (!betBases.TryGetValue(gameID, out result))
            {
                Debug.LogError($"get Game-{gameID} betBase is null");
            }
            return result;
        }

        async Task<Dictionary<string, Dictionary<string, BetBase>>> initBetBase()
        {
            if (null == betBases || betBases.Count <= 0)
            {
                betBases = new Dictionary<string, Dictionary<string, BetBase>>();
                string jsonFile = await WebRequestText.instance.loadTextFromServer("bet_base");
                BetBaseGame baseGame = JsonMapper.ToObject<BetBaseGame>(jsonFile);
                for (int i = 0; i < baseGame.games.Count; ++i)
                {
                    Room room = baseGame.games[i];
                    if (betBases.ContainsKey(room.id))
                    {
                        continue;
                    }
                    betBases.Add(room.id, room.betBase);
                }
            }

            return betBases;
        }

        public async Task<GameInfo> getNowPlayGameInfo()
        {
            string gameID = await getNowplayGameID();
            return getGameInfo(gameID);
        }

        public GameInfo getGameInfo(string gameID)
        {
            GameInfo result = null;
            if (!gameInfoDicts.TryGetValue(gameID, out result))
            {
                Debug.Log($"Get {gameID} Info is null");
            }
            return result;
        }

        public void setLvupRewardSubject(RewardPacks rewards)
        {
            lvupRewardSubscribe.OnNext(rewards);
        }

        Dictionary<string, long> lvUpRewardDatas = new Dictionary<string, long>();
        public void setLvupRewardData(string kind, long amount)
        {
            if (lvUpRewardDatas.ContainsKey(kind))
            {
                lvUpRewardDatas[kind] = amount;
                return;
            }
            lvUpRewardDatas.Add(kind, amount);
        }

        public long getLvupRewardData(string kind)
        {
            long result = 0;
            if (!lvUpRewardDatas.TryGetValue(kind, out result))
            {
                //Debug.LogError($"get {kind} lvup reward is empty");
            }
            return result;
        }

        public async Task<GameOrientation> getNowGameOrientation()
        {
            if (string.IsNullOrEmpty(nowPlayGameID))
            {
                return GameOrientation.Landscape;
            }
            GameInfo nowGameInfo = await DataStore.getInstance.dataInfo.getNowPlayGameInfo();
            if (null == nowGameInfo)
            {
                return GameOrientation.Landscape;
            }
            return nowGameInfo.getOrientation();
        }
    }

    #region classes
    public class GameBetInfo
    {
        public long bet;
        public int totalBetID;
    }
    public class GameInfos
    {
        public GameInfo[] Game;
    }
    public class GameInfo
    {
        public string id;
        public string name;
        public string name_cht;
        public bool onLine { get; private set; }
        public string orientation { get; private set; }
        public string loadingBGColor;
        public bool open { get; private set; }
        //public bool customOpen { get; private set; }
        public bool isUnLock { get { return DataStore.getInstance.playerInfo.level > requiredLevel; } }
        public int priority;
        public int requiredLevel;
        public string[] labels;
        public string[] tags;
        public long jackpotMultiplier;

        public string releaseServer;
        public string stableServer;
        public string devServer;

        public string serverIP
        {
            get
            {
                switch (ApplicationConfig.environment)
                {
                    case ApplicationConfig.Environment.Prod:
                        return "https://gs.diamondcrush.com.tw";
                    case ApplicationConfig.Environment.Stage:
                        return $"http://gs.stg.diamondcrush.com.tw";
                    case ApplicationConfig.Environment.Outer:
                        return releaseServer;
                    case ApplicationConfig.Environment.Inner:
                        return stableServer;
                    default:
                        return devServer;
                }
            }
        }

        public void checkIsOnline(string visibleAfter, string visibleBefore)
        {
            onLine = isShowTime(visibleAfter, visibleBefore);
        }

        public void checkIsOpen(string availableAfter, string availableBefore)
        {
            open = isShowTime(availableAfter, availableBefore);
        }

        public GameOrientation getOrientation()
        {
            GameOrientation result = GameOrientation.None;
            UtilServices.enumParse<GameOrientation>(orientation, out result);
            return result;
        }

        bool isShowTime(string afterTimeStr, string beforeTimeStr)
        {
            if (string.IsNullOrEmpty(afterTimeStr) && string.IsNullOrEmpty(beforeTimeStr))
            {
                return true;
            }

            DateTime afterTime = UtilServices.strConvertToDateTime(afterTimeStr, DateTime.MinValue);
            DateTime beforeTime = UtilServices.strConvertToDateTime(beforeTimeStr, DateTime.MaxValue);
            DateTime nowTime = UtilServices.nowTime;
            if (nowTime > afterTime && nowTime < beforeTime)
            {
                return true;
            }
            return false;
        }
    }

    public enum GameOrientation
    {
        Landscape,
        Portrait,
        None,
    }

    public class Area_codes
    {
        public Dictionary<string, string> areaCode;
    }
    public class JsonBetList
    {
        public long[] bet_list;
        public long[] level;
    }
    public class ChooseBetClass
    {
        public string Type;
        public int BetId;

        public const string Regular = "regular";
        public const string High_Roller = "high-roller";
        public const string Adventure = "adventure";
    }

    public enum BetClass
    {
        Regular,
        HighRoller,
        Adventure,
    }

    #region Betbase
    public class BetBaseGame
    {
        public List<Room> games;
    }

    public class Room
    {
        public string id;
        public Dictionary<string, BetBase> betBase;
    }

    public class BetBase
    {
        public float percent;
        public int upAmount;
        public int downAmount;
    }
    #endregion
    #region Rewards
    public class RewardPacks
    {
        public Dictionary<PurchaseItemType, Pack> rewards;
    }

    public class Pack
    {
        public CommonReward outcome;
    }
    #endregion
    #endregion
}
