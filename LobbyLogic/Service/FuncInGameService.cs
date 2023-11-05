using System;
using CommonService;
using UniRx;
using CommonILRuntime.BindingModule;
using EventActivity;
using System.Collections.Generic;
using Lobby;
using Shop.LimitTimeShop;
using System.Threading.Tasks;
using CommonILRuntime.Module;
using Service;
using LobbyLogic.Common;
using Network;
using Debug = UnityLogUtility.Debug;
using Mission;
using SaveTheDog;
using Game.Common;
using CommonILRuntime.Services;

namespace Services
{
    /// <summary>
    /// Lobby端將Common所提供的通道(FuncInGameToLobbyService)事件對應回Lobby自己的系統
    /// </summary>
    class EventInGameService
    {
        static EventInGameService _instance = null;

        public static EventInGameService getInstance
        {
            get
            {
                if (null == _instance)
                {
                    _instance = new EventInGameService();
                }
                return _instance;
            }
        }

        FuncInGameToLobbyService eventInGameToLobbyService;
        IDisposable receiveEventIDDispos;

        Dictionary<FunctionNo, Action> eventPresenter = new Dictionary<FunctionNo, Action>();
        Dictionary<FunctionNo, Action> inGameEventDict = new Dictionary<FunctionNo, Action>();

        bool isInit;

        EventActivityBarPresenter eventActivityBarPresenter;

        public void initFuncInGameService()
        {
            if (isInit)
            {
                return;
            }
            isInit = true;
            eventInGameToLobbyService = DataStore.getInstance.eventInGameToLobbyService;
            receiveEventIDDispos = eventInGameToLobbyService.eventOpenSubscribe.Subscribe(getEventIDFromGame);

            initEventPresenter();
            initInGameEvent();
        }

        public void NoticeEventEnd(FunctionNo functionNo)
        {
            eventInGameToLobbyService.SendEventEnd(functionNo);
        }

        /// <summary>
        /// Lobby各系統與 Common FunctionNo 映射
        /// </summary>
        void initEventPresenter()
        {
            eventPresenter.Clear();
            eventPresenter.Add(FunctionNo.Shop, openShop);
            eventPresenter.Add(FunctionNo.LimitShop, openLimitShop);
            eventPresenter.Add(FunctionNo.ClearAllDispose, clearPresenterDis);
            eventPresenter.Add(FunctionNo.GoldenEgg, openGoldenEgg);
            eventPresenter.Add(FunctionNo.SettingPage, openSettingPage);
            eventPresenter.Add(FunctionNo.DailyMission, openDailyMission);
            eventPresenter.Add(FunctionNo.HighRollerVault, openHighRollerVault);
            eventPresenter.Add(FunctionNo.UpdateDailyMission, updateDailyMission);
            eventPresenter.Add(FunctionNo.ExtraGameLevelComplete, onExtraGameLevelComplete);
            eventPresenter.Add(FunctionNo.UpdateAdventureMission, onUpdateAdventureMission);
            eventPresenter.Add(FunctionNo.OpenTransitionXParty, openTransitionXParty);
            eventPresenter.Add(FunctionNo.OpenCommonLoadingUi, openLoadingUI);
            eventPresenter.Add(FunctionNo.NoCoinNotice, noCoinNotice);
        }

        void initInGameEvent()
        {
            inGameEventDict.Clear();
            inGameEventDict.Add(FunctionNo.UpdateRollerVaultPay, updateHighRollerVault);
        }

        private void getEventIDFromGame(PlatformFuncInfo funcInfo)
        {
            Action inGameEvent;
            if (eventPresenter.TryGetValue(funcInfo.FunctionID, out inGameEvent))
            {
                inGameEvent();
                return;
            }

            if (inGameEventDict.TryGetValue(funcInfo.FunctionID, out inGameEvent))
            {
                inGameEvent();
            }
        }

        public void clearGameServices()
        {
            isInit = false;
            UtilServices.disposeSubscribes(receiveEventIDDispos);
        }
        public void clearPresenterDis()
        {
            if (null != eventActivityBarPresenter)
            {
                eventActivityBarPresenter.clearSubscribe();
                eventActivityBarPresenter = null;
            }
        }
        #region openPresenter
        async void openShop()
        {
            if (CoinFlyHelper.isCoinFlying)
            {
                GamePauseManager.gameResume();
                return;
            }

            switch (await getNowGameOrientation())
            {
                case GameOrientation.Landscape:
                    openLobbyPresenterWithGamePause<Shop.ShopMainPresenter>();
                    break;
                case GameOrientation.Portrait:
                    openLobbyPresenterWithGamePause<Shop.PortraitShopMainPresenter>();
                    break;
            }
        }
        void openLimitShop()
        {
            GamePauseManager.gamePause();
            LimitTimeShopManager.getInstance.openLimitTimeFirstPage();
            //openLobbyPresenter<LimitTimeFirstPresenter>();
        }

