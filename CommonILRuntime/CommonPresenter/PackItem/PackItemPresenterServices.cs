using UnityEngine;
using Common.Jigsaw;
using System.Collections.Generic;
using CommonILRuntime.BindingModule;

namespace CommonPresenter.PackItem
{
    public static class PackItemPresenterServices
    {
        public static List<PackItemNodePresenter> getPickItems(int itemCount, RectTransform parent, float scale = 1)
        {
            List<PackItemNodePresenter> result = new List<PackItemNodePresenter>();
            for (int i = 0; i < itemCount; ++i)
            {
                result.Add(getPackItem(parent, scale));
            }
            return result;
        }

        public static List<PackItemNodePresenter> getPickItems(List<long> packIDs, RectTransform parent, float scale = 1)
        {
            List<PackItemNodePresenter> result = getPickItems(packIDs.Count, parent, scale);
            for (int i = 0; i < result.Count; ++i)
            {
                result[i].showPackImg((PuzzlePackID)packIDs[i]);
            }
            return result;
        }

        public static PackItemNodePresenter getSinglePackItem(string packID, RectTransform parent, float scale = 1)
        {
            long id;
            if (long.TryParse(packID, out id))
            {
                return getSinglePackItem(id, parent, scale);
            }

            return null;
        }

        public static PackItemNodePresenter getSinglePackItem(long packID, RectTransform parent, float scale = 1)
        {
            PackItemNodePresenter result = getPackItem(parent, scale);
            result.showPackImg((PuzzlePackID)packID);
            return result;
        }

        static PackItemNodePresenter getPackItem(RectTransform parent, float scale)
        {
            PackItemNodePresenter packItem;
            var item = ResourceManager.instance.getObjectFromPool($"prefab/activity/activity_item_common/pack_item", parent).gameObject;
            item.name = "pack_item";
            packItem = UiManager.bindNode<PackItemNodePresenter>(item);
            var localScale = packItem.uiRectTransform.localScale;
            localScale.Set(scale, scale, scale);
            packItem.uiRectTransform.localScale = localScale;
            return packItem;
        }
    }
}
