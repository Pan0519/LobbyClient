using UnityEngine.UI;
using UnityEngine;
using Services;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using CommonService;
using CommonILRuntime.Module;
using CommonPresenter;
using CommonILRuntime.BindingModule;
using HighRoller;
using UniRx;
using Lobby.Common;
using Random = UnityEngine.Random;

namespace Lobby.PickupBetPage
{
    class PickupBetPresenter : SystemUIBasePresenter
    {
        public override string objPath { get { return "prefab/lobby/room_entry"; } }
        public override UiLayer uiLayer { get { return UiLayer.GameMessage; } }
        #region [top option]
        CustomBtn btn_goregular;
        GameObject img_goregular_notactive;

        CustomBtn btn_gohighroller;
        GameObject img_gohighroller_notactive;
        GameObject img_gohighroller_lock;
        RectTransform betBGRect;
        RectTransform activeBetBGRect;
        Animator vipTipAnim;
        GameObject lastChoiseObj;
        RectTransform roomGroupRect;
        RectTransform vipGroupRect;
        RectTransform normalGroupRect;
        #endregion

        #region [ Game Info ]
        Text text_JP;
        GameObject Obj_Jp;
        Image img_Banner;
        //Image img_NewMark;
        //GameObject img_LimitMark;
        #endregion

        Button btn_closePage;
        Button btn_nextgame;
        Button btn_lastgame;

        private bool isStatusRegular = true;
        private bool isOpenHighRoller;
        //private int lastBetClassPosition = -1;

        private LobbyGameInfo nowFocusGame;
        private int nowGameIndex;

        float hightBGHeight;
        float regularBGHeight;
        private List<GameInfo> gameList { get { return DataStore.getInstance.dataInfo.onLineGameInfos; } }

        private Dictionary<string, BetBase> nowBetBase;

        private BetBtnNode[] upBetBtnNodes = new BetBtnNode[2];

        private Action<string> changeToGame;
        private long nowJp;

        RoomData regularRoomData = new RoomData();
        RoomData highRoomData = new RoomData();
        LobbyItemSpriteProvider itemSpriteProvider;
        public override void initUIs()
        {
            #region [top option]
            btn_goregular = getCustomBtnData("btn_goregular");
            img_goregular_notactive = getGameObjectData("img_goregular_notactive");

            btn_gohighroller = getCustomBtnData("btn_gohighroller");
            img_gohighroller_notactive = getGameObjectData("img_gohighroller_notactive");
            img_gohighroller_lock = getGameObjectData("img_gohighroller_lock");
            lastChoiseObj = getGameObjectData("last_choise");
            roomGroupRect = getRectData("room_group_rect");
            vipGroupRect = getRectData("vip_group_rect");
            normalGroupRect = getRectData("normal_group_rect");
            #endregion

            #region [ Game info ]
            img_Banner = getImageData("game_banner_in_choose");

            text_JP = getTextData("jp_num_txt");
            Obj_Jp = getGameObjectData("jp_obj");
            //img_NewMark = getImageData("game_state_img");
            //img_LimitMark = getGameObjectData("game_mark_obj");
            #endregion

            btn_closePage = getBtnData("btn_close_page");
            btn_nextgame = getBtnData("btn_nextgame");
            btn_lastgame = getBtnData("btn_previousgame");
            betBGRect = getRectData("img_bgbet_regular");
            activeBetBGRect = getRectData("active_bg_rect");
            vipTipAnim = getAnimatorData("vip_tip_anim");

        }
        public override void init()
        {
            base.init();
            itemSpriteProvider = LobbySpriteProvider.instance.getSpriteProvider<LobbyItemSpriteProvider>(LobbySpriteType.LobbyItem);
            hightBGHeight = betBGRect.rect.height;
            regularBGHeight = (betBGRect.rect.height * 0.65f);
            vipTipAnim.gameObject.setActiveWhenChange(false);
            checkHighRollerOpen();
            setIntroBtnObjCollections();
            registButtonEvent();
            setNotactive();
            closeVipTip();
        }

