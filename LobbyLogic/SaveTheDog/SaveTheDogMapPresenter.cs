using Debug = UnityLogUtility.Debug;
using System.Collections.Generic;
using CommonILRuntime.Module;
using UnityEngine.UI;
using UnityEngine;
using Binding;
using Services;
using CommonILRuntime.BindingModule;
using CommonService;
using NewPlayerGuide;
using System;
using UniRx;
using Service;
using System.Threading.Tasks;
using LobbyLogic.Audio;
using Lobby.Audio;

namespace SaveTheDog
{
    /// <summary>
    /// 關卡種類
    /// </summary>
    public class SaveTheDogMapPresenter : ContainerPresenter
    {
        public override string objPath => "prefab/save_the_dog/save_the_dog_map_main";
        public override UiLayer uiLayer { get { return UiLayer.GameMessage; } }
        protected override BackHideBehaviour hideBehaviour => BackHideBehaviour.CanDoBoth;

        #region UI Obj
        RectTransform lvLayoutRect;
        BindingNode mapLineNode;
        GameObject startArrowObj;
        RectTransform mapScrollContent;
        ScrollRect mapScrollView;
        RectTransform bgLayoutRect;
        #endregion

        #region Prefab Path
        public readonly string PREFAB_PATH = "prefab/save_the_dog";
        public readonly string SLOT_REWARDS_OBJ = "slot_rewards";
        public readonly string REWARD_ITEM_OBJ = "reward_item";
        #endregion

        #region Other
        //小關卡物件清單
        public List<PoolObject> poolObjs = new List<PoolObject>();
        //獲獎物件清單
        public List<PoolObject> rewardList = new List<PoolObject>();
        #endregion

        int showStageID;

        Dictionary<SaveDogLvKind, BindingNode> lvNode = new Dictionary<SaveDogLvKind, BindingNode>();

        //SaveTheDogMapBtnDoge firstDogBtn;
        SaveTheDogMapUIPresenter mapUIPresenter;
        List<MapLineNode> lineNodes = new List<MapLineNode>();
        List<IPlayUnlockBtn> lvBtnNode = new List<IPlayUnlockBtn>();
        List<StageBGInfo> bgLayouts = new List<StageBGInfo>();
        string tweenMoveKey;
        SaveTheDogMapBtnTreasure nowTreasureBtn;
        public override void initContainerPresenter()
        {
            resOrder = new string[] { AssetBundleData.getBundleName(BundleType.SaveTheDog) };
            base.initContainerPresenter();
        }
        public override void initUIs()
        {
            lvNode.Add(SaveDogLvKind.Gift, getNodeData("btn_gift_node"));
            lvNode.Add(SaveDogLvKind.Doge, getNodeData("btn_doge_node"));
            lvNode.Add(SaveDogLvKind.Slot, getNodeData("btn_slot_node"));
            lvNode.Add(SaveDogLvKind.Treasure, getNodeData("btn_treasure_node"));

            lvLayoutRect = getRectData("level_layout_rect");
            mapLineNode = getNodeData("map_line_node");
            startArrowObj = getGameObjectData("map_start_arrow_obj");
            mapScrollContent = getRectData("map_scroll_content");
            mapScrollView = getBindingData<ScrollRect>("map_scroll");
            bgLayoutRect = getRectData("bg_layout_rect");

            bgLayouts.Add(new StageBGInfo()
            {
                cloudObj = getGameObjectData("clouds_normal_obj"),
                bgObj = getGameObjectData("bg_group_normal")
            });

            bgLayouts.Add(new StageBGInfo()
            {
                cloudObj = getGameObjectData("clouds_dusk_obj"),
                bgObj = getGameObjectData("bg_group_dusk")
            });

            bgLayouts.Add(new StageBGInfo()
            {
                cloudObj = getGameObjectData("clouds_night_obj"),
                bgObj = getGameObjectData("bg_group_night")
            });

            for (int i = 0; i < bgLayouts.Count; ++i)
            {
                var layout = bgLayouts[i];
                layout.bgObj.setActiveWhenChange(false);
                layout.cloudObj.setActiveWhenChange(false);
            }
        }

