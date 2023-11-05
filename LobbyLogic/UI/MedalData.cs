using System;
using System.Collections.Generic;
using UnityEngine;
using Shop;

namespace Lobby
{
    public static class MedalData
    {
        public static List<MedalState> medalStates = new List<MedalState>();

        public static void addMedalState(MedalState state)
        {
            medalStates.Add(state);
        }

        public static Sprite getMedalStateSprite(int id)
        {
            return ShopDataStore.getShopSprite($"medal_{Enum.GetName(typeof(MedalState), MedalData.medalStates[id]).ToLower()}");
        }
    }

    public enum MedalState
    {
        Gold,
        Silver,
    }
}
