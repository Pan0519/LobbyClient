using CommonILRuntime.BindingModule;
using CommonILRuntime.Module;
using Binding;
using UnityEngine;
using UnityEngine.UI;
using System;
using UniRx;
using UniRx.Triggers;
using System.Collections.Generic;
using Lobby;
using Service;
using Random = UnityEngine.Random;

namespace Shop
{
    class ShopGiftPresenter : ContainerPresenter
    {
        public override string objPath => "prefab/lobby_shop/gold_gift_box";

        public override UiLayer uiLayer { get => UiLayer.GameMessage; }

        #region UIs
        Button closeBtn;
        Button tipsBtn;
        CustomTextSizeChange goldNumText;
        BindingNode giftBoxNode;
        //Image medalHintImg;
        //GameObject medalBtnGroup;
        CustomBtn medalStartBtn;

        Animator boxAnim;
        //Animator groupAnim;
        RectTransform[] giftLayouts = new RectTransform[4];
        Image[] medalImgs = new Image[4];
        Animator[] medalAnims = new Animator[4];
        #endregion

        List<IDisposable> animTriggerDis = new List<IDisposable>();
        IDisposable animTimerDis;
        int nowMedalCount { get { return MedalData.medalStates.Count; } }

        GiftState getRandomGiftState { get { return (GiftState)Random.Range(0, 2); } }
        bool isShowLastMedal { get { return nowMedalCount >= medalImgs.Length; } }

        List<ShopGiftBoxNodePresneter> giftBindNodes = new List<ShopGiftBoxNodePresneter>();
        int stopGiftId;

        public override void initUIs()
        {
            closeBtn = getBtnData("close_btn");
            tipsBtn = getBtnData("tip_btn");
            goldNumText = getBindingData<CustomTextSizeChange>("gold_num_txt");
            giftBoxNode = getNodeData("gift_box");
            //medalBtnGroup = getGameObjectData("medal_btn_group");
            medalStartBtn = getCustomBtnData("btn_start");

            //medalHintImg = getImageData("medal_hint");
            boxAnim = getAnimatorData("box_anim");
            //groupAnim = getAnimatorData("group_anim");
            for (int i = 0; i < giftLayouts.Length; ++i)
            {
                int id = (i + 1);
                giftLayouts[i] = getBindingData<RectTransform>($"box_layout_{id}");
                medalImgs[i] = getImageData($"medal_{id}");
                medalImgs[i].gameObject.setActiveWhenChange(false);
                medalAnims[i] = getAnimatorData($"medal_{id}_anim");
            }
        }

        public override void init()
        {
            for (int i = 0; i < nowMedalCount - 1; ++i)
            {
                openMedalObj(i);
            }
            closeBtn.onClick.AddListener(() => { boxAnim.SetTrigger("out"); });
            tipsBtn.onClick.AddListener(openTips);

            for (int i = 0; i < giftLayouts.Length; ++i)
            {
                for (int j = 0; j < 3; ++j)
                {
                    var gift = ResourceManager.instance.getObjectFromPool(giftBoxNode.cachedGameObject, giftLayouts[i]);
                    gift.cachedRectTransform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
                    giftBindNodes.Add(bindGiftBoxNode(gift.cachedGameObject));
                }
            }
            bindGiftBoxNode(giftBoxNode.cachedGameObject);
            showMedalStatus();
            var animState = boxAnim.GetBehaviours<ObservableStateMachineTrigger>();
            for (int i = 0; i < animState.Length; ++i)
            {
                animTriggerDis.Add(animState[i].OnStateEnterAsObservable().ObserveOnMainThread().Subscribe(animTriggerSubscribe));
            }

            //for (int i = 0; i < medalAnims.Length; ++i)
            //{
            //    var machineTrigger = medalAnims[i].GetBehaviour<ObservableStateMachineTrigger>();
            //    Debug.Log($"medalAnims {i} , {machineTrigger == null}");
            //    if (null == machineTrigger)
            //    {
            //        continue;
            //    }
            //    animTriggerDis.Add(machineTrigger.OnStateEnterAsObservable().ObserveOnMainThread().Subscribe(medalAnimTrigger));
            //}
        }

        public override async void open()
        {
            //var collectData = await AppManager.lobbyServer.getChipsCollect();
            goldNumText.text = Random.Range(5000, 999999).ToString("N0");
            base.open();
        }

