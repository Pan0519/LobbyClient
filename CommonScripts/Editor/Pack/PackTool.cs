using System.Collections.Generic;
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.AssetGraph;
using System.Text.RegularExpressions;
using AssetBundles;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;
using System;
using System.Threading.Tasks;

public class BundleSettingPanel : EditorWindow
{
    [MenuItem("打包/設定資源版號")]
    static void Init()
    {
        ver = PlayerPrefs.GetString("BuildVer");
        EditorWindow window = GetWindow(typeof(BundleSettingPanel));
        GUIContent content = new GUIContent();
        content.text = "資源版號設定";
        window.titleContent = content;
        window.Show();
        if (PlayerPrefs.HasKey("isBundleSet"))
        {
            isBundleSet = PlayerPrefs.GetInt("isBundleSet") == 1;
        }
    }

    public static void addTabs(string lang, Dictionary<string, BundleInfo> localBundleInfo)
    {

        BundleTablePanel window = (BundleTablePanel)GetWindow(typeof(BundleTablePanel));
        GUIContent content = new GUIContent();
        content.text = $"{lang}";

        window.titleContent = content;
        window.setData(lang, localBundleInfo);
        window.Show();

    }

    public static string ver = "0.0.0";
    public static string source_ver = "-";
    public static bool isBundleSet = true;
    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(0, 0, 500, 180), "", EditorStyles.helpBox);
        {
            GUILayout.Label("1.設定要讀取的資源版號，按下設置版號");
            GUILayout.Label("2.打包完會自動讀取總表建立目錄(必須將git content更至最新)");
            isBundleSet = GUILayout.Toggle(isBundleSet, "打包完是否執行步驟2");
            PlayerPrefs.SetInt("isBundleSet", isBundleSet ? 1 : 0);
            GUILayout.Space(10);

            GUILayout.Label("本次打包資源版號");
            GUILayout.BeginHorizontal();
            {
                ver = EditorGUILayout.TextField(ver, GUILayout.Width(80), GUILayout.Height(20));
                GUILayout.Label("請輸入本次打包資源版號 (INNER 版號一律讀取0.0.0)");
            }
            GUILayout.EndHorizontal();

            GUILayout.Label("建新表繼承資源版號");
            GUILayout.BeginHorizontal();
            {

                source_ver = EditorGUILayout.TextField(source_ver, GUILayout.Width(80), GUILayout.Height(20));
                GUILayout.Label("注意：新建資源表，需要輸入來源版號做繼承(非新建不用輸入)");
            }
            GUILayout.EndHorizontal();

            if (GUILayout.Button("記錄設置版號", GUILayout.Width(150), GUILayout.Height(20)))
            {
                PlayerPrefs.SetString("BuildVer", ver);
            }
        }
        GUILayout.EndArea();

        GUILayout.BeginArea(new Rect(0, 180, 500, 50), "", EditorStyles.helpBox);
        {
            GUILayout.Label("檢查output資源，寫入總表，建立資源目錄");
            //GUILayout.Label("若要手動讀取總表建立目錄，再按下面");
            if (GUILayout.Button("寫入資源總表", GUILayout.Width(150), GUILayout.Height(20)))
            {
                var reSelect = EditorUtility.DisplayDialog($"警告", $"即將更新產出 {ver} 資源表，是否更新？", "是", "否");
                if (reSelect)
                {
                    PlayerPrefs.SetString("BuildVer", ver);
                    PackTool.editAndCheckBundleTable();
                }
            }
        }
        GUILayout.EndArea();

        GUILayout.BeginArea(new Rect(0, 230, 500, 50), "", EditorStyles.helpBox);
        {
            GUILayout.Label("檢查資源表共用資源版號，若有共用資源不符會顯示error log");
            if (GUILayout.Button("檢查各語系資源總表", GUILayout.Width(150), GUILayout.Height(20)))
            {
                PackTool.checkCommonTableBundleVer();
            }
        }
        GUILayout.EndArea();

        GUILayout.BeginArea(new Rect(0, 280, 500, 50), "", EditorStyles.helpBox);
        {
            GUILayout.Label("檢查missing prefab，若有missing資源會顯示");
            if (GUILayout.Button("檢查missing prefab", GUILayout.Width(150), GUILayout.Height(20)))
            {
                FindMissingWindow window = (FindMissingWindow)GetWindow(typeof(FindMissingWindow));
                FindMissingWindow.FindMissing();
            }
        }
        GUILayout.EndArea();

        GUILayout.BeginArea(new Rect(0, 330, 500, 100), "", EditorStyles.helpBox);
        {
            GUILayout.Label("讀取總表顯示");
            if (GUILayout.Button("讀取資源總表", GUILayout.Width(150), GUILayout.Height(20)))
            {
                PackTool.readBundleTable();
            }

            if (null != PackTool.localBundleInfo)
            {
                GUILayout.BeginHorizontal();
                {
                    foreach (var pair in PackTool.localBundleInfo)
                    {
                        if (GUILayout.Button(pair.Key, GUILayout.Width(50), GUILayout.Height(20)))
                        {
                            addTabs(pair.Key, pair.Value);
                        }
                    }
                }
                GUILayout.EndHorizontal();
            }
        }
        GUILayout.EndArea();
    }
}

public class BundleTablePanel : EditorWindow
{
    Dictionary<string, BundleInfo> bundleInfo;
    BundleInfo updateBundleInfo = null;
    BundleInfo newBundleInfo = new BundleInfo();
    DownloadInfo info = new DownloadInfo();
    List<KeyValuePair<string, BundleInfo>> mappings;
    public Vector2 scrollPosition;
    string nowlang = "";
    string focusText = "";
    string focusTmp = "";
    string bundleTitle = "";
    string type = "";
    string ver = "";
    string bundleName = "";
    string url = "";
    string crc = "0";
    string fileSize = "0";
    public void setData(string lang, Dictionary<string, BundleInfo> localBundleInfo)
    {
        nowlang = lang;
        bundleInfo = localBundleInfo;
        updateBundleInfo = new BundleInfo();
        updateBundleInfo.data = new DownloadInfos();
        updateBundleInfo.data.list = new List<DownloadInfo>();
        info = new DownloadInfo();
        info.bundle_name = "";
        info.bundle_url = "";
        info.crc = 0;
        updateBundleInfo.data.list.Add(info);
    }

    void OnGUI()
    {
        drawTitle();
        scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Width(1300), GUILayout.Height(500));
        {

            foreach (var pair in bundleInfo)
            {
                GUILayout.BeginHorizontal();
                {
                    GUIStyle style;
                    if (null != bundleTitle && pair.Value.title.Equals(bundleTitle))
                    {
                        style = new GUIStyle(GUI.skin.textField);
                        style.normal.textColor = Color.green;

                        style.normal.background = new Texture2D(250, 20);
                        GUILayout.TextArea(pair.Value.title, style, GUILayout.Width(250));
                        style.normal.background = new Texture2D(100, 20);
                        GUILayout.TextArea(pair.Value.type, style, GUILayout.Width(200));
                        style.normal.background = new Texture2D(50, 20);
                        GUILayout.TextArea(pair.Value.bundle_ver, style, GUILayout.Width(50));
                        style.normal.background = new Texture2D(250, 20);
                        GUILayout.TextArea(pair.Value.data.list[0].bundle_name, style, GUILayout.Width(250));
                        GUILayout.TextArea(pair.Value.data.list[0].bundle_url, style, GUILayout.Width(300));
                        style.normal.background = new Texture2D(150, 20);
                        GUILayout.TextArea(pair.Value.data.list[0].crc.ToString(), style, GUILayout.Width(150));
                        style.normal.background = new Texture2D(150, 20);
                        GUILayout.TextArea(pair.Value.data.list[0].file_size.ToString(), style, GUILayout.Width(150));
                    }
                    else
                    {
                        GUILayout.TextField(pair.Value.title, GUILayout.Width(250));
                        GUILayout.TextField(pair.Value.type, GUILayout.Width(200));
                        GUILayout.TextField(pair.Value.bundle_ver, GUILayout.Width(50));
                        GUILayout.TextField(pair.Value.data.list[0].bundle_name, GUILayout.Width(250));
                        GUILayout.TextField(pair.Value.data.list[0].bundle_url, GUILayout.Width(300));
                        GUILayout.TextField(pair.Value.data.list[0].crc.ToString(), GUILayout.Width(150));
                        GUILayout.TextField(pair.Value.data.list[0].file_size.ToString(), GUILayout.Width(150));
                    }


                }
                GUILayout.EndHorizontal();
            }

        }
        GUILayout.EndScrollView();

