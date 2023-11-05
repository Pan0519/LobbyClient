using Debug = UnityLogUtility.Debug;
using UnityEngine.UI;
using CommonILRuntime.Module;
using CommonILRuntime.BindingModule;
using Binding;
using System;
using UnityEngine;
using Lobby.Common;
using UniRx;
using EventActivity;
using CommonService;
using Service;
using Services;
using Lobby.LoadingUIModule;
using UnityEngine.SceneManagement;
using Network;
using System.Collections.Generic;
using LobbyLogic.Audio;
using Lobby.Audio;
using System.Threading.Tasks;
using CommonILRuntime.Outcome;

namespace SaveTheDog
{
    public interface IPlayUnlockBtn
    {
        void playUnLockAnim(Action finishCB);

        void playAni(string aniName);

        void playDone();
    }
    public class SaveTheDogMapBtnStage : NodePresenter
    {
        public int stageIndex { get; private set; } = -1;
        public Subject<int> stageClickSub = new Subject<int>();

        public Text stageBtnIDTxt;
        public CustomBtn stageBtn;
        public Image stageBtnImg;
        public Image stageImg;
        Color grayColor;
        Color normalColor;

        bool isLock;
        public override void initUIs()
        {
            stageBtn = getCustomBtnData("stage_btn");
            stageBtnImg = getImageData("stage_btn_img");
            stageImg = getImageData("stage_img");
            stageBtnIDTxt = getTextData("stage_txt");
        }

        public override void init()
        {
            stageBtn.clickHandler = onClick;
            stageBtn.pointerDownHandler = () =>
            {
                setBtnColors(false);
            };
            stageBtn.pointerUPHandler = () =>
            {
                setBtnColors(true);
            };
            ColorUtility.TryParseHtmlString("#FFFFFF", out normalColor);
            ColorUtility.TryParseHtmlString("#858585", out grayColor);
            SaveTheDogMapData.instance.nowOpenStageIDSub.Subscribe(updateSprite).AddTo(uiGameObject);
        }

        public async void onClick()
        {
            if (SaveTheDogMapData.instance.nowOpenStageID == stageIndex)
            {
                return;
            }

            TransitionSaveDogServices.instance.openTransitionPage();
            AudioManager.instance.playAudioOnce(CommonAudioPathProvider.getAudioPath(BasicCommonSound.InfoBtn));
            await Task.Delay(TimeSpan.FromSeconds(0.5f));
            stageClickSub.OnNext(stageIndex);
        }

        public void setStageIndex(int stageIndex)
        {
            isLock = false;
            this.stageIndex = stageIndex;
            stageBtnIDTxt.text = $"{stageIndex + 1}";
            bool isBtnInteractable = true;
            string btnSpriteName = "orange";
            if (stageIndex < SaveTheDogMapData.instance.nowStageID)
            {
                btnSpriteName = "purple";
            }
            else if (stageIndex > SaveTheDogMapData.instance.nowStageID)
            {
                btnSpriteName = "gray";
                isBtnInteractable = false;
                isLock = true;
            }
            stageBtnImg.sprite = LobbySpriteProvider.instance.getSprite<SaveTheDogSpriteProvider>(LobbySpriteType.SaveTheDog, $"stage_btn_{btnSpriteName}");
            setBtnInteractable(isBtnInteractable);
        }

        void updateSprite(int openStageID)
        {
            if (isLock)
            {
                if (stageIndex <= SaveTheDogMapData.instance.nowStageID)
                {
                    isLock = false;
                }
            }

            if (isLock)
            {
                return;
            }
            setBtnInteractable(true);
            string spriteName = openStageID == stageIndex ? "orange" : "purple";
            stageBtnImg.sprite = LobbySpriteProvider.instance.getSprite<SaveTheDogSpriteProvider>(LobbySpriteType.SaveTheDog, $"stage_btn_{spriteName}");
        }

        void setBtnInteractable(bool interactable)
        {
            stageBtn.setInteractable(interactable);
            setBtnColors(interactable);
        }

        void setBtnColors(bool isNormal)
        {
            Color changeColor = isNormal ? normalColor : grayColor;
            stageImg.color = changeColor;
            stageBtnIDTxt.color = changeColor;
        }
    }

    public class SaveTheDogLvBtn : NodePresenter, IPlayUnlockBtn
    {
        Animator statusAnim;
        Button lvBtn;

