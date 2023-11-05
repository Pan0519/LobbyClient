using CommonILRuntime.Module;
using CommonILRuntime.BindingModule;
using UnityEngine.UI;
using UnityEngine;
using System;
using System.Threading.Tasks;
using Event.Common;
using Service;
using FarmBlast;
using UniRx;
using EventActivity;
using LobbyLogic.NetWork.ResponseStruct;
using CommonPresenter.PackItem;
using LobbyLogic.Audio;
using Lobby.Audio;
using CommonILRuntime.Outcome;
using System.Collections.Generic;
using Debug = UnityLogUtility.Debug;

namespace FrenzyJourney
{
    class JourneyUIPresenter : ContainerPresenter
    {
        public override string objPath => FrenzyJourneyData.getInstance.getPrefabFullPath("activity_fj_ui");
        public override UiLayer uiLayer { get => UiLayer.System; }
        protected override BackHideBehaviour hideBehaviour => BackHideBehaviour.HideMe;

        #region UIs
        Button closeBtn;
        Button infoBtn;
        GameObject diceGroup;
        GameObject tutorialDiceGroup;
        Text lvRewardTxt;
        RectTransform mainMoneyGroupTrans;
        Image roundImg;
        GameObject maskObj;
        Animator changeSceneAnim;
        GameObject[] tutorialObjs = new GameObject[3];

        Image bossProgressImg;
        Text bossProgressTxt;
        Text bossRewardTxt;
        RectTransform bossMoneyGroupTrans;

        GameObject plusObj;
        RectTransform completeItemGroup;
        RectTransform treasureChestDummy;
        RectTransform boosterDummy;
        #endregion
        public DiceNodePresenter diceNodePresenter { get; private set; }

        public Action<FrenzyJourneyBoxData> sendBoxCB;
        public Action<string> boxOpenCardCB;
        Action closeAction;

        TreasureBoxChestNode mainChestNode;
        TreasureBoxChestNode bossChestNode;

        JourneyBoosterNodePresenter diceBoosterNode;
        CoinBoosterNodePresenter coinBoosterNode;
        JourneyBoosterNodePresenter frenzyDiceNode;

        Transform tutorialTreasureTrans = null;

        ActivityAwardData awardData = new ActivityAwardData();
        public Subject<int> toturialStepSubject { get; private set; } = new Subject<int>();
        string bossProgressTweenID;
        string bossProgressTxtTweenID;
        FrenzyJourneyBoxData boxData = new FrenzyJourneyBoxData();
        #region init
        public override void initContainerPresenter()
        {
            resOrder = new string[] { AssetBundleData.getBundleName(BundleType.FrenzyJourney) };
            base.initContainerPresenter();
        }
        public override void initUIs()
        {
            closeBtn = getBtnData("close_btn");
            infoBtn = getBtnData("info_btn");
            lvRewardTxt = getTextData("lv_award_txt");
            maskObj = getGameObjectData("mask_obj");
            roundImg = getImageData("round_img");
            mainMoneyGroupTrans = getRectData("main_money_group_trans");
            diceGroup = getGameObjectData("dice_group");
            tutorialDiceGroup = getGameObjectData("tutorial_dice_group");
            changeSceneAnim = getAnimatorData("change_anim");

            bossProgressImg = getImageData("boss_progress_img");
            bossProgressTxt = getTextData("boss_progress_txt");
            bossRewardTxt = getTextData("boss_reward_txt");
            bossMoneyGroupTrans = getRectData("boss_money_group_trans");
            plusObj = getGameObjectData("plus_obj");
            completeItemGroup = getRectData("item_group");

            treasureChestDummy = getRectData("treasure_chest_dummy");
            boosterDummy = getRectData("booster_dummy_trans");
            for (int i = 0; i < tutorialObjs.Length; ++i)
            {
                tutorialObjs[i] = getGameObjectData($"guild_{i + 1}_obj");
                var tutorialBtn = getBtnData($"tutorial_step_{i + 1}");
                tutorialBtn.onClick.AddListener(toNextTutorial);
            }

            diceBoosterNode = ActivityDataStore.getBoosterGO<JourneyBoosterNodePresenter>(boosterDummy).setJourneyIconImg(BoosterType.Dice);
            coinBoosterNode = ActivityDataStore.getBoosterGO<CoinBoosterNodePresenter>(boosterDummy).setJourneyIconImg(BoosterType.Coin) as CoinBoosterNodePresenter;
            frenzyDiceNode = ActivityDataStore.getBoosterGO<JourneyBoosterNodePresenter>(boosterDummy).setJourneyIconImg(BoosterType.FrenzyDice);

            diceNodePresenter = UiManager.bindNode<DiceNodePresenter>(getNodeData("dice_node").cachedGameObject);
            mainChestNode = UiManager.bindNode<TreasureBoxChestNode>(ActivityDataStore.getTreasureChestGO(treasureChestDummy));
            bossChestNode = UiManager.bindNode<TreasureBoxChestNode>(getNodeData("boss_chest").cachedGameObject);
        }

