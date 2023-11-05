using Service;
using CommonService;
using LobbyLogic.NetWork.ResponseStruct;
using LobbyLogic.Common;
using CommonILRuntime.BindingModule;
using System;
using System.Threading.Tasks;

namespace Shop.LimitTimeShop
{
    public class LimitTimeShopManager
    {
        static LimitTimeShopManager _instance = new LimitTimeShopManager();
        public static LimitTimeShopManager getInstance { get { return _instance; } }
        public SpecialOfferFirst firstPurchase { get; private set; }

        public async void openLimitTimeFirstPage(Action closeCB = null)
        {
            var productDataResponse = await AppManager.lobbyServer.getSpecialOffer();
            DataStore.getInstance.limitTimeServices.setHasLimitData(null != productDataResponse.firstPurchase);
            if (null == productDataResponse.firstPurchase)
            {
                DataStore.getInstance.limitTimeServices.limitSaleFinish();
                GamePauseManager.gameResume();
                return;
            }

            firstPurchase = productDataResponse.firstPurchase;
            UiManager.getPresenter<LimitTimeFirstPresenter>().openPage(closeCB);
            //UiManager.getPresenter<LimitTimeFirstPresenter>().open();
        }

        public async Task<bool> noCoinOpenLimitFirstPage()
        {
            var productDataResponse = await AppManager.lobbyServer.getSpecialOffer();
            DataStore.getInstance.limitTimeServices.setHasLimitData(null != productDataResponse.firstPurchase);
            if (null == productDataResponse.firstPurchase)
            {
                DataStore.getInstance.limitTimeServices.limitSaleFinish();
                return false;
            }

            firstPurchase = productDataResponse.firstPurchase;
            UiManager.getPresenter<LimitTimeFirstPresenter>().openPage(null);
            return true;
        }
    }
}
