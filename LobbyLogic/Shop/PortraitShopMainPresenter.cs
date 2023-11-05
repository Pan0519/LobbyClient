using System.Collections.Generic;
using CommonILRuntime.BindingModule;
using LobbyLogic.NetWork.ResponseStruct;
using UnityEngine;
using System;

namespace Shop
{
    class PortraitShopMainPresenter : ShopMainPresenter
    {
        public override string objPath => "prefab/lobby_shop/shop_main_portrait";

        GameObject portraitGroupObj;

        Queue<PoolObject> dividerPools = new Queue<PoolObject>();
        List<PoolObject> portraitGroups = new List<PoolObject>();

        public override void initUIs()
        {
            base.initUIs();
            portraitGroupObj = getGameObjectData("portrait_group");
        }

        public override void showShopItems(StoreProduct[] products)
        {
            base.showShopItems(products);
            var storeKinds = Enum.GetValues(typeof(StoreKind)).GetEnumerator();
            while (storeKinds.MoveNext())
            {
                StoreKind kind = (StoreKind)storeKinds.Current;
                if (StoreKind.Divider == kind)
                {
                    continue;
                }
                List<PoolObject> pools;
                if (!itemPools.TryGetValue(kind, out pools))
                {
                    continue;
                }

                if (pools.Count <= 0)
                {
                    continue;
                }

                if (StoreKind.Item == kind)
                {
                    getDivider().cachedRectTransform.SetAsLastSibling();
                }
                for (int i = 0; i < pools.Count; i += 2)
                {
                    var portraitGroup = ResourceManager.instance.getObjectFromPool(portraitGroupObj, getScrollContent());
                    portraitGroups.Add(portraitGroup);
                    pools[i].cachedRectTransform.SetParent(portraitGroup.cachedRectTransform);
                    int nextID = (i + 1);
                    if (nextID > pools.Count - 1)
                    {
                        break;
                    }
                    pools[nextID].cachedRectTransform.SetParent(portraitGroup.cachedRectTransform);
                }
            }
        }

        public override void returnPoolItems()
        {
            base.returnPoolItems();
            for (int i = 0; i < portraitGroups.Count; ++i)
            {
                ResourceManager.instance.returnObjectToPool(portraitGroups[i].cachedGameObject);
            }
        }

        PoolObject getDivider()
        {
            if (null == dividerPools)
            {
                List<PoolObject> pools;
                itemPools.TryGetValue(StoreKind.Divider, out pools);
                for (int i = 0; i < pools.Count; ++i)
                {
                    dividerPools.Enqueue(pools[i]);
                }
            }
            return dividerPools.Dequeue();
        }
    }
}
