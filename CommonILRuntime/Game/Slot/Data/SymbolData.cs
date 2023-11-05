namespace Game.Slot
{
    public class SymbolData
    {
        public int infoID { get; private set; }

        public SymbolData(ulong[] data)   //一個 Symbol 的資料結構
        {
            infoID = (int)data[0];  //指定輪帶的symbol ID
        }

        public virtual bool IsWild { get { return false; } }

        public virtual bool IsScatterFree { get { return false; } }
        public virtual bool IsScatterBonus { get { return false; } }
        public virtual bool IsChip { get{return false;}}
    }
}
