using Debug = UnityLogUtility.Debug;
using UnityEngine.UI;
using UnityEngine;
using UniRx;
using System;
using Service;
using Network;
using Lobby;
using Lobby.Common;
using LobbyLogic.NetWork.ResponseStruct;
using LobbyLogic.Common;
using LobbyLogic.Audio;
using Lobby.Audio;
using CommonService;
using CommonILRuntime.Module;
using CommonILRuntime.BindingModule;
using System.Threading.Tasks;
using System.Collections.Generic;
using LoginReward;
using Mission;
using Services;
using SaveTheDog;
using Lobby.Service;

namespace LobbyLogic.Login
{
    public enum LoginType
    {
        None,
        Anonymously,
        FB,
        Apple,
    }
    class LoginPresenter : ContainerPresenter
    {
        public override string objPath => "prefab/lobby_login/login";
        public override UiLayer uiLayer { get { return UiLayer.LockHeight; } }
        #region UIs
        Button guestLoginBtn;
        Button appleIDLoginBtn;
        GameObject appleLoginObj;
        Button fbLoginBtn;
        Button chooseLanguageBtn;
        Button termsServiceZhBtn;
        Button privacyZhBtn;
        Button termsServiceEnBtn;
        Button privacyEnBtn;
        GameObject hintZhBtns;
        GameObject hintEnBtns;
        Button loginBtn;
        Button userInfoBtn;
        Button customerServiceBtn;
        #endregion

        public Action openLobbyPresenter;
        LoginType loginType = LoginType.None;

        bool isLoginSuessfully { get; set; } = false;
        bool isAutoLogin { get; set; }

        List<IDisposable> loadingProgressSubscribe = new List<IDisposable>();
        IDisposable getTokenSendLoginSubscribe;

        Dictionary<LoginType, Action> signActionDict = new Dictionary<LoginType, Action>();
        Dictionary<LoginType, string> loginMethodDict = new Dictionary<LoginType, string>()
        {
            { LoginType.Anonymously,"guest"},
            { LoginType.FB,"facebook-id"},
            { LoginType.Apple,"apple-id" },
        };

        public override void initUIs()
        {
            guestLoginBtn = getBtnData("btn_guest_login");
            loginBtn = getBtnData("btn_login");
            appleIDLoginBtn = getBtnData("btn_login_apple");
            fbLoginBtn = getBtnData("btn_login_fb");
            chooseLanguageBtn = getBtnData("btn_language");
            termsServiceZhBtn = getBtnData("btn_terms_service_zh");
            privacyZhBtn = getBtnData("btn_privacy_zh");
            termsServiceEnBtn = getBtnData("btn_terms_service_en");
            privacyEnBtn = getBtnData("btn_privacy_en");
            userInfoBtn = getBtnData("btn_player_info");
            customerServiceBtn = getBtnData("btn_customer_service");

            appleLoginObj = getGameObjectData("apple_login_obj");

            hintZhBtns = getGameObjectData("hint_zh_btn");
            hintEnBtns = getGameObjectData("hint_en_btn");
        }
        public override void init()
        {
            loadingProgressSubscribe.Add(LoadingPageService.getProgressChangeEvent().Subscribe(loadingProgressChageValue).AddTo(uiGameObject));

            appleLoginObj.gameObject.setActiveWhenChange(ApplicationConfig.NowRuntimePlatform == RuntimePlatform.IPhonePlayer);
            initBtnClick();
            openLanguageBtns();

            getTokenSendLoginSubscribe = FirebaseService.TokenSubject.Subscribe(sendLogin).AddTo(uiGameObject);
            loginType = LoginType.None;
            signActionDict.Add(LoginType.Anonymously, anonymouslySign);
            signActionDict.Add(LoginType.FB, fbLogin);
            signActionDict.Add(LoginType.Apple, appleLogin);
        }

        public override void open()
        {
            showTermWithLogin();
            base.open();
        }

        void showTermWithLogin()
        {
            fbLoginBtn.gameObject.setActiveWhenChange(false);
            appleIDLoginBtn.gameObject.setActiveWhenChange(false);
            guestLoginBtn.gameObject.setActiveWhenChange(false);

            var agreement = UiManager.getPresenter<AgreementPagePresenter>();
            agreement.open();
            agreement.agreenCB = () =>
            {
                loginType = LoginType.Anonymously;
                anonymouslySign();
            };
        }

