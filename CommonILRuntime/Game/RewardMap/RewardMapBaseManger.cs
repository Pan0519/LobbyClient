//using ManekiNeko.Network.ResponseStruct;
using System.Collections.Generic;
using Debug = UnityLogUtility.Debug;
//using static ManekiNeko.Game.ManekiNekoGameConfig;
using Slot.Game.GameStruct;

namespace Game.Slot
{
    /// <summary>
    /// 獎勵地圖管理器
    /// </summary>
    public class RewardMapBaseManger
    {
        /*
        public class SFGInfo
        {
            public List<MapBonusType> bounsItems;
            public long coinMoney;
            public SFGInfo()
            {
                bounsItems = new List<MapBonusType>();
                coinMoney = 0;
            }
        }*/
        /// <summary>
        /// 進度累積條
        /// </summary>
        public BonusProgress mProgress { get; set; }
        /// <summary>
        /// 加乘道具
        /// </summary>
        //public Dictionary<MapCheckPoint, SFGInfo> mapBonusItems { get; private set; }
        /// <summary>
        /// 加乘道具(UI專用)
        /// </summary>
        //public List<MapBonusType> mapOnlyBouns { get; private set; }
        /// <summary>
        /// 當前關卡數(0~17)
        /// </summary>
        public int mapNowPoint { get; set; }
        /// <summary>
        /// 當前關卡類型
        /// </summary>
        //public MapCheckPoint mapNowPointType { get; private set; }
        /// <summary>
        /// 是否玩過？
        /// </summary>
        public bool isPlay { get;  set; }
        /// <summary>
        /// 本次BG/SFG結束，前往下一個關卡
        /// </summary>
        public bool isGoNextCheckPoint { get; set; }

        public int[] pointSFGNum;//= new int[] { 2, 6, 11, 17 };
        /*
        MapBonusType[] pointItems = new MapBonusType[] { MapBonusType.ExtraFree, MapBonusType.AddSymbol, MapBonusType.None,
                                                         MapBonusType.ExtraFree, MapBonusType.AddSymbol, MapBonusType.AddReel, MapBonusType.None,
                                                         MapBonusType.ExtraFree, MapBonusType.AddSymbol, MapBonusType.AddReel, MapBonusType.AddRow, MapBonusType.None,
                                                         MapBonusType.ExtraFree, MapBonusType.AddSymbol, MapBonusType.AddReel, MapBonusType.AddRow, MapBonusType.AddHundred, MapBonusType.None };
        */
        public SlotGameBasePresenter gameUI;


        public RewardMapBaseManger(SlotGameBasePresenter slot )
        {
            gameUI = slot;

            isPlay = false;
            isGoNextCheckPoint = false;
            //pointSFGNum = new int[] { 2, 6, 11, 17 };
            /*
            mapOnlyBouns = new List<MapBonusType>();
            mapBonusItems = new Dictionary<MapCheckPoint, SFGInfo>();
            for (MapCheckPoint i = MapCheckPoint.SFG_1; i <= MapCheckPoint.SFG_4; i++)
            {
                SFGInfo info = new SFGInfo();
                mapBonusItems.Add(i, info);
            }*/
        }

        public virtual void clearMapInfo()
        {
            /*
            mapOnlyBouns.Clear();
            for (MapCheckPoint i = MapCheckPoint.SFG_1; i <= MapCheckPoint.SFG_4; i++)
            {
                mapBonusItems[i].bounsItems.Clear();
            }*/
        }

        public virtual void updateMapInfo(int reachIndex ,ulong[] data)
        {
            /*
            //檢查
            if(response.ReachIndex < -1 || response.ReachIndex > 16)
            {
                Debug.LogError($"MAP回傳的資料有錯");
                mapNowPoint = 0;
                clearMapInfo();
                return;
            }

            //Server傳來的值為-1~16，故+1方便Client計算
            mapNowPoint = response.ReachIndex + 1;
            mapNowPointType = GetCheckPointType(mapNowPoint);

            clearMapInfo();
            long[] data = response.RoadMap;

            isPlay = getIsPlay(data);
            if(isPlay)
            {
                MapCheckPoint nowSFG = MapCheckPoint.SFG_1;
                for (int i = 0; i < data.Length; i++)
                {
                    long num = data[i];
                    SFGInfo info = mapBonusItems[nowSFG];
                    if(num > (long)MapBonusType.Max)
                    {
                        info.coinMoney = num;
                        nowSFG++;
                        continue;
                    }
                    MapBonusType type = (MapBonusType)num;
                    mapOnlyBouns.Add(type);
                    info.bounsItems.Add(type);
                }
            }
            */
        }

        public void updateProgress(BonusProgress bonusProgress)
        {
            mProgress = bonusProgress;
            initProgressUI();
        }

        public void addCoin(int value)
        {
            mProgress.value += value;
            setProgressUI();
        }

        /// <summary>
        /// BG/SFG結束，更新地圖與進度條
        /// </summary>
        public void endCheckPoint()
        {
            //處理進度條
            mProgress.value = 0;
            setProgressUI();
        }

        /*
        /// <summary>
        /// 抓取獎勵地圖的關卡類型
        /// </summary>
        public MapCheckPoint GetCheckPointType(int point)
        {
            for (int i = 0; i < pointSFGNum.Length; i++)
            {
                int order = pointSFGNum[i]; 
                if (point == order)
                {
                    return (MapCheckPoint)(i + 1);
                }
            }
            return MapCheckPoint.Bouns;
        }

        /// <summary>
        /// 抓取該次轉輪出現的加乘道具
        /// </summary>
        public MapBonusType GetMapBonusType(int point)
        {
            return pointItems[point];
        }

        /// <summary>
        /// 當前關卡是否有加一百隻金貓
        /// </summary>
        public bool GetNowHaveAddHundredCat()
        {
            //return true;

            if (mapNowPointType < MapCheckPoint.SFG_1)
            {
                return false;
            }

            bool IsHavaAddHundredCats = false;
            List<MapBonusType> list = mapBonusItems[mapNowPointType].bounsItems;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == MapBonusType.AddHundred)
                {
                    IsHavaAddHundredCats = true;
                    break;
                }
            }
            return IsHavaAddHundredCats;
        }*/


        //==================================以下為計算================================

        public bool getIsPlay(ulong[] data)
        {
            //有值代表有玩過
            for (int i = 0; i < data.Length; i++)
            {
                if (data[i] > 0)
                {
                    return true;
                }
            }
            return false;
        }

        void setProgressUI()
        {
            gameUI.setMapSlider(mProgress.value, mProgress.Target);
        }

        void initProgressUI()
        {
            gameUI.setMapSlider(mProgress.value, mProgress.Target, true);
        }
    }
}
