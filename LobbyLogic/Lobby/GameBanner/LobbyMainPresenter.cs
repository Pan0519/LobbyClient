using Debug = UnityLogUtility.Debug;
using CommonILRuntime.BindingModule;
using CommonILRuntime.Module;
using Binding;
using UnityEngine.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UniRx;
using Lobby.Common;
using System.Collections.Generic;
using Service;
using Services;
using CommonService;
using LobbyLogic.NetWork.ResponseStruct;
using System.Linq;
using System;
using HighRoller;
using NewPlayerGuide;

namespace Lobby
{
    class LobbyMainPresenter : ContainerPresenter
    {
        public override string objPath => "prefab/lobby/lobby_ui";
        protected override BackHideBehaviour hideBehaviour => BackHideBehaviour.HideMe;

        #region UIs
        BindingNode bannerNode;
        LoopHorizontalScrollRect gameLoopLayout;
        #endregion

        List<GameInfo> onLineGames = new List<GameInfo>();
        Subject<bool> isScrollingSub = new Subject<bool>();
        List<LobbyGameLongRootPresenter> runJPGames = new List<LobbyGameLongRootPresenter>();

        IDisposable runNextJPDis;
        IDisposable startRunGameJPDis;
        IDisposable delayAddNowRunGameDis;
        LobbyGameLongRootPresenter runningJPGameItem = null;
        IDisposable guideStatusDis;
        public override void initUIs()
        {
            bannerNode = getNodeData("banner_node");
            gameLoopLayout = getBindingData<LoopHorizontalScrollRect>("game_loop_layout");
            guideStatusDis = DataStore.getInstance.guideServices.tutorialStatusSub.Subscribe(checkNowGuideStep).AddTo(uiGameObject);
            if (null == gameLoopLayout.prefabSource)
            {
                gameLoopLayout.prefabSource = ResourceManager.instance.getGameObject("prefab/lobby/game_banner_long");
            }
        }

        public void initGames()
        {
            showGameList();
            showBanner();
            open();
        }

        async void showGameList()
        {
            GameInfoResponse infoResponse = await AppManager.lobbyServer.getGamesInfo();

            var infos = infoResponse.games.GetEnumerator();
            while (infos.MoveNext())
            {
                var info = infos.Current as GameInfoData;
                GameInfo gameInfo = DataStore.getInstance.dataInfo.getGameInfo(info.id);
                if (null == gameInfo)
                {
                    continue;
                }

                gameInfo.checkIsOnline(info.visibleAfter, info.visibleBefore);
                if (!gameInfo.onLine)
                {
                    string afterTime = UtilServices.strConvertToDateTime(info.visibleAfter, DateTime.UtcNow).ToShortDateString();
                    string beforeTime = UtilServices.strConvertToDateTime(info.visibleBefore, DateTime.UtcNow).ToShortDateString();
                    Debug.Log($"{info.id} game is onLine false,visibleTime - After:{afterTime}, Before:{beforeTime}");
                    continue;
                }
                gameInfo.requiredLevel = info.requiredLevel;
                gameInfo.priority = info.priority;
                gameInfo.labels = info.labels;
                gameInfo.tags = info.tags;
                gameInfo.jackpotMultiplier = info.jackpotMultiplier;
                gameInfo.checkIsOpen(info.availableAfter, info.availableBefore);
                onLineGames.Add(gameInfo);
            }
            onLineGames = onLineGames.OrderBy(info => info.id).ToList();
            onLineGames = onLineGames.OrderBy(game => game.requiredLevel).ToList();
            onLineGames.Sort((x, y) =>
            {
                if (x.requiredLevel == y.requiredLevel)
                {
                    return -x.priority.CompareTo(y.priority);
                }
                return 0;
            });

            DataStore.getInstance.dataInfo.onLineGameInfos = onLineGames;

            gameLoopLayout.onEndDrag = gameLayoutOnEndDrag;
            gameLoopLayout.onBeginDrag = gameLayoutOnBeginDrag;
            gameLoopLayout.setNewItemAction = newGameItemAction;
            gameLoopLayout.deleteItemAction = deleteGameItemAction;
            gameLoopLayout.totalCount = onLineGames.Count;
            gameLoopLayout.RefreshCells();
        }

