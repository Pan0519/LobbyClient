using CommonILRuntime.Module;
using CommonILRuntime.BindingModule;
using Lobby.Common;
using UnityEngine;
using UnityEngine.UI;
using LobbyLogic.NetWork.ResponseStruct;
using Event.Common;
using EventActivity;
using System.Threading.Tasks;
using UniRx;
using UniRx.Triggers;
using Service;
using Services;
using Network;
using System;
using System.Collections.Generic;
using CommonPresenter.PackItem;
using Common;
using LobbyLogic.Audio;
using Lobby.Audio;
using CommonPresenter;

namespace MagicForest
{
    public class MagicForestInDoorPresenter : SystemUIBasePresenter
    {
        public override string objPath => $"{ForestDataServices.prefabPath}/activity_mf_indoor";
        public override UiLayer uiLayer { get => UiLayer.GameMessage; }

        public Subject<bool> inDoorCloseSub = new Subject<bool>();
        readonly string itemGroupPath = $"{ForestDataServices.prefabPath}/bag_group";
        readonly string itemPath = $"{ForestDataServices.prefabPath}/bag_item";
        readonly string hammerPropsName = "goldenMallet";

        #region UIs
        Button closeBtn;
        Button infoBtn;
        Button useHammerBtn;
        RectTransform bagGroupRect;
        Text hammerProgressTxt;
        Text ticketCountTxt;
        Text rewardTxt;
        RectTransform packLayout;
        RectTransform rewardInfoLayout;
        Image hammerProgress;
        Animator hammerAnim;
        GameObject goldEffObj;
        RectTransform normalHammerRect;
        Animator tableAnim;
        Image tableGemImg;
        GameObject luckyCoinObj;
        RectTransform cabinetGroup;
        #endregion

        string jpName;
        ulong jpAmount;
        List<IDisposable> animTriggerDis = new List<IDisposable>();
        MagicForestBossData bossData;
        JPBoardNode jpNode;
        GameObject[] bagGroupRects = new GameObject[5];
        GameObject bagItemTempobj;
        InfoBaseNode infoNode;
        ForestBoosterNode goldenMalletBooster;
        ForestPrizeBoosterNode prizeBooster;
        int hammerCount;
        int hammerMaxCount;
        long goldenMalletCount;
        Action closeOutDoor;
        MagicForestBossPlayResponse playResponse;
        MagicForestMainOutDoorPresenter outDoor;
        public override void initContainerPresenter()
        {
            resOrder = new string[] { AssetBundleData.getBundleName(BundleType.MagicForest) };
            base.initContainerPresenter();
        }
        public override void initUIs()
        {
            closeBtn = getBtnData("close_btn");
            infoBtn = getBtnData("info_btn");
            bagGroupRect = getRectData("bag_item_group");
            hammerProgressTxt = getTextData("hammer_progress_text");
            ticketCountTxt = getTextData("ticket_count_txt");
            rewardTxt = getTextData("reward_num_txt");
            packLayout = getRectData("complete_pack_group");
            rewardInfoLayout = getRectData("info_layout");
            useHammerBtn = getBtnData("golden_use_btn");
            hammerProgress = getImageData("hammer_progress");
            hammerAnim = getAnimatorData("hammer_anim");
            tableAnim = getAnimatorData("table_anim");
            tableGemImg = getImageData("gem_img");
            normalHammerRect = getRectData("normal_hammer_rect");
            luckyCoinObj = getGameObjectData("lucky_coin");
            goldEffObj = getGameObjectData("gold_eff_obj");
            cabinetGroup = getRectData("cabinet_group");
        }

