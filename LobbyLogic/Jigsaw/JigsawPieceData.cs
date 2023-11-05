using System;
using UnityEngine;

namespace Lobby.Jigsaw
{
    public class JigsawPieceData
    {
        int iId;
        int count;

        public JigsawPieceData(string id, int count = 0)
        {
            ID = id;
            iId = Convert.ToInt32(id);
            this.count = count;
        }

        public string ID { get; }

        public bool collectted { get { return count > 0; } }

        public int getCount()
        {
            return count;
        }

        public void setCount(int value)
        {
            count = value;
        }

        /// <summary>
        /// 星級
        /// </summary>
        /// <returns></returns>
        public int getStarLevel()
        {
            return iId % 10;
        }

        /// <summary>
        /// 稀有度
        /// 對應 enum RareLevel
        /// GREEN = 1,
        /// BLUE = 2,
        /// YELLOW = 3,
        /// </summary>
        /// <returns></returns>
        public int getRareLevel()
        {
            return (iId / 10 % 10) + 1;
        }

        /// <summary>
        /// 位置 ( 0~13 )
        /// </summary>
        /// <returns></returns>
        public int getImagePos()
        {
            return iId /100 % 100;   //0~99
        }

        /// <summary>
        /// 拼圖冊編號 (01~99)
        /// </summary>
        /// <returns></returns>
        public int getAlbumIdx()
        {
            return iId / 10000 % 100;
        }

        /// <summary>
        /// 季度編號 (001~999)
        /// </summary>
        public int getSeasonIdx()
        {
            return iId / 1000000 % 1000;
        }

        public bool isUpSide()
        {
            var imageIdx = getImagePos();   //1~7,  8~14
            int downSideStartIdx = JigsawDefine.totalPieces / 2;
            return imageIdx <= downSideStartIdx;
        }

        public void printInfo()
        {
            Debug.Log($"id: {iId}");
            Debug.Log($"starCount: {getStarLevel()}");
            Debug.Log($"rareLevel: {getRareLevel()}");
            Debug.Log($"pos: {getImagePos()}");
            Debug.Log($"albumId: {getAlbumIdx()}");
            Debug.Log($"season: {getSeasonIdx()}");
        }
    }
}
