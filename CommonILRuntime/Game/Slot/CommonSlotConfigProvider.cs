using DG.Tweening;

namespace CommonILRuntime.Game.Slot
{
    public abstract class CommonSlotConfigProvider : ISlotConfigProvider
    {
        //輪帶移動參數
        public virtual float START_ROLL_DELAY_TIME { get; } = 0f;   //輪帶啟動間格時間 (秒)
        public virtual float SPEED_UP_TIME { get; } = 0.1f;            //加速時間 (秒)
        public virtual float MOVE_ITEM_PER_SEC { get; } = 18f;         //物件/秒 (以 normal symbol 為單位)
        public virtual float CONST_VELOCITY_DURATION { get; } = 1f;   //等速移動時間 (秒)
        public virtual float EXTRA_CONSTANT_DURATION { get; } = 0.4f;    //額外移動時間 (依輪帶遞增)
        public virtual float DELAY_TO_SINK_DURATION { get; } = 0.5f;           //延遲至下沉時間 (秒)
        public virtual float SINK_ITEM_PERCENT { get; } = 0.5f;        //下沉百分比(格)
        public virtual float SINK_DURATION { get; } = 1f;    //下沉到定點時間
        public virtual float REBOUND_DURATION { get; } = 0.24f;        //回彈時間 (秒)
        public virtual float RELOAD_DURATION { get; } = 1f; //補盤至定位時間 (秒)
        public virtual Ease SINK_TWEEN_TYPE { get; } = Ease.OutQuad;   //下沉TWEEN曲線
        public virtual Ease REBOUND_TWEEN_TYPE { get; } = Ease.InQuad; //回彈TWEEN曲線 

        //輪帶加速
        public virtual float SCATTER_EXTEND_SECONDS { get; } = 4.7f;    //聽牌旋轉延長時間 (秒)
        public virtual int LISTEN_SCATTER_COUNT { get; } = 2;        //Scatter加速門檻數量
        public virtual float SCATTER_SPEED_UP { get; } = 1.5f;         //Scatter輪帶加速
        public virtual float WILD_SPEED_UP { get; } = 1.5f;            //Wild輪帶加速
    }
}
