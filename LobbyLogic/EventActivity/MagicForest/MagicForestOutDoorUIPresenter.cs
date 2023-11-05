using CommonILRuntime.Module;
using CommonILRuntime.BindingModule;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using EventActivity;
using CommonPresenter;
using System;
using LobbyLogic.NetWork.ResponseStruct;
using UniRx;
using Services;
using CommonPresenter.PackItem;
using LobbyLogic.Common;
using Common;

namespace MagicForest
{
    class MagicForestOutDoorUIPresenter : SystemUIBasePresenter
    {
        public override string objPath => $"{ForestDataServices.prefabPath}/activity_mf_outdoor_ui";
        public override UiLayer uiLayer { get => UiLayer.GameMessage; }

        #region UIs
        Button closeBtn;
        RectTransform boosterDummy;
        Button infoBtn;
        GameObject activityBGObj;
        Button activityBGBtn;
        RectTransform completeRewardRect;
        Text completeRewardTxt;
        RectTransform completePackItemRect;
        TicketWithAnim ticketNode;
        Image roundImg;
        RectTransform uiGroupRoot;
        #endregion

        public Action outdoorCloseEvent = null;
        public GameObject ticketObj { get { return ticketNode.uiGameObject; } }
        InfoBaseNode infoNode;

        ForestBoosterNode goldenMalletBooster;
        ForestBoosterNode magnifireBooser;
        ForestPrizeBoosterNode prizeBooster;

        ForestStageNode stageNode;
        JPBoardNode jPBoardNode;

        ActivityAwardData awardData = new ActivityAwardData();
        ActivityAwardData buffMoreData = new ActivityAwardData();

        int uiGroupChildCount;

        List<PackItemNodePresenter> completePacks = new List<PackItemNodePresenter>();
        Sprite[] roundSprites = new Sprite[4];
        public override void initContainerPresenter()
        {
            resOrder = new string[] { AssetBundleData.getBundleName(BundleType.MagicForest) };
            base.initContainerPresenter();
        }

        public override void initUIs()
        {
            closeBtn = getBtnData("close_btn");
            infoBtn = getBtnData("info_btn");
            completeRewardTxt = getTextData("complete_reward_num");
            completeRewardRect = getRectData("info_horizontal_layout");
            completePackItemRect = getRectData("complete_pack_group");
            boosterDummy = getRectData("booster_dummy_rect");
            activityBGObj = getGameObjectData("activity_info_bg");
            activityBGBtn = getBtnData("actvity_info_bg_btn");
            roundImg = getImageData("complete_round_img");
            uiGroupRoot = getRectData("ui_group_trans");
            ticketNode = UiManager.bindNode<TicketWithAnim>(getNodeData("ticket_node").cachedGameObject);
        }

        public override void init()
        {
            base.init();
            var completeRoundSprite = ResourceManager.instance.loadAllWithResOrder($"prefab/activity/magic_forest/pic/grass_door/grass_door",resOrder);
            for (int i = 0; i < roundSprites.Length; ++i)
            {
                roundSprites[i] = Array.Find(completeRoundSprite, sprite => sprite.name.Equals($"tex_final_prize_r{i + 1}_{ApplicationConfig.nowLanguage.ToString().ToLower()}"));
            }

            uiGroupChildCount = uiGroupRoot.childCount;
            activityBGObj.setActiveWhenChange(false);
            infoNode = UiManager.bindNode<InfoBaseNode>(getNodeData("info_node").cachedGameObject);
            stageNode = UiManager.bindNode<ForestStageNode>(getNodeData("stage_node").cachedGameObject);
            jPBoardNode = UiManager.bindNode<JPBoardNode>(getNodeData("jp_board_node").cachedGameObject);

            magnifireBooser = ActivityDataStore.getBoosterGO<ForestBoosterNode>(boosterDummy).setIconImg(BoosterType.Magnifire) as ForestBoosterNode;
            prizeBooster = ActivityDataStore.getBoosterGO<ForestPrizeBoosterNode>(boosterDummy).setIconImg(BoosterType.Prize) as ForestPrizeBoosterNode;
            goldenMalletBooster = ActivityDataStore.getBoosterGO<ForestBoosterNode>(boosterDummy).setIconImg(BoosterType.GoldenMallet) as ForestBoosterNode;

            ForestDataServices.goldenMalletCountSub.Subscribe(goldenMalletBooster.updateTimesTxt).AddTo(uiGameObject);
            ForestDataServices.updateBoosterSub.Subscribe(updateBoosterData).AddTo(uiGameObject);
            ActivityDataStore.totalTicketCountUpdateSub.Subscribe(totalCount =>
            {
                ticketNode.updateTicketNum((int)totalCount);
            }).AddTo(uiGameObject);
            activityBGBtn.onClick.AddListener(activityBGBtnClick);
            closeBtn.onClick.AddListener(closeClick);
            infoBtn.onClick.AddListener(() =>
            {
                infoNode.open();
            });
        }

