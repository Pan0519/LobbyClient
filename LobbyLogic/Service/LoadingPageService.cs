using UniRx;

namespace Lobby.Service
{
    public static class LoadingPageService
    {
        static bool isOpen = true;

        public static Subject<float> getProgressChangeEvent()
        {
            if (isOpen)
            {
                return LoadingPageManager.instance.progressChangeValued;
            }
            return null;
        }

        public static float getNowProgressBarFillAmount()
        {
            if (isOpen)
            {
                return LoadingPageManager.instance.getNowProgressBarFillAmount();
            }
            return 0;
        }

        public static void clearFakeLoadingDispose()
        {
            if (isOpen)
            {
                LoadingPageManager.instance.clearFakeLoadingDispose();
            }
        }

        public static void openLoadingPage()
        {
            if (isOpen)
            {
                LoadingPageManager.instance.openLoadingPage();
            }
        }

        public static void openLoadingBar()
        {
            if (isOpen)
            {
                LoadingPageManager.instance.openLoadingBar();
            }
        }

        public static void runLoadingProgress(float runVale)
        {
            if (isOpen)
            {
                LoadingPageManager.instance.runLoadingProgress(runVale);
            }
        }

        public static void setLoadingInfo(string info)
        {
            if (isOpen)
            {
                LoadingPageManager.instance.setLoadingInfo(info);
            }
        }

        public static void stopLoadingProgress()
        {
            if (isOpen)
            {
                LoadingPageManager.instance.stopLoadingProgress();
            }
        }

        public static void closeLoadingPage()
        {
            if (isOpen)
            {
                LoadingPageManager.instance.closeLoadingPage();
            }
        }

        public static void resetSliderValue()
        {
            if (isOpen)
            {
                LoadingPageManager.instance.resetSliderValue();
            }
        }
    }
}
