using Services;
using System;
using UniRx;
using System.Collections.Generic;
using CommonILRuntime.Outcome;
using LobbyLogic.NetWork.ResponseStruct;
using Mission;
using Service;
using CommonService;
using LobbyLogic.NetWork;
using Network;
using Debug = UnityLogUtility.Debug;
using Lobby.Common;

namespace SaveTheDog
{
    #region Save The Dog Data

    public class LevelSetting
    {
        public string kind;
        public string type;
        public string role;
        public Reward[] rewards;
    }

    public class Stage
    {
        public int maxLevel;
        public LevelSetting[] levelSettings;
    }

    public class NewbieAdventureSettingData
    {
        public int maxStage;
        public Stage[] stages;
    }

    public class NewbieAdventureSetting
    {
        public NewbieAdventureSettingData newbieAdventureSetting;
    }
    #endregion

    public enum SaveDogLvKind
    {
        None,
        Slot,
        Gift,
        Doge,
        Treasure,
    }

    /// <summary>
    /// 關卡資訊結構
    /// </summary>
    public class LevelData
    {
        public string kind { get; private set; }
        public string type { get; private set; }
        public string role { get; private set; }
        public SaveDogLvKind lvKind { get { return _lvKind; } }
        SaveDogLvKind _lvKind;
        public void setData(LevelSetting setting)
        {
            this.kind = setting.kind;
            this.type = setting.type;
            this.role = setting.role;
            UtilServices.enumParse(kind, out _lvKind);
        }
    }
    public class SaveTheDogMapData
    {
        public static SaveTheDogMapData instance = new SaveTheDogMapData();

        public ulong treasureRewardMoney { get; private set; }
        public ulong totalRewardMoney { get; private set; }
        public string endTime { get; private set; }
        public int activityDays;
        public int maxStageAmount { get; private set; }
        public List<int> maxLevelAmountList { get; private set; } = new List<int>();
        public List<List<LevelData>> levelInfoList = new List<List<LevelData>>();
        public Dictionary<string, Reward[]> rewardInfoDict = new Dictionary<string, Reward[]>();
        public Dictionary<int, Dictionary<int, Reward[]>> giftRewardInfoDict { get; private set; } = new Dictionary<int, Dictionary<int, Reward[]>>();
        //public List<Reward[]> giftReward { get; private set; } = new List<Reward[]>();
        public int nowStageID { get; private set; }
        public int nowOpenStageID { get; private set; }
        public int nowLvID { get; private set; }
        public bool isLvAlreadyOpen { get; private set; }
        public int nowClickID { get; private set; }
        public bool isAlreadyPlayGrow { get; private set; } = true;

        public bool isSkipSaveTheDog = false;

        public bool isAlreadyReward { get; private set; }
        public bool isOpenSaveTheDog = false;
        public bool isFirstLv { get { return nowStageID <= 0 && nowLvID <= 0; } }
        public Subject<bool> isUpdateRecord = new Subject<bool>();
        public Subject<int> nowOpenStageIDSub = new Subject<int>();

        public readonly string transitionBallonPath = "prefab/transition/transition_ballon";

        SaveDogLvKind nowLvKind = SaveDogLvKind.None;
        public bool isDogGuideComplete;
        public NewbieRecord nowRecord { get; private set; }

        public void setNowAdventureRecord(NewbieAdventure adventure)
        {
            convertRecordKind(adventure.record.kind);
            endTime = adventure.endAt;
            nowStageID = adventure.stage;
            nowLvID = adventure.level;
            nowRecord = adventure.record;
            isAlreadyReward = !string.IsNullOrEmpty(adventure.record.rewardedAt);
            setIsAlreadyOpen(!string.IsNullOrEmpty(adventure.record.noticedAt));
            ActivityQuestData.setMissionPacks(adventure.record.missions);
            nowOpenStageID = nowStageID;
            isDogGuideComplete = adventure.stage > 0;
            if (isDogGuideComplete && adventure.level > 0)
            {
                DataStore.getInstance.guideServices.setNowGameGuideStep((int)GameGuideStatus.Completed);
                DataStore.getInstance.guideServices.setNowStatus(GuideStatus.Completed.ToString());
            }
            if (!string.IsNullOrEmpty(adventure.record.completedAt))
            {
                nowClickID = nowLvID;
                getRedeem();
            }
        }

        async void getRedeem()
        {
            var newbieAdventure = await AppManager.lobbyServer.getNewbieAdventureRedeem();
            if (Result.NewbieDogAlreadyComplete == newbieAdventure.result)
            {
                isDogGuideComplete = true;
                DataStore.getInstance.guideServices.skipGuideStep();
                return;
            }
            updateAdventureRecord(newbieAdventure.adventureRecord);
        }

        public void updateAdventureRecord(NewbieAdventureRecord adventureRecord)
        {
            convertRecordKind(adventureRecord.record.kind);
            endTime = adventureRecord.endAt;
            nowStageID = adventureRecord.stage;
            nowLvID = adventureRecord.level;
            nowRecord = adventureRecord.record;
            isAlreadyReward = !string.IsNullOrEmpty(adventureRecord.record.rewardedAt);
            isDogGuideComplete = adventureRecord.stage > 0;
            setIsAlreadyOpen(!string.IsNullOrEmpty(adventureRecord.record.noticedAt));
            ActivityQuestData.setMissionPacks(adventureRecord.record.missions);
            ActivityQuestData.questGameID = adventureRecord.record.kind.Equals("slot") ? adventureRecord.record.type : "";
            isUpdateRecord.OnNext(true);
            changeNowOpenStageID(nowStageID);
        }

