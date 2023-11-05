using System;
using System.Collections;
using System.Threading.Tasks;
using CommonILRuntime.BindingModule;
using CommonILRuntime.FiniteState;
using CommonService;
using global::Game.Common;
using global::Network;
using LobbyLogic.Audio;
using Services;
using Slot.Game.GameStruct;
using UniRx;
using UnityEngine;
using CommonILRuntime.PlayerProp;
using static Game.Slot.SlotDefine;

namespace Game.Slot
{
    public abstract class SlotGameBase
    {
        public static GameConfig gameConfig;
        #region UI Events
        public Action OnSpinHandler;
        public Action OnStopHandler;
        public Action<int> OnAutoItemClick;
        public Action OnTableSpinEnd;
        public Action OnTableRollEnd;
        public Action<int> OnItemHandler;
        #endregion

        #region Network
        public Action OnNormalSpinResult;
        public Action OnFreeSpinResult;
        public Action OnBonusSpinResult;
        public Action OnSuperFreeSpinResult;
        public Action OnMiniSpinResult;
        public Action OnNormalEndResult;
        public Action OnFreeEndResult;
        public Action OnBonusEndResult;
        public Action OnSuperFreeEndResult;
        public Action OnMiniEndResult;
        #endregion

        #region Game Object
        public SlotGameBasePresenter gameUI = null;
        public JackpotPresenter jackpotPresenter;
        public WinWindowPresenter winWindowPresenter;
        public NiceWinPresenter niceWinPresenter;
        public RewardMapBaseManger mapManger;
        private HierarchicalStateController stateController;
        public BetToLockManager betToLockManager = new BetToLockManager();
        #endregion

        #region normal data
        public ulong totalBet = 0;
        public ulong serverGameTotalWin;
        public bool grandJpAvaliable = false;
        public ulong[][] normalGameOriginReelData;
        #endregion

        #region free data
        public ulong freeGameWinTotal = 0;
        public int freeGameIdx = 0;
        public int freeGameTotalCount = 0;
        public ulong[][] freeGameInitReelData;
        #endregion

        #region bonus data
        public int bonusGameIdx = 0;
        public int bonusAutoCount { get; set; }
        public int[] bonusAutoCountArry { get; set; }
        public ulong[][] bonusGameInitReelData;
        public bool isBGToFG = false;
        #endregion

        #region game regular data
        private WalletData RackUpWallet;
        public ulong[][] gameReelData;               //本局盤面資訊(NG/FG/SFG共用)
        public int[] gameReelIndex;                 //本局盤面索引(NG/FG/SFG共用)
        public WinInfo gameWinInfo;                 //本局獲獎資訊(NG/FG/SFG共用)
        public int autoCount = 0;
        #endregion

        #region superfree data 
        public int superFreeAutoCount { get; set; } // 是否中了BounsGame或是SuperFreeGame
        public int superFreeGameIdx = 0;
        public int superFreeGameTotalCount = 0;
        public ulong superFreeGameWinTotal = 0;
        public ulong[][] superFreeGameInitReelData;
        #endregion

        #region jp data
        public ulong[] gameJPReelData;               //本局JP盤面資訊(NG/FG共用) (SFG = null)
        public int gameJPReelIndex;                 //本局JP盤面索引(NG/FG共用)
        public ulong gameJPReelWin;
        #endregion

        #region way game data
        public WinCondition[] winCondition;
        public ulong[][] reelData;
        public ulong[][] reelNGData;
        public int[] reelIndex;
        public ulong totalWin;
        public int totalPay;
        public int fgGameTime;
        public ulong[][] fgFirstReelData;
        public bool alreadyOnTableEnd = false;
        public bool haveShowWinWindow = false;
        #endregion

        #region way game jp
        public bool IsInJP;
        public int jackpotGameIdx = 0;
        public ulong jackpotGameWinTotal = 0;
        #endregion

