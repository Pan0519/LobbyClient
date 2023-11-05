using CommonILRuntime.Module;
using UnityEngine;

namespace CommonILRuntime.Module
{
    public abstract class NoBindingNodePresenter : IPresenter
    {
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

        public virtual void setUiGameObject(GameObject gameObject)
        {
            uiGameObject = gameObject;
        }

        public virtual void init() { }
    }
}
