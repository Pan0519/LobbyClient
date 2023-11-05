using CommonService;
using System;

namespace Game.Common
{
    public abstract class GameConfig
    {
        //數值有異動請override覆寫

        //Way Game表演
        public virtual float WIN_LINE_DELAY_TIME { get { return 2.0f; } }    //連線播放時間(秒)
        public virtual float START_SPIN_DELAY_TIME { get { return 1.0f; } }    //開始轉動輪盤前的等待時間(秒)
        public virtual float WINDOW_STAY_DELAY_TIME { get { return 3.0f; } }   //報獎版待機時間(秒)

        //連線表演
        public virtual float NO_WIN_NEXT_SPIN_TIME { get { return 1.0f; } }       //沒連線時下一SPIN間隔(毫秒)
        public virtual float ALL_LINE_PERFORM_TIME { get { return 2.0f; } }      //全連線表演時間(毫秒)
        public virtual float WAIT_TO_ITERATE_LINE_TIME { get { return 0.5f; } }   //等待進入單線表演時間(毫秒)
        public virtual float SINGLE_LINE_PERFORM_TIME { get { return 0.8f; } }    //單線表演時間(毫秒)
        public virtual float NEXT_LINE_PERFORM_INTERVAL { get { return 0.2f; } }  //單線表演間隔(毫秒)
        public virtual float JACKPET_PARTICLE_SPEED { get { return 0.7f; } }   //JP粒子特效速度(秒)，配合音效表演長度 1 秒

        public virtual int ORIGINATE_WIN_TYPE_NUM { get { return 2; } }       //Win_Type 起始連線數量

        //JP
        public virtual int JP_CHECK_LOCK_LEVEL { get { return 20; } }          //開始鎖定獎項的等級, ex checkLockLevel == 20, 玩家等級第20級開始檢查獎項是否鎖定
        public virtual float JP_MAJOR_UNLOCK_PERCENT { get { return 0.3f; } }     //JP Major 解鎖門檻
        public virtual float JP_GRAND_UNLOCK_PERCENT { get { return 0.6f; } }     //JP Grand 解鎖門檻

        //BottomBar 跑分
        public virtual float RESET_WIN_POINT_TIME { get { return 0.5f; } }      //重設贏分時間

        //過場表演
        public virtual float FREEGAME_ENTER_DELAY { get { return 3f; } }
        public virtual float BONUSGAME_ENTER_DELAY { get { return 0f; } }
        public virtual float BACK_TO_NORMAL_TIME { get { return 0f; } }         //回到NormalGame 的過場時間
        public virtual float BONUS_CUT_SCENE_TIME { get { return 5f; } }     //Bonus切場景時間
        public virtual float FREE_CUT_SCENE_TIME { get { return 5f; } }     //FreeGame切場景時間

        public virtual float ENTER_DELAY_TIME { get { return 2f; } }     //FreeGame切場景時間

        //BGM
        public virtual float BGM_FADE_TIME { get { return 1.5f; } }     //BGM淡出時間

        //公用報獎板參數
        public virtual ulong BIG_WIN_BET { get { return 5; } }
        public virtual ulong MEGA_WIN_BET { get { return 10; } }
        public virtual ulong EPIC_WIN_BET { get { return 30; } }
        public virtual ulong MASSIVE_WIN_BET { get { return 50; } }
        public virtual ulong ULTIMATE_WIN_BET { get { return 100; } }
        //公用報獎板滾分秒數
        public virtual float BIG_WIN_LAUNDERING_TIME { get { return 10f; } }
        public virtual float MEGA_WIN_LAUNDERING_TIME { get { return 10f; } }
        public virtual float EPIC_WIN_LAUNDERING_TIME { get { return 13.5f; } }
        public virtual float MASSIVE_WIN_LAUNDERING_TIME { get { return 13.5f; } }
        public virtual float ULTIMATE_WIN_LAUNDERING_TIME { get { return 19.5f; } }
        //公用報獎板停留秒數
        public virtual float BIG_WIN_WAIT { get { return 3f; } }
        public virtual float MEGA_WIN_WAIT { get { return 3f; } }
        public virtual float EPIC_WIN_WAIT { get { return 5f; } }
        public virtual float MASSIVE_WIN_WAIT { get { return 5f; } }
        public virtual float ULTIMATE_WIN_WAIT { get { return 8f; } }

        //公用報獎小面板參數
        public virtual ulong NICE_WIN_BET { get { return 5; } }
        public virtual ulong AMAZING_WIN_BET { get { return 10; } }
        public virtual ulong INCREDIBLE_WIN_BET { get { return 20; } }
        //公用報獎小面板停留秒數
        public virtual float NICE_WIN_WAIT { get { return 5f; } }
        public virtual float AMAZING_WIN_WAIT { get { return 5f; } }
        public virtual float INCREDIBLE_WIN_WAIT { get { return 5f; } }

        //JP飛幣參數
        public virtual float JP_EFFECT_POSITION_X { get { return 5.0f; } }    //飛幣曲線X軸偏移值
        public virtual float JP_EFFECT_POSITION_Y { get { return 0.0f; } }    //飛幣曲線Y軸偏移值

