using UniRx;
using UnityEngine;
using CommonService;
using CommonPresenter;
using Debug = UnityLogUtility.Debug;
using Game.Common;

namespace Services
{
    public class GameToLobbyServices
    {
        public Subject<GameConfig.GameState> updateGameStataSubject = new Subject<GameConfig.GameState>();
        public Subject<string> wagerIDSubscribe { get; private set; } = new Subject<string>();
        public Subject<bool> bonusTimeSubscribe { get; private set; } = new Subject<bool>();
        public Subject<string> topbarStayGameData { get; private set; } = new Subject<string>();
        public Subject<bool> autoPlayStateListener { get; private set; } = new Subject<bool>();
        public Subject<bool> highRollerVaultData { get; private set; } = new Subject<bool>();
        public Subject<GameGoldenNode> checkTopbarGoldenEgg { get; private set; } = new Subject<GameGoldenNode>();
        public Subject<GameBottomBarPresenter> getGameBottomBar { get; private set; } = new Subject<GameBottomBarPresenter>();

        public void sendWagerId(string wagerID)
        {
            if (string.IsNullOrEmpty(wagerID))
            {
                Debug.Log("sendWagerId ID is empty");
                return;
            }
            wagerIDSubscribe.OnNext(wagerID);
            var topbar = GameBar.GameBarServices.instance.getGameTopBar();
            topbar.openGoldenParticleObj();
        }

        public void sendBonus()
        {
            bonusTimeSubscribe.OnNext(true);
        }

        public void sendStayGameServer()
        {
            topbarStayGameData.OnNext(string.Empty);
        }

        public void sendStayGameRedeem(string gameType)
        {
            topbarStayGameData.OnNext(gameType);
        }

        public void sendAutoPlayState(bool isAutoPlay)
        {
            autoPlayStateListener.OnNext(isAutoPlay);
        }
        public void getHighRollerVault()
        {
            highRollerVaultData.OnNext(true);
        }

        public void setGameBottomBar(GameBottomBarPresenter barPresenter)
        {
            getGameBottomBar.OnNext(barPresenter);
        }

        public void checkGoldenMax(GameGoldenNode goldenNode)
        {
            checkTopbarGoldenEgg.OnNext(goldenNode);
        }
    }
}
