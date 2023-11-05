using CommonService;
using System.Collections.Generic;

namespace CommonILRuntime.Outcome.Processor
{
    class PassPointProcessor : IOutcomeProcessor
    {
        public void process(CommonRewardOutcome rewardOutcome)
        {
            Dictionary<string, int> outcome = rewardOutcome.highRoller;
            DataStore.getInstance.playerInfo.addPassPoint(outcome["passPoint"]);
        }

        public void subject()
        {

        }
    }
}
