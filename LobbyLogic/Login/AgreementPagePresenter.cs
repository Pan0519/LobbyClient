using CommonPresenter;
using CommonILRuntime.Module;
using CommonILRuntime.BindingModule;
using UnityEngine.UI;
using UnityEngine;
using Lobby.Common;
using Lobby;
using System;

namespace LobbyLogic.Login
{
    public class AgreementPagePresenter : ContainerPresenter
    {
        public override string objPath => "prefab/lobby_login/agreement_page";

        public override UiLayer uiLayer { get => UiLayer.GameMessage; }

        public Action agreenCB;
        Button agreenBtn;
        Button rejectBtn;

        AgreemntLanguageNode enNode;
        AgreemntLanguageNode zhNode;


        public override void initUIs()
        {
            agreenBtn = getBtnData("agreen_btn");
            rejectBtn = getBtnData("reject_btn");
            enNode = UiManager.bindNode<AgreemntLanguageNode>(getNodeData("en_obj_node").cachedGameObject);
            zhNode = UiManager.bindNode<AgreemntLanguageNode>(getNodeData("zh_obj_node").cachedGameObject);
        }

        public override void init()
        {
            enNode.close();
            zhNode.close();


            rejectBtn.onClick.AddListener(Util.ApplicationQuit);
            agreenBtn.onClick.AddListener(agreenClick);
        }

        public override void open()
        {
            switch (ApplicationConfig.nowLanguage)
            {
                case ApplicationConfig.Language.ZH:
                    zhNode.open();
                    break;
                default:
                    enNode.open();
                    break;
            }
        }

        void agreenClick()
        {
            if (null != agreenCB)
            {
                agreenCB();
            }
            clear();
        }
    }

    class AgreemntLanguageNode : NodePresenter
    {
        Button serviceBtn;
        Button privacyBtn;

        public override void initUIs()
        {
            serviceBtn = getBtnData("service_btn");
            privacyBtn = getBtnData("privacy_btn");
        }

        public override void init()
        {
            serviceBtn.onClick.AddListener(openServicePage);
            privacyBtn.onClick.AddListener(openPrivacyPage);
        }

        void openPrivacyPage()
        {
            UiManager.getPresenter<TermPresenter>().openTermWindow(TermContent.Privacy);
        }

        void openServicePage()
        {
            UiManager.getPresenter<TermPresenter>().openTermWindow(TermContent.Terms);
        }
    }
}
