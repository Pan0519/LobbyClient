using CommonILRuntime.SpriteProvider;
using UnityEngine;
using System.Collections.Generic;

namespace Lobby.Common
{
    class LobbyItemSpriteProvider : SpriteProviderBase
    {
        public override Sprite[] loadSpriteArray()
        {
            List<Sprite> sprite = new List<Sprite>();
            sprite.AddRange(ResourceManager.instance.loadAll("texture/res_banner/res_banner"));
            sprite.AddRange(ResourceManager.instance.loadAll("texture/res_banner_long/res_banner_long"));
            sprite.AddRange(ResourceManager.instance.loadAll("texture/res_banner_long/res_banner_long_2"));
            sprite.AddRange(ResourceManager.instance.loadAll("localization/en/res_banner_ui_localization"));
            //Debug.Log($"{UtilServices.getLocalizationAltasPath("res_banner_ui")}");
            return sprite.ToArray();
        }
    }
}
