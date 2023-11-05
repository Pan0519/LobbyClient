using UnityEngine.UI;
using UnityEngine;
using UniRx;
using LobbyLogic.Audio;
using CommonService;
using CommonILRuntime.Module;
using System;
using CommonILRuntime.SpriteProvider;
using Services;
using Binding;
using System.Collections.Generic;
using CommonILRuntime.BindingModule;
using ExpInfoOpenState = CommonPresenter.CommonUiConfig.ExpInfoOpenState;

namespace CommonPresenter
{
    public class TopBarBasePresenter : ContainerPresenter
    {
        public override UiLayer uiLayer { get { return UiLayer.BarRoot; } }

        #region UIs
        //Image buyIconImg;
      
        Button buyBtn;
        Button specialBtn;
        Button buyLongBtn;
        public GameObject buyLongBtnRoot { get; private set; }
        public GameObject shortBuyBtns { get; private set; }
        public GameObject buyIconObj { get; private set; }
        Image bgImg;
        Text specialTimeTxt;
        //Slider expBarSlider;
        Image expBarAmount;
        Text expNumTet;
        Button settingBtn;
        Button optionBtn;
        Image optionOuterRingImg;
        Image optionImg;
        GameObject optionListObj;
        Button tapBtn;
        Transform moneyPoint;

        #region expInfoBindingField
        Button expBtn;
        GameObject expInfoObj;
        GameObject expInfoListObj;
        Text expDoubleTxt;
        Text expLvUpNumTxt;
        GameObject infoBarObj;
        Text lvUpRewardTxt;
        //Text expTimeTxt;
        //Text expStateTxt;
        #endregion

        public BindingNode goldenPresenter { get; private set; }
        Animator expInfoAnim;
        #endregion

        TimerService limitTimeServices = new TimerService();
        TimerService bonusTimeService = new TimerService();
        //public bool isiOSSubmit { get { return DataStore.getInstance.dataInfo.isiOSSubmit; } }
        public PlayerInfo playerInfo { get { return DataStore.getInstance.playerInfo; } }

        public string expBarTween { get; private set; }
        public float previousExp;

        float previousMaxExp;
        float maxExp;
        bool isLvUp;
        List<string> expLoopInfo = new List<string>();
        ExpInfoNode expInfoNode;

        public override void initUIs()
        {
            goldenPresenter = getNodeData("golden_node");
            settingBtn = getBtnData("setting_btn");
            //buyIconImg = getImageData("buy_icon_img");
            buyIconObj = getGameObjectData("buy_icon_obj");
            buyBtn = getBtnData("buy_btn");
            buyLongBtn = getBtnData("buy_btn_long");
            shortBuyBtns = getGameObjectData("short_shop_btn");
            specialBtn = getBtnData("special_btn");
            buyLongBtnRoot = getGameObjectData("long_btn_root");
            specialTimeTxt = getTextData("special_time_txt");

            optionBtn = getBtnData("option_btn");
            optionImg = optionBtn.GetComponent<Image>();
            optionListObj = getGameObjectData("option_list_obj");
            optionOuterRingImg = getImageData("option_outring_img");
            expBarAmount = getImageData("exp_bar_img");
            //expBarSlider = getBindingData<Slider>("exp_bar_slider");
            expNumTet = getTextData("exp_num_txt");
            moneyPoint = getGameObjectData("money_point").transform;
            bgImg = getImageData("bg_img");
            tapBtn = getBtnData("tap_btn");
            #region expInfoBindingField
            expBtn = getBtnData("exp_btn");
            expInfoObj = getGameObjectData("exp_info_obj");
            expInfoListObj = getGameObjectData("exp_info_list_obj");
            expDoubleTxt = getTextData("exp_double_txt");
            infoBarObj = getGameObjectData("info_bar_obj");
            expLvUpNumTxt = getTextData("exp_lvup_num_txt");
            lvUpRewardTxt = getTextData("lvup_reward_txt");
            //expTimeTxt = getTextData("exp_time_txt");
            expInfoAnim = getAnimatorData("exp_info_anim");
            expInfoNode = UiManager.bindNode<ExpInfoNode>(getNodeData("exp_info_node").cachedGameObject);

            //expStateTxt = getTextData("exp_state_txt");
            #endregion
        }

