using UnityEngine;
using UniRx;
using System;
public static class ApplicationConfig
{
    public enum Language
    {
        None,
        ZH,
        EN
    }

    public enum LoadFrom
    {
        Resources,
        AssetBundle,
    }

    public enum Environment
    {
        Dev = 0,
        Inner = 1,
        Outer = 2,
        Stage,
        Prod,
    }

    public static Environment environment
    {
        get
        {
#if INNER
            return Environment.Inner;
#elif OUTER
            return Environment.Outer;
#elif STAGE
            return Environment.Stage;
#elif PROD
            return Environment.Prod;
#else
            return Environment.Dev;
#endif
        }
    }

    public enum APKChannel
    {
        Official,
        GooglePlay,
        AppleAppStore
    }

    public static APKChannel apkChannel
    {
        get
        {
#if OFFICIAL
            return APKChannel.Official;
#elif GOOGLE
            return APKChannel.GooglePlay;
#elif IOS || UNITY_IOS

            return APKChannel.AppleAppStore;
#else
            switch (NowRuntimePlatform)
            {
                case RuntimePlatform.IPhonePlayer:
                case RuntimePlatform.OSXEditor:
                    return APKChannel.AppleAppStore;

                default:
                    return APKChannel.GooglePlay;
            }
#endif
        }
    }

    public static string AppVersion
    {
        get
        {
            return Application.version;
        }
    }
    public static string bundleVersion = "0.0.0";
    static string _contentHost = string.Empty;

    public static string CONTENT_HOST
    {
        get
        {
#if UNITY_EDITOR
            if (string.IsNullOrEmpty(_contentHost))
            {
                _contentHost = "http://192.168.128.235:8080";
            }
#endif
            return _contentHost;
        }
        set
        {
            _contentHost = value;
        }
    }
    public static string ContentTypeStr { get { return "Content-Type"; } }
    public static string ContentLength { get { return "Content-Length"; } }
    public static string ContentAccept { get { return "Accept"; } }
    public static string ContentSid { get { return "Authorization"; } }
    public static string ApplicationMsgPack { get { return "application/msgpack"; } }
    public static string ApplicationJson { get { return "application/json"; } }
    public static string ApplicationPatchMsgpack { get { return "application/vnd.user.v1+msgpack"; } }
    public static string UserAgent { get { return "User-Agent"; } }
    public static string LoadFromSaveKey { get { return "LoadFrom"; } }
    public static string MusicVolumeSaveKey { get { return "MusicVolume"; } }
    public static string SoundVolumeSaveKey { get { return "SoundVolume"; } }
    public static string LanguageSaveKey { get { return "Language"; } }
    public static string TempUserIDKey { get { return "TempUserID"; } }
    public static string EditorDeiveceID { get; set; }
   
    #region [Keys for bet class in PlyerPrefs]

    public const string LastBetClass_Regular = "LastBetClass_Regular";
    public const string LastBetClass_HighRoller = "LastBetClass_HighRoller";

    #endregion

    public static string getStreamingPath
    {
        get
        {
#if UNITY_EDITOR_OSX
            return $"file://{Application.streamingAssetsPath}";
#elif UNITY_IOS
            return $"file://{Application.dataPath}/Raw";
#else
            return Application.streamingAssetsPath;
#endif
        }
    }

    public static string deviceID
    {
        get
        {
#if UNITY_EDITOR
            if (string.IsNullOrEmpty(EditorDeiveceID))
            {
                return SystemInfo.deviceUniqueIdentifier;
            }
            PlayerPrefs.SetString("EditorDeiveceID", EditorDeiveceID);
            return EditorDeiveceID;
#elif UNITY_IPHONE && !OUTER
            string iosDeviceID = iOSServices.Instance.getOnlyIDByKeyChain();
            if (string.IsNullOrEmpty(iosDeviceID))
            {
                iosDeviceID = iOSServices.Instance.saveDeiveIDToKeychain(SystemInfo.deviceUniqueIdentifier);
            }
            return iosDeviceID;

#else
            return SystemInfo.deviceUniqueIdentifier;
#endif
        }
    }

    public static bool isFBBind { get; set; }

    public static bool isAlreadyLogin { get; set; }

    public static string resourceGameFilePath { get; set; }

    public static bool serializerRegistered { get; set; }

    public static Action reloadLobbyScene = null;

    public static bool isLoadFromAB
    {
        get
        {
#if LOADFROM_AB && !UNITY_EDITOR
            return true;
#else
            return ResourceManager.instance.resourceLoadFrom == ResourceManager.UiLoadFrom.AssetBundle; ;
#endif
        }
    }

    public static Subject<bool> initFinish = new Subject<bool>();

    public static string getPicPath(string fileName)
    {
        return $"texture/res_{fileName}/texture/";
    }

    public static RuntimePlatform NowRuntimePlatform
    {
        get
        {
            return Application.platform;
        }
    }

    static Language _language = Language.EN;

    public static Language nowLanguage
    {
        get
        {
            return _language;
        }

        set
        {
            _language = value;
        }
    }

    public static AppDomainManager lobbyDomainManager { get; set; }
    public static bool isiOSSimplify { get; set; } = defaultSubmit;
    public static bool isUpdateAvailable { get; set; }

    public static bool defaultSubmit
    {
        get
        {
#if SUBMIT
            return true;
#endif
            return false;
        }
    }


    public static string platformName
    {
        get
        {
#if UNITY_ANDROID
              return "Android";
#elif UNITY_IOS
            return "iOS";
#else
            return "Android";
#endif
        }
    }
}
