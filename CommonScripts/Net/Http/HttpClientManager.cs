using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.Net.Http;
using System.Net.Http.Headers;
using UnityEngine;
using System.Net;

public class HttpClientManager : IDisposable
{
    public string host { get; private set; }

    const float defaultTimeoutSeconds = 30f;

    HttpClient wwwClient;
    bool headerInsert = false;
    bool disposed { get; set; } = false;

    public HttpClientManager(string host, Dictionary<string, string> headers = null, bool keepAlive = true, float timeoutSecond = 60f)
    {
        if (string.IsNullOrEmpty(host))
        {
            throw new NullReferenceException("Must provide a host");
        }

        this.host = host;

        wwwClient = new HttpClient();
        wwwClient.BaseAddress = new Uri(this.host);
        wwwClient.Timeout = TimeSpan.FromSeconds(timeoutSecond);

        //if (keepAlive)
        //{
        //    wwwClient.DefaultRequestHeaders.ConnectionClose = false;
        //    wwwClient.DefaultRequestHeaders.Connection.Add("Keep-Alive");
        //}
        Util.Log($"HttpClientManager Host {this.host}");
        updateHeaders(headers);
    }
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(false);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposed)
        {
            return;
        }

        disposed = true;

        if (disposing && null != wwwClient)
        {
            wwwClient.Dispose();
            wwwClient = null;
        }
    }

    void updateHeaders(Dictionary<string, string> headers)
    {
        if (null == headers)
        {
            return;
        }

        fillHttpRequestMessageHeaders(wwwClient.DefaultRequestHeaders, headers);
    }

    void fillHttpRequestMessageHeaders(HttpRequestHeaders httpHeaders, Dictionary<string, string> headers)
    {
        if (null == httpHeaders || null == headers)
        {
            return;
        }

        if (headerInsert)
        {
            Thread.Sleep(10);
        }

        try
        {
            headerInsert = true;
            var headersEnum = headers.GetEnumerator();
            while (headersEnum.MoveNext())
            {
                httpHeaders.TryAddWithoutValidation(headersEnum.Current.Key, headersEnum.Current.Value);
            }
        }
        finally
        {
            headerInsert = false;
        }
    }

    public Task<HttpResponseMessage> sendAsync(string api, CancellationToken ct, byte[] data = null, Dictionary<string, string> headers = null)
    {
        try
        {
            if (null == data)
            {
                return getAsync(api, ct, headers);
            }
            return postAsync(api, ct, data, headers);
        }
        catch (WebException webException)
        {
            Debug.LogError($"HttpClien Send WebException : {webException.Message} , status : {webException.Status}");
        }
        catch (Exception e)
        {
            Debug.LogError($"HttpClien Send Exception : {e.Message}");
        }
        return null;
    }

    //public Task<HttpResponseMessage> getAsync(string api, Dictionary<string, string> headers = null, float timeoutSeconds = defaultTimeoutSeconds)
    //{
    //    CancellationTokenSource source = new CancellationTokenSource();
    //    source.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));
    //    return getAsync(api, source.Token, headers);
    //}

    public async Task<HttpResponseMessage> getAsync(string api, CancellationToken ct, Dictionary<string, string> headers = null)
    {
        return await sendAsyncWithoutData(HttpMethod.Get, api, ct, headers);
    }

    public async Task<HttpResponseMessage> postAsync(string api, CancellationToken ct, byte[] data, Dictionary<string, string> headers = null)
    {
        return await sendAsync(HttpMethod.Post, api, ct, data, headers);
    }

    public async Task<HttpResponseMessage> patchAsync(string api, CancellationToken ct, byte[] data, Dictionary<string, string> headers = null)
    {
        return await sendAsync(new HttpMethod("PATCH"), api, ct, data, headers);
    }

    public async Task<HttpResponseMessage> deleteAsync(string api, CancellationToken ct, byte[] data, Dictionary<string, string> headers = null)
    {
        if (null == data)
        {
            return await sendAsyncWithoutData(HttpMethod.Delete, api, ct, headers);
        }
        return await sendAsync(HttpMethod.Delete, api, ct, data, headers);
    }

    async Task<HttpResponseMessage> sendAsyncWithoutData(HttpMethod httpMethod, string api, CancellationToken ct, Dictionary<string, string> headers)
    {
        using (HttpRequestMessage httpRequestMessage = new HttpRequestMessage(httpMethod, api))
        {
            fillHttpRequestMessageHeaders(httpRequestMessage.Headers, headers);

            var response = await wwwClient.SendAsync(httpRequestMessage, ct);
            response.EnsureSuccessStatusCode();
            return response;
        }
    }
    async Task<HttpResponseMessage> sendAsync(HttpMethod httpMethod, string api, CancellationToken ct, byte[] data, Dictionary<string, string> headers)
    {
        if (null == data)
        {
            throw new InvalidOperationException($"Http {httpMethod}: data can't be null");
        }

        using (HttpRequestMessage requestMessage = new HttpRequestMessage(httpMethod, api))
        {
            using (requestMessage.Content = new ByteArrayContent(data))
            {
                if (null != headers)
                {
                    fillHttpRequestMessageHeaders(requestMessage.Headers, headers);

                    string contentTypeValue;

                    if (!headers.TryGetValue(ApplicationConfig.ContentTypeStr, out contentTypeValue))
                    {
                        contentTypeValue = ApplicationConfig.ApplicationMsgPack;
                    }

                    requestMessage.Content.Headers.ContentType = new MediaTypeHeaderValue(contentTypeValue);
                }

                var response = await wwwClient.SendAsync(requestMessage, ct);
                response.EnsureSuccessStatusCode();
                return response;
            }
        }
    }

    #region StaticMethod
    public static Dictionary<string, string> getResponseHeaders(HttpResponseMessage responseMessage)
    {
        checkHttpResponseMessageOperate(responseMessage);

        var headers = new Dictionary<string, string>();

        foreach (var pair in responseMessage.Content.Headers)
        {
            headers.Add(pair.Key, String.Join(";", pair.Value));
        }

        return headers;
    }
    public static Task<byte[]> getByteArrayAsync(HttpResponseMessage httpResponseMessage)
    {
        checkHttpResponseMessageOperate(httpResponseMessage);

        return httpResponseMessage.Content.ReadAsByteArrayAsync();
    }

    public static Task<string> getStringAsync(HttpResponseMessage httpResponseMessage)
    {
        checkHttpResponseMessageOperate(httpResponseMessage);

        return httpResponseMessage.Content.ReadAsStringAsync();
    }

    public static Task<Stream> getSteamAsync(HttpResponseMessage httpResponseMessage)
    {
        checkHttpResponseMessageOperate(httpResponseMessage);

        return httpResponseMessage.Content.ReadAsStreamAsync();
    }

    static void checkHttpResponseMessageOperate(HttpResponseMessage responseMessage)
    {
        if (null == responseMessage)
        {
            throw new InvalidOperationException("HttpResponseMessage is null");
        }
    }

    #endregion
}
