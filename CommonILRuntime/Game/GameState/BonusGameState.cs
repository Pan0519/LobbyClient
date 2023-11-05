using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UniRx;
using Game.Common;

namespace Game.Slot
{
    using CommonILRuntime.BindingModule;
    using CommonService;

    public class BonusGameState: SlotGameState
    {
        public static Action audioSpinBGM;
        public BonusGamePresenter bonusGamePresenter;
        protected int reaminCount;
        protected bool isPlayingBGM = false;

        public BonusGameState(SlotGameBase currentGame, bool isEnter = false) : base(currentGame,isEnter)
        {
            Debug.LogWarning("<<< BonusGameState Game >>>");
        }

        protected override void initState()
        {
            bonusGamePresenter = UiManager.getPresenter<BonusGamePresenter>();

            DataStore.getInstance.gameToLobbyService.updateGameStataSubject.OnNext(GameConfig.GameState.BG);
            if (isEnter)
            {
                resetTableState();
                return;
            }
            bonusGamePresenter.OpenBGStartWindows(resetTableState);
        }

        protected override void setupCallback()
        {
            base.setupCallback();
            slotGame.OnSpinHandler = onSpinClick;
            slotGame.OnNormalEndResult = onNormalEnd;
            slotGame.OnBonusSpinResult = onReceiveSpinResult;
            slotGame.OnBonusEndResult = onSpinEndResult;
            slotGame.OnTableSpinEnd = onTableEnd;
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

        protected void resetTableState()
        {
            gameUI.bottomBarPresenter.setAsSpinWithNoLongPressBtn();
            initTable();
        }

        protected virtual void initTable()
        {
            gameUI.setAsBonusTable();
            slotGame.onBonusAutoItem();
            setupCallback();

            slotGame.showBonusTable();
            slotGame.setBonusInfo();
            slotGame.setPlayerGameStateAndSubject(GameConfig.PlayerState.BGSpin);
            reaminCount = slotGame.bonusCount() + 1;
            gameUI.setSideText(reaminCount, false);
            slotGame.setFGText();
            slotGame.openBGBackground();
            CoroutineManager.StartCoroutine(delayFirstSpin());
        }

        protected IEnumerator delayFirstSpin()
        {
            yield return CoroutineManager.StartCoroutine(beforeFirstSpinAction());
            gameUI.setPlayBtnEnable(true);
        }

        protected virtual IEnumerator beforeFirstSpinAction()
        {
            var delayTime = isEnter ? SlotGameBase.gameConfig.ENTER_DELAY_TIME : SlotGameBase.gameConfig.BONUS_CUT_SCENE_TIME;
            var delaySpineTime = delayTime + SlotGameBase.gameConfig.NO_WIN_NEXT_SPIN_TIME;
            CoroutineManager.AddCorotuine(delaySpin(delaySpineTime, true));
            yield return delayTime;
        }

        protected virtual void doSpin()
        {
            slotGame.sendBonusGameSpin();
        }

        protected override void doStop()
        {
            gameUI.stopTable();
        }

        protected override void onSpinResult()
        {
            reaminCount--;
            gameUI.setSideText(reaminCount);
            gameUI.spinTable();
            slotGame.onBonusAutoItem();
        }

        protected void onTableEnd()
        {
            slotGame.sendBonusGameEnd();
        }

        public virtual void onSpinEndResult()
        {
            switch (slotGame.NowPlayerState)
            {
                case GameConfig.PlayerState.BGSpin:
                    {
                        reaminCount = slotGame.bonusCount() + 1;
                        if (reaminCount == 3)
                        {
                            gameUI.setSideText(reaminCount);
                        }

                        startDelaySpin();
                    }
                    break;

                case GameConfig.PlayerState.FGSpin:
                    {
                        slotGame.isBGToFG = true;
                        CoroutineManager.StartCoroutine(slotGame.setBonusFinalTable(() => { slotGame.changeState(new FreeGameState(slotGame)); }));
                    }
                    break;
                case GameConfig.PlayerState.NGEndFromBG:
                    {
                        CoroutineManager.StartCoroutine(slotGame.setBonusFinalTable(()=> { slotGame.sendNormalGameEnd(GameConfig.PlayerState.NGEndFromBG); }));
                    }
                    break;
            }

        }

        protected virtual void onNormalEnd()
        {
            if (!slotGame.IsInFG)
            {
                bonusGamePresenter.OpenBGEndWindows(slotGame.bonusTotalWin(), () => { slotGame.changeState(new NormalGameState(slotGame)); });
            }
            else
            {
                bonusGamePresenter.OpenBGEndWindows(slotGame.bonusTotalWin(),
                    async () =>
                    {
                        slotGame.showFreeTable();
                        slotGame.openFGBackground();

                        var freeGamePresenter = UiManager.getPresenter<FreeGamePresenter>();
                        await Task.Delay(TimeSpan.FromSeconds(6.0f));
                        freeGamePresenter.OpenFGEndWindows(slotGame.freeGameWinTotal, slotGame.freeGameTotalCount,
                        () => { slotGame.changeState(new NormalGameState(slotGame)); });
                    });
            }
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
            return 0.5f;
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
    }
}