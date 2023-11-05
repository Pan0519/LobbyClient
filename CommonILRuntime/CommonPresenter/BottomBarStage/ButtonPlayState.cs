using System;

namespace CommonPresenter.BottomBarStage
{
    public class ButtonPlayState : ButtonState
    {
        public ButtonPlayState(CustomBtn btn, Action clickHandler, Action longClickHandler) : base(btn, clickHandler, longClickHandler) { }

        public override void enterState(bool isAutoState)
        {
            base.enterState(isAutoState);
            spinBtn.gameObject.setActiveWhenChange(true);
        }

        public override void leaveState()
        {
            base.leaveState();
            spinBtn.gameObject.setActiveWhenChange(false);
        }
    }
}
