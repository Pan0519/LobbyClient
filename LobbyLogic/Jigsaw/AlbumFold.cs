using CommonILRuntime.Module;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace Lobby.Jigsaw
{
    /// <summary>
    /// 博物館內的收集冊(關)
    /// </summary>
    public class AlbumFold : NodePresenter
    {
        public Action<string> onClick = null;

        Button selfButton;  //自己就是按鈕
        Image coverImage;   //封面
        Image progressImage;//進度條

        GameObject completeObj;//蒐集完成提示

        Text progressText;  //進度文字
        GameObject lockObj;

        string albumId;
        bool isOpen;

        public bool isComplete { get; private set; }

        public override void initUIs()
        {
            selfButton = getBtnData("selfButton");
            coverImage = getImageData("coverImage");
            progressImage = getImageData("progressImage");
            progressText = getTextData("progressText");
            completeObj = getGameObjectData("completeObj");
            lockObj = getGameObjectData("lockObj");
        }

        public override void init()
        {
            selfButton.onClick.AddListener(selfClick);
            completeObj.setActiveWhenChange(false);
            lockObj.setActiveWhenChange(false);
        }

        public void setId(string albumId)
        {
            this.albumId = albumId;
        }

        public void setCoverSprite(Sprite sprite)
        {
            coverImage.sprite = sprite;
        }

        public void setProgress(int collectedCount, int totalCount)
        {
            float progress = collectedCount / (float)totalCount;
            progressImage.fillAmount = progress;    //進度條

            progressText.text = $"{collectedCount}/{totalCount}";   //進度文字
            isComplete = collectedCount >= totalCount;
            completeObj.setActiveWhenChange(isComplete);  //如果完成要顯示完成圖示
        }

        public void setIsOpen(bool isOpen)
        {
            this.isOpen = isOpen;
            coverImage.gameObject.setActiveWhenChange(isOpen);
            lockObj.setActiveWhenChange(!isOpen);
        }

        void selfClick()
        {
            if (!isOpen)
            {
                return;
            }
            onClick?.Invoke(albumId);
        }
    }
}
