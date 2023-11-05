using CommonILRuntime.Module;
using UnityEngine;
using UnityEngine.UI;
using UniRx;
using UniRx.Triggers;
using System;
using System.Collections.Generic;
using Services;
using LobbyLogic.Audio;
using Lobby.Audio;
using EventActivity;

namespace Event.Common
{
    class ActivityJPRewardPresenter : ContainerPresenter
    {
        public override string objPath => "prefab/activity_jp_board";
        public override UiLayer uiLayer { get => UiLayer.System; }

        Animator winAnim;
        CustomTextSizeChange awardNumTxt;
        Button collectBtn;
        RectTransform coinFlySource;
        List<IDisposable> triggers = new List<IDisposable>();
        Action animOutEvent;

        public override void initUIs()
        {
            winAnim = getAnimatorData("win_ani");
            awardNumTxt = getBindingData<CustomTextSizeChange>("num_win");
            collectBtn = getBtnData("collect_btn");
        }

        public override void init()
        {
            coinFlySource = collectBtn.GetComponent<RectTransform>();
            collectBtn.onClick.AddListener(coinFly);
        }

        ulong rewardValue = 0;
        public void openAward(string jpTrigger, ulong award, Action finishEvent = null)
        {
            animOutEvent = finishEvent;
            rewardValue = award;
            awardNumTxt.text = award.ToString("N0");
            AudioManager.instance.playAudioOnce(AudioPathProvider.getAudioPath(ActivityBlastAudio.BigWin));
            open();
            winAnim.SetTrigger(jpTrigger.ToLower());
        }

        void coinFly()
        {
            collectBtn.onClick.RemoveAllListeners();
            ActivityDataStore.playClickAudio();
            ActivityDataStore.coinFly(coinFlySource, rewardValue, playOut);
        }
        void playOut()
        {
            var animTrigger = winAnim.GetBehaviours<ObservableStateMachineTrigger>();
            for (int i = 0; i < animTrigger.Length; ++i)
            {
                triggers.Add(animTrigger[i].OnStateEnterAsObservable().ObserveOnMainThread().Subscribe(onAniOut).AddTo(uiGameObject));
            }

            winAnim.SetTrigger("close");
        }

        private void onAniOut(ObservableStateMachineTrigger.OnStateInfo obj)
        {
            IDisposable animTimerDis = null;
            animTimerDis = Observable.Timer(TimeSpan.FromSeconds(obj.StateInfo.length)).Subscribe(_ =>
            {
                if (null != animOutEvent)
                {
                    animOutEvent();
                }
                animTimerDis.Dispose();
                UtilServices.disposeSubscribes(triggers.ToArray());
                clear();
            }).AddTo(uiGameObject);
        }
    }
}
