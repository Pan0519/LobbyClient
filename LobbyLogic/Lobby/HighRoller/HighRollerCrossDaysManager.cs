using UnityEngine;
using System;
using UniRx;
using Service;

namespace HighRoller
{
    public class HighRollerCrossDaysManager
    {
        static HighRollerCrossDaysManager _instance = new HighRollerCrossDaysManager();
        public static HighRollerCrossDaysManager getInstance
        {
            get
            {
                return _instance;
            }
        }

        public HighRollerCrossDaysManager()
        {
            crossDaysObj = new GameObject("HighRollerCorssDayObj");
            DontDestroyRoot.addChild(crossDaysObj.transform);
        }

        GameObject crossDaysObj;
        bool isRunning;
        IDisposable returnToPayDis;

        public void crossDaysReturnPayStartRun()
        {
            if (isRunning)
            {
                return;
            }

            isRunning = true;
            DateTime nextDay = DateTime.Now.AddDays(1).Date;
            var triggerMin = nextDay.Subtract(DateTime.Now).TotalMinutes;
            returnToPayDis = Observable.Timer(TimeSpan.FromMinutes(triggerMin)).Subscribe(_ =>
            {
                sendReturnToPay();
            }).AddTo(crossDaysObj);
        }

        async void sendReturnToPay()
        {
            if (null != returnToPayDis)
            {
                returnToPayDis.Dispose();
            }
            isRunning = false;
            await AppManager.lobbyServer.sendReturnToPay();
            crossDaysReturnPayStartRun();
        }
    }
}
