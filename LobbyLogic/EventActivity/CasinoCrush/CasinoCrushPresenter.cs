using UnityEngine;
using UnityEngine.UI;
using LobbyLogic.NetWork.ResponseStruct;
using System.Threading.Tasks;
using Service;
using EventActivity;
using Lobby.Common;
using LobbyLogic.Audio;
using Lobby.Audio;
using CommonILRuntime.Module;

namespace CasinoCrush
{
    public class CasinoCrushPresenter : ActivityPresenterBase
    {
        public override UiLayer uiLayer { get { return UiLayer.System; } }
        public override string objPath { get { return "prefab/activity/rookie/activity_rookie_main"; } }
        public override string jsonFileName { get => "rookielevelsetting"; }
        public override string iconSpriteStartName { get => "rookie_item_"; }
        public override string lvupEffectAnimName { get => "rookie_level_up_effect"; }
        public override string[] iconSpriteNames { get => new string[] { "chip", "dice", "poker" }; }
        public override int totalLvCount { get => 7; }
        public override int tutorialsItemNum { get => 11; }
        protected override BackHideBehaviour hideBehaviour => BackHideBehaviour.CanDoBoth;
        //public override int pickItemFinishFrame { get => 45; }

        #region UIs
        GameObject ticketNotEnoughBox;
        Text ticketNotEnoughContent;
        Button ticketNotEnoughCloseBtn;
        Button ticketNotEnoughGoSpin;
        #endregion

        private SendSelectBaseResponse selectResponse;

        #region [ Presenter ]

        public override void initUIs()
        {
            base.initUIs();
            ticketNotEnoughBox = getGameObjectData("ticket_not_enough_obj");
            ticketNotEnoughContent = getTextData("ticket_not_enough_content");
            ticketNotEnoughCloseBtn = getBtnData("ticket_not_enough_close_btn");
            ticketNotEnoughGoSpin = getBtnData("ticket_not_enough_gospin_btn");
        }

        public override void init()
        {
            base.init();
            ticketNotEnoughBox.setActiveWhenChange(false);
            ticketNotEnoughGoSpin.onClick.AddListener(ticketGoSpinClick);
            ticketNotEnoughCloseBtn.onClick.AddListener(() =>
            {
                ticketNotEnoughBox.setActiveWhenChange(false);
            });
            ticketNotEnoughContent.fontSize = ApplicationConfig.nowLanguage == ApplicationConfig.Language.EN ? 26 : 32;
        }
        #endregion

        public override AudioSource playMainBGM()
        {
            return AudioManager.instance.playBGMOnObj(uiGameObject, AudioPathProvider.getAudioPath(LobbyMainAudio.Main_BGM));
        }

        public override async Task<SendSelectBaseResponse> sendServerSelect(int playIndex, int clickItem)
        {
            selectResponse = await AppManager.eventServer.sendSelectItem(playIndex, clickItem);
            return selectResponse;
        }

        public override void ticketNotEnough()
        {
            ticketNotEnoughBox.setActiveWhenChange(true);
        }

        void ticketGoSpinClick()
        {
            ActivityDataStore.activityPageCloseCall();
            closeBtnClick();
        }

        public override string getAwardPrefabPath(AwardKind type)
        {
            if (AwardKind.CollectTarget == type)
            {
                return $"{ActivityDataStore.CasinoCrushPrefabPath}rookie_item_icon";
            }

            return base.getAwardPrefabPath(type);
        }

        public override void openFinalAwardPresenter<T>()
        {
            base.openFinalAwardPresenter<RookieFinalAwardPresenter>();
        }

        public override void openMediumAwardPresenter<T>()
        {
            base.openMediumAwardPresenter<RookieMediumAwardPresenter>();
        }
        public override Sprite findIconSprite(string spriteName)
        {
            return LobbySpriteProvider.instance.getSprite<CasinoCrushSpriteProvider>(LobbySpriteType.CasinoCrush, spriteName);
        }

        public override float getAwardFinalScale()
        {
            switch (awardData.kind)
            {
                case AwardKind.CollectTarget:
                    return 0.81f;
            }
            return base.getAwardFinalScale();
        }
    }
}
