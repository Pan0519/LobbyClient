using UnityEngine;
using UnityEngine.UI;
using CommonILRuntime.Module;
using CommonILRuntime.BindingModule;
using System.Collections.Generic;
using UniRx;
using CommonService;
using LobbyLogic.NetWork.ResponseStruct;
using Service;
using Services;
using Network;
using System;
using System.Threading.Tasks;
using System.Linq;
using Lobby;
using DG.Tweening;
using CommonILRuntime.Services;

namespace NewPlayerGuide
{
    class GuidePagePresenter : ContainerPresenter
    {
        public override string objPath => "prefab/new_player_guide";
        public override UiLayer uiLayer { get => UiLayer.GameMessage; }

        List<GuideStepBaseNode> guidePresenters = new List<GuideStepBaseNode>();

        GameObject bgObj;
        RectTransform dealerRoot;
        Image bgShadow;
        GuideStepBaseNode nowOpenNode;
        public override void initUIs()
        {
            bgObj = getGameObjectData("bg_obj");
            dealerRoot = getRectData("dealer_root");
            bgShadow = bgObj.GetComponent<Image>();
            //getNodeData("step_1_node").cachedGameObject.setActiveWhenChange(false);
            //getNodeData("step_2_node").cachedGameObject.setActiveWhenChange(false);

            setGuidePresenter(UiManager.bindNode<GuideStepThreeNode>(getNodeData("step_3_node").cachedGameObject));
            setGuidePresenter(UiManager.bindNode<GuideStepFourNode>(getNodeData("step_4_node").cachedGameObject));
        }

        public override void init()
        {
            DataStore.getInstance.guideServices.nowGameStep.Subscribe(openGuidePage).AddTo(uiGameObject);
            GuideDataManager.setBGEnableSub.Subscribe(setBGObjActive).AddTo(uiGameObject);
            GuideDataManager.fadeBGOutSub.Subscribe(_ =>
            {
                fadeOutBGShadow();
            }).AddTo(uiGameObject);
            resetBGShadow();
        }

        void setBGObjActive(bool active)
        {
            bgObj.setActiveWhenChange(active);
        }

        void setGuidePresenter(GuideStepBaseNode guidePresenter)
        {
            guidePresenters.Add(guidePresenter);
        }

        public void openGuidePage(int nowStep = 0)
        {
            if ((GameGuideStatus)nowStep == GameGuideStatus.Completed)
            {
                clear();
                return;
            }
            nowOpenNode = null;
            resetBGShadow();
            setBGObjActive(true);
            for (int i = 0; i < guidePresenters.Count; ++i)
            {
                var presenter = guidePresenters[i];
                if (i != nowStep)
                {
                    presenter.close();
                    continue;
                }
                presenter.open();
                nowOpenNode = presenter;
                fadeInBGShadow(presenter.startOpenPage);
            }
        }

        void resetBGShadow()
        {
            Color bgShadowColor = bgShadow.color;
            bgShadowColor.a = 0;
            bgShadow.color = bgShadowColor;
        }

        void fadeInBGShadow(Action finishCB)
        {
            Color bgShadowColor = bgShadow.color;
            TweenManager.tweenToFloat(0, 1, 0.25f, onUpdate: alpha =>
            {
                bgShadowColor.a = alpha;
                bgShadow.color = bgShadowColor;
            }, onComplete: () =>
            {
                moveDealer(nowOpenNode.dealerMoveData);
                if (null != finishCB)
                {
                    finishCB();
                }
            });
        }

        public void fadeOutBGShadow()
        {
            Color bgShadowColor = bgShadow.color;
            TweenManager.tweenToFloat(1, 0, 0.25f, onUpdate: alpha =>
              {
                  bgShadowColor.a = alpha;
                  bgShadow.color = bgShadowColor;
              });
        }