        public async void dogGameComplete()
        {
            if (false == checkClickIDAndStage())
            {
                DataStore.getInstance.extraGameServices.levelRewardSubject.OnNext(null);
                return;
            }

            var completeNotice = await AppManager.lobbyServer.setNewbieAdventureComplete();
            if (Result.OK != completeNotice.result)
            {
                DataStore.getInstance.extraGameServices.levelRewardSubject.OnNext(null);
                return;
            }
            var newbieAdventure = await AppManager.lobbyServer.getNewbieAdventureRedeem();
            if (Result.OK != newbieAdventure.result)
            {
                DataStore.getInstance.extraGameServices.levelRewardSubject.OnNext(null);
                return;
            }
            setIsAlreadyGrow(false);
            updateAdventureRecord(newbieAdventure.adventureRecord);
            var rewardPack = await AppManager.lobbyServer.getRewardPacks(newbieAdventure.rewardPackId);
            DataStore.getInstance.extraGameServices.levelRewardSubject.OnNext(rewardPack.rewards);
            var outcome = Outcome.process(rewardPack.rewards);
            outcome.apply();
        }

        public void checkNotice(bool isOpen)
        {
            int noticeAmount = isOpen ? 0 : 1;
            //Debug.Log($"Doge Notice : {noticeAmount} !!!");
            NoticeManager.instance.setDogEvent(noticeAmount);
        }

        public void setIsAlreadyOpen(bool isOpen)
        {
            checkNotice(isOpen);
            isLvAlreadyOpen = isOpen;
        }

        public void setIsAlreadyGrow(bool enable)
        {
            isAlreadyPlayGrow = enable;
        }

        public void changeNowOpenStageID(int nowOpenID)
        {
            if (nowOpenStageID == nowOpenID)
            {
                return;
            }
            nowOpenStageID = nowOpenID;
            nowOpenStageIDSub.OnNext(nowOpenID);
        }

        public void setNowClickLvID(int clickLvID)
        {
            nowClickID = clickLvID;
        }

        public void setMapInfo(NewbieAdventureSettingData mapData)
        {
            totalRewardMoney = 0;
            maxStageAmount = mapData.maxStage;
            for (var i = 0; i < maxStageAmount; i++)
            {
                maxLevelAmountList.Add(mapData.stages[i].maxLevel);
            }
            setLvInfos(mapData);
        }

        public void setLvInfos(NewbieAdventureSettingData mapData)
        {
            levelInfoList.Clear();
            rewardInfoDict.Clear();
            giftRewardInfoDict.Clear();
            for (var i = 0; i < mapData.stages.Length; i++)
            {
                List<LevelData> levelDataList = new List<LevelData>();
                var stageInfo = mapData.stages[i].levelSettings;
                Dictionary<int, Reward[]> giftReward = new Dictionary<int, Reward[]>();
                for (var j = 0; j < stageInfo.Length; j++)
                {
                    var levelSetting = stageInfo[j];
                    LevelData levelInfo = new LevelData();
                    levelInfo.setData(levelSetting);
                    levelDataList.Add(levelInfo);
                    switch (levelInfo.lvKind)
                    {
                        case SaveDogLvKind.Slot:
                            rewardInfoDict.Add(levelSetting.type, levelSetting.rewards);
                            break;
                        case SaveDogLvKind.Gift:
                            giftReward.Add(j, levelSetting.rewards);
                            break;
                    }
                }
                giftRewardInfoDict.Add(i, giftReward);
                levelInfoList.Add(levelDataList);
            }
            addTotalRewardUp(mapData.stages[nowStageID]);
        }

        public Reward[] getGiftRewardInfos(int lv)
        {
            Dictionary<int, Reward[]> info = giftRewardInfoDict[nowOpenStageID];

            Reward[] result = new Reward[] { };

            if (!info.TryGetValue(lv, out result))
            {
                Debug.LogError($"get {nowOpenStageID} - {lv} Reward Info is empty");
            }
            return result;
        }

        void addTotalRewardUp(Stage nowStage)
        {
            for (int i = 0; i < nowStage.levelSettings.Length; ++i)
            {
                var lvSetting = nowStage.levelSettings[i];
                var rewards = lvSetting.rewards.GetEnumerator();
                SaveDogLvKind lvKind;
                UtilServices.enumParse(lvSetting.kind, out lvKind);
                while (rewards.MoveNext())
                {
                    var reward = rewards.Current as Reward;
                    if (reward.kind.Equals(UtilServices.outcomeCoinKey))
                    {
                        totalRewardMoney += reward.getAmount();

                        if (SaveDogLvKind.Treasure == lvKind)
                        {
                            treasureRewardMoney = reward.getAmount();
                        }
                    }
                }
            }
        }

        void convertRecordKind(string kind)
        {
            UtilServices.enumParse(kind, out nowLvKind);
        }

        public SaveDogLvKind getNowRecordKind()
        {
            return nowLvKind;
        }

        public bool checkClickIDAndStage()
        {
            if (nowClickID != nowLvID || nowOpenStageID != nowStageID)
            {
                return false;
            }

            return true;
        }
    }
}
