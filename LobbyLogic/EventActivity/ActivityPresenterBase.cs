using CommonILRuntime.Module;
using CommonILRuntime.BindingModule;
using UnityEngine.UI;
using UnityEngine;
using Binding;
using System.Threading.Tasks;
using LitJson;
using Service;
using LobbyLogic.NetWork.ResponseStruct;
using System.Collections.Generic;
using System;
using System.IO;
using Services;
using Network;
using UniRx;
using UniRx.Triggers;
using CommonService;
using Event.Common;
using CommonPresenter;
using LobbyLogic.Audio;
using Lobby.Audio;
using Lobby.UI;
using Debug = UnityLogUtility.Debug;

namespace EventActivity
{
    public class ActivityPresenterBase : SystemUIBasePresenter, IActivityPage
    {
        public override UiLayer uiLayer { get { return UiLayer.GameMessage; } }

        #region UIs
        Button btn_Close;

        BindingNode defaultPickItem;
        RectTransform itemRoot;

        Text ticketCountTxt;
        Text text_Finish_Level_Award;
        Image collectProgress;
        Text text_FarmCollect_Target_count;

        Image lvProgressImg;
        Text text_Finished_Level_Count;
        GameObject obj_BuffPercent;
        Text text_BuffPercent;

        GameObject objInfo;
        Button btn_OpenInfo;
        Button btn_CloseInfo;

        GameObject lvupObj;
        Text lvupProgressTxt;

        Image tutorialCollectProgressImg;
        Text tutorialCollectProgressTxt;
        Image tutorialLvProgressImg;
        Text tutorialLvProgressTxt;

        Animator ticketAnim;
        Animator buffMoreAnim;
        Animator collectAnim;
        Animator lvupAnim;
        Animator infoAnim;
        RectTransform barEffectRect;
        Animator barEffectAnim;
        RectTransform completeLayoutRect;

        CanvasGroup itemCanvaseGroup;
        #endregion

        //public virtual string ticketID { get; set; } = string.Empty;//道具名稱
        public virtual string[] iconSpriteNames { get; set; } = new string[] { };
        public virtual string iconSpriteStartName { get; set; } = string.Empty;
        public virtual string jsonFileName { get; set; } = string.Empty;
        public virtual int totalLvCount { get; set; } = 0;
        public int pickItemFinishFrame { get { return 45; } }
        readonly int playIndex = 0;
        public long ticketCount { get; private set; }

        public ActivityAwardData awardData { get; private set; } = new ActivityAwardData();
        public RookieInitActivityResponse baseInitActivityResponse { get; private set; }
        public PoolObject awardObj { get; private set; }
        public bool isShowRunning { get; private set; }

        SendSelectBaseResponse clickItemResponse;
        RookieInitActivityResponse selectRefreshData;
        RookieLevelSetting leveldata;
        RookieLevelInfo nowLevelInfo;
        ulong nowBuffPercent;
        AudioSource bgmAudioSource;
        string refreshJsonData { get; set; }
        Activity activityInfo { get; set; }
        int progressTarget;
        int nowProgress;
        ulong levelAward;
        ulong lastLvAward;
        PickItemPresenter selectItem;

        Dictionary<int, PickItemPresenter> pickItems = new Dictionary<int, PickItemPresenter>();
        List<PickItemPresenter> showPickItems = new List<PickItemPresenter>();

        Dictionary<AwardKind, Transform> flyObjTargets = new Dictionary<AwardKind, Transform>();
        Dictionary<AwardKind, Action> flyFinishCB = new Dictionary<AwardKind, Action>();