        void moveDealer(MoveDealerData dealerMoveData)
        {
            if (null == dealerMoveData)
            {
                return;
            }
            dealerRoot.anchPosMoveX(dealerMoveData.endX, dealerMoveData.moveTime, onComplete: dealerMoveData.complete, easeType: dealerMoveData.easeType);
        }
    }

    class GuideStepBaseNode : NodePresenter
    {
        public virtual MoveDealerData dealerMoveData { get; set; } = null;
        //public Action fadeInBGShadowCB  = startOpenPage
        GameObject msgRootObj;
        GameObject fingerRootObj;

        public override void initUIs()
        {
            msgRootObj = getGameObjectData("msg_root");
            fingerRootObj = getGameObjectData("finger_root");
        }

        public void setMsgRootActive(bool active)
        {
            msgRootObj.setActiveWhenChange(active);
        }

        public void setFingerActive(bool active)
        {
            fingerRootObj.setActiveWhenChange(active);
        }

        public void setMsgPosY(float endY)
        {
            var msgRootRect = msgRootObj.GetComponent<RectTransform>();
            var originalPos = msgRootRect.anchoredPosition3D;
            originalPos.Set(originalPos.x, endY, 0);
            msgRootRect.anchoredPosition3D = originalPos;
        }

        public virtual void startOpenPage() { }
    }

    class GuideStepThreeNode : GuideMissionNode
    {
        public Action spinEvent = null;
        bool btnEnable;
        bool rewardMsgOpen;
        bool isRunComplete { get { return nowProgress >= 2; } }
        List<IDisposable> gameNoticesDis = new List<IDisposable>();
        int clickProgress = 0;
        public override void init()
        {
            base.init();
            setMsgRootActive(false);
            setSchedualAnimActive(false);
            setSpinBtnActive(false);
        }

        public override void open()
        {
            nowProgress = DataStore.getInstance.guideServices.getSaveSpinCount();
            GuideDataManager.setBGEnable(true);
            DataStore.getInstance.guideServices.setGameBtnsGroupActive(false);
            gameNoticesDis.Add(DataStore.getInstance.guideServices.playBtnEnableSubject.Subscribe(spinBtnEnable).AddTo(uiGameObject));
            gameNoticesDis.Add(DataStore.getInstance.guideServices.noticeWinWindowsStateSub.Subscribe(isRewardMsgOpen).AddTo(uiGameObject));

            disableBetBtns();
            base.open();
        }

        public override async void startOpenPage()
        {
            clickProgress = 0;
            await Task.Delay(TimeSpan.FromSeconds(0.4f));
            setSpinBtnActive(true);
            setMsgRootActive(true);
            await Task.Delay(TimeSpan.FromSeconds(0.4f));
            setFingerActive(true);
        }

        public override void spinClick()
        {
            spinBtnEnable(false);
            DataStore.getInstance.guideServices.guideSpinClick();
            nowProgress++;
            clickProgress++;
            DataStore.getInstance.guideServices.saveSpinCount((int)nowProgress);
            if (clickProgress == 1)
            {
                stepOut();
                Observable.TimerFrame(15).Subscribe(_ =>
                {
                    GuideDataManager.fadeOutBG();
                }).AddTo(uiGameObject);
            }
        }

        void spinBtnEnable(bool enable)
        {
            btnEnable = enable;

            disableBetBtns();
            if (enable && isRunComplete && !rewardMsgOpen)
            {
                DataStore.getInstance.guideServices.gameToNextStep();
                return;
            }
            setSpinBtnInteractable(enable);
        }

        void isRewardMsgOpen(bool isOpen)
        {
            rewardMsgOpen = isOpen;
        }

        void disableBetBtns()
        {
            DataStore.getInstance.guideServices.setBetBtnsEnable(false);
        }

        public override async void close()
        {
            UtilServices.disposeSubscribes(gameNoticesDis);
            schedualAnimTriggerOut();
            await Task.Delay(TimeSpan.FromSeconds(0.5f));
            base.close();
        }
    }
    class GuideStepFourNode : GuideMissionNode
    {
        Button maxBtn;
        List<IDisposable> guideDis = new List<IDisposable>();

