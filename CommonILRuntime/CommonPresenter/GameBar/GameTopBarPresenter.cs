using UnityEngine.UI;
using UnityEngine;
using UniRx;
using UniRx.Triggers;
using System;
using DG.Tweening;
using CommonService;
using CommonILRuntime.BindingModule;
using Services;
using CommonILRuntime.Services;
using System.Threading.Tasks;
using Debug = UnityLogUtility.Debug;
using CommonILRuntime.Outcome;
using Binding;
using Game.Common;
using CommonILRuntime.Extension;

namespace CommonPresenter
{
    public class GameTopBarPresenter : TopBarBasePresenter
    {
        public override string objPath => UtilServices.getOrientationObjPath("prefab/game/game_top_bar");

        #region UIs
        Button homeBtn;
        public RectTransform lvupObjRect { get; private set; }
        Text lvupTxt;
        Text lvupRewardTxt;
        Text lvupVIPPointTxt;

        BindingNode miniGame;
        BindingNode miniPrice;

        Button gameRuleBtn;

        RectTransform expBarEffectRect;
        Animator expBarEffectAnim;

        Image homeImg;
        Image homeOuterRingImg;
        //RectTransform lvupCoinTargetRect;

        #endregion

        public Action backToLobby;
        public Action createRule;

        float lvupOpenTime { get { return 0.3f; } }
        Ease lvupOPenType { get { return Ease.OutBack; } }
        float lvupCloseTime { get { return 0.25f; } }
        Ease lvupCloseType { get { return Ease.Linear; } }

        float lvupObjClosePosY;
        RewardPacks rewardPacks;

        TopMiniGamePresenter topMini;
        TopMiniPricePresenter topMiniPrice;

        RuleBasePresenter gameRule;
        GameGoldenNode goldenNode;

        IDisposable barEffectAnimTimerDis = null;
        IDisposable barEffectAnimTriggerDis = null;
        ObservableStateMachineTrigger animtrigger = null;

        public override void initUIs()
        {
            base.initUIs();

            homeBtn = getBtnData("btn_home");
            homeImg = homeBtn.GetComponent<Image>();
            homeOuterRingImg = getImageData("home_outerring_img");

            lvupObjRect = getBindingData<RectTransform>("lvup_obj_rect");
            lvupTxt = getTextData("lvup_txt");
            lvupRewardTxt = getTextData("lvup_reward_num_txt");
            lvupVIPPointTxt = getTextData("lvup_vip_point_txt");

            miniGame = getNodeData("mini_game");
            miniPrice = getNodeData("mini_price");
            gameRuleBtn = getBtnData("pay_table_btn");

            expBarEffectRect = getBindingData<RectTransform>("bar_effect_trans");
            expBarEffectAnim = getAnimatorData("bar_effect_anim");
            //lvupCoinTargetRect = getRectData("lvup_icon_target");
        }

        public override void init()
        {
            base.init();
            bool isHighRoller = BetClass.HighRoller == DataStore.getInstance.dataInfo.getChooseBetClassType();
            setBGImgSprite(isHighRoller);
            goldenNode = UiManager.bindNode<GameGoldenNode>(goldenPresenter.cachedGameObject);
            lvupObjClosePosY = lvupObjRect.anchoredPosition.y;
            lvupObjRect.gameObject.setActiveWhenChange(true);
            DataStore.getInstance.dataInfo.lvupRewardSubscribe.Subscribe(setLvupReward);
            DataStore.getInstance.gameToLobbyService.checkGoldenMax(goldenNode);
            checkGetBonusTime(DataStore.getInstance.dataInfo.bonusTimeStr);
            homeBtn.onClick.AddListener(homeBtnClick);
            setLvUpExp(playerInfo.LvUpExp);
            setPlayerExpValue(playerInfo.playerExp);
            initMiniGame();
            gameRuleBtn.onClick.AddListener(gameRuleBtnClick);
            string betClassSpriteName = getBetClassSpriteName(isHighRoller);
            homeImg.sprite = getTopBarSprite($"btn_home_{betClassSpriteName}_1");
            homeOuterRingImg.sprite = getTopBarSprite($"bg_bar_button_{betClassSpriteName}");
            if (DataStore.getInstance.guideServices.getSaveGuideStatus() != GuideStatus.Completed)
            {
                closeForDogGuide();
            }
        }