        async void noCoinNotice()
        {
            GamePauseManager.gamePause();
            var isOpenLimit = await LimitTimeShopManager.getInstance.noCoinOpenLimitFirstPage();
            if (!isOpenLimit)
            {
                openShop();
            }
        }

        void openGoldenEgg()
        {
            openLobbyPresenterWithGamePause<GoldenEgg.GoldenEggMainPresenter>();
        }
        void openSettingPage()
        {
            DataStore.getInstance.gameTimeManager.Pause();
            openLobbyPresenter<LobbySettingPresneter>();
        }

        void openDailyMission()
        {
            var presenter = UiManager.getPresenter<MissionMainPresenter>();
            presenter.uiGameObject.setActiveWhenChange(false);
            GamePauseManager.gamePause();
            presenter.openAndAutoObtainReward();
        }

        void openTransitionXParty()
        {
            TransitionxPartyServices.instance.openTransitionPage();
        }

        async void openLoadingUI()
        {
            await Lobby.LoadingUIModule.LoadingUIManager.instance.loadScreenOrientationSprite();
        }

        void updateDailyMission()
        {
            MissionData.updateProgress();
        }

        async void openHighRollerVault()
        {
            var vaultPresenter = openLobbyPresenterWithGamePause<HighRoller.HighRollerVaultPresenter>();
            var userRecord = await AppManager.lobbyServer.getHighRollerUser();
            vaultPresenter.setUserRecord(userRecord);
        }

        void onExtraGameLevelComplete()
        {
            SaveTheDogMapData.instance.dogGameComplete();
        }

        async void onUpdateAdventureMission()
        {
            var nowSlotGameID = await DataStore.getInstance.dataInfo.getNowplayGameID();
            if (SaveDogLvKind.Slot != SaveTheDogMapData.instance.getNowRecordKind() || false == SaveTheDogMapData.instance.nowRecord.type.Equals(nowSlotGameID))
            {
                return;
            }

            var progress = await AppManager.lobbyServer.getNewbieAdventureMissionProgress();
            if (Result.OK != progress.result)
            {
                return;
            }

            ActivityQuestData.missionProgressUpdate.OnNext(progress);

            if (!string.IsNullOrEmpty(progress.completedAt))
            {
                ActivityQuestData.isRewardCanShow = true;
                if (ActivityQuestData.gameState != GameConfig.GameState.NG)
                {
                    return;
                }
                ActivityQuestManager.instance.onQuestComplete();
            }
        }

        T openLobbyPresenterWithGamePause<T>() where T : ContainerPresenter, new()
        {
            GamePauseManager.gamePause();
            return openLobbyPresenter<T>();
        }

        T openLobbyPresenter<T>() where T : ContainerPresenter, new()
        {
            var presenter = UiManager.getPresenter<T>();
            presenter.open();
            return presenter as T;
        }

        async Task<GameOrientation> getNowGameOrientation()
        {
            return await DataStore.getInstance.dataInfo.getNowGameOrientation();
        }

        #endregion
        #region EventInGame
        public async void updateHighRollerVault()
        {
            var returnToPayResponse = await AppManager.lobbyServer.getCurrentReturnToPay();
            DataStore.getInstance.highVaultData.updateVaultReturnToPay(returnToPayResponse.highRoller.getReturnToPay);
        }
        #endregion
        class LobbyOrientationPresenter<T> where T : ContainerPresenter, new()
        {
            class OrientationPresenter
            {
                T _landPresenter;
                public T landpresenter
                {
                    get { return _landPresenter; }
                    set { _landPresenter = value; }
                }

                T _portraitPresenter;
                public T portraitPresenter
                {
                    get { return _portraitPresenter; }
                    set { _portraitPresenter = value; }
                }
            }

            OrientationPresenter orientationPresenter = new OrientationPresenter();

            public void addPresenters(T landPresenter, T portraitPresenter)
            {
                orientationPresenter.landpresenter = landPresenter;
                orientationPresenter.portraitPresenter = portraitPresenter;
            }

            public T getLandPresenter()
            {

                return orientationPresenter.landpresenter;
            }

            public T getPortraitPresenter()
            {
                return orientationPresenter.portraitPresenter;
            }
        }
    }
}
