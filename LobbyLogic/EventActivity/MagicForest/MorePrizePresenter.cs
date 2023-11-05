using UnityEngine;
using UnityEngine.UI;
using EventActivity;
using CommonPresenter;
using System;
using CommonILRuntime.Module;
using LobbyLogic.Audio;
using Lobby.Audio;

namespace MagicForest
{
    public class MorePrizePresenter : SystemUIBasePresenter
    {
        public override string objPath => $"{ForestDataServices.prefabPath}/mf_more_prize";
        public override UiLayer uiLayer { get => UiLayer.GameMessage; }

        Animator outAnim;
        Button collectBtn;
        Text buffNumText;
        Action finishCB;
        public override void initContainerPresenter()
        {
            resOrder = new string[] { AssetBundleData.getBundleName(BundleType.MagicForest) };
            base.initContainerPresenter();
        }

        public override void init()
        {
            base.init();
            collectBtn.onClick.AddListener(collectClick);
        }

        public override void initUIs()
        {
            outAnim = getAnimatorData("out_anim");
            collectBtn = getBtnData("collect_btn");
            buffNumText = getTextData("more_num_text");
        }

        public override Animator getUiAnimator()
        {
            return outAnim;
        }

        public void openMorePrize(ActivityAwardData awardData, Action finishCB)
        {
            AudioManager.instance.playAudioOnce(AudioPathProvider.getAudioPath(ActivityBlastAudio.SmallWin));
            buffNumText.text = $"{awardData.amount}%";
            this.finishCB = finishCB;
            collectBtn.interactable = true;
        }

        public override void animOut()
        {
            if (null != finishCB)
            {
                finishCB();
            }
            clear();
        }

        void collectClick()
        {
            collectBtn.interactable = false;
            closeBtnClick();
        }
    }
}
