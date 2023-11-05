using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UniRx;
using CommonILRuntime.Game.GameTime;
using Debug = UnityLogUtility.Debug;
using DG.Tweening;
namespace Game.Slot
{
    using CommonILRuntime.Game.Slot;
    using CommonILRuntime.Module;
    using CommonService;
    using Game.Slot.Interface;
    using LobbyLogic.Audio;
    using static SlotDefine;

    public abstract class SlotScrollPresenter : NoBindingNodePresenter, ISlotScroll
    {
        //輪帶基本資訊
        int scrollIdx;
        int currentDefaultReelIdx;
        protected int[] defaultReels;
        protected ulong[][] resultReels;
        protected List<ISlotItem> slotItems;
        List<int> showItemsIdx;

        //輪帶位移計算
        protected int onTheTopItemIdx = 0;    //輪帶停止時，位於最上方的Item，在 scroll 中的 idx
        int moveSymbolCount = 0;
        protected int onTheBottomItemIdx = 0;  //輪帶停止時，需位於最下方的Item，在 scrollItems 中的 idx ( 第一個開始被替換成結果的 Item )
        int resultReelStartIdx = 0;    //替換結果時使用的索引
        int sinkPosIdx;                 //下沉所在的posYList的ID
        public int itemMaxIdx { get { return slotItems.Count - 1; } }

        protected float remainDistance = 0f;
        float moveSpeed = 0f;
        float constantVelocityDuration = 0;
        float constantSpeedMultiple = 1f;
        float moveRange;    //可移動距離長，每個TimeDelta位移對moveRange做wrap
        float celingY;      //可移動最高點
        protected float floorY;       //可移動最低點
        float reboundDelayTime = 0;     //回彈前的等待時間
        protected float[] posYList;
        protected IDisposable everyUpdateDisposable;
        ScrollState currentMoveState = ScrollState.STOP;

        /// <summary> 輪帶上方的額外Symbol數量 </summary>
        protected virtual int topExtraSymbolCount { get { return TOP_EXTRA_SYMBOL_COUNT; } }
        /// <summary> 輪帶下方的額外Symbol數量 </summary>
        protected virtual int bottomExtraSymbolCount { get { return BOTTOM_EXTRA_SYMBOL_COUNT; } }
        /// <summary> 是否要假輪帶的更新順序的判斷 </summary>
        protected virtual bool checkDefaultReelOrder { get { return false; } }
        int startUpdateItem = 0;        //假輪帶起始更新itemId
        protected int showCount { get; private set; }
        float previousV = 0;

        bool inited = false;
        bool interrupted = false;

        /// <summary> 轉動時，SlotItem更新Symbol圖樣後要做的事 </summary>
        protected Action<ISlotItem> onRollingMoveNextItem = null;

        bool isPreSlotScroll = false;//是否收到spin data，收到才能將盤面停下開獎
        public ISlotConfigProvider config { get; set; }

        public Action<SlotScrollPresenter, ScrollState> stateChangeCall = null; //TODO: wait to refactor

        Action<ISlotItem> onItemChangeToResult = null;
        Action onUpdateAction = null;

        Stack<int> startFakeDefaultReels = new Stack<int>();
        Stack<int> endFakeDefaultReels = new Stack<int>();
        public void registerItemChangeToResult(Action<ISlotItem> handler)
        {
            onItemChangeToResult += handler;
        }

        public void unRegisterItemChangeToResult(Action<ISlotItem> handler)
        {
            onItemChangeToResult -= handler;
        }

        /// <summary>
        /// 旋轉停輪後通知的事件
        /// </summary>
        Action<ISlotScroll> onScrollEnd = null;
        public void registerScrollEnd(Action<ISlotScroll> handler)
        {
            onScrollEnd += handler;
        }

        public void unRegisterScrollEnd(Action<ISlotScroll> handler)
        {
            onScrollEnd -= handler;
        }

        void clearScrollEndHandlers()
        {
            onScrollEnd = null;
        }

        /// <summary>
        /// 沒有被中斷停輪(manualStop) 會發出的事件通知
        /// </summary>
        Action<ISlotScroll> onScrollComplete = null;
        public void registerScrollComplete(Action<ISlotScroll> handler)
        {
            onScrollComplete += handler;
        }