        List<IDisposable> shakeTimerDis = new List<IDisposable>();
        float barEffectPosX;
        const float surplusCollectBarAmount = 10;
        public bool isRefreshItemData { get; private set; }
        bool isGameEnd;
        #region init
        public override void initUIs()
        {
            flyObjTargets.Add(AwardKind.Ticket, getBindingData<Transform>("trans_FarmTicket"));
            flyObjTargets.Add(AwardKind.BuffMore, getBindingData<Transform>("trans_FarmMore"));
            flyObjTargets.Add(AwardKind.CollectTarget, getBindingData<Transform>("trans_FarmCollect"));

            ticketCountTxt = getTextData("text_Ticket_Count");
            text_Finish_Level_Award = getTextData("text_Finish_Level_Award");
            collectProgress = getImageData("img_Collection_Progress");
            text_FarmCollect_Target_count = getTextData("text_FarmCollect_Target_count");
            lvProgressImg = getImageData("img_Level_Progress");
            text_Finished_Level_Count = getTextData("text_Finished_Level_Count");

            obj_BuffPercent = getGameObjectData("obj_BuffPercent");
            text_BuffPercent = getTextData("text_BuffPercent");

            defaultPickItem = getNodeData("pick_item_node");
            defaultPickItem.name = $"{ActivityDataStore.getNowActivityID()}_DefaultPackItem";
            itemRoot = getBindingData<RectTransform>("item_group");
            btn_Close = getBtnData("btn_CloseFarm");

            objInfo = getGameObjectData("obj_Info");
            btn_OpenInfo = getBtnData("btn_OpenFarmInfo");
            btn_CloseInfo = getBtnData("btn_CloseInfo");

            lvupObj = getGameObjectData("levelup_obj");
            lvupProgressTxt = getTextData("levelup_progress_txt");
            lvupAnim = getAnimatorData("levelup_anim");
            itemCanvaseGroup = getBindingData<CanvasGroup>("item_canvasgroup");

            tutorialCollectProgressTxt = getTextData("tutorial_collect_progress_txt");
            tutorialCollectProgressImg = getImageData("tutorial_collect_progress_img");
            tutorialLvProgressTxt = getTextData("tutoria_lv_progress_txt");
            tutorialLvProgressImg = getImageData("tutoria_lv_progress_img");

            ticketAnim = getAnimatorData("anim_ticket");
            buffMoreAnim = getAnimatorData("anim_more");
            collectAnim = getAnimatorData("anim_collect");
            completeLayoutRect = getBindingData<RectTransform>("info_horizontal_layout");

            barEffectRect = getBindingData<RectTransform>("bar_effect_rect");
            barEffectAnim = getAnimatorData("bar_effect_anim");

            for (int i = 0; i < objTutorialsStep.Length; ++i)
            {
                GameObject stepObj = getGameObjectData($"obj_Tutorials_Step_{i + 1}");
                stepObj.setActiveWhenChange(true);
                objTutorialsStep[i] = stepObj;
                stepObj.setActiveWhenChange(false);
                if (i <= 0)
                {
                    continue;
                }
                getBtnData($"btn_Tutorials_Step_{i + 1}").onClick.AddListener(tutoralClick);
            }
        }
        public override void init()
        {
            text_Finish_Level_Award.text = string.Empty;
            lvupObj.setActiveWhenChange(false);
            base.init();
            barEffectPosX = collectProgress.gameObject.GetComponent<RectTransform>().rect.width - surplusCollectBarAmount;
            collectProgress.fillAmount = 0;
            lvProgressImg.fillAmount = 0;
            barEffectRect.gameObject.setActiveWhenChange(false);
            infoAnim = objInfo.GetComponent<Animator>();
            initBtnClick();
            flyFinishCB.Add(AwardKind.CollectTarget, collectFlyFinish);
            flyFinishCB.Add(AwardKind.Ticket, ticketFlyFinish);
            flyFinishCB.Add(AwardKind.BuffMore, bufferFlyFinish);
            ActivityDataStore.isEndSub.Subscribe(setGameEnd).AddTo(uiGameObject);
            ActivityDataStore.isEndErrorSub.Subscribe(_ =>
            {
                showGameEndMsg();
            }).AddTo(uiGameObject);
        }

        void initBtnClick()
        {
            btn_Close.onClick.AddListener(closePage);
            btn_OpenInfo.onClick.AddListener(openInfoPage);
            btn_CloseInfo.onClick.AddListener(closeInfoPage);
        }

        void closePage()
        {
            if (isShowRunning)
            {
                return;
            }

            ActivityDataStore.activityPageCloseCall();
            closeBtnClick();
        }

        public async override void open()
        {
            BindingLoadingPage.instance.open();
            await loadLocalfile();
            base.open();
            defaultPickItem.cachedGameObject.setActiveWhenChange(false);
            ActivityErrorMsgServices.registerErrorMSg();
            activityInfo = ActivityDataStore.nowActivityInfo;
            await getServerInfo();
            bgmAudioSource = playMainBGM();
            BindingLoadingPage.instance.close();
        }

        public virtual AudioSource playMainBGM()
        {
            return AudioManager.instance.playBGMOnObj(uiGameObject, AudioPathProvider.getAudioPath(ActivityBlastAudio.MainBgm));
        }

        private async Task loadLocalfile()
        {
            if (string.IsNullOrEmpty(jsonFileName))
            {
                Debug.LogError($"loadLocalFile jsonFileName isEmpty");
                return;
            }
            string jsonFile = await WebRequestText.instance.loadTextFromServer(jsonFileName);
            leveldata = JsonMapper.ToObject<RookieLevelSetting>(jsonFile);
        }
        private async Task getServerInfo()
        {
            string ticketID = ActivityDataStore.getNowActivityTicketID();
            if (activityInfo == null || string.IsNullOrEmpty(ticketID))
            {
                Debug.LogError($"get activity is null or ticketID {ticketID} is empty");
                return;
            }
            baseInitActivityResponse = await AppManager.eventServer.getAppleFarmInitData();
            if (baseInitActivityResponse.IsEnd)
            {
                Debug.Log("Event was finished");
                return;
            }
            GetBagItemResponse ticketCountRes = await AppManager.lobbyServer.getBagItem(ticketID);
            ticketCount = ticketCountRes.amount;
            updateTicketCountText();
            setIsRefreshItemData(false);
            setItemData(baseInitActivityResponse);
        }

