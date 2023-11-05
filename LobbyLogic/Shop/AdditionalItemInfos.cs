using CommonILRuntime.BindingModule;
using CommonILRuntime.Module;
using Binding;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Services;
using CommonPresenter.PackItem;
using CommonService;
using LobbyLogic.Audio;

namespace Shop
{
    class AdditionalItemInfos : ContainerPresenter
    {
        public override string objPath => "prefab/lobby_shop/additional_items_info";
        public override UiLayer uiLayer { get { return UiLayer.TopRoot; } }

        #region UIs
        Button closeBtn;
        ScrollRect itemGroupRect;
        RectTransform infoLayout;
        GameObject dividerLine;
        BindingNode itemInfoNode;
        #endregion

        List<PoolObject> itemPools = new List<PoolObject>();

        public override void initUIs()
        {
            closeBtn = getBtnData("close_btn");
            itemGroupRect = getBindingData<ScrollRect>("item_group_trans");
            itemInfoNode = getNodeData("item_info");
            infoLayout = getRectData("info_layout");
            dividerLine = getGameObjectData("line_obj");
        }

        public override void init()
        {
            itemInfoNode.cachedGameObject.setActiveWhenChange(false);
            infoLayout.gameObject.setActiveWhenChange(false);
            dividerLine.setActiveWhenChange(false);
            closeBtn.onClick.AddListener(clear);
        }

        public AdditionalItemInfos openItemInfos(List<PurchaseInfoData> infoDatas)
        {
            infoDatas = PurchaseInfoCover.stackPurchaseInfos(infoDatas);
            GameObject infoLayoutPool = null;
            for (int i = 0; i < infoDatas.Count; ++i)
            {
                if ((i + 1) % 3 == 0)
                {
                    var dividerLinePool = getPoolObj(dividerLine, itemGroupRect.content);
                }
                if (i % 2 == 0)
                {
                    infoLayoutPool = getPoolObj(infoLayout.gameObject, itemGroupRect.content);
                }
                GameObject item = getPoolObj(itemInfoNode.cachedGameObject, infoLayoutPool.transform);
                UiManager.bindNode<ItemInfoNode>(item).setItemInfo(infoDatas[i]);
            }
            open();
            return this;
        }

        GameObject getPoolObj(GameObject originalObj, Transform parent)
        {
            GameObject poolObj = GameObject.Instantiate<GameObject>(originalObj, parent);
            poolObj.setActiveWhenChange(true);
            return poolObj;
        }

        public override void clear()
        {
            AudioManager.instance.playAudioOnce(CommonAudioPathProvider.getAudioPath(BasicCommonSound.InfoBtn));
            base.clear();
        }
    }

    public class ItemInfoNode : NodePresenter
    {
        #region UIs
        public Image itemImg;
        public Text itemTitle;
        public Text itemNum;
        RectTransform packItemRect;
        #endregion

        public override void initUIs()
        {
            itemImg = getImageData("item_img");
            itemTitle = getTextData("item_title");
            itemNum = getTextData("item_info");
            packItemRect = getRectData("pack_dummy");
        }

        public ItemInfoNode setItemInfo(PurchaseInfoData infoData)
        {
            if (!string.IsNullOrEmpty(infoData.titleKey))
            {
                string unitKey = $"{infoData.titleKey}_Unit";
                itemNum.text = $"+{infoData.num} {LanguageService.instance.getLanguageValue(unitKey)}";
                itemTitle.text = LanguageService.instance.getLanguageValue(infoData.titleKey);
            }

            itemImg.sprite = infoData.iconSprite;
            itemImg.gameObject.setActiveWhenChange(PurchaseItemType.PuzzlePack != infoData.itemKind);
            if (PurchaseItemType.PuzzlePack == infoData.itemKind)
            {
                PackItemPresenterServices.getSinglePackItem(infoData.getPuzzleID(), packItemRect);
            }

            open();
            return this;
        }
    }
}


