using CommonILRuntime.Module;
using Lobby.ActivityUI.Audio;
using LobbyLogic.Audio;
using System;
using UnityEngine;
using UnityEngine.UI;
using CommonPresenter;

namespace Lobby.Popup
{
    public abstract class PopUpActivity : SystemUIBasePresenter
    {
        public override UiLayer uiLayer { get { return UiLayer.System; } }

        Action onClosehandler = null;

        Button confirmButton;
        Button closeButton;
        Animator animator;

        public override void initUIs()
        {
            confirmButton = getBtnData("confirmButton");
            closeButton = getBtnData("closeButton");
            animator = getAnimatorData("animator");
        }

        public override Animator getUiAnimator()
        {
            return animator;
        }

        public override void init()
        {
            confirmButton.onClick.AddListener(onConfirmClick);
            closeButton.onClick.AddListener(closeBtnClick);
            AudioManager.instance.playAudioOnce(PopupUISoundPathProvider.GetAudioPath(ActivityUIAudio.PopUp));
            base.init();
        }

        public void setOnCloseHandler(Action handler)
        {
            onClosehandler = handler;
        }

        //protected void setInfo(string info)
        //{
        //    infoText.text = info;
        //}

        public override void animOut()
        {
            onClosehandler?.Invoke();
            clear();
        }

        protected abstract void onConfirmClick();
    }
}