        void checkHighRollerOpen()
        {
            var accessInfo = HighRollerDataManager.instance.accessInfo;
            if (null == accessInfo)
            {
                isOpenHighRoller = false;
            }
            else
            {
                CompareTimeResult timeResult = HighRollerDataManager.instance.getAccessExpiredTimeCompareResult();
                isOpenHighRoller = CompareTimeResult.Later == timeResult;
            }

            img_gohighroller_lock.setActiveWhenChange(!isOpenHighRoller);
            img_goregular_notactive.setActiveWhenChange(!isOpenHighRoller);
        }

        public async void setFocusGame(LobbyGameInfo nextFocusGame)
        {
            nowFocusGame = nextFocusGame;
            DataStore.getInstance.dataInfo.setNowPlayGameID(nowFocusGame.gameID);
            regularRoomData.showGameBetInfos = await DataStore.getInstance.dataInfo.getNowRegularBetDataInfoList();
            highRoomData.showGameBetInfos = await DataStore.getInstance.dataInfo.getNowPlayerHighRollerBetDataInfoList();
            nowBetBase = await DataStore.getInstance.dataInfo.getGameBetBase();
            isStatusRegular = !DataStore.getInstance.playerInfo.hasHighRollerPermission;
            resetFocusgameIndex();
            registArrowEvent();
            updateAllView();
            UiManager.getPresenter<LobbyTopBarPresenter>().closeOptionListObj();
            vipTipAnim.gameObject.setActiveWhenChange(false);
            if (!isStatusRegular)
            {
                changeRoomToHighRoller();
            }
            open();
        }

        public void setChangeToGameScene(Action<string> toGameScene)
        {
            changeToGame = toGameScene;
        }

        private void setIntroBtnObjCollections()
        {
            for (int i = 0; i < upBetBtnNodes.Length; ++i)
            {
                var betBtnNode = UiManager.bindNode<BetBtnNode>(getNodeData($"btn_group_node_{i}").cachedGameObject);
                betBtnNode.setID(i);
                betBtnNode.clickSub.Subscribe(SelectBetClassBtnEvent).AddTo(betBtnNode.uiGameObject);
                upBetBtnNodes[i] = betBtnNode;
            }

            var lowBtns = regularRoomData.lowBtnNodes;
            for (int i = 0; i < lowBtns.Length; ++i)
            {
                var betBtnNode = UiManager.bindNode<BetBtnNode>(getNodeData($"regular_bet_btn_node_{i}").cachedGameObject);
                betBtnNode.setID(i + upBetBtnNodes.Length);
                betBtnNode.clickSub.Subscribe(SelectBetClassBtnEvent).AddTo(betBtnNode.uiGameObject);
                lowBtns[i] = betBtnNode;
            }

            lowBtns = highRoomData.lowBtnNodes;
            for (int i = 0; i < lowBtns.Length; ++i)
            {
                var betBtnNode = UiManager.bindNode<BetBtnNode>(getNodeData($"high_btn_group_node_{i}").cachedGameObject);
                betBtnNode.setID(i + upBetBtnNodes.Length);
                betBtnNode.clickSub.Subscribe(SelectBetClassBtnEvent).AddTo(betBtnNode.uiGameObject);
                lowBtns[i] = betBtnNode;
            }
        }
        #region RunJP
        DataInfo dataInfo { get { return DataStore.getInstance.dataInfo; } }
        async Task<long> getInitJPValue()
        {
            long maxJP;
            if (isStatusRegular)
            {
                maxJP = await dataInfo.getRegularMaxJP(nowFocusGame.gameID);
            }
            else
            {
                maxJP = await dataInfo.getHighRollerMaxJP(nowFocusGame.gameID);
            }
            //Debug.Log($"maxJP {maxJP} , jackpotMultiplier {dataInfo.getGameInfo(nowFocusGame.gameID).jackpotMultiplier} playerInfo : {DataStore.getInstance.playerInfo.level}");
            var result = maxJP * dataInfo.getGameInfo(nowFocusGame.gameID).jackpotMultiplier * (1.0f + Random.Range(0.5f, 2.0f) * 0.01f);
            return (long)result;
        }

