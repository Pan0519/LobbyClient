using UniRx;
using System;
using Service;
using Network;
using Services;
using Debug = UnityLogUtility.Debug;

namespace Lobby.Common
{
    public class KeepAliveManager
    {
        public static KeepAliveManager Instance { get { return _instance; } }
        static KeepAliveManager _instance = new KeepAliveManager();

        IDisposable keepAliveDisposable = null;
        IDisposable keepAliveCheck = null;
        DateTime lastDateTime;

        readonly int checkInterval = 10;
        readonly int checkMaxTimes = 3;
        int checkTimes = 0;

        public Subject<bool> isCrossDay = new Subject<bool>();
        public void sendKeepAlive()
        {
            if (null != keepAliveDisposable)
            {
                return;
            }
            lastDateTime = DateTime.MinValue;
            keepAliveDisposable = Observable.Interval(TimeSpan.FromSeconds(checkInterval)).Subscribe(sendServerKeepAlive);
        }
       
        async void sendServerKeepAlive(long time)
        {
            keepAliveCheck = Observable.Timer(TimeSpan.FromSeconds(checkInterval)).Subscribe(checkIsKeepAlive);
            var response = await AppManager.lobbyServer.sendKeepAlive();
            keepAliveCheck.Dispose();
            if (Result.OK != response.result)
            {
                checkIsKeepAlive();
            }
            else
            {
                checkTimes = 0;
            }
            var nowDate = UtilServices.strConvertToDateTime(response.date, DateTime.MinValue);
            if (UtilServices.compareTimes(lastDateTime, nowDate) == CompareTimeResult.Earlier)
            {
                isCrossDay.OnNext(true);
            }

            lastDateTime = nowDate;
        }

        /// <summary>
        /// 判斷是否回大廳重連
        /// </summary>
        private void checkIsKeepAlive(long time = 0)
        {
            checkTimes++;
            Debug.Log("Max Check Times : " + checkMaxTimes + " ,Current Check Times : " + checkTimes);

            if (checkTimes >= checkMaxTimes)
            {
                Debug.Log("Operate time out, back to lobby !");
                stopSendKeepAlive();
                UtilServices.openErrConnectionBox();
                checkTimes = 0;
            }
        }

        public void stopSendKeepAlive()
        {
            if (null == keepAliveDisposable)
            {
                return;
            }

            keepAliveDisposable.Dispose();
            keepAliveDisposable = null;
        }
    }
}
