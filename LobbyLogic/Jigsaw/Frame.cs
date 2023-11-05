using CommonILRuntime.Module;
using UnityEngine;
using UnityEngine.UI;

namespace Lobby.Jigsaw
{
    public class Frame : NodePresenter
    {
        GameObject ungetRoot;
        GameObject getRoot;
        string frameColor;

        Image frameImage;

        public override void initUIs()
        {
            ungetRoot = getGameObjectData("ungetRoot");
            getRoot = getGameObjectData("getRoot");
            frameImage = uiGameObject.GetComponent<Image>();
        }

        public override void init()
        {
            getRoot.setActiveWhenChange(false);

            for (int i = 0; i < ungetRoot.transform.childCount; i++)
            {
                var trans = ungetRoot.transform.GetChild(i);
                GameObject.DestroyImmediate(trans.gameObject);
            }

            for (int i = 0; i < getRoot.transform.childCount; i++)
            {
                var trans = getRoot.transform.GetChild(i);
                GameObject.DestroyImmediate(trans.gameObject);
            }
        }

        public void setFrameColor(string color)
        {
            frameColor = color;
        }

        public void setStarCount(int count, bool collected = false)
        {
            setCollected(collected);
            GameObject prefab;
            Transform starRoot;
            if (collected)
            {
                prefab = ResourceManager.instance.getGameObjectWithResOrder("prefab/lobby_puzzle/puzzle_starGet",AssetBundleData.getBundleName(BundleType.LobbyPuzzle));
                starRoot = getRoot.transform;
            }
            else
            {
                prefab = ResourceManager.instance.getGameObjectWithResOrder($"prefab/lobby_puzzle/puzzle_starUnget_{frameColor}", AssetBundleData.getBundleName(BundleType.LobbyPuzzle));
                starRoot = ungetRoot.transform;
            }

            for (int i = 0; i < count; i++)
            {
                addStar(prefab, starRoot);
            }
        }

        //Wild 選擇到 拼圖時，壓灰(改變ImageColor), 先處理會需要變色的就好，有新增物件再補上即可
        public void setAsGray(bool isGray)
        {
            Color color = isGray ? new Color(0.5f, 0.5f, 0.5f) : new Color(1f, 1f, 1f);
            frameImage.color = color;

            var starsImage = getRoot.GetComponentsInChildren<Image>();
            for (int i = 0; i < starsImage.Length; i++)
            {
                var image = starsImage[i];
                image.color = color;
            }

            var ungetStarsImage = ungetRoot.GetComponentsInChildren<Image>();
            for (int i = 0; i < ungetStarsImage.Length; i++)
            {
                var image = ungetStarsImage[i];
                image.color = color;
            }
        }

        void addStar(GameObject prefabObj, Transform root)
        {
            GameObject.Instantiate(prefabObj, root, false);
        }

        void setCollected(bool collected)
        {
            ungetRoot.setActiveWhenChange(!collected);
            getRoot.setActiveWhenChange(collected);
        }
    }
}
