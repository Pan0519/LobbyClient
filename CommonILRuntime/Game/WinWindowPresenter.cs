using System;
using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;
using System.Threading;
using LobbyLogic.Audio;
using CommonService;
using CommonILRuntime.Module;
using System.Collections;
using System.Collections.Generic;
using Game.Slot;
using CommonILRuntime.Services;

namespace Game.Common
{
    public class WinWindowPresenter : ContainerPresenter, ILongValueTweenerHandler
    {
        public override string objPath { get { return "prefab/win_score_board"; } }
        public override UiLayer uiLayer { get { return UiLayer.GameMessage; } }

        public enum WinLevels : int //name 對應到 animation trigger, 小心改名
        {
            big = 0,
            mega,
            epic,
            massive,
            ultimate,
        }

        LongValueTweener longValueTweener = null;
        Animator winAnimator;
        CustomTextSizeChange winTotalText;
        Button skipButton;
        RectTransform scaleGroup;

        Action callback = null;
        WinLevels levels;
        bool stopMoneyLaundering = false;
        bool isClosing = false;
        ulong totalwin = 0;

        public List<float> moneyLaunderingTime = new List<float>();// = { 10, 10, 15, 15, 20 }; //ILRuntime 使用 float[] malloc issue, 改用 List<float>
        public List<float> delayTime = new List<float>();//{ 10, 10, 15, 15, 20 };


        CancellationTokenSource suicideTask;
        CancellationToken token;

        private const string inAniFormat = "{0}_in";
        private const int updateScoreFrequency = 2;

        public override void initUIs()
        {
            winAnimator = getAnimatorData("win_ani");
            winTotalText = getBindingData<CustomTextSizeChange>("num_win");
            skipButton = getBtnData("skip_button");
            scaleGroup = getRectData("scale_group");
        }

        public override async void init()
        {
            await setOrientationScale();

            skipButton.onClick.AddListener(onSkipClick);
            uiGameObject.setActiveWhenChange(false);
            stopMoneyLaundering = false;

            setLaunderingTime();
            initLongTweener();
        }

        async Task setOrientationScale()
        {
            var nowGameOrientation = await DataStore.getInstance.dataInfo.getNowGameOrientation();
            float orientationScale = (GameOrientation.Landscape == nowGameOrientation) ? 1 : 0.6f;
            var scale = scaleGroup.localScale;
            scale.Set(orientationScale, orientationScale, orientationScale);
            scaleGroup.localScale = scale;
        }

        private void initLongTweener()
        {
            longValueTweener = new LongValueTweener(this, 0);
            longValueTweener.setFrameFrequency(updateScoreFrequency);
            longValueTweener.onComplete = onRunScoreComplete;
        }

        public void setLaunderingTime()
        {
            GameConfig config = SlotGameBase.gameConfig;
            moneyLaunderingTime.Clear();
            moneyLaunderingTime.Add(config.BIG_WIN_LAUNDERING_TIME);
            moneyLaunderingTime.Add(config.MEGA_WIN_LAUNDERING_TIME);
            moneyLaunderingTime.Add(config.EPIC_WIN_LAUNDERING_TIME);
            moneyLaunderingTime.Add(config.MASSIVE_WIN_LAUNDERING_TIME);
            moneyLaunderingTime.Add(config.ULTIMATE_WIN_LAUNDERING_TIME);

            delayTime.Clear();
            delayTime.Add(config.BIG_WIN_WAIT);
            delayTime.Add(config.MEGA_WIN_WAIT);
            delayTime.Add(config.EPIC_WIN_WAIT);
            delayTime.Add(config.MASSIVE_WIN_WAIT);
            delayTime.Add(config.ULTIMATE_WIN_WAIT);
        }

        public void openWinWindow(ulong total, WinLevels winLevels, Action callback = null)
        {
            string levelName = winLevels.ToString();
            suicideTask = new CancellationTokenSource();
            token = suicideTask.Token;
            open();
            winAnimator.SetTrigger(levelName);
            playAudio(winLevels);
            skipButton.enabled = false;
            levels = winLevels;
            totalwin = total;
            stopMoneyLaundering = false;
            isClosing = false;
            this.callback = callback;
            DataStore.getInstance.guideServices.noticeWinWindowsState(true);
            Debug.Log($"moneyLaunderingTime:{moneyLaunderingTime[(int)winLevels]}");
            CoroutineManager.StartCoroutine(delayOpenSkipBtn(levelName));
            startRunScore(winLevels);
        }

