using CommonILRuntime.Module;
using CommonPresenter;
using UnityEngine;
using UnityEngine.UI;

namespace FrenzyJourney
{
    class JourneyTipPresenter : SystemUIBasePresenter
    {
        public override string objPath => FrenzyJourneyData.getInstance.getPrefabFullPath("fj_tip_info");
        public override UiLayer uiLayer { get => UiLayer.System; }

        Button closeBtn;
        GameObject bossPage;
        GameObject gamePage;
        public override void initContainerPresenter()
        {
            resOrder = new string[] { AssetBundleData.getBundleName(BundleType.FrenzyJourney)};
            base.initContainerPresenter();
        }
        public override void initUIs()
        {
            closeBtn = getBtnData("close_btn");
            bossPage = getGameObjectData("boss_page");
            gamePage = getGameObjectData("game_page");
        }

        public override void init()
        {
            base.init();
            closeBtn.onClick.AddListener(closeBtnClick);
        }

        public override void open()
        {
            bossPage.setActiveWhenChange(FrenzyJourneyData.getInstance.frenzySceneType == FrenzySceneType.Boss);
            gamePage.setActiveWhenChange(FrenzyJourneyData.getInstance.frenzySceneType == FrenzySceneType.Main);
            base.open();
        }

        public override void animOut()
        {
            clear();
        }
    }
}
