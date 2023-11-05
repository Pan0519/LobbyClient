using CommonILRuntime.Module;
using CommonILRuntime.Outcome;
using System;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace CommonILRuntime.ExtraGame
{
    public class ExtraGameWinBoardPresenter : ExtraGameBoardPresenter
    {
        Image titleImage;
        Image titleCongratulationEffectImage;
        Image titlePassEffectImage;
        RectTransform itemParent;
        Text moneyRewartTxt;
        Button collectBtn;

        Action onCollectCallBack;

        const string coinType = "coin";
        const string puzzleType = "puzzle";
        const string rewardItem = "prefab/reward/reward_item";
        const string rewardItemPack = "prefab/reward_item/reward_item_pack";

        public override void initUIs()
        {
            base.initUIs();
            titleImage = getImageData("title");
            titleCongratulationEffectImage = getImageData("title_effect1");
            titlePassEffectImage = getImageData("title_effect2");
            itemParent = getRectData("item_parent");
            moneyRewartTxt = getTextData("money_reward_txt");
            collectBtn = getBtnData("collect_btn");
        }

        public override void init()
        {
            collectBtn.onClick.AddListener(onCollectClick);
            setImageSprite(titleImage, "title");
            setImageSprite(titleCongratulationEffectImage, "congrats");
            setImageSprite(titlePassEffectImage, "pass");
            setImageSprite(collectBtn.image, "btn");
        }

        void setImageSprite(Image image, string spriteName)
        {
            image.sprite = getBoardSprite(spriteName);
            image.SetNativeSize();
        }

        Sprite getBoardSprite(string spriteName)
        {
            spriteName = spriteProvider.convertFirstWinBoardName(spriteName);
            return spriteProvider.getSprite(spriteName);
        }

        void onCollectClick()
        {
            collectBtn.interactable = false;
            playOutAni(onOutAniOver);
        }

        private void onOutAniOver()
        {
            onCollectCallBack?.Invoke();
        }

        public void open(CommonReward[] commonRewards, Action onCollectCallBack)
        {
            this.onCollectCallBack = onCollectCallBack;
            updateReward(commonRewards);
        }

        void updateReward(CommonReward[] commonRewards)
        {
            int count = commonRewards.Length;
            decimal totalCoinReward = 0;
            CommonReward commonReward = null;

            for (int i = 0; i < count; ++i)
            {
                commonReward = commonRewards[i];
                totalCoinReward = updateCoinReward(commonReward, totalCoinReward);
                updateRewardItem(commonReward);
            }

            moneyRewartTxt.text = ((ulong)totalCoinReward).ToString("N0");
        }

        decimal updateCoinReward(CommonReward commonReward,decimal currentTotal)
        {
            if (commonReward.kind.Contains(coinType))
            {
                currentTotal += commonReward.amount;
            }

            return currentTotal;
        }

        void updateRewardItem(CommonReward commonReward)
        {
            //PoolObject rewardObj = null;
            //RewardPackItemNode 

            //if (commonReward.kind.Contains(puzzleType))
            //{
            //    rewardObj = ResourceManager.instance.getObjectFromPool(rewardItemPack, itemParent);

            //}
            //else
            //{
                
            //}
        }
    }
}
