using CommonILRuntime.Extension;
using CommonILRuntime.Module;
using CommonILRuntime.Services;
using CommonService;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UniRx;
using LobbyLogic.Audio;
using Lobby.Audio;
using Game.Common;
using CommonILRuntime.BindingModule;
using CommonPresenter;

namespace StayMiniGame
{
    public class StayMiniGameAwardsPresenter : SystemUIBasePresenter
    {
        public override string objPath => "prefab/stay_minigame/stay_minigame_result";
        public override UiLayer uiLayer { get => UiLayer.System; }

        #region UIs
        Animator awardsAnm;
        RectTransform moneyGroup;
        Text coinTxt;
        Button collectBtn;
        Text rewardTxt;
        Text vipTxt;
        Text muiltiplierTxt;
        Text totalTxt;
        RectTransform resultGroupTrans;
        GameObject rewardObj;
        GameObject vipObj;
        GameObject muiltiplierObj;
        GameObject totalObj;
        RectTransform dcMoreRect;
        Animator moreAnim;
        #endregion

        Queue<GameObject> showOrders = new Queue<GameObject>();
        public override void initContainerPresenter()
        {
            resOrder = new string[] { AssetBundleData.getBundleName(BundleType.StayMinigame) };
            base.initContainerPresenter();
        }
        public override void initUIs()
        {
            awardsAnm = getAnimatorData("ani_Show");
            moneyGroup = getBindingData<RectTransform>("money_group");
            coinTxt = getTextData("text_Coin");
            collectBtn = getBtnData("btn_Close");
            rewardTxt = getTextData("reward_txt");
            vipTxt = getTextData("vip_txt");
            muiltiplierTxt = getTextData("muiltiplier_txt");
            totalTxt = getTextData("total_txt");
            resultGroupTrans = getBindingData<RectTransform>("result_group_trans");
            rewardObj = getGameObjectData("reward_obj");
            vipObj = getGameObjectData("vip_obj");
            muiltiplierObj = getGameObjectData("muiltiplier_obj");
            totalObj = getGameObjectData("total_obj");
            dcMoreRect = getRectData("dc_more_rect");
            moreAnim = getAnimatorData("reward_anim");
        }

        public override Animator getUiAnimator()
        {
            return awardsAnm;
        }

        public override void init()
        {
            base.init();
            collectBtn.onClick.AddListener(onCollectClick);
            collectBtn.interactable = false;
            showOrders.Enqueue(rewardObj);
            showOrders.Enqueue(vipObj);
            showOrders.Enqueue(muiltiplierObj);
            showOrders.Enqueue(totalObj);
        }
        PoolObject rewardFlyGroup;
        const float rewardFlyTime = 0.3f;
        string scaleTween;
        public override void open()
        {
            dcMoreRect.gameObject.setActiveWhenChange(false);
            setData();
            base.open();
            AudioManager.instance.playAudioOnce(AudioPathProvider.getAudioPath(BonusAudio.Award));
            rewardFlyGroup = ResourceManager.instance.getObjectFromPool(moneyGroup.gameObject, moneyGroup.parent.transform);
            rewardFlyGroup.cachedGameObject.setActiveWhenChange(false);
            rewardFlyGroup.cachedGameObject.name = moneyGroup.gameObject.name;
            IDisposable showTimerDis = null;
            showTimerDis = Observable.TimerFrame(90).Subscribe(_ =>
            {
                coinGroupFly();
                showTimerDis.Dispose();
            }).AddTo(uiGameObject);
        }

        void coinGroupFly()
        {
            Vector3 rewardPos = rewardTxt.transform.position;
            rewardPos.Set(rewardPos.x, rewardPos.y - 0.1f, rewardPos.z);
            rewardFlyGroup.transform.SetParent(resultGroupTrans);
            rewardFlyGroup.cachedGameObject.setActiveWhenChange(true);
            rewardFlyGroup.cachedRectTransform.movePos(rewardPos, rewardFlyTime, onComplete: runHighRollerMore);
            scaleTween = TweenManager.tweenToFloat(rewardFlyGroup.cachedRectTransform.localScale.x, 0.2f, rewardFlyTime, onUpdate: scalerCoinGroup);
            AudioManager.instance.playAudioOnce(AudioPathProvider.getAudioPath(BonusAudio.AwardFly));
        }

        void scalerCoinGroup(float scale)
        {
            rewardFlyGroup.transform.localScale = new Vector3(scale, scale, scale);
        }

