using CommonILRuntime.Module;
using UnityEngine.UI;
using System.Collections.Generic;
using System;
using Debug = UnityLogUtility.Debug;

namespace CommonPresenter
{
    class GameBottomBarAutoItemPresenter : NodePresenter
    {
        #region
        Button autoUnlinited;
        Button autoSpeed;
        Button autoTime3Btn;
        Text autoTimes3Txt;
        Button autoTime2Btn;
        Text autoTimes2Txt;
        Button autoTime1Btn;
        Text autoTimes1Txt;
        Button autoTime0Btn;
        Text autoTimes0Txt;
        #endregion

        List<int> autoSpinDatas = new List<int>() { (int)CommonUiConfig.AutoMode.INFINITY, (int)CommonUiConfig.AutoMode.INFINITY_AND_BREAK, 500, 100, 50, 25 };
        Action<int> onAutoItemClick = null;
        AutoData[] autoBtns;

        public override void init()
        {
            setAutoItemData();
        }

        public override void initUIs()
        {
            autoUnlinited = getBtnData("auto_unlimited_btn");
            autoSpeed = getBtnData("auto_speed_btn");
            autoTime3Btn = getBtnData("auto_times3_btn");
            autoTimes3Txt = getTextData("auto_times3_txt");
            autoTime2Btn = getBtnData("auto_times2_btn");
            autoTimes2Txt = getTextData("auto_times2_txt");
            autoTime1Btn = getBtnData("auto_times1_btn");
            autoTimes1Txt = getTextData("auto_times1_txt");
            autoTime0Btn = getBtnData("auto_times0_btn");
            autoTimes0Txt = getTextData("auto_times0_txt");
        }

        public void setAutoItemClick(Action<int> clickCall)
        {
            onAutoItemClick = clickCall;
        }

        void setAutoItemData()
        {
            autoBtns = new AutoData[] {
                new AutoData().initAutoUIs(autoUnlinited),
                new AutoData().initAutoUIs(autoSpeed),
                new AutoData().initAutoUIs(autoTime3Btn, autoTimes3Txt),
                new AutoData().initAutoUIs(autoTime2Btn, autoTimes2Txt),
                new AutoData().initAutoUIs(autoTime1Btn, autoTimes1Txt),
                new AutoData().initAutoUIs(autoTime0Btn, autoTimes0Txt),
            };

            for (int i = 0; i < autoBtns.Length; ++i)
            {
                autoBtns[i].setEventAndData(autoItemClick, autoSpinDatas[i]);
            }
        }
        void autoItemClick(int autoSpinData)
        {
            uiGameObject.setActiveWhenChange(false);
            if (null == onAutoItemClick)
            {
                return;
            }
            onAutoItemClick(autoSpinData);
        }
    }

    class AutoData
    {
        int spinData { get; set; }
        Action<int> dataClickCall = null;

        Text autoCount;

        public AutoData initAutoUIs(Button dataBtn, Text dataText = null)
        {
            autoCount = dataText;
            dataBtn.onClick.AddListener(dataBtnClick);
            return this;
        }

        public AutoData setEventAndData(Action<int> click, int autoSpinData)
        {
            dataClickCall = click;
            spinData = autoSpinData;
            if (null != autoCount)
            {
                autoCount.text = autoSpinData.ToString();
            }
            return this;
        }

        void dataBtnClick()
        {
            if (null != dataClickCall)
            {
                dataClickCall(spinData);
            }
        }
    }
}
