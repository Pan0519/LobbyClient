using CommonILRuntime.Module;
using CommonILRuntime.BindingModule;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Service;
using Services;
using UniRx;
using LobbyLogic.NetWork.ResponseStruct;
using Lobby.Common;
using UniRx.Triggers;
using CommonService;
using LobbyLogic.Common;
using LitJson;

namespace EventActivity
{
    class EventActivityBarPresenter : ContainerPresenter
    {
        public override string objPath { get { return "prefab/game/activity_bar_common_all"; } }
        public override UiLayer uiLayer { get { return UiLayer.BarRoot; } }

        public GameOrientation gameOrientation;

        Animator barAnim;
        ShrinkNodePresenter shrinkNode;
        PortraitBarNode barNode;
        Button tapBtn;
        RectTransform tapBtnRect;
        MoveWithMouse moveWithMouse;
        Image moveAreaImg;
        string orientationTriggerName;
        #region ChangeTapBtnSizeData
        float totalHeight;
        float shrinkHeight;
        float closeBtnHeight;
        float activityBtnHalfHeight;

        float leftMargin;
        float rightMargin;
        float extendTopMargin;
        float extendBottomMargin;
        float shrinkTopMargin;
        float shrinkBottomMargin;
        RectTransform barRootRect;
        #endregion

        int nowTicketCount;
        public override void initUIs()
        {
            barAnim = getAnimatorData("bar_show_anim");
            tapBtn = getBtnData("tap_btn");
            tapBtnRect = tapBtn.GetComponent<RectTransform>();
            shrinkNode = UiManager.bindNode<ShrinkNodePresenter>(getNodeData("shrink_node").cachedGameObject);
            barNode = UiManager.bindNode<PortraitBarNode>(getNodeData("extend_node").cachedGameObject);
            moveAreaImg = tapBtn.gameObject.GetComponent<Image>();
        }

        public override async void init()
        {
            ActivityDataStore.activityCloseSub.Subscribe(resetGameOritien).AddTo(uiGameObject);
            ActivityDataStore.pageAmountChangedSub.Subscribe(nowTicketAmountChange).AddTo(uiGameObject);
            ActivityDataStore.isEndSub.Subscribe(activityComplete).AddTo(uiGameObject);
            ActivityDataStore.isEndErrorSub.Subscribe(activityComplete).AddTo(uiGameObject);
            gameOrientation = await DataStore.getInstance.dataInfo.getNowGameOrientation();
            if (GameOrientation.Portrait == gameOrientation)
            {
                moveWithMouse = uiGameObject.AddComponent<MoveWithMouse>();
            }
            orientationTriggerName = gameOrientation.ToString().ToLower();
            barRootRect = uiBarRoot.transform as RectTransform;
            initActivityData();
            tapBtn.onClick.AddListener(playExtendAnim);
            //tapBtn.onClick.AddListener(shrinkNode.openRewardTipPage);
            calculateMoveRange();
            tempSetting();
        }

        void nowTicketAmountChange(int ticketCount)
        {
            nowTicketCount = ticketCount;
        }

        void activityComplete(bool isComplete)
        {
            if (!isComplete)
            {
                return;
            }
            shrinkNode.resetActivityObjData();
            barNode.resetActivityObjData();
            initActivityData();
        }

        async void initActivityData()
        {
            var actRes = await AppManager.lobbyServer.getActivity();
            if (null == actRes.activity)
            {
                close();
                return;
            }
            ActivityDataStore.nowActivityInfo = actRes.activity;

            if (ActivityID.None == ActivityDataStore.getNowActivityID())
            {
                Debug.LogError($"get {actRes.activity.activityId} activity is empty return");
                close();
                return;
            }
            await AppManager.eventServer.getBaseActivityInfo();
            var propResponse = await AppManager.lobbyServer.getActivityProp();
            barNode.initData(actRes, propResponse);
            barNode.gameOrientation = gameOrientation;
            barNode.animToShrink = playShrinkAnim;
            shrinkNode.initData(actRes, propResponse);
        }

