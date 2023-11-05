using CommonILRuntime.Module;
using FarmBlast;
using CommonILRuntime.BindingModule;
using EventActivity;
using UniRx;
using System;
using LobbyLogic.NetWork.ResponseStruct;
using Debug = UnityLogUtility.Debug;

namespace Event.Common
{
    class TreasureBoxChestNode : NodePresenter
    {
        TreasuerBoxNodePresenter[] boxNodePresenter = new TreasuerBoxNodePresenter[4];

        public Action<TreasuerBoxNodePresenter> boxClick;
        public override void initUIs()
        {

        }

        public override void init()
        {
            for (int i = 0; i < boxNodePresenter.Length; ++i)
            {
                var boxNode = getNodeData($"treasure_node_{i + 1}");
                TreasuerBoxNodePresenter nodePresenter = UiManager.bindNode<TreasuerBoxNodePresenter>(boxNode.cachedGameObject);
                nodePresenter.observeClick.Subscribe(sendBox);
                nodePresenter.initBoxData(i);
                boxNodePresenter[i] = nodePresenter;
            }
        }

        public void setBoxData(TreasureBox[] boxData)
        {
            if (boxData.Length != boxNodePresenter.Length)
            {
                Debug.LogError($"baseResponse.TreasureBox.Length {boxData.Length} != boxNodePresenter.Length {boxNodePresenter.Length}");
                return;
            }
            for (int i = 0; i < boxData.Length; ++i)
            {
                boxNodePresenter[i].initTreasureData(boxData[i]);
            }
        }

        public void updateBoxStatus(int boxID, string type, long countDownTime)
        {
            boxNodePresenter[boxID].updateBoxType(type, countDownTime);
        }

        public void addBox(string type, long countDownTime)
        {
            var emptyBox = getEmptyBox();
            if (null != emptyBox)
            {
                emptyBox.updateBoxType(type, countDownTime);
            }
        }

        public TreasuerBoxNodePresenter getEmptyBox()
        {
            for (int i = 0; i < boxNodePresenter.Length; ++i)
            {
                var boxNode = boxNodePresenter[i];
                if (TreasureBoxType.None == boxNode.boxType)
                {
                    return boxNode;
                }
            }

            return null;
        }
        public void setBoxBtnInteractable(bool enable)
        {
            for (int i = 0; i < boxNodePresenter.Length; ++i)
            {
                var boxNode = boxNodePresenter[i];
                if (enable && TreasureBoxType.None != boxNode.boxType)
                {
                    boxNode.openBtnInteractable();
                    continue;
                }
                boxNode.closeBtnInteractable();
            }
        }

        void sendBox(TreasuerBoxNodePresenter selectBox)
        {
            if (null != boxClick)
            {
                boxClick(selectBox);
            }
        }
    }
}
