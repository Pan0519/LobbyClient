using CommonILRuntime.Module;
using UnityEngine;
using UnityEngine.UI;
using UniRx;
using System;
using System.Collections.Generic;
using Lobby.Common;
using LobbyLogic.Audio;
using Lobby.Audio;
using System.Threading.Tasks;
using CommonILRuntime.BindingModule;

namespace MagicForest
{
    public class GrassItemNodePresenter : NodePresenter
    {
        #region UIs
        GameObject grassItem;
        Animator grassAnim;
        Button playBtn;
        GameObject doorItem;
        GameObject urnItem;
        GameObject stoneItem;
        GameObject leprechaunItem;
        GameObject moneyItem;
        GameObject gemItem;
        Image gemImg;
        Canvas stoneCanvas;
        MeshRenderer leprechaunMesh;
        #endregion
        public int selfID { get; private set; }
        public Subject<GrassItemNodePresenter> playClickSub { get; private set; } = new Subject<GrassItemNodePresenter>();
        public Subject<bool> itemAnimSub = new Subject<bool>();
        Animator[] butterflyAnim = new Animator[3];
        string playItemKind;
        int butterflyDefaultLayout;
        List<Animator> childAnims = new List<Animator>();
        public GrassItemKind itemKind { get; private set; }
        public GrassItemKind rewardItemKind { get; private set; }
        Dictionary<GrassItemKind, Action> checkAnimSubs = new Dictionary<GrassItemKind, Action>();
        int[] floorLayoutCanvasNum = new int[] { 53, 54 };
        public override void initUIs()
        {
            grassItem = getGameObjectData("grass_item");
            grassAnim = getAnimatorData("grass_anim");
            doorItem = getGameObjectData("door_item");
            urnItem = getGameObjectData("urn_item");
            stoneItem = getGameObjectData("stone_item");
            leprechaunItem = getGameObjectData("leprechaun_item");
            playBtn = getBtnData("grass_item_btn");
            moneyItem = getGameObjectData("money_item");
            gemItem = getGameObjectData("gem_item");
            gemImg = getImageData("gem_img");
            stoneCanvas = getBindingData<Canvas>("stone_canvas");
            leprechaunMesh = leprechaunItem.GetComponentInChildren<MeshRenderer>();
            for (int i = 0; i < butterflyAnim.Length; ++i)
            {
                butterflyAnim[i] = getAnimatorData($"butterfly_{i + 1}_anim");
            }
            butterflyDefaultLayout = butterflyAnim[0].GetComponent<MeshRenderer>().sortingOrder;
        }

        public void addButterFlyLayer(int addLayer)
        {
            for (int i = 0; i < butterflyAnim.Length; ++i)
            {
                butterflyAnim[i].gameObject.GetComponent<MeshRenderer>().sortingOrder = addLayer + butterflyDefaultLayout;
            }
        }

        public void resetButterFlyLayer()
        {
            for (int i = 0; i < butterflyAnim.Length; ++i)
            {
                butterflyAnim[i].gameObject.GetComponent<MeshRenderer>().sortingOrder = butterflyDefaultLayout;
            }
        }

        public override void init()
        {
            playBtn.onClick.AddListener(sendPlay);
            for (int i = 0; i < uiRectTransform.childCount; ++i)
            {
                var anim = uiRectTransform.GetChild(i).gameObject.GetComponent<Animator>();
                if (null != anim)
                {
                    childAnims.Add(anim);
                }
            }
            checkAnimSubs.Add(GrassItemKind.Door, startCheckDoorAnim);
            checkAnimSubs.Add(GrassItemKind.Stone, startCheckStoneAnim);
        }

        public void setSelfID(int id)
        {
            selfID = id;
        }

        public void setFloorLayout(FloorLayout layout)
        {
            int orderNum = floorLayoutCanvasNum[(int)layout];
            stoneCanvas.sortingOrder = orderNum;
            leprechaunMesh.sortingOrder = orderNum;
        }

        public void setGuildLeprechaunMeshOrder()
        {
            leprechaunMesh.sortingOrder = 101;
        }

        void setItemStatus(GrassItemKind itemStatus)
        {
            //Debug.Log($"setItemStatus {itemStatus}");
            grassItem.setActiveWhenChange(GrassItemKind.Grass == itemStatus);
            playBtnEnable(GrassItemKind.Grass == itemStatus);
            showPlayItemStatus(itemStatus);
        }

        void showPlayItemStatus(GrassItemKind itemStatus)
        {
            itemKind = itemStatus;
            doorItem.setActiveWhenChange(GrassItemKind.Door == itemStatus);
            urnItem.setActiveWhenChange(GrassItemKind.Urn == itemStatus);
            stoneItem.setActiveWhenChange(GrassItemKind.Stone == itemStatus);
            leprechaunItem.setActiveWhenChange(GrassItemKind.Leprechaun == itemStatus);
            moneyItem.setActiveWhenChange(GrassItemKind.Coin == itemStatus);
            gemItem.setActiveWhenChange(GrassItemKind.Gem == itemStatus);
        }