        //Animation Trigger
        public virtual string SCATTER_JUMP { get { return "jump"; } }
        public virtual string SCATTER_OPEN { get { return "open"; } }
        public virtual string COIN_JUMP { get { return "jump"; } }         //停輪
        public virtual string COIN_LOOP { get { return "loop"; } }         //停輪
        public virtual string COIN_SHINE { get { return "shine"; } }       //獲得BonusGame or 全盤表演
        public virtual string COIN_SETTLEMENT { get { return "settle"; } } //結算時文字彈跳
        public virtual string BOTTOM_BAR_FLASH { get { return "flash"; } } //Bonus結算 BottomBar 閃光

        //過場
        public virtual string CUT_SCENE_ANIMATOR_FREE { get { return ""; } } //FREE過場動畫
        public virtual string CUT_SCENE_ANIMATOR_BONUS { get { return ""; } } //BINUS過場動畫

        //Obj PathName
        public virtual string SMALL_SYMBOL_OBJ { get { return "prefab/slot/symbol/icon_small_slot"; } }
        public virtual string SMALL_ANIMATED_SYMBOL { get { return "prefab/slot/symbol/icon_{0}"; } }
        public virtual string BIG_SYMBOL_OBJ { get { return "prefab/slot/symbol_big/icon_big_slot"; } }
        public virtual string BIG_ANIMATED_SYMBOL { get { return "prefab/slot/symbol_big/icon_{0}_big"; } }
        public virtual string SHORT_FRAME_EFFECT { get { return "prefab/slot/get_frame_short"; } }
        public virtual string LONG_FRAME_EFFECT { get { return "prefab/slot/listen_frame_long"; } }
        public virtual string BONUS_SETTLE_PARTICLE { get { return "prefab/slot/vbg_particle_2"; } } //已完成回收
        public virtual string BONUS_SETTLE_BOTTOM_LIGHT { get { return "prefab/slot/vbg_total_effect"; } }
        public virtual string PATH_CUT_SCENE_ANIMATOR_FREE { get { return "prefab/slot/free_game_cut"; } } //FREE過場動畫路徑
        public virtual string PATH_CUT_SCENE_ANIMATOR_BONUS { get { return "prefab/slot/bonus_game_cut"; } } //BINUS過場動畫路徑

        //NormalGame 輪帶數量(直的為一條)
        public virtual int NORMAL_TABLE_ROW_COUNT { get { return 3; } }        //盤面列數
        public virtual int NORMAL_TABLE_COLUMN_COUNT { get { return 5; }}     //盤面欄數

        //FreeGame
        public virtual int BIG_SCROLL_ROW_COUNT { get { return 1;}}
        public virtual int BIG_SCROLL_REEL_IDX_BEGIN { get { return 3;}}
        public virtual int BIG_SCROLL_REEL_IDX_END { get { return 11;}}

        //BonusGame 輪帶數量
        //prefab 以 row major 排列
        public virtual int BONUS_TABLE_ROW_COUNT { get { return 3;}}         //盤面列數
        public virtual int BONUS_TABLE_COLUMN_COUNT { get { return 5;}}      //盤面欄數
        public virtual int BONUS_SCROLL_SHOW_COUNT { get { return 1;}}        //一輪顯示幾個
        

        //Client統一PlayerState狀態，Server未統一，client須做轉換
        public enum PlayerState : int
        {
            ReadyGame = 0,
            NGSpin = 1,
            NGEnd,
            NGEndFromFG,
            NGEndFromBG,
            NGEndFromSFG,
            NGEndFromJP,
            FGSpin,
            FGEnd,
            BGSpin,
            BGEnd,
            SFGSpin,
            SFGEnd,
            JPSpin,
            JPEnd,
            AnotherFGSpin,
        }
        public enum GameState
        {
            ENTER = 0,
            NG,
            BG,
            FG,
            SFG,
            JP
        }

        
        public enum JackPotLevels : int
        {
            None,
            Grand,
            Major,
            Minor,
            Mini,
        }

        public virtual GameConfig.PlayerState GetNowPlayerState()
        {
            return TransClientPlayerState(DataStore.getInstance.playerInfo.nowGameState);
        }

        /// <summary>
        /// 將server state轉成 client state
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        public abstract GameConfig.PlayerState TransClientPlayerState(int state);

        public abstract int TransServerPlayerState(GameConfig.PlayerState state);

        #region 需將以下函式複製到各遊戲繼承的config裡
        /*
        public override GameConfig.PlayerState TransClientPlayerState(int state)
        {
            string stringvalue = ((PlayerState)state).ToString();
            GameConfig.PlayerState gamePlayerState = (GameConfig.PlayerState)Enum.Parse(typeof(GameConfig.PlayerState), stringvalue);

            return gamePlayerState;
        }

        public override int TransServerPlayerState(GameConfig.PlayerState state)
        {
            return ServerPlayerState(state);
        }

        /// <summary>
        /// 將Client player state轉成server用的值
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        public static int ServerPlayerState(GameConfig.PlayerState state)
        {
            string stringvalue = (state).ToString();
            PlayerState serverPlayerState = (PlayerState)Enum.Parse(typeof(PlayerState), stringvalue);
            return (int)serverPlayerState;
        }

        /// <summary>
        /// 將server player state轉成client端的state
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        public static GameConfig.PlayerState ClientPlayerState(int state)
        {
            string stringvalue = ((PlayerState)state).ToString();
            GameConfig.PlayerState gamePlayerState = (GameConfig.PlayerState)Enum.Parse(typeof(GameConfig.PlayerState), stringvalue);
            return gamePlayerState;
        }
        */
        #endregion
    }
}