        void closeForDogGuide()
        {
            topMini.close();
            buyIconObj.setActiveWhenChange(false);
            shortBuyBtns.gameObject.setActiveWhenChange(false);
            buyLongBtnRoot.gameObject.setActiveWhenChange(false);
            goldenPresenter.cachedGameObject.setActiveWhenChange(false);
        }

        public override void openOptionList()
        {
            base.openOptionList();
            gameOptionClick();
        }
        void initMiniGame()
        {
            DataStore.getInstance.miniGameData.gameBonusRedeemAmount.Subscribe(showMiniPrice).AddTo(uiGameObject);
            topMini = UiManager.bindNode<TopMiniGamePresenter>(miniGame.cachedGameObject);
            topMiniPrice = bindingMiniPrice(miniPrice.cachedGameObject);
        }
        public virtual TopMiniPricePresenter bindingMiniPrice(GameObject miniPriceObj)
        {
            return UiManager.bindNode<TopMiniPricePresenter>(miniPriceObj);
        }

        void showMiniPrice(ulong bonusCoin)
        {
            topMiniPrice.showPrice(bonusCoin, topMini.priceShowFinish);
        }

        public override void openShopPage()
        {
            DataStore.getInstance.eventInGameToLobbyService.OpenFuncInLobby(FunctionNo.Shop);
        }

        public override void openLimitShop()
        {
            base.openLimitShop();
            DataStore.getInstance.eventInGameToLobbyService.OpenFuncInLobby(FunctionNo.LimitShop);
        }
        public override void openSettingPage()
        {
            DataStore.getInstance.eventInGameToLobbyService.OpenFuncInLobby(FunctionNo.SettingPage);
            base.openSettingPage();
        }

        public void openGoldenParticleObj()
        {
            goldenNode.openGoldenParticleObj();
        }

        public override void countdownBonusTime(TimeSpan bonusTimeSpan)
        {
            if (bonusTimeSpan <= TimeSpan.Zero)
            {
                DataStore.getInstance.gameToLobbyService.bonusTimeSubscribe.OnNext(true);
            }
        }
        bool isOpening;
        void moveLvupObj(float endY, float durationTime, Ease moveType, Action moveComplete = null)
        {
            lvupObjRect.anchPosMoveY(endY, durationTime, onComplete: moveComplete, easeType: moveType);
        }

        public void lvupRewardCoinFly()
        {
            var source = DataStore.getInstance.playerInfo.myWallet.deprecatedCoin;
            var target = DataStore.getInstance.playerInfo.playerMoney;
            CoinFlyHelper.curveFly(lvupObjRect, source, target, duration: 0.5f, onComplete: () =>
              {
                  DataStore.getInstance.playerInfo.myWallet.unsafeAdd(rewardCoin);
                  //if (null != coinOutcome)
                  //{
                  //    coinOutcome.apply();
                  //}
                  lvupRunFinish();
              }, isPlaySound: false); ;
        }

        async void lvupRunFinish()
        {
            DataStore.getInstance.guideServices.isLvUP(true);
            lvupExpRunFinsih();
            await Task.Delay(TimeSpan.FromSeconds(2.0f));
            closeLvupObj();
            await Task.Delay(TimeSpan.FromSeconds(1.0f));
            isOpening = false;
        }

        public virtual void closeLvupObj()
        {
            moveLvupObj(lvupObjClosePosY, lvupCloseTime, lvupCloseType);
        }

        public void setLvupReward(RewardPacks rewardPacks)
        {
            this.rewardPacks = rewardPacks;
        }

