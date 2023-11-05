using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UniRx;
using UnityEngine;
using UnityEngine.Networking;
using UnityEditor;
using System.Linq;
using AssetBundles;
using System.Security.Cryptography;
using UniRx;
namespace dfBundleTool
{
    public struct AssetBundleDownloadCommand
    {
        public BundleInfo bundleInfo;
        //public string BundleName;
        public Hash128 Hash;
        //public uint Crc;
        //public uint Version;
        public Action<bool> OnComplete;
    }
    [Serializable]
    public class BundleProvider : MonoBehaviour, IBundleProvider
    {
        protected string contentHost;
        protected string platformName;
        protected string dirName;
        protected string manifestName;
        protected AssetBundle bundleManifest = null;
        protected AssetBundleManifest manifest = null;
        protected BundleInfo manifestBundleInfo = null;
        Dictionary<string, AssetBundle> cachedBundle;
        List<CancellationTokenSource> asyncCacheJobs = new List<CancellationTokenSource>();
        protected Subject<float> downloadProgress = new Subject<float>();
        public Subject<long> downloadFileCount { get; private set; } = new Subject<long>();
        int totalBundleCount;
        bool isCanceling = false;
        private const int MAX_RETRY_COUNT = 10;
        private const float RETRY_WAIT_PERIOD = 1;
        //最大同時下載量
        private const int MAX_SIMULTANEOUS_DOWNLOADS = 3;
        private static readonly Hash128 DEFAULT_HASH = default(Hash128);
        private static readonly long[] RETRY_ON_ERRORS = {
            503 // Temporary Server Error
        };

        //private string baseUri;
        private Action<IEnumerator> coroutineHandler;
        //同時下載量
        private int activeDownloads = 0;
        private Queue<IEnumerator> downloadQueue = new Queue<IEnumerator>();
        private bool cachingDisabled;

        protected byte[] manifestData = null;

        public BundleInfo nowBundlInfo = null;
        public UnityWebRequest bundleWebRequest = null; 

        private Action<bool> m_onResult;

        ~BundleProvider()
        {
            unloadBundles();


        }

        public IDisposable subscribeProgress(Action<float> progressHandler)
        {
            return downloadProgress.Subscribe(progressHandler);
        }

        public IDisposable subscribePatchFileCount(Action<long> fileCountHandler)
        {
            return downloadFileCount.Subscribe(fileCountHandler);
        }

        public int getBundleCount()
        {
            return totalBundleCount;
        }

        public void unloadBundles()
        {
            if (null != cachedBundle)
            {
                foreach (var bundle in cachedBundle.Values)
                {
                    bundle.Unload(false);
                }
                cachedBundle.Clear();
                manifest = null;
            }
        }

        protected virtual string getCachedDirPath()
        {
            return $"{Application.temporaryCachePath}";
        }

        public void init(string contentHost, string dirName, string manifestName = "manifest")
        {

            this.dirName = dirName; //lobby or (game_name)
            this.manifestName = manifestName;
            this.contentHost = contentHost;
            cachedBundle = new Dictionary<string, AssetBundle>();
            DontDestroyOnLoad(gameObject);

#if UNITY_EDITOR
            if (!Application.isPlaying)
                coroutineHandler = EditorCoroutine.Start;
            else
#endif
                coroutineHandler = AssetBundleDownloaderMonobehaviour.Instance.HandleCoroutine;
        }

        //dirName: lobby or (game_name)
        //lobby or game bundle manifestName
        public void preload(System.Action<bool> finishCallback)
        {
            float startPreloadTime = Time.realtimeSinceStartup;
            if (null != manifest)
            {
                string[] bundleNames = manifest.GetAllAssetBundles();
                for (int i = 0; i < bundleNames.Length; i++)
                {
                    AssetBundle bundle = loadBundle(bundleNames[i]);
                    bundle.LoadAllAssets();
                }
                Util.LogWarning($"preload cost time: {Time.realtimeSinceStartup - startPreloadTime}");
                finishCallback?.Invoke(true);
                return;
            }
            else
            {
                finishCallback?.Invoke(false);
                return;
            }
        }
        public void cacheBundle(string bundleName, AssetBundle ab)
        {
            AssetBundle bundle = null;
            if (!cachedBundle.TryGetValue(bundleName, out bundle))
            {
                cachedBundle.Add(bundleName, ab);
            }

            if (null != ab)
                AssetBundleManager.Instance.addBundleAssetPath(bundleName, ab.GetAllAssetNames());


        }

