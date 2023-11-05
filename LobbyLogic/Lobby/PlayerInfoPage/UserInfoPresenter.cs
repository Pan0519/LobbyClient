using CommonILRuntime.BindingModule;
using CommonILRuntime.Module;
using CommonPresenter;
using CommonService;
using EasyUI.Toast;
using UnityEngine;
using UnityEngine.UI;

namespace Lobby.PlayerInfoPage
{
    public class UserInfoPresenter : SystemUIBasePresenter
    {
        private Button closeBtn = null;
        private Button copyBtn = null;
        private Text infoTxt = null;

        private DataInfo dataInfo => DataStore.getInstance.dataInfo;
        private PlayerInfo playerInfo => DataStore.getInstance.playerInfo;
        public override string objPath => "prefab/lobby_login/player_info";
        public override UiLayer uiLayer { get => UiLayer.GameMessage; }

        private const string versionFomat = "Version : {0}";
        private const string resourceVerFormat = "Resource Version : {0}";
        private const string playerIDFormat = "ID : {0}";
        private const string diviceIDFormat = "Device ID : {0}";
        private const string successMsgKey = "Role_copy";

        public override void initUIs()
        {
            closeBtn = getBtnData("close_btn");
            copyBtn = getBtnData("copy_btn");
            infoTxt = getTextData("info_txt");
        }

        public override void init()
        {
            base.init();
            closeBtn.onClick.AddListener(closeBtnClick);
            copyBtn.onClick.AddListener(onClickCopy);
            close();
        }

        public void openInfo()
        {
            open();
            setInfoText();
        }

        private void setInfoText()
        {
            var deviceIDInfo = string.Format(diviceIDFormat, dataInfo.deviceId);
            var playerIDInfo = string.Format(playerIDFormat, getDisplayUserID());
            var versionInfo = string.Format(versionFomat, ApplicationConfig.AppVersion);
            var resourceIDInfo = string.Format(resourceVerFormat, ApplicationConfig.bundleVersion);

            infoTxt.text = convertInfo(versionInfo, resourceIDInfo, playerIDInfo, deviceIDInfo);
        }

        private string getDisplayUserID()
        {
            if (string.IsNullOrEmpty(playerInfo.userID))
            {
                return PlayerPrefs.GetString(ApplicationConfig.TempUserIDKey, string.Empty);
            }
            return playerInfo.userID;
        }

        private string convertInfo(params string[] infos)
        {
            string result = string.Empty;
            int lastIndex = infos.Length - 1;

            for (int i = 0; i <= lastIndex; ++i)
            {
                result += infos[i];
                if (i != lastIndex)
                {
                    result += "\n";
                }
            }

            return result;
        }

        private void onClickCopy()
        {
            var displayMsg = LanguageService.instance.getLanguageValue(successMsgKey);
            GUIUtility.systemCopyBuffer = infoTxt.text;
            Toast.Show(displayMsg, 1.5f);
        }

        public override void animOut()
        {
            UiManager.clearPresnter(this);
        }
    }
}
