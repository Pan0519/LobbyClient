using System;
using System.Collections.Generic;
using Services;
using Service;
using CommonService;
using CommonPresenter;
using LobbyLogic.NetWork.ResponseStruct;
using Lobby.VIP;
using Lobby.VIP.UI;
using Common.VIP;
using UniRx;
using System.Threading.Tasks;

using Debug = UnityLogUtility.Debug;

namespace StayMiniGame
{
    static class StayGameDataStore
    {
        public static int multiplierEnergy { get; private set; }

        public static int multiplierEnergyMakeup;
        public static ulong bonusAmount;
        public static float vipMakeup { get; private set; }
        public static ulong bonusReward { get; private set; }
        public static HighRollerBoardResultResponse highRollerBoard { get; private set; }

        static VipProfit[] vipProfits = new VipProfit[] { };

        static float[] boxTimes = new float[] { 0.6f, 1.2f };

        public static Subject<CompareBonusTimeResult> countdownTimeSub { get; private set; } = new Subject<CompareBonusTimeResult>();

        public static void setBonusReward(float multipliers)
        {
            bonusReward = (ulong)(multipliers * DataStore.getInstance.playerInfo.coinExchangeRate);
        }

        public static async void initGameData()
        {
            StayGameBonus gameBonus = await AppManager.lobbyServer.getStayGameBonus();
            setStayGameData(gameBonus.info);
            countdownTimeSub.OnNext(checkBonusTime());
        }

        static CompareBonusTimeResult checkBonusTime()
        {
            return DataStore.getInstance.miniGameData.compareBonusTime();
        }

        static void setStayGameData(StayGameBonusInfo bonusInfo)
        {
            multiplierEnergy = bonusInfo.multiplierEnergy;
            MiniGameConfig.instance.addStayGameDatas(StayGameType.gold, new CommonPresenter.StayGameData(endTimeStr: bonusInfo.goldenBoxAvailableAfter));
            MiniGameConfig.instance.addStayGameDatas(StayGameType.silver, new CommonPresenter.StayGameData(endTimeStr: bonusInfo.silverBoxAvailableAfter));
        }

        public static string getRedeemStr(StayGameType gameType)
        {
            return DataStore.getInstance.miniGameData.boxRedeemStr[(int)gameType];
        }

        public static DateTime refreshData(StayGameType gameType, StayGameBonusInfo bonusInfo)
        {
            setStayGameData(bonusInfo);
            countdownTimeSub.OnNext(checkBonusTime());
            return MiniGameConfig.instance.getStayGameData(gameType).endTime;
        }

        public static async Task setVIPProfitValue(StayGameType gameType)
        {
            vipMakeup = 0;
            VipProfitDef profitDefId = VipProfitDef.GOLDEN_BOX;
            if (StayGameType.silver == gameType)
            {
                profitDefId = VipProfitDef.SILVER_BOX;
            }
            if (vipProfits.Length <= 0)
            {
                var infos = await VipJsonData.getLevelInfos();
                VipLevelData vipLevels = infos[DataStore.getInstance.playerInfo.myVip.info.level - 1];
                vipProfits = vipLevels.profits;
            }

            for (int i = 0; i < vipProfits.Length; ++i)
            {
                var profit = vipProfits[i];
                if (profit.id == profitDefId)
                {
                    vipMakeup = profit.value * 0.01f;
                    break;
                }
            }
        }

        public static float getBoxMaxTimes(StayGameType gameType)
        {
            if ((int)gameType > boxTimes.Length - 1)
            {
                return 0;
            }
            return boxTimes[(int)gameType];
        }

        public static void setHighRollerBoard(HighRollerBoardResultResponse boardResponse)
        {
            highRollerBoard = boardResponse;
        }
    }

    public class StayGameData
    {
        public DateTime endTime { get; private set; }
        public long price { get; private set; }

        public StayGameData(string endTimeStr, long price)
        {
            endTime = UtilServices.strConvertToDateTime(endTimeStr, DateTime.MinValue);
            this.price = price;
        }
    }
}