        public void unRegisterScrollComplete(Action<ISlotScroll> handler)
        {
            onScrollComplete -= handler;
        }

        protected float itemHeight { get; set; }
        protected virtual float getBaseConstantSpeed()
        {
            return itemHeight * config.MOVE_ITEM_PER_SEC;
        }

        protected virtual float getSinkHeight()
        {
            return itemHeight * config.SINK_ITEM_PERCENT;
        }

        protected virtual float getSinkDuration()
        {
            return config.DELAY_TO_SINK_DURATION;
        }

        public virtual bool show
        {
            get
            {
                return uiGameObject.activeSelf;
            }

            set
            {
                uiGameObject.setActiveWhenChange(value);
            }
        }

        public int getScrollIdx()
        {
            return scrollIdx;
        }

        public virtual void initScroll(int showCount, int[] defaultReels, int scrollIdx, GameObject itemPrefab, Func<GameObject, ISlotItem> bindingDelegate,
            int initShowReelIdx = 0)
        {
            if (inited)
            {
                Debug.LogWarning("ScrollPresenter is inited but call initScroll again, return and do nothing.");
                return;
            }
            inited = true;
            this.showCount = showCount;
            this.defaultReels = defaultReels;
            this.scrollIdx = scrollIdx;
            slotItems = new List<ISlotItem>();
            showItemsIdx = new List<int>();
            currentDefaultReelIdx = wrapReelIdx(initShowReelIdx - topExtraSymbolCount);

            int totalSymbolCount = showCount + topExtraSymbolCount + bottomExtraSymbolCount;
            posYList = new float[totalSymbolCount];

            //string objPath = isBigScroll ? BIG_SYMBOL_OBJ : SMALL_SYMBOL_OBJ;
            //GameObject itemPrefab = ResourceManager.instance.getGameObject(objPath);
            RectTransform prefabRectTrans = itemPrefab.transform as RectTransform;
            itemHeight = prefabRectTrans.sizeDelta.y;
            float lastItemPosY = -itemHeight;
            int middleIdx = (showCount / 2) + topExtraSymbolCount;
            float middlePosIdx = showCount % 2 == 1 ? middleIdx : middleIdx - 0.5f;
            for (int itemIdx = 0; itemIdx < totalSymbolCount; ++itemIdx)
            {
                GameObject itemObj = UnityEngine.Object.Instantiate(itemPrefab, uiRectTransform, false);
                //SlotItemPresenter itemPresenter = bind<SlotItemPresenter>(itemObj);
                var itemPresenter = bindingDelegate(itemObj);
                float itemPosY = (middlePosIdx - itemIdx) * itemHeight;
                itemPresenter.setPosY(itemPosY);
                int reelIdx = wrapReelIdx(currentDefaultReelIdx + itemIdx);

                var reelData = new ReelStrip()
                {
                    infoID = defaultReels[reelIdx],    //default 輪帶
                    customValue = 0,
                };

                itemPresenter.setSymbolData(reelData);
                slotItems.Add(itemPresenter);
                posYList[itemIdx] = itemPosY;
                lastItemPosY = itemPosY;
            }
            cacheShowItems();

            onTheTopItemIdx = 0;
            onTheBottomItemIdx = itemMaxIdx;
            sinkPosIdx = itemMaxIdx;
            celingY = posYList[0];
            floorY = lastItemPosY - itemHeight; //最低點
            moveRange = celingY - floorY;
            handleItemFormHeight();
        }

        void printInputResulReels(long[][] inputRollResult)
        {
            Debug.Log($"printInputResulReels, scrollIdx: {scrollIdx}");
            for (int i = 0; i < inputRollResult.Length; i++)
            {
                Debug.Log($"inputRollResult: {inputRollResult[i][0]}");
            }
        }

