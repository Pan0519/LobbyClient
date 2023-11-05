using Common.Jigsaw;
using CommonILRuntime.BindingModule;
using CommonILRuntime.Module;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace Lobby.Jigsaw
{
    public enum RareLevel
    {
        NONE = 0,
        GREEN = 1,
        BLUE = 2,
        YELLOW = 3, //GOLD
        MAX = 4,
    }

    public class Piece : NodePresenter
    {
        Button selfButton;
        RectTransform frameRoot;
        protected GameObject notGetObj;
        GameObject selectedObj;
        GameObject isNewObj;
        Image pieceImage;
        Frame framePresenter;
        Animator cardAnim;

        protected GameObject objRecycleGroup;

        public JigsawPieceData data { get; private set; }

        Action<Piece> onSelected;

        public override void initUIs()
        {
            selfButton = getBtnData("selfButton");
            frameRoot = getBindingData<RectTransform>("frameRoot");
            notGetObj = getGameObjectData("notGetObj");
            selectedObj = getGameObjectData("selectedObj");
            isNewObj = getGameObjectData("isNewObj");
            pieceImage = getImageData("pieceImage");
            cardAnim = getAnimatorData("card_anim");
        }

        public override void init()
        {
            selfButton.onClick.AddListener(onClicked);
            notGetObj.setActiveWhenChange(false);
            selectedObj.setActiveWhenChange(false);
            isNewObj.setActiveWhenChange(false);

            //回收才要出現的UI，圖冊下排的 piece prefab 不會有此 group
            var recycleUse = getNodeData("recycleGroup");
            if (null != recycleUse)
            {
                objRecycleGroup = recycleUse.gameObject;
                objRecycleGroup.setActiveWhenChange(false);
            }
        }

        public void setData(JigsawPieceData data, bool forceUpsideFrame)
        {
            this.data = data;
            framePresenter = createFrame(data.getRareLevel(), data.isUpSide() || forceUpsideFrame); //建立拼圖外框
            framePresenter.setStarCount(data.getStarLevel(), data.collectted);

            //設定拼圖圖案
            setPieceSprite(JigsawSpriteProvider.getAlbumSprite(data.getSeasonIdx(), data.getAlbumIdx(), data.getImagePos()),
                data.collectted);

            selfButton.enabled = false;
            onSelected = null;

            onChangeData(data);
        }

        //Wild 選擇到 拼圖時，壓灰(改變ImageColor), 先處理會需要變色的就好，有新增物件再補上即可
        public void setAsGray(bool isGray)
        {
            framePresenter.setAsGray(isGray);

            Color color = isGray ? new Color(0.5f, 0.5f, 0.5f) : new Color(1f, 1f, 1f);
            notGetObj.GetComponent<Image>().color = color;
            pieceImage.color = color;
        }

        public void playCardRareAnim()
        {
            cardAnim.SetTrigger($"lv{data.getRareLevel()}");
        }

        public void setSelfBtnInteractable(bool interactable)
        {
            selfButton.interactable = interactable;
        }

        protected virtual void onChangeData(JigsawPieceData data)
        {
        }

        public void registerSelected(Action<Piece> handler)
        {
            selfButton.enabled = true;
            onSelected = handler;
        }

        //Wild 選擇到 拼圖時，壓灰(改變ImageColor), 先處理會需要變色的就好，有新增物件再補上即可
        public void setSelected(bool selected)
        {
            selectedObj.setActiveWhenChange(selected);
            setAsGray(selected);
        }

        public void isOpenNewObj(bool isNew)
        {
            isNewObj.setActiveWhenChange(isNew);
        }

        void setPieceSprite(Sprite sprite, bool collected = false)
        {
            pieceImage.sprite = sprite;
            if (collected)
            {
                pieceImage.material = null; //預設灰階, 改為null會變為default material, 會變為彩色
            }
        }

        Frame createFrame(int level, bool upside)
        {
            string frameColor = "green";
            RareLevel rareLevel = (RareLevel)level;
            if (RareLevel.NONE != rareLevel && RareLevel.MAX != rareLevel)
            {
                frameColor = rareLevel.ToString().ToLower();
            }

            //clean up frame
            for (int i = 0; i < frameRoot.childCount; i++)
            {
                var trans = frameRoot.GetChild(i);
                GameObject.DestroyImmediate(trans.gameObject);
            }

            var directionName = upside ? "up" : "down";
            var prefabPath = $"prefab/lobby_puzzle/puzzle_{directionName}_frame_{frameColor}";
            var frameTemplate = ResourceManager.instance.getGameObjectWithResOrder(prefabPath,AssetBundleData.getBundleName(BundleType.LobbyPuzzle));
            var frameObj = GameObject.Instantiate(frameTemplate, frameRoot, false);
            var frameNodePresenter = UiManager.bindNode<Frame>(frameObj);
            frameNodePresenter.setFrameColor(frameColor);
            return frameNodePresenter;
        }

        void onClicked()
        {
            onSelected?.Invoke(this);
        }
    }
}
