using Binding;
using CommonILRuntime.BindingModule;
using UnityEngine;
using UnityEngine.UI;
using UniRx;
using UniRx.Triggers;
using LobbyLogic.NetWork.ResponseStruct;
using System;
using System.IO;
using Network;
using Service;
using Services;
using EventActivity;
using Lobby.Common;
using System.Threading.Tasks;
using Event.Common;
using LitJson;
using System.Collections.Generic;
using CommonILRuntime.Module;
using Common.Jigsaw;
using CommonPresenter.PackItem;

namespace FarmBlast
{
    public class FarmBlastPresenter : ActivityPresenterBase
    {
        public override string objPath => "prefab/activity/farm_blast/activity_fb_main";
        public override string jsonFileName { get => "rookielevelsetting_blast"; }
        public override string iconSpriteStartName { get => "ga_basket_"; }
        public override string lvupEffectAnimName { get => "fb_level_up_effect"; }
        public override string[] iconSpriteNames { get => new string[] { "blue", "orange", "purple", "red", "yellow" }; }
        public override int totalLvCount { get => 10; }
        public override int tutorialsItemNum { get => 12; }
        protected override BackHideBehaviour hideBehaviour => BackHideBehaviour.CanDoBoth;
        //public override int pickItemFinishFrame { get => 45; }
        #region [UIs]
        GameObject completePlusObj;
        BindingNode jpBoardNode;
        //BindingNode ticketBoosterNode;
        //BindingNode prizeBoosterNode;
        //BindingNode goldenTicketNode;
        Button rewardTapBtn;
        BindingNode finalRewradNodeGroup;
        Button openFinalBtn;
        RectTransform completeItemGroup;

        RectTransform boosterDummy;
        RectTransform treasureDummy;
        #endregion
        AppleFarmInitResponse appleInitResponse;

        private AppleFrameSelectResponse selectResponse;
        private AppleFarmBoxResponse boxResponse;

        private JpBoardNodePresenter jpBoardNodePresenter;
        const int itemCount = 3;

        TreasureBoxChestNode treasureChestNode;

        FarmBlastBoosterNode ticketBoosterNodePresenter;
        PrizeBoosterPresenter prizeBoosterNodePresenter;
        FarmBlastBoosterNode goldenTicketNodePresenter;

        Transform jpBoardTrans;
        FinalRewardNode finalRewardNode;
        //ActivityReward[] finalRewards;
        List<PackItemNodePresenter> packItems = new List<PackItemNodePresenter>();
        long goldenTicketCount;
        public override void initContainerPresenter()
        {
            resOrder = new string[] { AssetBundleData.getBundleName(BundleType.FarmBlast) };
            base.initContainerPresenter();
        }
        public override void initUIs()
        {
            base.initUIs();
            completePlusObj = getGameObjectData("complete_plus_obj");
            jpBoardNode = getNodeData("jP_Board");

            rewardTapBtn = getBtnData("tap_btn");
            openFinalBtn = getBtnData("open_reward_btn");
            finalRewradNodeGroup = getNodeData("final_item_group");
            completeItemGroup = getBindingData<RectTransform>("complete_pack_group");
            boosterDummy = getRectData("booster_dummy_rect");
            treasureDummy = getRectData("treasure_chest_dummy");
        }