        /// <summary>
        /// 開始轉動前給予對比資料 將會比對假輪帶資料進行比對 如有相對應資料 便會以對應的資料開始轉動 如沒有便照常運轉
        /// </summary>
        /// <param name="comparisonData">比對的資料(由上往下的順序)</param>
        public void checkStartRollToSameDefaultReels(List<int> comparisonData)
        {
            for (int i = defaultReels.Length - 1; i >= 0; --i)
            {
                for (int j = comparisonData.Count - 1; j >= 0; --j)
                {
                    if (defaultReels[wrapReelIdx(i - (comparisonData.Count - j - 1))] != comparisonData[j])
                    {
                        break;
                    }
                    else
                    {
                        if (j == 0)
                        {
                            currentDefaultReelIdx = wrapReelIdx(i - (comparisonData.Count - 1));
                            return;
                        }
                    }
                }
            }
        }
        /// <summary>
        /// 輪盤開始轉動時 前幾個塞入的假轉資料
        /// </summary>
        /// <param name="reelData">假轉資料(由上往下的順序)</param>
        public void setStartFakeDefaultReels(List<int> reelData)
        {
            startFakeDefaultReels.Clear();
            startFakeDefaultReels = new Stack<int>(reelData);
        }

        /// <summary>
        /// 輪盤將要結束轉動時 最後塞入的假轉資料
        /// </summary>
        /// <param name="reelData">假轉資料(由上往下的順序) 如塞入-1會套用假輪帶最後顯示的值</param>
        public void setEndFakeDefaultReels(List<int> reelData)
        {
            endFakeDefaultReels.Clear();
            endFakeDefaultReels = new Stack<int>(reelData);
        }

        /// <summary>
        /// FG、BG 玩家沒介入的情形下，都會按照輪帶的參數停輪，除非玩家手動點，才會快停，所以輪帶預設INFINITY_AUTO)
        /// </summary>
        /// <param name="inputRollResult"></param>
        /// <param name="stopMode"></param>
        /// <param name="isPreScroll"></param>
        public void roll(ulong[][] inputRollResult, AutoMode stopMode = AutoMode.INFINITY_AUTO, bool isPreScroll = true)
        {
            prepareRollResult(inputRollResult);
            //printInputResulReels(inputRollResult);
            moveSymbolCount = 0;
            isPreSlotScroll = isPreScroll;

            changeState(ScrollState.SPEED_UP);

            switch (stopMode)
            {
                case AutoMode.INFINITY_AND_FAST_STOP:
                    {
                        //快停模式會同時停輪，故減去起始延遲時間達到規格需求
                        constantVelocityDuration = (config.CONST_VELOCITY_DURATION) - (config.START_ROLL_DELAY_TIME * scrollIdx);
                    }
                    break;
                default:
                    {
                        constantVelocityDuration = config.CONST_VELOCITY_DURATION + (config.EXTRA_CONSTANT_DURATION * scrollIdx);
                    }
                    break;

            }
            changeState(ScrollState.SPEED_UP);
            interrupted = false;    //重設是否被中斷
            string tweenID = TweenManager.tweenToFloat(0f, getBaseConstantSpeed(), config.SPEED_UP_TIME,
                delayTime: scrollIdx * config.START_ROLL_DELAY_TIME,
                onUpdate: itemSpeedUp,
                onComplete: delegate ()
                {
                    changeToConstantRoll();
                });

        }

        public void changeDefaultReels(int[] defaultReels)
        {
            this.defaultReels = defaultReels;
        }

        public void changeSlotConfig(ISlotConfigProvider config)
        {
            this.config = config;
        }

        void middleAlignmentAssign(ref ulong[][] target, ulong[][] source)
        {
            int targetMiddleIdx = target.Length / 2;
            int sourceMiddleIdx = source.Length / 2;

            int targetIdx = source.Length >= target.Length ? 0 : targetMiddleIdx - sourceMiddleIdx;
            int sourceIdx = source.Length <= target.Length ? 0 : sourceMiddleIdx - targetMiddleIdx;

            //置中對齊
            for (; targetIdx < target.Length; targetIdx++)
            {
                if (sourceIdx >= source.Length)
                {
                    break;
                }
                target[targetIdx] = source[sourceIdx];
                sourceIdx++;
            }
        }

