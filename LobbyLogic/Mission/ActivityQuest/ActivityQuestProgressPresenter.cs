using Debug = UnityLogUtility.Debug;
using CommonILRuntime.Module;
using UnityEngine.UI;
using UnityEngine;
using Lobby.UI;
using CommonILRuntime.BindingModule;
using Service;
using System.Collections.Generic;
using Binding;
using UniRx;
using System;
using EventActivity;
using CommonService;
using Services;
using LobbyLogic.Common;
using System.Threading.Tasks;
using LobbyLogic.NetWork.ResponseStruct;
using Network;
using Game.Common;
using Lobby.LoadingUIModule;

namespace Mission
{
    public class ActivityQuestProgressPresenter : ContainerPresenter
    {
        public override string objPath => "prefab/quest_mission/quest_mission_banner";

        public override UiLayer uiLayer { get { return UiLayer.BarRoot; } }

        #region UI Obj
        private Animator boardAni;
        private RectTransform activityLayout;
        private Button btnHide;
        private Button btnShow;
        private BindingNode numberNoticeNode;
        private Animator progressAni;
        private Text progress;
        private RectTransform dogQuestLayout;
        private BindingNode activityItemNode;
        private BindingNode dogQuestItemNode;
        private Button btnBack;
        private Image barImg;
        private Button tapBtn;
        private RectTransform tapBtnRect;
        private RectTransform shrinkNode;
        private RectTransform barObj;
        #endregion

        #region Other
        private List<PoolObject> poolObjList = new List<PoolObject>();
        private List<DogQuestItem> dogQuestList = new List<DogQuestItem>();

        private GameOrientation gameOrientation;
        private MoveWithMouse moveWithMouse;
        private const int topBarHight = 200;
        private const int extendBottomBarHigth = 160;
        private const float shrinkPivotY = 0.2f;
        private const float extendPivotY = 0.5f;

        private ActivityQuestItem activityNode;
        public ActivityQuestItem dogProgressNode;
        #endregion

        public override void initUIs()
        {
            boardAni = getAnimatorData("board_ani");
            activityLayout = getRectData("activity_layout_rect");
            btnHide = getBtnData("hide_btn");
            btnShow = getBtnData("show_btn");
            numberNoticeNode = getNodeData("number_notice_node");
            progressAni = getAnimatorData("progress_ani");
            progress = getTextData("progress_txt");
            dogQuestLayout = getRectData("dog_quest_layout_rect");
            activityItemNode = getNodeData("activity_item_node");
            dogQuestItemNode = getNodeData("dog_quest_item_node");
            btnBack = getBtnData("back_btn");
            barImg = getImageData("bar_img");
            barObj = getRectData("bar_rect");
            shrinkNode = getRectData("shrink_rect");
            tapBtn = getBtnData("tap_btn");
            tapBtnRect = tapBtn.GetComponent<RectTransform>();
        }

        public override void init()
        {
            BindingLoadingPage.instance.open();
            getQuestProgressInfo();
            initActivityInfo();
            btnHide.onClick.AddListener(onBtnHide);
            btnShow.onClick.AddListener(onBtnShow);
            btnBack.onClick.AddListener(onBtnBack);
            initDragObj();
            subscribeEvent();
        }

        private void subscribeEvent()
        {
            ActivityQuestData.missionProgressUpdate.Subscribe(updateDogQuestProgress).AddTo(uiGameObject);
            ActivityDataStore.pageAmountChangedSub.Subscribe(updateActivityPropAmount).AddTo(uiGameObject);
            FromGameMsgService.getInstance.props.Subscribe(updateActivityData).AddTo(uiGameObject);
            ActivityDataStore.activityCloseSub.Subscribe(resetGameOritien).AddTo(uiGameObject);
            DataStore.getInstance.gameToLobbyService.updateGameStataSubject.Subscribe(updateGameState).AddTo(uiGameObject);
            ActivityQuestManager.instance.closeQuestProgress.Subscribe(closeQuestObj).AddTo(uiGameObject);
        }

