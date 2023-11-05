using UnityEngine.Networking;
using System.Threading.Tasks;
using UnityEngine;
using System.IO;

public class WebRequestText : MonoSingleton<WebRequestText>
{
    public async Task<string> loadText(string path)
    {
        string result = string.Empty;
        using (UnityWebRequest www = UnityWebRequest.Get(path))
        {
            await www.SendWebRequest();

            switch (www.result)
            {
                case UnityWebRequest.Result.Success:
                    result = www.downloadHandler.text;
                    break;

                default:
                    Debug.LogError($"Get {www.url} Text is Error, Result is {www.result}");
                    break;
            }
        }

        return result;
    }

    public async Task<string> loadTextFromServer(string fileName, string specifyServer = "")
    {
        string result;
//#if UNITY_IOS
//        result = await loadStreamingJsonFile($"JsonFile/{fileName}");
//#else
        string host = (string.IsNullOrEmpty(specifyServer)) ? ApplicationConfig.CONTENT_HOST : specifyServer;
        result = await loadText($"{host}/jsonfile/{fileName}.json");
//#endif

        return result;
    }

    public async Task<string> loadStreamingJsonFile(string fileName)
    {
        return await loadText(Path.Combine(ApplicationConfig.getStreamingPath, $"{fileName}.json"));
    }
}
