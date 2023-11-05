using System;
using System.Collections.Generic;
using Slot.Game.GameStruct;

namespace Game.Slot
{
    public class SlotWinWayOperation : SlotWinOperation
    {
        #region Values
        public int[] slotStyle { get; private set; }
        public int symbolTypes { get; private set; } = 15;
        public int[] currentSymbols { get; private set; } = null;
        public int[][] specialCurrentSymbols { get; private set; } = null;
        private Func<ulong[][]> askReelDataCallBack = null;
        #endregion

        public SlotWinWayOperation()
        {
            setSlotStyle(3, 5);
        }

        /// <summary>
        /// 設定symbol種類數
        /// </summary>
        /// <param name="symbolTypes"></param>
        /// <returns></returns>
        public SlotWinWayOperation setSymbolTypes(int symbolTypes)
        {
            this.symbolTypes = symbolTypes;
            return this;
        }

        /// <summary>
        /// 設定取得ReelData管道
        /// </summary>
        /// <param name="askReelDataCallBack"></param>
        /// <returns></returns>
        public SlotWinWayOperation setAskReelDataCallBack(Func<ulong[][]> askReelDataCallBack)
        {
            this.askReelDataCallBack = askReelDataCallBack;
            return this;
        }

        /// <summary>
        /// 設定通用物件
        /// </summary>
        /// <param name="currentSymbols"></param>
        /// <returns></returns>
        public SlotWinWayOperation setCurrentSymbols(int[] currentSymbols)
        {
            this.currentSymbols = currentSymbols;
            return this;
        }

        /// <summary>
        /// 設定特定symbol對應可連線symbol
        /// </summary>
        /// <param name="specialCurrentSymbols">(ex. specialCurrentSymbols[0] = int[] { 4, 6, 8} 4可以和6跟8連線)</param>
        /// <returns></returns>
        public SlotWinWayOperation setSpecialCurrentSymbols(int[][] specialCurrentSymbols)
        {
            this.specialCurrentSymbols = specialCurrentSymbols;
            return this;
        }

        /// <summary>
        /// 寫入盤面樣式(適用於會增減輪帶長度的遊戲)
        /// </summary>
        /// <param name="style"></param>
        public SlotWinWayOperation setSlotStyle(int[] style)
        {
            slotStyle = style;
            return this;
        }

        /// <summary>
        /// 寫入盤面樣式(適用於固定輪帶長度的遊戲)
        /// </summary>
        /// <param name="style"></param>
        public SlotWinWayOperation setSlotStyle(int row, int column)
        {
            slotStyle = new int[column];
            for (int i = 0; i < slotStyle.Length; ++i)
            {
                slotStyle[i] = row;
            }
            return this;
        }

        /// <summary>
        /// 運算解果
        /// </summary>
        /// <param name="winConditions">贏線資料</param>
        /// <param name="reel">盤面資料</param>
        /// <returns></returns>
        public override void setWinCondition(WinCondition[] winConditions)
        {
            var reel = askReelDataCallBack?.Invoke();
            allWinLineItems = new WinLineItems();
            winLinesList = new List<WinLineItems>();
            if (winConditions.Length == 1)
            {
                allWinLineItems.winLineID = winConditions[0].Win_Item;
            }
            for (int i = 0; i < winConditions.Length; ++i)
            {
                var symbilLine = new WinLineItems();
                var condition = winConditions[i];
                symbilLine.winLineID = condition.Win_Item;
                for (int itemId = 0; itemId < getCountItemAmount(condition.Win_Type); ++itemId)
                {
                    if (isWinItems(condition.Win_Item, (int)reel[itemId][0]))
                    {
                        symbilLine.items.Add(itemId);
                        if (!allWinLineItems.items.Contains(itemId))
                        {
                            allWinLineItems.items.Add(itemId);
                        }
                    }
                }
                winLinesList.Add(symbilLine);
            }
            winLinesList = arrangeWinLinesList(winLinesList);
        }

        int getCountItemAmount(int winType)
        {
            int lastItem = 0;
            for (int i = 0; i < winType + shortestLine; ++i)
            {
                lastItem += slotStyle[i];
            }

            return lastItem;
        }

        bool isWinItems(int winLineId, int reelSymbolID)
        {
            return winLineId == reelSymbolID || checkCurrentSymbol(reelSymbolID) || checkSpecialCurrentSymbol(winLineId, reelSymbolID);
        }

        bool checkCurrentSymbol(int reelSymbolID)
        {
            if (null != currentSymbols)
            {
                for (int currentID = 0; currentID < currentSymbols.Length; ++currentID)
                {
                    if (currentSymbols[currentID] == reelSymbolID)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        bool checkSpecialCurrentSymbol(int winLineId, int reelSymbolID)
        {
            if (null != specialCurrentSymbols)
            {
                int[] specialRule = null;
                for (int i = 0; i < specialCurrentSymbols.Length; ++i)
                {
                    var specialSymbolData = specialCurrentSymbols[i];
                    if (specialSymbolData[0] == winLineId)
                    {
                        specialRule = specialSymbolData;
                    }
                }
                if (null != specialRule)
                {
                    for (int rules = 1; rules < specialRule.Length; ++rules)
                    {
                        if (specialRule[rules] == reelSymbolID)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 資料排序
        /// </summary>
        /// <param name="winList"></param>
        /// <returns></returns>
        protected virtual List<WinLineItems> arrangeWinLinesList(List<WinLineItems> winList)
        {
            List<WinLineItems> arrangeWinLines = new List<WinLineItems>();
            for (int i = 0; i < symbolTypes; ++i)
            {
                for (int j = 0; j < winList.Count; ++j)
                {
                    var lineList = winList[j];
                    if (lineList.winLineID == i)
                    {
                        arrangeWinLines.Add(lineList);
                        break;
                    }
                }
            }
            return arrangeWinLines;
        }
    }
}