        public override void init()
        {
            base.init();
            goldenMalletBooster = UiManager.bindNode<ForestBoosterNode>(getNodeData("golden_booster_item_node").cachedGameObject) as ForestBoosterNode;
            prizeBooster = UiManager.bindNode<ForestPrizeBoosterNode>(getNodeData("prize_booster_item_node").cachedGameObject) as ForestPrizeBoosterNode;
            infoNode = UiManager.bindNode<InfoBaseNode>(getNodeData("info_node").cachedGameObject);
            jpNode = UiManager.bindNode<JPBoardNode>(getNodeData("jp_board_node").cachedGameObject);
            closeBtn.onClick.AddListener(closeClick);
            useHammerBtn.onClick.AddListener(sendUseHammer);
            infoBtn.onClick.AddListener(() =>
            {
                infoNode.open();
            });
            ForestDataServices.totalTicketSub.Subscribe(updateTicketCount).AddTo(uiGameObject);
            ForestDataServices.goldenMalletCountSub.Subscribe(updateGoldCount).AddTo(uiGameObject);
            ForestDataServices.updateBoosterSub.Subscribe(updateBoostData).AddTo(uiGameObject);
            ActivityDataStore.totalTicketCountUpdateSub.Subscribe((totalCount) =>
            {
                updateTicketCount((long)totalCount);
            }).AddTo(uiGameObject);
            updateTicketCount(ForestDataServices.totalTicketCount);
            initItemGroup();
        }

        void closeClick()
        {
            if (null != closeOutDoor)
            {
                closeOutDoor();
            }
            closeBtnClick();
        }

        void updateTicketCount(long totalTicketCount)
        {
            ticketCountTxt.text = totalTicketCount <= 99 ? totalTicketCount.ToString() : "99+";
        }

        void initItemGroup()
        {
            bagItemTempobj = ResourceManager.instance.getGameObjectWithResOrder(itemPath, resOrder);
            var groupTemp = ResourceManager.instance.getGameObjectWithResOrder(itemGroupPath, resOrder);
            for (int i = 0; i < bagGroupRects.Length; ++i)
            {
                bagGroupRects[i] = GameObject.Instantiate(groupTemp, bagGroupRect).gameObject;
            }
        }

        public void openInDoor(ActivityReward[] history, MagicForestBossData bossData, long prizeEndTime)
        {
            this.bossData = bossData;
            goldenMalletCount = bossData.GoldenMallet;
            openGoldEffectObj();
            updateGoldCount(bossData.GoldenMallet);
            for (int i = 0; i < history.Length; ++i)
            {
                var item = GameObject.Instantiate(bagItemTempobj, bagGroupRects[i % bagGroupRects.Length].transform);
                item.name = $"bag_item_{i + 1}";
                var bagNode = UiManager.bindNode<ForsetBagItemNode>(item);
                bagNode.shopSpinClick = closeClick;
                bagNode.setBagID(i);
                bagNode.showBagHistory(history[i]);
                bagNode.openSub.Subscribe(sendBagOpen).AddTo(item);
            }
            hammerCount = bossData.Count;
            hammerMaxCount = bossData.Max;
            updateHammerCount();
            rewardTxt.text = bossData.CompleteReward.ToString("N0");
            if (bossData.CompleteItem.Length > 0)
            {
                rewardTxt.text = $"{rewardTxt.text}+";
                for (int i = 0; i < bossData.CompleteItem.Length; ++i)
                {
                    PackItemPresenterServices.getSinglePackItem(bossData.CompleteItem[i].Type, packLayout);
                }
            }
            updatePrizeData(prizeEndTime);
            LayoutRebuilder.ForceRebuildLayoutImmediate(rewardInfoLayout);
            ForestDataServices.stopShowing();
        }

        public void setCloseOutDoorCB(Action closeOutDoor)
        {
            this.closeOutDoor = closeOutDoor;
        }