        async void resetGameOritien(bool isActivityClose)
        {
            barNode.setBarBtnInteractable(true);
            if (GameOrientation.Portrait == gameOrientation)
            {
                await UIRootChangeScreenServices.Instance.justChangeScreenToProp();
            }
            barNode.activityPageAmountChanged(nowTicketCount);
            shrinkNode.activityPageAmountChanged(nowTicketCount);
            GamePauseManager.gameResume();
        }

        const int topBarHight = 150;
        const int extendBottomBarHigth = 160;
        const float shrinkPivotY = 0.2f;
        const float extendPivotY = 0.5f;

        void calculateMoveRange()
        {
            initTapBtnSizeData();

            float rootHalfWidth = barRootRect.rect.width * 0.5f;
            float rootHalfHeight = barRootRect.rect.height * 0.5f;
            float objHalfWidth = uiRectTransform.rect.width * 0.5f;
            float objHalfHeight = uiRectTransform.rect.height * 0.5f * uiRectTransform.localScale.y;
            float shrinkObjHalfHeight = shrinkNode.uiRectTransform.rect.height * uiRectTransform.localScale.y;

            rightMargin = rootHalfWidth - objHalfWidth;
            leftMargin = rightMargin * -1;
            extendTopMargin = rootHalfHeight - objHalfHeight;
            extendBottomMargin = (extendTopMargin - extendBottomBarHigth) * -1;
            shrinkBottomMargin = extendBottomMargin + (extendBottomMargin * 0.5f);
            extendTopMargin -= topBarHight;
            shrinkTopMargin = rootHalfHeight - shrinkObjHalfHeight - (topBarHight * shrinkPivotY);
        }

        public override async void open()
        {
            await Task.Delay(TimeSpan.FromSeconds(0.5f));
            playShrinkAnim();
            float initPosy = 0;
            float initPosX = leftMargin;
            if (GameOrientation.Portrait == gameOrientation)
            {
                initPosy = shrinkTopMargin;
            }
            else
            {
                initPosy = shrinkNode.uiRectTransform.rect.height * 0.1f * -1;
                if (UtilServices.screenProportion <= 0.53f)
                {
                    initPosX += 50;
                }
            }
            uiRectTransform.anchoredPosition3D = new Vector3(initPosX, initPosy, 0);
            base.open();
        }

        void initTapBtnSizeData()
        {
            totalHeight = uiRectTransform.rect.height;
            shrinkHeight = shrinkNode.uiRectTransform.rect.height;
            closeBtnHeight = barNode.toShrinkBtnHeight;
            activityBtnHalfHeight = barNode.activityBtnHalfHeight;
        }
        void playShrinkAnim()
        {
            changePivot(shrinkPivotY);
            tapBtn.interactable = true;
            moveAreaImg.raycastTarget = true;
            barAnim.SetTrigger($"shrink_{orientationTriggerName}");
            tapBtnSetOffset(shrinkHeight - totalHeight, 0);
            setMoveRange(shrinkTopMargin, shrinkBottomMargin);
            shrinkNode.isNodeOpening = true;
            barNode.isNodeOpening = false;
        }
        void playExtendAnim()
        {
            changePivot(extendPivotY);
            if (uiRectTransform.anchoredPosition.y > extendTopMargin)
            {
                uiRectTransform.anchPosMoveY(extendTopMargin, 0.2f);
            }
            tapBtn.interactable = false;
            moveAreaImg.raycastTarget = false;
            barAnim.SetTrigger($"extend_{orientationTriggerName}");
            tapBtnSetOffset(activityBtnHalfHeight * -1, closeBtnHeight + 10);
            setMoveRange(extendTopMargin, extendBottomMargin);
            shrinkNode.isNodeOpening = false;
            barNode.isNodeOpening = true;
        }
        void changePivot(float pointY)
        {
            if (GameOrientation.Landscape == gameOrientation)
            {
                return;
            }
            var point = uiRectTransform.pivot;
            point.Set(point.x, pointY);
            Vector3 deltapos = uiRectTransform.pivot - point;
            deltapos.Scale(uiRectTransform.rect.size);
            deltapos.Scale(uiRectTransform.localScale);
            deltapos = uiRectTransform.rotation * deltapos;

            uiRectTransform.pivot = point;
            uiRectTransform.localPosition -= deltapos;
        }
        void setMoveRange(float topRange, float bottomRange)
        {
            if (null == moveWithMouse)
            {
                return;
            }
            moveWithMouse.setPosRange(leftMargin, rightMargin, topRange, bottomRange);
        }
        void tapBtnSetOffset(float top, float bottom)
        {
            Vector2 setPos = tapBtnRect.offsetMin;
            setPos.Set(0, bottom);
            tapBtnRect.offsetMin = setPos;

            setPos = tapBtnRect.offsetMax;
            setPos.Set(0, top);
            tapBtnRect.offsetMax = setPos;
        }

