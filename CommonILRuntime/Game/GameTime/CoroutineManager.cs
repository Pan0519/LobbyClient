using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonService;
using UnityEngine;

public class CoroutineManager
{
    /// <summary>
    /// 主要共用coroutine，不會被取消的，大部份共用此
    /// </summary>
    public static CoroutineScheduler scheduler = new CoroutineScheduler();
    /// <summary>
    /// 額外corotuine，可以被單獨取消
    /// </summary>
    public static Dictionary<string, CoroutineScheduler> dict_scheduler = new Dictionary<string, CoroutineScheduler>();

    #region 共用scheduler, 取消會全部協程取消
    public static CoroutineNode StartCoroutine(IEnumerator coroutine)
    {
        return scheduler.StartCoroutine(coroutine);
    }

    public static void StopCoroutine()
    {
        scheduler.StopAllCoroutines();

    }
    #endregion

    #region 獨立scheduler，可單獨取消協程
    /// <summary>
    /// 加入額外corotuine可獨立移除
    /// </summary>
    /// <param name="fiber"></param>
    /// <returns></returns>
    public static CoroutineNode AddCorotuine(IEnumerator fiber)
    {
        //Debug.Log("AddCorotuine:" + fiber.ToString());
        string key = fiber.ToString();
        if (dict_scheduler.ContainsKey(key))
        {
            dict_scheduler[key].StopAllCoroutines();
            return dict_scheduler[key].StartCoroutine(fiber);
        }
        else
        {
            dict_scheduler.Add(key, new CoroutineScheduler());
            return dict_scheduler[key].StartCoroutine(fiber);
        }
    }

    /// <summary>
    /// 移除獨立corotuine
    /// </summary>
    /// <param name="fiber"></param>
    public static void StopCorotuine(IEnumerator fiber)
    {
        Debug.Log("StopCorotuine:" + fiber.ToString());
        string key = fiber.ToString();
        if (dict_scheduler.ContainsKey(key))
        {
            dict_scheduler[key].StopAllCoroutines();
            dict_scheduler.Remove(key);
        }
    }

    public static CoroutineScheduler GetCoroutineScheduler(IEnumerator fiber)
    {
        string key = fiber.ToString();
        return dict_scheduler[key];
    }
    #endregion

    /// <summary>
    /// 清除所有協程
    /// </summary>
    public void ClearAllCorotuine()
    {
        //主共用corotuine
        scheduler.StopAllCoroutines();

        //額外獨立corotuine
        foreach (KeyValuePair<string, CoroutineScheduler> co in dict_scheduler)
        {
            co.Value.StopAllCoroutines();
        }
    }

    public static void Update()
    {
        if (DataStore.getInstance.gameTimeManager.IsPaused()) return;

        //主共用corotuine
        scheduler.UpdateAllCoroutines(Time.frameCount, Time.time);

        //額外獨立corotuine
        foreach (KeyValuePair<string, CoroutineScheduler> co in dict_scheduler)
        {
            co.Value.UpdateAllCoroutines(Time.frameCount, Time.time);
        }
    }

}

