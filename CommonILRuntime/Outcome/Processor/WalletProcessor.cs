using CommonService;
using System.Collections.Generic;
using UnityEngine;

namespace CommonILRuntime.Outcome.Processor
{
    public class WalletProcessor : IOutcomeProcessor
    {
        bool commitSuccess = false;
        public void process(CommonRewardOutcome rewardOutcome)
        {
            Dictionary<string, decimal> outcome = rewardOutcome.wallet;
            var wallet = new Wallet()
            {
                revision = (long)outcome["revision"],
                coin = outcome["coin"]
            };
            commitSuccess = DataStore.getInstance.playerInfo.myWallet.commit(wallet);
        }

        public void subject()
        {
            if (commitSuccess)
            {
                DataStore.getInstance.playerInfo.myWallet.refresh();
            }
        }
    }
}
