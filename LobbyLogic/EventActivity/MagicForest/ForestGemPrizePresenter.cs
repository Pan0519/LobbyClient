using CommonILRuntime.Module;
using LobbyLogic.Audio;
using UnityEngine;
using UnityEngine.UI;
using Lobby.Common;
using CommonILRuntime.Services;
using CommonService;
using System;
using System.Threading.Tasks;
using Lobby.Audio;
using CommonPresenter;

namespace MagicForest
{
    public class ForestGemPrizePresenter : SystemUIBasePresenter
    {
        public override string objPath => $"{ForestDataServices.prefabPath}/mf_gem_prize";
        public override UiLayer uiLayer { get => UiLayer.GameMessage; }

        Image gemImg;
        Button collectBtn;
        Text rewardTxt;
        Animator showAnim;
        RectTransform rewardLayout;
        ulong coinAmount;
        RectTransform gemGroupRect;

        Action animOutEvent;
        string jpName;
        JPNode addJPRect;
        RectTransform flyGemRect;
        JPBoardNode jpBoard;
        public override void initContainerPresenter()
        {
            resOrder = new string[] { AssetBundleData.getBundleName(BundleType.MagicForest) };
            base.initContainerPresenter();
        }
        public override void initUIs()
        {
            showAnim = getAnimatorData("ani_show");
            collectBtn = getBtnData("collect_btn");
            rewardTxt = getTextData("reward_text");
            gemImg = getImageData("gem_img");
            gemGroupRect = getRectData("gem_group");
            rewardLayout = getRectData("reward_group_rect");
        }

        public override void init()
        {
            base.init();
            collectBtn.onClick.AddListener(collectBtnClick);
        }

        public void openPrize(string gemJP, ulong reward)
        {
            AudioManager.instance.playAudioOnce(AudioPathProvider.getAudioPath(ActivityBlastAudio.SmallWin));
            coinAmount = reward;
            if (!string.IsNullOrEmpty(gemJP))
            {
                jpName = gemJP;
                gemImg.sprite = LobbySpriteProvider.instance.getSprite<ForestSpriteProvider>(LobbySpriteType.MagicForest, $"jewel_{gemJP.ToLower()}");
                flyGemRect = GameObject.Instantiate(gemGroupRect.gameObject, gemGroupRect.transform.parent).GetComponent<RectTransform>();
            }
            rewardTxt.text = reward.ToString("N0");
            LayoutRebuilder.ForceRebuildLayoutImmediate(rewardLayout);
        }
        public void setAnimOutEvent(Action endEvent)
        {
            animOutEvent = endEvent;
        }

        public void addJPBoard(JPBoardNode jpBoard)
        {
            this.jpBoard = jpBoard;
            jpBoard.uiTransform.SetParent(showAnim.transform);
            jpBoard.uiTransform.SetAsFirstSibling();
            addJPRect = jpBoard.getJpNode(jpName);
        }

        void flyJPGem()
        {
            if (null == addJPRect)
            {
                Debug.LogError("flyJPGem null == addJPRect");
                return;
            }
            jpBoard.uiTransform.SetAsLastSibling();
            flyGemRect.SetParent(addJPRect.uiTransform);
            RectTransform addJPObj = addJPRect.getAddJPRect().GetComponent<RectTransform>();
            AudioManager.instance.playAudioOnce(AudioPathProvider.getAudioPath(ActivityBlastAudio.IconFly));
            string posTwID = flyGemRect.anchPosMove(addJPObj.anchoredPosition, durationTime: 0.8f, easeType: DG.Tweening.Ease.InBack);
            float endSize = addJPObj.sizeDelta.x;
            Animator gemFlyAnim = flyGemRect.gameObject.GetComponent<Animator>();
            string sizeTwID = TweenManager.tweenToFloat(flyGemRect.sizeDelta.x, endSize, durationTime: 0.8f, onUpdate: size =>
               {
                   Vector2 sizeDelta = flyGemRect.sizeDelta;
                   sizeDelta.Set(size, size);
                   flyGemRect.sizeDelta = sizeDelta;
               }, onComplete: () =>
               {
                   gemFlyAnim.SetTrigger("fly");
                   AudioManager.instance.playAudioOnce(AudioPathProvider.getAudioPath(ActivityBlastAudio.IconFlyIn));
                   gemFlyAnimFinish(addJPObj);

               }, easeType: DG.Tweening.Ease.InBack);
            TweenManager.tweenPlayByID(posTwID, sizeTwID);
        }

        async void gemFlyAnimFinish(RectTransform addJPObj)
        {
            await Task.Delay(TimeSpan.FromSeconds(0.3f));
            addJPObj.gameObject.setActiveWhenChange(true);
            await Task.Delay(TimeSpan.FromSeconds(0.5f));
            closePresenter();
        }

        public override void closePresenter()
        {
            jpBoard.returnToParent();
            base.closePresenter();
        }

        void collectBtnClick()
        {
            playBtnInfoAudio();
            ulong sourceVal = DataStore.getInstance.playerInfo.playerMoney;
            CoinFlyHelper.frontSFly(collectBtn.GetComponent<RectTransform>(), sourceVal, sourceVal + coinAmount, onComplete: flyJPGem);
        }

        public override Animator getUiAnimator()
        {
            return showAnim;
        }

        public override void animOut()
        {
            if (null != animOutEvent)
            {
                animOutEvent();
            }
            clear();
        }
    }
}