        public int freeAutoCount { get; set; }
        public virtual int TotalPay
        {
            get
            {
                if (null == gameWinInfo)
                {
                    return 0;
                }
                else
                {
                    return gameWinInfo.Total_Pay;
                }
            }
        }

        public Guid winLineGuid = new Guid();
        public virtual int SuperFreeGameTimes { get { return 0; } }
        public virtual bool IsInFG { get { return freeGameTotalCount > 0; } }
        public virtual bool IsInBG { get { return false; } }
        public virtual bool IsInSFG { get { return false; } }
        public virtual bool IsJpNoConstraint { get { return false; } }
        public virtual bool IsLineWin { get { return false; } }
        public virtual bool IsSkipLine { get { return IsAutoPlay || IsInFG || IsInBG; } }
        public bool IsShowingWinWindow { get; private set; }
        public bool IsAutoPlay { get { return (int)AutoMode.AUTO != autoCount; } }

        #region Player state轉換工具

        public GameConfig.PlayerState NowPlayerState { get { return gameConfig.GetNowPlayerState(); } }
        protected int TransToServerState(GameConfig.PlayerState state) { return gameConfig.TransServerPlayerState(state); }
        protected GameConfig.PlayerState TransToClientState(int state) { return gameConfig.TransClientPlayerState(state); }

        public void setPlayerGameStateAndSubject(GameConfig.PlayerState state)
        {
            DataStore.getInstance.playerInfo.setPlayerGameStateAndSubject(TransToServerState(state));
        }
        #endregion

        #region 初始設定
        IDisposable stateUpdateDis = null;
        public virtual void Init()
        {
            stateController = new HierarchicalStateController(new WaitGameState(this));
            waitInitUI();
        }

        async void waitInitUI()
        {
            while (gameUI == null)
            {
                await Task.Yield();
            }
            initUI();
            subscribeCancelAutoPlayListener();
        }

        public virtual void fetchUI()
        {
            jackpotPresenter = UiManager.getPresenter<JackpotPresenter>();
            winWindowPresenter = UiManager.getPresenter<WinWindowPresenter>();
            winWindowPresenter.close();
        }

        public virtual void initUI()
        {
            if (DataStore.getInstance.gameTimeManager.IsPaused())
                DataStore.getInstance.gameTimeManager.Resume();

            updateAutoMode();
            gameUI.changeBackgroundByState(GameConfig.GameState.NG);
            Observable.EveryUpdate().Subscribe((_) =>
            {
                stateController.StateUpdate();
                CoroutineManager.Update();
            }).AddTo(gameUI.uiGameObject);
        }

        private void subscribeCancelAutoPlayListener()
        {
            DataStore.getInstance.lobbyToGameServices.cancelAutoSubject.Subscribe((_) => cancelAutoPlay()).AddTo(gameUI.uiGameObject);
        }

        public virtual void setupCallback()
        {
            OnAutoItemClick = onAutoItemHandler;
        }

        public virtual void preloadAudio() { }

        public virtual void preloadAnimation() { }
        #endregion

        #region Windows 各種彈窗 (Jackpot / FG / BG / SFG)
        public bool haveNiceWin(ulong winPoints)
        {
            return winPoints >= totalBet * gameConfig.NICE_WIN_BET;
        }

        public virtual void showNiceWinWindow(ulong winPoints, Action callback = null)
        {

            Action niceWinCompleted = () =>
            {

                callback?.Invoke();
                gameUI.dashWinPoints(winPoints);
            };

            NiceWinType winType = NiceWinType.NiceWin;
            if (winPoints >= totalBet * gameConfig.INCREDIBLE_WIN_BET)
            {
                winType = NiceWinType.Incredible;
            }
            else if (winPoints >= totalBet * gameConfig.AMAZING_WIN_BET)
            {
                winType = NiceWinType.Amazing;
            }
            else if (winPoints >= totalBet * gameConfig.NICE_WIN_BET)
            {
                winType = NiceWinType.NiceWin;
            }
            else
            {
                callback?.Invoke();
                return;
            }

            if (null == niceWinPresenter)
                niceWinPresenter = UiManager.getPresenter<NiceWinPresenter>();
            niceWinPresenter.openNiceWindown(winType, winPoints, niceWinCompleted);
            niceWinPresenter.skipEvent = breakDashWinPoint;
        }