        public void setIsRefreshItemData(bool isRefresh)
        {
            isRefreshItemData = isRefresh;
        }

        #endregion

        #region InfoPage
        IDisposable infoAnimTriggerDis;
        void openInfoPage()
        {
            ActivityDataStore.playClickAudio();
            objInfo.gameObject.setActiveWhenChange(true);
        }

        void closeInfoPage()
        {
            ActivityDataStore.playClickAudio();
            var animTriggers = infoAnim.GetBehaviour<ObservableStateMachineTrigger>();
            infoAnimTriggerDis = animTriggers.OnStateEnterAsObservable().ObserveOnMainThread().Subscribe(onInfoAnimOut);
            infoAnim.SetTrigger("out");
        }

        private void onInfoAnimOut(ObservableStateMachineTrigger.OnStateInfo obj)
        {
            IDisposable animTimerDis = null;
            animTimerDis = Observable.Timer(TimeSpan.FromSeconds(obj.StateInfo.length)).Subscribe(_ =>
            {
                infoAnimTriggerDis.Dispose();
                animTimerDis.Dispose();
                objInfo.gameObject.setActiveWhenChange(false);
            });
        }
        #endregion

        public void showRunning()
        {
            isShowRunning = true;
        }
        async void isShowFinish()
        {
            if (isGameEnd)
            {
                showGameEndMsg();
                return;
            }

            await Task.Delay(TimeSpan.FromSeconds(0.5f));
            isShowRunning = false;
        }

        void setGameEnd(bool isEnd)
        {
            isGameEnd = isEnd;
            if (isShowRunning)
            {
                return;
            }
            showGameEndMsg();
        }

        public void showGameEndMsg()
        {
            isGameEnd = false;
            UiManager.getPresenter<MsgBoxPresenter>().openActivityEndNode(closePage);
        }

        public virtual void setItemData(RookieInitActivityResponse data)
        {
            //if (isShowRunning)
            //{
            //    Debug.LogError($"setItemData isShowRunning");
            //}

            showLevelPickItems(data);
            setFinishLvAward((ulong)data.CompleteReward);
            updateCollectText(data.Progress.Value, data.Progress.Target, isRunTween: false);
            setLevelText(data.Level);
            nowBuffPercent = (ulong)(data.ExtraBonus[0] == null ? 0 : data.ExtraBonus[0][1]);
            updateBuffPercent();
            levelAward = (ulong)data.CompleteReward;
            LayoutRebuilder.ForceRebuildLayoutImmediate(completeLayoutRect);

            if (itemCanvaseGroup.alpha <= 0)
            {
                fadeItemCanvasGroup(1);
            }
            if (data.Level <= 1 && otherCheckTutoralCondition())
            {
                checkTutoralStep();
                return;
            }
            startRunRandomItemShake();
        }

        void fadeItemCanvasGroup(float endAlpha, Action onComplete = null)
        {
            TweenManager.tweenToFloat(itemCanvaseGroup.alpha, endAlpha, 0.5f, onUpdate: alpha =>
            {
                itemCanvaseGroup.alpha = alpha;
            }, onComplete: () =>
            {
                itemCanvaseGroup.alpha = endAlpha;
                if (null != onComplete)
                {
                    onComplete();
                }
            });
        }

        public virtual bool otherCheckTutoralCondition()
        {
            return true;
        }

        public bool isAlreadyPlay()
        {
            for (int i = 0; i < baseInitActivityResponse.ClickHistory.Length; ++i)
            {
                if (baseInitActivityResponse.ClickHistory[i] == 1)
                {
                    return true;
                }
            }
            return false;
        }

        public void setTutorialData()
        {
            var progressData = baseInitActivityResponse.Progress;
            tutorialCollectProgressTxt.text = $"{progressData.Value}/{progressData.Target}";
            tutorialCollectProgressImg.fillAmount = (float)progressData.Value / progressData.Target;
            tutorialLvProgressTxt.text = $"{baseInitActivityResponse.Level}/{totalLvCount}";
            tutorialLvProgressImg.fillAmount = (float)baseInitActivityResponse.Level / totalLvCount;
        }

        public void updateTutorialCollectProgress()
        {
            tutorialCollectProgressTxt.text = $"{nowProgress}/{progressTarget}";
            tutorialCollectProgressImg.fillAmount = (float)nowProgress / progressTarget;
        }

