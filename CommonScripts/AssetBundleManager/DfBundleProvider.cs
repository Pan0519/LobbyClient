using System;
using UniRx;
using UnityEngine;

namespace dfBundleTool
{
    public class DfBundleProvider : IBundleProvider
    {
        ResourceBundleProvider resProvider = null;
        LanguageBundleProvider langProvider = null;
        Subject<float> patchProgress = new Subject<float>();
        Subject<int> downloadFileCount = new Subject<int>();

        public void init(string contentHost, string dirName, string manifestName = "manifest")
        {
            //resProvider
            GameObject resProviderObj = new GameObject();
            resProviderObj.name = $"ResProvider_{dirName}";
            resProvider = resProviderObj.AddComponent<ResourceBundleProvider>();
            resProvider.subscribeProgress(onResProgress);
            resProvider.init(contentHost, dirName);


            GameObject langProviderObj = new GameObject();
            langProviderObj.name = $"LangProvider_{dirName}";
            langProvider = langProviderObj.AddComponent<LanguageBundleProvider>();
            langProvider.subscribeProgress(onLangProgress);
            langProvider.init(contentHost, dirName);
        }

        public void patch(System.Action<bool> onResult)
        {
            resProvider.patch((res) =>
            {
                if (res)
                {
                    langProvider.patch(onResult);
                }
                else
                {
                    onResult(res);
                }
            });
        }

        public IDisposable subscribeProgress(Action<float> progressHandler)
        {
            return patchProgress.Subscribe(progressHandler);
        }

        public IDisposable subscribePatchFileCount(Action<long> fileCountHandler)
        {
            return resProvider.subscribePatchFileCount(fileCountHandler);
        }

        public void unloadBundles()
        {
            resProvider.unloadBundles();
            langProvider.unloadBundles();
        }

        public T getAssetFromBundle<T>(string bundleName) where T : UnityEngine.Object
        {
            if (bundleName.Contains("localization"))
            {
                return langProvider.getAssetFromBundle<T>(bundleName);
            }
            else
            {
                return resProvider.getAssetFromBundle<T>(bundleName);
            }
        }

        public T[] getAllAssetFromBundle<T>(string bundleName) where T : UnityEngine.Object
        {
            if (bundleName.Contains("localization"))
            {
                return langProvider.getAllAssetFromBundle<T>(bundleName);
            }
            else
            {
                return resProvider.getAllAssetFromBundle<T>(bundleName);
            }
        }

        public AssetBundle loadBundleWithDependency(string bundleName)
        {
            if (bundleName.Contains("localization"))
            {
                return langProvider.loadBundleWithDependency(bundleName);
            }
            else
            {
                return resProvider.loadBundleWithDependency(bundleName);
            }
        }

        public int getBundleCount()
        {
            return langProvider.getBundleCount() + resProvider.getBundleCount();
        }

        //DfBundleProvider 先 Patch Resource, 再 Patch 語系，取7:3分配進度
        void onResProgress(float progress)
        {
            progress *= 0.7f;
            patchProgress.OnNext(progress);
        }

        void onLangProgress(float progress)
        {
            progress *= 0.3f;
            progress += 0.7f;
            patchProgress.OnNext(progress);
        }

        public void cancelBundleDownload()
        {
            resProvider.cancelBundleDownload();
            langProvider.cancelBundleDownload();

        }
    }
}