        async void sendBagOpen(ForsetBagItemNode selectBag)
        {
            ForestDataServices.isShowing = true;
            ForestDataServices.updateTotalTicket(ForestDataServices.totalTicketCount - 1);
            playResponse = await AppManager.eventServer.sendBossPlay(clickItem: selectBag.selfID);
            if (Result.OK != playResponse.result)
            {
                return;
            }
            ForestDataServices.updateTotalTicket(playResponse.Ticket);
            if (playResponse.RewardResult.Length <= 0)
            {
                selectBag.showBagItem(null);
                ForestDataServices.stopShowing();
                return;
            }
            showTableItem();
            var rewardData = playResponse.RewardResult[0];
            selectBag.showBagItem(rewardData);

            switch (selectBag.bagKind)
            {
                case BagItemKind.Hammer:
                    hammerCount += (int)rewardData.Amount;
                    selectBag.showHammerFlySub.Subscribe(showFlyHammer);
                    break;

                case BagItemKind.Coin:
                    GameObject prizeObj = ActivityDataStore.isPrizeBooster ? GameObject.Instantiate(prizeBooster.uiGameObject) : null;
                    if (null != prizeObj)
                    {
                        prizeObj.transform.SetParent(prizeBooster.uiRectTransform.parent);
                        prizeObj.transform.localPosition = prizeBooster.uiTransform.localPosition;
                        prizeObj.transform.localScale = Vector3.one;
                    }
                    selectBag.setPrizeItem(prizeObj, bagGroupRect);
                    if (null != prizeObj)
                    {
                        prizeObj.transform.SetParent(cabinetGroup);
                    }
                    break;
            }
        }

        async void sendUseHammer()
        {
            if (goldenMalletCount <= 0 || ForestDataServices.totalTicketCount <= 0)
            {
                UiManager.getPresenter<ForestShopPresenter>().openShop(isShowSpinObj: false);
                return;
            }
            goldenMalletCount--;
            ForestDataServices.updateMalletCount(goldenMalletCount);
            goldEffObj.setActiveWhenChange(false);
            var response = await AppManager.eventServer.sendBossUse(hammerPropsName);
            if (Result.OK != response.result)
            {
                return;
            }
            playResponse = response;
            showTableItem();
            ForestDataServices.updateTotalTicket(response.Ticket);
            ForestDataServices.updateMalletCount(response.GoldenMallet);
            playTableHit();
        }

        void showTableItem()
        {
            tableGemImg.gameObject.setActiveWhenChange(!playResponse.IsLevelUp);
            luckyCoinObj.setActiveWhenChange(playResponse.IsLevelUp);
        }

        void showFlyHammer(ForsetBagItemNode selectBag)
        {
            var item = GameObject.Instantiate(bagItemTempobj, selectBag.uiTransform);
            var bagItemNode = UiManager.bindNode<ForsetBagItemNode>(item);
            bagItemNode.showFlyHammer();
            bagItemNode.uiRectTransform.SetParent(normalHammerRect.parent);
            AudioManager.instance.playAudioOnce(AudioPathProvider.getAudioPath(ActivityBlastAudio.IconFly));
            bagItemNode.uiTransform.movePos(normalHammerRect.position, 0.8f, onComplete: () =>
            {
                hammerFlyComplete();
                bagItemNode.clear();
            });
            selectBag.changeHammerToGray();
        }

        async void hammerFlyComplete()
        {
            AudioManager.instance.playAudioOnce(AudioPathProvider.getAudioPath(ActivityBlastAudio.IconFlyIn));
            hammerAnim.SetTrigger("get");
            updateHammerCount();
            if (playResponse.IsPass)
            {
                await Task.Delay(TimeSpan.FromSeconds(1.0f));
                AudioManager.instance.playAudioOnce(AudioPathProvider.getAudioPath(ActivityMFAudio.Shine));
                hammerAnim.SetTrigger("full");
                Observable.TimerFrame(45).Subscribe(_ =>
                {
                    playTableHit();
                }).AddTo(uiGameObject);
                return;
            }
            ForestDataServices.stopShowing();
        }
        void updateHammerCount()
        {
            hammerProgressTxt.text = $"{hammerCount}/{hammerMaxCount}";
            hammerProgress.fillAmount = (float)hammerCount / (float)hammerMaxCount;
        }

