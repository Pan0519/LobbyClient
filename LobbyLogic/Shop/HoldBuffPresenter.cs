using CommonILRuntime.Module;
using UnityEngine.UI;
using UnityEngine;
using Services;
using System;
using UniRx;
namespace Shop
{
    class HoldBuffPresenter : NodePresenter
    {
        #region UIs
        Image buffImg;
        Text buffTimeTxt;
        Button timeBtn;
        GameObject hintObj;
        #endregion

        TimerService timer = new TimerService();
        public Subject<HoldBuffPresenter> openTimeSub { get; private set; } = new Subject<HoldBuffPresenter>();
        IDisposable openCountdown;
        public override void initUIs()
        {
            buffImg = getImageData("buff_image");
            buffTimeTxt = getTextData("buff_time");
            timeBtn = getBtnData("time_btn");
            hintObj = getGameObjectData("hint_obj");
        }

        public override void init()
        {
            timer.setAddToGo(uiGameObject);
            hintObj.setActiveWhenChange(false);
            timeBtn.onClick.RemoveAllListeners();
            timeBtn.onClick.AddListener(switchBuffTimeActive);
        }

        public void switchBuffTimeActive()
        {
            UtilServices.disposeSubscribes(openCountdown);
            hintObj.setActiveWhenChange(!hintObj.activeInHierarchy);
            openTimeSub.OnNext(hintObj.activeInHierarchy ? this : null);
            if (hintObj.activeInHierarchy)
            {
                openCountdown = Observable.Timer(TimeSpan.FromSeconds(5)).Subscribe(_ =>
                {
                    switchBuffTimeActive();
                }).AddTo(uiGameObject);
            }
        }

        public HoldBuffPresenter openBuff(BuffState buffState, DateTime endTime)
        {
            buffImg.sprite = ShopDataStore.getBuffSprite(buffState);
            timer.StartTimer(endTime, timerCB);
            open();
            return this;
        }

        public void updateBuffTime(DateTime endTime)
        {
            if (null != timer)
            {
                timer.ExecuteTimer();
            }

            timer.StartTimer(endTime, timerCB);
            open();
        }

        void timerCB(TimeSpan boostTime)
        {
            buffTimeTxt.text = UtilServices.formatCountTimeSpan(boostTime);
        }
    }
}
