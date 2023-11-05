using System;
using UniRx;
using UnityEngine;

public interface IBundleProvider
{
    void init(string contentHost, string dirName, string manifestName = "manifest");
    void patch(Action<bool> onResult);
    IDisposable subscribeProgress(Action<float> progressHandler);

    IDisposable subscribePatchFileCount(Action<long> fileCountHandler);

    public void cancelBundleDownload();
    void unloadBundles();
    AssetBundle loadBundleWithDependency(string bundleName);
    T getAssetFromBundle<T>(string bundleName) where T : UnityEngine.Object;
    T[] getAllAssetFromBundle<T>(string bundleName) where T : UnityEngine.Object;

    int getBundleCount();
}