        void showLevelPickItems(RookieInitActivityResponse data)
        {
            nowLevelInfo = getNowLevelSetting(data.Level);
            if (null == nowLevelInfo)
            {
                Debug.LogError($"get {data.Level} settingInfo is null");
                return;
            }
            selectItem = null;
            pickItems.Clear();
            for (int lvInfoId = 0; lvInfoId < nowLevelInfo.itemsInfo.Length; lvInfoId++)
            {
                if (data.ClickHistory[lvInfoId] != 0)
                {
                    continue;
                }
                RookieItemInfo infoTemp = nowLevelInfo.itemsInfo[lvInfoId];
                var pickItemPool = ResourceManager.instance.getObjectFromPool(defaultPickItem.cachedGameObject, itemRoot.transform);
                pickItemPool.cachedGameObject.setActiveWhenChange(false);
                PosData newPos;
                if (!leveldata.itemPos.TryGetValue(infoTemp.index.ToString(), out newPos))
                {
                    Debug.LogError($"get {infoTemp.index} posData is null");
                    continue;
                }
                pickItemPool.cachedRectTransform.anchoredPosition = new Vector2(newPos.x, newPos.y);
                pickItemPool.cachedGameObject.name = $"PickItem_{infoTemp.index}";
                var pickItemPresenter = bindingItemPresenter(lvInfoId, pickItemPool.cachedGameObject, infoTemp);
                pickItems.Add(lvInfoId, pickItemPresenter);
                showPickItems.Add(pickItemPresenter);
            }
        }

        PickItemPresenter bindingItemPresenter(int clickId, GameObject itemObj, RookieItemInfo infoTemp)
        {
            Sprite itemSprite = findIconSprite(getPicNameById(infoTemp.pic_num));
            if (null == itemSprite)
            {
                Debug.LogError($"get iconSprite is null, clickId: {clickId},picNum:{infoTemp.pic_num}");
            }
            return UiManager.bindNode<PickItemPresenter>(itemObj).setInitData(itemSprite, clickId).setClickEvent(selectItemClick);
        }

        public virtual Sprite findIconSprite(string spriteName)
        {
            return null;
        }

        public PickItemPresenter getPackItem(int lvID)
        {
            PickItemPresenter result = null;
            for (int lvInfoId = 0; lvInfoId < nowLevelInfo.itemsInfo.Length; lvInfoId++)
            {
                RookieItemInfo infoTemp = nowLevelInfo.itemsInfo[lvInfoId];
                if (infoTemp.index == lvID)
                {
                    if (!pickItems.TryGetValue(lvInfoId, out result))
                    {
                        Debug.LogError($"get ID:{lvID} pickItem is null");
                    }
                    break;
                }
            }
            return result;
        }

        private RookieLevelInfo getNowLevelSetting(int level)
        {
            if (level < 0)
            {
                Debug.LogError($"get level setting is error, lv {level} < 0");
                return null;
            }

            return Array.Find(leveldata.levelsSetting, setting => setting.level == level); ;
        }

        string getPicNameById(int index)
        {
            if (index > iconSpriteNames.Length)
            {
                Debug.LogError($"get iconSprite Name is Error, id {index} > NamesLength {iconSpriteNames.Length}");
                return string.Empty;
            }
            return $"{iconSpriteStartName}{iconSpriteNames[index - 1]}";
        }

        bool isClickSameItem(int clickId)
        {
            if (null == selectItem)
            {
                return false;
            }
            return clickId == selectItem.showIndex;
        }

        private async void selectItemClick(int index)
        {
            if (isShowRunning || isClickSameItem(index))
            {
                Debug.LogError("isShowRunning");
                return;
            }
            showRunning();
            if (ticketCount <= 0)
            {
                ticketNotEnough();
                isShowFinish();
                return;
            }

            ActivityDataStore.playClickAudio();

            ticketCount--;
            updateTicketCountText();

            clickItemResponse = await sendServerSelect(playIndex, index);

            if (Result.OK != clickItemResponse.result)
            {
                if (Result.ActivityIDPromotedError == clickItemResponse.result)
                {
                    showGameEndMsg();
                }

                return;
            }

            if (clickItemResponse.IsLevelUp)
            {
                refreshJsonData = Util.msgpackToJsonStr(Convert.FromBase64String(clickItemResponse.RefreshData));
                //Debug.Log($"refreshJsonData : {refreshJsonData}");
            }

            pickupShow(index);
        }


        public virtual void ticketNotEnough()
        {
        }

        public virtual Task<SendSelectBaseResponse> sendServerSelect(int playIndex, int clickItem)
        {
            return Task.FromResult(new SendSelectBaseResponse());
        }

