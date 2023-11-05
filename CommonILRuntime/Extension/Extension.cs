using System;
using UnityEngine;
using Math = System.Math;
using Debug = UnityLogUtility.Debug;
using UniRx;

namespace CommonILRuntime.Extension
{

    /// <summary>
    /// 擴充類
    /// </summary>
    public static partial class Extention
    {
        /*
        /// <summary>
        /// 獲取列舉描述
        /// </summary>
        /// <param name="value">列舉值</param>
        /// <returns></returns>
        public static int ToInt(this Enum value)
        {
            return value.ToInt();
        }*/

        /// <summary>
        /// 將毫秒轉成秒
        /// </summary>
        /// <param name="value">傳入毫秒</param>
        /// <returns>回傳秒數</returns>
        public static float ConvertSeconds(this int value)
        {
            return value / 1000f;
        }

        /// <summary>
        /// 主場金額轉換縮寫字串(2位數以下顯示小數點)
        /// </summary>
        public static string ConvertUnit(this ulong value, bool haveFloat = true, int showLong = 3)
        {
            return value.convertToCurrencyUnit(showLong, haveFloat, 2);
        }

        /// <summary>
        /// decimal轉成ulong
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static ulong ConvertToUlong(this decimal value)
        {
            return (ulong)value;
        }

        /// <summary>
        /// decimal二維陣列轉成ulong二維陣列
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static ulong[][] ConvertToUlongTwoDismission(this decimal[][] value)
        {
            if (null == value)
            {
                return null;
            }

            int valueLength = value.Length;
            int memberLength = 0;
            ulong[][] result = new ulong[valueLength][];
            ulong[] resultMember = null;
            decimal[] member = null;

            for(int i = 0;i < valueLength;++i)
            {
                member = value[i];
                memberLength = member.Length;
                resultMember = new ulong[memberLength];
                for (int j = 0; j < memberLength; ++j)
                {
                    resultMember[j] = (ulong)member[j];
                }
                result[i] = resultMember;
            }

            return result;
        }

        /// <summary>
        /// decimal一維陣列轉成ulong一維陣列
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static ulong[] ConvertToUlongOneDismission(this decimal[] value)
        {
            if (null == value)
            {
                return null;
            }

            int valueLength = value.Length;
            ulong[] result = new ulong[valueLength];

            for (int i = 0; i < valueLength; ++i)
            {
                result[i] = (ulong)value[i];
            }

            return result;
        }

        /// <summary>
        /// 金額轉換縮寫字串
        /// </summary>
        ///  /// <param name="showLong">最多顯示幾位數</param>
        /// <param name="havePoint">是否要顯示小數點</param>
        /// <param name="pointDigits">幾位數以下顯示小數點</param>

        public static string convertToCurrencyUnit(this ulong value, int showLong, bool havePoint, int pointDigits = 0)
        {
            var maxLong = Math.Pow(10, showLong);
            if (value < maxLong)
            {
                return value.ToString("N0");
            }

            var unitValue = Enum.GetValues(typeof(CurrencyUnit)).GetEnumerator();
            double finalValue = 0;
            string unit = string.Empty;
            while (unitValue.MoveNext())
            {
                finalValue = (double)value / (double)unitValue.Current;
                if (finalValue < maxLong)
                {
                    unit = $"{(CurrencyUnit)unitValue.Current}";
                    break;
                }
            }
            if (havePoint && Math.Truncate(finalValue).ToString().Length < pointDigits)
            {
                //固定顯示小數點後一位
                double maxPointDigits = Math.Pow(10, pointDigits - 1);
                var point = finalValue * maxPointDigits;
                point = Math.Truncate(point);
                finalValue = point / maxPointDigits;
                string result = finalValue.ToString("N1");
                if (result.EndsWith(".0"))
                {
                    int spliteId = result.LastIndexOf('.');
                    result = result.Substring(0, spliteId);
                }
                return $"{result}{unit}";
            }

            return $"{Math.Truncate(finalValue).ToString("N0")}{unit}";
        }

        enum CurrencyUnit : long
        {
            K = 1000,
            M = 1000000,
            B = 1000000000,
        }

        public static void cleanRoot(RectTransform root)
        {
            for (int i = 0; i < root.childCount; i++)
            {
                var trans = root.GetChild(i);
                trans.gameObject.SetActive(false);
                GameObject.Destroy(trans.gameObject);
            }
            root.DetachChildren();
        }


        public class PausableObj
        {
            public T SendToFile<T>(IObservable<bool> pauser, IObservable<T> ps)
            {
                try
                {
                    var paused = new SerialDisposable();
                    var values = new ReplaySubject<T>();
                    Func<bool, IObservable<T>> switcher = b =>
                    {
                        if (b)
                        {
                            Debug.Log("SendToFile: true");
                            values.Dispose();
                            values = new ReplaySubject<T>();
                            paused.Disposable = ps.Subscribe(values);
                            return Observable.Empty<T>();
                        }
                        else
                        {
                            Debug.Log("SendToFile: false");
                            return values.Concat(ps);
                        }
                    };

                    return (T)pauser.StartWith(false).DistinctUntilChanged()
                        .Select(p => switcher(p))
                        .Switch();
                }
                catch
                {
                    return default(T);
                }
            }

            public T ReplaySubject<T>(IDisposable subscriptions, IObservable<bool> isPausedStream, IObservable<T> stream , bool startPaused)
            {
                try
                {
                    var replaySubjects = new SingleAssignmentDisposable();

                    Func<ReplaySubject<T>> replaySubjectFactory = () =>
                    {
                        var rs = new ReplaySubject<T>();

                        replaySubjects.Disposable = rs;
                        subscriptions = stream.Subscribe(rs);

                        return rs;
                    };

                    var replaySubject = replaySubjectFactory();

                    Func<bool, IObservable<T>> switcher = isPaused =>
                    {
                        if (isPaused)
                        {
                            replaySubject = replaySubjectFactory();

                            return Observable.Empty<T>();
                        }
                        else
                        {
                            return replaySubject.Concat(stream);
                        }
                    };

                    return (T)isPausedStream
                        .StartWith(startPaused)
                        .DistinctUntilChanged()
                        .Select(switcher)
                        .Switch();
                }
                catch
                {
                    return default(T);
                }
            }
        }

    }
}
