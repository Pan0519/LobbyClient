using LobbyLogic.NetWork.ResponseStruct;
using System;

namespace HighRoller
{
    interface IHighRollerReward
    {
        void openReward(HighRollerBoardResultResponse highRoller);

        void setToNextPopCB(Action toNextPop);
    }
}