        private IEnumerator delayOpenSkipBtn(string levelName)
        {
            var inAniState = string.Format(inAniFormat, levelName);
            var wrapper = new BooleanWrapper(() => { return !winAnimator.GetCurrentAnimatorStateInfo(0).IsName(inAniState); });

            yield return wrapper;
            skipButton.enabled = true;
        }

        private void startRunScore(WinLevels winLevels)
        {
            ulong frequency = (ulong)(totalwin / moneyLaunderingTime[(int)winLevels]);
            longValueTweener.setFrequency(frequency);
            longValueTweener.setRange(0, totalwin);
        }

        //判斷該播的音效
        void playAudio(WinLevels winLevels)
        {
            string path = string.Empty;
            switch (winLevels)
            {
                case WinLevels.big:
                    path = CommonAudioPathProvider.getAudioPath(MainGameCommonSound.BigWin);
                    break;
                case WinLevels.mega:
                    path = CommonAudioPathProvider.getAudioPath(MainGameCommonSound.MegaWin);
                    break;
                case WinLevels.epic:
                    path = CommonAudioPathProvider.getAudioPath(MainGameCommonSound.EpicWin);
                    break;
                case WinLevels.massive:
                    path = CommonAudioPathProvider.getAudioPath(MainGameCommonSound.MassiveWin);
                    break;
                case WinLevels.ultimate:
                    path = CommonAudioPathProvider.getAudioPath(MainGameCommonSound.UltimateWin);
                    break;
            }
            AudioManager.instance.fadeBgmAudio(0f);
            AudioManager.instance.breakFadeLoopAudio(true);
            AudioManager.instance.playAudioLoop(path, false);
        }

        public void onSkipClick()
        {
            if (isClosing) return;
            if (!stopMoneyLaundering)
            {
                stopMoneyLaundering = true;
                countDownSuicide(delayTime[(int)levels], token.GetHashCode());
                return;
            }
            skipButton.enabled = false;
            AudioManager.instance.playAudioOnce(CommonAudioPathProvider.getAudioPath(BasicCommonSound.InfoBtn));
            suicideTask.Cancel();
            suicideTask.Dispose();
            closeWinWindow();
        }

        async void countDownSuicide(float time, int hash)
        {
            await Task.Delay(TimeSpan.FromSeconds(time));
            if (token.IsCancellationRequested || token.GetHashCode() != hash)
            {
                return;
            }
            closeWinWindow();
        }

        async void closeWinWindow()
        {
            if (isClosing) return;
            isClosing = true;

            if (uiGameObject.activeSelf)
            {
                winAnimator.SetTrigger("close");
                await waiteEnterOutAni();
                await Task.Delay(TimeSpan.FromSeconds(getAnimationLength($"{levels}_out")));
                close();
                AudioManager.instance.fadeLoopAudio(2);
                AudioManager.instance.breakFadeBgmAudio(true);
                DataStore.getInstance.guideServices.noticeWinWindowsState(false);
                if (callback != null)
                {
                    callback();
                }
            }
        }

        private async Task waiteEnterOutAni()
        {
            while (!winAnimator.GetCurrentAnimatorStateInfo(0).IsName($"{levels}_out"))
            {
                await Task.Delay(TimeSpan.FromSeconds(0.1f));
            }
        }

        float getAnimationLength(string name)
        {
            AnimationClip[] clips = winAnimator.runtimeAnimatorController.animationClips;
            float length = 0;
            foreach (AnimationClip clip in clips)  //抓取動畫時長
            {
                if (clip.name.Equals(name))
                {
                    length = clip.length;
                    break;
                }
            }
            return length;
        }

        public void onValueChanged(ulong value)
        {
            if (!stopMoneyLaundering)
            {
                winTotalText.text = value.ToString("N0");
            }
            else
            {
                winTotalText.text = totalwin.ToString("N0");
                longValueTweener.stop();
            }
        }

        public GameObject getDisposableObj()
        {
            return uiGameObject;
        }

        private void onRunScoreComplete()
        {
            if (!stopMoneyLaundering)
            {
                onSkipClick();
            }
        }
    }
}