using Services;
using System;
using LobbyLogic.NetWork.ResponseStruct;
using CommonService;
using Service;
using Lobby;
using UniRx;
using CommonPresenter;
using System.Threading.Tasks;

namespace HighRoller
{
    public class HighRollerDataManager
    {
        public static HighRollerDataManager instance = new HighRollerDataManager();
        public HighRollerUserRecordResponse userRecord { get; private set; } = null;
        public AccessInfo accessInfo { get; private set; } = null;
        public Subject<HighRollerUserRecordResponse> userRecordSub = new Subject<HighRollerUserRecordResponse>();
        public Subject<long> passPointUpdateSub = new Subject<long>();
        public float getHighRollerCoinExchangeRate { get { return DataStore.getInstance.playerInfo.coinExchangeRate * 0.25f; } }
        static Action toNextPopCB = null;
        public async Task getHighUserRecord()
        {
            userRecord = await AppManager.lobbyServer.getHighRollerUser();
            accessInfo = userRecord.accessInfo;
            userRecordSub.OnNext(userRecord);
            CompareTimeResult timeResult = getAccessExpiredTimeCompareResult();
            DataStore.getInstance.playerInfo.checkHasHighRollerPermission(CompareTimeResult.Later == timeResult);
            DataStore.getInstance.playerInfo.setHighRollerEndTime(accessInfo.expiredAt);
        }

        public async Task getHighUserRecordAndCheck(Action checkCB = null)
        {
            toNextPopCB = checkCB;
            await updateUserRecord();
            checkUserRecordData();
        }

        public async void checkUserRecordData()
        {
            await startCheckDiamondClubData(userRecord);
            checkGetReturnToPayTime();
        }

        async Task updateUserRecord()
        {
            await getHighUserRecord();
            openVault();
        }

        void openVault()
        {
            DataStore.getInstance.highVaultData.openVault(true);
            VaultData vaultData = new VaultData();
            if (null != userRecord.vault)
            {
                vaultData.expireTime = userRecord.vault.expiredAt;
                vaultData.lastBillingAt = userRecord.vault.lastBillingAt;
                vaultData.returnToPay = userRecord.vault.getReturnToPay;
            }
            DataStore.getInstance.highVaultData.setVaultData(vaultData);
        }

        public void updatePassPoint(long updatePoint)
        {
            passPointUpdateSub.OnNext(updatePoint);
        }

        public void addPassPoints(long addPoint)
        {
            userRecord.passPoints += addPoint;
            updatePassPoint(userRecord.passPoints);
        }

        async Task startCheckDiamondClubData(HighRollerUserRecordResponse record)
        {
            if (userRecord.revision < record.revision)
            {
                userRecord = record;
            }

            await checkAccessInfoExpireAt();
        }

        public CompareTimeResult getAccessExpiredTimeCompareResult()
        {
            if (null == accessInfo)
            {
                return CompareTimeResult.Same;
            }

            DateTime expiredTime = accessExpireTime;
            return UtilServices.compareTimeWithNow(expiredTime);
        }

        public DateTime accessExpireTime { get { return UtilServices.strConvertToDateTime(accessInfo.expiredAt, DateTime.MinValue); } }
        public string accessExpireTimeStruct { get { return UtilServices.toTimeStruct(accessExpireTime.Subtract(UtilServices.nowTime)).toTimeString(LanguageService.instance.getLanguageValue("Time_Days")); } }

        /// <summary>
        /// 檢查鑽⽯俱樂部權限是否到期
        /// </summary>
        async Task checkAccessInfoExpireAt()
        {
            CompareTimeResult compareResult = compareResultWhitNowTime(accessInfo.expiredAt, DateTime.MaxValue);
            if (CompareTimeResult.Earlier == compareResult)
            {
                await sendCheckExpire();
                return;
            }

            if (accessInfo.details.Length <= 1)
            {
                runToNextCB();
                return;
            }

            for (int i = 0; i < accessInfo.details.Length; ++i)
            {
                compareResult = compareResultWhitNowTime(accessInfo.details[i].expiredAt, DateTime.MaxValue);
                if (CompareTimeResult.Earlier == compareResult)
                {
                    await sendCheckExpire();
                    break;
                }
            }
        }

        async Task sendCheckExpire()
        {
            var response = await AppManager.lobbyServer.checkExpire();
            HighRollerRewardManager.openReward(response.highRoller, runToNextCB);
            await updateUserRecord();
        }

        void runToNextCB()
        {
            if (null != toNextPopCB)
            {
                toNextPopCB();
            }
            toNextPopCB = null;
        }

        public async void checkGetReturnToPayTime()
        {
            if (null == userRecord)
            {
                return;
            }

            await AppManager.lobbyServer.sendReturnToPay();
        }

        public CompareTimeResult compareResultWhitNowTime(string compareTime, DateTime defaultTime)
        {
            DateTime compareDateTime = UtilServices.strConvertToDateTime(compareTime, defaultTime);
            return UtilServices.compareTimeWithNow(compareDateTime);
        }
    }

    class CrossDaysCompareData
    {
        public int year { get; private set; }
        public int dayOfYear { get; private set; }

        public CrossDaysCompareData()
        {
            getDateTimeNow();
        }

        public void getDateTimeNow()
        {
            var nowTime = UtilServices.nowTime;
            year = nowTime.Year;
            dayOfYear = nowTime.DayOfYear;
        }
    }
}
