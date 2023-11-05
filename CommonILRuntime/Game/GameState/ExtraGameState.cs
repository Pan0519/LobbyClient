using CommonILRuntime.FiniteState;

namespace Game.Slot
{
    public class ExtraGameState : IHierarchicalState
    {
        protected SlotGameBase slotGame;
        protected SlotGameBasePresenter gameUI;
        protected bool isEnter;

        public ExtraGameState(SlotGameBase currentGame, bool isEnter = false)
        {
            slotGame = currentGame;
            gameUI = slotGame.gameUI;
            this.isEnter = isEnter;
        }

        public override void StateBegin()
        {
            gameUI.setPlayBtnEnable(false);
            initState();
        }

        protected virtual void initState(){ }
    }
}
