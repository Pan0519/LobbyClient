using CommonILRuntime.Module;
using CommonPresenter.PackItem;
using CommonPresenter;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CommonService;
using CommonILRuntime.Services;

namespace Lobby.Jigsaw
{
    public class FantasyWheelResultPresenter : SystemUIBasePresenter
    {
        public override string objPath => "prefab/lobby_puzzle/fantasy_wheel_result";
        public override UiLayer uiLayer { get { return UiLayer.System; } }

        public Action closeListener = null;

        Text coinText;
        Button collectButton;
        RectTransform rewardGroupRect;
        GameObject packGroup;
        RectTransform packItemRoot;
        Animator uiAnimator;
        ulong rewardCoin;
        public override void initContainerPresenter()
        {
            resOrder = new string[] { AssetBundleData.getBundleName(BundleType.LobbyPuzzle) };
            base.initContainerPresenter();
        }
        public override void initUIs()
        {
            base.initUIs();
            coinText = getTextData("coinText");
            collectButton = getBtnData("collectButton");
            rewardGroupRect = getBindingData<RectTransform>("rewardGroupRect");
            packGroup = getGameObjectData("packGroup");
            packItemRoot = getBindingData<RectTransform>("packItemRoot");
            uiAnimator = getAnimatorData("uiAnimator");
        }

        public override Animator getUiAnimator()
        {
            return uiAnimator;
        }

        public override void init()
        {
            base.init();
            collectButton.onClick.AddListener(collectBtnClick);
        }

        public override void animOut()
        {
            clear();
            closeListener?.Invoke();
        }

        public void setReward(ulong coin, string packId = null)
        {
            collectButton.interactable = true;
            rewardCoin = coin;
            coinText.text = coin.ToString("N0");
            packGroup.setActiveWhenChange(!string.IsNullOrEmpty(packId));

            //Add Pack
            if (!string.IsNullOrEmpty(packId))
            {
                var id = long.Parse(packId);
                var packIds = new List<long>() { id };
                PackItemPresenterServices.getPickItems(packIds, packItemRoot);
            }

            //alignment
            rebuildLayout();
        }

        void collectBtnClick()
        {
            collectButton.interactable = false;
            CoinFlyHelper.frontSFly(collectButton.GetComponent<RectTransform>(), DataStore.getInstance.playerInfo.playerMoney, DataStore.getInstance.playerInfo.playerMoney + rewardCoin, onComplete: () =>
              {
                  DataStore.getInstance.playerInfo.myWallet.unsafeAdd(rewardCoin);
                  closePresenter();
              });
        }

        /// <summary>
        /// 調整版面
        /// </summary>
        void rebuildLayout()
        {
            //迫使總獎勵的金幣&文字重新排版對齊
            LayoutRebuilder.ForceRebuildLayoutImmediate(rewardGroupRect);
        }
    }
}
