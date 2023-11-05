using Common.Jigsaw;
using CommonILRuntime.Module;
using CommonILRuntime.Services;
using CommonService;
using CommonPresenter;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace Lobby.Jigsaw
{
    public class JjigsawCompleteBoard : SystemUIBasePresenter
    {
        public override string objPath => "prefab/lobby_puzzle/puzzle_collect_finish";
        public override UiLayer uiLayer { get { return UiLayer.System; } }

        Image albumCoverImage;
        GameObject bookObject;
        GameObject albumCompleteIconObject;
        GameObject allCompleteObject;
        Text coinText;
        RectTransform coinLabelGroupRectTrans;
        Button collectButton;

        string albumId;
        Action closeCallback = null;
        public override void initContainerPresenter()
        {
            resOrder = new string[] { AssetBundleData.getBundleName(BundleType.LobbyPuzzle)};
            base.initContainerPresenter();
        }
        public override void initUIs()
        {
            base.initUIs();

            albumCoverImage = getImageData("albumCoverImage");
            bookObject = getGameObjectData("bookObj");
            albumCompleteIconObject = getGameObjectData("albumCompleteIconObject");
            allCompleteObject = getGameObjectData("allCompleteObject");
            coinText = getTextData("coinText");
            coinLabelGroupRectTrans = getBindingData<RectTransform>("coinLabelGroupRectTrans");
            collectButton = getBtnData("collectButton");
        }

        public override void init()
        {
            base.init();
            collectButton.onClick.RemoveAllListeners();
            collectButton.onClick.AddListener(onCollectClick);
            collectButton.interactable = true;
            allCompleteObject.setActiveWhenChange(false);
            coinText.text = "";
        }

        public override void open()
        {
            base.open();
            closeCallback = null;
        }

        public override void animOut()
        {
            clear();
            closeCallback?.Invoke();    // 通知此UI已關閉
        }

        public void setData(ulong rewardCoin, string albumId, Action closeUICallback)
        {
            this.albumId = albumId;
            closeCallback = closeUICallback;
            coinText.text = rewardCoin.ToString("N0");
            bool seasonComplete = albumId.Length <= 3;
            Debug.Log($"seasonComplete? {seasonComplete} ,albumId : {albumId}");
            //整季完成
            allCompleteObject.setActiveWhenChange(seasonComplete);

            //單本完成
            bookObject.setActiveWhenChange(!seasonComplete);
            albumCoverImage.gameObject.setActiveWhenChange(!seasonComplete);
            albumCompleteIconObject.setActiveWhenChange(!seasonComplete);

            if (!seasonComplete)
            {
                albumCoverImage.sprite = JigsawCoverSpriteProvider.getAlbumCover(albumId);
            }
        }

        async void onCollectClick()
        {
            collectButton.interactable = false;
            var coinOutcome = await JigsawReward.redeemReward(albumId);
            if (null != coinOutcome)
            {
                var sourceValue = DataStore.getInstance.playerInfo.myWallet.deprecatedCoin;
                var targetValue = DataStore.getInstance.playerInfo.playerMoney;

                CoinFlyHelper.frontSFly((RectTransform)collectButton.transform, sourceValue, targetValue,
                    onComplete: () =>
                    {
                        coinOutcome.apply();
                        closePresenter();
                    });
                return;
            }
            closePresenter();
        }
    }
}