        private async void initRunJP()
        {
            for (int i = 0; i < tweenIDs.Count; ++i)
            {
                TweenManager.tweenKill(tweenIDs[i]);
            }
            nowJp = await getInitJPValue();
            Obj_Jp.setActiveWhenChange(nowJp > 0);
            if (nowJp <= 0)
            {
                return;
            }
            runJP();
        }
        List<string> tweenIDs = new List<string>();
        bool isReadyClear = false;
        void runJP()
        {
            if (isReadyClear)
            {
                return;
            }
            updateJPText(nowJp);
            long maxJP = (long)(nowJp + (nowJp * (Random.Range(5.0f, 15.0f) * 0.01)));
            float jpRunDurationTime = (maxJP - nowJp) / (nowJp * 0.001f);
            tweenIDs.Add(TweenManager.tweenToLong(nowJp, maxJP, jpRunDurationTime, updateJPText, runJP));
        }
        #endregion
        private int getLastBetInfo(string key)
        {
            if (!PlayerPrefs.HasKey(key))
            {
                return -1;
            }

            return PlayerPrefs.GetInt(key);
        }

        private void setGameInfo()
        {
            img_Banner.sprite = itemSpriteProvider.getSprite($"game_{nowFocusGame.gameID}");
        }

        private void registArrowEvent()
        {
            btn_lastgame.onClick.AddListener(() =>
            {
                ArrowBtnEvent(false);
            });

            btn_nextgame.onClick.AddListener(() =>
            {
                ArrowBtnEvent(true);
            });
        }

        private async void ArrowBtnEvent(bool isIndexForward)
        {
            while (true)
            {
                if (isIndexForward)
                {
                    nowGameIndex++;
                }
                else
                {
                    nowGameIndex--;
                }

                checkFocusGameIndex();
                var gameInfo = gameList[nowGameIndex];
                if (gameInfo.open && gameInfo.isUnLock)
                {
                    nowFocusGame = new LobbyGameInfo();
                    nowFocusGame.setGameInfo(gameInfo);
                    break;
                }
            }

            DataStore.getInstance.dataInfo.setNowPlayGameID(nowFocusGame.gameID);
            nowBetBase = await DataStore.getInstance.dataInfo.getGameBetBase();
            updateAllView();
        }

        private void updateAllView()
        {
            setGameInfo();
            initRunJP();
            updateSuggestBetClass();
            updateShowBetBtnValue();
        }

        private void resetFocusgameIndex()
        {
            nowGameIndex = gameList.FindIndex(x => x.id == nowFocusGame.gameID);

            if (nowGameIndex == -1)
            {
                Debug.LogError("nowFocusGame id not find match in game list");
            }
        }

        private void checkFocusGameIndex()
        {
            if (nowGameIndex < 0)
            {
                nowGameIndex = gameList.Count - 1;
            }
            else if (nowGameIndex >= gameList.Count)
            {
                nowGameIndex = 0;
            }
        }

        private Sprite loadSprite(string texturePath)
        {
            return Util.getSpriteFromPath(texturePath);
        }

        private void updateSuggestBetClass()
        {
            getRegularBtnShowValue();
            getHighRollBtnShowValue();
        }

        private void setNotactive()
        {
            img_goregular_notactive.setActiveWhenChange(!isStatusRegular);
            img_gohighroller_notactive.setActiveWhenChange(isStatusRegular);
            Vector2 bgSize = activeBetBGRect.sizeDelta;
            bgSize.Set(bgSize.x, isStatusRegular ? regularBGHeight : hightBGHeight);
            activeBetBGRect.sizeDelta = bgSize;
            highRoomData.isLowBtnsActive(!isStatusRegular);
            regularRoomData.isLowBtnsActive(isStatusRegular);
            if (!isStatusRegular)
            {
                normalGroupRect.SetAsFirstSibling();
            }
            else
            {
                vipGroupRect.SetAsFirstSibling();
            }
        }

