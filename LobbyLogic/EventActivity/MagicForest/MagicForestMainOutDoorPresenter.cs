using CommonILRuntime.Module;
using CommonILRuntime.BindingModule;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using EventActivity;
using CommonPresenter;
using Service;
using System;
using System.Threading.Tasks;
using LobbyLogic.NetWork.ResponseStruct;
using Network;
using UniRx;
using Services;
using Lobby.Jigsaw;
using System.Threading;
using LobbyLogic.Audio;
using Lobby.Audio;
using LobbyLogic.Common;
using Event.Common;

namespace MagicForest
{
    public class MagicForestMainOutDoorPresenter : SystemUIBasePresenter, IActivityPage
    {
        public override string objPath => $"{ForestDataServices.prefabPath}/activity_mf_outdoor_scene";
        public override UiLayer uiLayer { get => UiLayer.BarRoot; }

        readonly int rowMaxDoorCount = 4;
        #region UIs
        RectTransform itemGroup;
        RectTransform itemGroup1;
        RectTransform itemGroup2;
        Image bgImg;
        #endregion

        List<GrassItemNodePresenter> grassItems = new List<GrassItemNodePresenter>();
        List<IDisposable> grassClickDis = new List<IDisposable>();
        ForestGuidePresenter guidePresenter;
        MagicForestOutDoorUIPresenter uiPresenter;

        ActivityAwardData awardData = new ActivityAwardData();
        MagicForestStageReward stageReward;

        string packID;
        int nextDoorNum;
        //bool isGuide;
        CancellationTokenSource delayTaskCancel = new CancellationTokenSource();
        bool isOpenInDoor;
        bool isAlreadyInitData = false;
        CanvasGroup itemCanvasGroup;
        Sprite[] bgSprites = new Sprite[3];

        int lastBGID = -1;
        AudioSource bgmAudio;
        GameObject whiteChangeObj
        {
            get
            {
                if (null == _whiteChange)
                {
                    var tempObj = ResourceManager.instance.getGameObjectWithResOrder($"{ForestDataServices.prefabPath}/white_change", resOrder);
                    _whiteChange = GameObject.Instantiate(tempObj, UiRoot.instance.systemUiRoot);
                }
                return _whiteChange;
            }
        }
        GameObject _whiteChange;
        public override void initContainerPresenter()
        {
            resOrder = new string[] { AssetBundleData.getBundleName(BundleType.MagicForest) };
            base.initContainerPresenter();
        }
        public override void initUIs()
        {
            itemGroup = getRectData("grass_item_group");
            itemGroup1 = getRectData("item_group_1");
            itemGroup2 = getRectData("item_group_2");
            bgImg = getImageData("bg_img");
            itemCanvasGroup = itemGroup.gameObject.GetComponent<CanvasGroup>();
        }

        public override void init()
        {
            base.init();
            ForestDataServices.reduceMainBgmSub.Subscribe(reduceMainBgm).AddTo(uiGameObject);
            uiPresenter = UiManager.getPresenter<MagicForestOutDoorUIPresenter>();
            uiPresenter.outdoorCloseEvent = closePresenter;

            for (int i = 0; i < bgSprites.Length; ++i)
            {
                bgSprites[i] = ResourceManager.instance.loadWithResOrder<Sprite>($"prefab/activity/magic_forest/pic/res_mf/bg_maingame_{i}", resOrder);
            }
        }

        public override void closePresenter()
        {
            ForestDataServices.outDoorClearSub.OnNext(true);
            base.closePresenter();
        }

        void shopCloseEvent()
        {
            uiPresenter.closePresenter();
            closePresenter();
        }

        void closeAllPage()
        {
            shopCloseEvent();
            if (null != inDoorPresenter)
            {
                inDoorPresenter.clear();
            }
        }

        public override void animOut()
        {
            clear();
        }

        public override void clear()
        {
            delayTaskCancel.Cancel();
            if (null != bgmAudio)
            {
                AudioManager.instance.stopBGMOnObjAndResetMainBGM(bgmAudio);
            }
            ActivityDataStore.activityPageCloseCall();
            GamePauseManager.gameResume();
            base.clear();
        }

