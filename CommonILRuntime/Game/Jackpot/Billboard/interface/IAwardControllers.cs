using Binding;
using System;

namespace Game.Jackpot.Billboard
{
    public interface IAwardControllers
    {
        void bindJPObject(Func<string, BindingNode> bindingFunc);
        void changeTotalBet(ulong totalBet);
        void initAward(IConfig config);
        void restartRunScore(int jpType, ulong currentBet);
        void setServerAward(int jpType, ulong value, ulong currentBet);
    }
}