        public void patch(Action<bool> onResult)
        {
            m_onResult = onResult;
            StartCoroutine(downloadBundle(onResult));
        }


        public void retryPatch()
        {
            StartCoroutine(downloadBundle(m_onResult));
        }
       
        IEnumerator downloadBundle(Action<bool> onResult)
        {
            float startPatchTime = Time.realtimeSinceStartup;
            //Util.Log($"downloadBundle:{dirName}");
            List<AssetBundleDownloadCommand> infos = filterDownloadList(dirName);
            BundleInfoManager nowTable = AssetBundleManager.Instance.bundleInfoMng;
            BundleInfoManager lastTable = nowTable.getLastInfoTable();
            BundleInfo info = null;
            AssetBundleDownloadCommand abCmd;
            bundleManifest = null;
            //Util.Log($"{dirName} manifestfile check:{manifestBundleInfo != null}");

            if (null != manifestBundleInfo && manifestBundleInfo.title.Contains("common_manifest") && AssetBundleManager.Instance.manifest != null)
            {
                manifest = AssetBundleManager.Instance.manifest;
            }
            //下載manifest
            else if (null != manifestBundleInfo)
            {
                //Util.Log($"downloadBundle_1:{dirName}");
                var dirPath = getCachedFilePath($"/{manifestBundleInfo.data.list[0].bundle_name}");
                var filePath = $"{dirPath}/manifest";
                //Util.Log($"downloadBundle_2:{filePath}");
                if (File.Exists(filePath))
                {
                    if (manifest == null)
                    {
                        //Util.Log($"manifestfile exist:{filePath}");
                        byte[] bytes = null;
                        var t1 = Task.Run(async () => bytes = await FileAsync.ReadAllBytes(filePath));
                        while (!t1.IsCompleted)
                        {
                            yield return null;
                        }
                        //Util.Log($"downloadBundle_3:{filePath}");
                        AssetBundleCreateRequest bundleReq = AssetBundle.LoadFromMemoryAsync(bytes);
                        //Util.Log($"downloadBundle_4:{bundleReq == null}");
                        yield return bundleReq;
                        //Util.Log($"downloadBundle_5:{bundleReq.assetBundle == null}");
                        bundleManifest = bundleReq.assetBundle;
                        //Util.Log($"downloadBundle_6:{bundleManifest == null}");
                        if (null != bundleManifest)
                        {
                            AssetBundleRequest assetReq = bundleManifest.LoadAssetAsync<AssetBundleManifest>("assetbundlemanifest");
                            //Util.Log($"downloadBundle_7:{assetReq == null}");
                            yield return assetReq;
                            //Util.Log($"downloadBundle_8:{manifest == null}");
                            manifest = assetReq.asset as AssetBundleManifest;

                            if (manifestBundleInfo.title.Contains("common_manifest"))
                            {
                                AssetBundleManager.Instance.manifest = manifest;
                            }
                        }
                        //Util.Log($"downloadBundle_9:{manifest == null}");
                    }
                    //bundleManifest.Unload(true);
                    yield return 0;

                    downloadProgress.OnNext(1);
                }
                else
                {
                    //Util.Log($"manifestfile not exist:{filePath}");
                    abCmd = new AssetBundleDownloadCommand();
                    abCmd.bundleInfo = manifestBundleInfo;
                    nowBundlInfo = manifestBundleInfo;
                    //下載bundle
                    yield return StartCoroutine(download(abCmd));
                }

            }
            else
            {
                Util.Log($"manifest info is null:{dirName}");
            }

            int bundleCount = infos.Count;
            totalBundleCount = bundleCount;

            for (int i = 0; i < infos.Count; i++)
            {
                abCmd = infos[i];
                info = infos[i].bundleInfo;

                if (cachedBundle.ContainsKey(info.title))
                {
                    Util.LogWarning($"getAndCacheBundle duplicate: {info.title}");
                    //AssetBundleManager.Instance.addBundleAssetPath(info.title, cachedBundle[info.title].GetAllAssetNames());
                    abCmd.OnComplete?.Invoke(true);
                    continue;
                }

                //nowBundlInfo = info;
                //檢查版本資源是否存在
                var dirPath = getCachedFilePath($"/{info.data.list[0].bundle_name}");
                var filePath = $"{dirPath}/{info.title}";
                //Util.Log($"downloadBundle_dirPath:{dirPath}");
                //Util.Log($"downloadBundle_filePath:{filePath}");

                if(null != manifest)
                    abCmd.Hash = manifest.GetAssetBundleHash(info.title);
                abCmd.OnComplete = (success) =>
                {
                };

                //bundle存在
                if (File.Exists(filePath))
                {
                    //Util.Log($"bundle存在1 file exist:{filePath}");
                    byte[] bytes = null;// = File.ReadAllBytes(filePath);
                    var t1 = Task.Run(async () => bytes = await FileAsync.ReadAllBytes(filePath));
                    while (!t1.IsCompleted)
                    {
                        yield return null;
                    }
                    //Util.Log($"bundle存在2 file exist:{filePath}");
                    AssetBundleCreateRequest bundleReq = AssetBundle.LoadFromMemoryAsync(bytes);
                    //Util.Log($"bundle存在3 file exist:{filePath}");
                    yield return bundleReq;
                    //Util.Log($"bundle存在4 file exist:{filePath}");
                    AssetBundle bundle = bundleReq.assetBundle;
                    if (null != bundle)
                    {
                        AssetBundleManager.Instance.addBundleAssetPath(info.title, bundle.GetAllAssetNames());
                        AssetBundleManager.Instance.setSaveFilePath(info.title, filePath);
                        cacheBundle(info.title, bundle);
                    }
                    //bundle.Unload(false);

                    downloadProgress.OnNext(1);
                }
                else
                {
                    //Util.Log($"file not exist:{filePath}");
                    nowBundlInfo = info;
                    //下載bundle
                    yield return StartCoroutine(download(abCmd));
                }
            }
            if(null != bundleManifest)
                bundleManifest.Unload(false);
            yield return 1;
            onResult?.Invoke(true);
            Util.LogWarning($"patch total time: {Time.realtimeSinceStartup - startPatchTime}");
        }


