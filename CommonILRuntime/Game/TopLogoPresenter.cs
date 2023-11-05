using CommonILRuntime.Module;
using UnityEngine;

namespace Game.Slot
{
    public class TopLogoPresenter : NodePresenter
    {
        protected float screenHeight;
        protected float topBorder;
        protected float bottomBorder;

        GameObject logo;

        public TopLogoPresenter setBorder(float height, float top, float bottom)
        {
            screenHeight = height;
            topBorder = top;
            bottomBorder = bottom;
            return this;
        }

        public override void initUIs()
        {
            string language = ApplicationConfig.nowLanguage.ToString().ToLower();
            GameObject en = null;
            for (int i = 0; i < uiRectTransform.childCount; ++i)
            {
                var _logo = uiRectTransform.GetChild(i).gameObject;
                _logo.setActiveWhenChange(false);
                if (_logo.name == language)
                {
                    logo = _logo;
                }
                else if (_logo.name == "en")
                {
                    en = _logo;
                }
            }
            if (null == logo)
            {
                logo = en;
            }
        }

        public virtual void showLogo()
        {
            var spaceHeight = screenHeight - topBorder - bottomBorder;
            if (uiRectTransform.rect.height > spaceHeight)
            {
                return;
            }
            uiRectTransform.anchoredPosition = new Vector2(0, -topBorder - (spaceHeight / 2));
            open();
            logo.setActiveWhenChange(true);
        }
    }
}
