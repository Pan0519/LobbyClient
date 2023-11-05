using CommonILRuntime.Module;
using Services;
using CommonService;
using System;
using UnityEngine;
using UnityEngine.UI;
using UniRx;
using UniRx.Triggers;
using System.Collections.Generic;
using CommonILRuntime.Extension;

namespace CommonPresenter
{
    public class TopMiniGamePresenter : NodePresenter
    {
        #region UIs
        Animator miniGameAnimator;
        Image miniBar;
        GameObject miniLightObj;
        Animator miniLightAnim;
        Animator miniAnim;

        Text miniBonusText;
        Animator miniTimerAnimator;
        Text miniTimerText;
        Button collectBtn;
        RectTransform hintLayoutRect;
        Button hintBtn;
        GameObject hintObj;
        Text multiplierHintTitle;
        GameObject hintMultiplierObj;
        Button hintCloseBtn;
        Animator hintAnim;
        Text hintInfoTxt;
        #endregion

        GameObject[] explainObjs = new GameObject[2];
        TimerService timer = new TimerService();
        int totalEnergy = 0;
        public long bonusCoin;
        public Action<long> openPriceNode;

        MiniGameConfig miniConfig { get { return DataStore.getInstance.miniGameData; } }
        float effectBarHeight;
        int lastBonusLv;
        IDisposable openHintTimer;
        OpenType openType = OpenType.None;
        string multiplierHint;
        float loopTime = 5;
        public override void initUIs()
        {
            miniGameAnimator = getAnimatorData("mini_game_anim");
            miniBar = getImageData("mini_bar");
            miniLightObj = getGameObjectData("mini_light_obj");
            miniLightAnim = getAnimatorData("mini_light_anim");

            miniAnim = getAnimatorData("mini_anim");

            miniBonusText = getTextData("mini_bonus_txt");
            miniTimerAnimator = getAnimatorData("mini_timer_anm");
            miniTimerText = getTextData("mini_timer_txt");
            collectBtn = getBtnData("collect_btn");
            hintLayoutRect = getBindingData<RectTransform>("hint_layout_rect");
            hintBtn = getBtnData("hint_btn");
            hintObj = getGameObjectData("hint");
            multiplierHintTitle = getTextData("multiplier_hint_title");
            hintMultiplierObj = getGameObjectData("hint_multiplier_obj");
            hintCloseBtn = getBtnData("hint_close_btn");
            hintAnim = getAnimatorData("hint_anim");
            hintInfoTxt = getTextData("hint_info_txt");
            for (int i = 0; i < explainObjs.Length; ++i)
            {
                explainObjs[i] = getGameObjectData($"explain_{i + 1}_obj");
            }
        }

        public override void init()
        {
            timer.setAddToGo(uiGameObject);
            lastBonusLv = 0;

            collectBtn.onClick.AddListener(onCollectClick);

            hintBtn.onClick.AddListener(openInfoHint);
            hintObj.setActiveWhenChange(false);
            miniLightObj.setActiveWhenChange(false);
            hintCloseBtn.onClick.AddListener(closeHintClick);
            miniConfig.topbarGameBonusSub.Subscribe(initMiniGameDatas).AddTo(uiGameObject);
            miniConfig.addBonusEnergySub.Subscribe(calculationEnergyBarSub).AddTo(uiGameObject);
            DataStore.getInstance.gameToLobbyService.sendStayGameServer();
            hintBtn.interactable = DataStore.getInstance.guideServices.nowStatus == GuideStatus.Completed;
            initHintObj();
        }

        void initHintObj()
        {
            for (int i = 0; i < explainObjs.Length; ++i)
            {
                explainObjs[i].setActiveWhenChange(true);
            }
            hintObj.setActiveWhenChange(true);
            LayoutRebuilder.ForceRebuildLayoutImmediate(hintLayoutRect);
            Observable.Timer(TimeSpan.FromSeconds(0.3f)).Subscribe(_ =>
            {
                hintObj.setActiveWhenChange(false);
            }).AddTo(uiGameObject);
        }

        void initMiniGameDatas(TopBarGameBonusInfo gameInfo)
        {
            miniBar.fillAmount = 0;
            totalEnergy = 0;
            getBonusTimes();
            calculationEnergyBar(gameInfo.multiplierEnergy, isRunBar: false);
            loopStayGameIcon();
        }