        public override void init()
        {
            maskObj.setActiveWhenChange(true);
            closeBtn.onClick.AddListener(closeClick);
            infoBtn.onClick.AddListener(openInfoPage);
            bossProgressImg.fillAmount = 0;
            FrenzyJourneyData.getInstance.updateBoostDataSub.Subscribe(updateBoosterData).AddTo(uiGameObject);
            diceNodePresenter.shopSpinEvent = closeClick;
        }

        public void initUIData(JourneyInitResponse baseResponse)
        {
            mainChestNode.setBoxData(baseResponse.TreasureBox);
            mainChestNode.boxClick = boxClick;

            bossChestNode.setBoxData(baseResponse.TreasureBox);
            bossChestNode.boxClick = boxClick;
            updateBoosterData(baseResponse.BoostsData);
            setLvData(baseResponse);
        }
        #endregion

        #region Box
        GameObject boxAwardObj;
        PackItemNodePresenter packItemNode;
        public void setTreasureBoxEnable(bool enable)
        {
            mainChestNode.setBoxBtnInteractable(enable);
            bossChestNode.setBoxBtnInteractable(enable);
        }
        public async void boxClick(TreasuerBoxNodePresenter selectBox)
        {
            if (FrenzyJourneyData.getInstance.isAutoPlaying || FrenzyJourneyData.getInstance.isChessMoving || FrenzyJourneyData.getInstance.isShowRunning)
            {
                selectBox.openBtnInteractable();
                return;
            }
            FrenzyJourneyData.getInstance.showRunning(true, "boxClick");
            AudioManager.instance.playAudioOnce(AudioPathProvider.getAudioPath(ActivityBlastAudio.Open));
            var boxRes = await AppManager.eventServer.sendOpenBox(selectBox.boxID);
            var rewardResult = boxRes.RewardResult[0];
            boxData.awardData = awardData;
            boxData.rewardPackID = string.Empty;
            awardData.parseAwardData(rewardResult);
            if (AwardKind.PuzzlePack == awardData.kind || AwardKind.PuzzleVoucher == awardData.kind)
            {
                packItemNode = PackItemPresenterServices.getSinglePackItem(awardData.type, selectBox.packItemGroup);
            }
            else
            {
                getAwardObj(awardData.kind, selectBox.uiTransform);
            }

            if (awardData.kind != AwardKind.PuzzlePack || awardData.kind != AwardKind.PuzzleVoucher)
            {
                boxData.rewardPackID = boxRes.RewardPackId;
            }

            selectBox.setAnimFinishCB(() =>
            {
                updateAllBoxStatus(selectBox.boxID, 0);
                switch (awardData.kind)
                {
                    case AwardKind.PuzzlePack:
                    case AwardKind.PuzzleVoucher:
                        boxOpenCardCB(boxRes.RewardPackId);
                        break;

                    default:
                        sendBoxAction();
                        break;
                }
            });
            selectBox.playGetAnim();
            switch (awardData.kind)
            {
                case AwardKind.PuzzlePack:
                case AwardKind.PuzzleVoucher:
                    packItemNode.playShowAnim();
                    break;

                default:
                    boxAwardObj.setActiveWhenChange(true);
                    break;
            }
        }

        void getAwardObj(AwardKind awardKind, Transform parentTrans)
        {
            string prefabName = string.Empty; ;

            switch (awardKind)
            {
                case AwardKind.Coin:
                    prefabName = "coin";
                    break;

                case AwardKind.Ticket:
                    prefabName = "dice";
                    break;
            }

            if (string.IsNullOrEmpty(prefabName))
            {
                return;
            }
            var tempObj = ResourceManager.instance.getGameObjectWithResOrder($"{ActivityDataStore.CommonPrefabPath}activity_item_{prefabName}", resOrder);
            boxAwardObj = GameObject.Instantiate(tempObj, parentTrans);
            boxAwardObj.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
            boxAwardObj.setActiveWhenChange(false);
        }

