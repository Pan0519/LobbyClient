using LobbyLogic.Login;
using UnityEngine;
using System;
using Service;
using UniRx;
using LobbyLogic.NetWork.ResponseStruct;
using Network;
using LobbyLogic.Common;
using LobbyLogic.Audio;
using Lobby.Audio;
using CommonILRuntime.BindingModule;
using System.Threading.Tasks;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using Services;
using CommonService;
using Lobby.UI;
using SaveTheDog;
using CommonPresenter;
using Debug = UnityLogUtility.Debug;
using Lobby.Service;
using Lobby.Common;

namespace Lobby
{
    class LogicMain
    {
        static LoginPresenter loginPresenter;
        static List<IDisposable> loadingProgressSubscribe = new List<IDisposable>();
        static float preProgress;
        static float lastProgress;
        static TimeSpan closeLoadingWaitTime = TimeSpan.FromSeconds(0.5f);
        static long totalFileSize = 0;
        static long curFileSize = 0;
        static long tmpFileSize = 0;
        static long bundleFileSize = 0;
        static long scrollFileSize = 0;

        static long curFileCount = 0;
        static long totalFileCount = 0;

        static bool isCompletedLoading = false;
        static bool showRealProgress = false;

        static IDisposable _disposable = null;
        static IDisposable _disposable_1 = null;
        static IDisposable _disposable_2 = null;
        static IDisposable _disposable_3 = null;
        public static async void initAsync()
        {
            Util.LogWithTime($"initAsync.... {AssetBundleManager.Instance.bundleInfoMng.bunderVersion}");
            preProgress = 0;

            disposeGameServices();
            GamePauseManager.clearCount();
            //DefaultLoadingPage.setLoadingInfo(string.Empty);
            LoadingPageService.setLoadingInfo(string.Empty);

            LocalNotificationManager.getInstance.init();

            await UIRootChangeScreenServices.Instance.changeToLandscape();
            if (!ApplicationConfig.isAlreadyLogin)
            {
                Common.KeepAliveManager.Instance.stopSendKeepAlive();
                ApplicationConfig.reloadLobbyScene = UtilServices.reloadLobbyScene;
                ErrorCodeMsgService.registerCommonErrorMSg();
                await AppManager.initAsync();

                if (ApplicationConfig.isLoadFromAB)
                {
                    loadBundle();
                    //moveVegasToCache();
                    return;
                }
            }

            invokeGame();
        }

        /// <summary>
        /// 移動vegas到cache
        /// </summary>
        static void moveVegasToCache()
        {
            string vegasCachePath = Path.Combine(Application.temporaryCachePath, "vegas");
            string[] vegasFileName = { "manifest", "vegas_game", "vegas_game.manifest" };
            string[] pathName = { "bundle", "vegas" };
            string streamingAssetsPath = Path.Combine(Application.streamingAssetsPath, Path.Combine(pathName));

            if (!Directory.Exists(vegasCachePath))
            {
                Directory.CreateDirectory(vegasCachePath);
                MoveFileTools.instance.moveFile(streamingAssetsPath, vegasCachePath, loadBundle, vegasFileName);
            }
            else
            {
                loadBundle();
            }
        }

