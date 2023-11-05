using CommonILRuntime.Module;
using UnityEngine.UI;
using UnityEngine;
using LobbyLogic.NetWork.ResponseStruct;
using UniRx;
using UniRx.Triggers;
using System;
using Lobby.Common;
using Services;
using EventActivity;
using CommonILRuntime.Services;
using CommonService;
using LobbyLogic.Audio;
using Lobby.Audio;
using CommonILRuntime.SpriteProvider;

namespace FrenzyJourney
{
    class BossRewardPresenter : ContainerPresenter
    {
        public override string objPath => FrenzyJourneyData.getInstance.getPrefabFullPath("fj_boss_end");
        public override UiLayer uiLayer { get => UiLayer.System; }

        Button collectBtn;
        Text rewardTxt;
        Animator endAnim;
        RectTransform moneyLayoutTrans;
        Image chestImg;

        Action outCB;
        IDisposable animTriggerDis;
        ulong coinAmount;
        public override void initContainerPresenter()
        {
            resOrder = new string[] { AssetBundleData.getBundleName(BundleType.FrenzyJourney) };
            base.initContainerPresenter();
        }
        public override void initUIs()
        {
            collectBtn = getBtnData("btn_collect");
            rewardTxt = getTextData("reward_coin");
            endAnim = getAnimatorData("end_anim");
            chestImg = getImageData("chest_img");
            moneyLayoutTrans = getBindingData<RectTransform>("money_layout");
        }
        public override void init()
        {
            var animTriggers = endAnim.GetBehaviour<ObservableStateMachineTrigger>();
            animTriggerDis = animTriggers.OnStateEnterAsObservable().ObserveOnMainThread().Subscribe(onAniOut);
            collectBtn.onClick.AddListener(playOut);
        }
        public void openRewardPage(BossData bossData, Action outCB)
        {
            AudioManager.instance.playAudioOnce(AudioPathProvider.getAudioPath(ActivityBlastAudio.SmallWin));
            this.outCB = outCB;
            coinAmount = bossData.getCompleteReward;
            rewardTxt.text = coinAmount.ToString("N0");
            var rewardResult = bossData.CompleteItem[0];
            TreasureBoxType treasureBoxType;
            if (!UtilServices.enumParse(rewardResult.Type, out treasureBoxType))
            {
                return;
            }
            chestImg.sprite = LobbySpriteProvider.instance.getSprite<EventActivitySpriteProvider>(LobbySpriteType.EventActivity, $"activity_treasure_lv{(int)treasureBoxType}");
            LayoutRebuilder.ForceRebuildLayoutImmediate(moneyLayoutTrans);
        }

        void playOut()
        {
            var sourceValue = DataStore.getInstance.playerInfo.playerMoney;
            CoinFlyHelper.frontSFly(collectBtn.GetComponent<RectTransform>(), sourceValue, sourceValue + coinAmount, onComplete: () =>
            {
                DataStore.getInstance.playerInfo.myWallet.unsafeAdd(coinAmount);
                endAnim.SetTrigger("out");
            });
        }

        private void onAniOut(ObservableStateMachineTrigger.OnStateInfo obj)
        {
            IDisposable animTimerDis = null;
            animTimerDis = Observable.Timer(TimeSpan.FromSeconds(obj.StateInfo.length)).Subscribe(_ =>
            {
                animTriggerDis.Dispose();
                animTimerDis.Dispose();
                if (null != outCB)
                {
                    outCB();
                }
                clear();
            });
        }

    }
}