        GUILayout.Label("新增/修改/刪除內容 (點選TITLE 可快速修改)");
        drawTitle();
        GUILayout.BeginHorizontal();
        {
            bundleTitle = GUILayout.TextField(bundleTitle, GUILayout.Width(250));
            type = GUILayout.TextField(type, GUILayout.Width(200));
            ver = GUILayout.TextField(ver, GUILayout.Width(50));
            bundleName = GUILayout.TextField(bundleName, GUILayout.Width(250));
            url = GUILayout.TextField(url, GUILayout.Width(300));
            crc = GUILayout.TextField(crc, GUILayout.Width(150));
            fileSize = GUILayout.TextField(fileSize, GUILayout.Width(150));
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        {
            if (GUILayout.Button("修改儲存", GUILayout.Width(100), GUILayout.Height(20)))
            {

                if (null == bundleTitle || bundleTitle.Equals(""))
                {
                    EditorUtility.DisplayDialog("", $"TITLE 不得為空字串!", "確認");
                }
                else
                {
                    newBundleInfo = new BundleInfo();
                    newBundleInfo.title = bundleTitle;
                    newBundleInfo.type = type;
                    newBundleInfo.bundle_ver = ver;
                    newBundleInfo.data = new DownloadInfos();
                    newBundleInfo.data.list = new List<DownloadInfo>();
                    info = new DownloadInfo();
                    info.bundle_name = bundleName;
                    info.bundle_url = url;
                    info.crc = UInt32.Parse(crc);
                    info.file_size = long.Parse(fileSize);
                    newBundleInfo.data.list.Add(info);

                    //無此key則新增
                    if (!bundleInfo.ContainsKey(bundleTitle))
                    {
                        bundleInfo.Add(bundleTitle, newBundleInfo);
                        EditorUtility.DisplayDialog("", $"新增 {bundleTitle} 至 {nowlang} 資源表", "確認");
                    }
                    else
                    {
                        bundleInfo[bundleTitle] = newBundleInfo;
                        EditorUtility.DisplayDialog("", $"異動 {bundleTitle} 至 {nowlang} 資源表", "確認");
                    }

                    PackTool.writeTable(nowlang, bundleInfo);
                }
            }

            if (GUILayout.Button("刪除", GUILayout.Width(100), GUILayout.Height(20)))
            {

                if ((null == bundleTitle || bundleTitle.Equals("")) || !bundleInfo.ContainsKey(bundleTitle))
                {
                    EditorUtility.DisplayDialog($"", $"查無此筆資料 {bundleTitle} ，無法刪除!", "確認");
                }
                else
                {
                    var reSelect = EditorUtility.DisplayDialog($"警告", $"即將刪除 {bundleTitle} 資源，是否刪除？", "是", "否");
                    if (reSelect)
                    {
                        bundleInfo.Remove(bundleTitle);

                        PackTool.removeTable(nowlang, bundleTitle);
                    }
                }
            }

            if (GUILayout.Button("清除", GUILayout.Width(100), GUILayout.Height(20)))
            {
                bundleTitle = "";
                type = "";
                ver = "";
                bundleName = "";
                url = "";
                crc = "";
            }
            GUILayout.Space(200);
            if (GUILayout.Button("產生ASSET PATH", GUILayout.Width(150), GUILayout.Height(20)))
            {

                //此類型為特殊規格
                if (type.Equals("ilruntime"))
                {
                    if (bundleTitle.Equals("CommonILRuntime") || bundleTitle.Equals("Lobby"))
                    {
                        bundleName = "ilruntime/common";
                    }
                    else
                    {
                        bundleName = "ilruntime/game";
                    }
                }
                else
                {
                    bundleName = $"{type}/{bundleTitle}";
                }

            }
            GUILayout.Space(100);
            if (GUILayout.Button("產生URL", GUILayout.Width(150), GUILayout.Height(20)))
            {

                //此類型為特殊規格
                if (bundleName.Equals("ilruntime/game"))
                {
                    //var paltform = ApplicationConfig.platformName.ToLower();
                    url = $"/{PackTool.platformName}/{bundleName}/{bundleTitle}/{ver}/";
                }
                else
                {
                    //var paltform = ApplicationConfig.platformName.ToLower();
                    url = $"/{PackTool.platformName}/{bundleName}/{ver}/";
                }

            }

            GUILayout.Space(150);
            if (GUILayout.Button("產生CRC", GUILayout.Width(150), GUILayout.Height(20)))
            {


            }

        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        {
            if (GUILayout.Button("更新全部檔案size", GUILayout.Width(150), GUILayout.Height(20)))
            {
                string file = "";
                BundleInfo tmpInfo = null;
                string platform = PackTool.platformName;
                foreach (var info in bundleInfo)
                {
                    tmpInfo = info.Value;
                    if (tmpInfo.title.Contains(platform)) continue;

                    if (tmpInfo.type.Equals("ilruntime"))
                    {
                        file = PackTool.getContentPath($"{tmpInfo.data.list[0].bundle_url}{tmpInfo.title}.dll");
                        info.Value.data.list[0].file_size = new FileInfo(file).Length;
                    }
                    else
                    {
                        if (tmpInfo.title.Contains("manifest"))
                        {
                            file = PackTool.getContentPath($"{tmpInfo.data.list[0].bundle_url}manifest");
                            info.Value.data.list[0].file_size = new FileInfo(file).Length;
                        }
                        else
                        {
                            file = PackTool.getContentPath($"{tmpInfo.data.list[0].bundle_url}{tmpInfo.title}");
                            info.Value.data.list[0].file_size = new FileInfo(file).Length;
                        }
                    }
                }

                PackTool.writeTable(nowlang, bundleInfo);
            }
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(10);
        GUILayout.Label($"資料筆數:{PackTool.localBundleInfo[nowlang].Values.Count}");
        GUILayout.Space(10);
        GUILayout.Label("資料 上移/下移");
        GUILayout.BeginHorizontal();
        {
            if (GUILayout.Button("上移", GUILayout.Width(50), GUILayout.Height(20)))
            {
                if (bundleInfo.ContainsKey(bundleTitle))
                {
                    mappings = bundleInfo.ToList();
                    int index = mappings.FindIndex(x => x.Key == bundleTitle);

                    if (index != 1)
                    {
                        mappings.Swap(index, index - 1);
                        bundleInfo = mappings.ToDictionary(key => key.Key, info => info.Value);
                        PackTool.sortTable(nowlang, mappings);
                    }
                }
            }

            if (GUILayout.Button("下移", GUILayout.Width(50), GUILayout.Height(20)))
            {
                if (bundleInfo.ContainsKey(bundleTitle))
                {
                    mappings = bundleInfo.ToList();
                    int index = mappings.FindIndex(x => x.Key == bundleTitle);

                    if (index != mappings.Count - 1)
                    {
                        mappings.Swap(index, index + 1);
                        bundleInfo = mappings.ToDictionary(key => key.Key, info => info.Value);
                        PackTool.sortTable(nowlang, mappings);
                    }
                }
            }


        }
        GUILayout.EndHorizontal();

        GUILayout.Space(30);
        if (GUILayout.Button("同步異動至其他語系資源表", GUILayout.Width(180), GUILayout.Height(40)))
        {
            var reSelect = EditorUtility.DisplayDialog($"警告", $"是否同步 {nowlang} 資源表異動至其他語系資源表？", "是", "否");
            if (reSelect)
            {
                PackTool.writeAsyncOtherTable(nowlang, bundleInfo);
            }
        }

        TextEditor edit = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.hotControl);
        focusText = edit.text;
        if (!focusText.Equals(focusTmp))
        {
            if (bundleInfo.ContainsKey(focusText))
            {
                updateBundleInfo = bundleInfo[focusText];
                focusTmp = focusText;
            }
        }

        if (null != updateBundleInfo)
        {
            bundleTitle = updateBundleInfo.title;
            type = updateBundleInfo.type;
            ver = updateBundleInfo.bundle_ver;
            bundleName = updateBundleInfo.data.list[0].bundle_name;
            url = updateBundleInfo.data.list[0].bundle_url;
            crc = updateBundleInfo.data.list[0].crc.ToString();
            fileSize = updateBundleInfo.data.list[0].file_size.ToString();
            updateBundleInfo = null;
        }
    }


    void drawTitle()
    {
        GUILayout.BeginHorizontal();
        {
            GUILayout.Label("TITLE", GUILayout.Width(250));
            GUILayout.Label("TYPE", GUILayout.Width(200));
            GUILayout.Label("VER", GUILayout.Width(50));
            GUILayout.Label("ASSET_PATH", GUILayout.Width(250));
            GUILayout.Label("URL", GUILayout.Width(300));
            GUILayout.Label("CRC", GUILayout.Width(150));
            GUILayout.Label("SIZE", GUILayout.Width(150));
        }
        GUILayout.EndHorizontal();
    }
}

public class PackTool : EditorWindow
{
    public const string platformName =
#if UNITY_ANDROID
        "android";
#elif UNITY_IOS
        "ios";
#endif

