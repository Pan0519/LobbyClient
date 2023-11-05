using UnityEngine;
using Services;
using System.Collections.Generic;

namespace CommonILRuntime.SpriteProvider
{
    class BtnLocalizationProvider : SpriteProviderBase
    {
        public override Sprite[] loadSpriteArray()
        {
            List<Sprite> sprite = new List<Sprite>();
            sprite.AddRange(ResourceManager.instance.loadAll(UtilServices.getLocalizationAltasPath("btn")));
            return sprite.ToArray();
        }
    }
}
