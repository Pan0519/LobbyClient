using System;
using System.Collections;
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
    public class MiniGamePresenter : ContainerPresenter
    {
        public static Action audioOpenMiniGameStartWindows;
        public static Action audioOpenMiniGameEndWindows;
        public static Action audioCloseWindowsAndMakeCutEffect;
        public override string objPath { get { return "prefab/slot/minigame"; } }
        public override UiLayer uiLayer { get { return UiLayer.GameMessage; } }

        public override void initUIs()
        {
            btnCollect = getBtnData("btn_collect");
            btnStart = getBtnData("btn_start");
            numRewardText = getTextData("num_reward");
        }

        public GameObject uiWindows;
        public Button btnCollect;
        public Button btnStart;
        public Text numRewardText;

        public Animator uiAnim;
        string ani_trigger;
        string ui_reward;
        public Action onClose = null;
        public Action onToAutoPlay = null;

        public override void init()
        {
            uiWindows = uiGameObject;
            ui_reward = "0";

            btnStart.onClick.RemoveAllListeners();
            btnStart.onClick.AddListener(enterMiniGame);

            btnCollect.onClick.RemoveAllListeners();
            btnCollect.onClick.AddListener(leaveMiniGame);

            uiAnim = uiWindows.GetComponent<Animator>();
            uiWindows.SetActive(false);
        }
        
        public virtual void OpenMiniGameStartWindows(Action onCloseHandler = null, Action onToAutoPlayHandler = null)
        {
            btnStart.enabled = true;
            onClose = onCloseHandler;
            onToAutoPlay = onToAutoPlayHandler;
            ani_trigger = "start";
            animationTrigger();
            audioOpenMiniGameStartWindows?.Invoke();
        }

        //結束bonusGame時呼叫
        public virtual void OpenMiniGameEndWindows(ulong reward, Action onCloseHandler = null)
        {
            AudioManager.instance.fadeBgmAudio(0f);
            audioOpenMiniGameEndWindows?.Invoke();
            btnCollect.enabled = true;
            onClose = onCloseHandler;
            ani_trigger = "end";
            ui_reward = reward.ToString("N0");
            animationTrigger();
        }
        //開啟開場動畫
        void animationTrigger()
        {
            numRewardText.text = ui_reward;
            uiWindows.SetActive(true);
            uiAnim.SetTrigger(ani_trigger);
        }

        //按下按鈕後的動作
        public virtual async void enterMiniGame()
        {
            CoroutineManager.StartCoroutine(delayEnterCloseWin());
        }

        public IEnumerator delayEnterCloseWin()
        {
            btnStart.enabled = false;
            AudioManager.instance.playAudioOnce(CommonAudioPathProvider.getAudioPath(BasicCommonSound.SwitchBtn));
            uiAnim.SetTrigger("close");
            float length = 1.5f;

            /*AnimationClip[] clips = uiAnim.runtimeAnimatorController.animationClips;
            foreach (AnimationClip clip in clips)  //抓取動畫時長
            {
                if (clip.name.Equals("bonus_game_start_out") && ani_trigger == "BG_start")
                {
                    length = clip.length;
                    break;
                }
            }*/
            //await Task.Delay(TimeSpan.FromSeconds(length));
            yield return length;
            uiWindows.SetActive(false);
            yield return CoroutineManager.scheduler.StartCoroutine(closeWindowsAndMakeCutEffect());
        }

        public virtual async void leaveMiniGame()
        {
            CoroutineManager.StartCoroutine(delayLeaveCloseWin());
        }

        public IEnumerator delayLeaveCloseWin()
        {
            btnCollect.enabled = false;
            uiAnim.SetTrigger("close");
            float length = 1.5f;

            /*AnimationClip[] clips = uiAnim.runtimeAnimatorController.animationClips;
            foreach (AnimationClip clip in clips)  //抓取動畫時長
            {
                if (clip.name.Equals("bonus_game_end_out") && ani_trigger == "BG_end")
                {
                    length = clip.length;
                    break;
                }
            }*/

            //await Task.Delay(TimeSpan.FromSeconds(length));
            yield return length;
            uiWindows.SetActive(false);
            AudioManager.instance.stopLoop();
            //AppManager.VegasClient.showWinWindow(winPoints, ()=> { closeWindowsAndMakePlaneEffect(); });
            CoroutineManager.StartCoroutine(closeWindowsAndMakeCutEffect());
        }

        //關閉視窗及生成特效
        public virtual IEnumerator closeWindowsAndMakeCutEffect()
        {
            //生成過場特效
            var bg_cut = UiManager.getPresenter<BonusCutScenePresenter>();
            audioCloseWindowsAndMakeCutEffect?.Invoke();
            //AudioManager.instance.playAudioOnce(AudioPathProvider.getAudioPath(GameSound.BonusGame_Switch));
            //await Task.Delay(TimeSpan.FromSeconds(0.5f)); // 緩衝時間
            yield return 0.5f;

            onClose?.Invoke();
            //await Task.Delay(TimeSpan.FromSeconds(bg_cut.animationTimes - 0.5f));
            yield return bg_cut.animationTimes - 0.5f;
            UiManager.clearPresnter(bg_cut);
        }
    }
}
