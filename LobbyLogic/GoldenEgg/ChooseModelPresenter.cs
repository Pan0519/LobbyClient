using UnityEngine;
using UnityEngine.UI;
using CommonILRuntime.Module;
using CommonILRuntime.BindingModule;
using UniRx.Triggers;
using UniRx;
using System;
using System.Collections.Generic;
using Services;
using LobbyLogic.Audio;
using Lobby.Audio;

namespace GoldenEgg
{
    class ChooseModelPresenter : ContainerPresenter
    {
        public override string objPath
        {
            get
            {
                return UtilServices.getOrientationObjPath("prefab/golden_egg/golden_egg_choose");
            }
        }

        public override UiLayer uiLayer { get => UiLayer.GameMessage; }
        #region UIs
        Animator chooseAnim;
        Button tapBtn;
        #endregion

        ChooseModelNode chickenModelNode;
        ChooseModelNode gooseModelNode;

        List<IDisposable> animTriggerDisList = new List<IDisposable>();
        IDisposable animTimerDis;
        string animTriggerName;
        string inTriggerName;
        ItemProductData productData;
        public override void initUIs()
        {
            chooseAnim = getAnimatorData("choose_anim");
            tapBtn = getBtnData("tap_btn");
            chickenModelNode = UiManager.bindNode<ChooseModelNode>(getNodeData("chicken_node").cachedGameObject);
            gooseModelNode = UiManager.bindNode<ChooseModelNode>(getNodeData("goose_node").cachedGameObject);
        }

        public override void init()
        {
            chooseAnim.ResetTrigger("out");
            chickenModelNode.setAnimEnable(false);
            gooseModelNode.setAnimEnable(false);

            tapBtn.interactable = false;
            var animTriggers = chooseAnim.GetBehaviours<ObservableStateMachineTrigger>();

            for (int i = 0; i < animTriggers.Length; ++i)
            {
                var animTrigger = animTriggers[i];
                animTriggerDisList.Add(animTrigger.OnStateEnterAsObservable().ObserveOnMainThread().Subscribe(animEnterSubscribe));
            }
            tapBtn.onClick.AddListener(playOutAnim);
        }

        public void openChooseMode(ChooseMode chooseMode, ItemProductData productData, bool isMax)
        {
            chickenModelNode.setMaxObjEnable(isMax);
            gooseModelNode.setMaxObjEnable(isMax);

            open();
            animTriggerName = chooseMode.ToString().ToLower();
            checkTriggerInName();
            chooseAnim.SetTrigger(animTriggerName);
            this.productData = productData;
        }

        void checkTriggerInName()
        {
            inTriggerName = $"buy_egg_{animTriggerName}_in";
            if (UtilServices.getNowScreenOrientation == ScreenOrientation.Portrait || UtilServices.getNowScreenOrientation == ScreenOrientation.PortraitUpsideDown)
            {
                inTriggerName = $"{inTriggerName}_portrait";
            }
        }

        private void animEnterSubscribe(ObservableStateMachineTrigger.OnStateInfo obj)
        {
            animTimerDis = Observable.Timer(TimeSpan.FromSeconds(obj.StateInfo.length)).Subscribe(_ =>
             {
                 if (obj.StateInfo.IsName(inTriggerName))
                 {
                     chickenModelNode.setAnimEnable(true);
                     gooseModelNode.setAnimEnable(true);
                     tapBtn.interactable = true;
                     return;
                 }
                 UiManager.getPresenter<GoldenEggRewardPresenter>().setRewardItems(productData);
                 clear();
             });
        }

        void playOutAnim()
        {
            AudioManager.instance.playAudioOnce(AudioPathProvider.getAudioPath(ActivityBlastAudio.Open));
            tapBtn.interactable = false;
            chooseAnim.SetTrigger("out");
        }

        public override void clear()
        {
            animTriggerDisList.Add(animTimerDis);
            UtilServices.disposeSubscribes(animTriggerDisList.ToArray());
            base.clear();
        }
    }
}
