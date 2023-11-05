using Binding;
using CommonILRuntime.BindingModule;
using CommonILRuntime.Module;
using Lobby.Common;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CommonPresenter;
using CommonService;

namespace StayMiniGame
{
    public class StayMiniGameMainPresenter : SystemUIBasePresenter
    {
        public override string objPath => "prefab/stay_minigame/stay_minigame_main";
        public override UiLayer uiLayer { get { return UiLayer.GameMessage; } }
        protected override BackHideBehaviour hideBehaviour => BackHideBehaviour.CanDoBoth;

        #region UIs
        Button closeBtn;
        BindingNode silverBox;
        BindingNode goldenBox;
        #endregion

        StayMiniGameBoxPresenter silverBoxPreseter;
        StayMiniGameBoxPresenter goldenBoxPreseter;

        List<int> expList { get { return DataStore.getInstance.miniGameData.expList; } }
        List<int> bonusList { get { return DataStore.getInstance.miniGameData.bonusList; } }

        List<Text> bonusNumTxt = new List<Text>();
        List<Image> bonusImg = new List<Image>();
        Font normalFont;
        Font garyFont;
        public override void initContainerPresenter()
        {
            resOrder = new string[] { AssetBundleData.getBundleName(BundleType.StayMinigame) };
            base.initContainerPresenter();
        }
        public override void initUIs()
        {
            closeBtn = getBtnData("closeButton");
            silverBox = getNodeData("silver_box");
            goldenBox = getNodeData("golden_box");

            for (int i = 0; i < bonusList.Count; ++i)
            {
                int id = i + 1;
                var numTxt = getTextData($"multiplier_num_0{id}");
                numTxt.text = $"x{bonusList[i]}";
                bonusNumTxt.Add(numTxt);
                bonusImg.Add(getImageData($"price_bar_0{id}"));
            }
        }
        public override void init()
        {
            base.init();
            normalFont = ResourceManager.instance.loadWithResOrder<Font>("prefab/stay_minigame/font/num_stay_reach", resOrder);
            garyFont = ResourceManager.instance.loadWithResOrder<Font>("prefab/stay_minigame/font/num_unreached", resOrder);
            closeBtn.onClick.AddListener(closeClick);

            silverBoxPreseter = UiManager.bindNode<StayMiniGameBoxPresenter>(silverBox.cachedGameObject);
            goldenBoxPreseter = UiManager.bindNode<StayMiniGameBoxPresenter>(goldenBox.cachedGameObject);
        }

        void closeClick()
        {
            silverBoxPreseter.executeTimer();
            goldenBoxPreseter.executeTimer();
            closeBtnClick();
        }

        public override void open()
        {
            setBarData(StayGameDataStore.multiplierEnergy);
            base.open();
        }

        public override void animOut()
        {
            clear();
            if (DataStore.getInstance.guideServices.nowStatus != Services.GuideStatus.Completed)
            {
                DataStore.getInstance.guideServices.toNextStep();
            }
        }

        void setBarData(int exp)
        {
            int maxExp = expList[expList.Count - 1];
            bool isFull = exp >= maxExp;
            if (isFull)
            {
                StayGameDataStore.multiplierEnergyMakeup = bonusList[bonusList.Count - 1];
                setFullBar();
            }
            else
            {
                for (int i = 0; i < bonusNumTxt.Count; ++i)
                {
                    int expID = i;
                    var bonusTxt = bonusNumTxt[i];
                    var img = bonusImg[i];
                    var unitExp = expList[expID];
                    if (exp >= unitExp)
                    {
                        StayGameDataStore.multiplierEnergyMakeup = bonusList[i];
                        bonusTxt.font = normalFont;
                        img.fillAmount = 1;
                        continue;
                    }
                    float lastExp = expList[expID - 1];
                    float remainingExp = exp - lastExp;
                    float progress = 0;

                    if (remainingExp > 0)
                    {
                        float nowFullExp = unitExp - lastExp;
                        progress = (float)(remainingExp) / (float)nowFullExp;
                    }

                    img.fillAmount = progress;
                    bonusTxt.font = garyFont;
                }
            }
            setBoxData();
        }

        void setFullBar()
        {
            for (int i = 0; i < bonusNumTxt.Count; ++i)
            {
                bonusNumTxt[i].font = normalFont;
                bonusImg[i].fillAmount = 1;
            }
        }
        void setBoxData()
        {
            silverBoxPreseter.setOpenBoxType(StayGameType.silver);
            goldenBoxPreseter.setOpenBoxType(StayGameType.gold);
            goldenBoxPreseter.setClickAction(() => { LocalNotificationManager.getInstance.addGoldenBoxNotifiction(); });
        }
    }
}
