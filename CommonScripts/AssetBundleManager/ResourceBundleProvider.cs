using System.Collections.Generic;
using UnityEngine;
using AssetBundles;

namespace dfBundleTool
{
    public class ResourceBundleProvider : BundleProvider
    {
        protected override string getRemoteFilePath(string fileName)
        {
#if UNITY_EDITOR && STREAMING_ASSETS
            return $"{Application.streamingAssetsPath}{fileName}";
#else
            return $"{contentHost}{fileName}";
#endif
        }

        protected override string getCachedDirPath()
        {
            return $"{Application.temporaryCachePath}";
        }

        /*
        protected override string getCachedFilePath(string fileName)
        {
            var fileDir = $"{getCachedDirPath()}/_res/{dirName}";
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
                if (bundleInfo.title.Contains("localization")) continue;
                abCmd = new AssetBundleDownloadCommand();
                abCmd.bundleInfo = bundleInfo;
                outList.Add(abCmd);
            }

            var m_name = dir.Contains("common") || dir.Contains("lobby") ? dirName : $"{dirName}_game";
            var manifestName = $"{m_name}_manifest";
            manifestBundleInfo = AssetBundleManager.Instance.bundleInfoMng.getBundleInfo(manifestName);
            //Util.Log($"1_filterDownloadList:DIR-{dir}--manifestName-{manifestName},is null {manifestBundleInfo == null}");
            if(manifestBundleInfo == null) manifestBundleInfo = AssetBundleManager.Instance.bundleInfoMng.getBundleInfo("common_manifest");
            //Util.Log($"2_filterDownloadList:DIR-{dir}--manifestName-{manifestName},is null {manifestBundleInfo == null}");

            return outList;
        }
    }
}
