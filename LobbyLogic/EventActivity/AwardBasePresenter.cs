using CommonILRuntime.Module;
using System;
using UnityEngine.UI;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using LobbyLogic.Audio;
using Lobby.Audio;

namespace EventActivity
{
    public class AwardBasePresenter : ContainerPresenter
    {
        public override UiLayer uiLayer { get { return UiLayer.System; } }

        #region [ Uis & unity component ]
        Text awardTxt;
        Animator awardAnim;
        Button closeBtn;
        #endregion

        ObservableStateMachineTrigger ani_EndTrigger;
        Action animFinishAction;
        RectTransform coinFlyRect;
        public override void initUIs()
        {
            awardTxt = getBindingData<Text>("text_Award");
            awardAnim = getAnimatorData("ani_Show");
            closeBtn = getBtnData("btn_Close");
        }

        public override void init()
        {
            coinFlyRect = closeBtn.GetComponent<RectTransform>();
            close();
        }

        ulong award;
        public AwardBasePresenter openAwardPage(ulong award)
        {
            this.award = award;
            awardTxt.text = award.ToString("N0");
            closeBtn.onClick.AddListener(coinFly);
            open();
            AudioManager.instance.playAudioOnce(AudioPathProvider.getAudioPath(ActivityBlastAudio.BigWin));
            return this;
        }

        void coinFly()
        {
            closeBtn.onClick.RemoveAllListeners();
            AudioManager.instance.stopOnceAudio();
            ActivityDataStore.playClickAudio();
            ActivityDataStore.coinFly(coinFlyRect, award, coinFlyComplete);
        }

        void coinFlyComplete()
        {
            ani_EndTrigger = awardAnim.GetBehaviour<ObservableStateMachineTrigger>();
            ani_EndTrigger.OnStateEnterAsObservable().ObserveOnMainThread().Subscribe(onAniOut).AddTo(uiGameObject);
            awardAnim.SetTrigger("out");
        }

        public AwardBasePresenter setCallbackEvent(Action callbackAfterFinish)
        {
            animFinishAction = callbackAfterFinish;
            return this;
        }

        private void onAniOut(ObservableStateMachineTrigger.OnStateInfo obj)
        {
            IDisposable animTimerDis = null;
            animTimerDis = Observable.Timer(TimeSpan.FromSeconds(obj.StateInfo.length)).Subscribe(_ =>
            {
                animOut();
                animTimerDis.Dispose();
            });
        }

        public virtual void animOut()
        {
            clear();
            if (null != animFinishAction)
            {
                animFinishAction();
            }
        }
    }
}
