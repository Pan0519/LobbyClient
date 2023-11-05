using CommonILRuntime.Module;
using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Lobby.UI
{
    public class DragablePresenter : NodePresenter
    {
        Dragable dragable;
        RectTransform dragableRectTrans;

        public override void initUIs()
        {
            dragable = getBindingData<Dragable>("dragable");
        }

        public override void init()
        {
            dragableRectTrans = dragable.GetComponent<RectTransform>();
            dragable.setOnDragHandler(dragHandler);
        }

        void dragHandler(PointerEventData data)
        {
            Vector2 delta = data.delta;

            float slope = delta.x != 0f ? Math.Abs(delta.y / delta.x) : 1f;

            delta = clamp(delta);

            if (slope < 1f)
            {
                delta = new Vector2(delta.x, 0f);
                dragable.invokeHorizontalMove(delta);
            }
            else
            {
                delta = new Vector2(0f, delta.y);
                dragable.invokeVerticalMove(delta);
            }

            Vector2 newPos = dragableRectTrans.anchoredPosition + delta;

            dragableRectTrans.anchoredPosition = dragableRectTrans.anchoredPosition + delta;
        }

        Vector2 clamp(Vector2 delta)
        {
            float x = delta.x;
            float y = delta.y;

            float viewWidth = dragable.viewRect.rect.width;
            float viewHeight = dragable.viewRect.rect.height;

            float minX = Math.Min(viewWidth - dragableRectTrans.rect.width, 0f);
            float maxY = Math.Max(dragableRectTrans.rect.height - viewHeight, 0f);

            var newPos = dragableRectTrans.anchoredPosition + delta;
            if (newPos.x < minX || newPos.x > 0f)
            {
                x = 0f;
            }

            if (newPos.y < 0f || newPos.y > maxY)
            {
                y = 0f;
            }

            return new Vector2(x, y);
        }

    }
}
