namespace Game.Slot
{
    public static class SlotDefine
    {
        public const int TOP_EXTRA_SYMBOL_COUNT = 1;        //輪帶上方幾個Symbol
        public const int BOTTOM_EXTRA_SYMBOL_COUNT = 1;     //輪帶下方幾個Symbol

        public enum ScrollState : int
        {
            STOP = 0,                       //靜止
            SPEED_UP,                       //加速
            CONSTANT_SPEED,                 //等速
            CONSTANT_SPEED_TO_RESULT,       //等速(切換真盤面)
            SINK,                           //減速
            REBOUND                         //回彈
        }

        public enum AutoMode
        {
            AUTO = 0,                       //有限自動玩, 可計次
            INFINITY_AND_FAST_STOP = -1,    //無限自動玩+急停
            INFINITY_AUTO = -2,             //無限自動玩
        }
    }
}
