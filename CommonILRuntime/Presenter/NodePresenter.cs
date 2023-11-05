using CommonILRuntime.BindingModule;
using Binding;
using UnityEngine;

namespace CommonILRuntime.Module
{
    public class NodePresenter : Presenter
    {
        BindingNode node;

        public string nodeIdentifier { get { return node.getIdentifier().getIdentifier(); } }
        public override void initContainerPresenter()
        {
            node = uiGameObject.GetComponent<BindingNode>();
            if (null == node)
            {
                Debug.LogError($"get {uiGameObject} Component<BindingNode> is null");
                return;
            }
            mapDatas = node.initNodeBindingData();
        }
    }
}
