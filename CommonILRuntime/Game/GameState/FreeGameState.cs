using System;
using System.Collections;
using UnityEngine;
using Game.Common;

namespace Game.Slot
{
    using LobbyLogic.Audio;
    using CommonILRuntime.BindingModule;
    using CommonService;

    public class FreeGameState : SlotGameState
    {
        public static Action audioSpinBGM;
        public static Action audioOnFreeEndBGSpin;
        public static Action audioFreeEndShowNotify;
        public static Action audioFreeEndMoreSpin;
        public FreeGamePresenter freeGamePresenter;
        protected bool isPlayingBGM = false;
        public FreeGameState(SlotGameBase currentGame, bool isEnter = false) : base(currentGame, isEnter)
        {
            Debug.LogWarning("<<< FreeGameState Game >>>");
        }

        protected override void initState()
        {
            DataStore.getInstance.gameToLobbyService.updateGameStataSubject.OnNext(GameConfig.GameState.FG);
            freeGamePresenter = UiManager.getPresenter<FreeGamePresenter>();
            if (!slotGame.isBGToFG)
            {
                if (isEnter)
                {
                    resetTableState();
                    return;
                }
                freeGamePresenter.OpenFGStartWindows(slotGame.freeGameTotalCount, "", resetTableState);
            }
            else
            {
                slotGame.isBGToFG = false;
                var bonusGamePresenter = UiManager.getPresenter<BonusGamePresenter>();
                bonusGamePresenter.OpenBGEndWindows(slotGame.bonusTotalWin(), resetTableState);
            }
        }

        protected void resetTableState()
        {
            gameUI.bottomBarPresenter.setAsSpinWithNoLongPressBtn();
            initTable();
        }

        protected virtual void initTable()
        {
            gameUI.setAsFreeTable();
            slotGame.onFreeAutoItem();
            setupCallback();
            slotGame.showFreeTable();
            gameUI.setSideText(-1);
            slotGame.setFGText(1);
            slotGame.openFGBackground();
            slotGame.setPlayerGameStateAndSubject(GameConfig.PlayerState.FGSpin);
            CoroutineManager.StartCoroutine(delayFirstSpin());
        }

        protected IEnumerator delayFirstSpin()
        {
            yield return CoroutineManager.StartCoroutine(beforeFirstSpinAction());
            gameUI.setPlayBtnEnable(true);
        }

        protected virtual IEnumerator beforeFirstSpinAction()
        {
            var delayTime = isEnter ? SlotGameBase.gameConfig.ENTER_DELAY_TIME : SlotGameBase.gameConfig.FREE_CUT_SCENE_TIME;
            var delaySpineTime = delayTime + SlotGameBase.gameConfig.NO_WIN_NEXT_SPIN_TIME;
            CoroutineManager.AddCorotuine(delaySpin(delaySpineTime, true));
            yield return delayTime;
        }

        protected override void setupCallback()
        {
            base.setupCallback();
            slotGame.OnSpinHandler = onSpinClick;
            slotGame.OnFreeSpinResult = onReceiveSpinResult;
            slotGame.OnTableSpinEnd = onTableEnd;
            slotGame.OnNormalEndResult = onNormalEnd;
            slotGame.OnFreeEndResult = onFreeEnd;
        }

        protected void onSpinClick()
        {
            if (!isPlayingBGM)
            {
                isPlayingBGM = true;
                audioSpinBGM?.Invoke();
            }
            CoroutineManager.StopCorotuine(delaySpin(0f));
            prepareSpin();
        }

        protected void prepareSpin()
        {
            gameUI.bottomBarPresenter.setAsStopButton(false);
            gameUI.setPlayBtnEnable(false);
            doSpin();
        }

        protected virtual void doSpin()
        {
            gameUI.preSpinTable();
            slotGame.sendFreeGameSpin();
        }

        protected override void onSpinResult()
        {
            slotGame.setFGText(0);  //現有次數在收到封包後已更新，在此更新UI
            gameUI.spinTable();
            slotGame.onFreeAutoItem();
        }

