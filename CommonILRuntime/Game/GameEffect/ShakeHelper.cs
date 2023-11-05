using DG.Tweening;
using System;
using UnityEngine;

namespace Game.Common
{
    public class ShakeHelper
    {
        static ShakeHelper _instance = new ShakeHelper();
        public static ShakeHelper instance { get { return _instance; } }
        Transform root { get { return UiRoot.instance.getNowScreenOrientationUIRoot(); } }
        Transform barRoot { get { return UiRoot.instance.getNowScreenOrientationBarRoot(); } }
        Transform gameMsgRoot { get { return UiRoot.instance.getNowScreenOrientationGameMsgRoot(); } }

        #region Default Value
        const float defaultTotalTime = 2;
        const float defaultSpeed = 0.1f;
        const float defaultAmount = 0.5f;
        const float defaultDelay = 0;
        const Action defaultCompleteCB = null;
        const Ease defaultEaseType = Ease.InOutQuad;
        #endregion

        #region Values
        float speed { get; set; } = defaultSpeed;
        float amount { get; set; } = defaultAmount;
        float allTime { get; set; } = defaultTotalTime;
        float delayTime { get; set; } = defaultDelay;
        Action completeCB { get; set; } = defaultCompleteCB;
        Ease easeType { get; set; } = defaultEaseType;
        #endregion

        float rand = 0;
        Vector3 targetPos;
        Vector3 formerPos;
        ShakeHelper()
        {
            formerPos = root.position;
        }

        /// <summary>
        /// 震動速度
        /// </summary>
        public ShakeHelper setSpeed(float speed)
        {
            this.speed = speed;
            return this;
        }
        /// <summary>
        /// 震動幅度
        /// </summary>
        public ShakeHelper setAmount(float amount)
        {
            this.amount = amount;
            return this;
        }
        /// <summary>
        /// 震動時間總長
        /// </summary>
        public ShakeHelper setTotalTime(float time)
        {
            this.allTime = time;
            return this;
        }
        /// <summary>
        /// 開始停頓時間
        /// </summary>
        public ShakeHelper setDelayTime(float delayTime)
        {
            this.delayTime = delayTime;
            return this;
        }
        /// <summary>
        /// 結束時執行的動作
        /// </summary>
        public ShakeHelper setEndCB(Action cb)
        {
            this.completeCB = cb;
            return this;
        }
        /// <summary>
        /// 震動加速度曲線
        /// </summary>
        public ShakeHelper setEaseType(Ease easeType)
        {
            this.easeType = easeType;
            return this;
        }
        /// <summary>
        /// 震動效果
        /// </summary>
        public ShakeHelper startShake()
        {
            targetPos = formerPos;
            TweenManager.tweenToFloat(0, allTime, allTime, delayTime: delayTime, onUpdate: shake, onComplete: shakeEnd, easeType: easeType);
            return this;
        }

        void shake(float time)
        {
            if (root.position == targetPos)
            {
                updateTargetPosToNextPoint(time);
            }

            var addSpeed = time <= 1 ? time : 1;
            var movePos = Vector3.MoveTowards(root.position, targetPos, speed * addSpeed);
            setTargetPos(movePos);
        }

        private void updateTargetPosToNextPoint(float time)
        {
            rand = UnityEngine.Random.Range(0, 360);
            var amountRange = time / allTime > 0.5f ? (1 - (time / allTime)) * 2 * amount : (time / allTime) * 2 * amount;
            var pointX = (Mathf.Sin(rand * Mathf.Deg2Rad) * amountRange) + formerPos.x;
            var pointY = (Mathf.Cos(rand * Mathf.Deg2Rad) * amountRange) + formerPos.y;

            targetPos = new Vector3(pointX, pointY, root.position.z);
        }

        void setTargetPos(Vector3 pos)
        {
            root.position = pos;
            barRoot.position = pos;
            gameMsgRoot.position = pos;
        }

        void shakeEnd()
        {
            TweenManager.tweenToFloat(1, 0, 0.5f, onUpdate: setFormerPos, onComplete: shakeComplete);
        }

        void shakeComplete()
        {
            setTargetPos(formerPos);
            if (null != completeCB)
            {
                completeCB();
            }
            resetAllValue();
        }

        void setFormerPos(float timer)
        {
            Vector2 moveValue = (formerPos - root.position) * timer;
            setTargetPos(new Vector3(root.position.x + moveValue.x, root.position.y + moveValue.y, root.position.z));
        }

        void resetAllValue()
        {
            speed = defaultSpeed;
            amount = defaultAmount;
            allTime = defaultTotalTime;
            delayTime = defaultDelay;
            completeCB = defaultCompleteCB;
            easeType = defaultEaseType;
        }
    }
}