        private void breakDashWinPoint()
        {
            gameUI.bottomBarPresenter.breakDashWinPoint();
        }

        public bool haveWinWindow(ulong winPoints)
        {
            return (winPoints >= totalBet * gameConfig.BIG_WIN_BET);
        }

        public void showWinWindowAndDashPoint(ulong totalWin, ulong currentWin, Action completeCallBack = null)
        {
            IsShowingWinWindow = true;
            Action dashPointAction = () =>
            {
                IsShowingWinWindow = false;
                gameUI.dashWinPoints(currentWin);
                completeCallBack?.Invoke();
            };
            showWinWindow(totalWin, dashPointAction);
        }

        public virtual void showWinWindow(ulong winPoints, Action callback = null)
        {
            WinWindowPresenter.WinLevels winLevel = WinWindowPresenter.WinLevels.big;

            if (winPoints >= totalBet * gameConfig.ULTIMATE_WIN_BET)
            {
                winLevel = WinWindowPresenter.WinLevels.ultimate;
            }
            else if (winPoints >= totalBet * gameConfig.MASSIVE_WIN_BET)
            {
                winLevel = WinWindowPresenter.WinLevels.massive;
            }
            else if (winPoints >= totalBet * gameConfig.EPIC_WIN_BET)
            {
                winLevel = WinWindowPresenter.WinLevels.epic;
            }
            else if (winPoints >= totalBet * gameConfig.MEGA_WIN_BET)
            {
                winLevel = WinWindowPresenter.WinLevels.mega;
            }
            else if (winPoints >= totalBet * gameConfig.BIG_WIN_BET)
            {
                winLevel = WinWindowPresenter.WinLevels.big;
            }
            else
            {
                DataStore.getInstance.guideServices.noticeWinWindowsState(false);
                callback?.Invoke();
                return;
            }
            winWindowPresenter.openWinWindow(winPoints, winLevel, callback);
        }
        #endregion

        #region 自動遊玩
        public void updateAutoMode()
        {
            gameUI.bottomBarPresenter.setRemainAutoCount(autoCount);
            if (autoCount >= 0)
            {
                gameUI.stopMode = AutoMode.AUTO;
                return;
            }
            gameUI.stopMode = (AutoMode)autoCount;
        }
        public virtual void cancelAutoPlay()
        {
            autoCount = 0;
            gameUI.changeLineAniToLoop();
            updateAutoMode();
        }

        public virtual void onAutoItemHandler(int count)
        {
            autoCount = count;
            updateAutoMode();
            DataStore.getInstance.gameToLobbyService.sendAutoPlayState(IsAutoPlay);
            gameUI.bottomBarPresenter.clickPlayButton();
        }
        #endregion

        #region 狀態機切換
        public virtual void changeState(IHierarchicalState newState, int level = 0)
        {
            AudioManager.instance.stopBGM();
            stateController.TransTo(level, newState);
        }

        public IEnumerator delayChangeState(float delaySeconds, IHierarchicalState newState, int level = 0)
        {
            yield return delaySeconds;
            changeState(newState, level);
        }
        #endregion