        public override void init()
        {
            bonusTimeService.setAddToGo(uiGameObject);
            //expTimeService.setAddToGo(uiGameObject);
            expInfoListObj.setActiveWhenChange(false);

            expBtn.onClick.AddListener(openExpInfo);
            expInfoObj.setActiveWhenChange(false);
            changeBuyIcon(BuyType.None);

            buyBtn.onClick.AddListener(openShopPage);
            buyLongBtn.onClick.AddListener(openShopPage);
            specialBtn.onClick.AddListener(openLimitShop);
            optionBtn.onClick.AddListener(openOptionList);
            tapBtn.onClick.AddListener(tapBarClick);
            settingBtn.onClick.AddListener(openSettingPage);
            DataStore.getInstance.playerMoneyPresenter.addTo(moneyPoint);
            tapBtn.gameObject.setActiveWhenChange(false);
            //expBarSlider.onValueChanged.AddListener(expSliderOnValueChanged);
            DataStore.getInstance.dataInfo.bonusTimeSuscribe.Subscribe(checkGetBonusTime).AddTo(uiGameObject);

            //optionBtn.interactable = DataStore.getInstance.guideServices.nowStatus == GuideStatus.Completed;
            playerInfo.playerLvUpExpSubject.Subscribe(setLvUpExp).AddTo(uiGameObject);
            playerInfo.playerExpSubject.Subscribe(setPlayerExpValue).AddTo(uiGameObject);
            playerInfo.expBoostEndSubject.Subscribe(setExpBoosterTime).AddTo(uiGameObject);
            playerInfo.highRollerEndTimeSubject.Subscribe(setHighRollerExpUpTime).AddTo(uiGameObject);

            //buyLongBtn.interactable = DataStore.getInstance.guideServices.nowStatus == GuideStatus.Completed;
            setPlayerExpValue(playerInfo.playerExp);
            previousExp = playerInfo.playerExp;

            checkShopBtns(DataStore.getInstance.limitTimeServices.getLimitEndTime());
            DataStore.getInstance.limitTimeServices.limitEndTimeSub.Subscribe(checkShopBtns).AddTo(uiGameObject);

            checkExpInfoTime();
            expInfoNode.timeFinishCB = checkExpInfoTime;
        }

        float getExpBoosterPrecent()
        {
            float boosterPrecent = 1; ;

            if (DataStore.getInstance.playerInfo.hasHighRollerPermission)
            {
                boosterPrecent += 0.5f;
            }

            if (DataStore.getInstance.playerInfo.expBoostEndTime > UtilServices.nowTime)
            {
                boosterPrecent += 1.0f;
            }
            return boosterPrecent;
        }

        void setExpBoosterInfo()
        {
            float boosterPrecent = getExpBoosterPrecent();
            bool showExpTxt = boosterPrecent > 1;
            expDoubleTxt.text = $"{LanguageService.instance.getLanguageValue("LevelDirections_XPFeve")}\n<color=#fffc00>x{boosterPrecent} XP</color>";
            expDoubleTxt.gameObject.setActiveWhenChange(showExpTxt);
            infoBarObj.setActiveWhenChange(showExpTxt);
        }

        public async void setBGImgSprite(bool hasPermission)
        {
            string betClassSpriteName = getBetClassSpriteName(hasPermission);
            GameOrientation orientation = await DataStore.getInstance.dataInfo.getNowGameOrientation();
            string orientationStr = GameOrientation.Landscape == orientation ? string.Empty : $"_{orientation.ToString().ToLower()}";
            string spritePath = $"Bar_Resources/pic{orientationStr}/res_lobby_top_ui{orientationStr}/bg_ui_top_{betClassSpriteName}{orientationStr}";
            bgImg.sprite = ResourceManager.instance.load<Sprite>(spritePath);

            optionImg.sprite = getTopBarSprite($"btn_option_{betClassSpriteName}_1");
            optionOuterRingImg.sprite = getTopBarSprite($"bg_bar_button_{betClassSpriteName}");
        }

