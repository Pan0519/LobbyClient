using CommonILRuntime.BindingModule;
using CommonILRuntime.Module;
using LobbyLogic.NetWork.ResponseStruct;
using System;
using System.Collections.Generic;
using CommonPresenter;
using UnityEngine.UI;
using UnityEngine;
using CommonILRuntime.Services;
using Service;
using CommonILRuntime.Outcome;
using CommonService;
using Services;
using Network;
using UniRx;
using System.Threading.Tasks;
using LobbyLogic.Common;
using Lobby;

namespace HighRoller
{
    public static class HighRollerRewardManager
    {
        public static string objPath = "prefab/diamond_club/dc_board";

        public static async void openReward(HighRollerBoardResultResponse boardResponse, Action toNextPop = null)
        {
            if (null == boardResponse || string.IsNullOrEmpty(boardResponse.awardBoardType))
            {
                if (null != toNextPop)
                {
                    toNextPop();
                }
                return;
            }
            GamePauseManager.gamePause();
            Debug.Log($"get {boardResponse.awardBoardType} openReward");
            IHighRollerReward highRollerReward = null;
            switch (boardResponse.awardBoardType)
            {
                case "access-open":
                    highRollerReward = UiManager.getPresenter<OpenDiamondClubReward>();
                    break;

                case "access-close":
                    UiManager.getPresenter<CloseDiamondClubReward>().openRewardPage(toNextPop);
                    return;

                case "point-transfer-coin":
                    highRollerReward = UiManager.getPresenter<PointTransCoinReward>();
                    break;

                case "pass-transfer-coin":
                    highRollerReward = UiManager.getPresenter<PassTransCoinReward>();
                    break;
            }
            if (null == highRollerReward)
            {
                Debug.LogError($"get {boardResponse.awardBoardType} type highRollerReward is null");
                return;
            }
            highRollerReward.openReward(boardResponse);
            highRollerReward.setToNextPopCB(toNextPop);
            await Task.Delay(TimeSpan.FromSeconds(0.5f));
            await HighRollerDataManager.instance.getHighUserRecordAndCheck();
        }
    }

    public class OpenDiamondClubReward : SystemUIBasePresenter, IHighRollerReward
    {
        public override string objPath { get { return UtilServices.getOrientationObjPath($"{HighRollerRewardManager.objPath}_welcome"); } }

        public override UiLayer uiLayer { get => UiLayer.System; }

        Animator showAnim;
        Text expireInTxt;
        Button closeBtn;
        Button collectBtn;
        public override void initContainerPresenter()
        {
            resOrder = new string[] { AssetBundleData.getBundleName(BundleType.Crown) };
            base.initContainerPresenter();
        }
        public override void initUIs()
        {
            showAnim = getAnimatorData("ani_show");
            expireInTxt = getTextData("pass_expire_in_txt");
            closeBtn = getBtnData("close_btn");
            collectBtn = getBtnData("collect_btn");
        }

        public override void init()
        {
            base.init();
            closeBtn.onClick.AddListener(closeClick);
            collectBtn.onClick.AddListener(collectBtnClick);
        }

        public void setToNextPopCB(Action toNextPop)
        {

        }

        public void openReward(HighRollerBoardResultResponse highRoller)
        {
            expireInTxt.text = $"{LanguageService.instance.getLanguageValue("DiamondClub_BenefitsOpen_Text2")} {highRoller.expireDays} {LanguageService.instance.getLanguageValue("Time_Days")}";
        }

        void closeClick()
        {
            GamePauseManager.gameResume();
            closeBtnClick();
        }

        async void collectBtnClick()
        {
            if (GameOrientation.Portrait == await DataStore.getInstance.dataInfo.getNowGameOrientation())
            {
                await UIRootChangeScreenServices.Instance.justChangeScreenToLand();
            }

            UiManager.getPresenter<HighRollerMainPresenter>().open();
            closeBtnClick();
        }

        public override Animator getUiAnimator()
        {
            return showAnim;
        }

        public override void animOut()
        {
            clear();
        }
    }
    public class CloseDiamondClubReward : SystemUIBasePresenter
    {
        public override string objPath { get { return UtilServices.getOrientationObjPath($"{HighRollerRewardManager.objPath}_has_expired"); } }
        public override UiLayer uiLayer { get => UiLayer.GameMessage; }

        Animator showAnim;
        Button collectBtn;
        Action toNextPopCB;
        public override void initContainerPresenter()
        {
            resOrder = new string[] { AssetBundleData.getBundleName(BundleType.Crown) };
            base.initContainerPresenter();
        }
        public override void init()
        {
            base.init();
            collectBtn.onClick.AddListener(closeBtnClick);
        }

        public void openRewardPage(Action toNextPop)
        {
            toNextPopCB = toNextPop;
            open();
        }

        public override void initUIs()
        {
            showAnim = getAnimatorData("show_anim");
            collectBtn = getBtnData("collect_btn");
        }

