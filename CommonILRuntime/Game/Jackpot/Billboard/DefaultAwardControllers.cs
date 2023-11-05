using Binding;
using CommonILRuntime.BindingModule;
using CommonILRuntime.Module;
using System;
using static Game.Common.GameConfig;

namespace Game.Jackpot.Billboard
{
    public class DefaultAwardControllers : IAwardControllers
    {
        private JackpotAwardController miniJP = null;
        private JackpotAwardController minorJP = null;
        private JackpotAwardController majorJP = null;
        private JackpotAwardController grandJP = null;

        public void bindJPObject(Func<string, BindingNode> bindingFunc)
        {
            miniJP = getController<JackpotAwardController>(bindingFunc,"mini");
            minorJP = getController<JackpotAwardController>(bindingFunc, "minor");
            majorJP = getController<JackpotAwardController>(bindingFunc, "major");
            grandJP = getController<JackpotAwardController>(bindingFunc, "grand");
        }

        private T getController<T>(Func<string, BindingNode> bindingFunc, string bindingId) where T : NodePresenter, new()
        {
            var nodeData = bindingFunc(bindingId);
            return UiManager.bindNode<T>(nodeData.cachedGameObject);
        }

        public void changeTotalBet(ulong totalBet)
        {
            miniJP.changeTotalBet(totalBet);
            minorJP.changeTotalBet(totalBet);
            majorJP.changeTotalBet(totalBet);
            grandJP.changeTotalBet(totalBet);
        }

        public void initAward(IConfig config)
        {
            miniJP.initAward(config.MINI_BASIC_RATE, config.MAX_LIMIT_RATE, config.MIN_LIMIT_RATE, config.SERVER_SCALE);
            minorJP.initAward(config.MINOR_BASIC_RATE, config.MAX_LIMIT_RATE, config.MIN_LIMIT_RATE, config.SERVER_SCALE);
            majorJP.initAward(config.MAJOR_BASIC_RATE, config.MAX_LIMIT_RATE, config.MIN_LIMIT_RATE, config.SERVER_SCALE);
            grandJP.initAward(config.GRAND_BASIC_RATE, config.MAX_LIMIT_RATE, config.MIN_LIMIT_RATE, config.SERVER_SCALE);
        }

        public void restartRunScore(int jpType, ulong currentBet)
        {
            var controller = getController(jpType);
            controller.changeTotalBet(currentBet);
        }

        public void setServerAward(int jpType, ulong value, ulong currentBet)
        {
            var controller = getController(jpType);
            controller.setServerAward(value, currentBet);
        }

        private JackpotAwardController getController(int jpType)
        {
            JackpotAwardController result = null;
            var type = (JackPotLevels)jpType;

            switch (type)
            {
                case JackPotLevels.Grand:
                    result = grandJP;
                    break;
                case JackPotLevels.Major:
                    result = majorJP;
                    break;
                case JackPotLevels.Minor:
                    result = minorJP;
                    break;
                case JackPotLevels.Mini:
                    result = miniJP;
                    break;
            }

            return result;
        }
    }
}
