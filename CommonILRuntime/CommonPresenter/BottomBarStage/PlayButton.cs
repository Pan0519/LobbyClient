using System;

namespace CommonPresenter.BottomBarStage
{
    public class PlayButton
    {
        private IButtonState currentState = null;
        private Action<bool> enableSubscriber = null;
        private bool isEnable = false;

        public void registerOnEnableChange(Action<bool> onEnableChangeHandler)
        {
            enableSubscriber = onEnableChangeHandler;
        }

        public void changeState(IButtonState newState, bool isAutoState = false)
        {
            if (null != currentState)
            {
                currentState.leaveState();
            }
            currentState = newState;
            currentState.enterState(isAutoState);
            currentState.setEnable(isEnable);
        }

        public void setEnable(bool enable)
        {
            currentState.setEnable(enable);
            isEnable = enable;
            onEnableHandler(enable);
        }

        public void onClick()
        {
            currentState.onClick();
        }

        void onEnableHandler(bool isEnable)
        {
            enableSubscriber?.Invoke(isEnable);
        }
    }
}
