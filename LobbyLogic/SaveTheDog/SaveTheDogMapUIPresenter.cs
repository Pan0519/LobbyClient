using Debug = UnityLogUtility.Debug;
using System.Collections.Generic;
using CommonILRuntime.Module;
using UnityEngine.UI;
using UnityEngine;
using Services;
using CommonILRuntime.BindingModule;
using Lobby.UI;
using System;
using UniRx;
using System.IO;
using CommonService;

namespace SaveTheDog
{
    public class SaveTheDogMapUIPresenter : ContainerPresenter
    {
        public override string objPath => "prefab/save_the_dog/save_the_dog_map_ui";
        public override UiLayer uiLayer { get { return UiLayer.System; } }
        protected override BackHideBehaviour hideBehaviour => BackHideBehaviour.HideMe;

        #region UI Obj
        private Button btnBack;
        private RectTransform stageLayout;
        Text totalRewardTxt;
        GameObject totalRewardObj;
        Binding.BindingNode stageBtnNode;
        #endregion

        Action closeClickCB;

        #region Other
        //大關卡物件清單
        private List<SaveTheDogMapBtnStage> stageList = new List<SaveTheDogMapBtnStage>();
        #endregion
        TimerService timerService = new TimerService();
        public override void initContainerPresenter()
        {
            resOrder = new string[] { AssetBundleData.getBundleName(BundleType.SaveTheDog) };
            base.initContainerPresenter();
        }
        public override void initUIs()
        {
            btnBack = getBtnData("back_btn");
            stageLayout = getRectData("stage_layout_rect");

            totalRewardObj = getGameObjectData("total_reward_obj");
            totalRewardTxt = getTextData("total_reward_txt");
            stageBtnNode = getNodeData("stage_btn_node");
        }

        public override void init()
        {
            stageBtnNode.cachedGameObject.setActiveWhenChange(false);
            btnBack.onClick.AddListener(onBtnClose);
            btnBack.gameObject.setActiveWhenChange(SaveTheDogMapData.instance.nowStageID > 0);
            addStageObj();
            setTotalReward();
        }

        private void onBtnClose()
        {
            ResourceManager.instance.releasePoolWithObj(stageBtnNode.cachedGameObject);
            clear();
            if (null != closeClickCB)
            {
                closeClickCB();
            }
        }

        public void setCloseClickCB(Action closeCB)
        {
            closeClickCB = closeCB;
        }

        private void addStageObj()
        {
            //產生stage物件
            for (var i = 0; i < SaveTheDogMapData.instance.maxStageAmount; i++)
            {
                PoolObject stageObj = ResourceManager.instance.getObjectFromPool(stageBtnNode.cachedGameObject, stageLayout.transform);
                if (!stageObj)
                {
                    continue;
                }
                var stageNode = UiManager.bindNode<SaveTheDogMapBtnStage>(stageObj.cachedGameObject);
                stageNode.setStageIndex(i);
                stageNode.open();
                stageNode.stageClickSub.Subscribe(onBtnStage).AddTo(stageNode.uiGameObject);
                stageList.Add(stageNode);
            }

            onBtnStage(stageList[SaveTheDogMapData.instance.nowStageID].stageIndex);
        }

        void setTotalReward()
        {
            string totalRewardMoneyFormat = SaveTheDogMapData.instance.totalRewardMoney.ToString("N0");
            totalRewardTxt.text = totalRewardMoneyFormat;
            LayoutRebuilder.ForceRebuildLayoutImmediate(totalRewardTxt.transform.parent.transform as RectTransform);
        }

        private void onBtnStage(int stageID)
        {
            if (stageID == SaveTheDogMapData.instance.nowOpenStageID)
            {
                return;
            }
            SaveTheDogMapData.instance.changeNowOpenStageID(stageID);

        }
    }
}