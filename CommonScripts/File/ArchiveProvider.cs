using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using AssetBundles;

public class ArchiveProvider : Singleton<ArchiveProvider>
{
    string streamingAsssetsPath { get { return ApplicationConfig.getStreamingPath; } }
    bool isInitialed = false;

    public void init()
    {
        if (isInitialed)
        {
            return;
        }

        isInitialed = true;
    }

    public string getFilePathWithStreamingAsset(string key)
    {
        return $"{streamingAsssetsPath}/{key}";
    }

    #region Async

    public Task<byte[]> loadFileWithFullPathAsync(string path)
    {
        return loadFileWithFullPathAsync(path, CancellationToken.None);
    }


    public async Task<byte[]> loadFileWithFullPathAsync(string path, CancellationToken cancellationToken)
    {
        using (var request = UnityWebRequest.Get(path))
        {
            await request.SendWebRequest();

            if (cancellationToken.IsCancellationRequested)
            {
                return null;
            }

            string error = request.error;

            if (!string.IsNullOrEmpty(error))
            {
                //Util.Log($"LoadFileAsync: {path} error , Msg : {error}");
                return null;
            }
            //Util.Log($"LoadFileAsync: {path} error , Msg : {error}");
            return request.downloadHandler.data;
        }
    }

    #endregion
}


