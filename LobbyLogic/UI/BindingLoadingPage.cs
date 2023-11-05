using UnityEngine;
using Services;
using UniRx;
using System;

namespace Lobby.UI
{
    public class BindingLoadingPage
    {
        public static BindingLoadingPage instance { get { return _instance; } }
        static BindingLoadingPage _instance = new BindingLoadingPage();

        GameObject uiGameObject = null;
        IDisposable closeDis;
        BindingLoadingPage()
        {
            if (null == uiGameObject)
            {
                var obj = ResourceManager.instance.getGameObject("prefab/lobby/binding_loading");
                uiGameObject = GameObject.Instantiate(obj);
                DontDestroyRoot.addChild(uiGameObject.transform);
                uiGameObject.transform.localScale = Vector3.one;
                RectTransform rectTransform = uiGameObject.GetComponent<RectTransform>();
                rectTransform.offsetMax = Vector2.zero;
                rectTransform.offsetMin = Vector2.zero;
                uiGameObject.setActiveWhenChange(false);
            }
        }

        public void open()
        {
            uiGameObject.setActiveWhenChange(true);
            closeDis = Observable.Timer(TimeSpan.FromSeconds(5.0f)).Subscribe(_ =>
            {
                close();
            });
        }

        public void close()
        {
            UtilServices.disposeSubscribes(closeDis);
            uiGameObject.setActiveWhenChange(false);
        }
    }
}
