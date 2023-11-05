using CommonService;
using System.Collections.Generic;

namespace CommonILRuntime.Outcome.Processor
{
    public class VipInfoProcessor : IOutcomeProcessor
    {
        bool commitSuccess = false;
        public void process(CommonRewardOutcome rewardOutcome)
        {
            Dictionary<string, object> outcome = rewardOutcome.vip;
            var vipInfo = new VipInfo()
            {
                revision = (long)outcome["revision"],
                level = (int)outcome["level"],
                points = (int)outcome["points"],
            };

            object levelUpPoints;
            if (outcome.TryGetValue("levelUpPoints", out levelUpPoints))
            {
                vipInfo.levelUpPoints = (int)levelUpPoints;
            }

            object isLevelUp;
            if (outcome.TryGetValue("isLevelUp", out isLevelUp))
            {
                //TODO: notify vip level up ?
            }

            commitSuccess = DataStore.getInstance.playerInfo.myVip.commit(vipInfo);
        }

        public void subject()
        {
            if (commitSuccess)
            {
                DataStore.getInstance.playerInfo.myVip.refresh();
            }
        }
    }
}
