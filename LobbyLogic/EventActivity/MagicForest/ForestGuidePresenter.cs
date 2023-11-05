using CommonILRuntime.Module;
using CommonILRuntime.BindingModule;
using LobbyLogic.NetWork.ResponseStruct;
using UnityEngine;
using UnityEngine.UI;
using System;
using UniRx;
using CommonILRuntime.Extension;

namespace MagicForest
{
    class ForestGuidePresenter : ContainerPresenter
    {
        public override string objPath => $"{ForestDataServices.prefabPath}/mf_guide_step";
        public override UiLayer uiLayer { get => UiLayer.GameMessage; }

        public Subject<GuideStep> nowStepSub = new Subject<GuideStep>();
        Button guildBtn;
        RectTransform guildItemRect;
        Text guildStageNum;
        RectTransform guildStageRect;
        GameObject stepThreeMsgObj;

        GameObject[] guildObjs = new GameObject[5];
        int guildStep;
        Action<RectTransform> addItemGroup;
        Action backItemGroup;

        GuideStep[] stepSrot = new GuideStep[] { GuideStep.Step1, GuideStep.Step2, GuideStep.Step3, GuideStep.Step4, GuideStep.Step5, GuideStep.Pass };
        public override void initContainerPresenter()
        {
            resOrder = new string[] { AssetBundleData.getBundleName(BundleType.MagicForest) };
            base.initContainerPresenter();
        }

        public override void initUIs()
        {
            guildBtn = getBtnData("tutorial_step_btn");
            guildStageNum = getTextData("guild_stage_num");
            guildStageRect = getRectData("guild_stage_layout");
            guildItemRect = getRectData("guild_item_dummy");
            stepThreeMsgObj = getGameObjectData("step_3_msg_obj");

            for (int i = 0; i < guildObjs.Length; ++i)
            {
                guildObjs[i] = getGameObjectData($"guild_{i + 1}_obj");
            }
        }

        public override void init()
        {
            guildBtn.onClick.AddListener(toNextGuild);
            ForestDataServices.subGuildNextStep(isShowing =>
            {
                if (!isShowing)
                {
                    toNextGuild();
                }
            });
            ForestDataServices.guildGrassClickStepSub.Subscribe(showGrassItemPresenter).AddTo(uiGameObject);
        }

        public void openGuildPresenter(MagicForestInitResponse initData)
        {
            guildStageNum.text = $"{initData.Level}/{initData.StageRewards[initData.StageRewards.Length - 1].StageNum}";
            addGuildStageItem(initData.StageRewards);
        }

        public void startGuild(int guildStep = 0)
        {
            this.guildStep = guildStep;
            openNowGuild();
        }

        public void addGuildItem(Action<RectTransform> addItemGroupEvent)
        {
            addItemGroup = addItemGroupEvent;
        }

        public void setBackItemEvent(Action backEvent)
        {
            backItemGroup = backEvent;
        }

        void showGrassItemPresenter(GrassItemNodePresenter itemNodePresenter)
        {
            stepThreeMsgObj.setActiveWhenChange(false);
            Debug.Log($"show GrassItem : {itemNodePresenter.rewardItemKind}");
            if (GrassItemKind.Leprechaun == itemNodePresenter.rewardItemKind)
            {
                itemNodePresenter.setGuildLeprechaunMeshOrder();
            }
        }

        void toNextGuild()
        {
            guildStep++;
            openNowGuild();
        }

        void openNowGuild()
        {
            if (guildStep > stepSrot.Length - 1)
            {
                return;
            }
            GuideStep nowStep = stepSrot[guildStep];
            nowStepSub.OnNext(nowStep);
            guildBtn.interactable = GuideStep.Step3 != nowStep;
            switch (nowStep)
            {
                case GuideStep.Step3:
                    if (null != addItemGroup)
                    {
                        addItemGroup(guildItemRect);
                    }
                    break;

                case GuideStep.Step4:
                    if (null != backItemGroup)
                    {
                        backItemGroup();
                    }
                    break;
            }

            for (int i = 0; i < guildObjs.Length; ++i)
            {
                guildObjs[i].setActiveWhenChange((i + 1) == (int)nowStep);
            }

            if (GuideStep.Pass == nowStep)
            {
                ForestDataServices.disposeGuildNextStep();
                clear();
            }
        }
        void addGuildStageItem(MagicForestStageReward[] stageReward)
        {
            GameObject tempObj = ResourceManager.instance.getGameObjectWithResOrder("prefab/activity/magic_forest/stage_level_item",resOrder);
            int guildStageItemCount = 10;
            for (int i = 0; i < guildStageItemCount; ++i)
            {
                StageItemNode itemNode = UiManager.bindNode<StageItemNode>(GameObject.Instantiate(tempObj, guildStageRect));
                itemNode.setEffectActive(i <= 0);
                if (null != Array.Find(stageReward, reward => reward.StageNum == (i + 1)))
                {
                    itemNode.setIconImage(StageStatus.End);
                }
                else
                {
                    itemNode.setIconImage(StageStatus.Normal);
                }
                itemNode.isCheck(false);
            }
        }
    }

    enum GuideStep
    {
        Step1 = 1,
        Step2,
        Step3,
        Step4,
        Step5,
        Pass,
    }
}
