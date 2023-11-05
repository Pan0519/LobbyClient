using UnityEngine;
using System.Collections;
using CommonILRuntime.FiniteState;
using CommonILRuntime.Game.GameTime;
using CommonService;

namespace Game.Slot
{
    public abstract class SlotGameState : IHierarchicalState
    {
        protected SlotGameBase slotGame;
        protected SlotGameBasePresenter gameUI;
        protected bool isEnter;
        //protected CoroutineScheduler coroutineScheduler = new CoroutineScheduler();
        public SlotGameState(SlotGameBase currentGame, bool isEnter = false)
        {
            slotGame = currentGame;
            gameUI = slotGame.gameUI;
            this.isEnter = isEnter;
            if (checkNeedUpdateDailyMission())
            {
                DataStore.getInstance.eventInGameToLobbyService.OpenFuncInLobby(Services.FunctionNo.UpdateDailyMission);
            }
        }

        protected virtual bool checkNeedUpdateDailyMission()
        {
            return true;
        }

        public override void StateBegin()
        {
            gameUI.bottomBarPresenter.setBetBtnEnable(false);
            gameUI.setPlayBtnEnable(false);
            initState();
        }

        protected virtual void setupCallback()
        {
            slotGame.OnTableRollEnd = onTableRollEnd;
            slotGame.OnStopHandler = onTableStop;
        }

        protected virtual void initState()
        {

        }

        protected void onReceiveSpinResult()
        {
            CoroutineManager.StartCoroutine(delayShowSpinResult());
        }

        private IEnumerator delayShowSpinResult()
        {
            yield return CoroutineManager.StartCoroutine(waiteRollingAni());
            gameUI.setPlayBtnEnable(true);
            onSpinResult();
        }

        protected virtual IEnumerator waiteRollingAni()
        {
            yield break;
        }

        protected virtual void onSpinResult()
        {

        }

        protected void onTableStop()
        {
            gameUI.setPlayBtnEnable(false);
            doStop();
        }

        protected virtual void doStop()
        {

        }

        protected void onTableRollEnd()
        {
            if (checkNeedClosePlayBtnOnRollEnd())
            {
                gameUI.bottomBarPresenter.setPlayBtnEnable(false);
            }
        }

        protected virtual bool checkNeedClosePlayBtnOnRollEnd()
        {
            return true;
        }
    }
}
