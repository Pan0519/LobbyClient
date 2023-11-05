using CommonILRuntime.Module;
using UnityEngine.UI;
using UnityEngine;
using CommonService;
using Services;
using CommonILRuntime.BindingModule;
using System;
using CommonILRuntime.SpriteProvider;

namespace CommonPresenter
{
    public class MsgBoxPresenter : ContainerPresenter
    {
        public override string objPath => "prefab/page_common";
        public override UiLayer uiLayer { get => UiLayer.System; }

        RectTransform scaleRect;
        NormalPageNodePresenter normalPagePresenter;

        public override void initUIs()
        {
            var scaleObj = getGameObjectData("scale_root");
            scaleRect = scaleObj.GetComponent<RectTransform>();
        }

        public override void init()
        {
            normalPagePresenter = UiManager.bindNode<NormalPageNodePresenter>(getNodeData("normal_node").cachedGameObject);
            normalPagePresenter.setClearAction(clear);
        }

        public override void open()
        {
            float scale = 1.0f;
            if (Screen.orientation == ScreenOrientation.Portrait)
            {
                scale = 0.8f;
            }
            var rootScale = scaleRect.localScale;
            rootScale.Set(scale, scale, scale);
            scaleRect.localScale = rootScale;
            base.open();
        }

        public void openNormalBox(Action collectBtn, string title = "", string content = "")
        {
            normalPagePresenter.setCollectSprite(getBtnSprite("btn_confirm_1")).setTitle(title).setContent(content).setCollectAction(collectBtn).openPage();
            open();
        }

        public void openNoCoinMsg()
        {
            normalPagePresenter.setCollectSprite(getBtnSprite("btn_confirm_1"))
                .setContentKey("Game_GoldShortage_Title")
                .setCollectAction(() =>
                {
                    DataStore.getInstance.eventInGameToLobbyService.OpenFuncInLobby(FunctionNo.Shop);
                }).setCloseAction(() =>
                {
                    DataStore.getInstance.eventInGameToLobbyService.OpenFuncInLobby(FunctionNo.LimitShop);
                })
                .openPage(isOpenCloseBtn: true);
            open();
        }

        public void openChestFull(string contentKey)
        {
            normalPagePresenter.setCollectSprite(getBtnSprite("btn_confirm_1")).setContentKey(contentKey).openPage();
            open();
        }

        public void openActivityEndNode(Action activityEndEvent)
        {
            normalPagePresenter.setCollectSprite(getBtnSprite("btn_so_excited"))
                .setTitleKey("Universal_Title_1")
                .setContentKey("Universal_Msg_1")
                .setCollectAction(activityEndEvent).openPage();
            open();
        }

        Sprite getBtnSprite(string spriteName)
        {
            return CommonSpriteProvider.instance.getSprite<BtnLocalizationProvider>(CommonSpriteType.CommonButton, spriteName);
        }
    }

    public class BoxBasePresenter : NodePresenter
    {
        Action clearAction;

        public void setClearAction(Action closeBox)
        {
            clearAction = closeBox;
        }

        public void runClearAction()
        {
            if (null != clearAction)
            {
                clearAction();
            }
        }
    }

    #region BoxNodePresenters
    public class NormalPageNodePresenter : BoxBasePresenter
    {
        Text titleText;
        Text contentText;
        Button collectBtn;
        Button closeBtn;
        Image collectBtnImg;

        Action collectAction = null;
        Action closeAction = null;

        public override void init()
        {
            setTitleKey(string.Empty);
            setContentKey(string.Empty);
        }

        public override void initUIs()
        {
            titleText = getTextData("title_txt");
            contentText = getTextData("content_txt");
            collectBtn = getBtnData("collect_btn");
            closeBtn = getBtnData("off_btn");
            collectBtnImg = getImageData("collect_img");
        }

        public NormalPageNodePresenter setCollectAction(Action eventAction)
        {
            collectAction = eventAction;
            return this;
        }

        public NormalPageNodePresenter setCloseAction(Action eventAction)
        {
            closeAction = eventAction;
            return this;
        }

        public NormalPageNodePresenter setTitleKey(string titleKey)
        {
            if (!string.IsNullOrEmpty(titleKey))
            {
                titleText.text = LanguageService.instance.getLanguageValue(titleKey);
            }
            else
            {
                titleText.text = string.Empty;
            }

            return this;
        }

        public NormalPageNodePresenter setContentKey(string contentKey)
        {
            contentText.text = LanguageService.instance.getLanguageValue(contentKey);
            return this;
        }
        public NormalPageNodePresenter setTitle(string title)
        {
            titleText.text = title;
            return this;
        }

        public NormalPageNodePresenter setContent(string content)
        {
            contentText.text = content;
            return this;
        }

        public NormalPageNodePresenter setCollectSprite(Sprite btnSprite)
        {
            collectBtnImg.sprite = btnSprite;
            return this;
        }

        public void openPage(bool isOpenCloseBtn = false)
        {
            collectBtn.onClick.AddListener(collectClick);
            closeBtn.onClick.AddListener(closeClick);
            closeBtn.gameObject.setActiveWhenChange(isOpenCloseBtn);
            open();
        }

        void collectClick()
        {
            if (null != collectAction)
            {
                collectAction();
            }
            runClearAction();
        }

        void closeClick()
        {
            if (null != closeAction)
            {
                closeAction();
            }
            runClearAction();
        }
    }


    #endregion
}
