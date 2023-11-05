using System.IO;
using UnityEngine;

namespace dfBundleTool
{
    public class CacheHelper
    {
        public CacheHelper()
        {
            cleanDefaultCache();
        }

        //清理預設路徑下的 AssetBundle (沒下載完、下載不完全的Bundle會被快取在此)
        void cleanDefaultCache()
        {
            Cache defaultCache = Caching.GetCacheAt(0);
            bool cleanSuccess = defaultCache.ClearCache();
            if (!cleanSuccess)
            {
                Debug.LogWarning("Cleanup default cache failed");
            }
        }

        //創建&索引指定的Bundle目錄
        //dirName: game name or lobby
        public bool pushCurrentCache(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            Cache newCache = Caching.AddCache(path);

            //make sure cache is valid
            if (newCache.valid)
            {
                Caching.currentCacheForWriting = newCache;
            }
            else
            {
                Debug.LogWarning($"cache path is invalid: {path}");
                return false;
            }
            return true;
        }

        //改變當下資源快取索引目錄，回到前一個
        public void popCurrentCache()
        {
            if (Caching.cacheCount > 0)
            {
                int cacheIdx = Caching.cacheCount - 1;
                bool removeSuccess = Caching.RemoveCache(Caching.GetCacheAt(cacheIdx));
                if (removeSuccess)
                {
                    Caching.currentCacheForWriting = Caching.GetCacheAt(cacheIdx - 1);
                }
            }
            else
            {
                Debug.Log($"popCurrentCache failed: {Caching.cacheCount}, path: {Caching.currentCacheForWriting.path}");
            }
        }
    }
}
