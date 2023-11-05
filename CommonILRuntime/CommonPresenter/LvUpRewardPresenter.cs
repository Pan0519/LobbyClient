using CommonILRuntime.Module;
using UnityEngine.UI;
using UnityEngine;
using System.Collections.Generic;
using CommonService;
using Services;
using CommonILRuntime.BindingModule;
using UniRx;
using UniRx.Triggers;
using System;
using LobbyLogic.Audio;

namespace CommonPresenter
{
    class LvUpRewardPresenter : SystemUIBasePresenter
    {
        public override string objPath => "Prefab/lv_up/level_up_result";
        public override UiLayer uiLayer { get => UiLayer.GameMessage; }

        #region UIs
        Button collectBtn;
        Text lvTxt;
        Text rewardMoneyTxt;
        //Binding.BindingNode itemNode;
        RectTransform itemGroupTrans;
        RectTransform moneyGroupTrans;
        Animator lvupAnim;
        RectTransform scaleGroup;
        #endregion

        public override void initUIs()
        {
            collectBtn = getBtnData("btn_collect");
            lvTxt = getTextData("newlv_txt");
            rewardMoneyTxt = getTextData("money_txt");
            moneyGroupTrans = getRectData("money_group_trans");
            //itemNode = getNodeData("lvup_item_node");
            lvupAnim = getAnimatorData("lvup_anim");
            itemGroupTrans = getBindingData<RectTransform>("item_group_trans");
            scaleGroup = getRectData("scale_group_rect");
        }

        public override Animator getUiAnimator()
        {
            return lvupAnim;
        }

        public override async void init()
        {
            base.init();
            var nowGameOrientation = await DataStore.getInstance.dataInfo.getNowGameOrientation();
            float orientationScale = (GameOrientation.Landscape == nowGameOrientation) ? 1 : 0.8f;
            var scale = scaleGroup.localScale;
            scale.Set(orientationScale, orientationScale, orientationScale);
            scaleGroup.localScale = scale;

            //itemNode.cachedGameObject.setActiveWhenChange(false);
            collectBtn.onClick.AddListener(closeBtnClick);
        }

        public void setRewardPack(Dictionary<PurchaseItemType, Pack> rewards)
        {
            DataStore.getInstance.gameTimeManager.Pause();
            lvTxt.text = DataStore.getInstance.playerInfo.level.ToString();
            var rewardEnum = rewards.GetEnumerator();
            while (rewardEnum.MoveNext())
            {
                if (PurchaseItemType.Coin == rewardEnum.Current.Key)
                {
                    rewardMoneyTxt.text = rewardEnum.Current.Value.outcome.amount.ToString("N0");
                    continue;
                }
                PurchaseMappingData mappingData = PurchaseInfo.getMappingData(rewardEnum.Current.Key);
                if (null == mappingData)
                {
                    continue;
                }

                var poolObj = GameObject.Instantiate(ResourceManager.instance.getGameObject("prefab/lv_up/lv_up_item"), itemGroupTrans);
                UiManager.bindNode<itemNode>(poolObj).setItemData(rewardEnum.Current.Value.outcome.getAmount(), PurchaseInfo.getPuraseSprite(mappingData.spriteName));
                poolObj.setActiveWhenChange(true);
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(moneyGroupTrans);
            open();
            AudioManager.instance.playAudioOnce(CommonAudioPathProvider.getAudioPath(MainGameCommonSound.LvUpBig));
        }

        public override void animOut()
        {
            DataStore.getInstance.gameTimeManager.Resume();
            clear();
        }
    }

    class itemNode : NodePresenter
    {
        Text itemNumTxt;
        Image iconImg;

        public override void initUIs()
        {
            itemNumTxt = getTextData("item_num_txt");
            iconImg = getImageData("item_icon_img");
        }

        public itemNode setItemData(ulong num, Sprite iconSprite)
        {
            itemNumTxt.text = $"x{num}";
            iconImg.sprite = iconSprite;
            return this;
        }
    }
}
