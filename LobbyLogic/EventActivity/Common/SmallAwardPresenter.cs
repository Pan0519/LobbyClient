using System;
using System.Collections.Generic;
using UnityEngine.UI;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using CommonILRuntime.Module;
using EventActivity;
using Services;
using LobbyLogic.Audio;
using Lobby.Audio;

namespace Event.Common
{
    class SmallAwardPresenter : PrizeAward
    {
        public override string objPath { get { return "prefab/activity/rookie/rookie_small_prize"; } }

        public override UiLayer uiLayer { get { return UiLayer.System; } }

        #region [ Uis & unity component ]
        Animator showAnim;
        Text coinTxt;
        CustomTextSizeChange buffMoreTxt;
        CustomTextSizeChange ticketTxt;
        Button collectBtn;
        #endregion

        public Action finishCB;

        private ObservableStateMachineTrigger[] aniEndTrigger;
        private List<IDisposable> aniDispose;
        private string animName;

        AwardKind awardType;
        RectTransform coinFlySource;
        public override float YOffset { get => 5.0f; }

        public override void initUIs()
        {
            base.initUIs();
            showAnim = getAnimatorData("ani_Show");
            collectBtn = getBtnData("btn_CollectAndClose");
            coinTxt = getTextData("text_Coin");
            buffMoreTxt = getBindingData<CustomTextSizeChange>("text_BuffMore");
            ticketTxt = getBindingData<CustomTextSizeChange>("text_Ticket");
        }

        public override void init()
        {
            coinFlySource = collectBtn.GetComponent<RectTransform>();
            aniEndTrigger = showAnim.GetBehaviours<ObservableStateMachineTrigger>();
            aniDispose = new List<IDisposable>();
            for (int i = 0; i < aniEndTrigger.Length; ++i)
            {
                aniDispose.Add(aniEndTrigger[i].OnStateEnterAsObservable().ObserveOnMainThread().Subscribe(onAniOut));
            }
        }

        public override Button getCollectBtn()
        {
            return collectBtn;
        }

        public override Text getCoinTxt()
        {
            return coinTxt;
        }

        public override void clear()
        {
            UtilServices.disposeSubscribes(aniDispose.ToArray());
            base.clear();
        }

        public SmallAwardPresenter openAwardPage(GameObject prizeItem)
        {
            setPrizeItem(prizeItem);
            getCollectBtn().interactable = false;
            collectBtn.onClick.AddListener(closeBtnClick);
            open();
            showAnim.SetTrigger(animName);
            AudioManager.instance.playAudioOnce(AudioPathProvider.getAudioPath(ActivityBlastAudio.SmallWin));
            return this;
        }

        public SmallAwardPresenter setAwardStatus(ActivityAwardData awardData)
        {
            awardType = awardData.kind;
            setAwardValue(awardData.amount);

            switch (awardType)
            {
                case AwardKind.Coin:
                    coinTxt.text = prizeBoosterOriginalReward.ToString("N0");
                    animName = "prize";
                    break;
                case AwardKind.BuffMore:
                    buffMoreTxt.text = $"{awardValue}%";
                    animName = "more";
                    break;
                case AwardKind.Ticket:
                    ticketTxt.text = awardValue.ToString();
                    animName = "ticket";
                    break;
                default:
                    Debug.LogWarning("undefind award kind : " + awardType);
                    break;
            }

            return this;
        }

        public SmallAwardPresenter setEndEvent(Action afterEndAct)
        {
            finishCB = afterEndAct;
            return this;
        }

        void closeBtnClick()
        {
            AudioManager.instance.stopOnceAudio();
            ActivityDataStore.playClickAudio();
            collectBtn.onClick.RemoveAllListeners();
            if (AwardKind.Coin == awardType)
            {
                ActivityDataStore.coinFly(coinFlySource, awardValue, coinFlyComplete);
                return;
            }
            showAnim.SetTrigger("out");
        }

        void coinFlyComplete()
        {
            showAnim.SetTrigger("out");
        }

        private void onAniOut(ObservableStateMachineTrigger.OnStateInfo obj)
        {
            Observable.Timer(TimeSpan.FromSeconds(obj.StateInfo.length)).Subscribe(_ =>
            {
                if (obj.StateInfo.IsName($"rookie_small_{animName}_out"))
                {
                    if (null != finishCB)
                    {
                        finishCB();
                    }
                    clear();
                    return;
                }

                checkIsShowPrizeBooster();
            }).AddTo(uiGameObject);
        }
    }
}