        private void closeQuestObj(bool isClose)
        {
            dogProgressNode.close();
            playBoardAni("back_02");
        }

        private void updateGameState(GameConfig.GameState gameState)
        {
            ActivityQuestData.gameState = gameState;

            if (gameState == GameConfig.GameState.NG && ActivityQuestData.isRewardCanShow)
            {
                ActivityQuestManager.instance.onQuestComplete();
            }
        }

        private async void resetGameOritien(bool isActivityClose)
        {
            if (GameOrientation.Portrait == gameOrientation)
            {
                await UIRootChangeScreenServices.Instance.justChangeScreenToProp();
                await Task.Delay(TimeSpan.FromSeconds(0.5f));
                playBoardAni("step_02");
            }

            GamePauseManager.gameResume();
            barInfoInit();
        }

        private void barInfoInit()
        {
            string aniName = ActivityQuestData.isPropAmountMax() ? "max" : "get";
            progressAni.SetTrigger(aniName);
            setProgressPercentage();
        }

        private async void getQuestProgressInfo()
        {
            var newbieAdventure = await AppManager.lobbyServer.getNewbieAdventure();
            if (Result.OK != newbieAdventure.result)
            {
                return;
            }
            ActivityQuestData.setMissionPacks(newbieAdventure.record.missions);
            ActivityQuestData.questGameID = newbieAdventure.record.kind.Equals("slot") ? newbieAdventure.record.type : "";
            BindingLoadingPage.instance.close();
        }

        private void updateActivityPropAmount(int amount)
        {
            ActivityQuestData.nowTicketAmount = amount;
            setPropAmount();
            playBarAni();
        }

        private void updateActivityData(Props props)
        {
            ActivityQuestData.progressPercentage = props.percentage;
            showFlyObj();
            if (null == props.outcome)
            {
                return;
            }
            Dictionary<string, object> bagDict;
            if (props.outcome.TryGetValue("bag", out bagDict))
            {
                ActivityQuestData.nowTicketAmount = (int)bagDict["amount"];
            }
            updateActivityInfo();
        }

        public void updateDogQuestProgress(NewbieAdventureMissionProgress questProgress)
        {
            if (null == questProgress)
            {
                return;
            }
            ActivityQuestData.updateDogQuestProgress(questProgress);
            setQuestProgress();
        }

        private async void initActivityInfo()
        {
            var actRes = await AppManager.lobbyServer.getActivity();
            ActivityQuestData.isActivityExist = actRes.activity != null;

            if (ActivityQuestData.isActivityExist)
            {
                ActivityDataStore.nowActivityInfo = actRes.activity;
                await AppManager.eventServer.getBaseActivityInfo();
                var propResponse = await AppManager.lobbyServer.getActivityProp();
                ActivityQuestData.setActivityData(propResponse);
                barInfoInit();
            }
            initObj();
        }

        private async void initObj()
        {
            if (!ActivityQuestData.isActivityExist)
            {
                UiManager.bindNode<NoticeItem>(numberNoticeNode.cachedGameObject).setActive(false);
                barObj.gameObject.setActiveWhenChange(false);
            }
            else
            {
                addActivityObj();
            }
            updateActivityInfo(true);

            string currentGameID = await DataStore.getInstance.dataInfo.getNowplayGameID();
            if (currentGameID.Equals(ActivityQuestData.questGameID))
            {
                playBoardAni("step_03");
                addDogProgressObj();
                addDogQuestObj();
                showQuestTips();
            }
            else
            {
                playBoardAni("step_01");
            }
            activityItemNode.gameObject.setActiveWhenChange(false);
        }

        private async void initDragObj()
        {
            gameOrientation = await DataStore.getInstance.dataInfo.getNowGameOrientation();
            tapBtn.gameObject.setActiveWhenChange(GameOrientation.Portrait == gameOrientation);
            if (GameOrientation.Portrait == gameOrientation)
            {
                moveWithMouse = uiGameObject.AddComponent<MoveWithMouse>();
            }

            barRootRect = uiBarRoot.transform as RectTransform;
            calculateMoveRange();
            initIconPos();
            setMoveRange(shrinkTopMargin, shrinkBottomMargin);
        }

