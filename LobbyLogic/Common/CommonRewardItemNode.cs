using UnityEngine;
using UnityEngine.UI;
using CommonILRuntime.Module;
using CommonILRuntime.Extension;
using Common.Jigsaw;
using System;
using System.Threading.Tasks;
using EventActivity;
using CommonILRuntime.Outcome;
using System.Collections.Generic;

namespace Lobby.Common
{
    public enum ItemType
    {
        Coin,
        Coupon,
        Exp,
        Puzzle,
        LvUp,
        HighRollerPoint,
        HighRollerPassPoint
    }
    public class RewardItemNode : NodePresenter
    {
        Text numTxt;
        Image rewardImg;
        AwardKind awardKind;
        ulong rewardNum;

        Dictionary<string, string> activityPropSpriteName = new Dictionary<string, string>()
        {
            { "20001","ticket"},
            { "20002","ticket"},
            { "20003","dice"},
            { "20004","magnifier"}
        };
        public override void initUIs()
        {
            numTxt = getTextData("reward_num_txt");
            rewardImg = getImageData("reward_img");
        }

        public void changeNumScale(float newScale)
        {
            numTxt.transform.localScale = new Vector3(newScale, newScale, newScale);
        }

        void setNum()
        {
            switch (awardKind)
            {
                case AwardKind.Coin:
                    numTxt.text = rewardNum.convertToCurrencyUnit(showLong: 3, havePoint: true, pointDigits: 2);
                    break;
                case AwardKind.Coupon:
                    numTxt.text = $"{rewardNum}%";
                    break;
                case AwardKind.Exp:
                case AwardKind.HighRollerPoint:
                case AwardKind.HighRollerPassPoint:
                    numTxt.text = rewardNum.ToString();
                    break;
                case AwardKind.Ticket:
                    numTxt.text = $"x{rewardNum}";
                    break;
            }
            delayRebuildLayout();
        }

        public void setRewardData(Reward reward)
        {
            this.rewardNum = reward.getAmount();
            awardKind = ActivityDataStore.getAwardKind(reward.kind);
            string spriteName = reward.kind;

            if (AwardKind.Ticket == awardKind)
            {
                spriteName = activityPropSpriteName[reward.type];
            }
            rewardImg.sprite = LobbySpriteProvider.instance.getSprite<RewardItemSpriteProvider>(LobbySpriteType.RewardItem, $"reward_{spriteName}");
            setNum();
        }

        async void delayRebuildLayout()
        {
            await Task.Delay(TimeSpan.FromSeconds(0.03f));
            LayoutRebuilder.ForceRebuildLayoutImmediate(numTxt.transform.parent.transform as RectTransform);
        }
    }

    public class RewardPackItemNode : NodePresenter
    {
        RectTransform starRect;
        Image puzzleImg;
        public override void initUIs()
        {
            starRect = getBindingData<RectTransform>("star_group");
            puzzleImg = getImageData("puzzle_img");
        }
        public RewardPackItemNode setPuzzlePack(string type)
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