        void updateGoldCount(long hammerCount)
        {
            goldenMalletBooster.updateTimesTxt(hammerCount);
            goldenMalletCount = hammerCount;
            openGoldEffectObj();
        }

        void openGoldEffectObj()
        {
            goldEffObj.setActiveWhenChange(goldenMalletCount > 0);
        }

        void updatePrizeData(long endTime)
        {
            prizeBooster.updateTimerTxt(endTime);
        }

        void updateBoostData(ForestBoosterData boosterData)
        {
            updateGoldCount(boosterData.GoldenMallet);
            updatePrizeData(boosterData.PrizeBooster);
        }
        void playTableHit()
        {
            jpName = string.Empty;
            jpAmount = 0;

            for (int i = 1; i < playResponse.RewardResult.Length; ++i)
            {
                var reward = playResponse.RewardResult[i];
                AwardKind kind = ActivityDataStore.getAwardKind(reward.Kind);
                if (AwardKind.Jackpot != kind)
                {
                    continue;
                }
                if (reward.Amount <= 1)
                {
                    jpName = reward.Type;
                    ForestDataServices.addJPCount(jpName);
                }
                else
                {
                    jpAmount = reward.getAmount;
                }
            }
            if (!string.IsNullOrEmpty(jpName))
            {
                tableGemImg.sprite = LobbySpriteProvider.instance.getSprite<ForestSpriteProvider>(LobbySpriteType.MagicForest, $"jewel_{jpName.ToLower()}");
            }

            var animTriggers = tableAnim.GetBehaviour<ObservableStateMachineTrigger>();
            animTriggerDis.Add(animTriggers.OnStateEnterAsObservable().ObserveOnMainThread().Subscribe(onAniHit).AddTo(uiGameObject));
            AudioManager.instance.playAudioOnce(AudioPathProvider.getAudioPath(ActivityMFAudio.Find));
            tableAnim.SetTrigger("hit");
        }

        void onAniHit(ObservableStateMachineTrigger.OnStateInfo obj)
        {
            animTriggerDis.Add(Observable.Timer(TimeSpan.FromSeconds(obj.StateInfo.length)).Subscribe(_ =>
           {
               UtilServices.disposeSubscribes(animTriggerDis.ToArray());
               showPassReward();
           }).AddTo(uiGameObject));
        }

        void showPassReward()
        {
            outDoor = UiManager.getPresenter<MagicForestMainOutDoorPresenter>();

            if (!playResponse.IsLevelUp)
            {
                var presenter = UiManager.getPresenter<ForestGemPrizePresenter>();
                presenter.openPrize(jpName, bossData.getCompleteReward);
                presenter.addJPBoard(jpNode);
                presenter.setAnimOutEvent(() =>
                {
                    gemPresenterFinish(jpName, jpAmount);
                });

                outDoor.changeBossItemToGem(jpName);
                outDoor.refreshJPNode();
            }
            else
            {
                outDoor.clearGrassItem();
                var gameEnd = UiManager.getPresenter<ForestGameEndPresenter>();
                gameEnd.openGameReward(bossData, playResponse);
                gameEnd.mgCloseEvent = closePresenter;
            }
            outDoor.indoorReturnToOutDoor(playResponse.IsLevelUp);
            ForestDataServices.stopShowing();
        }

        void gemPresenterFinish(string jpName, ulong jpAmount)
        {
            if (jpAmount <= 0)
            {
                closePresenter();
                return;
            }
            UiManager.getPresenter<ActivityJPRewardPresenter>().openAward(jpName, jpAmount, closePresenter);
        }

        public override void closePresenter()
        {
            base.closePresenter();
            if (null != outDoor)
            {
                outDoor.isActvityEnd(playResponse.IsEnd);
            }
        }

        public void updateJpBoard()
        {
            jpNode.updateJpReward(ForestDataServices.jpRewards);
            jpNode.updateJpCount(ForestDataServices.jpCounts);
        }

        public override void animOut()
        {
            clear();
        }
    }
}
