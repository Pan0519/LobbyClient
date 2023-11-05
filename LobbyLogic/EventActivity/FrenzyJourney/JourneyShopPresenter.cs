using Event.Shop;
using Service;
using EventActivity;

namespace FrenzyJourney
{
    class JourneyShopPresenter : EventShopPresenter
    {
        public override string objPath => FrenzyJourneyData.getInstance.getPrefabFullPath("fj_shop");
        public override void initContainerPresenter()
        {
            resOrder = new string[] { AssetBundleData.getBundleName(BundleType.FrenzyJourney) };
            base.initContainerPresenter();
        }
        public override void open()
        {
            FrenzyJourneyData.getInstance.stopAutoPlay();
            base.open();
        }

        public override void setShopNodePresenter()
        {
            setBoosterItem<EventShopNodePresenter>(1, BoosterType.Dice).handlerRedeem = updateBoostData;
            setBoosterItem<EventShopNodePresenter>(2, BoosterType.Coin).handlerRedeem = updateBoostData;
            setBoosterItem<EventShopNodePresenter>(3, BoosterType.FrenzyDice).handlerRedeem = updateBoostData;
        }

        async void updateBoostData()
        {
            var data = await AppManager.eventServer.getFrenzyJourneyData();
            FrenzyJourneyData.getInstance.updateBoostData(data.BoostsData);
            FrenzyJourneyData.getInstance.updateFrenzyDiceCount(data.BoostsData.FrenzyDice, isCheckDiceType: true);
        }
    }
}
