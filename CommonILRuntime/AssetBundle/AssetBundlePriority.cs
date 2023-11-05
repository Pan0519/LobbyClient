using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UniRx;

namespace CommonService
{
    public enum Priority
    {
        High = 0,
        Normal,
        Low,
        Max
    }

    public class AssetBundlePriority
    {
        static AssetBundlePriority _instance = null;

        public static AssetBundlePriority getInstance
        {
            get
            {
                if (null == _instance)
                {
                    dict_priority = new Dictionary<Priority, List<AssetQueue>>();
                    dict_priority.Add(Priority.High, new List<AssetQueue>());
                    dict_priority.Add(Priority.Normal, new List<AssetQueue>());
                    dict_priority.Add(Priority.Low, new List<AssetQueue>());
                    _instance = new AssetBundlePriority();
                }
                return _instance;
            }
            set
            {
                _instance = value;
            }
        }

        static Dictionary<Priority, List<AssetQueue>> dict_priority = new Dictionary<Priority, List<AssetQueue>>();
        static List<AssetQueue> list_loadMemory = new List<AssetQueue>(0);
        int activeDownload = 0;
        const int maxActiveDownload = 1;

        int alreadyDownload = 0;
        public void addQueued(string key ,Image image, Priority priority , Action<bool> resultCallback = null)
        {
            AssetQueue assetQueued = new AssetQueue();
            assetQueued.keyName = key;
            assetQueued.image = image;
            assetQueued.priority = priority;
            assetQueued.totalFileSize = AssetBundleManager.Instance.getFileSizeByType(key);
            //Util.Log($"addQueued:key_{key}__filesize:{assetQueued.totalFileSize}...");
            if (assetQueued.totalFileSize > 0)
            {
                image.fillAmount = 0;
                assetQueued.resultCallback = res =>
                {
                    deQueued(assetQueued);
                    resultCallback(res);
                };
                dict_priority[priority].Add(assetQueued);

                if (activeDownload <= 0)
                {
                    activeDownload++;
                    assetQueued.startDownload();
                }
            }
            else
            {
                image.fillAmount = 1;
                assetQueued.resultCallback = res =>
                {
                    int index = list_loadMemory.FindIndex(x => x.keyName == assetQueued.keyName);
                    list_loadMemory.RemoveAt(index);
                    alreadyDownload--;
                    if (list_loadMemory.Count > 0)
                    {
                        alreadyDownload++;
                        list_loadMemory[0].startDownload();
                    }

                    resultCallback(res);
                };
                list_loadMemory.Add(assetQueued);
                if (alreadyDownload <= 0)
                {
                    alreadyDownload++;
                    assetQueued.startDownload();
                }
            }
        }

        public void deQueued(AssetQueue queue)
        {
            int index = dict_priority[queue.priority].FindIndex(x => x.keyName == queue.keyName);
            Util.Log($"deQueued_name:{queue.keyName}__priority:{queue.priority}__active:{activeDownload}__index:{index}");
            if(-1 != index)
                dict_priority[queue.priority].RemoveAt(index);
            activeDownload--;
            if (dict_priority[queue.priority].Count > 0 && activeDownload < maxActiveDownload)
            {
                activeDownload++;
                dict_priority[queue.priority][0].startDownload();
            }
        }

        public void clearAllQueued()
        {
            dict_priority.Clear();
        }
    }

    public class AssetQueue
    {
        public string keyName = "";
        public Image image = null;
        public Priority priority = Priority.Normal;
        public long totalFileSize;
        public float progress = 0f;
        public Action<bool> resultCallback;
        public IDisposable _disposable = null;
        public void startDownload()
        {
            if (totalFileSize > 0)
            {
                fakeDefaultLoading(0, 0.8f, 3f);

                AssetBundleManager.Instance.preloadBundles(keyName, (preloadRes) =>
                {
                    Util.Log($"startDownload.end:{preloadRes}");
                    if (preloadRes)
                    {
                        continueLoading();
                    }

                }, bundleLoadProgress);
            }
            else
            {
                AssetBundleManager.Instance.preloadBundles(keyName, (preloadRes) =>
                {
                    //Util.Log($"alreadyDownload.end:{preloadRes}");
                    if (preloadRes)
                    {
                        resultCallback?.Invoke(true);
                    }

                }, bundleLoadProgress);
            }
        }

        public void bundleLoadProgress(float progress)
        {

        }

        public void fakeDefaultLoading(float startValue, float endValue, float time, Action callback = null)
        {
            float actTime = time;
            float startTime = Time.time;
            float nowTime = 0;
            progress = 0f;
            float stepValue = endValue - startValue;
            _disposable = Observable.EveryUpdate().Subscribe(_ =>
            {
                nowTime = Time.time - startTime;

                if (nowTime >= actTime)
                {
                    _disposable.Dispose();
                    progress = endValue;
                    callback?.Invoke();
                    //Util.Log("fakeDefaultLoading is dispose...");
                }
                else
                {
                    progress = startValue + ((stepValue) * nowTime / actTime);
                    //Util.Log($"fakeDefaultLoading:{progress}");
                }

                image.fillAmount = progress;
            });

        }

        public void continueLoading()
        {
            if(null != _disposable)
                _disposable.Dispose();
            float actTime = 0.5f;
            float startTime = Time.time;
            float nowTime = 0;
            float startValue = progress;
            float stepValue = 1f - startValue;
            _disposable = Observable.EveryUpdate().Subscribe(_ =>
            {
                nowTime = Time.time - startTime;

                if (nowTime >= actTime)
                {
                    _disposable.Dispose();
                    progress = 1f;
                    resultCallback?.Invoke(true);
                    //Util.Log("fakeDefaultLoading is dispose...");
                }
                else
                {
                    progress = startValue + ((stepValue) * nowTime / actTime);
                    //Util.Log($"fakeDefaultLoading:{progress}");
                }

                image.fillAmount = progress;
            });
        }

    }
}
