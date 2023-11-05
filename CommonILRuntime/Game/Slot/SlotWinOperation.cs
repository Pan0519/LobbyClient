using Slot.Game.GameStruct;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Slot
{
    public abstract class SlotWinOperation
    {
        #region Values
        public int shortestLine { get; set; } = 2;
        #endregion

        public WinLineItems allWinLineItems;
        public List<WinLineItems> winLinesList;

        /// <summary>
        /// 設定起始連線數量
        /// </summary>
        /// <param name="shortestLine"></param>
        /// <returns></returns>
        public SlotWinOperation setShortestLine(int shortestLine)
        {
            this.shortestLine = shortestLine;
            return this;
        }


        /// <summary>
        /// 所有連線資料
        /// </summary>
        /// <returns></returns>
        public virtual WinLineItems getAllWinItems()
        {
            return allWinLineItems;
        }

        /// <summary>
        /// 各個連線資料
        /// </summary>
        /// <returns></returns>
        public virtual List<WinLineItems> getWinLinesList()
        {
            return winLinesList;
        }

        public abstract void setWinCondition(WinCondition[] winConditions);
    }
}
