using UnityEngine;
using UnityEngine.UI;
using EventActivity;
using CommonPresenter;
using LobbyLogic.NetWork.ResponseStruct;
using CommonPresenter.PackItem;
using Lobby.Jigsaw;
using CommonILRuntime.Module;
using CommonILRuntime.BindingModule;
using UniRx;
using UniRx.Triggers;
using System;
using System.Collections.Generic;
using Services;
using CommonService;
using CommonILRuntime.Services;
using LobbyLogic.Audio;
using Lobby.Audio;

namespace MagicForest
{
    public class StageRewardPresenter : ContainerPresenter
    {
        public override string objPath => $"{ForestDataServices.prefabPath}/mf_stage_reward";
        public override UiLayer uiLayer { get => UiLayer.GameMessage; }

        RewardNodePresenter rewardPresnter;
        UrnNodePresenter urnPresenter;
        string packID;
        MagicForestStageReward rewards;
        public override void initContainerPresenter()
        {
            resOrder = new string[] { AssetBundleData.getBundleName(BundleType.MagicForest) };
            base.initContainerPresenter();
        }

        public override void initUIs()
        {
            rewardPresnter = UiManager.bindNode<RewardNodePresenter>(getNodeData("reward_node").cachedGameObject);
            urnPresenter = UiManager.bindNode<UrnNodePresenter>(getNodeData("urn_node").cachedGameObject);
        }

        public override void init()
        {
            rewardPresnter.close();
            urnPresenter.close();
            rewardPresnter.setFinishCB(clear);
            urnPresenter.setTapFinishCB(openStageReward);
        }

        public void openReward(string rewardPackID, MagicForestStageReward rewards)
        {
            ForestDataServices.outDoorClearSub.Subscribe(_ =>
            {
                clear();
            }).AddTo(uiGameObject);
            packID = rewardPackID;
            this.rewards = rewards;
            urnPresenter.open();
        }

        public void showNextDoorEvent(Action showNextDoor)
        {
            rewardPresnter.setShowNextDoor(showNextDoor);
        }

        void openStageReward()
        {
            rewardPresnter.openStageReward(packID, rewards);
        }
    }

    public class RewardNodePresenter : SystemUINodePresenter
    {
        Animator outAnim;
        Button collectBtn;
        RectTransform rewardGroup;
        Text rewardText;
        RectTransform rewardPackGroup;

        string packID;
        ulong coinAmount;
        Action finishCB;
        Action showNextDoor;
        public override void init()
        {
            base.init();
            collectBtn.onClick.AddListener(coinFly);
        }

        public override void initUIs()
        {
            outAnim = getAnimatorData("show_anim");
            collectBtn = getBtnData("collect_btn");
            rewardGroup = getRectData("reward_group");
            rewardText = getTextData("reward_text");
            rewardPackGroup = getRectData("reward_pack_group");
        }

        void coinFly()
        {
            collectBtn.interactable = false;
            AudioManager.instance.playAudioOnce(CommonAudioPathProvider.getAudioPath(BasicCommonSound.InfoBtn));
            var sourceVal = DataStore.getInstance.playerInfo.playerMoney;
            CoinFlyHelper.frontSFly(collectBtn.GetComponent<RectTransform>(), sourceVal, sourceVal + coinAmount, onComplete: closePresenter);
        }
        public void openStageReward(string rewardPackID, MagicForestStageReward rewards)
        {
            AudioManager.instance.playAudioOnce(AudioPathProvider.getAudioPath(ActivityBlastAudio.SmallWin));
            coinAmount = rewards.getCompleteReward;
            packID = rewardPackID;
            for (int i = 0; i < rewards.CompleteItem.Length; ++i)
            {
                var rewardData = rewards.CompleteItem[i];
                AwardKind awardKind = ActivityDataStore.getAwardKind(rewardData.Kind);
                switch (awardKind)
                {
                    case AwardKind.PuzzlePack:
                    case AwardKind.PuzzleVoucher:
                        PackItemPresenterServices.getSinglePackItem(rewardData.Type, rewardPackGroup);
                        break;

                }
            }
            rewardText.text = coinAmount.ToString("N0");
            LayoutRebuilder.ForceRebuildLayoutImmediate(rewardGroup);
            open();
        }
        public override void closePresenter()
        {
            base.closePresenter();
            if (null != showNextDoor)
            {
                showNextDoor();
            }
        }

        public void setFinishCB(Action finishEvent)
        {
            finishCB = finishEvent;
        }

        public void setShowNextDoor(Action showNextDoor)
        {
            this.showNextDoor = showNextDoor;
        }

        public override Animator getUiAnimator()
        {
            return outAnim;
        }

        public override void animOut()
        {
            if (!string.IsNullOrEmpty(packID))
            {
                OpenPackWildProcess.openPackWildFromID(packID, finishCB);
            }
            else if (null != finishCB)
            {
                finishCB();
            }
        }
    }

    public class UrnNodePresenter : NodePresenter
    {
        Button tapBtn;
        Animator tapAnim;
        List<IDisposable> animTriggerDis = new List<IDisposable>();
        Action tapFinishCB;
        public override void initUIs()
        {
            tapBtn = getBtnData("urn_btn");
            tapAnim = getAnimatorData("urn_anim");
        }

        public override void init()
        {
            tapBtn.onClick.AddListener(playTapAnim);
        }

        public void setTapFinishCB(Action action)
        {
            tapFinishCB = action;
        }

        void playTapAnim()
        {
            tapBtn.interactable = false;
            var animTriggers = tapAnim.GetBehaviour<ObservableStateMachineTrigger>();
            animTriggerDis.Add(animTriggers.OnStateEnterAsObservable().ObserveOnMainThread().Subscribe(onAniOut).AddTo(uiGameObject));
            tapAnim.SetTrigger("tap");
            AudioManager.instance.playAudioOnce(AudioPathProvider.getAudioPath(BonusAudio.Open));
        }

        private void onAniOut(ObservableStateMachineTrigger.OnStateInfo obj)
        {
            animTriggerDis.Add(Observable.Timer(TimeSpan.FromSeconds(obj.StateInfo.length), Scheduler.MainThreadIgnoreTimeScale).Subscribe(_ =>
            {
                UtilServices.disposeSubscribes(animTriggerDis.ToArray());
                close();
                if (null != tapFinishCB)
                {
                    tapFinishCB();
                }

            }).AddTo(uiGameObject));
        }
    }
}
