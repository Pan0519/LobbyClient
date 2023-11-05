using DG.Tweening;
using UnityEngine;
using System;
using System.Collections.Generic;
using DG.Tweening.Core;

public static class TweenManager
{
    static Dictionary<string, Tweener> tweenerDict = new Dictionary<string, Tweener>();

    static int objID = 0;
    static int getObjID
    {
        get
        {
            if (objID >= 999)
            {
                objID = 0;
            }

            objID++;
            return objID;
        }
    }

    static Sequence _sequence = null;
    static Sequence sequence
    {
        get
        {
            if (null == _sequence)
            {
                _sequence = DOTween.Sequence();
            }

            return _sequence;
        }
    }

    #region DoTween
    public static string tweenToLong(long startValue, long endValue, float durationTime, Action<long> onUpdate = null, TweenCallback onComplete = null)
    {
        Tweener tw = DOTween.To(() => startValue, (val) =>
        {
            if (null != onUpdate)
            {
                onUpdate(val);
            }
        }, endValue, durationTime)
            .SetUpdate(UpdateType.Normal)
            .SetEase(Ease.Linear);

        tw.OnComplete(() =>
        {
            if (null != onUpdate)
            {
                onUpdate(endValue);
            }
            if (null != onComplete)
            {
                onComplete();
            }
            removeDictTween(tw);
        });

        return twAddDict(tw);
    }
    public static string tweenToFloat(float startValue, float endValue, float durationTime, int loopTimes = 0, float delayTime = 0, Action<float> onUpdate = null, TweenCallback onComplete = null, Action onStart = null, Ease easeType = Ease.Linear)
    {
        Tweener tw = DOTween.To(() => startValue, (val) =>
        {
            if (null != onUpdate)
            {
                onUpdate(val);
            }
        }, endValue, durationTime)
            .SetUpdate(UpdateType.Normal)
            .SetLoops(loopTimes)
            .SetDelay(delayTime)
            .SetEase(easeType);

        tw.OnComplete(() =>
        {
            removeDictTween(tw);
            if (null != onComplete)
            {
                onComplete();
            }
        });

        tw.OnStart(() =>
        {
            if (null == onStart)
            {
                return;
            }
            onStart();
        });

        return twAddDict(tw);
    }
    public static string tweenToUlong(ulong startValue, ulong endValue, float durationTime, int loopTimes = 0, float delayTime = 0, Action<ulong> onUpdate = null, TweenCallback onComplete = null, Action onStart = null, Ease easeType = Ease.Linear)
    {
        Tweener tw = DOTween.To(() => startValue, (val) =>
        {
            if (null != onUpdate)
            {
                onUpdate(val);
            }
        }, endValue, durationTime)
            .SetUpdate(UpdateType.Normal)
            .SetLoops(loopTimes)
            .SetDelay(delayTime)
            .SetEase(easeType);

        tw.OnComplete(() =>
        {
            removeDictTween(tw);
            if (null != onComplete)
            {
                onComplete();
            }
        });

        tw.OnStart(() =>
        {
            if (null == onStart)
            {
                return;
            }
            onStart();
        });

        return twAddDict(tw);
    }
    public static string anchPosMoveY(this RectTransform transform, float endValue, float durationTime, int loops = 1, Action onComplete = null, Ease easeType = Ease.Linear)
    {
        Tweener tw = DOTween.To(() => transform.anchoredPosition, (vector2) =>
        {
            transform.anchoredPosition = vector2;
        }, new Vector2(transform.anchoredPosition.x, endValue), durationTime)
            .SetUpdate(UpdateType.Normal)
            .SetEase(easeType);

        tw.OnComplete(() =>
        {
            removeDictTween(tw);
            if (null != onComplete)
            {
                onComplete();
            }
        });

        return twAddDict(tw);
    }

    public static string anchPosMoveX(this RectTransform transform, float endValue, float durationTime, int loops = 1, Action onComplete = null, Ease easeType = Ease.Linear)
    {
        Tweener tweener = DOTween.To(() => transform.anchoredPosition, (vector2) =>
        {
            transform.anchoredPosition = vector2;
        }, new Vector2(endValue, transform.anchoredPosition.y), durationTime)
            .SetUpdate(UpdateType.Normal)
            .SetEase(easeType);

        tweener.OnComplete(() =>
        {
            removeDictTween(tweener);
            if (null != onComplete)
            {
                onComplete();
            }
        });

        return twAddDict(tweener);
    }

    public static string anchPosMove(this RectTransform transform, Vector2 endPos, float durationTime, Action onComplete = null, Ease easeType = Ease.Linear)
    {
        Tweener tweener = DOTween.To(() => transform.anchoredPosition, (vector2) =>
        {
            transform.anchoredPosition = vector2;
        }, endPos, durationTime)
            .SetUpdate(UpdateType.Normal)
            .SetEase(easeType);

        tweener.OnComplete(() =>
        {
            removeDictTween(tweener);
            if (null != onComplete)
            {
                onComplete();
            }
        });

        return twAddDict(tweener);
    }

    public static string movePath(this RectTransform transform, Vector3[] paths, float durationTime, bool closePath = true, Action onComplete = null)
    {
        Tweener tw = transform.DOPath(paths, durationTime, pathMode: PathMode.TopDown2D)
            .SetOptions(closePath);

        tw.OnComplete(() =>
        {
            removeDictTween(tw);
            if (null != onComplete)
            {
                onComplete();
            }
        });

        return twAddDict(tw);
    }