        protected virtual List<AssetBundleDownloadCommand> filterDownloadList(string dir)
        {
            return null;
        }

        #region 待移除

        protected IEnumerator getRemoteManifest(System.Action<AssetBundleManifest> onComplete)
        {
            manifestData = null;
            string url = getRemoteFilePath(manifestName);
            using (UnityWebRequest bundleWebRequest = new UnityWebRequest(url))
            {
                bundleWebRequest.downloadHandler = new DownloadHandlerBuffer();
                yield return bundleWebRequest.SendWebRequest();
                if (UnityWebRequest.Result.Success != bundleWebRequest.result)
                {
                    Util.Log($"get manifest failed, url: {url}");
                    onComplete?.Invoke(null);
                    yield break;
                }
                else
                {
                    AssetBundle bundle = AssetBundle.LoadFromMemory(bundleWebRequest.downloadHandler.data);

                    AssetBundleManifest[] abms = bundle.LoadAllAssets<AssetBundleManifest>();



                    if (null == bundle)
                    {
                        onComplete?.Invoke(null);
                        yield break;
                    }

                    string[] allBundleNames = bundle.GetAllAssetNames();
                    if (1 != allBundleNames.Length)
                    {
                        Util.LogWarning($"unusual manifest bundle length: {allBundleNames.Length}");
                    }

                    //bundle name != manifest name，所以要以 manifest asset name 為準
                    manifest = bundle.LoadAsset<AssetBundleManifest>(bundle.GetAllAssetNames()[0]); //一個bundle一個manifest，所以直接取0
                    bundle.Unload(false);
                    //write to cache
                    //確定載完再寫入, 先暫存data
                    manifestData = bundleWebRequest.downloadHandler.data;
                    //FileAsync.WriteAllBytes(getCachedFilePath(manifestName), bundleWebRequest.downloadHandler.data, CancellationToken.None);

                    onComplete?.Invoke(manifest);
                    yield break;
                }
            }
        }