        private void initIconPos()
        {
            if (GameOrientation.Portrait != gameOrientation)
            {
                return;
            }

            float initPosy = 0;
            float initPosX = leftMargin;
            if (GameOrientation.Portrait == gameOrientation)
            {
                initPosy = shrinkTopMargin;
            }
            else
            {
                initPosy = shrinkNode.rect.height * 0.1f * -1;
                if (UtilServices.screenProportion <= 0.53f)
                {
                    initPosX += 50;
                }
            }
            uiRectTransform.anchoredPosition3D = new Vector3(initPosX, initPosy, 0);
        }

        private void updateActivityInfo(bool isInit = false)
        {
            if (isInit)
            {
                setActivityIcon();
            }
            setPropAmount();
            setProgressPercentage();
        }

        private void setActivityIcon()
        {
            string spriteName = !ActivityQuestData.isActivityExist ? "save_the_dog" : ActivityQuestData.nowActivityObjData.spriteName;
            var btnRect = btnShow.GetComponent<RectTransform>();
            Vector2 btnPos = btnRect.anchoredPosition;

            btnShow.image.sprite = ActivityQuestData.getIconSprite(spriteName);
            btnPos.y = ActivityQuestData.isActivityExist ? 22.3f : 5;
            btnRect.anchoredPosition = btnPos;

            if (activityNode != null)
            {
                activityNode.setActivityIcon();
            }
        }

        private void setPropAmount()
        {
            int amount = ActivityQuestData.nowTicketAmount;
            string amountStr = amount > 99 ? "99+" : amount.ToString();
            UiManager.bindNode<NoticeItem>(numberNoticeNode.cachedGameObject).setAmount(amountStr);
            if (activityNode != null)
            {
                activityNode.setPropAmount();
            }
        }

        private void playBarAni()
        {
            string aniName = ActivityQuestData.progressPercentage * 0.01f >= 1 && ActivityQuestData.isPropAmountMax() ? "max" : "get";
            progressAni.SetTrigger(aniName);
        }

        private void setProgressPercentage()
        {
            float fillAmount = 0;
            string progressContent = "";

            if (ActivityQuestData.isPropAmountMax())
            {
                fillAmount = 1;
                progressContent = "100%";
            }
            else
            {
                if (ActivityQuestData.progressPercentage != 100)
                {
                    fillAmount = ActivityQuestData.progressPercentage * 0.01f;
                    progressContent = $"{ActivityQuestData.progressPercentage}%";
                }
                else
                {
                    fillAmount = 0;
                    progressContent = "0%";
                }
            }
            barImg.fillAmount = fillAmount;
            progress.text = progressContent;
            playBarAni();

            if (activityNode != null)
            {
                activityNode.setProgressPercentage();
            }
        }

        #region MoveBar
        private float totalHeight;
        private float shrinkHeight;
        private float closeBtnHeight;
        private float activityBtnHalfHeight;

        private float leftMargin;
        private float rightMargin;
        private float extendTopMargin;
        private float extendBottomMargin;
        private float shrinkTopMargin;
        private float shrinkBottomMargin;
        private RectTransform barRootRect;

        private void changePivot(float pointY)
        {
            if (GameOrientation.Landscape == gameOrientation)
            {
                return;
            }
            var point = uiRectTransform.pivot;
            point.Set(point.x, pointY);
            Vector3 deltapos = uiRectTransform.pivot - point;
            deltapos.Scale(uiRectTransform.rect.size);
            deltapos.Scale(uiRectTransform.localScale);
            deltapos = uiRectTransform.rotation * deltapos;

            uiRectTransform.pivot = point;
            uiRectTransform.localPosition -= deltapos;
        }

        private void setMoveRange(float topRange, float bottomRange)
        {
            if (null == moveWithMouse)
            {
                return;
            }

            moveWithMouse.setPosRange(leftMargin, rightMargin, topRange, bottomRange);
        }

