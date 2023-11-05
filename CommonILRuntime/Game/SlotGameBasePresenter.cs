using System.Collections.Generic;
using System.Collections;
using System;
using UnityEngine;
using UnityEngine.UI;
using CommonILRuntime.BindingModule;
using System.Threading;
using CommonPresenter;
using CommonILRuntime.Module;
using CommonILRuntime.Game.Slot;
using Game.Common;
using Game.Jackpot.Billboard;
using Debug = UnityLogUtility.Debug;
using static Game.Slot.SlotDefine;
using CommonService;

namespace Game.Slot
{
    public class SlotGameBasePresenter : ContainerPresenter
    {
        public static Action audioPerformWinLine;
        public static Action audioBonusAdd;
        public override string objPath { get { return "prefab/slot/maingame"; } }
        public override UiLayer uiLayer { get { return UiLayer.Root; } }
        protected virtual float logoTopBorder { get { return 70.0f; } }
        protected virtual float logoBottomBorder { get { return 1200.0f; } }

        public SlotGameBase slotGame { get; set; }

        #region 共用介面
        public RectTransform EffectLayoutRect;
        protected GameObject JpUILayout;
        protected GameObject freegameBackground;
        protected GameObject bonusgameBackground;
        protected Slider BonusSlider;
        protected Button tapButton;
        protected Button infoBtn;
        protected Image CountImg;
        protected Animator effectCoinsMax;
        #endregion

        #region 管理器
        public GameTopBarPresenter topBarPresenter;
        public GameBottomBarPresenter bottomBarPresenter { get; set; }
        public JackpotBillboard jpBillboard;
        public SlotWinLine slotWinLine;
        #endregion

        #region Data
        public GameConfig gameConfig;
        public AutoMode stopMode { get; set; } = AutoMode.INFINITY_AUTO;
        public ISlotConfigProvider SlotConfig;
        public ISlotGameTable currentTable;
        public List<Vector3> linePos = new List<Vector3>();
        public int BottomBarBetId { get { return bottomBarPresenter.betIdx; } }
        #endregion
        public CancellationTokenSource resetTotalWinCts = null;
        private Action notifyDashWinPointsComplete = null;

        public override void initUIs()
        {
            freegameBackground = getGameObjectData("freegame_bg");
            bonusgameBackground = getGameObjectData("bonusgame_bg");

            EffectLayoutRect = getBindingData<RectTransform>("effect_layout");
            CountImg = getImageData("count_img");
            JpUILayout = getGameObjectData("jp_ui_layout");
            tapButton = getBtnData("spin_btn");
            BonusSlider = getBindingData<Slider>("bonus_slider");
            infoBtn = getBtnData("info_btn");
            effectCoinsMax = getAnimatorData("map_scroll_effect");
            initJpBillboard();
        }
        public override void init()
        {
            uiRectTransform.SetAsFirstSibling();

            topBarPresenter = GameBar.GameBarServices.instance.getGameTopBar();
            topBarPresenter.backToLobby = backToLobby;

            bottomBarPresenter = GameBar.GameBarServices.instance.getGameBottomBar();
            bottomBarPresenter.clearRegisteredActions();
            bottomBarPresenter.registerPlayButtonEnableChange(syncTapButtonEnable);
            bottomBarPresenter.spinOnClick = onSpinClick;
            bottomBarPresenter.stopOnClick = onStopClick;
            bottomBarPresenter.registerDashWinPointComplete(onDashWinPointsComplete);
            bottomBarPresenter.setTotalBetCall(setTotalBetCall, maxBetPercentChangeHandler);
            bottomBarPresenter.setAutoItemClick(autoItemClickHandler);
            slotGame.betToLockManager.init(bottomBarPresenter);
            tapButton.onClick.AddListener(onTapButtonClick);
            initTopLogo();
        }

        public override void open()
        {
            base.open();
            ApplicationConfig.initFinish.OnNext(true);
        }

