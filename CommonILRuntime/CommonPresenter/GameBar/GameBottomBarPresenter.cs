using CommonILRuntime.Module;
using UnityEngine.UI;
using UnityEngine;
using System.Collections.Generic;
using System;
using CommonPresenter.BottomBarStage;
using CommonILRuntime.BindingModule;
using LobbyLogic.Audio;
using CommonService;
using Binding;
using Services;
using UniRx;
using System.Threading.Tasks;
using CommonILRuntime.Services;
using static CommonILRuntime.Services.DailyMissionServices;

namespace CommonPresenter
{
    class TotalWinTweenerHandler : ILongValueTweenerHandler
    {
        Text totalWinText;

        public TotalWinTweenerHandler(Text totalWinText)
        {
            this.totalWinText = totalWinText;
        }

        public GameObject getDisposableObj()
        {
            return totalWinText.gameObject;
        }

        public void onValueChanged(ulong value)
        {
            totalWinText.text = value.ToString("N0");
        }
    }

    public class GameBottomBarPresenter : ContainerPresenter
    {
        string flashEffectPath
        {
            get
            {
                return UtilServices.getOrientationObjPath("prefab/game/win_flash_effect");
            }
        }
        string coinEffectPath
        {
            get
            {
                return UtilServices.getOrientationObjPath("prefab/game/win_coin_effect");
            }
        }
        public override string objPath
        {
            get
            {
                return UtilServices.getOrientationObjPath("prefab/game/game_low_bar");
            }
        }
        public override UiLayer uiLayer { get { return UiLayer.BarRoot; } }

        CustomBtn autoSpinBtn;
        CustomBtn spinBtn;
        CustomBtn stopBtn;
        GameObject stopBtnNormal;
        GameObject stopBtnAuto;
        Text autoCountTxt;
        GameObject maxBetEffect;
        GameObject infinityIcon;
        GameObject infinityAndBreakIcon;
        GameObject btnsGroupObj;

        Text totalWinTxt;
        Animator freespinCountAni;
        Text freespinCountTxt;

        GameObject effectTarget;
        Button autoItemCloseBtn;
        Animator clubTipAnim;
        Text clubMsgTxt;
        Image bgImg;

        #region betSetting
        Button maxBetBtn;
        Button decreaseBetBtn;
        Button increaseBetBtn;
        CustomTextSizeChange betNumTxt;
        GameObject totalBetObj;
        GameObject meanBetObj;
        #endregion

        #region autoSetting
        BindingNode autoItemPanel;
        GameBottomBarAutoItemPresenter autoItemPresenter;
        #endregion     

        #region setCall
        public Action spinOnClick = null;
        public Action stopOnClick = null;
        public Action<ulong> totalBetCall = null;
        public Action<float> onMaxBetPercentChange = null;  //用於監聽是否達標解鎖JP的檔位
        #endregion

        public CommonUiConfig.BottomBtnStage nowBtnStage { get; private set; }
        public int betIdx
        {
            get
            {
                return nowBetData[gameBetId].totalBetID;
            }
        }
        int gameBetId { get; set; } = DataStore.getInstance.dataInfo.chooseBetClass.BetId;
        ulong lastBetNum = 0;
        ulong openClubCoin;

        PlayButton playButton;

        List<float> dashTimeDatas = new List<float>() { 0.5f, 1f, 1.5f };
        List<GameBetInfo> nowBetData;
        List<long> totalBetData = new List<long>();
        Dictionary<string, BetBase> betBaseGame;

        ButtonPlayState btnPlayState;
        ButtonPlayState btnPlayWithNoLongPressState;
        ButtonStopState btnStopState;

        ulong totalBet = 0;
        ulong targetTotalWin = 0;
        float betDivide = 0;
        int upBetAmount = 0;
        int downBetAmount = 0;

        Font[] autoTextFont = new Font[3];

        LongValueTweener totalWinTweener;
        Animator stopAnim;
        DailyMissionNodePresenter dailyMissionPresenter;
        List<long> totalBets = new List<long>();
        ActivityIconsPresetner activityIconsPresetner;

