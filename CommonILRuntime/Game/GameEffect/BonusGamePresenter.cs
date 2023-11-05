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
    public class BonusGamePresenter : ContainerPresenter
    {
        public static Action audioOpenBGStartWindows;
        public static Action audioOpenBGEndWindows;
        public static Action audioCloseWindowsAndMakeCutEffect;
        public override string objPath { get { return "prefab/slot/bonus_game"; } }
        public override UiLayer uiLayer { get { return UiLayer.GameMessage; } }

        public override void initUIs()
        {
            btnCollect = getBtnData("BG_btn_collect");
            btnStart = getBtnData("BG_btn_start");
            numRewardText = getTextData("BG_num_reward");
        }

        public GameObject bgWindows;
        public Button btnCollect;
        public Button btnStart;
        public Text numRewardText;

        public Animator bg_ani;
        public string ani_trigger;
        string bg_reward;
        public Action onClose = null;
        public Action onToAutoPlay = null;

        public override void init()
        {
            bgWindows = uiGameObject;
            bg_reward = "0";

            btnStart.onClick.RemoveAllListeners();
            btnStart.onClick.AddListener(enterBonusGame);

            btnCollect.onClick.RemoveAllListeners();
            btnCollect.onClick.AddListener(leaveBonusGame);

            bg_ani = bgWindows.GetComponent<Animator>();
            bgWindows.SetActive(false);
        }
        
        public virtual void OpenBGStartWindows(Action onCloseHandler = null, Action onToAutoPlayHandler = null)
        {
            btnStart.enabled = true;
            onClose = onCloseHandler;
            onToAutoPlay = onToAutoPlayHandler;
            ani_trigger = "BG_start";
            animationTrigger();
            audioOpenBGStartWindows?.Invoke();
            //AudioManager.instance.playAudioOnce(AudioPathProvider.getAudioPath(GameSound.BonusGame_Notify));
        }

        //結束bonusGame時呼叫
        public virtual void OpenBGEndWindows(ulong reward, Action onCloseHandler = null)
        {
            AudioManager.instance.fadeBgmAudio(0f);
            audioOpenBGEndWindows?.Invoke();
            //AudioManager.instance.playAudioOnce(AudioPathProvider.getAudioPath(GameSound.BonusGame_End));
            btnCollect.enabled = true;
            onClose = onCloseHandler;
            ani_trigger = "BG_end";
            bg_reward = reward.ToString("N0");
            animationTrigger();
        }

        //開啟開場動畫
        void animationTrigger()
        {
            numRewardText.text = bg_reward;
            bgWindows.SetActive(true);
            bg_ani.SetTrigger(ani_trigger);
        }

        //按下按鈕後的動作
        public virtual async void enterBonusGame()
        {
            CoroutineManager.StartCoroutine(delayCloseWin());
        }

        public IEnumerator delayCloseWin()
        {
            btnStart.enabled = false;
            AudioManager.instance.playAudioOnce(CommonAudioPathProvider.getAudioPath(BasicCommonSound.SwitchBtn));
            bg_ani.SetTrigger("BG_close");
            float length = 1.5f;

            AnimationClip[] clips = bg_ani.runtimeAnimatorController.animationClips;
            foreach (AnimationClip clip in clips)  //抓取動畫時長
            {
                if (clip.name.Equals("bonus_game_start_out") && ani_trigger == "BG_start")
                {
                    length = clip.length;
                    break;
                }
            }
            //await Task.Delay(TimeSpan.FromSeconds(length));
            yield return length;
            bgWindows.SetActive(false);
            //await closeWindowsAndMakeCutEffect();
            yield return CoroutineManager.scheduler.StartCoroutine(closeWindowsAndMakeCutEffect());
        }

        public virtual async void leaveBonusGame()
        {
            btnCollect.enabled = false;
            bg_ani.SetTrigger("BG_close");
            float length = 1.5f;

            AnimationClip[] clips = bg_ani.runtimeAnimatorController.animationClips;
            foreach (AnimationClip clip in clips)  //抓取動畫時長
            {
                if (clip.name.Equals("bonus_game_end_out") && ani_trigger == "BG_end")
                {
                    length = clip.length;
                    break;
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(length));
            bgWindows.SetActive(false);
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