        public override void init()
        {
            var lvTempObjEnum = lvNode.GetEnumerator();
            while (lvTempObjEnum.MoveNext())
            {
                lvTempObjEnum.Current.Value.cachedGameObject.setActiveWhenChange(false);
            }
            mapLineNode.cachedGameObject.setActiveWhenChange(false);
            startArrowObj.setActiveWhenChange(false);
            DataStore.getInstance.extraGameServices.gameSwitchSubject.Subscribe(gameSwitch).AddTo(uiGameObject);
        }
        float lvPosX = 0;
        void gameSwitch(bool isSwitch)
        {
            if (isSwitch)
            {
                initToLobbyServices();
                return;
            }

            if (!isSwitch)
            {
                disposeGameServices();
                TransitionSaveDogServices.instance.openTransitionPage();
                Observable.Timer(TimeSpan.FromSeconds(0.8f)).Subscribe(_ =>
                {
                    AudioManager.instance.playBGM(AudioPathProvider.getAudioPath(SaveTheDogMapAudio.MainBgm));
                }).AddTo(uiGameObject);
            }
            if (SaveTheDogMapData.instance.nowClickID < SaveTheDogMapData.instance.nowLvID - 1 || SaveTheDogMapData.instance.nowOpenStageID != SaveTheDogMapData.instance.nowStageID)
            {
                TransitionSaveDogServices.instance.closeTransitionPage();
                return;
            }

            if (SaveTheDogMapData.instance.nowClickID == SaveTheDogMapData.instance.nowLvID && SaveTheDogMapData.instance.isLvAlreadyOpen)
            {
                TransitionSaveDogServices.instance.closeTransitionPage();
                return;
            }
            TransitionSaveDogServices.instance.closeTransitionPage();
            if (mapScrollView.normalizedPosition.x < 1)
            {
                moveToNowLvPos(playUnlock);
                return;
            }
            playUnlock();
        }

        async void playUnlock()
        {
            if (SaveTheDogMapData.instance.isAlreadyPlayGrow)
            {
                return;
            }
            SaveTheDogMapData.instance.setIsAlreadyGrow(true);
            lvBtnNode[SaveTheDogMapData.instance.nowLvID - 1].playDone();
            await Task.Delay(TimeSpan.FromSeconds(1.0f));
            playNowUnlockAnim();
        }

        private async Task getMapInfo()
        {
            var mapInfo = await WebRequestText.instance.loadTextFromServer("newbie_adventure_setting");
            var mapInfoJson = LitJson.JsonMapper.ToObject<NewbieAdventureSetting>(mapInfo);

            SaveTheDogMapData.instance.setMapInfo(mapInfoJson.newbieAdventureSetting);
            mapUIPresenter = UiManager.getPresenter<SaveTheDogMapUIPresenter>();
            mapUIPresenter.setCloseClickCB(clear);
            mapUIPresenter.close();
            await addLevelObj(SaveTheDogMapData.instance.nowStageID);
            SaveTheDogMapData.instance.nowOpenStageIDSub.Subscribe(updateLvObj).AddTo(uiGameObject);
            if (SaveTheDogMapData.instance.isFirstLv && !SaveTheDogMapData.instance.isLvAlreadyOpen)
            {
                mapScrollView.normalizedPosition = new Vector2(1, mapScrollView.normalizedPosition.y);
            }
            lvPosX = (1.0f / lvLayoutRect.childCount) * 2;
            await Task.Delay(TimeSpan.FromSeconds(1.0f));
        }

