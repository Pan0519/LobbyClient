using CommonILRuntime.Module;
using UnityEngine;
using UnityEngine.UI;
using CommonService;
using System;
using UniRx;
using Services;
using System.Collections.Generic;

namespace CommonPresenter
{
    public class PlayerMoneyPresenter : ContainerPresenter
    {
        public override string objPath => "Prefab/bg_money_common";
        public override UiLayer uiLayer { get => UiLayer.BarRoot; }

        GameObject landscapeGo;
        CustomTextSizeChange landscapeText;
        Button landscapeBtn;
        GameObject portraitGO;
        CustomTextSizeChange portraitText;
        Button portraitBtn;
        Stack<Transform> parentTransStack = new Stack<Transform>();

        public override void initUIs()
        {
            landscapeGo = getGameObjectData("landscape_obj");
            landscapeText = getBindingData<CustomTextSizeChange>("landscape_txt");
            landscapeBtn = getBtnData("landscape_btn");

            portraitGO = getGameObjectData("portrait_obj");
            portraitText = getBindingData<CustomTextSizeChange>("portrait_txt");
            portraitBtn = getBtnData("portrait_btn");
        }
        public override void init()
        {
            landscapeGo.setActiveWhenChange(true);
            portraitGO.setActiveWhenChange(true);

            var playerInfo = DataStore.getInstance.playerInfo;
            updateMoney(playerInfo.playerMoney.ToString("N0"));
            playerInfo.myWallet.subscribeCoinChange(updateMoney).AddTo(uiGameObject);
            landscapeBtn.onClick.AddListener(openShopMain);
            portraitBtn.onClick.AddListener(openShopMain);

            switch (UtilServices.getNowScreenOrientation)
            {
                case ScreenOrientation.Portrait:
                case ScreenOrientation.PortraitUpsideDown:
                    portraitGO.setActiveWhenChange(true);
                    landscapeGo.setActiveWhenChange(false);
                    break;

                default:
                    portraitGO.setActiveWhenChange(false);
                    landscapeGo.setActiveWhenChange(true);
                    break;
            }
        }

        void openShopMain()
        {
            if (parentTransStack.Count > 1)
            {
                return;
            }
            DataStore.getInstance.eventInGameToLobbyService.OpenFuncInLobby(FunctionNo.Shop);
        }

        public void updateMoney(string moneyFormat)
        {
            landscapeText.text = moneyFormat;
            portraitText.text = moneyFormat;
        }

        public void addTo(Transform parentTrans)
        {
            try
            {
                //Debug.Log($"playerMoney addTo {parentTrans.name} , nowParent : {uiTransform.parent.name} ,parentTrans == uiTransform ? {parentTrans == uiTransform}");
                if (parentTrans == uiTransform)
                {
                    return;
                }

                if (null != uiRectTransform.parent)
                {
                    parentTransStack.Push(uiRectTransform.parent);
                }
                setParent(parentTrans);

            }
            catch (Exception e)
            {
                Debug.LogError($"playerMoney addTo Exception Object is null? uiTransform?{null == uiTransform} , uiRectTransform?{null == uiRectTransform}");
                Debug.LogError($"playerMoney addTo {parentTrans} Exception : {e.Message}");
            }
        }

        public void returnToLastParent()
        {
            if (parentTransStack.Count <= 0)
            {
                return;
            }
            Transform lastParent = parentTransStack.Pop();
            setParent(lastParent);
            //Debug.Log($"PlayerMoneyPresenter returnToLastParent : {lastParent.name}");
        }

        void setParent(Transform parentTrans)
        {
            uiRectTransform.SetParent(parentTrans);
            uiRectTransform.localScale = Vector3.one;
            uiRectTransform.anchoredPosition3D = Vector3.zero;
        }
    }
}