        SpriteRenderer[] stageRenders = new SpriteRenderer[2];
        SpriteRenderer[] lvRenders = new SpriteRenderer[3];

        SpriteRenderer dashSprite;
        public SaveTheDogSpriteProvider imgProvider { get; private set; }
        public string lockStr = string.Empty;
        public bool isUnLock { get; private set; }
        public bool isDone { get; private set; }
        public int lvID { get; private set; }
        public virtual int changeNumColorTime { get; set; }
        public Subject<int> clicklvID = new Subject<int>();
        Action unlockCB;

        public override void initUIs()
        {
            stageRenders[0] = getBindingData<SpriteRenderer>("stage_units_img");
            stageRenders[1] = getBindingData<SpriteRenderer>("stage_tens_img");

            lvRenders[0] = getBindingData<SpriteRenderer>("level_units_img");
            lvRenders[1] = getBindingData<SpriteRenderer>("level_tens_img");
            lvRenders[2] = getBindingData<SpriteRenderer>("level_hundreds_img");
            dashSprite = getBindingData<SpriteRenderer>("dash_img");
            statusAnim = getAnimatorData("status_anim");
            lvBtn = getBtnData("lv_btn");
        }

        public override void init()
        {
            imgProvider = LobbySpriteProvider.instance.getSpriteProvider<SaveTheDogSpriteProvider>(LobbySpriteType.SaveTheDog);
            lvBtn.onClick.AddListener(onClick);
        }

        public void setLvContent(int stageIndex, int lvID)
        {
            setLvBtnInteractable(true);
            this.lvID = lvID;
            isUnLock = lvID <= SaveTheDogMapData.instance.nowLvID;
            if (SaveTheDogMapData.instance.isFirstLv && lvID <= 0)
            {
                isUnLock = SaveTheDogMapData.instance.isLvAlreadyOpen;
            }

            if (SaveTheDogMapData.instance.nowStageID > stageIndex)
            {
                isDone = true;
                isUnLock = true;
            }
            else
            {
                isDone = lvID != SaveTheDogMapData.instance.nowLvID && isUnLock;
            }


            if (!isUnLock)
            {
                playAni("lock");

            }
            else if (isDone)
            {
                playDone();
            }

            lockStr = isUnLock ? string.Empty : "_dark";
            dashSprite.sprite = imgProvider.getSprite($"dash{lockStr}");
            setSpriteNum(stageIndex + 1, stageRenders);
            setSpriteNum(lvID + 1, lvRenders);
        }

        public void setLvBtnInteractable(bool able)
        {
            lvBtn.interactable = able;
        }

        void setSpriteNum(int id, SpriteRenderer[] renderers)
        {
            var numStr = id.ToString();
            int renderNum = 0;
            for (int i = renderers.Length - 1; i >= 0; --i)
            {
                var render = renderers[i];
                if (i > numStr.Length - 1)
                {
                    render.gameObject.setActiveWhenChange(false);
                    continue;
                }
                render.gameObject.setActiveWhenChange(true);
                render.sprite = imgProvider.getSprite($"{numStr[renderNum]}{lockStr}");
                renderNum++;
            }
        }
        public void playAni(string aniName)
        {
            statusAnim.SetTrigger(aniName);
        }

        public void playDone()
        {
            playAni("done");
        }

        public void playTouchLock()
        {
            playAni("touch_lock");
        }

        public void playUnLockAnim(Action finishCB)
        {
            unlockCB = finishCB;
            setLvBtnInteractable(false);
            string handAnimName = string.Empty;
            if (SaveTheDogMapData.instance.nowLvID < 3 && SaveTheDogMapData.instance.nowStageID <= 0)
            {
                handAnimName = "_hand";
            }
            playAni($"unlock{handAnimName}");
            Observable.TimerFrame(changeNumColorTime).Subscribe(_ =>
            {
                dashSprite.sprite = imgProvider.getSprite("dash");
                changeRendersToNormal(stageRenders);
                changeRendersToNormal(lvRenders);
                changeIconToNormal();
                waitOpenBtn();
            }).AddTo(uiGameObject);
        }

        async void waitOpenBtn()
        {
            await Task.Delay(TimeSpan.FromSeconds(1.0f));
            isUnLock = true;
            await Task.Delay(TimeSpan.FromSeconds(1.0f));
            setLvBtnInteractable(true);
            if (null != unlockCB)
            {
                unlockCB();
            }
        }

