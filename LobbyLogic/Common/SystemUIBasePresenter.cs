using CommonILRuntime.Module;
using UnityEngine;
using UniRx.Triggers;
using System;
using UniRx;
using LobbyLogic.Audio;
using CommonService;

namespace Lobby.Common
{
    public class SystemUIBasePresenter : ContainerPresenter
    {
        Animator closeAnim = null;
        IDisposable animTriggerDis;
        public override void init()
        {
            closeAnim = getUiAnimator();

            if (null == closeAnim)
            {
                return;
            }
            closeAnim.updateMode = AnimatorUpdateMode.UnscaledTime;
            closeAnim.ResetTrigger("out");
            var animTriggers = closeAnim.GetBehaviour<ObservableStateMachineTrigger>();
            animTriggerDis = animTriggers.OnStateEnterAsObservable().ObserveOnMainThread().Subscribe(onAniOut).AddTo(uiGameObject);
        }

        /// <summary>
        /// 可繼承後覆寫，若ui gameobjcet root 無 animator, 自行實作獲得 animator 的函式
        /// </summary>
        /// <returns></returns>
        public virtual Animator getUiAnimator()
        {
            return uiGameObject.GetComponent<Animator>();
        }

        public void closeBtnClick()
        {
            closeEvent();
            AudioManager.instance.playAudioOnce(CommonAudioPathProvider.getAudioPath(BasicCommonSound.InfoBtn));
            closePresenter();
        }

        public virtual void closeEvent()
        {

        }

        public void closePresenter()
        {
            if (null == closeAnim)
            {
                Debug.LogWarning("closeAnim is Null");
                return;
            }

            closeAnim.SetTrigger("out");
        }

        public virtual void animOut()
        {

        }

        private void onAniOut(ObservableStateMachineTrigger.OnStateInfo obj)
        {
            IDisposable animTimerDis = null;
            animTimerDis = Observable.Timer(TimeSpan.FromSeconds(obj.StateInfo.length), Scheduler.MainThreadIgnoreTimeScale).Subscribe(_ =>
                {
                    animTriggerDis.Dispose();
                    animTimerDis.Dispose();
                    animOut();
                });
        }
    }

    public class SystemUINodePresenter : NodePresenter
    {
        Animator closeAnim = null;
        IDisposable animTriggerDis;
        public override void init()
        {
            closeAnim = getUiAnimator();

            if (null == closeAnim)
            {
                return;
            }
            closeAnim.updateMode = AnimatorUpdateMode.UnscaledTime;
            closeAnim.ResetTrigger("out");
            var animTriggers = closeAnim.GetBehaviour<ObservableStateMachineTrigger>();
            //if (null == animTriggerDis)
            //{
            //    Debug.Log("get animtrigger is null");
            //    return;
            //}
            animTriggerDis = animTriggers.OnStateEnterAsObservable().ObserveOnMainThread().Subscribe(onAniOut).AddTo(uiGameObject);
        }

        /// <summary>
        /// 可繼承後覆寫，若ui gameobjcet root 無 animator, 自行實作獲得 animator 的函式
        /// </summary>
        /// <returns></returns>
        public virtual Animator getUiAnimator()
        {
            return uiGameObject.GetComponent<Animator>();
        }

        public void closeBtnClick()
        {
            closeEvent();
            AudioManager.instance.playAudioOnce(CommonAudioPathProvider.getAudioPath(BasicCommonSound.InfoBtn));
            closePresenter();
        }

        public virtual void closeEvent()
        {

        }

        public void closePresenter()
        {
            if (null == closeAnim)
            {
                Debug.LogWarning("closeAnim is Null");
                return;
            }

            closeAnim.SetTrigger("out");
        }

        public virtual void animOut()
        {

        }

        private void onAniOut(ObservableStateMachineTrigger.OnStateInfo obj)
        {
            IDisposable animTimerDis = null;
            animTimerDis = Observable.Timer(TimeSpan.FromSeconds(obj.StateInfo.length), Scheduler.MainThreadIgnoreTimeScale).Subscribe(_ =>
            {
                animTriggerDis.Dispose();
                animTimerDis.Dispose();
                animOut();
            });
        }
    }
}