        bool isCloseDailyMission
        {
            get
            {
                return (DataStore.getInstance.playerInfo.level < 10);
            }
        }
        public override void initUIs()
        {
            bgImg = getImageData("bg_img");
            stopBtn = getCustomBtnData("stop_btn");
            stopBtnNormal = getGameObjectData("stopbtn_normal");
            stopBtnAuto = getGameObjectData("stopbtn_status");
            autoCountTxt = getTextData("auto_count_txt");
            autoSpinBtn = getCustomBtnData("spin_btn_status");
            spinBtn = getCustomBtnData("spin_btn_normal");
            totalWinTxt = getTextData("total_win_txt");
            freespinCountAni = getAnimatorData("bg_freespin_times_ani");
            freespinCountTxt = getTextData("bg_freespin_times_txt");

            infinityIcon = getGameObjectData("unlimited_stop");
            infinityAndBreakIcon = getGameObjectData("unlimited_speed_stop");

            effectTarget = getGameObjectData("effect_target");

            autoItemCloseBtn = getBtnData("auto_item_close");
            clubTipAnim = getAnimatorData("club_tip_anim");
            clubMsgTxt = getTextData("club_tip_msg");
            stopAnim = stopBtn.GetComponent<Animator>();
            maxBetEffect = getGameObjectData("max_bet_effect");

            var dailyMission = getNodeData("daily_mission").cachedGameObject;
            dailyMission.setActiveWhenChange(!isCloseDailyMission);
            if (!isCloseDailyMission)
            {
                dailyMissionPresenter = UiManager.bindNode<DailyMissionNodePresenter>(dailyMission);
            }

            activityIconsPresetner = UiManager.getPresenter<ActivityIconsPresetner>();
            #region betSetting
            maxBetBtn = getBtnData("maxbet_btn");
            decreaseBetBtn = getBtnData("bet_decrease_btn");
            increaseBetBtn = getBtnData("bet_increase_btn");
            betNumTxt = getBindingData<CustomTextSizeChange>("bet_num_txt");
            totalBetObj = getGameObjectData("total_bet_root");
            meanBetObj = getGameObjectData("mean_bet_obj");
            #endregion

            #region autoSetting
            autoItemPanel = getNodeData("auto_item");
            autoItemPresenter = UiManager.bindNode<GameBottomBarAutoItemPresenter>(autoItemPanel.cachedGameObject);
            #endregion

            landBarGetBtnGroups();
        }

        async void landBarGetBtnGroups()
        {
            var orientation = await DataStore.getInstance.dataInfo.getNowGameOrientation();
            if (GameOrientation.Landscape != orientation || GuideStatus.Completed == DataStore.getInstance.guideServices.nowStatus)
            {
                return;
            }
            btnsGroupObj = getGameObjectData("btns_group");
        }

        public override void init()
        {
            initClubTips();
            initDailyMission();
            openFunctionInGame(DataStore.getInstance.playerInfo.hasHighRollerPermission);
            changeBGImg(BetClass.HighRoller == DataStore.getInstance.dataInfo.getChooseBetClassType());

            CommonUiConfig.initStopBtnLockTime();

            clubTipAnim.gameObject.setActiveWhenChange(true);
            maxBetEffect.setActiveWhenChange(false);
            autoItemCloseBtn.gameObject.setActiveWhenChange(false);
            btnPlayState = new ButtonPlayState(autoSpinBtn, normalSpinClick, openAutoItemPanel);
            btnPlayWithNoLongPressState = new ButtonPlayState(spinBtn, fgSpinClick, null);
            btnStopState = new ButtonStopState(stopBtn, onStopClick, null);
            btnStopState.setStopBtnStateObjs(stopBtnNormal, stopBtnAuto);

            autoSpinBtn.setLongPressTime(1);
            autoSpinBtn.objScale = 1;
            spinBtn.objScale = 1;
            stopBtn.objScale = 1;

            initBtnsClick();
            initPlayButton();
            setTotalWinNum(0);

            getBetData();
            closeMeanBet();

            DataStore.getInstance.playerInfo.isLvUpSubject.Subscribe(lvUp).AddTo(uiGameObject);
            DataStore.getInstance.highVaultData.isShowVault.Subscribe(openFunctionInGame).AddTo(uiGameObject);

            totalWinTweener = new LongValueTweener(new TotalWinTweenerHandler(totalWinTxt), 1000);

            for (int i = 0; i < autoTextFont.Length; ++i)
            {
                autoTextFont[i] = ResourceManager.instance.load<Font>($"Bar_Resources/font/num_stop_{i + 1}");
            }

            stopBtn.pointerDownHandler = () => { changeAutoTextFont(StopBtnState.Press); };
            stopBtn.pointerUPHandler = () => { changeAutoTextFont(StopBtnState.Normal); };
            Observable.EveryUpdate().Subscribe(_ => { onStopBtnPlayAnim(); }).AddTo(uiGameObject);
            DataStore.getInstance.gameToLobbyService.getHighRollerVault();
            waitCloseClubTip();

            setGuideEvent();
        }

