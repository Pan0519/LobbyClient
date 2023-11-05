using CommonPresenter;
using UnityEngine.UI;
using UnityEngine;
using System.Threading.Tasks;
using CommonILRuntime.BindingModule;
using System;
using System.Collections.Generic;
using CommonILRuntime.Module;
using UniRx;
using Game.Common;
using LobbyLogic.Audio;
using Lobby.Audio;
using Lobby;
using Services;
using CommonService;

namespace LoginReward
{
    class LoginRewardPresenter : SystemUIBasePresenter
    {
        public override string objPath => "prefab/login_reward/login_reward_main";
        public override UiLayer uiLayer { get => UiLayer.GameMessage; }
        #region UIs
        Button closeBtn;
        Image dayProgressBar;
        RectTransform barEffectRect;
        Text daysTxt;
        RectTransform rewardInfoTrans;
        RectTransform rewardInfoGroup;
        Button infoTapBtn;
        RectTransform flyStampTarget;
        Animator stampAnim;
        RectTransform starGroup;
        #endregion
        List<SevenDayItemNode> sevenDayItems = new List<SevenDayItemNode>();
        SevenDayItemNode nowDayItems = null;
        const int sevenItemCount = 7;
        Dictionary<int, List<DayItemData>> dayRewardsDict = new Dictionary<int, List<DayItemData>>();
        Dictionary<int, DayRewardNode> dayRewardNodes = new Dictionary<int, DayRewardNode>();

        double progressBarUnit;
        int totalMonthDays { get { return LoginRewardServices.instance.totalMonthDays; } }
        int rewardFinalDay { get { return LoginRewardServices.instance.rewardFinalDay; } }
        float barEffectPosX;
        int cumulativeSeventDays;
        StampNode flyStamp;
        List<string> flyTweens = new List<string>();
        string barEffectTweenID;
        List<IDisposable> moveDis = new List<IDisposable>();
        RectTransform lastRoot;
        public override void initContainerPresenter()
        {
            resOrder = new string[] { AssetBundleData.getBundleName(BundleType.LoginReward) };
            base.initContainerPresenter();
        }
        public override void initUIs()
        {
            base.initUIs();
            closeBtn = getBtnData("closeButton");
            dayProgressBar = getImageData("progressBar");
            barEffectRect = getBindingData<RectTransform>("bar_effect");
            rewardInfoTrans = getBindingData<RectTransform>("reward_info_trans");
            rewardInfoGroup = getBindingData<RectTransform>("reward_info_group");
            infoTapBtn = getBtnData("info_tap_btn");
            daysTxt = getTextData("days_txt");
            flyStampTarget = getBindingData<RectTransform>("check_stamp_rect");
            stampAnim = getAnimatorData("stamp_anim");
            starGroup = getRectData("star_group");
            for (int i = 0; i < starGroup.childCount; ++i)
            {
                starGroup.GetChild(i).gameObject.setActiveWhenChange(false);
            }
            getGameObjectData($"star_{ApplicationConfig.nowLanguage.ToString().ToLower()}").setActiveWhenChange(true);
            for (int i = 0; i < sevenItemCount; ++i)
            {
                var dayItemNode = UiManager.bindNode<SevenDayItemNode>(getNodeData($"day_{i + 1}_node").cachedGameObject);
                sevenDayItems.Add(dayItemNode);
            }

            for (int i = 0; i < LoginRewardServices.instance.rewardDaysNum.Count; ++i)
            {
                var dayReward = UiManager.bindNode<DayRewardNode>(getNodeData($"day_reward_{i}").cachedGameObject);
                dayReward.setGoalDayNum(LoginRewardServices.instance.rewardDaysNum[i]);
                dayReward.infoBtnClickSub.Subscribe(rewardInfoSub).AddTo(uiGameObject);
                dayRewardNodes.Add(dayReward.goalDayNum, dayReward);
            }

            lastRoot = getRectData("last_root_rect");
        }

        public override void init()
        {
            closeBtn.gameObject.setActiveWhenChange(false);
            barEffectRect.gameObject.setActiveWhenChange(false);
            barEffectPosX = dayProgressBar.GetComponent<RectTransform>().rect.width;
            progressBarUnit = (1.0f / (float)rewardFinalDay);
            infoTapBtn.onClick.AddListener(closeInfoGroup);
            closeBtn.onClick.AddListener(closeBtnClick);
            closeInfoGroup();
            cumulativeSeventDays = LoginRewardServices.instance.resettableCumulativeDays % sevenItemCount;
            if (cumulativeSeventDays <= 0)
            {
                cumulativeSeventDays = sevenItemCount;
            }
            base.init();
        }

