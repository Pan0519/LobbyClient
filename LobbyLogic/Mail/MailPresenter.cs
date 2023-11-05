using CommonILRuntime.BindingModule;
using CommonILRuntime.Module;
using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Lobby.Mail
{
    public abstract class MailPresenter : NodePresenter, IMailPresenter
    {
        public Action onGet = null;
        public Action onExpire = null;

        protected IMessage data;
        protected Button getButton;
        protected Action onReaded = null;

        Text remainTimeText;

        DateTime endTime;

        CancellationTokenSource cts = null;

        public override void initUIs()
        {
            getButton = getBtnData("getButton");
            remainTimeText = getTextData("remainTime");
        }

        public override void init()
        {
            remainTimeText.text = string.Empty;
            getButton.onClick.AddListener(onGetClick);
        }

        public void setRemainTime(DateTime endTime)
        {
            this.endTime = endTime;
            countdown();
        }

        public override void close()
        {
            if (null != cts)
            {
                cts.Cancel();
            }
            base.close();
        }

        public GameObject getObj()
        {
            return uiGameObject;
        }

        public void setReadedListener(Action listener)
        {
            onReaded = listener;
        }

        public MailType mailType { get { return data.getType(); } }

        public abstract void setData(IMessage data);

        //已讀
        protected void readed()
        {
            UiManager.clearPresnter(this);
        }

        void onGetClick()
        {
            onGet?.Invoke();
        }

        void timeout()
        {
            onExpire?.Invoke();
            clear();
        }

        async void countdown()
        {
            if (null != cts)
            {
                cts.Cancel();
            }
            cts = new CancellationTokenSource();

            while (true)
            {
                if (cts.IsCancellationRequested)
                {
                    close();
                    break;
                }
                await Task.Delay(TimeSpan.FromSeconds(0.5f));
                var remainTime = endTime - Services.UtilServices.nowTime;
                if (remainTime.TotalSeconds < 0f)
                {
                    Debug.Log($"Timeout, current: {Services.UtilServices.nowTime}, endTime: {endTime}");
                    timeout();
                    break;
                }
                var timeString = timeToString(remainTime);
                remainTimeText.text = timeString;
            }
        }

        string timeToString(TimeSpan remainTime)
        {
            string timeString;

            if (remainTime.Days > 0)
            {
                timeString = $"{remainTime.Days} Days";
            }
            else
            {
                timeString = string.Format("{0:00}:{1:00}:{2:00}", remainTime.Hours, remainTime.Minutes, remainTime.Seconds);
            }

            return timeString;
        }
    }
}
