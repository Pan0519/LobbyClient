namespace Game.Jackpot.Billboard
{
    public interface IConfig
    {
        //JP相關參數

        //Mini
        ulong MINI_BASIC_RATE { get; }   //Mini 起始倍率

        //Minor
        ulong MINOR_BASIC_RATE { get; }  //Minor 起始倍率

        //Major
        ulong MAJOR_BASIC_RATE { get; }  //Major 起始倍率

        //Grand
        ulong GRAND_BASIC_RATE { get; }  //Grand 起始倍率

        float MAX_LIMIT_RATE { get; }   //上限倍率

        float MIN_LIMIT_RATE { get; }   //下限倍率

        ulong SERVER_SCALE { get; }     //Server 運算用比例
    }
}
