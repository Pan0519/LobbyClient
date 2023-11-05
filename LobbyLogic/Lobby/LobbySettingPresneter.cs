using UnityEngine.UI;
using UnityEngine;
using Service;
using CommonService;
using CommonILRuntime.Module;
using CommonILRuntime.BindingModule;
using LobbyLogic.Audio;
using Lobby.PlayerInfoPage;
using Services;
using System;
using Lobby.Common;
using CommonPresenter;
using System.Collections.Generic;
using UniRx;
using Network;
using LobbyLogic.Common;
using System.Threading.Tasks;

namespace Lobby
{
    class LobbySettingPresneter : SystemUIBasePresenter
    {
        public override string objPath => $"{UtilServices.getOrientationObjPath("prefab/lobby/page_setting")}";
        public override UiLayer uiLayer { get => UiLayer.GameMessage; }
        #region UIs
        Button closeBtn;
        Button zhBtn;
        Button ehBtn;
        Button fbLoginBtn;

        GameObject fbBindedObj;
        //GameObject fbLayoutObj;
        //GameObject languageLayoutObj;

        //Button appleLogin;
        //GameObject appleBinded;
        GameObject appleItem;

        Toggle musicToggle;
        Toggle soundsToggle;
        Toggle notificationToggle;

        GameObject deleteObj;
        Button deleteBtn;

        Button termBtn;
        Button privacyBtn;
        Button disclaimerBtn;

        Button userInfoBtn;
        Button customerServiceBtn;

        Button btnLogOutTest;
        #endregion

        public override void initUIs()
        {
            closeBtn = getBtnData("close_btn");
            zhBtn = getBtnData("zh_btn");
            ehBtn = getBtnData("en_btn");
            fbLoginBtn = getBtnData("fb_login");
            fbBindedObj = getGameObjectData("fb_binded_obj");

            appleItem = getGameObjectData("list_apple");

            musicToggle = getBindingData<Toggle>("musicToggle");
            soundsToggle = getBindingData<Toggle>("soundsToggle");
            notificationToggle = getBindingData<Toggle>("notificationToggle");

            termBtn = getBtnData("term_btn");
            privacyBtn = getBtnData("privacy_btn");
            disclaimerBtn = getBtnData("disclaimer_btn");

            userInfoBtn = getBtnData("btn_player_info");
            customerServiceBtn = getBtnData("btn_support");

            if (UtilServices.getNowScreenOrientation != ScreenOrientation.Portrait)
            {
                bool isShowLogOutBtn = ApplicationConfig.environment == ApplicationConfig.Environment.Inner || ApplicationConfig.environment == ApplicationConfig.Environment.Dev;

                deleteObj = getGameObjectData("delete_account_obj");
                deleteBtn = getBtnData("delete_account_btn");
                if (null != deleteObj)
                {
                    deleteObj.setActiveWhenChange(isShowLogOutBtn);
                }

                btnLogOutTest = getBtnData("btn_log_out_test");
                if (null != btnLogOutTest)
                {
                    btnLogOutTest.gameObject.setActiveWhenChange(isShowLogOutBtn);
                }
            }
        }

        public override void init()
        {
            base.init();

            closeBtn.onClick.AddListener(closeBtnClick);
            termBtn.onClick.AddListener(openTerms);
            privacyBtn.onClick.AddListener(openPrivacy);
            disclaimerBtn.onClick.AddListener(openDisclaimer);
            userInfoBtn.onClick.AddListener(openUserInfo);
            customerServiceBtn.onClick.AddListener(onClickSupportBtn);

            zhBtn.onClick.AddListener(openLanguagePage);
            ehBtn.onClick.AddListener(openLanguagePage);

            musicToggle.onValueChanged.AddListener(musicToggleValeChanged);
            musicToggle.SetIsOnWithoutNotify(true);
            musicToggle.isOn = !AudioManager.instance.isMusicOn;
            musicToggle.targetGraphic.enabled = !musicToggle.isOn;

            soundsToggle.onValueChanged.AddListener(soundToggleValeChanged);
            soundsToggle.SetIsOnWithoutNotify(true);
            soundsToggle.isOn = !AudioManager.instance.isSoundOn;
            soundsToggle.targetGraphic.enabled = !soundsToggle.isOn;

            notificationToggle.onValueChanged.AddListener(notificationToggleValueChanged);
            notificationToggle.SetIsOnWithoutNotify(true);
            notificationToggle.isOn = LocalNotificationManager.getInstance.getLocalKeyState();

            appleItem.setActiveWhenChange(ApplicationConfig.NowRuntimePlatform == RuntimePlatform.IPhonePlayer);
            setLanguageBtnsActive(ApplicationConfig.nowLanguage);
            setFBLoginBtnsActive();

            if (UtilServices.getNowScreenOrientation != ScreenOrientation.Portrait)
            {
                if (null != deleteBtn && null != btnLogOutTest)
                {
                    deleteBtn.onClick.AddListener(deleteAccountClick);
                    btnLogOutTest.onClick.AddListener(logOutClick);
                }
            }
        }

