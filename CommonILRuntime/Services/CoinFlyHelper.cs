using CommonILRuntime.BindingModule;
using CommonILRuntime.CommonPresenter;
using System;
using UnityEngine;
using UniRx;

namespace CommonILRuntime.Services
{
    public static class CoinFlyHelper
    {
        /// <summary>
        /// 正面S飛行
        /// </summary>
        public static void frontSFly(RectTransform sourceTrans, ulong sourceValue, ulong targetValue, float duration = 1f, bool haveBackground = false, Action onComplete = null, bool isPlaySound = true)
        {
            if (checkIsCoinFly(onComplete))
            {
                return;
            }
            setFlyCoinIsPlaySound(isPlaySound);
            UiManager.getPresenter<FlyCoinPresenter>().frontSFly(sourceTrans, sourceValue, targetValue, duration, haveBackground, onComplete);
        }
        /// <summary>
        /// 反向S飛行
        /// </summary>
        public static void obverseSFly(RectTransform sourceTrans, ulong sourceValue, ulong targetValue, float duration = 1f, bool haveBackground = false, Action onComplete = null, bool isPlaySound = true)
        {
            if (checkIsCoinFly(onComplete))
            {
                return;
            }
            setFlyCoinIsPlaySound(isPlaySound);
            UiManager.getPresenter<FlyCoinPresenter>().obverseSFly(sourceTrans, sourceValue, targetValue, duration, haveBackground, onComplete);
        }

        public static void curveFly(RectTransform sourceTrans, ulong sourceValue, ulong targetValue, float duration = 1f, bool haveBackground = false, Action onComplete = null, bool isPlaySound = true)
        {
            if (checkIsCoinFly(onComplete))
            {
                return;
            }
            setFlyCoinIsPlaySound(isPlaySound);
            UiManager.getPresenter<FlyCoinPresenter>().curveFly(sourceTrans, sourceValue, targetValue, duration, haveBackground, onComplete);
        }

        public static void reverseCurveFly(RectTransform sourceTrans, ulong sourceValue, ulong targetValue, float duration = 1f, bool haveBackground = false, Action onComplete = null, bool isPlaySound = true)
        {
            if (checkIsCoinFly(onComplete))
            {
                return;
            }
            setFlyCoinIsPlaySound(isPlaySound);
            UiManager.getPresenter<FlyCoinPresenter>().reverseCurveFly(sourceTrans, sourceValue, targetValue, duration, haveBackground, onComplete);
        }

        static void setFlyCoinIsPlaySound(bool isPlay)
        {
            UiManager.getPresenter<FlyCoinPresenter>().setIsPlaySound(isPlay);
        }

        public static bool checkIsCoinFly(Action complete)
        {
            if (CoinFlyManager.isCoinFlyFinish)
            {
                if (null != complete)
                {
                    complete();
                }
                return true;
            }
            CoinFlyManager.isCoinFlyFinish = true;
            return false;
        }

        public static bool isCoinFlying { get { return CoinFlyManager.isCoinFlyFinish; } }
    }

    public static class CoinFlyManager
    {
        public static bool isCoinFlyFinish;
    }
}
