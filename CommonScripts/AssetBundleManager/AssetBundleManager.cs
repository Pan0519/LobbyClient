using dfBundleTool;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UniRx;
using AssetBundles;
using System.IO;
public class AssetBundleManager : Singleton<AssetBundleManager>
{
    string CONTENT_HOST = "http://192.168.128.235:8080";
    //string CONTENT_HOST = "file://D:/Work/Work_RD3/Lobby_Client/_outputbundles";
    //string CONTENT_HOST = "file://D:/Asgard/LobbyClient";

    Dictionary<string, IBundleProvider> providers = new Dictionary<string, IBundleProvider>();

    List<IDisposable> providersDownloadSubscribe = new List<IDisposable>();

    string gameProvider = "";

    public Subject<long> patchFileCount = new Subject<long>();

    public BundleInfoManager bundleInfoMng;
    public AssetBundleManifest manifest = null;
    GameObject tableObj = null;
    public void setContentHost(string host)
    {
        CONTENT_HOST = host;

        if (null == tableObj)
        {
            tableObj = new GameObject();
            tableObj.name = "BundleInfoManager";
            bundleInfoMng = tableObj.AddComponent<BundleInfoManager>();
            bundleInfoMng.init(CONTENT_HOST);
            DontDestroyRoot.addChild(tableObj.transform);
        }

#if UNITY_EDITOR
        Observable.EveryUpdate().Subscribe((_) =>
        {
            if (Input.GetKeyDown(KeyCode.P))
            {
                foreach (KeyValuePair<string, IBundleProvider> pair in providers)
                {
                    Util.Log($"KeyValuePair_{pair.Key}");
                }
            }
        });
#endif
    }

    public void downloadAssetTable(Action<bool> resultCallback)
    {
        if (ApplicationConfig.isLoadFromAB)
        {
            bundleInfoMng.downloadTable(resultCallback);
        }
        else
        {
            resultCallback?.Invoke(true);
        }
    }


    public BundleInfo getBundleInfo(string name)
    {
        if (!bundleInfoMng.bundleInfos.ContainsKey(name))
        {
            Util.Log($"getBundleInfo {name} is null...");
            return null;
        }
        return bundleInfoMng.bundleInfos[name];
    }

    public BundleInfo getILRuntimeInfo(string name)
    {
        if (!bundleInfoMng.ilruntimeInfos.ContainsKey(name))
        {
            Util.Log($"getILRuntimeInfo {name} is null...");
            return null;
        }
        return bundleInfoMng.ilruntimeInfos[name];
    }

    public void addBundleAssetPath(string bundleName, string[] paths)
    {
        if(paths.Length != 0)
            bundleInfoMng.setBundleAssets(bundleName,paths);
    }

    public void setSaveFilePath(string bundleName, string path)
    {
        bundleInfoMng.setSaveFilePath(bundleName, path);
    }


    public void addBundleFilePath(string bundleName, string path)
    {
        bundleInfoMng.setSaveFilePath(bundleName, path);
    }


    IBundleProvider provider;
    public int totalBundleCount { get { return provider.getBundleCount(); } }

    Action<long> fileCountsub = null;

    public void preloadBundles(string lobbyOrGameName, Action<bool> resultCallback, Action<float> progressCallback = null)//, Priority priority = Priority.Normal)  //dirName: lobby or (game name)
    {
        provider = getUniqueProvider(lobbyOrGameName);
        if (null != progressCallback)
        {
            providersDownloadSubscribe.Add(provider.subscribeProgress(progressCallback));
        }

        if (null != fileCountsub)
        {
            Util.Log($"{lobbyOrGameName} add fileCountsub");
            providersDownloadSubscribe.Add(provider.subscribePatchFileCount(fileCountsub));
        }

        provider.patch((res) =>
        {
            if (!res)
            {
                Util.Log("patch failed");
            }
            resultCallback?.Invoke(res);
            //provider.preload(resultCallback);
        });
    }

    public void fileCountProgress(Action<long> fileCount)
    {
        fileCountsub = fileCount;
    }

    public long getFileSizeByType(string type)
    {
        type = type.ToLower();
        List<BundleInfo> infos = bundleInfoMng.getBundleInfos(type);
        long fileSize = 0;
        BundleInfo info = null;
        for (int i = 0; i < infos.Count; i++)
        {
            info = infos[i];
            //檢查版本資源是否存在
            var dirPath = getCachedFilePath($"/{info.data.list[0].bundle_name}");
            var fileName = info.title.Contains("manifest") ? "manifest" : info.title;
            var filePath =  $"{dirPath}/{fileName}";
            if (!File.Exists(filePath))
            {
                fileSize += info.data.list[0].file_size;
            }
        }
        return fileSize;
    }

    public long getFileCountByType(string type)
    {
        type = type.ToLower();
        List<BundleInfo> infos = bundleInfoMng.getBundleInfos(type);
        long fileCount = 0;
        BundleInfo info = null;
        for (int i = 0; i < infos.Count; i++)
        {
            info = infos[i];
            //檢查版本資源是否存在
            var dirPath = getCachedFilePath($"/{info.data.list[0].bundle_name}");
            var fileName = info.title.Contains("manifest") ? "manifest" : info.title;
            var filePath = $"{dirPath}/{fileName}";
            if (File.Exists(filePath))
            {
                fileCount++;
            }
        }
        return fileCount;
    }

