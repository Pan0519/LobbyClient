using UnityEditor;
using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;

public class CrushTools
{
    [MenuItem("CrushTools/Set Default PlayerSetting")]
    public static void setDefaultPlayerSetting()
    {
        PlayerSettings.allowUnsafeCode = true;
        PlayerSettings.SetApiCompatibilityLevel(BuildTargetGroup.Android, ApiCompatibilityLevel.NET_4_6);
    }

    [MenuItem("CrushTools/Clear All PlayerPrefs")]
    public static void clearAllPlayerPrefs()
    {
        PlayerPrefs.DeleteAll();
    }

    [MenuItem("CrushTools/Clear LoadFrom PlayerPrefs")]
    public static void clearLoadFromPlayerPrefs()
    {
        if (PlayerPrefs.HasKey(ApplicationConfig.LoadFromSaveKey))
        {
            PlayerPrefs.DeleteKey(ApplicationConfig.LoadFromSaveKey);
        }
    }

    public const string Regular = "regular";
    public const string High_Roller = "high-roller";

    [MenuItem("CrushTools/Clear BetInfo PlayerPrefs")]
    public static void clearBetInfoPlayerPrefs()
    {
        if (PlayerPrefs.HasKey(Regular))
        {
            PlayerPrefs.DeleteKey(Regular);
        }

        if (PlayerPrefs.HasKey(High_Roller))
        {
            PlayerPrefs.DeleteKey(High_Roller);
        }
    }

    #region Set DefineSymbol

    static string[] compilerArguments = new string[] { "ENABLE_LOG", "DEBUG" };
    static string loadFromAB { get { return "LOADFROM_AB"; } }
    static string APKName { get { return $"DC_{Application.version}_{DateTime.Now.ToString("MMdd")}.apk"; } }
    static BuildTargetGroup buildTargetGroup
    {
        get
        {
#if UNITY_ANDROID
            return BuildTargetGroup.Android;
#else
            return BuildTargetGroup.iOS;
#endif
        }
    }

    static ApplicationConfig.Environment currentEnvironment
    {
        get
        {
#if INNER
            return ApplicationConfig.Environment.Inner;
#elif OUTER
            return ApplicationConfig.Environment.Outer;
#elif STAGE
            return ApplicationConfig.Environment.Stage;
#elif PROD
            return ApplicationConfig.Environment.Prod;
#else
            return ApplicationConfig.Environment.Dev;
#endif
        }
    }

    const string devDefine = "CrushTools/Set DefineSymbol/Dev DefineSymbol";
    [MenuItem(devDefine)]
    static void setDevDefineSymbol()
    {
        List<string> argumentsList = new List<string>();
        argumentsList.AddRange(compilerArguments);
        argumentsList.Add(loadFromAB);
        argumentsList.Add("DEV");
        setDefineSymbol(argumentsList.ToArray());
    }

    [MenuItem(devDefine, true)]
    static bool setDevDefineSymbolCurrent()
    {
        Menu.SetChecked(devDefine, ApplicationConfig.Environment.Dev == currentEnvironment);
        return true;
    }

    const string stableDefine = "CrushTools/Set DefineSymbol/Inner DefineSymbol";
    [MenuItem(stableDefine)]
    static void setStableDefineSymbol()
    {
        List<string> argumentsList = new List<string>();
        argumentsList.AddRange(compilerArguments);
        argumentsList.Add(loadFromAB);
        argumentsList.Add("INNER");
        setDefineSymbol(argumentsList.ToArray());
    }
    [MenuItem(stableDefine, true)]
    static bool setStableDefineSymbolCurrent()
    {
        Menu.SetChecked(stableDefine, ApplicationConfig.Environment.Inner == currentEnvironment);
        return true;
    }

    const string releaseDefine = "CrushTools/Set DefineSymbol/Outer DefineSymbol";
    [MenuItem(releaseDefine)]
    static void setReleaseDefineSymbol()
    {
        List<string> argumentsList = new List<string>();
        argumentsList.AddRange(compilerArguments);
        argumentsList.Add(loadFromAB);
        argumentsList.Add("OUTER");
        setDefineSymbol(argumentsList.ToArray());
    }
    [MenuItem(releaseDefine, true)]
    static bool setReleaseDefineSymbolCurrent()
    {
        Menu.SetChecked(releaseDefine, ApplicationConfig.Environment.Outer == currentEnvironment);
        return true;
    }

