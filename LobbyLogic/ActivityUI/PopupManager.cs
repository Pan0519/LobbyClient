using LobbyLogic.NetWork.ResponseStruct;
using Network;
using Service;
using Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lobby.Popup
{
    public class PopupManager
    {
        //DateTime refreshTime;
        PopUpFactory pFactory;
        Queue<PopupData> popQueue = new Queue<PopupData>();
        Action finishCallback = null;

        private static PopupManager instance = null;
        public static PopupManager Instance
        {
            get
            {
                if (null == instance)
                {
                    instance = new PopupManager();
                }
                return instance;
            }
        }

        private PopupManager()
        {
            pFactory = new PopUpFactory();
        }

        public async void beginPopups(Action finishCB = null)
        {
            finishCallback = finishCB;
            await syncPopups();
            nextPopup();
        }

        async Task syncPopups()
        {
            var response = await AppManager.lobbyServer.getPopups();
            if (Result.OK == response.result)
            {
                setPopups(response.popups);
            }
        }

        void nextPopup()
        {
            if (popQueue.Count <= 0)
            {
                if (null != finishCallback)
                {
                    finishCallback();
                }
                return;
            }

            var peekData = popQueue.Peek();
            if (peekData.startedAt > UtilServices.nowTime)
            {
                return;
            }
            var data = popQueue.Dequeue();
            var presenter = pFactory.getPopUp(data);
            if (null != presenter)
            {
                presenter.setOnCloseHandler(() =>
               {
                   nextPopup();
               });
                return;
            }

            //可能填錯資料沒辦法產生對應的 prefab, 跳過繼續下一個popup
            nextPopup();
        }

        void setPopups(PopupData[] popups)
        {
            List<PopupData> popupList = new List<PopupData>();
            for (int i = 0; i < popups.Length; i++)
            {
                popupList.Add(popups[i]);
            }
            sort(popupList);
            filterToQueue(popupList);
        }

        void filterToQueue(List<PopupData> activities)
        {
            popQueue.Clear();
            for (int i = 0; i < activities.Count; i++)
            {
                var data = activities[i];
                if (data.popup)
                {
                    popQueue.Enqueue(data);
                }
            }
        }

        void sort(List<PopupData> popups)
        {
            popups.Sort((PopupData x, PopupData y) =>
           {
               var xStarted = x.startedAt < UtilServices.nowTime;
               var yStarted = y.startedAt < UtilServices.nowTime;

               //若有沒開放的活動，用start time 排序
               if (!xStarted || !yStarted)
               {
                   if (x.startedAt < y.startedAt)
                   {
                       return -1;
                   }
               }

               //皆已開放的活動用權重,結束時間排序
               if (xStarted && yStarted)
               {
                   if (x.priority > y.priority)
                   {
                       return -1;
                   }

                   if (x.endedAt < y.endedAt)
                   {
                       return -1;
                   }
               }

               return 0;
           });
        }
    }
}
