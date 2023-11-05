using Binding;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Module.Binding
{
    public class Binder
    {
        static readonly BindingFlags fieldBindingFlags = BindingFlags.Public | BindingFlags.NonPublic;

        static readonly Type bindingFieldType = typeof(BindingField);

        public static T bind<T>(GameObject bindedGameObject)
        {
            if (null == bindedGameObject)
            {
                return default(T);
            }

            T t = Activator.CreateInstance<T>();

            return t;
        }

        public static void bind(object instance, GameObject bindedGameObject)
        {
            BindingHub bindingHub = bindedGameObject.GetComponent<BindingHub>();
            if (null != bindingHub)
            {
                bind(instance, bindingHub.getBindings());
            }
        }

        static void bind(object instance, List<BindingData> bindingDatas)
        {
            if (null == instance || null == bindingDatas || 0 == bindingDatas.Count)
            {
                return;
            }

            FieldInfo[] fieldInfos = instance.GetType().GetFields(fieldBindingFlags);
            if (null == fieldInfos || 0 == fieldInfos.Length)
            {
                return;
            }

#if DEV
            List<FieldInfo> fieldList = new List<FieldInfo>();
#endif

            for (int k = 0; k < bindingDatas.Count; ++k)
            {
                BindingData bindingData = bindingDatas[k];

                if (null == bindingData.getObject())
                {
                    Debug.LogWarning($"Typeof: {instance.GetType()} getIdentifier : {bindingData.getIdentifier().getIdentifier()}");
                    continue;
                }

                for (int i = 0; i < fieldInfos.Length; ++i)
                {
                    FieldInfo fieldInfo = fieldInfos[i];

                    object[] customAttributes = fieldInfo.GetCustomAttributes(bindingFieldType, false);

                    if (0 < customAttributes.Length)
                    {
#if DEV
                        fieldList.Add(fieldInfo);
#endif

                        BindingField bindingField = (customAttributes[0] as BindingField);

                        if (string.Equals(bindingField.identifier, bindingData.getIdentifier().getIdentifier())
                            && fieldInfo.FieldType.Equals(bindingData.getObject().GetType()))
                        {
                            fieldInfo.SetValue(instance, bindingData.getObject());
                            break;
                        }
                    }
                }
#if DEV
                for (int i = 0; i < fieldList.Count; ++i)
                {
                    var field = fieldList[i];
                    if (field.GetValue(instance) == null)
                    {
                        Debug.LogWarning($"Check {instance.GetType()}'s field({field.FieldType}:{field.Name}) is null");
                    }
                }
#endif
            }
        }
    }
}