        public virtual void changeIconToNormal()
        {

        }

        void changeRendersToNormal(SpriteRenderer[] renders)
        {
            for (int i = 0; i < renders.Length; ++i)
            {
                var render = renders[i];
                if (!render.gameObject.activeSelf)
                {
                    continue;
                }
                var newName = render.sprite.name.Replace("_dark", string.Empty).Trim();
                render.sprite = imgProvider.getSprite(newName);
            }
        }

        public virtual void onClick()
        {
            setLvBtnInteractable(false);
            AudioManager.instance.playAudioOnce(CommonAudioPathProvider.getAudioPath(BasicCommonSound.InfoBtn));
        }

        public async Task noticeLvClick()
        {
            clicklvID.OnNext(lvID);
            if (false == SaveTheDogMapData.instance.checkClickIDAndStage())
            {
                return;
            }
            var response = await AppManager.lobbyServer.setNewbieAdventureNotice();
            if (Result.OK == response.result)
            {
                SaveTheDogMapData.instance.setIsAlreadyOpen(true);
            }
        }
    }

    public class SaveTheDogMapBtnDoge : SaveTheDogLvBtn
    {
        public int dogLv;
        public override int changeNumColorTime { get => 61; }
        public string lvRole;
        SpriteRenderer roleRender;
        string spriteName;
        public override void initUIs()
        {
            base.initUIs();
            roleRender = getBindingData<SpriteRenderer>("role_sprite_render");
        }
        public void setDogInfo(int lv, string role)
        {
            dogLv = lv;
            lvRole = role.Split('_')[1];
            spriteName = $"save{lvRole}_{lvRole}";
            if (isUnLock)
            {
                changeSprite(spriteName);
                return;
            }
            changeSprite($"{spriteName}_gray");
        }
        public override void changeIconToNormal()
        {
            changeSprite(spriteName);
        }
        void changeSprite(string changeSpriteName)
        {
            var renderSprite = LobbySpriteProvider.instance.getSprite<SaveTheDogSpriteProvider>(LobbySpriteType.SaveTheDog, changeSpriteName);
            roleRender.sprite = renderSprite;
        }

        public override async void onClick()
        {
            base.onClick();
            if (!isUnLock)
            {
                playTouchLock();
                setLvBtnInteractable(true);
                return;
            }

            await noticeLvClick();
            DataStore.getInstance.extraGameServices.levelID = dogLv;
            openDogPage();
        }

        GameInfo saveTheDogInfo;
        async void openDogPage()
        {
            TransitionSaveDogServices.instance.openTransitionPage();
            await Task.Delay(TimeSpan.FromSeconds(1.0f));
            DataStore.getInstance.extraGameServices.characterType = lvRole;
            DataStore.getInstance.dataInfo.setNowPlayGameID("20001");
            saveTheDogInfo = await DataStore.getInstance.dataInfo.getNowPlayGameInfo();
            invokeGame();
        }

        async void invokeGame()
        {
            if (string.IsNullOrEmpty(saveTheDogInfo.serverIP))
            {
                Debug.LogError($"get {saveTheDogInfo.name} ServerIP is Empty");
                UtilServices.backToLobby();
                return;
            }

            AppDomainManager domainManager = await new AppDomainManager().domainInit($"{saveTheDogInfo.name}");
            domainManager.invokeLogicMainMethod("setInfos", DataStore.getInstance);
            domainManager.invokeLogicMain(DataStore.getInstance.playerInfo.userID, saveTheDogInfo.serverIP);
            setLvBtnInteractable(true);
            await Task.Delay(TimeSpan.FromSeconds(1.0f));
            TransitionSaveDogServices.instance.closeTransitionPage();
        }
    }

    public class SaveTheDogMapBtnSlot : SaveTheDogLvBtn
    {
        public string slotGameID { get; private set; }

        public override int changeNumColorTime { get => 71; }
        SpriteRenderer slotRender;
        BindingNode rewardLayoutNode;

        public override void initUIs()
        {
            base.initUIs();
            slotRender = getBindingData<SpriteRenderer>("slot_render");
            rewardLayoutNode = getNodeData("reward_layout_node");
        }
        public void setSlotGameID(string slotID)
        {
            slotGameID = slotID;
            var rewardNodePresenter = UiManager.bindNode<RewardNodePresenter>(rewardLayoutNode.cachedGameObject);
            rewardNodePresenter.uiGameObject.setActiveWhenChange(!isDone);
            if (!isDone)
            {
                rewardNodePresenter.setRewards(SaveTheDogMapData.instance.rewardInfoDict[slotGameID]);
            }

            //setRewards();

            if (!isDone)
            {
                changeSlotSprite(isUnLock ? string.Empty : "_gray");
                return;
            }
            setLvBtnInteractable(false);
            changeSlotSprite("_gray");
        }

