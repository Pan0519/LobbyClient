using LobbyLogic.NetWork.ResponseStruct;

namespace Lobby.Popup
{
    public class PopUpCharge : PopUpActivity, IPopUpActivityPresenter
    {
        public override string objPath { get { return "prefab/activity/rookie/casino_crush_publicity"; } }    //TODO: 換成彈窗儲值的 prefab

        public override void initContainerPresenter()
        {
            resOrder = new string[] {AssetBundleData.getBundleName(BundleType.PublicityCasinoCrush)};
            base.initContainerPresenter();
        }
        public override void init()
        {
            base.init();
        }

        public void setData(PopupData data)
        {
            //string infoString = "999";
            //setInfo(infoString);
        }

        protected override void onConfirmClick()
        {
            //TODO: 前往儲值頁
            close();
        }
    }
}