        public override void close()
        {
            base.close();
            audioPerformWinLine = null;
            audioBonusAdd = null;
        }

        public override void clear()
        {
            topBarPresenter.clear();
            bottomBarPresenter.clear();
            base.clear();
        }


        public virtual void showTable(ulong[][] reelStripData)
        {
            slotWinLine.breakIterateWinLine();
            currentTable.showTable(reelStripData);
        }

        public virtual void changeBackgroundByState(GameConfig.GameState state)
        {
            freegameBackground.setActiveWhenChange(GameConfig.GameState.FG == state);
            bonusgameBackground.setActiveWhenChange(GameConfig.GameState.BG == state);
        }


        public virtual void setTotalBetCall(ulong totalBet)
        {
            slotGame.totalBet = totalBet;
            if (null == jpBillboard)
            {
                Debug.LogError("jpBillboard == null");
                return;
            }
            jpBillboard.setTotalBet(totalBet);
        }

        protected virtual void initJpBillboard()
        {
        }
        protected virtual void initTopLogo()
        {
            if (UiRoot.instance.getNowScreenOrientationUIRoot() != UiRoot.instance.propUIRoot)
            {
                return;
            }
            var topLogo = getNodeData("top_logo");
            if (null == topLogo)
            {
                return;
            }
            var topLogoPresenter = UiManager.bindNode<TopLogoPresenter>(topLogo.cachedGameObject).setBorder(uiRectTransform.rect.height, logoTopBorder, logoBottomBorder);
            topLogoPresenter.showLogo();
        }
        public void setTotalWin(ulong value)
        {
            stopDelayResetTotalWin();
            bottomBarPresenter.setTotalWinNum(value);
        }

        public void delayResetWinPoints()
        {
            if (null != resetTotalWinCts)
            {
                resetTotalWinCts.Cancel();
            }
            resetTotalWinCts = new CancellationTokenSource();
            CoroutineManager.StartCoroutine(resetWinPoints(resetTotalWinCts.Token));
        }

        IEnumerator resetWinPoints(CancellationToken token)
        {
            yield return gameConfig.RESET_WIN_POINT_TIME;
            if (!token.IsCancellationRequested)
            {
                setTotalWin(0);
            }
        }

        void stopDelayResetTotalWin()
        {
            if (null != resetTotalWinCts)
            {
                resetTotalWinCts.Cancel();
                resetTotalWinCts.Dispose();
                resetTotalWinCts = null;
            }
        }

        protected virtual void beforeSpin()
        {
            slotWinLine.breakIterateWinLine();
            bottomBarPresenter.breakDashWinPoint();
        }

        protected virtual void beforeChangeTable()
        {
            currentTable.unRegisterTableRollEnd(onTableRollEnd);
            currentTable.unRegisterTableSpinEnd(onTableSpinEnd);
            currentTable.closeTable();
            slotWinLine.breakIterateWinLine();
        }

        protected virtual void afterChangeTable()
        {
            currentTable.registerTableRollEnd(onTableRollEnd);
            currentTable.registerTableSpinEnd(onTableSpinEnd);
        }

        protected virtual void changeTable(ISlotGameTable table)
        {
            beforeChangeTable();
            currentTable = table;
            afterChangeTable();
        }

        public virtual void setAsNormalTable() { }
        public virtual void setAsFreeTable() { }
        public virtual void setAsSuperFreeTable() { }
        public virtual void setAsBonusTable() { }
        public virtual void setAsMiniTable() { }

        public virtual void preSpinTable()
        {
            beforeSpin();
            currentTable.preSpin();
        }

        public virtual void spinTable()
        {
            currentTable.spin();
        }

        public virtual void stopTable()
        {
            currentTable.stop();
        }

        protected virtual void onTableSpinEnd()
        {
            slotGame.OnTableSpinEnd?.Invoke();
        }