        IEnumerator patchByOder(List<string> bundleNames, System.Action<bool> callback)
        {
            int bundleCount = bundleNames.Count;
            totalBundleCount = bundleCount;
            int successCount = 0;
            for (int i = 0; i < bundleCount; i++)
            {
                string bundleName = bundleNames[i];

                //新版下載 retry機制
                AssetBundleDownloadCommand downloadCmd = new AssetBundleDownloadCommand();
                //downloadCmd.BundleName = bundleName;
                //to do 版本檢查
                //downloadCmd.Version = 0;
                //downloadCmd.Hash = Hash128.Parse(serverHash[bundleName]);
                //downloadCmd.Crc = 0;
                downloadCmd.OnComplete = (success) =>
                {
                    if (success)
                    {
                        successCount++;
                        float percent = (float)successCount / bundleCount;
                        downloadFileCount.OnNext(successCount);
                        downloadProgress.OnNext(percent);
                    }
                    else
                    {
                        callback?.Invoke(false);
                    }
                };
                InternalHandle(Download(downloadCmd, 0));
            }
            Util.Log($"patchByOder {downloadQueue.Count},{activeDownloads}");
            yield return new WaitUntil(() => downloadQueue.Count <= 0 && activeDownloads <= 0 && !isCanceling);
            Util.Log("patchByOder completed");
            if (null != manifestData)
                FileAsync.WriteAllBytes(getCachedFilePath(manifestName), manifestData, CancellationToken.None);

            callback?.Invoke(true);

        }


