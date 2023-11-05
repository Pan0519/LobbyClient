using UnityEngine;
using System.Collections.Generic;

namespace Lobby.Common
{
    class ForestSpriteProvider : EventActivitySpriteProvider
    {
        public override Sprite[] loadSpriteArray()
        {
            List<Sprite> sprites = new List<Sprite>();
            sprites.AddRange(base.loadSpriteArray());
            sprites.AddRange(ResourceManager.instance.loadAllWithResOrder("prefab/activity/magic_forest/pic/res_mf/res_mf",AssetBundleData.getBundleName(BundleType.MagicForest)));
            return sprites.ToArray();
        }
    }
}