        void showNowDaysData()
        {
            updateDaysTxt(totalMonthDays);
            updateRewardGoal(totalMonthDays);
            setProgressBar(totalMonthDays * (float)progressBarUnit);

            for (int i = 0; i < sevenDayItems.Count; ++i)
            {
                if (i >= cumulativeSeventDays)
                {
                    break;
                }
                var dayItem = sevenDayItems[i];
                dayItem.addStamp().openStamp();
                dayItem.showGetRewardShadow();
            }
        }

        void setLastDayData()
        {
            int day = totalMonthDays - 1;
            updateDaysTxt(day);
            updateRewardGoal(day);
            setProgressBar(day * (float)progressBarUnit);
            showSevenDayReward();
        }

        void showSevenDayReward()
        {
            for (int i = 0; i < sevenDayItems.Count; ++i)
            {
                if ((i + 1) >= cumulativeSeventDays)
                {
                    break;
                }
                var dayItem = sevenDayItems[i];
                dayItem.addStamp().openStamp();
                dayItem.showGetRewardShadow();
            }
        }

        public void showNowDailyData()
        {
            var stampObj = nowDayItems.addStamp();
            stampObj.uiRectTransform.SetParent(lastRoot);
            stampObj.stampIn(cumulativeSeventDays);
            Observable.TimerFrame(60).Subscribe(_ =>
             {
                 nowDayItems.showGetRewardShadow();
             });
            flyStamp = nowDayItems.addFlyStamp();
            flyStamp.uiGameObject.setActiveWhenChange(false);
            stampBezier = UiManager.bind<BezierPresenter>(flyStamp.uiGameObject);
            moveDis.Add(Observable.TimerFrame(100).Subscribe(timer =>
            {
                stampObj.uiRectTransform.SetParent(nowDayItems.uiRectTransform);
                moveStamp();
            }).AddTo(uiGameObject));
        }

        public void showAllDaysItem()
        {
            IDisposable testDis = null;
            testDis = Observable.Timer(TimeSpan.Zero, TimeSpan.FromSeconds(5f)).Subscribe(repeatCount =>
             {
                 nowDayItems = sevenDayItems[(int)repeatCount];
                 showNowDailyData();
                 if (repeatCount >= sevenDayItems.Count - 1)
                 {
                     testDis.Dispose();
                 }
             });
        }

        public void showHistoryDailyExpectToday(Dictionary<int, List<DayItemData>> dailyDatas)
        {
            for (int i = 0; i < sevenDayItems.Count; ++i)
            {
                List<DayItemData> itemData;
                if (dailyDatas.TryGetValue(i + 1, out itemData))
                {
                    sevenDayItems[i].addDayItemData(itemData);
                }
            }
            nowDayItems = sevenDayItems[cumulativeSeventDays - 1];
            setLastDayData();
            closeBtn.gameObject.setActiveWhenChange(true);
            open();
        }

        public void showHistoryDailyBesideToday(Dictionary<int, List<DayItemData>> dailyDatas)
        {
            for (int i = 0; i < sevenDayItems.Count; ++i)
            {
                List<DayItemData> itemData;
                if (dailyDatas.TryGetValue(i + 1, out itemData))
                {
                    sevenDayItems[i].addDayItemData(itemData);
                }
            }
            showNowDaysData();
            closeBtn.gameObject.setActiveWhenChange(true);
            open();
        }

        BezierPresenter stampBezier;
        void moveStamp()
        {
            if (null == flyStamp)
            {
                return;
            }
            flyStamp.uiGameObject.setActiveWhenChange(true);
            flyStamp.uiRectTransform.SetParent(lastRoot);
            AudioManager.instance.playAudioOnce(AudioPathProvider.getAudioPath(ActivityBlastAudio.IconFly));
            TweenManager.tweenToFloat(flyStamp.uiRectTransform.localScale.x, 0.55f, durationTime: 0.8f, onUpdate: (val) =>
            {
                flyStamp.uiRectTransform.localScale = new Vector2(val, val);
            });
            stampBezier.isDrawLine = true;
            Vector2 middlePos = BezierUtils.getNormalDirShiftPoint(flyStamp.uiTransform.position, flyStampTarget.position, -1f);
            stampBezier.bezierPoints.Add(flyStamp.uiTransform.position);
            stampBezier.bezierPoints.Add(middlePos);
            stampBezier.bezierPoints.Add(flyStampTarget.position);
            stampBezier.moveBezierLine(0.8f, callback: stampFlyComplete);
        }

