using CommonService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Game.Slot.Replenish
{
    public abstract class ReplenishScrollPresenter : SlotScrollPresenter
    {
        private float gridPos = 0f;
        private float speed = 0f;
        private List<ReelStrip> reelStrips = null;

        /// <summary>
        /// 單獨移動輪帶
        /// </summary>
        /// <param name="grid">移動格數 正數位向下移動 負數為向上移動</param>
        /// <param name="speed">移動速度 格數/秒</param>
        /// <param name="reelStrips">預替換值</param>
        public void moveScroll(int grid, float speed = 0, List<ReelStrip> reelStrips = null)
        {
            if (grid == 0)
            {
                return;
            }

            initParameter(grid, speed);
            if (reelStrips == null)         //如沒有值 便塞入假輪帶資料
            {
                reelStrips = new List<ReelStrip>();
                for (int i = 0; i < Math.Abs(grid); ++i)
                {
                    int reelIdx = nextDefaultReelIdx();
                    var reelData = new ReelStrip()
                    {
                        infoID = defaultReels[reelIdx],
                        customValue = 0,
                    };
                    reelStrips.Add(reelData);
                }
            }
            this.reelStrips = reelStrips;
            resetResultReels(grid, reelStrips);
            startMoveConstantRollToResult();
        }

        private void initParameter(int grid, float speed)
        {
            float isNegative = grid > 0 ? 1 : -1;
            this.speed = speed == 0 ? getBaseConstantSpeed() : speed * itemHeight;
            this.speed *= isNegative;
            gridPos = grid * itemHeight;
        }

        private void startMoveConstantRollToResult()
        {
            remainDistance = gridPos;    //由上往下移動，startPosY 必然大於targetPosY, 若 startPosY < targetPosY 則剩餘距離小於0
            changeState(SlotDefine.ScrollState.CONSTANT_SPEED_TO_RESULT, moveConstantRollToResult);
            startUpdate();
        }

        void moveConstantRollToResult()
        {
            if (remainDistance != 0)
            {
                if (DataStore.getInstance.gameTimeManager.IsPaused()) return;
                var moveDistance = speed * Time.deltaTime;
                moveDistance = Math.Abs(moveDistance) > Math.Abs(remainDistance) ? remainDistance : moveDistance;

                for (int itemIdx = 0; itemIdx < slotItems.Count; ++itemIdx)
                {
                    var item = slotItems[itemIdx];
                    bool changeSymbol = remainDistance > 0 ? cycleMoveItem(itemIdx, moveDistance) : reverseCycleMoveItem(itemIdx, moveDistance);
                    if (changeSymbol && reelStrips.Count > 0)
                    {
                        item.setSymbolData(reelStrips[0]);
                        reelStrips.RemoveAt(0);
                    }
                }

                if ((remainDistance <= 0 && gridPos > 0) || (remainDistance >= 0 && gridPos < 0))
                {
                    startMoveSink();
                }
                remainDistance -= moveDistance;
            }
            else
            {
                startMoveSink();
            }
        }

        private void startMoveSink()
        {
            remainDistance = speed > 0 ? getSinkHeight() : -getSinkHeight();
            changeState(SlotDefine.ScrollState.SINK, moveSink);
        }

        void moveSink()
        {
            if (remainDistance != 0)
            {
                if (DataStore.getInstance.gameTimeManager.IsPaused()) return;
                var moveDistance = speed * Time.deltaTime * config.SINK_DURATION;
                moveDistance = Math.Abs(moveDistance) > Math.Abs(remainDistance) ? remainDistance : moveDistance;
                for (int itemIdx = 0; itemIdx < slotItems.Count; ++itemIdx)
                {
                    var item = slotItems[itemIdx];
                    bool changeSymbol = remainDistance > 0 ? cycleMoveItem(itemIdx, moveDistance) : reverseCycleMoveItem(itemIdx, moveDistance);
                }

                if ((remainDistance <= 0 && speed > 0) || (remainDistance >= 0 && speed < 0))
                {
                    moveRebound(speed < 0);
                }
                remainDistance -= moveDistance;
            }
            else
            {
                moveRebound(speed < 0);
            }
        }

        void moveRebound(bool isNegative)
        {
            disposeEveyUpdate();
            changeState(SlotDefine.ScrollState.REBOUND);
            remainDistance = isNegative ? -getSinkHeight() : getSinkHeight();

            string tweenID = TweenManager.tweenToFloat(remainDistance, 0, config.REBOUND_DURATION, onUpdate: itemRebound,
                 onComplete: delegate
                 {
                     refineItemPosToResult();
                     refineReelToResult();
                     cacheShowItems();
                     changeState(SlotDefine.ScrollState.STOP);
                 });
        }

        void resetResultReels(int grid, List<ReelStrip> reelStrips)
        {
            var reel = resultReels;
            resultReels = new ulong[slotItems.Count][];
            for (int i = 0; i < slotItems.Count; ++i)
            {
                resultReels[i] = reel[wrapItemIdx(i - grid)];
            }
            for (int j = 1; j <= Math.Abs(grid); ++j)
            {
                var idx = wrapItemIdx(grid > 0 ? grid - j : slotItems.Count - 1 + grid + j);
                resultReels[idx] = reelStrips[j - 1].allData;
            }
        }
    }
}