        public void moveBoxAwardObj(Action finishCB)
        {
            switch (awardData.kind)
            {
                case AwardKind.Ticket:
                    var endPos = diceNodePresenter.uiTransform.position;
                    endPos.Set(endPos.x - 1, endPos.y, endPos.z);
                    AudioManager.instance.playAudioOnce(AudioPathProvider.getAudioPath(ActivityBlastAudio.IconFly));
                    var twID = boxAwardObj.transform.movePos(endPos, 0.8f, onComplete: () =>
                      {
                          if (null != finishCB)
                          {
                              finishCB();
                          }
                          AudioManager.instance.playAudioOnce(AudioPathProvider.getAudioPath(ActivityBlastAudio.IconFlyIn));
                          diceNodePresenter.playDiceGetAnim();
                          clearBoxAwardObj();
                      });
                    TweenManager.tweenPlay(twID);
                    break;
            }
        }

        void sendBoxAction()
        {
            if (null == sendBoxCB)
            {
                return;
            }

            sendBoxCB(boxData);
        }

        void updateAllBoxStatus(int boxID, long countDownTime, string type = "")
        {
            Debug.Log("updatAllBox");
            mainChestNode.updateBoxStatus(boxID, type, countDownTime);
            bossChestNode.updateBoxStatus(boxID, type, countDownTime);
        }

        public void addBox(string type, long countDownTime)
        {
            mainChestNode.addBox(type, countDownTime);
            bossChestNode.addBox(type, countDownTime);
        }

        public TreasuerBoxNodePresenter getMainEmptyBox()
        {
            return mainChestNode.getEmptyBox();
        }

        public void clearBoxAwardObj()
        {
            if (null != boxAwardObj)
            {
                GameObject.DestroyImmediate(boxAwardObj);
                boxAwardObj = null;
            }

            if (null != packItemNode)
            {
                ResourceManager.instance.releasePool(packItemNode.uiGameObject.name);
                packItemNode = null;
            }
        }

        #endregion

        void setCloseBtnInteractable(bool enable)
        {
            if (FrenzyJourneyData.getInstance.isAutoPlaying || FrenzyJourneyData.getInstance.isShowRunning)
            {
                enable = false;
            }
            closeBtn.interactable = enable;
        }

        float bossMaxProgressHP;
        int bossNowHP;
        int bossMaxHP;
        public async void setBossData(BossData bossData)
        {
            bossMaxHP = bossData.MaxHP;
            bossMaxProgressHP = 1 / (float)bossData.MaxHP;
            bossNowHP = bossData.MaxHP - bossData.Attack;
            bossRewardTxt.text = bossData.CompleteReward.ToString("N0");
            bossProgressImg.fillAmount = 1 - (bossData.Attack * bossMaxProgressHP);
            bossProgressTxt.text = $"{bossNowHP}";
            changeToBoss();
            await Task.Delay(TimeSpan.FromSeconds(0.5f));
            LayoutRebuilder.ForceRebuildLayoutImmediate(bossMoneyGroupTrans);
        }

        public void updateBossProgress(int attack)
        {
            float endVal = 1 - (attack * bossMaxProgressHP);
            bossProgressTweenID = TweenManager.tweenToFloat(bossProgressImg.fillAmount, endVal, 0.5f, onUpdate: val =>
                {
                    bossProgressImg.fillAmount = val;
                }, onComplete: () =>
                {
                    bossProgressTweenID = string.Empty;
                });

            int endHP = bossMaxHP - attack;
            bossProgressTxtTweenID = TweenManager.tweenToFloat(bossNowHP, endHP, 0.5f, onUpdate: val =>
            {
                bossProgressTxt.text = $"{(int)val}";
            }, onComplete: () =>
            {
                bossNowHP = endHP;
                bossProgressTxtTweenID = string.Empty;
            });
        }

