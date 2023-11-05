using UnityEngine;
using System.Collections.Generic;

namespace Lobby.Common
{
    class CasinoCrushSpriteProvider : EventActivitySpriteProvider
    {
        public override Sprite[] loadSpriteArray()
        {
            List<Sprite> sprites = new List<Sprite>();
            sprites.AddRange(ResourceManager.instance.loadAll("prefab/activity/rookie/pic/res_rookie/res_rookie"));
            sprites.AddRange(base.loadSpriteArray());
            return sprites.ToArray();
        }
    }
}
