using CommonILRuntime.BindingModule;
using Event.Common;
using EventActivity;

namespace FrenzyJourney
{
    class JourneyBoosterNodePresenter : BoosterNodePresenter
    {
        public override void openBoosterShop()
        {
            UiManager.getPresenter<JourneyShopPresenter>().openShop(isShowSpinObj: false);
        }

        public JourneyBoosterNodePresenter setJourneyIconImg(BoosterType boosterType)
        {
            return setIconImg(boosterType) as JourneyBoosterNodePresenter;
        }
    }

    class CoinBoosterNodePresenter : JourneyBoosterNodePresenter
    {
        public override void timeExpire()
        {
            FrenzyJourneyData.getInstance.coinBooster(false);
        }

        public override void startCountdownTime()
        {
            FrenzyJourneyData.getInstance.coinBooster(true);
        }
    }
}
