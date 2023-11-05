using CommonILRuntime.BindingModule;
using CommonILRuntime.Module;
using CommonILRuntime.Tooltip;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Lobby.Jigsaw
{
    class WheelTooltip : TooltipController
    {
        Text rewardText;
        public override void initUIs()
        {
            base.initUIs();
            rewardText = getTextData("rewardText");
        }

        public override void init()
        {
            base.init();
            rewardText.text = "0";
        }

        public void setReward(long reward)
        {
            rewardText.text = reward.ToString("N0");
        }
    }

    class ProgressTooltip : NodePresenter
    {
        Text starText;
        CancellationTokenSource cts = null;

        public override void initUIs()
        {
            base.initUIs();
            starText = getTextData("starText");
        }

        public override void init()
        {
            base.init();
            uiGameObject.setActiveWhenChange(false);
        }

        public void setStarCount(int count, bool overflow)
        {
            starText.text = $"{count}";

            starText.color = overflow ? new Color(1f, 0.34f, 0.34f) : new Color(1f, 1f, 1f);
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)starText.rectTransform.parent);

            if (0 != count)
            {
                if (null != cts)
                {
                    cts.Cancel();
                }

                uiGameObject.setActiveWhenChange(true);

                cts = new CancellationTokenSource();
                autoClose(cts.Token);
            }
        }

        async void autoClose(CancellationToken ct)
        {
            await Task.Delay(TimeSpan.FromSeconds(1.5f));
            if (!ct.IsCancellationRequested)
            {
                uiGameObject.setActiveWhenChange(false);
            }
        }
    }

    class ProgressWheel : NodePresenter
    {
        public int targetStar { get; private set; } = 0;

        Button wheelButton;
        Text targetStarText;

        public Action<GameObject, int> onClickListener = null;
        public int idx;

        public override void initUIs()
        {
            base.initUIs();
            wheelButton = getBtnData("wheelButton");
            targetStarText = getTextData("targetStarText");
        }

        public override void init()
        {
            base.init();
            wheelButton.interactable = true;
            wheelButton.onClick.AddListener(onWheelClick);
        }

        public void setTargetStarCount(int count)
        {
            targetStarText.text = $"{count}";
            targetStar = count;
        }

        public bool setWheelAvaliable(bool avaliable)
        {
            var newAvaliable = (wheelButton.interactable != avaliable) && avaliable;
            wheelButton.image.color = avaliable ? new Color(1f, 1f, 1f) : new Color(0.5f, 0.5f, 0.5f);
            return newAvaliable;
        }

        public void setPlayEffect(bool play)
        {
            var animator = getAnimatorData("effectAnimator");
            animator.SetBool("full", play);
        }

        void onWheelClick()
        {
            onClickListener?.Invoke(wheelButton.gameObject, idx);
        }
    }

    public class FantasyProgressBar : NodePresenter
    {
        const float FIRST_WHEEL_FILLAMOUNT = 0.30f;
        const float SECOND_WHEEL_FILLAMOUNT = 0.60f;
        const float THIRD_WHEEL_FILLAMOUNT = 1f;

        //const float BAR_LENGTH = 649f;
        const float BAR_START_X = -585f;
        const float BAR_END_X = 64;

        int maxTargetStar = 0;

        Image progressImage;
        ProgressTooltip starCountTooltip;
        ProgressWheel progressWhee1;
        ProgressWheel progressWhee2;
        ProgressWheel progressWhee3;
        List<ProgressWheel> progressWheels;

        WheelTooltip wheelTooltip;

        List<float> barFillAmount = new List<float>();  //ILRuntime 宣告 array 會 crash, 所以用 List

        public override void initUIs()
        {
            base.initUIs();
            progressImage = getImageData("progressImage");
            progressWhee1 = UiManager.bindNode<ProgressWheel>(getNodeData("progressWheel1").cachedGameObject);
            progressWhee1.idx = 0;
            progressWhee2 = UiManager.bindNode<ProgressWheel>(getNodeData("progressWheel2").cachedGameObject);
            progressWhee2.idx = 1;
            progressWhee3 = UiManager.bindNode<ProgressWheel>(getNodeData("progressWheel3").cachedGameObject);
            progressWhee3.idx = 2;
            starCountTooltip = UiManager.bindNode<ProgressTooltip>(getNodeData("starCountTooltip").cachedGameObject);
            wheelTooltip = UiManager.bindNode<WheelTooltip>(getNodeData("wheelTooltip").cachedGameObject);
        }

        public override void init()
        {
            base.init();
            progressWheels = new List<ProgressWheel>();
            progressWheels.Add(progressWhee1);
            progressWheels.Add(progressWhee2);
            progressWheels.Add(progressWhee3);

            progressWhee1.onClickListener = onWheelClick;
            progressWhee2.onClickListener = onWheelClick;
            progressWhee3.onClickListener = onWheelClick;

            barFillAmount.Add(FIRST_WHEEL_FILLAMOUNT);
            barFillAmount.Add(SECOND_WHEEL_FILLAMOUNT);
            barFillAmount.Add(THIRD_WHEEL_FILLAMOUNT);

            wheelTooltip.close();
        }

        public void initTargetStars(int[] stars)
        {
            if (stars.Length < progressWheels.Count)
            {
                return;
            }

            for (int i = 0; i < stars.Length; i++)
            {
                var progressWheel = progressWheels[i];
                progressWheel.setTargetStarCount(stars[i]);
                maxTargetStar = stars[i];
            }
        }

        public int setCurrentStar(int star, Action<int> newWheelAvaliableCallback = null)
        {
            //修改Tooltip
            starCountTooltip.setStarCount(star, star > maxTargetStar);

            //檢查亮燈的wheel Icon
            int avaliableWheelIdx = -1;
            for (int i = 0; i < progressWheels.Count; i++)
            {
                var wheel = progressWheels[i];
                bool avaliable = wheel.targetStar <= star;
                bool newWheelAvaliable = wheel.setWheelAvaliable(avaliable);
                if (avaliable)
                {
                    avaliableWheelIdx = i;
                }

                if (progressWheels.Count-1 == i)
                {
                    wheel.setPlayEffect(avaliable);
                }
            }

            if (avaliableWheelIdx >= 0)
            {
                newWheelAvaliableCallback?.Invoke(avaliableWheelIdx);
            }

            if (avaliableWheelIdx == progressWheels.Count - 1)
            {
                progressImage.fillAmount = THIRD_WHEEL_FILLAMOUNT;
            }
            else
            {
                int beginStar = avaliableWheelIdx == -1 ? 0 : progressWheels[avaliableWheelIdx].targetStar;
                var nextWheel = progressWheels[avaliableWheelIdx+1];
                var starDistance = nextWheel.targetStar - beginStar;

                float baseFillAmount = avaliableWheelIdx == -1 ? 0: barFillAmount[avaliableWheelIdx];
                float maxFillAmount = barFillAmount[avaliableWheelIdx + 1];

                if (0 != starDistance)
                {
                    var fillAmount = Mathf.Lerp(baseFillAmount, maxFillAmount, (star - beginStar) / (float)starDistance);
                    progressImage.fillAmount = fillAmount;
                }
                else
                {
                    progressImage.fillAmount = baseFillAmount;
                }
            }

            //修改Tooltip位置
            float x = Mathf.Lerp(BAR_START_X, BAR_END_X, progressImage.fillAmount);
            float y = starCountTooltip.uiRectTransform.anchoredPosition.y;
            starCountTooltip.uiRectTransform.anchoredPosition = new Vector2(x, y);

            return avaliableWheelIdx;
        }

        long[] rewards = new long[3];
        public void setMaxCoinReward(long reward, int idx)
        {
            rewards[idx] = reward;
        }

        void onWheelClick(GameObject wheelObject, int idx)
        {
            wheelTooltip.setReward(rewards[idx]);

            var wheelWorldPos = wheelObject.transform.position;

            var tooltipWorldPos = wheelTooltip.uiTransform.position;
            tooltipWorldPos.x = wheelWorldPos.x;
            wheelTooltip.uiTransform.position = tooltipWorldPos;
            wheelTooltip.open();
        }
    }
}