        private void calculateMoveRange()
        {
            initTapBtnSizeData();

            float rootHalfWidth = barRootRect.rect.width * 0.5f;
            float rootHalfHeight = barRootRect.rect.height * 0.5f;
            float objHalfWidth = uiRectTransform.rect.width * 0.5f;
            float objHalfHeight = uiRectTransform.rect.height * 0.5f * uiRectTransform.localScale.y;

            rightMargin = rootHalfWidth - objHalfWidth - 10;
            leftMargin = rightMargin * -1;
            extendTopMargin = rootHalfHeight - objHalfHeight;
            shrinkBottomMargin = (extendTopMargin - extendBottomBarHigth) * -1;
            extendBottomMargin = (extendTopMargin - extendBottomBarHigth) * -1 + topBarHight * 2;
            extendTopMargin -= topBarHight;
            shrinkTopMargin = rootHalfHeight - objHalfHeight - topBarHight;
        }

        private void initTapBtnSizeData()
        {
            totalHeight = uiRectTransform.rect.height;
            shrinkHeight = shrinkNode.rect.height;
            closeBtnHeight = barObj.rect.height;
            activityBtnHalfHeight = shrinkNode.rect.height * 0.5f;
        }

        void tapBtnSetOffset(float top, float bottom)
        {
            Vector2 setPos = tapBtnRect.offsetMin;
            setPos.Set(0, bottom);
            tapBtnRect.offsetMin = setPos;

            setPos = tapBtnRect.offsetMax;
            setPos.Set(0, top);
            tapBtnRect.offsetMax = setPos;
        }
        #endregion

        private void onBtnShow()
        {
            changePivot(extendPivotY);
            if (uiRectTransform.anchoredPosition.y < extendTopMargin)
            {
                uiRectTransform.anchPosMoveY(extendTopMargin, 0.2f);
            }
            tapBtn.interactable = false;
            playBoardAni("to_02");
            setMoveRange(extendTopMargin, extendBottomMargin);
            tapBtn.interactable = true;
            tapBtnSetOffset(activityBtnHalfHeight * -1, closeBtnHeight + 10);
        }

        private void onBtnHide()
        {
            changePivot(shrinkPivotY);
            tapBtn.interactable = true;
            playBoardAni("back_01");
            setMoveRange(shrinkTopMargin, shrinkBottomMargin);
            tapBtn.interactable = false;
            tapBtnSetOffset(shrinkHeight - totalHeight, 0);
        }

        private void onBtnBack()
        {
            playBoardAni("back_02");
        }

        private void addActivityObj()
        {
            if (!ActivityQuestData.isActivityExist)
            {
                return;
            }
            
            PoolObject activityObj = ResourceManager.instance.getObjectFromPool(activityItemNode.cachedGameObject, activityLayout);
            poolObjList.Add(activityObj);
            var activityNode = UiManager.bindNode<ActivityQuestItem>(activityObj.cachedGameObject);
            this.activityNode = activityNode;
            activityNode.setActivityBtn();
        }

        private void addDogProgressObj()
        {
            PoolObject dogProgressObj = ResourceManager.instance.getObjectFromPool(activityItemNode.cachedGameObject, activityLayout);

            poolObjList.Add(dogProgressObj);
            var dogProgressNode = UiManager.bindNode<ActivityQuestItem>(dogProgressObj.cachedGameObject);
            if (dogProgressObj)
            {
                this.dogProgressNode = dogProgressNode;
            }
            dogProgressNode.setDogBtn();
        }

        private void addDogQuestObj()
        {
            for (var i = 0; i < ActivityQuestData.missions.Length; i++)
            {
                PoolObject dogQuestObj = ResourceManager.instance.getObjectFromPool(dogQuestItemNode.cachedGameObject, dogQuestLayout);
                if (!dogQuestObj)
                {
                    continue;
                }
                poolObjList.Add(dogQuestObj);
                var dogQuestNode = UiManager.bindNode<DogQuestItem>(dogQuestObj.cachedGameObject);
                if (dogQuestNode == null)
                {
                    continue;
                }
                dogQuestList.Add(dogQuestNode);
            }
            setQuestProgress(true);
            dogQuestItemNode.gameObject.setActiveWhenChange(false);
        }

