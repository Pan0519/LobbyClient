using System;
using UniRx;

namespace CommonILRuntime.Game.GameTime
{
    public class GameTimeManager
    {
        public CoroutineScheduler coroutineScheduler;
        private Subject<string> pauseSubject = new Subject<string>();
        private Subject<string> resumeSubject = new Subject<string>();

        private bool isPaused = false;

        public IObservable<string> OnPaused
        {
            get { return pauseSubject; }
        }

        public IObservable<string> OnResumed
        {
            get { return resumeSubject; }
        }

        public void Pause()
        {
            isPaused = true;
            pauseSubject.OnNext("pause");
            TweenManager.pauseAll();
        }

        public void Resume()
        {
            isPaused = false;

            resumeSubject.OnNext("resume");
            TweenManager.playAll();
        }

        /// <summary>
        /// 是不是正在暫停中
        /// </summary>
        /// <returns></returns>
        public bool IsPaused()
        {
            return isPaused;
        }
    }
}