        public override void init()
        {
            base.init();

            treasureChestNode = UiManager.bindNode<TreasureBoxChestNode>(ActivityDataStore.getTreasureChestGO(treasureDummy));
            treasureChestNode.boxClick = sendBoxSelectToServer;
            ActivityDataStore.totalTicketCountUpdateSub.Subscribe(updateTicketCount).AddTo(uiGameObject);
            FarmBlastDataManager.getInstance.boostDataUpdateSub.Subscribe(updateBoosterData).AddTo(uiGameObject);
            ticketBoosterNodePresenter = ActivityDataStore.getBoosterGO<FarmBlastBoosterNode>(boosterDummy).setIconImg(BoosterType.Ticket) as FarmBlastBoosterNode;
            prizeBoosterNodePresenter = ActivityDataStore.getBoosterGO<PrizeBoosterPresenter>(boosterDummy).setIconImg(BoosterType.Prize) as PrizeBoosterPresenter;
            goldenTicketNodePresenter = ActivityDataStore.getBoosterGO<FarmBlastBoosterNode>(boosterDummy).setIconImg(BoosterType.GoldenTicket) as FarmBlastBoosterNode;

            ticketBoosterNodePresenter.redeemCallback = updateBoosterData;
            prizeBoosterNodePresenter.redeemCallback = updateBoosterData;
            goldenTicketNodePresenter.redeemCallback = updateBoosterData;

            jpBoardNodePresenter = UiManager.bindNode<JpBoardNodePresenter>(jpBoardNode.cachedGameObject);
            openFinalBtn.onClick.AddListener(openFinalRewardInfo);
            rewardTapBtn.onClick.AddListener(closeRewradInfo);
            finalRewardNode = UiManager.bindNode<FinalRewardNode>(finalRewradNodeGroup.cachedGameObject);
            finalRewardNode.animOutAc = finalRewardNodeAnimOut;
            finalRewardNode.close();
            packItems = PackItemPresenterServices.getPickItems(itemCount, completeItemGroup);
            for (int i = 0; i < packItems.Count; ++i)
            {
                packItems[i].close();
            }
        }

        void finalRewardNodeAnimOut()
        {
            rewardTapBtn.interactable = true;
            rewardTapBtn.gameObject.setActiveWhenChange(false);
        }

        public override void flyFinishCallback(AwardKind awardType)
        {
            switch (awardType)
            {
                case AwardKind.Box:
                    boxFlyFinishCB();
                    break;

                case AwardKind.PrizeBooster:
                case AwardKind.TicketBooster:
                case AwardKind.GoldenTicket:
                    updateBoosterData(boxResponse.BoostsData);
                    FarmBlastDataManager.getInstance.updateBoostData(boxResponse.BoostsData);
                    break;
            }
            base.flyFinishCallback(awardType);
        }

        public override void openMediumAwardPresenter<T>()
        {
            FarmBlastMediumAwardPresenter mediaAward = UiManager.getPresenter<FarmBlastMediumAwardPresenter>();
            mediaAward.changePuzzleIcon(puzzleIDs, selectResponse.RewardPackId);
            base.openMediumAwardPresenter<FarmBlastMediumAwardPresenter>();
        }

        public override void openFinalAwardPresenter<T>()
        {
            FarmBlastFinalAwardPresenter finalAwardPresenter = UiManager.getPresenter<FarmBlastFinalAwardPresenter>();
            base.openFinalAwardPresenter<FarmBlastFinalAwardPresenter>();
            Observable.TimerFrame(30).Subscribe(_ =>
            {
                finalAwardPresenter.changePuzzleIcon(puzzleIDs, selectResponse.RewardPackId);
            }).AddTo(finalAwardPresenter.uiGameObject);
        }

        public override Transform getFlyTargetObj(AwardKind awardType)
        {
            switch (awardType)
            {
                case AwardKind.Box:
                    return treasureChestNode.getEmptyBox().uiTransform;

                case AwardKind.Jackpot:
                    return jpBoardTrans;

                case AwardKind.TicketBooster:
                    return ticketBoosterNodePresenter.getBoosterTrans();

                case AwardKind.PrizeBooster:
                    return prizeBoosterNodePresenter.getBoosterTrans();

                case AwardKind.GoldenTicket:
                    return goldenTicketNodePresenter.getBoosterTrans();
            }
            return base.getFlyTargetObj(awardType);
        }

        List<long> puzzleIDs = new List<long>();