        private IEnumerator Download(AssetBundleDownloadCommand cmd, int retryCount)
        {
            if (cachedBundle.ContainsKey(cmd.bundleInfo.title))
            {
                Util.LogWarning($"getAndCacheBundle duplicate: {cmd.bundleInfo.title}");
                cmd.OnComplete?.Invoke(true);
                yield break;
            }

            string url = getRemoteFilePath(cmd.bundleInfo.title);
            Util.Log($"url{url}");
            AssetBundle bundle = null;
            /* TEST FOR HASH
            //var hash = new Hash128();
            //hash.Append("546548");
            //Util.Log($"Download cmd hash:{cmd.Hash}");
            //Util.Log($"Download var hash:{hash}");           
            //using (UnityWebRequest bundleWebRequest = UnityWebRequestAssetBundle.GetAssetBundle(url, hash))
            */
            using (UnityWebRequest bundleWebRequest = UnityWebRequestAssetBundle.GetAssetBundle(url))
            {
                bundleWebRequest.downloadHandler = new DownloadHandlerBuffer();
                yield return bundleWebRequest.SendWebRequest();
                var isNetworkError = bundleWebRequest.result == UnityWebRequest.Result.ConnectionError;// req.isNetworkError;
                var isHttpError = bundleWebRequest.result == UnityWebRequest.Result.ProtocolError;// req.isHttpError;

                //Util.Log($"Download Result:{bundleWebRequest.result}");

                if (UnityWebRequest.Result.Success != bundleWebRequest.result)
                {
                    if (isHttpError)
                    {
                        Util.LogError(string.Format("Error downloading [{0}]: [{1}] [{2}]", url, bundleWebRequest.responseCode, bundleWebRequest.error));

                        if (retryCount < MAX_RETRY_COUNT && RETRY_ON_ERRORS.Contains(bundleWebRequest.responseCode))
                        {
                            Util.LogWarning(string.Format("Retrying [{0}] in [{1}] seconds...", url, RETRY_WAIT_PERIOD));
                            bundleWebRequest.Dispose();
                            activeDownloads--;
                            yield return new WaitForSeconds(RETRY_WAIT_PERIOD);
                            InternalHandle(Download(cmd, retryCount + 1));
                            yield break;
                        }
                        else
                        {
                            downloadErrorMsg();
                        }

                    }
                    else if (isNetworkError)
                    {
                        downloadErrorMsg();
                        Util.LogError(string.Format("Error downloading [{0}]: [{1}]", url, bundleWebRequest.error));
                    }
                    else
                    {

                    }
                    //Util.Log($"{bundleWebRequest.error}, url: {url}");
                    cmd.OnComplete?.Invoke(false);

                    yield break;
                }
                else
                {
                    try
                    {
                        //Debug.Log($"Download001:{cmd.BundleName}");
                        bundle = AssetBundle.LoadFromMemory(bundleWebRequest.downloadHandler.data);
                        Debug.Log($"Download001_1:{bundle.GetAllAssetNames().Length}");
                        //AssetBundleManifest[] abms = bundle.LoadAllAssets<AssetBundleManifest>();
                        //bundle = DownloadHandlerAssetBundle.GetContent(bundleWebRequest);

                        string path = getCachedFilePath(cmd.bundleInfo.title);
                        File.WriteAllBytes(path, bundleWebRequest.downloadHandler.data);

                        string[] bNames = bundle.GetAllAssetNames();

                        for (int i = 0; i < bNames.Length; i++)
                        {
                            string bundleName = bNames[i];
                            Debug.Log($"Download002:{bundleName}");
                            string bundlePath = getCachedFilePath(bundleName);
                            Debug.Log($"Download002_2:{bundlePath}");
                            var obj = bundle.LoadAsset(bundleName);
                            Debug.Log($"Download002_3:{obj != null}");
                            //File.WriteAllBytes(bundlePath, ToByteArray(obj));
                        }
                        //Debug.Log($"Download002:{abms.Length}");
                        //AssetBundleManifest manifest = bundle.LoadAsset<AssetBundleManifest>(cmd.BundleName);
                        //Debug.Log($"Download003:{cmd.BundleName}");
                        //string[] bNames = manifest.GetAllAssetBundles();
                        //Debug.Log($"Download004:{bNames.Length}");
                        //bundle = DownloadHandlerAssetBundle.GetContent(bundleWebRequest);
                        //string path = getCachedFilePath(cmd.BundleName);
                        //File.WriteAllBytes(path, bundleWebRequest.downloadHandler.data);
                        //var req = UnityWebRequestAssetBundle.GetAssetBundle(path,cmd.Hash);
                        //bundle = DownloadHandlerAssetBundle.GetContent(req);
                        //bundle = ((DownloadHandlerAssetBundle)bundleWebRequest.downloadHandler).assetBundle;
                        //cmd.OnComplete?.Invoke(true);


                    }
                    catch (Exception ex)
                    {
                        // Let the user know there was a problem and continue on with a null bundle.
                        Util.LogError("Error processing downloaded bundle, exception follows...");
                        Util.LogException(ex);
                    }
                }

                if (!isNetworkError && !isHttpError && string.IsNullOrEmpty(bundleWebRequest.error) && bundleWebRequest.downloadHandler.data == null) //&& bundle == null)
                {
                    if (cachingDisabled)
                    {
                        Util.LogWarning(string.Format("There was no error downloading [{0}] but the bundle is null.  Caching has already been disabled, not sure there's anything else that can be done.  Returning...", url));
                    }
                    else
                    {
                        Util.LogWarning(string.Format("There was no error downloading [{0}] but the bundle is null.  Assuming there's something wrong with the cache folder, retrying with cache disabled now and for future requests...", url));
                        cachingDisabled = true;
                        bundleWebRequest.Dispose();
                        activeDownloads--;
                        yield return new WaitForSeconds(RETRY_WAIT_PERIOD);
                        InternalHandle(Download(cmd, retryCount + 1));
                        yield break;
                    }
                }

                try
                {
                    cmd.OnComplete?.Invoke(true);
                }
                finally
                {

                    bundleWebRequest.Dispose();

                    activeDownloads--;

                    if (downloadQueue.Count > 0)
                    {
                        InternalHandle(downloadQueue.Dequeue());
                    }
                }

            }
        }

