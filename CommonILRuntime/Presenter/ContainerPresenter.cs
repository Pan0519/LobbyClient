using CommonILRuntime.BindingModule;
using UnityEngine;
using System.Collections.Generic;
using Services;
using UniRx;

namespace CommonILRuntime.Module
{
    public enum UiLayer
    {
        Root,
        System,
        GameMessage,
        BarRoot,
        TopRoot,
        LockHeight,
    }

    public class ContainerPresenter : Presenter
    {
        [System.Flags]
        public enum BackHideBehaviour
        { 
            None = 0,
            HideMe = 1,
            HideOthers = 2,
            CanDoBoth = HideMe | HideOthers,
        }

        public static Transform uiRoot { get { return UiRoot.instance.getNowScreenOrientationUIRoot(); } }
        public static RectTransform uiRootRect { get { return UiRoot.instance.getNowScreenOrientationUIRootRect(); } }
        public static Transform systemUiRoot { get { return UiRoot.instance.getNowScreenOrientationSystemRoot(); } }
        public static Transform gameMsgUiRoot { get { return UiRoot.instance.getNowScreenOrientationGameMsgRoot(); } }
        public static Transform uiBarRoot { get { return UiRoot.instance.getNowScreenOrientationBarRoot(); } }
        public static Transform topUIRoot { get { return UiRoot.instance.getNowScreenOrientationTopUIRoot(); } }
        public static Transform lockHeightRoot { get { return UiRoot.instance.lockHeightRoot; } }

        public virtual UiLayer uiLayer { get; set; } = UiLayer.Root;

        public virtual string objPath { get; }
        public virtual string[] resOrder { get; set; }
        GameObject sourceGo = null;
        Dictionary<UiLayer, Transform> rootsTrans = new Dictionary<UiLayer, Transform>();

        protected virtual BackHideBehaviour hideBehaviour { get { return BackHideBehaviour.None; } }

        public override void initContainerPresenter()
        {
            initRoots();
            if (null == sourceGo)
            {
                if (null != resOrder)
                {
                    //Util.Log($"resOrder != null___{objPath}");
                    sourceGo = ResourceManager.instance.getGameObjectWithResOrder(objPath, resOrder);
                }
                else
                {
                    sourceGo = ResourceManager.instance.getGameObject(objPath);
                }
            }
            setUiGameObject(instantiateObject(sourceGo, getUiParent()));
            mapDatas = uiGameObject.initContainerBindingData();

            if ((hideBehaviour & BackHideBehaviour.HideMe) != 0)
                UIHideBackServices.Instance.subscribe(onTopScoreChange).AddTo(uiGameObject);
        }

        public void changeUILayout(UiLayer changeLayer)
        {
            uiRectTransform.SetParent(getUiParent(changeLayer));
        }

        public void resetUiLayout()
        {
            uiRectTransform.SetParent(getUiParent());
        }

        void initRoots()
        {
            rootsTrans.Add(UiLayer.Root, uiRoot);
            rootsTrans.Add(UiLayer.System, systemUiRoot);
            rootsTrans.Add(UiLayer.GameMessage, gameMsgUiRoot);
            rootsTrans.Add(UiLayer.BarRoot, uiBarRoot);
            if (null != lockHeightRoot)
            {
                rootsTrans.Add(UiLayer.LockHeight, lockHeightRoot);
            }
            if (null != topUIRoot)
            {
                rootsTrans.Add(UiLayer.TopRoot, topUIRoot);
            }
        }

        static GameObject instantiateObject(GameObject templateObject, Transform parent, bool worldPositionStays = false)
        {
            return GameObject.Instantiate(templateObject, parent, worldPositionStays);
        }

        public Transform getUiParent()
        {
            Transform result = null;
            if (!rootsTrans.TryGetValue(uiLayer, out result))
            {
                Debug.LogError($"get {uiLayer} Root is null");
            }
            return result;
        }

        Transform getUiParent(UiLayer layer)
        {
            Transform result = null;
            if (!rootsTrans.TryGetValue(layer, out result))
            {
                Debug.LogError($"get {layer} Root is null");
            }
            return result;
        }

        public override void open()
        {
            if ((hideBehaviour & BackHideBehaviour.HideOthers) != 0)
                UIHideBackServices.Instance.waitAddContainer(this);

            base.open();
        }

        public override void destory()
        {
            if ((hideBehaviour & BackHideBehaviour.HideOthers) != 0)
                UIHideBackServices.Instance.removeContainer(this);

            base.destory();
        }

        void onTopScoreChange(int nowTopScore)
        {
            int myScore = UIHideBackServices.CoculateContainerLayerScore(this);

            bool hide = nowTopScore > myScore;

            var anchorPos = uiRectTransform.anchoredPosition3D;
            anchorPos.z = hide ? -10000f : 0f;

            uiRectTransform.anchoredPosition3D = anchorPos;
        }
    }
}
