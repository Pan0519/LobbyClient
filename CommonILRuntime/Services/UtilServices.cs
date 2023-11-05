using UnityEngine.SceneManagement;
using System;
using System.Linq;
using System.Globalization;
using CommonILRuntime.BindingModule;
using Common.VIP;
using UnityEngine;
using CommonService;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Services
{
    public class TimeStruct
    {
        public int days;
        public int hours;
        public int minutes;
        public int seconds;

        public string toTimeString()
        {
            return toTimeString("Days");
        }

        /// <summary>
        /// 1/22 確認顯示天數計算無條件進位
        /// </summary>
        public string toTimeString(string daysStr)
        {
            string timeString;
            if (days > 0)
            {
                timeString = $"{days + 1} {daysStr}";
            }
            else
            {
                timeString = string.Format("{0:00}:{1:00}:{2:00}", hours, minutes, seconds);
            }
            return timeString;
        }
    }


    public static class UtilServices
    {
        public static DateTime nowTime { get { return DateTime.UtcNow; } }
        public static float screenProportion { get { return (float)Screen.height / (float)Screen.width; } }
        public static long nowUtcTimeSeconds
        {
            get
            {
                return new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
            }
        }
        public static ScreenOrientation getNowScreenOrientation
        {
            get
            {
                switch (ApplicationConfig.NowRuntimePlatform)
                {
                    case RuntimePlatform.WindowsEditor:
                    case RuntimePlatform.OSXEditor:
                        if (Screen.height > Screen.width)
                        {
                            return ScreenOrientation.Portrait;
                        }
                        else
                        {
                            return ScreenOrientation.Landscape;
                        }

                    default:
                        return Screen.orientation;
                }
            }
        }
        public static string getOrientationObjPath(string landPath)
        {
            switch (getNowScreenOrientation)
            {
                case ScreenOrientation.Portrait:
                case ScreenOrientation.PortraitUpsideDown:
                    return $"{landPath}_portrait";

                default:
                    return landPath;
            }
        }
        public static string getOrientationObjPath(string portraitPath, string landPath)
        {
            switch (getNowScreenOrientation)
            {
                case ScreenOrientation.Portrait:
                case ScreenOrientation.PortraitUpsideDown:
                    return portraitPath;

                default:
                    return landPath;
            }
        }

        public static string setDays(long day)
        {
            return $"{day / 24 / 60}";
        }

        public static async void backToLobby(bool openTransition = true)
        {
            if (openTransition)
            {
                DataStore.getInstance.eventInGameToLobbyService.OpenFuncInLobby(FunctionNo.OpenTransitionXParty);
            }
            await Task.Delay(TimeSpan.FromSeconds(1.5f));
            DataStore.getInstance.dataInfo.resetNowPlayGameID();
            TweenManager.killAll();
            DataStore.getInstance.eventInGameToLobbyService.OpenFuncInLobby(FunctionNo.ClearAllDispose);
            clearAllUI();
            SceneManager.LoadScene("Lobby");
        }

        public static void backToLobby()
        {
            backToLobby(openTransition: true);
        }

        //public static async void backToLobbyByLang()
        //{
        //    //DataStore.getInstance.eventInGameToLobbyService.OpenFuncInLobby(FunctionNo.OpenTransitionXParty);
        //    await Task.Delay(TimeSpan.FromSeconds(1.5f));
        //    DataStore.getInstance.dataInfo.resetNowPlayGameID();
        //    TweenManager.killAll();
        //    DataStore.getInstance.eventInGameToLobbyService.OpenFuncInLobby(FunctionNo.ClearAllDispose);
        //    clearAllUI();
        //    SceneManager.LoadScene("Lobby");
        //}

        public static void reloadLobbyScene(bool openTransition)
        {
            isOpenErrConnection = false;
            ApplicationConfig.isAlreadyLogin = false;
            releaseBundles();
            backToLobby(openTransition);
        }

        public static void reloadLobbyScene()
        {
            reloadLobbyScene(openTransition: false);
        }

        //public static void reloadLobbyScnenByLang()
        //{
        //    isOpenErrConnection = false;
        //    ApplicationConfig.isAlreadyLogin = false;
        //    releaseBundles();
        //    backToLobbyByLang();
        //}

        public static void clearAllUI()
        {
            UiManager.clearAllPresenter();
            OpenMsgBoxService.Instance.clearMsgBox();
        }

        public static void releaseBundles()
        {
            if (!ApplicationConfig.isLoadFromAB)
            {
                return;
            }
            AssetBundleManager.Instance.releaseAllBundles();
            VipSpriteGetter.clear();
        }

        public static DateTime strConvertToDateTime(string timeStr, DateTime defaultTime)
        {
            return (string.IsNullOrEmpty(timeStr)) ? defaultTime : DateTime.Parse(timeStr, null, DateTimeStyles.RoundtripKind);
        }

        public static TimeSpan getEndTimeStruct(string endTime)
        {
            return strConvertToDateTime(endTime, DateTime.MinValue).Subtract(DateTime.UtcNow);
        }

        public static string formatCountTimeSpan(TimeSpan timeSpan)
        {
            return toTimeStruct(timeSpan).toTimeString();
        }

        public static TimeStruct toTimeStruct(float timeSeconds)
        {
            if (timeSeconds <= 0)
            {
                return new TimeStruct();
            }

            var timeStruct = new TimeStruct();
            timeStruct.days = (int)(timeSeconds / 86400);
            timeStruct.hours = (int)(timeSeconds % 86400) / 3600;
            timeStruct.minutes = ((int)timeSeconds % 3600) / 60;
            timeStruct.seconds = (int)timeSeconds % 60;
            return timeStruct;
        }

        public static TimeStruct toTimeStruct(TimeSpan timeSpan)
        {
            return toTimeStruct((float)timeSpan.TotalSeconds);
        }

        public static TimeStruct updateTimeStruct(float timeSeconds, TimeStruct timeStruct)
        {
            if (timeSeconds <= 0)
            {
                return new TimeStruct();
            }

            timeStruct.days = (int)(timeSeconds / 86400);
            timeStruct.hours = (int)(timeSeconds % 86400) / 3600;
            timeStruct.minutes = ((int)timeSeconds % 3600) / 60;
            timeStruct.seconds = (int)timeSeconds % 60;
            return timeStruct;
        }

        public static TimeStruct updateTimeStruct(TimeSpan timeSpan, TimeStruct timeStruct)
        {
            return updateTimeStruct((float)timeSpan.TotalSeconds, timeStruct);
        }
        public static void disposeSubscribes(List<IDisposable> subscribes)
        {
            disposeSubscribes(subscribes.ToArray());
        }
        public static void disposeSubscribes(params IDisposable[] subscribes)
        {
            for (int i = 0; i < subscribes.Length; ++i)
            {
                var subscribe = subscribes[i];
                if (null == subscribe)
                {
                    continue;
                }

                subscribe.Dispose();
            }
        }

        public static string getLocalizationAltasPath(string altasName)
        {
            return $"/localization/{ApplicationConfig.nowLanguage.ToString().ToLower()}/{altasName}_localization";
        }

        public static Dictionary<string, Sprite> spritesToDictionary(Sprite[] sprites)
        {
            Dictionary<string, Sprite> dict = new Dictionary<string, Sprite>();
            if (null != sprites)
            {
                for (int i = 0; i < sprites.Length; i++)
                {
                    Sprite sprite = sprites[i];
                    if (dict.ContainsKey(sprite.name))
                    {
                        //新的作法待優化，各系統所有圖會集合一包，暫時不秀重複的圖檔
                        //Debug.LogError($"spritesToDictionary has same key : {sprite.name} ");
                        continue;
                    }
                    dict.Add(sprite.name, sprite);
                }
            }
            return dict;
        }

        public static bool enumParse<T>(string parseStr, out T result) where T : struct
        {
            var enumNames = Enum.GetNames(typeof(T));
            var enumValues = Enum.GetValues(typeof(T)).Cast<T>().ToList();
            for (int i = 0; i < enumNames.Length; ++i)
            {
                if (enumNames[i].Equals(parseStr, StringComparison.OrdinalIgnoreCase))
                {
                    result = enumValues[i];
                    return true;
                }
            }
            Debug.LogError($"enumParse {parseStr} failed");
            result = default(T);
            return false;
        }
        static TextInfo textInfo = new CultureInfo("es-ES", false).TextInfo;
        public static string toTitleCase(this string convertValue)
        {
            return textInfo.ToTitleCase(convertValue);
        }

        #region timeCompare
        /// <summary>
        /// <0-time1早於nowTime
        ///  0-time1==nowTime
        /// >0-time1晚於nowTime
        /// </summary>
        public static CompareTimeResult compareTimeWithNow(DateTime time1)
        {
            return compareTimes(time1, nowTime);
        }

        /// <summary>
        /// <0-time1早於time2
        ///  0-time1==time2
        /// >0-time1晚於time2
        /// </summary>
        public static CompareTimeResult compareTimes(DateTime time1, DateTime time2)
        {
            return (CompareTimeResult)DateTime.Compare(time1, time2);
        }
        #endregion

        static bool isOpenErrConnection;

        public static void openErrConnectionBox()
        {
            if (isOpenErrConnection)
            {
                return;
            }
            isOpenErrConnection = true;
            OpenMsgBoxService.Instance.openNormalBox(title: LanguageService.instance.getLanguageValue("Err_Connection"),
               content: LanguageService.instance.getLanguageValue("Err_CheckLoginAgain"),
               callback: reloadLobbyScene);
        }

        public static string byteToKB(long size)
        {
            return $"{Math.Round(Convert.ToDouble(size / 1024d))}";
        }

        #region OutomeDataParse
        public static string outcomeCoinKey { get { return "coin"; } }
        public static string outcomeCoinBankKey { get { return "coin-bank"; } }
        public static string outcomeVIPPointKey { get { return "vip-point"; } }
        public static string outcomeLvupBoost { get { return "level-up-reward-boost"; } }
        public static string outcomeDiamondPoint { get { return "diamond-point"; } }
        public static string outcomeExpBoost { get { return "exp-boost"; } }
        public static string outcomePuzzlePack { get { return "puzzle-pack"; } }
        public static string outcomePuzzleVoucher { get { return "puzzle-voucher"; } }
        public static string outcomeHighPassPoint { get { return "high-roller-pass-point"; } }
        #endregion
    }

    public enum CompareTimeResult
    {
        Earlier = -1,
        Same,
        Later,
    }
}
