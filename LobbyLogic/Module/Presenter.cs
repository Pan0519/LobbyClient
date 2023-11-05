using Module.Binding;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Module
{
    public abstract class Presenter : IPresenter
    {
        public GameObject uiGameObject { get; protected set; }

        #region
        Transform m_Transform;
        public Transform uiTransform
        {
            get
            {
                if (null == m_Transform)
                {
                    m_Transform = uiGameObject.transform;
                }
                return m_Transform;
            }
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

        List<NodePresenter> m_Nodes;
        protected List<NodePresenter> nodes
        {
            get
            {
                if (null == m_Nodes)
                {
                    m_Nodes = new List<NodePresenter>();
                }
                return m_Nodes;
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
            clearNode();
            UiManager.clearPresenter(this);
        }

        #region Node
        protected virtual T bindNode<T>(GameObject gameObject) where T : NodePresenter
        {
            T t = NodePresenter.bind<T>(gameObject);
            addNode(t);
            return t;
        }

        public static T createNode<T>(GameObject template, Transform parent) where T : NodePresenter
        {
            GameObject newGameObject = GameObject.Instantiate(template);
            T t = bind<T>(newGameObject);

            t.uiTransform.SetParent(parent, false);
            t.open();

            return t;
        }

        protected virtual void addNode(NodePresenter node)
        {
            nodes.Add(node);
        }

        protected virtual void clearNode()
        {
            for (int i = 0; i < nodes.Count; ++i)
            {
                nodes[i].clear();
            }
            nodes.Clear();
        }
        #endregion

        public static T bind<T>(GameObject bindedGameObject) where T : IPresenter
        {
            if (null == bindedGameObject)
            {
                return default(T);
            }

            T t = Activator.CreateInstance<T>();

            Binder.bind(t, bindedGameObject);
            t.setUiGameObject(bindedGameObject);

            try
            {
                t.init();
            }
            catch (Exception e)
            {
                Debug.LogError($"{typeof(T)} bindGame {bindedGameObject.name} init() fail. {e.Message}");
            }
            return t;
        }

    }
}
