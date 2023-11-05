using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CommonILRuntime.Module;
using UniRx;
using UniRx.Triggers;
using Services;
using CommonILRuntime.Services;
using CommonService;
using LobbyLogic.Audio;

namespace Game.Common
{
    public class NiceWinPresenter : ContainerPresenter, ILongValueTweenerHandler
    {
        public override string objPath => "prefab/nice_win_board";
        public override UiLayer uiLayer { get => UiLayer.GameMessage; }

        Animator winAnim;
        CustomTextSizeChange coinTxt;
        Button skipBtn;

        public Action skipEvent = null;
        Action completedCallback = null;

        List<IDisposable> animTriggerDis = new List<IDisposable>();
        LongValueTweener totalWinTweener;
        ulong winNum;
        const float runTotalSecond = 3.5f;
        const int runScoreFrameFrequency = 2;
        public override void initUIs()
        {
            winAnim = getAnimatorData("win_anim");
            coinTxt = getBindingData<CustomTextSizeChange>("coin_txt");
            skipBtn = getBtnData("skip_button");
        }

        public override void init()
        {
            skipBtn.onClick.AddListener(skipClick);
            totalWinTweener = new LongValueTweener(this, 0);
            totalWinTweener.onComplete = stopTweenCoin;
        }

        public void openNiceWindown(NiceWinType winType, ulong totalWin, Action callback = null)
        {
            coinTxt.text = string.Empty;
            winNum = totalWin;
            ulong frequency = (ulong)(totalWin / runTotalSecond);
            totalWinTweener.setFrequency(frequency);
            totalWinTweener.setFrameFrequency(runScoreFrameFrequency);
            completedCallback = callback;
            open();
            winAnim.SetTrigger($"{winType.ToString().ToLower()}_in");
            playAudio(winType);
            addDispose(Observable.TimerFrame(15).Subscribe(_ =>
            {
                startTweenWinCoin();
            }));
        }

        //判斷該播的音效
        void playAudio(NiceWinType winLevels)
        {
            string path = string.Empty;
            switch (winLevels)
            {
                case NiceWinType.NiceWin:
                    path = CommonAudioPathProvider.getAudioPath(MainGameCommonSound.NicwWin);
                    break;
                case NiceWinType.Amazing:
                    path = CommonAudioPathProvider.getAudioPath(MainGameCommonSound.AmazingWin);
                    break;
                case NiceWinType.Incredible:
                    path = CommonAudioPathProvider.getAudioPath(MainGameCommonSound.IncredibleWin);
                    break;
            }
            AudioManager.instance.fadeBgmAudio(0f);
            AudioManager.instance.breakFadeOnceAudio(true);
            AudioManager.instance.playAudioOnce(path);

            AudioManager.instance.breakFadeLoopAudio(true);
            AudioManager.instance.playAudioLoop(CommonAudioPathProvider.getAudioPath(MainGameCommonSound.CoinFall), true);
            CoroutineManager.AddCorotuine(waitCoinfalls(AudioManager.instance.getAudioClip(path).length -1f));
        }

        IEnumerator waitCoinfalls(float waitTime)
        {
            yield return waitTime;
            AudioManager.instance.stopLoop();
        }

        public void onValueChanged(ulong value)
        {
            coinTxt.text = value.ToString("N0");
        }

        public GameObject getDisposableObj()
        {
            return uiGameObject;
        }

        void startTweenWinCoin()
        {
            skipBtn.interactable = true;
            totalWinTweener.setRange(0, winNum);
        }

        void skipClick()
        {
            if (null != skipEvent)
            {
                skipEvent();
            }
            stopTweenCoin();
            AudioManager.instance.fadeLoopAudio(0.5f, true);
            AudioManager.instance.fadeOnceAudio(0.5f, true);
            AudioManager.instance.stopOnceAudio();
            AudioManager.instance.breakFadeBgmAudio(true);
            CoroutineManager.StopCorotuine(waitCoinfalls(0));
        }

        void stopTweenCoin()
        {
            skipBtn.interactable = false;
            totalWinTweener.stop();
            addDispose(Observable.Timer(TimeSpan.FromSeconds(0.5f)).Subscribe(_ =>
            {
                triggerCloseAnim();
            }));
        }

        void triggerCloseAnim()
        {
            var animTrigger = winAnim.GetBehaviours<ObservableStateMachineTrigger>();
            for (int i = 0; i < animTrigger.Length; ++i)
            {
                addDispose(animTrigger[i].OnStateEnterAsObservable().ObserveOnMainThread().Subscribe(onAnimTrigger));
            }
            winAnim.SetTrigger("close");
        }

        void onAnimTrigger(ObservableStateMachineTrigger.OnStateInfo obj)
        {
            addDispose(Observable.Timer(TimeSpan.FromSeconds(obj.StateInfo.length)).Subscribe(_ =>
            {
                close();
            }));
        }

        void addDispose(IDisposable disposable)
        {
            disposable.AddTo(uiGameObject);
            animTriggerDis.Add(disposable);
        }

        public override void close()
        {
            AudioManager.instance.breakFadeBgmAudio(true);
            AudioManager.instance.breakFadeOnceAudio(true);
            UtilServices.disposeSubscribes(animTriggerDis.ToArray());
            base.close();
            completedCallback?.Invoke();
        }
    }

    public enum NiceWinType
    {
        NiceWin,
        Amazing,
        Incredible,
    }
}
