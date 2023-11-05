using UnityEngine.UI;
using UnityEngine;
using System;
using CommonILRuntime.Extension;
using CommonILRuntime.Module;
using Services;
using CommonILRuntime.BindingModule;

namespace Mission
{
    class MissionSchedualNode : NodePresenter
    {
        Image progressBar;
        Text schedualTxt;
        Text timeTxt;
        Button collectBtn;
        Button boxBtn;
        Button directPassBtn;
        RectTransform boxBtnRect;
        Animator collectBtnEffect;
        GameObject boxMainObj;
        GameObject progressBarParent;
        MissionContentPresenter contentPresenter;
        Action<RectTransform, MissionProgressData> onClickBoxCallBack;
        public Action openReward;

        TimerService resetTimeServices = new TimerService();
        MissionProgressData missionProgress;

        public override void initUIs()
        {
            var contentNode = getNodeData("content_txt");
            contentPresenter = UiManager.bindNode<MissionContentPresenter>(contentNode.cachedGameObject);
            progressBarParent = getGameObjectData("progress_bar_parent");
            progressBar = getImageData("progress_bar");
            schedualTxt = getTextData("schedual_txt");
            timeTxt = getTextData("time_txt");
            boxMainObj = getGameObjectData("box_main_obj");
            collectBtn = getBtnData("collect_btn");
            collectBtnEffect = getAnimatorData("collect_btn_effect");
            directPassBtn = getBtnData("direct_pass_btn");
            boxBtn = getBtnData("box_btn");
            boxBtnRect = boxBtn.GetComponent<RectTransform>();
        }

        public override void init()
        {
            boxBtn.onClick.AddListener(onClickBox);
            collectBtn.onClick.AddListener(collectClick);
            resetTimeServices.setAddToGo(uiGameObject);
            directPassBtn.gameObject.setActiveWhenChange(false);
            initMissionContentPresenter();
        }

        void initMissionContentPresenter()
        {
            switch (UtilServices.getNowScreenOrientation)
            {
                case ScreenOrientation.Portrait:
                case ScreenOrientation.PortraitUpsideDown:
                    contentPresenter.setLowDisplayCondition(0, 1);
                    break;
                default:
                    contentPresenter.setLowDisplayCondition(1, 1);
                    break;
            }
        }

        public void collectClick()
        {
            if (null != openReward)
            {
                openReward();
            }

            changeCollectBtnInteractable(false);
        }

        public void setProgressData(MissionProgressData progressData)
        {
            var contentFactory = MissionData.getContentFactory();
            var progress = getProgress(progressData);
            var contentMsg = contentFactory.createContentMsg(progressData);

            missionProgress = progressData;
            contentPresenter.setContentTxt(contentMsg);
            schedualTxt.text = progress.ToString("P0");
            progressBar.fillAmount = progress;
            changeCollectBtnInteractable(progressBar.fillAmount >= 1);
            setProgressState(progressBar.fillAmount >= 1);
            resetTimeServices.ExecuteTimer();
            resetTimeServices.StartTimer(progressData.resetTime, updateResetTime);
        }

        void changeCollectBtnInteractable(bool interactable)
        {
            collectBtn.interactable = interactable;
            collectBtnEffect.enabled = interactable;
        }

        void setProgressState(bool isFull)
        {
            progressBarParent.setActiveWhenChange(!isFull);
            boxMainObj.setActiveWhenChange(!isFull);
            collectBtn.gameObject.setActiveWhenChange(isFull);
        }

        float getProgress(MissionProgressData progressData)
        {
            var result = (float)(progressData.nowProgress / progressData.finalTarget);
            if (1f < result)
            {
                result = 1f;
            }

            return result;
        }

        public void updateResetTime(TimeSpan resetTime)
        {
            timeTxt.text = UtilServices.formatCountTimeSpan(resetTime);
            if (resetTime <= TimeSpan.Zero)
            {
                setProgressData(missionProgress);
            }
        }

        void onClickBox()
        {
            onClickBoxCallBack?.Invoke(boxBtnRect, missionProgress);
        }

        

        public void setClickBoxCallBack(Action<RectTransform, MissionProgressData> onClickBoxCallBack)
        {
            this.onClickBoxCallBack = onClickBoxCallBack;
        }
    }
}