        void gameLayoutOnEndDrag(PointerEventData eventData)
        {
            isScrollingSub.OnNext(false);
            startRunGameJP();
        }

        void gameLayoutOnBeginDrag(PointerEventData eventData)
        {
            if (null != runningJPGameItem)
            {
                runJPGames.Add(runningJPGameItem);
            }
            UtilServices.disposeSubscribes(runNextJPDis, startRunGameJPDis, delayAddNowRunGameDis);
            isScrollingSub.OnNext(true);
        }

        public override void open()
        {
            base.open();
            HighRollerCrossDaysManager.getInstance.crossDaysReturnPayStartRun();
        }

        void checkNowGuideStep(GuideStatus nowStatus)
        {
            guideStatusDis.Dispose();
            if (GuideStatus.Completed != nowStatus)
            {
                return;
            }
            startRunGameJP();
        }

        void startRunGameJP()
        {
            startRunGameJPDis = Observable.Timer(TimeSpan.FromSeconds(1.0f)).Subscribe(_ =>
             {
                 runGameJP();
             }).AddTo(uiGameObject);
        }

        void newGameItemAction(GameObject go, int index)
        {
            LobbyGameInfo gameInfo = new LobbyGameInfo();
            gameInfo.setGameInfo(onLineGames[index]);
            var gameItem = UiManager.bindNode<LobbyGameLongRootPresenter>(go);
            gameItem.setGameInfo(gameInfo);
            gameItem.setScrollingSub(isScrollingSub);
            gameItem.setJPRunComplete(runNextGameJP);
            if (gameInfo.jackpotMultiplier > 0 && gameInfo.isOpen && !gameInfo.isLock)
            {
                runJPGames.Add(gameItem);
            }
        }

        void deleteGameItemAction(GameObject go)
        {
            var removeGameItem = runJPGames.Find(presenter => presenter.uiGameObject.name.Equals(go.name));
            if (null != removeGameItem)
            {
                runJPGames.Remove(removeGameItem);
            }
        }

        LobbyGameLongRootPresenter lastJpGamePresenter;
        void runNextGameJP(LobbyGameLongRootPresenter nowRunGamePresenter)
        {
            lastJpGamePresenter = nowRunGamePresenter;
            runningJPGameItem = null;
            if (runJPGames.Count <= 0)
            {
                runJPGames.Add(nowRunGamePresenter);
            }
            else
            {
                delayAddNowRunGameDis = Observable.Timer(TimeSpan.FromSeconds(3.5f)).Subscribe(time =>
                {
                    runJPGames.Add(nowRunGamePresenter);
                }).AddTo(uiGameObject);
            }
            runNextJPDis = Observable.Timer(TimeSpan.FromSeconds(3)).Subscribe(_ =>
             {
                 runGameJP();
                 runNextJPDis = null;
             }).AddTo(uiGameObject);
        }

        void runGameJP()
        {
            if (runJPGames.Count <= 0)
            {
                return;
            }
            runningJPGameItem = runJPGames[UnityEngine.Random.Range(0, runJPGames.Count - 1)];
            runningJPGameItem.runJP();
            runJPGames.Remove(runningJPGameItem);

            //if (null != lastJpGamePresenter && lastJpGamePresenter.lobbyGameInfo.gameID.Equals(runningJPGameItem.lobbyGameInfo.gameID))
            //{
            //    Debug.LogError($"runGameJP getSame Game {runningJPGameItem.lobbyGameInfo.gameID}");
            //    foreach (var game in runJPGames)
            //    {
            //        Debug.Log($"runJPGames {game.lobbyGameInfo.gameID}");
            //    }
            //}
        }

        void showBanner()
        {
            LobbyBannerNode lobbyBannerNode = UiManager.bindNode<LobbyBannerNode>(bannerNode.cachedGameObject);
            lobbyBannerNode.open();
        }
    }
}
