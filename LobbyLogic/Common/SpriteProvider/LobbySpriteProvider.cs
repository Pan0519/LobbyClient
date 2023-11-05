using System.Collections.Generic;
using UnityEngine;
using CommonILRuntime.SpriteProvider;

namespace Lobby.Common
{
    public class LobbySpriteProvider
    {
        static LobbySpriteProvider _instance = new LobbySpriteProvider();
        public static LobbySpriteProvider instance { get { return _instance; } }

        Dictionary<LobbySpriteType, ISpriteProvider> spriteProviders = new Dictionary<LobbySpriteType, ISpriteProvider>();

        public Sprite getSprite<T>(LobbySpriteType spriteType, string spriteName) where T : ISpriteProvider, new()
        {
            if (string.IsNullOrEmpty(spriteName))
            {
                Debug.LogError($"get {spriteType} Sprite Name IsNullOrEmpty");
                return null;
            }
            return getSpriteProvider<T>(spriteType).getSprite(spriteName);
        }

        public T getSpriteProvider<T>(LobbySpriteType spriteType) where T : ISpriteProvider, new()
        {
            ISpriteProvider spriteProvider = null;
            if (!spriteProviders.TryGetValue(spriteType, out spriteProvider))
            {
                spriteProvider = new T();
                spriteProviders.Add(spriteType, spriteProvider);
            }

            return (T)spriteProvider;
        }

        public void clearAllSpriteProviders()
        {
            spriteProviders.Clear();
        }
    }

    public enum LobbySpriteType
    {
        Shop,
        EventActivity,
        FarmBlast,
        MagicForest,
        CasinoCrush,
        FrenzyJourney,
        LobbyItem,
        Mission,
        SaveTheDog,
        RewardItem,
        ActivityQuest,
    }
}