        void initDailyMission()
        {
            if (isCloseDailyMission)
            {
                return;
            }

            DataStore.getInstance.dailyMissionServices.unlockSubject.Subscribe(updateDailyMissionPresenterState).AddTo(uiGameObject);
            DataStore.getInstance.dailyMissionServices.askIsUnLock();
        }

        void updateDailyMissionPresenterState(bool isUnlock)
        {
            if (isUnlock)
            {
                dailyMissionPresenter.open();
                return;
            }

            dailyMissionPresenter.close();
        }

        void openFunctionInGame(bool isShow)
        {
            if (isShow)
            {
                activityIconsPresetner.open();
                return;
            }
            activityIconsPresetner.close();
        }

        void setGuideEvent()
        {
            if (GameGuideStatus.Completed == DataStore.getInstance.guideServices.getSaveGameGuideStep())
            {
                return;
            }
            DataStore.getInstance.guideServices.guideSpinClickSub.Subscribe(_ =>
            {
                clickPlayButton();
            }).AddTo(uiGameObject);
            DataStore.getInstance.guideServices.setBetBtnEnableSub.Subscribe(setBetBtnEnable).AddTo(uiGameObject);
            DataStore.getInstance.guideServices.guideMaxBetClickSub.Subscribe(_ =>
            {
                maxBetClick();
            }).AddTo(uiGameObject);
            //DataStore.getInstance.guideServices.gameBetGroupActiveSub.Subscribe(active => btnsGroupObj.SetActive(active)).AddTo(uiGameObject);
        }

        public void registerDashWinPointComplete(Action onComplete)
        {
            totalWinTweener.onComplete = onComplete;
        }

        async void waitCloseClubTip()
        {
            await Task.Delay(TimeSpan.FromSeconds(0.5f));
            clubTipAnim.gameObject.setActiveWhenChange(false);
        }

        async void changeBGImg(bool hasPermission)
        {
            string spriteName = hasPermission ? "vip" : "normal";
            GameOrientation gameOrientation = await DataStore.getInstance.dataInfo.getNowGameOrientation();
            string orientationStr = GameOrientation.Landscape == gameOrientation ? string.Empty : $"_{gameOrientation.ToString().ToLower()}";
            bgImg.sprite = ResourceManager.instance.load<Sprite>($"Bar_Resources/pic{orientationStr}/res_game_low_ui{orientationStr}/bg_ui_game_down_{spriteName}{orientationStr}");
        }
        async void initClubTips()
        {
            clubMsgTxt.text = LanguageService.instance.getLanguageValue("DiamondClub_EligibleBet_Text_2");
            clubTipAnim.gameObject.setActiveWhenChange(true);
            await Task.Delay(TimeSpan.FromSeconds(0.1f));
            clubTipAnim.gameObject.setActiveWhenChange(false);
        }
        void openClubTip(string msgKey)
        {
            clubMsgTxt.text = LanguageService.instance.getLanguageValue(msgKey);
            clubTipAnim.gameObject.setActiveWhenChange(true);
            Observable.Timer(TimeSpan.FromSeconds(3)).Subscribe(_ =>
            {
                closeClubTip();
            }).AddTo(uiGameObject);
        }
        void closeClubTip()
        {
            clubTipAnim.SetTrigger("close");
            Observable.TimerFrame(28).Subscribe(_ =>
            {
                clubTipAnim.gameObject.setActiveWhenChange(false);
            }).AddTo(uiGameObject);
        }

        private void onStopBtnPlayAnim()
        {
            var animStateInfo = stopAnim.GetCurrentAnimatorStateInfo(0);

            if (animStateInfo.IsName("stopbtn_normal"))
            {
                changeAutoTextFont(StopBtnState.Normal);
                return;
            }

            if (animStateInfo.IsName("stopbtn_pressed"))
            {
                changeAutoTextFont(StopBtnState.Press);
                return;
            }

            changeAutoTextFont(StopBtnState.Disable);
        }

