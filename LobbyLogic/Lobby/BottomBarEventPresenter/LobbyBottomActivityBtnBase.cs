using Debug = UnityLogUtility.Debug;
using System;
using UnityEngine;
using UnityEngine.UI;
using CommonILRuntime.Module;
using CommonILRuntime.BindingModule;
using CommonService;
using Common;
using TMPro;
using LobbyLogic.Audio;
using LobbyLogic.NetWork.ResponseStruct;
using UniRx;
using Mission;
using HighRoller;
using Services;
using Lobby.Common;
using SaveTheDog;
using Binding;
using Notice;

namespace Lobby
{
    interface IBottomBarWithLoading
    {
        GameObject getLoadingObject();
        Image getLoadingIcon();
        GameObject getAnimeObject();

    }

    #region BottomActivityBase
    class LobbyBottomActivityBtnBase : NodePresenter
    {
        #region UIs
        public CustomBtn activityBtn;
        GameObject hintObj = null;
        TextMeshProUGUI hintText = null;
        protected Animator statusAnim;
        #endregion

        Action clickEvent;

        public virtual int rootLayoutID { get; set; } = -1;
        public override void initUIs()
        {
            activityBtn = getCustomBtnData("activity_btn");
            statusAnim = getAnimatorData("stataus_anim");
        }

        public void setRootLayoutID(int layoutID)
        {
            rootLayoutID = layoutID;
        }

        public override void init()
        {
            activityBtn.clickHandler = activityClick;
        }

        public virtual void activityClick()
        {
            AudioManager.instance.playAudioOnce(CommonAudioPathProvider.getAudioPath(BasicCommonSound.InfoBtn));
            if (null != clickEvent)
            {
                clickEvent();
            }
        }
        public LobbyBottomActivityBtnBase setClickEvent(Action clickEvent)
        {
            this.clickEvent = clickEvent;
            return this;
        }

        public LobbyBottomActivityBtnBase setBtnInteractable(bool interactable)
        {
            activityBtn.interactable = interactable;
            string statausAnimTriggerName = interactable ? "loop" : "stop";
            statusAnim.SetTrigger(statausAnimTriggerName);
            return this;
        }

        public void playStopTrigger()
        {
            statusAnim.SetTrigger("stop");
        }


        /// <summary>
        /// 不是每個物件都有綁定紅點&提示數字文字
        /// </summary>
        /// <param name="show"></param>
        /// <param name="count"></param>
        public void setHint(bool show, int count = 0)
        {
            //if (null == hintObj)
            //{
            //    hintObj = getGameObjectData("red_tips_obj");
            //}
            //if (null != hintObj)
            //{
            //    hintObj.setActiveWhenChange(show);
            //}

            //if (null == hintText)
            //{
            //    hintText = getBindingData<TextMeshProUGUI>("red_tips_txt");
            //}
            //if (null != hintText)
            //{
            //    hintText.text = count.ToString();
            //}
        }
    }

    class LobbyBottomBtnWithLoading : LobbyBottomActivityBtnBase
    {
        GameObject loading = null;
        Image iconImg = null;

        public virtual string bundleName { get; private set; } = string.Empty;
        public virtual Priority priority { get; private set; } = Priority.High;

        public override void initUIs()
        {
            base.initUIs();
            loading = getGameObjectData("loading");
            iconImg = getImageData("icon_color");
        }

        public void loadingBundle()
        {
            if (!ApplicationConfig.isLoadFromAB || string.IsNullOrEmpty(bundleName))
            {
                return;
            }

            statusAnim.gameObject.setActiveWhenChange(false);
            loading.setActiveWhenChange(true);
            AssetBundlePriority.getInstance.addQueued(bundleName, iconImg, priority, _res =>
            {
                statusAnim.gameObject.gameObject.setActiveWhenChange(true);
                loading.setActiveWhenChange(false);
            });
        }
    }

    class LobbyBottomBtnWithLock : LobbyBottomBtnWithLoading
    {
        #region UIs
        GameObject lockObj;
        public Button lockBtn;
        LvTipNodePresenter lvTipNode;
        #endregion

        public virtual int unLockLv { get; set; }
        public override void initUIs()
        {
            base.initUIs();
            lockObj = getGameObjectData("lock_obj");
            lockBtn = getBtnData("lock_btn");
        }

