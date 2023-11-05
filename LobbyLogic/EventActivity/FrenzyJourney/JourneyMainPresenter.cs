using CommonILRuntime.Module;
using CommonILRuntime.BindingModule;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;
using UniRx;
using System.Threading.Tasks;

namespace FrenzyJourney
{
    public class JourneyMainPresenter : ContainerPresenter
    {
        public override string objPath => FrenzyJourneyData.getInstance.getPrefabFullPath("activity_fj_main");

        public override UiLayer uiLayer { get => UiLayer.GameMessage; }
        protected override BackHideBehaviour hideBehaviour => BackHideBehaviour.CanDoBoth;

        RectTransform layoutGroup;
        RectTransform contentRect;
        RectTransform bgRect;
        ScrollRect mapScroll;

        RectTransform tutorialTreasureRect;
        GameObject tempNormalGoundObj;
        PoolObject endGroud;
        string mapScrollTween;
        int normalGroundChildCount;
        int endGroundChildCount;
        int[] nowMapInfos;
        List<JourneyGroundNodePresenter> groundNodePresenters = new List<JourneyGroundNodePresenter>();
        Dictionary<int, int> bossMapIDDict;
        List<float> bgWidths;
        List<long> historyMapIDList;
        public JourneyGroundNodePresenter nowGound { get; private set; }
        JourneyGroundNodePresenter preGround;
        public int nowItemID { get; private set; }
        Vector2 mapScrollEndPos;
        public override void initContainerPresenter()
        {
            resOrder = new string[] { AssetBundleData.getBundleName(BundleType.FrenzyJourney)};
            base.initContainerPresenter();
        }

        public override void initUIs()
        {
            layoutGroup = getBindingData<RectTransform>("group_laout");
            contentRect = getBindingData<RectTransform>("content_trans");
            bgRect = getBindingData<RectTransform>("bg_trans");
            mapScroll = getBindingData<ScrollRect>("map_rect");
            tutorialTreasureRect = getRectData("guild_treasure_rect");
        }

        public override void init()
        {
            //mapScroll.onValueChanged.AddListener(onValChange);
            tutorialTreasureRect.gameObject.setActiveWhenChange(false);
            tempNormalGoundObj = ResourceManager.instance.getGameObjectWithResOrder(FrenzyJourneyData.getInstance.getPrefabFullPath("ground_group"),resOrder);
            endGroud = ResourceManager.instance.getObjectFromPoolWithResOrder(FrenzyJourneyData.getInstance.getPrefabFullPath("ground_group_end"), layoutGroup,resNames:resOrder);
            normalGroundChildCount = tempNormalGoundObj.transform.childCount;
            endGroundChildCount = endGroud.cachedRectTransform.childCount;
        }

        public void setGroundLayoutActvie(bool active)
        {
            layoutGroup.gameObject.setActiveWhenChange(active);
        }

        public Transform getTutorialTreasureRect()
        {
            return tutorialTreasureRect;
        }

        public void changeLastGroundCoinBooster(bool isInCoinChange)
        {
            for (int i = nowGound.groundId - 1; i < groundNodePresenters.Count; ++i)
            {
                groundNodePresenters[i].changeLastItemCoinBoost(isInCoinChange);
            }
        }

        //void onValChange(Vector2 pos)
        //{
        //    mapScrollEndPos = pos;
        //    Debug.Log($"Map onValChange : {pos}");
        //}

        void setContentSize()
        {
            bgWidths = new List<float>();
            float bgTotalWidth = 0;
            float bgTotalHeight = 0;
            for (int i = 0; i < bgRect.childCount; ++i)
            {
                var bgChild = bgRect.GetChild(i).GetComponent<RectTransform>();
                bgWidths.Add(bgChild.sizeDelta.x);
                bgTotalWidth += bgChild.sizeDelta.x;
                bgTotalHeight = bgChild.sizeDelta.y;
            }
            float rootWidth = UiRoot.instance.gameMessageRoot.gameObject.GetComponent<RectTransform>().rect.width;
            contentRect.sizeDelta = new Vector2(bgTotalWidth - rootWidth, bgTotalHeight);
        }

        public void setNowLvMap(int mapID, long[][] historyMapIDs)
        {
            setContentSize();
            if (mapID < 0 || mapID > FrenzyJourneyData.getInstance.nabobMapInfo.Length - 1)
            {
                Debug.LogError($"getMapInfo Error: mapId = {mapID} , infoLength : {FrenzyJourneyData.getInstance.nabobMapInfo.Length}");
            }
            setNowMapInfo(mapID);
            parseHistoryArray(historyMapIDs);
            int normalGroundCount = (nowMapInfos.Length - endGroundChildCount) / normalGroundChildCount;
            for (int i = 0; i < normalGroundCount; ++i)
            {
                var groundObj = ResourceManager.instance.getObjectFromPool(tempNormalGoundObj, layoutGroup);
                bindingGroundNodePresenter(groundObj.cachedGameObject);
            }
            bindingGroundNodePresenter(endGroud.cachedGameObject, isEndGround: true);
            endGroud.transform.SetAsLastSibling();
            FrenzyJourneyData.getInstance.isCoinBoosterSub.Subscribe(changeLastGroundCoinBooster);
        }

