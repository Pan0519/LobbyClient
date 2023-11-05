using System.Threading.Tasks;
using CommonService;
using CommonPresenter;
using CommonILRuntime.BindingModule;
using Services;
using UnityEngine;

namespace GameBar
{
    public class GameBarServices
    {
        static GameBarServices _instance = new GameBarServices();
        public static GameBarServices instance { get { return _instance; } }

        async Task<GameOrientation> getNowGameOrientation()
        {
            return await DataStore.getInstance.dataInfo.getNowGameOrientation();
        }

        public GameTopBarPresenter getGameTopBar()
        {
            //GameOrientation gameOrientation = await getNowGameOrientation();
            var screen = UtilServices.getNowScreenOrientation;
            switch (screen)
            {
                case ScreenOrientation.Portrait:
                case ScreenOrientation.PortraitUpsideDown:
                    return UiManager.getPresenter<GamePortraitTopBarPresenter>();
                default:
                    return UiManager.getPresenter<GameTopBarPresenter>();
            }
        }

        public GameBottomBarPresenter getGameBottomBar()
        {
            return UiManager.getPresenter<GameBottomBarPresenter>();
        }
    }
}