        public void setGemImage(string gemJP)
        {
            if (string.IsNullOrEmpty(gemJP))
            {
                return;
            }
            setItemStatus(GrassItemKind.Gem);
            gemImg.sprite = LobbySpriteProvider.instance.getSprite<ForestSpriteProvider>(LobbySpriteType.MagicForest, $"jewel_{gemJP}");
        }

        public void showHistoryItemStatus(string kind, string type = "")
        {
            childAnimEnable(false);
            setItemStatus(ForestDataServices.getHistoryStatus(kind, type));
            if (GrassItemKind.Gem == itemKind)
            {
                setGemImage(type);
            }
        }

        public void resetItemStatus()
        {
            childAnimEnable(true);
            grassItem.setActiveWhenChange(false);
            showHistoryItemStatus(ForestDataServices.NotOpenKey);
            for (int i = 0; i < butterflyAnim.Length; ++i)
            {
                int random = UnityEngine.Random.Range(0, 2);

                bool isActive = random == 1;
                butterflyAnim[i].gameObject.setActiveWhenChange(isActive);
            }
        }

        public void showPlayItemStatus(string kind)
        {
            childAnimEnable(true);
            playItemKind = kind;
            playOpenAnim();
        }
        public void playBtnEnable(bool isEnable)
        {
            playBtn.interactable = isEnable;
        }

        void childAnimEnable(bool enable)
        {
            for (int i = 0; i < childAnims.Count; ++i)
            {
                childAnims[i].enabled = enable;
            }
        }

        async void playOpenAnim()
        {
            grassAnim.SetTrigger("open");
            rewardItemKind = ForestDataServices.getGrassItemStatus(playItemKind);

            Observable.TimerFrame(42).Subscribe(_ =>
            {
                for (int i = 0; i < butterflyAnim.Length; ++i)
                {
                    butterflyAnim[i].SetTrigger("open");
                }
                if (GrassItemKind.Door != rewardItemKind && GrassItemKind.Stone != rewardItemKind)
                {
                    showPlayItemStatus(rewardItemKind);
                }
            }).AddTo(uiGameObject);

            if (GrassItemKind.Door == rewardItemKind || GrassItemKind.Stone == rewardItemKind)
            {
                Observable.TimerFrame(55).Subscribe(_ =>
                {
                    showPlayItemStatus(rewardItemKind);
                    checkAnimSub(rewardItemKind);
                }).AddTo(uiGameObject);
            }

            await Task.Delay(TimeSpan.FromSeconds(1.5f));
            grassItem.setActiveWhenChange(false);
        }

        void checkAnimSub(GrassItemKind itemKind)
        {
            Action checkEvent;
            if (checkAnimSubs.TryGetValue(itemKind, out checkEvent))
            {
                checkEvent();
            }
        }

        void startCheckDoorAnim()
        {
            Animator doorAnim = doorItem.GetComponent<Animator>();
            if (null == doorAnim)
            {
                return;
            }
            IDisposable changeAnimDis = null;
            changeAnimDis = Observable.EveryUpdate().Subscribe(_ =>
            {
                if (doorAnim.GetCurrentAnimatorStateInfo(0).IsName("grass_door_open"))
                {
                    AudioManager.instance.playAudioOnce(AudioPathProvider.getAudioPath(ActivityMFAudio.Next));
                    itemAnimSub.OnNext(true);
                    changeAnimDis.Dispose();
                }
            }).AddTo(uiGameObject);
        }

        void startCheckStoneAnim()
        {
            Animator stoneAnim = stoneItem.GetComponent<Animator>();
            if (null == stoneAnim)
            {
                return;
            }

            IDisposable checkAnimDis = null;
            checkAnimDis = Observable.EveryUpdate().Subscribe(_ =>
            {
                AnimatorStateInfo stateInfo = stoneAnim.GetCurrentAnimatorStateInfo(0);
                if (stateInfo.IsName("grass_stone_change"))
                {
                    playStoneChangeAudio();
                    stoneCanvas.sortingOrder = 103;
                    itemAnimSub.OnNext(true);
                    checkAnimDis.Dispose();
                }
            }).AddTo(uiGameObject);
        }
        void playStoneChangeAudio()
        {
            Observable.TimerFrame(40).Subscribe(_ =>
            {
                AudioManager.instance.playAudioOnce(AudioPathProvider.getAudioPath(ActivityMFAudio.Rock));
            }).AddTo(uiGameObject);
        }
        void sendPlay()
        {
            if (ForestDataServices.isShowing)
            {
                Debug.LogError("isShowing");
                return;
            }

            playBtnEnable(false);
            playClickSub.OnNext(this);
        }
    }

    public enum FloorLayout
    {
        Up,
        Down,
    }
}
