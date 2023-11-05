using System;
using Services;
using CommonService;
using System.Collections.Generic;
using Debug = UnityLogUtility.Debug;

namespace Network
{
    public static class ShowErrorCodeMsgServices
    {
        static Dictionary<Result, errorCodeMsgData> errorCodeMsgDataDict = new Dictionary<Result, errorCodeMsgData>();
        public static void addErrorMsgBox(errorCodeMsgData errorData, params Result[] results)
        {
            var errorMsgData = errorData;

            for (int i = 0; i < results.Length; ++i)
            {
                var errorResult = results[i];

                if (errorCodeMsgDataDict.ContainsKey(errorResult))
                {
                    continue;
                }
                errorCodeMsgDataDict.Add(errorResult, errorMsgData);
            }
        }

        public static void showErrorMsgBox(Result result)
        {
            if (Result.SystemSessionError == result)
            {
                Debug.LogError($"SessionError ,SessionId : {DataStore.getInstance.dataInfo.sessionSid}");
            }
            errorCodeMsgData errorCodeMsg;
            if (errorCodeMsgDataDict.TryGetValue(result, out errorCodeMsg))
            {
                OpenMsgBoxService.Instance.openNormalBox(errorCodeMsg.getTitle(), string.Format(errorCodeMsg.getContent(), (int)result), errorCodeMsg.confirmCB);
                return;
            }

            OpenMsgBoxService.Instance.openNormalBox(LanguageService.instance.getLanguageValue("Err_System"),
                string.Format(LanguageService.instance.getLanguageValue("Err_ErrCodeOnly"), (int)result),
                UtilServices.reloadLobbyScene);
        }
    }


    public class errorCodeMsgData
    {
        public string title { private get; set; }
        public string content { private get; set; }
        public string titleKey { private get; set; }
        public string contentKey { private get; set; }
        public Action confirmCB;

        public string getTitle()
        {
            if (!string.IsNullOrEmpty(title))
            {
                return title;
            }

            if (!string.IsNullOrEmpty(titleKey))
            {
                return LanguageService.instance.getLanguageValue(titleKey);
            }

            return string.Empty;
        }

        public string getContent()
        {
            if (!string.IsNullOrEmpty(content))
            {
                return content;
            }


            if (!string.IsNullOrEmpty(contentKey))
            {
                return LanguageService.instance.getLanguageValue(contentKey);
            }

            return string.Empty;
        }
    }
}
