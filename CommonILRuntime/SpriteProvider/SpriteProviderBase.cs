using UnityEngine;
using System.Collections.Generic;
using Services;

namespace CommonILRuntime.SpriteProvider
{
    public class SpriteProviderBase : ISpriteProvider
    {
        Dictionary<string, Sprite> spriteDict = new Dictionary<string, Sprite>();

        public Sprite getSprite(string name)
        {
            if (spriteDict.Count <= 0)
            {
                spriteDict = UtilServices.spritesToDictionary(loadSpriteArray());
            }

            Sprite result = null;
            if (!spriteDict.TryGetValue(name, out result))
            {
                Debug.LogError($"SpriteProvider get {name} Sprite is null,check path or name");
            }

            return result;
        }

        public virtual Sprite[] loadSpriteArray()
        {
            return new Sprite[] { };
        }
    }
}
