using LobbyLogic.NetWork.ResponseStruct;
using System;
using System.Collections.Generic;
namespace Lobby.Popup
{
    public static class PopupTestDataCreator
    {
        public static List<PopupData> make()
        {
            var popups = new List<PopupData>();
            popups.Add(ChargeAct());
            popups.Add(QuestAct());
            return popups;
        }

        static PopupData ChargeAct(bool popUp = true)
        {
            PopupData data = new PopupData();
            data.id = PopupType.CHARGE.ToString();
            data.popup = popUp;
            return data;
        }

        static PopupData QuestAct(bool popUp = true)
        {
            PopupData data = new PopupData();
            data.id = PopupType.BCLASSACTIVITY.ToString();
            data.popup = popUp;
            return data;
        }
    }
}