        static void loadBundle()
        {
            AssetBundleManager.Instance.fileCountProgress(bundleFileCount);

            string[] bundleNames = new string[] {
                             "lobby_publicity_save_the_dog",
                             "lobby_save_the_dog",
                             "savedog",
                             "lobby_puzzle",
                             "lobby_stay_minigame",
                             "lobby_login_reward",
                             "lobby_daily_mission"
                             };

            totalFileSize = AssetBundleManager.Instance.getFileSizeByType("common");
            totalFileSize += AssetBundleManager.Instance.getFileSizeByType("lobby");

            tmpFileSize = 0;
            curFileSize = 0;
            bundleFileSize = 0;
            scrollFileSize = 0;
            curFileCount = 0;

            totalFileCount = 0;
            totalFileCount = AssetBundleManager.Instance.getFileCountByType("common");
            totalFileCount += AssetBundleManager.Instance.getFileCountByType("lobby");

            for (int i = 0; i < bundleNames.Length; i++)
            {
                totalFileSize += AssetBundleManager.Instance.getFileSizeByType(bundleNames[i]);
                totalFileCount += AssetBundleManager.Instance.getFileCountByType(bundleNames[i]);
            }

            _disposable = Observable.EveryUpdate().Subscribe(_ =>
            {
                CoroutineManager.Update();
            });

            DataStore.getInstance.gameTimeManager.Resume();
            CoroutineManager.AddCorotuine(showLoadingInfo());

            Util.LogWithTime($"loadBundle....fileSize:{totalFileSize}");
            Util.LogWithTime($"loadBundle....fileCount:{totalFileCount}");
            if (totalFileSize <= 0)
            {
                lastProgress = LoadingPageService.getNowProgressBarFillAmount();
                //lastProgress = DefaultLoadingPage.getNowProgressBarFillAmount();
                isCompletedLoading = false;
                showRealProgress = false;

                float curTime = Time.time;
                double tmpProgress = 0;
                double stepProgress = 0;
                _disposable_3 = Observable.EveryUpdate().Subscribe(_ =>
                {
                    stepProgress = curFileCount * 1.0f / totalFileCount;
                    //Util.LogWithTime($"stepProgress_curFileCount:{curFileCount}");
                    //Util.LogWithTime($"stepProgress_totalFileCount:{totalFileCount}");
                    //Util.LogWithTime($"stepProgress:{stepProgress}");
                    tmpProgress += 0.01f;
                    if (tmpProgress >= stepProgress) tmpProgress = stepProgress;
                    if (showRealProgress)
                    {
                        preProgress = lastProgress + ((1 - lastProgress) * (float)tmpProgress);

                        if (preProgress >= 1)
                        {
                            isCompletedLoading = true;
                            preProgress = 1;
                            _disposable_3.Dispose();
                        }

                        //DefaultLoadingPage.runLoadingProgress(preProgress);
                        LoadingPageService.runLoadingProgress(preProgress);
                        //Util.LogWithTime($"bundleLoadProgress:{preProgress}");
                    }
                    else
                    {
                        //lastProgress = DefaultLoadingPage.getNowProgressBarFillAmount();
                        lastProgress = LoadingPageService.getNowProgressBarFillAmount();
                        if (lastProgress >= 0.8f)
                        {
                            showRealProgress = true;
                        }
                    }
                });
            }
            else
            {
                lastProgress = LoadingPageService.getNowProgressBarFillAmount();
                //lastProgress = DefaultLoadingPage.getNowProgressBarFillAmount();
                isCompletedLoading = false;
                showRealProgress = false;
                //DefaultLoadingPage.clearFakeLoadingDispose();
                scrollFileSize = curFileSize;
                float nextProgress = totalFileCount > 0 ? 0.9f : 1f;
                double stepProgress = 0;
                _disposable_3 = Observable.EveryUpdate().Subscribe(_ =>
                {
                    scrollFileSize += UnityEngine.Random.Range(1024, 102400);
                    if (curFileSize >= totalFileSize) scrollFileSize = totalFileSize;
                    if (scrollFileSize >= totalFileSize) scrollFileSize = totalFileSize;

                    if (showRealProgress)
                    {

                        if (preProgress < nextProgress)
                        {
                            preProgress = lastProgress + ((nextProgress - lastProgress) * scrollFileSize / totalFileSize);
                        }
                        else
                        {
                            stepProgress = curFileCount * 1.0 / totalFileCount;
                            preProgress = nextProgress + ((1 - nextProgress) * (float)stepProgress);

                        }
                        if (preProgress >= 1)
                        {
                            isCompletedLoading = true;
                            preProgress = 1.0f;
                            _disposable_3.Dispose();
                        }
                        //DefaultLoadingPage.runLoadingProgress(preProgress);
                        LoadingPageService.runLoadingProgress(preProgress);
                    }
                    else
                    {
                        //lastProgress = DefaultLoadingPage.getNowProgressBarFillAmount();
                        lastProgress = LoadingPageService.getNowProgressBarFillAmount();
                        if (lastProgress >= 0.8f)
                        {
                            showRealProgress = true;
                        }
                    }
                });

            }

            AssetBundleManager.Instance.preloadBundles("common", (success) =>
             {
                 if (success)
                 {
                     Util.LogWithTime("commom bundles success");
                     AssetBundleManager.Instance.preloadBundles("lobby", (preloadRes) =>
                         {

                             CoroutineManager.AddCorotuine(downloads(bundleNames, () =>
                             {


                                 if (null != _disposable)
                                     _disposable.Dispose();

                                 if (null != _disposable_3)
                                     _disposable_3.Dispose();
                                 AssetBundleManager.Instance.fileCountProgress(null);
                                 AssetBundleManager.Instance.clearDownloadSubscribe();
                                 checkLoading();
                                 Util.LogWithTime($"lobby bundles success:{isCompletedLoading}");

                                 _disposable_1 = Observable.EveryUpdate().Subscribe(_ =>
                                 {
                                     if (isCompletedLoading && showRealProgress)
                                     {
                                         CoroutineManager.StopCorotuine(showLoadingInfo());
                                         LocalNotificationManager.getInstance.reschedulerNotification();
                                         LoadingPageService.setLoadingInfo("");
                                         invokeGame();
                                         _disposable_1.Dispose();
                                     }
                                 });
                             }));

                         }, bundleLoadProgress);
                 }
             }, bundleLoadProgress);
        }