        public string getBetClassSpriteName(bool hasPermission)
        {
            return hasPermission ? "vip" : "normal";
        }

        public Sprite getTopBarSprite(string spriteName)
        {
            return CommonSpriteProvider.instance.getSprite<TopBarSpriteProvider>(CommonSpriteType.Topbar, spriteName);
        }

        public void changeBuyIcon(BuyType buyType)
        {
            buyIconObj.setActiveWhenChange(BuyType.None != buyType);
        }

        public void checkGetBonusTime(string availableAfter)
        {
            var afterTime = UtilServices.strConvertToDateTime(availableAfter, DateTime.MinValue);
            CompareTimeResult compareTimeResult = UtilServices.compareTimeWithNow(afterTime);

            if (CompareTimeResult.Earlier == compareTimeResult)
            {
                changeBuyIcon(BuyType.Gift);
                return;
            }
            changeBuyIcon(BuyType.None);
            bonusTimeService.ExecuteTimer();
            bonusTimeService.StartTimer(afterTime, countdownBonusTime);
        }

        public virtual void countdownBonusTime(TimeSpan timeSpan)
        {

        }

        public virtual void openSettingPage()
        {
            closeOptionListObj();
        }
        public virtual void openOptionList()
        {
            audioManagerPlay(BasicCommonSound.InfoBtn);
        }

        public void lobbyTempOpenClick()
        {
            openSettingPage();
        }

        public void gameOptionClick()
        {
            optionListObj.setActiveWhenChange(!optionListObj.activeSelf);
            tapBtn.gameObject.setActiveWhenChange(optionListObj.activeSelf);
        }

        void tapBarClick()
        {
            expInfoListObj.setActiveWhenChange(false);
            closeOptionListObj();
        }
        public void closeOptionListObj()
        {
            tapBtn.gameObject.setActiveWhenChange(false);
            optionListObj.setActiveWhenChange(false);
        }

        public void openTapBtn()
        {
            tapBtn.gameObject.setActiveWhenChange(true);
        }
        #region EXPInfo
        void addExpLoopInfo()
        {
            expLoopInfo.Clear();
        }

        void openExpInfo()
        {
            setLvupExpData();
            setExpBoosterInfo();
            expInfoListObj.setActiveWhenChange(true);
            openTapBtn();
        }

        void setLvupExpData()
        {
            var moreXP = playerInfo.LvUpExp - playerInfo.playerExp;
            if (moreXP < 0)
            {
                moreXP = 0;
            }
            expLvUpNumTxt.text = $"{LanguageService.instance.getLanguageValue("LevelDirections_MoreXP")}\n<color=#fffc00>{moreXP}</color>";
            lvUpRewardTxt.text = $"{LanguageService.instance.getLanguageValue("LevelDirections_Reward")}\n<color=#fffc00>{DataStore.getInstance.dataInfo.getLvupRewardData(UtilServices.outcomeCoinKey).ToString("N0")}</color>";
        }

        void checkExpInfoTime()
        {
            bool isHighRollerLaterNow = playerInfo.highRollerEndTime > UtilServices.nowTime;
            bool isExpBoosterLaterNow = playerInfo.expBoostEndTime > UtilServices.nowTime;

            if (!isHighRollerLaterNow && !isExpBoosterLaterNow)
            {
                expInfoObj.setActiveWhenChange(false);
                return;
            }

            if (isHighRollerLaterNow && isExpBoosterLaterNow)
            {
                if (playerInfo.highRollerEndTime > playerInfo.expBoostEndTime)
                {
                    openExpInfoRedeem(playerInfo.expBoostEndTime);
                }
                else
                {
                    openExpInfoRedeem(playerInfo.highRollerEndTime);
                }
                return;
            }

            if (isHighRollerLaterNow)
            {
                openExpInfoRedeem(playerInfo.highRollerEndTime);
                return;
            }

            if (isExpBoosterLaterNow)
            {
                openExpInfoRedeem(playerInfo.expBoostEndTime);
            }

        }

