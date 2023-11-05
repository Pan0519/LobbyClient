using Debug = UnityLogUtility.Debug;
using System;
using System.Collections.Generic;
using Binding;
using CommonILRuntime.Module;
using CommonILRuntime.BindingModule;
using Event.Common;
using Lobby.Mail;
using Lobby.Jigsaw;
using Lobby.VIP;
using LoginReward;
using UnityEngine.UI;
using UnityEngine;
using CommonService;
using UniRx;
using Service;
using EventActivity;
using SaveTheDog;
using LobbyLogic.NetWork.ResponseStruct;
using Lobby.Common;

namespace Lobby
{
    class LobbyBottomBarPresenter : ContainerPresenter
    {
        public override string objPath => "prefab/lobby/lobby_low_bar";
        public override UiLayer uiLayer { get { return UiLayer.BarRoot; } }
        protected override BackHideBehaviour hideBehaviour => BackHideBehaviour.HideMe;

        #region BindingField Btn
        BindingNode vipNode;
        BindingNode mailNode;

        BindingNode puzzleNode;
        BindingNode crownNode;

        BindingNode dailyMissionNode;
        BindingNode dailyRwardNode;
        //BindingNode eventNode;
        BindingNode coinNode;

        BindingNode saveTheDogNode;
        Image lowBarBG;
        RectTransform activityRootRect;

        RectTransform itemRootLayout;
        Button upBtn;
        Button downBtn;
        Animator statusAnim;
        #endregion

        Dictionary<BindingNode, LobbyBottomActivityBtnBase> btnServices = new Dictionary<BindingNode, LobbyBottomActivityBtnBase>();

        float activityIconOriginPos;
        float xPartyOriginPos;

        SaveTheDogEntryBtn saveTheDogEntryBtn;
        int addCount = 0;
        public override void initUIs()
        {
            vipNode = getNodeData("vip_node");
            mailNode = getNodeData("mail_node");
            puzzleNode = getNodeData("puzzle_node");
            crownNode = getNodeData("crown_node");
            dailyMissionNode = getNodeData("daily_mission_node");
            //eventNode = getNodeData("event_node");
            coinNode = getNodeData("coin_node");
            dailyRwardNode = getNodeData("daily_reward_node");
            lowBarBG = getImageData("low_bar_bg");
            activityRootRect = getRectData("activity_entry_root_rect");
            saveTheDogNode = getNodeData("save_the_dog_node");

            itemRootLayout = getRectData("root_layout");
            upBtn = getBtnData("up_btn");
            downBtn = getBtnData("down_btn");
            statusAnim = getAnimatorData("enter_anim");
        }

        public override void init()
        {
            addCount = 0;
            upBtn.onClick.AddListener(() =>
            {
                statusAnim.SetTrigger("to_floor2");
                Observable.TimerFrame(30).Subscribe(_ =>
                {
                    upBtn.gameObject.setActiveWhenChange(false);
                    downBtn.gameObject.setActiveWhenChange(true);
                }).AddTo(uiGameObject);
            });

            downBtn.onClick.AddListener(() =>
            {
                statusAnim.SetTrigger("to_floor1");
                Observable.TimerFrame(30).Subscribe(_ =>
                {
                    downBtn.gameObject.setActiveWhenChange(false);
                    upBtn.gameObject.setActiveWhenChange(true);
                }).AddTo(uiGameObject);
            });
            downBtn.gameObject.setActiveWhenChange(false);

            changeLowBarBG(DataStore.getInstance.playerInfo.hasHighRollerPermission);
            DataStore.getInstance.playerInfo.checkHighRollerPermissionSub.Subscribe(changeLowBarBG).AddTo(uiGameObject);
            BottomBarLvTipManager.resetTips();

            saveTheDogEntryBtn = bindingActivityBtn<SaveTheDogEntryBtn>(saveTheDogNode, openSaveTheDogMap);
            bindingActivityBtn<DailyRewardWithLoading>(dailyRwardNode, openDailyReward);
            var crownNodePresenter = bindingActivityBtn<CrownBottomBtn>(crownNode, openGoldClub);
            var mailNodePresenter = bindingActivityBtn<MailNodeWithLoading>(mailNode, openMailBox);

            var dailyMission = bindingActivityBtn<DailyMissionNode>(dailyMissionNode, openMission);
            var vipNodePresenter = bindingActivityBtn<VIPNodeWithLoading>(vipNode, openVip);

            bindingActivityBtn<PuzzleBottomBtn>(puzzleNode, openMuseum);

            setActivityEntryIcon();

            UiManager.bindNode<LobbyBottomStayMiniGameNodePresenter>(coinNode.cachedGameObject);
            peekMail();

            activityIconOriginPos = activityRootRect.transform.localPosition.x;
            xPartyOriginPos = saveTheDogNode.transform.localPosition.x;

            vipNodePresenter.loadingBundle();
            mailNodePresenter.loadingBundle();
            crownNodePresenter.loadingBundle();

            upBtn.gameObject.setActiveWhenChange(addCount > 6);
        }

