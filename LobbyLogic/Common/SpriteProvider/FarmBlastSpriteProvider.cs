using CommonILRuntime.SpriteProvider;
using UnityEngine;
using System.Collections.Generic;

namespace Lobby.Common
{
    class FarmBlastSpriteProvider : EventActivitySpriteProvider
    {
        public override Sprite[] loadSpriteArray()
        {
            List<Sprite> sprites = new List<Sprite>();
            sprites.AddRange(base.loadSpriteArray());
            sprites.AddRange(ResourceManager.instance.loadAllWithResOrder("prefab/activity/farm_blast/pic/res_fb/res_fb", AssetBundleData.getBundleName(BundleType.FarmBlast)));
            sprites.AddRange(ResourceManager.instance.loadAllWithResOrder("prefab/activity/farm_blast/pic/res_fb/res_fb_localization",AssetBundleData.getBundleName(BundleType.FarmBlast)));
            return sprites.ToArray();
        }
    }
}
