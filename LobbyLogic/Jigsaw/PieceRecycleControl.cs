using CommonILRuntime.Module;
using System;
using UnityEngine;
using UnityEngine.UI;
using LobbyLogic.Audio;
using CommonService;
using Lobby.Audio;

namespace Lobby.Jigsaw
{
    public class PieceRecycleControl : NodePresenter
    {
        public Action onAmontChange;

        //Amount Control
        public GameObject objAmountGroup;
        public Text textSelectCount;
        public Button buttonAdd;
        public Button buttonSub;

        //User Jigsaw Amount
        public GameObject objUserAmount;
        public Text textUserAmount;

        //StarIcon
        public GameObject objFantasySar;
        public int currentAmount { get; private set; } = 0;

        Image grayMaskImage;

        int maxAmount = 0;

        public override void initUIs()
        {
            base.initUIs();
            objAmountGroup = getGameObjectData("objAmountGroup");
            textSelectCount = getTextData("textSelectCount");
            buttonAdd = getBtnData("buttonAdd");
            buttonSub = getBtnData("buttonSub");

            objUserAmount = getGameObjectData("objUserAmount");
            textUserAmount = getTextData("textUserAmount");

            objFantasySar = getGameObjectData("objFantasySar");

            grayMaskImage = getImageData("grayMaskImage");
        }

        public override void init()
        {
            base.init();
            buttonAdd.onClick.AddListener(onAddAmountClick);
            buttonSub.onClick.AddListener(onSubAmountClick);
            setSelectCountText(0);
            grayMaskImage.enabled = false;
        }

        public void setMaxAmount(int amount)
        {
            objUserAmount.setActiveWhenChange(amount > 0);
            objAmountGroup.setActiveWhenChange(amount > 0);
            maxAmount = amount;
            updateAvaliableAmount();
        }

        public void showFantasyStar(bool show)
        {
            objFantasySar.setActiveWhenChange(show);
        }

        public void setSelectCount(int count)
        {
            if (count > maxAmount)
            {
                Debug.LogWarning($"PieceRecycleControl select overflow count: {count}, maxCount: {maxAmount}");
                return;
            }
            changeSelectCount(count);
        }

        void updateAvaliableAmount()
        {
            var remainAmount = maxAmount - currentAmount;
            textUserAmount.text = $"{remainAmount}";
            grayMaskImage.enabled = 0 == remainAmount;
        }

        void changeSelectCount(int count)
        {
            var clampedValue = clampAmount(count);
            bool valueChanged = currentAmount != clampedValue;
            currentAmount = clampedValue;
            setSelectCountText(currentAmount);
            updateAvaliableAmount();
            refreshButtonState();
            if (valueChanged)
            {
                onAmontChange?.Invoke();
            }
        }

        int clampAmount(int amount)
        {
            return Math.Max(0, Math.Min(amount, maxAmount));
        }

        void refreshButtonState()
        {
            buttonAdd.interactable = !(maxAmount == currentAmount);
            buttonSub.interactable = !(0 == currentAmount);
        }

        void setSelectCountText(int count)
        {
            textSelectCount.text = $"{count}";
        }

        public void onAddAmountClick()
        {
            AudioManager.instance.playAudioOnce(CommonAudioPathProvider.getAudioPath(BasicCommonSound.PlusbetBtn));
            changeSelectCount(currentAmount + 1);
        }

        void onSubAmountClick()
        {
            AudioManager.instance.playAudioOnce(CommonAudioPathProvider.getAudioPath(BasicCommonSound.MinusBetBtn));
            changeSelectCount(currentAmount - 1);
        }

    }
}
