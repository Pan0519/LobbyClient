using CommonILRuntime.Module;
using UnityEngine;
using UnityEngine.UI;
using CommonILRuntime.BindingModule;
using System.Collections.Generic;
using UniRx;
using UniRx.Triggers;
using System;
using Services;
using Common.Jigsaw;
using Debug = UnityLogUtility.Debug;

namespace FrenzyJourney
{
    public class JourneyGroundNodePresenter : NoBindingNodePresenter
    {
        List<GroundItemNodePresenter> groundItemNodes = new List<GroundItemNodePresenter>();
        Dictionary<int, ItemGroupPresenter> itemGroupDict = new Dictionary<int, ItemGroupPresenter>();

        public int totalItemCount { get { return groundItemNodes.Count; } }
        public int groundId { get; private set; }
        int lastEndID;
        public override void init()
        {
            for (int i = 0; i < uiRectTransform.childCount; ++i)
            {
                GameObject child = uiRectTransform.GetChild(i).gameObject;
                int itemID;
                if (!int.TryParse(child.name.Split('_')[1], out itemID))
                {
                    continue;
                }
                var presenter = UiManager.bind<ItemGroupPresenter>(child);
                presenter.setGroupID(itemID);
                itemGroupDict.Add(itemID, presenter);
            }
        }

        public void setGroundItem(int id, int[] lvData)
        {
            groundId = id;
            for (int i = 0; i < lvData.Length; ++i)
            {
                PoolObject itemObj = ResourceManager.instance.getObjectFromPoolWithResOrder(FrenzyJourneyData.getInstance.getPrefabFullPath("ground_common"), getItemGroup(i + 1).uiRectTransform,resNames:AssetBundleData.getBundleName(BundleType.FrenzyJourney));
                GroundItemNodePresenter itemNodePresenter = UiManager.bindNode<GroundItemNodePresenter>(itemObj.cachedGameObject);
                itemNodePresenter.setItemData(lvData[i]);
                groundItemNodes.Add(itemNodePresenter);
            }
        }

        public void hideHistoryItem(List<long> historyID, bool isEndGround)
        {
            if (historyID.Count <= 0)
            {
                return;
            }
            int id = !isEndGround ? (groundId - 1) : groundId;
            int subtractID = id * groundItemNodes.Count;
            for (int i = 0; i < historyID.Count; ++i)
            {
                int itemID = (int)historyID[i] - subtractID - 1;
                groundItemNodes[itemID].setItemData((int)MapItemType.None);
                lastEndID = itemID + 1;
            }
        }

        public void refreshGroundItemData(int[] lvData)
        {
            lastEndID = 0;
            for (int i = 0; i < lvData.Length; ++i)
            {
                groundItemNodes[i].setItemData(lvData[i]);
            }
        }

        public GroundItemNodePresenter getGroundItemNode(int id)
        {
            if (id > groundItemNodes.Count)
            {
                return null;
            }

            return groundItemNodes[id - 1];
        }

        public GroundItemNodePresenter getBossItemNode()
        {
            return groundItemNodes.Find(item => item.itemType == MapItemType.Boss);
        }

        public ItemGroupPresenter getItemGroup(int id)
        {
            ItemGroupPresenter result;
            if (itemGroupDict.TryGetValue(id, out result))
            {
                return result;
            }
            Debug.LogError($"get {id} Item is null");
            return null;
        }

        public List<ItemGroupPresenter> getItemTransList(int startID, int endID)
        {
            List<ItemGroupPresenter> result = new List<ItemGroupPresenter>();
            lastEndID = endID;
            //Debug.Log($"getItemTransList lastEndID : {endID}");
            for (int id = startID; id <= endID; ++id)
            {
                result.Add(getItemGroup(id));
            }

            return result;
        }

        public void changeLastItemCoinBoost(bool isChange)
        {
            //Debug.Log($"changeLastItemCoinBoost endID : {lastEndID}");
            //if (lastEndID > 0)
            //{
            //    lastEndID++;
            //}

            for (int i = lastEndID; i < groundItemNodes.Count; ++i)
            {
                groundItemNodes[i].isCoinBooster(isChange);
            }
        }

        public void releaseItemPoolObj()
        {
            for (int i = 0; i < groundItemNodes.Count; ++i)
            {
                ResourceManager.instance.releasePoolWithObj(groundItemNodes[i].uiGameObject);
            }

            groundItemNodes.Clear();
        }

        public void clearItemCoinBoost()
        {
            for (int i = 0; i < groundItemNodes.Count; ++i)
            {
                groundItemNodes[i].isCoinBooster(false);
            }
        }
    }