        public bool checkIsAutoLogin()
        {
            isAutoLogin = FirebaseService.isAlreadyLogin();

            if (isAutoLogin)
            {
                FirebaseService.autoLogin();
            }

            return isAutoLogin;
        }

        void loginClick(LoginType loginType)
        {
            string loginMethod;
            if (loginMethodDict.TryGetValue(loginType, out loginMethod))
            {
                PlayerPrefs.SetString("LoginType", loginMethod);
            }
            this.loginType = loginType;
            openPrivacy();
        }

        void loadingProgressChageValue(float progress)
        {
            if (progress > 0.5 && progress < 1)
            {
                if (!isAutoLogin)
                {
                    LoadingPageService.stopLoadingProgress();
                    LoadingPageService.closeLoadingPage();
                    return;
                }
            }

            loadingFinishCallback(progress);
        }

        void openLanguageBtns(string fileName = "")
        {
            ApplicationConfig.Language language;
            if (!Enum.TryParse(fileName.ToLower(), out language))
            {
                language = ApplicationConfig.nowLanguage;
            }

            hintEnBtns.setActiveWhenChange(language == ApplicationConfig.Language.EN);
            hintZhBtns.setActiveWhenChange(language == ApplicationConfig.Language.ZH);
        }

        void initBtnClick()
        {
            termsServiceZhBtn.onClick.AddListener(termsClick);
            termsServiceEnBtn.onClick.AddListener(termsClick);
            privacyEnBtn.onClick.AddListener(openPrivacy);
            privacyZhBtn.onClick.AddListener(openPrivacy);

            chooseLanguageBtn.onClick.AddListener(openLanguageChoose);

            guestLoginBtn.onClick.AddListener(() => { loginClick(LoginType.Anonymously); });
            loginBtn.onClick.AddListener(() => { loginClick(LoginType.Anonymously); });
            fbLoginBtn.onClick.AddListener(() => { loginClick(LoginType.FB); });
            appleIDLoginBtn.onClick.AddListener(() => { loginClick(LoginType.Apple); });

            userInfoBtn.onClick.AddListener(onUserInfoClick);
            customerServiceBtn.onClick.AddListener(onCustomerServiceClick);
        }

        void termsClick()
        {
            openTermWindow(TermContent.Terms);
        }

        void openPrivacy()
        {
            openTermWindow(TermContent.Privacy);
        }

        void openTermWindow(TermContent content)
        {
            UiManager.getPresenter<TermPresenter>().openTermWindow(content);
        }

        void openLanguageChoose()
        {
            UiManager.getPresenter<ChooseLanguagePresenter>().open();
        }

        void onUserInfoClick()
        {
            var infoPage = UiManager.getPresenter<Lobby.PlayerInfoPage.UserInfoPresenter>();
            infoPage.openInfo();
        }

        void onCustomerServiceClick()
        {
            CommonUtil.connectToCustomerService();
        }

        async void anonymouslySign()
        {
            restartRunLoading();
            string deviceID = DataStore.getInstance.dataInfo.deviceId;
            if (string.IsNullOrEmpty(deviceID))
            {
                Debug.Log("get Device is Empty");
                return;
            }
            GuestLoginResponse guestResponse = await AppManager.lobbyServer.guestLogin(deviceID);
            FirebaseService.signInWithCustomToken(guestResponse.token);
        }

        async void sendLogin(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                Debug.Log("Send Login Token is Empty");
                return;
            }
            LoginResponse loginResponse = await AppManager.lobbyServer.login(token);
            if (Result.OK != loginResponse.result || loginResponse.settings == null)
            {
                Debug.LogError("loginData error");
                return;
            }
            KeepAliveManager.Instance.sendKeepAlive();
            UtilServices.disposeSubscribes(getTokenSendLoginSubscribe);
            DataStore.getInstance.dataInfo.setLoginResponse(loginResponse.sid, loginResponse.settings.bindingRewardWorths);
            await getNewbieAdventure();
            await getPlayerInfo();
            if (LoginType.None != loginType && checkIsLessOneMin() && ApplicationConfig.environment == ApplicationConfig.Environment.Prod)
            {
                AppsFlyerSDKService.instance.sendRegisterEvent(loginType.ToString());
            }
            checkOpenLobbyPresenter();
            if (SaveTheDogMapData.instance.isOpenSaveTheDog)
            {
                await Task.Delay(TimeSpan.FromSeconds(1.0f));
            }
            LoadingPageService.runLoadingProgress(1);
        }