        void tempSetting()
        {
            shrinkNode.closeImgCast();
            var tempPos = uiRectTransform.anchoredPosition;
            tempPos.Set(uiRectTransform.rect.width - (Screen.width / 2), (Screen.height / 2) - (uiRectTransform.rect.height / 2) - 157);
            uiRectTransform.anchoredPosition = tempPos;
        }

        public void clearSubscribe()
        {
            shrinkNode.clearSubscribe();
            barNode.clearSubscribe();
        }
    }

    class PortraitBarNode : BarNodePresenter
    {
        Button toShrinkBtn;
        public Action animToShrink { private get; set; }

        float closeBtnHeight = 0;
        public float toShrinkBtnHeight
        {
            get
            {
                if (closeBtnHeight <= 0)
                {
                    closeBtnHeight = toShrinkBtn.GetComponent<RectTransform>().rect.height;
                }
                return closeBtnHeight;
            }
        }

        public override void initUIs()
        {
            base.initUIs();
            toShrinkBtn = getBtnData("off_btn");
        }

        public override void init()
        {
            base.init();
            toShrinkBtn.onClick.AddListener(toShrinkBtnClick);
        }

        void toShrinkBtnClick()
        {
            if (null == animToShrink)
            {
                return;
            }
            animToShrink();
        }
    }

    class BarNodePresenter : ShrinkNodePresenter
    {
        #region [ UIs ]
        RectTransform barEffectRect;
        Image boosterImg;
        Image commonImg;
        GameObject commonObj;
        GameObject rookieObj;
        CustomTextSizeChange commonCountTxt;
        Text timesTxt;
        Text boosterTimeTxt;
        Button barBtn;
        #endregion
        TimerService activityTimerService;

        Dictionary<ActivityID, Action> boosterUpdateDict = new Dictionary<ActivityID, Action>();

        public override void initUIs()
        {
            base.initUIs();
            commonCountTxt = getBindingData<CustomTextSizeChange>("common_count_txt");
            timesTxt = getTextData("times_txt");
            rookieObj = getGameObjectData("rookie_obj");
            commonObj = getGameObjectData("common_obj");
            commonImg = getImageData("common_icon");
            boosterImg = getImageData("booster_img");
            boosterTimeTxt = getTextData("booster_time_txt");
            barEffectRect = getBindingData<RectTransform>("bar_effect_rect");
            barBtn = getBtnData("bar_btn");
        }

        public override void init()
        {
            base.init();
            barEffectRect.gameObject.setActiveWhenChange(false);
            rookieObj.setActiveWhenChange(true);
            commonObj.setActiveWhenChange(true);
            showBarEffect.Subscribe(showBarEffectSub).AddTo(uiGameObject);
            barBtn.onClick.AddListener(openActivityPage);
        }

        public void setBarBtnInteractable(bool interactable)
        {
            barBtn.interactable = interactable;
        }

        public override async void openActivityPage()
        {
            setBarBtnInteractable(false);
            base.openActivityPage();
            await Task.Delay(TimeSpan.FromSeconds(1.0f));
            setBarBtnInteractable(true);
        }

        public override void initDataFinish()
        {
            initBoosterEvent();
            rookieObj.setActiveWhenChange(nowActivityID == ActivityID.Rookie);
            commonObj.setActiveWhenChange(nowActivityID != ActivityID.Rookie);

            checkCountdownTime(actRes.activity.endAt);

            if (nowActivityID != ActivityID.Rookie)
            {
                commonCountTxt.text = propResponse.prop.amount.ToString();
                commonImg.sprite = getActivityItemImg();
                boosterImg.sprite = getIconSprite($"activity_{nowActivityObjData.boosterSpriteName}_small");
                Action boosterUpdate = null;
                bool isGetBoosterUpdate = boosterUpdateDict.TryGetValue(nowActivityID, out boosterUpdate);
                if (isGetBoosterUpdate)
                {
                    boosterUpdate();
                }
            }
        }

