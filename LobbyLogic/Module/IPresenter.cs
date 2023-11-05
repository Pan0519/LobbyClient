using UnityEngine;

namespace Module
{
    public interface IPresenter
    {
        void setUiGameObject(GameObject gameObject);

        void init();
    }
}
