using CommonILRuntime.Module;
using CommonILRuntime.BindingModule;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using EventActivity;
using UniRx;
using UniRx.Triggers;
using System;
using Services;
using CommonILRuntime.Services;
using CommonService;
using System.Threading.Tasks;
using LobbyLogic.Audio;
using Lobby.Audio;
using Event.Common;

namespace MagicForest
{
    public class NormalPrizePresenter : PrizeAward
    {
        public override string objPath => $"{ForestDataServices.prefabPath}/mf_prize";
        public override UiLayer uiLayer { get => UiLayer.System; }

        Animator showAnim;
        Button collectBtn;
        Text coinRewardText;

        List<IDisposable> animTriggerDis = new List<IDisposable>();
        ActivityAwardData awardData = null;
        Action finishCB;

        Dictionary<AwardKind, string> animInTriggerNames = new Dictionary<AwardKind, string>()
        {
            { AwardKind.Ticket,"magnifier" },
            { AwardKind.Coin,"money"},
        };

        RewardTicketNode flyTicket;
        TicketWithAnim ticketFlyTarget;

        RewardTicketNode ticketNode;
        public override void initContainerPresenter()
        {
            resOrder = new string[] { AssetBundleData.getBundleName(BundleType.MagicForest) };
            base.initContainerPresenter();
        }
        public override void init()
        {
            collectBtn.onClick.AddListener(closeClick);
            ticketNode = UiManager.bindNode<RewardTicketNode>(getNodeData("reward_ticket_node").cachedGameObject);
            var animTriggers = showAnim.GetBehaviours<ObservableStateMachineTrigger>();
            for (int i = 0; i < animTriggers.Length; ++i)
            {
                animTriggerDis.Add(animTriggers[i].OnStateEnterAsObservable().ObserveOnMainThread().Subscribe(onAniOut).AddTo(uiGameObject));
            }
        }

        public override void initUIs()
        {
            base.initUIs();
            showAnim = getAnimatorData("show_anim");
            collectBtn = getBtnData("collect_btn");
            coinRewardText = getTextData("coin_reward_text");
        }

        public override Button getCollectBtn()
        {
            return collectBtn;
        }

        public override Text getCoinTxt()
        {
            return coinRewardText;
        }

        string animName;

        public void openPrize(ActivityAwardData awardData, Action finshAction)
        {
            if (!animInTriggerNames.TryGetValue(awardData.kind, out animName))
            {
                return;
            }
            AudioManager.instance.playAudioOnce(AudioPathProvider.getAudioPath(ActivityBlastAudio.SmallWin));
            collectBtn.interactable = true;
            finishCB = finshAction;
            this.awardData = awardData;

            showAnim.SetTrigger($"{animName}_in");
            if (AwardKind.Coin == awardData.kind)
            {
                setAwardValue(awardData.amount);
                coinRewardText.text = prizeBoosterOriginalReward.ToString("N0");
                return;
            }
            showTicketObjs();
        }

        void showTicketObjs()
        {
            ticketNode.updateTicketNum((long)awardData.amount);
            flyTicket = UiManager.bindNode<RewardTicketNode>(GameObject.Instantiate(ticketNode.uiGameObject, ticketNode.uiTransform.parent));
            flyTicket.close();
            flyTicket.updateTicketNum((long)awardData.amount);
        }

        public void setTicketFlyTarget(GameObject target)
        {
            if (AwardKind.Ticket != awardData.kind)
            {
                return;
            }
            ticketFlyTarget = UiManager.bindNode<TicketWithAnim>(GameObject.Instantiate(target, ticketNode.uiTransform.parent));
            ticketFlyTarget.close();
        }

        void closeClick()
        {
            collectBtn.interactable = false;
            if (null == awardData)
            {
                showAnim.SetTrigger("out");
                return;
            }
            if (AwardKind.Coin == awardData.kind)
            {
                ulong sourceVal = DataStore.getInstance.playerInfo.playerMoney;
                CoinFlyHelper.frontSFly(collectBtn.GetComponent<RectTransform>(), sourceVal, sourceVal + awardData.amount, onComplete: playOut);
                return;
            }
            flyTicket.uiRectTransform.SetAsLastSibling();
            ticketFlyTarget.open();
            flyTicket.open();
            float flyTime = 0.6f;
            float delayScaleTime = 0.2f;
            AudioManager.instance.playAudioOnce(AudioPathProvider.getAudioPath(ActivityBlastAudio.IconFly));
            string posTwID = flyTicket.uiRectTransform.anchPosMove(ticketFlyTarget.uiRectTransform.anchoredPosition, durationTime: flyTime, easeType: DG.Tweening.Ease.InBack);
            string scaleTwID = TweenManager.tweenToFloat(flyTicket.uiTransform.localScale.x, ticketFlyTarget.uiTransform.localScale.x, delayTime: delayScaleTime, durationTime: flyTime - delayScaleTime, onUpdate: scale =>
                 {
                     var scalePos = flyTicket.uiTransform.localScale;
                     scalePos.Set(scale, scale, scale);
                     flyTicket.uiTransform.localScale = scalePos;
                 }, onComplete: playOut);

            TweenManager.tweenPlayByID(posTwID, scaleTwID);
        }

        async void playOut()
        {
            if (null != ticketFlyTarget)
            {
                flyTicket.close();
                ticketFlyTarget.playGetAnim();
                AudioManager.instance.playAudioOnce(AudioPathProvider.getAudioPath(ActivityBlastAudio.IconFlyIn));
            }
            if (AwardKind.Ticket == awardData.kind)
            {
                ForestDataServices.addTicketCount((int)awardData.amount);
                await Task.Delay(TimeSpan.FromSeconds(0.5f));
            }

            showAnim.SetTrigger("out");
        }

        private void onAniOut(ObservableStateMachineTrigger.OnStateInfo obj)
        {
            animTriggerDis.Add(Observable.Timer(TimeSpan.FromSeconds(obj.StateInfo.length), Scheduler.MainThreadIgnoreTimeScale).Subscribe(_ =>
           {
               bool isNameOut = obj.StateInfo.IsName($"mf_{animName}_out");
               if (isNameOut)
               {
                   UtilServices.disposeSubscribes(animTriggerDis.ToArray());
                   if (null != finishCB)
                   {
                       finishCB();
                   }
                   clear();
                   return;
               }
               checkIsShowPrizeBooster();
           }));
        }
    }
}