    public static string cachedPrefKey = "cachedBuildDri";
    static string[] langs = new string[] { "en", "zh" };
    static Dictionary<string, BundleInfo> bundleInfos = null;
    static List<UpdateBundleInfo> updateInfos = null;
    static List<UpdateBundleInfo> dllUpdateInfo = null;
    public static Dictionary<string, Dictionary<string, BundleInfo>> localBundleInfo;
    static Dictionary<string, List<UpdateBundleInfo>> localizationUpdateInfo;

    [MenuItem("打包/遊戲合集 bundle")]
    static void onPackGameCombine()
    {
        onPack("Assets/BundleGraph/GameBundleCombineGraph.asset");
    }

    [MenuItem("打包/大廳合集 bundle")]
    static void onPackLobbyCombine()
    {
        onPack("Assets/BundleGraph/LobbyBundleCombineGraph.asset");
    }


    static void onPack(string graphFullPath)
    {
        var pref = PlayerPrefs.GetString(cachedPrefKey, "");
        var dirFullPath = EditorUtility.OpenFolderPanel("協助指定大廳or遊戲美術資料夾", pref, "");
        if (string.IsNullOrEmpty(dirFullPath))  //點了取消，沒選資料夾
        {
            return;
        }
        PlayerPrefs.SetString(cachedPrefKey, dirFullPath);

        int option = EditorUtility.DisplayDialogComplex("再次確認", $"指定的資料夾: \n {dirFullPath}", "正確", "取消", "重新選擇");
        if (0 == option)    //"正確"
        {
            string dirName = "";
            int index = dirFullPath.LastIndexOfAny(new char[] { '/', '\\' });
            if (-1 != index)
            {
                dirName = dirFullPath.Substring(index + 1);
            }

            if (string.IsNullOrEmpty(dirName))
            {
                var reSelect = EditorUtility.DisplayDialog("警告", "指定資料夾有誤，是否重新指定", "重新指定", "否");
                if (reSelect)
                {
                    onPack(graphFullPath);
                }
                return;
            }

            string[] sourceDirs = new string[] {
                "ArtCommon",dirName
            };

            var packHelper = new PackHelper(sourceDirs, "./_stash");
            Debug.LogWarning("Ready To Pack");
            packHelper.packAll(graphFullPath);
        }
        else if (2 == option)
        {
            onPack(graphFullPath);
        }
    }

    public static async void editAndCheckBundleTable()
    {
        Debug.Log($"start create bundleTable {BundleSettingPanel.ver}");
        //將總表讀入
        readTable();
        await Task.Delay(TimeSpan.FromSeconds(2f));
        //將打包好的資源讀入, 取得異動資源
        readOutputBundle();
        readOuputDll();
        readLocalizationBundle();
        await Task.Delay(TimeSpan.FromSeconds(1f));
        //將異動資源copy至正式目錄
        copyFiles();
        await Task.Delay(TimeSpan.FromSeconds(1f));
        //將異動資源寫入總表
        writeUpdateToTable();
        await Task.Delay(TimeSpan.FromSeconds(1f));
        checkCommonTableBundleVer();
    }

    public static async void readBundleTable()
    {
        readTable();
    }

    static void readOutputBundle()
    {
        Debug.Log("readOutputBundle....");
        var dirs = Directory.GetDirectories(getOutputPath());
        string[] files = null;
        string file = "";
        string manifestPath = "";
        updateInfos = new List<UpdateBundleInfo>();
        bundleInfos = localBundleInfo[langs[0]];
        for (int i = 0; i < dirs.Length; i++)
        {
            if (dirs[i].Contains("scene")) continue;
            if (dirs[i].Contains("content")) continue;

            int count = 0;
            string type = "";
            files = Directory.GetFiles(dirs[i]);
            for (int j = 0; j < files.Length; j++)
            {
                file = files[j];
                long fileSize = new FileInfo(file).Length;
                if (!file.Contains("manifest"))
                {
                    var readFilePath = $"{file}.manifest";

                    foreach (string line in File.ReadLines(readFilePath))
                    {
                        Debug.Log(line);
                        if (line.Contains("CRC"))
                        {
                            var crc = line.Remove(0, 4);
                            var title = getAssetName(file);

                            if (!bundleInfos.ContainsKey(title))
                            {
                                Debug.Log($"not contains key {title}, then add new key");
                                BundleInfo info = new BundleInfo();
                                info.title = title;
                                info.data = new DownloadInfos();
                                info.data.list = new List<DownloadInfo>();
                                info.data.list.Add(new DownloadInfo());
                                info.data.list[0].bundle_name = "";
                                info.data.list[0].bundle_url = "";
                                info.data.list[0].crc = 0;
                                info.data.list[0].file_size = 0;
                                info.type = "lobby";//新key預設type為lobby，後續再從表改
                                info.bundle_ver = "0";//新key預設bundle ver為0
                                bundleInfos.Add(title, info);

                                UpdateBundleInfo ubi = new UpdateBundleInfo();
                                ubi.crc = 0;
                                ubi.fileSize = 0;
                                ubi.title = title;
                                ubi.localFilePath = "";
                                updateInfos.Add(ubi);
                                count++;
                                type = info.type;
                            }
                            else
                            {
                                if (bundleInfos.ContainsKey(title))
                                {
                                    var info = bundleInfos[title];
                                    var newCrc = UInt32.Parse(crc);
                                    if (info.data.list[0].crc != newCrc)
                                    {
                                        UpdateBundleInfo ubi = new UpdateBundleInfo();
                                        ubi.crc = newCrc;
                                        ubi.fileSize = fileSize;
                                        ubi.title = title;
                                        ubi.localFilePath = file;
                                        updateInfos.Add(ubi);
                                        count++;
                                        type = info.type;
                                    }
                                }
                                else
                                {
                                    Debug.Log($"not contains key {title}");
                                }
                            }
                            break;
                        }
                    }
                }
                else
                {
                    string fileName = getAssetName(file);
                    if (fileName.Equals("manifest"))
                    {
                        manifestPath = file;
                    }
                }
            }
            if (count > 0)
            {
                UpdateBundleInfo ubi = new UpdateBundleInfo();
                ubi.title = $"{type}_manifest";

                if (!type.Contains("common") &&
                    !type.Contains("lobby"))
                {
                    ubi.title = $"{type}_game_manifest";
                }

                if ((type.Equals("common") || type.Equals("lobby")) || (!type.Equals("lobby") && !type.Contains("lobby")))
                {
                    ubi.fileSize = new FileInfo(manifestPath).Length;
                    ubi.localFilePath = manifestPath;
                    updateInfos.Add(ubi);

                    Debug.Log($"add mainfest {ubi.title}");
                }
            }

        }

    }