        private void setQuestProgress(bool isInit = false)
        {
            for (var i = 0; i < dogQuestList.Count; i++)
            {
                dogQuestList[i].setQuestInfo(i, isInit);
            }
        }

        private void showQuestTips()
        {
            for (var i = 0; i < dogQuestList.Count; i++)
            {
                dogQuestList[i].showTips(10);
            }
        }

        public void playBoardAni(string aniName)
        {
            boardAni.SetTrigger(aniName);
            btnShow.image.raycastTarget = aniName.Equals("back_01") || aniName.Equals("step_01");
        }

        public override void clear()
        {
            var itemPoolsEnum = poolObjList.GetEnumerator();
            while (itemPoolsEnum.MoveNext())
            {
                ResourceManager.instance.returnObjectToPool(itemPoolsEnum.Current.gameObject);
            }
            poolObjList.Clear();
            base.clear();
        }

        #region Ticket Fly
        ActivityIconData _nowActivityObjData;
        public ActivityIconData nowActivityObjData
        {
            get
            {
                if (null == _nowActivityObjData)
                {
                    EventBarDataConfig.activityObjData.TryGetValue(ActivityDataStore.getNowActivityID(), out _nowActivityObjData);
                }
                return _nowActivityObjData;
            }
        }

        List<PoolObject> flyObjs = new List<PoolObject>();
        Queue<PoolObject> flyShowObjs = new Queue<PoolObject>();
        List<IDisposable> flyTimer = new List<IDisposable>();
        private void showFlyObj()
        {
            if (flyObjs.Count <= 0)
            {
                for (int i = 0; i < 5; ++i)
                {
                    //var obj = ResourceManager.instance.getObjectFromPool(nowActivityObjData.prefabPath, uiRectTransform);
                    //obj.cachedRectTransform.anchoredPosition = new Vector3(0, 0, 0);
                    var obj = ResourceManager.instance.getObjectFromPool(nowActivityObjData.prefabPath, uiBarRoot);
                    obj.cachedRectTransform.anchoredPosition = Vector3.zero;
                    resetFlyObjPos(obj);
                    flyObjs.Add(obj);
                }
            }

            IDisposable showFlyObjDis = null;

            showFlyObjDis = Observable.Timer(TimeSpan.Zero, TimeSpan.FromSeconds(0.1f)).Subscribe(repeatCount =>
            {
                PoolObject flyObj = flyObjs[(int)repeatCount];
                switch (ActivityDataStore.getNowActivityID())
                {
                    case ActivityID.FarmBlast:
                    case ActivityID.Rookie:
                        TicketItemNodePresenter ticketItem = UiManager.bindNode<TicketItemNodePresenter>(flyObj.cachedGameObject);
                        ticketItem.open();
                        break;

                    default:
                        flyObj.cachedGameObject.setActiveWhenChange(true);
                        break;
                }
                flyObj.cachedTransform.movePos(barObj.position, 0.8f, onComplete: flyFinish, easeType: DG.Tweening.Ease.OutSine);
                flyShowObjs.Enqueue(flyObj);
                if (repeatCount >= flyObjs.Count - 1)
                {
                    showFlyObjDis.Dispose();
                }
            });
        }

        private void flyFinish()
        {
            var showObj = flyShowObjs.Dequeue();
            showObj.cachedGameObject.setActiveWhenChange(false);
            resetFlyObjPos(showObj);
            showObj.cachedRectTransform.localScale = Vector3.one;

            if (flyShowObjs.Count <= 0)
            {
                UtilServices.disposeSubscribes(flyTimer.ToArray());
                flyTimer.Clear();
            }
            setProgressPercentage();
        }

