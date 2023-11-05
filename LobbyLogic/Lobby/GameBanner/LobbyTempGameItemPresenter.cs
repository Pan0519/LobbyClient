using Debug = UnityLogUtility.Debug;
using CommonILRuntime.Module;
using UnityEngine.UI;
using UnityEngine;
using Lobby.Common;

namespace Lobby
{
    class LobbyTempGameItemPresenter : NodePresenter
    {
        #region BindingField
        Image gameIcon;
        GameObject commingSoonObj;
        #endregion

        LobbyItemSpriteProvider itemSpriteProvider;

        public override void initUIs()
        {
            gameIcon = getImageData("game_icon");
            commingSoonObj = getGameObjectData("comming_soon_obj");
        }

        public override void init()
        {
            itemSpriteProvider = LobbySpriteProvider.instance.getSpriteProvider<LobbyItemSpriteProvider>(LobbySpriteType.LobbyItem);
        }

        public bool setGameInfo(LobbyGameInfo gameInfo)
        {
            commingSoonObj.setActiveWhenChange(!gameInfo.isOpen);
            return setGameSprite(gameInfo.gameID);
        }

        bool setGameSprite(string gameID)
        {
            Sprite gameSprite = itemSpriteProvider.getSprite($"game_{gameID}");
            if (null == gameSprite)
            {
                close();
                return false;
            }
            gameIcon.sprite = gameSprite;
            return true;
        }
    }
}
