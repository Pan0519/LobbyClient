using System.Collections.Generic;

namespace CommonILRuntime.Outcome
{
    public interface IOutcomeProcessor
    {
        void process(CommonRewardOutcome outcome);
        void subject();
    }
}
