using UnityEngine;
using System.Collections.Generic;
using Services;
using CommonILRuntime.SpriteProvider;

namespace Lobby.Common
{
    public class ShopSpriteProvider : PurchaseInfoProvider
    {
        public override Sprite[] loadSpriteArray()
        {
            string path = "prefab/lobby_shop/pic";
            List<Sprite> shopSprites = new List<Sprite>();
            shopSprites.AddRange(ResourceManager.instance.loadAll($"{path}/res_shop/res_shop_2"));
            shopSprites.AddRange(ResourceManager.instance.loadAll($"{path}/res_shop_tip/res_shop_tip"));
            shopSprites.AddRange(ResourceManager.instance.loadAll($"{path}/res_shop_icon/res_shop_icon"));
            shopSprites.AddRange(ResourceManager.instance.loadAll(UtilServices.getLocalizationAltasPath("res_shop_sell_icon")));
            shopSprites.AddRange(base.loadSpriteArray());
            return shopSprites.ToArray();
        }
    }
}
