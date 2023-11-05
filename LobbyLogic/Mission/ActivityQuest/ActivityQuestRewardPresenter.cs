using Debug = UnityLogUtility.Debug;
using CommonILRuntime.Module;
using UnityEngine.UI;
using UnityEngine;
using Lobby.UI;
using CommonILRuntime.BindingModule;
using Services;
using Lobby.Jigsaw;
using UniRx;
using System;
using System.Threading.Tasks;
using CommonILRuntime.Services;
using EventActivity;
using Lobby.Common;
using CommonILRuntime.Outcome;
using CommonService;
using LobbyLogic.Audio;

namespace Mission
{
    public class ActivityQuestRewardPresenter : ContainerPresenter
    {
        public override string objPath => "prefab/quest_mission/quest_reward_board";
        public override UiLayer uiLayer { get { return UiLayer.System; } }

        #region UI Obj
        private Animator boardAni;
        private RectTransform rewardLayout;
        private Button btnGet;
        #endregion

        #region Prefab Path
        private readonly string REWARD_ITEM_PACK = "prefab/reward_item/reward_item_pack";
        private readonly string REWARD_ITEM = "prefab/reward_item/reward_item";
        #endregion

        #region Other
        private IDisposable closeAniCallBack;
        #endregion

        Outcome outcome;
        CommonReward[] rewards;
        ulong coinReward;
        public override void initUIs()
        {
            boardAni = getAnimatorData("board_ani");
            rewardLayout = getRectData("reward_layout_rect");
            btnGet = getBtnData("get_btn");
        }

        public override void init()
        {
            btnGet.onClick.AddListener(onBtnGet);
        }

        public void getRewardInfo(CommonReward[] rewards)
        {
            this.rewards = rewards;
            BindingLoadingPage.instance.open();
            addRewardObj(rewards);
            BindingLoadingPage.instance.close();
        }

        private void onBtnGet()
        {
            btnGet.interactable = false;
            AudioManager.instance.playAudioOnce(CommonAudioPathProvider.getAudioPath(BasicCommonSound.InfoBtn));
            onCoinFly();
        }

        private void onCoinFly()
        {
            ulong startValue = DataStore.getInstance.playerInfo.myWallet.deprecatedCoin;
            ulong endValue = DataStore.getInstance.playerInfo.myWallet.coin;

            if (startValue == endValue)
            {
                startValue -= coinReward;
            }

            CoinFlyHelper.frontSFly(btnGet.transform as RectTransform, startValue, endValue, onComplete: () =>
            {
                coinFlyComplete();
            });
        }

        private async void coinFlyComplete()
        {
            await Task.Delay(TimeSpan.FromSeconds(1.0f));
            playOutAnim();
        }

        private void playOutAnim()
        {
            boardAni.SetTrigger("out");
            outcome.apply();
            closeAniCallBack = Observable.TimerFrame(33).Subscribe(_ =>
            {
                if (havePuzzle)
                {
                    OpenPackWildProcess.openPackWild(rewards, closePage);
                    return;
                }

                closePage();
            }).AddTo(uiGameObject);
        }

        private void closePage()
        {
            closeAniCallBack.Dispose();
            clear();
            if (DataStore.getInstance.dataInfo.getChooseBetClassType() == BetClass.Adventure)
            {
                UtilServices.backToLobby();
            }
        }
        bool havePuzzle;
        private void addRewardObj(CommonReward[] rewards)
        {
            havePuzzle = false;
            outcome = Outcome.process(rewards);
            coinReward = 0;
            for (var i = 0; i < rewards.Length; i++)
            {
                PoolObject rewardObj;
                var reward = rewards[i];
                var rewardKind = ActivityDataStore.getAwardKind(reward.kind);
                switch (rewardKind)
                {
                    case AwardKind.PuzzlePack:
                    case AwardKind.PuzzleVoucher:
                        rewardObj = ResourceManager.instance.getObjectFromPool(REWARD_ITEM_PACK, rewardLayout);
                        var packPresenter = UiManager.bindNode<RewardPackItemNode>(rewardObj.gameObject);
                        packPresenter.setPuzzlePack(reward.type);
                        havePuzzle = true;
                        break;

                    case AwardKind.Coin:
                    case AwardKind.Ticket:
                        rewardObj = ResourceManager.instance.getObjectFromPool(REWARD_ITEM, rewardLayout);
                        var rewardPresenter = UiManager.bindNode<RewardItemNode>(rewardObj.gameObject);
                        rewardPresenter.setRewardData(reward);

                        if (rewardKind == AwardKind.Coin)
                        {
                            coinReward += reward.getAmount();
                        }
                        break;
                    default:
                        Debug.LogError($"get error awardKind -{reward}");
                        break;
                }
            }
        }
    }
}