        public override async Task<SendSelectBaseResponse> sendServerSelect(int playIndex, int clickItem)
        {
            selectResponse = await AppManager.eventServer.sendFrameSelectItem(playIndex, clickItem);
            if (Result.OK != selectResponse.result)
            {
                if (Result.ActivityIDPromotedError == selectResponse.result)
                {
                    showGameEndMsg();
                }
                return null;
            }
            if (goldenTicketCount > 0)
            {
                goldenTicketCount--;
                goldenTicketNodePresenter.updateTimesTxt(goldenTicketCount);
            }

            puzzleIDs.Clear();
            if (selectResponse.RewardResult.Length > 1)
            {
                for (int i = 1; i < selectResponse.RewardResult.Length; ++i)
                {
                    var rewardResult = selectResponse.RewardResult[i];
                    AwardKind awardKind = ActivityDataStore.getAwardKind(rewardResult.Kind);
                    if (AwardKind.PuzzlePack == awardKind || AwardKind.PuzzleVoucher == awardKind)
                    {
                        long puzzleID;
                        if (long.TryParse(rewardResult.Type, out puzzleID))
                        {
                            puzzleIDs.Add(puzzleID);
                        }
                    }
                }
            }
            return selectResponse;
        }

        void boxFlyFinishCB()
        {
            treasureChestNode.addBox(awardData.type, selectResponse.CountDownTime);
        }
        Dictionary<string, ulong> jpReward = new Dictionary<string, ulong>();
        void convertJPReward()
        {
            jpReward.Clear();
            var initJPReward = appleInitResponse.JackPotReward.GetEnumerator();
            while (initJPReward.MoveNext())
            {
                jpReward.Add(initJPReward.Current.Key, (ulong)initJPReward.Current.Value);
            }
        }
        public override void setItemData(RookieInitActivityResponse data)
        {
            appleInitResponse = data as AppleFarmInitResponse;
            base.setItemData(data);
            finalRewardNode.initShowPacks(appleInitResponse.FinalItem);
            convertJPReward();
            jpBoardNodePresenter.setIniData(jpReward, appleInitResponse.JackPotCollection);
            if (!isRefreshItemData)
            {
                treasureChestNode.setBoxData(appleInitResponse.TreasureBox);
                updateBoosterData(appleInitResponse.BoostsData);
            }
            goldenTicketCount = appleInitResponse.BoostsData.PickBoost;
            completePlusObj.setActiveWhenChange(appleInitResponse.CompleteItem.Length > 0);
            for (int i = 0; i < packItems.Count; ++i)
            {
                var item = packItems[i];
                if (i >= appleInitResponse.CompleteItem.Length)
                {
                    item.close();
                    continue;
                }
                long packID;
                if (long.TryParse(appleInitResponse.CompleteItem[i].Type, out packID))
                {
                    item.showPackImg((PuzzlePackID)packID);
                }
            }
        }
        void updateBoosterData(BoostsData boostDatas)
        {
            ticketBoosterNodePresenter.updateTimerTxt(boostDatas.SpinBoost);
            prizeBoosterNodePresenter.updateTimerTxt(boostDatas.CoinBoost);
            goldenTicketNodePresenter.updateTimesTxt(boostDatas.PickBoost);
            goldenTicketCount = boostDatas.PickBoost;
        }

        public override void refreshItemData(string jsonData)
        {
            setIsRefreshItemData(true);
            setItemData(JsonMapper.ToObject<AppleFarmInitResponse>(jsonData));
        }

        private async void sendBoxSelectToServer(TreasuerBoxNodePresenter selectObj)
        {
            if (isShowRunning)
            {
                selectObj.openBtnInteractable();
                Debug.LogError($"send box is ShowRunning");
                return;
            }
            showRunning();
            boxResponse = await AppManager.eventServer.sendAppleOpenBox(selectObj.boxID);
            if (Result.OK != boxResponse.result)
            {
                return;
            }
            var rewardResult = boxResponse.RewardResult[0];
            awardData.parseAwardData(rewardResult);
            if (AwardKind.BuffMore == awardData.kind)
            {
                addBuffAmount(boxResponse.RewardResult);
            }

            selectObj.setAnimFinishCB(() =>
            {
                selectObj.updateBoxType(string.Empty, 0);
                showAnimFinish();
            });
            selectObj.playGetAnim();
            setShowAwardPic(rewardResult, selectObj.uiRectTransform);
        }

