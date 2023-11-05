using CommonILRuntime.Module;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using CommonILRuntime.BindingModule;
using CommonPresenter;
using UniRx;
using UniRx.Triggers;
using System;
using Services;
using Lobby.UI;
using LoginReward;
using LobbyLogic.NetWork.ResponseStruct;
using CommonILRuntime.Outcome;
using CommonService;
using Lobby.Common;
using Lobby.Jigsaw;

namespace Mission
{
    class MissionMainPresenter : SystemUIBasePresenter
    {
        public override string objPath => UtilServices.getOrientationObjPath("prefab/daily_mission/daily_mission_main");
        public override UiLayer uiLayer { get => UiLayer.GameMessage; }

        #region UIs
        Image progressBar;
        RectTransform barEffectRect;
        Animator barEffectAnim;
        Text schedualTxt;
        Button closeBtn;
        Button medalBoxBtn50;
        Button medalBoxBtn100;
        Button infoTapBtn;

        GameObject nextNormalObj;
        RectTransform rewardInfo;
        RectTransform rewardInfoParent;
        RectTransform medalEffectParent;

        Text normalLvTxt;
        RectTransform dailyMedalRect;
        Animator dailyMedalAnim;

        Text timesTxt;
        GameObject daysObj;
        GameObject titleLightObj;
        Text daysNum;

        TitleImage mainTitleImages;
        TitleImage normalMissionTitleImages;
        TitleImage specialMissionTitleImages;
        #endregion

        MissionSchedualNode normalNodePresneter;
        MissionSchedualNode specialNodePresneter;
        MissionHelper missionHelper;
        TimerService resetTimeServices;
        ShowingRewardInfo showingInfo;

        TitleMedalNode normalTilteNode;
        TitleMedalNode specialTitleNode;

        MissionRewardType missionRewardType;
        float barEffectPosX;
        IDisposable barEffectDis;
        TimerService totalMissionTimeServies;
        Outcome outcome;
        bool needNotifyState;
        CommonReward[] currentRewards;

        private const float rewardInfoScale = 0.5f;
        public override void initContainerPresenter()
        {
            resOrder = new string[] { AssetBundleData.getBundleName(BundleType.DailyMission) };
            base.initContainerPresenter();
        }
        public override void initUIs()
        {
            base.initUIs();
            progressBar = getImageData("progressBar");
            barEffectRect = getRectData("bar_effect");
            barEffectAnim = getAnimatorData("bar_effect_anim");
            schedualTxt = getTextData("schedual_txt");
            timesTxt = getTextData("times_txt");
            closeBtn = getBtnData("closeBtn");
            medalBoxBtn50 = getBtnData("medal_box_50");
            medalBoxBtn100 = getBtnData("medal_box_100");
            infoTapBtn = getBtnData("info_tap_btn");

            nextNormalObj = getGameObjectData("next_normal_obj");
            rewardInfo = getRectData("medal_reward_info_trans");
            rewardInfoParent = getRectData("medal_reward_info_group");
            medalEffectParent = getRectData("medal_effect_parent");

            normalLvTxt = getTextData("normal_lv_txt");
            dailyMedalRect = getRectData("daily_medal_rect");
            dailyMedalAnim = getAnimatorData("daily_medal_anim");

            string nowLanguage = ApplicationConfig.nowLanguage.ToString().ToLower();
            daysObj = getGameObjectData($"days_{nowLanguage}");
            daysNum = getTextData($"days_{nowLanguage}_num_txt");
            titleLightObj = getGameObjectData($"title_light_{nowLanguage}");

            var languageKeys = Enum.GetNames(typeof(ApplicationConfig.Language));
            for (int i = 1; i < languageKeys.Length; ++i)
            {
                getGameObjectData($"days_{languageKeys[i].ToLower()}").setActiveWhenChange(false);
            }

            normalNodePresneter = UiManager.bindNode<MissionSchedualNode>(getNodeData("normal_node").cachedGameObject);
            specialNodePresneter = UiManager.bindNode<MissionSchedualNode>(getNodeData("special_node").cachedGameObject);

            normalTilteNode = UiManager.bindNode<TitleMedalNode>(getNodeData("normal_medal_node").cachedGameObject);
            specialTitleNode = UiManager.bindNode<TitleMedalNode>(getNodeData("special_medal_node").cachedGameObject);

            mainTitleImages = new TitleImage(this, "main_title");
            normalMissionTitleImages = new TitleImage(this, "normal_mission_title");
            specialMissionTitleImages = new TitleImage(this, "special_mission_title");
        }

