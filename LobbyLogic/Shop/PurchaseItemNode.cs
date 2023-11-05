using CommonILRuntime.Module;
using UnityEngine;
using UnityEngine.UI;
using Services;
using CommonPresenter.PackItem;

namespace Shop
{
    class PurchaseItemNode : NodePresenter
    {
        #region UIs
        Image itemImg;
        Text itemName;
        Text itemInfo;
        RectTransform packRoot;
        #endregion

        public override void initUIs()
        {
            itemImg = getImageData("item_img");
            itemName = getTextData("item_name");
            itemInfo = getTextData("item_info");
            packRoot = getRectData("pack_item_root");
        }

        public void showItem(PurchaseInfoData infoData)
        {
            bool isPuzzlePack = PurchaseItemType.PuzzlePack == infoData.itemKind;
            itemImg.gameObject.setActiveWhenChange(!isPuzzlePack);
            packRoot.gameObject.setActiveWhenChange(isPuzzlePack);
            if (!isPuzzlePack)
            {
                itemImg.sprite = infoData.iconSprite;
            }
            else
            {
                PackItemPresenterServices.getSinglePackItem(infoData.getPuzzleID(), packRoot);
            }
            itemName.text = LanguageService.instance.getLanguageValue($"{infoData.titleKey}_Unit");
            itemInfo.text = infoData.num.ToString();
            open();
        }
    }
}
