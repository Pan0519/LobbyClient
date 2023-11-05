using CommonILRuntime.BindingModule;
using System.Collections.Generic;
using CommonILRuntime.Module;
using System;
using Event.Common;
using Service;
using UniRx;
using EventActivity;
using Lobby.Jigsaw;
using UnityEngine;
using System.Threading.Tasks;
using Services;
using LobbyLogic.NetWork.ResponseStruct;
using CommonPresenter;
using Lobby.UI;
using Lobby.Audio;
using LobbyLogic.Audio;
using LobbyLogic.Common;
using CommonILRuntime.Outcome;
using Debug = UnityLogUtility.Debug;

namespace FrenzyJourney
{
    public class JourneyPresenterManager : IActivityPage
    {
        JourneyMainPresenter journeyMain;
        JourneyUIPresenter journeyUI;
        ChessNodePresenter chessNode;
        DiceNodePresenter diceNode;
        BossPresenter bossPresenter;

        static JourneyPresenterManager instance = new JourneyPresenterManager();
        public static JourneyPresenterManager getInstance { get { return instance; } }

        HashSet<ContainerPresenter> journeyPresenters = new HashSet<ContainerPresenter>();
        int diceCount;
        BossData bossData;
        ActivityReward[] stageReward;
        string stageRewardPackID;
        ActivityAwardData awardData = new ActivityAwardData();
        AudioSource bgmAudioSource = null;
        bool coinBoostRecord;
        void addPresenter(params ContainerPresenter[] presenters)
        {
            for (int i = 0; i < presenters.Length; ++i)
            {
                journeyPresenters.Add(presenters[i]);
            }
        }
        int itemNodeID = 1;
        bool isFirstRun;
        public void open()
        {
            BindingLoadingPage.instance.open();
            initData();
            ActivityErrorMsgServices.registerErrorMSg();
        }

        async void initData()
        {
            initPresenter();
            FrenzyJourneyData.getInstance.chessStopMove();
            await Task.Delay(TimeSpan.FromSeconds(0.3f));
            JourneyInitResponse initData = await AppManager.eventServer.getFrenzyJourneyData();
            var getItemResponse = await AppManager.lobbyServer.getBagItem(ActivityDataStore.getNowActivityTicketID());
            FrenzyJourneyData.getInstance.nowRound = initData.Round;

            bossData = initData.BossData;
            diceCount = getItemResponse.amount;
            isFirstRun = initData.Level <= 1 && initData.Progress.Value <= 0;

            journeyUI.setCloseAction(clearPresenters);
            journeyUI.initUIData(initData);
            journeyUI.sendBoxCB = treasureBoxClick;
            journeyUI.boxOpenCardCB = openCradReward;
            coinBoostRecord = FrenzyJourneyData.getInstance.isInCoinBooster;

            Observable.Timer(TimeSpan.FromSeconds(1.0f)).Subscribe(_ =>
            {
                journeyUI.closeMaskObj();
                if (!initData.IsRecircle && isFirstRun)
                {
                    journeyUI.setTutorialTreasureRect(journeyMain.getTutorialTreasureRect());
                    journeyUI.toturialStepSubject.Subscribe(tutorialStepSubscribe);
                    journeyUI.runTutorialStep();
                }
            });

            FrenzyJourneyData.getInstance.updateFrenzyDiceCount(initData.BoostsData.FrenzyDice);
            journeyUI.subscribeDiceUpdate();

            diceNode.setNowDiceTotalCount(diceCount);
            diceNode.showDiceBody();
            diceNode.diceClickEvent = diceClickEvent;

            journeyMain.setNowLvMap(initData.MapIndex, initData.History);
            itemNodeID = initData.Progress.Value;
            var groundItem = journeyMain.getNowGroundItem(initData.Level, itemNodeID);
            chessNode.setParent(journeyMain.nowGound.uiRectTransform, groundItem);
            chessNode.setMoveEndCB(chessMoveEnd);
            if (isFirstRun)
            {
                chessNode.close();
            }
            else
            {
                chessNode.open();
            }
            Observable.Timer(TimeSpan.FromSeconds(0.5)).Subscribe(_ =>
            {
                float waitCloseLoadingTime = 0;
                journeyUI.open();
                if (journeyMain.nowGound.groundId > 1)
                {
                    if (journeyMain.isLastMap && journeyMain.nowItemID < 7)
                    {
                        journeyMain.moveToAssignBG(7);
                    }
                    else
                    {
                        journeyMain.moveToNowProgress();
                    }
                }
                FrenzyJourneyData.getInstance.showRunning(null != bossData, "initData");
                if (null != bossData)
                {
                    openBossPresenter();
                    waitCloseLoadingTime = 1.0f;
                }
                else
                {
                    changeToMain();
                    waitCloseLoadingTime = 1.0f;
                }
                closeLoading(waitCloseLoadingTime);
            }).AddTo(journeyMain.uiGameObject);
            journeyMain.changeLastGroundCoinBooster(FrenzyJourneyData.getInstance.isInCoinBooster);
            ActivityDataStore.isEndSub.Subscribe(FrenzyJourneyData.getInstance.gameEndSub).AddTo(journeyMain.uiGameObject);
            ActivityDataStore.isEndErrorSub.Subscribe(_ =>
            {
                FrenzyJourneyData.getInstance.showGameEndMsg();
            }).AddTo(journeyMain.uiGameObject);
            FrenzyJourneyData.getInstance.isAutoPlayingSub.Subscribe(setAutoMode).AddTo(journeyMain.uiGameObject);
            FrenzyJourneyData.getInstance.isCoinBoosterSub.Subscribe(coinBoostUpdateSub).AddTo(journeyMain.uiGameObject);
            FrenzyJourneyData.getInstance.setActivityEndEvent(clearPresenters);
        }

