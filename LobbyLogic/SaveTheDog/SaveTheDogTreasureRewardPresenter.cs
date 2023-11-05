using CommonILRuntime.Module;
using UnityEngine.UI;
using UnityEngine;
using CommonILRuntime.Outcome;
using Services;
using CommonService;
using LobbyLogic.NetWork.ResponseStruct;

namespace SaveTheDog
{
    class SaveTheDogTreasureRewardPresenter : ContainerPresenter
    {
        public override string objPath => "prefab/save_the_dog/save_the_dog_finaltreasure";
        public override UiLayer uiLayer { get => UiLayer.System; }
        CustomTextSizeChange rewardCoinTxt;
        Button confirmBtn;

        Outcome outcome;
        NewbieAdventureRecord updateRecord;
        public override void initContainerPresenter()
        {
            resOrder = new string[] { AssetBundleData.getBundleName(BundleType.SaveTheDog) };
            base.initContainerPresenter();
        }
        public override void initUIs()
        {
            rewardCoinTxt = getBindingData<CustomTextSizeChange>("reward_coin_txt");
            confirmBtn = getBtnData("confirm_btn");
        }

        public override void init()
        {
            confirmBtn.onClick.AddListener(confirmClick);
        }

        public void openPage(CommonReward[] rewards, NewbieAdventureRecord newbieAdventureRecord)
        {
            updateRecord = newbieAdventureRecord;
            outcome = Outcome.process(rewards);
            ulong totalReward = 0;
            for (int i = 0; i < rewards.Length; ++i)
            {
                var reward = rewards[i];
                if (reward.kind.Equals(UtilServices.outcomeCoinKey))
                {
                    totalReward += reward.getAmount();
                }
            }

            rewardCoinTxt.text = totalReward.ToString("N0");
            LayoutRebuilder.ForceRebuildLayoutImmediate(rewardCoinTxt.transform.parent.transform as RectTransform);
        }

        void confirmClick()
        {
            outcome.apply();
            confirmBtn.interactable = false;
            if (SaveTheDogMapData.instance.nowStageID <= 0)
            {
                DataStore.getInstance.dataInfo.setChooseBetClass(ChooseBetClass.Regular, 0);
                UtilServices.reloadLobbyScene(openTransition: false);
                return;
            }
            if (null == updateRecord)
            {
                clear();
                return;
            }
            TransitionSaveDogServices.instance.openTransitionPage();
            clear();
            SaveTheDogMapData.instance.updateAdventureRecord(updateRecord);
        }

    }
}
