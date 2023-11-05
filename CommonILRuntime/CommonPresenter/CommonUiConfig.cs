
namespace CommonPresenter
{
    public static class CommonUiConfig
    {
        //按鈕控制
        const float STOP_BTN_LOCK_TIME = 0.5f;          //開始轉動後，STOP鈕鎖定時間(秒)

        static float stopBtnLockTime;

        public static void initStopBtnLockTime()
        {
            stopBtnLockTime = STOP_BTN_LOCK_TIME;
        }

        public static void setStopBtnLockTime(int lockTime)
        {
            stopBtnLockTime = lockTime;
        }

        public static float getStopBtnLockTime()
        {
            return stopBtnLockTime;
        }

        public enum StopMode
        {
            Normal = 0,
            Quick = -1,
            ManualControl = -2,
        }
        //mode : 1(開啟) 0(無動作) -1(關閉)
        public enum BottomFreeMode
        {
            Close = -1,
            None,
            Open,
        }
        public enum BottomBtnStage
        {
            Spin,
            Stop,
        }

        public enum AutoMode
        {
            NUMBER = 0,                 //有限自動玩, 可計次
            INFINITY_AND_BREAK = -1,    //無限自動玩+急停
            INFINITY = -2,              //無限自動玩
        }

        public enum ExpInfoOpenState
        {
            None,
            Time,
            State,
        }

    }
}
