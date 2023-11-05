using CommonILRuntime.Module;
using UnityEngine.UI;
using UnityEngine;
using UniRx;
using UniRx.Triggers;
using System;
using System.Collections.Generic;
using Services;
using CommonService;
using CommonILRuntime.Services;
using LobbyLogic.Audio;
using Lobby.Audio;

namespace FrenzyJourney
{
    class NormalRewardPresenter : ContainerPresenter
    {
        public override string objPath => FrenzyJourneyData.getInstance.getPrefabFullPath("fj_prize");

        public override UiLayer uiLayer { get => UiLayer.System; }

        Animator animShow;
        RectTransform layoutRect;
        Text coinTxt;
        Text diceCountTxt;
        Button collectBtn;

        List<IDisposable> animTriggerDis = new List<IDisposable>();
        Action endCB;
        ulong coinReward;
        public override void initContainerPresenter()
        {
            resOrder = new string[] { AssetBundleData.getBundleName(BundleType.FrenzyJourney) };
            base.initContainerPresenter();
        }
        public override void initUIs()
        {
            animShow = getAnimatorData("anim_show");
            layoutRect = getBindingData<RectTransform>("layout_trans");
            coinTxt = getTextData("coin_txt");
            collectBtn = getBtnData("collect_btn");
            diceCountTxt = getTextData("dice_num_txt");
        }

        public override void init()
        {
            collectBtn.onClick.AddListener(playOut);
        }

        public override void open()
        {
            collectBtn.interactable = true;
            coinReward = 0;
            AudioManager.instance.playAudioOnce(AudioPathProvider.getAudioPath(ActivityBlastAudio.SmallWin));
            base.open();
        }

        public void openForDice(long reward, Action endCB)
        {
            open();
            diceCountTxt.text = $"x {reward}";
            animShow.SetTrigger("chest_prize");
            this.endCB = endCB;
        }

        public void openForCoin(ulong reward, Action endCB)
        {
            open();
            coinReward = reward;
            coinTxt.text = reward.ToString("N0");
            LayoutRebuilder.ForceRebuildLayoutImmediate(layoutRect);
            animShow.SetTrigger("coin");
            this.endCB = endCB;
        }

        void playOut()
        {
            collectBtn.interactable = false;
            var animTriggers = animShow.GetBehaviours<ObservableStateMachineTrigger>();
            for (int i = 0; i < animTriggers.Length; ++i)
            {
                animTriggerDis.Add(animTriggers[i].OnStateEnterAsObservable().ObserveOnMainThread().Subscribe(onAniOut).AddTo(uiGameObject));
            }

            if (coinReward > 0)
            {
                var sourceValue = DataStore.getInstance.playerInfo.myWallet.deprecatedCoin;
                CoinFlyHelper.frontSFly(collectBtn.GetComponent<RectTransform>(), sourceValue, sourceValue + coinReward, onComplete: () =>
                {
                    animShow.SetTrigger("out");
                });
                return;
            }
            animShow.SetTrigger("out");
        }
        private void onAniOut(ObservableStateMachineTrigger.OnStateInfo obj)
        {
            IDisposable animTimerDis = null;
            animTimerDis = Observable.Timer(TimeSpan.FromSeconds(obj.StateInfo.length)).Subscribe(_ =>
            {
                if (null != endCB)
                {
                    endCB();
                }
                animTimerDis.Dispose();
                UtilServices.disposeSubscribes(animTriggerDis.ToArray());
                clear();
            });
        }
    }
}