        public override async void open()
        {
            bgmAudio = AudioManager.instance.playBGMOnObj(uiGameObject, AudioPathProvider.getAudioPath(ActivityMFAudio.MainBgm));
            if (!isAlreadyInitData)
            {
                isAlreadyInitData = true;
                await initData();
                var getItemResponse = await AppManager.lobbyServer.getBagItem(ActivityDataStore.getNowActivityTicketID());
                ForestDataServices.updateTotalTicket(getItemResponse.amount);
            }
            ActivityErrorMsgServices.registerErrorMSg();
            base.open();
        }
        long prizeEndTime = 0;
        async Task initData()
        {
            var initData = await AppManager.eventServer.getForestInitData();
            setStageBG(initData.Level);
            ForestDataServices.IsRecircle = initData.IsRecircle;
            showGrassItem(initData.DoorNum);
            setDoorHistoryStatus(initData.DoorHistory);
            int alreadyClickCount = 0;

            if (initData.Level <= 1 && !initData.IsRecircle)
            {
                for (int i = 0; i < initData.DoorHistory.Length; ++i)
                {
                    if (!initData.DoorHistory[i].Kind.Equals(ForestDataServices.NotOpenKey))
                    {
                        alreadyClickCount++;
                    }
                }
                ForestDataServices.isGuide = alreadyClickCount <= 1;
            }

            uiPresenter.initUIData(initData);
            prizeEndTime = initData.BoostsData.PrizeBooster;
            ForestDataServices.updateJPReward(initData.JackPotReward);
            if (ForestDataServices.isGuide)
            {
                int guildStep = (alreadyClickCount == 1) ? 3 : 0;
                openGuildPresenter(guildStep, initData);
            }
            isOpenInDoor = null != initData.MagicForestBossData;
            if (isOpenInDoor)
            {
                openInDoor(initData.BossHistory, initData.MagicForestBossData);
            }
        }

        void testRunStage()
        {
            IDisposable runStage = null;
            runStage = Observable.Timer(TimeSpan.FromSeconds(2.0f), TimeSpan.FromSeconds(1.5f)).Subscribe(repeatCount =>
            {
                uiPresenter.upStageLv();
            });
        }

        public void refreshJPNode()
        {
            uiPresenter.refreshJPCount();
            uiPresenter.refreshJPReward();
        }

        void openGuildPresenter(int guildStep, MagicForestInitResponse initData)
        {
            guidePresenter = UiManager.getPresenter<ForestGuidePresenter>();
            guidePresenter.openGuildPresenter(initData);
            guidePresenter.startGuild(guildStep);
            guidePresenter.setBackItemEvent(guildBackItemGroup);
            guidePresenter.nowStepSub.Subscribe(guildNowStep).AddTo(guidePresenter.uiGameObject);
            guidePresenter.addGuildItem((rect) =>
            {
                itemGroup1.SetParent(rect);
                for (int i = 0; i < grassItems.Count; ++i)
                {
                    grassItems[i].addButterFlyLayer(100);
                }
            });
        }

        void guildNowStep(GuideStep step)
        {
            if (GuideStep.Step4 == step)
            {
                uiPresenter.moveStageScroll(0);
            }
        }

        void guildBackItemGroup()
        {
            itemGroup1.SetParent(itemGroup);
            itemGroup1.SetAsFirstSibling();
            for (int i = 0; i < grassItems.Count; ++i)
            {
                grassItems[i].resetButterFlyLayer();
            }
        }

        void showGrassItem(int doorNum)
        {
            grassItems.Clear();
            UtilServices.disposeSubscribes(grassClickDis.ToArray());
            grassClickDis.Clear();
            bool isOneRow = doorNum <= rowMaxDoorCount;
            itemGroup2.gameObject.setActiveWhenChange(!isOneRow);
            if (isOneRow)
            {
                addGrassItem(doorNum, itemGroup1);
                setGrassItemID();
                return;
            }

            addGrassItem(2, itemGroup1, FloorLayout.Up);
            addGrassItem(3, itemGroup2, FloorLayout.Down);
            setGrassItemID();
        }
        void addGrassItem(int grassCount, RectTransform layout, FloorLayout floor = FloorLayout.Up)
        {
            var tempObj = ResourceManager.instance.getGameObjectWithResOrder("prefab/activity/magic_forest/mf_grass_item", resOrder);
            for (int i = 0; i < grassCount; ++i)
            {
                var poolObj = GameObject.Instantiate(tempObj, layout);
                GrassItemNodePresenter grassItem = UiManager.bindNode<GrassItemNodePresenter>(poolObj);
                grassItem.resetItemStatus();
                grassItem.setFloorLayout(floor);
                grassClickDis.Add(grassItem.playClickSub.Subscribe(sendPlayClick).AddTo(grassItem.uiGameObject));
                grassItems.Add(grassItem);
            }
        }

