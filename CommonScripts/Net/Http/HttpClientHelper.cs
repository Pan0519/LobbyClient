using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using Unity.IO.Compression;
using System;
using MessagePack;
using UnityEngine.SceneManagement;

[MessagePackObject(keyAsPropertyName: true)]
public class MsgPackResponse
{
    public int result;
    public string errorId;
    public object data;
}

public static class HttpClientHelper
{
    public static async Task<Tuple<int, string>> sendThreadAsync(this HttpClientManager httpClient, string api, byte[] data, CancellationToken ct, Dictionary<string, string> headers, int numRetry = 0)
    {
        try
        {
            return await Task.Run(async () =>
            {
                byte[] requestData = compressDataAsync(data, headers);
                using (var response = await httpClient.sendAsync(api, ct, requestData, headers))
                {
                    byte[] responseData = await HttpClientManager.getByteArrayAsync(response);
                    return parseResponseData(responseData);
                }
            });
        }
        catch (Exception ex)
        {
            string url = $"{httpClient.host}{api}";
            if (ex is OperationCanceledException)
            {
                Debug.Log($"OperationCancled Exception : {url}");
            }
            else if (ex is TimeoutException)
            {
                Debug.LogError($"Send http request Exception,Url: {url}\nerror : {ex.Message}");
                showDisconnectionMsgBox();
            }
            else
            {
                Debug.LogError($"Send http request Exception,Url: {url}\nerror : {ex.Message}");
            }
            throw;
        }
    }

    public static async Task<Tuple<int, string>> sendPathcAsync(this HttpClientManager httpClient, string api, byte[] data, CancellationToken ct, Dictionary<string, string> headers, int numRetry = 0)
    {
        try
        {
            return await Task.Run(async () =>
            {
                byte[] requestData = compressDataAsync(data, headers);

                using (var response = await httpClient.patchAsync(api, ct, requestData, headers))
                {
                    byte[] responseData = await HttpClientManager.getByteArrayAsync(response);
                    return parseResponseData(responseData);
                }
            });
        }
        catch (Exception ex)
        {
            string url = $"{httpClient.host}{api}";
            if (ex is OperationCanceledException)
            {
                Debug.Log($"OperationCancled Exception : {url}");
            }
            else
            {
                Debug.LogError($"sendPathcAsync request Exception,Url: {url}, error : {ex.Message}");
                showDisconnectionMsgBox();
            }
            throw;
        }
    }

    public static async Task<Tuple<int, string>> sendDeleteAsync(this HttpClientManager httpClient, string api, byte[] data, CancellationToken ct, Dictionary<string, string> headers, int numRetry = 0)
    {
        try
        {
            return await Task.Run(async () =>
            {
                byte[] requestData = compressDataAsync(data, headers);

                using (var response = await httpClient.deleteAsync(api, ct, requestData, headers))
                {
                    byte[] responseData = await HttpClientManager.getByteArrayAsync(response);
                    return parseResponseData(responseData);
                }
            });
        }
        catch (Exception ex)
        {
            string url = $"{httpClient.host}{api}";
            if (ex is OperationCanceledException)
            {
                Debug.Log($"OperationCancled Exception : {url}");
            }
            else
            {
                Debug.LogError($"sendDeleteAsync request Exception,Url: {url}, error : {ex.Message}");
                showDisconnectionMsgBox();
            }
            throw;
        }
    }

    static Tuple<int, string> parseResponseData(byte[] data)
    {
        MsgPackResponse serverResponse = Util.msgPackDeserialResponse<MsgPackResponse>(data);
        if (serverResponse.result != 0)
        {
            Debug.LogError($"Response Result Is Error errorId? {serverResponse.errorId}");
        }
        if (null == serverResponse)
        {
            return Tuple.Create(-1, string.Empty);
        }

        if (null == serverResponse.data)
        {
            return Tuple.Create(serverResponse.result, string.Empty);
        }

        if (serverResponse.data.GetType().isType<byte[]>())
        {
            return Tuple.Create(serverResponse.result, Util.msgpackToJsonStr((byte[])serverResponse.data) ?? string.Empty);
        }
        return Tuple.Create(serverResponse.result, Util.toJson(serverResponse.data) ?? string.Empty);
    }

    public static byte[] compressDataAsync(byte[] data, Dictionary<string, string> headers)
    {
        if (null == data)
        {
            return data;
        }

        //string contentType = string.Empty;
        if (null != headers && headers.ContainsKey(ApplicationConfig.ContentLength))
        {
            headers[ApplicationConfig.ContentLength] = data.Length.ToString();
        }

        return data;
    }

    public static async Task<byte[]> decompressDataAsync(byte[] data, Dictionary<string, string> headers)
    {
        if (null == data)
        {
            return data;
        }

        string contentType = "application/json";
        if (null != headers)
        {
            headers.TryGetValue(ApplicationConfig.ContentTypeStr, out contentType);
        }

        switch (contentType)
        {
            case "application/json-gzip":
                {
                    using (var memoryStream = new MemoryStream(data))
                    {
                        using (var gZipStream = new GZipStream(memoryStream, CompressionMode.Decompress))
                        {
                            using (var outputStream = new MemoryStream())
                            {
                                await gZipStream.CopyToAsync(outputStream);
                                return outputStream.ToArray();
                            }
                        }
                    }
                }
            case "application/json":
            case "application/msgpack":
                {
                    return data;
                }

            default:
                {
                    throw new InvalidOperationException($"Must provide a  {contentType} Content-Type!");
                }
        }
    }

    static void showDisconnectionMsgBox()
    {
        DefaultMsgBox.Instance.getMsgBox()
            .setNormalTitle(LanguageService.instance.getLanguageValue("Err_Connection"))
            .setNormalContent(LanguageService.instance.getLanguageValue("Err_CheckLoginAgain"))
            .setNormalCB(ApplicationConfig.reloadLobbyScene)
            .openNormalBox(ApplicationConfig.nowLanguage.ToString().ToLower());
    }
}