        public GameObject getPrizeItem()
        {
            GameObject result = null;
            result = GameObject.Instantiate(prizeBooster.uiGameObject);
            result.transform.SetParent(boosterDummy.parent);
            result.transform.localScale = Vector3.one;
            RectTransform prizeTrans = result.GetComponent<RectTransform>();
            Vector2 anchorPos = new Vector2(1, 0.5f);
            prizeTrans.anchorMin = anchorPos;
            prizeTrans.anchorMax = anchorPos;
            prizeTrans.anchoredPosition = new Vector2(boosterDummy.anchoredPosition.x - (prizeTrans.rect.width / 2), boosterDummy.anchoredPosition.y);
            result.setActiveWhenChange(false);
            return result;
        }

        void activityBGBtnClick()
        {
            if (stageNode.isRunningReward)
            {
                return;
            }
            stageNode.closeInfo();
        }

        void closeClick()
        {
            if (null != outdoorCloseEvent)
            {
                outdoorCloseEvent();
            }
            closeBtnClick();
        }
        IDisposable openInfoSubDis;
        public void initUIData(MagicForestInitResponse initData)
        {
            UtilServices.disposeSubscribes(openInfoSubDis);
            roundImg.sprite = roundSprites[Mathf.Min(initData.Round, roundSprites.Length - 1)];
            updateJPReward(initData.JackPotReward);
            updateJPCount(initData.JackPotCount);
            updateBoosterData(initData.BoostsData);
            stageNode.initStageRewards(initData.StageRewards, initData.Level);
            openInfoSubDis = stageNode.openInfoSub.Subscribe(stageOpenInfoSub).AddTo(uiGameObject);
            stageNode.closeInfoCB = closeStageInfo;
            var lastStageReward = stageNode.getLastStageReward();
            addCompleteReward(lastStageReward.reward);
        }
        public void updateBuffData(ActivityReward buffAward, ActivityAwardData awardData)
        {
            this.awardData = awardData;
            buffMoreData.parseAwardData(buffAward);
        }
        void closeStageInfo()
        {
            activityBGObj.setActiveWhenChange(false);
        }
        void addCompleteReward(MagicForestStageReward lastStageReward)
        {
            for (int i = 0; i < completePacks.Count; ++i)
            {
                completePacks[i].clear();
            }
            completePacks.Clear();

            for (int i = 0; i < lastStageReward.CompleteItem.Length; ++i)
            {
                completePacks.Add(PackItemPresenterServices.getSinglePackItem(lastStageReward.CompleteItem[i].Type, completePackItemRect));
            }
            completeRewardTxt.text = lastStageReward.CompleteReward.ToString("N0");
            if (lastStageReward.CompleteItem.Length > 0)
            {
                completeRewardTxt.text = $"{completeRewardTxt.text}+";
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(completeRewardRect);
        }

        public StageItemNode getLastStageNode()
        {
            return stageNode.getLastStageReward();
        }

        public void runStageCompleteReward()
        {
            stageNode.addStageRewardAmount(buffMoreData.amount, (long)awardData.amount);
        }

        public void moveStageScroll(float posY)
        {
            stageNode.moveStageItemLayout(posY);
        }

        public int upStageLv()
        {
            return stageNode.upStageLv();
        }

        public void changeStageRoot(RectTransform parentRoot)
        {
            stageNode.uiRectTransform.SetParent(parentRoot);
        }

        public void stageBackToUIGroup()
        {
            stageNode.uiRectTransform.SetParent(uiGroupRoot);
            stageNode.uiRectTransform.SetSiblingIndex(uiGroupChildCount - 2);
        }

        void stageOpenInfoSub(StageItemNode openStageItem)
        {
            activityBGObj.setActiveWhenChange(true);
            if (stageNode.isLastStageNode(openStageItem))
            {
                activityBGObj.transform.SetSiblingIndex(uiGroupChildCount - 4);
                return;
            }
            activityBGObj.transform.SetSiblingIndex(uiGroupChildCount - 3);
        }

        void updateJPReward(Dictionary<string, long> reward)
        {
            ForestDataServices.updateJPReward(reward);
            jPBoardNode.updateJpReward(reward);
        }

        void updateJPCount(Dictionary<string, int> count)
        {
            ForestDataServices.updateJPCount(count);
            jPBoardNode.updateJpCount(count);
        }

        public void refreshJPReward()
        {
            jPBoardNode.updateJpReward(ForestDataServices.jpRewards);
        }

        public void refreshJPCount()
        {
            jPBoardNode.updateJpCount(ForestDataServices.jpCounts);
        }

        public void updateBoosterData(ForestBoosterData boosterData)
        {
            goldenMalletBooster.updateTimesTxt(boosterData.GoldenMallet);
            magnifireBooser.updateTimerTxt(boosterData.MagnifierBooster);
            prizeBooster.updateTimerTxt(boosterData.PrizeBooster);
        }
        public override void animOut()
        {
            clear();
        }
    }
}
