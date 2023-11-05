using CommonILRuntime.Module;
using CommonILRuntime.BindingModule;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using UniRx.Triggers;
using UniRx;
using Services;
using Service;
using System.Threading.Tasks;
using LobbyLogic.NetWork.ResponseStruct;
using EventActivity;
using Lobby.Audio;
using LobbyLogic.Audio;
using Network;

namespace FrenzyJourney
{
    class DiceNodePresenter : NodePresenter
    {
        #region UIs
        Text diceCountTxt;
        Animator diceInAnim;
        Animator diceGetAnim;

        GameObject redGroupObj;
        Animator redAnim;
        CustomBtn redBtn;
        RectTransform redRect;

        GameObject blueGroupObj;
        Animator blueAnim;
        CustomBtn blueBtn;
        RectTransform blueRect1;
        RectTransform blueRect2;
        Text autoInfoTxt;
        GameObject blueTotalDiceCountObj;
        Text blueTotalDiceCountTxt;
        #endregion

        public Action<JourneyPlayResponse> diceClickEvent;
        public Action<JourneyBossPlayResponse> bossDiceClickEvent;
        public Action shopSpinEvent;
        Dictionary<DiceTextType, Text[]> diceNumTxts = new Dictionary<DiceTextType, Text[]>();
        public int totalTicketCount { get; private set; }
        //List<IDisposable> animTriggerDis = new List<IDisposable>();
        DiceType nowDiceType;
        Animator nowAnimator;
        JourneyBossPlayResponse bossRespose;
        JourneyPlayResponse mainResponse;

        IDisposable animTriggerDis;
        public override void initUIs()
        {
            diceCountTxt = getTextData("dice_count_txt");
            diceInAnim = getAnimatorData("dice_in_anim");
            diceGetAnim = getAnimatorData("deice_get_anim");

            redGroupObj = getGameObjectData("red_group");
            redAnim = getAnimatorData("red_dice_anim");
            redBtn = getCustomBtnData("red_dice_btn");
            redRect = getBindingData<RectTransform>("red_dice_body_rect");

            blueGroupObj = getGameObjectData("blue_group");
            blueAnim = getAnimatorData("blue_dice_anim");
            blueBtn = getCustomBtnData("blue_dice_btn");
            blueRect1 = getBindingData<RectTransform>("blue_dice_body_rect_1");
            blueRect2 = getBindingData<RectTransform>("blue_dice_body_rect_2");

            autoInfoTxt = getTextData("auto_info_txt");
            blueTotalDiceCountObj = getGameObjectData("total_dice_obj");
            blueTotalDiceCountTxt = getTextData("total_dice_num");
        }

        public override void init()
        {
            initDiceTextData(DiceTextType.Red, redRect);
            initDiceTextData(DiceTextType.Blue1, blueRect1);
            initDiceTextData(DiceTextType.Blue2, blueRect2);

            redBtn.setLongPressTime(1.5f);
            blueBtn.setLongPressTime(1.5f);

            redBtn.clickHandler = redBtnClick;
            blueBtn.clickHandler = blueBtnClick;

            redBtn.onLongPress = redBtnLongClick;
            blueBtn.onLongPress = blueBtnLongClick;

            isAutoPlayingSubject(false);
            FrenzyJourneyData.getInstance.isAutoPlayingSub.Subscribe(isAutoPlayingSubject).AddTo(uiGameObject);
            ActivityDataStore.totalTicketCountUpdateSub.Subscribe(setNowDiceTotalCount).AddTo(uiGameObject);
        }

        public override void open()
        {
            base.open();
            diceInAnim.enabled = false;
        }

        private void onAniEnd(ObservableStateMachineTrigger.OnStateInfo obj)
        {
            Observable.TimerFrame(80).Subscribe(_ =>
            {
                AudioManager.instance.stopLoop();
                AudioManager.instance.playAudioOnce(AudioPathProvider.getAudioPath(ActivityFJAudio.DiceStop));
            }).AddTo(uiGameObject);

            IDisposable animTimerDis = null;
            animTimerDis = Observable.Timer(TimeSpan.FromSeconds(obj.StateInfo.length)).Subscribe(_ =>
             {
                 animTimerDis.Dispose();
                 diceRunComplete();
             }).AddTo(uiGameObject);
        }
        public void setBtnsRaycastTarget(bool enable)
        {
            redBtn.GetComponent<Image>().raycastTarget = enable;
            blueBtn.GetComponent<Image>().raycastTarget = enable;
        }

