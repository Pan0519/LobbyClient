using System;
using System.Collections;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;
using LobbyLogic.Audio;
using CommonService;
using CommonILRuntime.Module;
using CommonILRuntime.BindingModule;

namespace Game.Slot
{
    using Game.Common;
    public class FreeGamePresenter : ContainerPresenter
    {
        public static Action audioOpenFGStartWindows;
        public static Action audioGetMoreFreeSpins;
        public static Action audioOpenFGEndWindows;
        public static Action audioCloseWindowsAndMakeCutEffect;
        public override string objPath { get { return "prefab/slot/freegame"; } }
        public override UiLayer uiLayer { get { return UiLayer.GameMessage; } }

        public override void initUIs()
        {
            fg_ani = getAnimatorData("FG_anim");
            numTimesText = getTextData("FG_num_times");
            btnCollect = getBtnData("FG_btn_collect");
            btnStart = getBtnData("FG_btn_start");
            numRewardText = getTextData("FG_num_reward");
        }

        public GameObject fgWindows;
        public Text numTimesText;
        public Button btnCollect;
        public Button btnStart;
        public Text numRewardText;

        public Animator fg_ani;
        public string ani_trigger;
        public Action onClose = null;
        public string fg_times, fg_reward;
        ulong winPoints;
        public bool cutScene = true;

        public override void init()
        {
            fgWindows = uiGameObject;
            fg_times = "0";
            fg_reward = "0";

            btnStart.onClick.RemoveAllListeners();
            btnStart.onClick.AddListener(enterFreeGame);

            btnCollect.onClick.RemoveAllListeners();
            btnCollect.onClick.AddListener(leaveFreeGame);
            
            //fg_ani = fgWindows.GetComponent<Animator>();
            fgWindows.SetActive(false);
        }
        //進入freegame時呼叫
        public virtual void OpenFGStartWindows(int times, string moneyText,Action onCloseHandler = null)
        {
            this.cutScene = true;
            btnStart.enabled = true;
            onClose = onCloseHandler;
            ani_trigger = "FG_start";
            fg_times = times.ToString();
            animationTrigger();
            audioOpenFGStartWindows?.Invoke();
            //AudioManager.instance.playAudioOnce(AudioPathProvider.getAudioPath(GameSound.FreeGame_Notify));
        }

        //再次得到freegame時呼叫
        public virtual void OpenFGExtraWindows(int times, Action onCloseHandler = null)
        {
            this.cutScene = false;
            btnStart.enabled = true;
            onClose = onCloseHandler;
            ani_trigger = "FG_more";
            fg_times = times.ToString();
            animationTrigger();
            audioGetMoreFreeSpins?.Invoke();
            //AudioManager.instance.playAudioOnce(AudioPathProvider.getAudioPath(GameSound.FreeGame_Notify));
        }

        //結束freegame時呼叫
        public virtual void OpenFGEndWindows(ulong reward,long times, Action onCloseHandler = null)
        {
            this.cutScene = true;
            btnCollect.enabled = true;
            AudioManager.instance.fadeBgmAudio(0f);
            audioOpenFGEndWindows?.Invoke();
            //AudioManager.instance.playAudioOnce(AudioPathProvider.getAudioPath(GameSound.FreeGame_End));
            onClose = onCloseHandler;
            ani_trigger = "FG_end";
            winPoints = reward;
            fg_reward = reward.ToString("N0");
            if (times != 0)
                fg_times = times.ToString();
            animationTrigger();
        }
        //開啟開場動畫
        public virtual void animationTrigger()
        {
            Debug.Log("FreeGamePresenter animationTrigger:001");
            numTimesText.text = fg_times;
            numRewardText.text = fg_reward;
            fgWindows.SetActive(true);
            fg_ani.SetTrigger(ani_trigger);
            Debug.Log("FreeGamePresenter animationTrigger:002");
        }
        //按下按鈕後的動作
        public virtual async void enterFreeGame()
        {
            CoroutineManager.StartCoroutine(delayEnterCloseWin());
        }

        public virtual IEnumerator delayEnterCloseWin()
        {
            btnStart.enabled = false;
            AudioManager.instance.playAudioOnce(CommonAudioPathProvider.getAudioPath(BasicCommonSound.SwitchBtn));
            fg_ani.SetTrigger("FG_close");
            yield return 0.45f;
            fgWindows.SetActive(false);
            if (cutScene)
            {
                yield return CoroutineManager.scheduler.StartCoroutine(closeWindowsAndMakeCutEffect());
            }
            else
            {
                onClose?.Invoke();
            }
        }

        public virtual async void leaveFreeGame()
        {
            CoroutineManager.StartCoroutine(delayLeaveCloseWin());
        }

        public IEnumerator delayLeaveCloseWin()
        {

            btnCollect.enabled = false;
            AudioManager.instance.playAudioOnce(CommonAudioPathProvider.getAudioPath(BasicCommonSound.SwitchBtn));
            fg_ani.SetTrigger("FG_close");
            float length = 0;

            AnimationClip[] clips = fg_ani.runtimeAnimatorController.animationClips;
            foreach (AnimationClip clip in clips)  //抓取動畫時長
            {
                if (clip.name.Equals("free_game_end_out") && ani_trigger == "FG_end")
                {
                    length = clip.length;
                    break;
                }
            }

            yield return length;
            fgWindows.SetActive(false);
            AudioManager.instance.stopLoop();

            CoroutineManager.StartCoroutine(closeWindowsAndMakeCutEffect());
        }

        //關閉視窗及生成特效
        public virtual IEnumerator closeWindowsAndMakeCutEffect()
        {
            
            //生成過場特效
            var fg_cut = UiManager.getPresenter<FreeCutScenePresenter>();
            audioCloseWindowsAndMakeCutEffect?.Invoke();
            yield return 0.5f; // 緩衝時間
            onClose?.Invoke();
            yield return fg_cut.animationTimes - 0.5f;
            UiManager.clearPresnter(fg_cut);
            
        }
    }
}