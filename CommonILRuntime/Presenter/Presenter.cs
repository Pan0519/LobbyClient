using UnityEngine;
using CommonILRuntime.BindingModule;
using System.Collections.Generic;
using System;
using Object = UnityEngine.Object;
using UnityEngine.UI;
using Binding;


namespace CommonILRuntime.Module
{

    public abstract class Presenter : IPresenter
    {
        public Dictionary<string, BindingMapsData> mapDatas { private get; set; }

        public virtual void initUIs()
        {

        }

        public virtual void initContainerPresenter()
        {

        }

        public T getBindingData<T>(string identifier) where T : Object
        {
            BindingMapsData mapData;

            if (mapDatas.TryGetValue(identifier, out mapData))
            {
                if (mapData.getComponent() is T)
                {
                    return mapData.getComponent() as T;
                }
                else
                {
                    Debug.LogWarning($"{uiGameObject} getBindingData identifier: {identifier} 與期望的型別不符，是否指定綁定Component錯誤?" +
                        $",你綁定的是: {mapData.getComponent().GetType()}");
                    return null;
                }
            }
            Debug.LogError($"{uiGameObject.name} get identifier {identifier} component is null");
            return default(T);
        }

        #region getCommonComponent

        public BindingNode getNodeData(string identifier)
        {
            return getBindingData<BindingNode>(identifier);
        }

        public Button getBtnData(string identifier)
        {
            return getBindingData<Button>(identifier);
        }

        public Text getTextData(string identifier)
        {
            return getBindingData<Text>(identifier);
        }

        public Image getImageData(string identifier)
        {
            return getBindingData<Image>(identifier);
        }

        public GameObject getGameObjectData(string identifier)
        {
            return getBindingData<GameObject>(identifier);
        }
        public CustomBtn getCustomBtnData(string identifier)
        {
            return getBindingData<CustomBtn>(identifier);
        }

        public Animator getAnimatorData(string identifier)
        {
            return getBindingData<Animator>(identifier);
        }

        public RectTransform getRectData(string identifier)
        {
            return getBindingData<RectTransform>(identifier);
        }

        #endregion

        #region
        public GameObject uiGameObject { get; protected set; }

        public Transform uiTransform
        {
            get { return uiGameObject.transform; }
        }

        RectTransform m_RectTransform;
        public RectTransform uiRectTransform
        {
            get
            {
                if (null == m_RectTransform)
                {
                    m_RectTransform = uiGameObject.GetComponent<RectTransform>();
                }
                return m_RectTransform;
            }
        }
        #endregion
        public virtual void setUiGameObject(GameObject gameObject)
        {
            uiGameObject = gameObject;
        }

        public virtual void init()
        {

        }
        public virtual void open()
        {
            setVisible(true);
        }

        public virtual void close()
        {
            setVisible(false);
        }

        public virtual void setVisible(bool isVisible)
        {
            uiGameObject.setActiveWhenChange(isVisible);
        }

        public virtual void destory()
        {
            GameObject.DestroyImmediate(uiGameObject);
        }

        public virtual void clear()
        {
            UiManager.clearPresnter(this);
        }

        public static T bind<T>() where T : Presenter, new()
        {
            T t = new T();

            try
            {
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
    }
}