        public void clearRegisteredActions()
        {
            spinOnClick = null;
            stopOnClick = null;
            totalBetCall = null;
            onMaxBetPercentChange = null;
            totalWinTweener.onComplete = null;
        }

        void changeAutoTextFont(StopBtnState btnState)
        {
            autoCountTxt.font = autoTextFont[(int)btnState];
        }

        public void setTotalBetCall(Action<ulong> totalBetCall, Action<float> onMaxBetPercentChange = null)
        {
            if (null != onMaxBetPercentChange)
            {
                this.onMaxBetPercentChange = onMaxBetPercentChange;
            }
            this.totalBetCall = totalBetCall;
            getBetData();
        }

        async void getBetData(long preBetData = 0)
        {
            betBaseGame = await DataStore.getInstance.dataInfo.getGameBetBase();
            totalBetData = await DataStore.getInstance.dataInfo.getNowPlayerBetList();
            nowBetData = await DataStore.getInstance.dataInfo.getPlayerNowBetDataInfos();
            totalBets.Clear();

            string chooseBetClass = DataStore.getInstance.dataInfo.getChooseBetClassType() == BetClass.HighRoller ? ChooseBetClass.High_Roller : ChooseBetClass.Regular;
            BetBase betBase = betBaseGame[chooseBetClass];
            betDivide = betBase.percent * totalBetData.Count * 0.01f;
            upBetAmount = betBase.upAmount;
            downBetAmount = betBase.downAmount;
            openClubCoin = (ulong)(DataStore.getInstance.playerInfo.coinExchangeRate * 0.25f);
            for (int i = 0; i < nowBetData.Count; ++i)
            {
                int betAmount = upBetAmount;
                if (i >= betDivide)
                {
                    betAmount = downBetAmount;
                }

                totalBets.Add(nowBetData[i].bet * betAmount);
            }

            if (null == totalBetCall)
            {
                return;
            }

            if (preBetData != 0)
            {
                var newBetID = totalBetData.FindIndex(bet => bet == preBetData);
                if (newBetID >= 0)
                {
                    gameBetId = newBetID;
                }
            }

            setBetNum();
        }

        public void openMeanBet()
        {
            totalBetObj.setActiveWhenChange(false);
            meanBetObj.setActiveWhenChange(true);
        }

        public void closeMeanBet()
        {
            meanBetObj.setActiveWhenChange(false);
            totalBetObj.setActiveWhenChange(true);
        }

        public void setAutoItemClick(Action<int> autoItemClick)
        {
            autoItemPresenter.setAutoItemClick(autoItemClick);
        }

        void initBtnsClick()
        {
            decreaseBetBtn.onClick.AddListener(decreaseBetClick);
            increaseBetBtn.onClick.AddListener(increaseBetClick);
            maxBetBtn.onClick.AddListener(maxBetClick);
            autoItemCloseBtn.onClick.AddListener(closeAutoItemPanel);
        }

        void initPlayButton()
        {
            playButton = new PlayButton();
            stopBtn.gameObject.setActiveWhenChange(false);
            spinBtn.gameObject.setActiveWhenChange(false);
            playButton.changeState(btnPlayState);
        }

        public void registerPlayButtonEnableChange(Action<bool> onEnableChangeHandler)
        {
            playButton.registerOnEnableChange(onEnableChangeHandler);
        }

        public void setPlayBtnEnable(bool enable)
        {
            playButton.setEnable(enable);
        }

        public void clickPlayButton()
        {
            playButton.onClick();
        }

        public void setFreeCount(int fgCount, int fgMax, int mode) //mode : 1(開啟) 0(無動作) -1(關閉)
        {
            freespinCountTxt.text = $"{fgCount}/{fgMax}";
            CommonUiConfig.BottomFreeMode freeMode = (CommonUiConfig.BottomFreeMode)mode;
            if (CommonUiConfig.BottomFreeMode.None == freeMode)
            {
                return;
            }
            string freeAniTrigger = (freeMode == CommonUiConfig.BottomFreeMode.Open) ? "on" : "off";
            freespinCountAni.SetTrigger(freeAniTrigger);
        }

