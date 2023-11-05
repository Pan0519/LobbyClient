using System;
using System.Threading.Tasks;
using Services;
using Lobby.Popup;
using LoginReward;
using NewPlayerGuide;
using CommonService;
using System.Collections.Generic;
using CommonILRuntime.BindingModule;
using LobbyLogic.NetWork.ResponseStruct;
using Lobby.UI;
using Shop.LimitTimeShop;
using Service;
using EventActivity;
using UnityEngine;
using UnityEngine.UI;
using UniRx;

namespace Lobby
{
    public class LobbyStartPopSortManager
    {
        static LobbyStartPopSortManager _instance;
        public static LobbyStartPopSortManager instance
        {
            get
            {
                if (null == _instance)
                {
                    _instance = new LobbyStartPopSortManager();
                }
                return _instance;
            }
        }
        bool isAlreadyShowPops { get { return nowPopShowStep >= popsShowOrder.Count; } }
        public DailyReward dailyReward { get; private set; } = null;
        SpecialOfferFirst offerFirst = null;
        int nowPopShowStep = -1;
        Dictionary<PopShowStep, Action> popsShowOrder = new Dictionary<PopShowStep, Action>();
        public void finishShowPopPages()
        {
            closeMask();
            nowPopShowStep = popsShowOrder.Count;
        }

        private GameObject maskObj = null;
        IDisposable closeMaskObjDis = null;
        private void creatMask()
        {
            maskObj = new GameObject();
            maskObj.name = "Mask";
            RectTransform rect = maskObj.AddComponent<RectTransform>();
            rect.SetParent(UiRoot.instance.getNowScreenOrientationGameMsgRoot());
            rect.localPosition = Vector3.zero;
            rect.localScale = Vector3.one;
            rect.sizeDelta = new Vector2(1560, 1200);
            Image image = maskObj.AddComponent<Image>();
            image.raycastTarget = true;
            image.color = new Color(0, 0, 0, 0.01f);
            closeMaskObjDis = Observable.Timer(TimeSpan.FromSeconds(2.0f)).Subscribe(_ =>
            {
                closeMask();
            });
        }

        private async void closeMask()
        {
            UtilServices.disposeSubscribes(closeMaskObjDis);
            if (null == maskObj)
            {
                return;
            }
            await Task.Delay(TimeSpan.FromSeconds(1f));
            GameObject.DestroyImmediate(maskObj);
            maskObj = null;
        }

        public async void startShowPopPages()
        {
            creatMask();
            if (isAlreadyShowPops || false == SaveTheDog.SaveTheDogMapData.instance.isDogGuideComplete)
            {
                closeMask();
                return;
            }
            if (DataStore.getInstance.guideServices.getSaveGuideStatus() != GuideStatus.Completed)
            {
                closeMask();
                TransitionxPartyServices.instance.closeTransitionPage();
                showGuide();
                return;
            }
            BindingLoadingPage.instance.open();
            if (ApplicationConfig.isLoadFromAB)
            {
                var activityRes = await AppManager.lobbyServer.getActivity();
                if (null == activityRes)
                {
                    startToNextPop();
                    return;
                }
                ActivityDataStore.nowActivityInfo = activityRes.activity;
                string activityBundleName = $"lobby_publicity_{ActivityDataStore.getActivityEntryPrefabName(activityRes.activity.activityId)}";
                AssetBundleManager.Instance.preloadBundles(activityBundleName, success =>
                {
                    startToNextPop();
                });
                return;
            }
            startToNextPop();
        }

        async void startToNextPop()
        {
            if (popsShowOrder.Count <= 0)
            {
                addPopShowOrder(PopShowStep.HighRoller, getHighRollerRecord);
                addPopShowOrder(PopShowStep.DailyReward, showPopDiailyReward);
                addPopShowOrder(PopShowStep.ActivityBanner, showPopups);
                addPopShowOrder(PopShowStep.LimitTimeFirst, showLimitFirstPage);
                if (false == SaveTheDog.SaveTheDogMapData.instance.isSkipSaveTheDog)
                {
                    addPopShowOrder(PopShowStep.SaveTheDog, showSaveTheDog);
                }
            }
            await Task.Delay(TimeSpan.FromSeconds(0.2f));
            toNextPop();
        }

        void addPopShowOrder(PopShowStep step, Action callback)
        {
            if (popsShowOrder.ContainsKey(step))
            {
                return;
            }

            popsShowOrder.Add(step, callback);
        }

        public void setDailyReward(DailyReward dailyReward)
        {
            this.dailyReward = dailyReward;
        }

        public async void getLimitFirstData()
        {
            var productDataResponse = await AppManager.lobbyServer.getSpecialOffer();
            offerFirst = productDataResponse.firstPurchase;
            if (null == offerFirst)
            {
                DataStore.getInstance.limitTimeServices.limitSaleFinish();
            }
        }

        async void getHighRollerRecord()
        {
            await HighRoller.HighRollerDataManager.instance.getHighUserRecordAndCheck(toNextPop);
        }

        public void toNextPop()
        {
            if (nowPopShowStep >= popsShowOrder.Count - 1)
            {
                closeMask();
                return;
            }
            nowPopShowStep++;
            Action nowShowPosEvent;
            if (popsShowOrder.TryGetValue((PopShowStep)nowPopShowStep, out nowShowPosEvent))
            {
                nowShowPosEvent();
            }
            BindingLoadingPage.instance.close();
        }

        void showPopDiailyReward()
        {
            if (null == dailyReward || dailyReward.rewards.Length <= 0 || dailyReward.cumulativeDays <= 0)
            {
                toNextPop();
                return;
            }

            showDailyReward();
        }

        public void showDailyReward()
        {
            LoginRewardServices.instance.startRunReward(dailyReward);
            dailyReward = null;
        }

        /// <summary>
        /// 彈窗活動
        /// </summary>
        void showPopups()
        {
            PopupManager.Instance.beginPopups(toNextPop);
        }

        void showLimitFirstPage()
        {
            if (null == offerFirst)
            {
                toNextPop();
                return;
            }
            LimitTimeShopManager.getInstance.openLimitTimeFirstPage(toNextPop);
        }

        void showSaveTheDog()
        {
            if (SaveTheDog.SaveTheDogMapData.instance.isAlreadyReward)
            {
                toNextPop();
                return;
            }
            UiManager.getPresenter<SaveTheDog.SaveTheDogPublicPresenter>().open();
        }

        void showGuide()
        {
            UiManager.getPresenter<XPartyPagePresenter>().openGuidePage((int)DataStore.getInstance.guideServices.getSaveGuideStatus());
        }
    }

    public enum PopShowStep
    {
        HighRoller = 0,
        DailyReward,
        ActivityBanner,
        LimitTimeFirst,
        SaveTheDog,
    }
}