        public override async void open()
        {
            await getMapInfo();
            Camera.main.orthographic = false;
            base.open();
            if (SaveTheDogMapData.instance.isFirstLv && !SaveTheDogMapData.instance.isLvAlreadyOpen)
            {
                await Task.Delay(TimeSpan.FromSeconds(1.5f));
            }
            SaveTheDogMapData.instance.isOpenSaveTheDog = false;
            AudioManager.instance.stopBGM();
            AudioManager.instance.playBGM(AudioPathProvider.getAudioPath(SaveTheDogMapAudio.MainBgm));

            TransitionxPartyServices.instance.closeTransitionPage();
            TransitionSaveDogServices.instance.closeTransitionPage();

            mapUIPresenter.open();
            if (SaveTheDogMapData.instance.isFirstLv && !SaveTheDogMapData.instance.isLvAlreadyOpen)
            {
                await Task.Delay(TimeSpan.FromSeconds(1.0f));
                startRunToFirst();
                return;
            }

            if (SaveDogLvKind.Treasure == SaveTheDogMapData.instance.getNowRecordKind() || SaveTheDogMapData.instance.maxLevelAmountList[SaveTheDogMapData.instance.nowStageID] - SaveTheDogMapData.instance.nowLvID <= 3)
            {
                runToLast();
                return;
            }

            if (SaveTheDogMapData.instance.nowLvID >= 4)
            {
                moveToNowLvPos(openRareitemBoard);
            }
        }

        void moveToNowLvPos(Action complete = null)
        {
            float endX = lvPosX * SaveTheDogMapData.instance.nowLvID;
            scrollMovePos(mapScrollView.normalizedPosition.x, endX, complete);
        }

        async void openRareitemBoard()
        {
            if (!SaveTheDogMapData.instance.isDogGuideComplete && SaveTheDogMapData.instance.nowLvID == 5 && !SaveTheDogMapData.instance.isLvAlreadyOpen)
            {
                bool alreadyPlay = await checkActivityAlready();
                if (alreadyPlay)
                {
                    return;
                }
                UiManager.getPresenter<RareitemBoardPresenter>().open();
            }
        }

        async Task<bool> checkActivityAlready()
        {
            var actRes = await AppManager.lobbyServer.getActivity();
            EventActivity.ActivityDataStore.nowActivityInfo = actRes.activity;
            var baseInitActivityResponse = await AppManager.eventServer.getAppleFarmInitData();
            if (null == baseInitActivityResponse.ClickHistory)
            {
                return true;
            }
            for (int i = 0; i < baseInitActivityResponse.ClickHistory.Length; ++i)
            {
                if (baseInitActivityResponse.ClickHistory[i] == 1)
                {
                    return true;
                }
            }
            return false;
        }

        void initToLobbyServices()
        {
            FromGameMsgService.getInstance.initFromGameService();
            EventInGameService.getInstance.initFuncInGameService();
        }
        void disposeGameServices()
        {
            FromGameMsgService.getInstance.disposeGameMsgService();
            EventInGameService.getInstance.clearGameServices();
        }

        async void runToLast()
        {
            await Task.Delay(TimeSpan.FromSeconds(0.8f));
            scrollMovePos(0, 1, () =>
            {
                if (null != nowTreasureBtn && SaveTheDogMapData.instance.getNowRecordKind() == SaveDogLvKind.Treasure && !SaveTheDogMapData.instance.isAlreadyReward)
                {
                    nowTreasureBtn.clickRedeem();
                }
            });
        }

        async void startRunToFirst()
        {
            await Task.Delay(TimeSpan.FromSeconds(0.8f));
            scrollMovePos(mapScrollView.normalizedPosition.x, 0, complete: playNowUnlockAnim);
        }

        void scrollMovePos(float startX, float endX, Action complete = null)
        {
            if (endX > 1)
            {
                endX = 1;
            }

            if (mapScrollView.normalizedPosition.x == endX)
            {
                if (null != complete)
                {
                    complete();
                }
                return;
            }
            tweenMoveKey = TweenManager.tweenToFloat(startX, endX, durationTime: 1.0f, onUpdate: (moveX) =>
             {
                 var pos = mapScrollView.normalizedPosition;
                 pos.Set(moveX, pos.y);
                 mapScrollView.normalizedPosition = pos;
             }, onComplete: () =>
             {
                 tweenMoveKey = string.Empty;
                 if (null != complete)
                 {
                     complete();
                 }
             });
        }

