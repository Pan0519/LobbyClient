using Module.Binding;
using System;
using System.Collections.Generic;
using UnityEngine;
using Debug = UnityLogUtility.Debug;
using CommonService;

namespace Module
{
    public enum UiLayer
    {
        Default,
        System,
        GameMessage,
    }

    public enum UiLoadFrom
    {
        Resources,
        AssetBundle,
    }

    public enum UiLoadFile
    {
        CommonArt,
        GameArt
    }

    public static class UiManager
    {
        static readonly Type bindingPresenterType = typeof(BindingPresenter);
        static Dictionary<Type, string> persenterTypeNames = new Dictionary<Type, string>();
        static Dictionary<string, Presenter> presenters = new Dictionary<string, Presenter>();

        public static Transform uiRoot { get { return UiRoot.instance.uiRoot; } }
        public static RectTransform uiRootRect { get { return UiRoot.instance.uiRootRect; } }
        public static Transform systemUiRoot { get { return UiRoot.instance.systemUiRoot; } }
        public static Transform gameMsgUiRoot { get { return UiRoot.instance.gameMessageRoot; } }

        //public static string getGameFilePath { private get; set; }

        static string getPresenterName<T>() where T : Presenter
        {
            return getPresenterName(typeof(T));
        }

        static string getPresenterName(Type type)
        {
            string name;
            if (!persenterTypeNames.TryGetValue(type, out name))
            {
                persenterTypeNames[type] = name = type.ToString();
            }
            return name;
        }

        public static void unLoadUi(BindingPresenter bindingPresenter)
        {
            if (UiLoadFrom.AssetBundle == bindingPresenter.uiLoadFrom)
            {
                //TODO unLoadAssetbundle
            }
        }

        public static Transform getUiParent(UiLayer uiLayer)
        {
            switch (uiLayer)
            {
                default:
                case UiLayer.Default:
                    return uiRoot;

                case UiLayer.System:
                    return systemUiRoot;

                case UiLayer.GameMessage:
                    return gameMsgUiRoot;
            }
        }

        public static void setUiParent(Transform uiTransform, UiLayer uiLayer)
        {
            Transform parent = getUiParent(uiLayer);
            if (null == parent)
            {
                return;
            }
            uiTransform.SetParent(parent, false);
        }

        public static T getPresenter<T>(bool ifNotExistAutoCreate = true) where T : Presenter
        {
            string presenterName = getPresenterName<T>();

            Presenter presenter;
            if (!presenters.TryGetValue(presenterName, out presenter))
            {
                if (ifNotExistAutoCreate)
                {
                    presenter = createPresenter<T>();
                    presenters.Add(presenterName, presenter);
                }
            }

            return null != presenter ? (T)presenter : null;
        }

        static T createPresenter<T>() where T : Presenter
        {
            BindingPresenter presenterAttribute = getPresenterBinding<T>();

            GameObject uiGameObject = null;
            UiLoadFrom uiLoadFrom = presenterAttribute.uiLoadFrom;
            try
            {
                //Debug.Log($"uiLoadFrom {uiLoadFrom}");
                switch (uiLoadFrom)
                {
                    case UiLoadFrom.AssetBundle:
                    case UiLoadFrom.Resources:
                        var sourceGO = ResourceManager.instance.getGameObject(presenterAttribute.uiPath, loadFile: (int)presenterAttribute.uiLoadFile);
                        uiGameObject = instantiateObject(sourceGO, getUiParent(presenterAttribute.uiLayer));
                        break;

                    default:
                        Debug.LogWarning($"請補上 : {presenterAttribute.uiLoadFrom} 生成方式");
                        return null;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"UIManager createPresenter exception\nCan't load : {presenterAttribute.uiPath} from: {uiLoadFrom}\nMsg : {e.Message}");
                throw;
            }

            return Presenter.bind<T>(uiGameObject);
        }

        static void unloadPresneter(Presenter presenter)
        {
            presenter.destory();
            unLoadUi(getPresenterBinding(presenter));
        }

        #region clearPresenter
        public static void clearPresenter(string presenterName)
        {
            if (null == presenters)
            {
                return;
            }
            Presenter presenter = null;
            if (presenters.TryGetValue(presenterName, out presenter))
            {
                unloadPresneter(presenter);
                presenters.Remove(presenterName);
            }
        }

        public static void clearPresenter(Presenter presenter)
        {
            clearPresenter(getPresenterName(presenter.GetType()));
        }

        public static void clearPresenter<T>() where T : Presenter
        {
            clearPresenter(getPresenterName<T>());
        }

        public static void clearAllPresenter()
        {
            if (null == presenters)
            {
                return;
            }

            var presenterEnum = presenters.GetEnumerator();

            while (presenterEnum.MoveNext())
            {
                unloadPresneter(presenterEnum.Current.Value);
            }

            presenters.Clear();
        }

        #endregion

        #region getPresenterBinding

        static BindingPresenter getPresenterBinding<T>() where T : Presenter
        {
            return getPresenterBinding(typeof(T));
        }

        static BindingPresenter getPresenterBinding(Presenter presenter)
        {
            return getPresenterBinding(presenter.GetType());
        }

        static BindingPresenter getPresenterBinding(Type type)
        {
            object[] bindingPresenterCustomAttributes = type.GetCustomAttributes(bindingPresenterType, true);
            if (null != bindingPresenterCustomAttributes && 0 < bindingPresenterCustomAttributes.Length)
            {
                return bindingPresenterCustomAttributes[0] as BindingPresenter;
            }

            Debug.LogError($"{type} has no BindingPresenter attribute");
            return null;
        }

        #endregion

        static GameObject instantiateObject(GameObject templateObject, Transform parent, bool worldPositionStays = false)
        {
            return GameObject.Instantiate(templateObject, parent, worldPositionStays);
        }
    }
}