    protected virtual string getCachedFilePath(string fileName)
    {
        return $"{getCachedDirPath()}{fileName}";
    }
    protected string getCachedDirPath()
    {
        return $"{Application.temporaryCachePath}";
    }

    public void releaseAllBundles()
    {
        manifest = null;
        var provider = providers.GetEnumerator();
        while (provider.MoveNext())
        {
            provider.Current.Value.unloadBundles();
        }
    }

    public void releaseGameBundles()
    {
        if (!string.IsNullOrEmpty(gameProvider))
        {
            var provider = getUniqueProvider(gameProvider);
            provider.unloadBundles();
        }
    }

    public void clearDownloadSubscribe()
    {
        for (int i = 0; i < providersDownloadSubscribe.Count; ++i)
        {
            IDisposable downloadDispose = providersDownloadSubscribe[i];
            if (null != downloadDispose)
            {
                downloadDispose.Dispose();
            }
        }

        providersDownloadSubscribe.Clear();
    }

    public AssetBundle getBundle(string bundlename)
    {
        //Util.Log("==================");
        //Util.Log($"getBundle:{bundlename}");
        var prefix = BundleHelper.getPrefix(bundlename, '_');
        //Util.Log($"prefix:{prefix}");
        var provider = getUniqueProvider(prefix);
        var bundle = provider.loadBundleWithDependency(bundlename);
        //Util.Log("==================");
        return bundle;
    }

    public T getAsset<T>(string assetName, string gameOrCommon) where T : UnityEngine.Object
    {
        string bundleName = getBundleName<T>(assetName);
        //bundleName = $"{gameOrCommon}_{bundleName}";
        var provider = getUniqueProvider(gameOrCommon);
        T asset = provider.getAssetFromBundle<T>(bundleName);
        return asset;   //TODO: load dependencies
    }

    public T[] getAllAsset<T>(string assetName, string gameOrCommon) where T : UnityEngine.Object
    {
        string bundleName = getBundleName<T>(assetName);
        //bundleName = $"{gameOrCommon}_{bundleName}";
        var provider = getUniqueProvider(gameOrCommon);
        T[] asset = provider.getAllAssetFromBundle<T>(bundleName);
        return asset;   //TODO: load dependencies
    }

    IBundleProvider getUniqueProvider(string lobbyOrGameName)
    {
        string providerName = lobbyOrGameName.ToLower();
        IBundleProvider provider = null;
        if (!providers.TryGetValue(providerName, out provider))
        {
            Util.LogWarning($"建立 UniqueProvider, 載入{lobbyOrGameName}相關資源:");   //若建立UniqueProvider無呼叫 preload,可能造成後續provider manifest null 的錯誤
            provider = new DfBundleProvider();
            //Util.Log($"provider host {CONTENT_HOST}");
            provider.init(CONTENT_HOST, providerName);
            providers.Add(providerName, provider);
        }
        return provider;
    }

    string getBundleName<T>(string assetName)
    {
        string bundleName = assetName;
        /*
        Type tType = typeof(T);
        if (tType == typeof(UnityEngine.GameObject))
        {
            bundleName = $"{assetName}_prefab";
        }
        else if (tType == typeof(UnityEngine.Texture2D) || tType == typeof(Sprite))
        {
            bundleName = $"{assetName}_texture";
        }
        else
        {
            bundleName = $"bundle_{assetName}";
        }*/
        return bundleName.ToLower();
    }

    public AssetBundle getLoadedBundle(string assetName)
    {
        AssetBundle[] loadABs = Resources.FindObjectsOfTypeAll<AssetBundle>();
        AssetBundle existAB = null;
        if (null != loadABs)
        {
            foreach (var item in loadABs)
            {
                //Util.Log($"item name:{item}___{item.name}");
                if (item.Contains(assetName))
                {
                    existAB = item;
                    break;
                }
            }
        }
        return existAB;
    }
    public void cancelBundleDownload(string lobbyOrGameName)
    {
        string providerName = lobbyOrGameName.ToLower();
        IBundleProvider provider = null;
        if (providers.TryGetValue(providerName, out provider))
        {
            ((DfBundleProvider)provider).cancelBundleDownload();
        }
    }

    public void cancelAllBundleDownload()
    {
        DfBundleProvider df = null;
        foreach (KeyValuePair<string, IBundleProvider> pair in providers)
        {
            Util.Log($"KeyValuePair_{pair.Key}");
            df = (DfBundleProvider)pair.Value;
            df.cancelBundleDownload();
        }
    }

    public void callDownloadErrorMsg(Action callback = null)
    {
        DefaultMsgBox.Instance.getMsgBox()
            .setNormalTitle(LanguageService.instance.getLanguageValue("InternetUnstableTittle"))
            .setNormalContent(LanguageService.instance.getLanguageValue("InternetUnstable"))
            .setNormalCB(callback)
            .openNormalBox(ApplicationConfig.nowLanguage.ToString());
    }

    public GameObject getLocalPrefab(string prefabName , string path)
    {
        //檢查版本資源是否存在
        var filePath = getCachedFilePath($"/{path}");
        if (File.Exists(filePath))
        {
            byte[] bytes = File.ReadAllBytes(filePath);
            AssetBundle bundle = AssetBundle.LoadFromMemory(bytes);
            var obj = bundle.LoadAsset<GameObject>(prefabName);
            bundle.Unload(false);
            return obj;
        }
        return null;
    }

}
