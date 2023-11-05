using Game.Slot.Interface;
using System.Collections.Generic;

namespace Game.Slot
{
    public interface IGameSlotScroll : ISlotScroll
    {
        void resetAllSlotItems();
        List<IGameSlotItem> getShowItems();
    }
}