        void pickupShow(int selectIndex)
        {
            ActivityReward activityReward = null;
            if (clickItemResponse.RewardResult.Length > 0)
            {
                activityReward = clickItemResponse.RewardResult[0];
            }

            awardData.parseAwardData(activityReward);

            if (AwardKind.BuffMore == awardData.kind)
            {
                addBuffAmount(clickItemResponse.RewardResult);
            }
            ticketCount = clickItemResponse.Ticket;
            selectItem = pickItems[selectIndex];
            showPickItems.Remove(selectItem);
            if (selectItem == null)
            {
                Debug.LogError("get pick item is null");
                return;
            }

            selectItem.playOpenAnim(pickShowFinish, pickItemFinishFrame + 15);
            showAwardPic(selectItem.uiRectTransform.anchoredPosition);
            Observable.TimerFrame(pickItemFinishFrame).Subscribe(_ =>
              {
                  openAwardObj();
              });
        }

        public void addBuffAmount(ActivityReward[] rewards)
        {
            for (int i = 0; i < rewards.Length; ++i)
            {
                var result = rewards[i];
                if (result.Amount > 100)
                {
                    addLevelAward(result.getAmount);
                    continue;
                }
                awardData.parseAwardData(result);
            }
        }

        void pickShowFinish()
        {
            selectItem.close();
            showAnimFinish();
        }

        public void addLevelAward(ulong addAward)
        {
            lastLvAward = levelAward;
            levelAward += addAward;
        }

        public void showAnimFinish()
        {
            if (AwardKind.None == awardData.kind)
            {
                isShowFinish();
                return;
            }

            if (AwardKind.Coin != awardData.kind)
            {
                switch (awardData.kind)
                {
                    case AwardKind.BuffMore:
                        nowBuffPercent += awardData.amount;
                        break;
                    case AwardKind.Ticket:
                        ticketCount += (long)awardData.amount;
                        break;
                    default:
                        Observable.TimerFrame(35).Subscribe(_ =>
                        {
                            awardObjFlyToTarget();
                        });
                        return;
                }
            }

            openSmallAwardPresenter();
        }

        void openSmallAwardPresenter()
        {
            UiManager.getPresenter<SmallAwardPresenter>()
                .setAwardStatus(awardData)
                .setEndEvent(smallAwardShowFinish)
                .openAwardPage(getPrizeItem());
        }

        public virtual GameObject getPrizeItem()
        {
            return null;
        }

        void collectFlyFinish()
        {
            updateCollectText(nowProgress + 1, progressTarget);
            playGetAnim(collectAnim);
        }

        IDisposable barEffectDis;
        void openLvUpReward()
        {
            barEffectAnim.SetTrigger("out");
            barEffectDis = Observable.TimerFrame(35).Subscribe(_ =>
             {
                 barEffectDis.Dispose();
                 barEffectRect.gameObject.setActiveWhenChange(false);
             }).AddTo(uiGameObject);
            if (!clickItemResponse.IsLevelUp)
            {
                return;
            }
            selectRefreshData = JsonMapper.ToObject<RookieInitActivityResponse>(refreshJsonData);
            fadeOutBGM();

            if (selectRefreshData.IsEnd || selectRefreshData.Level == 1)
            {
                openFinalAwardPresenter<AwardBasePresenter>();
                return;
            }

            openMediumAwardPresenter<AwardBasePresenter>();
        }


        public virtual void openMediumAwardPresenter<T>() where T : AwardBasePresenter, new()
        {
            UiManager.getPresenter<T>()
                   .setCallbackEvent(awardFinish)
                   .openAwardPage(levelAward);
        }

        public virtual void openFinalAwardPresenter<T>() where T : AwardBasePresenter, new()
        {
            UiManager.getPresenter<T>()
                 .setCallbackEvent(activityIsEnd)
                 .openAwardPage(awardData.amount);
        }

        void fadeOutBGM()
        {
            AudioManager.instance.fadeCustomBGM(bgmAudioSource, 0.55f, isFadeOut: true);
        }

        void bufferFlyFinish()
        {
            playGetAnim(buffMoreAnim);
            updateBuffPercent();
            runLvAward();
        }

        void runLvAward()
        {
            var tweenID = TweenManager.tweenToUlong(lastLvAward, levelAward, 1.0f, onUpdate: setFinishLvAward, onComplete: () =>
                {
                    setFinishLvAward(levelAward);
                });

            TweenManager.tweenPlay(tweenID);
        }

        void ticketFlyFinish()
        {
            playGetAnim(ticketAnim);
            updateTicketCountText();
        }

        void playGetAnim(Animator targetAnim)
        {
            targetAnim.SetTrigger("get");
            AudioManager.instance.playAudioOnce(AudioPathProvider.getAudioPath(ActivityBlastAudio.IconFlyIn));
        }

        void resetItem()
        {
            isShowFinish();
            clearShakeDis();
            refreshItemData(refreshJsonData);
            startRunRandomItemShake();
            clickItemResponse = null;
        }

        void clearShakeDis()
        {
            UtilServices.disposeSubscribes(shakeTimerDis.ToArray());
            shakeTimerDis.Clear();
            isShaking = false;
        }

