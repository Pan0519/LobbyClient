using UnityEngine;

using UiLoadFrom = ResourceManager.UiLoadFrom;
using UiLoadFile = ResourceManager.UiLoadFile;

namespace CommonILRuntime.Module
{
    public interface IPresenter
    {
        void setUiGameObject(GameObject gameObject);

        void init();

        //void setBindingData<T>(T theObj, string identifier) where T : UnityEngine.Object;
    }
}
