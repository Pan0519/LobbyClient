using CommonILRuntime.BindingModule;
using CommonILRuntime.Outcome;
using LobbyLogic.NetWork.ResponseStruct;
using System.Collections.Generic;
using System.Threading.Tasks;
using EventActivity;
using UniRx;
using Services;

namespace Lobby.Jigsaw
{
    public static class JigsawReward
    {
        public static Subject<bool> isJigsawShowFinish = new Subject<bool>();

        public static async void checkCollectionRewards()
        {
            var rewards = await JigsawDataHelper.peekRewards();
            var performer = new JigsawCllectionRewardPresenter(rewards);
            performer.showNextReward();
        }

        public static async Task<Outcome> redeemReward(string id)
        {
            var rewards = await JigsawDataHelper.redeemReward(id);
            Outcome coinOutcome = null;
            for (int i = 0; i < rewards.Count; i++)
            {
                var reward = rewards[i];
                var outcome = Outcome.process(reward);
                if (!string.IsNullOrEmpty(reward.kind) && reward.kind.Equals(UtilServices.outcomeCoinKey))
                {
                    coinOutcome = outcome;    //飛幣延遲 apply
                }
                else
                {
                    outcome.apply();
                }
            }

            return coinOutcome;
        }
    }

    class JigsawCllectionRewardPresenter
    {
        int rewardIdx = -1;
        List<JigsawRewardKind> rewards; //單本獎勵、單季獎勵

        public JigsawCllectionRewardPresenter(List<JigsawRewardKind> rewards)   //可能同時包含多本完成+一季完成
        {
            rewardIdx = -1;
            this.rewards = rewards;
        }

        public void showNextReward()
        {
            rewardIdx++;
            if (rewards.Count > rewardIdx)  //avoid list out of bound
            {
                var rewardData = rewards[rewardIdx];
                Reward coinReward = null;
                for (int i = 0; i < rewardData.rewards.Length; i++)
                {
                    var reward = rewardData.rewards[i];
                    AwardKind awardKind = ActivityDataStore.getAwardKind(reward.kind);
                    if (AwardKind.Coin == awardKind)
                    {
                        coinReward = reward;
                        break;
                    }
                }

                if (null != coinReward)
                {
                    var presenter = UiManager.getPresenter<JjigsawCompleteBoard>();
                    presenter.setData(coinReward.getAmount(), rewardData.id, showNextReward);
                }
                else
                {
                    showNextReward();
                }
                return;
            }
            JigsawReward.isJigsawShowFinish.OnNext(true);
        }
    }
}
