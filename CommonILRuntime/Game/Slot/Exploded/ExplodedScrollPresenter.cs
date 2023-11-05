using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Slot.Exploded
{
    public class ExplodedScrollPresenter : SlotScrollPresenter
    {
        protected class ReloadItem
        {
            public IExplodedSlotItem SlotItem { get; private set; }
            public float FinalPosi { get; private set; }
            public float OriPosi { get; private set; }

            public ReloadItem(IExplodedSlotItem slotItem, float finalPosi)
            {
                SlotItem = slotItem;
                FinalPosi = finalPosi;
                OriPosi = slotItem.posY;
            }
        }

        protected new List<IExplodedSlotItem> slotItems = null;
        protected List<IExplodedSlotItem> showItems = null;
        protected List<ReloadItem> reloadItems = null;
        private List<int> currentSymbolOrder = null;
        private List<int> endRollSymbolOrder = null;
        private Queue<int> waitSymbolPool = null;

        private Action onSymbolMoveOver = null;

        public override void init()
        {
            slotItems = new List<IExplodedSlotItem>();
            showItems = new List<IExplodedSlotItem>();
            currentSymbolOrder = new List<int>();
            endRollSymbolOrder = new List<int>();
            waitSymbolPool = new Queue<int>();
            reloadItems = new List<ReloadItem>();
        }

        protected override void rollEnd()
        {
            updateSymbolOrder();
            base.rollEnd();
        }

        private void updateSymbolOrder()
        {
            var showIndexs = getShowItemsIdx();
            int topIndex = showIndexs[0] - 1;

            topIndex = wrapItemIdx(topIndex);
            currentSymbolOrder.Clear();
            currentSymbolOrder.Add(topIndex);
            for (int i = 0; i < showCount; ++i)
            {
                currentSymbolOrder.Add(showIndexs[i]);
            }
        }

        protected override void updateShowItems()
        {
            var showIndex = getShowItemsIdx();
            var count = showIndex.Count;
            int itemIndex = 0;

            showItems.Clear();
            for (int i = 0; i < count; ++i)
            {
                itemIndex = showIndex[i];
                showItems.Add(slotItems[itemIndex]);
            }
        }

        public List<Vector3> getExplodedItemPosi(int[] explodedPosiIndexs)
        {
            int count = explodedPosiIndexs.Length;
            int posiIndex = 0;
            int symbolIndex = 0;
            List<Vector3> result = new List<Vector3>();

            for (int i = 0; i < count; ++i)
            {
                posiIndex = explodedPosiIndexs[i];
                symbolIndex = currentSymbolOrder[posiIndex + 1];
                result.Add(slotItems[symbolIndex].position);
            }

            return result;
        }

        public void explodedSymbol(int[] explodedPosiIndexs)
        {
            int count = explodedPosiIndexs.Length;
            int posiIndex = 0;

            updateEndTableOrder();
            for (int i = 0; i < count; ++i)
            {
                posiIndex = explodedPosiIndexs[i];
                disappearSymbol(posiIndex);
                moveSymbolToTop(posiIndex);
                updateSymbolIndexOrder(posiIndex);
            }
        }

        private void updateEndTableOrder()
        {
            int count = currentSymbolOrder.Count;

            endRollSymbolOrder.Clear();
            for (int i = 0; i < count; ++i)
            {
                endRollSymbolOrder.Add(currentSymbolOrder[i]);
            }
        }

        private void disappearSymbol(int posiIndex)
        {
            var symbolIndex = endRollSymbolOrder[posiIndex + 1];
            slotItems[symbolIndex].setItemActive(false);
            waitSymbolPool.Enqueue(symbolIndex);
        }

        private void moveSymbolToTop(int posiIndex)
        {
            var symbolIndex = endRollSymbolOrder[posiIndex + 1];
            float topPosi = posYList[0] + (itemHeight * waitSymbolPool.Count);

            slotItems[symbolIndex].setPosY(topPosi);
        }

        private void updateSymbolIndexOrder(int posiIndex)
        {
            int targetSymbol = endRollSymbolOrder[posiIndex + 1];
            int targetIndex = getOrderIndex(targetSymbol);
            int symbolIndex = currentSymbolOrder[targetIndex];

            for (int i = targetIndex; i >= 0; --i)
            {
                if (0 == i)
                {
                    currentSymbolOrder[i] = symbolIndex;
                }
                else
                {
                    currentSymbolOrder[i] = currentSymbolOrder[i - 1];
                }
            }
        }

        private int getOrderIndex(int symbolIndex)
        {
            int count = currentSymbolOrder.Count;

            for (int i = 0; i < count; ++i)
            {
                if (currentSymbolOrder[i] == symbolIndex)
                {
                    return i;
                }
            }

            return 0;
        }

        public void moveLeftoverSymbol(Action onSymbolMoveOver)
        {
            this.onSymbolMoveOver = onSymbolMoveOver;
            prepareMoveLeftoverSymbol();
            moveSymbolToPosi();
        }

        private void prepareMoveLeftoverSymbol()
        {
            int count = currentSymbolOrder.Count;
            int symbolIndex = 0;
            IExplodedSlotItem symbol = null;
            ReloadItem reloadItem = null;

            for (int i = 1; i < count; ++i)
            {
                symbolIndex = currentSymbolOrder[i];
                symbol = slotItems[symbolIndex];
                if (checkIsHigherThanTargetPosi(symbol, i) && checkIsLowerThanCeiling(symbol))
                {
                    reloadItem = new ReloadItem(symbol, posYList[i]);
                    symbol.setItemActive(true);
                    reloadItems.Add(reloadItem);
                }
            }
        }

        private bool checkIsLowerThanCeiling(IExplodedSlotItem symbol)
        {
            return 0 >= Mathf.Floor(symbol.posY - posYList[1]);
        }

        public void reloadSymbol(int[] fallStrip, Action onSymbolMoveOver)
        {
            this.onSymbolMoveOver = onSymbolMoveOver;
            updateWaitSymbol(fallStrip);
            prepareMoveAllItems();
            moveSymbolToPosi();
            updateShowItemFromBlownUpOrder();
        }

        private void updateWaitSymbol(int[] fallStrip)
        {
            if (fallStrip.Length != waitSymbolPool.Count)
            {
                Debug.LogWarning("補盤回傳資料與前端消除物件數量對不齊");
                return;
            }

            int count = fallStrip.Length;
            int symbolIndex = 0;
            ulong[] stripData = null;

            for (int i = 0; i < count; ++i)
            {
                symbolIndex = waitSymbolPool.Dequeue();
                stripData = new ulong[] { (ulong)fallStrip[i] };
                slotItems[symbolIndex].setSymbolData(new ReelStrip(stripData));
            }
        }

        protected virtual void moveSymbolToPosi()
        {
            float startPosiRate = 0f;
            float endPosiRate = 1f;

            TweenManager.tweenToFloat(startPosiRate, endPosiRate, config.RELOAD_DURATION, onUpdate: onSymbolMoving, onComplete: onReloadComplete, easeType: getReloadTweenEase());
        }

        protected virtual DG.Tweening.Ease getReloadTweenEase()
        {
            return DG.Tweening.Ease.InCubic;
        }

        private void prepareMoveAllItems()
        {
            int count = currentSymbolOrder.Count;
            int symbolIndex = 0;
            IExplodedSlotItem symbol = null;
            ReloadItem reloadItem = null;

            for (int i = 0; i < count; ++i)
            {
                symbolIndex = currentSymbolOrder[i];
                symbol = slotItems[symbolIndex];
                if (checkIsHigherThanTargetPosi(symbol, i))
                {
                    reloadItem = new ReloadItem(symbol, posYList[i]);
                    symbol.setItemActive(true);
                    reloadItems.Add(reloadItem);
                }
            }
        }

        private bool checkIsHigherThanTargetPosi(IExplodedSlotItem symbol, int posiIndex)
        {
            return 0 < Mathf.Floor(symbol.posY - posYList[posiIndex]);
        }

        private void onSymbolMoving(float posiRate)
        {
            int count = reloadItems.Count;
            float val = 0f;
            float newPosi = 0f;
            ReloadItem reloadItem = null;

            for (int i = 0; i < count; ++i)
            {
                reloadItem = reloadItems[i];
                val = (reloadItem.FinalPosi - reloadItem.OriPosi) * posiRate;
                newPosi = reloadItem.OriPosi + val;
                reloadItem.SlotItem.setPosY(newPosi);
            }
        }

        private void onReloadComplete()
        {
            List<IExplodedSlotItem> moveItems = new List<IExplodedSlotItem>();

            for (int i = 0; i < reloadItems.Count; ++i)
            {
                moveItems.Add(reloadItems[i].SlotItem);
            }

            moveOverHook(moveItems);
            reloadItems.Clear();
            onSymbolMoveOver?.Invoke();
        }

        protected virtual void moveOverHook(List<IExplodedSlotItem> moveItems)
        { }

        private void updateShowItemFromBlownUpOrder()
        {
            var count = currentSymbolOrder.Count;
            int itemIndex = 0;

            showItems.Clear();
            for (int i = 1; i < count; ++i)
            {
                itemIndex = currentSymbolOrder[i];
                showItems.Add(slotItems[itemIndex]);
            }
        }

        public void resetSymbolOrder()
        {
            makeOrderToOriginal();
            resetSibling();
            updateShowItemFromBlownUpOrder();
        }

        private void makeOrderToOriginal()
        {
            int count = currentSymbolOrder.Count - 1;
            int currentPosiItemIndex = 0;
            int oriPosiItemIndex = 0;

            for (int i = 0; i < count; ++i)
            {
                currentPosiItemIndex = currentSymbolOrder[i];
                oriPosiItemIndex = getOriPositionItemIndex(i);
                if (currentPosiItemIndex != oriPosiItemIndex)
                {
                    exchangeSymbolItem(slotItems[currentPosiItemIndex], slotItems[oriPosiItemIndex]);
                    changeOrderIndexToOriginal(oriPosiItemIndex, currentPosiItemIndex, i);
                }
            }
        }

        private int getOriPositionItemIndex(int posiIndex)
        {
            int result = 0;
            var oriShowIndexs = getShowItemsIdx();

            if (posiIndex == 0)
            {
                result = wrapItemIdx(oriShowIndexs[0] - 1);
            }
            else
            {
                result = oriShowIndexs[posiIndex - 1];
            }

            return result;
        }

        private void exchangeSymbolItem(IExplodedSlotItem currentItem, IExplodedSlotItem oriItem)
        {
            var tempData = currentItem.getSymbolData();
            var posi = currentItem.posY;

            currentItem.changeSymbolData(oriItem.getSymbolData());
            currentItem.setPosY(oriItem.posY);
            oriItem.changeSymbolData(tempData);
            oriItem.setPosY(posi);
        }

        private void changeOrderIndexToOriginal(int oriPosiItemIndex, int currentPosiItemIndex, int orderIndex)
        {
            int targetIndex = currentSymbolOrder.IndexOf(oriPosiItemIndex);
            currentSymbolOrder[orderIndex] = oriPosiItemIndex;
            currentSymbolOrder[targetIndex] = currentPosiItemIndex;
        }

        private void resetSibling()
        {
            int symbolIndex = 0;
            int startIndex = currentSymbolOrder.Count - 1;

            for (int i = startIndex; i >= 0; --i)
            {
                symbolIndex = currentSymbolOrder[i];
                slotItems[symbolIndex].moveSiblingToFirst();
            }
        }

        public ulong[][] getSymbolDatasExceptBottom()
        {
            int symbolIndex = 0;
            int count = currentSymbolOrder.Count;
            ulong[][] result = new ulong[count][];
            SymbolData tempData = null;

            for (int i = 0; i < count; ++i)
            {
                symbolIndex = currentSymbolOrder[i];
                tempData = slotItems[symbolIndex].getSymbolData();
                result[i] = new ulong[] { (ulong)tempData.infoID };
            }

            return result;
        }
    }
}
