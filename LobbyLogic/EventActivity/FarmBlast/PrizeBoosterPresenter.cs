using EventActivity;
using Event.Common;

namespace FarmBlast
{
    class PrizeBoosterPresenter : FarmBlastBoosterNode
    {
        public override void timeExpire()
        {
            ActivityDataStore.isPrizeBooster = false;
        }

        public override void startCountdownTime()
        {
            ActivityDataStore.isPrizeBooster = true;
        }
    }
}