        private void onTableRollEnd()
        {
            slotGame.OnTableRollEnd?.Invoke();
        }

        public virtual void allScatterOpen()
        {
            currentTable.applyForAllShowingItem((item) =>
           {
               if (item.getSymbolData().IsScatterFree)
               {
                   item.addOrSetAnimatedSymbol(gameConfig.SCATTER_OPEN);
               }
           });
        }

        public virtual void allChipShine()
        {
            currentTable.applyForAllShowingItem((item) =>
            {
                if (item.getSymbolData().IsChip)
                {
                    item.addOrSetAnimatedSymbol(gameConfig.COIN_SHINE);
                }
            });
        }

        private void onTapButtonClick()
        {
            if (slotGame.IsAutoPlay || Services.GuideStatus.Completed != DataStore.getInstance.guideServices.nowStatus) return;
            bottomBarPresenter.clickPlayButton();
        }

        private void syncTapButtonEnable(bool isEnable)
        {
            tapButton.interactable = isEnable;
        }

        public virtual void setPlayBtnEnable(bool enable)
        {
            bottomBarPresenter.setPlayBtnEnable(enable);
        }

        public virtual void showMapInfoBtn(bool isEnable)
        {
            if (null == infoBtn) return;
            infoBtn.interactable = isEnable;
        }

        public virtual void onSpinClick() { }

        public virtual void onStopClick() { }

        public virtual void backToLobby()
        {
            UiManager.clearAllPresenter();
        }

        public virtual void autoItemClickHandler(int autoCount)
        {
            slotGame.OnAutoItemClick?.Invoke(autoCount);
        }

        public void dashWinPoints(ulong value)
        {
            bottomBarPresenter.dashWinPoints(value, slotGame.TotalPay);
        }

        public virtual IEnumerator performWinLineEffect(Action callback = null, GameConfig.GameState gameState = GameConfig.GameState.NG)
        {
            var data = slotGame.getWinInfo();
            if (null != data)
            {
                if (data.Total_Win > 0)//先做跑分，因為如果出Scatter達一定數量會有贏分，但不一定有連線
                {
                    dashWinPoints(data.getTotalWin);
                }
                if (null != data.Win_Condition)
                {
                    audioPerformWinLine?.Invoke();
                    SetCurrentTable(slotWinLine.setSlotWinConditon(data.Win_Condition));
                    if (!slotGame.IsInFG && slotGame.IsAutoPlay)
                    {
                        yield return CoroutineManager.StartCoroutine(performAllWinLineSingleTime());
                    }
                    else
                    {
                        slotWinLine.showInterruptableWinLineEffect();
                    }
                }
            }
            else
            {
                yield return gameConfig.NO_WIN_NEXT_SPIN_TIME;
            }
        }

        protected IEnumerator performAllWinLineSingleTime(Func<IEnumerator> beforeCloseAction = null)
        {
            var wrapper = new BooleanWrapper(() => !slotWinLine.IsShowingAllLine);

            slotWinLine.showAllWinLine(beforeCloseAction, waiteAutoModeWinLineBuffer);
            yield return wrapper;
        }

        protected IEnumerator waiteAutoModeWinLineBuffer()
        {
            var wrapper = new BooleanWrapper(() => !slotGame.IsShowingWinWindow);
            yield return wrapper;
            yield return gameConfig.ALL_LINE_PERFORM_TIME;
        }

        public void SetCurrentTable(List<int> addScollIdx)
        {
            int dataCount = addScollIdx.Count;
            int data = 0;
            int column = 0;
            int row = 0;
            int rowCount = gameConfig.NORMAL_TABLE_ROW_COUNT;

            for (int i = 0; i < dataCount; ++i)
            {
                data = addScollIdx[i];
                column = data / rowCount;
                row = data % rowCount;
                currentTable.addScrollAnimatedSymbol(column, row, "");
            }
        }