        void initBoosterEvent()
        {
            boosterAddData(ActivityID.FarmBlast, fbBooster);
            boosterAddData(ActivityID.FrenzyJourney, fjBooster);
            boosterAddData(ActivityID.MagicForest, mfBooster);
        }

        void boosterAddData(ActivityID activityID, Action cb)
        {
            if (boosterUpdateDict.ContainsKey(activityID))
            {
                return;
            }
            boosterUpdateDict.Add(activityID, cb);
        }

        void checkCountdownTime(string endAt)
        {
            TimeStruct timeStruct = UtilServices.toTimeStruct(UtilServices.getEndTimeStruct(endAt));
            timesTxt.text = timeStruct.toTimeString(LanguageService.instance.getLanguageValue("Time_Days"));

            if (timeStruct.days > 0)
            {
                return;
            }

            activityTimerService = new TimerService();
            activityTimerService.setAddToGo(uiGameObject);
            DateTime endDateTime = UtilServices.strConvertToDateTime(endAt, DateTime.MinValue);
            activityTimerService.StartTimer(endDateTime, updateActivityTime);
        }

        void updateActivityTime(TimeSpan expTime)
        {
            timesTxt.text = UtilServices.formatCountTimeSpan(expTime);
        }

        public override void setNowAmount()
        {
            base.setNowAmount();
            commonCountTxt.text = nowTicketAmount.ToString();
        }

        public override void updateBarFillAmount(float progress)
        {
            updataBarEffectPos(progress);
            base.updateBarFillAmount(progress);
        }

        void updataBarEffectPos(float progress)
        {
            float endY = (progress - 1) * barEffectPosY;
            var changePos = barEffectRect.anchoredPosition;
            changePos.Set(0, (float)Math.Round(endY + 10, 2));
            barEffectRect.anchoredPosition = changePos;
        }

        void showBarEffectSub(bool isShow)
        {
            barEffectRect.gameObject.setActiveWhenChange(isShow);
        }

        #region boosterData
        TimerService boosterTimeService = new TimerService();
        void updateTimerTxt(long endTime)
        {
            long nowTime = UtilServices.nowUtcTimeSeconds;
            boosterTimeService.ExecuteTimer();
            if (endTime <= 0 || nowTime > endTime)
            {
                boosterTimeTxt.text = TimeSpan.Zero.ToString();
                return;
            }

            boosterTimeService.StartTimeByTimestamp(endTime, updateTimer);
        }
        void updateTimer(TimeSpan time)
        {
            if (time <= TimeSpan.Zero)
            {
                updateTimerTxt(TimeSpan.Zero.Ticks);
                return;
            }

            boosterTimeTxt.text = UtilServices.formatCountTimeSpan(time);
        }
        void updateTimeTxt(long count)
        {
            boosterTimeTxt.text = count.ToString();
        }
        async void fbBooster()
        {
            var fbInitResponse = await AppManager.eventServer.getAppleFarmInitData();
            updateTimerTxt(fbInitResponse.BoostsData.SpinBoost);
            FarmBlast.FarmBlastDataManager.getInstance.boostDataUpdateSub.Subscribe(boosterData =>
            {
                updateTimerTxt(boosterData.SpinBoost);
            }).AddTo(uiGameObject);
        }
        async void fjBooster()
        {
            var fjInitResponse = await AppManager.eventServer.getFrenzyJourneyData();
            updateTimerTxt(fjInitResponse.BoostsData.DiceBoost);
            FrenzyJourney.FrenzyJourneyData.getInstance.updateBoostDataSub.Subscribe(boosterData =>
            {
                updateTimerTxt(boosterData.DiceBoost);
            }).AddTo(uiGameObject);
        }
        async void mfBooster()
        {
            var mfInitResponse = await AppManager.eventServer.getForestInitData();
            updateTimeTxt(mfInitResponse.BoostsData.GoldenMallet);
            MagicForest.ForestDataServices.updateBoosterSub.Subscribe(boosterData =>
            {
                updateTimerTxt(boosterData.GoldenMallet);
            }).AddTo(uiGameObject);
        }
        #endregion
    }