        async Task getNewbieAdventure()
        {
            var newbieAdventureResponse = await AppManager.lobbyServer.getNewbieAdventure();
            if (Result.NewbieAdventureSkipCode == newbieAdventureResponse.result)
            {
                skipNewbieTutorial();
                DataStore.getInstance.guideServices.skipGuideStep();
                SaveTheDogMapData.instance.isSkipSaveTheDog = true;
                return;
            }
            SaveTheDogMapData.instance.isSkipSaveTheDog = false;
            SaveTheDogMapData.instance.setNowAdventureRecord(newbieAdventureResponse);
            if (SaveTheDogMapData.instance.isDogGuideComplete)
            {
                getNewbieTutorial();
            }
        }

        void skipNewbieTutorial()
        {
            SaveTheDogMapData.instance.isDogGuideComplete = true;
        }

        void getNewbieTutorial()
        {
            DataStore.getInstance.guideServices.getSaveGuideStatus();
            if (DataStore.getInstance.guideServices.nowStatus != GuideStatus.Completed)
            {
                DataStore.getInstance.limitTimeServices.limitSaleFinish();
            }
        }
        SaveTheDogMapPresenter saveTheDogMap;
        void checkOpenLobbyPresenter()
        {
            if (false == SaveTheDogMapData.instance.isDogGuideComplete)
            {
                return;
            }
            if (null != openLobbyPresenter)
            {
                openLobbyPresenter();
            }
        }

        bool checkIsLessOneMin()
        {
            var createTime = UtilServices.nowTime.Subtract(DataStore.getInstance.playerInfo.createTime);
            return createTime.TotalSeconds < 60;
        }

        async Task getPlayerInfo()
        {
            PlayerInfoResponse response = await AppManager.lobbyServer.getPlayerInfo();
            if (response.result != Result.OK)
            {
                return;
            }
            isLoginSuessfully = true;
            LobbyPlayerInfo.setPlayerInfo(response);

            await HighRoller.HighRollerDataManager.instance.getHighUserRecord();

            if (!SaveTheDogMapData.instance.isDogGuideComplete)
            {
                return;
            }
            var dailyRewardRes = await AppManager.lobbyServer.getDailyReward();
            LobbyStartPopSortManager.instance.setDailyReward(dailyRewardRes.dailyReward);
            LobbyStartPopSortManager.instance.getLimitFirstData();
            await updateDailyReward(dailyRewardRes.dailyReward);
            initDailyMission();
        }

        void initDailyMission()
        {
            MissionData.initAskUnLockLvSubject();
            if (DataStore.getInstance.playerInfo.level >= MissionData.unLockLv && 0 < MissionData.unLockLv)
            {
                MissionData.initMissionProgressData();
            }
        }

        async Task updateDailyReward(DailyReward reward)
        {
            if (null == reward)
            {
                return;
            }
            await LoginRewardServices.instance.initDailyData(reward, reward.rewards.Length > 0);
        }

        IDisposable tokenSubject;
        void fbLogin()
        {
            FirebaseService.initFB();
            tokenSubject = FirebaseService.TokenSubject.Subscribe(_ =>
             {
                 if (null != tokenSubject)
                 {
                     tokenSubject.Dispose();
                 }
                 restartRunLoading();
             });
        }

        void restartRunLoading()
        {
            LoadingPageService.openLoadingPage();
            LoadingPageService.openLoadingBar();
        }

        async void loadingFinishCallback(float progress)
        {
            if (progress >= 1 && isLoginSuessfully)
            {
                await Task.Delay(TimeSpan.FromSeconds(0.5f));
                AudioManager.instance.playBGM(AudioPathProvider.getAudioPath(LobbyMainAudio.Main_BGM));
                if (false == SaveTheDogMapData.instance.isDogGuideComplete)
                {
                    saveTheDogMap = UiManager.getPresenter<SaveTheDogMapPresenter>();
                    saveTheDogMap.open();
                    await Task.Delay(TimeSpan.FromSeconds(2.5f));
                }
                else
                {
                    LobbyStartPopSortManager.instance.startShowPopPages();
                    await Task.Delay(TimeSpan.FromSeconds(0.5f));
                }
                clearLoadingPresenter();
            }
        }

        void clearLoadingPresenter()
        {
            LoadingPageService.closeLoadingPage();
            LoadingPageService.resetSliderValue();
            clear();
        }

        public override void clear()
        {
            UtilServices.disposeSubscribes(loadingProgressSubscribe);
            base.clear();
        }

        void appleLogin()
        {
            iOSServices.Instance.appleLogin(FirebaseService.loginWithApple);
        }
    }
}