        public virtual void setSideText(int count, bool withSound = true)
        {

        }
        public virtual void setFGCountTxt(int gameIdx, int fgMax, int mode = 0)
        {
            bottomBarPresenter.setFreeCount(gameIdx, fgMax, mode);
        }


        public virtual void setReelResult()
        {
            //設定結束盤面，換盤面內容
            //For Normal, FreeGame
        }

        public virtual List<Vector3> getLinePos()
        {
            return linePos;
        }

        public virtual void fetchWinLineEffectPos()
        {
            slotWinLine.fetchWinLineEffectPos(getLinePos());
        }

        public virtual void maxBetPercentChangeHandler(float percent)
        {
            slotGame.betToLockManager.checkUnlockOrLock(percent);
        }

        public ulong[][] trimTableStrip(ulong[][] sourceStrip)
        {
            int validLen = gameConfig.NORMAL_TABLE_ROW_COUNT * gameConfig.NORMAL_TABLE_COLUMN_COUNT;

            if (sourceStrip.Length == validLen || sourceStrip.Length < validLen)
            {
                return sourceStrip;
            }

            ulong[][] rtnStrip = new ulong[validLen][];
            int sourceRowCount = sourceStrip.Length / gameConfig.NORMAL_TABLE_COLUMN_COUNT;
            int topRowIdx = 0;
            int bottomRowIdx = topRowIdx + (sourceRowCount - 1);
            int sourceIdx = 0;
            for (int i = 0; i < sourceStrip.Length; i++)
            {
                int row = i % sourceRowCount;
                int column = i / sourceRowCount;
                if (topRowIdx == row || bottomRowIdx == row)
                {
                    continue;
                }
                rtnStrip[sourceIdx] = sourceStrip[i];
                sourceIdx++;
                if (sourceIdx >= validLen)
                {
                    break;
                }
            }

            return rtnStrip;
        }

        public virtual IEnumerator CheckCloseMap()
        {
            if (null != infoBtn)
                infoBtn.interactable = false;
            bottomBarPresenter.setBetBtnEnable(false);
            yield return 0;
        }

        public virtual void setMapSlider(int num, int max, bool isInit = false)
        {
            if (null == BonusSlider || null == effectCoinsMax) return;

            updateMapSliderProgress(num, max);
            clearSliderWhenProgressEmpty(num, max);
            fillUpSliderWhenProgressMax(num, max);
            reflashEffectCoinsWhenNumClear(num);
        }

        private void updateMapSliderProgress(int num, int max)
        {
            double progress = (double)num / (double)max;
            BonusSlider.value = (int)BonusSlider.maxValue - (int)(progress * BonusSlider.maxValue);
        }

        private void clearSliderWhenProgressEmpty(int num, int max)
        {
            if (BonusSlider.value == 0 && num < max)
            {
                BonusSlider.value = 1;
            }
        }

        private void fillUpSliderWhenProgressMax(int num, int max)
        {
            if (num >= max)
            {
                BonusSlider.value = 0;
            }
        }

        private void reflashEffectCoinsWhenNumClear(int num)
        {
            if (num == 0)
            {
                effectCoinsMax.gameObject.setActiveWhenChange(false);
                effectCoinsMax.gameObject.setActiveWhenChange(true);
            }
        }

        public virtual void clearEffectObjs()
        {
            slotWinLine.breakIterateWinLine();
        }

        public void changeLineAniToLoop()
        {
            slotWinLine.changeToLoopAni();
        }

        private void onDashWinPointsComplete()
        {
            notifyDashWinPointsComplete?.Invoke();
        }

        public void registerDashWinPointsComplete(Action notifyDashWinPointsComplete)
        {
            this.notifyDashWinPointsComplete = notifyDashWinPointsComplete;
        }

        public void clearNotifyDashWinPointsComplete()
        {
            this.notifyDashWinPointsComplete = null;
        }
    }
}
