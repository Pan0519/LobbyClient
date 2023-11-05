using CommonILRuntime.Module;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using Services;
using UniRx;
using UniRx.Triggers;

namespace Event.Common
{
    public class PickItemPresenter : NodePresenter
    {
        Animator itemAnim;
        Button pickupBtn;
        Image picImg;

        public int showIndex { get; private set; }

        Action animFinishCB;
        Action<int> clickAction;
        Action tutorialsAction = null;
        public override void initUIs()
        {
            itemAnim = getAnimatorData("ani_ItemShow");
            pickupBtn = getBtnData("btn_PickUp");
            picImg = getImageData("img_BtnPic");
        }

        public override void init()
        {
            pickupBtn.onClick.AddListener(clickItem);
        }

        public PickItemPresenter setClickEvent(Action<int> callBackEvent)
        {
            clickAction = callBackEvent;
            return this;
        }

        void clickItem()
        {
            if (null != clickAction)
            {
                clickAction(showIndex);
            }

            if (null != tutorialsAction)
            {
                tutorialsAction();
            }
        }

        public PickItemPresenter setInitData(Sprite pic, int itemId)
        {
            itemAnim.enabled = false;
            if (null != pic)
            {
                picImg.sprite = pic;
            }
            showIndex = itemId;
            itemAnim.enabled = true;
            open();
            return this;
        }

        public void setTutorialAction(Action tutorialEvent)
        {
            tutorialsAction = tutorialEvent;
        }

        public void playOpenAnim(Action finishCB, int pickFinishTimeFrame)
        {
            //animFinishCB = finishCB;
            itemAnim.SetTrigger("open");
            Observable.TimerFrame(pickFinishTimeFrame).Subscribe(_ =>
            {
                if (null != finishCB)
                {
                    finishCB();
                }
            });
        }

        public void playShakeAnim()
        {
            itemAnim.SetTrigger("shake");
        }

        public void setImgRaycast(bool enable)
        {
            picImg.raycastTarget = enable;
        }

        public override void close()
        {
            uiRectTransform.localScale = Vector3.one;
            pickupBtn.onClick.RemoveAllListeners();
            base.close();
        }
    }
}