        async void closeLoading(float waitTime)
        {
            await Task.Delay(TimeSpan.FromSeconds(waitTime));
            BindingLoadingPage.instance.close();
        }

        void initPresenter()
        {
            bossPresenter = null;
            journeyMain = UiManager.getPresenter<JourneyMainPresenter>();
            journeyMain.open();
            journeyUI = UiManager.getPresenter<JourneyUIPresenter>();
            journeyUI.close();
            var chessObj = ResourceManager.instance.getObjectFromPoolWithResOrder(FrenzyJourneyData.getInstance.getPrefabFullPath("fj_chess"), resNames: AssetBundleData.getBundleName(BundleType.FrenzyJourney));
            chessNode = UiManager.bindNode<ChessNodePresenter>(chessObj.cachedGameObject);
            diceNode = journeyUI.diceNodePresenter;
            addPresenter(journeyMain, journeyUI);
        }

        void openBossPresenter()
        {
            if (null == bossData)
            {
                return;
            }

            if (FrenzyJourneyData.getInstance.isGameEnd)
            {
                FrenzyJourneyData.getInstance.showGameEndMsg();
                return;
            }

            if (null != bgmAudioSource)
            {
                bgmAudioSource.Stop();
            }

            diceNode.bossDiceClickEvent = bossClickEvent;
            journeyUI.setBossData(bossData);
            bossPresenter = UiManager.getPresenter<BossPresenter>();
            bossPresenter.checkDiceTypeEvent = diceNode.checkDiceType;
            bossPresenter.bossFinishCB = bossEnd;
            journeyPresenters.Add(bossPresenter);
            bossPresenter.open();
            journeyMain.setGroundLayoutActvie(false);
            bgmAudioSource = AudioManager.instance.playBGMOnObj(bossPresenter.uiGameObject, AudioPathProvider.getAudioPath(ActivityFJAudio.MonsterBgm));
        }

        void changeToMain()
        {
            journeyUI.changeToMain();
            if (null != bgmAudioSource)
            {
                bgmAudioSource.Stop();
            }
            bgmAudioSource = AudioManager.instance.playBGMOnObj(journeyMain.uiGameObject, AudioPathProvider.getAudioPath(ActivityFJAudio.MainBgm));
        }

        async void bossEnd()
        {
            diceNode.checkDiceType();
            journeyPresenters.Remove(bossPresenter);
            changeToMain();
            journeyMain.setGroundLayoutActvie(true);
            bool isShowChestFull = checkShowChestFullMsg("Frenzyjourney_Message_6_Text");
            if (false == isShowChestFull)
            {
                journeyUI.addBox(bossData.CompleteItem[0].Type, boxCountdownTime);
            }

            if (null != stageReward)
            {
                await Task.Delay(TimeSpan.FromSeconds(0.5f));
                UiManager.getPresenter<JourneyGameRewardPresenter>().openRewardPage(stageRewardPackID, stageReward, resetMapData);
                return;
            }
            if (false == isShowChestFull)
            {
                checkShowChestFullMsg("Frenzyjourney_Message_5_Text");
            }
            resetMapData();
        }
        void resetMapData()
        {
            if (string.IsNullOrEmpty(FrenzyJourneyData.getInstance.refreshJsonData))
            {
                return;
            }
            JourneyInitResponse refreshData = LitJson.JsonMapper.ToObject<JourneyInitResponse>(FrenzyJourneyData.getInstance.refreshJsonData);
            ActivityDataStore.activtyCallIsEnd(refreshData.IsEnd);
            if (refreshData.IsEnd)
            {
                //clearPresenters();
                return;
            }
            if (FrenzyJourneyData.getInstance.nowRound != refreshData.Round)
            {
                refreshLvData(refreshData);
            }
            FrenzyJourneyData.getInstance.nowRound = refreshData.Round;
        }

