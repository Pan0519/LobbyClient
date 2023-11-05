using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Threading.Tasks;
using UnityEngine.Networking;
using System.Linq;

public sealed class ResourceManager : MonoSingleton<ResourceManager>, IResourceManager
{
    string LoadFromSaveKey { get { return "LoadFrom"; } }
    public enum UiLoadFrom
    {
        Resources,
        AssetBundle,
    }

    public enum UiLoadFile
    {
        CommonArt,
        GameArt
    }

    private Dictionary<string, Pool> pools = new Dictionary<string, Pool>();
    Dictionary<Type, string> resourceType = new Dictionary<Type, string>();
    List<string> loadResOrder = new List<string>();
    List<string> poolsNames = new List<string>();

    private void Awake()
    {
        cachedGameObject.setActiveWhenChange(false);

        resourceType.Add(typeof(GameObject), "prefab");
        resourceType.Add(typeof(AudioClip), "ogg");
        resourceType.Add(typeof(Texture2D), "png");
        resourceType.Add(typeof(Sprite), "png");
        resourceType.Add(typeof(Font), "fontsettings");

        int[] aaa = new int[2];
        Array.FindIndex(aaa, a => a == 1);
        for (int i = 0; i < aaa.Length; ++i)
        {

        }
    }

    public void setArtPath(params string[] gameNames)
    {
        loadResOrder.Clear();
        switch (resourceLoadFrom)
        {
            case UiLoadFrom.AssetBundle:
                {
                    for (int i = 0; i < gameNames.Length; ++i)
                    {
                        loadResOrder.Add(gameNames[i].ToLower());
                    }
                    loadResOrder.Add("common");
                }
                break;
            default:
                {
                    for (int i = 0; i < gameNames.Length; ++i)
                    {
                        loadResOrder.Add($"{gameNames[i]}Art");
                    }
                    loadResOrder.Add("ArtCommon");
                }
                break;
        }
        poolsNames.Clear();
    }

    public void setArtPath(string gameName)
    {
        loadResOrder.Clear();
        switch (resourceLoadFrom)
        {
            case UiLoadFrom.AssetBundle:
                {
                    loadResOrder.Add(gameName.ToLower());
                    loadResOrder.Add("common");
                }
                break;
            default:
                {
                    loadResOrder.Add($"{gameName}Art");
                    loadResOrder.Add("ArtCommon");
                }
                break;
        }
        poolsNames.Clear();
    }

    #region Pools
    public Pool initPoolByPrefab(string poolName, GameObject templateObject, int size = 1)
    {
        if (null == templateObject)
        {
            Debug.LogError($"[ResourceManager] Invalide prefab name for pooling : {poolName}");
            throw new NullReferenceException("obj is null");
        }

        Pool pool;
        if (!pools.TryGetValue(poolName, out pool))
        {
            pool = new Pool(poolName, cachedTransform, templateObject, size);
            pools[poolName] = pool;
            poolsNames.Add(poolName);
        }
        return pool;
    }

    Pool initPool(string objPath, int size = 1)
    {
        // 因為目前是把 每個獨立資源打包成 AssetBundle 即 Prefab 名稱就是 AssetBundle Name
        string poolName = Path.GetFileName(objPath);
        GameObject tempObj = getGameObject(objPath);
        return initPoolByPrefab(poolName, tempObj, size);
    }

    Pool initPoolWithResOrder(string objPath, int size = 1, params string[] resNames)
    {
        // 因為目前是把 每個獨立資源打包成 AssetBundle 即 Prefab 名稱就是 AssetBundle Name
        string poolName = Path.GetFileName(objPath);
        GameObject tempObj = getGameObjectWithResOrder(objPath, resNames);
        return initPoolByPrefab(poolName, tempObj, size);
    }

    public bool releasePool(string poolName)
    {
        Pool pool;
        if (pools.TryGetValue(poolName, out pool))
        {
            pool.release();
            pools.Remove(poolName);
            return true;
        }
        return false;
    }

