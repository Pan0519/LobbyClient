using CommonILRuntime.BindingModule;
using LobbyLogic.NetWork.ResponseStruct;
using UnityEngine;
using System.Collections.Generic;
using CommonService;

namespace Lobby.Popup
{
    public class PopUpFactory
    {
        public PopUpFactory()
        {
            initPopupTypeMap();
        }
        Dictionary<string, PopupType> popupTypeMap = new Dictionary<string, PopupType>();
        PopupType getPopupType(string id)
        {
            PopupType type;
            if (popupTypeMap.TryGetValue(id, out type))
            {
                return type;
            }
            return PopupType.ERROR_TYPE;
        }

        void initPopupTypeMap()
        {
            popupTypeMap.Add("activity-test-1", PopupType.BCLASSACTIVITY);
        }

        public IPopUpActivityPresenter getPopUp(PopupData data)
        {
            IPopUpActivityPresenter presenter = null;
            PopupType type = getPopupType(data.id);
            switch (type)
            {
                case PopupType.CHARGE:
                    {
                        presenter = UiManager.getPresenter<PopUpCharge>();
                    }
                    break;
                case PopupType.BCLASSACTIVITY:
                    {
                        if (DataStore.getInstance.playerInfo.level >= 4)
                        {
                            presenter = UiManager.getPresenter<PopUpBClassActivity>();
                        }
                    }
                    break;
                case PopupType.RICHMAN:
                    {
                        presenter = UiManager.getPresenter<PopUpRichman>();
                    }
                    break;
                default:
                    {
                        Debug.LogWarning($"PopUpFactory getPopup, id error: {data.id}");
                        return null;
                    }
            }
            if (null != presenter)
            {
                presenter.setData(data);
                presenter.open();
            }
            return presenter;
        }
    }
}