        void bossClickEvent(JourneyBossPlayResponse bossPlayResponse)
        {
            bossData = bossPlayResponse.BossData;
            stageReward = bossPlayResponse.StageResult;
            stageRewardPackID = bossPlayResponse.RewardPackId;
            if (bossPlayResponse.IsLevelUp)
            {
                var rewardResult = bossPlayResponse.BossData.CompleteItem[0];
                awardData.parseAwardData(rewardResult);
                if (AwardKind.Box == awardData.kind)
                {
                    boxCountdownTime = bossPlayResponse.CountDownTime;
                }
            }

            if (!string.IsNullOrEmpty(bossPlayResponse.RefreshData))
            {
                FrenzyJourneyData.getInstance.refreshJsonData = Util.msgpackToJsonStr(Convert.FromBase64String(bossPlayResponse.RefreshData));
            }

            bossPresenter.openBomb(bossData, bossPlayResponse.IsLevelUp, () =>
             {
                 journeyUI.updateBossProgress(bossPlayResponse.BossData.Attack);
             });
        }

        void refreshLvData(JourneyInitResponse initData)
        {
            isFirstRun = true;
            journeyMain.refreshMapInfo(initData.MapIndex);
            itemNodeID = initData.Progress.Value;
            var groundItemPresenter = journeyMain.getNowGroundItem(initData.Level, itemNodeID);
            chessNode.setParent(journeyMain.nowGound.uiRectTransform, groundItemPresenter);
            chessNode.close();
            journeyMain.moveMap(0);
            journeyUI.setLvData(initData);
        }

        GroundItemNodePresenter tutorialBoss;
        void tutorialStepSubscribe(int tutorialStep)
        {
            if (tutorialStep > (int)TutorialStep.Step3)
            {
                journeyMain.moveMap(0);
                journeyUI.closeTutorialTreasure();
                return;
            }
            switch ((TutorialStep)tutorialStep)
            {
                case TutorialStep.Step2:
                    GameObject step2TutorObj = journeyUI.getNowTutorialObj();
                    tutorialBoss = journeyMain.showTutorialBossItem();
                    journeyMain.moveToAssignBG(2, onComplete: () =>
                    {
                        MeshRenderer bossRender = tutorialBoss.bossObj.GetComponent<MeshRenderer>();
                        if (null != bossRender)
                        {
                            bossRender.sortingOrder = 202;
                        }

                        tutorialBoss.uiTransform.SetParent(step2TutorObj.transform);
                    });
                    break;

                case TutorialStep.Step3:
                    journeyMain.moveToMapEnd(onComplete: () =>
                    {
                        journeyUI.openFinalTutorialTreasure();
                    });
                    journeyMain.resetTutorialItem();
                    break;
            }
        }
        public void treasureBoxClick(FrenzyJourneyBoxData boxAwardData)
        {
            awardData = boxAwardData.awardData;
            rewardPackID = boxAwardData.rewardPackID;
            switch (awardData.kind)
            {
                case AwardKind.Coin:
                    openCoinReward();
                    break;

                case AwardKind.Ticket:
                    UiManager.getPresenter<NormalRewardPresenter>().openForDice((long)awardData.amount, boxOpenAwardFinish);
                    break;
            }
        }

        void boxOpenAwardFinish()
        {
            journeyUI.moveBoxAwardObj(() =>
            {
                switch (awardData.kind)
                {
                    case AwardKind.Ticket:
                        journeyUI.clearBoxAwardObj();
                        diceNode.addDiceTotalCount((int)awardData.amount);
                        ActivityDataStore.pageAmountChange(diceNode.totalTicketCount);
                        FrenzyJourneyData.getInstance.showRunning(false, "boxOpenAwardFinish");
                        break;
                }
            });
        }

        public void openCradReward(string rewardPackID)
        {
            if (string.IsNullOrEmpty(rewardPackID))
            {
                FrenzyJourneyData.getInstance.showRunning(false, $"openCradReward rewardPackID.isEmpty");
                return;
            }
            OpenPackWildProcess.openPackWildFromID(rewardPackID, () =>
            {
                journeyUI.clearBoxAwardObj();
                FrenzyJourneyData.getInstance.showRunning(false, $"openCradReward");
            });
        }

