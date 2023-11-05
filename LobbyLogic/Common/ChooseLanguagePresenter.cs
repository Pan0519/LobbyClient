using Debug = UnityLogUtility.Debug;
using UnityEngine.UI;
using UnityEngine;
using System;
using Service;
using CommonILRuntime.Module;
using Lobby.UI;
using Services;
using CommonPresenter;

namespace Lobby.Common
{
    class ChooseLanguagePresenter : SystemUIBasePresenter
    {
        public override string objPath => "prefab/page_common/language_choose";
        public override UiLayer uiLayer { get => UiLayer.GameMessage; }
        #region BindingField
        Button closeBtn;
        Button zhBtn;
        Button enBtn;
        #endregion

        public override void initUIs()
        {
            closeBtn = getBtnData("close_btn");
            zhBtn = getBtnData("btn_zh");
            enBtn = getBtnData("btn_en");
        }

        public override void init()
        {
            base.init();
            closeBtn.onClick.AddListener(closeBtnClick);
            zhBtn.onClick.AddListener(zhBtnClick);
            enBtn.onClick.AddListener(enBtnClick);

            zhBtn.interactable = ApplicationConfig.nowLanguage != ApplicationConfig.Language.ZH;
            enBtn.interactable = ApplicationConfig.nowLanguage != ApplicationConfig.Language.EN;
        }

        public override void animOut()
        {
            clear();
        }

        void zhBtnClick()
        {
            zhBtn.interactable = false;
            reloadLanguageFile(ApplicationConfig.Language.ZH);
        }

        void enBtnClick()
        {
            enBtn.interactable = false;
            reloadLanguageFile(ApplicationConfig.Language.EN);
        }

        void reloadLanguageFile(ApplicationConfig.Language language)
        {
            BindingLoadingPage.instance.open();
            LobbySpriteProvider.instance.clearAllSpriteProviders();
            string languageName = Enum.GetName(typeof(ApplicationConfig.Language), language).ToLower();
            PlayerPrefs.SetString(ApplicationConfig.LanguageSaveKey, languageName);
            KeepAliveManager.Instance.stopSendKeepAlive();
            BindingLoadingPage.instance.close();
            UtilServices.reloadLobbyScene(openTransition: false);
            //UtilServices.reloadLobbyScnenByLang();
        }
    }
}