        async void diceRunComplete()
        {
            if (DiceType.blue == nowDiceType)
            {
                blueTotalDiceCountObj.setActiveWhenChange(true);
                await Task.Delay(TimeSpan.FromSeconds(1.1f));
                blueTotalDiceCountObj.setActiveWhenChange(false);
            }

            switch (FrenzyJourneyData.getInstance.frenzySceneType)
            {
                case FrenzySceneType.Main:
                    if (null != diceClickEvent)
                    {
                        diceClickEvent(mainResponse);
                    }
                    break;

                case FrenzySceneType.Boss:
                    if (null != bossDiceClickEvent)
                    {
                        bossDiceClickEvent(bossRespose);
                    }
                    break;
            }
        }

        void redBtnLongClick()
        {
            if (FrenzyJourneyData.getInstance.isAutoPlaying)
            {
                return;
            }
            runRedDice();
            FrenzyJourneyData.getInstance.startAutoPlay();
        }

        void redBtnClick()
        {
            if (FrenzyJourneyData.getInstance.isAutoPlaying)
            {
                FrenzyJourneyData.getInstance.stopAutoPlay();
                return;
            }
            runRedDice();
        }

        void runRedDice()
        {
            nowAnimator = redAnim;
            diceRun();
        }

        void blueBtnLongClick()
        {
            if (FrenzyJourneyData.getInstance.isAutoPlaying)
            {
                return;
            }
            runBlueDice();
            FrenzyJourneyData.getInstance.startAutoPlay();
        }

        void blueBtnClick()
        {
            if (FrenzyJourneyData.getInstance.isAutoPlaying)
            {
                FrenzyJourneyData.getInstance.stopAutoPlay();
                return;
            }
            runBlueDice();
        }

        void runBlueDice()
        {
            nowAnimator = blueAnim;
            diceRun();
        }

        void diceRun()
        {
            if (FrenzyJourneyData.getInstance.isShowRunning)
            {
                Debug.LogError("FrenzyJourneyData.getInstance.isShowRunning");
                return;
            }
            if (totalTicketCount <= 0)
            {
                UiManager.getPresenter<JourneyShopPresenter>().openShop(isShowSpinObj: true, shopSpinEvent);
                FrenzyJourneyData.getInstance.stopAutoPlay();
                return;
            }

            UtilServices.disposeSubscribes(animTriggerDis);
            FrenzyJourneyData.getInstance.showRunning(true, "diceRun");
            var animTriggers = nowAnimator.GetBehaviour<ObservableStateMachineTrigger>();
            animTriggerDis = animTriggers.OnStateEnterAsObservable().ObserveOnMainThread().Subscribe(onAniEnd).AddTo(uiGameObject);
            nowAnimator.SetTrigger("run");
            AudioManager.instance.playAudioLoop(AudioPathProvider.getAudioPath(ActivityFJAudio.DiceLoop), true);
            diceClick();
        }

        void diceClick()
        {
            setNowDiceTotalCount(totalTicketCount - 1);
            if (nowDiceType == DiceType.blue)
            {
                FrenzyJourneyData.getInstance.updateFrenzyDiceCount(FrenzyJourneyData.getInstance.frenzyDiceCount - 1);
            }

            if (FrenzySceneType.Main == FrenzyJourneyData.getInstance.frenzySceneType)
            {
                sendMainPlay();
                return;
            }

            sendBossPlay();
        }

        async void sendMainPlay()
        {
            FrenzyJourneyData.getInstance.chessStartMoving();
            mainResponse = await AppManager.eventServer.sendFrenzyJourneyPlay();
            if (Result.OK != mainResponse.result)
            {
                isShowServerGameEnd(mainResponse.result);
                return;
            }
            setServerDiceNum(mainResponse.DiceIndex);
            totalTicketCount = mainResponse.Ticket;
        }

        async void sendBossPlay()
        {
            bossRespose = await AppManager.eventServer.sendBossPlay();
            if (Result.OK != bossRespose.result)
            {
                isShowServerGameEnd(bossRespose.result);
                return;
            }
            setServerDiceNum(bossRespose.BossData.TotalDiceIndex);
            totalTicketCount = bossRespose.Ticket;
        }

        void isShowServerGameEnd(Result playResult)
        {
            if (Result.ActivityIDPromotedError == playResult)
            {
                FrenzyJourneyData.getInstance.showGameEndMsg();
            }
        }

