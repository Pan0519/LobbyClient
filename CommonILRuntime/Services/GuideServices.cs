using UniRx;
using System.Collections.Generic;
using System;
using UnityEngine;
using CommonService;
using Debug = UnityLogUtility.Debug;

namespace Services
{
    public class GuideServices
    {
        public Subject<bool> playBtnEnableSubject = new Subject<bool>();
        public Subject<bool> guideSpinClickSub = new Subject<bool>();
        public Subject<bool> setBetBtnEnableSub = new Subject<bool>();
        public Subject<bool> guideMaxBetClickSub = new Subject<bool>();
        public Subject<bool> gameSpinOnClickSub = new Subject<bool>();
        public Subject<bool> gameMaxBetEnableSub = new Subject<bool>();
        public Subject<bool> gameBetGroupActiveSub = new Subject<bool>();
        public Subject<bool> lvupSub = new Subject<bool>();
        public Subject<bool> noticeWinWindowsStateSub = new Subject<bool>();


        public readonly string saveGuideKey = "GuideStep";
        readonly string saveGameGuideSpinKey = "GameGuideSpin";
        public readonly string saveGameGuideKey = "GameGuideStep";


        #region GameGuideStatus
        public Subject<int> nowGameStep = new Subject<int>();
        int gameStep = 0;
        public GameGuideStatus nowGameStatus = GameGuideStatus.Completed;

        public void saveSpinCount(int spinCount)
        {
            PlayerPrefs.SetInt(saveGameGuideSpinKey, spinCount);
        }

        public int getSaveSpinCount()
        {
            if (PlayerPrefs.HasKey(saveGameGuideSpinKey))
            {
                return PlayerPrefs.GetInt(saveGameGuideSpinKey);
            }

            return 0;
        }

        public void gameToNextStep()
        {
            gameStep++;
            nowGameStep.OnNext(gameStep);
            saveGameStatus();
        }

        public void setNowGameGuideStep(int nowStep)
        {
            gameStep = nowStep;
            saveGameStatus();
        }

        void saveGameStatus()
        {
            nowGameStatus = (GameGuideStatus)gameStep;
            PlayerPrefs.SetString(saveGameGuideKey, nowGameStatus.ToString());
        }

        public GameGuideStatus getSaveGameGuideStep()
        {
            nowGameStatus = GameGuideStatus.Spin;
            if (PlayerPrefs.HasKey(saveGameGuideKey))
            {
                string saveValue = PlayerPrefs.GetString(saveGameGuideKey);
                UtilServices.enumParse(saveValue, out nowGameStatus);
            }
            return nowGameStatus;
        }

        #endregion

        #region TutorialStatus
        public GuideStatus nowStatus = GuideStatus.Completed;
        public Subject<GuideStatus> tutorialStatusSub = new Subject<GuideStatus>();
        public Subject<int> nowStepSub = new Subject<int>();
        int nowStep = 0;
        static Dictionary<string, GuideStatus> statusMap = new Dictionary<string, GuideStatus>()
        {
            { GuideStatus.Introduce.ToString(),GuideStatus.Introduce},
            { GuideStatus.Daily.ToString(),GuideStatus.Daily},
            { GuideStatus.StayGame.ToString(),GuideStatus.StayGame},
            { GuideStatus.SaveDog.ToString(),GuideStatus.SaveDog},
            { GuideStatus.Completed.ToString(),GuideStatus.Completed}
        };

        public void skipGuideStep()
        {
            setNowStatus(GuideStatus.Completed.ToString());
            setNowGameGuideStep((int)GameGuideStatus.Completed);

        }

        public void toNextStep()
        {
            nowStep++;
            nowStepSub.OnNext(nowStep);
            var key = (GuideStatus)nowStep;
            setNowStatus(key.ToString());
        }

        public void setNowStep(int step)
        {
            nowStep = step;
        }

        public GuideStatus getSaveGuideStatus()
        {
            nowStatus = GuideStatus.Introduce;
            if (PlayerPrefs.HasKey(saveGuideKey))
            {
                var status = PlayerPrefs.GetString(saveGuideKey);
                nowStatus = convertStatus(status);
            }
            setNowStep((int)nowStatus);
            return nowStatus;
        }

        public void setNowStatus(string statusStr)
        {
            if (statusMap.TryGetValue(statusStr, out nowStatus))
            {
                tutorialStatusSub.OnNext(nowStatus);
            }
            else
            {
                nowStatus = GuideStatus.Completed;
            }
            PlayerPrefs.SetString(saveGuideKey, statusStr);
        }
        public GuideStatus convertStatus(string statusStr)
        {
            GuideStatus result = GuideStatus.None;
            statusMap.TryGetValue(statusStr, out result);
            return result;
        }
        #endregion

        #region OldTutorial
        public bool isSpinBtnEnable { get; private set; }
        public void subjectPlayBtnEnable(bool enable)
        {
            isSpinBtnEnable = enable;
            playBtnEnableSubject.OnNext(enable);
        }

        public void guideSpinClick()
        {
            guideSpinClickSub.OnNext(true);
        }

        public void setBetBtnsEnable(bool enable)
        {
            setBetBtnEnableSub.OnNext(enable);
        }

        public void guideMaxBetClick()
        {
            guideMaxBetClickSub.OnNext(true);
        }

        public void gameBarSpinClick()
        {
            gameSpinOnClickSub.OnNext(true);
        }

        public void isLvUP(bool lvup)
        {
            lvupSub.OnNext(lvup);
        }

        public void setMaxBetEnable(bool enable)
        {
            gameMaxBetEnableSub.OnNext(enable);
        }

        public void setGameBtnsGroupActive(bool active)
        {
            gameBetGroupActiveSub.OnNext(active);
        }

        public void noticeWinWindowsState(bool haveWinWindows)
        {
            noticeWinWindowsStateSub.OnNext(haveWinWindows);
        }
        #endregion


    }
    public enum GuideStatus
    {
        None,
        Introduce,
        Daily,
        StayGame,
        SaveDog,
        Completed,
    }

    public enum GameGuideStatus
    {
        Spin,
        Max,
        Completed,
    }
}