    public class ItemGroupPresenter : NoBindingNodePresenter
    {
        public int groupID { get; private set; }
        public void setGroupID(int groupID)
        {
            this.groupID = groupID;
        }
    }

    public class GroundItemNodePresenter : NodePresenter
    {
        public GameObject floorObj { get; private set; }
        public GameObject bossObj { get; private set; }
        public GameObject chestObj { get; private set; }
        GameObject coinObj;
        Image chestImg;
        GameObject packObj;
        Image packImg;
        GameObject particleObj;
        Animator itemAnim;
        Action animFinishCB;
        public MapItemType itemType { get; private set; }
        public ChangeDirectionType directionType { get; private set; }
        public bool isMapTypeToCoinFromBooster { get; private set; }
        public bool isChest { get; private set; }
        public override void initUIs()
        {
            coinObj = getGameObjectData("coin_obj");
            bossObj = getGameObjectData("boss_obj");
            chestObj = getGameObjectData("chest_obj");
            chestImg = getImageData("chest_img");
            packObj = getGameObjectData("pack_obj");
            packImg = getImageData("pack_img");
            particleObj = getGameObjectData("particle_obj");
            floorObj = getGameObjectData("floor_obj");
            itemAnim = getAnimatorData("item_anim");
        }

        public override void init()
        {
            itemAnim.enabled = false;
        }

        private void animEnterSubscribe(ObservableStateMachineTrigger.OnStateInfo obj)
        {
            animTriggers.Add(Observable.Timer(TimeSpan.FromSeconds(obj.StateInfo.length)).Subscribe(_ =>
            {
                if (null != animFinishCB)
                {
                    animFinishCB();
                }
                UtilServices.disposeSubscribes(animTriggers.ToArray());
                animTriggers.Clear();
            }));
        }

        public void setGroundDirectionType(ChangeDirectionType directionType)
        {
            this.directionType = directionType;
        }

        public void setItemData(int dataType)
        {
            itemType = (MapItemType)dataType;
            coinObj.setActiveWhenChange(MapItemType.Coin == itemType);
            bossObj.setActiveWhenChange(MapItemType.Boss == itemType);
            isChest = dataType >= (int)MapItemType.Wood && dataType <= (int)MapItemType.Gold;
            chestObj.setActiveWhenChange(isChest);
            if (isChest)
            {
                chestImg.sprite = FrenzyJourneyData.getInstance.getIconSprite($"activity_treasure_lv{dataType - 1}");
            }
            bool isOpenParticle = MapItemType.None != itemType && MapItemType.Boss != itemType;
            particleObj.setActiveWhenChange(isOpenParticle);

            bool isPack = dataType >= (long)MapItemType.GreenPack && dataType <= (long)MapItemType.GoldPack;
            packObj.setActiveWhenChange(isPack);
            if (isPack)
            {
                packImg.sprite = JigsawPackSpriteProvider.getPackSprite((PuzzlePackID)dataType);
            }
        }
        List<IDisposable> animTriggers = new List<IDisposable>();
        public void playAwardAnim(Action finishCB)
        {
            animFinishCB = finishCB;
            string triggerName = string.Empty;
            switch (itemType)
            {
                case MapItemType.Coin:
                    triggerName = "coin";
                    break;

                case MapItemType.GoldPack:
                case MapItemType.BluePack:
                case MapItemType.GreenPack:
                    triggerName = "pack";
                    break;
            }

            if (string.IsNullOrEmpty(triggerName))
            {
                return;
            }
            itemAnim.enabled = true;
            var animTrigger = itemAnim.GetBehaviours<ObservableStateMachineTrigger>();
            for (int i = 0; i < animTrigger.Length; ++i)
            {
                animTriggers.Add(animTrigger[i].OnStateEnterAsObservable().ObserveOnMainThread().Subscribe(animEnterSubscribe));
            }
            itemAnim.SetTrigger(triggerName);
        }

        public void isCoinBooster(bool isChangeToCoin)
        {
            if (isChangeToCoin && MapItemType.None == itemType)
            {
                isMapTypeToCoinFromBooster = true;
                setItemData((int)MapItemType.Coin);
                return;
            }

            if (!isChangeToCoin && isMapTypeToCoinFromBooster)
            {
                isMapTypeToCoinFromBooster = false;
                setItemData((int)MapItemType.None);
            }
        }

        public void closeParticleObj()
        {
            itemAnim.enabled = false;
            particleObj.setActiveWhenChange(false);
        }
    }
}