        public override void init()
        {
            base.init();
            missionHelper = MissionData.getMissionHelper();
            daysObj.setActiveWhenChange(false);
            timesTxt.gameObject.setActiveWhenChange(false);
            barEffectPosX = progressBar.rectTransform.rect.width;
            closeBtn.onClick.AddListener(closeBtnClick);
            normalNodePresneter.openReward = openNormalReward;
            specialNodePresneter.openReward = openSpecialReward;
            barEffectRect.gameObject.setActiveWhenChange(false);
            normalNodePresneter.setClickBoxCallBack(onClickMissionBox);
            specialNodePresneter.setClickBoxCallBack(onClickMissionBox);
            rewardInfo.gameObject.setActiveWhenChange(false);
            medalBoxBtn50.onClick.AddListener(onClickMedalBox50);
            medalBoxBtn100.onClick.AddListener(onClickMedalBox100);
            infoTapBtn.onClick.AddListener(onClickInfoTapBtn);
            infoTapBtn.gameObject.setActiveWhenChange(false);
            titleLightObj.setActiveWhenChange(true);
            initTitleImages();
        }

        void initTitleImages()
        {
            string tempName = UtilServices.getOrientationObjPath("title_daily_mission");

            mainTitleImages.setSprite(tempName, "flow");
            normalMissionTitleImages.setSprite("tex_normal_mission");
            tempName = UtilServices.getOrientationObjPath("tex_special_mission");
            specialMissionTitleImages.setSprite(tempName);
        }

        public override async void open()
        {
            BindingLoadingPage.instance.open();
            await MissionData.updateData();
            base.open();
            updateDailyMedalTxt();
            updateProgressBar();
            updateRemainTime();
            initData();
            BindingLoadingPage.instance.close();
        }

        public override void close()
        {
            base.close();
            titleLightObj.setActiveWhenChange(false);
        }

        public async void openAndAutoObtainReward()
        {
            BindingLoadingPage.instance.open();
            await MissionData.updateData();
            base.open();
            updateDailyMedalTxt();
            updateProgressBar();
            updateRemainTime();
            initData();
            attempToObtainReward();
        }

        void attempToObtainReward()
        {
            if (MissionData.checkNormalMissionCanReceive())
            {
                normalNodePresneter.collectClick();
                return;
            }
            if (MissionData.checkSpecialMissionCanReceive())
            {
                specialNodePresneter.collectClick();
                return;
            }
            BindingLoadingPage.instance.close();
        }

        void updateProgressBar()
        {
            progressBar.fillAmount = MissionData.missionMedalCount / MissionData.missionMedalMaxCount;
            runBarEffect(progressBar.fillAmount);
            tryObtainMedalReward();
            updateBoxState();
        }

        void updateRemainTime()
        {
            TimeSpan remainTime = MissionData.getMissionRemainingTime();
            if (remainTime.Days >= 1)
            {
                daysObj.setActiveWhenChange(true);
                daysNum.text = $"{remainTime.Days + 1}";
            }
            else
            {
                timesTxt.text = UtilServices.toTimeStruct(remainTime).toTimeString();
                timesTxt.gameObject.setActiveWhenChange(true);
                totalMissionTimeServies = new TimerService();
                totalMissionTimeServies.StartTimer(DateTime.UtcNow.Add(remainTime), countdownMissionTotalTime);
            }
        }