        public void setLvData(JourneyInitResponse baseResponse)
        {
            lvRewardTxt.text = baseResponse.CompleteReward.ToString("N0");
            roundImg.sprite = FrenzyJourneyData.getInstance.getIconSprite($"tex_final_round_{Math.Min(4, baseResponse.Round + 1)}");

            plusObj.setActiveWhenChange(baseResponse.CompleteItem.Length > 0);

            List<GameObject> oldPackItem = new List<GameObject>();
            for (int i = 0; i < completeItemGroup.transform.childCount; ++i)
            {
                oldPackItem.Add(completeItemGroup.transform.GetChild(i).gameObject);
            }

            for (int i = 0; i < oldPackItem.Count; ++i)
            {
                ResourceManager.instance.returnObjectToPool(oldPackItem[i]);
            }

            for (int i = 0; i < baseResponse.CompleteItem.Length; ++i)
            {
                var itemData = baseResponse.CompleteItem[i];
                AwardKind awardKind = ActivityDataStore.getAwardKind(itemData.Kind);
                if (AwardKind.PuzzlePack == awardKind || AwardKind.PuzzleVoucher == awardKind)
                {
                    PackItemPresenterServices.getSinglePackItem(itemData.Type, completeItemGroup);
                }
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(mainMoneyGroupTrans);
        }

        public void closeMaskObj()
        {
            maskObj.setActiveWhenChange(false);
        }

        public void changeToBoss()
        {
            uiChangeScene(FrenzySceneType.Boss);
        }

        public void changeToMain()
        {
            uiChangeScene(FrenzySceneType.Main);
        }

        void uiChangeScene(FrenzySceneType sceneType)
        {
            FrenzyJourneyData.getInstance.frenzySceneType = sceneType;
            changeSceneAnim.SetTrigger($"in_{sceneType.ToString().ToLower()}");
        }

        public void subscribeDiceUpdate()
        {
            FrenzyJourneyData.getInstance.frenzyDiceCountUpdateSub.Subscribe(diceCountSub).AddTo(uiGameObject);
            FrenzyJourneyData.getInstance.isCheckDiceTypeSub.Subscribe(isCheckDiceType).AddTo(uiGameObject);
            FrenzyJourneyData.getInstance.isShowRunningSub.Subscribe(running =>
            {
                setCloseBtnInteractable(!running);
            }).AddTo(uiGameObject);
            FrenzyJourneyData.getInstance.isAutoPlayingSub.Subscribe(isAutoMode =>
            {
                setCloseBtnInteractable(!isAutoMode);
            }).AddTo(uiGameObject);
        }

        void updateBoosterData(JourneyBoosterData boostDatas)
        {
            diceBoosterNode.updateTimerTxt(boostDatas.DiceBoost);
            coinBoosterNode.updateTimerTxt(boostDatas.CoinBoost);
            updateFrenzyDiceCount(boostDatas.FrenzyDice);
        }

        public void updateFrenzyDiceCount(long count)
        {
            frenzyDiceNode.updateTimesTxt(count);
        }

        void diceCountSub(long count)
        {
            updateFrenzyDiceCount(count);
        }

        void isCheckDiceType(bool isCheck)
        {
            if (false == isCheck)
            {
                return;
            }
            diceNodePresenter.checkDiceType();
        }

        public void setCloseAction(Action closeAc)
        {
            closeAction = closeAc;
        }
        #region Tutorial
        int nowTutorStep = 1;
        public void runTutorialStep()
        {
            diceNodePresenter.setBtnsRaycastTarget(false);
            diceNodePresenter.uiRectTransform.SetParent(tutorialDiceGroup.transform);
            showNowTutorialObj();
        }

        public void setTutorialTreasureRect(Transform treasureRect)
        {
            tutorialTreasureTrans = treasureRect;
        }

        public void openFinalTutorialTreasure()
        {
            tutorialTreasureTrans.SetParent(tutorialObjs[2].transform);
            tutorialTreasureTrans.gameObject.setActiveWhenChange(true);
        }

        public void closeTutorialTreasure()
        {
            if (null == tutorialTreasureTrans)
            {
                return;
            }

            tutorialTreasureTrans.gameObject.setActiveWhenChange(false);
        }

        void toNextTutorial()
        {
            diceNodePresenter.uiRectTransform.SetParent(diceGroup.transform);
            nowTutorStep++;
            showNowTutorialObj();
        }

        void showNowTutorialObj()
        {
            toturialStepSubject.OnNext(nowTutorStep);
            for (int i = 0; i < tutorialObjs.Length; ++i)
            {
                tutorialObjs[i].setActiveWhenChange((i + 1) == nowTutorStep);
            }
            if (nowTutorStep >= tutorialObjs.Length)
            {
                diceNodePresenter.setBtnsRaycastTarget(true);
            }
        }

        public GameObject getNowTutorialObj()
        {
            if (nowTutorStep > tutorialObjs.Length)
            {
                return null;
            }
            return tutorialObjs[nowTutorStep - 1];
        }

        #endregion
        void closeClick()
        {
            FrenzyJourneyData.getInstance.stopAutoPlay();
            if (null != closeAction)
            {
                closeAction();
            }
        }

        void openInfoPage()
        {
            UiManager.getPresenter<JourneyTipPresenter>().open();
            FrenzyJourneyData.getInstance.stopAutoPlay();
        }

        public override void clear()
        {
            TweenManager.tweenKill(bossProgressTweenID);
            TweenManager.tweenKill(bossProgressTxtTweenID);
            base.clear();
        }
    }
}