        public async void setBetBtnEnable(bool enable)
        {
            //if (GuideStatus.Spin == DataStore.getInstance.guideServices.nowStatus)
            //{
            //    enable = false;
            //}

            if (enable)
            {
                maxBetBtn.interactable = !isMaxBetID;
            }
            else
            {
                maxBetBtn.interactable = enable;
            }
            var gameID = await DataStore.getInstance.dataInfo.getNowplayGameID();
            if (GuideStatus.Completed != DataStore.getInstance.guideServices.getSaveGuideStatus() && gameID.Equals("10002"))
            {
                enable = false;
            }
            increaseBetBtn.interactable = enable;
            decreaseBetBtn.interactable = enable;
        }

        public void setRemainAutoCount(int autoCount)
        {
            //更新剩餘次數
            autoCountTxt.text = autoCount > 0 ? autoCount.ToString() : string.Empty;

            //更新按鈕圖示
            var autoMode = convertCountToMode(autoCount);
            infinityIcon.setActiveWhenChange(CommonUiConfig.AutoMode.INFINITY == autoMode);
            infinityAndBreakIcon.setActiveWhenChange(CommonUiConfig.AutoMode.INFINITY_AND_BREAK == autoMode);
        }

        CommonUiConfig.AutoMode convertCountToMode(int count)
        {
            if (count >= 0)
            {
                return CommonUiConfig.AutoMode.NUMBER;
            }

            return (CommonUiConfig.AutoMode)count;
        }

        public void hideAutoCount()
        {
            autoCountTxt.text = string.Empty;
            infinityIcon.setActiveWhenChange(false);
            infinityAndBreakIcon.setActiveWhenChange(false);
        }
        public void setAsSpinButton(bool isBetEnable = true)
        {
            setBetBtnEnable(isBetEnable);
            playButton.changeState(btnPlayState);
        }

        public void setAsSpinWithNoLongPressBtn()
        {
            setBetBtnEnable(false);
            playButton.changeState(btnPlayWithNoLongPressState);
            DataStore.getInstance.guideServices.subjectPlayBtnEnable(false);
        }

        public void setAsSpinButton()
        {
            setBetBtnEnable(true);
            playButton.changeState(btnPlayState);
            DataStore.getInstance.guideServices.subjectPlayBtnEnable(true);
        }

        public void setAsStopButton(bool isAutoState = false)
        {
            setBetBtnEnable(false);
            playButton.changeState(btnStopState, isAutoState);
        }

        public void setBetNumTxt(ulong showBet)
        {
            betNumTxt.text = showBet.ToString("N0");
        }

        public void assignNGBetID(int betID)
        {
            if (betID > nowBetData.Count)
            {
                Debug.LogError($"assignNGBetID {betID} > betDataCount {nowBetData.Count}");
                return;
            }

            gameBetId = betID;
            setBetNum();
        }

        void normalSpinClick()
        {
            closeAutoItemPanel();
            if (totalBet > DataStore.getInstance.playerInfo.playerMoney)
            {
                DataStore.getInstance.eventInGameToLobbyService.OpenFuncInLobby(FunctionNo.NoCoinNotice);
                //UiManager.getPresenter<MsgBoxPresenter>().openNoCoinMsg();
                return;
            }
            PlayerPrefs.SetInt(DataStore.getInstance.dataInfo.chooseBetClass.Type, gameBetId);
            if (null != spinOnClick)
            {
                spinOnClick();
            }
        }

        void fgSpinClick()
        {
            if (null != spinOnClick)
            {
                spinOnClick();
            }
        }

        public void guideOnSpinClick()
        {
            if (null == spinOnClick)
            {
                return;
            }
            spinOnClick();
        }
        public void closeAutoItemPanel()
        {
            autoItemPresenter.close();
            autoItemCloseBtn.gameObject.setActiveWhenChange(false);
        }

        void openAutoItemPanel()
        {
            autoItemPresenter.open();
            autoItemCloseBtn.gameObject.setActiveWhenChange(true);
        }

        void onStopClick()
        {
            stopOnClick?.Invoke();
        }

        public Vector3 getEffectTargetPoint()
        {
            return effectTarget.transform.position;
        }

        public void attachToEffectTarget(Transform child)
        {
            child.SetParent(effectTarget.transform, false);
        }

        void lvUp(bool isLvUp)
        {
            if (!isLvUp || DataStore.getInstance.playerInfo.level > 20)
            {
                return;
            }
            getBetData(nowBetData[gameBetId].bet);
        }

