using UnityEngine;

namespace CommonILRuntime.SpriteProvider
{
    class TopBarSpriteProvider : SpriteProviderBase
    {
        public override Sprite[] loadSpriteArray()
        {
            return ResourceManager.instance.loadAll("Bar_Resources/pic/res_lobby_top_ui/res_lobby_top_ui");
        }
    }
}
