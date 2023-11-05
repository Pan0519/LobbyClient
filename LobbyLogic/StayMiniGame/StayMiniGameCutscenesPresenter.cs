using CommonILRuntime.Module;
using System;
using CommonILRuntime.BindingModule;
using UnityEngine;
using UnityEngine.UI;
using UniRx;
using UniRx.Triggers;
using Services;
using System.Collections.Generic;
using CommonPresenter;
using LobbyLogic.Audio;
using Lobby.Audio;

namespace StayMiniGame
{
    public class StayMiniGameCutscenesPresenter : ContainerPresenter
    {
        public override string objPath => "prefab/stay_minigame/stay_minigame_open_box";

        public override UiLayer uiLayer { get => UiLayer.System; }

        Animator boxAnm;

        StayGameType openBoxType;
        List<IDisposable> animTriggerDis = new List<IDisposable>();

        public override void initContainerPresenter()
        {
            resOrder = new string[] { AssetBundleData.getBundleName(BundleType.StayMinigame) };
            base.initContainerPresenter();
        }
        public override void initUIs()
        {
            boxAnm = getAnimatorData("box_anm");
        }

        public override void init()
        {
            var animTrigger = boxAnm.GetBehaviours<ObservableStateMachineTrigger>();
            for (int i = 0; i < animTrigger.Length; ++i)
            {
                animTriggerDis.Add(animTrigger[i].OnStateEnterAsObservable().ObserveOnMainThread().Subscribe(tapAnimTrigger).AddTo(uiGameObject));
            }
        }

        public StayMiniGameCutscenesPresenter openCutscenes(StayGameType type)
        {
            openBoxType = type;
            open();
            boxAnm.SetTrigger($"in_{type}");
            Observable.TimerFrame(50).Subscribe(_ =>
            {
                AudioManager.instance.playAudioOnce(AudioPathProvider.getAudioPath(BonusAudio.Fall));
            }).AddTo(uiGameObject);
            return this;
        }

        void tapAnimTrigger(ObservableStateMachineTrigger.OnStateInfo obj)
        {
            if (obj.StateInfo.IsName($"stay_open_box_{openBoxType}_tap"))
            {
                AudioManager.instance.playAudioOnce(AudioPathProvider.getAudioPath(BonusAudio.Open));
                return;
            }
            if (!obj.StateInfo.IsName($"stay_open_box_{openBoxType}_out"))
            {
                return;
            }
            animTriggerDis.Add(Observable.Timer(TimeSpan.FromSeconds(obj.StateInfo.length)).Subscribe(_ =>
            {
                UiManager.getPresenter<StayMiniGameAwardsPresenter>().open();
                clear();
            }).AddTo(uiGameObject));
        }

        public override void clear()
        {
            UtilServices.disposeSubscribes(animTriggerDis.ToArray());
            animTriggerDis.Clear();
            base.clear();
        }
    }
}
