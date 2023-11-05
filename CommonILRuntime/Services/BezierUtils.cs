using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Debug = UnityLogUtility.Debug;

namespace Services
{
    public class BezierUtils
    {
        /// <summary>
        /// 根據T值，計算貝塞爾曲線上面相對應的點
        /// </summary>
        /// <param name="t">T值</param>
        /// <param name="startPoint">起始點</param>
        /// <param name="middlePos">控制點</param>
        /// <param name="endPos">目標點</param>
        /// <returns>根據T值計算出來的貝賽爾曲線點</returns>
        private static Vector3 CalculateCubicBezierPoint(float t, Vector3 startPoint, Vector3 middlePos, Vector3 endPos)
        {
            float u = 1 - t;
            float tt = t * t;
            float uu = u * u;

            Vector3 p = uu * startPoint;
            p += 2 * u * t * middlePos;
            p += tt * endPos;

            return p;
        }

        /// <summary>
        /// 獲取儲存貝塞爾曲線點的陣列
        /// </summary>
        /// <param name="startPoint">起始點</param>
        /// <param name="controlPoint">控制點</param>
        /// <param name="endPoint">目標點</param>
        /// <param name="segmentNum">取樣點的數量</param>
        /// <returns>儲存貝塞爾曲線點的陣列</returns>
        public static Vector3[] GetBeizerList(Vector3 startPoint, Vector3 controlPoint, Vector3 endPoint, int segmentNum)
        {
            Vector3[] pixelPath = new Vector3[segmentNum];
            for (int i = 1; i <= segmentNum; i++)
            {
                float t = i / (float)segmentNum;
                pixelPath[i - 1] = CalculateCubicBezierPoint(t, startPoint, controlPoint, endPoint);
                Debug.Log($"pixel : {pixelPath[i - 1] }");
            }
            return pixelPath;
        }

        /// <summary>
        /// 取得弧線路徑
        /// **************O*************
        /// *******S************T*******
        /// S -> O -> T 回傳 O:Vector2
        /// </summary>
        /// <param name="shiftQuant">弧線向量大小</param>
        public static Vector2 getNormalDirShiftPoint(Vector2 source, Vector2 target, float shiftQuant, bool clamp = false)
        {
            var middle = (source + target) / 2f;
            var delta = middle - source;
            if (delta.magnitude != 0)
            {
                var normalVec = new Vector2(-delta.y, delta.x);
                if (clamp)    //是否確保不超過畫面上下界
                {
                    var shiftDir = Math.Abs(shiftQuant) / shiftQuant;    //1 or -1
                    delta.Normalize();
                    var dotval = Math.Abs(Vector2.Dot(delta, new Vector2(0, 1)));
                    //用內積算出delta對Y軸的投影向量
                    //投影向量長 / delta 長度 求出 delta 與 Y軸垂直程度
                    //與Y軸垂直度越低，代表越接近水平飛幣，此時震幅不可過大，避免飛出面範圍
                    var scaling = dotval / delta.magnitude;
                    shiftQuant *= scaling;
                    shiftQuant = Math.Min(shiftQuant, 1f);
                }
                return middle + normalVec * shiftQuant;
            }
            return target;
        }
    }
}