        private void resetFlyObjPos(PoolObject flyTicketObj)
        {
            flyTicketObj.cachedTransform.position = Vector3.zero;
            var anchPos = flyTicketObj.cachedRectTransform.anchoredPosition3D;
            anchPos.Set(anchPos.x, 0, 0);
            flyTicketObj.cachedRectTransform.anchoredPosition3D = anchPos;
        }
        #endregion
    }

    public class ActivityQuestItem : NodePresenter
    {
        #region UI Obj
        private Button btnActivity;
        private BindingNode numberNoticeNode;
        private Animator progressAni;
        private Text progress;
        private Image progressMax;
        private RectTransform barObj;
        private Image barImg;
        #endregion        

        public override void initUIs()
        {
            btnActivity = getBtnData("activity_btn");
            numberNoticeNode = getNodeData("number_notice_node");
            progressAni = getAnimatorData("progress_ani");
            progress = getTextData("progress_txt");
            progressMax = getImageData("progress_max_img");
            barObj = getRectData("bar_rect");
            barImg = getImageData("bar_img");
        }

        public override void init()
        {
            setPropAmount();
            barInfoInit();
        }

        private void barInfoInit()
        {
            string aniName = ActivityQuestData.isPropAmountMax() ? "max" : "get";
            progressAni.SetTrigger(aniName);

            int barProgress = ActivityQuestData.isPropAmountMax() ? 1 : 0;
            barImg.fillAmount = barProgress;
        }

        private async void onBtnActivity()
        {
            LoadingUIManager.instance.openBGWithAutoClose();
            btnActivity.interactable = false;
            GamePauseManager.gamePause();
            switch (UtilServices.getNowScreenOrientation)
            {
                case ScreenOrientation.Portrait:
                case ScreenOrientation.PortraitUpsideDown:
                    await UIRootChangeScreenServices.Instance.justChangeScreenToLand();
                    break;
            }

            ActivityPageData.instance.openActivityPage(ActivityQuestData.nowActivityID);
            await Task.Delay(TimeSpan.FromSeconds(1.0f));
            btnActivity.interactable = true;

        }

        private async void onBtnDogQuest()
        {
            btnActivity.interactable = false;
            UiManager.getPresenter<ActivityQuestProgressPresenter>().playBoardAni("to_03");
            await Task.Delay(TimeSpan.FromSeconds(1.0f));
            btnActivity.interactable = true;
        }

        public void setDogBtn()
        {
            barObj.gameObject.setActiveWhenChange(false);
            UiManager.bindNode<NoticeItem>(numberNoticeNode.cachedGameObject).setActive(false);
            btnActivity.image.sprite = ActivityQuestData.getIconSprite("save_the_dog");
            btnActivity.onClick.AddListener(onBtnDogQuest);
        }

        public void setActivityBtn()
        {
            btnActivity.onClick.AddListener(onBtnActivity);
            btnActivity.image.sprite = ActivityQuestData.getIconSprite(ActivityQuestData.nowActivityObjData.spriteName);
        }

        public void setActivityIcon()
        {
            btnActivity.image.sprite = ActivityQuestData.getIconSprite(ActivityQuestData.nowActivityObjData.spriteName);
        }

        public void setPropAmount()
        {
            int amount = ActivityQuestData.nowTicketAmount;
            string amountStr = amount > 99 ? "99+" : amount.ToString();
            UiManager.bindNode<NoticeItem>(numberNoticeNode.cachedGameObject).setAmount(amountStr);
        }

        private void playBarAni()
        {
            string aniName = ActivityQuestData.progressPercentage * 0.01f >= 1 && ActivityQuestData.isPropAmountMax() ? "max" : "get";
            progressAni.SetTrigger(aniName);
        }

        public void setProgressPercentage()
        {
            if (ActivityQuestData.isPropAmountMax())
            {
                ActivityQuestData.progressPercentage = 100;
            }
            else if (ActivityQuestData.progressPercentage >= 100)
            {
                ActivityQuestData.progressPercentage = 0;
            }
            barImg.fillAmount = ActivityQuestData.progressPercentage * 0.01f;
            progress.text = $"{ActivityQuestData.progressPercentage}%";
            playBarAni();
        }
    }