        void showMedalStatus()
        {
            //medalBtnGroup.setActiveWhenChange(isShowLastMedal);
            //medalHintImg.gameObject.setActiveWhenChange(!isShowLastMedal);

            //if (!isShowLastMedal)
            //{
            //medalHintImg.sprite = ShopDataStore.getShopSprite(ShopDataStore.shopGiftMedalIconNames[nowMedalCount - 1]);
            //return;
            //}
            medalStartBtn.gameObject.setActiveWhenChange(true);
            medalStartBtn.clickHandler = medalStrtClick;
        }
        IDisposable chooseSubscribe;
        void medalStrtClick()
        {
            chooseSubscribe = Observable.EveryUpdate().Subscribe(val =>
             {
                 startRunChooseGift();
             }).AddTo(uiGameObject);

            Observable.Timer(TimeSpan.FromSeconds(3.0f)).Subscribe(_ =>
            {
                medalStop();
            });
        }

        void startRunChooseGift()
        {
            stopGiftId = Random.Range(0, giftBindNodes.Count - 1);
            //Debug.Log($"startRunChooseGift {stopGiftId}");
            for (int i = 0; i < giftBindNodes.Count; ++i)
            {
                var giftNode = giftBindNodes[i];
                giftNode.changeIsChooseImg(i == stopGiftId);
            }
        }

        void medalStop()
        {
            chooseSubscribe.Dispose();
            Observable.Timer(TimeSpan.FromSeconds(1f)).Subscribe(_ =>
            {
                openRewadPresenter();
            }).AddTo(uiGameObject);
        }

        void openRewadPresenter()
        {
            //var chipsRedeem = await AppManager.lobbyServer.postChipsCollectRedeem();
            //UiManager.getPresenter<ShopRewardPresenter>().openReward(chipsRedeem.collection.rewardPackId);
            UiManager.getPresenter<ShopRewardPresenter>().openReward(string.Empty);
        }

        void animTriggerSubscribe(ObservableStateMachineTrigger.OnStateInfo obj)
        {
            animTimerDis = Observable.Timer(TimeSpan.FromSeconds(obj.StateInfo.length)).Subscribe(_ =>
            {
                if (obj.StateInfo.IsName("shop_page_effect_out"))
                {
                    if (isShowLastMedal)
                    {
                        MedalData.medalStates.Clear();
                    }
                    UiManager.getPresenter<ShopMainPresenter>().updateMedals();
                    clear();
                    return;
                }
                if (nowMedalCount > 0)
                {
                    openMedalObj(nowMedalCount - 1, true);
                    Observable.Timer(TimeSpan.FromSeconds(0.3)).Subscribe(_time =>
                        {
                            for (int i = 0; i < giftBindNodes.Count; ++i)
                            {
                                giftBindNodes[i].giftPlayAnim();
                            }
                        }).AddTo(uiGameObject);
                    if (isShowLastMedal)
                    {
                        //Observable.Timer(TimeSpan.FromSeconds(0.35)).Subscribe(_time =>
                        //{
                        //    groupAnim.SetTrigger("full");
                        //}).AddTo(uiGameObject);
                    }
                }
            });
        }
        //IDisposable medalAnimTrigger;
        void openMedalObj(int openId, bool isAnimEnable = false)
        {
            var medal = medalImgs[openId];
            medalAnims[openId].enabled = isAnimEnable;
            medal.sprite = MedalData.getMedalStateSprite(openId);
            medal.gameObject.setActiveWhenChange(true);
        }

        ShopGiftBoxNodePresneter bindGiftBoxNode(GameObject bindObj)
        {
            GiftInfoData infoData = new GiftInfoData()
            {
                giftState = getRandomGiftState,
                num = Random.Range(100, 999)
            };
            var boxNode = UiManager.bindNode<ShopGiftBoxNodePresneter>(bindObj);
            boxNode.showGiftBox(infoData);
            return boxNode;
        }

        void openTips()
        {
            UiManager.getPresenter<ShopInfoPresenter>().openStartPage(1);
        }

        public override void clear()
        {
            animTriggerDis.Add(animTimerDis);
            Services.UtilServices.disposeSubscribes(animTriggerDis.ToArray());
            for (int i = 0; i < giftBindNodes.Count; ++i)
            {
                ResourceManager.instance.returnObjectToPool(giftBindNodes[i].uiGameObject);
            }
            base.clear();
        }
    }
}