        public virtual void openLvupObj()
        {
            audioManagerPlay(MainGameCommonSound.LvUpSmall);
            moveLvupObj(235f, lvupOpenTime, lvupOPenType, lvupRewardCoinFly);
        }
        ulong rewardCoin;
        //Outcome coinOutcome;
        public override void runLvupObj()
        {
            if (null == rewardPacks)
            {
                return;
            }

            if ((DataStore.getInstance.playerInfo.level % 10) != 0)
            {
                lvupTxt.text = DataStore.getInstance.playerInfo.level.ToString();

                Pack reward;

                if (rewardPacks.rewards.TryGetValue(PurchaseItemType.Coin, out reward))
                {
                    lvupRewardTxt.text = reward.outcome.getAmount().convertToCurrencyUnit(5, havePoint: false);
                    rewardCoin = reward.outcome.getAmount();
                    //coinOutcome = Outcome.process(reward.outcome);
                }

                if (rewardPacks.rewards.TryGetValue(PurchaseItemType.Vip, out reward))
                {
                    lvupVIPPointTxt.text = reward.outcome.amount.ToString();
                    Outcome.process(reward.outcome).apply();
                }
                if (!isOpening)
                {
                    isOpening = true;
                    openLvupObj();
                }
                return;
            }
            setPlayerExpValue(DataStore.getInstance.playerInfo.playerExp);
            UiManager.getPresenter<LvUpRewardPresenter>().setRewardPack(rewardPacks.rewards);
        }
        public virtual float barEffectPosX { get; set; } = 225;
        public override void barEffectPlayOut()
        {
            animtrigger = expBarEffectAnim.GetBehaviour<ObservableStateMachineTrigger>();
            barEffectAnimTriggerDis = animtrigger.OnStateEnterAsObservable().ObserveOnMainThread().Subscribe(barEffectAnimTrigger).AddTo(uiGameObject);
            expBarEffectAnim.SetTrigger("out");
        }

        void barEffectAnimTrigger(ObservableStateMachineTrigger.OnStateInfo obj)
        {
            barEffectAnimTriggerDis.Dispose();
            barEffectAnimTimerDis = Observable.Timer(TimeSpan.FromSeconds(obj.StateInfo.length)).Subscribe(_ =>
            {
                resetBarEffect();
            }).AddTo(uiGameObject);
        }

        public override void setBarEffectPos(float fillAmount)
        {
            float endX = (fillAmount - 1) * barEffectPosX;
            var changePos = expBarEffectRect.anchoredPosition;
            changePos.Set((float)Math.Round(endX, 2), 0);
            expBarEffectRect.anchoredPosition = changePos;
        }

        public override void resetBarEffect()
        {
            expBarEffectRect.gameObject.setActiveWhenChange(false);
            UtilServices.disposeSubscribes(barEffectAnimTimerDis);
        }

        public override void startRunGameBarEffect()
        {
            expBarEffectRect.gameObject.setActiveWhenChange(true);
        }

        void homeBtnClick()
        {
            if (null != backToLobby)
            {
                backToLobby();
            }
            DataStore.getInstance.eventInGameToLobbyService.OpenFuncInLobby(FunctionNo.OpenCommonLoadingUi);
            UtilServices.backToLobby();
        }

        void gameRuleBtnClick()
        {
            closeOptionListObj();
            if (null != gameRule)
            {
                gameRule.open();
                return;
            }

            if (null == createRule)
            {
                setGameRule<RuleBasePresenter>();
            }
            createRule();
            gameRule.open();
        }

        public void setGameRule<T>() where T : RuleBasePresenter, new()
        {
            createRule = delegate { gameRule = UiManager.getPresenter<T>(); };
        }

        public override void destory()
        {
            UtilServices.disposeSubscribes(barEffectAnimTimerDis);
            base.destory();
        }
    }

    public class GameGoldenNode : GoldenTopBarNode
    {
        public override void openGoldenEgg()
        {
            base.openGoldenEgg();
            if (DataStore.getInstance.guideServices.nowStatus != GuideStatus.Completed)
            {
                return;
            }
            DataStore.getInstance.eventInGameToLobbyService.OpenFuncInLobby(FunctionNo.GoldenEgg);
        }

        public void openGoldenParticleObj()
        {
            statusAnimPlay("tap");
        }
    }
}
