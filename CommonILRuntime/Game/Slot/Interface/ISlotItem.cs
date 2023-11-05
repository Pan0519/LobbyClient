using UnityEngine;

namespace Game.Slot.Interface
{
    public interface ISlotItem
    {
        //int symbolInfoId { get; set; }
        Vector3 position { get; }
        float posY { get; }
        void setPosY(float y);
        void setSymbolData(ReelStrip reelData); ///設定輪帶基礎資料，可繼承擴充同步更換對應Sprite Icon (參考 VegasSlotItem)
        int heightDouble { get; }               ///Symbol高度的尺寸倍數 (正常是1)
    }
}
