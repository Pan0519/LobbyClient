using UnityEngine;
using System.Collections.Generic;
using CommonILRuntime.SpriteProvider;

namespace Lobby.Common
{
    class ActivityQuestProvider : SpriteProviderBase
    {
        public override Sprite[] loadSpriteArray()
        {
            List<Sprite> sprites = new List<Sprite>();
            string lanStr = ApplicationConfig.nowLanguage.ToString().ToLower();
            sprites.AddRange(ResourceManager.instance.loadAll("prefab/quest_mission/pic/dog_enter_banner"));
            sprites.AddRange(ResourceManager.instance.loadAll($"localization/{lanStr}/res_banner_short_localization"));
            sprites.AddRange(ResourceManager.instance.loadAll($"localization/{lanStr}/res_banner_short_gray_localization"));
            return sprites.ToArray();

        }
    }
}
