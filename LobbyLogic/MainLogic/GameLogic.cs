using CommonService;
using Service;
using UnityEngine;
using UnityEngine.UI;
using UniRx;
using System;
using CommonILRuntime.BindingModule;
using Services;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using Lobby.LoadingUIModule;
using LobbyLogic.Common;
using NewPlayerGuide;
using SaveTheDog;
using Debug = UnityLogUtility.Debug;
using Lobby.Service;

namespace Lobby
{
    //Lobby 要啟動遊戲時，會進入 Game Scene, GameScene Mono: GameStartup 會 invoke GameLogic.initAsync
    class GameLogic
    {
        static AppDomainManager domainManager;

        static GameInfo nowGameInfo;
        static IDisposable initFinishSubscribe;
        static float stopLoadingProgress;
        static float runningProgress;
        static TimeSpan closeLoadingWaitTime = TimeSpan.FromSeconds(1.0f);
        static long totalFileSize = 0;
        static long curFileSize = 0;
        static long tmpFileSize = 0;
        static long bundleFileSize = 0;
        static IDisposable _disposable = null;
        public static async void initAsync()
        {
            showLoadingDefaultInfo();
            runningProgress = 0;
            GamePauseManager.clearCount();
            setNowNewbieData();
            initToLobbyServices();
            stopLoadingProgress = UnityEngine.Random.Range(1.0f, 0.95f);
            nowGameInfo = await DataStore.getInstance.dataInfo.getNowPlayGameInfo();
            switch (nowGameInfo.getOrientation())
            {
                case GameOrientation.Landscape:
                    await UIRootChangeScreenServices.Instance.changeToLandscape();
                    await LoadingUIManager.instance.changeToLandscape();
                    break;

                case GameOrientation.Portrait:
                    await UIRootChangeScreenServices.Instance.changeToPortrait();
                    await LoadingUIManager.instance.changeToPortrait();
                    addBarExtend();
                    break;
            }

            HighRoller.HighRollerDataManager.instance.checkGetReturnToPayTime();

            setResourceFileName();
            initFinishSubscribe = ApplicationConfig.initFinish.Subscribe(_ =>
             {
                 initFinishSubscribe.Dispose();
                 closeLoadingPage();
             });

            if (ApplicationConfig.isLoadFromAB)
            {
                totalFileSize = AssetBundleManager.Instance.getFileSizeByType($"{nowGameInfo.name}");
                tmpFileSize = 0;
                curFileSize = 0;
                bundleFileSize = 0;
                Util.Log($"loadBundle....fileSize:{totalFileSize}");
                if (totalFileSize <= 0)
                {
                    isFakeLoaing = false;
                    LoadingUIManager.instance.fakeDefaultLoading(0f, 1f, 2f, () =>
                    {
                        isFakeLoaing = true;
                    });
                }
                else
                {
                    isFakeLoaing = true;
                    LoadingUIManager.instance.clearFakeLoadingDispose();
                }


                if (totalFileSize <= 0)
                {
                    _disposable = Observable.EveryUpdate().Subscribe(_ =>
                    {
                        CoroutineManager.Update();
                    });
                    CoroutineManager.AddCorotuine(showLoadingInfo());
                }

                LoadingUIManager.instance.openLoadingCloseBtn();
                AssetBundleManager.Instance.fileCountProgress(bundleFileCount);
                AssetBundleManager.Instance.preloadBundles($"{nowGameInfo.name}", async (success) =>
                {
                    UtilServices.disposeSubscribes(_disposable);
                    _disposable = null;

                    if (success)
                    {
                        _disposable = Observable.EveryUpdate().Subscribe(_ =>
                        {
                            if (isFakeLoaing)
                            {
                                checkLoading();
                                invokeGame();
                                UtilServices.disposeSubscribes(_disposable);
                            }
                        });
                    }
                }, bundleLoadProgress);
                return;
            }
            invokeGame();
        }

        //private static async Task<bool> isFakeLoadingTrue()
        //{
        //    return isFakeLoaing;
        //}

        static bool isFakeLoaing = false;

