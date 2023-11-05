using CommonILRuntime.Module;
using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System.Text;

namespace Game.Common
{
    public class BezierPresenter : NoBindingNodePresenter
    {
        public List<Vector2> bezierPoints = new List<Vector2>();    //需三點以上的座標才能呈現曲線效果
        Action endCallback;
        public virtual Ease easeType { get; set; } = Ease.Linear;

        public string bezierTweenID { get; private set; }

        public bool isDrawLine = false;

        //移動當前物件跟著貝思曲線走
        public void moveBezierLine(float time, Action callback = null)
        {
            if (bezierPoints.Count < 3)
            {
                Debug.LogError($"moveBezierLine Error, bezierPoints.Count({bezierPoints.Count}) < 3");
                return;
            }

            endCallback = callback;
            uiTransform.position = setPos(bezierPoints[0]);
            //Debug.Log($"moveBezierLine {uiGameObject.name}");
            bezierTweenID = TweenManager.tweenToFloat(0, 1, time, onUpdate: setCalculateCubicBezierPoint, onComplete: completeMove, easeType: easeType);
        }

        void completeMove()
        {
            if (null != endCallback)
            {
                endCallback();
            }
            endBezierLine();
            if (isDrawLine && ApplicationConfig.NowRuntimePlatform == RuntimePlatform.WindowsEditor)
            {
                setPos(drawLinePos);
                drawLinePos.Clear();
            }
        }

        //移動完後所進行的動作
        public virtual void endBezierLine() { }
        //更換座標
        void setCalculateCubicBezierPoint(float frame)
        {
            if (null == uiGameObject)
            {
                if (!string.IsNullOrEmpty(bezierTweenID))
                {
                    TweenManager.tweenKill(bezierTweenID);
                }
                return;
            }
            bezierMove(CalculateCubicBezierPoint(frame));
        }
        public List<Vector3> drawLinePos = new List<Vector3>();
        public virtual void bezierMove(Vector2 newPos)
        {
            try
            {
                if (null == uiRectTransform || null == uiGameObject)
                {
                    Debug.LogError("bezierMove Error null == uiRectTransform");
                    completeMove();
                    return;
                }

                var transPos = setPos(newPos);
                if (isDrawLine && ApplicationConfig.NowRuntimePlatform == RuntimePlatform.WindowsEditor)
                {
                    drawLinePos.Add(transPos);
                }
                uiRectTransform.position = transPos;
            }
            catch (Exception e)
            {
                Debug.LogError($"bezierMove Exception : {e.Message}\nuiGameObject null Exception? {uiGameObject == null}");
            }
        }

        Vector3 setPos(Vector2 newPos)
        {
            var transPos = uiRectTransform.position;
            transPos.Set(newPos.x, newPos.y, transPos.z);
            return transPos;
        }

        /// <summary>
        /// 貝思曲線運算公式
        /// </summary>
        /// <param name="frame"></param>
        /// <returns></returns>
        Vector2 CalculateCubicBezierPoint(float frame)
        {
            Vector2 pos = new Vector2();

            float u = 1 - frame;

            for (int i = 0; i < bezierPoints.Count; ++i)
            {
                var bezier = (float)(PermutationsC(i) * Math.Pow(u, (bezierPoints.Count - 1) - i) * Math.Pow(frame, i));
                pos += bezierPoints[i] * bezier;
            }
            return pos;
        }
        //排列組合C公式運算
        int PermutationsC(int numberMin)
        {
            return JieCheng(bezierPoints.Count - 1) / (JieCheng(numberMin) * JieCheng((bezierPoints.Count - 1) - numberMin));
        }
        //乘階公式運算
        int JieCheng(int number)
        {
            int result = 1;

            if (number <= 0)
            {
                return result;
            }

            for (int i = number; i >= 1; i--)
            {
                result = result * i;
            }
            return result;
        }

        #region DrawLine
        void setPos(List<Vector3> pos)
        {
            var render = drawLineRender();
            render.positionCount = pos.Count;
            render.SetPositions(pos.ToArray());
        }

        LineRenderer drawLineRender()
        {
            var lineObj = new GameObject("Line");
            lineObj.transform.SetParent(uiTransform);
            LineRenderer lineRenderer = lineObj.getOrAddComponent<LineRenderer>();
            lineRenderer.startWidth = 0.1f;
            lineRenderer.endWidth = 0.1f;
            return lineRenderer;
        }
        #endregion
    }
}