        void changeSlotSprite(string spriteEndName)
        {
            var renderSprite = LobbySpriteProvider.instance.getSprite<SaveTheDogSpriteProvider>(LobbySpriteType.SaveTheDog, $"game_short_{slotGameID}{spriteEndName}");
            slotRender.sprite = renderSprite;
        }

        //void setRewards()
        //{
        //    rewardLayoutObj.setActiveWhenChange(!isDone);
        //    if (isDone)
        //    {
        //        return;
        //    }
        //    var rewards =;
        //    GameObject rewardObj;

        //    for (int i = 0; i < rewards.Length; ++i)
        //    {
        //        var reward = rewards[i];
        //        if (reward.amount <= 0)
        //        {
        //            continue;
        //        }
        //        var rewardKind = ActivityDataStore.getAwardKind(reward.kind);
        //        switch (rewardKind)
        //        {
        //            case AwardKind.PuzzlePack:
        //            case AwardKind.PuzzleVoucher:
        //                rewardObj = GameObject.Instantiate(ResourceManager.instance.getGameObjectWithResOrder("prefab/reward_item/reward_item_pack", AssetBundleData.getBundleName(BundleType.SaveTheDog)), rewardLayout);
        //                var packPresenter = UiManager.bindNode<RewardPackItemNode>(rewardObj);
        //                packPresenter.setPuzzlePack(reward.type);
        //                var pos = packPresenter.uiRectTransform.anchoredPosition;
        //                pos.Set(pos.x, -8);
        //                packPresenter.uiRectTransform.anchoredPosition = pos;
        //                break;

        //            case AwardKind.Coin:
        //            case AwardKind.Ticket:
        //                rewardObj = GameObject.Instantiate(ResourceManager.instance.getGameObjectWithResOrder("prefab/reward_item/reward_item", AssetBundleData.getBundleName(BundleType.SaveTheDog)), rewardLayout);
        //                var rewardPresenter = UiManager.bindNode<RewardItemNode>(rewardObj);
        //                rewardPresenter.setRewardData(reward);
        //                rewardPresenter.changeNumScale(2.5f);
        //                break;

        //            default:
        //                Debug.LogError($"get error awardKind -{reward}");
        //                break;
        //        }
        //    }

        //    float layoutScale = 0.005f;
        //    if (rewardLayout.transform.childCount < 3)
        //    {
        //        layoutScale = 0.006f;
        //    }
        //    rewardLayout.localScale = new Vector3(layoutScale, layoutScale, layoutScale);
        //}

        public override void changeIconToNormal()
        {
            changeSlotSprite(string.Empty);
        }

        public override async void onClick()
        {
            base.onClick();
            if (!isUnLock)
            {
                playTouchLock();
                return;
            }
            if (slotGameID.Equals("10002"))
            {
                DataStore.getInstance.guideServices.setNowGameGuideStep((int)DataStore.getInstance.guideServices.getSaveGameGuideStep());
            }
            await noticeLvClick();
            toGameScene();
        }
        async void toGameScene()
        {
            if (false == SaveTheDogMapData.instance.checkClickIDAndStage())
            {
                return;
            }
            SaveTheDogMapData.instance.isOpenSaveTheDog = true;
            DataStore.getInstance.dataInfo.setChooseBetClass(ChooseBetClass.Adventure, 0);
            DataStore.getInstance.dataInfo.setNowPlayGameID(slotGameID);
            await LoadingUIManager.instance.loadScreenOrientationSprite();
            await LoadingUIManager.instance.openGameLoadingPage();
            TweenManager.killAll();
            UiManager.clearAllPresenter();
            SceneManager.LoadScene("Game");
        }
    }

    public class MapLineNode : NodePresenter
    {
        public Animator growAnim;

        public override void initUIs()
        {
            growAnim = getAnimatorData("map_line_anim");
        }