        public override void animOut()
        {
            missionHelper = null;
            if (needNotifyState)
            {
                notifyCurrentState();
            }
            LobbyLogic.Common.GamePauseManager.gameResume();
            clear();
        }

        public override void clear()
        {
            base.clear();
            if (null != totalMissionTimeServies)
            {
                totalMissionTimeServies.disposable.Dispose();
                totalMissionTimeServies = null;
            }
        }

        void notifyCurrentState()
        {
            needNotifyState = false;
            MissionData.noticeHaveNewMission(true);
        }

        void countdownMissionTotalTime(TimeSpan remainTime)
        {
            timesTxt.text = UtilServices.toTimeStruct(remainTime).toTimeString();
            if (remainTime <= TimeSpan.Zero)
            {
                totalMissionTimeServies.ExecuteTimer();
            }
        }

        void initData()
        {
            setNormalMissionData();
            setSpecialMissionData();
        }

        void setNormalMissionData()
        {
            normalLvTxt.text = $"{MissionData.normalRound}/{MissionData.normalMaxRound}";
            if (MissionData.checkNormalMissionIsOver())
            {
                resetTimeServices = new TimerService();
                resetTimeServices.setAddToGo(nextNormalObj);
                resetTimeServices.StartTimer(MissionData.normalMissionData.resetTime, updateNextNormalTime);
                nextNormalObj.setActiveWhenChange(true);
                normalNodePresneter.close();
                normalTilteNode.close();
                return;
            }
            nextNormalObj.setActiveWhenChange(false);
            normalNodePresneter.open();
            normalNodePresneter.setProgressData(MissionData.normalMissionData);
            normalTilteNode.setReward(MissionData.normalMissionData.medalReward);
        }

        void updateNextNormalTime(TimeSpan timeSpan)
        {
            normalNodePresneter.updateResetTime(timeSpan);
            if (timeSpan <= TimeSpan.Zero)
            {
                resetTimeServices.ExecuteTimer();
            }
        }

        void setSpecialMissionData()
        {
            specialNodePresneter.setProgressData(MissionData.specialMissionData);
            specialTitleNode.setReward(MissionData.specialMissionData.medalReward);
        }

        void openNormalReward()
        {
            BindingLoadingPage.instance.open();
            missionHelper.askNormalReward(receiveNormelReward);
        }

        void receiveNormelReward(MissionHelper.RewardFormat format)
        {
            outcome = format.outcome;
            currentRewards = format.commonRewards.ToArray();
            BindingLoadingPage.instance.close();
            missionRewardType = MissionRewardType.Normal;
            openMissionReward().openReward(format.commonRewards, format.finalPlayerCoin);
        }

        void openSpecialReward()
        {
            BindingLoadingPage.instance.open();
            missionHelper.askSpecialReward(receiveSpecialReward);
        }

        void receiveSpecialReward(MissionHelper.RewardFormat format)
        {
            outcome = format.outcome;
            currentRewards = format.commonRewards.ToArray();
            BindingLoadingPage.instance.close();
            missionRewardType = MissionRewardType.Special;
            openMissionReward().openReward(format.commonRewards, format.finalPlayerCoin);
        }

        MissionRewardPresenter openMissionReward()
        {
            MissionRewardPresenter reward = null;
            needNotifyState = true;

            switch (missionRewardType)
            {
                case MissionRewardType.Normal:
                    reward = UiManager.getPresenter<MissionNormalRewardPresenter>();
                    break;
                case MissionRewardType.Special:
                    reward = UiManager.getPresenter<MissionSpecialRewardPresenter>();
                    break;
            }

            reward.setAnimoutCallback(() => playRewardItemAni(flyMedalObj));
            return reward;
        }