        void openCoinReward()
        {
            UiManager.getPresenter<NormalRewardPresenter>().openForCoin(awardData.amount, coinAwardShowFinish);
        }

        void coinAwardShowFinish()
        {
            journeyUI.clearBoxAwardObj();
            applyCoinRewardOutcome();
            FrenzyJourneyData.getInstance.showRunning(false, "coinAwardShowFinish");
        }

        async void applyCoinRewardOutcome()
        {
            if (string.IsNullOrEmpty(rewardPackID))
            {
                return;
            }
            var rewardPack = await AppManager.lobbyServer.getRewardPacks(rewardPackID);
            Outcome.process(rewardPack.rewards).apply();
            rewardPackID = string.Empty;
        }

        void coinBoostUpdateSub(bool isInBoost)
        {
            if (FrenzyJourneyData.getInstance.isChessMoving)
            {
                return;
            }

            coinBoostRecord = isInBoost;
            if (!isInBoost)
            {
                journeyMain.clearAllItemCoinBoost();
            }
        }

        long boxCountdownTime;
        string rewardPackID;
        Queue<ItemGroupPresenter> moveItemTrans;

        async void diceClickEvent(JourneyPlayResponse playResponse)
        {
            await journeyMain.checkMapPos();
            rewardPackID = playResponse.RewardPackId;
            bossData = playResponse.BossData;
            boxCountdownTime = playResponse.CountDownTime;
            ActivityReward rewardResult = null;
            if (playResponse.RewardResult.Length > 0)
            {
                rewardResult = playResponse.RewardResult[0];
            }

            awardData.parseAwardData(rewardResult);

            int addProgress = playResponse.ProgressValue - itemNodeID;
            if (addProgress < 0)
            {
                addProgress = playResponse.ProgressValue;
            }

            if (isFirstRun)
            {
                addProgress--;
                isFirstRun = false;
            }
            journeyMain.setScrollEnabel(false);
            moveItemTrans = journeyMain.getItemGroupList(addProgress);
            chessMove();
            itemNodeID = playResponse.ProgressValue;
        }

        void mapMovingCB()
        {
            if (moveItemTrans.Count <= 0)
            {
                chessMoveEnd();
                return;
            }
            chessMove();
        }
        int lastMoveID = 0;
        async void chessMove()
        {
            if (moveItemTrans.Count <= 0)
            {
                return;
            }
            if (FrenzyJourneyData.getInstance.isInCoinBooster == false && coinBoostRecord != FrenzyJourneyData.getInstance.isInCoinBooster)
            {
                coinBoostRecord = FrenzyJourneyData.getInstance.isInCoinBooster;
                journeyMain.clearAllItemCoinBoost();
                await Task.Delay(TimeSpan.FromSeconds(0.5f));
            }

            IDisposable moveDis = null;
            // 此次移動到下次移動間格時間 moveIntervaltime = animTime 0.3 movePosTime +0.15 intervaltime +0.1 = 0.55
            int moveIntervalFarme = (int)(0.55f * 60);
            int moveItemGroupID = 1;
            if (journeyMain.isLastMap)
            {
                moveItemGroupID = lastMoveID < 7 ? 7 : 15;
            }

            moveDis = Observable.TimerFrame(0, moveIntervalFarme).Subscribe(repeatCount =>
             {
                 ItemGroupPresenter moveToItem = moveItemTrans.Dequeue();
                 bool isLastMove = moveItemTrans.Count <= 0;
                 lastMoveID = moveToItem.groupID;
                 Action chessMapMoving = null;
                 bool isMapMoving = lastMoveID == moveItemGroupID;
                 if (isMapMoving)
                 {
                     chessMapMoving = checkChessMoving;
                 }
                 chessNode.movePos(journeyMain.nowGound.uiRectTransform, moveToItem, isLastMove, chessMapMoving);
                 if (isLastMove || isMapMoving)
                 {
                     moveDis.Dispose();
                 }
             }).AddTo(chessNode.uiGameObject);
        }

        void checkChessMoving()
        {
            if (!journeyMain.isLastMap)
            {
                journeyMain.moveToNowProgress(mapMovingCB);
                return;
            }

            if (lastMoveID <= 7)
            {
                journeyMain.moveToAssignBG(7, mapMovingCB);
                return;
            }

            journeyMain.moveToMapEnd(mapMovingCB);
        }