        public override Animator getUiAnimator()
        {
            return showAnim;
        }

        public override void animOut()
        {
            if (null != toNextPopCB)
            {
                toNextPopCB();
            }
            GamePauseManager.gameResume();
            clear();
        }
    }

    public class TransCoinReward : SystemUIBasePresenter
    {
        Animator showAnim;
        Button collectBtn;
        Text coinTxt;
        RectTransform coinLayoutRect;
        Outcome outCome = null;
        Action toNextPopCB;
        public override void initContainerPresenter()
        {
            resOrder = new string[] { AssetBundleData.getBundleName(BundleType.Crown) };
            base.initContainerPresenter();
        }
        public override void initUIs()
        {
            showAnim = getAnimatorData("show_anim");
            collectBtn = getBtnData("collect_btn");
            coinTxt = getTextData("coin_txt");
            coinLayoutRect = getRectData("coin_reward_group");
        }

        public void setToNextPopCB(Action toNextPop)
        {
            toNextPopCB = toNextPop;
        }

        public override void init()
        {
            base.init();
            collectBtn.onClick.AddListener(coinFly);
        }

        public async void getRewardPack(string packID)
        {
            var rewardPacket = await AppManager.lobbyServer.getRewardPacks(packID);
            if (Result.OK != rewardPacket.result)
            {
                return;
            }
            var coinReward = Array.Find(rewardPacket.rewards, reward => reward.kind.Equals(UtilServices.outcomeCoinKey)).amount;
            coinTxt.text = coinReward.ToString("N0");
            outCome = Outcome.process(rewardPacket.rewards);
            LayoutRebuilder.ForceRebuildLayoutImmediate(coinLayoutRect);
        }

        void coinFly()
        {
            CoinFlyHelper.frontSFly(collectBtn.gameObject.GetComponent<RectTransform>(), DataStore.getInstance.playerInfo.myWallet.deprecatedCoin, DataStore.getInstance.playerInfo.myWallet.coin, onComplete: () =>
            {
                if (null != outCome)
                {
                    outCome.apply();
                }
                closePresenter();
            });
        }

        public override Animator getUiAnimator()
        {
            return showAnim;
        }

        public override void animOut()
        {
            if (null != toNextPopCB)
            {
                toNextPopCB();
            }
            GamePauseManager.gameResume();
            clear();
        }
    }

    public class PointTransCoinReward : TransCoinReward, IHighRollerReward
    {
        public override string objPath { get { return UtilServices.getOrientationObjPath($"{HighRollerRewardManager.objPath}_times_up"); } }
        public override UiLayer uiLayer { get => UiLayer.GameMessage; }

        Text daysTxt;
        Text pointTxt;

        List<GameObject> crownObjs = new List<GameObject>();
        public override void initContainerPresenter()
        {
            resOrder = new string[] { AssetBundleData.getBundleName(BundleType.Crown) };
            base.initContainerPresenter();
        }
        public override void initUIs()
        {
            base.initUIs();
            daysTxt = getTextData("days_txt");
            pointTxt = getTextData("point_txt");
            crownObjs.Add(getGameObjectData("crown_left_obj"));
            crownObjs.Add(getGameObjectData("crown_right_obj"));
        }
        public void openReward(HighRollerBoardResultResponse highRoller)
        {
            pointTxt.text = $"{highRoller.passPoints}";
            getRewardPack(highRoller.rewardPackId);
            HighRollerDataManager.instance.userRecordSub.Subscribe(setUserRecord).AddTo(uiGameObject);
        }

        void setUserRecord(HighRollerUserRecordResponse record)
        {
            var accessInfo = record.accessInfo;
            DateTime expireTime = UtilServices.strConvertToDateTime(accessInfo.expiredAt, DateTime.MaxValue);
            TimeStruct expireTimeStruct = UtilServices.toTimeStruct(expireTime.Subtract(UtilServices.nowTime));
            daysTxt.text = expireTimeStruct.toTimeString(LanguageService.instance.getLanguageValue("Time_Days"));
            int detailLength = accessInfo.details.Length;
            for (int i = 0; i < crownObjs.Count; ++i)
            {
                crownObjs[i].setActiveWhenChange(detailLength > i);
            }
        }
    }
    public class PassTransCoinReward : TransCoinReward, IHighRollerReward
    {
        public override string objPath { get { return UtilServices.getOrientationObjPath($"{HighRollerRewardManager.objPath}_unlocked"); } }
        public override UiLayer uiLayer { get => UiLayer.GameMessage; }
        public override void initContainerPresenter()
        {
            resOrder = new string[] { AssetBundleData.getBundleName(BundleType.Crown) };
            base.initContainerPresenter();
        }
        public void openReward(HighRollerBoardResultResponse highRoller)
        {
            getRewardPack(highRoller.rewardPackId);
        }
    }
}
