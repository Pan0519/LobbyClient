using System;
using UnityEngine;

namespace Game.Slot
{
    using LobbyLogic.Audio;
    using Game.Common;
    using System.Collections;
    using CommonService;
    using CommonPresenter;
    using CommonILRuntime.BindingModule;
    using Services;
    using System;
    using System.Collections.Generic;
    using UniRx;

    public class NormalGameState : SlotGameState
    {
        private IDisposable lobbyFuncListener = null;
        private List<FunctionNo> waiteLobbyFunc = null;
        protected bool isPlayingBGM = false;

        public static Action audioOnTableEnd;
        public static Action audioResetBGM;

        protected virtual float delayStartTime { get { return SlotGameBase.gameConfig.BACK_TO_NORMAL_TIME; } }

        public NormalGameState(SlotGameBase currentGame, bool isEnter = false) : base(currentGame, isEnter)
        {
            waiteLobbyFunc = new List<FunctionNo>();
            Debug.LogWarning("<<< Normal Game >>>");
        }

        ~NormalGameState()
        {
            Debug.LogWarning("<<< ~Normal Game >>>");
            audioOnTableEnd = null;
            audioResetBGM = null;
            waiteLobbyFunc = null;
        }

        protected override bool checkNeedUpdateDailyMission()
        {
            return false;
        }

        public override void StateBegin()
        {
            base.StateBegin();
            lobbyFuncListener = DataStore.getInstance.eventInGameToLobbyService.eventEndSubscribe.Subscribe(removeWaitLobbyFunc);
        }

        protected override void initState()
        {
            setupCallback();
            initTable();
            gameUI.setSideText(-1);
            slotGame.setFGText();
            slotGame.freeGameWinTotal = 0;
            slotGame.freeGameTotalCount = 0;
            slotGame.freeGameIdx = 0;
            slotGame.openNGBackground();

            CoroutineManager.StartCoroutine(checkDelayStart());
        }

        protected virtual void initTable()
        {
            gameUI.setAsNormalTable();
            slotGame.showNormalTable();
        }

        protected override void setupCallback()
        {
            base.setupCallback();
            slotGame.OnSpinHandler = doSpin;
            slotGame.OnStopHandler = onTableStop;
            slotGame.OnNormalEndResult = onNormalEnd;
            slotGame.OnTableSpinEnd = onTableEnd;
            slotGame.OnNormalSpinResult = onReceiveSpinResult;
        }

        protected IEnumerator checkDelayStart()
        {
            updatePlayBtnInStateInit();

            if (!isEnter)
            {
                yield return delayStartTime;
                slotGame.showWinWindow(slotGame.serverGameTotalWin, start);
            }
            else
            {
                start();
            }
        }

        protected void updatePlayBtnInStateInit()
        {
            slotGame.betToLockManager.checkNowBetIndex();
            gameUI.setPlayBtnEnable(slotGame.IsAutoPlay);
            if (slotGame.IsAutoPlay)
            {
                gameUI.bottomBarPresenter.setAsStopButton(true);
                slotGame.updateAutoMode();
            }
        }

        protected void start()
        {
            startHook();
            CoroutineManager.scheduler.StartCoroutine(resetNormalGameState());
        }

        protected virtual void startHook() { }

        protected IEnumerator resetNormalGameState()
        {
            DataStore.getInstance.gameToLobbyService.updateGameStataSubject.OnNext(GameConfig.GameState.NG);
            DataStore.getInstance.eventInGameToLobbyService.OpenFuncInLobby(FunctionNo.UpdateAdventureMission);
            noticeUpdateDailyMission();
            yield return new BooleanWrapper(() => (waiteLobbyFunc.Count == 0));
            yield return CoroutineManager.StartCoroutine(beforeResetNormalGameState());
            updateStateWithAutoSpin();
        }

        private void noticeUpdateDailyMission()
        {
#if !GAME_PROJECT
            waiteLobbyFunc.Add(FunctionNo.UpdateDailyMission);
#endif
            DataStore.getInstance.eventInGameToLobbyService.OpenFuncInLobby(FunctionNo.UpdateDailyMission);
        }

        protected virtual IEnumerator beforeResetNormalGameState()
        {
            yield break;
        }

        protected void updateStateWithAutoSpin()
        {
            bool isAutoPlay = slotGame.IsAutoPlay;
            updateBtnState(isAutoPlay);
            checkAutoSpin(isAutoPlay);
        }

        private void updateBtnState(bool isAutoPlay)
        {
            if (!isAutoPlay)
            {
                gameUI.bottomBarPresenter.setAsSpinButton();
                gameUI.setPlayBtnEnable(true);
            }
        }

        protected virtual void checkAutoSpin(bool isAutoPlay)
        {
            if (isAutoPlay)
            {
                autoDoSpin();
            }
            else
            {
                slotGame.betToLockManager.setBtnClick(true);
                gameUI.showMapInfoBtn(true);
            }
        }

