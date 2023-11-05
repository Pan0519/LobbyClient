using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;
using Debug = UnityLogUtility.Debug;
using Binding;

namespace CommonILRuntime.BindingModule
{
    public static class BindingManager
    {
        static GameObject bindGo;
        public static Dictionary<string, BindingMapsData> initContainerBindingData(this GameObject bindObj)
        {
            Dictionary<string, BindingMapsData> mapDatas;
            bindGo = bindObj;
            var bindContainer = bindObj.GetComponent<BindingContainer>();
            if (null == bindContainer)
            {
                Debug.Log($"get {bindObj.name} BindingContainer is null");
                return null;
            }

            return setBindingMaps(bindContainer.getBindings());
        }

        public static Dictionary<string, BindingMapsData> initNodeBindingData(this BindingNode bindingNode)
        {
            return setBindingMaps(bindingNode.getBindings());
        }

        static Dictionary<string, BindingMapsData> setBindingMaps(List<BindingData> bindingList)
        {
            var mapDict = new Dictionary<string, BindingMapsData>();

            for (int i = 0; i < bindingList.Count; ++i)
            {
                var bindingData = bindingList[i];

                string identifier = bindingData.getIdentifier().getIdentifier();

                if (null == bindingData.getObject())
                {
                    Debug.LogError($"{bindGo.name} [發現綁定異常] identifier: {identifier}, obj is null, 可能未指定Component於 BindingElement");
                }

                var mapsData = new BindingMapsData()
                {
                    theObj = bindingData.getObject()
                };
                if (null == mapsData.theObj)
                {
                    continue;
                }
                if (mapDict.ContainsKey(identifier))
                {
                    Debug.LogError($"{mapsData.theObj.ToString()} MapDic get same key {identifier}");
                    continue;
                }

                mapDict.Add(identifier, mapsData);
            }

            return mapDict;
        }
    }

    public class BindingMapsData
    {
        Object component;
        public object theObj;

        public Object getComponent()
        {
            if (null == component)
            {
                if (theObj is Component)
                {
                    component = theObj as Component;
                }
                else if (theObj is GameObject)
                {
                    component = theObj as GameObject;
                }
                else
                {
                    Debug.LogError($"Get Component {component.name} is null {theObj}");
                }
            }
            return component;
        }

        public GameObject getGameObject()
        {
            if (theObj is GameObject)
            {
                var theGo = theObj as GameObject;

                return theGo.gameObject;
            }

            if (null == component)
            {
                Component theComponent = theObj as Component;

                return theComponent.gameObject;
            }

            return null;
        }
    }
}