        static void checkLoading()
        {
            if (totalFileSize > 0 && (curFileSize < totalFileSize))
            {
                showLoadingInfo(totalFileSize, totalFileSize);
            }
        }

        static void addBarExtend()
        {
            bool isHighRoller = BetClass.HighRoller == DataStore.getInstance.dataInfo.getChooseBetClassType();
            var tempObj = ResourceManager.instance.getGameObject("prefab/bar_extend");
            GameObject barExtendObj = GameObject.Instantiate<GameObject>(tempObj, UiRoot.instance.propUIBarRoot);
            Image topImg = barExtendObj.transform.Find("top").GetComponent<Image>();
            Image lowImg = barExtendObj.transform.Find("low").GetComponent<Image>();
            string betClassSpriteName = isHighRoller ? "vip" : "normal";
            string topBarSpritePath = $"Bar_Resources/pic_portrait/res_lobby_top_ui_portrait/extend_top_{betClassSpriteName}_portrait";
            topImg.sprite = ResourceManager.instance.load<Sprite>(topBarSpritePath);
            string lowBarSpritePath = $"Bar_Resources/pic_portrait/res_game_low_ui_portrait/extend_down_{betClassSpriteName}_portrait";
            lowImg.sprite = ResourceManager.instance.load<Sprite>(lowBarSpritePath);
        }
        static void showLoadingInfo(long curSize, long totalSize)
        {
            LoadingUIManager.instance.setLoadingInfo($"{LanguageService.instance.getLanguageValue("loadingHint")} {UtilServices.byteToKB(curSize)}KB/{UtilServices.byteToKB(totalSize)}KB");
        }

        static void showLoadingDefaultInfo()
        {
            LoadingUIManager.instance.setLoadingInfo($"{LanguageService.instance.getLanguageValue("loadingHint")}");
        }

        static void bundleFileCount(long fileCount)
        {
            bundleFileSize = fileCount;
        }

        static async void closeLoadingPage()
        {
            if (runningProgress < stopLoadingProgress)
            {
                await Task.Delay(closeLoadingWaitTime);
            }
            LoadingUIManager.instance.runProgressBar(1);
            UIRootChangeScreenServices.Instance.setOrientationObjActive();
            openTransitionBallon();
            await Task.Delay(closeLoadingWaitTime);
            LoadingUIManager.instance.close();
            openAdventureProgress();
            checkGuideStatus();
        }

        static void openTransitionBallon()
        {
            if (BetClass.Adventure != DataStore.getInstance.dataInfo.getChooseBetClassType())
            {
                return;
            }
            GameObject transitionBallon = GameObject.Instantiate(ResourceManager.instance.getGameObject("prefab/transition/transition_ballon"));
            transitionBallon.setActiveWhenChange(false);
            DontDestroyRoot.instance.addChildToCanvas(transitionBallon.transform);
            transitionBallon.transform.localScale = Vector3.one;
            var transitionRect = transitionBallon.transform as RectTransform;
            transitionRect.offsetMax = Vector2.zero;
            transitionRect.offsetMin = Vector2.zero;
            transitionBallon.setActiveWhenChange(true);
            Observable.TimerFrame(180).Subscribe(_ =>
            {
                GameObject.DestroyImmediate(transitionBallon);
            }).AddTo(transitionBallon);
        }

        static void checkGuideStatus()
        {
            if (GuideStatus.Completed == DataStore.getInstance.guideServices.getSaveGuideStatus())
            {
                LobbyStartPopSortManager.instance.finishShowPopPages();
                return;
            }
            var gameGuide = DataStore.getInstance.guideServices.getSaveGameGuideStep();
           
            if (GameGuideStatus.Completed == gameGuide)
            {
                return;
            }
            var guidePage = UiManager.getPresenter<GuidePagePresenter>();
            guidePage.openGuidePage((int)gameGuide);
            guidePage.uiRectTransform.SetAsFirstSibling();
        }