        void openSaveTheDogMap()
        {
            TransitionxPartyServices.instance.openTransitionPage();
            var mapPresenter = UiManager.getPresenter<SaveTheDogMapPresenter>();
            mapPresenter.open();
        }

        async void setActivityEntryIcon()
        {
            GetActivityResponse activityRes = await AppManager.lobbyServer.getActivity();
            ActivityDataStore.nowActivityInfo = activityRes.activity;
            string activityBundleName = $"lobby_publicity_{ActivityDataStore.getActivityEntryPrefabName(activityRes.activity.activityId)}";

            if (ApplicationConfig.isLoadFromAB)
            {
                AssetBundleManager.Instance.preloadBundles(activityBundleName, success =>
                {
                    createActivityEntryIcon(activityRes);
                });
                return;
            }

            createActivityEntryIcon(activityRes);
        }

        void createActivityEntryIcon(GetActivityResponse actRes)
        {
            PoolObject entryObj;
            string activityID = null == actRes.activity ? ((int)ActivityID.Rookie).ToString() : ActivityDataStore.nowActivityInfo.activityId;
            entryObj = ResourceManager.instance.getObjectFromPool(ActivityDataStore.getAcitivtyEntryPrefabPath(activityID), activityRootRect);
            UiManager.bindNode<EventBtnNodePresenter>(entryObj.cachedGameObject).setEventNode(actRes);
            ActivityDataStore.isEndSub.Subscribe(_ =>
            {
                setActivityEntryIcon();
            }).AddTo(uiGameObject);

            ActivityDataStore.isEndErrorSub.Subscribe(_ =>
            {
                setActivityEntryIcon();
            }).AddTo(uiGameObject);

            if (SaveTheDogMapData.instance.isSkipSaveTheDog)
            {
                return;
            }

            bool isSaveDogBtnPosInOrigin = ActivityDataStore.getNowActivityID() != ActivityID.Rookie;

            if (!isSaveDogBtnPosInOrigin)
            {
                saveTheDogNode.cachedRectTransform.SetParent(activityRootRect);
                entryObj.cachedRectTransform.SetParent(itemRootLayout.transform.GetChild(0));
                entryObj.cachedRectTransform.anchoredPosition3D = new Vector3(0, 27, 0);
            }
            else
            {
                saveTheDogNode.cachedRectTransform.SetParent(itemRootLayout.transform.GetChild(0));
            }

            saveTheDogNode.cachedRectTransform.localPosition = Vector3.zero;
            saveTheDogEntryBtn.changeNameImg(isSaveDogBtnPosInOrigin);
        }

        void changeLowBarBG(bool hasHighRollerPremission)
        {
            string spriteName = hasHighRollerPremission ? "vip" : "normal";
            lowBarBG.sprite = ResourceManager.instance.load<Sprite>($"texture/res_lobby_low_ui/texture/bg_ui_down_{spriteName}");
        }

        T bindingActivityBtn<T>(BindingNode serviceNode, Action clickEvent = null) where T : LobbyBottomActivityBtnBase, new()
        {
            bool isInteractable = clickEvent != null;

            LobbyBottomActivityBtnBase activityBtnBase = UiManager.bindNode<T>(serviceNode.cachedGameObject).setClickEvent(clickEvent);

            if (isInteractable)
            {
                if (btnServices.ContainsKey(serviceNode))
                {
                    btnServices[serviceNode] = activityBtnBase;
                }
                else
                {
                    btnServices.Add(serviceNode, activityBtnBase);
                }
            }

            if (activityBtnBase.rootLayoutID >= 0)
            {
                serviceNode.cachedRectTransform.SetParent(itemRootLayout.transform.GetChild(activityBtnBase.rootLayoutID));
                serviceNode.cachedRectTransform.localPosition = Vector3.zero;
                addCount++;
            }

            return activityBtnBase as T;
        }

        async void peekMail()
        {
            var helper = new MailBoxProvider();
            int count = await helper.peekMails();
            NoticeManager.instance.mailNoticeEvent.OnNext(count);
            LobbyBottomActivityBtnBase btn = null;
            if (btnServices.TryGetValue(mailNode, out btn))
            {
                btn.setHint(count > 0, count);
            }
        }

        void openVip()
        {
            UiManager.getPresenter<VipInfoBoardPresenter>().open();
        }

        void openMailBox()
        {
            var helper = new MailBoxProvider();
            helper.openMailBox(peekMail);
        }

        void openMission()
        {
            var presenter = UiManager.getPresenter<Mission.MissionMainPresenter>();
            presenter.uiGameObject.setActiveWhenChange(false);
            presenter.open();
        }

        void openMuseum()
        {
            Museum.openMuseum();
        }

        void openGoldClub()
        {
            UiManager.getPresenter<HighRoller.HighRollerMainPresenter>().open();
        }

        void openDailyReward()
        {
            LoginRewardServices.instance.showHistoryRewardBesideToDay();
        }
    }
}
