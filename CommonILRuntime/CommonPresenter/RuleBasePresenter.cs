
using CommonILRuntime.Module;
using CommonService;
using Services;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Common
{
    public class RuleBasePresenter : ContainerPresenter
    {
        public override string objPath { get { return "prefab/game_info"; } }
        public override UiLayer uiLayer { get { return UiLayer.GameMessage; } }

        #region UI
        public CustomBtn leftBtn;
        public CustomBtn rightBtn;
        public CustomBtn backBtn;

        // 舊版 (為了向下相容所以不砍掉)
        public RectTransform pagesParent = null;
        List<GameObject> pages = new List<GameObject>();

        // 多語系版
        Image page = null;
        List<Sprite> sprites = new List<Sprite>();
        #endregion

        int pageID = 0;
        bool isLanguageVer = false;

        public override void initUIs()
        {
            leftBtn = getCustomBtnData("btn_left");
            rightBtn = getCustomBtnData("btn_right");
            backBtn = getCustomBtnData("btn_back");
        }

        public override void init()
        {
            initPages();
            backBtn.clickHandler = close;
            leftBtn.clickHandler = previousPage;
            rightBtn.clickHandler = nextPage;
            setPage(pageID);
        }

        void initPages()
        {
            isLanguageVer = initLanguageSprite();
            if (isLanguageVer)
            {
                page = getBindingData<Image>("page");
                return;
            }

            pagesParent = getBindingData<RectTransform>("pages_parent");
            for (int i = 0; i < pagesParent.childCount; ++i)
            {
                pages.Add(pagesParent.GetChild(i).gameObject);
            }
        }

        bool initLanguageSprite()
        {
            string language = ApplicationConfig.nowLanguage.ToString().ToLower();
            bool haveNext = true;
            int num = 1;
            string path = "";
            while(haveNext)
            {
                path = $"texture/game_info/{language}/{language}_rule{num}";
                Sprite ruleSprite = ResourceManager.instance.load<Sprite>(path);
                if (ruleSprite != null)
                {
                    sprites.Add(ruleSprite);
                    num++;
                }
                else
                {
                    haveNext = false;
                }
            }

            var backSprite = ResourceManager.instance.load<Sprite>(UtilServices.getLocalizationAltasPath("btn_backtogame"));
            if (backSprite != null)
            {
                backBtn.image.sprite = backSprite;
            }

            return sprites.Count > 0;
        }

        public override void open()
        {
            DataStore.getInstance.gameTimeManager.Pause();
            setPage(0);
            base.open();
            Debug.Log($"IsPaused?{DataStore.getInstance.gameTimeManager.IsPaused()}");
        }

        public override void close()
        {
            base.close();
            DataStore.getInstance.gameTimeManager.Resume();
        }

        void previousPage()
        {
            int _page = wradPageIdx(pageID - 1);
            setPage(_page);
        }

        void nextPage()
        {
            int _page = wradPageIdx(pageID + 1);
            setPage(_page);
        }

        void setPage(int pageID)
        {
            this.pageID = pageID;
            if (isLanguageVer)
            {
                page.sprite = sprites[pageID];
                page.SetNativeSize();
                return;
            }
            for (int i = 0; i < pages.Count; ++i)
            {
                pages[i].setActiveWhenChange(i == pageID);
            }
        }

        int wradPageIdx(int idx)
        {
            if (isLanguageVer)
            {
                return (idx + sprites.Count) % sprites.Count;
            }
            return (idx + pages.Count) % pages.Count;
        }
    }
}
