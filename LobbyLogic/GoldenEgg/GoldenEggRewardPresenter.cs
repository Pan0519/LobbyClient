using UnityEngine;
using UnityEngine.UI;
using CommonILRuntime.Module;
using CommonILRuntime.BindingModule;
using CommonILRuntime.Outcome;
using UniRx.Triggers;
using UniRx;
using System;
using System.Collections.Generic;
using Shop;
using Services;
using CommonService;
using CommonILRuntime.Services;
using LobbyLogic.Audio;
using Lobby.Audio;
using Lobby.Jigsaw;

namespace GoldenEgg
{
    class GoldenEggRewardPresenter : ContainerPresenter
    {
        public override string objPath
        {
            get
            {
                return UtilServices.getOrientationObjPath("prefab/golden_egg/golden_egg_reward");
            }
        }

        public override UiLayer uiLayer { get => UiLayer.GameMessage; }

        #region UIs
        Animator rewardAnim;
        Text goldNumTxt;
        ScrollRect rewardScroll;
        Button okBtn;
        #endregion

        IDisposable animTimerDis;
        List<PoolObject> purchasePools = new List<PoolObject>();
        ulong coinAmounts;
        CommonReward[] rewards;
        public override void initUIs()
        {
            rewardAnim = getAnimatorData("reward_anim");
            goldNumTxt = getTextData("gold_num_txt");
            rewardScroll = getBindingData<ScrollRect>("reward_scroll");
            okBtn = getBtnData("ok_btn");
        }

        public override void init()
        {
            var animTrigger = rewardAnim.GetBehaviour<ObservableStateMachineTrigger>();
            animTrigger.OnStateEnterAsObservable().ObserveOnMainThread().Subscribe(animEnterSubscribe);
            okBtn.onClick.AddListener(okClick);
        }

        public void setRewardItems(ItemProductData productData)
        {
            coinAmounts = productData.eggData.getAmount;
            rewards = productData.commonRewards;
            var purchaseInfos = PurchaseInfoCover.rewardConvertToPurchase(productData.serverProductData.rewards);
            for (int i = 0; i < purchaseInfos.Count; ++i)
            {
                var info = purchaseInfos[i];
                var item = ResourceManager.instance.getObjectFromPool("prefab/lobby_shop/purchase_item", rewardScroll.content.transform);
                UiManager.bindNode<PurchaseItemNode>(item.cachedGameObject).showItem(info);
                purchasePools.Add(item);
            }
            goldNumTxt.text = coinAmounts.ToString("N0");
            okBtn.interactable = true;
            open();
            AudioManager.instance.playAudioOnce(AudioPathProvider.getAudioPath(ActivityBlastAudio.SmallWin));
        }

        private void animEnterSubscribe(ObservableStateMachineTrigger.OnStateInfo obj)
        {
            animTimerDis = Observable.Timer(TimeSpan.FromSeconds(obj.StateInfo.length)).Subscribe(_ =>
            {
                clear();
            });
        }

        public override void clear()
        {
            for (int i = 0; i < purchasePools.Count; ++i)
            {
                ResourceManager.instance.returnObjectToPool(purchasePools[i].cachedGameObject);
            }
            UtilServices.disposeSubscribes(animTimerDis);

            if (!DataStore.getInstance.playerInfo.isBindFB)
            {
                OpenMsgBoxService.Instance.openNormalBox(title: LanguageService.instance.getLanguageValue("Tips_BindAccount"),
                    content: LanguageService.instance.getLanguageValue("Tips_AccountLost"));
            }

            base.clear();
        }

        void okClick()
        {
            okBtn.interactable = false;
            if (coinAmounts > 0)
            {
                var sourceValue = DataStore.getInstance.playerInfo.playerMoney;
                CoinFlyHelper.frontSFly(okBtn.GetComponent<RectTransform>(), sourceValue, sourceValue + coinAmounts, onComplete: coinFlyFinish);
                return;
            }
            playOut();
        }

        void coinFlyFinish()
        {
            IDisposable openPackDis = null;
            openPackDis = Observable.Timer(TimeSpan.FromSeconds(0.5f)).Subscribe(_ =>
             {
                 OpenPackWildProcess.openPackWild(rewards, playOut);
                 openPackDis.Dispose();
             }).AddTo(uiGameObject);
        }

        void playOut()
        {
            var outcome = Outcome.process(rewards);
            outcome.apply();
            rewardAnim.SetTrigger("reward_out");
        }
    }
}