    static void readOuputDll()
    {
        Debug.Log("readOutputDll....");
        string[] files = Directory.GetFiles(getOutputPath());
        string file = "";
        dllUpdateInfo = new List<UpdateBundleInfo>();
        bundleInfos = localBundleInfo[langs[0]];
        for (int i = 0; i < files.Length; i++)
        {
            file = files[i];
            long fileSize = new FileInfo(file).Length;
            var title = getAssetName(file);
            if (file.Contains(".dll"))
            {
                UpdateBundleInfo ubi = new UpdateBundleInfo();
                ubi.title = title.Replace(".dll", "");
                ubi.localFilePath = file;
                ubi.fileSize = fileSize;
                dllUpdateInfo.Add(ubi);
            }
        }
    }

    static void readLocalizationBundle()
    {
        Debug.Log("readLocalizationBundle....");

        if (!Directory.Exists(getLocalizationPath()))
        {
            Debug.Log($"floder {getLocalizationPath()} not found....");
            return;
        }

        var dirs = Directory.GetDirectories(getLocalizationPath());
        string[] files = null;
        string file = "";
        string manifestPath = "";
        localizationUpdateInfo = new Dictionary<string, List<UpdateBundleInfo>>();
        List<UpdateBundleInfo> updateTmp = null;
        //語系層
        for (int i = 0; i < dirs.Length; i++)
        {
            updateTmp = new List<UpdateBundleInfo>();
            var bundleDirs = Directory.GetDirectories(dirs[i]);
            //資源層
            for (int j = 0; j < bundleDirs.Length; j++)
            {
                int count = 0;
                string type = "";
                files = Directory.GetFiles(bundleDirs[j]);
                for (int k = 0; k < files.Length; k++)
                {
                    file = files[k];
                    long fileSize = new FileInfo(file).Length;
                    if (!file.Contains("manifest"))
                    {
                        var readFilePath = $"{file}.manifest";

                        foreach (string line in File.ReadLines(readFilePath))
                        {
                            Debug.Log(line);
                            if (line.Contains("CRC"))
                            {
                                var crc = line.Remove(0, 4);
                                var title = getAssetName(file);
                                if (bundleInfos.ContainsKey(title))
                                {
                                    var info = bundleInfos[title];
                                    var newCrc = UInt32.Parse(crc);
                                    Debug.Log($"CRC COMPARE_{info.data.list[0].crc}_{newCrc}");
                                    if (info.data.list[0].crc != newCrc)
                                    {
                                        UpdateBundleInfo ubi = new UpdateBundleInfo();
                                        ubi.crc = newCrc;
                                        ubi.fileSize = fileSize;
                                        ubi.title = title;
                                        ubi.localFilePath = file;
                                        updateTmp.Add(ubi);
                                        count++;
                                        type = info.type;
                                    }
                                }
                                else
                                {
                                    Debug.Log($"not contains key {title}");
                                }
                                break;
                            }
                        }
                    }
                    else
                    {
                        string fileName = getAssetName(file);
                        if (fileName.Equals("manifest"))
                        {
                            manifestPath = file;
                        }
                    }
                }
                if (count > 0)
                {
                    UpdateBundleInfo ubi = new UpdateBundleInfo();
                    ubi.title = $"{type}_localization_manifest";
                    ubi.localFilePath = manifestPath;
                    ubi.fileSize = new FileInfo(manifestPath).Length;
                    updateTmp.Add(ubi);

                    Debug.Log($"add localization mainfest {ubi.title}");
                }
            }

            localizationUpdateInfo.Add(getAssetName(dirs[i]), updateTmp);
        }
    }

