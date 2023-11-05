using UnityEngine;
using UnityEngine.UI;
using CommonPresenter;
using CommonILRuntime.BindingModule;
using CommonILRuntime.Module;
using UniRx;
using System;
using Service;
using Lobby;

namespace SaveTheDog
{
    class SaveTheDogPublicPresenter : SystemUIBasePresenter
    {
        public override string objPath => "prefab/activity_publicity/save_the_dog/save_the_dog_publicity";

        public override UiLayer uiLayer { get => UiLayer.System; }

        Animator showAnim;
        Text rewardText;
        Button toPlayBtn;
        Button closeBtn;
        RectTransform rewardGroupRect;
        bool openSaveDogMap;

        public override void initContainerPresenter()
        {
            resOrder = new string[] { AssetBundleData.getBundleName(BundleType.PublicitySaveTheDog) };
            base.initContainerPresenter();
        }

        public override void initUIs()
        {
            showAnim = getAnimatorData("show_anim");
            rewardText = getTextData("reward_txt");
            toPlayBtn = getBtnData("play_btn");
            closeBtn = getBtnData("close_btn");
            rewardGroupRect = getRectData("reward_pack_group");
        }

        public override void init()
        {
            base.init();
            toPlayBtn.onClick.AddListener(toPlayClick);
            closeBtn.onClick.AddListener(closeClick);
            closeBtn.gameObject.setActiveWhenChange(false);
        }

        void toPlayClick()
        {
            toPlayBtn.interactable = false;
            openSaveDogMap = true;
            closeBtnClick();
        }

        void closeClick()
        {
            closeBtn.interactable = false;
            closeBtnClick();
        }

        public override void animOut()
        {
            if (openSaveDogMap)
            {
                TransitionxPartyServices.instance.openTransitionPage();
                UiManager.getPresenter<SaveTheDogMapPresenter>().open();
            }
            LobbyStartPopSortManager.instance.toNextPop();
            clear();
        }

        public override Animator getUiAnimator()
        {
            return showAnim;
        }

        public override async void open()
        {
            openSaveDogMap = false;
            var mapInfo = await WebRequestText.instance.loadTextFromServer("newbie_adventure_setting");
            var mapInfoJson = LitJson.JsonMapper.ToObject<NewbieAdventureSetting>(mapInfo);
            SaveTheDogMapData.instance.setMapInfo(mapInfoJson.newbieAdventureSetting);

            rewardText.text = SaveTheDogMapData.instance.totalRewardMoney.ToString("N0");
            base.open();
            LayoutRebuilder.ForceRebuildLayoutImmediate(rewardGroupRect);
            Observable.Timer(TimeSpan.FromSeconds(3f)).Subscribe(_ =>
            {
                closeBtn.gameObject.setActiveWhenChange(true);
            }).AddTo(uiGameObject);
        }

    }
}
