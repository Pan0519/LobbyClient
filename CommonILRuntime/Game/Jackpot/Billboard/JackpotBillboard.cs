using Binding;
using CommonILRuntime.BindingModule;
using CommonILRuntime.Module;

namespace Game.Jackpot.Billboard
{
    public abstract class JackpotBillboard : NodePresenter
    {
        protected IConfig jpConfig = new JPConfig();
        protected IAwardControllers awardControllers = null;

        public override void initUIs()
        {
            awardControllers = getAwardControllers();
            awardControllers.bindJPObject(getNodeData);
        }

        public override void init()
        {
            initConfig();
            awardControllers.initAward(jpConfig);
        }

        public virtual void setTotalBet(ulong totalBet)
        {
            awardControllers.changeTotalBet(totalBet);
        }

        protected abstract void initConfig();
        protected virtual IAwardControllers getAwardControllers()
        {
            return new DefaultAwardControllers();
        }

        public void setServerAward(int jpType, ulong value, ulong currentBet)
        {
            awardControllers.setServerAward(jpType, value, currentBet);
        }

        public void resetJpScore(int jpType, ulong currentBet)
        {
            awardControllers.restartRunScore(jpType, currentBet);
        }
    }
}