    class ShrinkNodePresenter : NodePresenter
    {
        #region UIs
        Image iconImg;

        CustomTextSizeChange ticketCountTxt;
        Image barImg;
        Text barProgressTxt;
        Animator barEffectAnim;
        RectTransform barRect;
        #endregion
        public GetActivityResponse actRes { get; private set; }
        public ActivityPropResponse propResponse { get; private set; }
        public float barEffectPosY { get; private set; }
        public int nowTicketAmount { get; private set; }
        public GameOrientation gameOrientation;
        public Subject<bool> showBarEffect = new Subject<bool>();
        public bool isNodeOpening;
        bool isAlwaysNotify;
        bool isShowMaxAmount;
        IDisposable wagersPropsDis;
        int maxTicketAmount;
        public ActivityID nowActivityID { get; private set; }
        ActivityIconData _nowActivityObjData;
        public ActivityIconData nowActivityObjData
        {
            get
            {
                if (null == _nowActivityObjData)
                {
                    EventBarDataConfig.activityObjData.TryGetValue(nowActivityID, out _nowActivityObjData);
                }
                return _nowActivityObjData;
            }
        }

        float _activityBtnHalfHeight = 0;
        public float activityBtnHalfHeight
        {
            get
            {
                if (_activityBtnHalfHeight <= 0)
                {
                    _activityBtnHalfHeight = iconImg.GetComponent<RectTransform>().rect.height * 0.5f;
                }
                return _activityBtnHalfHeight;
            }
        }

        RewardTipData rewardTipData = null;
        public override void initUIs()
        {
            iconImg = getImageData("bar_activity_img");
            ticketCountTxt = getBindingData<CustomTextSizeChange>("ticket_count_txt");
            barRect = getBindingData<RectTransform>("bar_rect");
            barEffectAnim = getAnimatorData("bar_anim");
            barImg = getImageData("bar_img");
            barProgressTxt = getTextData("bar_progress_txt");
        }

        public override void init()
        {
            barEffectPosY = barImg.GetComponent<RectTransform>().rect.height;
            resetActivityObjData();
            wagersPropsDis = FromGameMsgService.getInstance.props.Subscribe(wagersProps);
            checkActivityNotify();
        }

        public virtual async void openActivityPage()
        {
            GamePauseManager.gamePause();
            switch (UtilServices.getNowScreenOrientation)
            {
                case ScreenOrientation.Portrait:
                case ScreenOrientation.PortraitUpsideDown:
                    await UIRootChangeScreenServices.Instance.justChangeScreenToLand();
                    break;
            }

            ActivityPageData.instance.openActivityPage(nowActivityID);
        }

        public void closeImgCast()
        {
            iconImg.raycastTarget = false;
        }

        public void resetActivityObjData()
        {
            _nowActivityObjData = null;
        }

        public void initData(GetActivityResponse activityRes, ActivityPropResponse propRes)
        {
            actRes = activityRes;
            nowActivityID = ActivityDataStore.getNowActivityID();
            iconImg.sprite = getActivityIconImg();
            propResponse = propRes;

            nowTicketAmount = propResponse.prop.amount;
            maxTicketAmount = propResponse.prop.maximum;
            setNowAmount();

            if (propResponse.prop.amount >= maxTicketAmount)
            {
                showMaxEffect();
            }
            else
            {
                barImg.fillAmount = propResponse.prop.percentage * 0.01f;
                setProgressTxt();
            }

            uiRectTransform.SetAsFirstSibling();
            initDataFinish();
            open();
        }

        public virtual void initDataFinish()
        {

        }

        Sprite getActivityIconImg()
        {
            return getIconSprite($"bar_icon_{nowActivityObjData.spriteName}");
        }

        public Sprite getActivityItemImg()
        {
            return getIconSprite($"reward_{nowActivityObjData.rewardSpriteName}");
        }
        public Sprite getIconSprite(string name)
        {
            return LobbySpriteProvider.instance.getSprite<EventActivitySpriteProvider>(LobbySpriteType.EventActivity, name);
        }

