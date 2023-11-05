using CommonILRuntime.SpriteProvider;
using System.Collections.Generic;
using UnityEngine;

namespace Lobby.Common
{
    public class DailyMissionProvider : SpriteProviderBase
    {
        public override Sprite[] loadSpriteArray()
        {
            List<Sprite> sprites = new List<Sprite>();
            string language = ApplicationConfig.nowLanguage.ToString().ToLower();
            sprites.AddRange(ResourceManager.instance.loadAll($"localization/{language}/res_daily_localization"));
            sprites.AddRange(ResourceManager.instance.loadAll($"localization/{language}/result_title_common_localization"));
            return sprites.ToArray();
        }
    }
}
