using Service;
using Lobby.Common;
using CommonILRuntime.BindingModule;
using EventActivity;
using CommonILRuntime.Outcome;
using CommonPresenter;
using UnityEngine;
using UnityEngine.UI;
using CommonService;
using CommonILRuntime.Services;
using Lobby.Jigsaw;
using System;
using CommonILRuntime.Module;
using LobbyLogic.Audio;
using Lobby.Audio;

namespace SaveTheDog
{
    class SaveTheDogGiftRewardPresenter : SystemUIBasePresenter
    {
        public override string objPath => "prefab/save_the_dog/save_the_dog_gift_board";
        public override UiLayer uiLayer { get => UiLayer.System; }

        RectTransform rewardLayout;
        Animator showAnim;
        Button collectBtn;

        #region Prefab Path
        private readonly string REWARD_ITEM_PACK = "prefab/reward_item/reward_item_pack";
        private readonly string REWARD_ITEM = "prefab/reward_item/reward_item";
        #endregion

        Outcome outcome;
        bool havePuzzle;
        bool haveCoin;
        CommonReward[] rewards;
        Action closeCB;

        public override void initContainerPresenter()
        {
            resOrder = new string[] { AssetBundleData.getBundleName(BundleType.SaveTheDog) };
            base.initContainerPresenter();
        }

        public override void initUIs()
        {
            showAnim = getAnimatorData("board_ani");
            rewardLayout = getRectData("reward_layout_rect");
            collectBtn = getBtnData("collect_btn");
        }

        public override void init()
        {
            base.init();
            collectBtn.onClick.AddListener(collectClick);
        }

        void collectClick()
        {
            collectBtn.interactable = false;
            if (haveCoin)
            {
                var playerWallet = DataStore.getInstance.playerInfo.myWallet;
                CoinFlyHelper.frontSFly(collectBtn.GetComponent<RectTransform>(), playerWallet.deprecatedCoin, playerWallet.coin, onComplete: coinFlyComplete);
                return;
            }

            coinFlyComplete();
        }

        void coinFlyComplete()
        {
            outcome.apply();
            if (havePuzzle)
            {
                OpenPackWildProcess.openPackWild(rewards, closePresenter);
                return;
            }

            closePresenter();
        }

        public override Animator getUiAnimator()
        {
            return showAnim;
        }

        public override void animOut()
        {
            if (null != closeCB)
            {
                closeCB();
            }
            clear();
        }

        public void openRewardPage(CommonReward[] rewards, Action animOutCB)
        {
            closeCB = animOutCB;
            this.rewards = rewards;
            addRewardObj();
        }

        private void addRewardObj()
        {
            havePuzzle = false;
            haveCoin = false;
            outcome = Outcome.process(rewards);
            for (var i = 0; i < rewards.Length; i++)
            {
                PoolObject rewardObj;
                var reward = rewards[i];
                var rewardKind = ActivityDataStore.getAwardKind(reward.kind);
                switch (rewardKind)
                {
                    case AwardKind.PuzzlePack:
                    case AwardKind.PuzzleVoucher:
                        rewardObj = ResourceManager.instance.getObjectFromPool(REWARD_ITEM_PACK, rewardLayout);
                        var packPresenter = UiManager.bindNode<RewardPackItemNode>(rewardObj.gameObject);
                        packPresenter.setPuzzlePack(reward.type);
                        havePuzzle = true;
                        break;

                    case AwardKind.Coin:
                    case AwardKind.Ticket:
                        if (rewardKind == AwardKind.Coin)
                        {
                            haveCoin = true;
                        }
                        rewardObj = ResourceManager.instance.getObjectFromPool(REWARD_ITEM, rewardLayout);
                        var rewardPresenter = UiManager.bindNode<RewardItemNode>(rewardObj.gameObject);
                        rewardPresenter.setRewardData(reward);
                        break;
                    default:
                        Debug.LogError($"get error awardKind -{reward}");
                        break;
                }
            }
        }
    }
}
