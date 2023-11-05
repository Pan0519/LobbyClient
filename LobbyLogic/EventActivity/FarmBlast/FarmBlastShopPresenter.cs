using Event.Shop;
using LobbyLogic.NetWork.ResponseStruct;
using Service;
using EventActivity;
using UniRx;
using System;

namespace FarmBlast
{
    class FarmBlastShopPresenter : EventShopPresenter
    {
        public override string objPath => "prefab/activity/farm_blast/activity_fb_shop";
        public override void initContainerPresenter()
        {
            resOrder = new string[] { AssetBundleData.getBundleName(BundleType.FarmBlast)};
            base.initContainerPresenter();
        }

        public override void setShopNodePresenter()
        {
            setBoosterItem<EventShopNodePresenter>(1, BoosterType.Ticket).handlerRedeem = redeemCallback;
            setBoosterItem<EventShopNodePresenter>(2, BoosterType.Prize).handlerRedeem = redeemCallback;
            setBoosterItem<EventShopNodePresenter>(3, BoosterType.GoldenTicket).handlerRedeem = redeemCallback;
        }

        async void redeemCallback()
        {
            var data = await AppManager.eventServer.getAppleFarmInitData();
            FarmBlastDataManager.getInstance.updateBoostData(data.BoostsData);
        }
    }
}
