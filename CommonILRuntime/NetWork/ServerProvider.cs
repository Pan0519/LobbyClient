using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using CommonService;
using UnityEngine;

using Debug = UnityLogUtility.Debug;

namespace Network
{
    public class ServerProvider
    {
        HttpClientManager httpClient;
        Dictionary<string, string> headers;
        Dictionary<string, string> patchHeaders;

        Dictionary<string, string> mBaseHeader;
        Dictionary<string, string> baseHeader
        {
            get
            {
                if (null == mBaseHeader)
                {
                    mBaseHeader = new Dictionary<string, string>();
                    mBaseHeader.Add(ApplicationConfig.ContentTypeStr, ApplicationConfig.ApplicationMsgPack);
                    mBaseHeader.Add(ApplicationConfig.UserAgent, userAgent);
                }

                return mBaseHeader;
            }
        }
        string userAgent
        {
            get
            {
                var operatingSystem = SystemInfo.operatingSystem.Replace('(', '<').Replace(')', '>');
                var device = SystemInfo.deviceModel.Replace('(', '<').Replace(')', '>');
                return $"DiamondCrushClient/{ApplicationConfig.AppVersion} ({operatingSystem}; {device}; {Enum.GetName(typeof(ApplicationConfig.APKChannel), ApplicationConfig.apkChannel)}; {ApplicationConfig.nowLanguage.ToString().ToLower()}) r/{ApplicationConfig.bundleVersion}";
            }
        }

        public ServerProvider(string server)
        {
            httpClient = new HttpClientManager(server);
        }
        public ServerProvider(HttpClientManager clientManager)
        {
            httpClient = clientManager;
        }

        public Dictionary<string, string> getHeaders()
        {
            if (null == headers)
            {
                headers = baseHeader;
                headers.Add(ApplicationConfig.ContentLength, string.Empty);
            }

            checkHeaderValue(headers);
            headerAddSid(headers);

            return headers;
        }

        public Dictionary<string, string> getPatchHeaders()
        {
            if (null == patchHeaders)
            {
                patchHeaders = baseHeader;
                patchHeaders.Add(ApplicationConfig.ContentAccept, ApplicationConfig.ApplicationPatchMsgpack);
            }

            headerAddSid(patchHeaders);
            return patchHeaders;
        }

        void headerAddSid(Dictionary<string, string> header)
        {
            if (header.ContainsKey(ApplicationConfig.ContentSid))
            {
                string headerSid = header[ApplicationConfig.ContentSid];
                if (!string.IsNullOrEmpty(DataStore.getInstance.dataInfo.sessionSid) && !DataStore.getInstance.dataInfo.sessionSid.EndsWith(headerSid))
                {
                    header[ApplicationConfig.ContentSid] = $"Bearer {DataStore.getInstance.dataInfo.sessionSid}";
                }
                return;
            }

            header.Add(ApplicationConfig.ContentSid, $"Bearer {DataStore.getInstance.dataInfo.sessionSid}");
        }

        void checkHeaderValue(Dictionary<string, string> header)
        {
            if (ApplicationConfig.NowRuntimePlatform == UnityEngine.RuntimePlatform.WindowsEditor)
            {
                return;
            }
            var baseHeaderEnum = baseHeader.GetEnumerator();
            while (baseHeaderEnum.MoveNext())
            {
                if (!header.ContainsKey(baseHeaderEnum.Current.Key))
                {
                    header.Add(baseHeaderEnum.Current.Key, baseHeaderEnum.Current.Value);
                }
            }
        }

        public Task<Tuple<Result, T>> callApi<T>(string api, object objectData = null, bool isShowMsgBox = true)
        {
            return callApi<T>(api, objectData, CancellationToken.None, isShowMsgBox);
        }

        public Task<Tuple<Result, T>> callApi<T>(string api, object objectData, CancellationToken cts, bool isShowMsgBox = true)
        {
            return callApi<T>(api, objectData, cts, getHeaders(), 0, isShowMsgBox);
        }

        public Task<Tuple<Result, T>> callApi<T>(string api, object objectData, CancellationToken ct, Dictionary<string, string> headers, int numRetry = 0, bool isShowMsgBox = true)
        {
            string json = callApiObjToJson(objectData);

            if (string.IsNullOrEmpty(json))
            {
                Util.Log($"CallApi:{httpClient.host}{api}");
            }
            else
            {
                Util.Log($"CallApi:{httpClient.host}{api},data:{json}");
            }

            return callApi<T>(api, Util.msgPackConvertToBytes(json), ct, headers, numRetry, isShowMsgBox);
        }

        public async Task<Tuple<Result, T>> callApi<T>(string api, byte[] data, CancellationToken ct, Dictionary<string, string> headers, int numRetry = 0, bool isShowMsgBox = true)
        {
            Tuple<int, string> response = await httpClient.sendThreadAsync(api, data, ct, headers);
            return convertToResult<T>(api, response, isShowMsgBox);
        }

        public async Task<Tuple<Result, T>> callPatchApi<T>(string api, object objectData, CancellationToken ct, bool isShowMsgBox = true)
        {
            byte[] patchData = new byte[] { };
            string json = string.Empty;

            if (null != objectData)
            {
                json = callApiObjToJson(objectData);
                patchData = Util.msgPackConvertToBytes(json);
            }
            Debug.Log($"PatchApi:{api},data:{json}");
            Tuple<int, string> response = await httpClient.sendPathcAsync(api, patchData, ct, getPatchHeaders());
            return convertToResult<T>(api, response, isShowMsgBox);
        }

        public async Task<Tuple<Result, T>> callDeleteApi<T>(string api, object objectData, CancellationToken ct, bool isShowMsgBox = true)
        {
            string json = callApiObjToJson(objectData);
            Debug.Log($"Delete Api:{api},data:{json}");
            Tuple<int, string> response = await httpClient.sendDeleteAsync(api, Util.msgPackConvertToBytes(json), ct, getPatchHeaders());
            return convertToResult<T>(api, response, isShowMsgBox);
        }

        Tuple<Result, T> convertToResult<T>(string api, Tuple<int, string> response, bool isShowMsgBox)
        {
            Result result = (Result)response.Item1;

            if (Result.OK != result && Result.GetWagerEmpty != result && isShowMsgBox)
            {
                Debug.LogError($"api {api} result is Error {response.Item1}");
                ShowErrorCodeMsgServices.showErrorMsgBox(result);
                return Tuple.Create(result, default(T));
            }
            Debug.Log($"OnApi:{httpClient.host}{api}, Result: {result}, ResponseJson:{response.Item2}");
            return Tuple.Create(result, LitJson.JsonMapper.ToObject<T>(response.Item2));
        }

        string callApiObjToJson(object objectData)
        {
            string json = Util.toJson(objectData);
            return json;
        }

        public void httpDisconnect()
        {
            httpClient.Dispose();
        }
    }
}
