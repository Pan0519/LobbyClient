using System.Collections.Generic;
using UnityEngine;
using System;
using AssetBundles;
namespace dfBundleTool
{
    public class LanguageBundleProvider : BundleProvider
    {

        protected override string getRemoteFilePath(string fileName)
        {
#if  UNITY_EDITOR && STREAMING_ASSETS
            var fileDir = $"{Application.streamingAssetsPath}";
            return $"{fileDir}{fileName}";
#else
            var fileDir = $"{contentHost}/";
            return $"{fileDir}/{fileName}";
#endif 
        }

        protected override string getCachedDirPath()
        {
            return $"{Application.temporaryCachePath}";
        }

        /*
        protected override string getCachedFilePath(string fileName)
        {
            var fileDir = $"{getCachedDirPath()}/_localization/{currentLanguage}/{dirName}";
            Util.Log($"local lang path: {fileDir}");
            BundleHelper.createDirectoryIfNotExist(fileDir);
            return $"{fileDir}/{fileName}";
        }
        */
        protected override List<AssetBundleDownloadCommand> filterDownloadList(string dir)
        {
            List<AssetBundleDownloadCommand> outList = new List<AssetBundleDownloadCommand>();
            var inputList = AssetBundleManager.Instance.bundleInfoMng.getBundleInfos(dir);
            AssetBundleDownloadCommand abCmd;
            foreach (var bundleInfo in inputList)
            {
                if (bundleInfo.title.Contains("manifest")) continue;
                //只下載Provider自己的Bundle與語言bundle
                if (bundleInfo.title.Contains("_localization"))
                {
                    abCmd = new AssetBundleDownloadCommand();
                    abCmd.bundleInfo = bundleInfo;
                    outList.Add(abCmd);
                }
            }

            var manifestName = $"{dirName}_localization_manifest";
            manifestBundleInfo = AssetBundleManager.Instance.bundleInfoMng.getBundleInfo(manifestName);
            if (manifestBundleInfo == null) manifestBundleInfo = AssetBundleManager.Instance.bundleInfoMng.getBundleInfo("common_manifest");
            return outList;
        }
    }
}
