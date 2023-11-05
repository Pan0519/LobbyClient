using Services;
using System;
using UniRx;
using UnityEngine;

namespace CommonILRuntime.Services
{
    public class LongValueTweener
    {
        ILongValueTweenerHandler receiver = null;

        protected ulong source { get; private set; }
        protected ulong target { get; private set; }
        private ulong frequency; //一秒跑多少
        private ulong currentValue = 0;
        private ulong addValue = 0;
        private int frameFrequency = 0;
        private int currentFrame = 0;
        private float addRate = 0f;
        private float frameDeltaTime = 0f;
        private float lastDeviationValue = 0f;

        private const float deviationRate = 0.01f;

        public Action onComplete = null;
        IDisposable everyUpdateDisposable;

        public LongValueTweener(ILongValueTweenerHandler receiver, ulong frequency)
        {
            this.receiver = receiver;
            this.frequency = Math.Max(frequency, 1);
            frameFrequency = 1;
        }

        ~LongValueTweener()
        {
            stop();
        }

        /// <summary>
        /// setRange 呼叫過後就會立即開始跑分
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        public void setRange(ulong source, ulong target)
        {
            if (null == receiver)
            {
                return;
            }

            tryDisposeUpdate();
            resetCalculateParameter();
            this.source = source;
            this.target = target;

            if (source != target)
            {
                dashValue(source, target);
                return;
            }
            Debug.LogWarning("In LongValueTweener setRange source == target, may cause infinity loop");
            receiver?.onValueChanged(source);
            onComplete?.Invoke();
        }

        protected void resetCalculateParameter()
        {
            lastDeviationValue = 0f;
            addRate = 0f;
            addValue = 0;
        }

        public void setFrequency(ulong frequency)
        {
            this.frequency = frequency;
        }

        public void setFrameFrequency(int frameFrequency)
        {
            this.frameFrequency = frameFrequency;
        }

        protected void startDash()
        {
            dashValue(source, target);
        }

        public void stop()
        {
            if (tryDisposeUpdate())
            {
                receiver?.onValueChanged(target);
            }
        }

        protected bool tryDisposeUpdate()
        {
            if (null != everyUpdateDisposable)
            {
                UtilServices.disposeSubscribes(everyUpdateDisposable);
                everyUpdateDisposable = null;
                return true;
            }
            return false;
        }

        protected void dashValue(ulong startValue, ulong endValue)
        {
            currentValue = startValue;
            everyUpdateDisposable = Observable.EveryFixedUpdate().Subscribe((_) =>
            {
                frameDeltaTime += Time.deltaTime;
                if (0 == currentFrame % frameFrequency)
                {
                    onUpdate(endValue);
                }
                updateFrame();
            });
            bindinDisposableObj();
        }

        private void bindinDisposableObj()
        {
            GameObject target = receiver.getDisposableObj();
            if (null != target)
            {
                everyUpdateDisposable.AddTo(target);
            }
        }

        private void onUpdate(ulong endValue)
        {
            if (currentValue >= endValue)
            {
                stop();
                onComplete?.Invoke();
                return;
            }
            updateAddValue();
            updateCurrentValue(endValue);
        }

        private void updateAddValue()
        {
            addRate = UnityEngine.Random.Range(frameDeltaTime - deviationRate, frameDeltaTime + deviationRate);
            addValue = (ulong)((addRate + lastDeviationValue) * frequency);
            lastDeviationValue = frameDeltaTime - addRate;
            frameDeltaTime = 0f;
        }

        private void updateCurrentValue(ulong endValue)
        {
            if (addValue > 0)   //FPS過高 高過frequency時,addvalue會小於0，跳過此frame更新(依據frequency 速率進行刷新數值)
            {
                currentValue = Math.Min(endValue, currentValue + addValue);
                receiver?.onValueChanged(currentValue);
            }
        }

        private void updateFrame()
        {
            currentFrame++;
            if (currentFrame == int.MaxValue)
            {
                currentFrame = 0;
            }
        }
    }
}