        public void showMaxEffect()
        {
            isShowMaxAmount = true;
            barImg.fillAmount = 1;
            barProgressTxt.text = string.Empty;
            barEffectAnim.ResetTrigger("get");
            barEffectAnim.SetTrigger("max");
        }

        void wagersProps(Props props)
        {
            showFlyObj();
            progress = props.percentage * 0.01f;

            if (null == props.outcome)
            {
                return;
            }
            Dictionary<string, object> bagDict;
            if (props.outcome.TryGetValue("bag", out bagDict))
            {
                nowTicketAmount = (int)bagDict["amount"];
            }
        }

        List<PoolObject> flyObjs = new List<PoolObject>();
        Queue<PoolObject> flyShowObjs = new Queue<PoolObject>();
        List<IDisposable> flyTimer = new List<IDisposable>();
        void showFlyObj()
        {
            if (flyObjs.Count <= 0)
            {
                for (int i = 0; i < 5; ++i)
                {
                    var obj = ResourceManager.instance.getObjectFromPool(nowActivityObjData.prefabPath, uiRectTransform);
                    resetFlyObjPos(obj);
                    flyObjs.Add(obj);
                }
            }

            IDisposable showFlyObjDis = null;

            showFlyObjDis = Observable.Timer(TimeSpan.Zero, TimeSpan.FromSeconds(0.1f)).Subscribe(repeatCount =>
            {
                PoolObject flyObj = flyObjs[(int)repeatCount];
                switch (nowActivityID)
                {
                    case ActivityID.FarmBlast:
                    case ActivityID.Rookie:
                        TicketItemNodePresenter ticketItem = UiManager.bindNode<TicketItemNodePresenter>(flyObj.cachedGameObject);
                        ticketItem.open();
                        break;

                    default:
                        flyObj.cachedGameObject.setActiveWhenChange(true);
                        break;
                }
                flyObj.cachedTransform.movePos(barRect.position, 0.8f, onComplete: flyFinish, easeType: DG.Tweening.Ease.OutSine);
                flyShowObjs.Enqueue(flyObj);
                if (repeatCount >= flyObjs.Count - 1)
                {
                    showFlyObjDis.Dispose();
                }
            });
        }

        void flyFinish()
        {
            var showObj = flyShowObjs.Dequeue();
            showObj.cachedGameObject.setActiveWhenChange(false);
            resetFlyObjPos(showObj);
            showObj.cachedRectTransform.localScale = Vector3.one;

            if (flyShowObjs.Count <= 0)
            {
                changeBarProgress();
                UtilServices.disposeSubscribes(flyTimer.ToArray());
                flyTimer.Clear();
            }
        }

        void resetFlyObjPos(PoolObject flyTicketObj)
        {
            flyTicketObj.cachedTransform.position = Vector3.zero;
            var anchPos = flyTicketObj.cachedRectTransform.anchoredPosition3D;
            anchPos.Set(anchPos.x, 0, 0);
            flyTicketObj.cachedRectTransform.anchoredPosition3D = anchPos;
        }

        float progress;
        IDisposable animtriggerDis;
        public void changeBarProgress()
        {
            var animTrigger = barEffectAnim.GetBehaviour<ObservableStateMachineTrigger>();
            animtriggerDis = animTrigger.OnStateEnterAsObservable().ObserveOnMainThread().Subscribe(barEffectAnimTrigger).AddTo(uiGameObject);
            setGetTrigger();
        }

        void barEffectAnimTrigger(ObservableStateMachineTrigger.OnStateInfo obj)
        {
            animtriggerDis.Dispose();
            IDisposable animTimerDis = null;
            animTimerDis = Observable.Timer(TimeSpan.FromSeconds(obj.StateInfo.length)).Subscribe(_ =>
            {
                showBarEffect.OnNext(true);
                string twID = TweenManager.tweenToFloat(barImg.fillAmount, progress, 1f, onUpdate: updateBarFillAmount, onComplete: fillAmountComplete);
                TweenManager.tweenPlay(twID);
                animTimerDis.Dispose();
            });
        }

        public virtual void updateBarFillAmount(float progress)
        {
            barImg.fillAmount = progress;
            setProgressTxt();
        }

        void setProgressTxt()
        {
            barProgressTxt.text = $"{(int)(barImg.fillAmount * 100)}%";
        }