    static void copyFiles()
    {
        UpdateBundleInfo updateInfo = null;
        BundleInfo localInfo = null;
        var bundleInfo = localBundleInfo[langs[0]];
        for (int i = 0; i < updateInfos.Count; i++)
        {
            updateInfo = updateInfos[i];
            Debug.Log($"writeUpdateTable {updateInfo.title}");
            //foreach (var bundleInfo in localBundleInfo)
            {

                if (!bundleInfo.ContainsKey(updateInfo.title))
                {
                    Debug.Log($"writeUpdateTable not found {updateInfo.title}");
                }
                else
                {
                    //從總表找到要更新的bundle
                    localInfo = bundleInfo[updateInfo.title];
                    int bundleVer = int.Parse(localInfo.bundle_ver);
                    int newBundleVer = bundleVer + 1;
                    localInfo.bundle_ver = newBundleVer.ToString();
                    if (0 != updateInfo.crc)
                        localInfo.data.list[0].crc = updateInfo.crc;

                    localInfo.data.list[0].file_size = updateInfo.fileSize;

                    localInfo.data.list[0].bundle_url = localInfo.data.list[0].bundle_url.Replace($"/{bundleVer}/", $"/{newBundleVer}/");
                    updateInfo.newUrl = localInfo.data.list[0].bundle_url;
                    updateInfo.newBundleVer = newBundleVer;
                    //將檔案依版號與url建立目錄
                    if (File.Exists(updateInfo.localFilePath))
                    {
                        Debug.Log($"file exist {updateInfo.localFilePath}");
                        if (localInfo.data.list[0].bundle_url.Equals("")) continue;
                        string url = getContentPath($"{localInfo.data.list[0].bundle_url}");
                        createFloder(url);
                        string urlTmp = getOutputPath($"content/{localInfo.data.list[0].bundle_url}");
                        createFloder(urlTmp);

                        string fileName = $"{localInfo.data.list[0].bundle_url}{getAssetName(updateInfo.localFilePath)}";
                        string destPath = getContentPath(fileName);
                        string destTmpPath = getOutputPath($"content/{fileName}");
                        Debug.Log($"updateInfo.localFilePath:{updateInfo.localFilePath}");
                        Debug.Log($"destPath:{destPath}");
                        File.Copy($"{updateInfo.localFilePath}", destPath, true);
                        //同時備份一份至.output
                        File.Copy($"{updateInfo.localFilePath}", destTmpPath, true);

                        if (!updateInfo.title.Contains("manifest"))
                        {
                            File.Copy($"{updateInfo.localFilePath}.manifest", $"{destPath}.manifest", true);
                            //同時備份一份至.output
                            File.Copy($"{updateInfo.localFilePath}.manifest", $"{destTmpPath}.manifest", true);
                        }
                    }
                }
            }
        }

        //DLL更新資料
        for (int i = 0; i < dllUpdateInfo.Count; i++)
        {
            updateInfo = dllUpdateInfo[i];
            localInfo = bundleInfo[updateInfo.title];
            if (!bundleInfo.ContainsKey(updateInfo.title))
            {
                Debug.Log($"dllUpdateTable not found {updateInfo.title}");
            }
            else
            {
                int bundleVer = int.Parse(localInfo.bundle_ver);
                int newBundleVer = bundleVer + 1;
                localInfo.bundle_ver = newBundleVer.ToString();
                localInfo.data.list[0].file_size = updateInfo.fileSize;
                localInfo.data.list[0].bundle_url = localInfo.data.list[0].bundle_url.Replace($"/{bundleVer}/", $"/{newBundleVer}/");
                updateInfo.newUrl = localInfo.data.list[0].bundle_url;
                updateInfo.newBundleVer = newBundleVer;
                //將檔案依版號與url建立目錄
                if (File.Exists(updateInfo.localFilePath))
                {
                    Debug.Log($"file exist {updateInfo.localFilePath}");
                    string url = getContentPath($"{localInfo.data.list[0].bundle_url}");
                    createFloder(url);
                    string urlTmp = getOutputPath($"content/{localInfo.data.list[0].bundle_url}");
                    createFloder(urlTmp);

                    string fileName = $"{localInfo.data.list[0].bundle_url}{getAssetName(updateInfo.localFilePath)}";
                    string destPath = getContentPath(fileName);
                    string destTmpPath = getOutputPath($"content/{fileName}");
                    Debug.Log($"updateInfo.localFilePath:{updateInfo.localFilePath}");
                    Debug.Log($"destPath:{destPath}");
                    File.Copy($"{updateInfo.localFilePath}", destPath, true);
                    //同時備份一份至.output
                    File.Copy($"{updateInfo.localFilePath}", destTmpPath, true);

                    File.Copy($"{updateInfo.localFilePath.Replace(".dll", ".pdb")}", destPath.Replace(".dll", ".pdb"), true);
                    //同時備份一份至.output
                    File.Copy($"{updateInfo.localFilePath.Replace(".dll", ".pdb")}", destTmpPath.Replace(".dll", ".pdb"), true);
                }
            }
        }

        List<UpdateBundleInfo> updateTmp = null;
        Dictionary<string, BundleInfo> infosTmp = null;
        foreach (KeyValuePair<string, List<UpdateBundleInfo>> pair in localizationUpdateInfo)
        {

            updateTmp = pair.Value;
            infosTmp = localBundleInfo[pair.Key];
            for (int i = 0; i < updateTmp.Count; i++)
            {
                updateInfo = updateTmp[i];

                //從總表找到要更新的bundle
                localInfo = infosTmp[updateInfo.title];
                if (null == localInfo)
                {
                    Debug.Log($"writeUpdateTable not found {updateInfo.title}");
                }
                else
                {
                    int bundleVer = int.Parse(localInfo.bundle_ver);
                    int newBundleVer = bundleVer + 1;
                    localInfo.bundle_ver = newBundleVer.ToString();
                    if (0 != updateInfo.crc)
                        localInfo.data.list[0].crc = updateInfo.crc;

                    localInfo.data.list[0].file_size = updateInfo.fileSize;

                    localInfo.data.list[0].bundle_url = localInfo.data.list[0].bundle_url.Replace($"/{bundleVer}/", $"/{newBundleVer}/");
                    updateInfo.newUrl = localInfo.data.list[0].bundle_url;
                    updateInfo.newBundleVer = newBundleVer;
                    //將檔案依版號與url建立目錄
                    if (File.Exists(updateInfo.localFilePath))
                    {
                        Debug.Log($"file exist {updateInfo.localFilePath}");
                        string url = getContentPath($"{localInfo.data.list[0].bundle_url}");
                        createFloder(url);
                        string urlTmp = getOutputPath($"content/{localInfo.data.list[0].bundle_url}");
                        createFloder(urlTmp);

                        string fileName = $"{localInfo.data.list[0].bundle_url}{getAssetName(updateInfo.localFilePath)}";
                        string destPath = getContentPath(fileName);
                        string destTmpPath = getOutputPath($"content/{fileName}");
                        Debug.Log($"updateInfo.localFilePath:{updateInfo.localFilePath}");
                        Debug.Log($"destPath:{destPath}");
                        File.Copy($"{updateInfo.localFilePath}", destPath, true);
                        //同時備份一份至.output
                        File.Copy($"{updateInfo.localFilePath}", destTmpPath, true);
                        if (!updateInfo.title.Contains("manifest"))
                        {
                            File.Copy($"{updateInfo.localFilePath}.manifest", $"{destPath}.manifest", true);
                            //同時備份一份至.output
                            File.Copy($"{updateInfo.localFilePath}.manifest", $"{destTmpPath}.manifest", true);
                        }
                    }
                }
            }
        }
    }

    static void createFloder(string url)
    {

        if (Directory.Exists(url))
        {
            string[] files = Directory.GetFiles(url);
            if (0 == files.Length)
                Directory.Delete(url);
        }

        if (!Directory.Exists(url))
        {
            Directory.CreateDirectory(url);
        }
    }

    static void writeUpdateToTable()
    {
        string fileName = "";
        string table = "";
        string platform = PackTool.platformName;// ApplicationConfig.platformName.ToLower();
        UpdateBundleInfo updateInfo = null;
        List<UpdateBundleInfo> localizationInfo = null;
        for (int i = 0; i < langs.Length; i++)
        {
            fileName = $"bundleInfo_{platform}_{langs[i]}.sc";
            table = File.ReadAllText(getTablePath(fileName));
            JObject jo = JObject.Parse(table);

            jo[platform]["0.1.0"]["bundle_ver"] = BundleSettingPanel.ver;

            for (int j = 0; j < updateInfos.Count; j++)
            {
                updateInfo = updateInfos[j];
                if (jo.ContainsKey(updateInfo.title))
                {
                    jo[updateInfo.title]["0.1.0"]["bundle_ver"] = updateInfo.newBundleVer;
                    jo[updateInfo.title]["0.1.0"]["data"]["list"][0]["bundle_url"] = updateInfo.newUrl;
                    jo[updateInfo.title]["0.1.0"]["data"]["list"][0]["crc"] = updateInfo.crc;
                    jo[updateInfo.title]["0.1.0"]["data"]["list"][0]["file_size"] = updateInfo.fileSize;
                }
                else
                {

                    //JObject jo_one = new JObject();
                    JObject jo_two = new JObject();
                    JObject jo_three = new JObject();
                    JObject jo_four = new JObject();
                    JObject jo_five = new JObject();
                    JArray jarry = new JArray();

                    jo_five["bundle_name"] = "";
                    jo_five["bundle_url"] = "";
                    jo_five["crc"] = updateInfo.crc;
                    jo_five["file_size"] = updateInfo.fileSize;

                    jarry.Add(jo_five);

                    jo_four["list"] = jarry;

                    jo_three["type"] = "lobby";
                    jo_three["must_update"] = true;
                    jo_three["bundle_ver"] = updateInfo.newBundleVer;
                    jo_three["data"] = jo_four;

                    jo_two["0.1.0"] = jo_three;
                    //jo_one[updateInfo.title] = jo_two;

                    jo[updateInfo.title] = jo_two;
                }
            }

            for (int j = 0; j < dllUpdateInfo.Count; j++)
            {
                updateInfo = dllUpdateInfo[j];
                jo[updateInfo.title]["0.1.0"]["bundle_ver"] = updateInfo.newBundleVer;
                jo[updateInfo.title]["0.1.0"]["data"]["list"][0]["bundle_url"] = updateInfo.newUrl;
                //jo[updateInfo.title]["0.1.0"]["data"]["list"][0]["crc"] = updateInfo.crc;
                jo[updateInfo.title]["0.1.0"]["data"]["list"][0]["file_size"] = updateInfo.fileSize;
            }

            localizationInfo = localizationUpdateInfo[langs[i]];

            for (int j = 0; j < localizationInfo.Count; j++)
            {
                updateInfo = localizationInfo[j];
                jo[updateInfo.title]["0.1.0"]["bundle_ver"] = updateInfo.newBundleVer;
                jo[updateInfo.title]["0.1.0"]["data"]["list"][0]["bundle_url"] = updateInfo.newUrl;
                jo[updateInfo.title]["0.1.0"]["data"]["list"][0]["crc"] = updateInfo.crc;
                jo[updateInfo.title]["0.1.0"]["data"]["list"][0]["file_size"] = updateInfo.fileSize;
            }

            File.WriteAllText(getTablePath(fileName), jo.ToString());
            //同時備份一份至.output
            createFloder(getOutputTablePath(""));
            File.WriteAllText(getOutputTablePath(fileName), jo.ToString());
            Debug.Log($"write {getTablePath(fileName)} is success!!");
        }
        Debug.Log($"bundleTable {BundleSettingPanel.ver} update completed!!");
        Debug.Log($"All action is done!!");
    }