        public async void playGrowAnim(Action animCB)
        {
            growAnim.SetTrigger("grow");
            IDisposable growAnimDis = null;
            growAnimDis = Observable.EveryUpdate().Subscribe(_ =>
             {
                 var statInfo = growAnim.GetCurrentAnimatorStateInfo(0);
                 if (statInfo.IsName("fill"))
                 {
                     growAnimDis.Dispose();
                     if (null != animCB)
                     {
                         animCB();
                     }
                 }
             }).AddTo(uiGameObject);
            await Task.Delay(TimeSpan.FromSeconds(2.5f));
            AudioManager.instance.playAudioOnce(AudioPathProvider.getAudioPath(SaveTheDogMapAudio.MapLine));
        }

        public void setFull()
        {
            growAnim.SetTrigger("full");
        }
    }

    public class SaveTheDogMapBtnGift : NodePresenter, IPlayUnlockBtn
    {
        public bool isUnLock { get; private set; }
        bool isDone;

        Button clickBtn;
        Animator statusAnim;
        public Action playNextBtnUnLock;
        int lvID;
        BindingNode rewardLayoutNode;
        public override void initUIs()
        {
            clickBtn = getBtnData("gift_btn");
            statusAnim = getAnimatorData("gift_anim");
            rewardLayoutNode = getNodeData("reward_layout_node");
        }

        public override void init()
        {
            clickBtn.onClick.AddListener(clickRedeem);
        }

        public void setLvContent(int lvID)
        {
            this.lvID = lvID;
            isUnLock = lvID <= SaveTheDogMapData.instance.nowLvID;
            if (SaveTheDogMapData.instance.isFirstLv && lvID <= 0)
            {
                isUnLock = SaveTheDogMapData.instance.isLvAlreadyOpen;
            }

            if (SaveTheDogMapData.instance.nowOpenStageID < SaveTheDogMapData.instance.nowStageID)
            {
                isDone = true;
            }
            else if (SaveTheDogMapData.instance.nowOpenStageID == SaveTheDogMapData.instance.nowStageID)
            {
                isDone = lvID < SaveTheDogMapData.instance.nowLvID;
            }
            rewardLayoutNode.cachedGameObject.setActiveWhenChange(!isDone);
            setRewards();
            if (isDone)
            {
                playDone();
            }
            else if (isUnLock)
            {
                playAni("unlock");
            }
            else
            {
                playAni("lock");
            }
        }

        void setRewards()
        {
            if (isDone)
            {
                return;
            }
            var rewardLayoutPresenter = UiManager.bindNode<RewardNodePresenter>(rewardLayoutNode.cachedGameObject);
            rewardLayoutPresenter.setRewards(SaveTheDogMapData.instance.getGiftRewardInfos(lvID));
        }

        public void playDone()
        {
            playAni("done");
            rewardLayoutNode.gameObject.setActiveWhenChange(false);
        }

        async void clickRedeem()
        {
            if (false == isUnLock)
            {
                playAni("touch_lock");
                return;
            }

            SaveTheDogMapData.instance.setNowClickLvID(lvID);
            clickBtn.interactable = false;
            var adventureRedeem = await AppManager.lobbyServer.getNewbieAdventureRedeem();
            if (Result.OK != adventureRedeem.result)
            {
                return;
            }
            SaveTheDogMapData.instance.setIsAlreadyGrow(false);
            var rewardRedeem = await AppManager.lobbyServer.getRewardPacks(adventureRedeem.rewardPackId);
            AudioManager.instance.playAudioOnce(AudioPathProvider.getAudioPath(SaveTheDogMapAudio.Gift));
            UiManager.getPresenter<SaveTheDogGiftRewardPresenter>().openRewardPage(rewardRedeem.rewards, () =>
            {
                SaveTheDogMapData.instance.updateAdventureRecord(adventureRedeem.adventureRecord);
                if (null != playNextBtnUnLock)
                {
                    playNextBtnUnLock();
                }
            });
        }

        public void playUnLockAnim(Action finishCB)
        {
            playAni("unlock");
            clickBtn.interactable = true;
            isUnLock = true;
            Observable.TimerFrame(100).Subscribe(_ =>
            {
                if (null != finishCB)
                {
                    finishCB();
                }
            }).AddTo(uiGameObject);
        }

        public void playAni(string aniName)
        {
            statusAnim.SetTrigger(aniName);
        }
    }

