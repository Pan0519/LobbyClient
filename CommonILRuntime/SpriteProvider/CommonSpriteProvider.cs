using System.Collections.Generic;
using UnityEngine;

namespace CommonILRuntime.SpriteProvider
{
    public class CommonSpriteProvider
    {
        static CommonSpriteProvider _instance = new CommonSpriteProvider();
        public static CommonSpriteProvider instance { get { return _instance; } }

        Dictionary<CommonSpriteType, ISpriteProvider> providers = new Dictionary<CommonSpriteType, ISpriteProvider>();

        public Sprite getSprite<T>(CommonSpriteType spriteType, string spriteName) where T : ISpriteProvider, new()
        {
            if (string.IsNullOrEmpty(spriteName))
            {
                Debug.LogError($"get {spriteType} Sprite Name IsNullOrEmpty");
                return null;
            }

            return getSpriteProvider<T>(spriteType).getSprite(spriteName);
        }

        public ISpriteProvider getSpriteProvider<T>(CommonSpriteType spriteType) where T : ISpriteProvider, new()
        {
            ISpriteProvider spriteProvider = null;
            if (!providers.TryGetValue(spriteType, out spriteProvider))
            {
                spriteProvider = new T();
                providers.Add(spriteType, spriteProvider);
            }

            return (T)spriteProvider;
        }
    }

    public enum CommonSpriteType
    {
        PurchaseInfo,
        Topbar,
        CommonButton,
        ExtraGameBoard
    }
}