        public override Sprite getAwardSprite(ActivityAwardData awardData)
        {
            switch (awardData.kind)
            {
                case AwardKind.Jackpot:
                    jpBoardTrans = jpBoardNodePresenter.getAwardJPObj(awardData.type, boxResponse.JackPotReward);
                    string iconName = $"bg_activity_{awardData.type}_{awardData.type[boxResponse.JackPotReward]}";
                    return findIconSprite(iconName);

                case AwardKind.Box:
                    TreasureBoxType treasureBoxType;
                    if (UtilServices.enumParse<TreasureBoxType>(awardData.type, out treasureBoxType))
                    {
                        return findIconSprite($"activity_treasure_lv{(int)treasureBoxType}");
                    }
                    return null;
                case AwardKind.TicketBooster:
                    return findIconSprite("activity_ticket_booster");
                case AwardKind.PrizeBooster:
                case AwardKind.Coin:
                    return findIconSprite("activity_prize_booster");
                case AwardKind.GoldenTicket:
                    return findIconSprite("activity_golden_ticket");
                default:
                    return base.getAwardSprite(awardData);
            }
        }

        public override Animator awardObjAnim
        {
            get
            {
                if (awardData.kind == AwardKind.Jackpot)
                {
                    return awardObj.GetComponentInChildren<Animator>();
                }
                return base.awardObjAnim;
            }
        }

        public override Sprite findIconSprite(string spriteName)
        {
            return LobbySpriteProvider.instance.getSprite<FarmBlastSpriteProvider>(LobbySpriteType.FarmBlast, spriteName);
        }

        public override string getAwardPrefabPath(AwardKind type)
        {
            switch (type)
            {
                case AwardKind.CollectTarget:
                    return $"{ActivityDataStore.FarmBlastPrefabPath}fb_item_icon";
                case AwardKind.Jackpot:
                    return $"{ActivityDataStore.FarmBlastPrefabPath}fb_item_jp_tex";
                case AwardKind.TicketBooster:
                case AwardKind.PrizeBooster:
                case AwardKind.GoldenTicket:
                    return $"{ActivityDataStore.CommonPrefabPath}activity_item_booster";
            }

            return base.getAwardPrefabPath(type);
        }

        public override void setAwardObj(ulong awardAmount, Sprite iconSprite)
        {
            switch (awardData.kind)
            {
                case AwardKind.PrizeBooster:
                case AwardKind.TicketBooster:
                case AwardKind.GoldenTicket:
                    UiManager.bindNode<SpriteAwardObjPresenter>(awardObj.cachedGameObject).setSprite(iconSprite);
                    break;
            }
        }

        public override void ticketNotEnough()
        {
            var blastShop = UiManager.getPresenter<FarmBlastShopPresenter>();
            blastShop.openShop(isShowSpinObj: true, closePresenter);
        }

