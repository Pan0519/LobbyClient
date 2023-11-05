using CommonILRuntime.Module;
using CommonService;
using UnityEngine;
using System.Collections.Generic;
using Spine.Unity;

namespace Lobby
{
    class LobbyLongGameItemPresenter : NodePresenter
    {
        Dictionary<GameItemStatus, string> animTrigger = new Dictionary<GameItemStatus, string>()
        {
            { GameItemStatus.Static,"static"},
            { GameItemStatus.Loop,"loop"},
            { GameItemStatus.ComingSoon,"coming_soon"},
        };
        Animator statusAnim;
        public LobbyGameInfo lobbyGameInfo { get; private set; }

        Dictionary<string, SkeletonAnimation> gameNameAnims = new Dictionary<string, SkeletonAnimation>();

        public override void initUIs()
        {
            gameNameAnims.Add("loop", getBindingData<SkeletonAnimation>("loop_anim"));
            gameNameAnims.Add("static", getBindingData<SkeletonAnimation>("static_anim"));
            gameNameAnims.Add("black", getBindingData<SkeletonAnimation>("black_anim"));
        }

        public override void init()
        {
            statusAnim = uiGameObject.GetComponentInChildren<Animator>();
            initGameStatus();
        }

        void initGameStatus()
        {
            if (DataStore.getInstance.guideServices.nowStatus == Services.GuideStatus.Completed)
            {
                setStatusAnimTrigger(GameItemStatus.Loop);
                return;
            }
            setStatusAnimTrigger(GameItemStatus.Static);
        }

        public void setGameInfo(LobbyGameInfo gameInfo)
        {
            setLanguageName();
            lobbyGameInfo = gameInfo;
            bool isGameOpen = gameInfo.isOpen;
            if (!isGameOpen)
            {
                setStatusAnimTrigger(GameItemStatus.ComingSoon);
            }
        }

        void setLanguageName()
        {
            var gameNames = gameNameAnims.GetEnumerator();
            string language = ApplicationConfig.nowLanguage.ToString().ToLower();
            while (gameNames.MoveNext())
            {
                gameNames.Current.Value.AnimationName = $"long_title_{gameNames.Current.Key}_{language}";
            }
        }

        void setStatusAnimTrigger(GameItemStatus itemStatus)
        {
            string trigger;
            animTrigger.TryGetValue(itemStatus, out trigger);
            statusAnim.SetTrigger(trigger);
        }
    }

    public enum GameItemStatus
    {
        Static,
        Loop,
        ComingSoon,
    }
}