    public static void checkCommonTableBundleVer()
    {
        readTable();

        string firstLangName = langs[0];
        //以第一張表為基礎去比對各語系表的版號
        Dictionary<string, BundleInfo> firstTable = localBundleInfo[firstLangName];
        Dictionary<string, BundleInfo> compareTable;
        foreach (var pair in firstTable)
        {
            if (pair.Key.Contains("localization")) continue;

            foreach (var langKey in localBundleInfo.Keys)
            {
                if (langKey.Equals(firstLangName)) continue;
                compareTable = localBundleInfo[langKey];

                if (!compareTable.ContainsKey(pair.Key))
                {
                    Debug.LogError($"checkTable: not found key in {langKey} table");
                    continue;
                }

                if (!pair.Value.bundle_ver.Equals(compareTable[pair.Key].bundle_ver))
                {
                    Debug.LogError($"checkTable: {pair.Key} mismatch [{firstLangName} bundleVer:{pair.Value.bundle_ver} crc:{pair.Value.data.list[0].crc} ] and [{langKey} bundleVer:{compareTable[pair.Key].bundle_ver} crc:{compareTable[pair.Key].data.list[0].crc}]");
                }
            }
        }
        Debug.Log("checkTableBundleVer is done!!");
    }

    public static void writeTable(string lang, Dictionary<string, BundleInfo> bundleInfos)
    {
        string fileName = "";
        string table = "";
        string platform = PackTool.platformName;//ApplicationConfig.platformName.ToLower();
        BundleInfo updateInfo = null;
        List<UpdateBundleInfo> localizationInfo = null;

        localBundleInfo[lang] = bundleInfos;

        fileName = $"bundleInfo_{platform}_{lang}.sc";
        table = File.ReadAllText(getTablePath(fileName));
        JObject jo = JObject.Parse(table);
        foreach (var pair in bundleInfos)
        {
            updateInfo = pair.Value;

            if (jo.ContainsKey(updateInfo.title))
            {
                jo[updateInfo.title]["0.1.0"]["bundle_ver"] = updateInfo.bundle_ver;
                jo[updateInfo.title]["0.1.0"]["type"] = updateInfo.type;
                jo[updateInfo.title]["0.1.0"]["must_update"] = true;
                jo[updateInfo.title]["0.1.0"]["data"]["list"][0]["bundle_name"] = updateInfo.data.list[0].bundle_name;
                jo[updateInfo.title]["0.1.0"]["data"]["list"][0]["bundle_url"] = updateInfo.data.list[0].bundle_url;
                jo[updateInfo.title]["0.1.0"]["data"]["list"][0]["crc"] = updateInfo.data.list[0].crc;
                jo[updateInfo.title]["0.1.0"]["data"]["list"][0]["file_size"] = updateInfo.data.list[0].file_size;
            }
            else
            {

                //JObject jo_one = new JObject();
                JObject jo_two = new JObject();
                JObject jo_three = new JObject();
                JObject jo_four = new JObject();
                JObject jo_five = new JObject();
                JArray jarry = new JArray();

                jo_five["bundle_name"] = updateInfo.data.list[0].bundle_name;
                jo_five["bundle_url"] = updateInfo.data.list[0].bundle_url;
                jo_five["crc"] = updateInfo.data.list[0].crc;
                jo_five["file_size"] = updateInfo.data.list[0].file_size;

                jarry.Add(jo_five);

                jo_four["list"] = jarry;

                jo_three["type"] = updateInfo.type;
                jo_three["must_update"] = true;
                jo_three["bundle_ver"] = updateInfo.bundle_ver;
                jo_three["data"] = jo_four;

                jo_two["0.1.0"] = jo_three;
                //jo_one[updateInfo.title] = jo_two;

                jo[updateInfo.title] = jo_two;
            }
        }
        string output = jo.ToString();
        File.WriteAllText(getTablePath(fileName), output);
        //同時備份一份至.output
        createFloder(getOutputTablePath(""));
        File.WriteAllText(getOutputTablePath(fileName), output);

        Debug.Log($"write {getTablePath(fileName)} is success!!");
        Debug.Log($"bundleTable {BundleSettingPanel.ver} update completed!!");
        Debug.Log($"write table is done!!");

    }

    public static void removeTable(string lang, string title)
    {
        string fileName = "";
        string table = "";
        //string platform = ApplicationConfig.platformName.ToLower();
        fileName = $"bundleInfo_{platformName}_{lang}.sc";
        table = File.ReadAllText(getTablePath(fileName));
        JObject jo = JObject.Parse(table);

        if (jo.ContainsKey(title))
        {
            localBundleInfo[lang].Remove(title);
            jo.Remove(title);
            File.WriteAllText(getTablePath(fileName), jo.ToString());
            //同時備份一份至.output
            createFloder(getOutputTablePath(""));
            File.WriteAllText(getOutputTablePath(fileName), jo.ToString());
            Debug.Log($"remove {getTablePath(fileName)} is success!!");
            Debug.Log($"bundleTable {BundleSettingPanel.ver} update completed!!");
            Debug.Log($"remove table is done!!");
        }
        else
        {
            Debug.Log($"remove table not found key {title}");
        }
    }