        #endregion

  
        //下載&Bundle快取
        //In Windows cached in: C:\Users\(user_name)\AppData\LocalLow\Unity\finger_(projName)
        //C:\Users\(user_name\AppData\Loal\Temp\(com_name)finger\(proj_name))
        IEnumerator download(AssetBundleDownloadCommand downloadCmd)
        {
            BundleInfo info = downloadCmd.bundleInfo;
            Action<bool> OnComplete = downloadCmd.OnComplete;
            //Util.Log($"downloadBundle:{info.title}");
            if (cachedBundle.ContainsKey(info.title))
            {
                Util.LogWarning($"getAndCacheBundle duplicate: {info.title}");
                OnComplete?.Invoke(true);
                yield break;
            }

            string titleName = AssetBundleManager.Instance.bundleInfoMng.getRealTitle(info);
            string url = getRemoteFilePath($"{info.data.list[0].bundle_url}/{titleName}");

            downloadFileCount.OnNext(nowBundlInfo.data.list[0].file_size);
            IDisposable dispose = Observable.EveryUpdate().Subscribe((_) =>
            {
                if (null != bundleWebRequest)
                {
                    if (!bundleWebRequest.isDone)
                    {
                        downloadProgress.OnNext(bundleWebRequest.downloadProgress);
                    }
                    else
                    {
                        //downloadProgress.OnNext(-1);
                    }
                }
            }).AddTo(this);
            bundleWebRequest = UnityWebRequestAssetBundle.GetAssetBundle(url, downloadCmd.Hash, (uint)info.data.list[0].crc);
            {
                bundleWebRequest.downloadHandler = new DownloadHandlerBuffer();
                yield return bundleWebRequest.SendWebRequest();

                if (UnityWebRequest.Result.Success != bundleWebRequest.result)
                {
                    Util.Log($"fail:{url}");
                    Util.Log($"{bundleWebRequest.error}, url: {url}");
                    OnComplete?.Invoke(false);
                    downloadErrorMsg();
                    yield break;
                }
                else
                {
                    //Util.Log($"success:{url}");
                    //如果加入 cacheBundle 會破壞載入記憶體相依順序，造成破圖，故更新本地資源就好，先不載入記憶體

                    string path = getCachedFilePath($"/{info.data.list[0].bundle_name}");
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }

                    if (info.title.Contains("manifest"))
                    {
                        AssetBundleCreateRequest bundleReq = AssetBundle.LoadFromMemoryAsync(bundleWebRequest.downloadHandler.data);
                        yield return bundleReq;
                        bundleManifest = bundleReq.assetBundle;
                        AssetBundleRequest assetReq = bundleManifest.LoadAssetAsync<AssetBundleManifest>("assetbundlemanifest");
                        yield return assetReq;
                        manifest = assetReq.asset as AssetBundleManifest;

                        if (manifestBundleInfo.title.Contains("common_manifest"))
                        {
                            AssetBundleManager.Instance.manifest = manifest;
                        }

                        //bundle.Unload(true);
                        yield return 0;
#if UNITY_EDITOR
                        File.WriteAllBytes($"{path}/manifest", bundleWebRequest.downloadHandler.data);
#else
                        var t1 = Task.Run(async () => await FileAsync.WriteAllBytes($"{path}/manifest", bundleWebRequest.downloadHandler.data));
                            while (!t1.IsCompleted)
                            {
                                yield return null;
                            }
#endif
                    }
                    else
                    {
                        var filePath = $"{path}/{info.title}";
                        AssetBundleCreateRequest bundleReq = AssetBundle.LoadFromMemoryAsync(bundleWebRequest.downloadHandler.data);
                        yield return bundleReq;
                        AssetBundle bundle = bundleReq.assetBundle;
                        AssetBundleManager.Instance.addBundleAssetPath(info.title, bundle.GetAllAssetNames());
                        AssetBundleManager.Instance.setSaveFilePath(info.title, filePath);
                        cacheBundle(info.title, bundle);
                        //bundle.Unload(false);
#if UNITY_EDITOR
                        File.WriteAllBytes(filePath, bundleWebRequest.downloadHandler.data);
#else

                        var t1 = Task.Run(async () => await FileAsync.WriteAllBytes(filePath, bundleWebRequest.downloadHandler.data));
                            while (!t1.IsCompleted)
                            {
                                yield return null;
                            }
#endif
                    }
                }
            }
            OnComplete?.Invoke(true);
            downloadProgress.OnNext(-1);
            bundleWebRequest = null;
            dispose.Dispose();
        }

