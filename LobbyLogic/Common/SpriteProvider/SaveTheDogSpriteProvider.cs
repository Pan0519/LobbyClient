using UnityEngine;
using System.Collections.Generic;
using CommonILRuntime.SpriteProvider;

namespace Lobby.Common
{
    public class SaveTheDogSpriteProvider : SpriteProviderBase
    {
        public override Sprite[] loadSpriteArray()
        {
            string lanStr = ApplicationConfig.nowLanguage.ToString().ToLower();
            List<Sprite> sprites = new List<Sprite>();
            sprites.AddRange(ResourceManager.instance.loadAllWithResOrder ("prefab/save_the_dog/pic/savethedog_map",AssetBundleData.getBundleName(BundleType.SaveTheDog)));
            sprites.AddRange(ResourceManager.instance.loadAll($"localization/{lanStr}/res_banner_short_localization"));
            sprites.AddRange(ResourceManager.instance.loadAll($"localization/{lanStr}/res_banner_short_gray_localization"));
            return sprites.ToArray();
        }
    }
}