        static IEnumerator downloads(string[] bundleNames, Action completeCallback = null)
        {
            bool isSuccess = false;
            for (int i = 0; i < bundleNames.Length; i++)
            {
                AssetBundleManager.Instance.preloadBundles(bundleNames[i], (other) =>
                {
                    isSuccess = true;
                }, bundleLoadProgress);
                yield return new BooleanWrapper(() => isSuccess);
                isSuccess = false;
            }

            completeCallback?.Invoke();
        }

        static void disposeGameServices()
        {
            DataStore.getInstance.playerMoneyPresenter.clearMoneyPresenter();
            FromGameMsgService.getInstance.disposeGameMsgService();
            EventInGameService.getInstance.clearGameServices();
        }

        static void bundleLoadProgress(float progress)
        {
            if (totalFileSize <= 0)
            {
                curFileCount += 1;
                return;
            }

            if (progress >= 0.7f) curFileCount += 1;

            //已完成此檔
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
        }

        static IEnumerator showLoadingInfo()
        {
            string title = "loadingHint";

            List<int> hintKeys = new List<int>() { 2, 3, 4, 5, 6, 9, 10, 11, 12, 13, 14 };
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

        static void checkLoading()
        {
            if (totalFileSize > 0 && (scrollFileSize < totalFileSize))
            {
                float actTime = 0.5f;
                float startTime = Time.time;
                float nowTime = 0;
                long addSize = totalFileSize - scrollFileSize;

                float addProgress = 1f - preProgress;

                _disposable_2 = Observable.EveryUpdate().Subscribe(_ =>
                {
                    nowTime = Time.time - startTime;

                    if (nowTime >= actTime)
                    {
                        showRealProgress = true;
                        isCompletedLoading = true;
                        _disposable_2.Dispose();
                    }
                    else
                    {
                        //showLoadingInfo(scrollFileSize + (long)(addSize * (nowTime / actTime)), totalFileSize);
                    }

                    //DefaultLoadingPage.runLoadingProgress(preProgress + (addProgress * (nowTime / actTime)));
                    LoadingPageService.runLoadingProgress(preProgress + (addProgress * (nowTime / actTime)));
                });
            }
            else
            {
                showRealProgress = true;
                isCompletedLoading = true;
            }
        }

        static void setLoadingInfo(string key)
        {
            //DefaultLoadingPage.setLoadingInfo($"{LanguageService.instance.getLanguageValue(key)}");
            LoadingPageService.setLoadingInfo($"{LanguageService.instance.getLanguageValue(key)}");
        }

        static void bundleFileCount(long fileCount)
        {
            bundleFileSize = fileCount;
        }

        static async void invokeGame()
        {
            BindingLoadingPage.instance.close();
            EventInGameService.getInstance.initFuncInGameService();
            if (ApplicationConfig.isAlreadyLogin)
            {
                LoadingUIModule.LoadingUIManager.instance.close();
                openLobbyPage();
                loadingProgressSubscribe.Add(LoadingPageService.getProgressChangeEvent().Subscribe(loadingProgressChangeValued));
                //loadingProgressSubscribe.Add(DefaultLoadingPage.progressChangeValued.Subscribe(loadingProgressChangeValued));
                PlayerInfoResponse response = await AppManager.lobbyServer.getPlayerInfo();
                if (response.result == Result.OK)
                {
                    //Debug.LogWarning("LogicMain");
                    LobbyPlayerInfo.setPlayerInfo(response);
                    if (null != topBarPresenter)
                    {
                        topBarPresenter.initExpBar();
                    }
                }

                closeLoadingPage();
                AudioManager.instance.playBGM(AudioPathProvider.getAudioPath(LobbyMainAudio.Main_BGM));
                await HighRoller.HighRollerDataManager.instance.getHighUserRecordAndCheck();
                return;
            }

            if (ApplicationConfig.environment == ApplicationConfig.Environment.Prod)
            {
                AppsFlyerSDKService.instance.initSDK();
            }
            ApplicationConfig.isAlreadyLogin = true;
            //AudioManager.instance.playAudioOnce(AudioPathProvider.getAudioPath(LobbyMainAudio.Opening)); //TODO 登入音效替換後補回or刪除
            loginPresenter = UiManager.getPresenter<LoginPresenter>();
            loginPresenter.openLobbyPresenter = openLobbyPage;

            if (!loginPresenter.checkIsAutoLogin())
            {
                loginPresenter.open();
                //DefaultLoadingPage.closeLoadingPage();
                LoadingPageService.closeLoadingPage();
            }
        }

        async static void loadingProgressChangeValued(float progress)
        {
            if (progress < 1)
            {
                return;
            }
            await Task.Delay(closeLoadingWaitTime);
            //DefaultLoadingPage.closeLoadingPage();
            //DefaultLoadingPage.resetSliderValue();

            LoadingPageService.closeLoadingPage();
            LoadingPageService.resetSliderValue();
            UtilServices.disposeSubscribes(loadingProgressSubscribe);
        }

        async static void closeLoadingPage()
        {
            await Task.Delay(closeLoadingWaitTime);
            if (!SaveTheDogMapData.instance.isOpenSaveTheDog)
            {
                TransitionxPartyServices.instance.closeTransitionPage();
            }
            //DefaultLoadingPage.runLoadingProgress(1);
            LoadingPageService.runLoadingProgress(1);
        }

        static LobbyTopBarPresenter topBarPresenter;

        static void openLobbyPage()
        {
            if (false == SaveTheDogMapData.instance.isDogGuideComplete)
            {
                openSaveTheDogMap();
                return;
            }

            UiManager.getPresenter<LobbyBottomBarPresenter>().open();
            NoticeManager.instance.init();
            topBarPresenter = UiManager.getPresenter<LobbyTopBarPresenter>();
            UiManager.getPresenter<LobbyMainPresenter>().initGames();
            topBarPresenter.open();
            if (checkNeedOpenDogMap())
            {
                openSaveTheDogMap();
            }
        }

        static bool checkNeedOpenDogMap()
        {
            return (BetClass.Adventure == DataStore.getInstance.dataInfo.getChooseBetClassType() || SaveTheDogMapData.instance.isOpenSaveTheDog)
                && (GuideStatus.Completed == DataStore.getInstance.guideServices.getSaveGuideStatus());
        }

        static void openSaveTheDogMap()
        {
            UiManager.getPresenter<SaveTheDogMapPresenter>().open();
        }
    }
}
