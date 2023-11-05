using CommonILRuntime.BindingModule;
using Event.Common;
using System;
using UniRx;
using LobbyLogic.NetWork.ResponseStruct;

namespace FarmBlast
{
    public class FarmBlastBoosterNode : BoosterNodePresenter
    {
        public Action<BoostsData> redeemCallback;
        public override void openBoosterShop()
        {
            UiManager.getPresenter<FarmBlastShopPresenter>().openShop(isShowSpinObj: false);
        }
    }
}
