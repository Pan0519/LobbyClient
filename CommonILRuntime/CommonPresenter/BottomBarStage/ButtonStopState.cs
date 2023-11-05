using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace CommonPresenter.BottomBarStage
{
    public class ButtonStopState : ButtonState
    {
        GameObject normalStateObj;
        GameObject autoStateObj;

        public ButtonStopState(CustomBtn btn, Action clickHandler, Action longPressHandler) : base(btn, clickHandler, longPressHandler) { }

        public void setStopBtnStateObjs(GameObject normalStateObj, GameObject autoStateObj)
        {
            this.normalStateObj = normalStateObj;
            this.autoStateObj = autoStateObj;
        }

        public override void enterState(bool isAutoState)
        {
            base.enterState(isAutoState);
            normalStateObj.setActiveWhenChange(!isAutoState);
            autoStateObj.setActiveWhenChange(isAutoState);
            spinBtn.gameObject.setActiveWhenChange(true);
        }

        public override void leaveState()
        {
            base.leaveState();
            spinBtn.gameObject.setActiveWhenChange(false);
        }
    }
}