        void playNowUnlockAnim()
        {
            if (lvBtnNode.Count <= 0)
            {
                return;
            }
            mapScrollView.enabled = false;
            lineNodes[SaveTheDogMapData.instance.nowLvID].playGrowAnim(() =>
           {
               if (SaveTheDogMapData.instance.getNowRecordKind() != SaveDogLvKind.Treasure)
               {
                   lvBtnNode[SaveTheDogMapData.instance.nowLvID].playUnLockAnim(() =>
                   {
                       mapScrollView.enabled = true;
                   });
               }
               else if (null != nowTreasureBtn)
               {
                   nowTreasureBtn.clickRedeem();
               }
               mapScrollView.enabled = true;
           });
        }

        async void updateLvObj(int stageID)
        {
            clearPoolObjs();
            mapScrollView.normalizedPosition = Vector2.zero;
            await addLevelObj(stageID);
            await Task.Delay(TimeSpan.FromSeconds(1.5f));
            TransitionSaveDogServices.instance.closeTransitionPage();
        }

        async Task addLevelObj(int stageIndex)
        {
            lvBtnNode.Clear();
            lineNodes.Clear();
            showStageID = stageIndex;
            startArrowObj.transform.SetParent(lvLayoutRect);
            startArrowObj.gameObject.setActiveWhenChange(true);

            var levelInfo = SaveTheDogMapData.instance.levelInfoList[stageIndex];
            for (var i = 0; i < levelInfo.Count; i++)
            {
                var lvInfo = SaveTheDogMapData.instance.levelInfoList[stageIndex][i];
                string gameKindStr = lvInfo.kind;
                createMapLine();
                BindingNode lvNodeObj;
                if (!lvNode.TryGetValue(lvInfo.lvKind, out lvNodeObj))
                {
                    continue;
                }
                PoolObject levelObj = createPoolObj(lvNodeObj.cachedGameObject);
                SaveDogLvKind gameKind = SaveDogLvKind.None;
                UtilServices.enumParse(gameKindStr, out gameKind);
                initBtn(gameKind, lvInfo, levelObj.cachedGameObject, i);
            }
            for (int i = 0; i < lineNodes.Count; ++i)
            {
                if (i > SaveTheDogMapData.instance.nowLvID && stageIndex >= SaveTheDogMapData.instance.nowStageID)
                {
                    break;
                }
                lineNodes[i].setFull();
            }
            await Task.Delay(TimeSpan.FromSeconds(0.3f));
            mapScrollContent.sizeDelta = new Vector2(lvLayoutRect.offsetMax.x + 120, lvLayoutRect.sizeDelta.y);
            setStageBG(stageIndex);
        }

        void setStageBG(int stageID)
        {
            for (int i = 0; i < bgLayouts.Count; ++i)
            {
                bgLayouts[i].cloudObj.setActiveWhenChange(i == stageID);
            }

            for (int i = bgLayoutRect.childCount - 1; i >= 0; --i)
            {
                GameObject.DestroyImmediate(bgLayoutRect.GetChild(i).gameObject);
            }
            var bgLayoutObjRect = bgLayouts[stageID].bgObj.transform as RectTransform;
            int createBGCount = (int)Mathf.Ceil(mapScrollContent.sizeDelta.x / bgLayoutObjRect.rect.width);
            for (int i = 0; i <= createBGCount; ++i)
            {
                GameObject.Instantiate(bgLayouts[stageID].bgObj, bgLayoutRect).setActiveWhenChange(true);
            }
        }

        PoolObject createPoolObj(GameObject tempObj)
        {
            var poolObj = ResourceManager.instance.getObjectFromPool(tempObj, lvLayoutRect);
            poolObj.cachedGameObject.setActiveWhenChange(true);
            poolObjs.Add(poolObj);
            return poolObj;
        }