        public BarEffectPresenter addDefaultFlashEffect()
        {
            return addBarEffectPresenter<BarEffectPresenter>(flashEffectPath, "win_flash_effect");
        }

        public BarEffectPresenter addDefaultCoinEffect()
        {
            return addBarEffectPresenter<BarEffectPresenter>(coinEffectPath, "win_coin_effect");
        }

        public T addBarEffectPresenter<T>(string effectPath, string endStateName) where T : BarEffectPresenter, new()
        {
            var poolObj = ResourceManager.instance.getObjectFromPool(effectPath);
            var presenter = UiManager.bindNode<T>(poolObj.cachedGameObject);

            attachToEffectTarget(poolObj.cachedTransform);
            presenter.setEndStateName(endStateName);
            presenter.open();
            return presenter;
        }

        #region bet
        bool isMaxBetID;
        void setBetNum()
        {
            totalBet = (ulong)totalBets[gameBetId];
            checkOpenClubTip();
            lastBetNum = totalBet;
            betNumTxt.text = totalBet.ToString("N0");
            if (null != totalBetCall)
            {
                totalBetCall(totalBet);
            }
            isMaxBetID = (gameBetId >= nowBetData.Count - 1);
            if (autoSpinBtn.gameObject.activeSelf)
            {
                maxBetBtn.interactable = !isMaxBetID;
            }
            if (nowBetData.Count >= 0 && null != onMaxBetPercentChange)
            {
                onMaxBetPercentChange((gameBetId + 1) / (float)nowBetData.Count);
            }
            if (isMaxBetID)
            {
                openMaxBetEffect();
            }
            DataStore.getInstance.guideServices.setMaxBetEnable(maxBetBtn.interactable);
        }
        bool isMaxBetEffectOpeing;
        void openMaxBetEffect()
        {
            if (isMaxBetEffectOpeing)
            {
                return;
            }
            isMaxBetEffectOpeing = true;
            maxBetEffect.setActiveWhenChange(true);
            Observable.TimerFrame(30).Subscribe(_ =>
            {
                maxBetEffect.setActiveWhenChange(false);
                isMaxBetEffectOpeing = false;
            });
        }

        void checkOpenClubTip()
        {
            if (BetClass.HighRoller == DataStore.getInstance.dataInfo.getChooseBetClassType() || lastBetNum <= 0)
            {
                return;
            }

            if (lastBetNum < openClubCoin && totalBet > openClubCoin)
            {
                openClubTip("DiamondClub_EligibleBet_Text_2");
                return;
            }

            if (lastBetNum > openClubCoin && totalBet < openClubCoin)
            {
                openClubTip("DiamondClub_IneligibleBet_Text_2");
            }
        }

        void stopDashWinPoint()
        {
            totalWinTweener?.stop();
        }

        void updateTotalWinText(ulong winNum)
        {
            if (winNum > 0)
            {
                totalWinTxt.text = winNum.ToString("N0");
                return;
            }
            totalWinTxt.text = string.Empty;
        }

        /// <summary>
        /// 提供給各遊戲用的API，用來停止跑分、重設UI、校正totalWin目標值
        /// </summary>
        public void setTotalWinNum(ulong winNum)
        {
            stopDashWinPoint();
            targetTotalWin = winNum;
            updateTotalWinText(winNum);
        }

        /// <summary>
        /// 跑分且累加目標值，一次性跑分配合 setTotalWinNum 使用，JP分段跑分表演直接呼叫會持續累加目標值
        /// </summary>
        public void dashWinPoints(ulong winPoints, int totalPay)
        {
            stopDashWinPoint(); //stop previous tween
            ulong beginValue = targetTotalWin;
            ulong endValue = targetTotalWin + winPoints;
            //tweenDashWin = TweenManager.tweenToLong(beginValue, endValue, getDashPointTime(totalPay), updateTotalWin); //接續上一個target繼續跑
            ulong frequency = (ulong)(winPoints / getDashPointTime(totalPay));
            totalWinTweener.setFrequency(frequency);
            totalWinTweener.setRange(beginValue, endValue);
            targetTotalWin = endValue;
        }

        /// <summary>
        /// 中斷total win跑分且直接到目標值
        /// </summary>
        public void breakDashWinPoint()
        {
            totalWinTweener.onComplete?.Invoke();
            stopDashWinPoint();
            setTotalWinNum(targetTotalWin);
        }