        #region UI控制
        public virtual void setFGText(int mode = -1)
        {
            gameUI.setFGCountTxt(freeGameIdx, freeGameTotalCount, mode);
        }
        public virtual void setSFGText(int mode = -1)
        {
        }
        public virtual void openNGBackground()
        {
            gameUI.changeBackgroundByState(GameConfig.GameState.NG);
        }
        public virtual void openBGBackground()
        {
            gameUI.changeBackgroundByState(GameConfig.GameState.BG);
        }
        public virtual void openFGBackground()
        {
            gameUI.changeBackgroundByState(GameConfig.GameState.FG);
        }
        public virtual void openSFGBackground()
        {
            gameUI.changeBackgroundByState(GameConfig.GameState.SFG);
        }
        public virtual void showNormalTable()
        {
            gameUI.showTable(normalGameOriginReelData);
        }
        public virtual void showFreeTable()
        {
            gameUI.showTable(freeGameInitReelData);
        }
        public virtual void showSuperFreeTable()
        {
            gameUI.showTable(freeGameInitReelData);
        }
        public virtual void showBonusTable()
        {
            gameUI.showTable(bonusGameInitReelData);
        }
        public virtual void showMiniTable()
        {

        }
        #endregion

        public virtual async void sendNormalGameSpin() { }
        public virtual async void sendNormalGameEnd(GameConfig.PlayerState stateCode) { }
        public virtual async void sendFreeGameSpin() { }
        public virtual async void sendFreeGameEnd() { }
        public virtual async void sendBonusGameSpin() { }
        public virtual async void sendBonusGameEnd() { }
        public virtual async void sendSuperFreeGameSpin() { }
        public virtual async void sendSuperFreeGameEnd() { }
        public virtual async void sendMiniGameSpin() { }
        public virtual async void sendMiniGameEnd() { }
        protected bool checkResponse(ServerResponse response)
        {
            return Result.OK == response.result;
        }
        public virtual void onFreeAutoItem()
        {
            gameUI.bottomBarPresenter.hideAutoCount();
        }
        public virtual void onBonusAutoItem()
        {
            gameUI.bottomBarPresenter.hideAutoCount();
            bonusAutoCount = bonusGameInitReelData.Length - (bonusGameIdx + 1);
        }
        public virtual void onSpuerFreeAutoItem()
        {
            gameUI.bottomBarPresenter.hideAutoCount();
            superFreeAutoCount = superFreeGameInitReelData.Length - (superFreeGameIdx + 1);
        }
        PlayerWallet playerWallet { get { return DataStore.getInstance.playerInfo.myWallet; } }

        public void forceNotifyUpdatePlayerCoinUI()
        {
            if (null == RackUpWallet)
            {
                return;
            }
            playerWallet.commitAndPush(RackUpWallet.wallet);
        }

        #region Button callback
        public virtual void onSpinButtonClick()
        {
            OnSpinHandler?.Invoke();
        }
        public virtual void onStopButtonClick()
        {
            OnStopHandler?.Invoke();
        }
        #endregion

        #region Data
        public virtual ulong bonusTotalWin() { return 0; }
        public virtual ulong currentWin() { return gameWinInfo != null ? gameWinInfo.getTotalWin : 0; }
        public virtual WinInfo getWinInfo() { return gameWinInfo; }
        public virtual void setBonusInfo() { }
        public virtual int bonusCount() { return 0; }
        public virtual IEnumerator setBonusFinalTable(Action act = null) { yield break; }
        public bool checkHaveEnoughMoney()
        {
            return totalBet <= DataStore.getInstance.playerInfo.playerMoney;
        }
        #endregion

        /// <summary> 設定當時的押注擋位與等級(非NG使用) </summary>
        protected async void setOtherGameBetIndex(int betIndex, int Level)
        {
            betToLockManager.setBtnClick(false);
            ulong showBet = await betToLockManager.temporaryBetIndex(betIndex, Level);
            gameUI.setTotalBetCall(showBet);
        }

        /// <summary>
        /// 驗證版號並合併錢包
        /// </summary>
        /// <param name="data"></param>
        protected void setWallet(WalletData data)
        {
            playerWallet.commitAndPush(data.wallet);
        }

        /// <summary>
        /// 記錄最後收到錢包封包
        /// </summary>
        /// <param name="data"></param>
        protected void saveFinalWallet(WalletData data)
        {
            RackUpWallet = data;
        }
    }
}
