using CommonService;
using System.Collections.Generic;

namespace CommonILRuntime.Outcome.Processor
{
    class ExpBoostProcessor : IOutcomeProcessor
    {
        public void process(CommonRewardOutcome rewardOutcome)
        {
            Dictionary<string, string> outcome = rewardOutcome.user;
            DataStore.getInstance.playerInfo.setExpBoostEndTime(outcome["expBoostEndedAt"]);
        }

        public void subject()
        {

        }
    }
}