        void setHighRollerExpUpTime(DateTime highRollerTime)
        {
            checkExpInfoTime();
        }

        void setExpBoosterTime(DateTime expUpTime)
        {
            checkExpInfoTime();
        }

        void openExpInfoRedeem(DateTime redeemTime)
        {
            expInfoNode.setExpRedeemTime(redeemTime, getExpBoosterPrecent());
            bool isOpenInfo = redeemTime > UtilServices.nowTime;
            expInfoObj.setActiveWhenChange(isOpenInfo);
            if (isOpenInfo)
            {
                expInfoAnim.SetTrigger("expplus");
            }
        }

        #endregion
        public void setLvUpExp(long exp)
        {
            if (previousMaxExp > 0 && previousMaxExp != exp)
            {
                isLvUp = true;
                previousMaxExp = exp;
                return;
            }
            maxExp = exp;
            previousMaxExp = exp;
        }

        public void setPlayerExpValue(float exp)
        {
            if (previousExp == exp)
            {
                previousExp = exp;
                expBarAmount.fillAmount = exp / maxExp;
                setExpBarLvNum();
                return;
            }

            resetBarEffect();

            if (isLvUp)
            {
                exp = maxExp;
            }
            //Debug.Log($"setPlayerExpValue startTween {previousExp} to {exp} , isLvUp? {isLvUp}");
            startRunGameBarEffect();
            expBarTween = TweenManager.tweenToFloat(previousExp, exp, 1,
               onUpdate: setExpBarAmount,
               onComplete: expRunFinish);
        }

        void setExpBarAmount(float tweenAmount)
        {
            expBarAmount.fillAmount = tweenAmount / maxExp;
            expAmountOnValueChanged();
        }

        public virtual void resetBarEffect()
        {

        }

        public void expAmountOnValueChanged()
        {
            int expNum = (int)(expBarAmount.fillAmount * 100);
            expNumTet.text = $"{expNum}%";
            setBarEffectPos((float)Math.Round(expBarAmount.fillAmount, 2));
        }

        public virtual void setBarEffectPos(float SliderValue)
        {

        }

        public virtual void startRunGameBarEffect()
        {

        }

        public void setExpBarLvNum()
        {
            expNumTet.text = playerInfo.level.ToString();
        }

        public void lvupExpRunFinsih()
        {
            maxExp = previousMaxExp;
            setPlayerExpValue(DataStore.getInstance.playerInfo.playerExp);
            setExpBarLvNum();
            //Debug.Log($"lvupExpRunFinsih previousMaxExp : {previousMaxExp} , expBarSlider.maxValue :{expBarSlider.maxValue}");
        }

        void expRunFinish()
        {
            barEffectPlayOut();

            if (isLvUp)
            {
                isLvUp = false;
                previousExp = 0;
                runLvupObj();
                return;
            }
            previousExp = DataStore.getInstance.playerInfo.playerExp;
            Observable.Timer(TimeSpan.FromSeconds(0.5f)).Subscribe(_ => { setExpBarLvNum(); }).AddTo(uiGameObject);
        }

        public virtual void barEffectPlayOut()
        {

        }

        public virtual void runLvupObj()
        {

        }

        public void checkShopBtns(DateTime endTime)
        {
            bool isShowSpecialBtn = endTime > UtilServices.nowTime;
            shortBuyBtns.gameObject.setActiveWhenChange(isShowSpecialBtn);
            buyLongBtnRoot.gameObject.setActiveWhenChange(!isShowSpecialBtn);
            limitTimeServices.ExecuteTimer();
            if (isShowSpecialBtn)
            {
                limitTimeServices.setAddToGo(uiGameObject);
                limitTimeServices.StartTimer(endTime, updateSpecialTime);
                return;
            }
            DataStore.getInstance.limitTimeServices.limitSaleTimeFinish();
        }
        TimeStruct specialTimeStruct;
        void updateSpecialTime(TimeSpan endTime)
        {
            if (endTime <= TimeSpan.Zero)
            {
                limitTimeServices.ExecuteTimer();
                DataStore.getInstance.limitTimeServices.limitSaleTimeFinish();
                shortBuyBtns.gameObject.setActiveWhenChange(false);
                buyLongBtnRoot.gameObject.setActiveWhenChange(true);
                return;
            }

            specialTimeStruct = UtilServices.toTimeStruct(endTime);
            specialTimeTxt.text = specialTimeStruct.toTimeString(LanguageService.instance.getLanguageValue("Time_Days"));
        }

