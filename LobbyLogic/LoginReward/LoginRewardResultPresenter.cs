using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using CommonILRuntime.Module;
using UniRx;
using UniRx.Triggers;
using CommonILRuntime.Services;
using CommonService;
using LobbyLogic.Audio;
using Lobby.Audio;
using CommonILRuntime.Outcome;

namespace LoginReward
{
    class LoginRewardResultPresenter : ContainerPresenter
    {
        public override string objPath => "prefab/login_reward/login_reward_result";

        public override UiLayer uiLayer { get => UiLayer.System; }

        Animator showAnim;
        Text daysNumTxt;
        RectTransform itemGroupRect;
        GameObject plusObj;
        Button collectBtn;
        RectTransform daysRect;
        public Action closeCB;

        ulong coinAmount;
        Outcome outcome;
        public override void initContainerPresenter()
        {
            resOrder = new string[] { AssetBundleData.getBundleName(BundleType.LoginReward) };
            base.initContainerPresenter();
        }
        public override void initUIs()
        {
            showAnim = getAnimatorData("anim_show");
            daysNumTxt = getTextData("days_num");
            itemGroupRect = getRectData("item_group");
            plusObj = getGameObjectData("plus_obj");
            collectBtn = getBtnData("collect_btn");
            daysRect = getRectData("days_rect");
        }

        public override void init()
        {
            plusObj.setActiveWhenChange(false);
            collectBtn.onClick.AddListener(collectBtnClick);
        }

        void collectBtnClick()
        {
            collectBtn.interactable = false;
            ulong startMoney = DataStore.getInstance.playerInfo.myWallet.deprecatedCoin;
            ulong endMoney = DataStore.getInstance.playerInfo.myWallet.coin;
            CoinFlyHelper.frontSFly(collectBtn.GetComponent<RectTransform>(), startMoney, endMoney, onComplete: closeAnim);
        }

        void closeAnim()
        {
            if (null != outcome)
            {
                outcome.apply();
            }
            var animtrigger = showAnim.GetBehaviours<ObservableStateMachineTrigger>();
            for (int i = 0; i < animtrigger.Length; ++i)
            {
                animtrigger[i].OnStateEnterAsObservable().ObserveOnMainThread().Subscribe(onAniOut).AddTo(uiGameObject);
            }
            showAnim.SetTrigger("out");
        }

        void onAniOut(ObservableStateMachineTrigger.OnStateInfo obj)
        {
            IDisposable animTimerDis = null;
            animTimerDis = Observable.Timer(TimeSpan.FromSeconds(obj.StateInfo.length)).Subscribe(_ =>
            {
                clear();
                animTimerDis.Dispose();
            }).AddTo(uiGameObject);
        }
        /// <summary>
        /// 30天登入
        /// </summary>
        public void openDaysPage(int daysNum, List<DayItemData> itemDatas, Outcome rewradOutcome)
        {
            outcome = rewradOutcome;
            if (ApplicationConfig.nowLanguage == ApplicationConfig.Language.EN)
            {
                daysNumTxt.text = $"{daysNum}th";
            }
            else
            {
                daysNumTxt.text = $"{daysNum}";
            }

            showAnim.SetTrigger("days_in");
            showItems(itemDatas);
            LayoutRebuilder.ForceRebuildLayoutImmediate(daysRect);
            playOpenRewardAudio();
        }
        /// <summary>
        /// 七日登入
        /// </summary>
        public void openDailyPage(List<DayItemData> itemDatas, Outcome rewradOutcome)
        {
            outcome = rewradOutcome;
            showAnim.SetTrigger("daily_in");
            showItems(itemDatas);
            playOpenRewardAudio();
        }

        void playOpenRewardAudio()
        {
            AudioManager.instance.playAudioOnce(AudioPathProvider.getAudioPath(ActivityBlastAudio.SmallWin));
        }

        void showItems(List<DayItemData> itemDatas)
        {
            for (int i = 0; i < itemDatas.Count; ++i)
            {
                var data = itemDatas[i];
                LoginRewardItemData.addDayItem(data, itemGroupRect, animatorEnable: true);
                if (DayItemType.Coin == data.itemType)
                {
                    coinAmount += data.amount;
                }
                if (i < itemDatas.Count - 1)
                {
                    PoolObject plusItem = ResourceManager.instance.getObjectFromPool(plusObj, itemGroupRect);
                    plusItem.name = plusObj.name;
                    plusItem.cachedGameObject.setActiveWhenChange(true);
                }
            }
            collectBtn.interactable = true;
        }

        public override void clear()
        {
            ResourceManager.instance.releasePoolWithObj(plusObj);
            base.clear();
            if (null != closeCB)
            {
                closeCB();
            }
        }
    }
}
