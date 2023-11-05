using CommonILRuntime.Outcome;
using CommonService;
using Lobby.Common;
using LobbyLogic.NetWork.ResponseStruct;
using Network;
using Service;
using Services;
using System;
using System.Collections.Generic;
using UniRx;

namespace Mission
{
    public class MissionHelper
    {
        Action<DailyMissionProgressResponse> receiveProgressCallBack;
        Action<RewardFormat> receiveCallBack;
        MissionPacketResolver packetResolver;
        IDisposable packetTimer;
        int retryCount;

        public MissionHelper()
        {
            packetResolver = new MissionPacketResolver();
        }

        public async void askNormalReward(Action<RewardFormat> receiveCallBack)
        {
            var response = await AppManager.lobbyServer.sendDailyMissionGeneralReward();

            this.receiveCallBack = receiveCallBack;
            if (checkResponse(response))
            {
                sendObtainRewardAPI(response.rewardPackId);
            }
            else
            {
                releaseCallBack();
            }
        }

        public async void askSpecialReward(Action<RewardFormat> receiveCallBack)
        {
            var response = await AppManager.lobbyServer.sendDailyMissionSpecialReward();

            this.receiveCallBack = receiveCallBack;
            if (checkResponse(response))
            {
                sendObtainRewardAPI(response.rewardPackId);
            }
            else
            {
                releaseCallBack();
            }
        }

        public async void askMedalReward(int stageIndex, Action<RewardFormat> receiveCallBack)
        {
            var response = await AppManager.lobbyServer.sendDailyMissionMedalReward(stageIndex);

            this.receiveCallBack = receiveCallBack;
            if (checkResponse(response))
            {
                sendObtainRewardAPI(response.rewardPackId);
            }
            else
            {
                releaseCallBack();
            }
        }

        async void sendObtainRewardAPI(string packID)
        {
            var response = await AppManager.lobbyServer.getRewardPacks(packID);
            if (checkResponse(response))
            {
                checkNoticeAmount();
                receiveCallBack?.Invoke(packetResolver.packageResponse(response));
                receiveCallBack = null;
            }
            else
            {
                releaseCallBack();
            }
        }

        private void checkNoticeAmount()
        {
            NoticeManager.instance.dailyNoticeAmount--;
            NoticeManager.instance.dailyNoticeEvent.OnNext(Math.Max(0, NoticeManager.instance.dailyNoticeAmount));
        }

        bool checkResponse(ServerResponse response)
        {
            return (Result.OK == response.result);
        }

        void releaseCallBack()
        {
            receiveCallBack = null;
        }

        public async void askProgress(Action<DailyMissionProgressResponse> receiveCallBack)
        {
            receiveProgressCallBack = receiveCallBack;
            packetTimer = Observable.Timer(TimeSpan.FromSeconds(1f)).Subscribe(onProgressPacketTimeOut);
            var response = await AppManager.lobbyServer.getDailyMissionProgress();
            if (Result.OK == response.result)
            {
                releasePacketTimer();
                retryCount = 0;
                receiveProgressCallBack = null;
                receiveCallBack?.Invoke(response);
            }
        }

        void releasePacketTimer()
        {
            if (packetTimer == null)
            {
                return;
            }

            packetTimer.Dispose();
            packetTimer = null;
        }

        void onProgressPacketTimeOut(long tick)
        {
            if (1 <= retryCount)
            {
                retryCount = 0;
                receiveProgressCallBack = null;
                releasePacketTimer();
                DataStore.getInstance.eventInGameToLobbyService.SendEventEnd(FunctionNo.UpdateDailyMission);
            }
            else
            {
                retryCount++;
                askProgress(receiveProgressCallBack);
            }
        }

        public class RewardFormat
        {
            public List<CommonReward> commonRewards = null;
            public Outcome outcome = null;
            public ulong finalPlayerCoin = 0;

            public RewardFormat()
            {
                commonRewards = new List<CommonReward>();
            }
        }
    }
}