        protected virtual string getRemoteFilePath(string fileName)
        {
#if UNITY_EDITOR && STREAMING_ASSETS
            return $"{Application.streamingAssetsPath}/{fileName}";
#else
            return $"{contentHost}{fileName}";
#endif
        }

        protected virtual string getCachedFilePath(string fileName)
        {
            return $"{getCachedDirPath()}{fileName}";
        }

        AssetBundle loadBundle(string bundleName)
        {

            var bundlePath = getCachedFilePath($"/{bundleName}");
            Util.Log($"loadBundle:{bundleName}_path:{bundlePath}");
            AssetBundle bundle = null;
            if (!cachedBundle.TryGetValue(bundleName, out bundle))
            {
                if (PatchHelper.isFileExist(bundlePath))
                {
                    var bytes = File.ReadAllBytes(bundlePath);
                    bundle = AssetBundle.LoadFromMemory(bytes);
                    cachedBundle.Add(bundleName, bundle);
                }
                return bundle;
            }
            return bundle;
        }

        public AssetBundle loadBundleWithDependency(string bundleName)
        {
            AssetBundle bundle = null;

            if (!cachedBundle.TryGetValue(bundleName, out bundle))  //沒被cache, 代表沒載入過，且還沒載入相依
            {
                Queue<string> loadOrder = new Queue<string>();
                addByDependencies(bundleName, ref loadOrder);
                while (loadOrder.Count > 0)
                {
                    string name = loadOrder.Dequeue();

                    if (name.Equals(bundleName))    //最後一個是自己，就不使用AssetBundleManager載入，避免陷入無窮呼叫
                    {
                        bundle = loadBundle(name);
                    }
                    else
                    {
                        bundle = AssetBundleManager.Instance.getBundle(name);   //Get From Other Provider
                    }

                    if (null != bundle)
                    {
                        bundle.LoadAllAssets(); //one bundle, one resource, use loadAll
                        if (0 == loadOrder.Count)   //最後一個是自己
                        {
                            return bundle;
                        }
                    }
                    else
                    {
                        Util.Log($"In BundleProvider, loadBundleWithDependency, bundle null: {name}, dirName: {dirName}");
                    }
                }
            }

            return bundle;
        }

        public T getAssetFromBundle<T>(string bundleName) where T : UnityEngine.Object
        {
            AssetBundle ab = null;
            BundleInfo depAb = null;
            T bundle = null;
            foreach (KeyValuePair<string, AssetBundle> pair in cachedBundle)
            {
                if (pair.Key.Contains("scene")) continue;
                ab = pair.Value;
                /*
                string[] dependencies = manifest.GetAllDependencies(pair.Key);
                foreach (string dependency in dependencies)
                {
                    //Debug.Log(dependency);
                    //depAb = AssetBundleManager.Instance.bundleInfoMng.getBundleInfo(dependency);

                    //if (cachedBundle.ContainsKey(depAb.title)) continue;

                    // var dirPath = getCachedFilePath($"/{depAb.data.list[0].bundle_name}");
                    //var filePath = $"{depAb.data.list[0].bundle_name}/{depAb.title}";
                    //loadBundle(filePath);
                    //cacheBundle(depAb.title, AssetBundle.LoadFromFile(filePath));
                }*/

                bundle = ab.LoadAsset<T>(bundleName);
                if (null != bundle)
                    return bundle;

            }

            /*
            AssetBundle bundle = loadBundleWithDependency(bundleName);

            if (null != bundle)
            {
                T[] allAssets = bundle.LoadAllAssets<T>();
                if (allAssets.Length > 0)
                {

                    return allAssets[0];   //因為一個檔案包成一包 bundle
                }
            }*/
            return null;
        }