        void parseHistoryArray(long[][] history)
        {
            historyMapIDList = new List<long>();
            if (null == history || history.Length <= 0)
            {
                return;
            }
            historyMapIDList.AddRange(history[0]);

            for (int i = 1; i < history.Length; ++i)
            {
                long[] lvHistory = history[i];
                int bossMapID;
                if (!bossMapIDDict.TryGetValue(i, out bossMapID))
                {
                    break;
                }

                for (int j = 0; j < lvHistory.Length; ++j)
                {
                    historyMapIDList.Add(lvHistory[j] + bossMapID);
                }
            }
        }

        public void refreshMapInfo(int mapID)
        {
            setNowMapInfo(mapID);
            lvDataSpliteId = 0;
            for (int i = 0; i < groundNodePresenters.Count; ++i)
            {
                var ground = groundNodePresenters[i];
                ground.refreshGroundItemData(spliteGroundMapInfo(ground.totalItemCount));
            }
        }

        void setNowMapInfo(int mapID)
        {
            //mapID = 0;
            nowMapInfos = FrenzyJourneyData.getInstance.nabobMapInfo[mapID];
            bossMapIDDict = new Dictionary<int, int>();
            int bossLv = 0;

            for (int i = 0; i < nowMapInfos.Length; ++i)
            {
                if (nowMapInfos[i] == (int)MapItemType.Boss)
                {
                    bossLv++;
                    bossMapIDDict.Add(bossLv, i + 1);
                }
            }
        }

        int lvDataSpliteId = 0;
        int groundID = 1;

        void bindingGroundNodePresenter(GameObject groundObj, bool isEndGround = false)
        {
            groundObj.name = $"Ground_{groundID}";
            JourneyGroundNodePresenter groundNode = UiManager.bind<JourneyGroundNodePresenter>(groundObj);
            groundNode.setGroundItem(groundID, spliteGroundMapInfo(groundNode.uiTransform.childCount));
            List<long> historyID = historyMapIDList.FindAll(id => id > lvDataSpliteId - groundObj.transform.childCount && id <= lvDataSpliteId);
            groundNode.hideHistoryItem(historyID, isEndGround);
            groundObj.setActiveWhenChange(true);
            groundNodePresenters.Add(groundNode);
            groundID++;
        }

        int[] spliteGroundMapInfo(int totalItemCount)
        {
            int[] spliteLvData = new int[totalItemCount];
            Array.Copy(nowMapInfos, lvDataSpliteId, spliteLvData, 0, spliteLvData.Length);
            lvDataSpliteId += spliteLvData.Length;
            return spliteLvData;
        }

        public void playRewardItemAnim(Action playCB)
        {
            getNowItemNodePresenter().playAwardAnim(playCB);
        }

        public void closeRewardItemParticleObj()
        {
            getNowItemNodePresenter().closeParticleObj();
        }

        public GroundItemNodePresenter getNowItemNodePresenter()
        {
            return nowGound.getGroundItemNode(nowItemID);
        }

        public void clearAllItemCoinBoost()
        {
            for (int i = 0; i < groundNodePresenters.Count; ++i)
            {
                groundNodePresenters[i].clearItemCoinBoost();
            }
        }

        #region GetTrans
        int totalItemID = 0;
        void setNowGround(int addProgress)
        {
            preGround = nowGound;
            totalItemID += addProgress;
            nowItemID = totalItemID;
            for (int i = 0; i < groundNodePresenters.Count; ++i)
            {
                nowGound = groundNodePresenters[i];
                int groundTotalItemCount = nowGound.totalItemCount;
                if (nowItemID <= groundTotalItemCount)
                {
                    break;
                }
                nowItemID -= groundTotalItemCount;
            }
        }

        public Queue<ItemGroupPresenter> getItemGroupList(int addProgress)
        {
            setNowGround(addProgress);
            Queue<ItemGroupPresenter> result = new Queue<ItemGroupPresenter>();
            List<ItemGroupPresenter> itemTrans;
            int startID = nowItemID - addProgress + 1;
            //Debug.Log($"getItemTransList startID : {startID} , nowItemID:{nowItemID} ,addProgress : {addProgress} ");
            if (startID <= 0)
            {
                var lastGround = groundNodePresenters[nowGound.groundId - 2];
                itemTrans = lastGround.getItemTransList(lastGround.totalItemCount - Math.Abs(startID), lastGround.totalItemCount);
                for (int i = 0; i < itemTrans.Count; ++i)
                {
                    result.Enqueue(itemTrans[i]);
                }
            }
            itemTrans = nowGound.getItemTransList(Math.Max(1, startID), nowItemID);
            for (int i = 0; i < itemTrans.Count; ++i)
            {
                result.Enqueue(itemTrans[i]);
            }
            return result;
        }

