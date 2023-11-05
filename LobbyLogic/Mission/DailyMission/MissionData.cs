using UnityEngine;
using Lobby.Common;
using Services;
using System;
using System.Collections.Generic;
using UniRx;
using System.Threading.Tasks;
using Service;
using LobbyLogic.NetWork.ResponseStruct;
using CommonService;
using CommonILRuntime.Services;

namespace Mission
{
    public static class MissionData
    {
        static MissionHelper missionHelper;
        static MissionContentFactory contentFactory;
        public static int normalRound { get; private set; }
        public static List<MedalRewardData> medalRewardDatas { get; private set; }
        public static int normalMaxRound { get; private set; }
        public static MissionProgressData normalMissionData { get; private set; }
        public static MissionProgressData specialMissionData { get; private set; }
        public static float missionMedalMaxCount { get; private set; }

        public static float missionMedalCount;

        public static DateTime totalMissionTime;
        public static int unLockLv
        {
            get
            {
                return 10;
            }
        }

        public static void initMissionProgressData()
        {
            if (!checkHaveInit())
            {
                normalMissionData = new MissionProgressData();
                specialMissionData = new MissionProgressData();
                medalRewardDatas = new List<MedalRewardData>();
                KeepAliveManager.Instance.isCrossDay.Subscribe(detectCrossDay);
                DataStore.getInstance.playerInfo.addPassPointSub.Subscribe(detectAddPassPoint);
                DataStore.getInstance.dailyMissionServices.askNewMissionSubject.Subscribe(noticeHaveNewMission);
                DataStore.getInstance.dailyMissionServices.collectCurrentRewardSubject.Subscribe(collectReward);
                updateData();
            }
        }

        public static void initAskUnLockLvSubject()
        {
            DataStore.getInstance.dailyMissionServices.askUnLockLvSubject.Subscribe((_) => DataStore.getInstance.dailyMissionServices.setUnlockLv(unLockLv));
        }

        static bool checkHaveInit()
        {
            return (null != normalMissionData && null != specialMissionData && null != medalRewardDatas);
        }

        static void collectReward(bool _)
        {
            var helper = getMissionHelper();
            if (normalMissionData.isComplete && !normalMissionData.isRedeem)
            {
                helper.askNormalReward(receiveMissionReward);
            }
            else if (specialMissionData.isComplete && !specialMissionData.isRedeem)
            {
                helper.askSpecialReward(receiveMissionReward);
            }
        }

        static async void receiveMissionReward(MissionHelper.RewardFormat _)
        {
            await updateData();
            noticeHaveNewMission(true);
        }

        static void detectCrossDay(bool isCross)
        {
            if (isCross)
            {
                updateData();
            }
        }

        public static async Task updateData()
        {
            if (unLockLv <= 0)
            {
                return;
            }

            var response = await AppManager.lobbyServer.getDailyMissionData();
            if (Network.Result.OK == response.result)
            {
                setMedalData(response.medal);
                setGeneralMissionData(response.generalMission);
                setSpeciallMissionData(response.specialMission);
            }
        }

        static void setMedalData(DailyMedalData data)
        {
            setMedalProgress(data);
            setMedalReward(data.stages);
        }

        static void setMedalProgress(DailyMedalData data)
        {
            missionMedalMaxCount = data.max;
            missionMedalCount = data.count;
            totalMissionTime = UtilServices.strConvertToDateTime(data.nextResetAt, DateTime.MinValue);
        }

        static void setMedalReward(DailyMissionStage[] stage)
        {
            int count = stage.Length;
            MedalRewardData rewardData = null;

            medalRewardDatas.Clear();
            for (int i = 0; i < count; ++i)
            {
                rewardData = new MedalRewardData();
                rewardData.updateData(stage[i], i);
                medalRewardDatas.Add(rewardData);
            }
        }

        static void setGeneralMissionData(DailyMissionGeneralInfo info)
        {
            normalMaxRound = info.max;
            normalMissionData.updateData(info);
            setNormalRound(info.index);
        }

        static void setSpeciallMissionData(DailyMissionInfo info)
        {
            specialMissionData.updateData(info);
        }

        public static void setNormalRound(int round)
        {
            normalRound = round;
        }

        public static TimeSpan getMissionRemainingTime()
        {
            return totalMissionTime.Subtract(UtilServices.nowTime);
        }

        public static bool tryGetCurrentMedalRewardIndex(ref int index)
        {
            int count = medalRewardDatas.Count;
            MedalRewardData data = null;

            for (int i = 0; i < count; ++i)
            {
                data = medalRewardDatas[i];
                if (data.quantityReached <= missionMedalCount && !data.checkHaveReceive())
                {
                    index = data.stageIndex;
                    return true;
                }
            }

            return false;
        }

        public static bool checkMedalIsReceive(int index)
        {
            return medalRewardDatas[index].checkHaveReceive();
        }

        public static bool checkNormalMissionCanReceive()
        {
            return checkMissionCanReceive(normalMissionData);
        }

        public static bool checkSpecialMissionCanReceive()
        {
            return checkMissionCanReceive(specialMissionData);
        }

        static bool checkMissionCanReceive(MissionProgressData progressData)
        {
            return progressData.isComplete && !progressData.isRedeem;
        }

