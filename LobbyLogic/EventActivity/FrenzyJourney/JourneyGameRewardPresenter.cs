using CommonILRuntime.Module;
using CommonILRuntime.BindingModule;
using Binding;
using UnityEngine.UI;
using UnityEngine;
using System;
using UniRx.Triggers;
using UniRx;
using EventActivity;
using LobbyLogic.NetWork.ResponseStruct;
using CommonPresenter.PackItem;
using CommonILRuntime.Services;
using CommonService;
using Lobby.Jigsaw;
using LobbyLogic.Audio;
using Lobby.Audio;
using CommonILRuntime.Outcome;
using Service;

namespace FrenzyJourney
{
    class JourneyGameRewardPresenter : ContainerPresenter
    {
        public override string objPath => FrenzyJourneyData.getInstance.getPrefabFullPath("fj_game_end");

        public override UiLayer uiLayer { get => UiLayer.System; }

        Button collectBtn;
        Text rewardTxt;
        Animator endAnim;
        RectTransform moneyLayoutTrans;
        BindingNode packNode;
        RectTransform packGroup;

        Action rewardFinishCB;
        IDisposable animTriggerDis;
        public override void initContainerPresenter()
        {
            resOrder = new string[] { AssetBundleData.getBundleName(BundleType.FrenzyJourney)};
            base.initContainerPresenter();
        }
        public override void initUIs()
        {
            endAnim = getAnimatorData("reward_anim");
            collectBtn = getBtnData("btn_collect");
            rewardTxt = getTextData("reward_txt");
            packNode = getNodeData("pack_node");
            packGroup = getBindingData<RectTransform>("pack_group");
            moneyLayoutTrans = getBindingData<RectTransform>("reward_layout");
        }

        public override void init()
        {
            packNode.cachedGameObject.setActiveWhenChange(false);
            var animTriggers = endAnim.GetBehaviour<ObservableStateMachineTrigger>();
            animTriggerDis = animTriggers.OnStateEnterAsObservable().ObserveOnMainThread().Subscribe(onAniOut);
            collectBtn.onClick.AddListener(playOut);
        }
        ulong coinAmount;
        string rewardPackID;
        Outcome rewardOutcome;
        public async void openRewardPage(string rewardPackID, ActivityReward[] stageReward, Action finishCB)
        {
            AudioManager.instance.playAudioOnce(AudioPathProvider.getAudioPath(ActivityBlastAudio.BigWin));
            FrenzyJourneyData.getInstance.stopAutoPlay();
            var rewardRedeem = await AppManager.lobbyServer.getRewardPacks(rewardPackID);
            rewardOutcome = Outcome.process(rewardRedeem.rewards);
            this.rewardPackID = rewardPackID;
            rewardFinishCB = finishCB;
            coinAmount = 0;
            for (int i = 0; i < stageReward.Length; ++i)
            {
                var rewardData = stageReward[i];
                AwardKind awardKind = ActivityDataStore.getAwardKind(rewardData.Kind);
                switch (awardKind)
                {
                    case AwardKind.Coin:
                        coinAmount += rewardData.getAmount;
                        break;

                    case AwardKind.PuzzlePack:
                    case AwardKind.PuzzleVoucher:
                        var packObj = ResourceManager.instance.getObjectFromPool(packNode.cachedGameObject, packGroup);
                        PackNode packNodePresenter = UiManager.bindNode<PackNode>(packObj.cachedGameObject);
                        long packID;
                        if (!long.TryParse(rewardData.Type, out packID))
                        {
                            Debug.LogError($"Parse {packID} Error");
                            continue;
                        }
                        packNodePresenter.showPackItem(packID, (long)rewardData.Amount);
                        break;

                }
            }
            rewardTxt.text = coinAmount.ToString("N0");
            LayoutRebuilder.ForceRebuildLayoutImmediate(moneyLayoutTrans);
        }

        void playOut()
        {
            CoinFlyHelper.frontSFly(collectBtn.GetComponent<RectTransform>(), DataStore.getInstance.playerInfo.myWallet.deprecatedCoin, DataStore.getInstance.playerInfo.myWallet.coin, onComplete: () =>
            {
                rewardOutcome.apply();
                endAnim.SetTrigger("out");
            });
        }

        private void onAniOut(ObservableStateMachineTrigger.OnStateInfo obj)
        {
            IDisposable animTimerDis = null;
            animTimerDis = Observable.Timer(TimeSpan.FromSeconds(obj.StateInfo.length)).Subscribe(_ =>
            {
                animOutFinish();
                animTimerDis.Dispose();
                clear();
            }).AddTo(uiGameObject);
        }

        void animOutFinish()
        {
            if (!string.IsNullOrEmpty(rewardPackID))
            {
                OpenPackWildProcess.openPackWildFromID(rewardPackID, rewardFinishCB);
                return;
            }

            if (rewardFinishCB != null)
            {
                rewardFinishCB();
            }
        }

        public override void clear()
        {
            ResourceManager.instance.releasePoolWithObj(packNode.cachedGameObject);
            animTriggerDis.Dispose();
            base.clear();
        }
    }

    public class PackNode : NodePresenter
    {
        Text amountTxt;
        public override void initUIs()
        {
            amountTxt = getTextData("amount_txt");
        }

        public void showPackItem(long packID, long award)
        {
            PackItemPresenterServices.getSinglePackItem(packID, uiRectTransform);
            amountTxt.text = award.ToString("N0");
            open();
        }
    }
}
