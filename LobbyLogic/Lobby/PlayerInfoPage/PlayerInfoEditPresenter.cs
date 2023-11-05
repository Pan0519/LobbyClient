using CommonILRuntime.Module;
using CommonILRuntime.BindingModule;
using UnityEngine.UI;
using UnityEngine;
using Service;
using System;
using System.Collections.Generic;
using LobbyLogic.NetWork.ResponseStruct;
using CommonService;
using CommonPresenter;

namespace Lobby.PlayerInfoPage
{
    class PlayerInfoEditPresenter : SystemUIBasePresenter
    {
        public override string objPath => "prefab/lobby/page_edit_profile";
        public override UiLayer uiLayer { get => UiLayer.GameMessage; }
        #region BindingField
        Button closeBtn;
        Button confirmBtn;
        InputField nickNameInput;
        LoopVerticalScrollRect headScroll;
        #endregion

        int headIconID { get; set; }
        Dictionary<int, HeadInfoPresenter> headInfoDicts = new Dictionary<int, HeadInfoPresenter>();
        Sprite[] headSprites = null;
        public override void initUIs()
        {
            closeBtn = getBtnData("close_btn");
            confirmBtn = getBtnData("confirm_btn");
            nickNameInput = getBindingData<InputField>("nickname_input");
            headScroll = getBindingData<LoopVerticalScrollRect>("head_scroll");
        }

        public override void init()
        {
            loadHeadSprite();
            base.init();
            headScroll.setNewItemAction = setHeadImage;
            headScroll.totalCount = 10;
            headScroll.RefillCells();
            closeBtn.onClick.AddListener(closeBtnClick);
            confirmBtn.onClick.AddListener(confirmClick);
        }

        void loadHeadSprite()
        {
            if (null != headSprites)
            {
                return;
            }
            headSprites = ResourceManager.instance.loadAll("prefab/player_head/player_head");
        }

        public override void animOut()
        {
            clear();
        }

        public override void open()
        {
            base.open();
            headIconID = DataStore.getInstance.playerInfo.iconIndex;
            nickNameInput.placeholder.GetComponent<Text>().text = DataStore.getInstance.playerInfo.playerName;
            setHeadIconActive(headIconID, true);
        }

        async void confirmClick()
        {
            string playerNewName = nickNameInput.text;
            if (string.IsNullOrEmpty(playerNewName))
            {
                playerNewName = DataStore.getInstance.playerInfo.playerName;
            }
            PlayerInfoResponse infoResponse = await AppManager.lobbyServer.modifyPlayerInfo(playerNewName, headIconID);
            DataStore.getInstance.playerInfo.setIconIdx(infoResponse.iconIndex);
            DataStore.getInstance.playerInfo.setName(infoResponse.name);
            clear();
        }

        public override void clear()
        {
            var headItemEnum = headInfoDicts.GetEnumerator();
            while (headItemEnum.MoveNext())
            {
                headItemEnum.Current.Value.clear();
            }
            base.clear();
        }

        void setHeadImage(GameObject go, int index)
        {
            HeadInfoPresenter headInfo = UiManager.bindNode<HeadInfoPresenter>(go);
            headInfo.setChooseIconActive(false);
            headInfo.id = index - 1;
            headInfo.setBtnAction((id) =>
            {
                setHeadIconActive(headIconID, false);
                setHeadIconActive(id, true);
                headIconID = headInfo.id;
            });
            Sprite headSprite;
            if (headInfo.id < 0)
            {
                if (!string.IsNullOrEmpty(DataStore.getInstance.playerInfo.fbImageUrl))
                {
                    headSprite = null;
                }
                else
                {
                    headSprite = Array.Find(headSprites, sprite => sprite.name.Equals("head_fb"));
                }
            }
            else
            {
                headSprite = Array.Find(headSprites, sprite => sprite.name.Equals($"head_{headInfo.id}"));
            }
            headInfo.setHeadSprite(headSprite);

            if (headInfoDicts.ContainsKey(headInfo.id))
            {
                headInfoDicts.Remove(headInfo.id);
            }
            headInfoDicts.Add(headInfo.id, headInfo);
        }

        void setHeadIconActive(int id, bool enable)
        {
            HeadInfoPresenter head;

            if (headInfoDicts.TryGetValue(id, out head))
            {
                head.setChooseIconActive(enable);
            }
        }
    }

    class HeadInfoPresenter : NodePresenter
    {
        Button chooseBtn;
        Image headImage;
        GameObject chooseIcon;

        public int id;

        public override void initUIs()
        {
            chooseBtn = getBtnData("choose_btn");
            headImage = getImageData("head_img");
            chooseIcon = getGameObjectData("choose_icon");
        }

        public void setBtnAction(Action<int> chooseCallback)
        {
            chooseBtn.onClick.AddListener(() =>
            {
                chooseCallback(id);
            });
        }

        public void setChooseIconActive(bool active)
        {
            chooseIcon.setActiveWhenChange(active);
        }

        public void setHeadSprite(Sprite headSprite)
        {
            if (null == headSprite)
            {
                WebRequestTextureScheduler.instance.request(DataStore.getInstance.playerInfo.fbImageUrl, (texture) =>
                {
                    setHeadImage(Util.getSpriteFromTexture(texture));
                }).download();
                return;
            }
            setHeadImage(headSprite);
        }

        void setHeadImage(Sprite headSprite)
        {
            if (headSprite == null)
            {
                return;
            }
            headImage.sprite = headSprite;
        }

    }
}
