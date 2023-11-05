using Slot.Game.GameStruct;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Slot
{
    public class SlotWinLineOperation : SlotWinOperation
    {
        #region Values
        public int[][] payLines { get; private set; } = null;
        #endregion

        /// <summary>
        /// 設定連線種類
        /// </summary>
        /// <param name="payLines"></param>
        /// <returns></returns>
        public SlotWinLineOperation setPayLines(int[][] payLines)
        {
            this.payLines = payLines;
            return this;
        }
        
        /// <summary>
        /// 運算解果
        /// </summary>
        /// <param name="winConditions">贏線資料</param>
        /// <returns></returns>
        public override void setWinCondition(WinCondition[] winConditions)
        {
            if (null == payLines)
            {
                Debug.LogError("You don't have setting payline");
                return;
            }

            if (null == winConditions)
            {
                Debug.LogError("WinCondition is null");
                return;
            }

            winConditions = sortWinCondition(winConditions);
            allWinLineItems = new WinLineItems();
            winLinesList = new List<WinLineItems>();
            for (int i = 0; i < winConditions.Length; ++i)
            {
                var winline = setWinLine(winConditions[i]);
                var items = winline.items;
                for (int j = 0; j < items.Count; ++j)
                {
                    if (!allWinLineItems.items.Contains(items[j]))
                    {
                        allWinLineItems.items.Add(items[j]);
                    }
                }
                winLinesList.Add(winline);
            }
        }

        private WinCondition[] sortWinCondition(WinCondition[] winConditions)
        {
            int count = winConditions.Length;
            int compareLength = count - 1;
            WinCondition condition = null;
            WinCondition nextCondition = null;

            for(int i = 0;i < count;++i)
            {
                condition = winConditions[i];
                for (int j = 0; j < compareLength; ++j)
                {
                    condition = winConditions[j];
                    nextCondition = winConditions[j + 1];
                    if (condition.Win_Type < nextCondition.Win_Type)
                    {
                        winConditions[j] = nextCondition;
                        winConditions[j + 1] = condition;
                    }
                }
                compareLength--;
            }


            return winConditions;
        }

        /// <summary>
        /// 將WinCondition轉換為WinLineItems格式
        /// </summary>
        /// <param name="winCondition"></param>
        /// <returns></returns>
        protected WinLineItems setWinLine(WinCondition winCondition)
        {
            var effect = new WinLineItems()
            {
                winLineID = winCondition.Win_Item,
                items = getWinLineItems(winCondition.Win_Line, winCondition.Win_Type)
            };
            return effect;
        }

        /// <summary>
        /// 取的items值
        /// </summary>
        /// <param name="win_line"></param>
        /// <param name="win_type"></param>
        /// <returns></returns>
        List<int> getWinLineItems(int win_line, int win_type)
        {
            List<int> winLines = new List<int>();
            int[] winLine = payLines[win_line];
            for (int i = 0; i < win_type + shortestLine; ++i)
            {
                winLines.Add(winLine[i]);
            }
            return winLines;
        }
    }
}
