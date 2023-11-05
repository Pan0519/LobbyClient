using CommonILRuntime.Module;
using UnityEngine.UI;
using UnityEngine;
using UniRx;
using UniRx.Triggers;
using System;

namespace Common
{
    public class InfoBaseNode : NodePresenter
    {
        Button closeBtn;
        Animator closeAnim;
        IDisposable animTriggerDis;
        public override void initUIs()
        {
            closeBtn = getBtnData("info_close_btn");
            closeAnim = getAnimatorData("info_anim");
        }

        public override void init()
        {
            closeBtn.onClick.AddListener(closeBtnClick);
        }

        public override void open()
        {
            closeAnim.ResetTrigger("out");
            base.open();
        }
        public void closeBtnClick()
        {
            var animTriggers = closeAnim.GetBehaviour<ObservableStateMachineTrigger>();
            animTriggerDis = animTriggers.OnStateEnterAsObservable().ObserveOnMainThread().Subscribe(onAniOut).AddTo(uiGameObject);
            closeAnim.SetTrigger("out");
        }

        private void onAniOut(ObservableStateMachineTrigger.OnStateInfo obj)
        {
            IDisposable animTimerDis = null;
            animTimerDis = Observable.Timer(TimeSpan.FromSeconds(obj.StateInfo.length), Scheduler.MainThreadIgnoreTimeScale).Subscribe(_ =>
            {
                animTriggerDis.Dispose();
                animTimerDis.Dispose();
                close();
            });
        }
    }
}