        void clearPickItems()
        {
            fadeItemCanvasGroup(endAlpha: 0, onComplete: () =>
            {
                var oldItems = pickItems.GetEnumerator();
                while (oldItems.MoveNext())
                {
                    PickItemPresenter itemPresenter = oldItems.Current.Value;
                    itemPresenter.close();
                    ResourceManager.instance.returnObjectToPool(itemPresenter.uiGameObject);
                }
            });

        }
        IDisposable lvupAnimCheckDis;
        public virtual string lvupEffectAnimName { get; set; }
        void awardFinish()
        {
            AudioManager.instance.fadeCustomBGM(bgmAudioSource, 0.40f, isFadeOut: false);
            lvupProgressTxt.text = $"{selectRefreshData.Level}/{totalLvCount}";

            startCheckLvupAnim();
        }

        void startCheckLvupAnim()
        {
            lvupObj.setActiveWhenChange(true);
            lvupAnimCheckDis = Observable.EveryUpdate().Subscribe(_ =>
             {
                 if (lvupAnim.GetCurrentAnimatorStateInfo(0).IsName(lvupEffectAnimName))
                 {
                     lvupAnimCheckDis.Dispose();
                     lvupTriggerAnim();
                 }
             }).AddTo(uiGameObject);
        }

        void lvupTriggerAnim()
        {
            clearPickItems();
            Observable.TimerFrame(60).Subscribe(_ =>
            {
                resetItem();
            }).AddTo(uiGameObject);
            Observable.TimerFrame(180).Subscribe(_ =>
            {
                lvupObj.setActiveWhenChange(false);
            }).AddTo(uiGameObject);
        }

        private void activityIsEnd()
        {
            if (!selectRefreshData.IsEnd)
            {
                awardFinish();
                return;
            }
            isShowRunning = false;
            ActivityDataStore.activtyCallIsEnd(selectRefreshData.IsEnd);
        }

        void smallAwardShowFinish()
        {
            if (AwardKind.Coin == awardData.kind)
            {
                recycleAwardObj();
                isShowFinish();
                return;
            }
            awardObjFlyToTarget();
        }
        const float movePosTime = 0.8f;
        string awardObjScaleTween;
        void awardObjFlyToTarget()
        {
            Transform flyTarget = getFlyTargetObj(awardData.kind);
            if (null == flyTarget)
            {
                //Debug.LogError($"get {awardData.kind} fly target is null");
                return;
            }
            if (AwardKind.Box == awardData.kind)
            {
                awardObj.cachedRectTransform.SetParent(flyTarget.transform);
            }
            AudioManager.instance.playAudioOnce(AudioPathProvider.getAudioPath(ActivityBlastAudio.IconFly));
            var moveID = awardObj.cachedTransform.movePos(flyTarget.position, movePosTime, onComplete: awradFlyComplete, easeType: DG.Tweening.Ease.OutQuad);
            TweenManager.tweenPlay(moveID);
            float finalScale = getAwardFinalScale();
            awardObjScaleTween = string.Empty;
            if (finalScale <= 0)
            {
                return;
            }

            awardObjScaleTween = TweenManager.tweenToFloat(awardObj.transform.localScale.x, finalScale, movePosTime, onUpdate: (val) =>
             {
                 awardObj.transform.localScale = new Vector2(val, val);
             });
            TweenManager.tweenPlay(awardObjScaleTween);
        }

        public virtual float getAwardFinalScale()
        {
            switch (awardData.kind)
            {
                case AwardKind.Ticket:
                    return 1.16f;
            }
            return 0;
        }

        public virtual Animator awardObjAnim { get { return awardObj.GetComponent<Animator>(); } }

        void awradFlyComplete()
        {
            TweenManager.tweenKill(awardObjScaleTween);
            AudioManager.instance.stopOnceAudio();

            if (null == awardObjAnim)
            {
                //Debug.Log($"awardobj {awardObj.name} GetComponent<Animator> is null");
                awardObjReturnToPool();
                return;
            }
            playGetAnim(awardObjAnim);
            customAwardPlayGetAnim();
            IDisposable animTimerDis = null;
            animTimerDis = Observable.Timer(TimeSpan.FromSeconds(1)).Subscribe(_ =>
              {
                  animTimerDis.Dispose();
                  awardObjReturnToPool();
              });
        }

        public virtual void customAwardPlayGetAnim()
        {
        }

        void awardObjReturnToPool()
        {
            flyFinishCallback(awardData.kind);
            if (null != awardObj)
            {
                recycleAwardObj();
            }
            else
            {
                Debug.LogError($"{awardData.kind} awardObj is null");
            }

            if (null == clickItemResponse)
            {
                isShowFinish();
                return;
            }
            isShowRunning = clickItemResponse.IsLevelUp;
        }

        void recycleAwardObj()
        {
            ResourceManager.instance.releasePoolWithObj(awardObj.cachedGameObject);
            GameObject.DestroyImmediate(awardObj.cachedGameObject);
            awardObj = null;
        }

