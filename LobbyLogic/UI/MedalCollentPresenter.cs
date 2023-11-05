using CommonILRuntime.Module;
using UnityEngine;
using UnityEngine.UI;
using LobbyLogic.Common;
using DG.Tweening;
using System;
using Shop;
using LobbyLogic.Audio;
using CommonService;
using CommonILRuntime.BindingModule;

namespace Lobby.UI
{
    public class MedalCollentPresenter : NodePresenter
    {
        #region UIs
        Text stateMoney_2;
        Text stateMoney_3;
        Text stateTime_3;
        GameObject medalTipsObj;

        Image medalMaskBG;
        RectTransform medalsRootTrans;
        Button medalOpenBtn;
        Button medalCloseBtn;
        GameObject medalShadowBG;
        GameObject medalGroup;

        Button tipBtn;

        Image[] medalImgs = new Image[4];
        GameObject[] medalStateObjs = new GameObject[3];
        #endregion

        Button testOpenGoldBtn;

        float medalCloseY;

        public override void initUIs()
        {
            testOpenGoldBtn = getBtnData("coming_soon_btn");
            //testOpenGoldBtn.onClick.AddListener(openGiftPresenter);

            stateMoney_2 = getTextData("medal_state_2_money");
            stateMoney_3 = getTextData("medal_state_3_money");
            stateTime_3 = getTextData("medal_state_3_time");

            medalTipsObj = getGameObjectData("medal_tips_obj");

            medalMaskBG = getImageData("medal_mask_bg");
            medalsRootTrans = getBindingData<RectTransform>("medal_state_root");
            medalOpenBtn = getBtnData("medal_state_open_btn");
            medalCloseBtn = getBtnData("medal_state_close_btn");
            medalShadowBG = getGameObjectData("medal_shadow_obj");
            medalGroup = getGameObjectData("medal_group");

            tipBtn = getBtnData("tip_btn");

            medalCloseY = medalsRootTrans.anchoredPosition.y;

            for (int i = 0; i < medalStateObjs.Length; ++i)
            {
                medalStateObjs[i] = getGameObjectData($"medal_state_{i + 1}");
            }

            for (int i = 0; i < medalImgs.Length; ++i)
            {
                medalImgs[i] = getImageData($"medal_{i + 1}_img");
            }
        }

        void openGiftPresenter()
        {
            UiManager.getPresenter<ShopGiftPresenter>().open();
        }

        public override void init()
        {
            medalOpenBtn.onClick.AddListener(clickCloseMedal);
            medalCloseBtn.onClick.AddListener(openMedal);
            tipBtn.onClick.AddListener(openTip);
            medalOpenBtn.gameObject.setActiveWhenChange(false);
            medalGroup.setActiveWhenChange(false);
        }

        void openMedalBGs(bool isOpen)
        {
            medalMaskBG.raycastTarget = isOpen;
            medalShadowBG.gameObject.setActiveWhenChange(isOpen);
        }

        bool isMedlaOpen;

        void clickCloseMedal()
        {
            AudioManager.instance.playAudioOnce(CommonAudioPathProvider.getAudioPath(BasicCommonSound.InfoBtn));
            closeMedal();
        }

        public void closeMedal()
        {
            openMedalBGs(false);
            medalOpenBtn.interactable = false;
            isMedlaOpen = false;
            changeMedalPos(medalCloseY, CommonUtil.medlaCloseTime, CommonUtil.medlaCloseType);
        }

        public void openMedal()
        {
            AudioManager.instance.playAudioOnce(CommonAudioPathProvider.getAudioPath(BasicCommonSound.InfoBtn));
            openMedalBGs(true);
            medalCloseBtn.interactable = false;
            isMedlaOpen = true;
            changeMedalPos(0, CommonUtil.medlaOpenTime, CommonUtil.medlaOpenType);
        }

        void changeMedalPos(float endPos, float moveTime, Ease moveType)
        {
            if (medalsRootTrans.anchoredPosition.y == endPos)
            {
                medalMoveComplete();
                return;
            }
            medalsRootTrans.anchPosMoveY(endPos, (float)TimeSpan.FromSeconds(moveTime).TotalSeconds, easeType: moveType, onComplete: medalMoveComplete);
        }

        void medalMoveComplete()
        {
            medalOpenBtn.gameObject.setActiveWhenChange(isMedlaOpen);
            medalCloseBtn.gameObject.setActiveWhenChange(!isMedlaOpen);
            medalCloseBtn.interactable = true;
            medalOpenBtn.interactable = true;
        }

        void openTip()
        {
            AudioManager.instance.playAudioOnce(CommonAudioPathProvider.getAudioPath(BasicCommonSound.InfoBtn));
            medalTipsObj.setActiveWhenChange(true);
        }

        public override void open()
        {
            int showID = MedalData.medalStates.Count;
            for (int i = 0; i < medalImgs.Length; ++i)
            {
                var medalImg = medalImgs[i];
                medalImg.gameObject.setActiveWhenChange(false);
                if (i < showID)
                {
                    medalImg.gameObject.setActiveWhenChange(true);
                    medalImg.sprite = ShopDataStore.getShopSprite($"medal_{MedalData.medalStates[i].ToString().ToLower()}");
                }
            }
            closeMedal();
            base.open();
        }
    }
}
