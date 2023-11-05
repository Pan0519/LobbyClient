namespace Game.Slot
{
    public class ReelStrip
    {
        public int infoID = -1;
        public ulong customValue;
        public ulong[] allData = null;

        public ReelStrip() { }

        public ReelStrip(ulong[] reel)   //一個 Symbol 的資料結構
        {
            infoID = (int)reel[0];  //指定輪帶的symbol ID
            customValue = reel.Length <= 1 ? 0 : reel[1];   //客製化的參數值 (ex: Vegas-金幣值, 金虎爺-Icon)
            allData = reel.Length <= 1 ? new ulong[] { (ulong)infoID } : reel;
        }

        public ulong[] toArray()
        {
            return null != allData ? allData : new ulong[] { (ulong)infoID };
        }

        public bool equals(ReelStrip reelStrip)
        {
            return reelStrip.infoID == infoID && reelStrip.customValue == customValue;
        }
    }
}