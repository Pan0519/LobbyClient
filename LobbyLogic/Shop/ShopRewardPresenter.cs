using CommonILRuntime.Module;
using Service;
using UnityEngine.UI;
using CommonPresenter;
using UnityEngine;
using Services;
using CommonService;
using CommonILRuntime.Outcome;
using CommonILRuntime.Services;

namespace Shop
{
    class ShopRewardPresenter : SystemUIBasePresenter
    {
        public override string objPath => "prefab/lobby_shop/gold_gift_box_result";
        public override UiLayer uiLayer { get => UiLayer.GameMessage; }

        #region UIs
        Button collectBtn;
        Text numTxt;
        Animator statusAnim;
        #endregion

        Outcome outcome;
        public override void initUIs()
        {
            collectBtn = getBtnData("collect_btn");
            numTxt = getTextData("reward_num");
            statusAnim = getAnimatorData("status_anim");
        }

        public override void init()
        {
            collectBtn.onClick.AddListener(collectClick);
        }

        public async void openReward(string rewardPackID)
        {
            if (string.IsNullOrEmpty(rewardPackID))
            {
                return;
            }

            var rewardPacks = await AppManager.lobbyServer.getRewardPacks(rewardPackID);
            outcome = Outcome.process(rewardPacks.rewards);
            ulong rewardNum = 0;
            for (int i = 0; i < rewardPacks.rewards.Length; ++i)
            {
                var reward = rewardPacks.rewards[i];
                if (reward.kind.Equals(UtilServices.outcomeCoinKey))
                {
                    rewardNum += reward.getAmount();
                }
            }
            numTxt.text = rewardNum.ToString("N0");
            LayoutRebuilder.ForceRebuildLayoutImmediate(numTxt.transform.parent.transform as RectTransform);
        }

        void collectClick()
        {
            CoinFlyHelper.frontSFly(collectBtn.GetComponent<RectTransform>(), DataStore.getInstance.playerInfo.myWallet.deprecatedCoin, DataStore.getInstance.playerInfo.myWallet.coin, onComplete: () =>
            {
                outcome.apply();
                getUiAnimator().SetTrigger("out");
            });
        }

        public override Animator getUiAnimator()
        {
            return statusAnim;
        }

        public override void animOut()
        {
            clear();
        }
    }
}