        protected void autoDoSpin()
        {
            if (!slotGame.checkHaveEnoughMoney())
            {
                showNoCoinMsgAndInterruptAuto();
                return;
            }

            doSpin();
        }

        private void showNoCoinMsgAndInterruptAuto()
        {
            //UiManager.getPresenter<MsgBoxPresenter>().openNoCoinMsg();
            DataStore.getInstance.eventInGameToLobbyService.OpenFuncInLobby(FunctionNo.NoCoinNotice);
            slotGame.cancelAutoPlay();
            updateBtnState(false);
        }

        protected void doSpin()
        {
            slotGame.betToLockManager.setBtnClick(false);
            gameUI.bottomBarPresenter.setAsStopButton(slotGame.IsAutoPlay);
            gameUI.setPlayBtnEnable(false);
            CoroutineManager.scheduler.StartCoroutine(OnSpin());
        }

        protected IEnumerator OnSpin()
        {
            yield return CoroutineManager.scheduler.StartCoroutine(gameUI.CheckCloseMap());
            preparePreSpinTable();
        }

        protected virtual void preparePreSpinTable()
        {
            gameUI.delayResetWinPoints();
            slotGame.freeGameIdx = 0;
            slotGame.freeGameTotalCount = 0;
            slotGame.bonusGameIdx = 0;
            gameUI.preSpinTable();
            slotGame.sendNormalGameSpin();
            resetBGM();
        }

        protected override void onSpinResult()
        {
            gameUI.spinTable();
        }

        protected override void doStop()
        {
            slotGame.cancelAutoPlay();
            gameUI.stopTable();
        }

        protected virtual void onTableEnd()
        {
            if (slotGame.IsInBG)
            {
                turnToBonusGame();
            }
            else if (slotGame.IsInFG)
            {
                turnToFreeGame();
            }
            else
            {
                slotGame.sendNormalGameEnd(GameConfig.PlayerState.NGEnd);
            }
        }

        private void turnToBonusGame()
        {
            cancelCountdownFadeBgm();
            gameUI.allChipShine();

            audioOnTableEnd?.Invoke();

            if (slotGame.IsInSFG)
            {
                slotGame.changeState(new SuperFreeGameState(slotGame));
            }
            else
            {
                CoroutineManager.StartCoroutine(slotGame.delayChangeState(SlotGameBase.gameConfig.BONUSGAME_ENTER_DELAY, new BonusGameState(slotGame)));
            }
        }

        private void turnToFreeGame()
        {
            cancelCountdownFadeBgm();
            gameUI.allScatterOpen();
            audioOnTableEnd?.Invoke();
            slotGame.freeGameIdx = 0;
            CoroutineManager.StartCoroutine(slotGame.delayChangeState(SlotGameBase.gameConfig.FREEGAME_ENTER_DELAY, new FreeGameState(slotGame)));
        }

        protected virtual void onNormalEnd()
        {
            slotGame.showWinWindow(slotGame.serverGameTotalWin, () =>
            {
                attempToNotifyUpdatePlayerCoint();
                countdownFadeBgm();
                CoroutineManager.scheduler.StartCoroutine(resetNormalGameState());
            });
        }

        protected void attempToNotifyUpdatePlayerCoint()
        {
            slotGame.forceNotifyUpdatePlayerCoinUI();
        }

        protected virtual void resetBGM()
        {
            if (!isPlayingBGM)
            {
                audioResetBGM?.Invoke();
            }
            else
            {
                cancelCountdownFadeBgm();
            }
            AudioManager.instance.breakFadeBgmAudio();
        }

        protected void countdownFadeBgm()
        {
            isPlayingBGM = true;
            CoroutineManager.AddCorotuine(fadeoutBgmCountdown());
        }

        protected virtual IEnumerator fadeoutBgmCountdown()
        {
            yield return 3f;
            AudioManager.instance.fadeBgmAudio(SlotGameBase.gameConfig.BGM_FADE_TIME, true);
            isPlayingBGM = false;
        }

        protected virtual void cancelCountdownFadeBgm()
        {
            CoroutineManager.StopCorotuine(fadeoutBgmCountdown());
            isPlayingBGM = false;
        }

        protected override bool checkNeedClosePlayBtnOnRollEnd()
        {
            return !slotGame.IsAutoPlay;
        }

        private void removeWaitLobbyFunc(FunctionNo functionNo)
        {
            if (waiteLobbyFunc.Contains(functionNo))
            {
                waiteLobbyFunc.Remove(functionNo);
            }
        }

        public override void StateEnd()
        {
            base.StateEnd();
            lobbyFuncListener.Dispose();
            waiteLobbyFunc.Clear();
        }
    }
}