        public virtual void setNowAmount()
        {
            ticketCountTxt.text = nowTicketAmount.ToString();
        }

        void fillAmountComplete()
        {
            showBarEffect.OnNext(false);
            if (barImg.fillAmount < 1)
            {
                return;
            }

            openRewardTipPage();
            setNowAmount();
            if (nowTicketAmount >= maxTicketAmount)
            {
                showMaxEffect();
                return;
            }
            barImg.fillAmount = 0;
            setProgressTxt();
        }

        void openRewardTipPage()
        {
            if (!isNodeOpening)
            {
                return;
            }

            if (!isAlwaysNotify && nowTicketAmount < maxTicketAmount)
            {
                return;
            }

            ActivityRewardTipPresenter rewardTipPresenter = UiManager.getPresenter<ActivityRewardTipPresenter>();
            rewardTipPresenter.openActivityPage = openActivityPage;
            rewardTipPresenter.notifyToggleSub.Subscribe(isNotifyChangedValue).AddTo(uiGameObject);
            rewardTipData = new RewardTipData()
            {
                iconSprite = getActivityIconImg(),
                itemSprite = getActivityItemImg(),
                amount = nowTicketAmount,
                maxAmount = maxTicketAmount,
            };

            rewardTipPresenter.openTipPage(rewardTipData, isAlwaysNotify);
        }

        public void activityPageAmountChanged(int amount)
        {
            nowTicketAmount = amount;
            setNowAmount();

            if (nowTicketAmount >= maxTicketAmount)
            {
                showMaxEffect();
                return;
            }

            if (amount < maxTicketAmount && isShowMaxAmount)
            {
                isShowMaxAmount = false;
                progress = 0;
                setGetTrigger();
                updateBarFillAmount(progress);
            }
        }

        void setGetTrigger()
        {
            barEffectAnim.ResetTrigger("max");
            barEffectAnim.SetTrigger("get");
        }

        #region NotifyChangeSave
        const string notifyChangedKey = "ActivityNotifyChange";
        void checkActivityNotify()
        {
            var notifyChangeData = getNotifyChangedData();
            if (null == notifyChangeData || (ActivityID)notifyChangeData.activityID != nowActivityID)
            {
                isAlwaysNotify = true;
                setDefaultNotifyChangedData();
                return;
            }
            isAlwaysNotify = notifyChangeData.isNotify == 0;
        }

        void isNotifyChangedValue(bool isOn)
        {
            isAlwaysNotify = isOn;
            ActivityNotifyChange notifyChangeData = getNotifyChangedData();
            if (null != notifyChangeData)
            {
                notifyChangeData.isNotify = isOn ? 0 : 1;
            }
            PlayerPrefs.SetString(notifyChangedKey, JsonMapper.ToJson(notifyChangeData));
        }

        void setDefaultNotifyChangedData()
        {
            ActivityNotifyChange changed = new ActivityNotifyChange()
            {
                activityID = (int)nowActivityID,
                isNotify = 0,
            };
            PlayerPrefs.SetString(notifyChangedKey, JsonMapper.ToJson(changed));
        }

        ActivityNotifyChange getNotifyChangedData()
        {
            if (PlayerPrefs.HasKey(notifyChangedKey))
            {
                ActivityNotifyChange notifyChange = JsonMapper.ToObject<ActivityNotifyChange>(PlayerPrefs.GetString(notifyChangedKey));
                return notifyChange;
            }
            return null;
        }
        #endregion
        public void clearSubscribe()
        {
            UtilServices.disposeSubscribes(wagersPropsDis);
            ResourceManager.instance.releasePoolWithObj(flyObjs[0].cachedGameObject);
        }
    }

    class ActivityNotifyChange
    {
        public int activityID;
        public int isNotify;
    }

    class TicketItemNodePresenter : NodePresenter
    {
        Animator itemAnim;
        GameObject amountGroup;
        public override void initUIs()
        {
            itemAnim = getAnimatorData("ticket_anim");
            amountGroup = getGameObjectData("amount_group_obj");
        }

        public override void open()
        {
            itemAnim.enabled = false;
            amountGroup.setActiveWhenChange(false);
            base.open();
        }
    }
}