        public T[] getAllAssetFromBundle<T>(string bundleName) where T : UnityEngine.Object
        {
            AssetBundle ab = null;
            BundleInfo depAb = null;
            T[] bundle = null;
            string[] assetsName;
            foreach (KeyValuePair<string, AssetBundle> pair in cachedBundle)
            {
                if (pair.Key.Contains("scene")) continue;

                ab = pair.Value;

                assetsName = AssetBundleManager.Instance.getBundleInfo(pair.Key).assetName;

                if (assetsName.Contains(bundleName))
                {
                    bundle = ab.LoadAllAssets<T>();
                    return bundle;
                }
                /*
                string[] dependencies = manifest.GetAllDependencies(pair.Key);
                foreach (string dependency in dependencies)
                {
                    Debug.Log(dependency);
                    depAb = AssetBundleManager.Instance.bundleInfoMng.getBundleInfo(dependency);

                    if (cachedBundle.ContainsKey(depAb.title)) ;

                    var dirPath = getCachedFilePath($"/{depAb.data.list[0].bundle_name}");
                    var filePath = $"{dirPath}/{depAb.title}";
                    //cacheBundle(depAb.title, AssetBundle.LoadFromFile(filePath));
                }*/
            }
            /*
            AssetBundle bundle = loadBundleWithDependency(bundleName);
            if (null != bundle)
            {
                T[] allAssets = bundle.LoadAllAssets<T>();
                if (allAssets.Length > 0)
                {
                    return allAssets;   //因為一個檔案包成一包 bundle
                }
            }*/
            return null;
        }
        string getAssetName(string fullPath)
        {
            var path = fullPath.Replace('\\', '/');  //不同作業系統資料夾符號規格統一
            return fullPath.Substring(path.LastIndexOf("/") + 1);
        }

        void addByDependencies(string bundleName, ref Queue<string> sortedBundleNames)
        {
            if (null != manifest)
            {
                string[] names = manifest.GetAllDependencies(bundleName);

                for (int i = 0; i < names.Length; i++)
                {
                    string name = names[i];
                    addByDependencies(name, ref sortedBundleNames);
                }
            }

            if (!sortedBundleNames.Contains(bundleName))
            {
                sortedBundleNames.Enqueue(bundleName);
            }
        }

        private void InternalHandle(IEnumerator downloadCoroutine)
        {
            if (activeDownloads < MAX_SIMULTANEOUS_DOWNLOADS)
            {
                activeDownloads++;
                coroutineHandler(downloadCoroutine);
            }
            else
            {
                downloadQueue.Enqueue(downloadCoroutine);
            }
        }

#region check tools
        public byte[] ToByteArray<T>(T obj)
        {
            if (obj == null)
                return null;
            BinaryFormatter bf = new BinaryFormatter();
            using (MemoryStream ms = new MemoryStream())
            {
                bf.Serialize(ms, obj);
                return ms.ToArray();
            }
        }

        // Convert an object to a byte array
        public static byte[] ObjectToByteArray(UnityEngine.Object obj)
        {
            BinaryFormatter bf = new BinaryFormatter();
            using (var ms = new MemoryStream())
            {
                bf.Serialize(ms, obj);
                return ms.ToArray();
            }
        }
        public byte[] MD5Check(string filename)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filename))
                {
                    return md5.ComputeHash(stream);
                }
            }
        }

        static string CalculateMD5(string filename)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filename))
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }
#endregion

        public void downloadErrorMsg()
        {
            callDownloadErrorMsg(() => {
                Util.Log("retrypatch.....");
                retryPatch(); });
        }

        public void callDownloadErrorMsg(Action callback = null)
        {

            DefaultMsgBox.Instance.getMsgBox()
                .setNormalTitle(LanguageService.instance.getLanguageValue("InternetUnstableTittle"))
                .setNormalContent(LanguageService.instance.getLanguageValue("InternetUnstable"))
                .setNormalCB(callback)
                .openNormalBox(ApplicationConfig.nowLanguage.ToString().ToLower());
        }

        public void cancelBundleDownload()
        {
            isCanceling = true;
            Util.Log($"cancelBundleDownload..{downloadQueue.Count}");
            this.StopAllCoroutines();
            downloadQueue.Clear();
            activeDownloads = 0;
            manifest = null;
            manifestData = null;
        }
    }
}
