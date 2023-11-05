using UnityEngine;
using CommonILRuntime.BindingModule;
using UniRx.Triggers;
using UniRx;
using System;
using CommonService;

namespace CommonPresenter
{
    class GamePortraitTopBarPresenter : GameTopBarPresenter
    {
        //public override string objPath => "prefab/game/game_top_bar_portrait";

        Animator levelInfoAnim;
        IDisposable animTriggerDis;
        public override float barEffectPosX { get => 150; }
        public override void initUIs()
        {
            base.initUIs();
            levelInfoAnim = getAnimatorData("lvup_info_anim");
            levelInfoAnim.gameObject.setActiveWhenChange(false);
        }

        public override void init()
        {
            base.init();
            lvupObjRect.gameObject.setActiveWhenChange(false);
        }

        public override void openLvupObj()
        {
            audioManagerPlay(MainGameCommonSound.LvUpSmall);
            lvupObjRect.gameObject.setActiveWhenChange(true);
            Observable.TimerFrame(27).Subscribe(_ =>
            {
                lvupRewardCoinFly();
            }).AddTo(uiGameObject);
        }

        public override void closeLvupObj()
        {
            var animTriggers = levelInfoAnim.GetBehaviour<ObservableStateMachineTrigger>();
            animTriggerDis = animTriggers.OnStateEnterAsObservable().ObserveOnMainThread().Subscribe(onLvupAnimOut).AddTo(uiGameObject);
            levelInfoAnim.SetTrigger("out");
        }

        private void onLvupAnimOut(ObservableStateMachineTrigger.OnStateInfo obj)
        {
            IDisposable animTimerDis = null;
            animTimerDis = Observable.Timer(TimeSpan.FromSeconds(obj.StateInfo.length)).Subscribe(_ =>
            {
                animTriggerDis.Dispose();
                animTimerDis.Dispose();
                lvupObjRect.gameObject.setActiveWhenChange(false);
            });
        }

        public override TopMiniPricePresenter bindingMiniPrice(GameObject miniPriceObj)
        {
            miniPriceObj.setActiveWhenChange(false);
            return UiManager.bindNode<PortraitTopMiniPricePresenter>(miniPriceObj);
        }
    }

    public class PortraitTopMiniPricePresenter : TopMiniPricePresenter
    {
        Animator closeAnim;
        IDisposable animTriggerDis;
        public override void initUIs()
        {
            base.initUIs();
            closeAnim = getAnimatorData("price_anim");
        }
        public override void movePrice()
        {
            Observable.TimerFrame(27).Subscribe(_ => { playCoinFly(); }).AddTo(uiGameObject);
        }

        public override void hidePrice()
        {
            var animTriggers = closeAnim.GetBehaviour<ObservableStateMachineTrigger>();
            animTriggerDis = animTriggers.OnStateEnterAsObservable().ObserveOnMainThread().Subscribe(onAniOut);
            closeAnim.SetTrigger("out");
        }

        private void onAniOut(ObservableStateMachineTrigger.OnStateInfo obj)
        {
            IDisposable animTimerDis = null;
            animTimerDis = Observable.Timer(TimeSpan.FromSeconds(obj.StateInfo.length)).Subscribe(_ =>
            {
                animTriggerDis.Dispose();
                animTimerDis.Dispose();
                close();
            });
        }
    }
}