        public void prepareRollResult(ulong[][] inputResultReels)
        {
            if (inputResultReels == null || inputResultReels.Length == 0) return;
            resultReels = new ulong[slotItems.Count][];
            //先放上預設輪帶
            for (int i = 0; i < resultReels.Length; i++)
            {
                int defaultIdx = wrapReelIdx(currentDefaultReelIdx - 1);  //對齊基礎輪帶位置
                resultReels[i] = new ulong[1] { (ulong)defaultReels[defaultIdx] };
            }

            middleAlignmentAssign(ref resultReels, inputResultReels);
        }

        protected void startUpdate()
        {
            if (null != everyUpdateDisposable)
            {
                return;
            }

            everyUpdateDisposable = Observable.EveryUpdate().Subscribe(onUpdate).AddTo(uiGameObject);
        }

        private void onUpdate(long frame)
        {
            onUpdateAction?.Invoke();
        }

        private void changeToConstantRoll()
        {
            changeState(ScrollState.CONSTANT_SPEED,constantRoll);
            startUpdate();
        }

        void constantRoll()
        {
            if (DataStore.getInstance.gameTimeManager.IsPaused()) return;

            if (isPreSlotScroll)
            {
                constantVelocityDuration -= Time.deltaTime;
            }
            moveSpeed = getBaseConstantSpeed() * constantSpeedMultiple;    //等速過程中可因特殊狀況加速
            var moveDistance = moveSpeed * Time.deltaTime;

            bool readyToResult = false;
            startUpdateItem = onTheTopItemIdx;
            for (int i = 0; i < slotItems.Count; ++i)
            {
                var itemIdx = checkDefaultReelStartItemIdx(i);
                var item = slotItems[itemIdx];
                bool changeSymbol = cycleMoveItem(itemIdx, moveDistance);
                if (changeSymbol)
                {
                    moveSymbolCount++;
                    if (constantVelocityDuration <= 0 && isPreSlotScroll)
                    {
                        //先把要轉去最下方的Item做替換
                        resultReelStartIdx = resultReels.Length - 1;
                        changeToNextResultReel(item);
                        if (!readyToResult && endFakeDefaultReels.Count == 0)
                        {
                            readyToResult = true;
                        }
                    }
                    else
                    {
                        setItemToNextDefaultReel(item);
                    }
                    handleItemFormHeightToItem(item);
                }
            }

            if (readyToResult && isPreSlotScroll)
            {
                //開始替換轉輪
                startConstantRollToResult();
            }
        }

        public void ContinuePreScroll()
        {
            isPreSlotScroll = true;
        }

        private void startConstantRollToResult()
        {
            isPreSlotScroll = true;
            var startPosY = slotItems[onTheTopItemIdx].posY;
            var targetPosY = posYList[sinkPosIdx];
            remainDistance = startPosY - targetPosY;    //由上往下移動，startPosY 必然大於targetPosY, 若 startPosY < targetPosY 則剩餘距離小於0
            changeState(ScrollState.CONSTANT_SPEED_TO_RESULT, constantRollToResult);
        }

        void constantRollToResult()
        {
            if (remainDistance > 0)
            {
                if (DataStore.getInstance.gameTimeManager.IsPaused()) return;

                var moveDistance = moveSpeed * Time.deltaTime;
                moveDistance = Math.Min(moveDistance, remainDistance);
                startUpdateItem = onTheTopItemIdx;
                for (int i = 0; i < slotItems.Count; ++i)
                {
                    var itemIdx = checkDefaultReelStartItemIdx(i);
                    var item = slotItems[itemIdx];
                    bool changeSymbol = cycleMoveItem(itemIdx, moveDistance);
                    if (changeSymbol)
                    {
                        changeToNextResultReel(item);
                        handleItemFormHeightToItem(item);
                    }
                }

                if (interrupted)
                {
                    checkInterruptedInConstantRollToResult();
                }
                remainDistance -= moveDistance;

                if (remainDistance <= 0f)
                {
                    startSink();
                }
            }
            else
            {
                startSink();
            }
        }

        protected void refineItemPosToResult()
        {
            int itemIdx = onTheTopItemIdx;
            for (int i = 0; i < slotItems.Count; ++i)
            {
                itemIdx = wrapItemIdx(itemIdx);
                slotItems[itemIdx].setPosY(posYList[i]);
                itemIdx++;
            }
        }

