using UnityEngine;
using CommonILRuntime.BindingModule;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using CommonService;
using Services;
using UnityEngine.UI;

namespace Lobby.LoadingUIModule
{
    public class LoadingUIManager
    {
        static LoadingUIManager _instance = new LoadingUIManager();
        public static LoadingUIManager instance { get { return _instance; } }

        Vector2 landscapeResolution = new Vector2(1558, 720);
        Vector2 portraitResolution = new Vector2(720, 1558);

        Dictionary<GameOrientation, GameLoadingPresenter> loadingPresenterDict = new Dictionary<GameOrientation, GameLoadingPresenter>();
        GameLoadingPresenter openLoadingPresneter;
        GameObject loadingPage;
        UICanvasHelper canvasHelper;
        CanvasScaler canvasScaler;
        GameObject transitionBG;
        Image transitionBGImg;
        public LoadingUIManager()
        {
            if (null != loadingPage)
            {
                return;
            }
            var tempPage = ResourceManager.instance.getGameObject("prefab/lobby_login/game_loading");
            loadingPage = GameObject.Instantiate(tempPage);
            canvasHelper = loadingPage.GetComponent<UICanvasHelper>();
            canvasScaler = loadingPage.GetComponent<CanvasScaler>();
            transitionBG = loadingPage.transform.Find("transition_img").gameObject;
            transitionBGImg = transitionBG.GetComponent<Image>();
            DontDestroyRoot.addChild(loadingPage.transform);
            for (int i = 0; i < loadingPage.transform.childCount - 1; ++i)
            {
                loadingPage.transform.GetChild(i).gameObject.setActiveWhenChange(true);
            }
            addPresenterToDict(GameOrientation.Landscape);
            addPresenterToDict(GameOrientation.Portrait);

            loadingPage.setActiveWhenChange(false);
        }

        public async Task changeToLandscape()
        {
            changeResolution(landscapeResolution);
            await setCanvasScaler();
        }

        public async Task changeToPortrait()
        {
            changeResolution(portraitResolution);
            await setCanvasScaler();
        }

        async Task setCanvasScaler()
        {
            transitionBG.setActiveWhenChange(false);
            await Task.Delay(TimeSpan.FromSeconds(1.0f));
            canvasHelper.setCanvasScaler();
            openLoadingPresneter.openGameImg();
        }

        void changeResolution(Vector2 endResolution)
        {
            Vector2 resolution = canvasScaler.referenceResolution;
            if (resolution.Equals(endResolution))
            {
                return;
            }
            canvasScaler.referenceResolution = endResolution;
        }
        GameInfo nowGameInfo;

        public async Task loadScreenOrientationSprite()
        {
            closeAllGamePage();
            nowGameInfo = await DataStore.getInstance.dataInfo.getNowPlayGameInfo();
            string spriteName = $"{UtilServices.getNowScreenOrientation}_{nowGameInfo.loadingBGColor}";
            setBGSprite(spriteName);
        }

        public async void openBGWithAutoClose()
        {
            closeAllGamePage();
            setBGSprite("turnpagepng");
            await Task.Delay(TimeSpan.FromSeconds(1.0f));
            close();
        }

        void setBGSprite(string spriteName)
        {
            if (ResourceManager.UiLoadFrom.Resources == ResourceManager.instance.resourceLoadFrom)
            {
                spriteName = $"{spriteName}.jpg";
            }
            transitionBGImg.sprite = ResourceManager.instance.load<Sprite>($"prefab/game_loading/bg_game_loading_{spriteName}");
            transitionBG.setActiveWhenChange(true);
            loadingPage.setActiveWhenChange(true);
        }

        public async Task openGameLoadingPage()
        {
            var nowGameOrientation = await DataStore.getInstance.dataInfo.getNowGameOrientation();
            openLoadingPresneter = loadingPresenterDict[nowGameOrientation];
            openLoadingPresneter.resetBarValue();

            var presneters = loadingPresenterDict.GetEnumerator();
            while (presneters.MoveNext())
            {
                var loadingPresenter = presneters.Current.Value;
                loadingPresenter.setLoadingInfo(string.Empty);
                loadingPresenter.close();
                if (!string.IsNullOrEmpty(nowGameInfo.loadingBGColor))
                {
                    loadingPresenter.setLoadingBGImg($"{presneters.Current.Key.ToString().ToLower()}_{nowGameInfo.loadingBGColor}");
                }
                if (presneters.Current.Key == nowGameOrientation && !nowGameInfo.id.Equals("20001"))
                {
                    loadingPresenter.setLoadingGameImg(nowGameInfo.id);
                    continue;
                }
                loadingPresenter.setLoadingGameImg(string.Empty);
            }
            openLoadingPresneter.open();
        }

        public void setLoadingPageCloseBtnActive(bool active)
        {
            openLoadingPresneter.setCloseBtnActivte(active);
        }

        public void setLoadingInfo(string info)
        {
            openLoadingPresneter.setLoadingInfo(info);
        }

        void closeAllGamePage()
        {
            var presneters = loadingPresenterDict.GetEnumerator();
            while (presneters.MoveNext())
            {
                var loadingPresenter = presneters.Current.Value;
                loadingPresenter.setLoadingInfo(string.Empty);
                loadingPresenter.close();
            }
        }

        void addPresenterToDict(GameOrientation key)
        {
            var nodeGO = GameObject.Find($"{loadingPage.name}/game_loading_{key.ToString().ToLower()}");
            var nodePresenter = UiManager.bindNode<GameLoadingPresenter>(nodeGO);
            nodePresenter.closeBtnClick = returnToLobby;
            loadingPresenterDict.Add(key, nodePresenter);
        }

        void returnToLobby()
        {
            UtilServices.backToLobby();
            close();
        }

        public void runProgressBar(float value)
        {
            openLoadingPresneter.runLoadingProgress(value);
        }

        public void close()
        {
            loadingPage.setActiveWhenChange(false);
        }

        public void openLoadingCloseBtn()
        {
            openLoadingPresneter.openCloseBtn();
        }

        public void clearFakeLoadingDispose()
        {
            openLoadingPresneter.clearFakeLoadingDispose();
        }
        public void fakeDefaultLoading(float startValue, float endValue, float time, Action callback = null)
        {
            openLoadingPresneter.fakeDefaultLoading(startValue, endValue, time, callback);
        }
    }
}
