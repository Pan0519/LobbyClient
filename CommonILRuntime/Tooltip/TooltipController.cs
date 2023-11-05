using CommonILRuntime.Module;
using System;
using UniRx;

namespace CommonILRuntime.Tooltip
{
    public class TooltipController : NodePresenter
    {
        IDisposable anyTouchDisposable = null;
        public override void initUIs()
        {
            base.initUIs();
        }

        public override void init()
        {
            base.init();
        }

        public override void open()
        {
            base.open();
            startTouchDetect();
        }

        void startTouchDetect()
        {
            anyTouchDisposable = Observable.EveryUpdate().Subscribe(_ => {
                if (TouchManager.anyTouch)
                {
                    onCloseClick();
                }
            });
        }

        void stopTouchDetect()
        {
            if (null != anyTouchDisposable)
            {
                anyTouchDisposable.Dispose();
                anyTouchDisposable = null;
            }
        }

        void onCloseClick()
        {
            stopTouchDetect();
            close();
        }

        public override void clear()
        {
            stopTouchDetect();
            base.clear();
        }
    }
}
