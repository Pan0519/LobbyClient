using UnityEngine;
using CommonILRuntime.Module;
using System.Collections.Generic;
using System;

namespace CommonILRuntime.BindingModule
{
    public static class UiManager
    {
        static Dictionary<string, Presenter> presenters = new Dictionary<string, Presenter>();

        public static T getPresenter<T>(bool ifNotExistAutoCreate = true) where T : Presenter, new()
        {
            string name = getPresenterName(typeof(T));

            Presenter presenter;

            if (!presenters.TryGetValue(name, out presenter))
            {
                if (!ifNotExistAutoCreate)
                {
                    return null;
                }
                presenter = createPresenter<T>();
                presenters.Add(name, presenter);
            }

            return null != presenter ? (T)presenter : null;
        }

        static T createPresenter<T>() where T : Presenter, new()
        {
            return Presenter.bind<T>();
        }

        public static void clearPresnter(Presenter presenter)
        {
            presenters.Remove(getPresenterName(presenter.GetType()));
            presenter.destory();
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

        static void unloadPresneter(Presenter presenter)
        {
            presenter.destory();
            unLoadUi(presenter);
        }

        static void unLoadUi(Presenter bindingPresenter)
        {
            if (ApplicationConfig.isLoadFromAB)
            {
                //TODO assetbundle unload 
            }
        }

        static string getPresenterName(Type presenter)
        {
            return presenter.ToString();
        }

        public static T bindNode<T>(GameObject uiGameobject) where T : NodePresenter, new()
        {
            if (null == uiGameobject)
            {
                return default(T);
            }
            T t = new T();

            try
            {
                t.setUiGameObject(uiGameobject);
                t.initContainerPresenter();
                t.initUIs();
                t.init();
            }
            catch (Exception e)
            {
                Debug.LogError($"{typeof(T)} init() fail. {e.Message}");
                throw;
            }

            return t;
        }

        public static T bind<T>(GameObject uiGameobject) where T : NoBindingNodePresenter, new()
        {
            if (null == uiGameobject)
            {
                Debug.LogWarning("NoBindingNodePresenter uiGameobject null");
                return default(T);
            }

            T t = new T();

            try
            {
                t.setUiGameObject(uiGameobject);
                t.init();
            }
            catch (Exception e)
            {
                Debug.LogError($"{typeof(T)} init() fail. {e.Message}");
                throw;
            }

            return t;
        }
    }
}