        public virtual void openShopPage()
        {
            audioManagerPlay(BasicCommonSound.InfoBtn);
        }

        public virtual void openLimitShop()
        {
            audioManagerPlay(BasicCommonSound.InfoBtn);
        }

        public void audioManagerPlay(BasicCommonSound commonSound)
        {
            AudioManager.instance.playAudioOnce(CommonAudioPathProvider.getAudioPath(commonSound));
        }
        public void audioManagerPlay(MainGameCommonSound commonSound)
        {
            AudioManager.instance.playAudioOnce(CommonAudioPathProvider.getAudioPath(commonSound));
        }
    }

    public enum BuyType
    {
        None,
        Gift,
        Sale,
    }

    public class ExpInfoNode : NodePresenter
    {
        GameObject daysObj;
        GameObject daysUnitObj;
        GameObject infoObj;
        GameObject infoUnitObj;

        Text timeTxt;
        Text daysTxt;
        Text infoNumTxt;

        TimerService expTimeService = new TimerService();

        public Action timeFinishCB;

        public override void initUIs()
        {
            string nowLanguage = ApplicationConfig.nowLanguage.ToString().ToLower();
            daysObj = getGameObjectData("days_obj");
            daysUnitObj = getGameObjectData($"days_obj_{nowLanguage}");
            timeTxt = getTextData("time_txt");
            daysTxt = getTextData("day_txt");

            infoObj = getGameObjectData("info_obj");
            infoUnitObj = getGameObjectData($"info_obj_{nowLanguage}");
            infoNumTxt = getTextData("info_plus_txt");
        }

        public override void init()
        {
            //infoObj.setActiveWhenChange(true);
            expTimeService.setAddToGo(uiGameObject);
            for (int i = 0; i < daysObj.transform.childCount; ++i)
            {
                daysObj.transform.GetChild(i).gameObject.setActiveWhenChange(false);
            }

            for (int i = 0; i < infoObj.transform.childCount; ++i)
            {
                infoObj.transform.GetChild(i).gameObject.setActiveWhenChange(false);
            }
            daysUnitObj.setActiveWhenChange(true);
            infoUnitObj.setActiveWhenChange(true);
            timeTxt.gameObject.setActiveWhenChange(true);
            daysTxt.gameObject.setActiveWhenChange(true);
            infoNumTxt.gameObject.setActiveWhenChange(true);
        }

        public void setExpRedeemTime(DateTime redeemTime, float boosterPrecent)
        {
            if (redeemTime <= UtilServices.nowTime)
            {
                close();
                return;
            }
            open();
            expTimeService.ExecuteTimer();
            TimeStruct timeStruct = UtilServices.toTimeStruct(redeemTime.Subtract(DateTime.UtcNow));
            daysObj.setActiveWhenChange(timeStruct.days > 0);
            timeTxt.gameObject.setActiveWhenChange(timeStruct.days <= 0);


            infoNumTxt.text = $"x {boosterPrecent}";
            if (timeStruct.days > 0)
            {
                daysTxt.text = timeStruct.toTimeString(string.Empty).Trim();
                return;
            }
            expTimeService.StartTimer(redeemTime, updateExpTime);
        }

        void updateExpTime(TimeSpan expTime)
        {
            timeTxt.text = UtilServices.formatCountTimeSpan(expTime);
            if (expTime <= TimeSpan.Zero)
            {
                expTimeService.ExecuteTimer();
                close();
                if (null != timeFinishCB)
                {
                    timeFinishCB();
                }
            }
        }
    }
}
