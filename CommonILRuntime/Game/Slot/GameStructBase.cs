using CommonILRuntime.Extension;
using CommonService;

namespace Slot.Game.GameStruct
{
    /// <summary>
    /// 各線獎項內容
    /// </summary>
    public class WinCondition
    {
        public int Win_Line;
        public int Win_Item;
        public int Win_Type;
        public int Win_Pay;
    }

    /// <summary>
    /// 共用的贏線結構
    /// </summary>
    public class WinInfo
    {
        public int Fg_Game_Times;
        public int Fg_Game_Type; 
        public decimal[][] Fg_First_Reel_Strip;
        public ulong[][] getFgFirstReelStrip { get { return Fg_First_Reel_Strip.ConvertToUlongTwoDismission(); } }
        public int[] Fg_First_Reel_Index;
        public decimal Total_Win; //總贏分
        public ulong getTotalWin { get { return (ulong)Total_Win; } }
        public int Total_Pay;  //總倍率
        public int Total_Line; //總贏線
        public WinCondition[] Win_Condition;

        //新增
        public int Bonus_Index;                 //Bonus_Info 的索引(預設-1 代表沒中)
        public decimal JPReelWin;                  //JP 轉輪金額
        public ulong getJPReelWin { get { return (ulong)JPReelWin; } }
        //Billionaire
        public decimal RapidJPWin;                 //Rapid JP金額
        public ulong getRapidJPWin { get { return (ulong)RapidJPWin; } }
        public int[] CEJPTimes;                  //CEJP次數

        public int FreeMode;
    }

    public class BonusProgress
    {  
        public int value;       //累積值
        public int Target;      //目標值
    }



    public class Pools
    {
        public decimal Grand = 0;
        public decimal Minor = 0;
        public decimal Major = 0;
        public decimal Mini = 0;
    }


    public class WalletData
    {
        public decimal Coin;
        public long Revision;
        public Wallet wallet 
        {
            get
            {
                return new Wallet()
                {
                    revision = Revision,
                    coin = Coin
                };
            }
        }
    }

    /// <summary>
    /// 此結構不共用
    /// </summary>
    /// 
    /*
    public class BonusInfo
    {
        public bool Bonus_Type;
        public int Bonus_Num;
        public long[][][] Bonus_ReelStrip;
        public int Bonus_Pay;
        public long Bonus_TotalWin;
        public long Jp_Prize;
    }*/
}