    public bool releasePools(params string[] poolNames)
    {
        Pool pool;
        bool result = true;
        for (int i = 0; i < poolNames.Length; ++i)
        {
            string name = poolNames[i];
            if (pools.TryGetValue(name, out pool))
            {
                pool.release();
                pools.Remove(name);
                continue;
            }
            result = false;
        }

        return result;
    }

    public bool releasePoolWithObj(GameObject tempObj)
    {
        return releasePool(tempObj.name);
    }

    public void clearAllPools()
    {
        var poolsEnum = poolsNames.GetEnumerator();

        while (poolsEnum.MoveNext())
        {
            bool isReleasePoolSuccess = releasePool(poolsEnum.Current);
            if (!isReleasePoolSuccess)
            {
                Debug.Log($"Remove {poolsEnum.Current} is failed");
            }
            //Debug.Log($"Remove {poolsEnum.Current} result? {isReleasePoolSuccess}");
        }

        poolsNames.Clear();
    }

    public PoolObject getObjectFromPool(string objPath, Transform parent = null, int createCount = 1)
    {
        Pool pool;
        string poolName = Path.GetFileName(objPath);
        if (!pools.TryGetValue(poolName, out pool))
        {
            pool = initPool(objPath, createCount);
        }

        return pool.nextAvailableObject(parent);
    }

    public PoolObject getObjectFromPoolWithResOrder(string objPath, Transform parent = null, int createCount = 1, params string[] resNames)
    {
        Pool pool;
        string poolName = Path.GetFileName(objPath);
        if (!pools.TryGetValue(poolName, out pool))
        {
            pool = initPoolWithResOrder(objPath, createCount, resNames);
        }

        return pool.nextAvailableObject(parent);
    }

    public PoolObject getObjectFromPool(GameObject tempObj, Transform parent = null, int createCount = 1)
    {
        Pool pool;
        if (!pools.TryGetValue(tempObj.name, out pool))
        {
            //TODO 這裡要改掉比較好
            //Debug.Log($"Get Obj form Pool {tempObj.name}");
            pool = initPoolByPrefab(tempObj.name, tempObj, createCount);
        }

        return pool.nextAvailableObject(parent);
    }

    public void returnObjectToPool(GameObject go)
    {
        PoolObject poolObject = go.GetComponent<PoolObject>();
        Pool pool;
        if (pools.TryGetValue(poolObject.poolName, out pool))
        {
            pool.returnObjectToPool(poolObject);
        }
        else
        {
            Debug.LogWarning($"No pool available with name: {poolObject.poolName}");
        }
    }
    #endregion

    #region LoadWithResOrder
    public GameObject getGameObjectWithResOrder(string path, params string[] resNames)
    {
        resNames = getResOrder(resNames);

        return loadWithResOrder<GameObject>(path, resNames);
    }

    public T loadWithResOrder<T>(string path, string[] resNames) where T : UnityEngine.Object
    {
        T result = null;
        switch (resourceLoadFrom)
        {
            case UiLoadFrom.AssetBundle:
                {
                    result = loadFromBundleWithResOrder<T>(path, resNames);
                }
                break;

            default:
                {
                    result = loadFromResourceWithResOrder<T>(path, resNames);
                }
                break;
        }
        if (null == result)
        {
            Debug.LogError($"Get {path} is null, LoadFrom {resourceLoadFrom}");
        }
        return result;
    }

    T loadFromBundleWithResOrder<T>(string path, string[] resNames) where T : UnityEngine.Object
    {
        T result = null;
        var assetName = getAssetName(path);
        for (int i = 0; i < resNames.Length; i++)
        {
            result = AssetBundleManager.Instance.getAsset<T>(assetName, resNames[i]);
            if (null != result)
            {
                return result;
            }
        }

        Debug.Log($"loadFromBundleWithResOrder null:{path}");

        return null;
    }

