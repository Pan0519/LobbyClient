using CommonILRuntime.Outcome;
using CommonService;
using LobbyLogic.NetWork.ResponseStruct;
using static Mission.MissionHelper;

namespace Mission
{
    public class MissionPacketResolver
    {
        const string worthCoinType = "worth";
        const string coinKind = "coin";

        public RewardFormat packageResponse(RewardPacksResponse response)
        {
            RewardFormat result = new RewardFormat();
            CommonReward[] rewards = convertCommonRewardAmount(response.rewards);

            result = packagePlayerCoinInfo(result, rewards);
            result.commonRewards.AddRange(rewards);
            result.outcome = Outcome.process(rewards);

            return result;
        }

        RewardFormat packagePlayerCoinInfo(RewardFormat result, CommonReward[] rewards)
        {
            CommonReward commonReward = getCoinReward(rewards);
            result.finalPlayerCoin = getFinalPlayerCoin(commonReward);

            return result;
        }

        CommonReward getCoinReward(CommonReward[] rewards)
        {
            int count = rewards.Length;
            CommonReward result = null;

            for (int i = 0; i < count; ++i)
            {
                result = rewards[i];
                if (result.kind.Contains(coinKind))
                {
                    break;
                }
            }

            return result;
        }

        ulong getFinalPlayerCoin(CommonReward commonReward)
        {
            var outcom = commonReward.outcome;
            var wallet = outcom.wallet;

            return (ulong)wallet["coin"];
        }

        CommonReward[] convertCommonRewardAmount(CommonReward[] commonRewards)
        {
            int count = commonRewards.Length;
            CommonReward commonReward = null;

            for (int i = 0; i < count; ++i)
            {
                commonReward = commonRewards[i];
                if (checkIsNeedConvertCoin(commonReward))
                {
                    commonReward.amount = commonReward.amount * DataStore.getInstance.playerInfo.coinExchangeRate;
                }
            }

            return commonRewards;
        }

        bool checkIsNeedConvertCoin(CommonReward commonReward)
        {
            return commonReward.kind.Contains(coinKind) &&
                !string.IsNullOrEmpty(commonReward.type) &&
                commonReward.type.Contains(worthCoinType);
        }
    }
}