        void calculationEnergyBarSub(int energy)
        {
            calculationEnergyBar(energy);
        }
        void calculationEnergyBar(int energy, bool isRunBar = true)
        {
            totalEnergy += energy;
            int bonusLv = miniConfig.bonusList[0];
            int expLv = 1;
            for (int i = 1; i < miniConfig.expList.Count; ++i)
            {
                if (totalEnergy < miniConfig.expList[i - 1])
                {
                    break;
                }
                bonusLv = miniConfig.bonusList[i - 1];
                expLv = i;
            }
            if (totalEnergy >= miniConfig.expList[miniConfig.expList.Count - 1])
            {
                miniBar.fillAmount = 1;
                setBounsTxt(bonusLv);
                return;
            }

            float endAmount = getBarEndAmout(expLv);
            if (!isRunBar)
            {
                miniBar.fillAmount = endAmount;
                barRunComplete(bonusLv);
                return;
            }
            UtilServices.disposeSubscribes(barEffectAnimDis);

            miniLightObj.setActiveWhenChange(true);
            TweenManager.tweenToFloat(miniBar.fillAmount, endAmount, 0.3f, onUpdate: runBarFillAmount, onComplete: () =>
            {
                barRunComplete(bonusLv);
            });
        }

        void setBounsTxt(int lv)
        {
            miniBonusText.text = $"X{lv}";
        }

        float getBarEndAmout(int expLv)
        {
            float exp = miniConfig.expList[expLv];
            float lastExp = miniConfig.expList[expLv - 1]; ;
            float expRange = exp - lastExp;
            float energyRange = totalEnergy - lastExp;
            double result = energyRange / expRange;
            result = Math.Round(result, 3);
            if (result < miniBar.fillAmount)
            {
                return 1;
            }
            return (float)result;
        }

        void runBarFillAmount(float amount)
        {
            miniBar.fillAmount = amount;
        }

        void barRunComplete(int bonusLv)
        {
            if (lastBonusLv != 0 && bonusLv > lastBonusLv)
            {
                Observable.Timer(TimeSpan.FromSeconds(1.0f)).Subscribe(_ =>
                {
                    multiplierHint = $"{LanguageService.instance.getLanguageValue("CrushBonus_GameSever_LVUPText")} X{bonusLv}";

                    if (openType != OpenType.None)
                    {
                        closeHintObj();
                        openType = OpenType.Multiplier;
                    }
                    else
                    {
                        openHint(OpenType.Multiplier);
                    }
                    miniAnim.SetTrigger("plus");
                    Observable.TimerFrame(21).Subscribe(timer =>
                    {
                        setBounsTxt(bonusLv);
                        miniBar.fillAmount = 0;
                        calculationEnergyBar(0);
                    }).AddTo(uiGameObject);
                }).AddTo(uiGameObject);
            }
            else
            {
                setBounsTxt(bonusLv);
            }
            lastBonusLv = bonusLv;
            miniLightOutAnim();
        }

        IDisposable miniOutAnim;
        void miniLightOutAnim()
        {
            UtilServices.disposeSubscribes(miniOutAnim);
            var animtrigger = miniLightAnim.GetBehaviour<ObservableStateMachineTrigger>();
            if (null != animtrigger)
            {
                miniOutAnim = animtrigger.OnStateEnterAsObservable().ObserveOnMainThread().Subscribe(onMiniLightOut);
            }
            miniLightAnim.SetTrigger("off");
        }

        void onMiniLightOut(ObservableStateMachineTrigger.OnStateInfo obj)
        {
            barEffectAnimDis = Observable.Timer(TimeSpan.FromSeconds(obj.StateInfo.length)).Subscribe(_ =>
            {
                miniLightObj.setActiveWhenChange(false);

            }).AddTo(uiGameObject);
        }

        IDisposable barEffectAnimDis = null;
        CompareBonusTimeResult compareTimeResult;
        void getBonusTimes()
        {
            compareTimeResult = miniConfig.compareBonusTime();
            bool isCountdownTime = compareTimeResult.isCountdownTime;
            miniTimerAnimator.gameObject.setActiveWhenChange(isCountdownTime);
            if (!isCountdownTime)
            {
                return;
            }
            if (null != timer)
            {
                timer.ExecuteTimer();
            }
            timer.StartTimer(compareTimeResult.countdownTime, setTime);
        }

        void setTime(TimeSpan lastTime)
        {
            miniTimerText.text = UtilServices.toTimeStruct(lastTime).toTimeString();
            if (lastTime <= TimeSpan.Zero)
            {
                timer.ExecuteTimer();
                resetLoopStayIcon();
                DataStore.getInstance.gameToLobbyService.sendStayGameServer();
            }
        }

        int nowLoopID = 0;
        IDisposable loopTimerDis;

