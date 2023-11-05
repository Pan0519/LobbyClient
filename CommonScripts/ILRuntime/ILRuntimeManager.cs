using System;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using UnityEngine;
using AppDomain = ILRuntime.Runtime.Enviorment.AppDomain;
using static ResourceManager;
using AssetBundles;

public class ILRuntimeManager : MonoSingleton<ILRuntimeManager>
{
#if UNITY_EDITOR
    public const string forceLoadStreamingAssetDllInEditor = "ILRuntime/ForceLoadStreamingAssetDllInEditor";

    public static bool getForceLoadStreamingAssetDllInEditor()
    {
        return UnityEditor.EditorPrefs.GetBool(forceLoadStreamingAssetDllInEditor, true);
    }
#endif

    public AppDomain appDomain { get; private set; }

    string filName { get { return "ILRuntime"; } }

    BundleInfo[] dllNames;

    string CONTENT_HOST;

    public async Task<AppDomain> init(string name, string contentHost)
    {
        BundleInfo game;
        BundleInfo common;
        if (ApplicationConfig.isLoadFromAB)
        {
            game = AssetBundleManager.Instance.getILRuntimeInfo(name);
            common = AssetBundleManager.Instance.getILRuntimeInfo("CommonILRuntime");
        }
        else
        {
            game = new BundleInfo();
            game.title = name;
            common = new BundleInfo();
            common.title = "CommonILRuntime";
        }
        dllNames = new BundleInfo[] { game, common };
        CONTENT_HOST = contentHost;

        appDomain = await getAppDomain();
        return appDomain;
    }

    async Task<AppDomain> getAppDomain()
    {
        try
        {
            List<AppDomainLoadData> loadDatas = getDomainDatas();
            AppDomain appDomain = await ILRuntimeHelper.Instance.initAppDomain(loadDatas);
            return appDomain;

        }
        catch (Exception e)
        {
            Debug.Log($"Get AppDomain Error {e.Message}");
        }

        return null;
    }

    List<AppDomainLoadData> getDomainDatas()
    {
        List<AppDomainLoadData> datas = new List<AppDomainLoadData>();
        for (int i = 0; i < dllNames.Length; ++i)
        {
            AppDomainLoadData loadData = null;
            if (UiLoadFrom.AssetBundle == ResourceManager.instance.resourceLoadFrom)
            {
                loadData = getRemoteAppDomainData(dllNames[i]);
            }
            else
            {
                loadData = getAppDomainLoadData(dllNames[i]);
            }
            datas.Add(loadData);
        }

        return datas;
    }

    AppDomainLoadData getAppDomainLoadData(BundleInfo dllName)
    {
        Tuple<string, string> loadPath = getLoadDataPath(dllName.title);
        Task<byte[]> loadStreamingDll = ArchiveProvider.Instance.loadFileWithFullPathAsync(ArchiveProvider.Instance.getFilePathWithStreamingAsset(loadPath.Item1));
        Task<byte[]> loadStreamingAssetPdb = ArchiveProvider.Instance.loadFileWithFullPathAsync(ArchiveProvider.Instance.getFilePathWithStreamingAsset(loadPath.Item2));

        return new AppDomainLoadData(loadStreamingDll, loadStreamingAssetPdb);
    }

    AppDomainLoadData getRemoteAppDomainData(BundleInfo dllBundle)
    {
        Task<byte[]> loadStreamingDll;
        Task<byte[]> loadStreamingAssetPdb;
        string bundleUrl = dllBundle.data.list[0].bundle_url;
        string assetName = dllBundle.data.list[0].bundle_name;

        string path = $"{assetName}/{dllBundle.title}/{dllBundle.bundle_ver}";
        string dllPath =$"{path}/{dllBundle.title}.dll";
        string pdbPath = $"{path}/{dllBundle.title}.pdb";

        if (File.Exists(getCachedDirPath(dllPath)))
        {
            //Util.Log($"file exist:{dllPath}");
            loadStreamingDll = getLocalFile(getCachedDirPath(dllPath));
        }
        else
        {
            //Util.Log($"file not exist:{dllPath}");
            var dir = getCachedDirPath(path);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            loadStreamingDll = ArchiveProvider.Instance.loadFileWithFullPathAsync($"{CONTENT_HOST}{bundleUrl}{dllBundle.title}.dll");
            saveDll(loadStreamingDll, getCachedDirPath(dllPath));
        }
#if !PROD && !STAGE
        if (File.Exists(getCachedDirPath(pdbPath)))
        {
            //Util.Log($"file exist:{pdbPath}");
            loadStreamingAssetPdb = getLocalFile(getCachedDirPath(pdbPath));
        }
        else
        {
            //Util.Log($"file not exist:{pdbPath}");
            var dir = getCachedDirPath(path);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            loadStreamingAssetPdb = ArchiveProvider.Instance.loadFileWithFullPathAsync($"{CONTENT_HOST}{bundleUrl}{dllBundle.title}.pdb");
            saveDll(loadStreamingAssetPdb, getCachedDirPath(pdbPath));
        }
        return new AppDomainLoadData(loadStreamingDll, loadStreamingAssetPdb);
#else

  return new AppDomainLoadData(loadStreamingDll, null);
#endif

    }

    public async void saveDll(Task<byte[]> loadData, string filePath)
    {
        byte[] dllBytes = await loadData;
        await FileAsync.WriteAllBytes(filePath, dllBytes, CancellationToken.None);
    }


    protected string getCachedDirPath(string path)
    {
        return $"{Application.temporaryCachePath}/{path}";
    }


    async Task<byte[]> getLocalFile(string path)
    {
        return File.ReadAllBytes(path);
    }

    public Tuple<string, string> getLoadDataPath(string dllName)
    {
        return Tuple.Create($"{filName}/{dllName}.dll", $"{filName}/{dllName}.pdb");
    }
}
