using System;
using System.Text;
using System.Diagnostics;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using MessagePack;
using UnityEngine;
using Debug = UnityEngine.Debug;


public enum TextureLoadFrom
{
    Resource,
    AssetBundle,
}

public static class Util
{
    public static byte[] msgPackConvertToBytes(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return null;
        }
        return MessagePackSerializer.ConvertFromJson(json);
    }

    public static T msgPackDeserialResponse<T>(byte[] responseData)
    {
        return MessagePackSerializer.Deserialize<T>(responseData);
    }

    public static string msgpackToJsonStr(byte[] responseData)
    {
        return MessagePackSerializer.ConvertToJson(responseData);
    }

    public static string toUtf8String(byte[] data)
    {
        if (null == data)
        {
            return string.Empty;
        }

        return Encoding.UTF8.GetString(data);
    }

    public static string toJson(object obj)
    {
        if (null == obj)
        {
            return null;
        }

        return LitJson.JsonMapper.ToJson(obj);
    }

    public static byte[] toBinary(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        return Encoding.UTF8.GetBytes(value);
    }

    public static T toObjectArray<T>(IEnumerable list, Dictionary<Type, Dictionary<string, IMemberOperator>> cachedMemberInfos = null) where T : IList
    {
        return (T)toObjectArray(list, typeof(T), cachedMemberInfos);
    }

    public static object toObjectArray(IEnumerable list, Type t, Dictionary<Type, Dictionary<string, IMemberOperator>> cachedMemberInfos)
    {
        if (null == list)
        {
            return null;
        }

        if (t.IsArray)
        {
            Array array;

            if (null == cachedMemberInfos)
            {
                cachedMemberInfos = new Dictionary<Type, Dictionary<string, IMemberOperator>>();
            }

            ICollection collection = list as ICollection;
            if (null != collection)
            {
                array = Array.CreateInstance(t.GetElementType(), collection.Count);
            }
            else
            {
                array = Activator.CreateInstance(t) as Array;
            }

            Type elementType = t.GetElementType();
            int i = 0;

            var iter = list.GetEnumerator();
            while (iter.MoveNext())
            {
                object element = null;

                try
                {
                    object obj = iter.Current;
                    Type objType = obj.GetType();

                    if (elementType.Equals(objType))
                    {
                        element = obj;
                    }
                    else
                    {
                        element = convertObject(obj, elementType, cachedMemberInfos);
                    }
                }
                catch (Exception e)
                {
                    LogError("[toObjectArray] : " + e.Message);
                    return null;
                }
                finally
                {
                    array.SetValue(element, i++);
                }
            }

            return array;
        }
        else if (t.isList())
        {
            Type elementType = t.GetProperty("Item").PropertyType;
            var addMethod = t.GetMethod("Add");

            if (null == addMethod)
            {
                throw new InvalidCastException();
            }

            object target;

            ICollection collection = list as ICollection;
            if (null != collection)
            {
                if (0 == collection.Count)
                {
                    //return null;
                    return Activator.CreateInstance(t);
                }

                target = Activator.CreateInstance(t, new object[] { collection.Count });
            }
            else
            {
                target = Activator.CreateInstance(t);
            }

            if (null == cachedMemberInfos)
            {
                cachedMemberInfos = new Dictionary<Type, Dictionary<string, IMemberOperator>>();
            }

            var iter = list.GetEnumerator();

            while (iter.MoveNext())
            {
                object element = null;

                try
                {
                    object obj = iter.Current;
                    Type objType = obj.GetType();

                    if (elementType.Equals(objType))
                    {
                        element = obj;
                    }
                    else
                    {
                        element = convertObject(obj, elementType, cachedMemberInfos);
                    }
                }
                catch (Exception e)
                {
                    Util.LogError("[toObjectArray] : " + e.Message);
                    //return null;
                }
                finally
                {
                    addMethod.Invoke(target, new object[] { element });
                }
            }

            return target;
        }

        throw new InvalidCastException();
    }

    public static T convertObject<T>(object value, Dictionary<Type, Dictionary<string, IMemberOperator>> cachedMemberInfos = null)
    {
        return (T)convertObject(value, typeof(T), cachedMemberInfos);
    }

    /// <summary>
    /// convert value to target type
    /// </summary>
    public static object convertObject(object value, Type t, Dictionary<Type, Dictionary<string, IMemberOperator>> cachedMemberInfos)
    {
        Type valueType = value.GetType();

        if (t.Equals(valueType))
        {
            return value;
        }

        if (t.IsEnum)
        {
            if (value is string)
            {
                return Enum.Parse(t, (string)value, true);
            }

            return Enum.ToObject(t, value);
        }

        var dict = value as IDictionary;

        if (null != dict && t.isDictionary())
        {
            value = Activator.CreateInstance(t);

            var addMethod = t.GetMethod("Add");
            var paramInfo = addMethod.GetParameters();
            var targetKeyType = paramInfo[0].ParameterType;
            var targetValueType = paramInfo[1].ParameterType;

            var iter = dict.GetEnumerator();
            while (iter.MoveNext())
            {
                try
                {
                    var args = new object[]
                    {
                        convertObject(iter.Key, targetKeyType, cachedMemberInfos),
                        convertObject(iter.Value, targetValueType, cachedMemberInfos)
                    };

                    addMethod.Invoke(value, args);
                }
                catch (Exception e)
                {
                    Util.LogError("[convertObject] " + e.Message + "\n" + e.StackTrace);
                }
            }

        }
        else if (value is Dictionary<string, object>)
        {
            value = toObject(Activator.CreateInstance(t), t, value as Dictionary<string, object>, cachedMemberInfos);
        }
        else if (valueType.isType<ICollection>())
        {
            value = toObjectArray(value as ICollection, t, cachedMemberInfos);
        }
        else
        {
            value = Convert.ChangeType(value, t);
        }

        return value;
    }

    public static T toObject<T>(T target, Type targetType, Dictionary<string, object> dict,
       Dictionary<Type, Dictionary<string, IMemberOperator>> cachedMemberInfos = null) where T : class
    {
        if (null == dict)
        {
            return null;
        }

        if (null == cachedMemberInfos)
        {
            cachedMemberInfos = new Dictionary<Type, Dictionary<string, IMemberOperator>>();
        }

        Dictionary<string, IMemberOperator> members;

        if (!cachedMemberInfos.TryGetValue(targetType, out members))
        {
            members = new Dictionary<string, IMemberOperator>();

            foreach (PropertyInfo property in targetType.GetProperties())
            {
                if (!property.CanWrite)
                {
                    continue;
                }

                members.Add(property.Name, new PropertyOperator(property));
            }

            foreach (FieldInfo field in targetType.GetFields())
            {
                members.Add(field.Name, new FieldOperator(field));
            }

            cachedMemberInfos.Add(targetType, members);
        }

        object value;
        string name;
        IMemberOperator member;

        foreach (var pair in dict)
        {
            name = pair.Key;

            if (members.TryGetValue(name, out member))
            {
                try
                {
                    var type = member.type;
                    Dictionary<string, object> data = null;
                    value = null;

                    if ((type.IsClass && type != typeof(string)) || type.isStruct())
                    {
                        value = member.get(target);
                        if (null != value && !(value is IDictionary))
                        {
                            data = pair.Value as Dictionary<string, object>;
                        }
                    }

                    if (null != value && null != data)
                    {
                        value = toObject(value, type, data, cachedMemberInfos);
                    }
                    else
                    {
                        value = convertObject(pair.Value, type, cachedMemberInfos);
                    }

                    member.set(target, value);
                }
                catch (Exception e)
                {
                    LogWarning("could not set value to \"" + name + "\" of " + target.GetType().Name + " with " + (pair.Value ?? "null")
                        + "\n" + e.Message
                        + "\n" + e.StackTrace);
                }
            }
        }

        return target;
    }


    public interface IMemberOperator
    {
        Type type { get; }
        object get(object instance);
        void set(object instance, object value);
    }

    public static void ApplicationQuit()
    {
        Application.Quit();
    }

    #region getTexture&&Sprite
    public static Sprite getSpriteFromPath(string path, ResourceManager.UiLoadFrom loadFrom = ResourceManager.UiLoadFrom.Resources)
    {
        Texture2D sourceTexture = getTextureFromPath(path, loadFrom);
        if (null == sourceTexture)
        {
            Debug.LogError($"Get {path} Texture is null");
        }
        return getSpriteFromTexture(sourceTexture);
    }

    public static Texture2D getTextureFromPath(string path, ResourceManager.UiLoadFrom loadFrom = ResourceManager.UiLoadFrom.Resources)
    {
        return ResourceManager.instance.load<Texture2D>(path);
    }

    public static Sprite getSpriteFromTexture(Texture2D sourceTexture)
    {
        return Sprite.Create(sourceTexture, new Rect(0, 0, sourceTexture.width, sourceTexture.height), Vector2.zero);
    }
    #endregion

    #region class
    class PropertyOperator : IMemberOperator
    {
        public Type type { get { return property.PropertyType; } }

        PropertyInfo property;

        public PropertyOperator(PropertyInfo info)
        {
            property = info;
        }

        public object get(object instance)
        {
            return property.GetValue(instance, null);
        }

        public void set(object instance, object value)
        {
            property.SetValue(instance, value, null);
        }
    }

    class FieldOperator : IMemberOperator
    {
        public Type type { get { return field.FieldType; } }

        FieldInfo field;

        public FieldOperator(FieldInfo info)
        {
            field = info;
        }

        public object get(object instance)
        {
            return field.GetValue(instance);
        }

        public void set(object instance, object value)
        {
            field.SetValue(instance, value);
        }
    }

    #endregion

    #region checkType
    public static bool isStruct(this Type t)
    {
        return t.IsValueType && !t.IsEnum && !t.IsPrimitive;
    }

    public static bool isDictionary(this Type t)
    {
        return isType<IDictionary>(t);
    }

    public static bool isList(this Type t)
    {
        return isType<IList>(t);
    }

    public static bool isType<T>(this Type t)
    {
        return typeof(T).IsAssignableFrom(t);
    }

    #endregion

    #region Log
    //[Conditional("ENABLE_LOG")]
    public static void Log(string message)
    {
#if ENABLE_LOG
        Debug.Log(message);
#endif
    }

    public static void LogError(string message)
    {
#if ENABLE_LOG
        Debug.LogError(message);
#endif
    }

    public static void LogWarning(string message)
    {
#if ENABLE_LOG
        Debug.LogWarning(message);
#endif
    }

    public static void LogException(Exception message)
    {
#if ENABLE_LOG
        Debug.LogException(message);
#endif
    }

    public static void LogException(Exception message, UnityEngine.Object context)
    {
#if ENABLE_LOG
        Debug.LogException(message, context);
#endif
    }

    public static void LogWithTime(string message)
    {
#if ENABLE_LOG
        Debug.Log($"since time:{Time.realtimeSinceStartup}_{message}");
#endif
    }

    #endregion
}