        void resetLoopStayIcon()
        {
            nowLoopID = 0;
        }
        List<IDisposable> outAnimDis = new List<IDisposable>();
        StayGameType loopType;
        void loopStayGameIcon()
        {
            miniGameAnimator.enabled = false;
            collectBtn.gameObject.setActiveWhenChange(false);
            if (DataStore.getInstance.guideServices.nowStatus != GuideStatus.Completed)
            {
                return;
            }

            if (miniConfig.loopTypes.Count <= 1)
            {
                loopTime = 5;
                return;
            }
            loopTime = 6;
            loopType = miniConfig.loopTypes[nowLoopID];
            nowLoopID++;
            if (nowLoopID >= miniConfig.loopTypes.Count)
            {
                nowLoopID = 0;
            }
            //miniGameAnimator.enabled = false;
            switch (loopType)
            {
                case StayGameType.gold:
                case StayGameType.silver:
                    collectBtn.gameObject.setActiveWhenChange(true);
                    miniGameAnimator.enabled = true;
                    miniGameAnimator.SetTrigger($"{loopType}");
                    loopTimer(() =>
                    {
                        var animtrigger = miniGameAnimator.GetBehaviours<ObservableStateMachineTrigger>();
                        for (int i = 0; i < animtrigger.Length; ++i)
                        {
                            outAnimDis.Add(animtrigger[i].OnStateEnterAsObservable().ObserveOnMainThread().Subscribe(onAniOut));
                        }
                        miniGameAnimator.SetTrigger("time_out");
                    });

                    return;
            }

            collectBtn.gameObject.setActiveWhenChange(false);
            loopTimer(() =>
            {
                loopStayGameIcon();
            });
        }

        void loopTimer(Action cb)
        {
            loopTimerDis = Observable.Timer(TimeSpan.FromSeconds(loopTime)).Subscribe(_ =>
            {
                loopTimerDis.Dispose();
                if (null != cb)
                {
                    cb();
                }
            }).AddTo(uiGameObject);
        }

        void onAniOut(ObservableStateMachineTrigger.OnStateInfo obj)
        {
            UtilServices.disposeSubscribes(outAnimDis.ToArray());
            outAnimDis.Clear();
            IDisposable animTimerDis = null;
            animTimerDis = Observable.Timer(TimeSpan.FromSeconds(obj.StateInfo.length)).Subscribe(_ =>
               {
                   loopStayGameIcon();
                   animTimerDis.Dispose();
               }).AddTo(uiGameObject);
        }

        void onCollectClick()
        {
            collectBtn.interactable = false;
            miniGameAnimator.SetTrigger("time_out");
            string gameType = DataStore.getInstance.miniGameData.boxRedeemStr[(int)loopType];
            DataStore.getInstance.gameToLobbyService.sendStayGameRedeem(gameType);
            miniConfig.removeLoopType(loopType);
            UtilServices.disposeSubscribes(loopTimerDis);
            resetLoopStayIcon();
        }

        void openInfoHint()
        {
            openHint(OpenType.Info);
        }

        void openHint(OpenType type)
        {
            openType = type;
            for (int i = 0; i < explainObjs.Length; ++i)
            {
                explainObjs[i].setActiveWhenChange(OpenType.Info == openType);
            }
            hintMultiplierObj.setActiveWhenChange(OpenType.Multiplier == openType);
            multiplierHintTitle.text = multiplierHint;
            hintObj.setActiveWhenChange(true);
            LayoutRebuilder.ForceRebuildLayoutImmediate(hintLayoutRect);
            countdownCloseHint();
        }

        void countdownCloseHint()
        {
            openHintTimer = Observable.Timer(TimeSpan.FromSeconds(5.0f)).Subscribe(_ =>
            {
                closeHintClick();
            }).AddTo(uiGameObject);
        }

        void closeHintClick()
        {
            openType = OpenType.None;
            closeHintObj();
        }

        void closeHintObj()
        {
            hintBtn.interactable = false;
            hintCloseBtn.interactable = false;
            openHintTimer.Dispose();

            var animtrigger = hintAnim.GetBehaviour<ObservableStateMachineTrigger>();
            animtrigger.OnStateEnterAsObservable().ObserveOnMainThread().Subscribe(onHintAniOut).AddTo(uiGameObject);
            hintAnim.SetTrigger("out");
        }

        void onHintAniOut(ObservableStateMachineTrigger.OnStateInfo obj)
        {
            IDisposable animTimerDis = null;
            animTimerDis = Observable.Timer(TimeSpan.FromSeconds(obj.StateInfo.length)).Subscribe(_ =>
            {
                hintCloseBtn.interactable = true;
                hintBtn.interactable = true;
                hintObj.setActiveWhenChange(false);
                animTimerDis.Dispose();

                if (OpenType.None != openType)
                {
                    openHint(openType);
                }
            });
        }
        public void priceShowFinish()
        {
            collectBtn.interactable = true;
            getBonusTimes();
        }
    }

    enum OpenType
    {
        None,
        Info,
        Multiplier,
    }
}
