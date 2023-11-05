using System;

namespace CommonPresenter.BottomBarStage
{
    public interface IButtonState
    {
        void enterState(bool isAutoState);
        void leaveState();
        void setEnable(bool enable);
        void onClick();
    }
}
