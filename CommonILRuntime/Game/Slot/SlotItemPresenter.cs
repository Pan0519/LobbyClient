using UnityEngine;
using CommonILRuntime.Module;

namespace Game.Slot
{
    using Game.Slot.Interface;
    public class SlotItemPresenter : NodePresenter, IGameSlotItem
    {
        protected ReelStrip reelStrip = new ReelStrip();

        public virtual float posY { get { return uiRectTransform.anchoredPosition.y; } }

        public virtual Vector3 position { get { return uiRectTransform.position; } }

        public virtual void setPosY(float y)
        {
            uiRectTransform.anchoredPosition = new Vector2(uiRectTransform.anchoredPosition.x, y);
        }

        public virtual void setSymbolData(ReelStrip reelData)
        {
            reelStrip = reelData;
        }

        public virtual int heightDouble { get { return 1; } }

        public virtual SymbolData getSymbolData() { return null; }

        public virtual void addOrSetAnimatedSymbol(string ani_trigger = "") { }
        public virtual void changeToStatic() { }
        public virtual void changeAnimatedSymbol(string ani_trigger) { }
        public virtual void showAnimatedSymbol(bool show) { }
    }
}
