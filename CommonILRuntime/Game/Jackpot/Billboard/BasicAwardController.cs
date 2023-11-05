using CommonILRuntime.Module;
using UnityEngine.UI;

namespace Game.Jackpot.Billboard
{
    public class BasicAwardController : NodePresenter
    {
        protected Text awardText = null;
        protected float basicRate = 0f;

        public override void initUIs()
        {
            awardText = getTextData("awardText");
        }

        public void initAward(ulong basicRate)
        {
            this.basicRate = basicRate;
        }

        public virtual void changeTotalBet(ulong totalBet)
        {
            var award = totalBet * basicRate;
            awardText.text = award.ToString("N0");
        }
    }
}