        private void getRegularBtnShowValue()
        {
            var lastBetClassPosition = getLastBetInfo(ChooseBetClass.Regular);
            decimal countedMidIndex = regularRoomData.showGameBetInfos.Count / 2;
            int currectMidIndex = (int)Math.Round(countedMidIndex);
            regularRoomData.resetLastBet();
            regularRoomData.showBetPoint = new int[4];
            regularRoomData.showBetPoint[0] = regularRoomData.showGameBetInfos.Count - 2;
            regularRoomData.showBetPoint[1] = currectMidIndex + 1;
            regularRoomData.showBetPoint[2] = currectMidIndex - 1;
            regularRoomData.showBetPoint[3] = 1;

            if (lastBetClassPosition < 0)
            {
                return;
            }

            if (lastBetClassPosition >= regularRoomData.showGameBetInfos.Count - 3)
            {
                regularRoomData.setLastBetPos(0, lastBetClassPosition);
                return;
            }

            if (lastBetClassPosition >= currectMidIndex && lastBetClassPosition <= regularRoomData.showGameBetInfos.Count - 4)
            {
                regularRoomData.setLastBetPos(1, lastBetClassPosition);
                return;
            }

            if (lastBetClassPosition <= currectMidIndex - 1 && lastBetClassPosition > regularRoomData.showBetPoint[3] + 4)
            {
                regularRoomData.setLastBetPos(2, lastBetClassPosition);
                return;
            }

            if (lastBetClassPosition <= regularRoomData.showBetPoint[3] + 4)
            {
                regularRoomData.setLastBetPos(3, lastBetClassPosition);
            }
        }
        private void getHighRollBtnShowValue()
        {
            var lastBetClassPosition = getLastBetInfo(ChooseBetClass.High_Roller);
            highRoomData.resetLastBet();
            highRoomData.showBetPoint = new int[4];
            highRoomData.showBetPoint[0] = highRoomData.showGameBetInfos.Count - 2;
            int halfBetID = highRoomData.showGameBetInfos.Count / 2;
            highRoomData.showBetPoint[1] = halfBetID + 3;
            highRoomData.showBetPoint[2] = halfBetID - 3;
            highRoomData.showBetPoint[3] = 1;

            if (lastBetClassPosition < 0)
            {
                return;
            }

            if (lastBetClassPosition < highRoomData.showBetPoint[3])
            {
                highRoomData.setLastBetPos(3, lastBetClassPosition);
                return;
            }

            if (lastBetClassPosition > highRoomData.showBetPoint[0])
            {
                highRoomData.setLastBetPos(0, lastBetClassPosition);
                return;
            }

            for (int i = 0; i < highRoomData.showBetPoint.Length - 1; ++i)
            {
                if (lastBetClassPosition < highRoomData.showBetPoint[i] && lastBetClassPosition > highRoomData.showBetPoint[i + 1])
                {
                    highRoomData.setLastBetPos(i + 1, lastBetClassPosition);
                    break;
                }
            }
        }

        private void updateShowBetBtnValue()
        {
            lastChoiseObj.transform.SetParent(roomGroupRect);
            lastChoiseObj.setActiveWhenChange(false);
            BetBase baseBet = null;
            nowBetBase.TryGetValue(GetRoomTypeKey(), out baseBet);
            RoomData roomData = isStatusRegular ? regularRoomData : highRoomData;
            float UpamountLine = roomData.showGameBetInfos.Count * baseBet.percent * 0.01f;
            for (int count = 0; count < upBetBtnNodes.Length; count++)
            {
                int theBetID = roomData.showBetPoint[count];
                int amount = (theBetID >= UpamountLine) ? baseBet.upAmount : baseBet.downAmount;
                var betNode = upBetBtnNodes[count];
                var bet = roomData.showGameBetInfos[theBetID].bet;
                betNode.setBetText(bet * amount);
                if (roomData.lastBetID == count)
                {
                    betNode.addLastObj(lastChoiseObj);
                }
                else
                {
                    betNode.removeLastObj();
                }
            }

            for (int i = 0; i < roomData.lowBtnNodes.Length; ++i)
            {
                int betNodeID = i + upBetBtnNodes.Length;
                int theBetID = roomData.showBetPoint[betNodeID];
                int amount = (theBetID > UpamountLine) ? baseBet.upAmount : baseBet.downAmount;
                var betNode = roomData.lowBtnNodes[i];
                betNode.setBetText(roomData.showGameBetInfos[theBetID].bet * amount);
                if (roomData.lastBetID == betNodeID)
                {
                    betNode.addLastObj(lastChoiseObj);
                }
                else
                {
                    betNode.removeLastObj();
                }
            }
        }

        private void registButtonEvent()
        {
            btn_gohighroller.onClick.AddListener(changeRoomToHighRoller);
            btn_goregular.onClick.AddListener(regularClick);
            btn_closePage.onClick.AddListener(closeBtnClick);
        }

        void regularClick()
        {
            isStatusRegular = true;
            topBtnEvent();
        }