        public override void open()
        {
            base.open();
        }

        void deleteAccountClick()
        {
            UiManager.getPresenter<LobbyLogoutPresenter>().startOpenPage();
        }

        void logOutClick()
        {
            FirebaseService.logout();
            UtilServices.reloadLobbyScene();
        }

        public override void animOut()
        {
            DataStore.getInstance.gameTimeManager.Resume();
            clear();
        }

        void musicToggleValeChanged(bool isOn)
        {
            musicToggle.targetGraphic.enabled = !isOn;
            saveVolumn(ApplicationConfig.MusicVolumeSaveKey, isOn);
        }

        void soundToggleValeChanged(bool isOn)
        {
            soundsToggle.targetGraphic.enabled = !isOn;
            saveVolumn(ApplicationConfig.SoundVolumeSaveKey, isOn);
        }

        void notificationToggleValueChanged(bool isOn)
        {
            notificationToggle.targetGraphic.enabled = !isOn;
            setNotification(isOn);
        }

        void setNotification(bool isOn)
        {
            Util.Log($"setNotification:{isOn}");
            LocalNotificationManager.getInstance.setLocalSaveKey(isOn);
        }

        void saveVolumn(string saveKey, bool isOn)
        {
            float volume = isOn ? 0 : 1;
            if (PlayerPrefs.GetFloat(saveKey, -1) == volume)
            {
                return;
            }
            PlayerPrefs.SetFloat(saveKey, volume);
            AudioManager.instance.setSettingVolume();
        }

        void setLanguageBtnsActive(ApplicationConfig.Language nowLanguage)
        {
            zhBtn.gameObject.setActiveWhenChange(nowLanguage == ApplicationConfig.Language.ZH);
            ehBtn.gameObject.setActiveWhenChange(nowLanguage == ApplicationConfig.Language.EN);
        }

        void setFBLoginBtnsActive()
        {
            bool isFBBinding = DataStore.getInstance.playerInfo.isBindFB;
            fbLoginBtn.gameObject.setActiveWhenChange(!isFBBinding);
            fbLoginBtn.onClick.AddListener(bindingFB);
            fbBindedObj.setActiveWhenChange(isFBBinding);
        }
        IDisposable bindingFBDis;
        void bindingFB()
        {
            bindingFBDis = FirebaseService.TokenSubject.Subscribe(sendLinkFBSuccess).AddTo(uiGameObject);
            FirebaseService.linkFB();
        }
        async void sendLinkFBSuccess(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return;
            }
            ShowErrorCodeMsgServices.addErrorMsgBox(new errorCodeMsgData()
            {
                titleKey = string.Empty,
                contentKey = "FBBindingError",
            }, Result.FBBindingRepeat);

            var response = await AppManager.lobbyServer.linkToFB();
            if (null == response.reward)
            {
                return;
            }
            UtilServices.disposeSubscribes(bindingFBDis);
            UiManager.getPresenter<BindingSuccessMsgPresenter>().openPage(response.reward.coin);
        }

        void openLanguagePage()
        {
            UiManager.getPresenter<ChooseLanguagePresenter>().open();
        }

        void openTerms()
        {
            openTermWindow(TermContent.Terms);
        }

        void openPrivacy()
        {
            openTermWindow(TermContent.Privacy);
        }

        void openDisclaimer()
        {
            openTermWindow(TermContent.Disclaimer);
        }

        void openTermWindow(TermContent termContent)
        {
            UiManager.getPresenter<TermPresenter>().openTermWindow(termContent);
        }

        void openUserInfo()
        {
            var infoPage = UiManager.getPresenter<UserInfoPresenter>();
            infoPage.openInfo();
        }

        void onClickSupportBtn()
        {
            CommonUtil.connectToCustomerService();
        }
    }

    enum TermContent
    {
        Terms,
        Privacy,
        Disclaimer,
    }
}