        float getDashPointTime(int totalPay)
        {
            for (int i = dashTimeDatas.Count - 1; i >= 0; i--)
            {
                if (totalPay >= i)
                {
                    return dashTimeDatas[i];
                }
            }
            return 0f;
        }

        void maxBetClick()
        {
            playBtnSound(BasicCommonSound.MaxbetBtn);
            gameBetId = nowBetData.Count - 1;
            setBetNum();
        }

        void decreaseBetClick()
        {
            playBtnSound(BasicCommonSound.MinusBetBtn);
            gameBetId--;
            gameBetId = (gameBetId + nowBetData.Count) % nowBetData.Count;

            setBetNum();
        }

        void increaseBetClick()
        {
            playBtnSound(BasicCommonSound.PlusbetBtn);
            if (gameBetId >= nowBetData.Count - 1)
            {
                gameBetId = 0;
            }
            else
            {
                gameBetId++;
            }

            setBetNum();
        }

        void playBtnSound(BasicCommonSound btnSound)
        {
            AudioManager.instance.playAudioOnce(CommonAudioPathProvider.getAudioPath(btnSound));
        }

        #endregion
    }

    public class GameBottomPortraitBarPresenter : GameBottomBarPresenter
    {
        RectTransform inGameGroupRect;
        Button openInGameBtn;
        Button closeInGameBtn;

        float openInGameX;
        float closeInGameX;
        public override void initUIs()
        {
            base.initUIs();
            inGameGroupRect = getRectData("ingame_group_rect");
            openInGameBtn = getBtnData("open_ingame_btn");
            closeInGameBtn = getBtnData("close_ingame_btn");
        }

        public override void init()
        {
            DataStore.getInstance.highVaultData.isShowVault.Subscribe(inGameShow).AddTo(uiGameObject);
            closeInGameX = inGameGroupRect.anchoredPosition.x;
            openInGameX = closeInGameX - inGameGroupRect.rect.width;
            openInGameBtn.onClick.AddListener(openInGameGroup);
            closeInGameBtn.onClick.AddListener(closeInGameGroup);
            base.init();
            closeInGameBtn.gameObject.setActiveWhenChange(false);
            openInGameBtn.gameObject.setActiveWhenChange(true);
            closeInGameGroup();
        }

        void closeInGameGroup()
        {
            if (inGameGroupRect.anchoredPosition.x == closeInGameX)
            {
                return;
            }
            inGameGroupRect.anchPosMoveX(closeInGameX, 1, onComplete: () =>
            {
                closeInGameBtn.gameObject.setActiveWhenChange(false);
                openInGameBtn.gameObject.setActiveWhenChange(true);
            });
        }

        void openInGameGroup()
        {
            if (inGameGroupRect.anchoredPosition.x == openInGameX)
            {
                return;
            }

            inGameGroupRect.anchPosMoveX(openInGameX, 1, onComplete: () =>
            {
                closeInGameBtn.gameObject.setActiveWhenChange(true);
                openInGameBtn.gameObject.setActiveWhenChange(false);
            });
        }

        void inGameShow(bool isShow)
        {
            inGameGroupRect.gameObject.setActiveWhenChange(isShow);
        }
    }

    public class DailyMissionNodePresenter : NodePresenter
    {
        Image dailySchedualImg;
        Animator dailySchedialAnim;
        Animator giftAnim;
        Text spinTxt;
        Button collectBtn;
        Button infoBtn;
        ContentInfo contentInfo;

        const string completeMsgKey = "Dailymission_Collect";
        const string spinMsgKey = "Dailymission_Spin";

        public override void initUIs()
        {
            dailySchedualImg = getImageData("daily_schedual_img");
            dailySchedialAnim = getAnimatorData("daily_anim");
            giftAnim = getAnimatorData("daily_gift_anim");
            spinTxt = getTextData("dialy_spin_txt");
            collectBtn = getBtnData("collect_btn");
            infoBtn = giftAnim.GetComponent<Button>();
            initInfoPage();
        }

        void initInfoPage()
        {
            var node = getNodeData("content_info");
            contentInfo = UiManager.bindNode<ContentInfo>(node.cachedGameObject);
        }

