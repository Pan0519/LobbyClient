using CommonILRuntime.Module;
using UnityEngine;
using UnityEngine.UI;
using System;

namespace Lobby.VIP
{
    public class VipProfitHint : ContainerPresenter
    {
        public override string objPath { get { return "prefab/vip_info/vip_profit_hint"; } }
        public override UiLayer uiLayer { get { return UiLayer.BarRoot; } }

        Button closeTipButton;
        GameObject tipGameObject;
        Text tipText;
        Action closeTipCB;
        public override void initContainerPresenter()
        {
            resOrder = new string[] { AssetBundleData.getBundleName(BundleType.Vip) };
            base.initContainerPresenter();
        }
        public override void initUIs()
        {
            closeTipButton = getBtnData("closeTipButton");
            tipGameObject = getGameObjectData("tipGameObject");
            tipText = getTextData("tipText");
        }

        public override void init()
        {
            base.init();
            closeTipButton.gameObject.setActiveWhenChange(true);    //避免美術編輯誤關
            tipText.gameObject.setActiveWhenChange(true);   //避免美術編輯誤關
            tipGameObject.setActiveWhenChange(true);   //避免美術編輯誤開

            closeTipButton.onClick.AddListener(onCloseTipButtonClick);
        }

        public void setInfoAndPos(string message, GameObject target, Action closeTipCB)
        {
            tipText.text = message;
            uiRectTransform.SetParent(target.transform.parent);
            tipGameObject.transform.position = target.transform.position;
            this.closeTipCB = closeTipCB;
        }

        void onCloseTipButtonClick()
        {
            if (null != closeTipButton)
            {
                closeTipCB();
            }
            clear();
        }
    }
}
