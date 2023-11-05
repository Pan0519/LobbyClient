using CommonILRuntime.Module;
using CommonPresenter;
using UnityEngine;
using UnityEngine.UI;
using LobbyLogic.Common;
using System;

namespace EventActivity
{
    class RewardMaxTipCheckPresenter : SystemUIBasePresenter
    {
        public override string objPath => "prefab/game/game_common_check";

        public override UiLayer uiLayer { get => UiLayer.System; }

        Button closeBtn;
        Button openActivityBtn;
        Image iconImg;

        public Action openActivityPageCB;

        public override void initUIs()
        {
            base.initUIs();
            closeBtn = getBtnData("close_btn");
            openActivityBtn = getBtnData("play_now_btn");
            iconImg = getImageData("activity_icon_img");
        }
        public override void init()
        {
            base.init();
            closeBtn.onClick.AddListener(closeBtnClick);
            openActivityBtn.onClick.AddListener(openActivityPageClick);
        }

        public void openCheckPage(Sprite iconSprite)
        {
            GamePauseManager.gamePause();
            iconImg.sprite = iconSprite;
        }

        void openActivityPageClick()
        {
            if (null != openActivityPageCB)
            {
                openActivityPageCB();
            }
            closePresenter();
        }

        public override void animOut()
        {
            clear();
            GamePauseManager.gameResume();
        }
    }
}
