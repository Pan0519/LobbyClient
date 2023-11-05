using Services;
using UnityEngine;
using LobbyLogic.NetWork.ResponseStruct;
using UniRx;
using System;
using CommonPresenter;
using System.Collections.Generic;
using CommonService;
using Network;
using Lobby.Jigsaw;
using CommonILRuntime.Outcome;
using HighRoller;
using LobbyLogic.Common;
using System.Threading.Tasks;

using Debug = UnityLogUtility.Debug;
using Mission;

namespace Service
{
    public class FromGameMsgService
    {
        static FromGameMsgService _instance = null;

        public static FromGameMsgService getInstance
        {
            get
            {
                if (null == _instance)
                {
                    _instance = new FromGameMsgService();
                }
                return _instance;
            }
        }

        List<IDisposable> toServerSubscribe = new List<IDisposable>();
        GameToLobbyServices toLobbyService;

        //public Subject<Props> props { get; private set; }   //道具
        public Subject<Props> props = new Subject<Props>();   //道具

        bool isGameAuto;

        public void initFromGameService()
        {
            props = new Subject<Props>();
            toLobbyService = DataStore.getInstance.gameToLobbyService;
            addToLobbySubscribe(toLobbyService.wagerIDSubscribe.Subscribe(getWagerIDFromGame));
            addToLobbySubscribe(toLobbyService.bonusTimeSubscribe.Subscribe(getBonusTime));
            addToLobbySubscribe(toLobbyService.topbarStayGameData.Subscribe(getStayGameData));
            addToLobbySubscribe(toLobbyService.autoPlayStateListener.Subscribe(isGameAutoSub));
            addToLobbySubscribe(toLobbyService.highRollerVaultData.Subscribe(getHighRollerVault));
            addToLobbySubscribe(toLobbyService.checkTopbarGoldenEgg.Subscribe(initGoldenMax));
        }

        void addToLobbySubscribe(IDisposable disposable)
        {
            toServerSubscribe.Add(disposable);
        }
        public void disposeGameMsgService()
        {
            UtilServices.disposeSubscribes(toServerSubscribe.ToArray());
        }

        void isGameAutoSub(bool isAuto)
        {
            isGameAuto = isAuto;
        }

        #region toLobbyFunction
        #region WagerPack
        async void getWagerIDFromGame(string wagerID)
        {
            //DataStore.getInstance.eventInGameToLobbyService.OpenFuncInLobby(FunctionNo.UpdateAdventureMission);
            checkGoldenEggAmount();
            WagerResponse response = await AppManager.lobbyServer.getWagerInfos(wagerID);
            if (Result.GetWagerEmpty == response.result || Result.OK != response.result)
            {
                return;
            }
            WagerUser user = response.exp.outcome.user;
            if (user.isLevelUp)
            {
                DataStore.getInstance.playerInfo.setLv(user.level);
                DataStore.getInstance.playerInfo.setLvUpExp(user.levelUpExp);
                updatePlayerInfo();
                attempToInitDailyMission(user.level);
                if (!string.IsNullOrEmpty(user.levelUpRewardPackId))
                {
                    sendRewardPack(user.levelUpRewardPackId);
                }
            }
           
            DataStore.getInstance.playerInfo.setPlayerExpAndSubject(user.exp);
            DataStore.getInstance.playerInfo.setIsLvUP(user.isLevelUp);

            if (null != response.props)
            {
                props.OnNext(response.props);
            }

            if (null != response.album)
            {
                onRecvWagerAlbum(response.album);
            }

            if (null != response.retentionBonusEnergy)
            {
                DataStore.getInstance.miniGameData.addBonusEnergy(response.retentionBonusEnergy.amount);
            }

            HighRollerRewardManager.openReward(response.highRoller);
        }

        async void updatePlayerInfo()
        {
            var playerInfoResponse = await AppManager.lobbyServer.getPlayerInfo();
            LobbyPlayerInfo.setPlayerInfo(playerInfoResponse);
        }
        async void sendRewardPack(string packId)
        {
            CommonRewardsResponse rewardPack = await AppManager.lobbyServer.rewardPacksRedeem(packId);
            RewardPacks packs = new RewardPacks()
            {
                rewards = new Dictionary<PurchaseItemType, Pack>(),
            };
            for (int i = 0; i < rewardPack.rewards.Length; ++i)
            {
                CommonReward serverReward = rewardPack.rewards[i];
                Pack packReward = new Pack()
                {
                    outcome = serverReward,
                };
                PurchaseItemType itemKind = PurchaseInfo.getItemType(serverReward.kind);
                if (PurchaseItemType.None != itemKind)
                {
                    packs.rewards.Add(itemKind, packReward);
                }
            }
            DataStore.getInstance.dataInfo.setLvupRewardSubject(packs);
        }

        void attempToInitDailyMission(int newLv)
        {
            if (newLv == MissionData.unLockLv)
            {
                MissionData.initMissionProgressData();
            }
        }