        protected void refineReelToResult()
        {
            int itemIdx = onTheBottomItemIdx;
            for (int i = itemMaxIdx; i >= 0; --i)
            {
                itemIdx = wrapItemIdx(itemIdx);
                var reelIdx = wrapItemIdx(i + (itemMaxIdx - sinkPosIdx));
                slotItems[itemIdx].setSymbolData(new ReelStrip(resultReels[reelIdx]));
                itemIdx--;
            }
            handleItemFormHeight();
        }

        private void startSink()
        {
            refineItemPosToResult();   //校正到正確位置後再開始下沉 (constantRollToResult可能會有0.001的誤差) 
            refineReelToResult();
            //計算下沉會用到的參數
            remainDistance = getSinkHeight();
            changeState(ScrollState.SINK, sink);
        }

        void sink()
        {
            if (remainDistance > 0)
            {
                if (DataStore.getInstance.gameTimeManager.IsPaused()) return;
                var moveDistance = moveSpeed * Time.deltaTime * config.SINK_DURATION;
                moveDistance = Math.Min(moveDistance, remainDistance);
                for (int itemIdx = 0; itemIdx < slotItems.Count; ++itemIdx)
                {
                    var item = slotItems[itemIdx];
                    cycleMoveItem(itemIdx, moveDistance);   //下沉不做輪帶循環
                }
                remainDistance -= moveDistance;
                if (remainDistance <= 0)
                {
                    rebound();
                }
            }
            else
            {
                rebound();
            }
        }

        void rebound()
        {
            disposeEveyUpdate();
            var startPos = itemMaxIdx - sinkPosIdx; //slotItems[onTheTopItemIdx].posY;          //最後一格 Item 目前的位置
            var targetPos = config.SINK_ITEM_PERCENT; //posYList[itemLength - 1 - sinkPosIdx];         //最後一格 Item停輪目標位置
            remainDistance = (targetPos - startPos) * itemHeight;

            changeState(ScrollState.REBOUND);
            if (reboundCondition(remainDistance))
            {
                string tweenID = TweenManager.tweenToFloat(remainDistance, 0, config.REBOUND_DURATION,
                     delayTime: reboundDelayTime,
                     onUpdate: itemRebound,
                     onComplete: delegate ()
                     {
                         end();
                     });
            }
            else
            {
                Debug.LogWarning("Scroll Motion Err !");
                end();
            }
        }

        /// <summary>
        /// 執行下沉及回彈效果時的條件
        /// </summary>
        /// <param name="remainDistance"></param>
        /// <returns></returns>
        public virtual bool reboundCondition(float remainDistance)
        {
            return remainDistance > 0;
        }

        /// <summary>
        /// 判斷在ConstantRollToResult時進行即停時進行的動作
        /// </summary>
        public virtual void checkInterruptedInConstantRollToResult()
        {

        }

        /// <summary>
        /// 執行下沉時的目標座標
        /// </summary>
        /// <param name="targetPos"></param>
        /// <returns></returns>
        public void setSinkPosIdx(int id = -1)
        {
            sinkPosIdx = id >= 0 ? id : itemMaxIdx;
        }
        /// <summary>
        /// 設定回彈時的等待時間
        /// </summary>
        /// <param name="delayTime"></param>
        public void setReboundDelayTime(float delayTime)
        {
            reboundDelayTime = delayTime;
        }

        protected void itemRebound(float currentDistance)
        {
            if (currentDistance == 0)
            {
                return;
            }

            int itemIdx = onTheTopItemIdx;

            for (int i = 0; i < slotItems.Count; ++i)
            {
                itemIdx = wrapItemIdx(itemIdx);
                var newY = posYList[currentDistance > 0 ? i : wrapItemIdx(i + 1)] - currentDistance;

                if (newY <= floorY)
                {
                    onTheBottomItemIdx = wrapItemIdx(itemIdx - 1);    //輪帶停止時，需位於最下方的Item，在 scrollItems 中的 idx ( 第一個開始被替換成結果的 Item )
                    onTheTopItemIdx = wrapItemIdx(itemIdx);  //輪帶停止時，位於最上方的Item，在 scroll 中的 idx
                    float distanceFromCeling = Math.Abs((celingY - newY));
                    newY = celingY - (distanceFromCeling % moveRange);
                }
                slotItems[itemIdx].setPosY(newY);
                itemIdx++;
            }
        }