    public static void sortTable(string lang, List<KeyValuePair<string, BundleInfo>> mappings)
    {
        localBundleInfo[lang] = mappings.ToDictionary(key => key.Key, info => info.Value);
        string fileName = "";
        string table = "";
        //string platform = ApplicationConfig.platformName.ToLower();
        fileName = $"bundleInfo_{platformName}_{lang}.sc";
        BundleInfo updateInfo = null;
        JObject jo_one = new JObject();
        bundleInfos = new Dictionary<string, BundleInfo>();
        for (int i = 0; i < mappings.Count; i++)
        {
            updateInfo = mappings[i].Value;
            bundleInfos.Add(updateInfo.title, updateInfo);
            JObject jo_two = new JObject();
            JObject jo_three = new JObject();
            JObject jo_four = new JObject();
            JObject jo_five = new JObject();
            JArray jarry = new JArray();

            jo_five["bundle_name"] = updateInfo.data.list[0].bundle_name;
            jo_five["bundle_url"] = updateInfo.data.list[0].bundle_url;
            jo_five["crc"] = updateInfo.data.list[0].crc;
            jo_five["file_size"] = updateInfo.data.list[0].file_size;
            jarry.Add(jo_five);

            jo_four["list"] = jarry;

            jo_three["type"] = updateInfo.type;
            jo_three["must_update"] = true;
            jo_three["bundle_ver"] = updateInfo.bundle_ver;
            jo_three["data"] = jo_four;

            jo_two["0.1.0"] = jo_three;

            jo_one[updateInfo.title] = jo_two;
        }

        File.WriteAllText(getTablePath(fileName), jo_one.ToString());
        //同時備份一份至.output
        createFloder(getOutputTablePath(""));
        File.WriteAllText(getOutputTablePath(fileName), jo_one.ToString());
        Debug.Log($"sorting {getTablePath(fileName)} is success!!");
        Debug.Log($"bundleTable {BundleSettingPanel.ver} update completed!!");
        Debug.Log($"sorting table is done!!");
    }

    public static async void writeAsyncOtherTable(string lang, Dictionary<string, BundleInfo> bundleInfos)
    {
        string title = "";
        //遍歷各語系表
        foreach (var pair in localBundleInfo)
        {
            //略過目前語系資源
            if (pair.Key.Equals(lang)) continue;

            Dictionary<string, BundleInfo> newInfos = new Dictionary<string, BundleInfo>(bundleInfos);
            Dictionary<string, BundleInfo> localInfos = pair.Value;

            //同步localization
            List<string> keyList = new List<string>(newInfos.Keys);
            string key = "";
            for (int i = 0; i < keyList.Count; i++)
            {
                key = keyList[i];
                if (key.Contains("localization"))
                {
                    if (localInfos.ContainsKey(key))
                        newInfos[key] = localInfos[key];
                }
            }

            localInfos = newInfos;

            //寫入本地資源表
            string fileName = "";
            string table = "";
            //string platform = ApplicationConfig.platformName.ToLower();
            fileName = $"bundleInfo_{platformName}_{pair.Key}.sc";
            BundleInfo updateInfo = null;
            JObject jo_one = new JObject();
            List<KeyValuePair<string, BundleInfo>> mappings = localInfos.ToList();
            for (int i = 0; i < mappings.Count; i++)
            {
                updateInfo = mappings[i].Value;
                JObject jo_two = new JObject();
                JObject jo_three = new JObject();
                JObject jo_four = new JObject();
                JObject jo_five = new JObject();
                JArray jarry = new JArray();

                jo_five["bundle_name"] = updateInfo.data.list[0].bundle_name;
                jo_five["bundle_url"] = updateInfo.data.list[0].bundle_url;
                jo_five["crc"] = updateInfo.data.list[0].crc;
                jo_five["file_size"] = updateInfo.data.list[0].file_size;
                jarry.Add(jo_five);

                jo_four["list"] = jarry;

                jo_three["type"] = updateInfo.type;
                jo_three["must_update"] = true;
                jo_three["bundle_ver"] = updateInfo.bundle_ver;
                jo_three["data"] = jo_four;

                jo_two["0.1.0"] = jo_three;

                jo_one[updateInfo.title] = jo_two;
            }

            File.WriteAllText(getTablePath(fileName), jo_one.ToString());
            //同時備份一份至.output
            createFloder(getOutputTablePath(""));
            File.WriteAllText(getOutputTablePath(fileName), jo_one.ToString());
            Debug.Log($"async other table {getTablePath(fileName)} is success!!");
            Debug.Log($"bundleTable {BundleSettingPanel.ver} update completed!!");
            Debug.Log($"async other table table is done!!");
        }
        //重新載表
        readBundleTable();
    }

    static string getAssetName(string fullPath)
    {
        var path = fullPath.Replace('\\', '/');  //不同作業系統資料夾符號規格統一
        return fullPath.Substring(path.LastIndexOf("/") + 1);
    }


    static async void readTable()
    {
        if (Directory.Exists(getTablePath()))
        {
            string[] files = Directory.GetFiles(getTablePath());
            if (0 == files.Length)
                Directory.Delete(getTablePath());
        }

        if (!Directory.Exists(getTablePath()))
        {
            if (Directory.Exists(getSourceTablePath()))
            {
                Debug.Log($"CreateDirectory {getTablePath()}");
                Directory.CreateDirectory(getTablePath());
                string[] files = Directory.GetFiles(getSourceTablePath());
                for (int i = 0; i < files.Length; i++)
                {
                    string assetName = getAssetName(files[i]);
                    await FileAsync.Copy(files[i], getTablePath(assetName));
                    //await Task.Delay(TimeSpan.FromSeconds(1f));
                }
            }
            else
            {
                Debug.LogError($"create new bundle ver floder failed. plz check /{platformName}/bundle_table/{BundleSettingPanel.source_ver}");
            }
        }

        string fileName = "";
        string table = "";
        //string platform = ApplicationConfig.platformName.ToLower();
        string tmpJson = "";
        string tmpJsonSecond = "";
        localBundleInfo = new Dictionary<string, Dictionary<string, BundleInfo>>();
        for (int i = 0; i < langs.Length; i++)
        {
            fileName = $"bundleInfo_{platformName}_{langs[i]}.sc";
            table = File.ReadAllText(getTablePath(fileName));
            Debug.Log($"table:{table}");
            JObject jo = JObject.Parse(table);
            var bundleInfosTmp = new Dictionary<string, BundleInfo>();
            foreach (var x in jo)
            {
                tmpJson = x.Value.ToString();
                JObject secondjo = JObject.Parse(tmpJson);
                string appVer = "";
                foreach (var y in secondjo)
                {
                    //Util.Log($"{y.Key} : {y.Value}");
                    appVer = y.Key;
                    tmpJsonSecond = y.Value.ToString();
                    break;
                }
                //Util.Log($"setData:{x.Key}:{thirdJson}");
                BundleInfo bundleInfo = JsonUtility.FromJson<BundleInfo>(tmpJsonSecond);
                bundleInfo.title = x.Key;
                bundleInfo.app_ver = appVer;
                bundleInfosTmp.Add(bundleInfo.title, bundleInfo);
            }

            localBundleInfo.Add(langs[i], bundleInfosTmp);
        }



    }

    public static string getLocalizationPath(string fileName = "")
    {
        return $"./_localization/{fileName}";
    }

    public static string getOutputPath(string fileName = "")
    {
        return $"./_outputbundles/{fileName}";
    }

    public static string getContentPath(string fileName = "")
    {
        return $"./assetbundles/content/{fileName}";
    }

    public static string getOutputTablePath(string fileName = "")
    {
        //string platform = ApplicationConfig.platformName.ToLower();
        return getOutputPath($"content/{platformName}/bundle_table/{BundleSettingPanel.ver}/{fileName}");
    }

    public static string getTablePath(string fileName = "")
    {
        //platformNamestring platform = ApplicationConfig.platformName.ToLower();
        return $"./assetbundles/content/{platformName}/bundle_table/{BundleSettingPanel.ver}/{fileName}";
    }

