using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using System.Linq;
using System.Threading.Tasks;
using Debug = UnityLogUtility.Debug;
using Slot.Game.GameStruct;
using System.Threading;
using Game.Common;
using System;

namespace Game.Slot
{
    public class SlotWinLine
    {
        protected SlotWinOperation winOperation = null;
        protected SlotWinFrames winFrames = null;
        protected GameConfig gameConfig = null;
        protected List<IGameSlotItem> showItems = null;
        protected RectTransform EffectLayoutRect;
        protected WinCondition[] winConditions;
        protected CancellationTokenSource iterateWinLineCts = null;
        protected Action<bool> OnShowWinLineBeforeAction = null;
        public string ANIMATION_TRIGGER = string.Empty;

        public bool IsShowingAllLine { get; private set; }

        public virtual void init(SlotWinFrames slotWinFrames, SlotWinOperation slotWinOperation, GameConfig gameConfig, RectTransform effectLayoutRect)
        {
            winFrames = slotWinFrames;
            winOperation = slotWinOperation;
            this.gameConfig = gameConfig;
            EffectLayoutRect = effectLayoutRect;
        }

        public void registerShowWinLineBeforeAction(Action<bool> action)
        {
            OnShowWinLineBeforeAction += action;
        }
        public void unregisterShowWinLineBeforeAction(Action<bool> action)
        {
            OnShowWinLineBeforeAction -= action;
        }
        public void clearShowWinLineBeforeAction()
        {
            OnShowWinLineBeforeAction = null;
        }

        public void breakIterateWinLine()
        {
            if (null != iterateWinLineCts)
            {
                iterateWinLineCts.Cancel();
                iterateWinLineCts.Dispose();
                iterateWinLineCts = null;
                showAllWinLineEffect(false);
            }
        }


        public void setShowItems(List<IGameSlotItem> items)
        {
            showItems = items;
        }

        public List<int> setSlotWinConditon(WinCondition[] conditions)
        {
            winOperation.setWinCondition(conditions);
            return winOperation.allWinLineItems.items;
        }

        public void fetchWinLineEffectPos(List<Vector3> pos)
        {
            winFrames.makeFrameEffect(EffectLayoutRect, pos);
        }

        protected void showAllWinLineEffect(bool show, bool canShowSymbolAnime = true)
        {
            if (null == winOperation.allWinLineItems)
            {
                return;
            }

            var lineItems = winOperation.allWinLineItems;

            OnShowWinLineBeforeAction?.Invoke(show);
            if (show)
            {
                showWinLineEffect(lineItems, canShowSymbolAnime);
            }
            else
            {
                clearWinLineEffect(lineItems);
            }
        }

        protected void showWinLineEffect(WinLineItems winLineItems, bool canShowSymbolAnime)
        {
            List<int> winSymbolIndex = winLineItems.items;
            if (null == winSymbolIndex || 0 == winSymbolIndex.Count)
            {
                return;
            }

            winFrames.setFrameEffects(winSymbolIndex);
            showExtraSymbolEffect(winLineItems);
            attempToChangeSymbolAniEnable(canShowSymbolAnime, winSymbolIndex);
        }

        protected virtual void showExtraSymbolEffect(WinLineItems winLineItems) { }

        protected virtual void attempToChangeSymbolAniEnable(bool canShowSymbolAnime, List<int> winSymbolIndex)
        {
            if (!canShowSymbolAnime || null == showItems)
            {
                return;
            }

            int count = showItems.Count;

            for (int i = 0; i < count; ++i)
            {
                if (winSymbolIndex.Contains(i))
                {
                    showItems[i].addOrSetAnimatedSymbol(ANIMATION_TRIGGER);
                }
                else
                {
                    showItems[i].changeToStatic();
                }
            }
        }

        private void clearWinLineEffect(WinLineItems winLineItems)
        {
            winFrames.clearFrameEffects();
            clearExtraSymbolEffect(winLineItems);
            closeInLineSymbol(winLineItems.items);
        }

