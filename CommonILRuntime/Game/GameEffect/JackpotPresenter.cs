using System;
using System.Collections;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using LobbyLogic.Audio;
using CommonService;
using System.Threading.Tasks;
using CommonILRuntime.Module;
using Game.Common;

namespace Game.Slot
{
    public class JackpotPresenter : ContainerPresenter
    {
        public static Action audioMini;
        public static Action audioMinor;
        public static Action audioMajor;
        public static Action audioGrand;
        /*
        public enum JackPotLevels : int
        {
            None = 0,
            Mini,
            Minor,
            Major,
            Grand,
        }*/

        public override string objPath { get { return "prefab/slot/jackpot"; } }
        public override UiLayer uiLayer { get { return UiLayer.GameMessage; } }

        public virtual string closeTrigger { get { return "Close"; } }

        public Button btnCollect;
        public Text numRewardText;

        public override void initUIs()
        {
            btnCollect = getBtnData("JP_btn_collect");
            numRewardText = getTextData("JP_num_reward");
        }

        protected Animator jp_ani;
        protected Action callBack = null;
        protected string jp_level;

        bool closed = false;

        public override void init()
        {
            jp_ani = uiGameObject.GetComponent<Animator>();
            btnCollect.onClick.RemoveAllListeners();
            btnCollect.onClick.AddListener(closeHandler);
            close();
        }

        public virtual IEnumerator OpenWindows(ulong reward, GameConfig.JackPotLevels level, Action callback = null, Action onDelete = null)
        {
            yield return CoroutineManager.scheduler.StartCoroutine(OpenWindows(reward, (long)level, callback));
        }
        public virtual IEnumerator OpenWindows(ulong reward, long level, Action callback = null)
        {
            AudioManager.instance.fadeBgmAudio(0f);
            callBack = callback;

            int _level = (int)level;

            if (String.IsNullOrEmpty(setJpLevel(_level)))
            {
                callBack?.Invoke();
                yield break;
            }
            playJpAudio(_level);

            numRewardText.text = reward.ToString("N0");
            open();
            jp_ani.SetTrigger(getOpenTrigger());

            closed = false;
           
            yield return new BooleanWrapper(()=>closed);

            /*
            closed = false;
            while (!closed)
            {
                yield break;
            }*/
        }

        public virtual string setJpLevel(int level)
        {
            jp_level = (GameConfig.JackPotLevels)level != GameConfig.JackPotLevels.None ? ((GameConfig.JackPotLevels)level).ToString() : string.Empty;
            return jp_level;
        }

        public virtual void playJpAudio(int level)
        {
            switch ((GameConfig.JackPotLevels)level)
            {
                case GameConfig.JackPotLevels.Mini:
                    audioMini?.Invoke();
                    //AudioManager.instance.playAudioOnce(AudioPathProvider.getAudioPath(GameSound.Mini));
                    break;
                case GameConfig.JackPotLevels.Minor:
                    audioMinor?.Invoke();
                    //AudioManager.instance.playAudioOnce(AudioPathProvider.getAudioPath(GameSound.Minor));
                    break;
                case GameConfig.JackPotLevels.Major:
                    audioMajor?.Invoke();
                    //AudioManager.instance.playAudioOnce(AudioPathProvider.getAudioPath(GameSound.Major));
                    break;
                case GameConfig.JackPotLevels.Grand:
                    audioGrand?.Invoke();
                    //AudioManager.instance.playAudioOnce(AudioPathProvider.getAudioPath(GameSound.Grand));
                    break;
            }
        }

        public void closeHandler()
        {
            AudioManager.instance.playAudioOnce(CommonAudioPathProvider.getAudioPath(BasicCommonSound.SwitchBtn));
            closeWindows();
        }
        public virtual void closeWindows()
        {
            CoroutineManager.StartCoroutine(delayCloseWin());
        }

        public IEnumerator delayCloseWin()
        {
            AudioManager.instance.breakFadeBgmAudio(true);

            float animLength = 1.5f;
            jp_ani.SetTrigger(closeTrigger);
            AnimationClip[] clips = jp_ani.runtimeAnimatorController.animationClips;
            foreach (AnimationClip clip in clips)  //抓取動畫時長
            {
                if (clip.name.Equals(getCloseAnimationName()))
                {
                    animLength = clip.length;
                    break;
                }
            }

            yield return animLength;
            close();
            closed = true;
        }

        public virtual string getOpenTrigger()
        {
            return jp_level;
        }

        public virtual string getCloseAnimationName()
        {
            return $"jp_{jp_level}_out";
        }
    }
}
