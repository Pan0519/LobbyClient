using CommonILRuntime.Services;

namespace Game.Jackpot
{
    public class AwardLooper : LongValueTweener
    {
        public AwardLooper(ILongValueTweenerHandler receiver, ulong frequency = 1000) : base(receiver, frequency)
        {
            onComplete = startDash;
        }
    }
}
