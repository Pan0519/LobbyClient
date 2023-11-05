using Game.Slot;
using Game.Slot.Interface;

namespace Game.Slot
{
    public interface IGameSlotItem : ISlotItem
    {
        SymbolData getSymbolData();

        void addOrSetAnimatedSymbol(string ani_trigger = "");
        void changeToStatic();
        void changeAnimatedSymbol(string ani_trigger);
        void showAnimatedSymbol(bool show);
    }
}