        private void playRewardItemAni(Action aniOverAni)
        {
            OpenPackWildProcess.openPackWild(currentRewards, aniOverAni);
        }

        void flyMedalObj()
        {
            var flyObjTemp = ResourceManager.instance.getGameObjectWithResOrder("prefab/daily_mission/medal_mission", resOrder);
            MissionMedalNode flyObjNode = UiManager.bindNode<MissionMedalNode>(GameObject.Instantiate(flyObjTemp));

            closeBtn.interactable = false;
            switch (missionRewardType)
            {
                case MissionRewardType.Normal:
                    normalTilteNode.addMedalFlyObj(flyObjNode);
                    break;
                case MissionRewardType.Special:
                    specialTitleNode.addMedalFlyObj(flyObjNode);
                    break;
            }

            flyObjNode.playGetAnim(() =>
            {
                flyObjNode.uiRectTransform.SetParent(medalEffectParent);
                flyObjNode.uiRectTransform.movePos(dailyMedalRect.transform.position, 0.8f, onComplete: () =>
                {
                    flyObjNode.close();
                    dailyMedalAnim.SetTrigger("get");
                    runProgressBar(flyObjNode.rewardAmount);
                });
            });
        }

        void runProgressBar(long reward)
        {
            MissionData.missionMedalCount += reward;
            updateDailyMedalTxt();
            var endProgress = MissionData.missionMedalCount / MissionData.missionMedalMaxCount;
            barEffectRect.gameObject.setActiveWhenChange(true);
            TweenManager.tweenToFloat(progressBar.fillAmount, endProgress, 1.0f, onUpdate: runBarEffect, onComplete: barEffectRunComplete);
        }

        void runBarEffect(float value)
        {
            progressBar.fillAmount = value;
            float endX = (value - 1) * barEffectPosX;
            var changePos = barEffectRect.anchoredPosition;
            changePos.Set(((float)Math.Round(endX, 2)) + 5, 0);
            barEffectRect.anchoredPosition = changePos;
        }

        void barEffectRunComplete()
        {
            var animtrigger = barEffectAnim.GetBehaviour<ObservableStateMachineTrigger>();
            barEffectDis = animtrigger.OnStateEnterAsObservable().ObserveOnMainThread().Subscribe(barEffectAnimTrigger).AddTo(uiGameObject);
            barEffectAnim.SetTrigger("out");
            closeBtn.interactable = true;
        }

        void barEffectAnimTrigger(ObservableStateMachineTrigger.OnStateInfo obj)
        {
            IDisposable barEffectAnimTimerDis = null;
            barEffectAnimTimerDis = Observable.Timer(TimeSpan.FromSeconds(obj.StateInfo.length)).Subscribe(_ =>
            {
                if (!tryObtainMedalReward())
                {
                    resetMissionRewardData();
                }
                barEffectAnimTimerDis.Dispose();
                barEffectRect.gameObject.setActiveWhenChange(false);
                UtilServices.disposeSubscribes(barEffectDis);
            }).AddTo(uiGameObject);
        }

        bool tryObtainMedalReward()
        {
            int stageIndex = 0;
            if (MissionData.tryGetCurrentMedalRewardIndex(ref stageIndex))
            {
                missionHelper.askMedalReward(stageIndex, showMedalReward);
                return true;
            }

            return false;
        }

        void updateBoxState()
        {
            medalBoxBtn50.interactable = !MissionData.checkMedalIsReceive(0);
            medalBoxBtn100.interactable = !MissionData.checkMedalIsReceive(1);
        }

        void showMedalReward(MissionHelper.RewardFormat format)
        {
            var prizePresenter = UiManager.getPresenter<MissionPrizePresenter>();

            outcome = format.outcome;
            currentRewards = format.commonRewards.ToArray();
            prizePresenter.openReward(format.commonRewards, format.finalPlayerCoin);
            prizePresenter.setAnimoutCallback(() => playRewardItemAni(resetMissionRewardData));
        }

