using System.Threading.Tasks;
using System.Threading;
using CommonService;
using Services;
using System;
using Debug = UnityLogUtility.Debug;
using UniRx;

namespace Network
{
    public class ServerResponse
    {
        public Result result;
    }

    public class ServerProxy
    {
        public ServerProvider provider { get; private set; }
        IDisposable apiResponseCheck = null;
        readonly int checkInterval = 60;

        public ServerProxy setProvider(ServerProvider serverProvider)
        {
            provider = serverProvider;
            return this;
        }

        public async Task<T> callGameApi<T>(string api, GameRequestBase gameRequest) where T : ServerResponse
        {
            bool isShowMsgBox = true;
            gameRequest.UserID = DataStore.getInstance.playerInfo.userID;
            gameRequest.SessionID = DataStore.getInstance.dataInfo.sessionSid;
            string url = api;
            switch (ApplicationConfig.environment)
            {
                case ApplicationConfig.Environment.Stage:
                case ApplicationConfig.Environment.Prod:
                    string gameID = await DataStore.getInstance.dataInfo.getNowplayGameID();
                    url = $"/{gameID}{api}";
                    break;
            }

            apiResponseCheck = Observable.Timer(TimeSpan.FromSeconds(checkInterval)).Subscribe(_ =>
            {
                Debug.Log("Api no response, back to login !");
                UtilServices.openErrConnectionBox();
            });

            var response = await callApi<T>(url, gameRequest, isShowMsgBox) as T;

            if (Result.OK != response.result)
            {
                OpenMsgBoxService.Instance.openNormalBox(LanguageService.instance.getLanguageValue("Err_System"), 
                    string.Format(LanguageService.instance.getLanguageValue("Err_ErrCodeOnly"), response.result), 
                    UtilServices.reloadLobbyScene);
            }

            apiResponseCheck.Dispose();
            return response;
        }

        public Task<T> callApi<T>(string api, object objectData = null, bool isShowMsgBox = true) where T : ServerResponse
        {
            return callApi<T>(api, objectData, CancellationToken.None, isShowMsgBox);
        }

        public async Task<T> callApi<T>(string api, object objectData, CancellationToken cts, bool isShowMsgBox = true) where T : ServerResponse
        {
            var response = await provider.callApi<T>(api, objectData, cts, isShowMsgBox);
            return parseApi(response);
        }

        public async Task<T> callPatchApi<T>(string api, object objectData, bool isShowMsgBox = true) where T : ServerResponse
        {
            var response = await provider.callPatchApi<T>(api, objectData, CancellationToken.None, isShowMsgBox);
            return parseApi(response);
        }

        public async Task<T> callDeleteApi<T>(string api, object objectData = null, bool isShowMsgBox = true) where T : ServerResponse
        {
            var response = await provider.callDeleteApi<T>(api, objectData, CancellationToken.None, isShowMsgBox);
            return parseApi(response);
        }

        T parseApi<T>(Tuple<Result, T> response) where T : ServerResponse
        {
            if (null == response.Item2)
            {
                Debug.LogWarning($"Get Response Data is null,Result:{(Result)response.Item1} ");
                return (T)new ServerResponse()
                {
                    result = response.Item1,
                };
            }
            response.Item2.result = response.Item1;
            return response.Item2;
        }

        public async Task<T> callApiWithEmptyData<T>(string api, bool isShowMsgBox = true) where T : ServerResponse
        {
            var response = await provider.callApi<T>(api, new byte[] { }, CancellationToken.None, isShowMsgBox);
            if (null == response.Item2)
            {
                Debug.LogWarning($"Get Response Data is null,Result:{(Result)response.Item1} ");
                return (T)new ServerResponse()
                {
                    result = response.Item1,
                };
            }
            response.Item2.result = response.Item1;
            return response.Item2;
        }

        public void httpClientDisconnect()
        {
            provider.httpDisconnect();
        }
    }
}