        void changeRoomToHighRoller()
        {
            if (!isOpenHighRoller)
            {
                openVipTip();
                return;
            }
            isStatusRegular = false;
            topBtnEvent();
        }
        IDisposable closeVipTipDis;
        bool isOpenVipTips;
        void openVipTip()
        {
            if (isOpenVipTips)
            {
                closeVipTip();
                return;
            }
            isOpenVipTips = true;
            vipTipAnim.gameObject.setActiveWhenChange(true);
            vipTipAnim.SetTrigger("open");
            closeVipTipDis = Observable.Timer(TimeSpan.FromSeconds(5.0f)).Subscribe(_ =>
             {
                 closeVipTip();
             }).AddTo(uiGameObject);
        }

        void closeVipTip()
        {
            UtilServices.disposeSubscribes(closeVipTipDis);
            vipTipAnim.SetTrigger("close");
            isOpenVipTips = false;
        }

        public override void animOut()
        {
            isReadyClear = true;
            for (int i = 0; i < tweenIDs.Count; ++i)
            {
                TweenManager.tweenKill(tweenIDs[i]);
            }

            clear();
        }

        private void topBtnEvent()
        {
            updateShowBetBtnValue();
            setNotactive();
            initRunJP();
        }

        private void SelectBetClassBtnEvent(int clickValue)
        {
            if (isStatusRegular)
            {
                SetChooseClass(regularRoomData.showBetPoint[clickValue]);
            }
            else
            {
                SetChooseClass(highRoomData.showBetPoint[clickValue]);
            }

            if (null != changeToGame)
            {
                changeToGame(nowFocusGame.gameID);
            }
        }

        private void SetChooseClass(int _class)
        {
            DataStore.getInstance.dataInfo.setChooseBetClass(GetRoomTypeKey(), _class);
        }

        private string GetRoomTypeKey()
        {
            if (isStatusRegular)
            {
                return ChooseBetClass.Regular;
            }
            return ChooseBetClass.High_Roller;
        }

        void updateJPText(long jpUpdateVal)
        {
            text_JP.text = jpUpdateVal.ToString("N0");
        }
    }

    class BetBtnNode : NodePresenter
    {
        CustomBtn betBtn;
        Text betTxt;
        GameObject tapLight;
        int index;
        VerticalLayoutGroup layoutGroup;

        public Subject<int> clickSub = new Subject<int>();
        public override void initUIs()
        {
            betBtn = getCustomBtnData("btn_bet");
            betTxt = getTextData("text_bet");
            tapLight = getGameObjectData("bet_btnlight");
            layoutGroup = getBindingData<VerticalLayoutGroup>("info_layout");
        }
        public override void init()
        {
            betBtn.clickHandler = betBtnClick;
            betBtn.pointerDownHandler = () =>
            {
                tapLight.transform.localScale = betBtn.transform.localScale;
                tapLight.setActiveWhenChange(true);
            };
            betBtn.pointerUPHandler = () =>
            {
                tapLight.transform.localScale = betBtn.transform.localScale;
                tapLight.setActiveWhenChange(false);
            };
        }
        public void setID(int id)
        {
            index = id;
        }

        public void setBetText(long betNum)
        {
            betTxt.text = betNum.ToString("N0");
            open();
        }

        public void addLastObj(GameObject lastBetObj)
        {
            lastBetObj.transform.SetParent(layoutGroup.transform);
            lastBetObj.transform.SetAsFirstSibling();
            layoutGroup.padding.top = 0;
            lastBetObj.setActiveWhenChange(true);
        }

        public void removeLastObj()
        {
            layoutGroup.padding.top = 13;
        }

        void betBtnClick()
        {
            clickSub.OnNext(index);
        }
    }

    class RoomData
    {
        public int[] showBetPoint;
        public List<GameBetInfo> showGameBetInfos;
        public BetBtnNode[] lowBtnNodes = new BetBtnNode[2];
        public int lastBetID { get; private set; } = -1;

        public void isLowBtnsActive(bool isActive)
        {
            for (int i = 0; i < lowBtnNodes.Length; ++i)
            {
                lowBtnNodes[i].setVisible(isActive);
            }
        }

        public void resetLastBet()
        {
            lastBetID = -1;
        }

        public void setLastBetPos(int lastBetID, int lastPosition)
        {
            this.lastBetID = lastBetID;
            showBetPoint[lastBetID] = lastPosition;
        }
    }
}