        protected virtual void clearExtraSymbolEffect(WinLineItems winLineItems) { }

        protected virtual void closeInLineSymbol(List<int> lineItems)
        {
            if (null == showItems)
            {
                return;
            }

            int count = lineItems.Count;
            int symbolIndex = 0;

            for (int i = 0; i < count; ++i)
            {
                symbolIndex = lineItems[i];
                showItems[symbolIndex].changeToStatic();
            }
        }

        public void showInterruptableWinLineEffect(bool isNeedLoop = true)
        {
            if (isNeedLoop)
            {
                CoroutineManager.StartCoroutine(loopEffects());
            }
            else
            {
                CoroutineManager.StartCoroutine(showAllLineAndWaiteInterrupt());
            }
        }

        private IEnumerator showAllLineAndWaiteInterrupt()
        {
            iterateWinLineCts = new CancellationTokenSource();
            CancellationToken cancelToken = iterateWinLineCts.Token;
            showAllWinLineEffect(true);
            while (!cancelToken.IsCancellationRequested)
            {
                yield return 0;
            }
            showAllWinLineEffect(false);
        }

        private IEnumerator loopEffects()
        {
            var loopLineItems = getAllWinLineItems();
            WinLineItems lineItems = null;
            int loopIndex = 0;

            iterateWinLineCts = new CancellationTokenSource();
            CancellationToken cancelToken = iterateWinLineCts.Token;

            while (true)
            {
                lineItems = loopLineItems[loopIndex];
                OnShowWinLineBeforeAction?.Invoke(true);
                showWinLineEffect(lineItems, true);

                yield return gameConfig.SINGLE_LINE_PERFORM_TIME;

                if (cancelToken.IsCancellationRequested)
                {
                    yield break;
                }
                OnShowWinLineBeforeAction?.Invoke(false);
                clearWinLineEffect(lineItems);

                loopIndex = updateLoopIndex(loopIndex, loopLineItems.Count);
            }
        }

        private List<WinLineItems> getAllWinLineItems()
        {
            List<WinLineItems> result = new List<WinLineItems>();

            result.Add(winOperation.allWinLineItems);
            result.AddRange(winOperation.winLinesList);

            return result;
        }

        private int updateLoopIndex(int currentIndex, int totalCount)
        {
            currentIndex++;
            if (currentIndex >= totalCount)
            {
                currentIndex = 0;
            }
            return currentIndex;
        }

        public void showAllWinLine(Func<IEnumerator> beforeCloseAction = null, Func<IEnumerator> performBuffer = null)
        {
            CoroutineManager.AddCorotuine(startSingleAllLineEffect(beforeCloseAction, performBuffer));
        }

        private IEnumerator startSingleAllLineEffect(Func<IEnumerator> beforeCloseAction, Func<IEnumerator> performBuffer = null)
        {
            IsShowingAllLine = true;
            showAllWinLineEffect(true);

            yield return CoroutineManager.StartCoroutine(performLineBuffer(performBuffer));
            if (null != beforeCloseAction)
            {
                yield return CoroutineManager.StartCoroutine(beforeCloseAction.Invoke());
            }
            endAllWinLine();
        }

        private IEnumerator performLineBuffer(Func<IEnumerator> performBuffer)
        {
            if (null != performBuffer)
            {
                yield return CoroutineManager.StartCoroutine(performBuffer.Invoke());
            }
            else
            {
                yield return gameConfig.ALL_LINE_PERFORM_TIME;
            }
        }

        private void endAllWinLine()
        {
            IsShowingAllLine = false;
            showAllWinLineEffect(false);
        }

        public void changeToLoopAni()
        {
            if (!IsShowingAllLine)
            {
                return;
            }

            IsShowingAllLine = false;
            CoroutineManager.StopCorotuine(startSingleAllLineEffect(null));
            showInterruptableWinLineEffect();
        }
    }
}