    public class NoticeItem : NodePresenter
    {
        private RectTransform noticeObj;
        private Text notice;
        public override void initUIs()
        {
            noticeObj = getRectData("notice_rect");
            notice = getTextData("notice_txt");
        }

        public void setAmount(string amount)
        {
            notice.text = amount;
        }

        public void setActive(bool active)
        {
            noticeObj.gameObject.setActiveWhenChange(active);
        }
    }

    public class DogQuestItem : NodePresenter
    {
        #region UI Obj
        private Button btnQuest;
        private Animator progressAni;
        private Text progress;
        private Text questTips;
        private Animator tipsAni;
        private Image barImg;
        #endregion

        #region Other
        private IDisposable closeTip;
        private bool isTipsOpen = false;
        #endregion

        public override void initUIs()
        {
            btnQuest = getBtnData("quest_btn");
            progressAni = getAnimatorData("progress_ani");
            progress = getTextData("progress_txt");
            questTips = getTextData("tips_txt");
            tipsAni = getAnimatorData("tips_ani");
            barImg = getImageData("bar_img");
        }

        public override void init()
        {
            btnQuest.onClick.AddListener(onBtnQuest);
            tipsAni.SetTrigger("out");
            barImg.fillAmount = 0;
            progressAni.SetTrigger("get");
        }

        private void onBtnQuest()
        {
            if (isTipsOpen)
            {
                return;
            }

            checkShowDirection();
            showTips(5);
        }

        public void showTips(int showTime)
        {
            tipsAni.SetTrigger("in");
            isTipsOpen = true;
            closeTip = Observable.Timer(TimeSpan.FromSeconds(showTime)).Subscribe(_ =>
            {
                tipsAni.SetTrigger("out");
                isTipsOpen = false;
                closeTip.Dispose();
            }).AddTo(uiGameObject);
        }

        private void checkShowDirection()
        {
            var tipsNode = tipsAni.GetComponent<RectTransform>();
            var textNode = questTips.GetComponent<RectTransform>();
            float rotationY = UiManager.getPresenter<ActivityQuestProgressPresenter>().uiRectTransform.anchoredPosition.x > 0 ? 180 : 0;
            tipsNode.eulerAngles = new Vector3(0, rotationY, 0);
            textNode.eulerAngles = new Vector3(0, tipsNode.transform.rotation.y, 0);
        }

        public void setQuestInfo(int questInfex, bool isInit = false)
        {
            var mission = ActivityQuestData.missions[questInfex];
            var questInfoList = mission.progress;
            string questInfo = questInfoList.type;
            btnQuest.image.sprite = ActivityQuestData.getQuestImage(questInfo);
            if (isInit)
            {
                string[] conditionMsg = ActivityQuestData.convertToConditionsMsg(questInfoList.conditions);
                questTips.text = ActivityQuestData.getQuestInfoContent(questInfo, conditionMsg);
            }

            float questAmounts = (float)mission.progress.amounts[0];
            ulong questConditions = (ulong)questInfoList.conditions[0];
            float questAmount = isInit ? questAmounts : ActivityQuestData.dogQuestProgress[questInfex];
            float percentage = Math.Min(1f, questAmount / questConditions);
            updateQuestProgress(percentage, isInit);
        }

        private void updateQuestProgress(float progressValue, bool isInit)
        {
            if (barImg.fillAmount < 1)
            {
                float currentFillAmount = barImg.fillAmount;
                if (currentFillAmount != progressValue)
                {
                    string aniName = progressValue >= 1 ? "max" : "get";
                    progressAni.SetTrigger(aniName);
                }

                float fakePrecentage = ActivityQuestData.fakePrecentage;
                progress.text = progressValue >= fakePrecentage && progressValue < 1 ? $"{fakePrecentage * 100}%" : $"{Math.Round(progressValue * 100)}%";
                barImg.fillAmount = progressValue;
            }

            if (!isInit)
            {
                ActivityQuestData.checkAutoSpinIsStop();
            }
        }
    }
}
