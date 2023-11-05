using UnityEditor;
using ILRuntime.Runtime.CLRBinding;
using AppDomain = ILRuntime.Runtime.Enviorment.AppDomain;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using LitJson;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class CLRBindingWindow : EditorWindow
{
    static string SaveGameNameKey { get { return "BindingGameName"; } }
    static string serverUrl
    {
        get
        {
            return $"http://{serverHost}";
        }
    }

    static string BindingGameName;
    static Dictionary<string, string> ilruntimeInfo = new Dictionary<string, string>();
    static List<GameData> gameDatas;

    [MenuItem("ILRuntime/Open CLRBinding Window")]
    public static async void showCLRBindingWindow()
    {
        GetWindow(typeof(CLRBindingWindow), true, "Generate CLR Binding Window");
        await loadGameList();
        if (PlayerPrefs.HasKey(SaveGameNameKey))
        {
            BindingGameName = PlayerPrefs.GetString(SaveGameNameKey);
        }
        if (string.IsNullOrEmpty(BindingGameName) || !BindingGameName.Equals("Lobby"))
        {
            getGameName();
        }
    }

    static void getGameName()
    {
        string[] filesName = Directory.GetFiles(Path.Combine(ApplicationConfig.getStreamingPath, "ILRuntime"), "*.dll");
        for (int i = 0; i < filesName.Length; ++i)
        {
            string fileName = Path.GetFileName(filesName[i]);
            GameData gameData = gameDatas.Find(data => fileName.StartsWith(data.name));

            if (null != gameData)
            {
                BindingGameName = gameData.name;
                break;
            }
        }
        if (string.IsNullOrEmpty(BindingGameName))
        {
            Debug.LogError($"Get Binding Game Name is Empty, Check {ApplicationConfig.getStreamingPath}/ILRuntime.");
        }
    }

    private void OnGUI()
    {
        GUILayout.Label("Generate CLRBinding Setting");
        EditorGUILayout.Space(10);
        BindingGameName = EditorGUILayout.TextField("Input Binding GameName : ", BindingGameName);

        EditorGUILayout.Space(20);

        if (GUILayout.Button("Generate CLRBinding"))
        {
            if (string.IsNullOrEmpty(BindingGameName))
            {
                return;
            }
            if (BindingGameName.Equals("Lobby"))
            {
                PlayerPrefs.SetString(SaveGameNameKey, BindingGameName);
            }

            GenerateCLRBindingByAnalysis();
        }

        EditorGUILayout.Space(20);
        if (GUILayout.Button("Generate All Game CLRBinding"))
        {
            GenerateAllGameCLRBindingByAnalysis();
        }
    }

    static string serverHost
    {
        get
        {
            //if (ApplicationConfig.environment == ApplicationConfig.Environment.Dev)
            //{
            //    return "34.80.106.119:8080";
            //}

            return "192.168.128.235:8080";
        }
    }

    static async Task<List<GameData>> loadGameList()
    {
        string jsonFile = await WebRequestText.instance.loadTextFromServer("game_list", serverUrl);
        if (string.IsNullOrEmpty(jsonFile))
        {
            Debug.LogError("GenerateAllGameCLRBindingByAnalysis Get game_list is empty , check again");
            return new List<GameData>();
        }
        gameDatas = JsonMapper.ToObject<CLRBindingData>(jsonFile).Game.ToList();

        return gameDatas;
    }
    public static async void GenerateAllGameCLRBindingByAnalysis()
    {
        string generatedPath = "Assets/ILRuntime/Generated";
        if (Directory.Exists(generatedPath))
        {
            Directory.Delete(generatedPath, true);
            AssetDatabase.Refresh();
        }
        string clrBindingsPath = "CLRBindings";
        if (!Directory.Exists(clrBindingsPath))
        {
            Directory.CreateDirectory(clrBindingsPath);
        }

        List<GameData> gameDatas = await loadGameList();

        string platform = "android";

#if UNITY_IOS
                platform = "ios";
#endif

        await loadTable(platform);

        gameDatas.Insert(0, new GameData() { name = "CommonILRuntime", onLine = true });
        gameDatas.Insert(1, new GameData() { name = "Lobby", onLine = true });

        AppDomain domain = new AppDomain();
        FileStream fs = null;

        for (int i = 0; i < gameDatas.Count; ++i)
        {
            GameData data = gameDatas[i];
            if (!data.onLine)
            {
                continue;
            }
            string name = data.name;
            string version;
            if (!ilruntimeInfo.TryGetValue(name, out version))
            {
                continue;
            }
            string ilruntimeUrl = $"{serverUrl}/{platform}/ilruntime/{data.fileName}/{version}/{name}.dll";
            using (UnityWebRequest www = UnityWebRequest.Get(ilruntimeUrl))
            {
                Debug.Log($"Binding {www.url}");

                await www.SendWebRequest();

                switch (www.result)
                {
                    case UnityWebRequest.Result.Success:
                        fs = new FileStream(Path.Combine(clrBindingsPath, name), FileMode.OpenOrCreate, FileAccess.ReadWrite);
                        var datas = www.downloadHandler.data;
                        for (int j = 0; j < datas.Length; ++j)
                        {
                            fs.WriteByte(datas[j]);
                        }
                        fs.Seek(0, SeekOrigin.Begin);
                        domain.LoadAssembly(fs);
                        break;

                    default:
                        Debug.LogError($"GenerateAllGameCLRBinding {www.url} is Error, Result is {www.result}");
                        break;
                }
            }
        }

        InitILRuntime(domain);
        BindingCodeGenerator.GenerateBindingCode(domain, generatedPath);
        if (null != fs)
        {
            fs.Close();
        }
        AssetDatabase.Refresh();
    }

    static async Task loadTable(string platform)
    {
        ilruntimeInfo.Clear();
        string url = $"{serverUrl}/{platform}/bundle_table/0.0.0/bundleInfo_{platform}_en.sc";
        Dictionary<string, Dictionary<string, TableData>> tableData;
        using (UnityWebRequest tableRequest = UnityWebRequest.Get(url))
        {
            tableRequest.downloadHandler = new DownloadHandlerBuffer();
            await tableRequest.SendWebRequest();
            if (UnityWebRequest.Result.Success != tableRequest.result)
            {
                Util.Log($"get serverAssetTable failed, url: {url}");
                return;
            }
            var text = tableRequest.downloadHandler.text;
            tableData = JsonMapper.ToObject<Dictionary<string, Dictionary<string, TableData>>>(text);
        }

        var tableInfo = tableData.GetEnumerator();
        while (tableInfo.MoveNext())
        {
            var info = tableInfo.Current.Value.GetEnumerator();
            while (info.MoveNext())
            {
                var data = info.Current.Value as TableData;
                if (data.type.Equals("ilruntime"))
                {
                    ilruntimeInfo.Add(tableInfo.Current.Key, data.bundle_ver.ToString());
                }
            }
        }
    }

    public static void GenerateCLRBindingByAnalysis()
    {
        AppDomain domain = new AppDomain();

        string[] CLRFileNames = new string[] { "CommonILRuntime", BindingGameName };
        FileStream fs = null;
        for (int i = 0; i < CLRFileNames.Length; ++i)
        {
            fs = new FileStream($"Assets/StreamingAssets/ILRuntime/{CLRFileNames[i]}.dll", FileMode.Open, FileAccess.Read);
            domain.LoadAssembly(fs);
        }

        InitILRuntime(domain);
        BindingCodeGenerator.GenerateBindingCode(domain, "Assets/ILRuntime/Generated");
        if (null != fs)
        {
            fs.Close();
        }
        AssetDatabase.Refresh();
    }

    static void InitILRuntime(AppDomain domain)
    {
        ILRuntimeHelper.Instance.registerCrossBindingAdaptor(domain);
    }
}

class CLRBindingData
{
    public GameData[] Game;
}
class GameData
{
    public string name;
    public bool onLine;

    public string fileName
    {
        get
        {
            if (name.Equals("CommonILRuntime"))
            {
                return "common";

            }
            if (name.Equals("Lobby"))
            {
                return "lobby";
            }
            return $"game/{name}";
        }
    }
}

class TableData
{
    public string type;
    public bool must_update;
    public object bundle_ver;
}