    public class SaveTheDogMapBtnTreasure : NodePresenter
    {
        //Button clickBtn;
        Animator statusAnim;
        Text totalRewardTxt;
        int lvID;
        public override void initUIs()
        {
            //clickBtn = getBtnData("treasure_btn");
            statusAnim = getAnimatorData("treasure_anim");
            totalRewardTxt = getTextData("total_reward_txt");
        }

        public void setLVID(int id)
        {
            lvID = id;
        }

        public void setTotalReward(ulong reward)
        {
            totalRewardTxt.text = reward.ToString("N0");
            LayoutRebuilder.ForceRebuildLayoutImmediate(totalRewardTxt.transform.parent.transform as RectTransform);
        }

        public void setDoneStatus(bool isdone)
        {
            if (isdone)
            {
                statusAnim.SetTrigger("done");
                return;
            }
        }

        public async void clickRedeem()
        {
            if (SaveDogLvKind.Treasure != SaveTheDogMapData.instance.getNowRecordKind() || SaveTheDogMapData.instance.nowStageID != SaveTheDogMapData.instance.nowOpenStageID)
            {
                return;
            }
            SaveTheDogMapData.instance.setNowClickLvID(lvID);
            if (!SaveTheDogMapData.instance.checkClickIDAndStage())
            {
                return;
            }
            var notice = await AppManager.lobbyServer.setNewbieAdventureNotice();
            SaveTheDogMapData.instance.setIsAlreadyOpen(true);
            var adventureRedeem = await AppManager.lobbyServer.getNewbieAdventureRedeem();
            if (Result.OK != adventureRedeem.result)
            {
                return;
            }
            var rewardRedeem = await AppManager.lobbyServer.getRewardPacks(adventureRedeem.rewardPackId);
            AudioManager.instance.playAudioOnce(AudioPathProvider.getAudioPath(SaveTheDogMapAudio.Treasure));
            UiManager.getPresenter<SaveTheDogTreasureRewardPresenter>().openPage(rewardRedeem.rewards, adventureRedeem.adventureRecord);
            setDoneStatus(true);
        }
    }

    public class RewardNodePresenter : NodePresenter
    {
        RectTransform layoutRect;

        public override void initUIs()
        {
            layoutRect = getRectData("reward_layout_rect");
        }

        public void setRewards(Reward[] rewards)
        {
            GameObject rewardObj;
            clearRewardObjs();
            for (int i = 0; i < rewards.Length; ++i)
            {
                var reward = rewards[i];
                if (reward.amount <= 0)
                {
                    continue;
                }
                var rewardKind = ActivityDataStore.getAwardKind(reward.kind);
                switch (rewardKind)
                {
                    case AwardKind.PuzzlePack:
                    case AwardKind.PuzzleVoucher:
                        rewardObj = GameObject.Instantiate(ResourceManager.instance.getGameObjectWithResOrder("prefab/reward_item/reward_item_pack", AssetBundleData.getBundleName(BundleType.SaveTheDog)), layoutRect);
                        var packPresenter = UiManager.bindNode<RewardPackItemNode>(rewardObj);
                        packPresenter.setPuzzlePack(reward.type);
                        var pos = packPresenter.uiRectTransform.anchoredPosition;
                        pos.Set(pos.x, -8);
                        packPresenter.uiRectTransform.anchoredPosition = pos;
                        break;

                    case AwardKind.Coin:
                    case AwardKind.Ticket:
                        rewardObj = GameObject.Instantiate(ResourceManager.instance.getGameObjectWithResOrder("prefab/reward_item/reward_item", AssetBundleData.getBundleName(BundleType.SaveTheDog)), layoutRect);
                        var rewardPresenter = UiManager.bindNode<RewardItemNode>(rewardObj);
                        rewardPresenter.setRewardData(reward);
                        rewardPresenter.changeNumScale(2.5f);
                        break;

                    default:
                        Debug.LogError($"get error awardKind -{reward}");
                        break;
                }
            }

            float layoutScale = 0.005f;
            if (layoutRect.childCount < 3)
            {
                layoutScale = 0.006f;
            }
            layoutRect.localScale = new Vector3(layoutScale, layoutScale, layoutScale);
        }
        void clearRewardObjs()
        {
            List<GameObject> objs = new List<GameObject>();
            for (int i = 0; i < layoutRect.childCount; ++i)
            {
                objs.Add(layoutRect.GetChild(i).gameObject);
            }

            for (int i = 0; i < objs.Count; ++i)
            {
                GameObject.DestroyImmediate(objs[i]);
            }
        }
    }
}