        public override void init()
        {
            base.init();
            lvTipNode = UiManager.bindNode<LvTipNodePresenter>(getNodeData("unlock_tip_node").cachedGameObject);
            lockBtn.onClick.AddListener(openUnLockTip);
            checkLvIsLock(DataStore.getInstance.playerInfo.level);
            DataStore.getInstance.playerInfo.lvSubject.Subscribe(checkLvIsLock).AddTo(uiGameObject);
            BottomBarLvTipManager.addBottomBarLvTips(lvTipNode);
        }

        void checkLvIsLock(int playerLv)
        {
            setBtnIsLock(playerLv < unLockLv);
        }

        public void setBtnIsLock(bool isLock)
        {
            if (unLockLv < 0)
            {
                isLock = true;
            }
            if (isLock)
            {
                lvTipNode.open();
            }
            setBtnInteractable(!isLock);
            lockObj.setActiveWhenChange(isLock);
            lockBtn.gameObject.setActiveWhenChange(isLock);
            if (unLockLv < 0)
            {
                lockObj.setActiveWhenChange(false);
            }

            if (false == isLock && !string.IsNullOrEmpty(bundleName))
            {
                loadingBundle();
            }
        }

        void openUnLockTip()
        {
            BottomBarLvTipManager.closeTips();
            AudioManager.instance.playAudioOnce(CommonAudioPathProvider.getAudioPath(BasicCommonSound.InfoBtn));
            lvTipNode.openLvTip(LvTipArrowDirection.Bottom, unLockLv);
        }
    }
    #endregion

    class DailyRewardWithLoading : LobbyBottomBtnWithLoading
    {
        public override int rootLayoutID { get => 1; }
    }
    class MailNodeWithLoading : LobbyBottomBtnWithLoading
    {
        public override string bundleName => "lobby_mail";
        public override int rootLayoutID { get => 4; }

        private BindingNode numberNoticeNode;

        public override void initUIs()
        {
            base.initUIs();
            numberNoticeNode = getNodeData("number_notice_node");
        }

        public override void init()
        {
            base.init();
            UiManager.bindNode<NumberNoticePresenter>(numberNoticeNode.cachedGameObject).setSubject(NoticeManager.instance.mailNoticeEvent);
        }
    }
    class VIPNodeWithLoading : LobbyBottomBtnWithLoading
    {
        public override string bundleName => "lobby_vip";

        public override int rootLayoutID
        {
            get
            {
                if (SaveTheDogMapData.instance.isSkipSaveTheDog)
                {
                    return 0;
                }
                return 3;
            }
        }
    }
    class DailyMissionNode : LobbyBottomBtnWithLock
    {
        public override int unLockLv { get => MissionData.unLockLv; }

        public override string bundleName => "lobby_daily_mission";

        public override int rootLayoutID { get => 5; }

        private BindingNode noticeNode;
        private BindingNode numberNoticeNode;

        public override void initUIs()
        {
            base.initUIs();
            numberNoticeNode = getNodeData("number_notice_node");
            noticeNode = getNodeData("notice_node");
        }

        public override void init()
        {
            base.init();
            UiManager.bindNode<NumberNoticePresenter>(numberNoticeNode.cachedGameObject).setSubject(NoticeManager.instance.dailyNoticeEvent);
            UiManager.bindNode<NoticePresenter>(noticeNode.cachedGameObject).setSubject(NoticeManager.instance.dailyNoticeEvent);
            NoticeManager.instance.dailyNoticeEvent.Subscribe(setNoticeNodeActive).AddTo(uiGameObject);
        }

        private void setNoticeNodeActive(int dailyNoticeAmount)
        {
            bool numberNoticeActive = NoticeManager.instance.dailyNoticeAmount > 0;
            numberNoticeNode.gameObject.setActiveWhenChange(numberNoticeActive);
            noticeNode.gameObject.setActiveWhenChange(!numberNoticeActive);
        }
    }
    class PuzzleBottomBtn : LobbyBottomBtnWithLock
    {
        GameObject timerObj;
        Text timerTxt;
        private RectTransform tipObj;
        private BindingNode noticeNode;
        public override int rootLayoutID { get => 2; }
        public override int unLockLv { get => 1; }

