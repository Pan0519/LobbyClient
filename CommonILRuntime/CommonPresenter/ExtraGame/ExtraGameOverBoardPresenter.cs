using System;
using UnityEngine;
using UnityEngine.UI;

namespace CommonILRuntime.ExtraGame
{
    public class ExtraGameOverBoardPresenter : ExtraGameBoardPresenter
    {
        Image titleImage;
        Image msgImage;
        Image continueImage;
        Button quiteBtn;
        Button retryBtn;
        Button tapBtn;
        Action onQuiteClickCallBack;
        Action onRetryClickCallBack;

        public override void initUIs()
        {
            base.initUIs();
            titleImage = getImageData("title");
            msgImage = getImageData("msg");
            continueImage = getImageData("continue");
            quiteBtn = getBtnData("quite_btn");
            retryBtn = getBtnData("retry_btn");
            tapBtn = getBtnData("tap_btn");
        }

        public override void init()
        {
            base.init();
            quiteBtn.onClick.AddListener(onQuiteClick);
            retryBtn.onClick.AddListener(onRetryClick);
            tapBtn.onClick.AddListener(onRetryClick);
        }

        public void setOnQuiteClickCallBack(Action onQuiteClickCallBack)
        {
            this.onQuiteClickCallBack = onQuiteClickCallBack;
        }

        public void setOnRetryClickCallBack(Action onRetryClickCallBack)
        {
            this.onRetryClickCallBack = onRetryClickCallBack;
        }

        public void setBoardType(bool isWin)
        {
            setImageSprite(titleImage, "title", isWin);
            setImageSprite(msgImage, "tryagain", isWin);
            setImageSprite(continueImage, "click", isWin);
            setImageSprite(quiteBtn.image, "btn_exit", false);
            setImageSprite(retryBtn.image, "btn_retry", false);
        }

        void setImageSprite(Image image, string spriteName,bool isWin)
        {
            image.sprite = getBoardSprite(spriteName, isWin);
            image.SetNativeSize();
        }

        Sprite getBoardSprite(string spriteName, bool isWin)
        {
            spriteName = spriteProvider.convertGameOverBoardName(spriteName, isWin);
            return spriteProvider.getSprite(spriteName);
        }

        void onQuiteClick()
        {
            closeAllBtn();
            onQuiteClickCallBack?.Invoke();
        }

        void onRetryClick()
        {
            closeAllBtn();
            onRetryClickCallBack?.Invoke();
            playOutAni(null);
        }

        void closeAllBtn()
        {
            quiteBtn.interactable = false;
            retryBtn.interactable = false;
            tapBtn.interactable = false;
        }
    }
}