        public override void initUIs()
        {
            base.initUIs();
            maxBtn = getBtnData("maxbet_btn");
        }

        public override void init()
        {
            base.init();
            setSchedualAnimActive(false);
            setFingerActive(false);
            setMsgRootActive(false);
            maxBtn.gameObject.setActiveWhenChange(false);
            maxBtn.onClick.AddListener(maxBetClick);
        }

        public override void open()
        {
            setSpinBtnInteractable(false);
            DataStore.getInstance.guideServices.setGameBtnsGroupActive(false);
            guideDis.Add(DataStore.getInstance.guideServices.gameMaxBetEnableSub.Subscribe(setMaxBetEnable).AddTo(uiGameObject));
            base.open();
            GuideDataManager.setBGEnable(true);
        }

        public override async void startOpenPage()
        {
            await Task.Delay(TimeSpan.FromSeconds(0.4f));
            maxBtn.gameObject.setActiveWhenChange(true);
            setMsgRootActive(true);
            await Task.Delay(TimeSpan.FromSeconds(0.6f));
            setFingerActive(true);
        }

        void setMaxBetEnable(bool enable)
        {
            maxBtn.interactable = enable;
        }

        void maxBetClick()
        {
            DataStore.getInstance.guideServices.guideMaxBetClick();
            stepOut();
            Observable.TimerFrame(30).Subscribe(_ =>
            {
                DataStore.getInstance.guideServices.gameToNextStep();
            });
        }

        public override void close()
        {
            UtilServices.disposeSubscribes(guideDis);
            base.close();
        }
    }

    class GuideMissionNode : GuideStepBaseNode
    {
        CustomBtn spinBtn;
        Image schedualImg;
        Text schedualTxt;

        Animator schedualAnim;
        GameObject completeEffectObj;
        Animator stepAnim;

        public string rewardPackID;
        public float maxCount;
        public float nowProgress;

        public override void initUIs()
        {
            base.initUIs();
            schedualImg = getImageData("schedual_img");
            schedualTxt = getTextData("schedual_txt");
            schedualAnim = getAnimatorData("schedual_anim");
            completeEffectObj = getGameObjectData("complete_obj");
            spinBtn = getBindingData<CustomBtn>("spin_btn");
            stepAnim = uiGameObject.GetComponent<Animator>();
        }

        public override void init()
        {
            base.init();
            spinBtn.clickHandler = spinClick;
        }

        public void stepOut()
        {
            stepAnim.SetTrigger("out");
        }

        public override void open()
        {
            completeEffectObj.setActiveWhenChange(false);
            schedualImg.fillAmount = 0;
            schedualTxt.text = "0%";
            base.open();
        }

        public virtual void spinClick()
        {

        }

        public void setSpinBtnActive(bool active)
        {
            spinBtn.gameObject.setActiveWhenChange(active);
        }

        public void setSpinBtnInteractable(bool enable)
        {
            spinBtn.interactable = enable;
        }

        public void setSchedualAnimActive(bool active)
        {
            schedualAnim.gameObject.setActiveWhenChange(active);
        }

        public void schedualAnimTriggerOut()
        {
            schedualAnim.SetTrigger("out");
        }

        //public void showRewardPage(bool isAllComplete)
        //{
        //    var resultPage = UiManager.getPresenter<GuideResultPagePresenter>();
        //    resultPage.close();
        //    resultPage.openPage(rewardPackID, isAllComplete);
        //    resultPage.setFinishAction(rewardFinishEvent);
        //}

        //public void rewardFinishEvent()
        //{
        //    DataStore.getInstance.guideServices.toNextStep();
        //}
    }

    class MoveDealerData
    {
        public float endX;
        public float moveTime;
        public Action complete;
        public Ease easeType;
    }
}