        void chessMoveEnd()
        {
            //Debug.Log($"chessMoveEnd RewardType : {awardData.kind}");
            switch (awardData.kind)
            {
                case AwardKind.None:
                    if (journeyMain.getNowItemNodePresenter().isChest)
                    {
                        checkShowChestFullMsg("Frenzyjourney_Message_6_Text");
                    }
                    FrenzyJourneyData.getInstance.showRunning(false, "chessMoveEnd AwardKind.None");
                    break;

                case AwardKind.Boss:
                    openBossPresenter();
                    journeyMain.getNowItemNodePresenter().bossObj.setActiveWhenChange(false);
                    break;

                case AwardKind.Box:
                    boxFly();
                    journeyMain.closeRewardItemParticleObj();
                    break;

                case AwardKind.Coin:
                    journeyMain.playRewardItemAnim(openCoinReward);
                    break;

                case AwardKind.PuzzlePack:
                case AwardKind.PuzzleVoucher:
                    journeyMain.playRewardItemAnim(() =>
                    {
                        openCradReward(rewardPackID);
                    });
                    break;
            }

            diceNode.checkDiceType();
            journeyMain.setScrollEnabel(!FrenzyJourneyData.getInstance.isAutoPlaying);
            FrenzyJourneyData.getInstance.chessStopMove();
        }

        void boxFly()
        {
            var chestItemNode = journeyMain.getNowItemNodePresenter();
            var flyChestObj = chestItemNode.chestObj;
            flyChestObj.transform.SetParent(journeyUI.uiTransform);
            var target = journeyUI.getMainEmptyBox();
            if (null != target)
            {
                AudioManager.instance.playAudioOnce(AudioPathProvider.getAudioPath(ActivityBlastAudio.IconFly));
                var twID = flyChestObj.transform.movePos(target.uiTransform.position, 0.8f, onComplete: () =>
                  {
                      journeyUI.addBox(awardData.type, boxCountdownTime);
                      flyChestObj.setActiveWhenChange(false);
                      flyChestObj.transform.SetParent(chestItemNode.uiTransform);
                      AudioManager.instance.playAudioOnce(AudioPathProvider.getAudioPath(ActivityBlastAudio.IconFlyIn));
                      checkShowChestFullMsg("Frenzyjourney_Message_5_Text");
                  }, easeType: DG.Tweening.Ease.OutSine);
                TweenManager.tweenPlay(twID);
            }
            FrenzyJourneyData.getInstance.showRunning(false, "boxFly target == null");
        }

        bool checkShowChestFullMsg(string contentMsgKey)
        {
            if (null != journeyUI.getMainEmptyBox())
            {
                return false;
            }

            UiManager.getPresenter<MsgBoxPresenter>().openChestFull(contentKey: contentMsgKey);
            if (FrenzyJourneyData.getInstance.isAutoPlaying)
            {
                Observable.Timer(TimeSpan.FromSeconds(2.0f)).Subscribe(_ =>
                {
                    UiManager.getPresenter<MsgBoxPresenter>().clear();
                });
            }

            return true;
        }

        void clearPresenters()
        {
            FrenzyJourneyData.getInstance.setActivityEndEvent(null);
            AudioManager.instance.stopLoop();
            if (null != bgmAudioSource)
            {
                AudioManager.instance.stopBGMOnObjAndResetMainBGM(bgmAudioSource);
            }
            chessNode.tweenKill();
            var presenterEnum = journeyPresenters.GetEnumerator();
            while (presenterEnum.MoveNext())
            {
                presenterEnum.Current.clear();
            }

            ActivityDataStore.activityPageCloseCall();
            GamePauseManager.gameResume();
        }

        #region AutoMode

        IDisposable autoModeDis;
        void setAutoMode(bool isAutoPlaying)
        {
            if (!FrenzyJourneyData.getInstance.isChessMoving)
            {
                journeyMain.setScrollEnabel(!isAutoPlaying);
            }
            UtilServices.disposeSubscribes(autoModeDis);
            journeyUI.setTreasureBoxEnable(!isAutoPlaying);
            if (!isAutoPlaying)
            {
                return;
            }

            autoModeDis = FrenzyJourneyData.getInstance.isShowRunningSub.Delay(TimeSpan.FromSeconds(0.8f)).Subscribe(runAutoMode).AddTo(journeyUI.uiGameObject);
        }

        void runAutoMode(bool isShowRunning)
        {
            if (FrenzyJourneyData.getInstance.isShowRunning)
            {
                return;
            }
            diceNode.diceAutoClick();
        }

        #endregion
    }

    public enum TutorialStep
    {
        Step1 = 1,
        Step2,
        Step3,
    }
}