        public override void init()
        {
            collectBtn.onClick.AddListener(collectCurrentReward);
            infoBtn.onClick.AddListener(openMissionPage);
            DataStore.getInstance.dailyMissionServices.progressSubject.Subscribe(setDailySchedual).AddTo(uiGameObject);
            DataStore.getInstance.dailyMissionServices.newMissionSubject.Subscribe(showContentMsg).AddTo(uiGameObject);
            DataStore.getInstance.dailyMissionServices.askNewMission();
            collectBtn.gameObject.setActiveWhenChange(false);
        }

        void collectCurrentReward()
        {
            DataStore.getInstance.dailyMissionServices.collectCurrentReward();
        }

        void openMissionPage()
        {
            DataStore.getInstance.eventInGameToLobbyService.OpenFuncInLobby(FunctionNo.DailyMission);
        }

        void setDailySchedual(InfoFormat format)
        {
            detectNotShowMissionState(format);
            updateDisplayMissionProgress(format);
        }

        void detectNotShowMissionState(InfoFormat format)
        {
            var normalMission = format.normalMission;
            var specialMission = format.specialMission;

            if (!normalMission.isComplete && specialMission.isComplete)
            {
                openMissionPage();
            }
        }

        void updateDisplayMissionProgress(InfoFormat format)
        {
            var info = DataStore.getInstance.dailyMissionServices.getMissionInfo(format);
            var progress = info.count / info.max;
            var isComplete = (progress >= 1);

            dailySchedualImg.fillAmount = (float)progress;
            detectCompleteState(isComplete);
            if (isComplete && info.isNeedNotice)
            {
                showMissionProgressFullAni();
            }
            else if (info.haveExtraCount)
            {
                spinTxt.text = info.extraCount.ToString("f0");
            }
        }

        void showMissionProgressFullAni()
        {
            if (DataStore.getInstance.gameTimeManager.IsPaused())
            {
                giftAnim.SetTrigger("get");
            }
            else
            {
                dailySchedialAnim.SetTrigger("full");
                DataStore.getInstance.gameTimeManager.Pause();
                Observable.TimerFrame(55).Subscribe(_ =>
                {
                    giftAnim.SetTrigger("get");
                    openMissionPage();
                }).AddTo(uiGameObject);
            }
        }

        void detectCompleteState(bool isComplete)
        {
            //TODO 等規格確定後決定移除or開啟收集按鈕
            //collectBtn.gameObject.setActiveWhenChange(isComplete);
            string msgKey = isComplete ? completeMsgKey : spinMsgKey;
            spinTxt.text = LanguageService.instance.getLanguageValue(msgKey);
        }

        void showContentMsg(MissionInfo missionInfo)
        {
            IDisposable infoDis = null;

            detectCompleteState(missionInfo.isComplete);
            updateDailySchedual(missionInfo);
            if (missionInfo.haveExtraCount)
            {
                spinTxt.text = missionInfo.extraCount.ToString("f0");
            }
            contentInfo.setContent(convertContent(missionInfo.content));
            contentInfo.open();
            infoDis = Observable.Timer(TimeSpan.FromSeconds(10f), Scheduler.MainThreadIgnoreTimeScale).Subscribe(_ =>
            {
                contentInfo.close();
                infoDis.Dispose();
            });
        }
        string convertContent(string content)
        {
            while (content.Contains(";"))
            {
                content = content.Replace(";", string.Empty);
            }
            return content;
        }

        void updateDailySchedual(MissionInfo missionInfo)
        {
            var progress = missionInfo.count / missionInfo.max;
            var isComplete = (progress >= 1);

            dailySchedualImg.fillAmount = (float)progress;
            if (isComplete)
            {
                giftAnim.enabled = true;
                giftAnim.SetTrigger("get");
            }
            else
            {
                giftAnim.enabled = false;
            }
        }

        public class ContentInfo : NodePresenter
        {
            RectTransform layout;
            Text text;

            public override void initUIs()
            {
                layout = getRectData("info_layout");
                text = getTextData("info_text");
            }

            public void setContent(string msg)
            {
                text.text = msg;
                CoroutineManager.StartCoroutine(delayRebuildLayout());
            }

            System.Collections.IEnumerator delayRebuildLayout()
            {
                yield return 0f;
                LayoutRebuilder.ForceRebuildLayoutImmediate(layout);
            }
        }
    }

    enum StopBtnState : int
    {
        Normal = 0,
        Press,
        Disable
    }
}