    T loadFromResourceWithResOrder<T>(string path, string[] resNames) where T : UnityEngine.Object
    {
        T result = null;
        path = getResourceLoadPath<T>(path);
#if UNITY_EDITOR
        for (int i = 0; i < resNames.Length; i++)
        {
            result = UnityEditor.AssetDatabase.LoadAssetAtPath<T>($"Assets/AssetBundles/{resNames[i]}/{path}");
            if (null != result)
            {
                break;
            }
        }

#elif SUBMIT && UNITY_IOS
       for (int i = 0; i < loadResOrder.Count; ++i)
       {
            string folderPath = Path.Combine(Application.streamingAssetsPath, loadResOrder[i], path);
            result = Resources.Load<T>(Path.GetFullPath(folderPath));
            if (null != result)
            {
                break;
            }
        }
#endif
        return result;
    }

    public Sprite[] loadAllWithResOrder(string path, params string[] resNames)
    {
        resNames = getResOrder(resNames);

        Sprite[] result = null;
        switch (resourceLoadFrom)
        {
            case UiLoadFrom.AssetBundle:
                {
                    result = loadAllFromBundleWithResOrder<Sprite>(path, resNames);
                }
                break;
            default:
                {
                    result = loadAllFromResourceWithResOrder<Sprite>(path, resNames);
                }
                break;
        }
        return result;
    }

    T[] loadAllFromBundleWithResOrder<T>(string path, string[] resNames) where T : UnityEngine.Object
    {
        T[] result = null;
        var assetName = getAssetName(path);

        for (int i = 0; i < resNames.Length; i++)
        {
            result = AssetBundleManager.Instance.getAllAsset<T>(assetName, resNames[i]);
            if (null != result)
            {
                return result;
            }
        }
        Debug.Log($"loadFromBundle null:{path}");
        return null;
    }

    T[] loadAllFromResourceWithResOrder<T>(string path, string[] resNames) where T : UnityEngine.Object
    {
        List<T> result = new List<T>();
        path = getResourceLoadPath<T>(path);
#if UNITY_EDITOR
        for (int i = 0; i < resNames.Length; i++)
        {
            var objects = UnityEditor.AssetDatabase.LoadAllAssetsAtPath($"Assets/AssetBundles/{resNames[i]}/{path}");

            if (null != objects && objects.Length > 0)
            {
                var allObject = objects.GetEnumerator();
                while (allObject.MoveNext())
                {
                    if (allObject.Current.GetType() != typeof(T))
                    {
                        continue;
                    }
                    result.Add(allObject.Current as T);
                }
            }

            if (result.Count > 0)
            {
                break;
            }
        }
#endif
        return result.ToArray();
    }

    #endregion

    #region LoadWithArtPath
    public GameObject getGameObject(string path)
    {
        return load<GameObject>(path);
    }

    public T load<T>(string path) where T : UnityEngine.Object
    {
        T result = null;
        switch (resourceLoadFrom)
        {
            case UiLoadFrom.AssetBundle:
                {
                    result = loadFromBundle<T>(path);
                }
                break;

            default:
                {
                    result = loadFromResource<T>(path);
                }
                break;
        }
        if (null == result)
        {
            Debug.LogError($"Get {path} is null, LoadFrom {resourceLoadFrom}");
        }
        return result;
    }

    public Sprite[] loadAll(string path)
    {
        Sprite[] result = null;
        switch (resourceLoadFrom)
        {
            case UiLoadFrom.AssetBundle:
                {
                    result = loadAllFromBundle<Sprite>(path);
                }
                break;
            default:
                {
                    result = loadAllFromResource<Sprite>(path);
                }
                break;
        }
        return result;
    }

    public T loadUnBundleData<T>(string path) where T : UnityEngine.Object
    {
        return Resources.Load<T>(path);
    }

    T loadFromResource<T>(string path) where T : UnityEngine.Object
    {
        T result = null;
        path = getResourceLoadPath<T>(path);
#if UNITY_EDITOR
        for (int i = 0; i < loadResOrder.Count; i++)
        {
            result = UnityEditor.AssetDatabase.LoadAssetAtPath<T>($"Assets/AssetBundles/{loadResOrder[i]}/{path}");
            if (null != result)
            {
                break;
            }
        }

#elif SUBMIT && UNITY_IOS
       for (int i = 0; i < loadResOrder.Count; ++i)
       {
            string folderPath = Path.Combine(Application.streamingAssetsPath, loadResOrder[i], path);
            result = Resources.Load<T>(Path.GetFullPath(folderPath));
            if (null != result)
            {
                break;
            }
        }
#endif
        return result;
    }

