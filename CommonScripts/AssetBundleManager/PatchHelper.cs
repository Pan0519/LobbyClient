using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace dfBundleTool
{
    public static class PatchHelper
    {
        public static List<string> compareDifference(Dictionary<string, string> serverHash, Dictionary<string, string> localHash)
        {
            List<string> differences = new List<string>();
            foreach (string serverkey in serverHash.Keys)
            {
                string localHashValue;
                // 本地有這檔案
                if (localHash.TryGetValue(serverkey, out localHashValue))
                {
                    // 此檔案與Server不一樣
                    if (!string.Equals(serverHash[serverkey], localHashValue))
                    {
                        differences.Add(serverkey);
                    }
                }
                else
                {
                    differences.Add(serverkey);
                }
            }

            return differences;
        }

        public static Dictionary<string, string> bundleManifestToDictionary(AssetBundleManifest manifest)
        {
            var outDir = new Dictionary<string, string>();
            string[] bundleNames = manifest.GetAllAssetBundles();
            for (int i = 0; i < bundleNames.Length; i++)
            {
                string bundleName = bundleNames[i];
                Hash128 hash = manifest.GetAssetBundleHash(bundleName);
                outDir.Add(bundleName, hash.ToString());
            }
            return outDir;
        }

        public static bool isFileExist(string filePath)
        {
            return File.Exists(filePath);
        }
    }
}
