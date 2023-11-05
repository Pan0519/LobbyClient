using CommonILRuntime.Module;
using CommonILRuntime.BindingModule;
using UnityEngine.UI;
using UnityEngine;
using CommonService;
using UniRx;
using Services;
using System;
using System.Threading.Tasks;
using Lobby;
using LoginReward;
using Service;

namespace NewPlayerGuide
{
    class XPartyPagePresenter : ContainerPresenter
    {
        public override string objPath => "Prefab/player_guide_xparty";
        public override UiLayer uiLayer { get => UiLayer.GameMessage; }
        Text msgTxtR;
        Text msgTxtL;
        Animator statusAnim;
        Button guideBtn;
        Button circleBtn;
        public override void initUIs()
        {
            statusAnim = getAnimatorData("guide_anim");
            msgTxtR = getTextData("guide_msg_content_r");
            msgTxtL = getTextData("guide_msg_content_l");
            guideBtn = getBtnData("guide_btn");
            circleBtn = getBtnData("circle_shadow_btn");
        }

        public override void init()
        {
            DataStore.getInstance.guideServices.nowStepSub.Subscribe(playAnim).AddTo(uiGameObject);
            guideBtn.onClick.AddListener(toNextStepClick);
            circleBtn.onClick.AddListener(toNextStepClick);
        }

        void toNextStepClick()
        {
            guideBtn.interactable = false;
            circleBtn.interactable = false;
            switch (DataStore.getInstance.guideServices.nowStatus)
            {
                case GuideStatus.Introduce:
                    DataStore.getInstance.guideServices.toNextStep();
                    break;
                case GuideStatus.Daily:
                    var dailyReward = LobbyStartPopSortManager.instance.dailyReward;
                    if (null == dailyReward || dailyReward.rewards.Length <= 0 || dailyReward.cumulativeDays <= 0)
                    {
                        LoginRewardServices.instance.showHistoryRewardBesideToDay();
                    }
                    else
                    {
                        LobbyStartPopSortManager.instance.showDailyReward();
                    }
                    close();
                    break;
                case GuideStatus.StayGame:
                    UiManager.getPresenter<StayMiniGame.StayMiniGameMainPresenter>().open();
                    close();
                    break;
                case GuideStatus.SaveDog:
                    openSaveTheDog();
                    break;
            }
        }

        async void openSaveTheDog()
        {
            DataStore.getInstance.guideServices.toNextStep();
            await Task.Delay(TimeSpan.FromSeconds(0.75f));
            TransitionxPartyServices.instance.openTransitionPage();
            var mapPresenter = UiManager.getPresenter<SaveTheDog.SaveTheDogMapPresenter>();
            mapPresenter.open();
        }

        public void openGuidePage(int nowStep)
        {
            DataStore.getInstance.guideServices.setNowStep(nowStep);
            playAnim(nowStep);
        }

        async void playAnim(int nowStep)
        {
            open();
            if ((GuideStatus)nowStep == GuideStatus.Completed)
            {
                statusAnim.SetTrigger("out");
                Observable.TimerFrame(40).Subscribe(_ => { clear(); }).AddTo(uiGameObject);
                return;
            }
      
            guideBtn.gameObject.setActiveWhenChange(nowStep == 1);
            guideBtn.interactable = nowStep == 1;
            circleBtn.interactable = nowStep > 1;
            circleBtn.gameObject.setActiveWhenChange(nowStep > 1);
            statusAnim.SetTrigger($"to_0{nowStep}");
            float waitTime = 0;
            switch (nowStep)
            {
                case 2:
                    waitTime = 0.8f;
                    break;
                case 3:
                case 4:
                    waitTime = 1.0f;
                    break;
            }
            await Task.Delay(TimeSpan.FromSeconds(waitTime));
            string content = LanguageService.instance.getLanguageValue($"Newplayerguide_Quest_{nowStep}");
            msgTxtR.text = content;
            msgTxtL.text = content;
        }
    }
}