    T[] loadAllFromResource<T>(string path) where T : UnityEngine.Object
    {
        List<T> result = new List<T>();
        path = getResourceLoadPath<T>(path);
#if UNITY_EDITOR
        for (int i = 0; i < loadResOrder.Count; i++)
        {
            var objects = UnityEditor.AssetDatabase.LoadAllAssetsAtPath($"Assets/AssetBundles/{loadResOrder[i]}/{path}");

            if (null != objects && objects.Length > 0)
            {
                var allObject = objects.GetEnumerator();
                while (allObject.MoveNext())
                {
                    if (allObject.Current.GetType() != typeof(T))
                    {
                        continue;
                    }
                    result.Add(allObject.Current as T);
                }
            }

            if (result.Count > 0)
            {
                break;
            }
        }
#endif
        return result.ToArray();
    }

    string getResourceLoadPath<T>(string path) where T : UnityEngine.Object
    {
        Type type = typeof(T);

        if (type == typeof(Sprite) && path.EndsWith(".jpg"))
        {
            return path;
        }

        string typeValue;
        if (resourceType.TryGetValue(type, out typeValue))
        {
            return $"{path}.{typeValue}";
        }
        return path;
    }

    T loadFromBundle<T>(string path) where T : UnityEngine.Object
    {
        T result = null;
        var assetName = getAssetName(path);
        for (int i = 0; i < loadResOrder.Count; i++)
        {
            result = AssetBundleManager.Instance.getAsset<T>(assetName, loadResOrder[i]);
            if (null != result)
            {
                return result;
            }
        }

        Debug.Log($"loadFromBundle null:{path}");

        return null;
    }

    T[] loadAllFromBundle<T>(string path) where T : UnityEngine.Object
    {
        T[] result = null;
        var assetName = getAssetName(path);

        for (int i = 0; i < loadResOrder.Count; i++)
        {
            result = AssetBundleManager.Instance.getAllAsset<T>(assetName, loadResOrder[i]);
            if (null != result)
            {
                return result;
            }
        }
        Debug.Log($"loadFromBundle null:{path}");
        return null;
    }

    #endregion

    string[] getResOrder(string[] resNames)
    {
        string[] resOrder = new string[] { };
        switch (resourceLoadFrom)
        {
            case UiLoadFrom.AssetBundle:
                {
                    if (null != resNames)
                    {
                        List<string> list = new List<string>(resNames.ToList());
                        list.Add("lobby");
                        list.Add("common");
                        resOrder = list.ToArray();
                    }
                    else
                    {
                        resOrder = new string[] { "lobby", "common" };
                    }
                }
                break;
            default:
                {
                    if (null != resNames)
                    {
                        List<string> list = new List<string>();
                        for (int i = 0; i < resNames.Length; i++)
                        {
                            list.Add($"{resNames[i]}Art");
                        }
                        list.Add("LobbyArt");
                        list.Add("ArtCommon");
                        resOrder = list.ToArray();
                    }
                    else
                    {
                        resOrder = new string[] { "ArtCommon", "LobbyArt" };
                    }
                }
                break;
        }

        return resOrder;

    }

    string getAssetName(string fullPath)
    {
        var path = fullPath.Replace('\\', '/');  //不同作業系統資料夾符號規格統一
        return fullPath.Substring(path.LastIndexOf("/") + 1);
    }

    public UiLoadFrom resourceLoadFrom
    {
        get
        {
#if LOADFROM_AB && !UNITY_EDITOR
            return UiLoadFrom.AssetBundle;
#else
            UiLoadFrom loadFromEnum = PlayerPrefs.HasKey("LoadFrom") ? (UiLoadFrom)PlayerPrefs.GetInt(LoadFromSaveKey) : UiLoadFrom.Resources;
            return loadFromEnum;
#endif
        }
    }
}