        void updateDailyMedalTxt()
        {
            schedualTxt.text = $"{(int)MissionData.missionMedalCount}/{(int)MissionData.missionMedalMaxCount}";
        }

        async void resetMissionRewardData()
        {
            applyRewardOutcome();
            await MissionData.updateData();
            updateBoxState();
            switch (missionRewardType)
            {
                case MissionRewardType.Normal:
                    setNormalMissionData();
                    break;

                case MissionRewardType.Special:
                    setSpecialMissionData();
                    break;
            }
        }

        void applyRewardOutcome()
        {
            outcome.apply();
            outcome = null;
        }

        void onClickInfoTapBtn()
        {
            if (null != showingInfo)
            {
                releaseRewardInfoItem(showingInfo.itemParent);
                showingInfo.infoRect.gameObject.setActiveWhenChange(false);
                showingInfo = null;
                infoTapBtn.gameObject.setActiveWhenChange(false);
            }
        }

        void onClickMedalBox50()
        {
            var data = MissionData.medalRewardDatas[0];
            var btnRect = medalBoxBtn50.GetComponent<RectTransform>();

            showMedalRewardInfo(data, btnRect);
        }

        void onClickMedalBox100()
        {
            var data = MissionData.medalRewardDatas[1];
            var btnRect = medalBoxBtn100.GetComponent<RectTransform>();

            showMedalRewardInfo(data, btnRect);
        }

        void showMedalRewardInfo(MedalRewardData data, RectTransform btnRect)
        {
            setRewardInfoPosi(btnRect);
            setRewardItem(rewardInfoParent, true, data.rewardDatas);
            showRewardInfo(rewardInfo, rewardInfoParent);
        }

        void setRewardInfoPosi(RectTransform boxRect)
        {
            Vector2 tempPosi = default(Vector2);

            rewardInfo.position = boxRect.position;
            tempPosi = rewardInfo.anchoredPosition;
            tempPosi.y += (boxRect.sizeDelta.y / 2);
            rewardInfo.anchoredPosition = tempPosi;
        }

        void onClickMissionBox(RectTransform boxRect, MissionProgressData progressData)
        {
            setRewardInfoPosi(boxRect);
            setRewardItem(rewardInfoParent, false, progressData.rewardData);
            showRewardInfo(rewardInfo, rewardInfoParent);
        }

        void setRewardItem(RectTransform parent, bool isNeedExchange, params MissionReward[] rewardDatas)
        {
            var uiFormat = convertItemToDayItemData(isNeedExchange, rewardDatas);
            LoginRewardServices.instance.setRewardInfos(uiFormat, parent, rewardInfoScale);
        }

        List<DayItemData> convertItemToDayItemData(bool isNeedExchange, params MissionReward[] rewardDatas)
        {
            List<DayItemData> result = new List<DayItemData>();
            DayItemData dayItemData = null;
            RewardItem rewardItem = null; ;
            int count = rewardDatas.Length;

            for (int i = 0; i < count; ++i)
            {
                rewardItem = convertRewardItem(rewardDatas[i], isNeedExchange);
                dayItemData = new DayItemData();
                dayItemData.parseItemType(rewardItem);
                result.Add(dayItemData);
            }

            return result;
        }

        RewardItem convertRewardItem(MissionReward missionReward, bool isNeedExchange)
        {
            return new RewardItem()
            {
                kind = missionReward.kind,
                type = missionReward.type,
                amount = getRewardAmount(missionReward, isNeedExchange),
                level = 1
            };
        }

        ulong getRewardAmount(MissionReward missionReward, bool isNeedExchange)
        {
            if (isNeedExchange && missionReward.kind.Contains("coin"))
            {
                return (ulong)(missionReward.amount * DataStore.getInstance.playerInfo.coinExchangeRate);
            }

            return (ulong)missionReward.amount;
        }

