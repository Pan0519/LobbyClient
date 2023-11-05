using CommonILRuntime.Module;
using UnityEngine;

namespace Lobby.UI
{
    public class DragLinkerPresenter : NodePresenter
    {
        DragLinker linker;

        public override void initUIs()
        {
            linker = getBindingData<DragLinker>("linker");
        }

        public override void init()
        {
            if (null != linker.parent)
            {
                if (linker.vertical)
                {
                    linker.parent.registerVerticalMoved(onRectTransChanged);
                }

                if (linker.horizontal)
                {
                    linker.parent.registerHorizontalMoved(onRectTransChanged);
                }
            }
        }


        void onRectTransChanged(Vector2 delta)
        {
            uiRectTransform.anchoredPosition += delta;
        }
    }
}