        public virtual void flyFinishCallback(AwardKind awardType)
        {
            Action flyFinishAction;
            if (!flyFinishCB.TryGetValue(awardType, out flyFinishAction))
            {
                return;
            }
            flyFinishAction();
            tutorialFlyComplete();
        }

        public virtual Transform getFlyTargetObj(AwardKind awardType)
        {
            Transform flyTarget;
            flyObjTargets.TryGetValue(awardData.kind, out flyTarget);
            return flyTarget;
        }

        public void setShowAwardPic(ActivityReward rewardData, RectTransform assignRectRoot = null)
        {
            awardData.parseAwardData(rewardData);
            showAwardPic(Vector2.zero, assignRectRoot);
            openAwardObj();
        }
        void showAwardPic(Vector2 awardObjShowPos, RectTransform assignRect = null)
        {
            if (AwardKind.None == awardData.kind)
            {
                return;
            }

            Transform parentRoot = (null == assignRect) ? itemRoot : assignRect.transform;
            string objPath = getAwardPrefabPath(awardData.kind);
            awardObj = ResourceManager.instance.getObjectFromPoolWithResOrder(objPath, parentRoot, resNames: resOrder);
            awardObj.cachedGameObject.name = Path.GetFileName(objPath);
            awardObj.cachedRectTransform.anchoredPosition = awardObjShowPos;
            Sprite changeSprite = getAwardSprite(awardData);
            switch (awardData.kind)
            {
                case AwardKind.Ticket:
                    UiManager.bindNode<TicketAwardPresenter>(awardObj.cachedGameObject).setAmount(awardData.amount.ToString());
                    break;

                case AwardKind.BuffMore:
                    UiManager.bindNode<TxtAmountAwardObjPresenter>(awardObj.cachedGameObject).setAmount($"{awardData.amount}%");
                    break;

                case AwardKind.Jackpot:
                case AwardKind.Box:
                    UiManager.bindNode<SpriteAwardObjPresenter>(awardObj.cachedGameObject).setSprite(changeSprite);
                    break;

                default:
                    setAwardObj(awardData.amount, changeSprite);
                    break;
            }
            awardObj.cachedGameObject.setActiveWhenChange(false);
        }

        void openAwardObj()
        {
            if (null == awardObj)
            {
                return;
            }
            AudioManager.instance.playAudioOnce(AudioPathProvider.getAudioPath(ActivityBlastAudio.Open));
            awardObj.cachedGameObject.setActiveWhenChange(true);
        }

        public virtual void setAwardObj(ulong awardAmount, Sprite iconSprite)
        {
        }

        public virtual Sprite getAwardSprite(ActivityAwardData awardData)
        {
            return null;
        }

        public virtual string getAwardPrefabPath(AwardKind type)
        {
            string prefabName = string.Empty;

            switch (type)
            {
                case AwardKind.BuffMore:
                    prefabName = "more";
                    break;
                case AwardKind.Box:
                    prefabName = "treasure_chest";
                    break;
                case AwardKind.Ticket:
                    prefabName = "ticket";
                    break;
                case AwardKind.Coin:
                    prefabName = "coin";
                    break;
                case AwardKind.CollectTarget:
                    prefabName = "icon";
                    break;
            }

            if (string.IsNullOrEmpty(prefabName))
            {
                Debug.LogError($"get {type} Prefab Name is null");
            }

            return $"{ActivityDataStore.CommonPrefabPath}activity_item_{prefabName}";
        }

        public virtual void refreshItemData(string jsonData)
        {
            if (null == selectRefreshData)
            {
                selectRefreshData = JsonMapper.ToObject<RookieInitActivityResponse>(refreshJsonData);
            }
            setIsRefreshItemData(true);
            setItemData(selectRefreshData);
        }

        bool isShaking = false;

        public void startRunRandomItemShake()
        {
            if (isShaking)
            {
                return;
            }
            isShaking = true;
            int randomTime = UnityEngine.Random.Range(5, 8);
            shakeTimerDis.Add(Observable.Timer(TimeSpan.FromSeconds(randomTime)).Subscribe(_ =>
            {
                isShaking = false;
                randomItemShake();
            }));
        }

        void randomItemShake()
        {
            if (pickItems.Count <= 0)
            {
                startRunRandomItemShake();
                return;
            }

            PickItemPresenter shakeItem = showPickItems[UnityEngine.Random.Range(0, showPickItems.Count - 1)];
            shakeItem.playShakeAnim();
            startRunRandomItemShake();
        }

        #region ActivityDatas