    public static string getSourceTablePath(string fileName = "")
    {
        //platformNamestring platform = ApplicationConfig.platformName.ToLower();
        return $"./assetbundles/content/{platformName}/bundle_table/{BundleSettingPanel.source_ver}/{fileName}";
    }
}
class PackHelper
{
    string sourceRoot;
    string stashRoot;
    string defaultType;

    string[] artDirs;
    string[] packList = new string[] { "en", "zh" };

    public PackHelper(string[] artDirs, string stashRoot)
    {
        this.artDirs = artDirs;
        this.stashRoot = stashRoot;
    }

    public void packAll(string graphFullGraph = "Assets/BuildBundle/Graph/LocalizationBundle.asset")
    {
        defaultType = "en";
        if (Directory.Exists("./_stash"))
        {
            Directory.Delete("./_stash", true);
        }

        stash(defaultType);
        for (int i = 0; i < packList.Length; i++)
        {
            var type = packList[i];
            if (type != defaultType)
            {
                switchLocalization(type);
            }
            pack(graphFullGraph);
            splitLocalization(type);
        }
        revert(defaultType);
    }

    string getSourceDir(string artDir, string type)
    {
        return $"./Assets/AssetBundles/{artDir}/localization/{type}";
    }

    string getStashDir(string artDir, string type)
    {
        return $"{stashRoot}/{artDir}/{type}";
    }

    void stash(string type)
    {
        foreach (var artDir in artDirs)
        {
            var sourceDir = getSourceDir(artDir, type);
            if (!Directory.Exists(sourceDir))   //此大廳/遊戲暫時沒有語言包資料夾
            {
                continue;
            }
            var files = Directory.GetFiles($"{getSourceDir(artDir, type)}", $"*.*", SearchOption.AllDirectories);

            var stashDir = getStashDir(artDir, type);
            if (!Directory.Exists(stashDir))
            {
                Directory.CreateDirectory(stashDir);
            }

            foreach (var fileFullPath in files)
            {
                copyFile(fileFullPath, stashDir, false);
            }
        }

        Debug.Log($"stash {type} finish");
    }

    void revert(string type)
    {
        AssetDatabase.StartAssetEditing();
        foreach (var artDir in artDirs)
        {
            var stashDir = getStashDir(artDir, type);
            var sourceDir = getSourceDir(artDir, type);
            if (!Directory.Exists(sourceDir))   //此大廳/遊戲暫時沒有語言包資料夾
            {
                continue;
            }

            var files = Directory.GetFiles($"{stashDir}", $"*.*", SearchOption.AllDirectories);
            foreach (var fileFullPath in files)
            {
                copyFile(fileFullPath, sourceDir, true);
            }
        }
        AssetDatabase.StopAssetEditing();
        AssetDatabase.Refresh();
        //Directory.Delete("./_stash", true);
        Debug.Log("revert finish");

        if (BundleSettingPanel.isBundleSet)
        {
            PackTool.editAndCheckBundleTable();
        }
    }

    void copyFile(string sourceFullPath, string targetDir, bool overwrite)
    {
        var fileName = sourceFullPath;
        int index = sourceFullPath.LastIndexOfAny(new char[] { '/', '\\' });
        if (-1 != index)
        {
            fileName = sourceFullPath.Substring(index + 1);
        }
        var newFilePath = $"{targetDir}/{fileName}";
        //Debug.Log($"moveFile: {sourceFullPath} -> {newFilePath}");
        File.Copy(sourceFullPath, newFilePath, overwrite);
    }

    void moveFileToDir(string sourceFullPath, string targetDir, bool overwrite)
    {
        var fileName = getFileName(sourceFullPath);
        var newFilePath = $"{targetDir}/{fileName}";
        //Debug.Log($"moveFile: {sourceFullPath} -> {newFilePath}");

        if (overwrite)
        {
            if (File.Exists(newFilePath))
            {
                File.Delete(newFilePath);
            }
        }
        File.Move(sourceFullPath, newFilePath);
    }

    void switchLocalization(string type)
    {
        AssetDatabase.StartAssetEditing();
        //搬移本地化語言美術文字圖檔
        foreach (var artDir in artDirs)
        {
            var sourceDir = getSourceDir(artDir, type);
            if (!Directory.Exists(sourceDir))   //此大廳/遊戲暫時沒有語言包資料夾
            {
                continue;
            }
            var files = Directory.GetFiles($"{sourceDir}", $"*.png", SearchOption.AllDirectories);
            var targetDir = getSourceDir(artDir, defaultType);
            foreach (var fileFullPath in files)
            {
                Debug.Log($"moveFile: {fileFullPath}, targetDir: {targetDir}");
                copyFile(fileFullPath, targetDir, true);
            }
        }
        AssetDatabase.StopAssetEditing();
        AssetDatabase.Refresh();
    }

    void pack(string graphFullPath)
    {
        Debug.Log($"start build bundle");
        //開啟打包AssetGraph
        AssetGraphUtility.ExecuteGraph(graphFullPath);
    }

    void splitLocalization(string type)
    {
        var outputDir = "./_outputbundles";
        var files = Directory.GetFiles($"{outputDir}", $"*_*.*", SearchOption.AllDirectories);
        var localizationDir = $"./_localization/{type}";
        createDirectoryIfNotExist(localizationDir);

        Dictionary<string, string> providerNames = new Dictionary<string, string>();

        foreach (var file in files)
        {
            var fileName = getFileName(file);
            var prefix = BundleHelper.getPrefix(fileName, '_');

            if (string.IsNullOrEmpty(prefix))
            {
                Debug.LogWarning($"fileName: {fileName} hasNo prefix");
                continue;
            }

            if (!providerNames.ContainsKey(prefix))
            {
                providerNames.Add(prefix, prefix);
            }

            //To Localization Dir
            if (fileName.Contains("_localization_texture"))
            {
                var targetDir = $"{localizationDir}/{prefix}";
                createDirectoryIfNotExist(targetDir);
                moveFileToDir(file, targetDir, true);
            }
            else
            {
                var targetDir = $"{outputDir}/{prefix}";
                createDirectoryIfNotExist(targetDir);
                moveFileToDir(file, targetDir, true);
            }
        }

        //Copy Manifest
        var sourceManifestPath = $"{outputDir}/manifest";
        foreach (var value in providerNames.Values)
        {
            copyFile(sourceManifestPath, $"{outputDir}/{value}", true);
            var dir = $"{localizationDir}/{value}";
            if (Directory.Exists(dir))  //大廳/遊戲有語言包才複製manifest
            {
                copyFile(sourceManifestPath, $"{localizationDir}/{value}", true);
            }
        }
        File.Delete(sourceManifestPath);
        File.Delete($"{sourceManifestPath}.manifest");
    }

    //File Tool
    void createDirectoryIfNotExist(string dirPath)
    {
        //if (Directory.Exists(dirPath))
        //{
        //    Directory.Delete(dirPath, true);
        //}
        Directory.CreateDirectory(dirPath);
    }

    string getFileName(string fileFullPath)
    {
        var fileName = fileFullPath;
        int index = fileFullPath.LastIndexOfAny(new char[] { '/', '\\' });
        if (-1 != index)
        {
            fileName = fileFullPath.Substring(index + 1);
        }

        if (!IsMatchRegularExpression(fileName))
        {
            Debug.LogError($"FileName NotMatchRule :{fileName}");
        }
        return fileName;
    }

    bool IsMatchRegularExpression(string fileName)
    {
        bool isMatch = Regex.IsMatch(fileName, @"^[A-Za-z0-9_.@-]+$");

        return isMatch;
    }
}
