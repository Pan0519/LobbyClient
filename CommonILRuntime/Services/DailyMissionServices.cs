using CommonService;
using Services;
using UniRx;

namespace CommonILRuntime.Services
{
    public class DailyMissionServices
    {
        public Subject<InfoFormat> progressSubject = new Subject<InfoFormat>();
        public Subject<MissionInfo> newMissionSubject = new Subject<MissionInfo>();
        public Subject<bool> unlockSubject = new Subject<bool>();
        public Subject<bool> askUnLockLvSubject = new Subject<bool>();
        public Subject<bool> askNewMissionSubject = new Subject<bool>();
        public Subject<bool> collectCurrentRewardSubject = new Subject<bool>();

        public void askNewMission()
        {
            askNewMissionSubject.OnNext(true);
        }

        public void askIsUnLock()
        {
            askUnLockLvSubject.OnNext(true);
        }

        public void collectCurrentReward()
        {
            collectCurrentRewardSubject.OnNext(true);
        }

        public void setUnlockLv(float unLockLv)
        {
            bool isUnLock = (DataStore.getInstance.playerInfo.level >= unLockLv && 0 < unLockLv);
            unlockSubject.OnNext(isUnLock);
        }

        public void setCurrentPorgress(InfoFormat progressFormat)
        {
            progressSubject.OnNext(progressFormat);
        }

        public void setNewMission(InfoFormat progressFormat)
        {
            MissionInfo missionInfo = getMissionInfo(progressFormat);
            newMissionSubject.OnNext(missionInfo);
        }

        public MissionInfo getMissionInfo(InfoFormat progressFormat)
        {
            var isNormal = progressFormat.normalMission.isComplete;

            return isNormal ? progressFormat.specialMission : progressFormat.normalMission;
        }

        public class InfoFormat
        {
            public MissionInfo normalMission;
            public MissionInfo specialMission;
        }

        public class MissionInfo
        {
            public bool isComplete;
            public decimal count;
            public decimal max;
            public decimal extraCount;
            public string content;
            public bool haveExtraCount;
            public bool isNeedNotice;
        }
    }
}