        public override void initUIs()
        {
            base.initUIs();
            timerObj = getGameObjectData("puzzle_time_obj");
            timerTxt = getTextData("puzzle_time_txt");
            //tipObj = getRectData("tip_rect");
            noticeNode = getNodeData("notice_node");
        }

        public override void init()
        {
            base.init();
            timerObj.setActiveWhenChange(false);
            UiManager.bindNode<NoticePresenter>(noticeNode.cachedGameObject).setSubject(NoticeManager.instance.puzzleNoticeEvent);
            NoticeManager.instance.puzzleNoticeEvent.Subscribe(setGlowObj).AddTo(uiGameObject);
        }

        private void setGlowObj(int amount)
        {
            noticeNode.cachedRectTransform.parent.gameObject.setActiveWhenChange(amount > 0);
            //tipObj.gameObject.setActiveWhenChange(amount > 0);
        }
    }
    class CrownBottomBtn : LobbyBottomBtnWithLock
    {
        public override string bundleName => "lobby_crown";

        public Text pointTxt;
        long passPoint;

        public override int rootLayoutID
        {
            get
            {
                if (SaveTheDogMapData.instance.isSkipSaveTheDog)
                {
                    return 3;
                }
                return 6;
            }
        }
        public override int unLockLv { get => 1; }

        public override void initUIs()
        {
            base.initUIs();
            pointTxt = getTextData("point_txt");
        }
        public override void init()
        {
            base.init();
            pointTxt.text = "0";

            setPoint(HighRollerDataManager.instance.userRecord);
            HighRollerDataManager.instance.userRecordSub.Subscribe(setPoint).AddTo(uiGameObject);
            HighRollerDataManager.instance.passPointUpdateSub.Subscribe(updatePassPoint).AddTo(uiGameObject);
            DataStore.getInstance.playerInfo.addPassPointSub.Subscribe(addPassPoint).AddTo(uiGameObject);
        }
        void setPoint(HighRollerUserRecordResponse userRecord)
        {
            bool isUnLock = DataStore.getInstance.playerInfo.level >= unLockLv || userRecord.passPoints >= 20000;
            setBtnIsLock(!isUnLock);
            setBtnInteractable(isUnLock);
            updatePassPoint(userRecord.passPoints);
        }

        void addPassPoint(long point)
        {
            passPoint += point;
            resetPointTxt();
        }

        void updatePassPoint(long newPoint)
        {
            passPoint = newPoint;
            resetPointTxt();
        }

        void resetPointTxt()
        {
            pointTxt.text = passPoint.ToString();
        }
    }
    class SaveTheDogEntryBtn : LobbyBottomActivityBtnBase
    {
        Image nameImg;
        Sprite activityPosNameSprite;
        Sprite originPosNameSprite;
        private BindingNode noticeNode;

        public override int rootLayoutID
        {
            get
            {
                if (!SaveTheDogMapData.instance.isSkipSaveTheDog)
                {
                    return 0;
                }

                return -1;
            }
        }

        public override void initUIs()
        {
            base.initUIs();
            nameImg = getImageData("name_img");
            noticeNode = getNodeData("notice_node");
        }

        public override void init()
        {
            base.init();
            Sprite[] barIcons = ResourceManager.instance.loadAll(UtilServices.getLocalizationAltasPath("res_lobby_function_tex"));
            activityPosNameSprite = Array.Find(barIcons, iconSprite => iconSprite.name.Equals("tex_cat_and_dog"));
            originPosNameSprite = Array.Find(barIcons, iconSprite => iconSprite.name.Equals("tex_cat_and_dog_b"));

            if (SaveTheDogMapData.instance.isSkipSaveTheDog)
            {
                close();
            }

            var dogNotice = UiManager.bindNode<NoticePresenter>(noticeNode.cachedGameObject);
            dogNotice.setSubject(NoticeManager.instance.dogeNoticeEvent);
            dogNotice.showNotice(NoticeManager.instance.dogEventAmount);
        }

        public void changeNameImg(bool isOriginPos)
        {
            if (isOriginPos)
            {
                nameImg.sprite = originPosNameSprite;
                return;
            }

            nameImg.sprite = activityPosNameSprite;
        }
    }
}
