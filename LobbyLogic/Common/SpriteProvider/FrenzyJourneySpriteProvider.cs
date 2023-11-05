using CommonILRuntime.SpriteProvider;
using UnityEngine;
using System.Collections.Generic;
using Services;

namespace Lobby.Common
{
    class FrenzyJourneySpriteProvider : EventActivitySpriteProvider
    {
        public override Sprite[] loadSpriteArray()
        {
            List<Sprite> sprites = new List<Sprite>();
            sprites.AddRange(base.loadSpriteArray());
            sprites.AddRange(ResourceManager.instance.loadAllWithResOrder("prefab/lobby_puzzle/pic/puzzle_pack/puzzle_pack",AssetBundleData.getBundleName(BundleType.LobbyPuzzle)));
            sprites.AddRange(ResourceManager.instance.loadAll(UtilServices.getLocalizationAltasPath("res_fj")));
            return sprites.ToArray();
        }
    }
}
