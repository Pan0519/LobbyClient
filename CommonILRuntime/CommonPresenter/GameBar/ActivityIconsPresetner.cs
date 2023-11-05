using UnityEngine;
using UniRx;
using System;
using Services;
using UnityEngine.UI;
using CommonService;
using System.Collections.Generic;
using CommonILRuntime.Module;
using CommonILRuntime.BindingModule;

namespace CommonPresenter
{
    class ActivityIconsPresetner : ContainerPresenter
    {
        public override string objPath => "prefab/game/function_ingame";
        public override UiLayer uiLayer { get => UiLayer.BarRoot; }
        GameObject openInGameObj;
        GameObject closeInGameObj;
        Button ingameBtn;
        RectTransform rootRect;

        float openInGameX;
        float closeInGameX;
        float maxOpenX;
        float btnWidth;
        List<IconNodePresenter> iconNodes = new List<IconNodePresenter>();
        bool isOpen = false;
        bool isMoving;
        const float moveTime = 0.3f;
        public override void initUIs()
        {
            openInGameObj = getGameObjectData("open_ingame_obj");
            closeInGameObj = getGameObjectData("close_ingame_obj");
            rootRect = getRectData("root_rect");
            ingameBtn = getBtnData("ingame_btn");
            var vaultNode = UiManager.bindNode<VaultNodePresenter>(getNodeData("vault_node").cachedGameObject);
            vaultNode.getRootRect(rootRect);
            iconNodes.Add(vaultNode);
        }
        public override void init()
        {
            setInitPos();
            DataStore.getInstance.highVaultData.isShowVault.Subscribe(inGameShow).AddTo(uiGameObject);
            btnWidth = ingameBtn.gameObject.GetComponent<RectTransform>().rect.width;
            ingameBtn.onClick.AddListener(inGameBtnClick);
            closeInGameObj.setActiveWhenChange(isOpen);
            openInGameObj.setActiveWhenChange(!isOpen);
        }

        void setInitPos()
        {
            var changePos = uiRectTransform.anchoredPosition3D;
            float posY = UtilServices.getNowScreenOrientation == ScreenOrientation.Portrait ? 168 : 100;
            float posX = changePos.x;
            if (UtilServices.screenProportion <= 0.45f)
            {
                posX -= 60;
                uiRectTransform.anchoredPosition3D = changePos;
            }
            changePos.Set(posX, posY, changePos.z);
            uiRectTransform.anchoredPosition3D = changePos;
            closeInGameX = uiRectTransform.anchoredPosition.x;
            maxOpenX = closeInGameX - uiRectTransform.rect.width;
        }

        void inGameBtnClick()
        {
            if (isMoving)
            {
                return;
            }
            isOpen = !isOpen;
            if (isOpen)
            {
                openInGameGroup();
                return;
            }

            closeInGameGroup();
        }

        void closeInGameGroup()
        {
            if (uiRectTransform.anchoredPosition.x == closeInGameX)
            {
                return;
            }
            isMoving = true;
            uiRectTransform.anchPosMoveX(closeInGameX, moveTime, onComplete: setInGameObj);
        }

        void openInGameGroup()
        {
            float moveNodesX = uiRectTransform.anchoredPosition.x;
            for (int i = 0; i < iconNodes.Count; ++i)
            {
                var node = iconNodes[i];
                if (!node.isOpening)
                {
                    continue;
                }
                moveNodesX -= (node.uiRectTransform.rect.width + 10);
            }
            openInGameX = Mathf.Max(moveNodesX, maxOpenX);
            isMoving = true;
            uiRectTransform.anchPosMoveX(openInGameX, moveTime, onComplete: setInGameObj);
        }

        void setInGameObj()
        {
            isMoving = false;
            closeInGameObj.setActiveWhenChange(isOpen);
            openInGameObj.setActiveWhenChange(!isOpen);
        }

        void inGameShow(bool isShow)
        {
            uiRectTransform.gameObject.setActiveWhenChange(isShow);
        }
    }

    class IconNodePresenter : NodePresenter
    {
        public bool isOpening { get; private set; } = false;
        public void setIsOpening(bool opening)
        {
            isOpening = opening;
        }
    }

    class VaultNodePresenter : IconNodePresenter
    {
        GameObject vaultTimeObj;
        GameObject vaultLockObj;
        Text vaultTimeTxt;
        Animator vaultTipAnim;
        Text vaultReturnPayTxt;
        Button vaultBtn;

