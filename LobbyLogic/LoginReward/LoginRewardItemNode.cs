using UnityEngine;
using UnityEngine.UI;
using CommonILRuntime.Module;
using CommonILRuntime.Extension;
using Common.Jigsaw;
using System;
using System.Threading.Tasks;

namespace LoginReward
{
    public class LoginRewardItemNode : NodePresenter
    {
        Text numTxt;

        public override void initUIs()
        {
            numTxt = getTextData("reward_num_txt");
        }

        public LoginRewardItemNode setNum(DayItemType itemType, ulong rewardNum)
        {
            switch (itemType)
            {
                case DayItemType.Coin:
                    numTxt.text = rewardNum.convertToCurrencyUnit(showLong: 3, havePoint: true, pointDigits: 2);
                    break;

                case DayItemType.Coupon:
                    numTxt.text = $"{rewardNum}%";
                    break;

                case DayItemType.Exp:
                    numTxt.text = $"{rewardNum}";
                    break;
                case DayItemType.HighRollerPassPoint:
                case DayItemType.HighRollerPoint:
                    numTxt.text = rewardNum.ToString();
                    break;
            }
            delayRebuildLayout();
            return this;
        }

        async void delayRebuildLayout()
        {
            await Task.Delay(TimeSpan.FromSeconds(0.03f));
            LayoutRebuilder.ForceRebuildLayoutImmediate(numTxt.transform.parent.transform as RectTransform);
        }
    }

    public class LoginRewardStarItemNode : NodePresenter
    {
        RectTransform starRect;
        Image puzzleImg;
        public override void initUIs()
        {
            starRect = getBindingData<RectTransform>("star_group");
            puzzleImg = getImageData("puzzle_img");
        }
        public LoginRewardStarItemNode setPuzzlePack(string type)
        {
            long puzzleType;
            if (long.TryParse(type, out puzzleType))
            {
                PuzzlePackID packId = (PuzzlePackID)puzzleType;
                puzzleImg.sprite = JigsawPackSpriteProvider.getPackSprite(packId);
                setStar(JigsawPackSpriteProvider.getPackStarID(packId));
            }

            return this;
        }

        async void setStar(long starID)
        {
            for (int i = 0; i < starRect.childCount; ++i)
            {
                starRect.GetChild(i).gameObject.setActiveWhenChange((i + 1) <= starID);
            }
            await Task.Delay(TimeSpan.FromSeconds(0.1f));
            LayoutRebuilder.ForceRebuildLayoutImmediate(starRect.transform.parent.GetComponent<RectTransform>());
        }
    }
}
