using CommonILRuntime.BindingModule;
using CommonILRuntime.Module;
using UnityEngine.UI;
using UnityEngine;
using System;
using System.Collections.Generic;
using Lobby;
using Lobby.Common;
using Services;
using CommonILRuntime.Outcome;
using EventActivity;
using Lobby.Jigsaw;
using CommonPresenter;
using CommonService;

namespace Shop
{
    public class PurchasePagePresenter : SystemUIBasePresenter
    {
        public override string objPath
        {
            get
            {
                return UtilServices.getOrientationObjPath("prefab/lobby_shop/purchase_successful");
            }
        }
        public override UiLayer uiLayer { get => UiLayer.System; }

        #region UIs
        Button okBtn;
        ScrollRect rewardScroll;
        Text numTxt;
        Animator rewardAnim;
        GameObject moneyGroupObj;
        RectTransform moneyGroupRect;
        GameObject vaultClubObj;
        Text vaultDaysTxt;
        GameObject activityBoosterObj;
        Text activityBoosterNumTxt;
        Image activityBoosterImg;
        #endregion

        List<PurchaseInfoData> infoDatas;
        ulong totalNum = 0;
        List<Outcome> outcomes = new List<Outcome>();
        PurchaseItemType mainPurchaseType = PurchaseItemType.None;
        Dictionary<PurchaseItemType, Action> mainPurchaseEvent = new Dictionary<PurchaseItemType, Action>();
        Action finishCB;
        string boosterUnit;
        bool hasPackItem;
        CommonReward[] packRewards;
        public override void initUIs()
        {
            okBtn = getBtnData("ok_btn");
            rewardScroll = getBindingData<ScrollRect>("reward_scroll");
            numTxt = getTextData("money_txt");
            rewardAnim = getAnimatorData("reward_anim");
            moneyGroupObj = getGameObjectData("money_group");
            vaultClubObj = getGameObjectData("vault_club");
            vaultDaysTxt = getTextData("vault_num_txt");
            activityBoosterObj = getGameObjectData("activity_boost");
            activityBoosterNumTxt = getTextData("booster_num_txt");
            activityBoosterImg = getImageData("activity_booter_img");
            moneyGroupRect = moneyGroupObj.GetComponent<RectTransform>();
        }
        public override void init()
        {
            base.init();
            initPurchaseMainEvent();
            okBtn.onClick.AddListener(closeBtnClick);
        }
        void initPurchaseMainEvent()
        {
            mainPurchaseEvent.Add(PurchaseItemType.Coin, coinTxtEvent);
            mainPurchaseEvent.Add(PurchaseItemType.HighRollerVault, vaultDaysTxtEvent);
            mainPurchaseEvent.Add(PurchaseItemType.ActivityBoost, showActivityPorp);
        }
        public override Animator getUiAnimator()
        {
            return rewardAnim;
        }

        public override void animOut()
        {
            if (hasPackItem)
            {
                OpenPackWildProcess.openPackWild(packRewards, closeGiftPresenter);
            }
            else
            {
                closeGiftPresenter();
            }

            for (int i = 0; i < outcomes.Count; i++)
            {
                outcomes[i].apply();
            }
        }

        public void openPurchase(CommonReward[] rewards, Action finishCallback = null)
        {
            outcomes.Clear();
            infoDatas = PurchaseInfoCover.rewardConvertToPurchase(rewards);
            hasPackItem = false;
            finishCB = finishCallback;
            totalNum = 0;
            packRewards = rewards;
            infoDatas = PurchaseInfoCover.stackPurchaseInfos(infoDatas);

            for (int i = 0; i < infoDatas.Count; ++i)
            {
                var info = infoDatas[i];
                outcomes.Add(Outcome.process(info.outcomeObj));
                if (mainPurchaseEvent.ContainsKey(info.itemKind))
                {
                    mainPurchaseType = info.itemKind;
                    totalNum = info.num;
                    continue;
                }
                if (PurchaseItemType.PuzzlePack == info.itemKind)
                {
                    hasPackItem = true;
                }

                parseExceptionItemData(info);
                var item = ResourceManager.instance.getObjectFromPool("prefab/lobby_shop/purchase_item", rewardScroll.content.transform);
                var itemNode = UiManager.bindNode<PurchaseItemNode>(item.cachedGameObject);
                itemNode.showItem(info);
            }

            moneyGroupObj.setActiveWhenChange(PurchaseItemType.Coin == mainPurchaseType);
            vaultClubObj.setActiveWhenChange(PurchaseItemType.HighRollerVault == mainPurchaseType);
            activityBoosterObj.setActiveWhenChange(PurchaseItemType.ActivityBoost == mainPurchaseType);
            Action mainEvent;
            if (mainPurchaseEvent.TryGetValue(mainPurchaseType, out mainEvent))
            {
                mainEvent();
                LayoutRebuilder.ForceRebuildLayoutImmediate(moneyGroupRect);
            }
            open();
        }
        PurchaseInfoData parseExceptionItemData(PurchaseInfoData infoData)
        {
            switch (infoData.itemKind)
            {
                case PurchaseItemType.ActivityProp:
                    string propName = ActivityDataStore.getActivityPurchaseItemName(infoData.type);
                    infoData.setIconSprite(propName);
                    infoData.setTitleKey($"Store_SeeMore_{propName.toTitleCase()}");
                    break;
            }

            return infoData;
        }

        public void setActivityBoosterData(BoosterType boosterType, string unit)
        {
            string spriteName = ActivityDataStore.getBoosterSpriteName(boosterType);
            activityBoosterImg.sprite = LobbySpriteProvider.instance.getSprite<EventActivitySpriteProvider>(LobbySpriteType.EventActivity, $"activity_{spriteName}");
            boosterUnit = unit;
        }

        void coinTxtEvent()
        {
            updateCoinNumTxt(totalNum);
        }

        void vaultDaysTxtEvent()
        {
            vaultDaysTxt.text = $"{(totalNum / 1440)}{LanguageService.instance.getLanguageValue("Time_Days")}";
        }

        void showActivityPorp()
        {
            activityBoosterNumTxt.text = $"{totalNum} {boosterUnit}";
            updateCoinNumTxt(infoDatas.Find(info => info.itemKind == PurchaseItemType.Coin).num);
            moneyGroupObj.setActiveWhenChange(true);
        }
        void updateCoinNumTxt(ulong coinNum)
        {
            numTxt.text = coinNum.ToString("N0");
        }

        void closeGiftPresenter()
        {
            //bool isGetMedal = infoDatas.Find(purchItem => purchItem.IsMedal) != null;
            //if (isGetMedal)
            //{
            //    for (int i = 0; i < infoDatas.Count; ++i)
            //    {
            //        switch (infoDatas[i].itemKind)
            //        {
            //            case PurchaseItemType.MedalGold:
            //                MedalData.addMedalState(MedalState.Gold);
            //                break;
            //            case PurchaseItemType.MedalSilver:
            //                MedalData.addMedalState(MedalState.Silver);
            //                break;
            //        }
            //    }

            //    UiManager.getPresenter<ShopGiftPresenter>().open();
            //}
            clear();
        }

        public override void clear()
        {
            if (null != finishCB)
            {
                finishCB();
            }

            if (!DataStore.getInstance.playerInfo.isBindFB)
            {
                OpenMsgBoxService.Instance.openNormalBox(title: LanguageService.instance.getLanguageValue("Tips_BindAccount"), content: LanguageService.instance.getLanguageValue("Tips_AccountLost"));
            }

            base.clear();
        }
    }
}
