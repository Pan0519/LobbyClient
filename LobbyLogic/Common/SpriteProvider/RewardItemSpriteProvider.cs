using CommonILRuntime.SpriteProvider;
using UnityEngine;
using System.Collections.Generic;

namespace Lobby.Common
{
    class RewardItemSpriteProvider : SpriteProviderBase
    {
        public override Sprite[] loadSpriteArray()
        {
            List<Sprite> sprite = new List<Sprite>();
            sprite.AddRange(ResourceManager.instance.loadAll("texture/reward/pic/reward_item_texture"));
            return sprite.ToArray();
        }
    }
}
