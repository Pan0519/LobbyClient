using System;
using System.Threading.Tasks;
using UnityEngine;
using UniRx;
using Game.Common;

namespace Game.Slot
{
    using CommonILRuntime.BindingModule;
    using CommonService;
    using System.Collections;

    public class MiniGameState: SlotGameState
    {
        public static Action audioSpinBGM;
        public MiniGamePresenter miniGamePresenter;
        protected bool isPlayingBGM = false;

        public MiniGameState(SlotGameBase currentGame, bool isEnter = false) : base(currentGame,isEnter)
        {
            Debug.LogWarning("<<< MiniGameState Game >>>");
        }

        protected override void initState()
        {
            miniGamePresenter = UiManager.getPresenter<MiniGamePresenter>();

            DataStore.getInstance.gameToLobbyService.updateGameStataSubject.OnNext(GameConfig.GameState.JP);
            if (isEnter)
            {
                resetTableState();
                return;
            }
            miniGamePresenter.OpenMiniGameStartWindows(resetTableState);
        }

        protected override void setupCallback()
        {
            base.setupCallback();
            slotGame.OnSpinHandler = onSpinClick;
            slotGame.OnNormalEndResult = onNormalEnd;
            slotGame.OnMiniSpinResult = onReceiveSpinResult;
            slotGame.OnMiniEndResult = onSpinEndResult;
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
            setupCallback();

            slotGame.showMiniTable();
            slotGame.setPlayerGameStateAndSubject(GameConfig.PlayerState.JPSpin);
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

        protected virtual void doSpin()
        {
            slotGame.sendMiniGameSpin();
        }

        protected override void doStop()
        {
            gameUI.stopTable();
        }

        protected override void onSpinResult()
        {
            gameUI.spinTable();
        }

        protected virtual void onTableEnd()
        {
            slotGame.sendMiniGameEnd();
        }

        protected virtual void onSpinEndResult()
        {
            switch (slotGame.NowPlayerState)
            {
                case GameConfig.PlayerState.JPSpin:
                    {
                        startDelaySpin();
                    }
                    break;
                case GameConfig.PlayerState.NGEndFromJP:
                    {
                        float delayTime = getDelaySpinTime();
                        CoroutineManager.StartCoroutine(delayEnd(delayTime));
                    }
                    break;
            }

        }

        protected IEnumerator delayEnd(float delayTime)
        {
            yield return delayTime;
            slotGame.sendNormalGameEnd(GameConfig.PlayerState.NGEndFromJP);
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

        protected virtual void onNormalEnd()
        {
            miniGamePresenter.OpenMiniGameEndWindows(slotGame.gameJPReelWin, () => 
                { 
                    slotGame.changeState(new NormalGameState(slotGame)); 
                });
        }
    }
}