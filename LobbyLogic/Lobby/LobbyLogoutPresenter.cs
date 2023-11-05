using System;
using System.Collections.Generic;
using CommonILRuntime.Module;
using CommonPresenter;
using UnityEngine.UI;
using UnityEngine;
using CommonILRuntime.BindingModule;
using Lobby.UI;
using Service;
using Network;
using Services;
using CommonService;

namespace Lobby
{
    class LobbyLogoutPresenter : SystemUIBasePresenter
    {
        public override string objPath => "prefab/lobby_login/log_out";
        public override UiLayer uiLayer { get => UiLayer.GameMessage; }
        List<LogoutDeleteToNextNode> logoutNodes = new List<LogoutDeleteToNextNode>();
        LogoutFirstNodePresenter firstNodePresenter;
        int nowOpenPageID = 1;
        public override void init()
        {
            base.init();
            firstNodePresenter = UiManager.bindNode<LogoutFirstNodePresenter>(getNodeData("page_1_node").cachedGameObject);
            firstNodePresenter.cancelAction = closeBtnClick;
            firstNodePresenter.deleteAction = openNextNode;
            logoutNodes.Add(firstNodePresenter);

            var inputNode = UiManager.bindNode<LogoutInputIDNode>(getNodeData("page_2_node").cachedGameObject);
            logoutNodes.Add(inputNode);

            var thridPageNode = UiManager.bindNode<LogoutDeleteToNextNode>(getNodeData("page_3_node").cachedGameObject);
            logoutNodes.Add(thridPageNode);

            for (int i = 0; i < logoutNodes.Count; ++i)
            {
                var deleteToNextNode = logoutNodes[i];
                deleteToNextNode.cancelAction = closeBtnClick;
                deleteToNextNode.deleteAction = openNextNode;
                deleteToNextNode.close();
            }
        }

        public void startOpenPage()
        {
            if (!ApplicationConfig.isiOSSimplify)
            {
                firstNodePresenter.open();
                return;
            }
            openNextNode();
        }

        void openNextNode()
        {
            nowOpenPageID++;
            if (nowOpenPageID > logoutNodes.Count)
            {
                //closePresenter();
                returnToLobbyClick();
                return;
            }
            for (int i = 0; i < logoutNodes.Count; ++i)
            {
                if (i + 1 == nowOpenPageID)
                {
                    logoutNodes[i].open();
                    continue;
                }
                logoutNodes[i].close();
            }
        }
        async void returnToLobbyClick()
        {
            BindingLoadingPage.instance.open();
            Common.KeepAliveManager.Instance.stopSendKeepAlive();
            var deleteSessionResponse = await AppManager.lobbyServer.deleteSession();

            if (Result.OK != deleteSessionResponse.result)
            {
                BindingLoadingPage.instance.close();
                return;
            }

            FirebaseService.logout();
            BindingLoadingPage.instance.close();
            UtilServices.reloadLobbyScene();
        }

        public override void animOut()
        {
            clear();
        }
    }

    class LogoutDeleteToNextNode : LogoutBaseNodePresenter
    {
        Button deleteBtn;
        public Action deleteAction;

        public override void initUIs()
        {
            base.initUIs();
            deleteBtn = getBtnData("delete_btn");
        }
        public override void init()
        {
            base.init();
            deleteBtn.onClick.AddListener(deleteClick);
        }
        public virtual void deleteClick()
        {
            if (null != deleteAction)
            {
                deleteAction();
            }
        }

        public void setDeleteBtnInteractable(bool able)
        {
            deleteBtn.interactable = able;
        }
    }
    class LockAccountNode : LogoutBaseNodePresenter
    {
        Button loginBtn;
        public override void initUIs()
        {
            base.initUIs();
            loginBtn = getBtnData("return_login_btn");
        }
        public override void init()
        {
            base.init();
        }
    }

    class LogoutFirstNodePresenter : LogoutDeleteToNextNode
    {
        Animator unDeleteAnim;
        public override void initUIs()
        {
            base.initUIs();
            unDeleteAnim = getAnimatorData("undelete_tip_anim");
        }

        public override void init()
        {
            unDeleteAnim.gameObject.setActiveWhenChange(false);
            base.init();
        }

        public override void deleteClick()
        {
            var createDays = DateTime.Now.Subtract(DataStore.getInstance.playerInfo.createTime).TotalDays;

            if (createDays <= 14)
            {
                unDeleteAnim.gameObject.setActiveWhenChange(true);
                return;
            }

            base.deleteClick();
        }
    }

    class LogoutInputIDNode : LogoutDeleteToNextNode
    {
        InputField idInput;
        public override void initUIs()
        {
            base.initUIs();
            idInput = getBindingData<InputField>("id_input");
        }

        public override void init()
        {
            base.init();
            idInput.placeholder.GetComponent<Text>().text = LanguageService.instance.getLanguageValue("DeleteAccount_Message2_InputBox");
            idInput.onValueChanged.AddListener(idInputValueChanged);
        }

        public override void open()
        {
            setDeleteBtnInteractable(false);
            base.open();
        }

        private void idInputValueChanged(string val)
        {
            if (string.IsNullOrEmpty(val))
            {
                setDeleteBtnInteractable(false);
                return;
            }

            setDeleteBtnInteractable(val.Equals(DataStore.getInstance.playerInfo.userID));
        }
    }

    class LogoutBaseNodePresenter : NodePresenter
    {
        Button cancelBtn;
        public Action cancelAction;
        public override void initUIs()
        {
            cancelBtn = getBtnData("cancel_btn");
        }
        public override void init()
        {
            cancelBtn.onClick.AddListener(cancelClick);

        }
        void cancelClick()
        {
            if (null != cancelAction)
            {
                cancelAction();
            }
        }
    }
}
