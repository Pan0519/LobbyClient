using UnityEngine;

namespace CommonILRuntime.SpriteProvider
{
    public class PurchaseInfoProvider : SpriteProviderBase
    {
        public override Sprite[] loadSpriteArray()
        {
            return ResourceManager.instance.loadAll($"prefab/lobby_shop/pic/res_purchase_item/res_purchase_item");
        }
    }
}
