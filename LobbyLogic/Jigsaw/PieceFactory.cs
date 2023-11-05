using CommonILRuntime.BindingModule;
using Lobby.Jigsaw.wild;
using UnityEngine;

namespace Lobby.Jigsaw
{
    public static class PieceFactory
    {
        const string upPiecePrefabPath = "prefab/lobby_puzzle/puzzle_piece_up";
        const string downPiecePrefabPath = "prefab/lobby_puzzle/puzzle_piece_down";

        /// <summary>
        /// 獲取相關的提示 forceUpsideFrame, 強制使用星星在上側的框，2021/12/08 專案負責人Vic & 企劃:慶安
        /// </summary>
        public static Piece createPiece(JigsawPieceData data, Transform root, bool forceUpsideFrame = false)
        {
            string prefabPath = data.isUpSide() || forceUpsideFrame ? upPiecePrefabPath : downPiecePrefabPath;
            GameObject prefab = ResourceManager.instance.getGameObjectWithResOrder(prefabPath,AssetBundleData.getBundleName(BundleType.LobbyPuzzle));
            var obj = GameObject.Instantiate(prefab, root);
            var presenter = UiManager.bindNode<Piece>(obj);
            presenter.setData(data, forceUpsideFrame);
            return presenter;
        }

        public static Piece createWildSelectorPiece(JigsawPieceData data, RectTransform root)
        {
            //WILD自選介面顯示拼圖時，外框統一星星朝上顯示
            string prefabPath = upPiecePrefabPath;
            GameObject prefab = ResourceManager.instance.getGameObjectWithResOrder(prefabPath, AssetBundleData.getBundleName(BundleType.LobbyPuzzle));
            var obj = GameObject.Instantiate(prefab, root);
            var presenter = UiManager.bindNode<WildPiece>(obj);
            //WILD自選介面顯示拼圖時，外框統一星星朝上顯示
            presenter.setData(data, true);
            return presenter;
        }

        public static RecyclingPiece createReclcyingPiece(JigsawPieceData data, RectTransform root)
        {
            GameObject prefab = ResourceManager.instance.getGameObjectWithResOrder(upPiecePrefabPath, AssetBundleData.getBundleName(BundleType.LobbyPuzzle));
            var obj = GameObject.Instantiate(prefab, root);
            var presenter = UiManager.bindNode<RecyclingPiece>(obj);
            presenter.setData(data, true);
            return presenter;
        }
    }
}
