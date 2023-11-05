using CommonILRuntime.Module;
using UnityEngine;
using CommonILRuntime.BindingModule;
using Game.Common;
using UniRx;
using UniRx.Triggers;
using LobbyLogic.NetWork.ResponseStruct;
using System;
using Services;
using Lobby.Audio;
using LobbyLogic.Audio;

namespace FrenzyJourney
{
    class BossPresenter : ContainerPresenter
    {
        public override string objPath => FrenzyJourneyData.getInstance.getPrefabFullPath("fj_boss_scene");
        public override UiLayer uiLayer { get => UiLayer.GameMessage; }

        public Action bossFinishCB;
        public Action checkDiceTypeEvent;

        PoolObject bomb;

        GameObject hitBoss;
        GameObject hitSelf;
        RectTransform bossGroupTrans;
        Animator hitBossAnim;
        Animator hitSelfAnim;
        Animator attackAnim;
        Animator bombAnim;

        Animator bossShakeAnim;
        Animator bossGroupAnim;
        Animator bossSceneAnim;

        BezierPresenter bombBezierPresenter;
        Vector2 bombPos;

        BossData bossData;

        bool isLvup;
        string bezierTweenID;
        public override void initContainerPresenter()
        {
            resOrder = new string[] { AssetBundleData.getBundleName(BundleType.FrenzyJourney) };
            base.initContainerPresenter();
        }
        public override void initUIs()
        {
            hitBoss = getGameObjectData("hit_boss");
            hitSelf = getGameObjectData("hit_self");
            bossGroupTrans = getBindingData<RectTransform>("boss_group_trans");

            hitBossAnim = getAnimatorData("hit_boss_anim");
            hitSelfAnim = getAnimatorData("hit_self_anim");
            attackAnim = getAnimatorData("attack_anim");
            bossGroupAnim = getAnimatorData("boss_group_ani");
            bossSceneAnim = getAnimatorData("boss_ani");
            bossShakeAnim = getAnimatorData("boss_shake_anim");
        }

        public override void init()
        {
            hitBoss.setActiveWhenChange(false);
            hitSelf.setActiveWhenChange(false);

            var animTrigger = bossGroupAnim.GetBehaviours<ObservableStateMachineTrigger>();
            for (int i = 0; i < animTrigger.Length; ++i)
            {
                animTrigger[i].OnStateEnterAsObservable().ObserveOnMainThread().Subscribe(bossGroupOutAnimTrigger).AddTo(uiGameObject); ;
            }

            initBomb();
        }

        public override void open()
        {
            base.open();
            IDisposable bossIn = null;
            bossIn = Observable.EveryUpdate().Subscribe(_ =>
             {
                 var animatorInfo = bossGroupAnim.GetCurrentAnimatorStateInfo(0);
                 if (animatorInfo.IsName("boss_appear_in"))
                 {
                     AudioManager.instance.playAudioOnce(AudioPathProvider.getAudioPath(ActivityFJAudio.MonstoreMove));
                     Observable.Timer(TimeSpan.FromSeconds(1.0f)).Subscribe(time =>
                     {
                         FrenzyJourneyData.getInstance.showRunning(false, "BossOpen");
                     }).AddTo(uiGameObject);
                     bossIn.Dispose();
                 }
             }).AddTo(uiGameObject);
        }

        void initBomb()
        {
            bomb = ResourceManager.instance.getObjectFromPoolWithResOrder(FrenzyJourneyData.getInstance.getPrefabFullPath("fj_attack_bomb"), bossGroupTrans, resNames: resOrder);
            bombAnim = bomb.GetComponent<Animator>();
            float bombPosY = (bossGroupTrans.sizeDelta.y / 2) + bomb.cachedRectTransform.sizeDelta.y;
            float bombPosX = (bossGroupTrans.sizeDelta.x / 2) - (bomb.cachedRectTransform.sizeDelta.x / 2);
            bombPos = new Vector2(bombPosX, bombPosY * -1);
            bomb.cachedRectTransform.anchoredPosition = bombPos;
            bombBezierPresenter = UiManager.bind<BezierPresenter>(bomb.cachedGameObject);
            //bombBezierPresenter.isDrawLine = true;

            Vector2 middlePos = new Vector2(bomb.transform.position.x - (bomb.transform.position.x / 6), bossGroupTrans.position.y + 3.0f);
            Vector2 middleShiftPos = BezierUtils.getNormalDirShiftPoint(bomb.transform.position, middlePos, shiftQuant: -0.2f);

            Vector2 endPos = new Vector2(bossGroupTrans.position.x + 1.0f, bossGroupTrans.position.y);
            Vector2 secondShiftPos = BezierUtils.getNormalDirShiftPoint(middlePos, endPos, -0.8f);
            secondShiftPos.Set(secondShiftPos.x, secondShiftPos.y - 2);

            bombBezierAddPos(bombBezierPresenter.uiTransform.position);
            bombBezierAddPos(middleShiftPos);
            bombBezierAddPos(middlePos);
            bombBezierAddPos(secondShiftPos);
            bombBezierAddPos(endPos);
        }

