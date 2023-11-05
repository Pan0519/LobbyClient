using DG.Tweening;

namespace CommonILRuntime.Game.Slot
{
    public interface ISlotConfigProvider
    {
        //輪帶移動參數
        float START_ROLL_DELAY_TIME { get; }   //輪帶啟動間格時間 (秒)
        float SPEED_UP_TIME { get; }            //加速時間 (秒)
        float MOVE_ITEM_PER_SEC { get; }         //物件/秒 (以 normal symbol 為單位)
        float CONST_VELOCITY_DURATION { get; }    //等速移動時間 (秒)
        float EXTRA_CONSTANT_DURATION { get; }    //額外移動時間 (依輪帶遞增)
        float DELAY_TO_SINK_DURATION { get; }           //延遲至下沉時間 (秒)
        float SINK_ITEM_PERCENT { get; }        //下沉百分比(格)
        float REBOUND_DURATION { get; }        //回彈時間 (秒)
        float SINK_DURATION { get; }    //下沉到定點時間 (秒)
        float RELOAD_DURATION { get; }  //補盤至定位時間 (秒)
        Ease SINK_TWEEN_TYPE { get; }   //下沉TWEEN曲線
        Ease REBOUND_TWEEN_TYPE { get; } //回彈TWEEN曲線 

        //輪帶加速
        float SCATTER_EXTEND_SECONDS { get; }      //聽牌旋轉延長時間 (秒)
        int LISTEN_SCATTER_COUNT { get; }        //Scatter加速門檻數量
        float SCATTER_SPEED_UP { get; }         //Scatter輪帶加速
        float WILD_SPEED_UP { get; }            //Wild輪帶加速
    }
}
