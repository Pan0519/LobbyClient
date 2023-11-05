using System.Collections.Generic;
using System;
using UnityEngine;

public class TimerItem
{
    public int id;
    public Action func;
    public float interval;
    public int loopCount;
    public bool isLoop;
    public float tmLastCall;
}

public class TimerManager : MonoSingleton<TimerManager>
{
    List<TimerItem> mapCalls = new List<TimerItem>();

    int iID { get; set; } = 0;

    public int addCallBack(Action func, float interval, int loops = 1, bool isLoop = false)
    {
        TimerItem item = setTimerItem(func, interval, loops, isLoop);
        return item.id;
    }

    public bool dropCallback(int id)
    {
        int listId = mapCalls.FindIndex(item => item.id == id);
        if (listId >= 0)
        {
            mapCalls.RemoveAt(listId);
            return true;
        }
        return false;
    }

    TimerItem setTimerItem(Action func, float interval, int loops, bool isLoop)
    {
        TimerItem item = new TimerItem();
        item.func = func;
        item.id = iID++;
        item.interval = interval;
        item.loopCount = loops;
        item.isLoop = isLoop;

        mapCalls.Add(item);

        return item;
    }

    private void Update()
    {
        if (mapCalls.Count <= 0)
        {
            return;
        }

        float time = Time.time;

        for (int i = mapCalls.Count - 1; i >= 0; --i)
        {
            TimerItem item = mapCalls[i];
            float useTime = time;
            if (useTime < item.tmLastCall + item.interval)
            {
                continue;
            }

            item.tmLastCall = time;

            if (null != item.func)
            {
                item.func();
            }

            if (item.isLoop)
            {
                continue;
            }
            
            item.loopCount--;
            if (item.loopCount <= 0)
            {
                dropCallback(item.id);
            }
        }
    }
}