        protected bool cycleMoveItem(int itemIdx, float moveDistance)
        {
            var item = slotItems[itemIdx];
            var newY = item.posY - moveDistance;
            bool overflow = newY <= floorY;
            if (overflow)
            {
                onTheBottomItemIdx = wrapItemIdx(itemIdx - 1);    //輪帶停止時，需位於最下方的Item，在 scrollItems 中的 idx ( 第一個開始被替換成結果的 Item )
                onTheTopItemIdx = wrapItemIdx(itemIdx);  //輪帶停止時，位於最上方的Item，在 scroll 中的 idx
                float distanceFromCeling = Math.Abs((celingY - newY));
                newY = celingY - (distanceFromCeling % moveRange);
                cycleAction();
            }
            item.setPosY(newY);
            return overflow;
        }

        public virtual void cycleAction() { }

        protected bool reverseCycleMoveItem(int itemIdx, float moveDistance)
        {
            var item = slotItems[itemIdx];
            var newY = item.posY - moveDistance;
            bool overflow = newY >= celingY;
            if (overflow)
            {
                onTheBottomItemIdx = wrapItemIdx(itemIdx);    //輪帶停止時，需位於最下方的Item，在 scrollItems 中的 idx ( 第一個開始被替換成結果的 Item )
                onTheTopItemIdx = wrapItemIdx(itemIdx + 1);  //輪帶停止時，位於最上方的Item，在 scroll 中的 idx
                float distanceFromCeling = Math.Abs((celingY - newY));
                newY = floorY + (distanceFromCeling % moveRange);
                cycleAction();
            }
            item.setPosY(newY);
            return overflow;
        }

        protected int nextDefaultReelIdx()
        {
            currentDefaultReelIdx = wrapReelIdx(currentDefaultReelIdx - 1);
            return wrapReelIdx(currentDefaultReelIdx);
        }
        int nextFakeDefaultReel()
        {
            int reel = startFakeDefaultReels.Pop();
            return reel;
        }
        void setItemToNextDefaultReel(ISlotItem item)
        {
            int idx = -1;
            if (startFakeDefaultReels.Count == 0)
            {
                int reelIdx = nextDefaultReelIdx();

                if (reelIdx < 0 || (reelIdx >= defaultReels.Length))
                {
                    Debug.Log($"setItemToNextDefaultReel err, reelIdx: {reelIdx}, defaultReels.Length: {defaultReels.Length}, scrollIdx: {scrollIdx}");
                }
                idx = defaultReels[reelIdx];
            }
            else
            {
                idx = nextFakeDefaultReel();
            }
            var reelData = new ReelStrip()
            {
                infoID = idx,    //default 輪帶
                customValue = 0,
            };
            item.setSymbolData(reelData);

            onRollingMoveNextItem?.Invoke(item);
        }

        /// <summary>
        /// 換結果輪帶
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        void changeToNextResultReel(ISlotItem item)
        {
            ulong[] data = new ulong[] { (ulong)defaultReels[currentDefaultReelIdx] };
            if (endFakeDefaultReels.Count == 0)
            {
                resultReelStartIdx = (resultReelStartIdx + resultReels.Length) % resultReels.Length;  //wrap
                data = resultReels[resultReelStartIdx];
            }
            else
            {
                var fakeIdx = endFakeDefaultReels.Pop();
                ulong idx = fakeIdx == -1 ? (ulong)defaultReels[currentDefaultReelIdx] : (ulong)fakeIdx;
                data = new ulong[] { idx };
            }
            var reelStrip = new ReelStrip(data);

            //更新已換盤面
            item.setSymbolData(reelStrip);
            resultReelStartIdx--;

            onItemChangeToResult?.Invoke(item);
        }

        protected int wrapItemIdx(int idx)
        {
            return (idx + slotItems.Count) % slotItems.Count;
        }

