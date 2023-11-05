using CommonILRuntime.BindingModule;
using CommonILRuntime.Module;
using CommonService;
using LobbyLogic.Audio;
using Services;
using StayMiniGame;
using System;
using UnityEngine;
using UnityEngine.UI;
using UniRx;
using CommonPresenter;
using Binding;
using Notice;
using Lobby.Common;

namespace Lobby
{
    class LobbyBottomStayMiniGameNodePresenter : NodePresenter
    {
        #region UIs
        Button collectBtn;
        GameObject timeObj;
        Text timeTxt;
        Animator statusAnim;
        Image maskImg;

        GameObject icon_group;
        GameObject icon_loading;
        Image icon_color;
        private BindingNode numberNoticeNode;
        #endregion

        TimerService timer = new TimerService();
        Sprite[] lowUISprite;

        public override void initUIs()
        {
            collectBtn = getBtnData("collect_btn");
            timeObj = getGameObjectData("time_obj");
            timeTxt = getTextData("time_txt");
            statusAnim = getAnimatorData("status_anim");
            maskImg = getImageData("mask_img");
            icon_group = getGameObjectData("icon_group");
            icon_loading = getGameObjectData("loading");
            icon_color = getImageData("icon_color");
            numberNoticeNode = getNodeData("number_notice_node");
        }

        public override void init()
        {
            lowUISprite = ResourceManager.instance.loadAllWithResOrder("texture/res_lobby_low_ui/texture/res_lobby_low_ui", AssetBundleData.getBundleName(BundleType.StayMinigame));
            changeMask(DataStore.getInstance.playerInfo.hasHighRollerPermission);
            timer.setAddToGo(uiGameObject);
            StayGameDataStore.countdownTimeSub.Subscribe(checkCountdownBonus).AddTo(uiGameObject);
            DataStore.getInstance.playerInfo.checkHighRollerPermissionSub.Subscribe(changeMask).AddTo(uiGameObject);
            StayGameDataStore.initGameData();
            collectBtn.onClick.AddListener(activityClick);
            UiManager.bindNode<NumberNoticePresenter>(numberNoticeNode.cachedGameObject).setSubject(NoticeManager.instance.stayGameNoticeEvent);
        }

        void activityClick()
        {
            AudioManager.instance.playAudioOnce(CommonAudioPathProvider.getAudioPath(BasicCommonSound.InfoBtn));
            UiManager.getPresenter<StayMiniGameMainPresenter>().open();
        }

        void changeMask(bool hasHighRollerPremission)
        {
            string spriteName = hasHighRollerPremission ? "vip" : "normal";
            maskImg.sprite = Array.Find(lowUISprite, sprite => sprite.name.Equals($"bg_ui_down_{spriteName}_mask"));
        }

        void checkCountdownBonus(CompareBonusTimeResult compareTimeResult)
        {
            bool isCountdownTime = compareTimeResult.isCountdownTime && compareTimeResult.getRewardGameType == StayGameType.none;
            timeObj.setActiveWhenChange(isCountdownTime);
            if (isCountdownTime)
            {
                timer.StartTimer(compareTimeResult.countdownTime, setTime);
                statusAnim.SetTrigger("out");
                Observable.TimerFrame(25).Subscribe(_ =>
                {
                    statusAnim.enabled = false;
                });
                return;
            }
            statusAnim.enabled = true;
        }

        void setTime(TimeSpan lastTime)
        {
            timeTxt.text = UtilServices.toTimeStruct(lastTime).toTimeString();
            if (lastTime <= TimeSpan.Zero)
            {
                StayGameDataStore.initGameData();
                timeObj.setActiveWhenChange(false);
                collectBtn.gameObject.setActiveWhenChange(true);
            }
        }
    }
}
