namespace Game.Slot.Exploded
{
    public interface IExplodedSlotItem : IGameSlotItem
    {
        void setItemActive(bool active);
        void changeSymbolData(SymbolData newData);
        void moveSiblingToFirst();
    }
}