    public static string rotateLocal(this GameObject transform, Vector3 endValue, float durationTime, Ease easeType, Action onComplete = null)
    {
        Tweener tweener = transform.transform.DOLocalRotate(endValue, durationTime, RotateMode.FastBeyond360).
            SetEase(easeType);

        tweener.OnComplete(() =>
           {
               removeDictTween(tweener);
               if (null != onComplete)
               {
                   onComplete();
               }
           });

        return twAddDict(tweener);
    }


    public static string movePos(this Transform transform, Vector3 endpos, float durationTime, int loops = 1, Action onComplete = null, Ease easeType = Ease.Linear)
    {
        Tweener tweener = DOTween.To(() => transform.position, (vector3) =>
        {
            transform.position = vector3;
        }, endpos, durationTime)
            .SetUpdate(UpdateType.Normal)
            .SetEase(easeType);

        tweener.OnComplete(() =>
        {
            if (null != onComplete)
            {
                onComplete();
            }
        });

        return twAddDict(tweener);
    }

    /// <summary> 定點震動效果 </summary>
    /// <param name="transform">對象</param>
    /// <param name="directionPos">方向</param>
    /// <param name="durationTime">持續秒數</param>
    /// <param name="randomNess">幅度</param>
    public static string shakePos(this Transform transform, Vector3 directionPos, float durationTime, float randomNess)
    {
        Tweener tweener = DOTween.Shake(() => transform.position, (vector3) =>
        {
            transform.position = vector3;
        }, durationTime, strength: directionPos, randomness: randomNess)
            .SetUpdate(UpdateType.Normal);

        return twAddDict(tweener);
    }
    #endregion

    static string twAddDict(Tweener tweener)
    {
        string tweenerID = getObjID.ToString();
        tweener.SetId(tweenerID);
        if (!tweenerDict.ContainsKey(tweenerID))
        {
            tweenerDict.Add(tweenerID, tweener);
        }
        else
        {
            tweenerDict[tweenerID] = tweener;
        }
        return tweenerID;
    }

    static void removeDictTween(Tween tweener)
    {
        if (null == tweener)
        {
            return;
        }
        string tweenerID = tweener.stringId;
        if (string.IsNullOrEmpty(tweenerID))
        {
            return;
        }
        tweenerDict.Remove(tweenerID);
    }

    #region sequence
    public static void SequenceAppend(string twID, Action onAppendCB = null, bool autoKill = true)
    {
        Tweener tweener;
        if (tweenerDict.TryGetValue(twID, out tweener))
        {
            sequence.Append(tweener).AppendCallback(() =>
            {
                if (onAppendCB != null)
                {
                    onAppendCB();
                }
            }).SetAutoKill(autoKill);
            return;
        }
        Debug.LogError($"SequenceAppend Get {twID} is null");
    }

    public static void SequenceJoin(string twID, bool autoKill = true)
    {
        Tweener tweener;
        if (tweenerDict.TryGetValue(twID, out tweener))
        {
            sequence.Join(tweener).SetAutoKill(autoKill);
            return;
        }
        Debug.LogError($"SequenceJoin Get {twID} is null");
    }

    public static void SequenceAppendCall(TweenCallback call)
    {
        sequence.AppendCallback(call);
    }

    public static void SequenceInsertCall(TweenCallback call, float insertTime = 0)
    {
        sequence.InsertCallback(insertTime, call);
    }
    #endregion

    public static void tweenPauseByID(params string[] tweenIDs)
    {
        for (int i = 0; i < tweenIDs.Length; ++i)
        {
            Tween tw = getTweener(tweenIDs[i]);
            if (null == tw)
            {
                continue;
            }
            tw.Pause();
        }
    }

    public static void tweenPlayByID(params string[] tweenIDs)
    {
        for (int i = 0; i < tweenIDs.Length; ++i)
        {
            Tween tw = getTweener(tweenIDs[i]);
            if (null == tw)
            {
                continue;
            }
            tw.Play();
        }
    }

    public static void tweenPlay(string twID)
    {
        Tweener tweener = getTweener(twID);
        if (null != tweener)
        {
            tweener.Play();
        }
    }

    public static void pauseAll()
    {
        DOTween.PauseAll();
    }

    public static void playAll()
    {
        DOTween.PlayAll();
    }

    public static void killAll()
    {
        DOTween.KillAll(false);
        tweenerDict.Clear();
    }

    public static void tweenKill(string twID, bool complete = false)
    {
        Tweener tweener = getTweener(twID);
        if (null == tweener)
        {
            return;
        }
        removeDictTween(tweener);
        tweener.Kill(complete);
    }

    public static void completeAll()
    {
        DOTween.CompleteAll();
    }

    public static void tweenComplete(string twID)
    {
        Tweener tweener = getTweener(twID);
        if (null == tweener)
        {
            return;
        }
        tweener.Complete();
    }

    public static List<Tween> playingTweens()
    {
        return DOTween.PlayingTweens();
    }

    public static Tweener getTweener(string tweenID)
    {
        if (string.IsNullOrEmpty(tweenID))
        {
            return null;
        }
        Tweener result = null;
        if (tweenerDict.TryGetValue(tweenID, out result))
        {
            return result;
        }
        return null;
    }
}
