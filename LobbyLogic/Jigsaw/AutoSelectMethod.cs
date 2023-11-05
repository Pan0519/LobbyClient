using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lobby.Jigsaw.FantasySelectorMethod
{
    public interface ISelector
    {
        void select(List<RecyclingPiece> input, int[] starConditions, int targetFantasyCount);
    }

    public class FantasyFirstSelector : ISelector
    {
        //　a.先以啟動「鑽石之星」輪盤為首要條件，但若玩家「藍」「金」拼圖總數不足啟動「鑽石之星」時，則不選取「藍」「金」拼圖
        //  b.先以星級較少的拼圖開始選取（藍、金也一樣）

        //a.持有總星星數<第一階門檻：選取所有星星，但SPIN按鈕不亮起（未達第一階星星本來就不會亮）										
        //b.第一階門檻<持有總星星數<第二階門檻：選取星星數達第一階門檻
        //c.第二階門檻<持有總星星數<第三階門檻：選取星星數達第二階門檻
        //d.第三階門檻<持有總星星數：選取星星數達第三階門檻

        //targetFantasyCount: 所需的 Fantasy張數

        public void select(List<RecyclingPiece> input, int[] starConditions, int targetFantasyCount)
        {
            int fantasyCardCount = 0;
            for (int i = 0; i < input.Count; i++)
            {
                var piece = input[i];
                fantasyCardCount += piece.avaliableFantasyCount;
            }

            //不夠啟動FantasyStar
            if (fantasyCardCount < targetFantasyCount)
            {
                selectForNormal(input, starConditions);
            }
            else
            {
                selectForFantasy(input, targetFantasyCount, starConditions);
            }
           
        }

        void selectForNormal(List<RecyclingPiece> input, int[] starConditions)
        {
            //UI介面已經排序過了，這裡不另做排序

            //挑出一般無fantasy星星的拼圖
            var filtered = input.FindAll(piece => {
                return !piece.isFantasy;
            });

            //計算一般星星可用的有幾顆
            int normalStars = 0;
            for (int i = 0; i < filtered.Count; i++)
            {
                var piece = filtered[i];
                normalStars += piece.avaliableStarCount;
            }

            //算出可以啟動到第幾個轉輪
            int targetStars = 0;
            for (int i = 0; i < starConditions.Length; i++)
            {
                if (starConditions[i] <= normalStars)
                {
                    targetStars = starConditions[i];
                }
                else
                {
                    break;
                }
            }

            bool finish = false;
            for (int i = 0; (i < filtered.Count) && !finish; i++)
            {
                var piece = filtered[i];
                var selectedStars = 0;
                for (int selectCount = 1; selectCount <= piece.data.getCount(); selectCount++)
                {
                    var remainTargetStars = targetStars;
                    piece.setSelectedCount(selectCount);
                    selectedStars = piece.selectedStarCount;
                    remainTargetStars -= selectedStars;
                    if (remainTargetStars <= 0)
                    {
                        finish = true;
                        break;
                    }
                }
                targetStars -= selectedStars;
            }
        }

        void selectForFantasy(List<RecyclingPiece> input, int targetFantasyCount, int[] starConditions)
        {
            //UI介面已經排序過了，這裡不另做排序

            //挑出fantasy星星的拼圖
            var fantasyPieces = input.FindAll(piece => {
                return piece.isFantasy;
            });

            //挑出一般星星的拼圖
            var normalPieces = input.FindAll(piece => {
                return !piece.isFantasy;
            });

            int selectedFantasyStarCount = 0;

            //選取足夠的fantasy張數
            bool fantasyFinish = false;
            for (int i = 0; (i < fantasyPieces.Count) && !fantasyFinish; i++)
            {
                var piece = fantasyPieces[i];
                for (int selectCount = 1; selectCount <= piece.data.getCount(); selectCount++)
                {
                    piece.setSelectedCount(selectCount);
                    selectedFantasyStarCount += piece.singlePiecelStarCount;
                    if (targetFantasyCount - piece.getSelectCount() <= 0)
                    {
                        fantasyFinish = true;
                        break;
                    }
                }
                targetFantasyCount -= piece.getSelectCount();
            }

            //計算一般星星可用的有幾顆
            int normalStars = 0;
            for (int i = 0; i < normalPieces.Count; i++)
            {
                var piece = normalPieces[i];
                normalStars += piece.avaliableStarCount;
            }

            //算出可以啟動到第幾個轉輪
            int targetStars = 0;    //第N個condition所需要的星星量
            for (int i = 0; i < starConditions.Length; i++)
            {
                if (starConditions[i] <= normalStars + selectedFantasyStarCount)
                {
                    targetStars = starConditions[i];
                }
            }

            //還需要補上幾顆一般星星
            int neededNormalStars = targetStars - selectedFantasyStarCount;
            if (neededNormalStars > 0)
            {
                //選取足夠的一般星星
                bool normalFinish = false;
                for (int i = 0; (i < normalPieces.Count) && !normalFinish; i++)
                {
                    var piece = normalPieces[i];
                    var selectedStars = 0;
                    for (int selectCount = 1; selectCount <= piece.data.getCount(); selectCount++)
                    {
                        var remainTargetStars = neededNormalStars;
                        piece.setSelectedCount(selectCount);
                        selectedStars = piece.selectedStarCount;
                        remainTargetStars -= selectedStars;
                        if (remainTargetStars <= 0)
                        {
                            normalFinish = true;
                            break;
                        }
                    }
                    neededNormalStars -= selectedStars;
                }
            }
        }
    }
}