        void setGrassItemID()
        {
            for (int i = 0; i < grassItems.Count; ++i)
            {
                var item = grassItems[i];
                item.setSelfID(i);
            }
            ForestDataServices.stopShowing();
        }

        void setDoorHistoryStatus(ActivityReward[] doorHistory)
        {
            for (int i = 0; i < grassItems.Count; ++i)
            {
                var history = doorHistory[i];
                grassItems[i].showHistoryItemStatus(history.Kind, history.Type);
            }
        }
        IDisposable grassItemCheckAnim;
        async void sendPlayClick(GrassItemNodePresenter clickItem)
        {
            if (ForestDataServices.totalTicketCount <= 0)
            {
                UiManager.getPresenter<ForestShopPresenter>().openShop(isShowSpinObj: true, shopCloseEvent);
                clickItem.playBtnEnable(true);
                return;
            }

            AudioManager.instance.playAudioOnce(AudioPathProvider.getAudioPath(ActivityMFAudio.Open));
            UtilServices.disposeSubscribes(grassItemCheckAnim);
            ForestDataServices.isShowing = true;
            ForestDataServices.updateTotalTicket(ForestDataServices.totalTicketCount - 1);

            var playResponse = await AppManager.eventServer.sendForestPlay(clickItem.selfID);
            if (Result.OK != playResponse.result)
            {
                if (Result.ActivityIDPromotedError == playResponse.result)
                {
                    UiManager.getPresenter<MsgBoxPresenter>().openActivityEndNode(() => { isActvityEnd(true); });
                }
                return;
            }
            stageReward = playResponse.StageReward;
            ForestDataServices.updateTotalTicket(playResponse.Ticket);
            nextDoorNum = playResponse.NextDoorNum;
            packID = playResponse.RewardPackId;
            ActivityReward activityReward = playResponse.RewardResult[0];
            clickItem.showPlayItemStatus(activityReward.Kind);
            if (playResponse.IsLevelUp)
            {
                grassItemCheckAnim = clickItem.itemAnimSub.Subscribe(doorTransitions).AddTo(clickItem.uiGameObject);
                return;
            }
            awardData.parseAwardData(activityReward);

            if (ForestDataServices.isGuide)
            {
                ForestDataServices.guildTreeClickStep(clickItem);
            }

            if (AwardKind.Boss == awardData.kind)
            {
                ActivityReward[] notOpenHistory = new ActivityReward[20];
                for (int i = 0; i < notOpenHistory.Length; ++i)
                {
                    notOpenHistory[i] = new ActivityReward()
                    {
                        Kind = ForestDataServices.NotOpenKey,
                    };
                }
                grassItemCheckAnim = clickItem.itemAnimSub.Subscribe(_ =>
                {
                    openWhiteTransitions(60, () =>
                    {
                        openInDoor(notOpenHistory, playResponse.BossData);
                    });
                }).AddTo(clickItem.uiGameObject);
                return;
            }

            if (AwardKind.BuffMore == awardData.kind)
            {
                uiPresenter.updateBuffData(playResponse.RewardResult[1], awardData);
            }
            openPrize();
        }

        public void changeBossItemToGem(string jpGem)
        {
            for (int i = 0; i < grassItems.Count; ++i)
            {
                var item = grassItems[i];
                if (GrassItemKind.Stone == item.itemKind)
                {
                    item.setGemImage(jpGem);
                    break;
                }
            }
        }

        async void openPrize()
        {
            //Debug.Log($"AwardKind {awardData.kind}");
            await Task.Delay(TimeSpan.FromSeconds(1.5f));
            switch (awardData.kind)
            {
                case AwardKind.BuffMore:
                    UiManager.getPresenter<MorePrizePresenter>().openMorePrize(awardData, () =>
                    {
                        if (ForestDataServices.isGuide)
                        {
                            uiPresenter.changeStageRoot(guidePresenter.uiRectTransform);
                        }

                        uiPresenter.runStageCompleteReward();
                        Observable.Timer(TimeSpan.FromSeconds(1.0f)).Subscribe(_ =>
                        {
                            uiPresenter.stageBackToUIGroup();
                            ForestDataServices.stopShowing();
                        }).AddTo(uiGameObject);
                    });
                    break;

                case AwardKind.Ticket:
                case AwardKind.Coin:
                    var normalPresenter = UiManager.getPresenter<NormalPrizePresenter>();
                    if (ForestDataServices.isGuide)
                    {
                        normalPresenter.changeUILayout(UiLayer.System);
                    }
                    if (ActivityDataStore.isPrizeBooster && awardData.kind == AwardKind.Coin)
                    {
                        normalPresenter.setPrizeItem(uiPresenter.getPrizeItem());
                    }
                    normalPresenter.openPrize(awardData, ForestDataServices.stopShowing);
                    normalPresenter.setTicketFlyTarget(uiPresenter.ticketObj);
                    break;

                case AwardKind.PuzzlePack:
                case AwardKind.PuzzleVoucher:
                    OpenPackWildProcess.openPackWildFromID(packID, ForestDataServices.stopShowing);
                    break;
            }
        }

