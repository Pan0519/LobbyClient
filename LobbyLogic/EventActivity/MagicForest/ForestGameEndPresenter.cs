using UnityEngine;
using UnityEngine.UI;
using Lobby.Jigsaw;
using System;
using CommonService;
using CommonILRuntime.Module;
using CommonILRuntime.Services;
using CommonPresenter.PackItem;
using LobbyLogic.NetWork.ResponseStruct;
using LobbyLogic.Audio;
using Lobby.Audio;
using CommonPresenter;

namespace MagicForest
{
    class ForestGameEndPresenter : SystemUIBasePresenter
    {
        public override string objPath => $"{ForestDataServices.prefabPath}/mf_game_end";
        public override UiLayer uiLayer { get => UiLayer.GameMessage; }

        #region UIs
        Animator rewardAnim;
        RectTransform packGroupRect;
        RectTransform rewardLayoutRect;
        Text rewardTxt;
        Button collectBtn;
        #endregion

        ulong coinAmount;
        string packID;
        public Action mgCloseEvent;
        public override void initContainerPresenter()
        {
            resOrder = new string[] { AssetBundleData.getBundleName(BundleType.MagicForest) };
            base.initContainerPresenter();
        }
        public override void initUIs()
        {
            rewardAnim = getAnimatorData("reward_anim");
            packGroupRect = getRectData("pack_group");
            rewardLayoutRect = getRectData("reward_layout");
            collectBtn = getBtnData("btn_collect");
            rewardTxt = getTextData("reward_txt");
        }

        public override void init()
        {
            base.init();
            collectBtn.onClick.AddListener(collectClick);
        }

        public void openGameReward(MagicForestBossData bossData, MagicForestBossPlayResponse response)
        {
            ForestDataServices.isReduceMainBGM(true);
            AudioManager.instance.playAudioOnce(AudioPathProvider.getAudioPath(ActivityBlastAudio.BigWin));
            collectBtn.interactable = true;
            ActivityReward[] completeItem = bossData.CompleteItem;
            coinAmount = bossData.getCompleteReward;
            packID = response.RewardPackId;
            for (int i = 0; i < completeItem.Length; ++i)
            {
                PackItemPresenterServices.getSinglePackItem(completeItem[i].Type, packGroupRect);
            }
            rewardTxt.text = coinAmount.ToString("N0");
            LayoutRebuilder.ForceRebuildLayoutImmediate(rewardLayoutRect);
        }

        void collectClick()
        {
            collectBtn.interactable = false;
            var sourceVal = DataStore.getInstance.playerInfo.playerMoney;
            CoinFlyHelper.frontSFly(collectBtn.GetComponent<RectTransform>(), sourceVal, sourceVal + coinAmount, onComplete: coinFlyFinish);
        }

        void coinFlyFinish()
        {
            DataStore.getInstance.playerInfo.myWallet.unsafeAdd(coinAmount);
            if (!string.IsNullOrEmpty(packID))
            {
                OpenPackWildProcess.openPackWildFromID(packID, closePresenter);
                return;
            }

            closePresenter();
        }

        public override void closePresenter()
        {
            if (null != mgCloseEvent)
            {
                mgCloseEvent();
            }
            base.closePresenter();
        }

        public override Animator getUiAnimator()
        {
            return rewardAnim;
        }

        public override void animOut()
        {
            ForestDataServices.isReduceMainBGM(false);
            clear();
        }
    }
}