        async void getBonusTime(bool defaultValue)
        {
            var bonusTime = await AppManager.lobbyServer.getBouns();
            DataStore.getInstance.dataInfo.setAfterBonusTime(bonusTime.availableAfter);
        }
        IDisposable pieceFinishDis;
        IDisposable autoClosePieceDis;
        void onRecvWagerAlbum(WagerAttachedAlbum album)
        {
            GamePauseManager.gamePause();
            var ids = album.items;
            if (null != ids)
            {
                PieceGetterPresenter pieceGetterPresenter = PieceGetter.getPieces(ids);
                pieceFinishDis = JigsawReward.isJigsawShowFinish.Subscribe(_ =>
                 {
                     GamePauseManager.gameResume();
                     UtilServices.disposeSubscribes(autoClosePieceDis, pieceFinishDis);
                 });
                if (isGameAuto)
                {
                    autoClosePieceDis = Observable.Timer(TimeSpan.FromSeconds(10)).Subscribe(_ =>
                    {
                        pieceGetterPresenter.fadeoutUI();
                        autoClosePieceDis.Dispose();
                    }).AddTo(pieceGetterPresenter.uiGameObject);
                }
                return;
            }
            GamePauseManager.gameResume();
        }
        GameGoldenNode goldenNode;
        public async void checkGoldenEggAmount()
        {
            var eggData = await AppManager.lobbyServer.getCoinBank();
            bool isMax = eggData.highPool.amount >= eggData.highPool.maximum || eggData.lowPool.amount >= eggData.lowPool.maximum;
            if (isMax)
            {
                await Task.Delay(TimeSpan.FromSeconds(1.0f));
                goldenNode.setEggMaxActive(isMax);
            }
        }

        void initGoldenMax(GameGoldenNode goldenNode)
        {
            this.goldenNode = goldenNode;
            checkGoldenEggAmount();
        }

        #endregion
        #region StayGame
        async void getStayGameData(string gameType)
        {
            if (string.IsNullOrEmpty(gameType))
            {
                StayGameBonus gameBonus = await AppManager.lobbyServer.getStayGameBonus();
                setGameTopbarStayGameInfo(gameBonus.info);
                return;
            }

            StayGameBonusRedeem bonusRedeem = await AppManager.lobbyServer.stayGameBonusRedeem(gameType);
            //DataStore.getInstance.playerInfo.myWallet.commit(bonusRedeem.wallet);
            DataStore.getInstance.miniGameData.setBonusRedeemAmount(bonusRedeem.bonusAmount);
            setGameTopbarStayGameInfo(bonusRedeem.info);
            await Task.Delay(TimeSpan.FromSeconds(1.0f));
            HighRollerRewardManager.openReward(bonusRedeem.highRoller);
        }

        void setGameTopbarStayGameInfo(StayGameBonusInfo info)
        {
            if (null == info)
            {
                Debug.LogError($"getGameTopBarStayGameInfo is null");
                return;
            }
            DataStore.getInstance.miniGameData.setTopBarGameInfo(new TopBarGameBonusInfo()
            {
                silverBoxAvailableAfter = info.silverBoxAvailableAfter,
                goldenBoxAvailableAfter = info.goldenBoxAvailableAfter,
                wheelEnergy = info.wheelEnergy,
                diceEnergy = info.diceEnergy,
                multiplierEnergy = info.multiplierEnergy,
            });
        }

        #endregion
        #region HighRoller
        public async void getHighRollerVault(bool emptyValue)
        {
            var userRecord = await AppManager.lobbyServer.getHighRollerUser();
            DataStore.getInstance.highVaultData.openVault(userRecord != null);
            if (null == userRecord)
            {
                return;
            }

            DateTime expireTime = UtilServices.strConvertToDateTime(userRecord.accessInfo.expiredAt, DateTime.MinValue);
            DateTime vaultExpireTime = UtilServices.strConvertToDateTime(userRecord.vault.expiredAt, DateTime.MinValue);
            CompareTimeResult expireTimeCompare = UtilServices.compareTimeWithNow(expireTime);
            CompareTimeResult vaultExpireTimeCompare = UtilServices.compareTimeWithNow(vaultExpireTime);

            if (CompareTimeResult.Earlier == expireTimeCompare && CompareTimeResult.Earlier == vaultExpireTimeCompare)
            {
                DataStore.getInstance.highVaultData.openVault(false);
                return;
            }

            VaultData vaultData = new VaultData();
            if (null != userRecord.vault)
            {
                vaultData.expireTime = userRecord.vault.expiredAt;
                vaultData.lastBillingAt = userRecord.vault.lastBillingAt;
                vaultData.returnToPay = userRecord.vault.getReturnToPay;
            }
            DataStore.getInstance.highVaultData.setVaultData(vaultData);
        }
        #endregion

        void addBGMoneyFromGame(Transform target)
        {
            DataStore.getInstance.playerMoneyPresenter.addTo(target);
        }

        #endregion
    }
}
