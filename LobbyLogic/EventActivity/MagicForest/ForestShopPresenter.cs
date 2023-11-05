using Event.Shop;
using Service;
using EventActivity;

namespace MagicForest
{
    class ForestShopPresenter : EventShopPresenter
    {
        public override string objPath => $"{ForestDataServices.prefabPath}/mf_shop";
        public override void initContainerPresenter()
        {
            resOrder = new string[] { AssetBundleData.getBundleName(BundleType.MagicForest) };
            base.initContainerPresenter();
        }
        public override void setShopNodePresenter()
        {
            setBoosterItem<EventShopNodePresenter>(1, BoosterType.Magnifire).handlerRedeem = updateBoostData;
            setBoosterItem<EventShopNodePresenter>(2, BoosterType.Prize).handlerRedeem = updateBoostData;
            setBoosterItem<EventShopNodePresenter>(3, BoosterType.GoldenMallet).handlerRedeem = updateBoostData;
        }
        async void updateBoostData()
        {
            var data = await AppManager.eventServer.getForestInitData();
            ForestDataServices.updateBooster(data.BoostsData);
        }
    }
}