        async static void openAdventureProgress()
        {
            var slotGameID = await DataStore.getInstance.dataInfo.getNowplayGameID();
            if (SaveDogLvKind.Slot != SaveTheDogMapData.instance.getNowRecordKind() || false == SaveTheDogMapData.instance.nowRecord.type.Equals(slotGameID))
            {
                return;
            }

            if (BetClass.Adventure != DataStore.getInstance.dataInfo.getChooseBetClassType())
            {
                return;
            }
            var missionProgress = await AppManager.lobbyServer.getNewbieAdventureMissionProgress();
            if (!string.IsNullOrEmpty(missionProgress.completedAt))
            {
                return;
            }

            UiManager.getPresenter<Mission.ActivityQuestInfoPresenter>().open();
        }

        async static void setNowNewbieData()
        {
            var slotGameID = await DataStore.getInstance.dataInfo.getNowplayGameID();
            if (SaveDogLvKind.Slot != SaveTheDogMapData.instance.getNowRecordKind() || false == SaveTheDogMapData.instance.nowRecord.type.Equals(slotGameID))
            {
                return;
            }
            var newbieData = await AppManager.lobbyServer.getNewbieAdventure();
            SaveTheDogMapData.instance.changeNowOpenStageID(newbieData.stage);
            SaveTheDogMapData.instance.setNowClickLvID(newbieData.level);
        }

        static void initToLobbyServices()
        {
            DataStore.getInstance.playerMoneyPresenter.clearMoneyPresenter();
            FromGameMsgService.getInstance.initFromGameService();
            EventInGameService.getInstance.initFuncInGameService();
        }

        static void bundleLoadProgress(float progress)  //IBundleProvider 0~100
        {
            if (totalFileSize <= 0) return;

            if (0 < progress)
            {
                curFileSize = tmpFileSize + bundleFileSize;
                tmpFileSize += bundleFileSize;
                bundleFileSize = 0;
            }
            else
            {
                curFileSize = tmpFileSize + (long)(bundleFileSize * progress);
            }
            runningProgress = ((float)curFileSize / (float)totalFileSize);
            LoadingUIManager.instance.runProgressBar(runningProgress);

            if (totalFileSize > 0)
            {
                showLoadingInfo(curFileSize, totalFileSize);
            }
        }

        static IEnumerator showLoadingInfo()
        {
            string title = "loadingHint";

            List<int> hintKeys = new List<int>() { 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 };
            List<int> randKeys = new List<int>();
            var rand = new System.Random();
            int len = hintKeys.Count;
            for (int i = 0; i < len; i++)
            {
                int rand_i = rand.Next(hintKeys.Count);
                randKeys.Add(hintKeys[rand_i]);
                hintKeys.RemoveAt(rand_i);
            }

            setLoadingInfo(title);
            yield return 2f;
            setLoadingInfo($"{title}_1");
            yield return 2f;

            for (int i = 0; i < randKeys.Count; i++)
            {
                setLoadingInfo($"{title}_{randKeys[i]}");
                yield return 4f;
            }
        }

        static void setLoadingInfo(string key)
        {
            LoadingPageService.setLoadingInfo($"{LanguageService.instance.getLanguageValue(key)}");
        }

        static async void invokeGame()
        {
            if (string.IsNullOrEmpty(nowGameInfo.serverIP))
            {
                Debug.LogError($"get {nowGameInfo.name} ServerIP is Empty");
                UtilServices.backToLobby();
                return;
            }

            domainManager = await new AppDomainManager().domainInit($"{nowGameInfo.name}");
            domainManager.invokeLogicMainMethod("setInfos", DataStore.getInstance);
            domainManager.invokeLogicMain(DataStore.getInstance.playerInfo.userID, nowGameInfo.serverIP);
            LoadingUIManager.instance.setLoadingPageCloseBtnActive(false);

            openQuestBar();
        }

        static void openQuestBar()
        {
            var progressBar = UiManager.getPresenter<Mission.ActivityQuestProgressPresenter>();
            progressBar.open();
            progressBar.uiRectTransform.SetAsFirstSibling();
        }

        static void setResourceFileName()
        {
            ResourceManager.instance.setArtPath(nowGameInfo.name, "Lobby");
        }
    }
}