        public override GameObject getPrizeItem()
        {
            if (ActivityDataStore.isPrizeBooster && AwardKind.Coin == awardData.kind)
            {
                string objPath = getAwardPrefabPath(AwardKind.PrizeBooster);
                GameObject prizeBoosterObj = GameObject.Instantiate(ResourceManager.instance.getGameObjectWithResOrder(objPath, resOrder));
                prizeBoosterObj.transform.SetParent(prizeBoosterNodePresenter.uiRectTransform);
                prizeBoosterObj.transform.localPosition = Vector3.zero;
                prizeBoosterObj.name = Path.GetFileName(objPath);
                SpriteAwardObjPresenter awardObjPresenter = UiManager.bindNode<SpriteAwardObjPresenter>(prizeBoosterObj).setSprite(getAwardSprite(awardData));
                awardObjPresenter.close();
                awardObjPresenter.uiTransform.localScale = Vector3.one;
                return prizeBoosterObj;
            }
            return null;
        }
        public override void customAwardPlayGetAnim()
        {
            switch (awardData.kind)
            {
                case AwardKind.Jackpot:
                    Debug.Log("customAwardPlayGetAnim");
                    Observable.TimerFrame(25).Subscribe(_ =>
                    {
                        Debug.Log("customAwardPlayGetAnim addJPCollect");
                        jpBoardNodePresenter.addJPCollect();
                    });
                    break;
            }
        }

        public override float getAwardFinalScale()
        {
            switch (awardData.kind)
            {
                case AwardKind.Jackpot:
                    return 0.5f;
                case AwardKind.CollectTarget:
                    return 0.66f;
                case AwardKind.TicketBooster:
                case AwardKind.PrizeBooster:
                case AwardKind.GoldenTicket:
                    return 0.85f;
            }

            return base.getAwardFinalScale();
        }

        public override bool otherCheckTutoralCondition()
        {
            return !appleInitResponse.IsRecircle;
        }

        #region Final Reward
        void openFinalRewardInfo()
        {
            finalRewardNode.showPacks();
            rewardTapBtn.gameObject.setActiveWhenChange(true);
        }

        void closeRewradInfo()
        {
            finalRewardNode.closeInfos();
        }
        #endregion
    }

    class FinalRewardNode : NodePresenter
    {
        List<PackItemNodePresenter> packItems = new List<PackItemNodePresenter>();
        Animator infoAnim;

        IDisposable closeTriggerDis;
        IDisposable closeCountdownTrigger;
        bool isStartPlayClose;
        public Action animOutAc;

        public override void initUIs()
        {
            infoAnim = getAnimatorData("info_anim");
        }

        public override void init()
        {

        }

        public void initShowPacks(ActivityReward[] puzzleRewards)
        {
            if (packItems.Count > 0)
            {
                for (int i = 0; i < packItems.Count; ++i)
                {
                    ResourceManager.instance.returnObjectToPool(packItems[i].uiGameObject);
                }
            }
            packItems.Clear();
            packItems = PackItemPresenterServices.getPickItems(puzzleRewards.Length, uiRectTransform, 0.3f);
            for (int i = 0; i < puzzleRewards.Length; ++i)
            {
                long packID;
                if (long.TryParse(puzzleRewards[i].Type, out packID))
                {
                    packItems[i].showPackImg((PuzzlePackID)packID);
                }
            }
        }

        public void showPacks()
        {
            isStartPlayClose = false;
            open();
            infoAnim.SetTrigger("open");
            closeCountdownTrigger = Observable.Timer(TimeSpan.FromSeconds(5)).Subscribe(_ =>
            {
                closeInfos();
            });
        }

        public void closeInfos()
        {
            if (isStartPlayClose)
            {
                return;
            }
            isStartPlayClose = true;
            UtilServices.disposeSubscribes(closeCountdownTrigger, closeTriggerDis);
            var closeTrigger = infoAnim.GetBehaviour<ObservableStateMachineTrigger>();
            closeTrigger.OnStateEnterAsObservable().ObserveOnMainThread().Subscribe(onAniClose).AddTo(uiGameObject);
            infoAnim.SetTrigger("close");
        }

        void onAniClose(ObservableStateMachineTrigger.OnStateInfo obj)
        {
            closeTriggerDis = Observable.Timer(TimeSpan.FromSeconds(obj.StateInfo.length)).Subscribe(_ =>
            {
                close();
            });
        }

        public override void close()
        {
            base.close();
            if (null != animOutAc)
            {
                animOutAc();
            }
            isStartPlayClose = false;
        }
    }
}
