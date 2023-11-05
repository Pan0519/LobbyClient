using CommonILRuntime.Module;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using System;
using Services;
using Lobby.Common;
using EventActivity;
using Debug = UnityLogUtility.Debug;

namespace Event.Common
{
    public class BoosterNodePresenter : NodePresenter
    {
        #region UIs
        Text timerTxt;
        Button openShopBtn;
        Image iconImg;
        Image maskImg;
        #endregion

        TimerService timerService = new TimerService();
        Transform targetTrans = null;

        public override void initUIs()
        {
            timerTxt = getTextData("booster_timer_txt");
            openShopBtn = getBtnData("booster_btn");
            iconImg = getImageData("icon_img");
            //maskImg = iconImg.transform.GetChild(0).GetComponent<Image>();
            maskImg = getImageData("mask_img");
        }

        public override void init()
        {
            timerService.setAddToGo(uiGameObject);
            openShopBtn.onClick.AddListener(openBoosterShop);
        }

        public virtual void openBoosterShop()
        {

        }

        public virtual void timeExpire()
        {

        }

        public virtual void startCountdownTime()
        {

        }

        public BoosterNodePresenter setIconImg(BoosterType boosterType)
        {
            string spriteName = ActivityDataStore.getBoosterSpriteName(boosterType);
            if (string.IsNullOrEmpty(spriteName))
            {
                return this;
            }
            iconImg.sprite = getSprite($"activity_{spriteName}_small");
            maskImg.sprite = getSprite($"mask_{spriteName}_small");
            return this;
        }

        Sprite getSprite(string name)
        {
            return LobbySpriteProvider.instance.getSprite<EventActivitySpriteProvider>(LobbySpriteType.EventActivity, name);
        }
        public void updateTimerTxt(long endTime)
        {
            long nowTime = UtilServices.nowUtcTimeSeconds;
            timerService.ExecuteTimer();

            if (endTime <= 0 || nowTime > endTime)
            {
                timeExpire();
                timerTxt.text = TimeSpan.Zero.ToString();
                return;
            }
            startCountdownTime();
            timerService.StartTimeByTimestamp(endTime, updateTimer);
        }

        public void updateTimesTxt(long times)
        {
            timerTxt.text = times.ToString();
        }

        void updateTimer(TimeSpan time)
        {
            if (time <= TimeSpan.Zero)
            {
                updateTimerTxt(TimeSpan.Zero.Ticks);
                return;
            }

            timerTxt.text = UtilServices.formatCountTimeSpan(time);
        }

        public Transform getBoosterTrans()
        {
            if (null == targetTrans)
            {
                targetTrans = openShopBtn.GetComponent<Transform>();
            }

            return targetTrans;
        }
    }

}
