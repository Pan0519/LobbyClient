using UnityEngine;
using UnityEngine.UI;
using Lobby.PickupBetPage;
using UnityEngine.SceneManagement;
using CommonService;
using UniRx;
using System;
using CommonILRuntime.Module;
using CommonILRuntime.BindingModule;

namespace Lobby
{
    class LobbyGameLongRootPresenter : NodePresenter
    {
        #region BindingField
        RectTransform rootRect;
        GameObject loadingObj;
        Button entryBtn;
        LongStatusNodePresenter statusNodePresenter;
        #endregion
        public LobbyGameInfo lobbyGameInfo { get; private set; }

        Action<LobbyGameLongRootPresenter> jpRunFinishCB;
        float jpRunDurationTime { get { return 10; } }
        string gameID { get { return lobbyGameInfo.gameID; } }
        public long nowJp { get; private set; }
        string jpTweenID;
        LobbyLongGameItemPresenter gameItemPresenter;
        public override void initUIs()
        {
            rootRect = getRectData("banner_root_rect");
            loadingObj = getGameObjectData("loading_obj");
            entryBtn = getBtnData("entry_btn");
            statusNodePresenter = UiManager.bindNode<LongStatusNodePresenter>(getNodeData("status_node").cachedGameObject);
        }

        public override void init()
        {
            loadingObj.setActiveWhenChange(false);
            entryBtn.onClick.RemoveAllListeners();
            entryBtn.onClick.AddListener(itemClick);
            closeJPObj();
        }

        public void setGameInfo(LobbyGameInfo gameInfo)
        {
            for (int i = 0; i < rootRect.childCount; ++i)
            {
                ResourceManager.instance.returnObjectToPool(rootRect.GetChild(i).gameObject);
            }

            lobbyGameInfo = gameInfo;
            uiGameObject.name = $"Item-{gameID}";
            GameObject originalObj = GameBannerManager.getBannerItem(gameID);
            statusNodePresenter.setGameName(gameInfo.languageName);
            if (null == originalObj)
            {
                Debug.Log($"get {gameID} Obj is null");
                setTempGameItem();
                return;
            }
            PoolObject itemPresenterObj = ResourceManager.instance.getObjectFromPool(originalObj, rootRect);
            gameItemPresenter = UiManager.bindNode<LobbyLongGameItemPresenter>(itemPresenterObj.cachedGameObject);
            gameItemPresenter.setGameInfo(lobbyGameInfo);
            entryBtn.interactable = lobbyGameInfo.isOpen;
            statusNodePresenter.setUnLockLv(gameInfo.unLockLv);
            statusNodePresenter.setHintObjActivte(gameInfo.gameState);
        }

        public GameObject setGuideInfo(LobbyGameInfo gameInfo)
        {
            lobbyGameInfo = gameInfo;
            var tempGo = ResourceManager.instance.getGameObject("prefab/lobby_game_banner/game_banner_long");
            GameObject bannerObj = GameObject.Instantiate<GameObject>(tempGo, rootRect);
            LobbyTempGameItemPresenter itemPresenter = UiManager.bindNode<LobbyTempGameItemPresenter>(bannerObj);
            itemPresenter.setGameInfo(lobbyGameInfo);
            return itemPresenter.uiGameObject;
        }

        public void setScrollingSub(Subject<bool> isScrolling)
        {
            isScrolling.Subscribe(isMainScrolling).AddTo(uiGameObject);
        }
        bool scrolling;
        void isMainScrolling(bool isScrolling)
        {
            scrolling = isScrolling;
            if (isScrolling && !string.IsNullOrEmpty(jpTweenID))
            {
                closeJPObj();
                TweenManager.tweenKill(jpTweenID);
            }
        }
        void setTempGameItem()
        {
            PoolObject itemPresenterObj = ResourceManager.instance.getObjectFromPool("prefab/lobby/game_banner_long", rootRect);
            LobbyTempGameItemPresenter itemPresenter = UiManager.bindNode<LobbyTempGameItemPresenter>(itemPresenterObj.cachedGameObject);
            bool hasSprite = itemPresenter.setGameInfo(lobbyGameInfo);
            if (!hasSprite)
            {
                close();
            }
        }
        #region RunJP
        void closeJPObj()
        {
            statusNodePresenter.setJPObjEnable(false);
        }
        public void setJPRunComplete(Action<LobbyGameLongRootPresenter> jpRunComplete)
        {
            jpRunFinishCB = jpRunComplete;
        }
        public async void runJP()
        {
            nowJp = await lobbyGameInfo.getInitJP();
            updateJPText(nowJp);
            long maxJP = lobbyGameInfo.getMaxJP(nowJp);
            statusNodePresenter.setJPObjEnable(true);
            jpTweenID = TweenManager.tweenToLong(nowJp, maxJP, jpRunDurationTime, updateJPText, jpRunComplete);
        }
        void jpRunComplete()
        {
            jpTweenID = string.Empty;
            closeJPObj();
            if (null != jpRunFinishCB)
            {
                jpRunFinishCB(this);
            }
        }

        void updateJPText(long jpUpdateVal)
        {
            statusNodePresenter.updateJPText(jpUpdateVal);
        }
        #endregion
        private void itemClick()
        {
            if (scrolling)
            {
                return;
            }
            if (lobbyGameInfo.isLock)
            {
                statusNodePresenter.openLvTip();
                return;
            }
            if (DataStore.getInstance.playerInfo.level < 4)
            {
                toGameScene(lobbyGameInfo.gameID);
                return;
            }
            PickupBetPresenter pickupBetPresenter = UiManager.getPresenter<PickupBetPresenter>();
            pickupBetPresenter.close();
            pickupBetPresenter.setFocusGame(lobbyGameInfo);
            pickupBetPresenter.setChangeToGameScene(toGameScene);
        }

        async void toGameScene(string gameID)
        {
            //DefaultLoadingPage.openLoadingPage();
            DataStore.getInstance.dataInfo.setNowPlayGameID(gameID);
            await LoadingUIModule.LoadingUIManager.instance.loadScreenOrientationSprite();
            await LoadingUIModule.LoadingUIManager.instance.openGameLoadingPage();
            TweenManager.killAll();
            UiManager.clearAllPresenter();
            SceneManager.LoadScene("Game");
        }
    }
}