        async void stampFlyComplete()
        {
            stampAnim.SetTrigger("stamp_in");
            AudioManager.instance.playAudioOnce(AudioPathProvider.getAudioPath(ActivityBlastAudio.IconFlyIn));
            flyStamp.uiGameObject.setActiveWhenChange(false);
            updateDaysTxt(totalMonthDays);
            flyTweens.Clear();
            await Task.Delay(TimeSpan.FromSeconds(0.5f));
            tweenProgressBar();
            closeBtn.gameObject.setActiveWhenChange(true);
        }

        public void setDayItemDatas(Dictionary<int, List<DayItemData>> dayItems)
        {
            dayRewardsDict = dayItems;
        }

        public void setNowDayGoal()
        {
            updateRewardGoal(totalMonthDays);
        }

        void updateDaysTxt(int day)
        {
            daysTxt.text = $"{day}/{rewardFinalDay}";
        }

        void updateRewardGoal(int totalDay)
        {
            var dayRewardEnum = dayRewardNodes.GetEnumerator();
            while (dayRewardEnum.MoveNext())
            {
                dayRewardEnum.Current.Value.setDayRewardGoal(totalDay);
            }
        }

        public DayRewardNode getDayRewardNode(int goalDay)
        {
            DayRewardNode result;
            dayRewardNodes.TryGetValue(goalDay, out result);
            return result;
        }

        public override void animOut()
        {
            HighRoller.HighRollerDataManager.instance.checkUserRecordData();
            UtilServices.disposeSubscribes(moveDis.ToArray());
            clear();
            if (!string.IsNullOrEmpty(barEffectTweenID))
            {
                TweenManager.tweenKill(barEffectTweenID);
            }
            if (DataStore.getInstance.guideServices.nowStatus != GuideStatus.Completed)
            {
                DataStore.getInstance.guideServices.toNextStep();
                return;
            }
            LobbyStartPopSortManager.instance.toNextPop();
        }

        void closeInfoGroup()
        {
            rewardInfoTrans.gameObject.setActiveWhenChange(false);
            int childCount = rewardInfoGroup.transform.childCount;
            for (int i = childCount - 1; i >= 0; i--)
            {
                ResourceManager.instance.returnObjectToPool(rewardInfoGroup.GetChild(i).gameObject);
            }

            infoTapBtn.gameObject.setActiveWhenChange(false);
        }

        void rewardInfoSub(DayRewardNode rewardNode)
        {
            infoTapBtn.gameObject.setActiveWhenChange(true);
            rewardInfoTrans.SetParent(rewardNode.uiRectTransform);
            var infoPos = rewardInfoTrans.anchoredPosition;
            infoPos.Set(0, infoPos.y);
            rewardInfoTrans.anchoredPosition = infoPos;
            List<DayItemData> rewardItemDatas;
            if (!dayRewardsDict.TryGetValue(rewardNode.goalDayNum, out rewardItemDatas))
            {
                return;
            }

            LoginRewardServices.instance.setRewardInfos(rewardItemDatas, rewardInfoGroup, 0.5f);
            rewardInfoTrans.gameObject.setActiveWhenChange(true);
        }

        void tweenProgressBar()
        {
            barEffectRect.gameObject.setActiveWhenChange(true);
            float endAmount = totalMonthDays * (float)progressBarUnit;
            barEffectTweenID = TweenManager.tweenToFloat(dayProgressBar.fillAmount, endAmount, 0.5f, onUpdate: setProgressBar, onComplete: () =>
             {
                 barEffectTweenID = string.Empty;
                 LoginRewardServices.instance.showNextStep();
                 barEffectRect.gameObject.setActiveWhenChange(false);
             });
        }

        void setProgressBar(float amount)
        {
            dayProgressBar.fillAmount = amount;
            float posX = (dayProgressBar.fillAmount - 1) * barEffectPosX;
            barEffectRect.anchoredPosition = new Vector2(posX + 5, 0);
        }
    }
}
