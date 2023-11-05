using DG.Tweening;
using System;
using UnityEngine;

namespace TweenModule
{
    public class RoulatteTurnTable
    {
        public class TurntableInfo
        {
            public GameObject gameObject = null;
            public int totalChildCount;
            public int firstTurns;
            public float firstTurnTime;
            public int lastTurns;
            public int lastTurnTime;
            public TurnTableOverNotify overNotify = null;
            public TurnTableStoppingNotify stopNotify = null;
        }

        enum Stage
        {
            START,
            STOP,
            None,
        }

        public delegate void TurnTableOverNotify();
        public TurnTableOverNotify overNotify;

        public delegate void TurnTableStoppingNotify();
        public TurnTableStoppingNotify stoppingNotify;

        public bool isRolling { get; private set; }
        readonly int totalAngle = 360;

        Stage stage { get; set; }
        Ease easeType { get; set; }
        float playAngle { get; set; }
        float playTime
        {
            get
            {
                if (stage == Stage.START)
                {
                    return iFirstTurnTime;
                }
                else
                {
                    return iLastTurnTime;
                }
            }
        }

        int iFirstTurn { get; set; }
        int iLastTurn { get; set; }
        float iFirstTurnTime { get; set; }
        int iLastTurnTime { get; set; }
        public int targetID;

        float unitDegree = 0;
        int childCount = 0;

        GameObject turntable;

        public void setTargetID(int targetID)
        {
            this.targetID = (targetID <= 0) ? childCount : targetID;
        }

        public void setLastNums(int lastTurns, int lastTurnTime)
        {
            iLastTurn = lastTurns;
            iLastTurnTime = lastTurnTime;
        }

        public void setFirstNums(int firstTurns, float firstTurnTime)
        {
            iFirstTurn = firstTurns;
            iFirstTurnTime = firstTurnTime;
            //Debug.Log($"setFirstNums {firstTurns} , {firstTurnTime}");
        }

        public void initRoll(int totalChildCount, GameObject turntable)
        {
            childCount = totalChildCount;
            unitDegree = totalAngle / totalChildCount;
            this.turntable = turntable;
            resetTurnTable();
        }

        public void setRollOverNotify(TurnTableOverNotify notify)
        {
            overNotify = notify;
        }

        public void setStoppingNotify(TurnTableStoppingNotify notify)
        {
            stoppingNotify = notify;
        }

        public void resetTurnTable()
        {
            turntable.transform.rotation = Quaternion.Euler(Vector3.zero);
        }

        public void setTurntableInfo(TurntableInfo info)
        {
            initRoll(info.totalChildCount, info.gameObject);
            setFirstNums(info.firstTurns, info.firstTurnTime);
            setLastNums(info.lastTurns, info.lastTurnTime);
            if(null != info.overNotify)
            {
                setRollOverNotify(info.overNotify);
            }
            if (null != info.stopNotify)
            {
                setStoppingNotify(info.stopNotify);
            }
        }

        public void startRoll()
        {
            playAngle = iFirstTurn * totalAngle;
            stage = Stage.START;
            easeType = Ease.InQuint;
            isRolling = true;
            tweenTable();
        }

        public void startRollReverse()
        {
            playAngle = iFirstTurn * totalAngle;
            stage = Stage.START;
            easeType = Ease.InQuint;
            isRolling = true;
            tweenTable(-1f);
        }

        void tweenTable(float dir = 1f)
        {
            Action action = delegate
            {
                switch (stage)
                {
                    case Stage.START:
                        playAngle = ((childCount - targetID + 1) * unitDegree) + (totalAngle * iLastTurn);
                        easeType = Ease.OutQuint;
                        stage = Stage.STOP;
                        tweenTable(dir);
                        if (null != stoppingNotify)
                        {
                            stoppingNotify();
                        }
                        break;

                    case Stage.STOP:
                        if (null != overNotify)
                        {
                            overNotify();
                        }
                        isRolling = false;
                        stage = Stage.None;
                        break;
                }
            };

            Vector3 endRotation = dir * Vector3.back * playAngle;
            string tweenerID = TweenManager.rotateLocal(turntable, endRotation, playTime, easeType, action);
            TweenManager.SequenceJoin(tweenerID);
        }
    }
}