        void bombBezierAddPos(Vector2 bezierPos)
        {
            bombBezierPresenter.bezierPoints.Add(bezierPos);
        }

        public void openBomb(BossData bossData, bool isLvup, Action bombPlayCB)
        {
            this.isLvup = isLvup;
            this.bossData = bossData;
            AudioManager.instance.playAudioOnce(AudioPathProvider.getAudioPath(ActivityFJAudio.MonsterFight1));
            bombBezierPresenter.moveBezierLine(0.5f, () =>
                 {
                     hitBoss.setActiveWhenChange(true);
                     bombBezierPresenter.uiRectTransform.anchoredPosition = bombPos;
                     setHitAnimFinishCB(hitBossAnim, hitAnimFinish, () =>
                     {
                         bossShakeAnim.SetTrigger("hit");
                     });
                     if (null != bombPlayCB)
                     {
                         bombPlayCB();
                     }
                 });
            bombAnim.SetTrigger("throw");
            bezierTweenID = bombBezierPresenter.bezierTweenID;
            TweenManager.tweenPlayByID(bezierTweenID);
        }

        void hitAnimFinish()
        {
            hitBoss.setActiveWhenChange(false);

            if (isLvup)
            {
                bossGroupAnim.SetTrigger("out");
                AudioManager.instance.playAudioOnce(AudioPathProvider.getAudioPath(ActivityFJAudio.MonstoreMove));
                Observable.TimerFrame(150).Subscribe(_ =>
                {
                    AudioManager.instance.playAudioOnce(AudioPathProvider.getAudioPath(BonusAudio.Fall));
                }).AddTo(uiGameObject);
                return;
            }
            checkDiceTypeEvent();
            FrenzyJourneyData.getInstance.showRunning(false, "hitAnimFinish");
        }

        void setHitAnimFinishCB(Animator hitAnim, Action finishCB, Action shakeEvent, float addTime = 0.1f)
        {
            if (null == finishCB)
            {
                return;
            }
            IDisposable timerDis = null;
            IDisposable getHitAnimDis = null;
            getHitAnimDis = Observable.EveryUpdate().Subscribe(_ =>
              {
                  AnimatorStateInfo playAnimInfo = hitAnim.GetCurrentAnimatorStateInfo(0);
                  if (playAnimInfo.IsName("hit_effect"))
                  {
                      if (null != shakeEvent)
                      {
                          shakeEvent();
                      }
                      getHitAnimDis.Dispose();
                      timerDis = Observable.Timer(TimeSpan.FromSeconds(playAnimInfo.length + addTime)).Subscribe(time =>
                         {
                             finishCB();
                             timerDis.Dispose();
                         }).AddTo(uiGameObject); ;
                  }
              }).AddTo(uiGameObject);
        }

        void bossGroupOutAnimTrigger(ObservableStateMachineTrigger.OnStateInfo obj)
        {
            IDisposable animTimerDis = null;
            animTimerDis = Observable.Timer(TimeSpan.FromSeconds(obj.StateInfo.length)).Subscribe(_ =>
            {
                UiManager.getPresenter<BossRewardPresenter>().openRewardPage(bossData, closeRewardCB);
                bossGroupTrans.gameObject.setActiveWhenChange(false);
                animTimerDis.Dispose();
            }).AddTo(uiGameObject); ;
        }

        void bossSceneOutAnimTrigger(ObservableStateMachineTrigger.OnStateInfo obj)
        {
            IDisposable animTimerDis = null;
            animTimerDis = Observable.Timer(TimeSpan.FromSeconds(obj.StateInfo.length)).Subscribe(_ =>
            {
                clear();
                animTimerDis.Dispose();
                FrenzyJourneyData.getInstance.showRunning(false, "bossSceneOutAnimTrigger");
            }).AddTo(uiGameObject); ;
        }

        void closeRewardCB()
        {
            if (null != bossFinishCB)
            {
                bossFinishCB();
            }

            var animTrigger = bossSceneAnim.GetBehaviour<ObservableStateMachineTrigger>();
            animTrigger.OnStateEnterAsObservable().ObserveOnMainThread().Subscribe(bossSceneOutAnimTrigger).AddTo(uiGameObject); ;
            bossSceneAnim.SetTrigger("out");
        }

        public override void clear()
        {
            TweenManager.tweenKill(bezierTweenID);
            base.clear();
        }
    }
}