        TimerService vaultTimeService = new TimerService();
        bool isVaultOpening = false;
        TimeStruct openTimeStruct;
        RectTransform parentRootRect;
        public override void initUIs()
        {
            vaultLockObj = getGameObjectData("vault_lock_obj");
            vaultTimeObj = getGameObjectData("vault_time_obj");
            vaultTimeTxt = getTextData("vault_time_txt");
            vaultTipAnim = getAnimatorData("coin_back_anim");
            vaultReturnPayTxt = getTextData("coin_back_txt");
            vaultBtn = getBtnData("vault_btn");
        }
        public override void init()
        {
            DataStore.getInstance.highVaultData.vaultReturnToPaySub.Subscribe(updateVaultReturnToPay).AddTo(uiGameObject);
            DataStore.getInstance.highVaultData.getVaultDataSub.Subscribe(getVaultData).AddTo(uiGameObject);
            DataStore.getInstance.highVaultData.isShowVault.Subscribe(isVaultShow).AddTo(uiGameObject);

            vaultBtn.onClick.AddListener(vaultBtnClick);
            closeVaultTip();
        }

        public void getRootRect(RectTransform rootRect)
        {
            parentRootRect = rootRect;
        }

        void getVaultData(VaultData vaultData)
        {
            vaultTimeService.ExecuteTimer();
            DateTime expireAtTime = UtilServices.strConvertToDateTime(vaultData.expireTime, DateTime.MinValue);
            CompareTimeResult compareTimeResult = UtilServices.compareTimeWithNow(expireAtTime);
            isVaultOpening = CompareTimeResult.Later == compareTimeResult;
            vaultLockObj.setActiveWhenChange(!isVaultOpening);
            vaultTimeObj.setActiveWhenChange(isVaultOpening);
            vaultBtn.interactable = isVaultOpening;
            vaultReturnPayTxt.text = vaultData.returnToPay.ToString("N0");
            if (isVaultOpening)
            {
                vaultTimeService.setAddToGo(uiGameObject);
                vaultTimeService.StartTimer(expireAtTime, updateVaultTimeTxt);
            }
        }

        void updateVaultReturnToPay(ulong returnToPay)
        {
            if (vaultBtn.interactable)
            {
                return;
            }
            vaultReturnPayTxt.text = returnToPay.ToString("N0");
            vaultTipAnim.gameObject.transform.SetParent(parentRootRect);
            vaultTipAnim.gameObject.setActiveWhenChange(true);
            Observable.Timer(TimeSpan.FromSeconds(5.0f)).Subscribe(_ =>
            {
                closeVaultTip();
            });
        }

        void updateVaultTimeTxt(TimeSpan vaultTime)
        {
            if (vaultTime <= TimeSpan.Zero)
            {
                vaultTimeService.ExecuteTimer();
                vaultLockObj.setActiveWhenChange(true);
                vaultBtn.interactable = false;
                return;
            }
            if (null != openTimeStruct)
            {
                openTimeStruct = UtilServices.updateTimeStruct(vaultTime, openTimeStruct);
            }
            else
            {
                openTimeStruct = UtilServices.toTimeStruct(vaultTime);
            }
            vaultTimeTxt.text = openTimeStruct.toTimeString(LanguageService.instance.getLanguageValue("Time_Days"));
        }

        void isVaultShow(bool isShow)
        {
            setIsOpening(isShow);
            if (isShow)
            {
                open();
                return;
            }
            close();
        }

        void vaultBtnClick()
        {
            vaultBtn.interactable = false;
            if (isVaultOpening)
            {
                DataStore.getInstance.eventInGameToLobbyService.OpenFuncInLobby(FunctionNo.UpdateRollerVaultPay);
                return;
            }
            DataStore.getInstance.eventInGameToLobbyService.OpenFuncInLobby(FunctionNo.HighRollerVault);
            vaultBtn.interactable = true;
        }

        void closeVaultTip()
        {
            vaultTipAnim.SetTrigger("close");
            Observable.TimerFrame(25).Subscribe(_ =>
            {
                vaultTipAnim.gameObject.transform.SetParent(uiTransform);
                vaultTipAnim.gameObject.setActiveWhenChange(false);
                vaultBtn.interactable = true;
            }).AddTo(uiGameObject);
        }

    }
}
