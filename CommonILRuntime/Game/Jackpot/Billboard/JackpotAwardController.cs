using CommonILRuntime.Module;
using CommonILRuntime.Services;
using UnityEngine.UI;
using UnityEngine;

namespace Game.Jackpot.Billboard
{
    public class JackpotAwardController : BasicAwardController, ILongValueTweenerHandler
    {
        protected AwardLooper looper;
        protected float minMultiple = 0;
        protected float maxMultiple = 0;
        private ulong minAward = 0;
        private ulong maxAward = 0;
        private ulong serverScale = 0;
        private float maxLimitRate = 0f;
        private float minLimitRate = 0f;

        protected const int updateUIFrequency = 3;
        protected const float displayRate = 0.8f;
        protected const ulong minFrequency = 20;

        public void initAward(ulong basicRate, float maxLimitRate, float minLimitRate, ulong serverScale)
        {
            initAward(basicRate);
            this.maxLimitRate = maxLimitRate;
            this.minLimitRate = minLimitRate;
            this.minMultiple = (basicRate * displayRate * minLimitRate);
            this.maxMultiple = (basicRate * displayRate * maxLimitRate);
            this.serverScale = serverScale;
            looper = new AwardLooper(this);
            looper.setFrameFrequency(updateUIFrequency);
        }

        public override void changeTotalBet(ulong totalBet)
        {
            var rangeMultiple = (maxMultiple - minMultiple) * Random.Range(0.1f, 0.5f);
            float frequency = 0f;
            minAward = (ulong)(totalBet * minMultiple);
            maxAward = (ulong)(totalBet * maxMultiple);
            frequency = (rangeMultiple * totalBet * 0.1f);
            looper.setFrequency(wrapFrequency(frequency));
            looper.setRange(minAward, maxAward);
        }

        public void setServerAward(ulong value, ulong totalBet)
        {
            var serverValue = convertServerValue(value, totalBet);

            if (serverValue > maxAward || serverValue < minAward)
            {
                var frequency = 0f;
                maxAward = (ulong)(serverValue * maxLimitRate);
                minAward = (ulong)(serverValue * minLimitRate);
                frequency = (maxAward - minAward) * Random.Range(0.1f, 0.5f) * 0.1f;
                looper.setFrequency(wrapFrequency(frequency));
                looper.setRange(minAward, maxAward);
            }
        }

        private ulong wrapFrequency(float frequency)
        {
            if (frequency < minFrequency)
            {
                frequency = minFrequency;
            }

            return (ulong)frequency;
        }

        private float convertServerValue(ulong value, ulong totalBet)
        {
            value = value / serverScale;
            value += (ulong)(totalBet * basicRate);
            value = (ulong)(value * displayRate);

            return value;
        }

        public virtual void onValueChanged(ulong value)
        {
            if (null == awardText)
            {
                looper.stop();
                return;
            }
            awardText.text = value.ToString("N0");
        }

        public GameObject getDisposableObj()
        {
            return uiGameObject;
        }
    }
}
