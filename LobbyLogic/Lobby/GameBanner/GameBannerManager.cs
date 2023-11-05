using System.Collections.Generic;
using UnityEngine;

namespace Lobby
{
    static class GameBannerManager
    {
        static Dictionary<string, GameObject> originalObj = new Dictionary<string, GameObject>();
        public static GameObject getBannerItem(string gameID)
        {
            GameObject result = null;

            if (originalObj.TryGetValue(gameID, out result))
            {
                return result;
            }

            result = ResourceManager.instance.getGameObject($"prefab/lobby_game_banner/game_{gameID}/game_{gameID}_long");
            if (null != result && !originalObj.ContainsKey(gameID))
            {
                originalObj.Add(gameID, result);
            }

            return result;
        }
    }
}
