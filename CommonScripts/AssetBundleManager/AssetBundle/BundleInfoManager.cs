using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace AssetBundles
{
    public enum VER_STATE
    {
        Higher,
        Lower,
        Equal,
    }

    public class UpdateBundleInfo
    {
        public string title;
        public string localFilePath;
        public string newUrl;
        public int newBundleVer;
        public uint crc = 0;
        public long fileSize = 0;
    }

    public class BundleInfo
    {
        public string app_ver = "";
        public string title;
        public string type;
        public bool must_update;
        public string bundle_ver;
        public DownloadInfos data;

        public string[] assetPath;
        public string[] assetName;
        public string file_path;
    }

    [Serializable]
    public class DownloadInfos
    {
        public List<DownloadInfo> list;
    }

    [Serializable]
    public class DownloadInfo
    {
        public string bundle_name;
        public string bundle_url;
        public uint crc;
        public long file_size;
    }

    public class BundleInfoManager : MonoBehaviour
    {
        public string CONTENT_HOST = "http://192.168.128.235:8080";
        public string RESOURCE_VER;
        public string LANGUAGE;
        public string BUNDLE_FOLDER = "bundle_table";
        public string BUNDLE_INFO = "bundleInfo";
        public string serverAssetTable = "";

        BundleInfo platformBundleInfo;
        public string platfrom;
        public string appVersion = "";
        public string bunderVersion = "";
        public Dictionary<string, BundleInfo> bundleInfos = new Dictionary<string, BundleInfo>();
        public Dictionary<string, BundleInfo> ilruntimeInfos = new Dictionary<string, BundleInfo>();

        BundleInfoManager lastBundleInfo;
        GameObject lastTableObj = null;

        public void init(string host)
        {
            CONTENT_HOST = host;
            RESOURCE_VER = ApplicationConfig.bundleVersion;
            LANGUAGE = ApplicationConfig.nowLanguage.ToString().ToLower();
            platfrom = ApplicationConfig.platformName.ToLower();
            serverAssetTable = BUNDLE_INFO + "_" + platfrom + "_" + LANGUAGE.ToLower() + ".sc";
            Util.Log($"resourceVersion:{ApplicationConfig.bundleVersion}");
        }

        public BundleInfoManager getLastInfoTable()
        {
            return lastBundleInfo;
        }
        public void createLastBundleInfo()
        {
            if (lastTableObj == null)
            {
                lastTableObj = new GameObject();
                lastTableObj.name = "LastBundleInfoManager";
                lastTableObj.transform.parent = this.transform;
                lastBundleInfo = lastTableObj.AddComponent<BundleInfoManager>();
                lastBundleInfo.RESOURCE_VER = ApplicationConfig.bundleVersion;
                lastBundleInfo.LANGUAGE = ApplicationConfig.nowLanguage.ToString().ToLower();
                lastBundleInfo.platfrom = ApplicationConfig.platformName.ToLower();
                lastBundleInfo.serverAssetTable = lastBundleInfo.BUNDLE_INFO + "_" + ApplicationConfig.platformName.ToLower() + "_" + lastBundleInfo.LANGUAGE.ToLower() + "_Last.sc";
                //lastBundleInfo.setData(lastInfo);
                //File.WriteAllText(lastBundleInfo.serverAssetTable, lastInfo);
            }
            else
            {
                lastBundleInfo.RESOURCE_VER = ApplicationConfig.bundleVersion;
                lastBundleInfo.LANGUAGE = ApplicationConfig.nowLanguage.ToString().ToLower();
                lastBundleInfo.platfrom = ApplicationConfig.platformName.ToLower();
                lastBundleInfo.serverAssetTable = lastBundleInfo.BUNDLE_INFO + "_" + ApplicationConfig.platformName.ToLower() + "_" + lastBundleInfo.LANGUAGE.ToLower() + "_Last.sc";
            }
        }

        public void setData(string strJson)
        {
            //Util.Log($"setData_1_{strJson}");
            string secondJson = "";
            string thirdJson = "";
            string title;
            JObject jo = JObject.Parse(strJson);
            int index = 0;
            bundleInfos.Clear();
            ilruntimeInfos.Clear();

            foreach (var x in jo)
            {
                //第一筆記錄版本資訊
                if (index == 0)
                {
                    //Util.Log($"{x.Key} : {x.Value}");
                    //platfrom = x.Key;
                    secondJson = x.Value.ToString();
                    JObject secondjo = JObject.Parse(secondJson);
                    foreach (var y in secondjo)
                    {
                        //Util.Log($"{y.Key} : {y.Value}");
                        appVersion = y.Key;
                        thirdJson = y.Value.ToString();
                        break;
                    }
                    platformBundleInfo = JsonUtility.FromJson<BundleInfo>(thirdJson);
                    bunderVersion = platformBundleInfo.bundle_ver;
                    //Util.Log($"bundleInfo:{platformBundleInfo.type}:{platformBundleInfo.bundle_ver}:{platformBundleInfo.data.list.Count}");
                    //Util.Log($"downloadInfo:{platformBundleInfo.data.list[0].bundle_name}_{platformBundleInfo.data.list[0].bundle_url}_{platformBundleInfo.data.list[0].crc}");
                }
                else
                {
                    secondJson = x.Value.ToString();
                    JObject secondjo = JObject.Parse(secondJson);
                    string appVer = "";
                    foreach (var y in secondjo)
                    {
                        //Util.Log($"{y.Key} : {y.Value}");
                        appVer = y.Key;
                        thirdJson = y.Value.ToString();
                        break;
                    }
                    //Util.Log($"setData:{x.Key}:{thirdJson}");
                    BundleInfo bundleInfo = JsonUtility.FromJson<BundleInfo>(thirdJson);
                    bundleInfo.title = x.Key;
                    bundleInfo.app_ver = appVer;
                    //Util.Log($"bundleInfo:{bundleInfo.title}:{bundleInfo.type}:{bundleInfo.bundle_ver}:{bundleInfo.data.list.Count}");
                    //Util.Log($"downloadInfo:{bundleInfo.data.list[0].bundle_name}_{bundleInfo.data.list[0].bundle_url}_{bundleInfo.data.list[0].crc}");

                    if (bundleInfo.type.Equals("ilruntime"))
                    {
                        ilruntimeInfos.Add(x.Key.ToString(), bundleInfo);
                    }
                    else
                    {
                        bundleInfos.Add(getMainfestName(bundleInfo), bundleInfo);
                    }
                }
                index++;
            }

        }

        public void downloadTable(Action<bool> resultCallback)
        {
            createLastBundleInfo();
            RESOURCE_VER = ApplicationConfig.bundleVersion;
            LANGUAGE = ApplicationConfig.nowLanguage.ToString().ToLower();
            platfrom = ApplicationConfig.platformName.ToLower();
            serverAssetTable = BUNDLE_INFO + "_" + platfrom + "_" + LANGUAGE.ToLower() + ".sc";
            //Debug.Log($"asset table {serverAssetTable} ,platform : {platfrom}");
            StartCoroutine(downloadServerAssetTable(resultCallback));
            /*
            if (appVersion.Equals(""))
            {

                Util.Log("downloadTable...");
                StartCoroutine(downloadServerAssetTable(resultCallback));
            }
            else
            {
                resultCallback?.Invoke(true);
            }*/
        }

        IEnumerator downloadServerAssetTable(Action<bool> resultCallback)
        {
            //Util.Log("downloadServerAssetTable...");
            string url = getRemoteFilePath(serverAssetTable);
            //Util.Log($"downloadServerAssetTable...{url}");
            using (UnityWebRequest bundleWebRequest = new UnityWebRequest(url))
            {
                bundleWebRequest.downloadHandler = new DownloadHandlerBuffer();
                yield return bundleWebRequest.SendWebRequest();
                if (UnityWebRequest.Result.Success != bundleWebRequest.result)
                {
                    //Util.Log($"get serverAssetTable failed, url: {url}");
                    resultCallback?.Invoke(false);
                    yield break;
                }
                else
                {
                    var text = bundleWebRequest.downloadHandler.text;
                    //Util.Log($"get serverAssetTable success, text: {text}");
                    saveData(resultCallback, text);
                    yield break;
                }
            }
        }

        async void saveData(Action<bool> resultCallback, string text)
        {
            //取得上一個暫存版本
            if (File.Exists(getCachedFilePath(lastBundleInfo.serverAssetTable)))
            {
                var lastData = await FileAsync.ReadAllText(getCachedFilePath(lastBundleInfo.serverAssetTable));
                lastBundleInfo.setData(lastData);

            }
            else if (File.Exists(getCachedFilePath(serverAssetTable)))
            {
                //沒有暫存上一版
                var last = await FileAsync.ReadAllText(getCachedFilePath(serverAssetTable));
                lastBundleInfo.setData(last);
                await FileAsync.WriteAllText(getCachedFilePath(lastBundleInfo.serverAssetTable), last, CancellationToken.None);

            }
            else
            {

            }


            //保存現在的版本info
            await FileAsync.WriteAllText(getCachedFilePath(serverAssetTable), text, CancellationToken.None);
            setData(text);
            //比對上一版，delete異動資源           
            if (!lastBundleInfo.bunderVersion.Equals(""))
            {
                deleteOlderFile();
                //刪除完所有異動資源，就將總表寫到last.sc暫存，提供下次比對
                await FileAsync.WriteAllText(getCachedFilePath(lastBundleInfo.serverAssetTable), text, CancellationToken.None);
            }
            await Task.Delay(TimeSpan.FromSeconds(0.5f));
            resultCallback?.Invoke(true);

        }

        void deleteOlderFile()
        {
            BundleInfo lastInfo = null;
            BundleInfo newInfo = null;
            //刪除舊ilruntime
            foreach (KeyValuePair<string, BundleInfo> pair in ilruntimeInfos)
            {
                newInfo = pair.Value;
                //舊版沒有此KEY，略過
                if (!lastBundleInfo.ilruntimeInfos.ContainsKey(pair.Key)) continue;
                lastInfo = lastBundleInfo.ilruntimeInfos[pair.Key];
                var oldPath = getCachedFilePath($"{lastInfo.data.list[0].bundle_name}/{lastInfo.title}/{lastInfo.bundle_ver}");
                //新舊版本號相等
                //比對bundle號, 新版較高
                if (int.Parse(newInfo.bundle_ver) > int.Parse(lastInfo.bundle_ver))
                {
                    //Util.Log($"delete:{oldPath}");
                    if (Directory.Exists(oldPath))
                    {
                        string[] files = Directory.GetFiles(oldPath);
                        for (int i = 0; i < files.Length; i++)
                        {
                            //Util.Log($"delete file:{files[i]}");
                            File.Delete($"{files[i]}");
                        }
                        //刪除舊資料
                        Directory.Delete(oldPath, true);
                    }
                }
            }

            //刪除舊assetbundle
            foreach (KeyValuePair<string, BundleInfo> pair in bundleInfos)
            {
                newInfo = pair.Value;
                //舊版沒有此KEY，略過
                if (!lastBundleInfo.bundleInfos.ContainsKey(pair.Key)) continue;
                lastInfo = lastBundleInfo.bundleInfos[pair.Key];
                var oldPath = getCachedFilePath($"{lastInfo.data.list[0].bundle_name}/{lastInfo.title}");

                //比對bundle號, 新版較高
                if (int.Parse(newInfo.bundle_ver) > int.Parse(lastInfo.bundle_ver))
                {
                    //Util.Log($"delete:{oldPath}");
                    if (File.Exists(oldPath))
                        File.Delete(oldPath);
                }
            }

            compareAndDeleteOldBundle();
        }

        /// <summary>
        ///  刪除新版總表沒有，但舊版有的資源
        /// </summary>
        void compareAndDeleteOldBundle()
        {
            BundleInfo lastInfo = null;
            //搜尋ilruntime
            foreach (KeyValuePair<string, BundleInfo> pair in lastBundleInfo.ilruntimeInfos)
            {
                lastInfo = pair.Value;
                var oldPath = getCachedFilePath($"{lastInfo.data.list[0].bundle_name}/{lastInfo.title}/{lastInfo.bundle_ver}");
                //新版查無此key，則刪除
                if (!ilruntimeInfos.ContainsKey(pair.Key))
                {
                    if (Directory.Exists(oldPath))
                    {
                        //Util.Log($"compareAndDeleteOldBundle delete:{oldPath}");
                        string[] files = Directory.GetFiles(oldPath);
                        for (int i = 0; i < files.Length; i++)
                        {
                            //Util.Log($"compareAndDeleteOldBundle delete file:{files[i]}");
                            File.Delete($"{files[i]}");
                        }
                        //刪除舊資料
                        Directory.Delete(oldPath, true);
                    }
                }
            }

            //搜尋不存在新表的bundle並刪除
            foreach (KeyValuePair<string, BundleInfo> pair in lastBundleInfo.bundleInfos)
            {
                lastInfo = pair.Value;
                var title = lastInfo.title.Contains("manifest") ? "manifest" : lastInfo.title;
                var oldPath = getCachedFilePath($"{lastInfo.data.list[0].bundle_name}/{title}");
                if (!bundleInfos.ContainsKey(pair.Key))
                {
                    //Util.Log($"compareAndDeleteOldBundle:{oldPath}");
                    if (File.Exists(oldPath))
                        File.Delete(oldPath);
                }
            }
        }

        protected string getRemoteFilePath(string fileName)
        {
#if UNITY_EDITOR && STREAMING_ASSETS
            return $"{Application.streamingAssetsPath}/{platfrom}/{BUNDLE_FOLDER}/{RESOURCE_VER}/{fileName}";
#elif INNER || DEV
            return $"{CONTENT_HOST}/{platfrom}/{BUNDLE_FOLDER}/0.0.0/{fileName}";
#else
            return $"{CONTENT_HOST}/{platfrom}/{BUNDLE_FOLDER}/{RESOURCE_VER}/{fileName}";
#endif
        }

        protected string getCachedFilePath(string fileName)
        {
            return $"{getCachedDirPath()}/{fileName}";
        }
        protected virtual string getCachedDirPath()
        {
            return $"{Application.temporaryCachePath}";
        }

        public VER_STATE versionCompare(string currVer, string lastVer)
        {
            Version v1 = new Version(currVer);
            Version v2 = new Version(lastVer);
            VER_STATE state;
            if (v1 > v2)
            {
                state = VER_STATE.Higher;
            }
            else if (v1 < v2)
            {
                state = VER_STATE.Lower;
            }
            else
            {
                state = VER_STATE.Equal;
            }
            return state;
        }

        public List<BundleInfo> getBundleInfos(string type)
        {
            List<BundleInfo> infos = new List<BundleInfo>();
            BundleInfo bundleInfo = null;
            foreach (KeyValuePair<string, BundleInfo> pair in bundleInfos)
            {
                bundleInfo = pair.Value;
                if (bundleInfo.type.Equals(type))
                {
                    if (bundleInfo.must_update)
                    {
                        infos.Add(bundleInfo);
                    }
                }

            }

            return infos;
        }

        public BundleInfo getBundleInfo(string title)
        {
            if (!bundleInfos.ContainsKey(title)) return null;
            return bundleInfos[title];
        }

        public void setBundleAssets(string title, string[] assets)
        {
            if (!bundleInfos.ContainsKey(title))
            {
                Util.Log($"bundleInfos not contains key:{title} by setBundleAssets");
                return;
            }
            if (assets.Length != 0)
                bundleInfos[title].assetPath = assets;

            string[] assetsName = new string[assets.Length];
            for (int i = 0; i < assets.Length; i++)
            {
                assetsName[i] = Path.GetFileNameWithoutExtension(getAssetName(assets[i]));
            }
            if (assetsName.Length != 0)
                bundleInfos[title].assetName = assetsName;
        }

        public void setSaveFilePath(string title, string filePath)
        {
            if (!bundleInfos.ContainsKey(title))
            {
                Util.Log($"bundleInfos not contains key:{title} by setSaveFilePath");
                return;
            }
            bundleInfos[title].file_path = filePath;
        }

        string getAssetName(string fullPath)
        {
            var path = fullPath.Replace('\\', '/');  //不同作業系統資料夾符號規格統一
            return fullPath.Substring(path.LastIndexOf("/") + 1);
        }

        public string getRealTitle(BundleInfo info)
        {
            string keyName = info.title;
            if (info.title.Contains("manifest"))
            {
                keyName = "manifest";
            }
            return keyName;
        }

        public string getMainfestName(BundleInfo info)
        {
            string keyName = info.title;
            if (info.title.Contains("manifest"))
            {
                if (info.data.list[0].bundle_name.Contains("localization"))
                {
                    keyName = $"{info.type}_localization_manifest";
                }
                else
                {
                    keyName = $"{info.title}";
                }
            }
            return keyName;
        }

    }
}
