using CommonILRuntime.BindingModule;
using Event.Common;
using EventActivity;

namespace MagicForest
{
    class ForestBoosterNode : BoosterNodePresenter
    {
        public override void openBoosterShop()
        {
            UiManager.getPresenter<ForestShopPresenter>().openShop(isShowSpinObj: false);
        }
    }

    class ForestPrizeBoosterNode : ForestBoosterNode
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