        public static MissionHelper getMissionHelper()
        {
            if (null == missionHelper)
            {
                missionHelper = new MissionHelper();
            }
            return missionHelper;
        }

        public static MissionContentFactory getContentFactory()
        {
            if (null == contentFactory)
            {
                contentFactory = new MissionContentFactory();
            }

            return contentFactory;
        }

        static void detectAddPassPoint(long point)
        {
            updateProgress();
        }

        public static void updateProgress()
        {
            if (DataStore.getInstance.playerInfo.level < unLockLv || unLockLv <= 0)
            {
                DataStore.getInstance.eventInGameToLobbyService.SendEventEnd(FunctionNo.UpdateDailyMission);
                return;
            }

            var missionHelper = getMissionHelper();
            missionHelper.askProgress((response) =>
            {
                setProgressResponse(response);
                noticeGameProgressUpdate();
            });
        }

        static void setProgressResponse(DailyMissionProgressResponse response)
        {
            setMissionProgress(response.generalMission, normalMissionData);
            setMissionProgress(response.specialMission, specialMissionData);
        }

        static void setMissionProgress(DailyMissionProgressInfo info, MissionProgressData target)
        {
            target.updateProgress(info.amounts, info.completedAt);
        }

        static void noticeGameProgressUpdate()
        {
            var format = getToGameFormat();
            DataStore.getInstance.dailyMissionServices.setCurrentPorgress(format);
            DataStore.getInstance.eventInGameToLobbyService.SendEventEnd(FunctionNo.UpdateDailyMission);
        }

        public static bool checkNormalMissionIsOver()
        {
            return ((normalRound >= normalMaxRound) && normalMissionData.isComplete && normalMissionData.isRedeem);
        }

        public static void noticeHaveNewMission(bool _)
        {
            if (DataStore.getInstance.playerInfo.level < MissionData.unLockLv || 0 >= MissionData.unLockLv)
            {
                return;
            }

            var format = getToGameFormat();
            DataStore.getInstance.dailyMissionServices.setNewMission(format);
        }

        static DailyMissionServices.InfoFormat getToGameFormat()
        {
            DailyMissionServices.InfoFormat format = new DailyMissionServices.InfoFormat();

            format.normalMission = getGameDisplayInfo(normalMissionData);
            format.specialMission = getGameDisplayInfo(specialMissionData);

            return format;
        }

        static DailyMissionServices.MissionInfo getGameDisplayInfo(MissionProgressData data)
        {
            return new DailyMissionServices.MissionInfo()
            {
                isComplete = (data.isComplete && data.isRedeem),
                count = data.nowProgress,
                max = data.finalTarget,
                content = getContentFactory().createContentMsg(data),
                extraCount = getExtraProgress(data.nowMultipleProgress, data.condition),
                haveExtraCount = (1 < data.nowMultipleProgress.Length),
                isNeedNotice = data.isNeedNotice
            };
        }

        static decimal getExtraProgress(decimal[] progress, decimal[] condition)
        {
            if (1 < progress.Length)
            {
                return condition[1] - progress[1];
            }

            return 0;
        }
    }

    public class MissionProgressData
    {
        public MissionReward rewardData { get; private set; }
        public string contentKey { get; private set; }
        public decimal finalTarget { get { return condition[0]; } }
        public decimal nowProgress { get { return nowMultipleProgress[0]; } }
        public decimal[] nowMultipleProgress { get; private set; }
        public decimal[] condition { get; private set; }
        public DateTime resetTime { get; private set; }
        public int medalReward { get; private set; }
        public bool isComplete { get; private set; }
        public bool isRedeem { get; private set; }
        public bool isNeedNotice { get; private set; }
        string completedAt;

        public void updateProgress(decimal[] progress, string completedAt)
        {
            isComplete = !string.IsNullOrEmpty(completedAt);
            isNeedNotice = isComplete && string.IsNullOrEmpty(this.completedAt);
            this.completedAt = completedAt;
            nowMultipleProgress = progress;
        }

        public void updateData(DailyMissionInfo info)
        {
            var pack = info.mission;
            var progress = pack.progress;

            contentKey = UtilServices.toTitleCase(progress.type);
            condition = progress.conditions;
            nowMultipleProgress = progress.amounts;
            medalReward = info.rewardMedalPoint;
            resetTime = UtilServices.strConvertToDateTime(info.nextAssignAt, DateTime.MinValue);
            rewardData = pack.reward;
            isComplete = !string.IsNullOrEmpty(pack.completedAt);
            isRedeem = !string.IsNullOrEmpty(pack.redeemAt);
            completedAt = pack.completedAt;
        }
    }

    public class MedalRewardData
    {
        public int stageIndex { get; private set; }
        public int quantityReached { get; private set; }
        public MissionReward[] rewardDatas { get; private set; }
        public string redeemAtTime { get; private set; }

        public void updateData(DailyMissionStage stage, int stageIndex)
        {
            this.stageIndex = stageIndex;
            quantityReached = stage.quantityReached;
            redeemAtTime = stage.redeemAt;
            rewardDatas = stage.rewards;
        }

        public bool checkHaveReceive()
        {
            return !string.IsNullOrEmpty(redeemAtTime);
        }
    }
}