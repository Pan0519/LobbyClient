using UnityEngine;
using System.Collections.Generic;
using CommonILRuntime.Module;
using LobbyLogic.Audio;
using Lobby.Audio;
using CommonService;

namespace LoginReward
{
    class SevenDayItemNode : NodePresenter
    {
        RectTransform itemGroupTrans;
        GameObject getRewardShadowObj;

        const float itemScale = 0.6f;

        public override void initUIs()
        {
            itemGroupTrans = getBindingData<RectTransform>("day_group_trans");
            getRewardShadowObj = getGameObjectData("check_shadow_obj");
        }

        public override void init()
        {
            getRewardShadowObj.setActiveWhenChange(false);
        }

        public void addDayItemData(List<DayItemData> itemDatas)
        {
            int nowPlayerLv = DataStore.getInstance.playerInfo.level;
            List<DayItemData> showItems = itemDatas.FindAll(item => item.level <= nowPlayerLv || item.itemType == DayItemType.Puzzle);
            for (int i = 0; i < showItems.Count; ++i)
            {
                var data = showItems[i];
                LoginRewardItemData.addDayItem(data, itemGroupTrans, itemScale);
                if (i < showItems.Count - 1)
                {
                    LoginRewardItemData.addPlusItem(itemGroupTrans);
                }
            }
        }

        public StampNode addStamp()
        {
            return LoginRewardItemData.addStampItem(uiRectTransform);
        }

        public void showGetRewardShadow()
        {
            getRewardShadowObj.setActiveWhenChange(true);
        }

        public StampNode addFlyStamp()
        {
            StampNode stampNode = LoginRewardItemData.addStampItem(uiRectTransform);
            stampNode.closeStampAnim();
            stampNode.openStamp();
            return stampNode;
        }
    }

    public class StampNode : NodePresenter
    {
        Animator stampAnim;
        GameObject stampObjA;

        public override void initUIs()
        {
            stampAnim = getAnimatorData("stamp_anim");
            stampObjA = getGameObjectData("stamp_a_obj");
        }

        public void openStamp()
        {
            stampObjA.setActiveWhenChange(true);
        }

        public void closeStampAnim()
        {
            stampAnim.enabled = false;
        }

        public void stampIn(int daysNum)
        {
            AudioManager.instance.playAudioOnce(AudioPathProvider.getAudioPath(LoginAudio.Stamp));
            stampAnim.SetTrigger($"day_{daysNum}");
        }
    }

}
