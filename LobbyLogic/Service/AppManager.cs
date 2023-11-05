using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using LobbyLogic.NetWork;
using Network;
using CommonService;
using LitJson;
using LobbyLogic.Common;

namespace Service
{
    public static class AppManager
    {
        public static LobbyServer lobbyServer { get; private set; }
        public static EventServer eventServer { get; private set; }

        public static async Task initAsync()
        {
            try
            {
                List<Task> initTasks = new List<Task>();
                initTasks.Add(FirebaseService.initFirebase());

                initTasks.Add(Task.Run(() =>
                {
                    if (null == lobbyServer)
                    {
                        ServerProxy serverProxy = new ServerProxy().setProvider(new ServerProvider(LobbyHttpClient.Instance.httpClient));
                        lobbyServer = new LobbyServer().setServerProxy(serverProxy);
                    }

                    if (null == eventServer)
                    {
                        ServerProxy eventServerProxy = new ServerProxy().setProvider(new ServerProvider(EventServerIP));
                        eventServer = new EventServer().setServerProxy(eventServerProxy);
                    }
                }));

                await Task.WhenAll(initTasks);
                await DataStore.getInstance.dataInfo.initGameInfos();
                initAreaCodeData();
                LanguageManager.checkLanguage();
            }
            catch (NullReferenceException e)
            {
                Debug.LogError($"AppManager initAsync NullReferenceException {e.Message}");
            }
            catch (Exception e)
            {
                Debug.LogError($"AppManager initAsync exception {e.Message}");
            }
        }

        static async void initAreaCodeData()
        {
            string jsonFile = await WebRequestText.instance.loadTextFromServer("area_code");
            DataStore.getInstance.dataInfo.setAreaCode(JsonMapper.ToObject<Area_codes>(jsonFile));
        }

        public static string EventServerIP
        {
            get
            {
                switch (ApplicationConfig.environment)
                {
                    case ApplicationConfig.Environment.Prod:
                        return "http://as.diamondcrush.com.tw";
                    case ApplicationConfig.Environment.Stage:
                        return "http://as.stg.diamondcrush.com.tw";
                    case ApplicationConfig.Environment.Outer:
                        return "http://34.126.159.186:6005";
                    case ApplicationConfig.Environment.Inner:
                        return "http://34.80.106.119:6005";
                    default:
                        return "http://34.80.106.119:6006";
                }
            }
        }
    }
}
