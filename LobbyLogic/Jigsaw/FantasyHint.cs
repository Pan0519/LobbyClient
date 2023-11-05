using CommonILRuntime.Module;
using CommonPresenter;
using UnityEngine.UI;

namespace Lobby.Jigsaw
{
    public class FantasyHint : SystemUIBasePresenter
    {
        public override string objPath => "prefab/lobby_puzzle/fantasy_wheel_tip_info";
        public override UiLayer uiLayer { get { return UiLayer.System; } }

        Button closeButton;
        public override void initContainerPresenter()
        {
            resOrder = new string[] { AssetBundleData.getBundleName(BundleType.LobbyPuzzle) };
            base.initContainerPresenter();
        }
        public override void initUIs()
        {
            closeButton = getBtnData("closeButton");
        }

        public override void init()
        {
            base.init();
            closeButton.onClick.AddListener(closeBtnClick);
        }

        public override void animOut()
        {
            clear();
        }
    }
}