        public ItemGroupPresenter getNowGroundItem(int mapLv, int progress)
        {
            if (mapLv <= 1 && progress <= 1)
            {
                totalItemID = 0;
                progress = 1;
            }
            int preTotalItemID = 0;
            bossMapIDDict.TryGetValue(mapLv - 1, out preTotalItemID);
            setNowGround(preTotalItemID + progress);
            nowItemID = Math.Max(1, nowItemID);
            return nowGound.getItemGroup(nowItemID);
        }

        public List<JourneyGroundNodePresenter> getRemainingGroup()
        {
            return groundNodePresenters.FindAll(ground => ground.groundId >= nowGound.groundId);
        }

        public ItemGroupPresenter getItemNode(int itemId)
        {
            return nowGound.getItemGroup(itemId);
        }
        #endregion
        #region MoveMap
        public void checkIsMoveMap(Action onComplete = null)
        {
            if (!isMoveMap())
            {
                return;
            }

            moveToNowProgress(onComplete);
        }

        public bool isMoveMap()
        {
            if (nowGound.groundId == groundNodePresenters.Count)
            {
                return false;
            }
            return preGround.groundId != nowGound.groundId;
        }

        public bool isLastMap { get { return nowGound.groundId == groundNodePresenters.Count; } }

        public void moveToNowProgress(Action onComplete = null)
        {
            if (nowGound.groundId == groundNodePresenters.Count)
            {
                moveToMapEnd(onComplete);
                return;
            }
            float nowGroundWidth = nowGound.uiRectTransform.sizeDelta.x;
            float totalGroudWidth = nowGroundWidth * (nowGound.groundId - 1);
            float totalItemWidth = (nowGroundWidth / nowGound.totalItemCount) * nowItemID;
            moveMap(totalGroudWidth + totalItemWidth, onComplete);
        }

        public void moveToMapEnd(Action onComplete = null)
        {
            moveMap(contentRect.sizeDelta.x, onComplete);
        }

        public void moveToAssignBG(int bgID, Action onComplete = null)
        {
            if (bgID <= 1)
            {
                moveMap(0);
                return;
            }

            float moveY = 0;
            for (int i = 0; i < bgID; ++i)
            {
                moveY += bgWidths[i];
            }
            moveMap(moveY, onComplete);
        }

        public void moveMap(float endPosX, Action onComplete = null)
        {
            float movePosX = Mathf.Min(1, endPosX / contentRect.sizeDelta.x);
            //Debug.Log($"moveMap? {movePosX} , {mapScroll.normalizedPosition.x} - endPosX:{endPosX}");
            if (movePosX == mapScroll.normalizedPosition.x)
            {
                return;
            }
            if (!string.IsNullOrEmpty(mapScrollTween))
            {
                TweenManager.tweenKill(mapScrollTween);
            }
            mapScrollTween = TweenManager.tweenToFloat(mapScroll.normalizedPosition.x, movePosX, (float)TimeSpan.FromSeconds(0.3f).TotalSeconds, onUpdate: changeMapNormalPos, onComplete: () =>
            {
                changeMapNormalPos(movePosX);
                mapScrollEndPos = mapScroll.normalizedPosition;
                if (null != onComplete)
                {
                    onComplete();
                }
            });

            TweenManager.tweenPlay(mapScrollTween);
        }

        void changeMapNormalPos(float posX)
        {
            Vector2 oldPos = mapScroll.normalizedPosition;
            oldPos.Set(posX, 1.0f);
            mapScroll.normalizedPosition = oldPos;
        }

        public async Task checkMapPos()
        {
            if (mapScroll.normalizedPosition.x != mapScrollEndPos.x)
            {
                moveToNowProgress();
                await Task.Delay(TimeSpan.FromSeconds(0.5f));
            }
        }

        public void setScrollEnabel(bool enable)
        {
            mapScroll.enabled = enable;
        }
        #endregion
        GroundItemNodePresenter getFirstBossNode()
        {
            GroundItemNodePresenter bossItem = groundNodePresenters.Find(item => item.getBossItemNode() != null).getBossItemNode();
            return bossItem;
        }
        GroundItemNodePresenter firstBossItem;
        GroundItemNodePresenter tutorialBossNode;
        public GroundItemNodePresenter showTutorialBossItem()
        {
            firstBossItem = getFirstBossNode();
            PoolObject itemNode = ResourceManager.instance.getObjectFromPool(firstBossItem.uiGameObject, firstBossItem.uiRectTransform.parent.transform);
            firstBossItem.bossObj.setActiveWhenChange(false);
            tutorialBossNode = UiManager.bindNode<GroundItemNodePresenter>(itemNode.cachedGameObject);
            tutorialBossNode.bossObj.GetComponent<MeshRenderer>().sortingOrder = 106;
            tutorialBossNode.floorObj.setActiveWhenChange(false);
            return tutorialBossNode;
        }

        public void resetTutorialItem()
        {
            firstBossItem.bossObj.setActiveWhenChange(true);
        }

        public override void clear()
        {
            for (int i = 0; i < groundNodePresenters.Count; ++i)
            {
                var groundNode = groundNodePresenters[i];
                groundNode.releaseItemPoolObj();
                ResourceManager.instance.releasePoolWithObj(groundNode.uiGameObject);
            }
            groundNodePresenters.Clear();
            base.clear();
        }
    }
}
