using LobbyLogic.NetWork.ResponseStruct;
using Services;
using System;
using Service;
using UnityEngine;
using UnityEngine.UI;
using EventActivity;
using System.Collections.Generic;
using CommonILRuntime.Module;
using CommonILRuntime.BindingModule;
using Common.Jigsaw;
using System.Threading.Tasks;

namespace Lobby.Popup
{
    public class PopUpBClassActivity : PopUpActivity, IPopUpActivityPresenter
    {
        Dictionary<ActivityID, string> activityPaths = new Dictionary<ActivityID, string>()
        {
            { ActivityID.Rookie,"rookie/rookie_publicity"},
            { ActivityID.FarmBlast,"farm_blast/fb_publicity"},
            { ActivityID.FrenzyJourney,"frenzy_journey/fj_publicity"},
            { ActivityID.MagicForest,"magic_forest/mf_publicity"}
        };
        public override string objPath
        {
            get
            {
                int activityIDInt;
                string objPath = activityPaths[ActivityID.Rookie];
                if (int.TryParse(ActivityDataStore.nowActivityInfo.activityId, out activityIDInt))
                {
                    nowActivityID = (ActivityID)activityIDInt;
                    activityPaths.TryGetValue(nowActivityID, out objPath);
                }
                return $"prefab/activity_publicity/{objPath}";
            }
        }

        protected override BackHideBehaviour hideBehaviour
        {
            get
            {
                if (int.TryParse(ActivityDataStore.nowActivityInfo.activityId, out int activityIDInt))
                {
                    nowActivityID = (ActivityID)activityIDInt;
                    switch (nowActivityID)
                    {
                        case ActivityID.FrenzyJourney:
                        case ActivityID.MagicForest:
                            return BackHideBehaviour.CanDoBoth;
                        default:
                            break;
                    }
                }
                return base.hideBehaviour;
            }
        }

        Text dayLeftText;
        Text infoTxt;
        RectTransform infoParentRect;
        GameObject daysObj;

        TimerService timerService = new TimerService();
        ActivityID nowActivityID;
        List<CardRewardNode> cardRewardNodes = new List<CardRewardNode>();
        bool isOpenActivityPage;

        public override void initContainerPresenter()
        {
            getActivityName();
            base.initContainerPresenter();
        }

        void getActivityName()
        {
            string activityBundleName = $"lobby_publicity_{ActivityDataStore.getActivityEntryPrefabName(ActivityDataStore.getNowActivityID())}";
            resOrder = new string[] { activityBundleName };
        }

        public override void initUIs()
        {
            base.initUIs();
            dayLeftText = getTextData("dayLeftText");
            daysObj = getGameObjectData($"days_{ApplicationConfig.nowLanguage.ToString().ToLower()}");

            if (ActivityID.Rookie == nowActivityID)
            {
                infoTxt = getTextData("infoText");
                infoParentRect = infoTxt.transform.parent.GetComponent<RectTransform>();
                return;
            }

            for (int i = 1; i <= 3; ++i)
            {
                var cardReward = UiManager.bindNode<CardRewardNode>(getNodeData($"round_{i}").cachedGameObject);
                cardReward.showRoundNum(i);
                cardRewardNodes.Add(cardReward);
            }
        }

        public async void setData(PopupData data)
        {
            await setInfoData();
            if (ActivityID.None == nowActivityID)
            {
                close();
                return;
            }
            isOpenActivityPage = false;
            DateTime endTime = UtilServices.strConvertToDateTime(ActivityDataStore.nowActivityInfo.endAt, DateTime.MinValue);
            var remainTime = endTime - UtilServices.nowTime;
            if (remainTime <= TimeSpan.Zero)
            {
                close();
                return;
            }
            setTimeInfo(remainTime);
            if (remainTime.Days > 0)
            {
                return;
            }
            timerService.setAddToGo(uiGameObject);
            timerService.StartTimer(endTime, countDownTime);
        }

        async Task setInfoData()
        {
            var activityData = await AppManager.eventServer.getBaseActivityInfo();
            nowActivityID = ActivityDataStore.getNowActivityID();
            if (ActivityID.Rookie == nowActivityID)
            {
                infoTxt.text = activityData.Banner.Reward.ToString("N0");
                LayoutRebuilder.ForceRebuildLayoutImmediate(infoParentRect);
                return;
            }

            for (int i = 0; i < activityData.Banner.Item.Length; ++i)
            {
                cardRewardNodes[i].setRewardCard(activityData.Banner.Item[i].Type);
            }

        }

        void setTimeInfo(TimeSpan remainTime)
        {
            daysObj.setActiveWhenChange(remainTime.Days > 0);
            if (remainTime.Days > 0)
            {
                dayLeftText.text = UtilServices.formatCountTimeSpan(remainTime);
                return;
            }
            setTimeTxt(UtilServices.formatCountTimeSpan(remainTime));
        }

        void countDownTime(TimeSpan remainTime)
        {
            var timeString = UtilServices.formatCountTimeSpan(remainTime);
            setTimeTxt(timeString);
            if (remainTime <= TimeSpan.Zero)
            {
                stopTimer();
            }
        }

        void setTimeTxt(string times)
        {
            dayLeftText.text = times;
        }

        void stopTimer()
        {
            timerService.disposable.Dispose();
        }

        public override void close()
        {
            stopTimer();
            base.close();
        }

        protected override void onConfirmClick()
        {
            isOpenActivityPage = true;
            LobbyStartPopSortManager.instance.finishShowPopPages();
            closeBtnClick();
        }

        public override void clear()
        {
            base.clear();
            if (isOpenActivityPage)
            {
                ActivityPageData.instance.openActivityPage(nowActivityID);
            }
        }
    }

    public class CardRewardNode : NodePresenter
    {
        public void showRoundNum(int round)
        {
            getTextData($"num_round").text = string.Format(LanguageService.instance.getLanguageValue("EventRound"), round);
        }

        public void setRewardCard(string packIDStr)
        {
            long packID;
            if (!long.TryParse(packIDStr, out packID))
            {
                return;
            }
            getImageData("puzzle_pack_img").sprite = JigsawPackSpriteProvider.getPackSprite((PuzzlePackID)packID);
        }
    }
}
