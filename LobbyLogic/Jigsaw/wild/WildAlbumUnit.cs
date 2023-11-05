using Common.Jigsaw;
using CommonILRuntime.Module;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Lobby.Jigsaw
{
    public class WildAlbumUnit : NodePresenter
    {
        const int TOTAL_PIECES_COUNT = 14;

        Image coverImage;
        Text collectedText;
        Text totalCountText;
        GameObject infoObj;
        RectTransform pieceRoot;

        List<Piece> uiPieces = new List<Piece>();


        public override void initUIs()
        {
            coverImage = getImageData("coverImage");
            collectedText = getTextData("collectedText");
            totalCountText = getTextData("totalCountText");
            infoObj = getGameObjectData("infoObj");
            pieceRoot = getBindingData<RectTransform>("pieceRoot");
        }

        public void setData(string albumId, int progressCount, List<JigsawPieceData> selectablePieces, Action<Piece> selectPieceHandler)
        {
            //設定相簿Logo
            Sprite coverSprite = JigsawCoverSpriteProvider.getAlbumCover(albumId);
            if (null != coverSprite)
            {
                coverImage.sprite = coverSprite;
            }
            else
            {
                coverImage.enabled = false;
            }

            //設定拼圖
            uiPieces.Clear();
            for (int i = 0; i < selectablePieces.Count; i++)
            {
                var data = selectablePieces[i];
                var p = PieceFactory.createWildSelectorPiece(data, pieceRoot);
                p.registerSelected(selectPieceHandler);
                uiPieces.Add(p);
            }

            //設定進度
            totalCountText.text = $"/{TOTAL_PIECES_COUNT}"; //WILD自選介面中，不管是哪種顏色拼圖的自選，後方的N/14都顯示為此拼圖冊的收集進度
            collectedText.text = progressCount.ToString();
        }

        public void ignoreCollected(bool ignore)
        {
            bool allHided = true;
            for (int i = 0; i < uiPieces.Count; i++)
            {
                var uiPiece = uiPieces[i];
                var hide = uiPiece.data.collectted && ignore;
                uiPiece.uiGameObject.setActiveWhenChange(!hide);

                if (!hide)
                {
                    allHided = false;
                }
            }

            infoObj.setActiveWhenChange(!allHided || !ignore);
        }
    }
}
