using System;

namespace CommonPresenter.BottomBarStage
{
    public class ButtonState : IButtonState
    {
        protected CustomBtn spinBtn;
        private Action clickHandler = null;
        private Action longClickHandler = null;

        public ButtonState(CustomBtn btn, Action clickHandler, Action longPressHandler)
        {
            spinBtn = btn;
            this.clickHandler = clickHandler;
            this.longClickHandler = longPressHandler;
           
        }

        public virtual void enterState(bool isAutoState = false) 
        {
            spinBtn.clickHandler = clickHandler;
            spinBtn.onLongPress = longClickHandler;
        }
        public virtual void leaveState() 
        {
            spinBtn.clickHandler = null;
            spinBtn.onLongPress = null;
        }

        public void setEnable(bool enable)
        {
            spinBtn.setInteractable(enable);
        }

        public void onClick()
        {
            clickHandler?.Invoke();
        }
    }
}