        void runHighRollerMore()
        {
            TweenManager.tweenKill(scaleTween);
            rewardFlyGroup.cachedGameObject.setActiveWhenChange(false);
            updateRewardTxt(StayGameDataStore.bonusReward);
            if (DataStore.getInstance.playerInfo.hasHighRollerPermission)
            {
                var rewardOrder = showOrders.Dequeue();
                rewardOrder.setActiveWhenChange(true);
                moveHighMoreObj();
                return;
            }
            startShowRewards();
        }

        async void moveHighMoreObj()
        {
            dcMoreRect.gameObject.setActiveWhenChange(true);
            await Task.Delay(TimeSpan.FromSeconds(1.0f));
            BezierPresenter bezierPresenter = UiManager.bind<BezierPresenter>(dcMoreRect.gameObject);
            Vector2 middlePos = new Vector2(dcMoreRect.transform.position.x + 2, dcMoreRect.transform.position.y + 2);
            bezierPresenter.bezierPoints.Add(dcMoreRect.transform.position);
            bezierPresenter.bezierPoints.Add(middlePos);
            bezierPresenter.bezierPoints.Add(rewardObj.transform.position);
            bezierPresenter.moveBezierLine(0.5f, startShowRewards);
            TweenManager.tweenToFloat(dcMoreRect.localScale.x, 0.5f, durationTime: 0.5f, onUpdate: scale =>
              {
                  dcMoreRect.localScale = new Vector3(scale, scale, scale);
              });
        }

        async void startShowRewards()
        {
            dcMoreRect.gameObject.setActiveWhenChange(false);
            if (DataStore.getInstance.playerInfo.hasHighRollerPermission)
            {
                updateRewardTxt((ulong)(StayGameDataStore.bonusReward * 1.5f));
                moreAnim.SetTrigger("more");
                await Task.Delay(TimeSpan.FromSeconds(0.5f));
            }
            IDisposable showTimerDis = null;
            showTimerDis = Observable.Timer(TimeSpan.Zero, TimeSpan.FromSeconds(0.6f)).Subscribe(repeatCount =>
             {
                 showOrders.Dequeue().setActiveWhenChange(true);
                 if (repeatCount > 0)
                 {
                     AudioManager.instance.playAudioOnce(AudioPathProvider.getAudioPath(BonusAudio.Appear));
                 }
                 if (showOrders.Count <= 0)
                 {
                     collectBtn.interactable = true;
                     showTimerDis.Dispose();
                 }
             }).AddTo(uiGameObject);
        }

        void setData()
        {
            vipTxt.text = $"{StayGameDataStore.vipMakeup * 100}%";
            muiltiplierTxt.text = $"{StayGameDataStore.multiplierEnergyMakeup * 100}%";
            totalTxt.text = StayGameDataStore.bonusAmount.convertToCurrencyUnit(showLong: 6, havePoint: false);
            coinTxt.text = StayGameDataStore.bonusReward.ToString("N0");
            LayoutRebuilder.ForceRebuildLayoutImmediate(moneyGroup);
            var orders = showOrders.GetEnumerator();
            while (orders.MoveNext())
            {
                orders.Current.setActiveWhenChange(false);
            }
        }

        void updateRewardTxt(ulong bonusReward)
        {
            rewardTxt.text = bonusReward.convertToCurrencyUnit(showLong: 4, havePoint: false);
        }

        void onCollectClick()
        {
            collectBtn.interactable = false;
            AudioManager.instance.playAudioOnce(CommonAudioPathProvider.getAudioPath(BasicCommonSound.InfoBtn));
            var sourceValue = DataStore.getInstance.playerInfo.myWallet.deprecatedCoin;
            var targetValue = DataStore.getInstance.playerInfo.myWallet.coin;
            CoinFlyHelper.obverseSFly(collectBtn.GetComponent<RectTransform>(), sourceValue, targetValue, onComplete: closePresenter);
        }

        public override void closePresenter()
        {
            DataStore.getInstance.playerInfo.myWallet.refresh();
            base.closePresenter();
        }

        public override void animOut()
        {
            if (null != rewardFlyGroup)
            {
                ResourceManager.instance.releasePoolWithObj(rewardFlyGroup.cachedGameObject);
                GameObject.DestroyImmediate(rewardFlyGroup.cachedGameObject);
            }
            HighRoller.HighRollerRewardManager.openReward(StayGameDataStore.highRollerBoard);
            clear();
        }
    }
}
