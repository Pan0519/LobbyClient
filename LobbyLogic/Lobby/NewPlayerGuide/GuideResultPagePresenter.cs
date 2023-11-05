using UnityEngine;
using UnityEngine.UI;
using System;
using UniRx;
using CommonPresenter;
using CommonService;
using CommonILRuntime.Services;
using CommonILRuntime.Module;

namespace NewPlayerGuide
{
    class GuideResultPagePresenter : SystemUIBasePresenter
    {
        public override string objPath { get { return "prefab/new_player_guide_result"; } }
        public override UiLayer uiLayer { get => UiLayer.System; }
        Animator uiAnimator;
        Button collectBtn;
        RectTransform groupRect;
        Text coinTxt;
        ulong rewardCoin;
        Action finishCB;

        bool isAllComplete;
        public override void initUIs()
        {
            uiAnimator = getAnimatorData("uiAnimator");
            collectBtn = getBtnData("collectButton");
            groupRect = getRectData("rewardGroupRect");
            coinTxt = getTextData("coinText");
        }

        public override void init()
        {
            coinTxt.text = "0";
            base.init();
            collectBtn.onClick.AddListener(coinFly);
        }

        public override Animator getUiAnimator()
        {
            return uiAnimator;
        }

        public async void openPage(string packID, bool isAllComplete)
        {
            collectBtn.interactable = true;
            rewardCoin = await GuideDataManager.getRewards(packID);
            coinTxt.text = rewardCoin.ToString("N0");
            open();
            string triggerName = isAllComplete ? "complete" : "normal";
            uiAnimator.SetTrigger($"{triggerName}_in");
            this.isAllComplete = isAllComplete;
            LayoutRebuilder.ForceRebuildLayoutImmediate(groupRect);
        }

        public void setFinishAction(Action finishEvent)
        {
            finishCB = finishEvent;
        }

        void coinFly()
        {
            collectBtn.interactable = false;
            var sourceValue = DataStore.getInstance.playerInfo.myWallet.coin;
            CoinFlyHelper.frontSFly(collectBtn.gameObject.GetComponent<RectTransform>(), sourceValue, sourceValue + rewardCoin, onComplete: () =>
            {
                DataStore.getInstance.playerInfo.myWallet.unsafeAdd(rewardCoin);
                closePresenter();
            });
        }

        public override void closePresenter()
        {
            base.closePresenter();
            if (isAllComplete)
            {
                Observable.TimerFrame(30).Subscribe(_ =>
                {
                    animOut();
                });
            }
        }

        public override void animOut()
        {
            if (null != finishCB)
            {
                finishCB();
            }
            clear();
        }
    }
}