        public void setNowDiceTotalCount(int count)
        {
            totalTicketCount = count;
            updateTicketCount();
            ActivityDataStore.pageAmountChange(count);
        }

        public void addDiceTotalCount(int addCount)
        {
            diceInAnim.enabled = true;
            totalTicketCount += addCount;
            updateTicketCount();
            Observable.Timer(TimeSpan.FromSeconds(0.5f)).Subscribe(_ =>
            {
                diceInAnim.enabled = false;
            }).AddTo(uiGameObject);
        }

        void updateTicketCount()
        {
            //string showCount = totalTicketCount <= 99 ? totalTicketCount.ToString() : "99+";
            string showCount = totalTicketCount.ToString();
            diceCountTxt.text = $"{LanguageService.instance.getLanguageValue("FrenzyJourney_DICE_Left")} {showCount}";
        }

        public void showDiceBody()
        {
            checkDiceType();
            showRandomDice();
        }

        public void checkDiceType()
        {
            bool isShowBlueDice = FrenzyJourneyData.getInstance.frenzyDiceCount > 0;
            blueGroupObj.setActiveWhenChange(isShowBlueDice);
            redGroupObj.setActiveWhenChange(!isShowBlueDice);
            nowDiceType = !isShowBlueDice ? DiceType.red : DiceType.blue;
        }

        public void diceAutoClick()
        {
            switch (nowDiceType)
            {
                case DiceType.red:
                    runRedDice();
                    break;

                case DiceType.blue:
                    runBlueDice();
                    break;
            }
        }

        public void playDiceGetAnim()
        {
            diceGetAnim.SetTrigger($"{nowDiceType}");
        }

        public void setServerDiceNum(int[] index)
        {
            if (DiceType.red == nowDiceType)
            {
                setDiceNum(diceNumTxts[DiceTextType.Red], index[0]);
                return;
            }

            int totalDiceCount = 0;
            for (int i = 1; i <= (int)DiceTextType.Blue2; ++i)
            {
                totalDiceCount += setDiceNum(diceNumTxts[(DiceTextType)i], index[i - 1]);
            }

            blueTotalDiceCountTxt.text = $"+{totalDiceCount}";
        }

        void initDiceTextData(DiceTextType diceType, RectTransform diceRect)
        {
            if (diceNumTxts.ContainsKey(diceType))
            {
                return;
            }
            Text[] numTxts = diceRect.gameObject.GetComponentsInChildren<Text>();
            diceNumTxts.Add(diceType, numTxts);
        }

        public void showRandomDice()
        {
            resetDiceNumDatas();
            setRandomDiceNum(diceNumTxts[DiceTextType.Red], diceNumData);
            for (int i = 1; i <= (int)DiceTextType.Blue2; ++i)
            {
                setRandomDiceNum(diceNumTxts[(DiceTextType)i], diceNumData);
                resetDiceNumDatas();
            }
        }

        void setRandomDiceNum(Text[] numTxts, List<int> diceNums)
        {
            System.Random rand = new System.Random(Guid.NewGuid().GetHashCode());
            List<int> randomDiceCount = diceNums;
            for (int i = 0; i < numTxts.Length - 1; ++i)
            {
                var randomId = rand.Next(0, randomDiceCount.Count - 1);
                numTxts[i].text = randomDiceCount[randomId].ToString();
                randomDiceCount.RemoveAt(randomId);
            }
        }

        void isAutoPlayingSubject(bool isAutoPlaying)
        {
            if (isAutoPlaying)
            {
                autoInfoTxt.text = LanguageService.instance.getLanguageValue("FrenzyJourney_DICE_Stop");
                return;
            }
            autoInfoTxt.text = LanguageService.instance.getLanguageValue("FrenzyJourney_DICE_Auto");
        }

        List<int> diceNumData = new List<int>();

        int setDiceNum(Text[] numTxts, int index)
        {
            resetDiceNumDatas();
            diceNumData.RemoveAt(index);
            setRandomDiceNum(numTxts, diceNumData);
            int diceNum = FrenzyJourneyData.getInstance.diceNums[index];
            numTxts[0].text = diceNum.ToString();
            return diceNum;
        }

        List<int> resetDiceNumDatas()
        {
            diceNumData.Clear();
            diceNumData.AddRange(FrenzyJourneyData.getInstance.diceNums);
            return diceNumData;
        }
    }

    enum DiceType
    {
        red,
        blue,
    }

    enum DiceTextType
    {
        Red = 0,
        Blue1,
        Blue2,
    }
}
