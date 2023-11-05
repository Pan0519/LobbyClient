using CommonILRuntime.Outcome.Processor;
using System.Collections.Generic;
using UnityEngine;
using Services;

namespace CommonILRuntime.Outcome
{
    public class Outcome
    {
        public static Outcome process(params CommonReward[] rewardOutcome)
        {
            Outcome outcome = new Outcome();
            outcome.processAll(rewardOutcome);
            return outcome;
        }

        Dictionary<string, IOutcomeProcessor> processors;

        public Outcome()
        {
            processors = new Dictionary<string, IOutcomeProcessor>();
            processors.Add(UtilServices.outcomeCoinBankKey, new WalletProcessor());
            processors.Add(UtilServices.outcomeCoinKey, new WalletProcessor());
            processors.Add(UtilServices.outcomeVIPPointKey, new VipInfoProcessor());
            processors.Add(UtilServices.outcomeExpBoost, new ExpBoostProcessor());
            processors.Add(UtilServices.outcomeHighPassPoint, new PassPointProcessor());
        }

        /// <summary>
        /// 結果套用並更新到玩家數值(commitedValue)
        /// </summary>
        public void apply()
        {
            foreach (var processor in processors.Values)
            {
                processor.subject();
            }
        }

        void processAll(params CommonReward[] commonRewards)
        {
            for (int i = 0; i < commonRewards.Length; ++i)
            {
                IOutcomeProcessor processor = null;
                CommonReward reward = commonRewards[i];
                if (processors.TryGetValue(reward.kind, out processor))
                {
                    processor.process(reward.outcome);
                }
            }
        }
    }
}
