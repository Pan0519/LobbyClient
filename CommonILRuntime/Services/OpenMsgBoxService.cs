using LobbyLogic.Audio;
using CommonService;
using UnityEngine;
using System;
using CommonPresenter;
using CommonILRuntime.BindingModule;

namespace Services
{
    public class OpenMsgBoxService
    {
        static OpenMsgBoxService _instance = null;

        public static OpenMsgBoxService Instance
        {
            get
            {
                if (null == _instance)
                {
                    _instance = new OpenMsgBoxService();
                }
                return _instance;
            }
        }

        MsgBoxPresenter boxPresenter = null;
        Action cbAction;

        public void openNormalBox(string title, string content, Action callback = null)
        {
            cbAction = callback;
            AudioManager.instance.playAudioOnce(CommonAudioPathProvider.getAudioPath(BasicCommonSound.PopupEffect));
            boxPresenter = UiManager.getPresenter<MsgBoxPresenter>();
            boxPresenter.openNormalBox(collectCallback, title, content);
        }

        void collectCallback()
        {
            clearMsgBox();
            boxPresenter = null;
            if (null != cbAction)
            {
                cbAction();
            }
        }

        public void clearMsgBox()
        {
            if (null != boxPresenter)
            {
                boxPresenter.clear();
            }
        }
    }
}
