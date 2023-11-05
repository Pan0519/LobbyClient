using LobbyLogic.NetWork.ResponseStruct;
using System;

namespace Lobby.Popup
{
    public interface IPopUpActivityPresenter
    {
        void open();
        void setData(PopupData data);
        void setOnCloseHandler(Action handler);
    }
}