        void showRewardInfo(RectTransform info, RectTransform itemParent)
        {
            info.gameObject.setActiveWhenChange(true);
            infoTapBtn.gameObject.setActiveWhenChange(true);
            LayoutRebuilder.ForceRebuildLayoutImmediate(info);
            showingInfo = new ShowingRewardInfo(info, itemParent);
        }

        void releaseRewardInfoItem(RectTransform info)
        {
            int count = info.childCount;
            GameObject temp = null;

            for (int i = 0; i < count; ++i)
            {
                temp = info.GetChild(0).gameObject;
                ResourceManager.instance.returnObjectToPool(temp);
            }
        }


        class ShowingRewardInfo
        {
            public RectTransform infoRect { get; private set; }
            public RectTransform itemParent { get; private set; }

            public ShowingRewardInfo(RectTransform infoRect, RectTransform itemParent)
            {
                this.infoRect = infoRect;
                this.itemParent = itemParent;
            }
        }

        class TitleImage
        {
            Image mainImage;
            Image effectImage;

            public TitleImage(Presenter presenter, string bindingID)
            {
                mainImage = presenter.getImageData(bindingID);
                effectImage = presenter.getImageData($"{bindingID}_effect");
            }

            public void setSprite(string spriteName, string effectFlag = "")
            {
                mainImage.sprite = LobbySpriteProvider.instance.getSprite<DailyMissionProvider>(LobbySpriteType.Mission, spriteName);
                effectImage.sprite = LobbySpriteProvider.instance.getSprite<DailyMissionProvider>(LobbySpriteType.Mission, getEffectImageName(spriteName, effectFlag));
                mainImage.SetNativeSize();
                effectImage.SetNativeSize();
            }

            string getEffectImageName(string spriteName, string effectFlag)
            {
                return (string.IsNullOrEmpty(effectFlag)) ? spriteName : $"{spriteName}_{effectFlag}";
            }
        }
    }

    class TitleMedalNode : MissionMedalNode
    {
        public void addMedalFlyObj(MissionMedalNode flyReward)
        {
            flyReward.uiRectTransform.SetParent(uiRectTransform.parent.transform);
            flyReward.setReward(rewardAmount);
            flyReward.uiRectTransform.anchoredPosition = uiRectTransform.anchoredPosition;
            flyReward.uiRectTransform.localScale = Vector3.one;
            close();
        }
    }

    class MissionMedalNode : NodePresenter
    {
        Animator medalAnim;
        Text rewradTxt;
        public long rewardAmount;
        IDisposable medalAnimDis;
        Action getAnimCallback;
        public override void initUIs()
        {
            medalAnim = getAnimatorData("medal_anim");
            rewradTxt = getTextData("medal_reward");
        }

        public void playGetAnim(Action callback = null)
        {
            getAnimCallback = callback;
            var animtrigger = medalAnim.GetBehaviour<ObservableStateMachineTrigger>();
            medalAnimDis = animtrigger.OnStateEnterAsObservable().ObserveOnMainThread().Subscribe(getAnim).AddTo(uiGameObject);
            medalAnim.SetTrigger("get");
        }

        void getAnim(ObservableStateMachineTrigger.OnStateInfo obj)
        {
            IDisposable barEffectAnimTimerDis = null;
            barEffectAnimTimerDis = Observable.Timer(TimeSpan.FromSeconds(obj.StateInfo.length)).Subscribe(_ =>
            {
                if (null != getAnimCallback)
                {
                    getAnimCallback();
                }
                UtilServices.disposeSubscribes(medalAnimDis);
            }).AddTo(uiGameObject);
        }

        public void setReward(long reward)
        {
            rewardAmount = reward;
            rewradTxt.text = reward.ToString();
            open();
        }
    }

    public enum MissionRewardType
    {
        Normal,
        Special,
    }
}
