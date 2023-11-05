using Common.Jigsaw;
using CommonILRuntime.Module;
using CommonPresenter;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Lobby.Jigsaw
{
    /// <summary>
    /// 收集冊(打開)
    /// </summary>
    public class Album : SystemUIBasePresenter
    {
        public override string objPath => "prefab/lobby_puzzle/puzzle_open_book";
        public override UiLayer uiLayer { get { return UiLayer.System; } }
        protected override BackHideBehaviour hideBehaviour => BackHideBehaviour.CanDoBoth;

        Button closeButton;
        Button previousBookButton;
        Button nextBookButton;
        Text rewardText;

        RectTransform rewardGroupRect;
        RectTransform upPieceRoot;
        RectTransform downPieceRoot;

        Image coverImage;
        GameObject allCompleteObj;
        GameObject completeRewardObj;

        public Action onNextButtonHandler = null;
        public Action onPreviousButtonHandler = null;

        string albumId;
        NewData newObjDatas;
        public override void initContainerPresenter()
        {
            resOrder = new string[] { AssetBundleData.getBundleName(BundleType.LobbyPuzzle)};
            base.initContainerPresenter();
        }
        public override void initUIs()
        {
            closeButton = getBtnData("closeButton");
            previousBookButton = getBtnData("previousBookButton");
            nextBookButton = getBtnData("nextBookButton");

            rewardText = getTextData("rewardText");

            coverImage = getImageData("coverImage");

            rewardGroupRect = getBindingData<RectTransform>("rewardGroupRect");
            upPieceRoot = getBindingData<RectTransform>("upPieceRoot");
            downPieceRoot = getBindingData<RectTransform>("downPieceRoot");
            allCompleteObj = getGameObjectData("allCompleteObj");
            completeRewardObj = getGameObjectData("completeRewardObj");
        }

        public override void init()
        {
            base.init();
            closeButton.onClick.AddListener(closeBtnClick);

            previousBookButton.onClick.AddListener(onClickPrevious);
            nextBookButton.onClick.AddListener(onClickNext);
        }

        /// <summary>
        /// 調整版面
        /// </summary>
        void rebuildLayout()
        {
            //迫使總獎勵的金幣&文字重新排版對齊
            LayoutRebuilder.ForceRebuildLayoutImmediate(rewardGroupRect);
        }

        public void setData(string albumId, List<JigsawPieceData> pieces, long reward)
        {
            bool isAllComplete = pieces.Exists(data => !data.collectted) == false;
            allCompleteObj.setActiveWhenChange(isAllComplete);
            rewardGroupRect.gameObject.setActiveWhenChange(!isAllComplete);
            completeRewardObj.gameObject.setActiveWhenChange(!isAllComplete);

            cleanRoot(upPieceRoot);
            cleanRoot(downPieceRoot);

            if (!isAllComplete)
            {
                rewardText.text = reward.ToString("N0");
                rebuildLayout();
            }

            this.albumId = albumId;
            newObjDatas = null;

            if (PlayerPrefs.HasKey(this.albumId))
            {
                var newObjsJson = PlayerPrefs.GetString(albumId);
                newObjDatas = LitJson.JsonMapper.ToObject<NewData>(newObjsJson);
                PlayerPrefs.DeleteKey(albumId);
            }

            var coverSprite = JigsawCoverSpriteProvider.getAlbumCover(albumId);
            coverImage.sprite = coverSprite;

            //sort by pos
            pieces.Sort((a, b) =>
            {
                return a.getImagePos() < b.getImagePos() ? -1 : 1;
            });

            for (int i = 0; i < pieces.Count; i++)
            {
                var data = pieces[i];
                createPiece(data);
            }
        }

        public void enableSwitchAlbum(bool enable)
        {
            previousBookButton.gameObject.setActiveWhenChange(enable);
            nextBookButton.gameObject.setActiveWhenChange(enable);
        }

        void cleanRoot(RectTransform root)
        {
            for (int i = 0; i < root.childCount; i++)
            {
                var trans = root.GetChild(i);
                trans.gameObject.SetActive(false);
                GameObject.Destroy(trans.gameObject);
            }
            root.DetachChildren();
        }

        void createPiece(JigsawPieceData data)
        {
            RectTransform root = data.isUpSide() ? upPieceRoot : downPieceRoot;
            var piece = PieceFactory.createPiece(data, root);
            piece.uiGameObject.name = data.ID;
            if (null != newObjDatas)
            {
                piece.isOpenNewObj(newObjDatas.pieceDataIsNew(data.ID));
            }
            piece.uiTransform.SetAsFirstSibling();
        }

        void onClickNext()
        {
            onNextButtonHandler?.Invoke();
        }

        void onClickPrevious()
        {
            onPreviousButtonHandler?.Invoke();
        }


        public override void animOut()
        {
            clear();
        }

    }
}
