using CommonILRuntime.SpriteProvider;
using UnityEngine;
using System.Collections.Generic;

namespace Lobby.Common
{
    class EventActivitySpriteProvider : SpriteProviderBase
    {
        public override Sprite[] loadSpriteArray()
        {
            List<Sprite> sprite = new List<Sprite>();
            sprite.AddRange(ResourceManager.instance.loadAll("texture/activity_item/activity_item"));
            sprite.AddRange(ResourceManager.instance.loadAll("texture/activity_common/activity_common"));
            sprite.AddRange(ResourceManager.instance.loadAll("texture/activity_shop_sign/activity_shop_sign"));
            return sprite.ToArray();
        }
    }
}
