using UniRx;
using System.Threading.Tasks;
using System.Collections.Generic;
using CommonService;
using System;
using Service;
using CommonILRuntime.Outcome;
using Debug = UnityLogUtility.Debug;

namespace NewPlayerGuide
{
    public static class GuideDataManager
    {
        public static Subject<bool> setBGEnableSub = new Subject<bool>();
        public static Subject<bool> fadeBGOutSub = new Subject<bool>();

        public static async Task<ulong> getRewards(string packID)
        {
            var rewardPack = await AppManager.lobbyServer.getRewardPacks(packID);
            //Outcome outcome = Outcome.process(rewardPack.rewards);
            ulong rewardCoin = 0;
            for (int i = 0; i < rewardPack.rewards.Length; ++i)
            {
                var reward = rewardPack.rewards[i];
                if (reward.kind.EndsWith("coin"))
                {
                    rewardCoin = reward.getAmount();
                    break;
                }
            }

            return rewardCoin;
        }

        public static void setBGEnable(bool enable)
        {
            setBGEnableSub.OnNext(enable);
        }

        public static void fadeOutBG()
        {
            fadeBGOutSub.OnNext(true);
        }
    }
}