        private void updateCollectText(int progress, int goal, bool isRunTween = true)
        {
            nowProgress = progress;
            progressTarget = goal;

            float endAmount = (float)nowProgress / progressTarget;
            text_FarmCollect_Target_count.text = $"{nowProgress}/{progressTarget}";
            if (!isRunTween)
            {
                collectProgress.fillAmount = endAmount;
                return;
            }
            barEffectRect.gameObject.setActiveWhenChange(true);
            string twID = TweenManager.tweenToFloat(collectProgress.fillAmount, endAmount, 0.5f, onUpdate: updateCollectBarAmount, onComplete: openLvUpReward);
            TweenManager.tweenPlay(twID);
        }

        void updateCollectBarAmount(float value)
        {
            collectProgress.fillAmount = value;
            Vector2 changePos = barEffectRect.anchoredPosition;
            float endX = (value - 1) * barEffectPosX;
            changePos.Set(endX, changePos.y);
            barEffectRect.anchoredPosition = changePos;
        }

        void setLevelText(int progress)
        {
            lvProgressImg.fillAmount = (float)progress / totalLvCount;
            text_Finished_Level_Count.text = $"{progress}/{totalLvCount}";
        }
        void updateBuffPercent()
        {
            obj_BuffPercent.SetActive(nowBuffPercent > 0);
            text_BuffPercent.text = $"{nowBuffPercent}%";
        }
        public void updateTicketCount(int totalCount)
        {
            ticketCount = totalCount;
            updateTicketCountText();
        }

        void updateTicketCountText()
        {
            ActivityDataStore.pageAmountChange((int)ticketCount);
            ticketCountTxt.text = ticketCount.ToString();
        }
        void setFinishLvAward(ulong award)
        {
            text_Finish_Level_Award.text = award.ToString("N0");
            LayoutRebuilder.ForceRebuildLayoutImmediate(completeLayoutRect);
        }

        #endregion

        #region [ Tutorial ]
        PickItemPresenter tutorialDesignItem;
        public virtual int tutorialsItemNum { get; set; }
        GameObject[] objTutorialsStep = new GameObject[3];
        TutorialStep nowTutorialStep = TutorialStep.Pass;
        void checkTutoralStep()
        {
            int clickCount = 0;
            for (int i = 0; i < baseInitActivityResponse.ClickHistory.Length; ++i)
            {
                if (baseInitActivityResponse.ClickHistory[i] == 1)
                {
                    clickCount++;
                }
            }

            if (clickCount > 1)
            {
                startRunRandomItemShake();
                return;
            }

            setTutorialData();
            if (clickCount <= 0)
            {
                nowTutorialStep = TutorialStep.Step1;
                tutorialDesignItem = getPackItem(tutorialsItemNum);
                tutorialDesignItem.setTutorialAction(toNextTutorialStep);
                tutorialDesignItem.uiRectTransform.SetParent(objTutorialsStep[0].transform);
                tutorialDesignItem.uiRectTransform.SetSiblingIndex(1);
                //tutorialDesignItem.setImgRaycast(enable: false);
                tutorialDesignItem.open();
            }
            else
            {
                nowTutorialStep = TutorialStep.Step2;
            }
            setNowTutorialStatus();
        }

        void setNowTutorialStatus()
        {
            objTutorialsStep[0].setActiveWhenChange(TutorialStep.Step1 == nowTutorialStep);
            objTutorialsStep[1].setActiveWhenChange(TutorialStep.Step2 == nowTutorialStep);
            objTutorialsStep[2].setActiveWhenChange(TutorialStep.Step3 == nowTutorialStep);

            if (null != tutorialDesignItem && TutorialStep.Pause == nowTutorialStep)
            {
                tutorialDesignItem.uiRectTransform.SetParent(itemRoot);
                //tutorialDesignItem.setImgRaycast(enable: true);
            }
        }

        void tutoralClick()
        {
            ActivityDataStore.playClickAudio();
            toNextTutorialStep();
        }

        void toNextTutorialStep()
        {
            nowTutorialStep = (TutorialStep)((int)nowTutorialStep + 1);
            setNowTutorialStatus();
        }
        void tutorialFlyComplete()
        {
            if (TutorialStep.Pause != nowTutorialStep)
            {
                return;
            }
            updateTutorialCollectProgress();
            toNextTutorialStep();
        }
        private enum TutorialStep
        {
            Step1,
            Pause,
            Step2,
            Step3,
            Pass,
        }
        #endregion

        public override void animOut()
        {
            //AudioPathProvider.getAudioPath(LobbyMainAudio.Main_BGM)
            if (null != bgmAudioSource)
            {
                AudioManager.instance.stopBGMOnObjAndResetMainBGM(bgmAudioSource);
            }

            DataStore.getInstance.playerInfo.myWallet.forceNotify();
            clear();
        }

        public override void clear()
        {
            ResourceManager.instance.releasePoolWithObj(defaultPickItem.cachedGameObject);
            UtilServices.disposeSubscribes(shakeTimerDis.ToArray());
            LobbyLogic.Common.GamePauseManager.gameResume();
            base.clear();
        }
    }
}
