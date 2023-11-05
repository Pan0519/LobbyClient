namespace Game.Jackpot.Billboard
{
    public class JPConfig : IConfig
    {
        //JP相關參數

        public virtual ulong MINI_BASIC_RATE { get; } = 5;  //Mini 起始倍率
        public virtual ulong MINOR_BASIC_RATE { get; } = 20; //Minor 起始倍率
        public virtual ulong MAJOR_BASIC_RATE { get; } = 50;  //Major 起始倍率
        public virtual ulong GRAND_BASIC_RATE { get; } = 200; //Grand 起始倍率

        public virtual float MAX_LIMIT_RATE { get; } = 1.1f;

        public virtual float MIN_LIMIT_RATE { get; } = 0.95f;

        public virtual ulong SERVER_SCALE { get; } = 1000;
    }
}