    const string stageDefine = "CrushTools/Set DefineSymbol/Stage DefineSymbol";
    [MenuItem(stageDefine)]
    static void setStageDefineSymbol()
    {
        List<string> argumentsList = new List<string>();
        argumentsList.AddRange(compilerArguments);
        argumentsList.Add(loadFromAB);
        argumentsList.Add("STAGE");
        setDefineSymbol(argumentsList.ToArray());
    }
    [MenuItem(stageDefine, true)]
    static bool setStageDefineSymbolCurrent()
    {
        Menu.SetChecked(stageDefine, ApplicationConfig.Environment.Stage == currentEnvironment);
        return true;
    }

    const string prodDefine = "CrushTools/Set DefineSymbol/Prod DefineSymbol";
    [MenuItem(prodDefine)]
    static void setProdDefineSymbol()
    {
        List<string> argumentsList = new List<string>();
        argumentsList.AddRange(compilerArguments);
        argumentsList.Add(loadFromAB);
        argumentsList.Add("PROD");
        setDefineSymbol(argumentsList.ToArray());
    }

    [MenuItem(prodDefine, true)]
    static bool setProdDefineSymbolCurrent()
    {
        Menu.SetChecked(prodDefine, ApplicationConfig.Environment.Prod == currentEnvironment);
        return true;
    }

    const string normalProdDefine = "CrushTools/Set DefineSymbol/Normal Prod DefineSymbol";
    [MenuItem(normalProdDefine)]
    static void setNormalProdDefineSymbol()
    {
        List<string> argumentsList = new List<string>();
        argumentsList.Add(loadFromAB);
        argumentsList.Add("PROD");
        setDefineSymbol(argumentsList.ToArray());
    }

    [MenuItem(normalProdDefine, true)]
    static bool setOnlineProdDefineSymbolCurrent()
    {
        Menu.SetChecked(normalProdDefine, ApplicationConfig.Environment.Prod == currentEnvironment);
        return true;
    }

    static void setDefineSymbol(string[] defines)
    {
        PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, defines);
    }

    #endregion

    #region Build Andoird APK
    [MenuItem("CrushTools/BuildTool/Build Dev Android")]
    public static void BuildDebugAndroid()
    {
        setDevDefineSymbol();
        buildAPK("Dev");
    }

    [MenuItem("CrushTools/BuildTool/Build Inner Android")]
    public static void BuildInnerAndroid()
    {
        setStableDefineSymbol();
        buildAPK("Inner");
    }

    [MenuItem("CrushTools/BuildTool/Build Outer Android")]
    public static void BuildOuterAndroid()
    {
        setReleaseDefineSymbol();
        buildAPK("Outer");
    }

    [MenuItem("CrushTools/BuildTool/Build Stage Android")]
    public static void BuildStageAndroid()
    {
        setStageDefineSymbol();
        buildAPK("Stage");
    }

    [MenuItem("CrushTools/BuildTool/Build Prod Android")]
    public static void BuildProdAndroid()
    {
        setProdDefineSymbol();
        buildAPK("Prod");
    }

    [MenuItem("CrushTools/BuildTool/Build Normal Prod Android")]
    public static void BuildNormalProdAndroid()
    {
        setNormalProdDefineSymbol();
        buildAPK("Normal_Prod");
    }

    static void buildAPK(string apkPlatform)
    {
        string outPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../..", $"{apkPlatform}_{APKName}"));
        BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, outPath, BuildTarget.Android, BuildOptions.None);
        AssetDatabase.Refresh();
    }

    #endregion
}

public class UiLoadFromTools
{
    const string itemAssetBundle = "CrushTools/LoadFrom/AssetBundle";
    const string itemResource = "CrushTools/LoadFrom/Resource";

    static ApplicationConfig.LoadFrom current { get; set; } = (ApplicationConfig.LoadFrom)PlayerPrefs.GetInt(ApplicationConfig.LoadFromSaveKey, 0);

    [MenuItem(itemAssetBundle)]
    static void setBundle()
    {
        current = ApplicationConfig.LoadFrom.AssetBundle;
        saveLoadFrom();
    }

    [MenuItem(itemAssetBundle, true)]
    static bool setBundleValidate()
    {
        Menu.SetChecked(itemAssetBundle, current == ApplicationConfig.LoadFrom.AssetBundle);
        return true;
    }

    [MenuItem(itemResource)]
    static void setResource()
    {
        current = ApplicationConfig.LoadFrom.Resources;
        saveLoadFrom();
    }

    [MenuItem(itemResource, true)]
    static bool setResourceValidate()
    {
        Menu.SetChecked(itemResource, current == ApplicationConfig.LoadFrom.Resources);
        return true;
    }

    static void saveLoadFrom()
    {
        PlayerPrefs.SetInt(ApplicationConfig.LoadFromSaveKey, (int)current);
    }
}
