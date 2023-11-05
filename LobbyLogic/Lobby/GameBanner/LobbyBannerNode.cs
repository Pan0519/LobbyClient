using CommonILRuntime.Module;
using UnityEngine.UI;
using UnityEngine;
using System.Collections.Generic;
using Lobby.Common;

namespace Lobby
{
    class LobbyBannerNode : NodePresenter
    {
        RectTransform bannerContent;
        PageView bannerPageView;
        RectTransform bannerItemPointContent;

        List<Image> bannerItemPoints = new List<Image>();

        Sprite bannerItemPointOffSprite;
        Sprite bannerItemPointOnSprite;

        int bannerCount = 1;

        public override void initUIs()
        {
            bannerContent = getBindingData<RectTransform>("banner_content");
            bannerPageView = getBindingData<PageView>("banner_pageview");
            bannerItemPointContent = getBindingData<RectTransform>("banner_item_layout");
        }

        public override void init()
        {
            LobbyItemSpriteProvider itemSpriteProvider = LobbySpriteProvider.instance.getSpriteProvider<LobbyItemSpriteProvider>(LobbySpriteType.LobbyItem);
            bannerItemPointOffSprite = itemSpriteProvider.getSprite("page_off");
            bannerItemPointOnSprite = itemSpriteProvider.getSprite("page_on");
            bannerPageView.enabled = bannerCount > 1;
        }

        public override void open()
        {
            showBanner();
            base.open();
        }

        void showBanner()
        {
            for (int i = 0; i < bannerCount; ++i)
            {
                PoolObject pool = ResourceManager.instance.getObjectFromPool("prefab/lobby/banner_announce", bannerContent);
                //PoolObject itemPoint = ResourceManager.instance.getObjectFromPool("prefab/lobby/banner_item_point", bannerItemPointContent);
                //bannerItemPoints.Add(itemPoint.cachedGameObject.GetComponent<Image>());
            }

            bannerPageView.onObjChanceEvent.AddListener(bannerChangeEvent);
            bannerChangeEvent(0);
        }

        void bannerChangeEvent(int index)
        {
            for (int i = 0; i < bannerItemPoints.Count; ++i)
            {
                if (i == index)
                {
                    bannerItemPoints[i].sprite = bannerItemPointOnSprite;
                    continue;
                }

                bannerItemPoints[i].sprite = bannerItemPointOffSprite;
            }
        }
    }
}
