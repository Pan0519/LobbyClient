using CommonILRuntime.BindingModule;
using CommonILRuntime.Module;
using Services;
using Service;
using System;
using CommonService;
using UnityEngine;
using UnityEngine.UI;
using LobbyLogic.NetWork.ResponseStruct;
using CommonPresenter;
using UniRx;
using LobbyLogic.Audio;
using HighRoller;

namespace StayMiniGame
{
    public class StayMiniGameBoxPresenter : NodePresenter
    {
        #region UIs
        Text priceTxt;
        CustomBtn collectBtn;
        GameObject countObj;
        Text countTxt;
        Animator boxAnim;
        GameObject effectObj;
        #endregion

        TimerService timer = new TimerService();
        StayGameType openBoxType;
        Action clickAction = null;
        public override void initUIs()
        {
            priceTxt = getTextData("price");
            collectBtn = getCustomBtnData("btn_collect");
            countObj = getGameObjectData("obj_count");
            countTxt = getTextData("count_text");
            boxAnim = getAnimatorData("box_anim");
            effectObj = getGameObjectData("effect_obj");
        }

        public override void init()
        {
            boxAnim.enabled = true;
            collectBtn.clickHandler = onCollect;
            collectBtn.pointerDownHandler = () =>
            {
                setEffectObjActive(false);
            };
            collectBtn.pointerUPHandler = () =>
            {
                setEffectObjActive(true);
            };

            clickAction = null;
        }
        public void setOpenBoxType(StayGameType openBoxType)
        {
            this.openBoxType = openBoxType;
            setData(MiniGameConfig.instance.getStayGameData(openBoxType).endTime);
        }

        public void setClickAction(Action action)
        {
            clickAction = action;
        }

        void setEffectObjActive(bool active)
        {
            if (active)
            {
                active = isBtnIInteractable;
            }
            effectObj.setActiveWhenChange(active);
        }

        async void setData(DateTime endTime)
        {
            await StayGameDataStore.setVIPProfitValue(openBoxType);
            float price = DataStore.getInstance.playerInfo.coinExchangeRate * StayGameDataStore.getBoxMaxTimes(openBoxType) * (1.0f + StayGameDataStore.vipMakeup) * StayGameDataStore.multiplierEnergyMakeup;
            priceTxt.text = price.ToString("N0");
            CompareTimeResult compareTimeResult = UtilServices.compareTimeWithNow(endTime);
            bool isCollectBtnActive = CompareTimeResult.Earlier == compareTimeResult;
            countObj.setActiveWhenChange(!isCollectBtnActive);
            collectBtn.gameObject.setActiveWhenChange(isCollectBtnActive);
            collectBtn.interactable = isCollectBtnActive;
            if (CompareTimeResult.Earlier == compareTimeResult)
            {
                boxAnim.SetTrigger("get");
                return;
            }
            timer.StartTimer(endTime, setTime);
        }

        void setTime(TimeSpan lastTime)
        {
            countTxt.text = UtilServices.toTimeStruct(lastTime).toTimeString();

            if (lastTime <= TimeSpan.Zero)
            {
                countObj.setActiveWhenChange(false);
                collectBtn.gameObject.setActiveWhenChange(true);
                boxAnim.SetTrigger("get");
            }
        }

        async void onCollect()
        {
            setCollectBtnInteractable(false);
            AudioManager.instance.playAudioOnce(CommonAudioPathProvider.getAudioPath(BasicCommonSound.InfoBtn));
            StayGameBonusRedeem bonusRedeem = await AppManager.lobbyServer.stayGameBonusRedeem(StayGameDataStore.getRedeemStr(openBoxType));
            DataStore.getInstance.playerInfo.myWallet.commit(bonusRedeem.wallet);
            HighRollerDataManager.instance.addPassPoints(bonusRedeem.passPoints);
            StayGameDataStore.setHighRollerBoard(bonusRedeem.highRoller);
            StayGameDataStore.bonusAmount = bonusRedeem.bonusAmount;
            StayGameDataStore.setBonusReward(bonusRedeem.multipliers[0] * 0.001f);
            UiManager.getPresenter<StayMiniGameCutscenesPresenter>().openCutscenes(openBoxType);
            DateTime endTime = StayGameDataStore.refreshData(openBoxType, bonusRedeem.info);
            boxAnim.SetTrigger("normal");
            setData(endTime);
            clickAction?.Invoke();
        }
        bool isBtnIInteractable;
        void setCollectBtnInteractable(bool enable)
        {
            collectBtn.setInteractable(enable);
            isBtnIInteractable = enable;
            setEffectObjActive(enable);
        }

        public void executeTimer()
        {
            if (null != timer)
            {
                timer.ExecuteTimer();
            }
        }
    }
}
