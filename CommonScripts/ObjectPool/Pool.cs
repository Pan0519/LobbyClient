using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 物件池
/// </summary>
public class Pool
{
    //push pop控管物件池物件
    private Stack<PoolObject> availableObjStack = new Stack<PoolObject>();

    private Transform rootTransform;
    private string poolName;
    private GameObject templateGameObject;

    private int objectsInUse = 0;

    public Pool(string poolName, Transform rootObjectTransform, GameObject templateGameObject, int initialCount)
    {
        this.poolName = poolName;
        this.templateGameObject = templateGameObject;
        this.rootTransform = rootObjectTransform;
        this.rootTransform.SetParent(rootObjectTransform, false);
        populatePool(initialCount);
    }

    // 填充物件池
    private void populatePool(int initialCount = 1)
    {
        for (int i = 0; i < initialCount; ++i)
        {
            addObjectToPool(createPoolObject(this.rootTransform));
        }
    }

    //將物件放入池子
    private void addObjectToPool(PoolObject po)
    {
        //po.cachedGameObject.setActiveWhenChange(false);
        po.cachedTransform.SetParent(this.rootTransform, false);
        availableObjStack.Push(po);
        po.isPooled = true;
    }

    private PoolObject createPoolObject(Transform parent)
    {
        GameObject gameObject = GameObject.Instantiate(templateGameObject, parent, false);
        PoolObject poolObj = gameObject.getOrAddComponent<PoolObject>();
        poolObj.setPoolName(poolName);
        return poolObj;
    }

    //o(1)
    public PoolObject nextAvailableObject(Transform parent = null)
    {
        PoolObject po;
        if (1 > availableObjStack.Count)
        {
            if (null == parent)
            {
                populatePool();
                po = availableObjStack.Pop();
            }
            else
            {
                po = createPoolObject(parent);
            }
        }
        else
        {
            po = availableObjStack.Pop();
            if (null != parent)
            {
                po.cachedTransform.SetParent(parent, false);
            }
        }

        ++objectsInUse;
        po.isPooled = false;

        return po;
    }

    //o(1)
    public void returnObjectToPool(PoolObject po)
    {
        if (poolName.Equals(po.poolName))
        {
            /* we could have used availableObjStack.Contains(po) to check if this object is in pool.
            * While that would have been more robust, it would have made this method O(n)
            */
            if (po.isPooled)
            {
                //if (UnityDefineBool.isEditor)
                //{
                //    Debug.LogWarning($"{po.cachedGameObject.name} is already in pool. Why are you trying to return it again? Check usage.");
                //}
                return;
            }

            --objectsInUse;
            addObjectToPool(po);
        }
        else
        {
            Debug.LogError($"Trying to add object to incorrect pool {po.poolName} {poolName}");
        }
    }

    public void clear()
    {
        while (availableObjStack.Count > 1)
        {
            var po = availableObjStack.Pop();
            po.Destroy();
        }

        objectsInUse = 0;
    }

    public void release()
    {
        clear();

        //GameObject.Destroy(templateGameObject);
    }
}
