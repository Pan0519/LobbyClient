using UnityEngine;
using UnityEngine.UI;
using CommonILRuntime.Module;
using EventActivity;
using LobbyLogic.Audio;
using CommonService;
using LobbyLogic.NetWork.ResponseStruct;
using Service;
using Services;
using System;
using UniRx;
using Binding;
using CommonILRuntime.BindingModule;
using Notice;
using Lobby.Common;

namespace Event.Common
{
    class EventBtnNodePresenter : NodePresenter
    {
        #region [ UIs ]
        Button clickBtn;
        GameObject lockObj;
        //Image eventNameImg;
        //Text eventNameTxt;
        //Image iconImg;
        Text timeTxt;
        GameObject timeObj;
        Animator statusAnim;

        GameObject icon_loading;
        Image icon_color;
        private BindingNode numberNoticeNode;
        #endregion

        TimerService timerService;
        ActivityID activityID;
        //public Subject<bool> isCompleteSub = new Subject<bool>();
        public override void initUIs()
        {
            clickBtn = getBtnData("btn_Click");
            lockObj = getGameObjectData("lock_obj");
            //eventNameImg = getImageData("event_name_img");
            //eventNameTxt = getTextData("text_EventName");
            //iconImg = getImageData("icon_img");
            statusAnim = getAnimatorData("entry_anim");
            timeTxt = getTextData("time_txt");
            timeObj = getGameObjectData("activity_time_obj");

            icon_loading = getGameObjectData("loading");
            icon_color = getImageData("icon_color");
            numberNoticeNode = getNodeData("number_notice_node");
        }
        public override void init()
        {
            //clickBtn.onClick.AddListener(onClick);
            UiManager.bindNode<NumberNoticePresenter>(numberNoticeNode.cachedGameObject).setSubject(NoticeManager.instance.activityNoticeEvent);
            NoticeManager.instance.getActivityPropNoticeAmount();
        }

        void onClick()
        {
            AudioManager.instance.playAudioOnce(CommonAudioPathProvider.getAudioPath(BasicCommonSound.InfoBtn));
            ActivityPageData.instance.openActivityPage(activityID);
        }

        public void setEventNode(GetActivityResponse actRes)
        {
            if (null == ActivityDataStore.nowActivityInfo || null == actRes)
            {
                timeObj.setActiveWhenChange(false);
                setIsLock(true);
                //showEvntIcon(ActivityID.Rookie);
                return;
            }
            ActivityDataStore.isEndSub.Subscribe(_ => { clear(); }).AddTo(uiGameObject);
            ActivityDataStore.isEndErrorSub.Subscribe(_ => { clear(); }).AddTo(uiGameObject);
            activityID = ActivityDataStore.getNowActivityID();

            TimeStruct timeStruct = UtilServices.toTimeStruct(UtilServices.getEndTimeStruct(actRes.activity.endAt));
            bool isShowTimer = timeStruct.days <= 7;
            timeTxt.text = timeStruct.toTimeString(LanguageService.instance.getLanguageValue("Time_Days"));
            timeObj.setActiveWhenChange(true);
            if (timeStruct.days <= 0 && timeStruct.seconds > 0)
            {
                timerService = new TimerService();
                timerService.setAddToGo(uiGameObject);
                DateTime endDateTime = UtilServices.strConvertToDateTime(actRes.activity.endAt, DateTime.MinValue);
                timerService.StartTimer(endDateTime, updateActivityTime);
            }

            //showEvntIcon((ActivityID)activityIDInt);
            setIsLock(false);

            if (ApplicationConfig.isLoadFromAB)
            {
                //Util.Log("AssetBundlePriority.START...activity");
                statusAnim.gameObject.setActiveWhenChange(false);
                icon_loading.setActiveWhenChange(true);
                AssetBundlePriority.getInstance.addQueued($"{ActivityDataStore.getAcitivtyEntryPrefabName()}", icon_color, Priority.High, _res =>
                {
                    //Util.Log("AssetBundlePriority.END...activity");
                    clickBtn.onClick.AddListener(onClick);
                    statusAnim.gameObject.setActiveWhenChange(true);
                    icon_loading.setActiveWhenChange(false);
                });
            }
            else
            {
                clickBtn.onClick.AddListener(onClick);
            }
        }

        void updateActivityTime(TimeSpan expTime)
        {
            timeTxt.text = UtilServices.formatCountTimeSpan(expTime);
            if (expTime <= TimeSpan.Zero)
            {
                eventComplete(true);
            }
        }

        private void eventComplete(bool isComplete)
        {
            if (!isComplete)
            {
                return;
            }
            ActivityDataStore.activtyCallIsEnd(isComplete);
        }

        void setIsLock(bool islock)
        {
            lockObj.setActiveWhenChange(islock);
            clickBtn.interactable = !islock;
            string statusAnimTriggerName = islock ? "gray" : "loop";
            statusAnim.SetTrigger(statusAnimTriggerName);
        }
    }


}