        protected override void doStop()
        {
            gameUI.stopTable();
        }

        protected virtual void onTableEnd()
        {
            slotGame.sendFreeGameEnd();
        }

        protected virtual void onFreeEnd()
        {
            switch (slotGame.NowPlayerState)
            {
                case GameConfig.PlayerState.NGEndFromFG:
                    {
                        slotGame.showNiceWinWindow(slotGame.currentWin(), () =>
                        {
                            float delayTime = getDelaySpinTime();
                            CoroutineManager.StartCoroutine(delayEnd(delayTime));
                        });
                    }
                    break;
                case GameConfig.PlayerState.FGSpin:
                    {
                        CoroutineManager.StartCoroutine(freeEnd());
                    }
                    break;
                case GameConfig.PlayerState.BGSpin:
                    {
                        AudioManager.instance.stopLoop();
                        gameUI.allChipShine();
                        audioOnFreeEndBGSpin?.Invoke();
                        chageBonusGameState();
                    }
                    break;
            }
        }

        protected IEnumerator delayEnd(float delayTime)
        {
            yield return delayTime;
            slotGame.sendNormalGameEnd(GameConfig.PlayerState.NGEndFromFG);
        }

        protected virtual void onNormalEnd()
        {
            AudioManager.instance.stopLoop();
            Debug.Log($"FreeGame onNormalEnd freeGameWinTotal: {slotGame.freeGameWinTotal}");
            freeGamePresenter.OpenFGEndWindows(slotGame.freeGameWinTotal, slotGame.freeGameTotalCount,
                () => { chageNormalGameState(); });
        }

        protected virtual IEnumerator freeEnd()
        {
            bool showNotify = false;
            var info = slotGame.getWinInfo();
            if (null != info)
            {
                if (info.Fg_Game_Times > 0)
                {
                    showNotify = true;
                }
            }

            if (showNotify)
            {
                gameUI.allScatterOpen();
                audioFreeEndShowNotify?.Invoke();
                yield return SlotGameBase.gameConfig.FREEGAME_ENTER_DELAY;
                showExtraFgWindows(info.Fg_Game_Times);
            }
            else
            {
                slotGame.showNiceWinWindow(slotGame.currentWin(), startDelaySpin);
            }
        }

        protected void showExtraFgWindows(int fgCount)
        {
            freeGamePresenter.OpenFGExtraWindows(fgCount, () =>
            {
                slotGame.setFGText(0);
                audioFreeEndMoreSpin?.Invoke();
                CoroutineManager.StartCoroutine(delayNext());
            });
        }

        protected IEnumerator delayNext()
        {
            yield return 1.5f;
            slotGame.showNiceWinWindow(slotGame.currentWin(), startDelaySpin);
        }

        protected void startDelaySpin()
        {
            var delayTime = getDelaySpinTime();

            gameUI.bottomBarPresenter.setAsSpinWithNoLongPressBtn();
            gameUI.setPlayBtnEnable(true);
            CoroutineManager.AddCorotuine(delaySpin(delayTime));
        }

        protected virtual float getDelaySpinTime()
        {
            var winData = slotGame.getWinInfo();
            return (null == winData || 0 == winData.Total_Win) ? SlotGameBase.gameConfig.NO_WIN_NEXT_SPIN_TIME : SlotGameBase.gameConfig.ALL_LINE_PERFORM_TIME;
        }

        protected IEnumerator delaySpin(float delayTime, bool isFirstSpin = false)
        {
            yield return delayTime;
            if (isFirstSpin)
            {
                audioSpinBGM?.Invoke();
            }
            prepareSpin();
        }

        protected virtual void chageNormalGameState()
        {
            slotGame.changeState(new NormalGameState(slotGame));
        }

        protected virtual void chageBonusGameState()
        {
            CoroutineManager.StartCoroutine(slotGame.delayChangeState(SlotGameBase.gameConfig.BONUSGAME_ENTER_DELAY, new BonusGameState(slotGame)));
        }
        protected virtual void chageJackpotGameState()
        {

        }
    }
}