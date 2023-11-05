using System.Collections.Generic;
using Debug = UnityLogUtility.Debug;
using LobbyLogic.NetWork.ResponseStruct;
using CommonILRuntime.Outcome;
using Services;
using UniRx;
using UnityEngine;
using Lobby.Common;
using CommonILRuntime.Extension;
using EventActivity;
using CommonService;
using System;
using Game.Common;

namespace Mission
{
    public struct Quest
    {
        public string type;
        public decimal[] questConditions;
    }

    public static class ActivityQuestConfig
    {
        public static readonly string spinTimes = "spin-times";
        public static readonly string continuousWinTimes = "continuous-win-times";
        public static readonly string cumulativeWinTimes = "cumulative-win-times";
        public static readonly string cumulativeTotalBet = "cumulative-total-bet";
        public static readonly string cumulativeTotalWin = "cumulative-total-win";
        public static readonly string cumulativeAwardBoardTimes = "cumulative-award-board-times";
        public static readonly string levelUpTimes = "level-up-times";
        public static readonly string gainSlotFiveLines = "gain-slot-five-lines";
        public static readonly string winOverAndTimesInSingleBet = "win-over-and-times-in-single-bet";
        public static readonly string featureGameTimes = "feature-game-times";
        public static readonly string maxBetTimes = "max-bet-times";
    }

    public static class ActivityQuestData
    {
        public static MissionPack[] missions { get; private set; }
        public static Subject<NewbieAdventureMissionProgress> missionProgressUpdate = new Subject<NewbieAdventureMissionProgress>();
        static Dictionary<string, string> questImageConvert = new Dictionary<string, string>()
        {
            { ActivityQuestConfig.spinTimes,"icon_spins"},
            { ActivityQuestConfig.continuousWinTimes,"icon_win_combo"},
            { ActivityQuestConfig.cumulativeWinTimes,"icon_win"},
            { ActivityQuestConfig.cumulativeTotalBet,"icon_bet"},
            { ActivityQuestConfig.cumulativeTotalWin,"icon_total_win"},
            { ActivityQuestConfig.cumulativeAwardBoardTimes,"icon_big_win"},
            { ActivityQuestConfig.levelUpTimes,"icon_level_up"},
            { ActivityQuestConfig.gainSlotFiveLines,"icon_5_lines"},
            { ActivityQuestConfig.winOverAndTimesInSingleBet,"icon_win_over"},
            { ActivityQuestConfig.featureGameTimes,"icon_feature"},
            { ActivityQuestConfig.maxBetTimes,"icon_max_bet"},
        };

        public static int nowTicketAmount = 0;
        public static int maxTicketAmount = 0;
        public static int progressPercentage = 0;
        private static ActivityIconData _nowActivityObjData;
        public static ActivityID nowActivityID;
        public static bool isActivityExist = false;
        public static List<ulong> dogQuestProgress;
        public static string questGameID = "";
        public static GameConfig.GameState gameState;
        public static bool isRewardCanShow = false;
        public static float fakePrecentage = 0.95f;

        public static bool isPropAmountMax()
        {
            return nowTicketAmount >= maxTicketAmount;
        }

        public static void checkAutoSpinIsStop()
        {
            int questCompleteAmount = 0;
            int questConditionsAmount = missions.Length;
            ulong conditionAount = 0;
            string conditionMsg = "";
            for (var i = 0; i < questConditionsAmount; i++)
            {
                conditionMsg = missions[i].progress.conditions[0].ToString();
                ulong.TryParse(conditionMsg, out conditionAount);
                if (dogQuestProgress[i] >= conditionAount)
                {
                    questCompleteAmount++;
                }
            }
            
            if (questCompleteAmount >= questConditionsAmount)
            {
                DataStore.getInstance.lobbyToGameServices.cancelCurrentAutoPlay();
            }
        }

        public static void updateDogQuestProgress(NewbieAdventureMissionProgress questProgress)
        {
            if (null == questProgress.missions || questProgress.missions.Length <= 0)
            {
                return;
            }

            dogQuestProgress = new List<ulong>();
            ulong progress = 0;
            for (var i = 0; i < questProgress.missions.Length; i++)
            {
                progress = (ulong)questProgress.missions[i].amounts[0];
                dogQuestProgress.Add(progress);
            }
        }

        public static void setActivityData(ActivityPropResponse propRes)
        {
            nowActivityID = ActivityDataStore.getNowActivityID();
            nowTicketAmount = propRes.prop.amount;
            maxTicketAmount = propRes.prop.maximum;
        }

        public static ActivityIconData nowActivityObjData
        {
            get
            {
                if (null == _nowActivityObjData)
                {
                    EventBarDataConfig.activityObjData.TryGetValue(nowActivityID, out _nowActivityObjData);
                }
                return _nowActivityObjData;
            }
        }

        public static Sprite getQuestImage(string serverKey)
        {
            string imageName;
            if (!questImageConvert.TryGetValue(serverKey, out imageName))
            {
                return null;
            }
            return LobbySpriteProvider.instance.getSprite<ActivityQuestProvider>(LobbySpriteType.ActivityQuest, imageName);
        }

        public static void setMissionPacks(MissionPack[] packs)
        {
            missions = packs;
        }

        public static string[] convertToConditionsMsg(params decimal[] condition)
        {
            int count = condition.Length;
            string[] result = new string[count];
            ulong reward = 0;

            for (int i = 0; i < count; ++i)
            {
                reward = (ulong)condition[i];
                result[i] = reward.convertToCurrencyUnit(3, false);
            }

            return result;
        }

        public static string getQuestInfoContent(string questInfo, params string[] questCondition)
        {
            try
            {
                string keyStr = $"{UtilServices.toTitleCase(questInfo)}_Quest";
                return string.Format(LanguageService.instance.getLanguageValue(UtilServices.toTitleCase(keyStr)), questCondition);
            }
            catch (Exception e)
            {
                string keyStr = $"{UtilServices.toTitleCase(questInfo)}_Quest";
                Debug.LogError($"Pao {e}   , questInfo : {questInfo}, key {keyStr}, conQuestInfo : {UtilServices.toTitleCase(keyStr)} , questConditionL : {questCondition.Length} , laVa : {LanguageService.instance.getLanguageValue(UtilServices.toTitleCase(keyStr))}");
                return string.Empty;
            }
        }

        public static Sprite getIconSprite(string spriteName)
        {
            string spritePath = $"bar_icon_{spriteName}";
            return LobbySpriteProvider.instance.getSprite<EventActivitySpriteProvider>(LobbySpriteType.EventActivity, spritePath);
        }
    }
}