        void createMapLine()
        {
            var poolObj = createPoolObj(mapLineNode.cachedGameObject);
            lineNodes.Add(UiManager.bindNode<MapLineNode>(poolObj.cachedGameObject));
        }

        private void initBtn(SaveDogLvKind btnType, LevelData lvData, GameObject btnObj, int levelIndex)
        {
            switch (btnType)
            {
                case SaveDogLvKind.Slot:
                    var btnSlot = UiManager.bindNode<SaveTheDogMapBtnSlot>(btnObj);
                    btnSlot.setLvContent(showStageID, levelIndex);
                    btnSlot.setSlotGameID(lvData.type);
                    btnSlot.clicklvID.Subscribe(SaveTheDogMapData.instance.setNowClickLvID).AddTo(btnSlot.uiGameObject);
                    lvBtnNode.Add(btnSlot);
                    break;
                case SaveDogLvKind.Doge:
                    var btnDoge = UiManager.bindNode<SaveTheDogMapBtnDoge>(btnObj);
                    btnDoge.setLvContent(showStageID, levelIndex);
                    btnDoge.clicklvID.Subscribe(SaveTheDogMapData.instance.setNowClickLvID).AddTo(btnDoge.uiGameObject);
                    int lvID;
                    int.TryParse(lvData.type, out lvID);
                    btnDoge.setDogInfo(lvID, lvData.role);
                    lvBtnNode.Add(btnDoge);
                    break;
                case SaveDogLvKind.Gift:
                    var btnGift = UiManager.bindNode<SaveTheDogMapBtnGift>(btnObj);
                    btnGift.playNextBtnUnLock = playUnlock;
                    btnGift.setLvContent(levelIndex);
                    lvBtnNode.Add(btnGift);
                    break;
                case SaveDogLvKind.Treasure:
                    nowTreasureBtn = UiManager.bindNode<SaveTheDogMapBtnTreasure>(btnObj);
                    nowTreasureBtn.setTotalReward(SaveTheDogMapData.instance.treasureRewardMoney);
                    nowTreasureBtn.setLVID(levelIndex);
                    bool isDone = SaveTheDogMapData.instance.nowStageID > SaveTheDogMapData.instance.nowOpenStageID;
                    if (SaveTheDogMapData.instance.nowOpenStageID >= SaveTheDogMapData.instance.maxStageAmount - 1 && SaveTheDogMapData.instance.nowLvID >= SaveTheDogMapData.instance.maxLevelAmountList[SaveTheDogMapData.instance.nowStageID] - 1)
                    {
                        isDone = SaveTheDogMapData.instance.isAlreadyReward;
                    }
                    nowTreasureBtn.setDoneStatus(isDone);
                    break;
                default:
                    Debug.LogError("btnNode Type Error !!!");
                    break;
            }
        }

        public override void clear()
        {
            if (!string.IsNullOrEmpty(tweenMoveKey))
            {
                TweenManager.tweenKill(tweenMoveKey);
            }
            clearPoolObjs();
            disposeGameServices();
            Camera.main.orthographic = true;
            base.clear();

        }
        void clearPoolObjs()
        {
            for (int i = 0; i < poolObjs.Count; ++i)
            {
                ResourceManager.instance.returnObjectToPool(poolObjs[i].cachedGameObject);
            }

            var poolTempObjs = lvNode.GetEnumerator();
            while (poolTempObjs.MoveNext())
            {
                ResourceManager.instance.releasePoolWithObj(poolTempObjs.Current.Value.cachedGameObject);
            }

            ResourceManager.instance.releasePoolWithObj(mapLineNode.cachedGameObject);

            poolObjs.Clear();
        }
    }

    public enum GameKind
    {
        None,
        Slot,
        Gift,
        Doge,
        Treasure,
    }

    class StageBGInfo
    {
        public GameObject cloudObj;
        public GameObject bgObj;
    }
}