        protected int checkDefaultReelStartItemIdx(int idx)
        {
            return checkDefaultReelOrder ? wrapItemIdx(startUpdateItem - idx) : idx;
        }

        public virtual void manualStop()
        {
            setManualStopState();
            clearEndFakeDefaultReels();
        }

        protected void setManualStopState()
        {
            interrupted = true;
            constantVelocityDuration = 0;
        }

        protected void clearEndFakeDefaultReels()
        {
            endFakeDefaultReels.Clear();
        }

        /// <summary>
        /// 校正輪帶索引值
        /// </summary>
        /// <param name="idx"></param>
        /// <returns></returns>
        int wrapReelIdx(int idx)
        {
            return (idx + defaultReels.Length) % defaultReels.Length; //消除負數,
        }

        void itemSpeedUp(float currentV)
        {
            var moveDistance = Time.deltaTime * (previousV + currentV) * 0.5f;
            startUpdateItem = onTheTopItemIdx;
            for (int i = 0; i < slotItems.Count; ++i)
            {
                var itemIdx = checkDefaultReelStartItemIdx(i);
                var item = slotItems[itemIdx];
                var changeSymbol = cycleMoveItem(itemIdx, moveDistance);
                if (changeSymbol)
                {
                    setItemToNextDefaultReel(item);
                }
            }
            previousV = currentV;
        }

        /// <summary>
        /// 處理佔{倍數}格的特殊Symbol (遍歷)
        /// </summary>
        protected virtual void handleItemFormHeight()
        {

        }

        /// <summary>
        /// 處理佔{倍數}格的特殊Symbol (單顆)
        /// </summary>

        protected virtual void handleItemFormHeightToItem(ISlotItem slotItem)
        {

        }

        /// <summary>
        /// 要在還未進入等速前就加時，若在準備下沉階段加值，會無作用
        /// </summary>
        /// <param name="addTime"></param>
        public void addConstantTime(float addTime)
        {
            constantVelocityDuration += addTime;
        }

        public void setSpeedMultiple(float value)
        {
            constantSpeedMultiple = value;
        }


        public List<int> getShowItemsIdx()
        {
            return showItemsIdx;
        }

        protected void changeState(ScrollState state, Action onUpdateAction = null)
        {
            bool notify = (currentMoveState != state);
            currentMoveState = state;
            if (notify)
            {
                stateChangeCall?.Invoke(this, state);
            }
            this.onUpdateAction = onUpdateAction;
        }

        public void end()
        {
            //矯正回正確位置
            setSinkPosIdx();
            refineItemPosToResult();
            refineReelToResult();
            //避免因rollEnd被繼承覆寫造成cacheShowItems等觸發事件時間差
            cacheShowItems();
            constantSpeedMultiple = 1f;
            moveSpeed = 0f;
            remainDistance = 0f;
            rollEnd();
        }

        public void disposeEveyUpdate()
        {
            everyUpdateDisposable.Dispose();
            everyUpdateDisposable = null;
        }

        protected virtual void rollEnd()
        {
            changeState(ScrollState.STOP);
            if (!interrupted)
            {
                onScrollComplete?.Invoke(this);
            }

            onScrollEnd?.Invoke(this);
        }

        protected abstract void updateShowItems();

        protected void cacheShowItems()
        {
            showItemsIdx.Clear();
            int firstIdx = onTheTopItemIdx + topExtraSymbolCount;
            for (int i = 0; i < showCount; i++)
            {
                var idx = wrapItemIdx(firstIdx + i);
                showItemsIdx.Add(idx);
            }

            updateShowItems();
        }


        public void setReels(List<ReelStrip> reelStrips)
        {
            int itemIdx = reelStrips.Count >= slotItems.Count ? onTheTopItemIdx : onTheTopItemIdx + topExtraSymbolCount;
            for (int i = 0; i < reelStrips.Count; ++i)
            {
                itemIdx = wrapItemIdx(itemIdx);
                slotItems[itemIdx].setSymbolData(reelStrips[i]);
                itemIdx++;
            }
            handleItemFormHeight();
        }

    }
}