        void doorTransitions(bool show)
        {
            openWhiteTransitions(115, levelUp);
        }

        void openWhiteTransitions(int frame, Action transEvent)
        {
            whiteChangeObj.setActiveWhenChange(false);
            IDisposable transDis = null;
            transDis = Observable.TimerFrame(frame).Subscribe(_ =>
             {
                 transDis.Dispose();
                 whiteChangeObj.setActiveWhenChange(true);
                 Observable.TimerFrame(45).Subscribe(time =>
                 {
                     transEvent();
                 }).AddTo(uiGameObject);
             }).AddTo(uiGameObject);
        }

        void levelUp()
        {
            clearGrassItem();
            int stageLv = uiPresenter.upStageLv();
            if (null == stageReward)
            {
                setStageBG(stageLv);
                showNextGrassItem();
                return;
            }

            var stageRewardPresneter = UiManager.getPresenter<StageRewardPresenter>();
            stageRewardPresneter.openReward(packID, stageReward);
            stageRewardPresneter.showNextDoorEvent(fadeNextGrassLayout);
        }

        void setStageBG(int nowStageLv)
        {
            int nowStageBG = getNowStageBG(nowStageLv);
            if (lastBGID < 0 || lastBGID != nowStageBG)
            {
                lastBGID = nowStageBG;
                bgImg.sprite = bgSprites[nowStageBG];
            }
        }

        int getNowStageBG(int nowLv)
        {
            if (nowLv <= 11)
            {
                return 0;
            }

            if (nowLv <= 24)
            {
                return 1;
            }

            return 2;
        }

        void fadeNextGrassLayout()
        {
            uiPresenter.upStageLv();
            itemCanvasGroup.alpha = 0;
            showNextGrassItem();
            string twID = TweenManager.tweenToFloat(itemCanvasGroup.alpha, 1, durationTime: 0.5f, onUpdate: val =>
              {
                  itemCanvasGroup.alpha = val;
              }, onComplete: () =>
              {
                  itemCanvasGroup.alpha = 1;
              });
            TweenManager.tweenPlay(twID);
        }

        void showNextGrassItem()
        {
            showGrassItem(nextDoorNum);
        }

        public void clearGrassItem()
        {
            for (int i = 0; i < grassItems.Count; ++i)
            {
                GameObject.DestroyImmediate(grassItems[i].uiGameObject);
            }
        }

        public async void indoorReturnToOutDoor(bool isLevelUp)
        {
            inDoorPresenter = null;
            if (isLevelUp)
            {
                await initData();
            }
        }

        public void isActvityEnd(bool isEnd)
        {
            ActivityDataStore.activtyCallIsEnd(isEnd);
            if (isEnd)
            {
                inDoorCloseOutClick();
            }
        }
        MagicForestInDoorPresenter inDoorPresenter;
        void openInDoor(ActivityReward[] history, MagicForestBossData bossData)
        {
            inDoorPresenter = UiManager.getPresenter<MagicForestInDoorPresenter>();
            inDoorPresenter.updateJpBoard();
            inDoorPresenter.openInDoor(history, bossData, prizeEndTime);
            inDoorPresenter.setCloseOutDoorCB(inDoorCloseOutClick);
        }

        void inDoorCloseOutClick()
        {
            ForestDataServices.outDoorClearSub.OnNext(true);
            uiPresenter.clear();
            clear();
        }

        void reduceMainBgm(bool isReduce)
        {
            if (null == bgmAudio || bgmAudio.volume <= 0)
            {
                return;
            }
            float audioVolume = isReduce ? 0.8f : 1.0f;
            bgmAudio.volume = audioVolume;
        }
    }
}
