using System;
using UniRx;
using UnityEngine;
using Debug = UnityLogUtility.Debug;

namespace Services
{
    public class TimerService
    {
        public IDisposable disposable
        {
            get
            {
                return (timerUpdate == null) ? Disposable.Empty : timerUpdate;
            }
        }

        GameObject addToGO = null;

        public void setAddToGo(GameObject addGO)
        {
            addToGO = addGO;
        }

        private Action<TimeSpan> callBack;
        IDisposable timerUpdate;

        private DateTime endTime;

        public void StartTimeByTimestamp(long endTimestamp, Action<TimeSpan> callBackAct)
        {
            DateTimeOffset timeOffset = DateTimeOffset.FromUnixTimeSeconds(endTimestamp);

            StartTimer(timeOffset.DateTime, callBackAct);
        }

        public void StartTimer(TimeSpan targetTime, Action<TimeSpan> callBackAct)
        {
            StartTimer(UtilServices.nowTime.Add(targetTime), callBackAct);
        }

        public void StartTimer(DateTime endDateTime, Action<TimeSpan> callBackAct)
        {
            if (endDateTime.Subtract(UtilServices.nowTime).TotalSeconds <= 0)
            {
                return;
            }
            callBack = callBackAct;
            endTime = endDateTime;
            timerUpdate = Observable.Timer(TimeSpan.Zero, TimeSpan.FromSeconds(1)).Subscribe(_ =>
            {
                PushTime((int)endTime.Subtract(UtilServices.nowTime).TotalSeconds);
            });

            if (null != addToGO)
            {
                timerUpdate.AddTo(addToGO);
            }
        }

        public void ExecuteTimer()
        {
            if (timerUpdate == null)
            {
                return;
            }

            timerUpdate.Dispose();
            timerUpdate = null;
            callBack = null;
        }

        private void PushTime(int nowValue)
        {
            callBack(TimeSpan.FromSeconds(nowValue));
            if (nowValue <= 0)
            {
                ExecuteTimer();
            }
        }
    }
}
