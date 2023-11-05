using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace CommonILRuntime.Outcome
{
    public class Reward
    {
        public string kind;
        public string type;
        public decimal amount;
        public ulong getAmount()
        {
            return (ulong)amount;
        }
    }

    public class CommonReward : Reward
    {
        public CommonRewardOutcome outcome;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct CommonRewardOutcome
    {
        [FieldOffset(0)]
        public Dictionary<string, decimal> wallet;

        [FieldOffset(0)]
        public Dictionary<string, Dictionary<string, object>[]> album;

        [FieldOffset(0)]
        public Dictionary<string, string> user;

        [FieldOffset(0)]
        public Dictionary<string, object> vip;

        [FieldOffset(0)]
        public Dictionary<string, int> highRoller;
        [FieldOffset(0)]
        public Dictionary<string, object> bag;
    }
}
