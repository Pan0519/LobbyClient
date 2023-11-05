using UnityEngine;

namespace Services
{
    public class DontDestroyRootUI
    {
        static DontDestroyRootUI _instance = new DontDestroyRootUI();
        public static DontDestroyRootUI instance { get { return _instance; } }

        GameObject uiRoot = null;
        DontDestroyRootUI()
        {
            if (null == uiRoot)
            {
                var tempObj = ResourceManager.instance.getGameObject("Prefab/DontDestroyRootUI");
                uiRoot = GameObject.Instantiate(tempObj);
                DontDestroyRoot.instance.addChildToCanvas(uiRoot.transform);
                uiRoot.transform.localScale = Vector3.one;
                //var mainCamera = uiRoot.GetComponentInChildren<Camera>();
                //mainCamera.depth = 5;
            }
        }

        public void addChild(Transform childTrans)
        {
            childTrans.SetParent(uiRoot.transform);
        }

        public void destoryRoot()
        {
            GameObject.DestroyImmediate(uiRoot);
            uiRoot = null;
        }
    }
}
